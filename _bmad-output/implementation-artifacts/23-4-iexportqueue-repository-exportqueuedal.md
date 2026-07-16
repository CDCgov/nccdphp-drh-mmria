# Story 23.4 — `IExportQueueRepository` over `ExportQueueDAL`

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story ID:** 23.4
**Status:** ready-for-dev
**Date added:** 2026-07-16
**Depends on:** 23.1
**Source requirements:** epics.md §Epic 23 Story 23.4; project-context.md §2.2

---

## User Story

As a developer,
I want a single `IExportQueueRepository` interface over all `export_queue` database operations,
So that the export queue can be migrated to SQL (or a dedicated job-queue store) by changing only `ExportQueueDAL`.

---

## Acceptance Criteria

**AC-1 — `IExportQueueRepository` interface extracted**
Given `ExportQueueDAL` already uses `dbConfig.Get_Prefix_DB_Url(...)` throughout (no URL fixes required)
When the interface is extracted
Then `IExportQueueRepository` is defined in `mmria.common/SharedLibraries/ExportQueue/` with async method signatures matching every `ExportQueueDAL` method; `ExportQueueDAL` implements `IExportQueueRepository`

**AC-2 — DI registration**
Given `IExportQueueRepository` is defined
When DI registration is updated in `mmria-server/Program.cs`
Then `IExportQueueRepository` is registered as `ExportQueueDAL` in the service collection

**AC-3 — core_element_exporter leak routed**
Given `core_element_exporter.cs` in `mmria-server/util/` line ~804 reads an export queue document directly using Pattern A (`$"{db_config.url}/{db_config.prefix}export_queue/{item_id}"`)
When this story is complete
Then that call is replaced with the corresponding `IExportQueueRepository` method; `IExportQueueRepository` is injected

**AC-4 — Rebuild actors declared out of scope**
Given `Rebuild_Export_Queue.cs` and `rebuild_export_queue_job.cs` perform DROP/CREATE on the export_queue database
When evaluated
Then they are confirmed as out of scope in the catalog — DB lifecycle operations are not application CRUD and do not belong behind `IExportQueueRepository`

**AC-5 — Build passes**
Given the build after all changes
When verified
Then all three projects build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/ExportQueue/IExportQueueRepository.cs` | **CREATE** — interface |
| `mmria.common/SharedLibraries/ExportQueue/DAL/ExportQueueDAL.cs` | **UPDATE** — implement `IExportQueueRepository` (no URL changes needed) |
| `mmria-server/util/core_element_export/core_element_exporter.cs` | **UPDATE** — inject `IExportQueueRepository`; replace 1 direct Pattern A call at line ~804 |
| `mmria-server/Program.cs` | **UPDATE** — add `services.AddScoped<IExportQueueRepository, ExportQueueDAL>()` |

**Design notes:**
- `ExportQueueDAL` already uses Pattern B throughout — this is the lowest-effort interface extraction in Epic 23. The only code change beyond the interface is routing one Pattern A call in `core_element_exporter`.
- Infra out-of-scope: `Rebuild_Export_Queue.cs` (actor), `rebuild_export_queue_job.cs`, and `c_db_setup.cs` all perform export_queue DB lifecycle operations — these are not routed through `IExportQueueRepository`.

---

## Sequencing

Depends on 23.1. Lowest risk story in Epic 23 — can be implemented quickly. Can proceed in parallel with 23.2, 23.3, 23.5, 23.6, 23.8.
