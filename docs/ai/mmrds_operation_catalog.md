# mmrds Operation Catalog

**Epic:** 17 — mmrds CRUD Consolidation (SQL Migration Foundation)
**Story:** 17.1
**Date:** 2026-07-14

This catalog records every distinct operation against the `{prefix}mmrds` CouchDB database across `mmria-server`, `mmria.common`, and `mmria.services`. It is the authoritative operation set for Stories 17.2–17.7.

---

## URL Construction Patterns

| Label | Pattern | Example |
|-------|---------|---------|
| **A** | Hand-assembled with string interpolation — **wrong** | `$"{dbConfig.url}/{dbConfig.prefix}mmrds/{id}"` |
| **B** | Uses `Get_Prefix_DB_Url` helper — **correct** | `dbConfig.Get_Prefix_DB_Url($"mmrds/{id}")` |
| **C** | CDC write — no prefix, different CouchDB instance (CDC system) | `$"{cdcConnection.url}/mmrds/_bulk_docs"` |

> **Note on Pattern C:** Pattern C applies only to `BulkSavePopulateCdcDocumentsAsync`, which writes to the CDC system's own CouchDB instance (`cdcConnection`) — a completely different server from the tenant database. No prefix applies because the CDC system uses a flat `mmrds` database. The private `GetMmrdsDatabaseUrl(dbInfo)` helper (formerly lines 553–557, deleted in Story 17.7) produced `{url}/{prefix}_mmrds` (underscore separator) for non-empty prefixes — this was a **bug**, not intentional design. All three methods that used it (`GetCaseIdsByDateCreated`, `GetCaseDocumentForPopulateCDC`, `GetCaseDocumentsForPopulateCDC`) read from the tenant's `{prefix}mmrds` database and now correctly use Pattern B (`dbInfo.Get_Prefix_DB_Url("mmrds")`).

---

## In-Scope Operations

### Case CRUD (GET/PUT/DELETE by ID)

#### GET case by ID

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetCaseAsync` | `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs` | 23 | A | `mmria_case` |
| `GetCaseDocumentJsonAsync` | `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs` | 32 | A | `string` (raw JSON) |
| `GetCaseAsync` (with authz) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 900 | B | `mmria_case` |
| `SaveCaseAsync` (pre-write existence probe) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 1025 | B | `ExpandoObject` |
| `ForceClearCaseLockAsync` (read before update) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 1330 | B | `JObject` |
| `ToggleOfflineModeAsync` (read before update) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 1519 | A | `ExpandoObject` |
| `FinalizeUnloadForSingleCaseAsync` (read/retry loop) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 2098 | B | `JObject` |
| `DeleteCaseAsync` (pre-delete read) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 2294 | B | `ExpandoObject` |
| `GetCaseDocumentAsync` | `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | 32 | B | `ExpandoObject` |
| `GetCaseDocumentAsync` | `mmria.common/SharedLibraries/CaseView/DAL/CaseViewDAL.cs` | 39 | B | `ExpandoObject` |
| `UpgradeCaseToHardLockAsync` (read before update) | `mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs` | 104 | A | `Dictionary<string,object>` |
| `ReleaseOfflineCaseLocksAsync` (read before update — loop) | `mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs` | 298 | A | `JObject` |
| `RecoverSoftLocksAsync` (read before update — loop) | `mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs` | 398 | A | `JObject` |
| `GetCaseAsync` | `mmria.common/SharedLibraries/VitalImport/DAL/VitalImportDAL.cs` | 26 | A | `ExpandoObject` |
| `GetCaseById` (single case branch) | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 83 | A | `ExpandoObject` |
| `GetCaseDocumentForPopulateCDC` | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 335 | B | `ExpandoObject` |
| `GetCaseAsync` | `mmria.common/SharedLibraries/CVS/DAL/CVSDAL.cs` | 84 | B | `ExpandoObject` |
| `GetRev` (HEAD — returns ETag rev only) | `mmria-server/Controllers/api/caseController.cs` | 87 | A | `ETag` header (rev string) |
| `Get(case_id)` (PMSS) | `mmria-server/Controllers/api/caseController.pmss.cs` | 63 | B | `mmria_case` (PMSS schema) |
| `Post` (PMSS — pre-save existence check) | `mmria-server/Controllers/api/caseController.pmss.cs` | 199 | B | `mmria_case` (PMSS schema) |
| `Delete` (PMSS — pre-delete read) | `mmria-server/Controllers/api/caseController.pmss.cs` | 343 | B | `mmria_case` (PMSS schema) |
| `GetCaseCount` (per-case GET in id loop) | `mmria-server/util/VROSummary.cs` | 181 | A | `ExpandoObject` |
| `Execute` (per-case export GET) | `mmria.services/Utilities/Exporter/mmrds_exporter.cs` | 277 | A | `ExpandoObject` |
| `Execute` (per-case export GET) | `mmria.services/Utilities/Exporter/exporter.cs` | 536 | A | `ExpandoObject` |
| `Execute` (per-case export GET — two call sites) | `mmria.services/Utilities/Exporter/core_element_exporter.cs` | 246, 260 | A | `ExpandoObject` |
| `ProcessBatchItemAsync` (import — PUT new case creates via GET-then-PUT pattern) | `mmria.services/Services/BatchItemProcessingService.cs` | 2616 | A | `document_put_response` |

> **caseController.cs note:** `caseController.cs::Get` delegates to `CaseManager.GetCaseAsync`; it does not build an mmrds URL directly. `caseController.cs::GetRev` at line 87 does build a direct URL for a `HEAD` probe.

#### PUT case by ID (create / update)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `UpdateCaseAsync` | `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs` | 46 | A | `document_put_response` |
| `PutCaseDocumentJsonAsync` | `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs` | 55 | A | `string` (raw JSON) |
| `SaveCaseAsync` (PUT) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 1205 | B | `document_put_response` |
| `ForceClearCaseLockAsync` (PUT) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 1369 | B | `document_put_response` |
| `ToggleOfflineModeAsync` (PUT) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 1723 | A | `document_put_response` |
| `FinalizeUnloadForSingleCaseAsync` (PUT with 409-retry) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | ~2165 | B | `document_put_response` |
| `UpdateCaseDocumentAsync` | `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | 39 | B | `document_put_response` |
| `RestoreCaseDocumentAsync` (recover deleted) | `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | 87 | B | `document_put_response` |
| `UpgradeCaseToHardLockAsync` (PUT after read) | `mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs` | ~113 | A | `string` (ignored) |
| `ReleaseOfflineCaseLocksAsync` (PUT — loop) | `mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs` | ~312 | A | `document_put_response` |
| `RecoverSoftLocksAsync` (PUT — loop) | `mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs` | ~443 | A | `document_put_response` |
| `PutCaseAsync` | `mmria.common/SharedLibraries/VitalImport/DAL/VitalImportDAL.cs` | 33 | A | `document_put_response` |
| `Post` (PMSS — PUT save) | `mmria-server/Controllers/api/caseController.pmss.cs` | 230 | B | `document_put_response` |
| `ProcessBatchItemAsync` (import — PUT new case) | `mmria.services/Services/BatchItemProcessingService.cs` | 2616 | A | `document_put_response` |
| `ExecuteAsync` (data migration PUT — char→numeric) | `mmria-server/model/actor/quartz/Process_Migrate_Charactor_to_Numeric.cs` | 88 | A | `string` |
| `ExecuteAsync` (data migration PUT) | `mmria-server/model/actor/quartz/Process_Migrate_Data.cs` | 160 | A | `string` |

#### DELETE case by ID (with `?rev=`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `DeleteCaseAsync` (primary DELETE using stored rev) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 2280, 2392 | B | `ExpandoObject` |
| `DeleteCaseAsync` (DELETE with caller-supplied and then stored rev — two URLs) | `mmria.common/SharedLibraries/VitalImport/Manager/VitalImportManager.cs` | 218, 239 | A | via `VitalImportDAL.DeleteCaseAsync` |
| `DeleteCaseAsync` | `mmria.common/SharedLibraries/VitalImport/DAL/VitalImportDAL.cs` | (delegated from VitalImportManager) | A | `ExpandoObject` |
| `Delete` (PMSS — primary and then stored-rev override) | `mmria-server/Controllers/api/caseController.pmss.cs` | 330, 362 | B | `mmria_case` (PMSS schema) |
| `ExecuteAsync` (batch rejection — DELETE with rev) | `mmria.services/Actors/BatchProcessor.cs` | 512 | A | `string` |

---

### Versioned Reads

#### GET case at specific revision (`?rev={rev}`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetCaseRevisionAsync` | `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | 75 | A | `ExpandoObject` |
| `GetCaseAtRevisionAsync` | `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | 80 | B | `ExpandoObject` |

#### GET all revisions (`?revs=true&open_revs=all`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetCaseRevisionsRawAsync` | `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | 74 | B | `string` (raw JSON — multipart response) |
| `Get` (case revision list) | `mmria-server/Controllers/api/caseRevisionListController.cs` | 48 | A | `string` (raw JSON) |

---

### View Queries

#### `by_date_last_updated`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetCasesByDateLastUpdatedViewJsonAsync` | `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs` | 68 | A | `string` (raw JSON → `case_view_response`) |
| `GetCasesByDateAsync` | `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | 22 | B | `case_view_response` |
| `GetCaseViewByRecordIdAsync` | `mmria.common/SharedLibraries/CVS/DAL/CVSDAL.cs` | 73 | B | `case_view_response` |

#### `by_date_created`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetCasesByDateCreatedViewJsonAsync` (with and without prefix branches) | `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs` | 84, 88 | A | `string` (raw JSON → `case_view_response`) |
| `GetPagedViewAsync` | `mmria.common/SharedLibraries/CaseView/CaseViewManager.cs` | 1385 | B | `case_view_response` |
| `GetCaseViewAsync` (keyed search mode) | `mmria.common/SharedLibraries/CaseView/CaseViewManager.cs` | 1489 | A | `case_view_response` |
| `GetExistingRecordIds` | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 189 | A | `case_view_response` |
| `GetCaseIdsByDateCreated` | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 307 | B | `HashSet<string>` |
| `Execute` (export — enumerate all case IDs) | `mmria.services/Utilities/Exporter/core_element_exporter.cs` | 240 | A | `case_view_response` |
| `GetCaseIdsAsync` (paged streaming) | `mmria.services/Utilities/PagedCaseIdLoader.cs` | 36 | A | `IAsyncEnumerable<string>` |
| `Get` (PMSS — all cases) | `mmria-server/Controllers/api/case_viewController.pmss.cs` | 111 | A | `case_view_response` |
| `Get` (record ID list) | `mmria-server/Controllers/api/record_idController.cs` | 50 | B | `case_view_response` |

