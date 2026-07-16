# Story 24.7 — Route `c_document_sync_all` Variants Through Repository Interfaces

**Epic:** 24 — Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)
**Story ID:** 24.7
**Status:** not-started
**Date added:** 2026-07-16
**Depends on:** 24.2, 24.3, 24.6
**Source requirements:** epics.md §Epic 24 Story 24.7; project-context.md §2.2

---

## User Story

As a developer,
I want all four `c_document_sync_all` variants to route their mmrds reads, de_id writes, report writes, and DB-lifecycle operations through repository interfaces,
So that the full-database rebuild orchestration has no direct CouchDB calls.

---

## Acceptance Criteria

**AC-1 — Four files in scope**
Given the four rebuild orchestrator variants:
1. `mmria-server/util/c_document_sync_all.cs` (non-PMSS, bulk-write path)
2. `mmria-server/util/c_document_sync_all_legacy.cs` (non-PMSS, individual-write path)
3. `mmria-server/util/c_document_sync_all.pmss.cs` (PMSS variant)
4. `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` (shared library, barrier-query path)
When this story is complete
Then every direct `CouchDbHttpClient.ExecuteAsync` call in each file is replaced with the corresponding interface method; no file constructs a CouchDB URL directly after this story

**AC-2 — mmrds paged bulk reads replaced**
Given all four files use `mmrds/_all_docs` for paged bulk reads (cursor or skip-based)
When this story is complete
Then each paged read is replaced with `ICaseRepository.GetCasesPagedAsync(startKey, limit, dbConfig)`; the cursor-advance loop logic stays in the orchestrator — the repository returns one page and the orchestrator advances the cursor and calls again; no cursor/pagination logic moves into `CaseDAL`

**AC-3 — de_id operations replaced**
Given the de_id operations across the four files:
- Drop and recreate de_id (full rebuild start) → `IDeIdentifiedRepository.DropAndResetAsync(dbConfig)`
- PUT design document on de_id → `IDeIdentifiedRepository.EnsureDesignDocumentAsync(name, json, dbConfig)`
- Bulk write de-identified documents → `IDeIdentifiedRepository.BulkUpsertAsync(docs, dbConfig)`
- Individual per-document writes (legacy variant) → `IDeIdentifiedRepository.UpsertDocumentAsync(id, doc, dbConfig)`
When this story is complete
Then each de_id call is replaced with the corresponding `IDeIdentifiedRepository` method

**AC-4 — report operations replaced**
Given the report operations across the four files:
- Drop and recreate report → `IReportRepository.DropAndResetWithSystemDocPreservationAsync(dbConfig)`
- PUT design documents (`interactive_aggregate_report`, `data_summary_view_report`, `powerbi-report-index`, etc.) → `IReportRepository.EnsureDesignDocumentAsync(name, json, dbConfig)`
- POST Mango indexes → `IReportRepository.EnsureIndexAsync(json, dbConfig)`
- Bulk write report documents → `IReportRepository.BulkUpsertAsync(docs, dbConfig)`
When this story is complete
Then each report call is replaced with the corresponding `IReportRepository` method

**AC-5 — Barrier queries replaced (common library legacy variant)**
Given `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` uses barrier queries to wait for index readiness:
- `GET de_id/_design/sortable/_view/by_date_created?limit=1&update=true`
- `POST report/_find` (minimal selector to confirm index availability)
When this story is complete
Then each is replaced with `IDeIdentifiedRepository.WaitForIndexReadyAsync(dbConfig)` and `IReportRepository.WaitForIndexReadyAsync(dbConfig)` respectively; the retry loop and delay logic around the barrier queries stay in the orchestrator — the DAL just executes the query

**AC-6 — Orchestration and progress tracking unchanged**
Given the rebuild orchestration, progress callbacks, `db_rebuild` progress-document writes, `TenantRebuildLease` multi-tenant locking, retry logic, and startup-checkpoint handling in the files
When this story is implemented
Then none of this logic changes; if Story 24.1 identifies `db_rebuild` progress-document writes, they are confirmed as already-routed through the appropriate DAL or are explicitly noted as requiring a follow-on story; the orchestration control flow is not restructured

**AC-7 — PMSS variant uses same interfaces**
Given `c_document_sync_all.pmss.cs` is guarded by `#if IS_PMSS_ENHANCED`
When this story is implemented
Then the PMSS code path uses the same `IDeIdentifiedRepository` and `IReportRepository` interfaces as the non-PMSS paths; no PMSS-specific divergence is introduced in the interface calls

**AC-8 — `c_sync_document` dependency unchanged**
Given `c_document_sync_all.cs` calls `c_sync_document.build_documents_async()` for per-document transformation
When this story is complete
Then the calls to `c_sync_document` remain unchanged — `c_sync_document` is a transformation utility; only the wrapping CouchDB calls in the orchestrators themselves are replaced

**AC-9 — Build passes**
Given the build after all changes
When verified
Then `mmria-server`, `mmria.common`, and `mmria.services` all build with zero errors; full-database rebuild behavior is identical to pre-change

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/util/c_document_sync_all.cs` | **UPDATE** — inject `ICaseRepository`, `IDeIdentifiedRepository`, `IReportRepository`; replace all direct CouchDB calls |
| `source-code/mmria/mmria-server/util/c_document_sync_all_legacy.cs` | **UPDATE** — same repositories; replace individual writes and lifecycle calls |
| `source-code/mmria/mmria-server/util/c_document_sync_all.pmss.cs` | **UPDATE** — same repositories; PMSS-guarded file |
| `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | **UPDATE** — inject `ICaseRepository`, `IDeIdentifiedRepository`, `IReportRepository`; replace all CouchDB calls including barrier queries |
| Callers that instantiate the above classes directly | **UPDATE** — pass repositories through; identify callers from Story 24.1 catalog (e.g., `MMRIARebuildManager`, startup actors) |

**Design notes:**
- The four files have different architectures: the server variants are typically called via constructor injection or actor props; the common library variant is instantiated by `MMRIARebuildManager`. Confirm instantiation points from Story 24.1 catalog before implementing.
- `Report_Opioid_Index_Struct` and other index-definition structs are currently defined in `c_document_sync_all.cs` and referenced by `Process_Central_Pull_list.cs` (Story 24.9). Do NOT move these structs in this story — keep them in place to avoid breaking 24.9's reference before 24.9 is implemented.
- The common library variant (`MMRIARebuild/Manager/c_document_sync_all_legacy.cs`) is the most complex due to the barrier queries and progress tracking. Implement last within this story.
- `IMetadataRepository` is already in use in the common library variant (confirmed in Epic 24 scope table — metadata goes via DAL already). Confirm which metadata calls are already routed and which (if any) are still direct — route any remaining direct metadata calls through `IMetadataRepository` in this story.

---

## Sequencing

Depends on 24.2 (`IDeIdentifiedRepository` + `IReportRepository` writes), 24.3 (`ICaseRepository.GetCasesPagedAsync`), and 24.6 (`c_sync_document.pmss.cs` must use repositories before the orchestrator that calls it is updated). Story 24.9 depends on this story. Story 24.8 can proceed in parallel once 24.2, 24.3, and 24.6 are complete.
