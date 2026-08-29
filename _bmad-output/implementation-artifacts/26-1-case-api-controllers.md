# Story 26.1 — Case API Controllers

**Epic:** 26 — Controller API Direct-Call Remediation
**Story ID:** 26.1
**Status:** done
**Date added:** 2026-07-17
**Depends on:** Epic 17 story 17.2 (ICaseRepository), Epic 24 story 24.2 (IDeIdentifiedRepository)
**Source requirements:** epics.md §Epic 26 Story 26.1; project-context.md §2.2

---

## User Story

As a developer,
I want the five case-data API controllers to route their database calls through `ICaseRepository` or `IDeIdentifiedRepository` instead of calling `CouchDbHttpClient.ExecuteAsync` directly,
So that these controller-layer call sites have the same SQL migration seam as the managers.

---

## Acceptance Criteria

**AC-1 — `caseController.cs` direct call replaced**
Given `caseController.cs` at approximately line 114 calls `_couchDbHttpClient.ExecuteAsync("GET", "{prefix}mmrds/{id}", ...)` in the `GetRev` action to fetch the case document for its `_rev`
When this story is complete
Then that call is replaced with `await _caseRepository.GetCaseDocumentJsonAsync(sanitizedId, db_config)` (or `GetCaseDocumentWithStatusAsync` if 404 handling is needed); `ICaseRepository` is injected via the controller constructor and registered in DI; the response shape and route are unchanged

**AC-2 — `case_viewController.pmss.cs` direct calls replaced**
Given `case_viewController.pmss.cs` (guarded by `#if IS_PMSS_ENHANCED`) calls `_couchDbHttpClient.ExecuteAsync` at approximately lines 113–114
When this story is complete
Then each call is replaced with the corresponding `ICaseRepository` method; the `#if IS_PMSS_ENHANCED` guard is preserved unchanged; behavior is identical to pre-change

**AC-3 — `caseRevisionListController.cs` call replaced**
Given `caseRevisionListController.cs` at approximately line 52 calls `_couchDbHttpClient.ExecuteAsync("GET", "{prefix}mmrds/{id}", ...)` to retrieve the case revision list
When this story is complete
Then that call is replaced with `await _caseRepository.GetCaseRevisionsRawAsync(sanitizedId, db_config)` (returns raw JSON string needed for the revision list response)

**AC-4 — `de_idController.cs` call replaced**
Given `de_idController.cs` at approximately line 50 calls `_couchDbHttpClient.ExecuteAsync("GET", "{prefix}de_id/{id}", ...)` to read a de-identified document
When this story is complete
Then that call is replaced with the appropriate `IDeIdentifiedRepository` read method; if `IDeIdentifiedRepository` does not yet have a `GetDocumentAsync(string id, DBConfigurationDetail dbConfig)` method, add it to the interface and implement it in `DeIdentifiedDAL` before replacing the call site (see Dev Notes below)

**AC-5 — `record_idController.cs` call replaced**
Given `record_idController.cs` at approximately line 52 calls `_couchDbHttpClient.ExecuteAsync("GET", "{prefix}mmrds/_design/...", ...)` to check record ID existence (view query)
When this story is complete
Then that call is replaced with `await _caseRepository.RecordIdExistsAsync(sanitizedId, db_config)` or the equivalent view-query method on `ICaseRepository`

**AC-6 — No route, signature, or response shape changes**
Given the controllers' existing HTTP method attributes, route paths, controller action signatures, and JSON response shapes
When this story is implemented
Then none are changed — only the internal CouchDB call site is replaced; no `[Bind]` attributes are added or removed

**AC-7 — Build passes with zero errors**
Given the changes above
When the build runs
Then `mmria-server` builds with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/Controllers/api/caseController.cs` | **UPDATE** — inject `ICaseRepository`; replace `ExecuteAsync` GET with `GetCaseDocumentJsonAsync` or `GetCaseDocumentWithStatusAsync` |
| `source-code/mmria/mmria-server/Controllers/api/case_viewController.pmss.cs` | **UPDATE** — inject `ICaseRepository`; replace call(s); PMSS guard preserved |
| `source-code/mmria/mmria-server/Controllers/api/caseRevisionListController.cs` | **UPDATE** — inject `ICaseRepository`; replace with `GetCaseRevisionsRawAsync` |
| `source-code/mmria/mmria-server/Controllers/api/de_idController.cs` | **UPDATE** — inject `IDeIdentifiedRepository`; replace read with repo method (add `GetDocumentAsync` if needed) |
| `source-code/mmria/mmria-server/Controllers/api/record_idController.cs` | **UPDATE** — inject `ICaseRepository`; replace view query with `RecordIdExistsAsync` |

**`IDeIdentifiedRepository` read method gap:**
`IDeIdentifiedRepository` currently has write/lifecycle methods from Story 24.2 but no individual document GET. If `de_idController.cs` reads a full de-identified document (not just the `_rev`), add to the interface:
```csharp
Task<string?> GetDocumentJsonAsync(string id, DBConfigurationDetail dbConfig);
```
Implement in `DeIdentifiedDAL` as `GET {prefix}de_id/{id}`. If it only needs the revision, use the existing `GetRevisionAsync(id, dbConfig)` instead.

**DI registration:** All five controllers should already have `ICaseRepository` available from the existing DI registration (Epic 17). Confirm `IDeIdentifiedRepository` is also registered from Story 24.2 before adding the injection to `de_idController.cs`.

**Architecture rule reminder:** `HttpContext`, `User`, `View()`, `Json()`, cookies, and response headers **stay in the controller**. Only the CouchDB call site moves — the surrounding action logic, authorization attributes, and response construction remain unchanged.
