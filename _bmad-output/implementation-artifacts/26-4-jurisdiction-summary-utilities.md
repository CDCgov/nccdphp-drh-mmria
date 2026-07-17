# Story 26.4 — Jurisdiction, Summary, and Remaining Utility Leakers

**Epic:** 26 — Controller API Direct-Call Remediation
**Story ID:** 26.4
**Status:** ready-for-dev
**Date added:** 2026-07-17
**Depends on:** Epic 19 story 19.2 (IJurisdictionRepository), Epic 20 (IMetadataRepository), Epic 17 story 17.2 (ICaseRepository)
**Source requirements:** epics.md §Epic 26 Story 26.4; project-context.md §2.2

---

## User Story

As a developer,
I want the remaining utility files that access jurisdiction, case view, or metadata data directly to route through existing repository interfaces,
So that these non-controller call sites complete the controller migration wave with zero new interfaces.

---

## Acceptance Criteria

**AC-1 — `JurisdictionSummary.cs` call replaced**
Given `mmria-server/util/JurisdictionSummary.cs` at approximately line 342 calls `_couchDbHttpClient.ExecuteAsync("GET", "{prefix}mmrds/{path}", ...)` to read jurisdiction-related summary data
When this story is complete
Then that call is replaced with the appropriate `IJurisdictionRepository` method that returns the jurisdiction summary data; `IJurisdictionRepository` is injected via the `JurisdictionSummary` constructor

**AC-2 — `mmria.services/Utilities/authorization.cs` calls replaced**
Given `authorization.cs` in mmria.services at approximately line 61 (and 63+) calls `_couchDbHttpClient.ExecuteAsync` to query a jurisdiction view (`/{prefix}jurisdiction/_design/...`) to evaluate user authorization
When this story is complete
Then each call is replaced with the appropriate `IJurisdictionRepository` view-query method; `IJurisdictionRepository` is injected into the `authorization` class via its constructor; the authorization evaluation logic (read view, check result, return bool) is unchanged

**AC-3 — `CaseViewSearch.pmss.cs` call replaced**
Given `mmria-server/util/CaseViewSearch.pmss.cs` (PMSS-guarded, `#if IS_PMSS_ENHANCED`) at approximately line 1998 calls `_couchDbHttpClient.ExecuteAsync("GET", "{prefix}mmrds/...", ...)` to execute a case view query
When this story is complete
Then that call is replaced with the appropriate `ICaseRepository` view-query method; the `#if IS_PMSS_ENHANCED` guard is preserved unchanged; `ICaseRepository` is injected via the constructor

**AC-4 — `export_all_generate_name_map.cs` metadata call replaced**
Given `mmria-server/util/exporter/export_all_generate_name_map.cs` at approximately line 53 calls `_couchDbHttpClient.ExecuteAsync("GET", "{url}/metadata/...", ...)` to read a metadata document for building the export name map
When this story is complete
Then that call is replaced with the appropriate `IMetadataRepository` method; `IMetadataRepository` is injected via the constructor

**AC-5 — No new interfaces created**
Given all required repositories already exist from Epics 17–24
When this story is implemented
Then no new interface files or DAL files are created; if a view-query method is missing from an existing interface, it is added to the interface and implemented in the existing DAL before replacing the call site

**AC-6 — Build passes with zero errors**
Given the changes above
When the build runs
Then `mmria-server` and `mmria.services` both build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/util/JurisdictionSummary.cs` | **UPDATE** — inject `IJurisdictionRepository`; replace direct GET with repo method |
| `nccdphp-drh-mmria-services/mmria.services/Utilities/authorization.cs` | **UPDATE** — inject `IJurisdictionRepository`; replace jurisdiction view query |
| `source-code/mmria/mmria-server/util/CaseViewSearch.pmss.cs` | **UPDATE** — inject `ICaseRepository`; replace mmrds view query; PMSS guard preserved |
| `source-code/mmria/mmria-server/util/exporter/export_all_generate_name_map.cs` | **UPDATE** — inject `IMetadataRepository`; replace metadata GET |

**`IJurisdictionRepository` relevant methods:**
- `GetUserRoleJurisdictionSortableViewAsync(requestUrl, dbConfig)` — for view-based queries
- `GetUserRoleJurisdictionSortableViewByParamsAsync(...)` — parameterized view query
- `GetAllUserRoleJurisdictionsAsync(dbConfig)` — full list
- `GetJurisdictionTreeAsync(dbConfig)` — jurisdiction tree document

Confirm which method matches the URL pattern used in `JurisdictionSummary.cs` and `authorization.cs` before implementing — check the exact view name in the URL construction.

**`ICaseRepository` view method note for `CaseViewSearch.pmss.cs`:**
The PMSS case view search likely queries a PMSS-specific case view. Check if the view query (`GET {prefix}mmrds/_design/.../_view/...`) is already covered by an existing `ICaseRepository` method. If not, add `GetCasesByCustomViewAsync(string viewUrl, DBConfigurationDetail dbConfig)` → `string` (raw JSON) to the interface and implement in `CaseDAL`.

**Null-fallback pattern:** Do NOT use the optional-parameter + null-guard pattern. Follow the same required-parameter pattern established in Epics 17–24 for all four files. Pass a real `IMetadataRepository` instance via constructor. No null-guard or fallback branch.
