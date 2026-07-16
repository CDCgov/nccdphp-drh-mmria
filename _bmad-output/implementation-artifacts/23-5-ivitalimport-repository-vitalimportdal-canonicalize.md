# Story 23.5 — Canonicalize `VitalImportDAL` for `vital_import` DB and Extract `IVitalImportRepository`

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story ID:** 23.5
**Status:** done
**Date added:** 2026-07-16
**Depends on:** 23.1
**Source requirements:** epics.md §Epic 23 Story 23.5; project-context.md §2.2

---

## User Story

As a developer,
I want all `vital_import` database operations consolidated in `VitalImportDAL` behind `IVitalImportRepository`,
So that the vital import batch store can be migrated by changing only `VitalImportDAL`.

---

## Acceptance Criteria

**AC-1 — VitalImportDAL canonicalized with all operations**
Given the `vital_import` database is currently accessed in three places:
- `VitalImportDAL.cs` line ~47: `GET vital_import/_all_docs` (1 operation — already in DAL)
- `MMRIAServicesDAL.cs` lines ~118, 141, 156: `GET vital_import/_all_docs`, `PUT vital_import/{batch_id}`, `PUT vital_import/{_id}` (3 operations)
- `ije_messageController.cs` lines ~73: `GET vital_import/_all_docs` (1 operation)
When this story is complete
Then `VitalImportDAL` contains all in-scope `vital_import` CRUD operations: GET all docs, PUT batch document, PUT/DELETE individual document; `IVitalImportRepository` is defined in `mmria.common/SharedLibraries/VitalImport/` with async method signatures for every operation

**AC-2 — vital_import URL pattern preserved (no prefix separator)**
Given the `vital_import` database URL uses no prefix separator (`$"{config.url}/vital_import/..."` — intentional, non-tenant DB)
When `VitalImportDAL` methods are written or updated
Then all methods preserve this exact URL construction — no `Get_Prefix_DB_Url` is used for the `vital_import` database; this exception is documented in the catalog

**AC-3 — DI registration**
Given `IVitalImportRepository` is defined
When DI registration is updated in `mmria-server/Program.cs`
Then `IVitalImportRepository` is registered as `VitalImportDAL` in the service collection

**AC-4 — MMRIAServicesDAL routed**
Given `MMRIAServicesDAL.cs` lines ~118, 141, 156 construct `vital_import` URLs directly
When this story is complete
Then each is replaced with the corresponding `IVitalImportRepository` method; `IVitalImportRepository` is injected into `MMRIAServicesDAL` via constructor injection

**AC-5 — ije_messageController GET routed**
Given `ije_messageController.cs` line ~73 constructs `vital_import/_all_docs` directly
When this story is complete
Then that call is replaced with `IVitalImportRepository.GetAllBatchesAsync(...)`; `IVitalImportRepository` is injected into the controller via constructor injection

**AC-6 — External vitals_url calls left unchanged**
Given the external `vitals_url` POST/DELETE calls at lines ~107 and ~148 in `ije_messageController`
When this story is complete
Then those calls are not changed — they are external-service calls to the vitals notification endpoint, not CouchDB operations

**AC-7 — Build passes**
Given the build after all changes
When verified
Then all three projects build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/VitalImport/IVitalImportRepository.cs` | **CREATE** — interface |
| `mmria.common/SharedLibraries/VitalImport/DAL/VitalImportDAL.cs` | **UPDATE** — add missing PUT/DELETE operations; implement `IVitalImportRepository` |
| `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | **UPDATE** — inject `IVitalImportRepository`; replace 3 direct `vital_import` URL constructions (lines ~118, 141, 156) |
| `mmria-server/Controllers/api/ije_messageController.cs` | **UPDATE** — inject `IVitalImportRepository`; replace 1 direct `vital_import/_all_docs` call (line ~73) |
| `mmria-server/Program.cs` | **UPDATE** — add `services.AddScoped<IVitalImportRepository, VitalImportDAL>()` |

**Design notes:**
- **URL exception:** `vital_import` is a special non-tenant database. Its URL is `$"{couchDbUrl}/vital_import/{path}"` — no prefix. This is intentional and must NOT be changed to use `Get_Prefix_DB_Url`. Document this as a deliberate exception in the catalog.
- The existing `VitalImportDAL` already accesses `vital_import/_all_docs`. The new operations (PUT batch, PUT/DELETE individual) are additions to the existing DAL, not rewrites.
- `ije_messageController` also submits IJE batches via `vitals_url` (external notification service). Lines ~107 and ~148 are NOT CouchDB calls — leave them as-is.
- `MMRIAServicesDAL` line ~151 filters `vital_import` from a config key list — this is not a DB call and should not be touched.

---

## Sequencing

Depends on 23.1. Can proceed in parallel with 23.2, 23.3, 23.4, 23.6, 23.8.
