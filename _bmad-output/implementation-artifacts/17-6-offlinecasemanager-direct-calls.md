# Story 17.6 — Eliminate Direct mmrds Calls in OfflineCaseManager

**Epic:** 17 — mmrds CRUD Consolidation (SQL Migration Foundation)
**Story ID:** 17.6
**Status:** done
**Date added:** 2026-07-14
**Depends on:** 17.2 (ICaseRepository + CaseDAL canonicalized)
**Source requirements:** epics.md §Epic 17 Story 17.6; project-context.md §2.2

---

## User Story

As a developer,
I want `OfflineCaseManager` to stop issuing raw HTTP requests to mmrds URLs,
So that the offline case path follows the same Manager → DAL boundary as every other feature.

---

## Acceptance Criteria

**AC-1 — All three direct mmrds calls replaced**
Given the three direct mmrds HTTP calls in `OfflineCaseManager.cs` at lines 104, 298, and 398
(all using Pattern A: `$"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}"`)
When this story is complete
Then each is replaced with the corresponding `ICaseRepository` method; no `ExecuteAsync` calls referencing `mmrds` remain in `OfflineCaseManager.cs`

**AC-2 — ICaseRepository injected directly into manager**
Given the `OfflineCase` feature in `SharedLibraries` already has an `OfflineCaseDAL`
When the developer chooses the injection point
Then `ICaseRepository` is injected directly into `OfflineCaseManager` — not routed through `OfflineCaseDAL`; `OfflineCaseDAL` owns offline-specific document types and is not the right owner for generic case CRUD

**AC-3 — DI registration updated**
Given `ICaseRepository` is added as a constructor parameter to `OfflineCaseManager`
When DI registration is updated in `mmria-server/Program.cs`
Then the registration satisfies the new dependency; no other registration changes are made

**AC-4 — Offline case behavior unchanged**
Given the offline sync logic in `OfflineCaseManager` (lines 104, 298, 398 and surrounding code)
When the substitution is complete
Then HTTP verb, URL path, request body, response deserialization type, and error handling are identical to the original; no business logic changes are made in this story

**AC-5 — Build succeeds**
Given the changes are complete
When `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` runs
Then the build succeeds with exit code 0

---

## Dev Notes — Implementation

### Files to change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs` | **UPDATE** — inject `ICaseRepository`; replace 3 direct mmrds calls |
| `mmria-server/Program.cs` | **UPDATE** — add `ICaseRepository` to `OfflineCaseManager` DI registration |

---

### Call sites inventory (verified 2026-07-14)

All three use identical Pattern A construction:

| Line | HTTP Verb | Operation | Pattern | ICaseRepository method |
|------|-----------|-----------|---------|----------------------|
| 104 | GET | `mmrds/{caseId}` | **A** — `$"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}"` | `GetCaseDocumentJsonAsync(caseId, dbConfig)` |
| 298 | GET or PUT (verify by reading context) | `mmrds/{caseId}` | **A** | match based on HTTP verb at each line |
| 398 | GET or PUT (verify by reading context) | `mmrds/{caseId}` | **A** | match based on HTTP verb at each line |

> Before implementing, read the surrounding context at lines 104, 298, and 398 to verify the HTTP verb used at each site (`"GET"`, `"PUT"`, etc.). The variable `caseUrl` is set at those lines — check the `ExecuteAsync` call that uses it to confirm the method.

---

### Rationale for direct manager injection

`OfflineCaseManager` is responsible for managing offline case lifecycle (lock, sync, unlock). It reads and writes the generic case document as part of that lifecycle — these are not offline-specific documents, they are the real mmrds case documents. Routing through `ICaseRepository` directly in the manager is correct here; adding a passthrough method to `OfflineCaseDAL` would just be another layer of indirection with no benefit.

---

### Line 826 — comment only

`OfflineCaseManager.cs` line 826 contains the text `"mmrds database"` as part of a log/status message string. This is not a call site and requires no change.
