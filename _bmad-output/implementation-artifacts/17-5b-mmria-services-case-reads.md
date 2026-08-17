# Story 17.5b — Route mmria.services Case Reads Through ICaseRepository

**Epic:** 17 — mmrds CRUD Consolidation (SQL Migration Foundation)
**Story ID:** 17.5b
**Status:** done
**Date added:** 2026-07-14
**Depends on:** 17.2 (ICaseRepository + CaseDAL canonicalized)
**Source requirements:** epics.md §Epic 17 Story 17.5b; project-context.md §2.2

---

## User Story

As a developer,
I want the background-job and exporter code in `mmria.services` to stop constructing mmrds URLs directly,
So that the services project is covered by the same `ICaseRepository` contract as the server.

---

## Acceptance Criteria

**AC-1 — BatchItemProcessingService mmrds call replaced**
Given `BatchItemProcessingService.cs` line 2616 — `$"{db_info.url}/{db_info.prefix}mmrds/{mmria_id}"` (Pattern A, GET by ID)
When this story is complete
Then the call is replaced with `ICaseRepository.GetCaseDocumentJsonAsync` or appropriate method; `ICaseRepository` is injected into the class

**AC-2 — BatchProcessor mmrds calls replaced**
Given `BatchProcessor.cs` line 498 — `_all_docs?include_docs=true` and line 512 — GET at revision
When this story is complete
Then the `_all_docs` call is replaced with a `GetAllCaseDocsAsync` method (add to `ICaseRepository` / `CaseDAL` if not already present); the revision call is replaced with `GetCaseAtRevisionAsync`

**AC-3 — core_element_exporter mmrds call replaced**
Given `core_element_exporter.cs` line 246 — `$"{db_config.url}/{db_config.prefix}mmrds/{case_id}"` (Pattern A, GET by ID)
When this story is complete
Then the call is replaced with the corresponding `ICaseRepository` method; `ICaseRepository` is injected into the class

**AC-4 — mmrds_exporter mmrds call replaced**
Given `mmrds_exporter.cs` line 277 — `$"{db_config.url}/{db_config.prefix}mmrds/{case_id}"` (Pattern A, GET by ID)
When this story is complete
Then the call is replaced with the corresponding `ICaseRepository` method; `ICaseRepository` is injected into the class

**AC-5 — PagedCaseIdLoader mmrds call replaced**
Given `PagedCaseIdLoader.cs` line 36 — `$"{dbConfig.url}/{dbConfig.prefix}mmrds/_design/sortable/_view/by_date_created?skip={skip}&limit={pageSize}"` (Pattern A, paginated view)
When this story is complete
Then the call is replaced with a paginated `GetCasesByDateCreatedPagedAsync(int skip, int pageSize, DBConfigurationDetail dbConfig)` method; add to `ICaseRepository` / `CaseDAL` if not already present

**AC-6 — exporter.cs mmrds calls replaced**
Given `exporter.cs` line 154 — `_all_docs` and line 536 — case GET by ID
When this story is complete
Then both calls are replaced with the corresponding `ICaseRepository` methods

**AC-7 — c_document_sync_all remains unchanged**
Given `c_document_sync_all.cs` in `mmria.services` lines 201, 264, 277 — bulk `_all_docs` sync operations
When this story is complete
Then these three calls are NOT changed — they are bulk infrastructure/CDC sync operations classified as out of scope in the Story 17.1 catalog; a code comment is added to each noting "Out of scope per Epic 17 — infrastructure/CDC sync"

**AC-8 — No new project reference needed**
Given `mmria.services` already references `mmria.common`
When this story is complete
Then no new project reference is added; `ICaseRepository` is available through the existing reference

**AC-9 — Build succeeds**
Given the build after all changes
When verified
Then `mmria.services`, `mmria.common`, and `mmria-server` all build with zero errors

---

## Dev Notes — Implementation

### Files to change

