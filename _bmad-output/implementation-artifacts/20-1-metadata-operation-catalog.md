# Story 20.1 — `metadata` Operation Catalog

**Epic:** 20 — `metadata` Consolidation (SQL Migration Foundation)
**Story ID:** 20.1
**Status:** ready-for-dev
**Date added:** 2026-07-15
**Depends on:** none — discovery only
**Source requirements:** epics.md §Epic 20 Story 20.1; project-context.md §2.2

---

## User Story

As a developer,
I want a definitive catalog of every operation against the `metadata` database,
So that Stories 20.2–20.6 have an agreed-upon, complete operation set before any code changes begin.

---

## Acceptance Criteria

**AC-1 — `metadata` section added to operation catalog**
Given all `.cs` files in `mmria-server`, `mmria.common`, and `mmria.services`
When the developer completes the catalog
Then `docs/ai/mmrds_operation_catalog.md` gains a `metadata` section listing every distinct operation grouped into: version specification CRUD, de-identification list reads, metadata document GET/PUT (by ID), UI specification CRUD, attachment reads/writes, broadcast/offline/populate-CDC config document CRUD, export list and substance mapping CRUD, and bulk reads (`_all_docs`)

**AC-2 — Per-entry detail**
Given each catalog entry
When the catalog is complete
Then each entry records: operation name, calling file(s), URL pattern in use, and response type expected

**AC-3 — Infrastructure scoped out**
Given `c_db_setup.cs` and `Process_Migrate_*` references
When evaluated
Then they are listed but marked **out of scope** — DB setup and one-time migration scripts are not application CRUD

---

## Dev Notes — Scope Context

| Category | Files | Hits | Notes |
|---|---|---|---|
| `MetadataVersionManager` (already DAL-backed) | `MetadataVersionManager.cs` | 22 | Builds URLs directly in manager — not all through DAL |
| Controllers bypassing DAL | `broadcast_messageController`, `de_identified_listController`, `export_list_managerController`, `substance_mappingController`, `abstractorDeidentifiedCaseController`, `CaseController`, `versionController`, `record_idController`, `systemOfflineController` | ~14 | Mix of Wave targets |
| SharedLibraries bypassing DAL | `AuditRecoveryDAL`, `CaseValidationDAL`, `MMRIAServicesDAL` | ~8 | Within common — bypass the canonical DAL |
| Services actors/exporters | `c_convert_to_report_object`, `c_convert_to_opioid_report_object`, `c_convert_to_dqr_detail`, `c_de_identifier`, `c_cdc_de_identifier`, `c_document_sync_all`, `c_document_sync_all_legacy`, `c_generate_frequency_summary_report`, `c_sync_document`, `BatchItemProcessingService`, `core_element_exporter`, `exporter`, `mmrds_exporter`, `export_all_generate_name_map`, `PopulateCDCInstanceSupervisor` | ~39 | Mostly read-only: GET version_specification and GET de-identified-list |
| Infra/out-of-scope | `c_db_setup.cs`, `Process_Migrate_*` | ~15 | DB setup and one-time migration scripts |

**Key observation:** The services layer makes two operations overwhelmingly — `GET metadata/version_specification-{version}/metadata` and `GET metadata/de-identified-list` — accounting for the majority of the 39 services hits and all read-only.

---

## Sequencing

Discovery only. 20.2 is unblocked once this is complete. 20.6 (boundary decision) can run in parallel with 20.1.