#### `by_jurisdiction_id`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetCaseCount` | `mmria-server/util/JurisdictionSummary.cs` | 337 | A | `case_view_response` |
| `GetCaseCount` | `mmria-server/util/VROSummary.cs` | 333 | A | `case_view_response` |

#### `by_last_name`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetCaseView` (search by last name) | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 27 | A | `case_view_response` |

#### `by_pmss_number`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetPmssCaseViewByNumberAsync` | `mmria.common/SharedLibraries/Attachment/DAL/AttachmentDAL.cs` | 21 | A | `pmss_case_view_response` |
| `Get` (PMSS — by PMSS number) | `mmria-server/Controllers/api/case_viewController.pmss.cs` | 149 | A | `case_view_response` |

#### `record_id_list`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetRecordIdListAsync` | `mmria.common/SharedLibraries/CaseView/CaseViewManager.cs` | 1268 | A | `List<string>` |

#### `by_id` (AuditRecovery view)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetCaseViewResponseAsync` (audit recovery lookup by case ID) | `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | 24 | A | `case_view_response` |

#### Dynamic / parameterized sort view (multiple views via runtime `{sort_view}` parameter)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetCaseViewAsync` (sort_view param, standard case list) | `mmria.common/SharedLibraries/CaseView/CaseViewManager.cs` | 938 | A | `case_view_response` |
| `GetFilteredCaseViewAsync` (sort_view param) | `mmria.common/SharedLibraries/CaseView/CaseViewManager.cs` | 1317 | B | `case_view_response` |
| `GetPaginatedCaseViewAsync` (sort_view param) | `mmria.common/SharedLibraries/CaseView/CaseViewManager.cs` | 1662 | B | `case_view_response` |
| `GetSortedCaseViewAsync` (sort_view param, vital import) | `mmria.common/SharedLibraries/VitalImport/Manager/VitalImportManager.cs` | 58 | A | `case_view_response` (via `VitalImportDAL`) |
| `GetCaseViewAsync` (PMSS sort_view param) | `mmria-server/Controllers/api/CaseViewSearch.pmss.cs` | 1953 | A | `case_view_response` |

---

### Mango `_find` Queries

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Selector |
|-----------|----------------|---------|-------------|---------------|----------|
| `GetSoftLockedCaseIdForUserInAnotherTabAsync` | `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs` | 128 | B | `string` (→ first `_id` or `null`) | `{offline_by, is_offline:true, offline_lock_type:1, offline_by_tab_id:{$ne:currentTabId}}` |
| `RecordIdExistsAsync` (record-id uniqueness check) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 704 | A | `bool` | `{record_id:{$eq:recordId}}` |

---

### Bulk Operations (`_all_docs`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Notes |
|-----------|----------------|---------|-------------|---------------|-------|
| `Get(case_id=null)` fallback (PMSS — all docs) | `mmria-server/Controllers/api/caseController.pmss.cs` | 59 | B | (never returned — overridden by id check) | Dead branch; `case_id` is always provided |
| `GetCaseById(case_id=null)` fallback | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 79 | A | `ExpandoObject` (all docs) | Dead branch; `case_id` is always provided in practice |
| `GetCaseDocumentsForPopulateCDC` (POST `_all_docs` with `keys` body) | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 359 | B | `List<ExpandoObject>` | CDC populate: bulk fetch by IDs |
| `GetCaseCountAll` (VRO stats) | `mmria-server/util/VROSummary.cs` | 544 | A | `c_all_docs_response` | Counts all mmrds docs |
| `Execute` (batch rejection — `_all_docs` initial value, never sent) | `mmria.services/Actors/BatchProcessor.cs` | 498 | A | (dead — `request_string` overridden before use) | Artifact: line 498 sets `_all_docs` URL but it is reassigned before the HTTP call at line 512 |
| `Execute` (export — enumerate all cases) | `mmria.services/Utilities/Exporter/exporter.cs` | 154 | A | allDocs response | Full case export |
| `Execute` (data migration — char→numeric, all cases) | `mmria-server/model/actor/quartz/Process_Migrate_Charactor_to_Numeric.cs` | 57 | A | `string` | One-time migration |
| `Execute` (data migration — general, all cases) | `mmria-server/model/actor/quartz/Process_Migrate_Data.cs` | 80 | A | `string` | One-time migration |

---

### Bulk Write (`_bulk_docs`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Notes |
|-----------|----------------|---------|-------------|---------------|-------|
| `BulkSavePopulateCdcDocumentsAsync` | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 468 | C | `List<document_put_response>` | POST to CDC `{cdcConnection.url}/mmrds/_bulk_docs` — no prefix, CDC-only |

---

## Infrastructure / Out of Scope

These operations are not targeted by Stories 17.2–17.7. They operate on mmrds as part of database initialization, replication, or sync infrastructure rather than application case CRUD.

