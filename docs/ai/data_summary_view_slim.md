# /view-data-summary — proposed payload reductions (deferred)

> Companion to [performance_risk_review.md](performance_risk_review.md) and
> [data_summary_report.md](data_summary_report.md). This document captures
> three proposed payload reductions for the `/view-data-summary` page that
> were analyzed and **deferred — no code change shipped**.

## Background

`/view-data-summary` paginates the `data_summary_view_report/year_of_death`
view through `/api/data-summary/{page}`. Each row is currently ~40 KB; a
typical tenant returns ~8 pages of 100 rows. Per-pod cost per request:

- The controller buffers each CouchDB page response as a string.
- It then parses it with Newtonsoft (`JsonConvert.DeserializeObject<...>`)
  into a typed `List<...>`.
- ASP.NET reserializes it into the response.
- The browser holds every row in memory before filtering and rendering.

`path_to_detail` is roughly 95 % of each row. It is a dictionary of
hundreds of paths, each mapped to a list of `{ value, count }` pairs
written by `c_generate_frequency_summary_report`. In every code path the
generator writes `count: 1`, so the `count` field is effectively constant.

## Proposals

### 3a — Drop fields the client never reads (view emit only)

Remove from the view emit:

- `type` (constant `'freq-measure'`)
- `host_state`
- `case_id` (the client uses the row-level document `_id` instead)
- `case_folder` (the server-side jurisdiction filter that consumed it
  was disabled — unrelated to this proposal)

Stored documents unchanged. Frontend consumers do not reference these
fields. Estimated savings: ~1–2 % per row.

Files that would change:

- [data-summary-view.json](../../source-code/mmria/mmria-server/database-scripts/data-summary-view.json)
- [data-summary-view.json (rebuild copy)](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIARebuild/database-scripts/data-summary-view.json)

Deployment: push the updated design document to each tenant's `report`
database; CouchDB triggers a one-shot view re-index. No application
restart, no document regeneration.

### 3b — Slim `path_to_detail` (view emit only)

In the view map, walk `doc.path_to_detail` and emit each entry as just
its `value` string instead of the `{ value, count: 1 }` wrapper.

Before (per-entry):

```json
{ "value": "(-)", "count": 1 }
```

After (per-entry):

```json
"(-)"
```

Stored documents unchanged. Two frontend consumer sites in
[view-data-summary/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/index.js)
would need to iterate strings directly and treat `count` as the implicit
constant `1`:

- `build_report` loop near line 408 (`for(const v of Object.keys(detail_item))`
  → `for(const detail_entry of detail_item)`; `detail_item[v].value`
  references replaced with the string itself; `detail_entry_count = 1`
  inlined).
- Per-key fan-out loop near line 736 (`element.value` → element string;
  `element.count > 0` guards removed because count was always 1).

Estimated savings: ~25–35 % per-row payload, scaling linearly through
the controller buffer, the Newtonsoft deserialize graph, the wire, and
the browser-side `data_list`. The CouchDB on-disk view index is also
smaller (no nested wrappers, no `count`).

Deployment: same as 3a — design-doc push only, no document regeneration.

Files that would change:

- both `data-summary-view.json` files above
- [view-data-summary/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/index.js)

### 3c — Schema change at the document level (next release)

Stop writing `count: 1` in `c_generate_frequency_summary_report`; store
`path_to_detail[<path>] = [<value>, ...]` directly on the `freq-*`
document. The view map then becomes a one-line pass-through that emits
`doc.path_to_detail` unchanged.

This is the cleanest end state but requires a per-tenant rebuild of all
`freq-*` documents — which is why it is deferred to the next release
window.

Files that would change:

- `c_generate_frequency_summary_report` (and any other writer that
  builds `path_to_detail`)
- both `data-summary-view.json` files
- the same two consumer sites in `view-data-summary/index.js` as 3b

Deployment: ship the code, then trigger the existing per-tenant
`freq-*` rebuild path. The view map can be updated in the same release
because both old (`{ value, count }`) and new (string) shapes will exist
during the rebuild window — the view should be deployed *after* the
rebuild completes, or the consumer should accept both shapes during the
transition.

## Status

**All three proposals are deferred.** Decision recorded April 23, 2026.
Code changes that had been drafted for 3a and 3b were rolled back; no
files in the repository carry the slim view shape today.

## Related future work (separate from 3a/3b/3c)

- **#4 — Reduce view for the chart.** If the chart can be served from a
  reduced view, the entire per-row pipeline collapses to a single small
  request regardless of case count.
- **#5 — Re-enable the server-side jurisdiction filter.** Correctness +
  perf; pending product confirmation.
- **#6 — Skip the controller's Newtonsoft round-trip.** Stream the
  CouchDB body straight through; same trick already applied in Issue R.
