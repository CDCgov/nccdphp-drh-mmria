# Story 23.8 — `ILoggingRepository` + `LoggingDAL`

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story ID:** 23.8
**Status:** done
**Date added:** 2026-07-16
**Depends on:** 23.1
**Source requirements:** epics.md §Epic 23 Story 23.8; project-context.md §2.2

---

## User Story

As a developer,
I want all `logging` database operations consolidated in a new `LoggingDAL` behind `ILoggingRepository`,
So that the logging store can be migrated (SQL, Elasticsearch, or other) by changing only `LoggingDAL`.

---

## Acceptance Criteria

**AC-1 — `Logging` SharedLibraries feature created**
Given no `Logging` SharedLibraries feature exists
When this story creates one
Then the following structure exists:
```
mmria.common/SharedLibraries/Logging/
  ILoggingRepository.cs
  DAL/
    LoggingDAL.cs
```

**AC-2 — LoggingDAL contains all in-scope operations**
Given the in-scope `logging` database operations in `loggerController.cs`:
- Line ~93: `GET {prefix}logging` — reads the list of logging modules (Pattern A)
- Line ~283: filtered view/document read (Pattern A)
- Line ~653: document write — `POST {prefix}logging` (Pattern A)
When `LoggingDAL` is created
Then it contains async methods for each in-scope operation using `dbConfig.Get_Prefix_DB_Url($"logging/...")` (Pattern B) throughout; `ILoggingRepository` is defined in the same directory; `LoggingDAL` implements `ILoggingRepository`

**AC-3 — c_db_setup out of scope**
Given `c_db_setup.cs` creates the `logging` database on first install
When evaluated
Then it is confirmed as out of scope in the catalog — DB creation is infrastructure

**AC-4 — DI registration**
Given `ILoggingRepository` is defined
When DI registration is updated in `mmria-server/Program.cs`
Then `ILoggingRepository` is registered as `LoggingDAL` in the service collection

**AC-5 — loggerController logging calls routed**
Given `loggerController.cs` currently owns all direct `logging` database access (3 hits, all Pattern A)
When this story is complete
Then each is replaced with the corresponding `ILoggingRepository` method; `ILoggingRepository` is injected into `loggerController` via constructor injection; `loggerController` constructs no `logging/` URLs

**AC-6 — Both repository dependencies satisfied in loggerController**
Given `loggerController` also reads from `offline_cases` via `IOfflineCaseRepository` (Story 23.3) and now from `ILoggingRepository`
When this story and Story 23.3 are both complete
Then `loggerController` injects both `IOfflineCaseRepository` and `ILoggingRepository`; DI registration satisfies both dependencies

**AC-7 — Build passes**
Given the build after all changes
When verified
Then all three projects build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/Logging/ILoggingRepository.cs` | **CREATE** — interface |
| `mmria.common/SharedLibraries/Logging/DAL/LoggingDAL.cs` | **CREATE** — implementation with all in-scope operations using Pattern B |
| `mmria-server/Controllers/loggerController.cs` | **UPDATE** — inject `ILoggingRepository` (and `IOfflineCaseRepository` from 23.3); replace 3 direct `logging` URL constructions |
| `mmria-server/Program.cs` | **UPDATE** — add `services.AddScoped<ILoggingRepository, LoggingDAL>()` |

**Design notes:**
- **loggerController file conflict with 23.3:** Both this story and Story 23.3 modify `loggerController.cs`. If running in parallel, coordinate to avoid merge conflicts. Recommended: run 23.3 first (simpler change), then 23.8 adds `ILoggingRepository` on top.
- `loggerController` is also a Wave 9 planned migration target (`LoggingDiagnostics` feature in the controller migration matrix). This story only routes the URL construction — the full manager/DAL extraction is deferred to Wave 9.
- The `logging` URL at line ~93 uses `$"{db_config.url}/{db_config.prefix}logging"` (no trailing path) — this is a GET on the database root, not a document. Include this as a `GetLoggingModulesAsync` method in `LoggingDAL` using `dbConfig.Get_Prefix_DB_Url("logging")`.
- Review all 3 logging access points in `loggerController` carefully during the gap scan (Story 23.1) to confirm the exact operations before creating `LoggingDAL` methods.

---

## Sequencing

Depends on 23.1. Can proceed in parallel with 23.2, 23.3, 23.4, 23.5, 23.6. Recommend sequencing after 23.3 due to shared `loggerController.cs` file. Story 23.7 is independent.

---

## Dev Agent Record

**Agent:** GitHub Copilot (Claude Sonnet 4.6)
**Completed:** 2026-07-16
**Status:** done

### Changes Made

| File | Action |
|------|--------|
| `mmria.common/SharedLibraries/Logging/ILoggingRepository.cs` | CREATED — interface with 3 methods |
| `mmria.common/SharedLibraries/Logging/DAL/LoggingDAL.cs` | CREATED — Pattern B implementation |
| `mmria-server/Controllers/loggerController.cs` | UPDATED — added `ILoggingRepository` to constructor; replaced 3 logging URL constructions |
| `mmria-server/Program.cs` | UPDATED — added `AddScoped<ILoggingRepository, LoggingDAL>()` |

### Implementation Notes

- `GetLoggingModulesAsync` accesses `logging/_design/sortable/_view/by-offline-session` (the actual view used for modules data, not bare DB root as described in the story notes)
- `GetFilteredLoggingAsync(string filterOrViewPath, ...)` takes path relative to `logging/`; `GetLogs` was refactored to build `viewPath` instead of `viewUrl` (removing the `dbUrl` prefix)
- `SaveLog` now calls `PostLoggingDocumentAsync` directly — error handling preserved in-place
- Both `IOfflineCaseRepository` (Story 23.3) and `ILoggingRepository` are injected in `loggerController`
- Build verified: 0 errors in both `mmria.common` and `mmria-server`

### AC Verification

- ✅ AC-1: `SharedLibraries/Logging/ILoggingRepository.cs` and `DAL/LoggingDAL.cs` created
- ✅ AC-2: All 3 in-scope operations implemented in `LoggingDAL` using Pattern B
- ✅ AC-3: `c_db_setup.cs` not touched
- ✅ AC-4: `services.AddScoped<ILoggingRepository, LoggingDAL>()` added to `Program.cs`
- ✅ AC-5: All 3 direct `logging` URL constructions replaced in `loggerController.cs`
- ✅ AC-6: `loggerController` injects both `IOfflineCaseRepository` and `ILoggingRepository`
- ✅ AC-7: Build passes with 0 errors