| Operation | File(s) | Line(s) | Reason |
|-----------|---------|---------|--------|
| PUT `mmrds` (database creation) | `mmria-server/model/actor/c_db_setup.cs` | 141 | DB initialization |
| PUT `mmrds` (database creation, CDC rebuild) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | 81 | CDC rebuild infra |
| PUT `mmrds` (database creation, CDC populate) | `mmria.common/SharedLibraries/MMRIAServices/MMRIAServicesManager.cs` | 1003 | CDC populate infra |
| DELETE `mmrds` (database deletion, CDC rebuild) | `mmria.common/SharedLibraries/MMRIAServices/MMRIAServicesManager.cs` | 995 | CDC populate infra |
| PUT `mmrds/_security` | `mmria-server/model/actor/c_db_setup.cs` | 144 | DB security setup |
| PUT `mmrds/_security` | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | 83 | CDC rebuild infra |
| PUT `mmrds/_security` (CDC) | `mmria.common/SharedLibraries/MMRIAServices/MMRIAServicesManager.cs` | 1007 | CDC populate infra |
| PUT `mmrds/_design/sortable` | `mmria-server/model/actor/c_db_setup.cs` | 154 | View/index initialization |
| PUT `mmrds/_design/auth` | `mmria-server/model/actor/c_db_setup.cs` | 163 | Auth design doc setup |
| PUT `mmrds/_design/sortable` (CDC rebuild) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | 92 | CDC rebuild infra |
| PUT `mmrds/_design/auth` (CDC rebuild) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | 98 | CDC rebuild infra |
| PUT `mmrds/_design/sortable` (CDC populate) | `mmria.common/SharedLibraries/MMRIAServices/MMRIAServicesManager.cs` | 1016 | CDC populate infra |
| PUT `mmrds/_design/auth` (CDC populate) | `mmria.common/SharedLibraries/MMRIAServices/MMRIAServicesManager.cs` | 1019 | CDC populate infra |
| GET `mmrds/_changes` (health probe / startup check) | `mmria-server/model/actor/c_db_setup.cs` | 337 | Health / replication probe |
| GET `mmrds/_changes` (sync feed) | `mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | 220, 224 | Change-feed sync job |
| GET `mmrds/_changes` (sync feed) | `mmria-server/model/actor/quartz/Synchronize_Deleted_Case_Records.cs` | 150, 154 | Change-feed sync job |
| GET `mmrds` (database existence check) | `mmria-server/Controllers/api/healthzController.cs` | 40 | Health endpoint probe |
| GET `mmrds/_all_docs` (ID set for sync) | `mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | 170 | Sync job — ID reconciliation |
| GET `mmrds/_all_docs` (paged sync) | `mmria-server/model/actor/c_document_sync_all.cs` | 938 | Full sync job |
| GET `mmrds/_all_docs` (paged CDC sync, mmria.services) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | 202, 266, 280 | CDC populate sync job |
| GET `mmrds/_all_docs` (paged sync, PMSS) | `mmria-server/model/actor/c_document_sync_all.pmss.cs` | 241 | PMSS sync job |
| GET `mmrds/{document_id}` (per-doc fetch inside sync) | `mmria-server/model/actor/c_document_sync_all.pmss.cs` | 257 | PMSS sync job |
| GET `mmrds/_all_docs` (legacy sync) | `mmria-server/model/actor/c_document_sync_all_legacy.cs` | 167, 198 | Legacy sync job |
| GET `mmrds/{id}` (per-doc fetch inside legacy sync) | `mmria-server/model/actor/c_document_sync_all_legacy.cs` | 201, 232 | Legacy sync job |
| GET `mmrds/_all_docs` (CDC rebuild source read) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | 200 | CDC rebuild — reads all source cases |
| PUT `mmrds/{id}` (CDC rebuild — write to target) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | 241 | CDC rebuild — writes to CDC mmrds |
| GET `mmrds/{id}` / PUT `mmrds/{id}` (sync job per-case) | `mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | 108 | Sync job — per-case push |
| GET `mmrds/{id}` / PUT `mmrds/{id}` (sync job per-case) | `mmria-server/model/actor/quartz/Synchronize_Deleted_Case_Records.cs` | 115 | Sync job — per-case push |
| POST `mmrds/_compact`, POST `mmrds/_view_cleanup` | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | 300, 301 | Post-rebuild maintenance |

---

## Boundary Decisions

**Story 17.7 — Decision recorded 2026-07-14**

---

### Decision 1: `GetMmrdsDatabaseUrl` prefix bug — Option (a) Unify (fix the bug)

**Finding:** The private `GetMmrdsDatabaseUrl(DBConfigurationDetail dbInfo)` helper in `MMRIAServicesDAL` (formerly lines 553–557) produced `{url}/{prefix}_mmrds` when `prefix` is non-empty. This is a **bug**:

- Every other URL construction in the entire codebase — including both `c_document_sync_all.cs` files, all `CaseDAL` operations, and the other three Pattern A methods in `MMRIAServicesDAL` itself — uses `{prefix}mmrds` (no separator).
- The three methods that called this helper (`GetCaseIdsByDateCreated`, `GetCaseDocumentForPopulateCDC`, `GetCaseDocumentsForPopulateCDC`) are CDC populate **reads** — they read case documents FROM the tenant's `{prefix}mmrds` database (the same one as regular application CRUD). In a multi-tenant deployment where `prefix` is non-empty, they were silently targeting a non-existent `{prefix}_mmrds` database.
- The Story 17.1 catalog note claiming this was "intentional for CDC connections" was incorrect — no CouchDB database named `{prefix}_mmrds` exists or is created anywhere in the system.

**Resolution:** `GetMmrdsDatabaseUrl` deleted. The three call sites updated to `dbInfo.Get_Prefix_DB_Url("mmrds")` (Pattern B). Catalog pattern column for these rows updated from C to B.

---

### Decision 2: `c_document_sync_all` bulk reads — Option (b) Separate concerns (infrastructure-only)

**Finding:** Both `c_document_sync_all.cs` implementations perform bulk `_all_docs` enumeration of the entire `{prefix}mmrds` database to drive sync orchestration:

- `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` lines 202, 266, 280 — CDC populate sync
- `mmria-server/util/c_document_sync_all.cs` line 938 — tenant rebuild sync

Both already use the correct `{prefix}mmrds` URL (no underscore bug). These are full-database streaming operations, not individual case CRUD. They operate at the infrastructure level: enumerating all documents to drive replication, not servicing application requests.

**Resolution:** `c_document_sync_all` bulk reads are **not** routed through `ICaseRepository`. They remain as direct infrastructure calls. Future SQL migration does not require changing these files — they are not part of the `CaseDAL`/`ICaseRepository` boundary.

---

### Decision 3: `BulkSavePopulateCdcDocumentsAsync` CDC write — Option (b) Separate concerns (infrastructure-only)

**Finding:** `BulkSavePopulateCdcDocumentsAsync` (line 468) writes to `{cdcConnection.url}/mmrds/_bulk_docs` — a completely different CouchDB instance (the CDC system, not the tenant database). The `cdcConnection` parameter is a separate `DBConfigurationDetail` pointing to the CDC server. No prefix applies because the CDC system uses a flat unprefixed `mmrds` database.

**Resolution:** This is CDC populate infrastructure — it writes to the CDC system, not the tenant application database. It is **not** routed through `ICaseRepository`. Future SQL migration targets the tenant `{prefix}mmrds` database only; the CDC write path is independent.

---

### Scope summary for Epic 17

| Concern | Decision | `ICaseRepository` scope? |
|---------|----------|--------------------------|
| CDC populate reads (`GetCaseIdsByDateCreated`, `GetCaseDocumentForPopulateCDC`, `GetCaseDocumentsForPopulateCDC`) | Prefix bug fixed (option a) — but remain in `MMRIAServicesDAL`; not promoted to `ICaseRepository` | No — CDC-specific bulk reads; out of scope |
| `c_document_sync_all` bulk `_all_docs` reads | Infrastructure-only (option b) | No — sync infrastructure |
| `BulkSavePopulateCdcDocumentsAsync` CDC write | Infrastructure-only (option b) | No — writes to CDC system, not tenant DB |

| Actor | Decision | Status |
|-------|----------|--------|
| `MMRIAServicesDAL` — CDC path (`GetMmrdsDatabaseUrl`, Pattern C callers) | Does CDC populate / sync boundary belong behind `ICaseRepository`? Pattern C callers (`GetCaseDocumentForPopulateCDC`, `GetCaseDocumentsForPopulateCDC`, `GetCaseIdsByDateCreated`, `BulkSavePopulateCdcDocumentsAsync`) all target the CDC-side mmrds (no-prefix or underscore-prefix). These cannot share `CaseDAL` without adding CDC-specific config branching. | **TBD — Story 17.7** |
| `c_document_sync_all` (and `_legacy`, PMSS variants) — sync `_all_docs` + per-document GET | These bulk reads are pure sync infrastructure. They require low-level `_all_docs` paged access that `CaseDAL` does not expose. Routing through `ICaseRepository` would require a cursor/paging API not present in the current DAL contract. | **TBD — Story 17.7** (likely: explicitly excluded) |
| `Process_DB_Synchronization_Set.cs` / `Synchronize_Deleted_Case_Records.cs` — per-case GET/PUT inside Quartz sync actors | These read and write case documents as part of the de-id/report propagation job. They operate on the Quartz scheduler thread and bypass the normal HTTP request cycle. | **TBD — Story 17.7** (likely: explicitly excluded) |

---

## _users Operations

**Epic:** 18 — `_users` and `configuration` Consolidation (SQL Migration Foundation)
**Story:** 18.1
**Date:** 2026-07-14

This catalog records every distinct operation against the CouchDB `_users` database across `mmria-server` and `mmria.common`. It is the authoritative operation set for Story 18.2.

---

### In-Scope Operations

#### User CRUD (GET/PUT/DELETE by ID)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetUserByUserNameAsync` (GET user by doc ID) | `mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs` | 47 | `$"{dbConfig.url}/_users/{HtmlEncode(userDocId)}"` | `user` |
| `GetUserAsync` (GET user by user_id) | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 41 | `db_config.url + "/_users/" + user_id` | `user` |
| `CheckUserAsync` (GET user — check-exists) | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 58 | `db_config.url + "/_users/" + user_id` | `user` (empty `user` on not-found or error) |
| `PutUserAsync` (PUT user — create/update) | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 93 | `db_config.url + "/_users/" + user._id` | `document_put_response` |
| `DeleteUserAsync` (DELETE user) | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 115 | `db_config.url + "/_users/" + user_id + "?rev=" + rev` | `ExpandoObject` |

#### User List Queries (`_all_docs`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetAllUsersAsync` (paginated user list) | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 144 | `$"{db_config.url}/_users/_all_docs?include_docs=true&skip={skip}&limit={take}"` | `get_response_header<user>` |
| `GetCaseCount` (all-users read for jurisdiction summary) | `mmria-server/util/JurisdictionSummary.cs` | 268 | `$"{p_config_detail.url}/_users/_all_docs?include_docs=true&skip=1"` | `get_response_header<user>` (enumerated inline) |
| `GetCaseCount` (all-users read for VRO summary) | `mmria-server/util/VROSummary.cs` | 265 | `$"{p_config_detail.url}/_users/_all_docs?include_docs=true&skip=1"` | `get_response_header<user>` (enumerated inline) |

#### Out-of-DAL Leaking Calls (targets for Story 18.3)

These calls bypass `ManageUsersDAL` and build `_users` URLs directly in controllers. They are in-scope for cataloging and must be routed through `IUserRepository` in Story 18.2/18.3.

| Operation | Calling File(s) | Line(s) | URL Pattern | HTTP Verb | Response Type |
|-----------|----------------|---------|-------------|-----------|---------------|
| GET user (OIDC lookup by email) | `mmria-server/Controllers/AccountController.OIDC.cs` | 255 | `$"{config_couchdb_url}/_users/{Uri.EscapeDataString("org.couchdb.user:" + email)}"` | GET | `user` |
| PUT user (OIDC provision — create if not found) | `mmria-server/Controllers/AccountController.OIDC.cs` | 301 | `$"{config_couchdb_url}/_users/{Uri.EscapeDataString(user._id)}"` | PUT | `document_put_response` |
| GET user then PUT user (password change — same URL for both) | `mmria-server/Controllers/api/passwordChangeController.cs` | 137 | `db_config.url + "/_users/org.couchdb.user:" + userName` | GET then PUT | GET → `user`; PUT → `document_put_response` |

---

### `ManageUsersDAL` vs. `AccountDAL` Call Assessment (AC-4)

`ManageUsersDAL` contains 5 `_users` operations. `AccountDAL` contains 1 overlapping `_users` GET. The table below identifies which represent generic user CRUD (candidates for `IUserRepository`) versus manage-users-workflow-specific operations.

| ManageUsersDAL Method | Duplicates AccountDAL? | Assessment |
|-----------------------|------------------------|------------|
| `GetUserAsync` | Yes — mirrors `AccountDAL.GetUserByUserNameAsync` (both GET by doc ID) | Generic user CRUD — `IUserRepository` candidate |
| `CheckUserAsync` | Partial — same URL, different error semantics (never throws, returns empty) | Generic user CRUD — `IUserRepository` candidate (replace with `GetUserAsync` + null check) |
| `PutUserAsync` | No equivalent in `AccountDAL` | Generic user CRUD — `IUserRepository` candidate |
| `DeleteUserAsync` | No equivalent in `AccountDAL` | Generic user CRUD — `IUserRepository` candidate |
| `GetAllUsersAsync` | No equivalent in `AccountDAL` | Manage-users-workflow-specific (paginated admin list) — `IUserRepository` candidate with pagination |

**Summary:** All 5 `ManageUsersDAL` `_users` operations are generic user CRUD (GET, check-exists, PUT, DELETE, paginated list). None are manage-users-workflow-specific in a way that would prevent promotion to `IUserRepository`. `CheckUserAsync` is a defensive wrapper around GET that can be unified with `GetUserAsync` under a shared interface.

---

### Infrastructure / Out of Scope

These operations are not targeted by Stories 18.2–18.x. They initialize the `_users` database as part of startup or health checks.

| File | Line(s) | Operation | Reason |
|------|---------|-----------|--------|
| `mmria-server/util/c_db_setup.cs` | 116 | PUT `_users` (database creation) | One-time DB initialization — creates the `_users` database if absent |
| `mmria-server/model/actor/quartz/Check_DB_Install.cs` | 62 | PUT `_users` (database creation) | Startup health check — ensures `_users` database exists before accepting traffic |

---

### Boundary Decisions

