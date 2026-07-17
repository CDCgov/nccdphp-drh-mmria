# Story 25.2 — Metadata Reader `IMetadataRepository` Injection Pass

**Epic:** 25 — Async Safety + Metadata Reader Consolidation
**Story ID:** 25.2
**Status:** ready-for-dev
**Date added:** 2026-07-17
**Depends on:** None (can proceed in parallel with 25.1)
**Source requirements:** epics.md §Epic 25 Story 25.2; project-context.md §2.2

---

## User Story

As a developer,
I want the six transform-helper classes in `mmria-server/util/` and their counterparts in `mmria.common/.../MMRIARebuild/Manager/` to read metadata through `IMetadataRepository` instead of calling `CouchDbHttpClient.ExecuteAsync` directly,
So that the metadata database access in the rebuild and de-identification pipeline has a SQL migration seam.

---

## Acceptance Criteria

**AC-1 — `IMetadataRepository` injected into all 14 target files**
Given each target file currently constructs a metadata URL and calls `_couchDbHttpClient.ExecuteAsync("GET", metadata_url, ...)` to read one of three documents
When this story is complete
Then `IMetadataRepository` is injected via constructor parameter (optional, defaulting to null) into each of the 14 target files; all required interface methods already exist — no new methods need to be added to `IMetadataRepository`

**AC-2 — `version_specification` reads replaced in transform converters**
Given `c_convert_to_dqr_detail.cs`, `c_convert_to_opioid_report_object.cs`, `c_convert_to_report_object.cs`, and `c_generate_frequency_summary_report.cs` (both server and common variants — 8 files total) each call `GET metadata/version_specification-{version}/metadata` to load the form schema for report generation
When this story is complete
Then each direct call is replaced with `await _metadataRepository.GetAppDocumentAsync(metadata_version, db_config)` when `_metadataRepository != null`; when `_metadataRepository == null`, the existing direct HTTP call is used as fallback

**AC-3 — `de-identified-list` reads replaced in de-identifier helpers**
Given `c_de_identifier.cs` (server and common variants — 2 files) calls `GET {url}/metadata/de-identified-list` to load the field-path de-identification list
When this story is complete
Then each direct call is replaced with `await _metadataRepository.GetDeIdentifiedListAsync(db_config)` when `_metadataRepository != null`; when `_metadataRepository == null`, the existing direct HTTP call is used as fallback; `GetDeIdentifiedListAsync` returns `ExpandoObject` matching what the existing JSON deserialization produces

**AC-4 — `de-identified-export-list` reads replaced in CDC de-identifier helpers**
Given `c_cdc_de_identifier.cs` (server: `mmria-server/util/c_cdc_de_identifier.cs`; common: `mmria.common/SharedLibraries/MMRIAServices/Helper/c_cdc_de_identifier.cs` — 2 files) calls `GET {url}/metadata/de-identified-export-list`
When this story is complete
Then each direct call is replaced with `await _metadataRepository.GetDeIdentifiedExportListAsync(db_config)` when `_metadataRepository != null`; fallback preserved when null

**AC-5 — Null-fallback pattern used throughout**
Given callers of these transform helpers that do not yet pass an `IMetadataRepository` instance
When this story is complete
Then the null-fallback (use direct `_couchDbHttpClient.ExecuteAsync`) preserves existing behavior for those callers; no caller changes are required in this story

**AC-6 — Build passes with zero errors**
Given the changes above
When the build runs
Then `mmria-server`, `mmria.common`, and `mmria.services` all build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria-server/util/c_convert_to_dqr_detail.cs` | **UPDATE** — inject `IMetadataRepository?`; replace metadata GET with `GetAppDocumentAsync` |
| `mmria-server/util/c_convert_to_opioid_report_object.cs` | **UPDATE** — same pattern |
| `mmria-server/util/c_convert_to_report_object.cs` | **UPDATE** — same pattern |
| `mmria-server/util/c_generate_frequency_summary_report.cs` | **UPDATE** — same pattern |
| `mmria-server/util/c_de_identifier.cs` | **UPDATE** — inject `IMetadataRepository?`; replace de-identified-list GET with `GetDeIdentifiedListAsync` |
| `mmria-server/util/c_cdc_de_identifier.cs` | **UPDATE** — inject `IMetadataRepository?`; replace de-identified-export-list GET with `GetDeIdentifiedExportListAsync` |
| `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_convert_to_dqr_detail.cs` | **UPDATE** — same as server variant |
| `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_convert_to_opioid_report_object.cs` | **UPDATE** — same as server variant |
| `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_convert_to_report_object.cs` | **UPDATE** — same as server variant |
| `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_generate_frequency_summary_report.cs` | **UPDATE** — same as server variant |
| `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_de_identifier.cs` | **UPDATE** — same as server `c_de_identifier.cs` |
| `mmria.common/SharedLibraries/MMRIAServices/Helper/c_cdc_de_identifier.cs` | **UPDATE** — same as server `c_cdc_de_identifier.cs` |

**`IMetadataRepository` method mapping:**

| File group | Metadata document read | Interface method |
|---|---|---|
| `c_convert_to_*`, `c_generate_frequency_summary_report` | `metadata/version_specification-{version}/metadata` | `GetAppDocumentAsync(version, dbConfig)` → `mmria.common.metadata.app` |
| `c_de_identifier` | `metadata/de-identified-list` | `GetDeIdentifiedListAsync(dbConfig)` → `ExpandoObject` |
| `c_cdc_de_identifier` | `metadata/de-identified-export-list` | `GetDeIdentifiedExportListAsync(dbConfig)` → `ExpandoObject` |

**All three interface methods already exist in `IMetadataRepository`** (`mmria.common/SharedLibraries/MetadataVersion/IMetadataRepository.cs`) — no interface changes required.

**Implementation pattern for each file:**
```csharp
// Constructor — add optional parameter:
public c_convert_to_dqr_detail(
    ...,
    IMetadataRepository metadataRepository = null)
{
    ...
    _metadataRepository = metadataRepository;
}

// In method body — replace direct call:
mmria.common.metadata.app metadata;
if (_metadataRepository != null)
{
    metadata = await _metadataRepository.GetAppDocumentAsync(metadata_version, db_config);
}
else
{
    string metadata_url = db_config.url + $"/metadata/version_specification-{metadata_version}/metadata";
    string metadata_response = await _couchDbHttpClient.ExecuteAsync("GET", metadata_url, null, db_config.user_name, db_config.user_value);
    metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_response);
}
```

Note: `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` already injects `IMetadataRepository` correctly (Epic 24 work). Confirm its usage pattern before implementing to ensure consistency.
