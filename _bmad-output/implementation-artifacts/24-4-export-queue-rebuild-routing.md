# Story 24.4 — Route Export Queue Rebuild Actors Through `IExportQueueRepository`

**Epic:** 24 — Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)
**Story ID:** 24.4
**Status:** done
**Date added:** 2026-07-16
**Depends on:** 24.1; Epic 23 Story 23.4 done
**Source requirements:** epics.md §Epic 24 Story 24.4; project-context.md §2.2

---

## User Story

As a developer,
I want `Rebuild_Export_Queue` and `rebuild_export_queue_job` to route their database lifecycle operations through `IExportQueueRepository`,
So that the nightly export-queue drop/recreate is fully behind the repository interface established in Story 23.4.

---

## Acceptance Criteria

**AC-1 — `PurgeAndReinitializeAsync` added to `IExportQueueRepository`**
Given `IExportQueueRepository` from Story 23.4 covers application CRUD but not database-lifecycle operations
When this story extends it
Then `IExportQueueRepository` gains:
- `PurgeAndReinitializeAsync(DBConfigurationDetail dbConfig)` — drops the `export_queue` database, recreates it empty, and restores the security document restricting access to the `abstractor` role
- SQL migration equivalent: `TRUNCATE TABLE export_queue` followed by resetting row-level permissions

**AC-2 — `ExportQueueDAL` implements `PurgeAndReinitializeAsync`**
Given the lifecycle operations currently in `Rebuild_Export_Queue.cs`
When `ExportQueueDAL` is updated
Then all CouchDB DELETE/PUT/security calls for the export_queue database lifecycle are moved into `ExportQueueDAL.PurgeAndReinitializeAsync`; the method uses `dbConfig.Get_Prefix_DB_Url("export_queue")` and `dbConfig.Get_Prefix_DB_Url("export_queue/_security")` (Pattern B) throughout

**AC-3 — `Rebuild_Export_Queue.cs` routes through repository**
Given `Rebuild_Export_Queue.cs` currently assembles `{url}/{prefix}export_queue` and `{url}/{prefix}export_queue/_security` URLs directly with `CouchDbHttpClient.ExecuteAsync`
When this story is complete
Then all direct `CouchDbHttpClient.ExecuteAsync` calls for database lifecycle in this file are replaced with `await _exportQueueRepository.PurgeAndReinitializeAsync(dbConfig)`; `IExportQueueRepository` is injected into the actor via Akka.NET actor props factory; the actor's Akka message-handling logic, scheduling conditions (midnight-only check at hour == 0), and log statements are unchanged

**AC-4 — `rebuild_export_queue_job.cs` handled per catalog finding**
Given `rebuild_export_queue_job.cs` is a legacy `IJob` implementation
When Story 24.1 catalog determines its status:
- If **actively registered** in the Quartz scheduler: the job is updated to inject `IExportQueueRepository` (via constructor injection on the Quartz job class) and call `PurgeAndReinitializeAsync(dbConfig)`; all direct CouchDB calls are removed
- If **not registered** (dead code superseded by the Akka actor): the file is left unchanged; a comment is added at the top of the class: `// Superseded by Rebuild_Export_Queue Akka actor. Not registered in scheduler. Retained for reference only.`; the catalog records this disposition

**AC-5 — Filesystem directory cleanup stays in actor**
Given `Rebuild_Export_Queue.cs` deletes the export output directory from the filesystem as part of the rebuild
When this story is implemented
Then the filesystem directory deletion code remains in the actor class — it is not moved to `ExportQueueDAL`; filesystem operations are not database operations

**AC-6 — Build passes**
Given the build after all changes
When verified
Then all three projects build with zero errors; the export-queue rebuild actor's observable behavior is identical to pre-change

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/ExportQueue/IExportQueueRepository.cs` | **UPDATE** — add `PurgeAndReinitializeAsync` signature |
| `mmria.common/SharedLibraries/ExportQueue/DAL/ExportQueueDAL.cs` | **UPDATE** — implement `PurgeAndReinitializeAsync` |
| `source-code/mmria/mmria-server/model/actor/quartz/Rebuild_Export_Queue.cs` | **UPDATE** — inject `IExportQueueRepository`; replace direct CouchDB lifecycle calls with `PurgeAndReinitializeAsync` |
| `source-code/mmria/mmria-server/model/rebuild_export_queue_job.cs` | **UPDATE or comment** — per AC-4 determination from Story 24.1 |

**New interface method signature:**
```csharp
Task PurgeAndReinitializeAsync(DBConfigurationDetail dbConfig);
```

**Security document note:**
The `_security` document set during purge/reinit restricts the export_queue database to the `abstractor` role. The JSON payload is currently hard-coded in `Rebuild_Export_Queue.cs`. Move it to a private constant in `ExportQueueDAL` — do not extract it to configuration.

**Design notes:**
- `Rebuild_Export_Queue.cs` is an Akka actor. `IExportQueueRepository` injection follows the same Akka.NET props-factory pattern as other actor dependencies in the codebase (see `MMRIA_Background_Jobs_Documentation.md` for the actor DI pattern).
- The midnight-only check (`if (message.hour == 0)`) is scheduling logic — it stays in the actor, not the DAL.
- `ExportQueueDAL` already uses Pattern B (from Story 23.4), so no URL-pattern changes are needed for existing methods.

---

## Sequencing

Depends on 24.1 and Epic 23 Story 23.4 (which creates `IExportQueueRepository`). Can proceed in parallel with 24.2, 24.3, 24.5 once 24.1 is complete. No other Epic 24 stories depend on this one.
