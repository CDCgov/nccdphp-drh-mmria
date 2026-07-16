# Story 24.9 — Route `Process_Central_Pull_list` and CDC `c_document_sync_all` Through Repository Interfaces

**Epic:** 24 — Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)
**Story ID:** 24.9
**Status:** not-started
**Date added:** 2026-07-16
**Depends on:** 24.2, 24.3, 24.7
**Source requirements:** epics.md §Epic 24 Story 24.9; project-context.md §2.2

---

## User Story

As a developer,
I want `Process_Central_Pull_list.cs` and the CDC populate `c_document_sync_all.cs` (in `mmria.services`) to route all their CouchDB calls through repository interfaces,
So that the CDC data integration path — the most complex infra flow — has no direct HTTP calls and is SQL-migration-ready.

---

## Acceptance Criteria

**AC-1 — Source-instance mmrds reads replaced**
Given `Process_Central_Pull_list.cs` iterates over CDC source instances and reads paged `mmrds/_all_docs?include_docs=true` from each
When this story is complete
Then each paged read is replaced with `ICaseRepository.GetCasesPagedAsync(startKey, limit, sourceDbConfig)` where `sourceDbConfig` is the `DBConfigurationDetail` for the source instance (not the local instance); the multi-instance loop over `cdc_instance_pull_list` entries is unchanged

**AC-2 — Target database lifecycle operations replaced**
Given `Process_Central_Pull_list.cs` drops and recreates the target mmrds, de_id, and report databases at the start of each CDC pull
When this story is complete
Then:
- Target mmrds lifecycle → `ICaseRepository.DropAndResetAsync(targetDbConfig)` (from Story 24.3)
- Target de_id lifecycle → `IDeIdentifiedRepository.DropAndResetAsync(targetDbConfig)`
- Target report lifecycle → `IReportRepository.DropAndResetWithSystemDocPreservationAsync(targetDbConfig)`

**AC-3 — Target design document and index operations replaced**
Given `Process_Central_Pull_list.cs` installs design documents and indexes on the target de_id and report databases after recreation
When this story is complete
Then:
- de_id design doc → `IDeIdentifiedRepository.EnsureDesignDocumentAsync(name, json, targetDbConfig)`
- report Mango indexes (`opioid`, `powerbi`) → `IReportRepository.EnsureIndexAsync(json, targetDbConfig)`
- Any report design docs → `IReportRepository.EnsureDesignDocumentAsync(name, json, targetDbConfig)`

**AC-4 — `Report_Opioid_Index_Struct` reference preserved**
Given `Process_Central_Pull_list.cs` references `Report_Opioid_Index_Struct` (and similar structs) defined in `c_document_sync_all.cs`
When Story 24.7 routes those files through repositories but does not move the struct definitions
Then `Process_Central_Pull_list.cs` continues to reference the struct at its existing location; if Story 24.7 does move the struct, the reference in this file is updated accordingly in this story

**AC-5 — CDC populate `c_document_sync_all.cs` (mmria.services) routed**
Given `c_document_sync_all.cs` in `mmria.services/Actors/populate-cdc-instance/` has direct CouchDB calls for:
- mmrds cursor-paged reads
- de_id bulk writes
- report bulk writes
- report design document and index operations
And metadata reads are already routed through the metadata DAL
When this story is complete
Then:
- mmrds paged reads → `ICaseRepository.GetCasesPagedAsync(...)`
- de_id bulk writes → `IDeIdentifiedRepository.BulkUpsertAsync(...)`
- report bulk writes → `IReportRepository.BulkUpsertAsync(...)`
- report design docs → `IReportRepository.EnsureDesignDocumentAsync(...)`
- report indexes → `IReportRepository.EnsureIndexAsync(...)`
- Metadata calls: unchanged (already correct)

**AC-6 — `c_cdc_de_identifier` evaluated**
Given `c_cdc_de_identifier` is used by `Process_Central_Pull_list.cs` for de-identification
When Story 24.1 catalog reveals its direct CouchDB calls (if any)
Then if it has direct CouchDB calls against de_id, report, or mmrds, those calls are replaced with the corresponding repository methods in this story; if it has no direct CouchDB calls, it is confirmed clean and noted in the catalog

