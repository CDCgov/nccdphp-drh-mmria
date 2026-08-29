# Story 28.3 — mmria-server `core_element_exporter.cs` Remaining Calls

**Epic:** 28 — mmria-server Non-DAL Remnants
**Story ID:** 28.3
**Status:** done
**Date added:** 2026-07-17
**Depends on:** Epic 17 story 17.2 (ICaseRepository), Epic 20 story 20.2 (IMetadataRepository)
**Source requirements:** epics.md §Epic 28 Story 28.3; project-context.md §2.2

---

## User Story

As a developer,
I want the mmria-server copy of `core_element_exporter.cs` to read metadata and case data through `IMetadataRepository` and `ICaseRepository` instead of constructing CouchDB URLs directly,
So that the server-side core-element export path has the same SQL migration seam as its mmria.services counterpart (which was fully remediated in Epics 24–27).

---

## Acceptance Criteria

**AC-1 — Metadata version-spec read replaced**
Given `mmria-server/util/core_element_export/core_element_exporter.cs` at approximately line 132 calls `_couchDbHttpClient.ExecuteAsync("GET", metadata_url, ...)` where `metadata_url = db_config.url + $"/metadata/version_specification-{version}/metadata"`
When this story is complete
Then that call is replaced with `IMetadataRepository.GetAppDocumentAsync(version, db_config)` (the same method used by the `c_convert_to_*` files in Epic 25 Story 25.2); `IMetadataRepository` is injected via constructor injection

**AC-2 — De-identified list read replaced**
Given the same file at approximately line 213 calls `_couchDbHttpClient.ExecuteAsync("GET", db_config.url + "/metadata/de-identified-list", ...)` to load the de-identification field list
When this story is complete
Then that call is replaced with `IMetadataRepository.GetDeIdentifiedListAsync(db_config)` (the same method used by `c_de_identifier` in Story 25.2)

**AC-3 — Case view read replaced**
Given the same file at approximately line 246 calls `_couchDbHttpClient.ExecuteAsync("GET", request_string, ...)` where `request_string` is a `mmrds` view query URL for export filtering
When this story is complete
Then that call is replaced with the appropriate `ICaseRepository` view query method; if no view query method covering this specific view exists on `ICaseRepository`, one is added to the interface and implemented in `CaseDAL` before replacing the call site

**AC-4 — Per-case document GET replaced**
Given the same file at approximately line 265 calls `_couchDbHttpClient.ExecuteAsync("GET", URL, ...)` where `URL = $"{db_config.url}/{db_config.prefix}mmrds/{id}"` to fetch the full case document for export
When this story is complete
Then that call is replaced with `ICaseRepository.GetCaseDocumentJsonAsync(id, db_config)` or equivalent

**AC-5 — `_couchDbHttpClient` removed from server `core_element_exporter` if no calls remain**
Given the server copy currently injects `CouchDbHttpClient`
When this story is complete
Then if all four call sites are replaced, `_couchDbHttpClient` is removed from the constructor and field; callers that pass `CouchDbHttpClient` to this constructor are updated

**AC-6 — Build passes with zero errors**
Given the changes above
When the build runs
Then `mmria-server` builds with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/util/core_element_export/core_element_exporter.cs` | **UPDATE** — inject `IMetadataRepository` + `ICaseRepository`; replace 4 direct calls; remove `_couchDbHttpClient` if no calls remain |
| Caller(s) that instantiate this exporter | **UPDATE** — resolve and pass repo instances from DI scope; remove `CouchDbHttpClient` arg if removed from ctor |

**This is the server copy:** `mmria-server/util/core_element_export/core_element_exporter.cs` is a separate file from `nccdphp-drh-mmria-services/mmria.services/Utilities/CoreElementExport/core_element_exporter.cs`. The services copy was remediated in Epics 24 and 27. This story covers the server copy only.

**Method signatures to use (confirmed in Epic 25 Story 25.2):**
- AC-1: `IMetadataRepository.GetAppDocumentAsync(version, dbConfig)` → `mmria.common.metadata.app`
- AC-2: `IMetadataRepository.GetDeIdentifiedListAsync(dbConfig)` → `ExpandoObject` (add to interface if absent, following Story 25.2 guidance)
- AC-3: Check the URL in `request_string` at line 246 to determine which `ICaseRepository` view method applies
- AC-4: `ICaseRepository.GetCaseDocumentJsonAsync(id, dbConfig)` → raw JSON string

**Finding callers:** Search for `new core_element_exporter(` or `core_element_exporter(` in mmria-server to find instantiation sites.
