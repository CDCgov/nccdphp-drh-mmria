# Story 46.1: Migrate Case Routing from Numeric Index to Case `_id`

Status: backlog

> **Origin:** Architectural review with Winston (2026-08-21). The current case-editor hash route uses `path_array[0]` as a **numeric index into `g_ui.case_view_list`** which resolves to a case at request time. Because the list mutates (sort/filter/offline sync/add-case), the same URL can silently point to a different case, and refresh must load the list before it can resolve the index. This story replaces the numeric segment with the CouchDB case `_id` (GUID) while preserving the rest of the URL shape verbatim.

## Story

As a case abstractor / reviewer,
I want the URL for a case page to identify the case by its stable `_id` (GUID) instead of its position in the current case list,
so that refresh, back/forward navigation, and shared links always resolve to the same case regardless of list mutations.

## Acceptance Criteria

1. Route shape is preserved exactly. Only the numeric segment changes.
   - Before: `/Case#/{numericIndex}/{form}/{child}` — e.g. `/Case#/0/home_record`
   - After:  `/Case#/{caseId}/{form}/{child}` — e.g. `/Case#/550e8400-e29b-41d4-a716-446655440000/home_record`
   - Non-case routes (`#/summary`, `#/field_search/…`, `#/notifications`, etc.) are **unchanged**.