_No boundary decisions recorded for `_users` — pending Story 18.2._
| `Process_Migrate_Charactor_to_Numeric.cs` / `Process_Migrate_Data.cs` — migration `_all_docs` + per-case PUT | One-time or rare migration scripts inside Quartz. Their direct mmrds access is intentional for bulk operations. | **TBD — Story 17.7** (likely: explicitly excluded as one-time infra) |

---

## Summary Counts

| Category | Call Sites |
|----------|-----------|
| GET case by ID (in-scope) | 25 |
| PUT case by ID (in-scope) | 17 |
| DELETE case by ID (in-scope) | 5 |
| GET at revision | 2 |
| GET all revisions | 2 |
| View queries (all views) | 28 |
| Mango `_find` | 2 |
| Bulk `_all_docs` (in-scope) | 8 |
| Bulk `_bulk_docs` (in-scope) | 1 |
| **Total in-scope call sites** | **90** |
| Infrastructure / out-of-scope call sites | 29 |

---

## Pattern A Callsites to Remediate (Stories 17.3–17.6 scope)

The following files contain Pattern A calls that are in scope for routing through `ICaseRepository` / `CaseDAL`. Stories 17.3–17.6 should eliminate all Pattern A mmrds URLs in these files.

| File | Story | Pattern A lines (approx.) |
|------|-------|--------------------------|
| `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 17.3 | 1519, 1723 (also lines 704, 900 use mixed — line 900 already B) |
| `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | 17.4 | None — all Pattern B ✓ |
| `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | 17.5 | 24, 75 |
| `mmria.common/SharedLibraries/CVS/DAL/CVSDAL.cs` | 17.5 | None — all Pattern B ✓ |
| `mmria.common/SharedLibraries/VitalImport/DAL/VitalImportDAL.cs` | 17.5 | 26, 33 |
| `mmria.common/SharedLibraries/VitalImport/Manager/VitalImportManager.cs` | 17.5 | 58, 218, 239 |
| `mmria.common/SharedLibraries/Attachment/DAL/AttachmentDAL.cs` | 17.5 | 21 |
| `mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs` | 17.6 | 104, ~113, 298, ~312, 398, ~443 |
| `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 17.7 (boundary decision) | 27, 79, 83, 189 |
| `mmria-server/Controllers/api/caseController.cs` | 17.3 (minor) | 87 (HEAD only) |
| `mmria-server/Controllers/api/caseController.pmss.cs` | 17.3 (PMSS) | None — all Pattern B ✓ |
| `mmria-server/util/VROSummary.cs` | 17.5 (server actor) | 181, 333, 544 |
| `mmria-server/util/JurisdictionSummary.cs` | 17.5 (server actor) | 337 |
| `mmria-server/Controllers/api/caseRevisionListController.cs` | 17.5 (server controller) | 48 |
| `mmria-server/Controllers/api/case_viewController.pmss.cs` | 17.5 (PMSS) | 111, 149 |
| `mmria.services/Actors/BatchProcessor.cs` | 17.5b | 512 |
| `mmria.services/Services/BatchItemProcessingService.cs` | 17.5b | 2616 |
| `mmria.services/Utilities/PagedCaseIdLoader.cs` | 17.5b | 36 |
| `mmria.services/Utilities/Exporter/core_element_exporter.cs` | 17.5b | 240, 246, 260 |
| `mmria.services/Utilities/Exporter/exporter.cs` | 17.5b | 154, 536 |
| `mmria.services/Utilities/Exporter/mmrds_exporter.cs` | 17.5b | 277 |

> **CaseDAL.cs Pattern A note:** `CaseDAL.cs` itself uses Pattern A at lines 23, 32, 46, 55 and 68. Lines 23, 32, 46, 55 are direct `{url}/{prefix}mmrds/{id}` constructions. Only line 128 uses Pattern B. Story 17.2 should harmonize CaseDAL internals to Pattern B throughout, and add any missing CRUD operations.

---

## jurisdiction Operations

**Epic:** 19 — `jurisdiction` Consolidation (SQL Migration Foundation)
**Story:** 19.1
**Date:** 2026-07-15

This catalog records every distinct operation against the `{prefix}jurisdiction` CouchDB database across `mmria-server`, `mmria.common`, and `mmria.services`. It is the authoritative operation set for Stories 19.2–19.4.

---

### URL Construction Patterns

| Label | Pattern | Example |
|-------|---------|---------|
| **A** | Hand-assembled with prefix interpolation — acceptable | `$"{db_config.url}/{db_config.prefix}jurisdiction/{id}"` |
| **B** | Uses `Get_Prefix_DB_Url` helper — preferred | `db_config.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree")` |
| **C** | Missing prefix — **wrong in multi-tenant** | `$"{db_config.url}/jurisdiction/{id}"` |

---

### Two-Interface Design

| Interface | Purpose |
|-----------|---------|
| `IJurisdictionRepository` | Full application CRUD — user-role-jurisdiction documents, jurisdiction tree, form access, pinned cases, admin sortable views |
| `IJurisdictionAuthorizationReader` | High-frequency read-only auth lookups — runs on every authorized request — `by_user_id` view only |

---

### In-Scope Operations

#### User-Role-Jurisdiction Document CRUD

Individual `user_role_jurisdiction` documents represent a single user–role–folder assignment.

##### GET by ID

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Interface |
|-----------|----------------|---------|-------------|---------------|-----------|
| `GetUserRoleJurisdictionAsync` | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 120 | A | `user_role_jurisdiction` | `IJurisdictionRepository` |

##### PUT (create/update)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Interface |
|-----------|----------------|---------|-------------|---------------|-----------|
| `PutUserRoleJurisdictionAsync` | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 133 | A | `document_put_response` | `IJurisdictionRepository` |

##### DELETE

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Interface |

---

## metadata Operations

**Epic:** 20 — `metadata` Consolidation (SQL Migration Foundation)
**Story:** 20.1
**Date:** 2026-07-15

This catalog records every distinct operation against the `metadata` CouchDB database across `mmria-server`, `mmria.common`, and `mmria.services`. It is the authoritative operation set for Stories 20.2–20.6.

> **Note:** Unlike `mmrds`, `audit`, and `jurisdiction`, the `metadata` database is **global** (not per-tenant prefixed) in the vast majority of call sites. Two exceptions in `mmria.services` (`broadcastMessageController`, `systemOfflineController`) apply `{prefix}metadata/…` — documented below as Pattern B.

---

### URL Construction Patterns

| Label | Pattern | Example |
|-------|---------|---------|
| **A** | Direct assembly — no prefix — correct for this global database | `$"{db_config.url}/metadata/{id}"` |
| **B** | Prefix-qualified — used in `mmria.services` for two singleton documents | `$"{p_config_detail.url}/{p_config_detail.prefix}metadata/broadcast-message-list"` |

---

### In-Scope Operations

#### Version Specification CRUD

The version specification is the core MMRIA form definition. Two URL shapes exist:
- `metadata/version_specification-{ver}/metadata` — the **app document** (`mmria.common.metadata.app`), the full field-tree used at runtime
- `metadata/version_specification-{ver}` — the **envelope** (`Version_Specification`), the metadata record (publish status, dates, etc.)

##### GET app document — `metadata/version_specification-{ver}/metadata`

The most-called metadata operation in the codebase. Every rebuild, sync, export, and import path that needs the form schema calls this URL directly without going through `MetadataVersionManager`.

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetAppMetadataAsync` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 49 | A | `mmria.common.metadata.app` |
| `GetMetadataAsync` (AuditRecovery) | `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | 46 | A | `mmria.common.metadata.app` |
| `Execute` | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_convert_to_dqr_detail.cs` | 55 | A | `app` |
| `Execute` | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_convert_to_opioid_report_object.cs` | 338 | A | `app` |
| `Execute` | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_convert_to_report_object.cs` | 142 | A | `app` |
| `Execute` (sync legacy) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | 171 | A | `app` |
| `Execute` (freq summary) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_generate_frequency_summary_report.cs` | 187 | A | `app` |
| `Execute` (CDC rebuild) | `mmria.services/Actors/populate-cdc-instance/c_convert_to_dqr_detail.cs` | 55 | A | `app` |
| `Execute` (CDC rebuild) | `mmria.services/Actors/populate-cdc-instance/c_convert_to_opioid_report_object.cs` | 333 | A | `app` |
| `Execute` (CDC rebuild) | `mmria.services/Actors/populate-cdc-instance/c_convert_to_report_object.cs` | 137 | A | `app` |
| `Execute` (CDC sync) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | 154 | A | `app` |
| `Execute` (CDC freq summary) | `mmria.services/Actors/populate-cdc-instance/c_generate_frequency_summary_report.cs` | 157 | A | `app` |
| `ProcessBatchItemAsync` (vitals import) | `mmria.services/Services/BatchItemProcessingService.cs` | 790 | A | `app` |
| `Execute` (core element export) | `mmria.services/Utilities/CoreElementExport/core_element_exporter.cs` | 130 | A | `app` |
| `Execute` (standard export) | `mmria.services/Utilities/Exporter/exporter.cs` | 157 | A | `app` |
| `Execute` (mmrds export) | `mmria.services/Utilities/Exporter/mmrds_exporter.cs` | 157 | A | `app` |
| `Execute` | `mmria-server/util/c_convert_to_dqr_detail.cs` | 55 | A | `app` |
| `Execute` | `mmria-server/util/c_convert_to_opioid_report_object.cs` | 338 | A | `app` |
| `Execute` | `mmria-server/util/c_convert_to_report_object.cs` | 142 | A | `app` |
| `Execute` (sync server) | `mmria-server/util/c_document_sync_all.cs` | 223 | A | `app` |
| `Execute` (freq summary server) | `mmria-server/util/c_generate_frequency_summary_report.cs` | 187 | A | `app` |
| `Execute` (sync document server) | `mmria-server/util/c_sync_document.cs` | 213 | A | `app` |
| `Execute` (core element export server) | `mmria-server/util/core_element_export/core_element_exporter.cs` | 127 | A | `app` |

##### GET app document — `metadata/{version}/metadata` (alternate shape, no `version_specification-` prefix)

