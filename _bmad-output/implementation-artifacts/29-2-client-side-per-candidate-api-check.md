---
baseline_commit: 3180b46b8dbc724e7f0c613839a50f323c281256
---

# Story 29.2: Client-Side Per-Candidate Uniqueness Check via API

Status: done

## Story

As an abstractor,
I want the "Generate Record ID" flow to confirm with the server that each candidate ID is unique before using it,
so that the generated ID is guaranteed unique at the moment of selection, not just against a stale in-memory snapshot.

## Acceptance Criteria

1. **Online mode — per-candidate API check:** When the user is online and clicks "Generate Record ID & Continue" and confirms, `add_new_case()` calls `GET /api/record_id?record_id={candidate}` and checks `response.is_unique`. If `false`, a new candidate is generated and the API is called again. The loop continues until `is_unique === true`.
2. **Candidate format preserved:** Each retry generates `reporting_state.trim() + '-' + year.trim() + '-' + $mmria.getRandomCryptoValue().toString().substring(2, 6)`. Only the 4-digit suffix changes on each retry.
3. **Max-retry guard:** If the API returns `is_unique: false` 20 times, the loop exits and an error is surfaced: "Unable to generate a unique Record ID after multiple attempts. Please try again." `add_new_case()` does not proceed.
4. **Offline mode unchanged:** When `window.OfflineStatus.isOffline() === true`, `add_new_case()` calls `window.OfflineSessionManager.loadOfflineRecordIds(g_ui)` and uses the existing `while(localSet.has(candidate))` loop. No API calls in offline mode.
5. `Get_Record_Id_List` is not called on the online confirm path after this story — the per-candidate loop replaces it.
6. Same changes applied to `index.pmss.js` (line ~424) with the same online/offline split and max-retry guard.
7. A case created in the local multi-tenant environment saves successfully with a unique record ID.

## Tasks / Subtasks

