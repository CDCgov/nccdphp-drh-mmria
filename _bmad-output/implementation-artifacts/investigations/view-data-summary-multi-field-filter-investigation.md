# Investigation: View Data Summary Check — multi-field selection filter does not filter

## Hand-off Brief

1. **What happened.** On `/view-data-summary`, checking two or more specific fields (not "All Fields") in the Field filter has no effect on the results table — all fields still render, exactly as if "All Fields" were selected. Confirmed in `render_search_result_item` in [renderer.js](../../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/renderer.js#L662-L679).
2. **Where the case stands.** Root cause Confirmed via source trace. No implementation performed (out of scope for this workflow).
3. **What's needed next.** One-line-scope fix to the exclusion guard (see Fix direction) — trivial, ready for `bmad-quick-dev`.

## Case Info

| Field            | Value                                                                      |
| ---------------- | --------------------------------------------------------------------------- |
| Ticket           | N/A (client-reported, verbal/support description)                          |
| Date opened      | 2026-07-22                                                                  |
| Status           | Concluded (root cause Confirmed)                                            |
| System           | mmria-server, client-side JS, `/view-data-summary` page                    |
| Evidence sources | Source code (`renderer.js`, `index.js`), git history, prior story artifact |

## Problem Statement

Client reported: on the "View Data Summary Check" page, filtering by "all" and then selecting specific fields does not correctly filter the results — the table still displays all fields regardless of the specific-field selection.

## Evidence Inventory

| Source                                                                 | Status    | Notes |
| ----------------------------------------------------------------------- | --------- | ----- |
| `view-data-summary/renderer.js`                                        | Available | Contains `render_field_filter` (checkbox UI) and `render_search_result_item` (actual row/field rendering + exclusion logic). |
| `view-data-summary/index.js`                                           | Available | Owns `g_filter` state object, `field_selection: new Set(['all'])` default. |
| `docs/ai/data_summary_report.md`                                        | Available | Confirms frontend filtering is client-side, over data already loaded into memory. |
| Git history (`git log -L`)                                             | Available | Confirms the exclusion guard's shape has been stable since commit `08b667d49` ("Proper Filtering of report"); a later commit `2691d48d2` only removed dead commented-out code, no logic change. |
| Prior story `9-1-fix-data-summary-checks-field-filter.md`               | Available | Documents a **different**, already-fixed bug (ALL-toggle bypassing form-scoping in the checkbox dropdown, `render_field_filter`). Does not touch the results-table exclusion logic that is the subject of this case. |
| Reproduction steps from client                                         | Missing   | No screenshot, HAR, or exact field names provided. Root cause was reachable from static analysis alone, so this did not block the investigation. |

## Investigation Backlog

| # | Path to Explore | Priority | Status | Notes |
| - | --------------- | -------- | ------ | ----- |
| 1 | Confirm with client whether they selected exactly 2+ specific fields (not exactly 1) when the symptom occurred | Medium | Open | Would corroborate the Confirmed root cause with an exact repro, but is not required to proceed to a fix. |
| 2 | Check whether `g_case_view_request.field_selection` (server-side query string param, `index.js` line ~85) is consumed by any backend endpoint, or is dead/unused | Low | Open | Tangential; not implicated in the client-side rendering bug, but worth a quick grep before touching related code. |

## Timeline of Events

| Time       | Event                                                              | Source                                              | Confidence |
| ---------- | ------------------------------------------------------------------- | ---------------------------------------------------- | ---------- |
| (historical) | Commit `08b667d49` "View Data Summary: Proper Filtering of report" introduces the single-field exclusion guard in `render_search_result_item`, replacing an older commented-out DOM-based single-field filter | `git log -L 655,685:.../renderer.js` | Confirmed |
| (historical) | Commit `2691d48d2` "View Data Summary: Add download Link" deletes leftover commented-out code near the guard; the guard's logic itself is untouched | `git log -L 655,685:.../renderer.js` | Confirmed |
| Prior sprint | Story 9.1 fixes an unrelated bug: the ALL-toggle checkbox bypassing form-scoping inside `render_field_filter` (the field *picker* UI, not the results *filter*) | `9-1-fix-data-summary-checks-field-filter.md` | Confirmed |
| 2026-07-22 | Client reports selecting specific fields (plural) after "all" still shows all fields in results | User report | Confirmed (as a report; underlying mechanism independently verified below) |

## Confirmed Findings

### Finding 1: The results-table exclusion guard only fires when exactly one field is selected

**Evidence:** [renderer.js:662-679](../../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/renderer.js#L662-L679)

```js
let is_single_field_filter = false;
if
(
    !g_filter.field_selection.has("all")
)
{
    if(g_filter.field_selection.size == 1)
    {
        if(g_filter.field_selection.has(field_name))
        {
            is_single_field_filter = true;
        }
        else
        {
            return;   // <-- the ONLY place a field is excluded from the results table
        }
    }
    // size > 1 (and "all" not selected): falls through here, no exclusion of any kind
}
```

**Detail:** This block executes once per candidate field row inside `render_search_result_item` (the function that actually builds the results table). The single `return;` statement is the *entire* exclusion mechanism for the Field filter. It is reached only when `field_selection.size == 1` (exactly one specific field checked). When two or more specific fields are checked (`"all"` not present, `size > 1`), the `if(g_filter.field_selection.size == 1)` branch is never entered, so the exclusion path is unreachable — every field renders, identical to the "All Fields" behavior.

### Finding 2: The checkbox UI (`render_field_filter`) correctly tracks multi-select state; only the results renderer ignores it

**Evidence:** [renderer.js:125-181](../../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/renderer.js#L125-L181) (`on_field_filter_changed`), [renderer.js:265-295](../../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/renderer.js#L265-L295) (`render_field_filter`)

**Detail:** `g_filter.field_selection` is a `Set` that correctly accumulates multiple field names as the user checks additional boxes (`g_filter.field_selection.add(value)` at line 168), and the checkbox UI correctly reflects `checked` state per selected field name (line 292: `g_filter.field_selection.has(v2.field_name)`). The state model is not the problem — the consumer of that state in the results table (Finding 1) is.

## Deduced Conclusions

### Deduction 1: Symptom reproduces whenever 2+ specific fields are selected, and only then

**Based on:** Finding 1

**Reasoning:** The guard's only branch that can `return` (exclude a field) requires `size == 1`. Selecting "all" → `has("all")` true → outer `if` false → no exclusion (expected: show everything). Selecting exactly 1 specific field → `size == 1` → correct inclusion/exclusion per field (this works, matching the client's expectation for a single field). Selecting 2+ specific fields → outer `if` true, inner `if` false → no code path returns → every field renders regardless of selection.

**Conclusion:** The client's report ("selecting specific fields isn't correctly filtering and still showing all fields") is the direct, deterministic consequence of this guard's `size == 1` condition. This is not a data, config, or CouchDB-view issue — the bug is entirely in this one client-side conditional.

## Hypothesized Paths

### Hypothesis 1: This is the same defect as Story 9.1

**Status:** Refuted

**Theory:** The recently-fixed 9.1 bug (ALL toggle bypassing form-scoping) might be the same root cause resurfacing.

**Supporting indicators:** Same page, same `g_filter.field_selection` state object, same file.

**Would confirm:** The 9.1 fix touching `render_search_result_item`'s exclusion guard.

**Would refute:** 9.1's diff only touches `render_field_filter` (the checkbox-list renderer, controlling which checkboxes are *shown*), never `render_search_result_item` (the results-table renderer, controlling which rows are *included*). Confirmed by reading the story file's Dev Notes and diff description — different function, different bug.

**Resolution:** Refuted by direct comparison of the two functions; 9.1's fix and this defect are independent and do not overlap.

## Missing Evidence

| Gap                                            | Impact                                                                 | How to Obtain                                             |
| ------------------------------------------------ | ------------------------------------------------------------------------- | -------------------------------------------------------------- |
| Client's exact repro (number/names of fields checked) | Would provide an exact confirmatory repro string, though not required for the fix | Ask client for the specific fields they selected, or a screenshot |

## Source Code Trace

| Element       | Detail                                                                                                     |
| ------------- | -------------------------------------------------------------------------------------------------------------- |
| Error origin  | [renderer.js:662-679](../../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/renderer.js#L662) inside `render_search_result_item` (default/field case) |
| Trigger       | User checks 2+ specific field checkboxes (not "All Fields") in the Field filter, then clicks "Apply Filters" |
| Condition     | `g_filter.field_selection.has("all") === false` AND `g_filter.field_selection.size > 1`                        |
| Related files | [index.js](../../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/index.js) (`g_filter` state, `build_report`), [Index.cshtml](../../../source-code/mmria/mmria-server/Views/view_data_summary/Index.cshtml) (Apply Filters button) |

## Conclusion

**Confidence:** High

Confirmed root cause: in `render_search_result_item` (renderer.js), the Field-filter exclusion guard only excludes non-matching fields when exactly one specific field is selected (`field_selection.size == 1`). When "All Fields" is not selected and 2 or more specific fields are checked, no exclusion path exists, so every field renders — reproducing the client's reported symptom deterministically. This is unrelated to the previously-fixed Story 9.1 defect (which affected only the field-picker checkbox list, not the results table).

## Recommended Next Steps

### Fix direction

Extend the guard in `render_search_result_item` to exclude non-matching fields for the general multi-select case, not just the `size == 1` case — e.g. replace the `size == 1` special case with a general "if not `all` selected and `field_selection` does not contain this `field_name`, skip" check, while preserving `is_single_field_filter`'s existing role (forcing a new header per row when exactly one field is selected) as a separate, additional condition rather than conflating it with exclusion.

### Diagnostic

None required — root cause is deterministic and reachable via static trace.

## Reproduction Plan

1. Navigate to `/view-data-summary`.
2. Load data (any jurisdiction/date range with results).
3. In the Field filter, uncheck "All Fields", then check 2 or more specific field checkboxes.
4. Click "Apply Filters".
5. **Expected:** results table shows only rows for the checked fields.
6. **Observed (bug):** results table shows rows for every field, identical to "All Fields" selected.

## Side Findings

- `g_case_view_request.field_selection` in `index.js` (used to build a `/api/...` query string) appears to carry `field_selection` to the server, but the actual client-side filtering happens entirely against in-memory `g_metadata` in `render_search_result_item` — worth confirming in a follow-up whether that server-side query param is actually consumed by any backend endpoint or is vestigial (Backlog #2).