Used by the legacy `export_all_generate_name_map` utility. The `{version}` parameter is the raw value passed in by the caller — callers may already include `version_specification-` in the string, making this equivalent to the standard shape. Confirm before routing through `IMetadataRepository`.

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `Execute` (name map) | `mmria.services/Utilities/Exporter/export_all_generate_name_map.cs` | 50 | A | `app` |
| `Execute` (name map server) | `mmria-server/util/exporter/export_all_generate_name_map.cs` | 52 | A | `app` |

##### GET Version_Specification envelope — `metadata/version_specification-{ver}`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetVersionSpecificationMetadataAsync` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 143 | A | `Version_Specification` |
| `SaveVersionSpecificationAsync` (pre-save existence check) | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 174 | A | `Version_Specification` |
| `SaveVersionAttachmentAsync` (pre-save existence check) | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 232 | A | `Version_Specification` |

##### PUT Version_Specification document

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `SaveMetadataVersionSpecificationAsync` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 108 | A | `document_put_response` |
| `SaveVersionSpecificationAsync` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 210 | A | `document_put_response` |

---

#### Default Metadata Document CRUD — `metadata/2016-06-12T13:49:24.759Z`

`DefaultMetadataId = "2016-06-12T13:49:24.759Z"` is the root MMRIA app document — the legacy default form schema that predates the version_specification pattern.

##### GET metadata document by ID

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetMetadataAsync()` (no-arg overload) | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 33 | A | `ExpandoObject` |
| `GetMetadataAsync(string id, …)` (by-ID overload) | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 41 | A | `ExpandoObject` |

##### PUT metadata document

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `SaveMetadataAsync` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 57 | A | `document_put_response` |

##### GET revision (HEAD probe for `_rev`)

Used before attachment PUTs that require `If-Match: {rev}`.

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `SaveCheckCodeAsync` (pre-write rev fetch) | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 73 | A | rev `string` |
| `SaveValidatorAsync` (pre-write rev fetch) | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 345 | A | rev `string` |

---

#### Attachment Reads and Writes

All attachments belong to `DefaultMetadataId` or a version specification document.

##### GET attachment

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetCheckCodeAsync` — `metadata/{DefaultId}/mmria-check-code.js` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 63 | A | `string` (JS text) |
| `GetValidatorAsync` — `metadata/{DefaultId}/validator.js` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 151 | A | `string` (JS text) |
| `GetVersionDocumentAsync` — `metadata/version_specification-{id}/{doc_name}` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 159 | A | `string` |

##### PUT attachment

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `SaveCheckCodeAsync` — `metadata/{DefaultId}/mmria-check-code.js` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 93 | A | `document_put_response` |
| `SaveValidatorAsync` — `metadata/{DefaultId}/validator.js` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 365 | A | `document_put_response` |
| `SaveVersionAttachmentAsync` — `metadata/{id}/{doc_name}` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 264 | A | `document_put_response` |

---

#### De-Identification List Reads

Two distinct documents: `de-identified-list` (tenant rebuild/sync de-id) and `de-identified-export-list` (CDC export de-id). Both are heavily read across rebuild, sync, and export paths and are read-only from most callers.

##### GET `metadata/de-identified-list`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `Execute` (rebuild de-id common) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_de_identifier.cs` | 74 | A | `ExpandoObject` |
| `Execute` (sync legacy common) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | 174 | A | `ExpandoObject` |
| `Execute` (CDC rebuild de-id) | `mmria.services/Actors/populate-cdc-instance/c_de_identifier.cs` | 46 | A | `ExpandoObject` |
| `Execute` (CDC sync) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | 157 | A | `ExpandoObject` |
| `Execute` (core element export services) | `mmria.services/Utilities/CoreElementExport/core_element_exporter.cs` | 211 | A | `ExpandoObject` |
| `Execute` (de-id server) | `mmria-server/util/c_de_identifier.cs` | 74 | A | `ExpandoObject` |
| `Execute` (sync server) | `mmria-server/util/c_document_sync_all.cs` | 226 | A | `ExpandoObject` |
| `Execute` (sync document server) | `mmria-server/util/c_sync_document.cs` | 225 | A | `ExpandoObject` |
| `Execute` (core element export server) | `mmria-server/util/core_element_export/core_element_exporter.cs` | 209 | A | `ExpandoObject` |
| `Get` (controller — `id` absent or not "export") | `mmria-server/Controllers/api/de_identified_listController.cs` | 54 | A | `ExpandoObject` |

##### PUT `metadata/de-identified-list`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `Post` (controller — `id` absent or not "export") | `mmria-server/Controllers/api/de_identified_listController.cs` | 107 | A | `document_put_response` |

##### GET `metadata/de-identified-export-list`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetDeIdentifiedExportListPathMapAsync` | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 378 | A | `Dictionary<string, HashSet<string>>` |
| `Execute` (CDC de-id common helper) | `mmria.common/SharedLibraries/MMRIAServices/Helper/c_cdc_de_identifier.cs` | 42 | A | `ExpandoObject` |
| `Execute` (CDC de-id services) | `mmria.services/Actors/populate-cdc-instance/c_cdc_de_identifier.cs` | 30 | A | `ExpandoObject` |
| `Execute` (CDC de-id server) | `mmria-server/util/c_cdc_de_identifier.cs` | 69 | A | `ExpandoObject` |
| `Get` (controller — `id` == "export") | `mmria-server/Controllers/api/de_identified_listController.cs` | 54 | A | `ExpandoObject` |

##### PUT `metadata/de-identified-export-list`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `Post` (controller — `id` == "export") | `mmria-server/Controllers/api/de_identified_listController.cs` | 107 | A | `document_put_response` |

---

#### UI Specification CRUD

All UI specification operations are behind `MetadataVersionManager` — no out-of-DAL callers.

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `ListUiSpecificationsAsync` — GET `metadata/_all_docs?include_docs=true` (filtered to `data_type == "ui-specification"`) | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 277 | A | `List<UI_Specification>` |
| `GetUiSpecificationAsync` — GET `metadata/{id}` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 306 | A | `UI_Specification` |
| `SaveUiSpecificationAsync` — PUT `metadata/{ui_specification._id}` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 320 | A | `document_put_response` |
| `DeleteUiSpecificationAsync` — DELETE `metadata/{id}?rev={rev}` | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 337 | A | `ExpandoObject` |

---

#### Broadcast / Offline / PopulateCDC Config Document CRUD

These are administrative singleton documents stored in `metadata`.

##### `metadata/broadcast-message-list`

> ⚠️ **Prefix inconsistency:** `mmria-server`'s `broadcast_messageController` uses `metadata/broadcast-message-list` (no prefix — Pattern A). `mmria.services`'s `broadcastMessageController` uses `{prefix}metadata/broadcast-message-list` (Pattern B). In single-tenant mode these are equivalent. In multi-tenant mode the services layer writes to a per-tenant `{prefix}metadata` database while the server writes to the global `metadata` database. This is a boundary decision item for Story 20.6.

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `LoadBroadcastMessageListAsync` (GET) | `mmria-server/Controllers/broadcast_messageController.cs` | 193 | A | `BroadcastMessageList` |
| `save_request` (PUT) | `mmria-server/Controllers/broadcast_messageController.cs` | 140 | A | `document_put_response` |
| `get_existing_document` (GET) | `mmria.services/Controllers/broadcastMessageController.cs` | ~165 | B | `BroadcastMessageList` |
| `UpdateBroadcastMessage` (PUT) | `mmria.services/Controllers/broadcastMessageController.cs` | 106 | B | `document_put_response` |

##### `metadata/populate-cdc-instance`

> ⚠️ **Duplicate DAL:** `PopulateCDCInstanceSupervisor.cs` builds direct URLs that duplicate `MMRIAServicesDAL.cs`. Both must be consolidated under `IMetadataRepository`.

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetPopulateCDCInstanceDocumentAsync` (GET) | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 459 | A | `Populate_CDC_Instance` |
| `SavePopulateCDCInstanceDocumentAsync` (PUT) | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 468 | A | `document_put_response` |
| `GetPopulate` (GET — duplicate of above) | `mmria.services/Actors/populate-cdc-instance/PopulateCDCInstanceSupervisor.cs` | 383 | A | `Populate_CDC_Instance` |
| `SavePopulate` (PUT — duplicate of above) | `mmria.services/Actors/populate-cdc-instance/PopulateCDCInstanceSupervisor.cs` | 405 | A | `document_put_response` |

##### `metadata/system-offline-config`

> ℹ️ `mmria-server`'s `system_offlineController` delegates to `SystemOfflineDAL` → `mmria.services` HTTP API — **no direct CouchDB call from mmria-server**. Only `mmria.services.Controllers.systemOfflineController` hits CouchDB directly.
>
> ⚠️ Uses Pattern B (`{cdcConfig.prefix}metadata/system-offline-config`). Since `system-offline-config` is a CDC-level document and the CDC prefix is typically empty, this resolves to `metadata/system-offline-config` in practice.

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetSystemOfflineConfig` (GET) | `mmria.services/Controllers/systemOfflineController.cs` | 34 | B | `SystemOfflineConfig` |
| `SaveSystemOfflineConfig` — GET existing rev | `mmria.services/Controllers/systemOfflineController.cs` | ~85 | B | `SystemOfflineConfig` |
| `SaveSystemOfflineConfig` (PUT) | `mmria.services/Controllers/systemOfflineController.cs` | ~129 | B | `document_put_response` |

---

#### Export List and Substance Mapping CRUD

##### `metadata/export-standard-list`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `Get` (controller GET) | `mmria-server/Controllers/api/export_list_managerController.cs` | 41 | A | `ExpandoObject` |
| `Post` (controller PUT) | `mmria-server/Controllers/api/export_list_managerController.cs` | 83 | A | `document_put_response` |
| `Execute` (standard export — read) | `mmria.services/Utilities/Exporter/exporter.cs` | 168 | A | `StandardReportList` |

##### `metadata/substance-mapping`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `Get` (controller GET) | `mmria-server/Controllers/api/substance_mappingController.cs` | 45 | A | `Substance_Mapping` |
| `Post` (controller PUT) | `mmria-server/Controllers/api/substance_mappingController.cs` | 81 | A | `document_put_response` |

