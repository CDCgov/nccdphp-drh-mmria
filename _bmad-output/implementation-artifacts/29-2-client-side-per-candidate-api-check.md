# Story 29.2: Client-Side Per-Candidate Uniqueness Check via API

Status: ready-for-dev

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

- [ ] Locate `add_new_case()` in `index.mmria.js` (AC: #1, #2, #3, #4)
  - [ ] Find the online confirm handler (the branch that previously called `Get_Record_Id_List`)
  - [ ] Replace the `Get_Record_Id_List` call + stale-Set loop with a per-candidate API loop
  - [ ] Implement: generate initial candidate → call `GET /api/record_id?record_id=...` → if not unique, regenerate + retry → max 20 retries
  - [ ] On max-retry exhaustion: surface error to user and return without creating case
  - [ ] Keep `g_record_id_list.add(new_record_id.toUpperCase())` after a unique candidate is confirmed
  - [ ] Offline branch: call `window.OfflineSessionManager.loadOfflineRecordIds(g_ui)` directly at generation time; keep existing `while(localSet.has(candidate))` loop
- [ ] Apply same change to `index.pmss.js` (AC: #6)
  - [ ] Locate the equivalent `add_new_case()` flow at ~line 424
  - [ ] Apply identical online/offline split, per-candidate API loop, and max-retry guard
- [ ] Build and smoke test (AC: #7)
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
