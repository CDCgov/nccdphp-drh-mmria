# Story 23.7 — Route Report Read Calls Through `IReportRepository`

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story ID:** 23.7
**Status:** done
**Date added:** 2026-07-16
**Depends on:** 23.6
**Source requirements:** epics.md §Epic 23 Story 23.7; project-context.md §2.2

---

## User Story

As a developer,
I want all application-layer files that directly construct `report` database read URLs to delegate to `IReportRepository`,
So that no manager or controller constructs a `report/` URL directly.

---

## Acceptance Criteria

**AC-1 — AggregateReportManager routed**
Given `AggregateReportManager.cs` line ~35 uses `dbConfig.Get_Prefix_DB_Url("report/_all_docs?include_docs=true")` directly in the Manager layer
When this story is complete
Then that call is replaced with `IReportRepository.GetAllReportDocumentsAsync(dbConfig)`; `IReportRepository` is injected into `AggregateReportManager` via constructor injection

**AC-2 — InteractiveReportManager routed**
Given `InteractiveReportManager.cs` line ~30 constructs `report/_design/interactive_aggregate_report/_view/indicator_id?...` directly using Pattern A
When this story is complete
Then that call is replaced with `IReportRepository.GetIndicatorByIdAsync(indicatorId, dbConfig)`; `IReportRepository` is injected into `InteractiveReportManager` via constructor injection

**AC-3 — Report controllers routed**
Given the following controllers with direct `report` URL construction:
- `data_summary_viewController.cs` — 1 hit (view GET — Wave 8 planned migration target)
- `dqrReportController.cs` — 1 hit (`_find` POST)
- `overdose_measureController.cs` — 1 hit (`_find` POST)
- `powerbi_measureController.cs` — 1 hit (`_find` POST)
When this story is complete
Then each is replaced with the corresponding `IReportRepository` method; `IReportRepository` is injected into each controller via constructor injection; no controller constructs a `report/` URL

**AC-4 — Wave 8 extraction deferred**
Given `data_summary_viewController` is also a Wave 8 SharedLibraries migration target
When this story touches it
Then only the URL construction is replaced — the Wave 8 `DataSummary` feature extraction is deferred; this story does not restructure the controller's business logic

**AC-5 — Build passes**
Given the build after all changes
When verified
Then all three projects build with zero errors and no route, action signature, or response shape changes are made

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/AggregateReport/Manager/AggregateReportManager.cs` | **UPDATE** — inject `IReportRepository`; replace 1 direct Pattern B `report/_all_docs` call |
| `mmria.common/SharedLibraries/InteractiveReport/Manager/InteractiveReportManager.cs` | **UPDATE** — inject `IReportRepository`; replace 1 direct Pattern A view query |
| `mmria-server/Controllers/api/data_summary_viewController.cs` | **UPDATE** — inject `IReportRepository`; replace 1 direct Pattern A view GET |
| `mmria-server/Controllers/api/dqrReportController.cs` | **UPDATE** — inject `IReportRepository`; replace 1 direct Pattern A `_find` POST |
| `mmria-server/Controllers/api/overdose_measureController.cs` | **UPDATE** — inject `IReportRepository`; replace 1 direct Pattern A `_find` POST |
| `mmria-server/Controllers/api/powerbi_measureController.cs` | **UPDATE** — inject `IReportRepository`; replace 1 direct Pattern A `_find` POST |

**Design notes:**
- `AggregateReportManager` already uses Pattern B — only the delegation wiring changes (add injection, call the method).
- `InteractiveReportManager` uses Pattern A — the URL construction moves entirely into `ReportDAL`.
- All 4 controllers use Pattern A and construct URLs using local variables (`config_couchdb_url`, `config_db_prefix`) rather than a `DBConfigurationDetail` object. The `dbConfig` object for the `IReportRepository` call should be resolved from the tenant runtime at the same point the controller currently resolves its URL variables.
- The Mango selector JSON for `dqrReportController`, `overdose_measureController`, and `powerbi_measureController` stays in the controller — only the URL construction and `ExecuteAsync` call move to the DAL via `FindReportDocumentsAsync(selectorJson, dbConfig)`.

---

## Sequencing

Depends on 23.6. All 6 file changes in this story are independent of each other and can be done in any order within the story.

---

## Dev Agent Record

**Completed:** 2026-07-16
**Agent:** Amelia (bmad-agent-dev)

### Changes Made
| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/AggregateReport/Manager/AggregateReportManager.cs` | Injected `IReportRepository`; replaced `ExecuteForJsonDocumentAsync` pattern-B call with `_reportRepository.GetAllReportDocumentsAsync(dbConfig)` + `JsonDocument.Parse()`; removed unused `CouchDbHttpClient` dependency |
| `mmria.common/SharedLibraries/InteractiveReport/Manager/InteractiveReportManager.cs` | Injected `IReportRepository`; replaced pattern-A view URL + `ExecuteAsync` with `_reportRepository.GetIndicatorByIdAsync(indicator_id, db_config)`; removed unused `CouchDbHttpClient` and 4 local URL vars |
| `mmria-server/Controllers/api/data_summary_viewController.cs` | Added `IReportRepository` constructor param; replaced pattern-A view URL + `ExecuteAsync` with `_reportRepository.GetDataSummaryViewAsync(skip_number, take, db_config)`; kept `_couchDbHttpClient` (still used for authorization) |
| `mmria-server/Controllers/api/dqrReportController.cs` | Replaced `CouchDbHttpClient` with `IReportRepository`; replaced `report/_find` URL + `ExecuteAsync` with `_reportRepository.FindReportDocumentsAsync(selector_struc_string, db_config)` |
| `mmria-server/Controllers/api/overdose_measureController.cs` | Replaced `CouchDbHttpClient` with `IReportRepository`; replaced `report/_find` URL + `ExecuteAsync` with `_reportRepository.FindReportDocumentsAsync(selector_struc_string, db_config)` |
| `mmria-server/Controllers/api/powerbi_measureController.cs` | Replaced `CouchDbHttpClient` with `IReportRepository`; replaced `report/_find` URL + `ExecuteAsync` with `_reportRepository.FindReportDocumentsAsync(selector_struc_string, db_config)` |

### Build Result
`dotnet build mmria-server.csproj` — **0 errors**, 138 warnings (all pre-existing)