##### `metadata/duplicate-multiform-list`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetDuplicateMultiFormList` | `mmria-server/Controllers/abstractorDeidentifiedCaseController.cs` | 70 | A | `DuplicateMultiformResult` |
| `GetDuplicateMultiFormList` | `mmria-server/Controllers/CaseController.cs` | 141 | A | `DuplicateMultiformResult` |

---

#### Case Validation Rules CRUD — `metadata/case-validation-rules`

Added in Epic 6 (Story 6.1). The document lives in `metadata` database.

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetRuleDocumentAsync` (GET) | `mmria.common/SharedLibraries/CaseValidation/DAL/CaseValidationDAL.cs` | 26 | A | `CaseValidationRuleDocument` |
| `SaveRuleDocumentAsync` (PUT) | `mmria.common/SharedLibraries/CaseValidation/DAL/CaseValidationDAL.cs` | 50 | A | `document_put_response` |

---

#### Bulk Reads (`_all_docs`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `ListVersionSpecificationsAsync` — GET `metadata/_all_docs?include_docs=true` (filtered to `data_type == "version-specification"`) | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 114 | A | `List<Version_Specification>` |
| `ListUiSpecificationsAsync` — GET `metadata/_all_docs?include_docs=true` (filtered to `data_type == "ui-specification"`) | `mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs` | 277 | A | `List<UI_Specification>` |

> ℹ️ Both `_all_docs` calls already go through `MetadataVersionManager` — no callsite remediation needed for these.

---

### Infrastructure / Out of Scope

The following `metadata` operations appear in DB setup and one-time migration scripts. They are **not** in scope for `IMetadataRepository`.

| Operation | Calling File(s) | Line(s) | Notes |
|-----------|----------------|---------|-------|
| `PUT metadata/_security` | `mmria-server/util/c_db_setup.cs` | 483 | DB initialization |
| `PUT metadata/_design/auth` | `mmria-server/util/c_db_setup.cs` | 489 | DB initialization |
| `PUT metadata/{DefaultMetadataId}` (seed) | `mmria-server/util/c_db_setup.cs` | 496 | DB initialization |
| `PUT metadata/{DefaultMetadataId}/mmria-check-code.js` (seed) | `mmria-server/util/c_db_setup.cs` | 506 | DB initialization |
| `PUT metadata/{DefaultMetadataId}/validator.js` (seed) | `mmria-server/util/c_db_setup.cs` | 525 | DB initialization |
| `PUT metadata/_design/sortable` | `mmria-server/util/c_db_setup.cs` | 541 | DB initialization |
| `PUT metadata/default_ui_specification` (seed) | `mmria-server/util/c_db_setup.cs` | 548 | DB initialization |
| `PUT metadata/{id}` (version spec seed loop) | `mmria-server/util/c_db_setup.cs` | 586 | DB initialization — seeds version specs from JSON files |
| `PUT metadata/de-identified-list` (seed) | `mmria-server/util/c_db_setup.cs` | 611 | DB initialization |
| `GET metadata/{p_id}` (migration plan read) | `mmria-server/model/actor/quartz/Process_Migrate_Data.cs` | 186 | One-time migration script |
| `GET metadata/{DefaultMetadataId}` (migration read) | `mmria-server/model/actor/quartz/Process_Migrate_Charactor_to_Numeric.cs` | 51 | One-time migration script |

---

### Summary Counts (metadata)

| Category | Distinct Operations | Call Sites | Notes |
|----------|--------------------|-----------:|-------|
| Version spec app doc GET (`version_specification-{v}/metadata`) | 1 | 23 | Highest-volume; all direct CouchDB — primary 20.3–20.5 target |
| Version spec app doc GET (alternate `{v}/metadata` shape) | 1 | 2 | `export_all_generate_name_map` — confirm URL value before routing |
| Version spec envelope GET | 3 | 3 | `MetadataVersionManager` only |
| Version spec PUT | 2 | 2 | `MetadataVersionManager` only |
| Default metadata document GET/PUT/rev | 4 | 4 | `MetadataVersionManager` only |
| Attachment GET | 3 | 3 | `MetadataVersionManager` only |
| Attachment PUT | 3 | 3 | `MetadataVersionManager` only |
| de-identified-list GET/PUT | 2 | 11 | Spread across rebuild/sync/export — 20.3 + 20.5 targets |
| de-identified-export-list GET/PUT | 2 | 6 | CDC export paths — 20.5 targets |
| UI specification CRUD | 4 | 4 | `MetadataVersionManager` only |
| broadcast-message-list GET/PUT | 2 | 4 | Prefix inconsistency — boundary decision item for 20.6 |
| populate-cdc-instance GET/PUT | 2 | 4 | `MMRIAServicesDAL` + duplicate in `PopulateCDCInstanceSupervisor` |
| system-offline-config GET/PUT | 2 | 3 | `mmria.services` only; server uses HTTP relay |
| export-standard-list GET/PUT | 2 | 3 | Controller + exporter |
| substance-mapping GET/PUT | 2 | 2 | Controller only |
| duplicate-multiform-list GET | 1 | 2 | Two controllers |
| case-validation-rules GET/PUT | 2 | 2 | `CaseValidationDAL` (Epic 6) |
| Bulk `_all_docs` | 1 (2 filtered uses) | 2 | `MetadataVersionManager` only — no remediation needed |
| **In-scope total** | **~38** | **~84** | |
| Out-of-scope (DB setup + migrations) | — | 11 | `c_db_setup.cs` + `Process_Migrate_*` |

---

### Key Observations for Stories 20.2–20.6

1. **`MetadataVersionManager` is already the intended DAL** for version specs, attachments, and UI specs — it routes through `MetadataVersionDAL`. Story 20.2 canonicalizes this into `IMetadataRepository`.

2. **Version spec app document** (`metadata/version_specification-{ver}/metadata`) has **23 out-of-DAL call sites** spread across both repos — the single largest remediation target. These are the primary 20.3–20.5 targets.

3. **`AuditRecoveryDAL`** bypasses `MetadataVersionManager` to read `version_specification-{ver}/metadata` directly — in-scope for Story 20.3 (SharedLibraries DAL routing).

4. **`CaseValidationDAL`** goes directly to CouchDB for `case-validation-rules` — in-scope for Story 20.3.

5. **Duplicate `populate-cdc-instance`** calls in `PopulateCDCInstanceSupervisor.cs` duplicate `MMRIAServicesDAL` — must consolidate; assign to 20.3 or 20.5 depending on which layer owns it.

6. **Prefix inconsistency** for `broadcast-message-list` (server: no prefix; services: prefix-qualified) — boundary decision for Story 20.6.

7. **`_all_docs` bulk reads** are already correctly behind `MetadataVersionManager` — no callsite remediation required.

8. **`export_all_generate_name_map`** uses `metadata/{version}/metadata` without the `version_specification-` prefix — confirm what value is passed as `{version}` before routing through `IMetadataRepository`.
|-----------|----------------|---------|-------------|---------------|-----------|
| `DeleteUserRoleJurisdictionAsync` | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 143 | A | `document_put_response` | `IJurisdictionRepository` |

##### GET all docs (`_all_docs`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Interface |
|-----------|----------------|---------|-------------|---------------|-----------|
| `GetAllUserRoleJurisdictionsAsync` | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 111 | A | `get_response_header<user_role_jurisdiction>` | `IJurisdictionRepository` |

##### Bulk write (`_bulk_docs`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Interface |
|-----------|----------------|---------|-------------|---------------|-----------|
| `BulkUpsertUserRoleJurisdictionsAsync` | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 94 | A | `List<document_put_response>` | `IJurisdictionRepository` |

---

#### Jurisdiction Tree Document CRUD

The `jurisdiction_tree` document (`_id = "jurisdiction_tree"`) stores the tenant's entire jurisdiction folder hierarchy as a single well-known document.

##### GET jurisdiction_tree

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Interface |
|-----------|----------------|---------|-------------|---------------|-----------|
| `GetJurisdictionTreeAsync` | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 158 | B | `jurisdiction_tree` | `IJurisdictionRepository` |
| `Get()` | `mmria-server/Controllers/api/jurisdiction_treeController.cs` | 55 | B | `jurisdiction_tree` | `IJurisdictionRepository` |
| `GetJurisdictionTree()` (new_case_folder endpoint) | `mmria-server/Controllers/api/jurisdiction_treeController.cs` | 79 | B | `jurisdiction_tree` | `IJurisdictionRepository` |
| `GetCurrentJurisdictionTreeAsync()` (private pre-write read) | `mmria-server/Controllers/api/jurisdiction_treeController.cs` | 240 | B | `jurisdiction_tree` | `IJurisdictionRepository` |
| `GetJurisdictionTree(j)` (single-tenant branch) | `mmria-server/Controllers/vitalsController.cs` | 80, 83 | C then A (conditional) | `jurisdiction_tree` | `IJurisdictionRepository` |
| `GetJurisdictionTree(j)` (multi-tenant branch) | `mmria-server/Controllers/vitalsController.cs` | 113, 116 | C then A (conditional) | `jurisdiction_tree` | `IJurisdictionRepository` |

> **vitalsController.cs prefix bug:** Both `GetJurisdictionTree` actions assign Pattern C first (`$"{url}/jurisdiction/jurisdiction_tree"`), then conditionally overwrite with Pattern A when `prefix` is non-empty. In true single-tenant (empty prefix) the C-path is correct; in multi-tenant the override fires. Story 19.4 should replace both with `Get_Prefix_DB_Url`.

##### PUT jurisdiction_tree

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Interface |
|-----------|----------------|---------|-------------|---------------|-----------|
| `Post()` (save jurisdiction tree) | `mmria-server/Controllers/api/jurisdiction_treeController.cs` | 135 | B | `document_put_response` | `IJurisdictionRepository` |

---

#### Form Access List Document

The `form-access-list` document (`_id = "form-access-list"`) stores per-form role access specifications.

##### GET form-access-list

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Interface |
|-----------|----------------|---------|-------------|---------------|-----------|
| `GetFormAccessAsync` | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 178 | B | `FormAccessSpecification` | `IJurisdictionRepository` |
| `LoadFormAccessSpecificationAsync()` (private) | `mmria-server/Controllers/_usersController.cs` | 141 | B | `FormAccessSpecification` | `IJurisdictionRepository` |

##### PUT form-access-list

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Interface |
|-----------|----------------|---------|-------------|---------------|-----------|
| `SaveFormAccessAsync` | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 191 | B | `document_put_response` | `IJurisdictionRepository` |
| `Post()` (save form access) | `mmria-server/Controllers/_usersController.cs` | 207 | B | `document_put_response` | `IJurisdictionRepository` |

---

#### Pinned Case Set Document

The `pinned-case-set` document (`_id = "pinned-case-set"`) stores per-user and shared pinned case IDs for the case list view.

##### GET pinned-case-set

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Interface |
|-----------|----------------|---------|-------------|---------------|-----------|
| `LoadPinnedCaseSetAsync()` (single-tenant early-exit branch) | `mmria.common/SharedLibraries/CaseView/CaseViewManager.cs` | 1253 | C | `pinned_case_set` | `IJurisdictionRepository` |
| `GetPagedViewAsync` (pinned case branch) | `mmria.common/SharedLibraries/CaseView/CaseViewManager.cs` | 1408–1409 | B/C (conditional) | `pinned_case_set` | `IJurisdictionRepository` |
| `GetCaseViewAsync` (pinned case branch) | `mmria.common/SharedLibraries/CaseView/CaseViewManager.cs` | 1453–1454 | B/C (conditional) | `pinned_case_set` | `IJurisdictionRepository` |
| `GetPinnedCaseSetAsync` (PMSS) | `mmria-server/util/CaseViewSearch.pmss.cs` | 2293 | C | `pinned_case_set` | `IJurisdictionRepository` |

> **CaseViewManager.cs prefix bug:** Line 1253 unconditionally uses Pattern C (no prefix). Lines 1408–1409 and 1453–1454 use `Get_Prefix_DB_Url` when prefix is non-empty but fall back to Pattern C otherwise. All four call sites should use `Get_Prefix_DB_Url` unconditionally. Story 19.4 must fix these.

---

#### Authorization View Queries (Hot Path — `IJurisdictionAuthorizationReader`)

All queries against `jurisdiction/_design/sortable/_view/by_user_id`. Read-only. Execute on every authorized HTTP request via the auth middleware stack.

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Filter |
|-----------|----------------|---------|-------------|---------------|--------|
| `LoadActiveUserRoleJurisdictions` (per-user, keyed) | `mmria.common/SharedLibraries/Other/authorization.cs` | 260 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | `?startkey={u}&endkey={u}` |
| `LoadUserJurisdictionSet` (whole-tenant, no filter) | `mmria.common/utils/authorization_case.cs` | 217 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | None |
| `is_authorized_to_handle_jurisdiction_id` (per-user) | `mmria-server/util/authorization_user.cs` | 33 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | `?{p_user.name}` |
| `GetUserRoleJurisdictions` (per-user filter) | `mmria-server/util/authorization_user.cs` | 139 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | `?{user_name}` |
| `LoadActiveUserRoleJurisdictions` (PMSS, per-user) | `mmria-server/util/authorization.pmss.cs` | 235 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | `?startkey={u}&endkey={u}` |
| `LoadUserJurisdictionSet` (PMSS, whole-tenant) | `mmria-server/util/authorization_case.pmss.cs` | 154 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | None |
| `is_authorized_to_handle_jurisdiction_id` (PMSS, per-user) | `mmria-server/util/authorization_user.pmss.cs` | 31 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | `?{p_user.name}` |
| `GetUserRoleJurisdictions` (PMSS, per-user) | `mmria-server/util/authorization_user.pmss.cs` | 162 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | `?{user_name}` |
| `HandleRequirementAsync` (custom authz handler — stub, not actively registered) | `mmria-server/util/JurisdictionAuthorizationRequirement.cs` | 41 | A | `get_response_header<jurisdiction_view_sortable_item>` | None — uses POST |
| `get_current_user_role_jurisdiction_set_for` (services, whole-tenant) | `mmria.services/Utilities/authorization.cs` | 57, 170, 282 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | None |

> **`JurisdictionAuthorizationRequirement.cs` note:** This handler uses `POST` (line 41) rather than `GET`. The body of `HandleRequirementAsync` calls `context.Succeed(requirement)` unconditionally without inspecting the view response, making it a stub. It does not appear to be actively registered in the ASP.NET authorization pipeline. Cataloged for completeness; Story 19.3 should evaluate whether to remove it or wire it properly.

> **`AuthorizationRoleCache.cs` note:** This class is a 5-second TTL in-process cache that sits in front of the `by_user_id` view calls. It performs no CouchDB operations itself — it calls `loader()` (which hits CouchDB) on cache miss. No separate catalog entry; it is a performance wrapper around the `LoadActiveUserRoleJurisdictions` and `LoadUserJurisdictionSet` entries above.

---

#### Admin and Session Sortable View Queries (`IJurisdictionRepository`)

These read `user_role_jurisdiction` (and session) documents via sortable design-doc views for manage-users workflows and admin summary reporting.

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Sort View | Interface |
|-----------|----------------|---------|-------------|---------------|-----------|-----------|
| `GetSessionSortableViewAsync` (session listing) | `mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs` | 31 | A | `get_sortable_view_reponse_header<session>` | Dynamic (`{sortView}`) | `IJurisdictionRepository` |
| `IsAuthorizedToDeleteUserAsync` (URJ lookup) | `mmria.common/SharedLibraries/ManageUsers/Manager/ManageUsersManager.cs` | 313 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | `by_user_id` | `IJurisdictionRepository` |
| User listing (URJ by date) | `mmria.common/SharedLibraries/ManageUsers/Manager/ManageUsersManager.cs` | 405 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | `by_date_created` | `IJurisdictionRepository` |
| Dynamic sorted URJ listing | `mmria.common/SharedLibraries/ManageUsers/Manager/ManageUsersManager.cs` | 483 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | Dynamic (`{sort_view}`) | `IJurisdictionRepository` |
| User delete authorization (URJ by user name) | `mmria.common/SharedLibraries/ManageUsers/Manager/ManageUsersManager.cs` | 710 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | `by_user_id` | `IJurisdictionRepository` |
| `GetJurisdictions` (admin summary — role counts) | `mmria-server/util/JurisdictionSummary.cs` | 431 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | `by_date_created` | `IJurisdictionRepository` |
| `GetJurisdictions` (VRO summary — role counts) | `mmria-server/util/VROSummary.cs` | 429 | A | `get_sortable_view_reponse_header<user_role_jurisdiction>` | `by_date_created` | `IJurisdictionRepository` |

> **`SessionDAL.cs` note:** `GetSessionSortableViewAsync` queries the `jurisdiction` database with a dynamic sort view and deserializes to `get_sortable_view_reponse_header<session>`. In MMRIA, `user_role_jurisdiction` and `session` documents coexist in the `jurisdiction` database; the `_design/sortable` design document provides views for both types. This is expected — not a routing error.

> **`ManageUsersManager.cs` note:** Lines 313, 405, 483, and 710 build CouchDB view URLs directly in the Manager layer and pass them to `ManageUsersDAL.GetUserRoleJurisdictionSortableViewAsync(url, config)`, which takes the pre-built URL as a parameter. This is an out-of-DAL URL-construction pattern. Story 19.4 should move URL construction entirely into `JurisdictionDAL`.

---

### Infrastructure / Out of Scope

These operations initialize the `jurisdiction` database during server startup. They are not application CRUD and are not targeted by Stories 19.2–19.4.

| File | Line(s) | Operation | Reason |
|------|---------|-----------|--------|
| `mmria-server/util/c_db_setup.cs` | 432 | PUT `jurisdiction/_design/sortable` | Design doc initialization — creates view indexes |
| `mmria-server/util/c_db_setup.cs` | 439 | PUT `jurisdiction/_design/auth` | Design doc initialization — creates auth security index |

---

### Summary Counts

| Category | Call Sites |
|----------|-----------|
| User-role-jurisdiction GET by ID | 1 |
| User-role-jurisdiction PUT | 1 |
| User-role-jurisdiction DELETE | 1 |
| User-role-jurisdiction GET all docs | 1 |
| User-role-jurisdiction bulk write (`_bulk_docs`) | 1 |
| Jurisdiction tree GET | 6 |
| Jurisdiction tree PUT | 1 |
| Form access list GET | 2 |
| Form access list PUT | 2 |
| Pinned case set GET | 4 |
| Auth `by_user_id` view queries (`IJurisdictionAuthorizationReader`) | 10 |
| Admin/session sortable view queries (`IJurisdictionRepository`) | 7 |
| **Total in-scope call sites** | **37** |
| Infrastructure / out-of-scope | 2 |

---

### Out-of-DAL Callsites to Remediate (Stories 19.3–19.4 scope)

| File | Story | Operations | Prefix Bug? |
|------|-------|------------|-------------|
| `mmria-server/Controllers/api/jurisdiction_treeController.cs` | 19.4 | GET/PUT `jurisdiction_tree` (Pattern B — no URL change needed) | No |
| `mmria-server/Controllers/vitalsController.cs` | 19.4 | GET `jurisdiction_tree` (Pattern C/A conditional) | **Yes** |
| `mmria-server/Controllers/_usersController.cs` | 19.4 | GET/PUT `form-access-list` (Pattern B — no URL change needed) | No |
| `mmria.common/SharedLibraries/CaseView/CaseViewManager.cs` | 19.4 | GET `pinned-case-set` (Pattern C at line 1253; B/C at lines 1408–1409, 1453–1454) | **Yes** |
| `mmria-server/util/CaseViewSearch.pmss.cs` | 19.4 | GET `pinned-case-set` (Pattern C) | **Yes** |
| `mmria.common/SharedLibraries/ManageUsers/Manager/ManageUsersManager.cs` | 19.4 | GET sortable views — builds URL in Manager (Pattern A, delegates to DAL) | No |
| `mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs` | 19.4 | GET sortable view (Pattern A — URL built in DAL already) | No |
| `mmria-server/util/JurisdictionSummary.cs` | 19.4 | GET sortable view (Pattern A) | No |
| `mmria-server/util/VROSummary.cs` | 19.4 | GET sortable view (Pattern A) | No |
| `mmria.common/SharedLibraries/Other/authorization.cs` | 19.3 | GET `by_user_id` (Pattern A) | No |
| `mmria.common/utils/authorization_case.cs` | 19.3 | GET `by_user_id` (Pattern A, whole-tenant) | No |
| `mmria-server/util/authorization_user.cs` | 19.3 | GET `by_user_id` (Pattern A, ×2) | No |
| `mmria-server/util/authorization.pmss.cs` | 19.3 | GET `by_user_id` (Pattern A) | No |
| `mmria-server/util/authorization_case.pmss.cs` | 19.3 | GET `by_user_id` (Pattern A, whole-tenant) | No |
| `mmria-server/util/authorization_user.pmss.cs` | 19.3 | GET `by_user_id` (Pattern A, ×2) | No |
| `mmria-server/util/JurisdictionAuthorizationRequirement.cs` | 19.3 | POST `by_user_id` (Pattern A — stub, evaluate for removal) | No |
| `mmria.services/Utilities/authorization.cs` | 19.3 | GET `by_user_id` (Pattern A, ×3) | No |

---

## `audit` Operations

**Epic:** 21 — `audit` Consolidation (SQL Migration Foundation)
**Story:** 21.1
**Date:** 2026-07-15

This catalog records every distinct operation against the `{prefix}audit` CouchDB database across `mmria-server` and `mmria.common`. It is the authoritative operation set for Stories 21.2–21.6.

---

### URL Construction Patterns

| Label | Pattern | Example |
|-------|---------|---------|
| **A** | Hand-assembled with string interpolation — **wrong** | `$"{db_config.url}/{db_config.prefix}audit/{id}"` |
| **B** | Uses `Get_Prefix_DB_Url` helper — **correct** | `dbConfig.Get_Prefix_DB_Url($"audit/{id}")` |

---

### In-Scope Operations

#### Audit Entry Writes (PUT `Change_Stack`)

These calls create a new `Change_Stack` document (an audit trail entry) in the `audit` database. The canonical home is a new `AuditDAL` behind `IAuditRepository` (Story 21.2). All Manager and Controller layer writes are wrong-layer and are targeted for extraction in Stories 21.3 and 21.5.

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `WriteAuditEntryAsync` | `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | 45 | **B** ✓ (DAL) | `void` (response ignored) |
| `UpdateYearOfDeathAsync` (audit write) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 318 | **B** (Manager ✗ — wrong layer) | `document_put_response` |
| `UpdateMaidenNameAsync` (audit write) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 537 | **B** (Manager ✗ — wrong layer) | `document_put_response` |
| `SaveCaseAsync` (audit write) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 1180 | **B** (Manager ✗ — wrong layer) | `document_put_response` |
| `ForceReleaseCaseLockAsync` (audit write) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 1330 | **B** (Manager ✗ — wrong layer) | `document_put_response` |
| `ForceRemoveOfflineLockAsync` (audit write) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 1831 | **B** (Manager ✗ — wrong layer) | `document_put_response` |
| `DeleteCaseAsync` (audit write) | `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | 2330 | **B** (Manager ✗ — wrong layer) | `document_put_response` |
| `Post` (PMSS save — audit write) | `mmria-server/Controllers/api/caseController.pmss.cs` | 261 | **B** (Controller ✗ — wrong layer) | `document_put_response` |
| `Delete` (PMSS delete — audit write) | `mmria-server/Controllers/api/caseController.pmss.cs` | 418 | **B** (Controller ✗ — wrong layer) | `document_put_response` |

> **Pattern B at wrong layer:** All 6 `CaseManager.cs` writes and both `caseController.pmss.cs` writes already use Pattern B (correct URL construction). The violation is the architectural layer — audit HTTP calls are made directly from Manager and Controller code rather than through a dedicated `AuditDAL`. Stories 21.3 and 21.5 extract these to `IAuditRepository.WriteAuditEntryAsync`.

---

#### Audit Entry Reads (GET by ID)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetChangeStackAsync` (GET `Change_Stack` by audit ID) | `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | 39 | **A** (wrong) | `Change_Stack` |
| `GetAuditDocumentAsync` (GET `Change_Stack` by audit ID — recover-deleted workflow) | `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | 65 | **B** ✓ | `Change_Stack` |

