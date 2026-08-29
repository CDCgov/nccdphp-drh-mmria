---
baseline_commit: 25e950c7e34b25a646fd004cff9128411354506d
---

# Story 23.1 — Remaining Database Gap Scan

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story ID:** 23.1
**Status:** done
**Date added:** 2026-07-16
**Depends on:** none — discovery only
**Source requirements:** epics.md §Epic 23 Story 23.1; project-context.md §2.2

---

## User Story

As a developer,
I want a definitive catalog of every operation against the six remaining databases (`session`, `offline_cases`, `export_queue`, `vital_import`, `report`, `logging`) across all three projects,
So that Stories 23.2–23.8 have an agreed-upon, complete operation set and no call sites are missed before any code changes begin.

---

## Acceptance Criteria

**AC-1 — Six database sections added to operation catalog**
Given all `.cs` files in `mmria-server`, `mmria.common`, and `mmria.services`
When the developer completes the catalog
Then `docs/ai/mmrds_operation_catalog.md` gains a section for each of the six databases listing every distinct operation grouped by: document CRUD (GET/PUT/DELETE by ID), view queries, Mango `_find` queries, list reads (`_all_docs`), and bulk/admin operations

**AC-2 — Per-entry detail**
Given each catalog entry
When the catalog is complete
Then each entry records: operation name, calling file(s), URL pattern in use (A, B, or other), response type expected, and layer classification (DAL ✓, Manager ✗, Controller ✗, Actor ✗)

**AC-3 — Infra operations marked out of scope**
Given infra operations (`c_db_setup`, rebuild actors, sync actors, `Rebuild_Export_Queue`)
When encountered
Then they are listed but marked **out of scope** — DB lifecycle and bulk-write infrastructure do not belong behind application repository interfaces

**AC-4 — `vital_import` URL pattern documented**
Given the `vital_import` database
When cataloged
Then the entry notes that URLs use `config.url/vital_import/...` with no prefix separator, and this is intentional (non-tenant special config DB); all callers preserve this pattern

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `docs/ai/mmrds_operation_catalog.md` | **UPDATE** — add sections for `session`, `offline_cases`, `export_queue`, `vital_import`, `report`, `logging` |

**Known call sites to enumerate (already located during epic analysis):**

| Database | Files with direct calls |
|---|---|
| `session` | `SessionDAL`, `SessionManager`, `AccountController`, `AccountController.OIDC`, `Post_Session_Actor`, `Record_Session_Event`, `SessionSummary`, `AccountDAL` |
| `offline_cases` | `OfflineCaseDAL`, `loggerController` |
| `export_queue` | `ExportQueueDAL`, `core_element_exporter`, `Rebuild_Export_Queue`, `rebuild_export_queue_job` |
| `vital_import` | `VitalImportDAL`, `MMRIAServicesDAL`, `ije_messageController` |
| `report` | `AggregateReportManager`, `InteractiveReportManager`, `data_summary_viewController`, `dqrReportController`, `overdose_measureController`, `powerbi_measureController`, `c_document_sync_all*`, `c_sync_document*`, `Process_Central_Pull_list`, `Process_DB_Synchronization_Set` |
| `logging` | `loggerController`, `c_db_setup` |

**Infra out-of-scope:** `c_db_setup.cs`, `Rebuild_Export_Queue.cs`, `rebuild_export_queue_job.cs`, `Process_Central_Pull_list.cs`, `Process_DB_Synchronization_Set.cs`, `c_document_sync_all*.cs`, `c_document_sync_all_legacy.cs`, `c_sync_document.pmss.cs`

---

## Sequencing

No dependencies. Run first. Once complete, Stories 23.2–23.8 can all proceed in parallel.
