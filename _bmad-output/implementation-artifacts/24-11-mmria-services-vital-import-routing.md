# Story 24.11 — Route `mmria.services` Vital Import Calls Through `IVitalImportRepository`

**Epic:** 24 — Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)
**Story ID:** 24.11
**Status:** not-started
**Date added:** 2026-07-16
**Depends on:** Epic 23 Story 23.5 done
**Source requirements:** epics.md §Epic 24; mmrds_operation_catalog.md §vital_import Operations; project-context.md §2.2

---

## User Story

As a developer,
I want all `vital_import` database calls in `mmria.services` to route through `IVitalImportRepository`,
So that the services project is covered by the same repository contract as `mmria-server` and a SQL migration requires changing only `VitalImportDAL`.

---

## Background

Story 23.5 established `IVitalImportRepository` and routed the `mmria-server` and `mmria.common` call sites. The Story 23.1 catalog also found direct calls in two `mmria.services` files that were not addressed. This story completes the `vital_import` consolidation.

---

## Acceptance Criteria

**AC-1 — `VitalNotificationController.cs` calls replaced**
Given `mmria.services/Controllers/VitalNotificationController.cs` constructs `vital_import/_all_docs` URLs directly at approximately lines ~39 and ~69:
- Line ~39: `GET vital_import/_all_docs?include_docs=true` — enumerate all batches for the notification list endpoint
- Line ~69: `GET vital_import/_all_docs?include_docs=true` — enumerate batches before queuing actor-driven deletes
When this story is complete
Then each is replaced with `IVitalImportRepository.GetAllBatchesAsync(...)`; `IVitalImportRepository` is injected via constructor injection

**AC-2 — `PopulateCDCInstanceSupervisor.cs` call replaced**
Given `mmria.services/Actors/populate-cdc-instance/PopulateCDCInstanceSupervisor.cs` constructs a `vital_import/_all_docs` URL directly at approximately line 343:
- Line ~343: `GET vital_import/_all_docs?include_docs=true` — enumerate vital import batches during CDC populate pass
When this story is complete
Then that call is replaced with `IVitalImportRepository.GetAllBatchesAsync(...)`; `IVitalImportRepository` is injected into the actor via Akka.NET actor props factory

**AC-3 — vital_import URL exception preserved**
Given the `vital_import` database uses no prefix separator (intentional, non-tenant DB)
When this story is implemented
Then all repository calls preserve this pattern — `IVitalImportRepository` implementations use `$"{config.url}/vital_import/..."` directly; `Get_Prefix_DB_Url` is never used for this database

**AC-4 — Build passes**
Given the build after all changes
When verified
Then `mmria.services`, `mmria.common`, and `mmria-server` all build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.services/Controllers/VitalNotificationController.cs` | **UPDATE** — inject `IVitalImportRepository`; replace ~2 direct `vital_import` URL constructions |
| `mmria.services/Actors/populate-cdc-instance/PopulateCDCInstanceSupervisor.cs` | **UPDATE** — inject `IVitalImportRepository` via actor props factory; replace 1 direct `vital_import` URL construction |

**Injection pattern:**
`PopulateCDCInstanceSupervisor.cs` is an Akka actor — use the Akka.NET actor props-factory DI pattern. `VitalNotificationController.cs` uses standard MVC constructor injection.

**Design notes:**
- `IVitalImportRepository.GetAllBatchesAsync` is the appropriate method — it maps to the `GET vital_import/_all_docs` operation established in `VitalImportDAL`.
- The actor delete-queue dispatch in `VitalNotificationController.cs` line ~69 reads the batch list to determine what to delete via an actor message — only the CouchDB read is replaced; the actor dispatch logic is unchanged.
- Confirm exact line numbers against the Story 24.1 catalog before implementing.

---

## Sequencing

Depends only on Epic 23 Story 23.5 (`IVitalImportRepository` must exist). Independent of all other Epic 24 stories. Low risk.