---

#### Audit View Queries (`by_deleted`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetDeletedCasesViewAsync` (GET `_design/sortable/_view/by_deleted`) | `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | 57 | **B** ✓ | `get_sortable_view_reponse_header<Audit_Detail_View>` |

---

#### Mango `_find` Queries (by `case_id`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Note |
|-----------|----------------|---------|-------------|---------------|------|
| `GetFindUrl` (private helper — called by `GetAuditViewDataAsync`) | `mmria.common/SharedLibraries/AuditRecovery/Manager/AuditRecoveryManager.cs` | 144 | **A** (Manager ✗ — wrong layer) | URL passed to `AuditRecoveryDAL.FindChangeStacksAsync` → `ChangeStackResult` | Live — URL construction should move to `AuditDAL` in Story 21.2 |
| `get_find_url` (private method — **dead code**, never called from any action) | `mmria-server/Controllers/_auditController.cs` | 91 | **A** (Controller ✗ — vestigial) | URL `string` only | Dead — both action methods (`Index`, `MoreDetail`) delegate to `AuditRecoveryManager`; delete in Story 21.5 |
| `get_find_url` (private method — **dead code**, never called from any action) | `mmria-server/Controllers/api/AuditRecoverUtilController.cs` | 36 | **A** (Controller ✗ — vestigial) | URL `string` only | Dead — `Get` action delegates to `AuditRecoveryManager`; delete in Story 21.5 |

