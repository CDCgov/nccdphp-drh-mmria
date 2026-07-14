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
