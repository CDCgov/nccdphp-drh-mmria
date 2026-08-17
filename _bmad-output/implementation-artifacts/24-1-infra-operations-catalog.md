# Story 24.1 — Infra Operations Catalog

**Epic:** 24 — Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)
**Story ID:** 24.1
**Status:** done
**Date added:** 2026-07-16
**Depends on:** none — discovery only
**Source requirements:** epics.md §Epic 24 Story 24.1; project-context.md §2.2

---

## User Story

As a developer,
I want a definitive catalog of every database operation in all in-scope infra files,
So that Stories 24.2–24.9 have an agreed-upon, complete operation set and every call site is identified before any code changes begin.

---

## Acceptance Criteria

**AC-1 — Epic 24 section added to operation catalog**
Given all eleven in-scope files (the ten files in the scope table plus `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs`)
When the developer completes the catalog
Then `docs/ai/mmrds_operation_catalog.md` gains an "Epic 24 — Infrastructure Consolidation" section documenting every distinct operation grouped by: DB lifecycle (CREATE database, DELETE database, SECURITY, PUT design document, POST `_index`), paged bulk read (`_all_docs` with cursor/skip), change-stream read (`_changes`), per-document CRUD (GET/PUT/DELETE by ID), and bulk write (`_bulk_docs`)

**AC-2 — Per-entry detail**
Given each catalog entry
When the catalog is complete
Then each entry records: file name, approximate line number, operation type, target database, URL pattern in use (A, B, or other), and which new interface from the Epic 24 "New interfaces introduced" table will own the call

**AC-3 — CDC services variant noted**
Given the `c_document_sync_all.cs` in `mmria.services/Actors/populate-cdc-instance/`
When cataloged
Then its CDC-specific characteristics are noted: cursor-based pagination, bulk-write throttling, and metadata already routed through DAL (those calls are already correct and require no change)

**AC-4 — Design doc and index routing decision recorded**
Given design document PUT and Mango index POST operations in sync/rebuild files
When cataloged
Then they are explicitly marked as DB-lifecycle operations to be routed through `IDeIdentifiedRepository` or `IReportRepository` lifecycle methods — the catalog records this routing decision (not `IDatabaseLifecycleService`)

**AC-5 — rebuild_export_queue_job.cs registration status confirmed**
Given `rebuild_export_queue_job.cs` is a legacy `IJob` implementation
When cataloged
Then the catalog records whether the job is actively registered in the Quartz scheduler in `Program.cs` or is unreachable dead code superseded by the `Rebuild_Export_Queue` Akka actor; this determines the approach in Story 24.4

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `docs/ai/mmrds_operation_catalog.md` | **UPDATE** — add "Epic 24 — Infrastructure Consolidation" section |

**Files to enumerate (all eleven):**

| File | Project | Category |
|---|---|---|
| `source-code/mmria/mmria-server/util/c_db_setup.cs` | mmria-server | DB lifecycle — startup initialization |
| `source-code/mmria/mmria-server/model/actor/quartz/Rebuild_Export_Queue.cs` | mmria-server | DB lifecycle — nightly export_queue rebuild |
| `source-code/mmria/mmria-server/model/rebuild_export_queue_job.cs` | mmria-server | DB lifecycle — legacy Quartz IJob (check if active) |
| `source-code/mmria/mmria-server/util/c_sync_document.pmss.cs` | mmria-server | Per-doc writes to de_id and report |
| `source-code/mmria/mmria-server/util/c_document_sync_all.cs` | mmria-server | Full rebuild orchestrator (non-PMSS) |
| `source-code/mmria/mmria-server/util/c_document_sync_all_legacy.cs` | mmria-server | Legacy individual-document rebuild |
| `source-code/mmria/mmria-server/util/c_document_sync_all.pmss.cs` | mmria-server | Full rebuild orchestrator (PMSS variant) |
| `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | mmria.common | Shared rebuild — barrier queries, progress tracking |
| `source-code/mmria/mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | mmria-server | Change-feed sync actor |
| `source-code/mmria/mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | mmria-server | CDC data integration actor |
| `nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | mmria.services | CDC bulk rebuild (modern, bulk-write path) |

---

## Sequencing

No dependencies. Run first. Once complete, Stories 24.2–24.5 can all proceed in parallel. Stories 24.6–24.9 depend on the interfaces established by 24.2 and 24.3.
