# Story 24.10 — Route `mmria.services` Export Queue Calls Through `IExportQueueRepository`

**Epic:** 24 — Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)
**Story ID:** 24.10
**Status:** not-started
**Date added:** 2026-07-16
**Depends on:** Epic 23 Story 23.4 done
**Source requirements:** epics.md §Epic 24; mmrds_operation_catalog.md §export_queue Operations; project-context.md §2.2

---

## User Story

As a developer,
I want all `export_queue` database calls in `mmria.services` to route through `IExportQueueRepository`,
So that the services project is covered by the same repository contract as `mmria-server` and a SQL migration requires changing only `ExportQueueDAL`.

---

## Background

Story 23.4 established `IExportQueueRepository` and routed the `mmria-server` call site. The Story 23.1 catalog also found Pattern A direct calls in four `mmria.services` files that were not addressed. This story completes the export_queue consolidation.

---

## Acceptance Criteria

**AC-1 — `Process_Export_Queue.cs` calls replaced**
Given `mmria.services/Actors/ExportQueue/Process_Export_Queue.cs` constructs `export_queue` URLs directly at approximately lines 221, 285, 316, 354, and 385 (all Pattern A):
- Line ~221: `GET {prefix}export_queue/_all_docs` — enumerate all queue items at actor tick start
- Line ~285: `GET {prefix}export_queue/{id}` — read queue item before status update
- Lines ~316, 354, 385: `PUT {prefix}export_queue/{id}` — update status to processing / success / failure
When this story is complete
Then each is replaced with the corresponding `IExportQueueRepository` method; `IExportQueueRepository` is injected into the actor via Akka.NET actor props factory

**AC-2 — `exporter.cs` calls replaced**
Given `mmria.services/Utilities/Exporter/exporter.cs` constructs `export_queue` URLs directly at approximately lines 1364, 1373, and 1386 (all Pattern A):
- Line ~1364: GET queue item (read before update)
- Line ~1373: PUT queue item (write back after status update)
- Line ~1386: GET queue item (separate read path)
When this story is complete
Then each is replaced with the corresponding `IExportQueueRepository` method; `IExportQueueRepository` is injected via constructor injection

**AC-3 — `mmrds_exporter.cs` calls replaced**
Given `mmria.services/Utilities/Exporter/mmrds_exporter.cs` constructs `export_queue` URLs directly at approximately lines 1794 and 1803 (all Pattern A):
- Line ~1794: GET queue item (read before update)
- Line ~1803: PUT queue item (write back after status update)
When this story is complete
Then each is replaced with the corresponding `IExportQueueRepository` method; `IExportQueueRepository` is injected via constructor injection

**AC-4 — `core_element_exporter.cs` (services) call replaced**
Given `mmria.services/Utilities/CoreElementExport/core_element_exporter.cs` constructs an `export_queue` URL directly at approximately line 795 (Pattern A):
- Line ~795: GET export queue item by ID
When this story is complete
Then that call is replaced with the corresponding `IExportQueueRepository` method; `IExportQueueRepository` is injected via constructor injection

**Note:** This is distinct from `mmria-server/util/core_element_export/core_element_exporter.cs` (line 804) which was already addressed by Story 23.4.

**AC-5 — Build passes**
Given the build after all changes
When verified
Then `mmria.services`, `mmria.common`, and `mmria-server` all build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.services/Actors/ExportQueue/Process_Export_Queue.cs` | **UPDATE** — inject `IExportQueueRepository` via actor props factory; replace ~5 direct export_queue calls |
| `mmria.services/Utilities/Exporter/exporter.cs` | **UPDATE** — inject `IExportQueueRepository`; replace ~3 direct export_queue calls |
| `mmria.services/Utilities/Exporter/mmrds_exporter.cs` | **UPDATE** — inject `IExportQueueRepository`; replace ~2 direct export_queue calls |
| `mmria.services/Utilities/CoreElementExport/core_element_exporter.cs` | **UPDATE** — inject `IExportQueueRepository`; replace ~1 direct export_queue call |

**Injection pattern:**
`Process_Export_Queue.cs` is an Akka actor — use the Akka.NET actor props-factory DI pattern. The utility files (`exporter.cs`, `mmrds_exporter.cs`, `core_element_exporter.cs`) use constructor injection following the `mmria.services` DI conventions.

**Design notes:**
- The export queue GET/PUT calls in `exporter.cs` and `mmrds_exporter.cs` are part of job status tracking (mark job as in-progress, complete, or failed). The business logic for status transitions stays in the caller; only the HTTP calls move to the repository.
- `ExportQueueDAL` already uses Pattern B throughout (confirmed in Story 23.4) — no URL changes needed in the DAL itself.
- Confirm exact line numbers and method names against the Story 24.1 catalog before implementing.

---

## Sequencing

Depends only on Epic 23 Story 23.4 (`IExportQueueRepository` must exist). Independent of all other Epic 24 stories — can proceed in any order. Low risk.