---

#### Special Document Reads/Writes (`audit-manage-user`)

The `audit-manage-user` document is a singleton in the `audit` database that accumulates manage-user admin events. It is not a `Change_Stack`.

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `GetAuditManageUserAsync` (GET `audit-manage-user`) | `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | 51 | **A** (wrong) | `Audit_Manage_User` (null if not_found) |
| `SaveAuditManageUserAsync` (PUT `audit-manage-user`) | `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | 63 | **A** (wrong) | `document_put_response` |
| `GetAuditManageUserAsync` (GET `audit-manage-user` — **duplicate**) | `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 146 | **A** (wrong) | `Audit_Manage_User` (null if not_found) |

> **Duplicate:** `AuditRecoveryDAL.GetAuditManageUserAsync` and `ManageUsersDAL.GetAuditManageUserAsync` are functionally identical GET operations against the same `audit-manage-user` document. After `AuditDAL` is created in Story 21.2, `ManageUsersDAL` must delegate to `IAuditRepository` rather than maintain its own copy (Story 21.6).

---

#### Delete Operations

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
|-----------|----------------|---------|-------------|---------------|
| `DeleteAuditDocumentAsync` (DELETE `Change_Stack` by ID + rev) | `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | 92 | **B** ✓ | `void` (response ignored) |

---

### Infrastructure / Out of Scope

These operations initialize the `audit` database and its design documents during server startup. They are **not** targets for `IAuditRepository`.

| File | Line(s) | Operation | Reason |
|------|---------|-----------|--------|
| `mmria-server/util/c_db_setup.cs` | 178 | Check existence of `{prefix}audit` database | DB setup — out of scope |
| `mmria-server/util/c_db_setup.cs` | 180 | PUT `{prefix}audit` (create database) | DB setup — out of scope |
| `mmria-server/util/c_db_setup.cs` | 183 | PUT `{prefix}audit/_security` (set access roles) | DB setup — out of scope |
| `mmria-server/util/c_db_setup.cs` | 187 | Check existence of `{prefix}audit/_design/sortable` | DB setup — out of scope |
| `mmria-server/util/c_db_setup.cs` | 190, 194 | PUT `{prefix}audit/_design/sortable` (create design doc) | DB setup — out of scope |

---

### Summary Counts

| Category | Call Sites |
|----------|-----------|
| Audit entry writes — PUT `Change_Stack` | 9 |
| Audit entry reads — GET by ID | 2 |
| Audit view queries — `by_deleted` | 1 |
| Mango `_find` — by `case_id` (1 live + 2 dead code) | 3 |
| Special document reads/writes — `audit-manage-user` | 3 |
| DELETE operations | 1 |
| **Total in-scope call sites** | **19** |
| Infrastructure / out-of-scope call sites | 5 |

---

### Pattern A Callsites to Remediate (Stories 21.2–21.6 scope)

| File | Method | Line(s) | Target Story |
|------|--------|---------|-------------|
| `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | `GetChangeStackAsync` | 39 | 21.2 — move to `AuditDAL` |
| `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | `GetAuditManageUserAsync` | 51 | 21.2 — move to `AuditDAL` |
| `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | `SaveAuditManageUserAsync` | 63 | 21.2 — move to `AuditDAL` |
| `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | `GetAuditManageUserAsync` | 146 | 21.6 — route through `IAuditRepository` |
| `mmria.common/SharedLibraries/AuditRecovery/Manager/AuditRecoveryManager.cs` | `GetFindUrl` (private) | 144 | 21.6 — URL construction moves to `AuditDAL` |
| `mmria-server/Controllers/_auditController.cs` | `get_find_url` (dead code) | 91 | 21.5 — delete vestigial method |
| `mmria-server/Controllers/api/AuditRecoverUtilController.cs` | `get_find_url` (dead code) | 36 | 21.5 — delete vestigial method |

> **Note on Pattern B at wrong layer:** The 6 `CaseManager.cs` and 2 `caseController.pmss.cs` audit writes already use Pattern B — no URL pattern change is required for those. The remediation is moving the HTTP call site to `AuditDAL` by introducing `IAuditRepository.WriteAuditEntryAsync` and having the Manager/Controller call the interface method instead.