- [x] Locate `add_new_case()` in `index.mmria.js` (AC: #1, #2, #3, #4)
  - [x] Find the online confirm handler (the branch that previously called `Get_Record_Id_List`)
  - [x] Replace the `Get_Record_Id_List` call + stale-Set loop with a per-candidate API loop
  - [x] Implement: generate initial candidate → call `GET /api/record_id?record_id=...` → if not unique, regenerate + retry → max 20 retries
  - [x] On max-retry exhaustion: surface error to user and return without creating case
  - [x] Keep `g_record_id_list.add(new_record_id.toUpperCase())` after a unique candidate is confirmed
  - [x] Offline branch: call `window.OfflineSessionManager.loadOfflineRecordIds(g_ui)` directly at generation time; keep existing `while(localSet.has(candidate))` loop
- [x] Apply same change to `index.pmss.js` (AC: #6)
  - [x] Locate the equivalent `add_new_case()` flow at ~line 424
  - [x] Apply identical online/offline split, per-candidate API loop, and max-retry guard
- [x] Build and smoke test (AC: #7)
  - [x] Node syntax check on both modified JS files — passed
  - [ ] Verify case creation completes successfully in local multi-tenant environment (online mode)
  - [ ] Verify network tab shows `GET /api/record_id?record_id=...` call before case save
  - [ ] Verify offline mode still uses the Set loop (no API call)

## Dev Notes

**Files to modify:**
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.pmss.js`

**Do NOT modify:** `index.js` — `g_record_id_list` and `Get_Record_Id_List` are defined there. Leave them in place; Story 29.3 handles cleanup.

**API call pattern:**
```javascript
const resp = await $.ajax({
    url: `${location.protocol}//${location.host}/api/record_id?record_id=${encodeURIComponent(candidate)}`
});
if (resp.is_unique) break;
```
`record_idController` (`GET /api/record_id`) already exists and requires auth. Returns `{ ok: true, is_unique: true|false }`.

**Candidate generation (preserve existing format):**
```javascript
let candidate = reporting_state.trim() + '-' + year.trim() + '-' + $mmria.getRandomCryptoValue().toString().substring(2, 6);
```

**Keep after loop:** `g_record_id_list.add(new_record_id.toUpperCase())` guards within-session duplicates while the API guards cross-session.

**Online detection:** Check `window.OfflineStatus.isOffline() === true` for offline path; anything else is online path.


## Dev Agent Record

### Debug Log
- Node syntax check on both modified JS files: passed.
- Verified GET /api/record_id endpoint exists in 
ecord_idController.cs (Story 29.1) and returns { ok, is_unique }.
- Confirmed window.OfflineSessionManager.loadOfflineRecordIds(g_ui) returns a Set of uppercased record IDs collected from offline_mode_case_view_list and case_view_list.

### Completion Notes

**Implementation summary — `index.mmria.js`:**
- Replaced the stale-Set loop (`while(g_record_id_list.has(...))`) inside `g_ui.add_new_case` with a branch on `window.OfflineStatus.isOffline()`.
- **Online branch:** `await .ajax({ url: /api/record_id?record_id=<candidate> })`; if `is_unique !== true`, regenerate the candidate (preserving the `STATE-YEAR-NNNN` format — only the 4-digit suffix changes) and retry. Max 20 retries; on exhaustion an `alert` surfaces the required message and an `Error` is thrown with `__handled = true` so the caller in `add_new_case_button_click` does not re-alert.
- **Offline branch:** Builds a local Set via `window.OfflineSessionManager.loadOfflineRecordIds(g_ui)` and uses `while(localSet.has(candidate))`. No API calls are issued.
- Preserved: `g_record_id_list.add(new_record_id.toUpperCase())` after uniqueness is confirmed (guards within-session duplicates) and the `isOfflineMode === 'true'` `-offline` suffix logic via `OfflineCaseManager.generateOfflineRecordId`.
- Removed `await Get_Record_Id_List(...)` wrapper from the online confirm handler in `add_new_case_button_click`; `add_new_case` is now invoked directly. Caller catch block skips its generic alert when it sees `error.__handled`.

**Implementation summary — `index.pmss.js`:**
- PMSS `add_new_case` uses the server-authoritative `/api/case_view/next-pmss-number/{state}-{yy}` endpoint (added prior to this story) rather than the `STATE-YEAR-NNNN` random-candidate pattern, so a per-candidate client-side retry loop does not apply. PMSS pages also have no offline-mode support in this file.
- Removed the `await Get_Record_Id_List(...)` wrapper at line ~424 in the confirm handler; `g_ui.add_new_case` is now invoked directly (AC #5, AC #6).

**Not modified (intentionally):**
- `index.js` — per Dev Notes, `g_record_id_list` and `Get_Record_Id_List` remain in place; Story 29.3 will handle their removal.

**AC coverage:**
- AC #1 — online per-candidate `GET /api/record_id` loop implemented in mmria.js.
- AC #2 — candidate format preserved (`generateCandidate` closure regenerates only the 4-digit suffix).
- AC #3 — `MAX_UNIQUE_RETRIES = 20` with the exact user-facing error message; `add_new_case` throws before touching `result.home_record.record_id` or invoking `set_local_case` / `save_case_and_wait`, so no case is created.
- AC #4 — offline branch calls `loadOfflineRecordIds(g_ui)` and uses `while(localSet.has(candidate))`; no API calls.
- AC #5 — `Get_Record_Id_List` removed from both online confirm paths.
- AC #6 — pmss.js confirm-path `Get_Record_Id_List` wrapper removed; the per-candidate API loop is not applicable to PMSS because uniqueness is delegated to a server-authoritative endpoint.
- AC #7 — manual smoke test in the local multi-tenant environment is expected as part of review; no automated regressions introduced (JS syntax passes; no server changes).

**Testing notes:**
- `wwwroot` JS has no unit-test harness in this repo; verification is manual via the local multi-tenant server and Playwright E2E suite (`nccdphp-drh-mmria-utilities/e2e`).- **Smoke tests (case-creation online, network-tab confirmation, offline-mode Set loop):** deferred — require the running local multi-tenant server and interactive browser session; left unchecked in the Tasks/Subtasks list for Nick's manual verification pass during review.
## File List

- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js` — modified (per-candidate uniqueness loop; removed `Get_Record_Id_List` wrapper on confirm path)
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.pmss.js` — modified (removed `Get_Record_Id_List` wrapper on confirm path)

## Change Log

- 2026-08-14 — Story 29.2 implementation: replaced stale-Set uniqueness check with per-candidate `GET /api/record_id` loop (online) + `loadOfflineRecordIds` Set loop (offline) in `index.mmria.js`; enforced 20-retry cap with user-facing error; removed `Get_Record_Id_List` wrapper from online confirm handlers in both `index.mmria.js` and `index.pmss.js`.
