# Story 17.4 — Eliminate Duplicate mmrds CRUD in CaseWorkflowAdminDAL

**Epic:** 17 — mmrds CRUD Consolidation (SQL Migration Foundation)
**Story ID:** 17.4
**Status:** ready-for-dev
**Date added:** 2026-07-14
**Depends on:** 17.2 (ICaseRepository + CaseDAL canonicalized)
**Source requirements:** epics.md §Epic 17 Story 17.4; project-context.md §2.2

---

## User Story

As a developer,
I want `CaseWorkflowAdminDAL` to delegate case document operations to `ICaseRepository` instead of reimplementing them,
So that the duplicate mmrds methods in this DAL are removed.

---

## Acceptance Criteria

**AC-1 — Duplicate mmrds methods eliminated**
Given the following methods in `CaseWorkflowAdminDAL` that duplicate operations already in `ICaseRepository`:
`GetCasesByDateAsync`, `GetCaseDocumentAsync`, `UpdateCaseDocumentAsync`, `GetCaseRevisionsRawAsync`, `GetCaseAtRevisionAsync`, `RestoreCaseDocumentAsync`
When this story is complete
Then `ICaseRepository` is injected into `CaseWorkflowAdminDAL` and each of these methods delegates to the corresponding repository method; the direct `CouchDbHttpClient.ExecuteAsync` mmrds calls are removed from each method body

**AC-2 — Return type compatibility preserved**
Given that `GetCaseDocumentAsync` returns `ExpandoObject` and `GetCaseAtRevisionAsync` returns `ExpandoObject`, while `ICaseRepository` methods return `string`
When the methods are updated to delegate
Then the deserialization from `string` to `ExpandoObject` happens inside the DAL method body after the repository call returns; the manager calling these methods observes no signature change

**AC-3 — Audit and non-mmrds methods unchanged**
Given the audit write methods in `CaseWorkflowAdminDAL` (operations against the `audit` database): `WriteAuditEntryAsync`, `GetDeletedCasesViewAsync`, `GetAuditDocumentAsync`, `DeleteAuditDocumentAsync`
When this story is complete
Then these methods are unchanged — audit writes are not mmrds operations and are out of scope

**AC-4 — Manager and controller callers unchanged**
Given `CaseWorkflowAdminManager` calls the above DAL methods with unchanged signatures
When the DAL method signatures are preserved
Then `CaseWorkflowAdminManager` and all controllers that use it compile without changes

**AC-5 — Build succeeds**
Given the refactor is complete
When `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` runs
Then the build succeeds with exit code 0

---

## Dev Notes — Implementation

### Files to change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | **UPDATE** — inject `ICaseRepository`; replace 6 mmrds methods' implementations |
| `mmria-server/Program.cs` | **UPDATE** — add `ICaseRepository` to `CaseWorkflowAdminDAL` DI registration |

---

### Current state — CaseWorkflowAdminDAL mmrds methods (verified 2026-07-14)

All 6 mmrds call sites already use **Pattern B** (`Get_Prefix_DB_Url`) — correct URL construction. The change here is routing through the repository, not fixing URL patterns.

| Line | Method | mmrds operation | ICaseRepository equivalent |
|------|--------|----------------|---------------------------|
| 22 | `GetCasesByDateAsync` | `mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=25000&descending=true` | `GetCasesByDateLastUpdatedViewJsonAsync` + deserialize to `case_view_response` |
| 32 | `GetCaseDocumentAsync` | GET `mmrds/{caseId}` → `ExpandoObject` | `GetCaseDocumentJsonAsync` + deserialize to `ExpandoObject` |
| 39 | `UpdateCaseDocumentAsync` | PUT `mmrds/{caseId}` | `PutCaseDocumentJsonAsync` + deserialize to `document_put_response` |
| 74 | `GetCaseRevisionsRawAsync` | GET `mmrds/{caseId}?revs=true&open_revs=all` → `string` | `GetCaseRevisionsAsync` (ensure this method uses `?revs=true&open_revs=all`) |
| 80 | `GetCaseAtRevisionAsync` | GET `mmrds/{caseId}?rev={revision}` → `ExpandoObject` | `GetCaseAtRevisionAsync` + deserialize to `ExpandoObject` |
| 87 | `RestoreCaseDocumentAsync` | PUT `mmrds/{caseId}` | `PutCaseDocumentJsonAsync` + deserialize to `document_put_response` |

> **Note on `GetCaseRevisionsRawAsync`:** The current call uses `?revs=true&open_revs=all` — verify that the `GetCaseRevisionsAsync` method added in Story 17.2 uses the same query string. If Story 17.2 used `?revs_info=true` instead, a separate `GetCaseRevisionsRawAsync` method may be needed in `ICaseRepository`.

---

### Delegation pattern example

```csharp
// Before:
public async Task<System.Dynamic.ExpandoObject> GetCaseDocumentAsync(DBConfigurationDetail dbConfig, string caseId)
{
    var url = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}");
    var response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
    return Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(response);
}

// After:
public async Task<System.Dynamic.ExpandoObject> GetCaseDocumentAsync(DBConfigurationDetail dbConfig, string caseId)
{
    var json = await _caseRepository.GetCaseDocumentJsonAsync(caseId, dbConfig);
    return Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(json);
}
```

---

### Constructor update

```csharp
// Before:
public CaseWorkflowAdminDAL(CouchDbHttpClient couchDbHttpClient)
{
    _couchDbHttpClient = couchDbHttpClient;
}

// After:
public CaseWorkflowAdminDAL(CouchDbHttpClient couchDbHttpClient, ICaseRepository caseRepository)
{
    _couchDbHttpClient = couchDbHttpClient;  // still needed for audit operations
    _caseRepository = caseRepository;
}
```

The `_couchDbHttpClient` field must be retained because `WriteAuditEntryAsync`, `GetDeletedCasesViewAsync`, `GetAuditDocumentAsync`, and `DeleteAuditDocumentAsync` still use it directly for `audit/` database operations.