**AC-7 — IS_PMSS_ENHANCED guard respected**
Given `Process_Central_Pull_list.cs` is guarded by `!IS_PMSS_ENHANCED`
When this story is implemented
Then only the non-PMSS code path is modified; if a PMSS variant exists, it is evaluated independently; no PMSS-specific logic is touched in this story

**AC-8 — Orchestration and CDC data flow unchanged**
Given the CDC source-instance iteration loop, de-identification delegation to `c_cdc_de_identifier`, `Synchronize_Case` actor dispatch for per-document writes to target mmrds, and CDC throttling in `c_document_sync_all.cs` (mmria.services)
When this story is implemented
Then none of this orchestration logic changes; only CouchDB URL construction and `CouchDbHttpClient.ExecuteAsync` calls are replaced; actor dispatch, de-identification pipeline, and throttling logic are not restructured

**AC-9 — CDC integration test passes before story is marked complete**
Given the sensitivity of the CDC data integration path
When this story is implemented
Then the developer runs the full CDC populate integration flow in the multi-tenant test environment before marking the story complete; a note confirming that de-identification was preserved through the refactor is added to the story's completion notes

**AC-10 — Build passes**
Given the build after all changes
When verified
Then `mmria-server`, `mmria.common`, and `mmria.services` all build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | **UPDATE** — inject `ICaseRepository`, `IDeIdentifiedRepository`, `IReportRepository`; replace all direct CouchDB calls |
| `nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | **UPDATE** — inject `ICaseRepository`, `IDeIdentifiedRepository`, `IReportRepository`; replace mmrds/de_id/report CouchDB calls; leave metadata calls unchanged |
| `source-code/mmria/mmria-server/util/c_cdc_de_identifier.cs` (if applicable) | **UPDATE** — replace any direct CouchDB calls per AC-6 evaluation |

**CDC data flow overview (for reference):**
```
Process_Central_Pull_list actor (runs on schedule)
  └─ for each source instance in cdc_instance_pull_list:
       1. Drop + recreate target mmrds, de_id, report  ← AC-2
       2. Install design docs + indexes on de_id, report  ← AC-3
       3. Page through source mmrds/_all_docs  ← AC-1
       4. Pass each doc to c_cdc_de_identifier for de-identification
       5. Dispatch Synchronize_Case actor → writes to target mmrds
  └─ c_document_sync_all.cs (mmria.services) handles the bulk de_id/report populate  ← AC-5
```

**Injection pattern:**
`Process_Central_Pull_list.cs` is an Akka actor. Use the Akka.NET actor props-factory DI pattern (see `docs/ai/MMRIA_Background_Jobs_Documentation.md`). `c_document_sync_all.cs` in mmria.services is also an actor — confirm its DI pattern from the existing services actor registration.

**Design notes:**
- This is the highest-risk story in Epic 24 due to the CDC integration's cross-tenant scope, de-identification pipeline, and multi-source reads. **Do not rush this story.** Implement incrementally: source reads first, then lifecycle, then design docs/indexes.
- `c_document_sync_all.cs` in mmria.services references `IMetadataRepository` already (confirmed in scope table). Do not touch those calls.
- `dbConfig` context: `Process_Central_Pull_list.cs` works with two different `DBConfigurationDetail` instances — the source (for reads) and the target (for writes and lifecycle). Ensure each repository call receives the correct context. Mixing source and target dbConfig is a logic error that would cause data to be written to the wrong tenant.
- Confirm how `c_document_sync_all.cs` (mmria.services) is instantiated by the CDC populate supervisor — whether it is a sub-actor or a utility class determines the injection approach.

---

## Sequencing

Depends on 24.2, 24.3, and 24.7 (24.7 must complete first to stabilize struct definitions referenced by this file). This is the final implementation story in Epic 24. Once complete, Epic 24 is done and the full SQL migration readiness gate is achieved.
