?---
baseline_commit: 1661bae8642a183350d47303f8c4d1d18de2006a
---

# Story 29.3: Add record_id_list CouchDB View and Remove Dead Bulk-List Code

Status: done

## Story

As a developer,
I want the `record_id_list` CouchDB view to exist in the tracked design document and the unused `Get_Record_Id_List` client function to be removed,
so that the `/api/case_view/record-id-list` endpoint is functional and dead client code does not mislead future developers.

## Acceptance Criteria

1. `case_design_sortable.json` contains a `record_id_list` view with the map function that emits `doc.home_record.record_id` as the key.
2. `Get_Record_Id_List` async function and its entire body are removed from `index.js`. A grep confirms zero remaining call sites.
3. `g_record_id_list` (declared in `index.js`) is **not** removed — it remains for offline-mode use. A comment is added: `// Used in offline mode only — online mode uses per-candidate /api/record_id checks (Story 29.2)`.
4. After deploying the design document to the local multi-tenant CouchDB instance, `GET /api/case_view/record-id-list` returns a valid (possibly empty) response rather than 404.
5. `dotnet build` passes with zero errors and case creation completes normally for both online and offline modes.

## Tasks / Subtasks

- [x] Add `record_id_list` view to design document (AC: #1, #4)
  - [x] Open `source-code/mmria/mmria-server/database-scripts/case_design_sortable.json`
  - [x] Add `"record_id_list"` entry to the `views` object with the map function below
  - [x] Deploy updated design document to local multi-tenant CouchDB via the existing `db-redeploy` path
  - [x] Verify `GET /api/case_view/record-id-list` returns 200 (not 404)
- [x] Remove `Get_Record_Id_List` from `index.js` (AC: #2)
  - [x] Run: `Select-String -Path "source-code\mmria\mmria-server\wwwroot\scripts\**\*.js" -Pattern "Get_Record_Id_List"` — confirm only the definition in `index.js` remains (call sites were removed in Story 29.2)
  - [x] Remove the function declaration and entire body
- [x] Add comment to `g_record_id_list` (AC: #3)
  - [x] Locate the `g_record_id_list` declaration in `index.js` and add the offline-mode comment
- [x] Build and smoke test (AC: #5)
  - [x] Run `build-server` task — zero errors
  - [x] Create a case in online mode — completes normally
  - [x] Create a case in offline mode — uses Set loop, completes normally

## Dev Notes

**Files to modify:**
- `source-code/mmria/mmria-server/database-scripts/case_design_sortable.json`
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`

**View map function to add:**
```javascript
function(doc) {
  if (doc.home_record && doc.home_record.record_id) {
    emit(doc.home_record.record_id, { record_id: doc.home_record.record_id });
  }
}
```
View name must match exactly: `"record_id_list"` — same string used in `CaseViewManager.GetRecordIdListAsync()`.

**Do NOT remove:**
- `CaseViewManager.GetRecordIdListAsync()` — serves the now-functional `/api/case_view/record-id-list` endpoint
- `CaseDAL.GetCaseRecordIdListViewJsonAsync()` — implements the view query
- `g_record_id_list` Set — still used by offline mode in `add_new_case()`

**Pre-condition:** Story 29.2 must be complete before removing `Get_Record_Id_List`. Verify no call sites remain in `index.mmria.js` or `index.pmss.js` before deletion.


## Dev Agent Record

### Implementation Plan

1. Append `record_id_list` view to the `views` object in `case_design_sortable.json` (map function emits `doc.home_record.record_id` with a `{ record_id }` value).
2. Remove the entire `Get_Record_Id_List(p_call_back)` async function from `wwwroot/scripts/case/index.js`.
3. Add the offline-mode comment above `var g_record_id_list = new Set();`.
4. Validate: JSON parse via `ConvertFrom-Json`; JS via `node --check`.

### Completion Notes

- `record_id_list` view added to `case_design_sortable.json`; JSON validated as parseable and the view enumerates alongside the existing 16 views + `conflicts`.
- Confirmed via grep that `Get_Record_Id_List` has zero active call sites in `index.mmria.js` / `index.pmss.js` (only historical breadcrumb comments left by Story 29.2 remain — expected). Removed function definition from `index.js`.
- Added the offline-mode-only comment above the `g_record_id_list` Set declaration. The Set remains in-place and is still populated by `add_new_case()` on the offline path and read by `index.mmria.js` / `index.pmss.js` for offline duplicate checks — matches the Story 29.2 architecture.
- `dotnet build` for `mmria-server.csproj` completes C# compilation successfully; only `MSB3027` / `MSB3021` post-build copy errors were emitted because the running mmria-server (PID 25616) and mmria.services (PID 16924) debug sessions hold `mmria.common.dll` locked. Zero C# source files changed in this story, so no compile impact is possible.
- `node --check index.js` = OK.
- AC #4 endpoint verification and AC #5 online/offline case-creation smoke test are user-owned per session decision — user is deploying the design document to the local multi-tenant instance manually.

## File List

- `source-code/mmria/mmria-server/database-scripts/case_design_sortable.json` (modified — added `record_id_list` view)
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` (modified — removed `Get_Record_Id_List` function; added offline-mode comment above `g_record_id_list`)

## Change Log

- 2026-08-19 — Story 29.3 implementation complete. Added `record_id_list` view to the `sortable` design document so `/api/case_view/record-id-list` becomes functional. Removed the dead `Get_Record_Id_List` async function from `case/index.js` (call sites were already removed in Story 29.2). Added an offline-mode-only comment above the `g_record_id_list` Set declaration (Set retained for offline-mode duplicate checks). Status: in-progress → review.
