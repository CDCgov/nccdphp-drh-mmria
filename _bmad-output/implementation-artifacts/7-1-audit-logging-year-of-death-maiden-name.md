# Story 7.1: Audit Logging for Year of Death and Maiden Name Admin Changes

Status: done

## Story

As an installation administrator,
I want my Year of Death and Maiden Name updates to appear in the case audit log,
so that there is a complete record of who changed these fields and what the values were before and after.

## Acceptance Criteria

1. When `UpdateYearOfDeathAsync()` succeeds, a `Change_Stack` document is written to the `audit` CouchDB database with `note` = `"admin change, year of death updated"`, a single `Change_Stack_Item` carrying the old year value in `old_value` and the new year value in `new_value`, and `prompt` / `object_path` identifying the year-of-death field.
2. When `UpdateMaidenNameAsync()` succeeds, a `Change_Stack` document is written to the `audit` CouchDB database with `note` = `"admin change, maiden name updated"`, a single `Change_Stack_Item` carrying the old maiden name in `old_value` and the new maiden name in `new_value`, and `prompt` / `object_path` identifying the maiden name field.
3. Both `Change_Stack` documents carry: `_id` (new `Guid.NewGuid().ToString()`), `case_id`, `user_name` (authenticated user), `date_created` (UTC now), `doc_type = "Change_Stack"`.
4. Each `Change_Stack_Item` carries: `user_name`, `doc_type = "Change_Stack_Item"`, and accurate `old_value` / `new_value`.
5. The audit entry is written after the case document is saved successfully — if the case save fails, no audit entry is written.
6. The audit write is fire-and-forget with logged failure: if the CouchDB `audit` PUT fails, the failure is logged to console but the admin action result is still returned as successful (same pattern as `SaveCaseAsync()`).
7. No changes to the audit log display, view filtering, or admin UI are required.

## Tasks / Subtasks

