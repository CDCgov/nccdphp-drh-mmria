# Story 17.5 — Eliminate Duplicate mmrds Calls in AuditRecoveryDAL, CVSDAL, VitalImportDAL, and AttachmentDAL

**Epic:** 17 — mmrds CRUD Consolidation (SQL Migration Foundation)
**Story ID:** 17.5
**Status:** ready-for-dev
**Date added:** 2026-07-14
**Depends on:** 17.2 (ICaseRepository + CaseDAL canonicalized)
**Source requirements:** epics.md §Epic 17 Story 17.5; project-context.md §2.2

---

## User Story

As a developer,
I want the remaining SharedLibraries DAL files that independently call mmrds URLs to delegate to `ICaseRepository`,
So that mmrds access is fully consolidated within the common library layer.

---

## Acceptance Criteria

**AC-1 — AuditRecoveryDAL mmrds calls replaced**
Given `AuditRecoveryDAL.cs` line 24 (view by_id query, Pattern A) and line 75 (GET at revision, Pattern A)
When this story is complete
Then both calls are replaced with the corresponding `ICaseRepository` methods; `ICaseRepository` is injected via constructor injection

**AC-2 — CVSDAL mmrds calls replaced**
Given `CVSDAL.cs` line 73 (by_date_last_updated view, Pattern B) and line 84 (case GET by ID, Pattern B)
When this story is complete
Then both calls are replaced with the corresponding `ICaseRepository` methods; `ICaseRepository` is injected via constructor injection

**AC-3 — VitalImportDAL mmrds calls replaced**
Given `VitalImportDAL.cs` line 26 (case GET by ID, Pattern A) and line 33 (case GET by ID, Pattern A)
When this story is complete
Then both calls are replaced with the corresponding `ICaseRepository` methods; `ICaseRepository` is injected via constructor injection

**AC-4 — AttachmentDAL mmrds call replaced**
Given `AttachmentDAL.cs` line 21 (by_pmss_number view query, Pattern A)
When this story is complete
Then the call is replaced with the corresponding `ICaseRepository` method; `ICaseRepository` is injected via constructor injection

**AC-5 — DI registrations updated**
Given each DAL's updated constructor
When `ICaseRepository` is added as a constructor parameter to all four DALs
Then the DI registrations in `mmria-server/Program.cs` are updated to satisfy the new dependencies; no other registration changes are made

**AC-6 — Build succeeds, no manager or controller changes**
Given the build after all changes
When verified
Then all three projects (`mmria-server`, `mmria.common`, `mmria.services`) build with zero errors; no manager or controller code is changed in this story

---

## Dev Notes — Implementation

### Files to change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | **UPDATE** — inject `ICaseRepository`; replace 2 mmrds calls |
| `mmria.common/SharedLibraries/CVS/DAL/CVSDAL.cs` | **UPDATE** — inject `ICaseRepository`; replace 2 mmrds calls |
| `mmria.common/SharedLibraries/VitalImport/DAL/VitalImportDAL.cs` | **UPDATE** — inject `ICaseRepository`; replace 2 mmrds calls |
| `mmria.common/SharedLibraries/Attachment/DAL/AttachmentDAL.cs` | **UPDATE** — inject `ICaseRepository`; replace 1 mmrds call |
| `mmria-server/Program.cs` | **UPDATE** — add `ICaseRepository` to DI registrations for all 4 DALs |

---

### Call sites inventory (verified 2026-07-14)

#### AuditRecoveryDAL.cs

| Line | Operation | Pattern | ICaseRepository method |
|------|-----------|---------|----------------------|
| 24 | `mmrds/_design/sortable/_view/by_id?key="{caseId}"` | **A** | Verify method name from 17.1 catalog — possibly `GetCasesByIdViewJsonAsync` or equivalent; add to CaseDAL if missing |
| 75 | GET `mmrds/{caseId}?rev={revisionId}` | **A** | `GetCaseAtRevisionAsync(caseId, revisionId, dbConfig)` |

> **Note on line 24 view name:** `by_id` is a different view from `by_date_last_updated` or others in `CaseDAL`. Confirm the exact design document/view name exists in CouchDB. If `ICaseRepository` does not have a `by_id` view method after Story 17.2, add one in this story following the same pattern.

#### CVSDAL.cs

| Line | Operation | Pattern | ICaseRepository method |
|------|-----------|---------|----------------------|
| 73 | `mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=30000&descending=true` | **B** | `GetCasesByDateLastUpdatedViewJsonAsync(dbConfig)` — note: limit 30000 vs. CaseDAL's 25000; align or add parameter |
| 84 | GET `mmrds/{caseId}` | **B** | `GetCaseDocumentJsonAsync(caseId, dbConfig)` |

> **Note on line 73 limit difference:** CVSDAL uses `limit=30000` while `CaseDAL.GetCasesByDateLastUpdatedViewJsonAsync` uses `limit=25000`. Decide: either use the same method (accepting the limit difference), or add a `GetCasesByDateLastUpdatedViewJsonAsync(DBConfigurationDetail dbConfig, int limit)` overload to `ICaseRepository`. Document the choice in a code comment.

#### VitalImportDAL.cs

| Line | Operation | Pattern | ICaseRepository method |
|------|-----------|---------|----------------------|
| 26 | GET `mmrds/{case_id}` | **A** | `GetCaseDocumentJsonAsync(case_id, dbConfig)` |
| 33 | GET `mmrds/{id}` | **A** | `GetCaseDocumentJsonAsync(id, dbConfig)` |

#### AttachmentDAL.cs

| Line | Operation | Pattern | ICaseRepository method |
|------|-----------|---------|----------------------|
| 21 | `mmrds/_design/sortable/_view/by_pmss_number?skip=0&take=250000` | **A** | `GetCasesByPmssNumberViewJsonAsync(dbConfig)` — add to `ICaseRepository` / `CaseDAL` in this story if not already present from 17.2 |

---

### Adding missing view methods

If `GetCasesByPmssNumberViewJsonAsync`, `GetCasesByIdViewJsonAsync`, or other view methods are not yet in `ICaseRepository` after Story 17.2, add them in this story following the same pattern:

```csharp
// In CaseDAL:
public async Task<string> GetCasesByPmssNumberViewJsonAsync(DBConfigurationDetail dbConfig)
{
    string requestUrl = dbConfig.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_pmss_number?skip=0&take=250000");
    return await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
}
```

Add the corresponding signature to `ICaseRepository` at the same time.