| File | Change |
|------|--------|
| `mmria.services/Services/BatchItemProcessingService.cs` | **UPDATE** — inject `ICaseRepository`; replace line 2616 |
| `mmria.services/Actors/BatchProcessor.cs` | **UPDATE** — inject `ICaseRepository`; replace lines 498, 512 |
| `mmria.services/Utilities/CoreElementExport/core_element_exporter.cs` | **UPDATE** — inject `ICaseRepository`; replace line 246 |
| `mmria.services/Utilities/Exporter/mmrds_exporter.cs` | **UPDATE** — inject `ICaseRepository`; replace line 277 |
| `mmria.services/Utilities/PagedCaseIdLoader.cs` | **UPDATE** — inject `ICaseRepository`; replace line 36 |
| `mmria.services/Utilities/Exporter/exporter.cs` | **UPDATE** — inject `ICaseRepository`; replace lines 154, 536 |
| `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | **UPDATE** — add out-of-scope comment only; no logic change |
| `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs` + `ICaseRepository.cs` | **UPDATE** — add missing methods if `_all_docs` or paginated view not already present |

---

### Call sites inventory (verified 2026-07-14)

| File | Line | Operation | Pattern | ICaseRepository method |
|------|------|-----------|---------|----------------------|
| `BatchItemProcessingService.cs` | 2616 | GET `mmrds/{mmria_id}` | **A** | `GetCaseDocumentJsonAsync` |
| `BatchProcessor.cs` | 498 | `mmrds/_all_docs?include_docs=true` | **A** | `GetAllCaseDocsAsync` (add if missing) |
| `BatchProcessor.cs` | 512 | GET `mmrds/{case_id}?rev={rev}` | **A** | `GetCaseAtRevisionAsync` |
| `core_element_exporter.cs` | 246 | GET `mmrds/{case_id}` | **A** | `GetCaseDocumentJsonAsync` |
| `mmrds_exporter.cs` | 277 | GET `mmrds/{case_id}` | **A** | `GetCaseDocumentJsonAsync` |
| `PagedCaseIdLoader.cs` | 36 | `mmrds/_design/sortable/_view/by_date_created?skip={skip}&limit={pageSize}` | **A** | `GetCasesByDateCreatedPagedAsync(skip, pageSize, dbConfig)` |
| `exporter.cs` | 154 | `mmrds/_all_docs` | **A** | `GetAllCaseDocsAsync` (add if missing) |
| `exporter.cs` | 536 | GET `mmrds/{case_id}` | **A** | `GetCaseDocumentJsonAsync` |
| `c_document_sync_all.cs` | 201, 264, 277 | bulk `_all_docs` CDC sync | **A** | **OUT OF SCOPE** — comment only |

---

### Methods to add to ICaseRepository / CaseDAL if not present after 17.2

#### GetAllCaseDocsAsync (used by BatchProcessor and exporter)
```csharp
public async Task<string> GetAllCaseDocsAsync(bool includeDocs, DBConfigurationDetail dbConfig)
{
    string query = includeDocs ? "?include_docs=true" : string.Empty;
    string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/_all_docs{query}");
    return await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
}
```

#### GetCasesByDateCreatedPagedAsync (used by PagedCaseIdLoader)
```csharp
public async Task<string> GetCasesByDateCreatedPagedAsync(int skip, int pageSize, DBConfigurationDetail dbConfig)
{
    string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/_design/sortable/_view/by_date_created?skip={skip}&limit={pageSize}");
    return await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
}
```

Note: The existing `GetCasesByDateCreatedViewJsonAsync` in CaseDAL hard-codes `skip=0&take=25000`. It should remain as-is for existing callers. The new paginated variant is a separate method.

---

### DI injection in services

`mmria.services` uses Akka.NET actors (Actors/) and hosted services (Services/). The injection approach differs by class type:

- **Hosted services** (`BatchItemProcessingService`): already use constructor DI — add `ICaseRepository` as a new constructor parameter
- **Akka actors** (`BatchProcessor`): actors are typically instantiated via `Props` — check how `BatchProcessor` receives its `CouchDbHttpClient` today; follow the same approach to add `ICaseRepository`
- **Utility classes** (`exporter`, `mmrds_exporter`, `core_element_exporter`, `PagedCaseIdLoader`): check if they use constructor injection or if they are `new`-ed up with `CouchDbHttpClient` passed in; follow the same construction pattern
