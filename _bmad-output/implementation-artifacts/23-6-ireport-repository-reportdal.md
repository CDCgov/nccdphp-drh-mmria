# Story 23.6 — `IReportRepository` + `ReportDAL` (Application Read Interface)

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story ID:** 23.6
**Status:** done
**Date added:** 2026-07-16
**Depends on:** 23.1
**Source requirements:** epics.md §Epic 23 Story 23.6; project-context.md §2.2

---

## User Story

As a developer,
I want a single `IReportRepository` interface over all application-layer `report` database read operations,
So that report query controllers and managers depend on the interface and a SQL migration requires changing only `ReportDAL`.

---

## Acceptance Criteria

**AC-1 — `Report` SharedLibraries feature created**
Given no `Report` SharedLibraries feature exists
When this story creates one
Then the following structure exists:
```
mmria.common/SharedLibraries/Report/
  IReportRepository.cs
  DAL/
    ReportDAL.cs
```

**AC-2 — ReportDAL contains all in-scope application read operations**
Given the in-scope application read operations from the catalog:
- `GET report/_all_docs?include_docs=true` — used by `AggregateReportManager`
- `GET report/_design/interactive_aggregate_report/_view/indicator_id?...` — used by `InteractiveReportManager`
- `GET report/_design/data_summary_view_report/_view/year_of_death?skip=N&limit=N` — used by `data_summary_viewController`
- `POST report/_find` — used by `dqrReportController`, `overdose_measureController`, `powerbi_measureController`
When `ReportDAL` is created
Then it contains async methods for each: `GetAllReportDocumentsAsync(DBConfigurationDetail dbConfig)`, `GetIndicatorByIdAsync(string indicatorId, DBConfigurationDetail dbConfig)`, `GetDataSummaryViewAsync(int skip, int take, DBConfigurationDetail dbConfig)`, `FindReportDocumentsAsync(string selectorJson, DBConfigurationDetail dbConfig)` — all using Pattern B via `dbConfig.Get_Prefix_DB_Url($"report/...")`

**AC-3 — Write/rebuild boundary decision recorded**
Given the sync/rebuild actors in `mmria-server/util/` and `mmria.common/SharedLibraries/MMRIARebuild/` write to and manage the `report` database
When they are evaluated
Then a boundary decision is recorded in `docs/ai/mmrds_operation_catalog.md` under the `report` Boundary Decisions section: write/rebuild operations (DROP DB, CREATE DB, bulk PUT report documents, `_index` creation, design document PUT) are declared **infrastructure out-of-scope**; `IReportRepository` covers read operations only

**AC-4 — DI registration**
Given `IReportRepository` is defined
When DI registration is updated in `mmria-server/Program.cs`
Then `IReportRepository` is registered as `ReportDAL` in the service collection

**AC-5 — No callers changed yet**
Given no callers are changed in this story
When the build runs
Then all three projects build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/Report/IReportRepository.cs` | **CREATE** — interface with 4 read methods |
| `mmria.common/SharedLibraries/Report/DAL/ReportDAL.cs` | **CREATE** — implementation using Pattern B |
| `mmria-server/Program.cs` | **UPDATE** — add `services.AddScoped<IReportRepository, ReportDAL>()` |
| `docs/ai/mmrds_operation_catalog.md` | **UPDATE** — add `report` Boundary Decisions section |

---

## Dev Agent Record

**Implemented by:** Amelia (bmad-agent-dev)
**Date:** 2026-07-16
**Status:** done

### Files Created
- `mmria.common/SharedLibraries/Report/IReportRepository.cs` — interface with 4 read methods
- `mmria.common/SharedLibraries/Report/DAL/ReportDAL.cs` — Pattern B implementation

### Files Updated
- `mmria-server/Program.cs` — added `AddScoped<IReportRepository, ReportDAL>()` after `ISessionRepository` registration
- `docs/ai/mmrds_operation_catalog.md` — added `report` Boundary Decisions section

### Build Result
`mmria-server` build: **0 errors, 0 warnings**

### AC Verification
- AC-1 ✅ `SharedLibraries/Report/` directory with `IReportRepository.cs` and `DAL/ReportDAL.cs`
- AC-2 ✅ All 4 read methods implemented using Pattern B (`dbConfig.Get_Prefix_DB_Url`)
- AC-3 ✅ Boundary Decisions section added to `mmrds_operation_catalog.md`
- AC-4 ✅ DI registration added to `Program.cs`
- AC-5 ✅ No callers changed; build passes with 0 errors

**Interface method signatures:**
```csharp
Task<string> GetAllReportDocumentsAsync(DBConfigurationDetail dbConfig);
Task<string> GetIndicatorByIdAsync(string indicatorId, DBConfigurationDetail dbConfig);
Task<string> GetDataSummaryViewAsync(int skip, int take, DBConfigurationDetail dbConfig);
Task<string> FindReportDocumentsAsync(string selectorJson, DBConfigurationDetail dbConfig);
```

**Design notes:**
- `IReportRepository` is intentionally read-only. The `report` database is written to exclusively by background sync/rebuild infrastructure (`c_document_sync_all`, `c_sync_document`, `Process_Central_Pull_list`, etc.). These are infrastructure concerns that will be addressed as part of SQL migration implementation, not as application interface changes.
- `FindReportDocumentsAsync` accepts a raw `selectorJson` string. The three controllers that use it each pass a different Mango selector — keeping it generic avoids creating three type-specific methods for what is effectively the same `_find` operation.
- `GetDataSummaryViewAsync` is also a Wave 8 migration target (`data_summary_viewController` → `DataSummary` SharedLibrary). Story 23.7 only routes the URL construction; the controller restructuring is deferred.
- This story creates the DAL and interface only. Callers are routed in Story 23.7.

---

## Sequencing

Depends on 23.1. Can proceed in parallel with 23.2, 23.3, 23.4, 23.5, 23.8. Story 23.7 depends on this story.
