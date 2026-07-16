# Story 24.6 — Route `c_sync_document.pmss.cs` Through Repository Interfaces

**Epic:** 24 — Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)
**Story ID:** 24.6
**Status:** not-started
**Date added:** 2026-07-16
**Depends on:** 24.2
**Source requirements:** epics.md §Epic 24 Story 24.6; project-context.md §2.2

---

## User Story

As a developer,
I want `c_sync_document.pmss.cs` to route its `de_id` and `report` writes through repository interfaces,
So that this leaf-level per-document sync utility has no direct CouchDB calls — establishing the foundation that Stories 24.7 and 24.8 build on.

---

## Acceptance Criteria

**AC-1 — All `de_id` calls replaced with `IDeIdentifiedRepository`**
Given the de_id operations in `c_sync_document.pmss.cs` (exact lines from Story 24.1 catalog):
- GET to fetch existing document revision before overwrite or delete
- PUT de-identified document (with rev for overwrite, without rev for new)
- DELETE de-identified document
When this story is complete
Then each is replaced with the corresponding `IDeIdentifiedRepository` method:
- Revision fetch → `IDeIdentifiedRepository.GetRevisionAsync(id, dbConfig)`
- Write → `IDeIdentifiedRepository.UpsertDocumentAsync(id, doc, dbConfig)`
- Delete → `IDeIdentifiedRepository.DeleteDocumentAsync(id, rev, dbConfig)`

**AC-2 — All `report` calls replaced with `IReportRepository`**
Given the report operations in `c_sync_document.pmss.cs` — writes to four document-type variants per case:
- `freq-{caseId}` (frequency summary)
- `opioid-{caseId}` (opioid report)
- `powerbi-{caseId}` (PowerBI report)
- `dqr-{caseId}` (DQR report)
For each variant: GET revision before overwrite, PUT variant, DELETE variant (when case deleted from mmrds)
When this story is complete
Then each revision GET is replaced with `IReportRepository.GetRevisionAsync(fullId, dbConfig)` and each PUT/DELETE with `IReportRepository.UpsertDocumentAsync(fullId, doc, dbConfig)` / `IReportRepository.DeleteDocumentAsync(fullId, rev, dbConfig)` — the full document-type-prefixed ID (e.g., `"freq-{caseId}"`) is passed as the `id` parameter; the prefix is preserved in the ID, not extracted as a separate parameter

**AC-3 — Transformation logic unchanged**
Given the PMSS-specific de-identification and report-generation logic in this file (`c_de_identifier`, `c_generate_frequency_summary_report`, `c_convert_to_opioid_report_object`, `c_convert_to_dqr_detail`)
When this story is implemented
Then all transformation and generation logic remains in `c_sync_document.pmss.cs` unchanged — only the CouchDB HTTP calls at the end of each transformation path are replaced; the transformation pipeline, error silencing patterns, and revision management flow are not restructured

**AC-4 — `IDeIdentifiedRepository` and `IReportRepository` injected**
Given `c_sync_document.pmss.cs` must receive the two repositories
When this story is complete
Then `IDeIdentifiedRepository` and `IReportRepository` are injected via constructor parameters; callers that instantiate `c_sync_document.pmss.cs` — specifically `c_document_sync_all.pmss.cs` and `Process_DB_Synchronization_Set.cs` (if applicable) — are updated to pass the injected repositories through; no `new c_sync_document(...)` with manually constructed dependencies remains

**AC-5 — Build passes**
Given the build after all changes
When verified
Then `mmria-server` builds with zero errors; the de_id and report documents written per case are identical in content and structure to pre-change

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/util/c_sync_document.pmss.cs` | **UPDATE** — inject `IDeIdentifiedRepository`, `IReportRepository`; replace all direct `CouchDbHttpClient.ExecuteAsync` calls |
| `source-code/mmria/mmria-server/util/c_document_sync_all.pmss.cs` | **UPDATE** — pass repositories when constructing `c_sync_document.pmss.cs` |
| Other files that instantiate `c_sync_document.pmss.cs` | **UPDATE** — as discovered in Story 24.1 catalog; pass repositories through |

**Design notes:**
- There is also a non-PMSS `c_sync_document.cs` variant used by the non-PMSS sync paths. If Story 24.1 finds that `c_sync_document.cs` (non-PMSS) also has direct CouchDB calls for de_id and report, those calls are routed in this story following the same pattern. Confirm scope from 24.1 catalog.
- The revision fetch pattern in this file is: GET doc → extract `_rev` → use rev in subsequent PUT or DELETE. `IDeIdentifiedRepository.GetRevisionAsync` and `IReportRepository.GetRevisionAsync` encapsulate this HEAD-or-GET behavior. If the existing code does a full GET just for the rev, the DAL implementation can use a lightweight HEAD request or a minimal GET to optimize — but the interface contract does not change.
- Error silencing: `c_sync_document.pmss.cs` has extensive error silencing (catch blocks that swallow exceptions). These error-handling patterns are preserved exactly as-is — they are not a CouchDB call site and should not be touched.
- This is a `#if IS_PMSS_ENHANCED`-guarded file. Only the PMSS build path is modified. Confirm with the codebase whether the non-PMSS `c_sync_document.cs` is a separate file or if it shares logic.

---

## Sequencing

Depends on 24.2 (`IDeIdentifiedRepository` and `IReportRepository` write extension must exist). Stories 24.7 and 24.8 both depend on this story being complete (they use `c_sync_document` for per-document transformation). This is the lowest-risk implementation story — leaf-level utility with no actor hierarchy or orchestration.
