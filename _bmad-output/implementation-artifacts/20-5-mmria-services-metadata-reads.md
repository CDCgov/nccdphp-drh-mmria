# Story 20.5 — Route `mmria.services` Read-Only `metadata` Calls Through `IMetadataRepository`

**Epic:** 20 — `metadata` Consolidation (SQL Migration Foundation)
**Story ID:** 20.5
**Status:** done
**Date added:** 2026-07-15
**Depends on:** 20.2
**Source requirements:** epics.md §Epic 20 Story 20.5; project-context.md §2.2

---

## User Story

As a developer,
I want the background-job and exporter code in `mmria.services` that reads `metadata` documents to delegate to `IMetadataRepository`,
So that the services project is covered by the same interface contract as the server.

---

## Acceptance Criteria

**AC-1 — Services version-spec and de-id-list reads replaced**
Given the two dominant read operations in `mmria.services`:
- `GET metadata/version_specification-{version}/metadata` — in `c_convert_to_report_object`, `c_convert_to_opioid_report_object`, `c_convert_to_dqr_detail`, `c_de_identifier`, `c_cdc_de_identifier`, `c_document_sync_all`, `c_document_sync_all_legacy`, `c_generate_frequency_summary_report`, `c_sync_document`, `BatchItemProcessingService`, `core_element_exporter`, `exporter`, `mmrds_exporter`, `export_all_generate_name_map`
- `GET metadata/de-identified-list` and `GET metadata/de-identified-export-list` — in `c_de_identifier`, `c_cdc_de_identifier`, `c_document_sync_all`, `c_document_sync_all_legacy`, `c_sync_document`, `core_element_exporter`

When this story is complete
Then each is replaced with the corresponding `IMetadataRepository` method; since `mmria.services` already references `mmria.common`, no new project reference is needed

**AC-2 — `PopulateCDCInstanceSupervisor` replaced**
Given `PopulateCDCInstanceSupervisor.cs` — 2 hits (populate-CDC-instance config document)
When evaluated
Then these are replaced using the same `IMetadataRepository` method as `MMRIAServicesDAL`

**AC-3 — Sync orchestration logic preserved**
Given `c_document_sync_all` and `c_document_sync_all_legacy` use `metadata` reads as part of sync orchestration
When replaced
Then only the URL construction is replaced — sync orchestration logic remains in the actor classes

**AC-4 — Build succeeds**
Given the build after all changes
When verified
Then `mmria.services`, `mmria.common`, and `mmria-server` all build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `c_convert_to_report_object.cs` | **UPDATE** — inject `IMetadataRepository` |
| `c_convert_to_opioid_report_object.cs` | **UPDATE** |
| `c_convert_to_dqr_detail.cs` | **UPDATE** |
| `c_de_identifier.cs` | **UPDATE** |
| `c_cdc_de_identifier.cs` | **UPDATE** |
| `c_document_sync_all.cs` | **UPDATE** |
| `c_document_sync_all_legacy.cs` | **UPDATE** |
| `c_generate_frequency_summary_report.cs` | **UPDATE** |
| `c_sync_document.cs` | **UPDATE** |
| `BatchItemProcessingService.cs` | **UPDATE** |
| `core_element_exporter.cs` | **UPDATE** |
| `exporter.cs` | **UPDATE** |
| `mmrds_exporter.cs` | **UPDATE** |
| `export_all_generate_name_map.cs` | **UPDATE** |
| `PopulateCDCInstanceSupervisor.cs` | **UPDATE** |

---

## Sequencing

Depends on 20.2. Can proceed in parallel with 20.3 and 20.4. Note: this is the highest-touch story in Epic 20 — 15 files across the services project.
