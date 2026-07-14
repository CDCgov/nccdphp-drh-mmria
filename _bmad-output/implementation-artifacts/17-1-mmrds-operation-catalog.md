# Story 17.1 — mmrds Operation Catalog

**Epic:** 17 — mmrds CRUD Consolidation (SQL Migration Foundation)
**Story ID:** 17.1
**Status:** done
**Date added:** 2026-07-14
**Depends on:** none — discovery only
**Source requirements:** epics.md §Epic 17 Story 17.1

---

## User Story

As a developer,
I want a definitive catalog of every operation against the `mmrds` database across all three projects,
So that Stories 17.2–17.7 have an agreed-upon, complete operation set before any code changes begin.

---

## Acceptance Criteria

**AC-1 — Catalog document created**
Given all `.cs` files in `mmria-server`, `mmria.common`, and `mmria.services`
When the developer completes the catalog
Then `docs/ai/mmrds_operation_catalog.md` exists and contains a table of every distinct operation grouped into: Case CRUD (GET/PUT/DELETE by ID), versioned reads (GET at revision, GET all revisions), view queries (`by_date_created`, `by_date_last_updated`, `by_jurisdiction_id`, `by_last_name`, `by_pmss_number`, `record_id_list`), Mango `_find` queries, bulk operations (`_bulk_docs`, `_all_docs`), and admin/infra operations (`_security`, `_design/*`, `_changes`)

**AC-2 — Each entry records calling context**
Given each catalog entry
When the catalog is complete
Then each entry records: operation name, calling file(s) with approximate line number, the URL construction pattern in use (A, B, or C — see Dev Notes), and the response type expected

**AC-3 — Out-of-scope operations clearly marked**
Given admin/infra operations (`_security`, `_design/*`, `_changes`, sync `_all_docs`)
When the catalog is written
Then they are listed in a separate "Infrastructure / Out of Scope" section and marked **out of scope for Stories 17.2–17.7**

**AC-4 — Boundary decision placeholder added**
Given the catalog document
When written
Then it includes a "Boundary Decisions" section with a placeholder entry for MMRIAServicesDAL and `c_document_sync_all` (to be completed in Story 17.7)

---

## Dev Notes — Implementation

### Output file

Create: `docs/ai/mmrds_operation_catalog.md`

No code changes — this is a discovery/documentation story only.

---

### URL construction patterns to classify

As you encounter each call site, label it with one of these patterns:

| Label | Pattern | Example |
|-------|---------|---------|
| **A** | Hand-assembled with string interpolation — **wrong** | `$"{dbConfig.url}/{dbConfig.prefix}mmrds/{id}"` |
| **B** | Uses `Get_Prefix_DB_Url` — **correct** | `dbConfig.Get_Prefix_DB_Url($"mmrds/{id}")` |
| **C** | CDC special-case with underscore separator | `$"{dbInfo.url}/{dbInfo.prefix}_mmrds"` (MMRIAServicesDAL only) |

---

### Search patterns — start here

Run the following searches to find all call sites:

```
grep -r "mmrds" --include="*.cs" -n
```

Focus on files in:
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/`
- `source-code/mmria/mmria-server/`
- `nccdphp-drh-mmria-services/`

---

### Known call sites (from architecture analysis — verify current line numbers)

#### mmria.common/SharedLibraries

| File | Operations | Pattern |
|------|-----------|---------|
| `Case/DAL/CaseDAL.cs` | GetCaseAsync, GetCaseDocumentJsonAsync, UpdateCaseAsync, PutCaseDocumentJsonAsync, GetCasesByDateLastUpdatedViewJsonAsync, GetCasesByDateCreatedViewJsonAsync, GetSoftLockedCaseIdForUserInAnotherTabAsync (_find) | A (most), B (one) |
| `Case/Manager/CaseManager.cs` | ~11 direct mmrds call sites | A |
| `CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | GetCaseDocumentAsync, UpdateCaseDocumentAsync, GetCaseRevisionsRawAsync, GetCaseAtRevisionAsync, RestoreCaseDocumentAsync | A |
| `AuditRecovery/DAL/AuditRecoveryDAL.cs` | Case view by_id query (line ~24), GetCaseAtRevision (line ~75) | A |
| `CVS/DAL/CVSDAL.cs` | by_date_last_updated view (line ~73), case GET by ID (line ~84) | A |
| `VitalImport/DAL/VitalImportDAL.cs` | Case GET by ID ×2 (lines ~26, ~33) | A |
| `AttachmentDAL.cs` | by_pmss_number view query (line ~21) | A |
| `OfflineCase/Manager/OfflineCaseManager.cs` | Case GET ×1, Case PUT ×2 (lines ~104, ~298, ~398) | A |
| `MMRIAServices/DAL/MMRIAServicesDAL.cs` | CDC populate path (multiple), GetMmrdsDatabaseUrl() helper (lines ~553–557) | C |

#### mmria-server/model/actor

| File | Operations | Pattern |
|------|-----------|---------|
| `JurisdictionSummary.cs` | mmrds reads for jurisdiction stats | A |
| `VROSummary.cs` | mmrds reads for VRO stats | A |
| `c_db_setup.cs` | `_security`, `_design/*` setup | Infra |
| `c_document_sync_all.cs` | `_all_docs` bulk sync | Infra |
| `c_document_sync_all_legacy.cs` | `_all_docs` bulk sync | Infra |

#### mmria.services

| File | Operations | Pattern |
|------|-----------|---------|
| `BatchProcessor.cs` | `_all_docs`, case GET at revision | A |
| `BatchItemProcessingService.cs` | Case GET by ID | A |
| `PagedCaseIdLoader.cs` | `by_date_created` view | A |
| `core_element_exporter.cs` | Case GET by ID | A |
| `exporter.cs` | `_all_docs`, case GET by ID | A |
| `mmrds_exporter.cs` | Case GET by ID | A |
| `c_document_sync_all.cs` | `_all_docs` bulk sync | Infra |

---

### Catalog document structure

Use this structure for `docs/ai/mmrds_operation_catalog.md`:

```markdown
# mmrds Operation Catalog

## In-Scope Operations

### Case CRUD (GET/PUT/DELETE by ID)
| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| ...       | ...            | ...     | A/B/C       | ...           |

### Versioned Reads
...

### View Queries
...

### Mango _find Queries
...

### Bulk Operations
...

## Infrastructure / Out of Scope
| Operation | File(s) | Reason |
|-----------|---------|--------|
| _security setup | c_db_setup.cs | DB initialization |
| _design/* | c_db_setup.cs | View/index setup |
| _all_docs (sync) | c_document_sync_all.cs | Change-feed sync |
| _changes | ... | Change-feed infra |

## Boundary Decisions
_Placeholder — to be completed in Story 17.7_

- **MMRIAServicesDAL CDC populate path**: TBD
- **c_document_sync_all bulk _all_docs**: TBD
```
