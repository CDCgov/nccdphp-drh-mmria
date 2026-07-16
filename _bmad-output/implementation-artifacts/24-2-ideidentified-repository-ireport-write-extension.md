# Story 24.2 — `IDeIdentifiedRepository` and Extend `IReportRepository` for Sync Writes and Lifecycle

**Epic:** 24 — Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)
**Story ID:** 24.2
**Status:** not-started
**Date added:** 2026-07-16
**Depends on:** 24.1
**Source requirements:** epics.md §Epic 24 Story 24.2; project-context.md §2.2

---

## User Story

As a developer,
I want repository interfaces covering all de_id and report database operations — including write, bulk-write, and DB-lifecycle — that sync and rebuild files require,
So that Stories 24.6–24.9 can route every call through a typed interface instead of direct HTTP calls.

---

## Acceptance Criteria

**AC-1 — `DeIdentified` SharedLibraries feature created**
Given no `DeIdentified` SharedLibraries feature exists
When this story creates one
Then the following structure exists:
```
mmria.common/SharedLibraries/DeIdentified/
  IDeIdentifiedRepository.cs
  DAL/
    DeIdentifiedDAL.cs
```

**AC-2 — `DeIdentifiedDAL` contains all de_id operations**
Given the de_id operations identified in Story 24.1
When `DeIdentifiedDAL` is created
Then it contains async methods for:
- `GetRevisionAsync(string id, DBConfigurationDetail dbConfig)` → `string? rev`
- `UpsertDocumentAsync(string id, JObject doc, DBConfigurationDetail dbConfig)` → `document_put_response`
- `DeleteDocumentAsync(string id, string rev, DBConfigurationDetail dbConfig)` → `document_put_response`
- `BulkUpsertAsync(IEnumerable<JObject> docs, DBConfigurationDetail dbConfig)` → `IEnumerable<document_put_response>`
- `DropAndResetAsync(DBConfigurationDetail dbConfig)` — drops the de_id database and recreates it empty; SQL equivalent: `TRUNCATE TABLE de_id`
- `EnsureDesignDocumentAsync(string designName, string designDocJson, DBConfigurationDetail dbConfig)` — PUT `de_id/_design/{designName}`
- `EnsureIndexAsync(string indexJson, DBConfigurationDetail dbConfig)` — POST `de_id/_index`
- `WaitForIndexReadyAsync(DBConfigurationDetail dbConfig)` — barrier query: `GET de_id/_design/sortable/_view/by_date_created?limit=1&update=true`; used by rebuild orchestrators to confirm index availability before marking rebuild complete
- `GetRevisionBulkAsync(IEnumerable<string> ids, DBConfigurationDetail dbConfig)` → `IDictionary<string, string>` (id → rev) — executes `POST de_id/_all_docs?include_docs=false` with a keys body; used by `c_document_sync_all.cs` to look up existing revisions before a bulk write in order to set the `_rev` field correctly and avoid 409 conflicts

All CRUD and bulk methods use `dbConfig.Get_Prefix_DB_Url($"de_id/...")` (Pattern B). `DropAndResetAsync` uses `dbConfig.Get_Prefix_DB_Url("de_id")` for the database-level DELETE and PUT.

**AC-3 — DI registration for `IDeIdentifiedRepository`**
Given `IDeIdentifiedRepository` is defined
When DI registration is updated in `mmria-server/Program.cs`
Then `IDeIdentifiedRepository` is registered as `DeIdentifiedDAL` in the service collection; `mmria.services` already references `mmria.common` so no new project reference is needed

