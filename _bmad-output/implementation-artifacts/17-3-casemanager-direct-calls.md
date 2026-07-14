# Story 17.3 — Route CaseManager Direct mmrds Calls Through CaseDAL

**Epic:** 17 — mmrds CRUD Consolidation (SQL Migration Foundation)
**Story ID:** 17.3
**Status:** ready-for-dev
**Date added:** 2026-07-14
**Depends on:** 17.2 (ICaseRepository + CaseDAL canonicalized)
**Source requirements:** epics.md §Epic 17 Story 17.3; project-context.md §2.2

---

## User Story

As a developer,
I want `CaseManager` to stop calling `CouchDbHttpClient.ExecuteAsync` with mmrds URLs directly,
So that all case document access in the manager layer routes through `ICaseRepository`.

---

## Acceptance Criteria

**AC-1 — All direct mmrds HTTP calls replaced**
Given the 13 direct mmrds call sites in `CaseManager.cs` documented in Dev Notes below
When this story is complete
Then each call is replaced with the corresponding `ICaseRepository` method; no `ExecuteAsync` calls referencing `mmrds` remain in `CaseManager.cs`

**AC-2 — ICaseRepository injected into CaseManager**
Given `CaseManager` currently receives `CouchDbHttpClient` for its direct HTTP calls
When `ICaseRepository` is added as a constructor parameter
Then it is injected via constructor injection; DI registration in `mmria-server/Program.cs` is updated to satisfy the new dependency

**AC-3 — Mechanical substitution only**
Given each replacement
When the developer implements it
Then the HTTP verb, URL path, request body, response deserialization type, and error handling are identical to the original; no business logic changes are made

**AC-4 — Build succeeds, no controller changes**
Given the build after all substitutions
When verified
Then `mmria-server`, `mmria.common`, and `mmria.services` all build with zero errors; no changes are made outside `CaseManager.cs` and `Program.cs` in this story

---

## Dev Notes — Implementation

### Files to change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | **UPDATE** — replace 13 direct mmrds calls with ICaseRepository methods |
| `mmria-server/Program.cs` | **UPDATE** — add `ICaseRepository` to `CaseManager`'s DI registration |

---

### Call sites inventory (verified 2026-07-14)

All 13 direct mmrds call sites in `CaseManager.cs`:

| Line | Operation | URL Pattern | Replace with |
|------|-----------|-------------|-------------|
| 704 | `_find` POST — record ID existence check | **A** — `$"{dbInfo.url}/{dbInfo.prefix}mmrds/_find"` | `ICaseRepository._find` method (verify method name from 17.2) |
| 900 | Case GET by ID | B — `dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}")` | `GetCaseDocumentJsonAsync` or `GetCaseAsync` |
| 1025 | Case GET by ID | B | same as above |
| 1205 | Case GET by ID | B | same as above |
| 1330 | Case PUT by ID | B | `PutCaseDocumentJsonAsync` or `UpdateCaseAsync` |
| 1369 | Case PUT/GET by ID | B | match based on HTTP verb |
| 1519 | Case GET/PUT by ID | **A** — `dbConfig.url + $"/{dbConfig.prefix}mmrds/" + caseId` | same as corresponding B-pattern equivalent |
| 1723 | Case GET/PUT by ID | **A** — `dbConfig.url + $"/{dbConfig.prefix}mmrds/" + caseId` | same |
| 2098 | Case GET by ID | B | `GetCaseDocumentJsonAsync` |
| 2280 | Case GET at revision | B — `mmrds/{caseId}?rev={rev}` | `GetCaseAtRevisionAsync` |
| 2294 | Case PUT by ID | B | `PutCaseDocumentJsonAsync` or `UpdateCaseAsync` |
| 2392 | Case GET at revision | B — `mmrds/{caseId}?rev={storedRev}` | `GetCaseAtRevisionAsync` |
| (line 667) | doc comment only | — | no change needed |

> **Note on line 704:** This call uses `dbInfo` (not `dbConfig`) as the variable name, and calls `_find` on the mmrds database to check if a record ID already exists. Read the surrounding context to determine the correct `ICaseRepository` method name from Story 17.2. This uses `System.Text.Json` for deserialization while other areas use Newtonsoft — preserve the existing deserializer.

---

### Pattern A fixes included in this story

Lines 704, 1519, and 1723 use Pattern A. As part of the substitution, these are fixed to route through the repository (Pattern A is eliminated as a side effect).

---

### Architecture rule

Per project-context.md §2.2: Managers must not call `CouchDbHttpClient.ExecuteAsync` directly. All CouchDB access belongs in the DAL layer. `CaseManager` will continue to own business logic, orchestration, and result handling — it just stops constructing HTTP requests.