- [x] Audit `UpdateYearOfDeathAsync()` in `CaseManager.cs` (AC: #1, #3–#6)
  - [x] Capture old year value before overwriting `case_response.home_record.date_of_death.year`
  - [x] After `document_put_response.ok == true`: construct `Change_Stack` with `note = "admin change, year of death updated"`, `_id = Guid.NewGuid().ToString()`, `case_id`, `user_name`, `date_created = DateTime.UtcNow`
  - [x] Add one `Change_Stack_Item`: `prompt = "Year of Death"`, `object_path = "/home_record/date_of_death/year"`, `old_value = oldYear.ToString()`, `new_value = newYear.ToString()`, `user_name`, `doc_type = "Change_Stack_Item"`
  - [x] PUT to `{db_config.url}/{db_config.prefix}audit/{changeStack._id}` using `_couchDbHttpClient`
  - [x] Log failure; do not surface to caller
- [x] Audit `UpdateMaidenNameAsync()` in `CaseManager.cs` (AC: #2–#6)
  - [x] Capture old maiden name value before `certificate_identification["dmaiden"] = maidenNameReplacement`
  - [x] After `document_put_response.ok == true`: construct `Change_Stack` with `note = "admin change, maiden name updated"`, `_id = Guid.NewGuid().ToString()`, `case_id`, `user_name`, `date_created = DateTime.UtcNow`
  - [x] Add one `Change_Stack_Item`: `prompt = "Maiden Name"`, `object_path = "/death_certificate/certificate_identification/dmaiden"`, `old_value = oldMaidenName`, `new_value = maidenNameReplacement`, `user_name`, `doc_type = "Change_Stack_Item"`
  - [x] PUT to audit database; log failure; do not surface to caller
- [x] Build and verify (AC: #1–#6)
  - [x] Run `build-server` task — zero errors
  - [ ] Manually trigger a year-of-death update on a test case; check `audit` DB for the `Change_Stack` document
  - [ ] Manually trigger a maiden name update on a test case; check `audit` DB for the `Change_Stack` document

## Dev Notes

**File to modify:**
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs`

**Audit types:**
```csharp
// mmria.common.model.couchdb namespace
// Change_Stack fields used: _id, case_id, user_name, note, date_created, doc_type, items
// Change_Stack_Item fields used: user_name, prompt, object_path, old_value, new_value, doc_type
```

**Audit write pattern** (from `SaveCaseAsync()` in `CaseManager.cs`):
```csharp
var changeStack = new Change_Stack
{
    _id = Guid.NewGuid().ToString(),
    case_id = caseId,
    user_name = userName,
    note = "admin change, year of death updated",
    date_created = DateTime.UtcNow,
    doc_type = "Change_Stack",
    items = new List<Change_Stack_Item>
    {
        new Change_Stack_Item
        {
            user_name = userName,
            prompt = "Year of Death",
            object_path = "/home_record/date_of_death/year",
            old_value = oldYear.ToString(),
            new_value = yearOfDeathReplacement.Value.ToString(),
            doc_type = "Change_Stack_Item"
        }
    }
};

JsonSerializerSettings auditSettings = new JsonSerializerSettings();
auditSettings.NullValueHandling = NullValueHandling.Ignore;
var audit_string = JsonConvert.SerializeObject(changeStack, auditSettings);

string audit_url = db_config_for_audit.Get_Prefix_DB_Url($"audit/{changeStack._id}");
try
{
    string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
        "PUT", audit_url, audit_string,
        db_config_for_audit.user_name, db_config_for_audit.user_value);
    var audit_result = JsonConvert.DeserializeObject<document_put_response>(responseFromServer);
    if (audit_result == null || !audit_result.ok)
        Console.WriteLine($"Audit save failed for case {caseId}, audit {changeStack._id}");
}
catch (Exception ex)
{
    Console.WriteLine($"Audit save threw for case {caseId}, audit {changeStack._id}: {ex.Message}");
}
```

**`db_config` for audit write:** `UpdateYearOfDeathAsync` already has `db_config` (non-CDC-admin path) and `dbConfigSet.detail_list[stateDatabase]` (CDC admin path). The audit write must target the same `db_config` used for the case write — the `audit` prefix is on the same CouchDB instance as `mmrds`.

**Maiden name field path:** The maiden name lives at `death_certificate.certificate_identification.dmaiden`. The `object_path` for the `Change_Stack_Item` should be `/death_certificate/certificate_identification/dmaiden`.

**Old value capture:** In `UpdateYearOfDeathAsync`, capture `case_response.home_record.date_of_death.year` before overwriting. In `UpdateMaidenNameAsync`, capture `certificate_identification["dmaiden"]?.ToString() ?? ""` before overwriting.

**No UI changes:** The audit log display at `/_audit/{caseId}` already renders these entries using the standard `Change_Stack` format — the `note` field maps to the Update Action column in the UI.

### Project Structure Notes

- One file modified: `CaseManager.cs`
- No new C# files
- No new NuGet packages
- Changes are purely additive (new `Change_Stack` construction + CouchDB PUT calls)

### References

- [Source: nccdphp-drh-mmria-common/mmria.common/couchdb/save_case_request.cs — Change_Stack, Change_Stack_Item model]
- [Source: nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs — SaveCaseAsync() audit write pattern, UpdateYearOfDeathAsync(), UpdateMaidenNameAsync()]
- [Source: prd-mmria-2026-06-12/prd.md#FR-7.1, FR-7.2]

## Dev Agent Record

### Agent Model Used
Claude Sonnet 4.6 (GitHub Copilot)

### Debug Log References
- Build verified: `dotnet build mmria.common.csproj` — Build succeeded, zero errors.

### Completion Notes List
- Added `oldYear` capture before `case_response.home_record.date_of_death.year` assignment in `UpdateYearOfDeathAsync`. Audit write is guarded by `yearOfDeathReplacement.HasValue` and only fires on `document_put_response.ok`.
- Added `oldMaidenName` capture before `certificate_identification["dmaiden"]` assignment in `UpdateMaidenNameAsync`. Audit write fires on `document_put_response.ok`.
- Both audit writes use the same `db_config` as the case write (CDC admin vs. regular resolved at write time). Fire-and-forget: exceptions are logged to console, never surfaced to caller.
- Manual verification against a live CouchDB `audit` database remains for the developer per the last two subtasks.

### File List
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs`