2. `url_monitor.get_url_state` returns a stable `selected_case_id` field when `path_array[0]` is a case id, and leaves `selected_form_name` populated (as today) when it is a known form keyword. Callers do not need to guess.
3. The discriminator is deterministic: `path_array[0]` is treated as a case id **only if** it does not match the known form-keyword list. Case ids are CouchDB `_id` values (GUIDs). Form keywords are: `summary`, `field_search`, `notifications`, `pinned`, and any others enumerated during implementation from a full grep of `path_array[0] ==` comparisons.
4. All in-scope JS files (see Dev Notes) that write `window.location.hash = '#/{index}/…'` or read `path_array[0]` as an index are updated to use the case `_id`.
5. `case_view_list[index].id` lookups on hashchange are removed. The hashchange handler resolves the target case by `_id` directly. No dependency on list ordering.
6. **Legacy URLs redirect to `#/summary`.** When `path_array[0]` is purely numeric (`/^\d+$/`), the hashchange handler logs a one-time console info and redirects to `#/summary`. No silent index → id translation.
7. **Unauthorized / unknown case id redirects to `#/summary`** with a `// TODO(46.x): show landing page / modal for unauthorized case access` stub comment at the redirect site. No landing page or modal is implemented in this story.
8. Next/prev navigation (if present in the current UI) computes position by `case_view_list.findIndex(c => c.id === currentCaseId)` at click time — position is derived, never persisted in the URL.
9. Offline mode: `OfflineNavigationManager.getTargetCaseIdForHashChange` (and any sibling offline reconciliation code that maps index → id) is simplified to accept a case id directly. `g_offline_case_index_map` continues to exist as an offline lookup for id → list-position (for UI affordances), but is no longer read to resolve URL → case.
10. Backward-compat for browser history: on legacy-URL redirect, use `history.replaceState` (not `location.hash =`) to avoid polluting the back stack with the old numeric URL.
11. `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` — zero errors (no C# changes expected, but build to confirm no Razor `@Url.Action` or hash-emit strings in `.cshtml` were missed).
12. Playwright smoke: existing case-editor smoke tests in [4.1-smoke-tests.md](../../../nccdphp-drh-mmria-utilities/e2e/4.1-smoke-tests.md) pass unchanged after their test fixtures are updated to expect `_id`-shaped URLs.

## Tasks / Subtasks

- [ ] **T1 — Enumerate the form-keyword list**
  - [ ] Grep the entire in-scope tree for `path_array[0] ==`, `path_array[0] ===`, and `url_state.selected_form_name ==` comparisons. Every string literal on the right-hand side is a form keyword.
  - [ ] Consolidate the list into a single constant (e.g. `URL_FORM_KEYWORDS`) exported from `url_monitor.js`.

- [ ] **T2 — Update `url_monitor.get_url_state`**
  - [ ] Add `selected_case_id` to the returned object.
  - [ ] Populate `selected_case_id = path_array[0]` when `path_array[0]` is not in `URL_FORM_KEYWORDS` and not purely numeric.
  - [ ] Populate `selected_form_name = path_array[0]` when it **is** in `URL_FORM_KEYWORDS` (unchanged behavior).
  - [ ] When `path_array[0]` is purely numeric (`/^\d+$/`), leave both `selected_case_id` and `selected_form_name` null and set a `legacy_numeric_index` flag so the caller can trigger the redirect in AC-6.
  - [ ] Preserve `path_array`, `selected_id`, `selected_child_id` fields exactly as today for any consumer that reads them.

- [ ] **T3 — Update the case editor hashchange path in [case/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js)**
  - [ ] Remove `parseInt(g_ui.url_state.path_array[0]) >= 0` index probes at [L3075-L3095](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L3075) and [L3251-L3262](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L3251).
  - [ ] Replace `caseIndex = parseInt(path_array[0])` + `case_view_list[caseIndex].id` with `targetCaseId = url_state.selected_case_id`.
  - [ ] If `url_state.legacy_numeric_index` is set, call the shared redirect helper (T7) and return.
  - [ ] Update every `window.location.hash = '#/{...}'` writer in this file to emit `#/${caseId}/...`. Enumerated occurrences (from grep — verify during dev):
    - [L3107](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L3107), [L3290](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L3290), [L3318](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L3318), [L3334](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L3334), [L3337](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L3337), [L3385](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L3385), [L3417](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L3417), [L3483](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L3483) — most already emit `#/summary` which is unchanged; verify none emit `#/${index}/`.
  - [ ] [L4133](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L4133) `path_array[0] == 'summary'` — unchanged (form keyword).

- [ ] **T4 — Update sibling case entry points**
  - [ ] [case/index.mmria.js](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js) — [L262](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js#L262) writes `'#/' + g_ui.selected_record_index + '/home_record'`. Replace with `'#/' + g_ui.case_view_list[g_ui.selected_record_index].id + '/home_record'` (still using in-memory list to get the id — never emitting the index).
  - [ ] [case/index.pmss.js](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.pmss.js) — same treatment for the equivalent write site.
  - [ ] [case/search_view.js](../../source-code/mmria/mmria-server/wwwroot/scripts/case/search_view.js) — [L28](../../source-code/mmria/mmria-server/wwwroot/scripts/case/search_view.js#L28) reads `path_array[0]` as `record_index`. Rename to `case_id` and remove any index-based list dereference on this path.
  - [ ] [case/case-validation.js](../../source-code/mmria/mmria-server/wwwroot/scripts/case/case-validation.js) — [L1798](../../source-code/mmria/mmria-server/wwwroot/scripts/case/case-validation.js#L1798) constructs `'#/' + path_array[0] + '/' + formPath`. This is already index-agnostic (it just reuses whatever is at `path_array[0]`) — will inherit case ids automatically once upstream writers are fixed. **No change required** but verify.

- [ ] **T5 — Update de-identified and committee-member**
  - [ ] [de-identified/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/de-identified/index.js) — [L124](../../source-code/mmria/mmria-server/wwwroot/scripts/de-identified/index.js#L124) sets `g_ui.selected_record_index` and any hash writer that uses it.
  - [ ] [committee-member/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/committee-member/index.js) — [L71](../../source-code/mmria/mmria-server/wwwroot/scripts/committee-member/index.js#L71) and its navigation_renderer.
  - [ ] [committee-member/navigation_renderer.js](../../source-code/mmria/mmria-server/wwwroot/scripts/committee-member/navigation_renderer.js) — [L43](../../source-code/mmria/mmria-server/wwwroot/scripts/committee-member/navigation_renderer.js#L43) already resolves to `.id` for display; verify no downstream re-indexes.
  - [ ] Case-adjacent editor navigation renderers under `editor/`:
    - [editor/navigation_renderer.js](../../source-code/mmria/mmria-server/wwwroot/scripts/editor/navigation_renderer.js) L40
    - [editor/navigation_renderer.abstractor.committee.js](../../source-code/mmria/mmria-server/wwwroot/scripts/editor/navigation_renderer.abstractor.committee.js) L40
    - [editor/navigation_renderer.committee_member.js](../../source-code/mmria/mmria-server/wwwroot/scripts/editor/navigation_renderer.committee_member.js) L40
    - [editor/preview.js](../../source-code/mmria/mmria-server/wwwroot/scripts/editor/preview.js) L70

- [ ] **T6 — Update offline navigation**
  - [ ] [offline/offline-navigation-manager.js](../../source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-navigation-manager.js) — `getTargetCaseIdForHashChange` signature changes to accept a case id (or null) rather than an index. Callers already know the id from `url_state.selected_case_id`.
  - [ ] [offline/offline-case-manager.js](../../source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-case-manager.js) — [L483](../../source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-case-manager.js#L483): `g_offline_case_index_map` continues to exist as an id-list for UI affordances; **do not** delete it, just stop using it for URL resolution.
  - [ ] [offline/offline-modals.js](../../source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-modals.js) — [L960](../../source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-modals.js#L960): same as above.
  - [ ] Offline case-storage keying (`case_index` in localStorage in [offline-case-storage.js](../../source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-case-storage.js)) is **out of scope** — that's a storage-schema concept, not a URL routing concept. Do not rename.

- [ ] **T7 — Add a shared redirect helper**
  - [ ] Location: alongside `url_monitor` (e.g. `url_monitor.redirect_to_case_list(reason)`) or inline in the hashchange handlers.
  - [ ] Behavior: `history.replaceState(null, '', '#/summary')` + a one-line `console.info` tagged with the redirect reason (`legacy-numeric-url`, `unauthorized-case`, `case-not-found`).
  - [ ] Stub the unauthorized branch:
    ```js
    // TODO(46.x): show landing page / modal for unauthorized case access
    // instead of silent redirect to #/summary.
    ```

- [ ] **T8 — Verify print-version has no residual index coupling**
  - [ ] [print-version/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/print-version/index.js) and [print-version/print_version_renderer.js](../../source-code/mmria/mmria-server/wwwroot/scripts/print-version/print_version_renderer.js) currently receive the case via server-side rendering and reference `path_array` only in commented-out code. **Confirm** during dev that no active print-version code path constructs a `#/{index}/…` URL. No code changes expected. If active coupling is found, apply the same treatment as T3–T5.

- [ ] **T9 — Test coverage**
  - [ ] Add a small Playwright regression in [e2e/tests](../../../nccdphp-drh-mmria-utilities/e2e/tests/) covering:
    - Fresh case-list navigation → open case → refresh → same case loads.
    - Sort/filter the case list → previously-open case's URL still resolves to the same case.
    - Legacy `#/0/home_record` URL → lands on `#/summary` (no case load, no console error).
    - Bogus case id `#/does-not-exist-guid/home_record` → lands on `#/summary`.
  - [ ] Update existing case-editor smoke fixtures that hard-code `#/0/…` shape to `#/${caseId}/…`.

- [ ] **T10 — Documentation & context**
  - [ ] Update [docs/ai/case_view_edit_playwright_testing_context.md](../../docs/ai/case_view_edit_playwright_testing_context.md) if it documents the URL shape.
  - [ ] Add a one-paragraph "URL routing" note to [docs/ai/AI_CONTEXT.md](../../docs/ai/AI_CONTEXT.md) so future agents don't reintroduce index-based patterns.

## Dev Notes

### Scope boundary

**In scope (case-adjacent JS entry points):**
- [wwwroot/scripts/case/](../../source-code/mmria/mmria-server/wwwroot/scripts/case/) — `index.js`, `index.mmria.js`, `index.pmss.js`, `search_view.js`, `case-validation.js`
- [wwwroot/scripts/de-identified/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/de-identified/index.js)
- [wwwroot/scripts/committee-member/](../../source-code/mmria/mmria-server/wwwroot/scripts/committee-member/)
- [wwwroot/scripts/editor/](../../source-code/mmria/mmria-server/wwwroot/scripts/editor/) — navigation renderers and `preview.js` only (these render case navigation, not the metadata form designer)
- [wwwroot/scripts/offline/](../../source-code/mmria/mmria-server/wwwroot/scripts/offline/) — navigation manager, case-manager URL reconciliation, modals URL reconciliation
- [wwwroot/scripts/url_monitor.js](../../source-code/mmria/mmria-server/wwwroot/scripts/url_monitor.js) — the discriminator
- [wwwroot/scripts/print-version/](../../source-code/mmria/mmria-server/wwwroot/scripts/print-version/) — verify only, no changes expected

**Out of scope (per architectural review):**
- `aggregate-report/`, `overdose-data-summary/` — reports
- `data-dictionary/` — `_record_index` mentions are field names, not routing
- `export-list-manager/`
- Server-side C# `core_element_export`, `exporter` — `record_index` mentions are C# export tooling, not routing
- `editor/page_renderer/app.mmria.js` `window.location.hash = '#/summary'` at [L395](../../source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js#L395) — this is inside the metadata form designer tool, out of scope for this story (safe as-is because it only writes `#/summary`)

### Identifier choice

Use the CouchDB case document `_id` (GUID) — **not** the human-readable record id (`STATE-YEAR-NNNN`). Rationale:
- GUIDs are unambiguous vs form keywords with zero discriminator overhead.
- Record ids can be **null or malformed** on cases mid-creation (see [project-context.md §3.3](../project-context.md)); `_id` is always present and immutable.
- The offline store already keys by `_id` — no schema change needed.

### The critical hashchange handler

The two hashchange branches that today probe `parseInt(path_array[0]) >= 0` are the hot spot:
- [case/index.js L3060-L3140](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L3060) — the `isTrusted` branch
- [case/index.js L3251-L3262](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L3251) — the fallback branch

Both must be rewritten to consume `url_state.selected_case_id` directly. The current safety check `caseIndex >= case_view_list.length || caseIndex < 0 → return` becomes `if (!targetCaseId) { redirect_to_case_list('missing-case-id'); return; }`.

### Preserve, do not rename, the offline case-storage key

`CASE_INDEX_KEY = 'case_index'` in [offline-case-storage.js](../../source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-case-storage.js) is a **storage-schema constant** used inside IndexedDB/localStorage. The word "index" there refers to a lookup index, not to a URL positional index. **Do not rename** — it will break offline reads across the upgrade.

### Non-goal

Migrating from hash-based routing (`#/…`) to History API path routing (`/case/{id}/{form}`) is a **separate future epic** (Winston's Option D). This story preserves the hash prefix exactly.

### Failure modes to test

| Symptom | Root cause | Fix |
|---|---|---|
| Legacy bookmark opens a random case | Silent index → id translation using a stale list | We chose redirect-to-summary; no silent translation |
| Shared link 403s with cryptic error | Unauthorized case id | Redirect to `#/summary` + TODO stub |
| Browser back button revisits legacy URL | `location.hash = '#/summary'` pushes a new entry | Use `history.replaceState` on legacy redirect |
| Case list re-sorts and URL now shows old id | List sort is UX-only; URL identifies resource | Expected — this is the whole point of the change |
| Offline case opened, list re-syncs, wrong case loads | Reconciliation code re-derives from stale index | Handler now consumes id directly; list order irrelevant |

### Related

- Depends on: none.
- Enables: future History API migration (Winston's Option D) as a follow-on epic.
- Sibling reference: [docs/ai/case_view_edit_playwright_testing_context.md](../../docs/ai/case_view_edit_playwright_testing_context.md)
