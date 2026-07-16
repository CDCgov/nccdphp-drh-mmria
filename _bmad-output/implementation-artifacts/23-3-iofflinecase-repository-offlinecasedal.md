# Story 23.3 — `IOfflineCaseRepository` over `OfflineCaseDAL`

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story ID:** 23.3
**Status:** ready-for-dev
**Date added:** 2026-07-16
**Depends on:** 23.1
**Source requirements:** epics.md §Epic 23 Story 23.3; project-context.md §2.2

---

## User Story

As a developer,
I want a single `IOfflineCaseRepository` interface over all `offline_cases` database operations,
So that the offline case path can be migrated to SQL by changing only `OfflineCaseDAL`.

---

## Acceptance Criteria

**AC-1 — OfflineCaseDAL URL canonicalization**
Given `OfflineCaseDAL` uses Pattern A for CRUD methods (lines ~46, 55, 197, 206) and Pattern B for view queries (lines ~183, 216)
When this story is complete
Then all `OfflineCaseDAL` CRUD methods use `dbConfig.Get_Prefix_DB_Url($"offline_cases/{...}")` (Pattern B) — no Pattern A strings remain in the file

**AC-2 — `IOfflineCaseRepository` interface extracted**
Given the full operation set in `OfflineCaseDAL`
When the interface is extracted
Then `IOfflineCaseRepository` is defined in `mmria.common/SharedLibraries/OfflineCase/` with async method signatures matching every `OfflineCaseDAL` method; `OfflineCaseDAL` implements `IOfflineCaseRepository`

**AC-3 — DI registration**
Given `IOfflineCaseRepository` is defined
When DI registration is updated in `mmria-server/Program.cs`
Then `IOfflineCaseRepository` is registered as `OfflineCaseDAL` in the service collection

**AC-4 — loggerController leak routed**
Given `loggerController.cs` line ~101 reads `offline_cases/_design/sortable/_view/lightweight-status-only` directly using `dbConfig.Get_Prefix_DB_Url(...)`
When this story is complete
Then that call is replaced with the corresponding `IOfflineCaseRepository` method; `IOfflineCaseRepository` is injected into `loggerController` via constructor injection

**AC-5 — Build passes**
Given the build after all changes
When verified
Then all three projects build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/OfflineCase/IOfflineCaseRepository.cs` | **CREATE** — interface |
| `mmria.common/SharedLibraries/OfflineCase/DAL/OfflineCaseDAL.cs` | **UPDATE** — fix Pattern A CRUD URLs to Pattern B; implement `IOfflineCaseRepository` |
| `mmria-server/Controllers/loggerController.cs` | **UPDATE** — inject `IOfflineCaseRepository`; replace 1 direct `offline_cases` view call (line ~101) |
| `mmria-server/Program.cs` | **UPDATE** — add `services.AddScoped<IOfflineCaseRepository, OfflineCaseDAL>()` |

**Design notes:**
- Only the 4 CRUD methods in `OfflineCaseDAL` need URL pattern fixes — the 2 view query methods already use `Get_Prefix_DB_Url`.
- `loggerController` will also receive `ILoggingRepository` in Story 23.8. When implementing 23.3, add the `IOfflineCaseRepository` constructor parameter alongside any existing dependencies; leave the logging calls unchanged for Story 23.8.
- `OfflineCaseDAL` private constant `OfflineCasesByCreatedByViewPath = "offline_cases/_design/sortable/_view/by-created-by"` is already relative path — this is correct and should be preserved.

---

## Sequencing

Depends on 23.1. Can proceed in parallel with 23.2, 23.4, 23.5, 23.6, 23.8. Note: `loggerController.cs` is also touched by Story 23.8 — coordinate or sequence 23.8 after 23.3 to avoid file conflicts.