**AC-4 — `IReportRepository` extended with write and lifecycle methods**
Given `IReportRepository` from Story 23.6 is currently read-only (4 read methods)
When this story extends it
Then `IReportRepository` gains these additional methods, implemented in `ReportDAL`:
- `GetRevisionAsync(string id, DBConfigurationDetail dbConfig)` → `string? rev`
- `UpsertDocumentAsync(string id, JObject doc, DBConfigurationDetail dbConfig)` → `document_put_response`
- `DeleteDocumentAsync(string id, string rev, DBConfigurationDetail dbConfig)` → `document_put_response`
- `BulkUpsertAsync(IEnumerable<JObject> docs, DBConfigurationDetail dbConfig)` → `IEnumerable<document_put_response>`
- `DropAndResetWithSystemDocPreservationAsync(DBConfigurationDetail dbConfig)` — drops and recreates the report database while preserving system/config documents; SQL equivalent: targeted `DELETE FROM report_documents WHERE type NOT IN ('system', 'config')`
- `EnsureDesignDocumentAsync(string designName, string designDocJson, DBConfigurationDetail dbConfig)`
- `EnsureIndexAsync(string indexJson, DBConfigurationDetail dbConfig)`
- `WaitForIndexReadyAsync(DBConfigurationDetail dbConfig)` — barrier query: `POST report/_find` with a minimal selector to confirm index availability
- `GetRevisionBulkAsync(IEnumerable<string> ids, DBConfigurationDetail dbConfig)` → `IDictionary<string, string>` (id → rev) — executes `POST report/_all_docs?include_docs=false` with a keys body; used by `c_document_sync_all.cs` to look up existing report document revisions before bulk writes

**AC-5 — Catalog updated to reflect write coverage**
Given the boundary decision in Story 23.6 declared `report` write/rebuild operations as "infrastructure out-of-scope"
When this story adds write methods to `IReportRepository`
Then the `report` Boundary Decisions section in `docs/ai/mmrds_operation_catalog.md` is updated: the prior "out-of-scope" declaration is superseded; write and lifecycle operations are now covered by `IReportRepository`

**AC-6 — No existing callers changed**
Given no callers are changed in this story
When the build runs after this story
Then all three projects build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/DeIdentified/IDeIdentifiedRepository.cs` | **CREATE** — interface with 8 methods |
| `mmria.common/SharedLibraries/DeIdentified/DAL/DeIdentifiedDAL.cs` | **CREATE** — CouchDB implementation using Pattern B throughout |
| `mmria.common/SharedLibraries/Report/IReportRepository.cs` | **UPDATE** — add 8 write/lifecycle method signatures |
| `mmria.common/SharedLibraries/Report/DAL/ReportDAL.cs` | **UPDATE** — implement 8 new methods |
| `mmria-server/Program.cs` | **UPDATE** — add `services.AddScoped<IDeIdentifiedRepository, DeIdentifiedDAL>()` |
| `docs/ai/mmrds_operation_catalog.md` | **UPDATE** — supersede Story 23.6 boundary decision for `report` writes |

**Interface method signatures for `IDeIdentifiedRepository`:**
```csharp
Task<string?> GetRevisionAsync(string id, DBConfigurationDetail dbConfig);
Task<document_put_response> UpsertDocumentAsync(string id, JObject doc, DBConfigurationDetail dbConfig);
Task<document_put_response> DeleteDocumentAsync(string id, string rev, DBConfigurationDetail dbConfig);
Task<IEnumerable<document_put_response>> BulkUpsertAsync(IEnumerable<JObject> docs, DBConfigurationDetail dbConfig);
Task DropAndResetAsync(DBConfigurationDetail dbConfig);
Task EnsureDesignDocumentAsync(string designName, string designDocJson, DBConfigurationDetail dbConfig);
Task EnsureIndexAsync(string indexJson, DBConfigurationDetail dbConfig);
Task WaitForIndexReadyAsync(DBConfigurationDetail dbConfig);
Task<IDictionary<string, string>> GetRevisionBulkAsync(IEnumerable<string> ids, DBConfigurationDetail dbConfig);
```

**Design notes:**
- `DropAndResetWithSystemDocPreservationAsync` on `IReportRepository` is the more complex variant used by `Process_Central_Pull_list` and `c_document_sync_all` where certain system docs survive a rebuild. The implementation must pre-fetch those documents, drop the database, recreate it, and re-insert the preserved docs. Confirm exact documents to preserve from Story 24.1 catalog.
- `WaitForIndexReadyAsync` is needed only by the common-library legacy variant (`c_document_sync_all_legacy.cs` in mmria.common). The server sync-all variants use a simpler poll. Implement and confirm the exact barrier query URL per Story 24.1.
- `IDeIdentifiedRepository` is injected in `mmria.services` via existing DI wiring — confirm that `mmria.services` Program.cs / actor registration allows the DAL to resolve.

---

## Sequencing

Depends on 24.1. Can proceed in parallel with 24.3, 24.4, 24.5 once 24.1 is complete. Stories 24.6, 24.7, 24.8, 24.9 all depend on this story.
