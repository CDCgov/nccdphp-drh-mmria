# Story 29.3: Add record_id_list CouchDB View and Remove Dead Bulk-List Code

Status: ready-for-dev

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

- [ ] Add `record_id_list` view to design document (AC: #1, #4)
  - [ ] Open `source-code/mmria/mmria-server/database-scripts/case_design_sortable.json`
  - [ ] Add `"record_id_list"` entry to the `views` object with the map function below
  - [ ] Deploy updated design document to local multi-tenant CouchDB via the existing `db-redeploy` path
  - [ ] Verify `GET /api/case_view/record-id-list` returns 200 (not 404)
- [ ] Remove `Get_Record_Id_List` from `index.js` (AC: #2)
  - [ ] Run: `Select-String -Path "source-code\mmria\mmria-server\wwwroot\scripts\**\*.js" -Pattern "Get_Record_Id_List"` — confirm only the definition in `index.js` remains (call sites were removed in Story 29.2)
  - [ ] Remove the function declaration and entire body
- [ ] Add comment to `g_record_id_list` (AC: #3)
  - [ ] Locate the `g_record_id_list` declaration in `index.js` and add the offline-mode comment
- [ ] Build and smoke test (AC: #5)
  - [ ] Run `build-server` task — zero errors
  - [ ] Create a case in online mode — completes normally
  - [ ] Create a case in offline mode — uses Set loop, completes normally

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
