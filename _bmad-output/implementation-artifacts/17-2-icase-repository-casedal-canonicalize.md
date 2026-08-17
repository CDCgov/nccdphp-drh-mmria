# Story 17.2 — Canonicalize CaseDAL and Extract ICaseRepository

**Epic:** 17 — mmrds CRUD Consolidation (SQL Migration Foundation)
**Story ID:** 17.2
**Status:** done
**Date added:** 2026-07-14
**Depends on:** 17.1 (mmrds Operation Catalog)
**Source requirements:** epics.md §Epic 17 Story 17.2; project-context.md §2.2

---

## User Story

As a developer,
I want a single `ICaseRepository` interface over all mmrds CRUD operations,
So that every caller in mmria-server and mmria.services can depend on the interface and a SQL migration requires changing only the `CaseDAL` implementation.

---

## Acceptance Criteria

**AC-1 — All CaseDAL methods use Pattern B**
Given the existing `CaseDAL` in `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs`
When this story is complete
Then all existing methods in `CaseDAL` use `dbConfig.Get_Prefix_DB_Url(...)` uniformly — no `$"{dbConfig.url}/{dbConfig.prefix}mmrds/..."` string interpolations remain in `CaseDAL.cs`

**AC-2 — All in-scope operations present in CaseDAL**
Given the operation catalog from Story 17.1
When the developer adds missing operations to `CaseDAL`
Then `CaseDAL` contains methods for every in-scope operation identified in the catalog, including at minimum: `GetCaseAsync`, `GetCaseDocumentJsonAsync`, `UpdateCaseAsync`, `PutCaseDocumentJsonAsync`, `DeleteCaseAsync`, `GetCaseAtRevisionAsync`, `GetCaseRevisionsAsync`, `GetCasesByDateLastUpdatedViewJsonAsync`, `GetCasesByDateCreatedViewJsonAsync`, `GetCasesByJurisdictionIdViewJsonAsync`, `GetCasesByLastNameViewJsonAsync`, `GetCasesByPmssNumberViewJsonAsync`, `GetCaseRecordIdListViewJsonAsync`, and any `_find` overloads required by other stories

**AC-3 — ICaseRepository interface extracted**
Given the full operation set is in `CaseDAL`
When the interface is extracted
Then `ICaseRepository` is defined in `mmria.common/SharedLibraries/Case/ICaseRepository.cs` with async method signatures matching every `CaseDAL` method; `CaseDAL` declares `public class CaseDAL : ICaseRepository`

**AC-4 — DI registration updated**
Given `ICaseRepository` is defined
When DI registration is updated in `mmria-server/Program.cs`
Then `services.AddScoped<ICaseRepository, CaseDAL>()` is present; any existing `AddScoped<CaseDAL>()` registrations are replaced

**AC-5 — Build succeeds with no caller changes**
Given no callers are changed in this story
When the build runs
Then `mmria-server` and `mmria.common` build with zero errors

---

## Dev Notes — Implementation

### Files to change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs` | **UPDATE** — fix all Pattern A to Pattern B; add missing operations |
| `mmria.common/SharedLibraries/Case/ICaseRepository.cs` | **CREATE** — interface with async signatures |
| `mmria-server/Program.cs` | **UPDATE** — swap DI registration to `AddScoped<ICaseRepository, CaseDAL>()` |

---

### Current state of CaseDAL

The file exists at `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs`. Current methods and their URL patterns:

| Method | Current Pattern | Fix needed |
|--------|----------------|------------|
| `GetCaseAsync` | A — `$"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}"` | → `dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}")` |
| `GetCaseDocumentJsonAsync` | A — same | → Pattern B |
| `UpdateCaseAsync` | A — same | → Pattern B |
| `PutCaseDocumentJsonAsync` | A — same | → Pattern B |
| `GetCasesByDateLastUpdatedViewJsonAsync` | A — `$"{dbConfig.url}/{dbConfig.prefix}mmrds/_design/sortable/_view/by_date_last_updated?..."` | → Pattern B |
| `GetCasesByDateCreatedViewJsonAsync` | A variant with manual prefix/no-prefix branch | → single Pattern B call, remove branch |
| `GetSoftLockedCaseIdForUserInAnotherTabAsync` | **B** — already correct | No change |

All Pattern A methods have the same substitution formula:
```csharp
// Before (Pattern A):
string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/{path}";

// After (Pattern B):
string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{path}");
```

For view queries the substitution is:
```csharp
// Before:
$"{dbConfig.url}/{dbConfig.prefix}mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=25000&descending=true"

// After:
dbConfig.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=25000&descending=true")
```

---

### Methods to add to CaseDAL

Based on the catalog (17.1), the following operations are used by other callers but not yet in `CaseDAL`. Add them before extracting the interface:

```csharp
// GET at specific revision — used by CaseWorkflowAdminDAL, AuditRecoveryDAL, BatchProcessor
public async Task<string> GetCaseAtRevisionAsync(string caseId, string revision, DBConfigurationDetail dbConfig)
{
    string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}?rev={revision}");
    return await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
}

// GET all revisions — used by CaseWorkflowAdminDAL
public async Task<string> GetCaseRevisionsAsync(string caseId, DBConfigurationDetail dbConfig)
{
    string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}?revs_info=true");
    return await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
}

// DELETE — add if catalog confirms usage
public async Task<string> DeleteCaseAsync(string caseId, string revision, DBConfigurationDetail dbConfig)
{
    string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}?rev={revision}");
    return await _couchDbHttpClient.ExecuteAsync("DELETE", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
}
```

Add view query methods for `by_jurisdiction_id`, `by_last_name`, `by_pmss_number`, `record_id_list` following the same pattern as `GetCasesByDateLastUpdatedViewJsonAsync`. Verify exact view/design document names against the catalog.

---

### ICaseRepository interface template

```csharp
// mmria.common/SharedLibraries/Case/ICaseRepository.cs
namespace mmria.common.SharedLibraries.Case;

public interface ICaseRepository
{
    Task<mmria.case_version.v260615.mmria_case> GetCaseAsync(string caseId, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string> GetCaseDocumentJsonAsync(string caseId, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<mmria.common.model.couchdb.document_put_response> UpdateCaseAsync(string caseId, mmria.case_version.v260615.mmria_case caseDoc, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string> PutCaseDocumentJsonAsync(string caseId, string caseDocumentJson, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string> DeleteCaseAsync(string caseId, string revision, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string> GetCaseAtRevisionAsync(string caseId, string revision, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string> GetCaseRevisionsAsync(string caseId, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string> GetCasesByDateLastUpdatedViewJsonAsync(mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string> GetCasesByDateCreatedViewJsonAsync(mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string> GetCasesByJurisdictionIdViewJsonAsync(mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string> GetCasesByLastNameViewJsonAsync(mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string> GetCasesByPmssNumberViewJsonAsync(mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string> GetCaseRecordIdListViewJsonAsync(mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string> GetSoftLockedCaseIdForUserInAnotherTabAsync(string userName, string currentTabId, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    // Add any additional _find overloads discovered in the catalog
}
```

Verify the exact namespace and type names against existing CaseDAL usages before finalizing.

---

### Architecture rule

Per project-context.md §2.2: the DAL layer owns all `CouchDbHttpClient.ExecuteAsync` calls. No Manager or Controller code is changed in this story.
