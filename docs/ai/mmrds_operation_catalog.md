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

---

### Boundary Decisions (metadata)

**Story 20.6 — Decision recorded 2026-07-16**

---

#### Decision 1: `_all_docs` bulk reads — Option (a) Application-owned — already in `IMetadataRepository`

**Finding:** `MetadataVersionManager` issues `GET metadata/_all_docs?include_docs=true` in two places:

- `ListVersionSpecificationsAsync` (line 68) → `_dal.GetAllVersionSpecificationHeadersAsync(dbConfig)` → `IMetadataRepository.GetAllVersionSpecificationHeadersAsync`
- `ListUiSpecificationsAsync` (line ~220) → `_dal.GetAllUiSpecificationHeadersAsync(dbConfig)` → `IMetadataRepository.GetAllUiSpecificationHeadersAsync`

Both methods are already declared in `IMetadataRepository` and implemented in `MetadataVersionDAL`. `MetadataVersionManager` holds `IMetadataRepository` via constructor injection (`_dal`) and calls through it exclusively. No direct CouchDB URL construction for `_all_docs` exists in the Manager layer.

**Resolution:** Both bulk reads are **application-owned enumeration operations** (admin UX lists version and UI specs for the version manager). They belong in `IMetadataRepository` and are already there. No additional method, no remediation work required for Story 20.2.

**Consistency with Story 17.7:** Story 17.7-Decision 2 excluded `c_document_sync_all` bulk reads from `ICaseRepository` because they are full-database streaming operations that drive sync infrastructure, not application requests. The metadata `_all_docs` calls are architecturally distinct:
- They enumerate a bounded set of version/UI specification records for admin UX display
- They are already managed entirely within the `MetadataVersionManager` → `MetadataVersionDAL` → `IMetadataRepository` chain
- They are not infrastructure drivers — they do not power sync, replication, or low-level paging operations

The metadata case is closer to the 17.7 CDC-read pattern decision (which kept `GetCaseIdsByDateCreated` etc. in `MMRIAServicesDAL`, not `ICaseRepository`) than to the sync exclusion: the reads are specific to a single manager's workflow and the manager already provides the correct encapsulation.

| Concern | Decision | `IMetadataRepository` scope? |
|---------|----------|------------------------------|
| `_all_docs` version-spec enumeration (`ListVersionSpecificationsAsync`) | Already in `IMetadataRepository` (option a) | Yes — `GetAllVersionSpecificationHeadersAsync` |
| `_all_docs` UI-spec enumeration (`ListUiSpecificationsAsync`) | Already in `IMetadataRepository` (option a) | Yes — `GetAllUiSpecificationHeadersAsync` |

---

#### Decision 2: `broadcast-message-list` prefix inconsistency — Intentional by design (not a bug)

**Finding:** Two different URL shapes exist for `broadcast-message-list`:

- `mmria-server`'s `broadcast_messageController` routes through `IMetadataRepository.GetBroadcastMessageListAsync` / `SaveBroadcastMessageListAsync`, which use `$"{dbConfig.url}/metadata/broadcast-message-list"` (Pattern A — global, no prefix). The server writes the canonical broadcast message to the global `metadata` database, then calls the services `api/broadcastMessage/ReplicateMessage` endpoint to trigger per-tenant replication.

- `mmria.services`'s `broadcastMessageController.ReplicateMessage` is a replication sink. It receives a signed server-push, loops over every configured tenant, and writes the message to `$"{p_config_detail.url}/{p_config_detail.prefix}metadata/broadcast-message-list"` (Pattern B — per-tenant prefix). This is not application CRUD — it is explicit broadcast replication infrastructure.

**Resolution:** The prefix difference is **intentional multi-tenant broadcast architecture**:

1. CDC admin writes to the global `metadata` database via the server (IMetadataRepository, no prefix).
2. The server triggers per-tenant replication by calling the services endpoint.
3. The services endpoint writes the message to each tenant's own `{prefix}metadata` database so every tenant's case workers see it.

The `IMetadataRepository` boundary applies to the `mmria-server` layer only (global `metadata`). The `mmria.services` `broadcastMessageController.ReplicateMessage` is a replication sink and its direct URL construction with per-tenant prefix is correct. It is not routed through `IMetadataRepository` — no change required for Stories 20.3–20.5.

| Layer | Database targeted | Correct? | `IMetadataRepository`? |
|-------|-------------------|----------|------------------------|
| `mmria-server` `broadcast_messageController` | Global `metadata` (no prefix) | ✅ | Yes — routes through interface |
| `mmria.services` `broadcastMessageController.ReplicateMessage` | Per-tenant `{prefix}metadata` | ✅ (by design) | No — replication infrastructure sink |

---

#### Scope summary for Epic 20

| Concern | Decision | `IMetadataRepository` scope? |
|---------|----------|------------------------------|
| `_all_docs` version-spec enumeration | Already in interface — application-owned | Yes (already done) |
| `_all_docs` UI-spec enumeration | Already in interface — application-owned | Yes (already done) |
| `broadcast-message-list` prefix in services | Intentional replication sink — not a bug | No — services replication infrastructure |

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

---

## `session` Operations

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story:** 23.1
**Date:** 2026-07-16

This catalog records every distinct operation against the `{prefix}session` CouchDB database across `mmria-server`, `mmria.common`, and `mmria.services`. It is the authoritative operation set for Story 23.2.

---

### URL Construction Patterns

| Label | Pattern | Example |
|-------|---------|---------|
| **A** | Hand-assembled — **wrong** | `$"{dbConfig.url}/{dbConfig.prefix}session/{id}"` |
| **B** | Uses `Get_Prefix_DB_Url` helper — **correct** | `dbConfig.Get_Prefix_DB_Url($"session/{id}")` |

> **Note on `_session` vs `{prefix}session`:** `SessionDAL.GetCouchDbSessionAsync` and `SessionDAL.LoginToCouchDbSessionAsync` call the CouchDB built-in `/_session` endpoint for cookie-based authentication — not the application `{prefix}session` database. Those operations are not cataloged here.

---

### In-Scope Operations

#### Session Document Writes (PUT)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `CreateSessionAsync` (PUT new session) | `mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs` | 80 | A | `document_put_response` | DAL ✓ |
| `SaveSessionEventAsync` (PUT session event) | `mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs` | 90 | A | `void` (response ignored) | DAL ✓ |
| `SaveSessionAsync` (PUT session update) | `mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs` | 111 | A | `document_put_response` | DAL ✓ |
| `CreateSessionEventAsync` (PUT session event) | `mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs` | 370 | B | `bool` (wraps `document_put_response.ok`) | DAL ✓ (duplicate — see note) |
| `CreateSessionDocumentAsync` (PUT session document) | `mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs` | 410 | B | `document_put_response?` | DAL ✓ (duplicate — see note) |
| OIDC login — PUT session doc | `mmria-server/Controllers/AccountController.OIDC.cs` | 401 | A | `document_put_response` | Controller ✗ |
| `Post_Session` actor — PUT session doc (read-then-write) | `mmria-server/model/actor/Post_Session_Actor.cs` | 54 | A | `document_put_response` | Actor ✗ |
| `Record_Session_Event` actor — PUT session event | `mmria-server/model/actor/Record_Session_Event.cs` | 68 | A | `void` (fire-and-forget) | Actor ✗ |

> **`AccountController.OIDC.cs:401` note:** After the PUT succeeds, `_sessionManager.PostSessionAsync` is called — which also calls `SessionDAL.SaveSessionAsync` (a second PUT for the same document). OIDC login therefore issues two PUTs for the same session document. Story 23.2 must confirm whether both are intentional or the second is a redundant duplicate.

#### Session Document Reads (GET by ID)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `GetSessionDocumentAsync` | `mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs` | 103 | A | `session` | DAL ✓ |
| Logout — GET session by cookie `sid` | `mmria-server/Controllers/AccountController.cs` | 346 | A | `Session_MessageDTO` | Controller ✗ |
| `Post_Session` actor — GET session doc (read before update) | `mmria-server/model/actor/Post_Session_Actor.cs` | 30 | A | `session` | Actor ✗ |

#### Session Database Existence Check (GET `{prefix}session`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `GetSessionDatabaseAsync` | `mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs` | 96 | A | `session_response` | DAL ✓ |

#### View Queries

##### `session_event_sortable/by_user_id`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `GetSessionEventsByUserIdAsync` | `mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs` | 161 | A | `get_sortable_view_reponse_header<session_event>` | DAL ✓ |
| `GetSessionEventsAsync` (failed-login lockout check) | `mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs` | 323 | B | `List<session_event>` | DAL ✓ (duplicate — see note) |

##### `session_sortable/by_date_created`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `GetSessionCount` (admin dashboard session history) | `mmria-server/util/SessionSummary.cs` | 165 | A | `view_response<SessionItem>` | Util ✗ |

---

### Duplicate DAL Note

`SessionDAL` and `AccountDAL` both write to and read from the `{prefix}session` database. `AccountDAL` holds `GetSessionEventsAsync`, `CreateSessionEventAsync`, and `CreateSessionDocumentAsync` because the Account feature's login lockout and session creation logic lives there. `SessionDAL` holds general session lifecycle operations. Both are correct DAL-layer placement but create split ownership of one database. Story 23.2 must decide: consolidate both into a single `ISessionRepository` (preferred), or expose `ISessionRepository` from `SessionDAL` and have `AccountDAL` delegate to it.

---

### Infrastructure / Out of Scope

| File | Line(s) | Operation | Reason |
|------|---------|-----------|--------|
| `mmria-server/util/c_db_setup.cs` | 203–226 | Check existence, PUT `{prefix}session`, PUT `{prefix}session/_security`, PUT `{prefix}session/_design/session_event_sortable`, PUT `{prefix}session/_design/session_sortable` | DB initialization |

---

### Summary Counts (session)

| Category | Call Sites |
|----------|-----------|
| Session document PUT (create/update) | 8 |
| Session document GET | 3 |
| Session database GET (existence check) | 1 |
| View query: `session_event_sortable/by_user_id` | 2 |
| View query: `session_sortable/by_date_created` | 1 |
| **Total in-scope call sites** | **15** |
| Infrastructure / out-of-scope | 5+ |

---

## `offline_cases` Operations

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story:** 23.1
**Date:** 2026-07-16

This catalog records every distinct operation against the `{prefix}offline_cases` CouchDB database across `mmria-server` and `mmria.common`. It is the authoritative operation set for Story 23.3.

---

### URL Construction Patterns

| Label | Pattern | Example |
|-------|---------|---------|
| **A** | Hand-assembled — **wrong** | `$"{dbConfig.url}/{dbConfig.prefix}offline_cases/{id}"` |
| **B** | Uses `Get_Prefix_DB_Url` helper — **correct** | `dbConfig.Get_Prefix_DB_Url("offline_cases/_design/sortable/_view/by-created-by")` |

---

### In-Scope Operations

#### Document CRUD (GET/PUT/DELETE by ID)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `CreateOfflineCaseAsync` (PUT) | `mmria.common/SharedLibraries/OfflineCase/DAL/OfflineCaseDAL.cs` | 46 | A | `document_put_response` | DAL ✓ |
| `GetOfflineCaseAsync` (GET) | `mmria.common/SharedLibraries/OfflineCase/DAL/OfflineCaseDAL.cs` | 55 | A | `OfflineCaseResponse` | DAL ✓ |
| `UpdateOfflineCaseAsync` (PUT update) | `mmria.common/SharedLibraries/OfflineCase/DAL/OfflineCaseDAL.cs` | 197 | A | `document_put_response` | DAL ✓ |
| `DeleteOfflineCaseAsync` (DELETE with `?rev=`) | `mmria.common/SharedLibraries/OfflineCase/DAL/OfflineCaseDAL.cs` | 206 | A | `document_put_response` | DAL ✓ |

#### View Queries

##### `sortable/by-created-by`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `GetUserOfflineCasesAsync` (keyed by userId — all states) | `mmria.common/SharedLibraries/OfflineCase/DAL/OfflineCaseDAL.cs` | ~75 | B | `OfflineCaseListResponse` | DAL ✓ |
| `TryGetUserOfflineCasesAsync` (keyed, non-throwing) | `mmria.common/SharedLibraries/OfflineCase/DAL/OfflineCaseDAL.cs` | ~90 | B | `OfflineCaseListResponse` | DAL ✓ |
| `GetActiveSessionIdForUserInAnotherTabAsync` (keyed, filter in-process) | `mmria.common/SharedLibraries/OfflineCase/DAL/OfflineCaseDAL.cs` | ~115 | B | `string` (session ID or `null`) | DAL ✓ |
| `GetAllActiveSessionsAsync` (unkeyed, active states only) | `mmria.common/SharedLibraries/OfflineCase/DAL/OfflineCaseDAL.cs` | ~186 | B | `OfflineCaseListResponse` | DAL ✓ |

##### `sortable/lightweight-status-only`

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `LoadOfflineSessionsAsync` (logger metadata panel) | `mmria-server/Controllers/loggerController.cs` | ~99 | B | `dynamic` | Controller ✗ |

---

### Infrastructure / Out of Scope

| File | Line(s) | Operation | Reason |
|------|---------|-----------|--------|
| `mmria-server/util/c_db_setup.cs` | 275–292 | Check existence, PUT `{prefix}offline_cases`, PUT `{prefix}offline_cases/_security`, PUT `{prefix}offline_cases/_design/sortable` | DB initialization |

---

### Summary Counts (offline_cases)

| Category | Call Sites |
|----------|-----------|
| Document PUT (create/update) | 2 |
| Document GET | 1 |
| Document DELETE | 1 |
| View query: `by-created-by` | 4 |
| View query: `lightweight-status-only` | 1 |
| **Total in-scope call sites** | **9** |
| Infrastructure / out-of-scope | 4+ |

---

## `export_queue` Operations

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story:** 23.1
**Date:** 2026-07-16

This catalog records every distinct operation against the `{prefix}export_queue` CouchDB database across `mmria-server`, `mmria.common`, and `mmria.services`. It is the authoritative operation set for Story 23.4.

---

### URL Construction Patterns

| Label | Pattern | Example |
|-------|---------|---------|
| **A** | Hand-assembled — **wrong** | `$"{db_config.url}/{db_config.prefix}export_queue/{id}"` |
| **B** | Uses `Get_Prefix_DB_Url` helper — **correct** | `db_config.Get_Prefix_DB_Url("export_queue/" + id)` |

---

### In-Scope Operations

#### GET all documents (`_all_docs`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `GetAllQueueDocumentsAsync` | `mmria.common/SharedLibraries/ExportQueue/DAL/ExportQueueDAL.cs` | ~22 | B | `ExpandoObject` | DAL ✓ |
| `Process_Export_Queue_Item` — GET all queue items at actor tick start | `mmria.services/Actors/ExportQueue/Process_Export_Queue.cs` | 221 | A | `string` (raw JSON) | Actor ✗ |

#### GET document by ID

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `GetQueueDocumentAsync<T>` | `mmria.common/SharedLibraries/ExportQueue/DAL/ExportQueueDAL.cs` | ~30 | B | `T` (generic) | DAL ✓ |
| GET queue item by ID (read before status update) | `mmria.services/Actors/ExportQueue/Process_Export_Queue.cs` | 285 | A | `ExpandoObject` | Actor ✗ |
| `UpdateQueueItem` — GET queue item (read before update) | `mmria.services/Utilities/Exporter/exporter.cs` | 1364 | A | `export_queue_item` | Exporter ✗ |
| `GetQueueItem` — GET queue item | `mmria.services/Utilities/Exporter/exporter.cs` | 1386 | A | `export_queue_item` | Exporter ✗ |
| `UpdateQueueItem` — GET queue item (read before update) | `mmria.services/Utilities/Exporter/mmrds_exporter.cs` | 1794 | A | `export_queue_item` | Exporter ✗ |
| GET queue item by ID | `mmria.services/Utilities/CoreElementExport/core_element_exporter.cs` | 795 | A | `export_queue_item` | Exporter ✗ |
| GET queue item by ID | `mmria-server/util/core_element_export/core_element_exporter.cs` | 804 | A | `export_queue_item` | Util ✗ |

#### PUT document by ID (create / update)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `SaveQueueDocumentAsync` | `mmria.common/SharedLibraries/ExportQueue/DAL/ExportQueueDAL.cs` | ~38 | B | `document_put_response` | DAL ✓ |
| PUT queue item status update (3 call sites — processing, success, failure) | `mmria.services/Actors/ExportQueue/Process_Export_Queue.cs` | 316, 354, 385 | A | `string` (response ignored) | Actor ✗ |
| PUT queue item (write back after status update) | `mmria.services/Utilities/Exporter/exporter.cs` | 1373 | A | `export_queue_item` | Exporter ✗ |
| PUT queue item (write back after status update) | `mmria.services/Utilities/Exporter/mmrds_exporter.cs` | 1803 | A | `export_queue_item` | Exporter ✗ |

---

### Infrastructure / Out of Scope

| File | Line(s) | Operation | Reason |
|------|---------|-----------|--------|
| `mmria-server/model/actor/quartz/Rebuild_Export_Queue.cs` | 66–74 | DELETE `{prefix}export_queue` (if exists) then PUT (recreate) + setup | DB rebuild infrastructure |
| `mmria-server/model/rebuild_export_queue_job.cs` | 78–84 | DELETE `{prefix}export_queue` then PUT + setup | DB rebuild infrastructure |
| `mmria-server/util/c_db_setup.cs` | 244–260 | Check existence, DELETE (if exists), PUT `{prefix}export_queue`, PUT `{prefix}export_queue/_security` | DB initialization |

---

### Summary Counts (export_queue)

| Category | Call Sites |
|----------|-----------|
| GET `_all_docs` | 2 |
| GET by ID | 7 |
| PUT by ID | 5 |
| **Total in-scope call sites** | **14** |
| Infrastructure / out-of-scope (DB rebuild + setup) | 6+ |

---

## `vital_import` Operations

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story:** 23.1
**Date:** 2026-07-16

This catalog records every distinct operation against the `vital_import` CouchDB database across `mmria-server`, `mmria.common`, and `mmria.services`. It is the authoritative operation set for Story 23.5.

> ⚠️ **`vital_import` URL exception:** This database does **not** use the tenant prefix separator. All calls use `{config.url}/vital_import/...` directly — never `Get_Prefix_DB_Url`. This is intentional: `vital_import` is a special non-tenant system-level database. Story 23.5 must document this as a deliberate exception in both `IVitalImportRepository` and `VitalImportDAL`. All callers must preserve this pattern.

---

### URL Construction Patterns

| Label | Pattern | Example |
|-------|---------|---------|
| **D** | No-prefix direct URL — **intentional** | `$"{config.url}/vital_import/_all_docs?include_docs=true"` |

---

### In-Scope Operations

#### GET all documents (`_all_docs`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `GetBatchSetAsync` | `mmria.common/SharedLibraries/VitalImport/DAL/VitalImportDAL.cs` | 47 | D | `alldocs_response<Batch>` | DAL ✓ |
| `GetBatchSet` | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 118 | D | `alldocs_response<Batch>` | DAL ✗ (wrong DAL — see note) |
| `Get` (GET IJE batch list) | `mmria-server/Controllers/api/ije_messageController.cs` | 74 | D | `alldocs_response<Batch>` | Controller ✗ |
| `Get` (GET vital import list) | `mmria.services/Controllers/VitalNotificationController.cs` | 39 | D | `List<Batch>` | Controller ✗ |
| `Delete` (read batch list before queuing actor removes) | `mmria.services/Controllers/VitalNotificationController.cs` | ~69 | D | `alldocs_response<Batch>` (read only; deletes via actor) | Controller ✗ |
| CDC populate — enumerate vital import batches | `mmria.services/Actors/populate-cdc-instance/PopulateCDCInstanceSupervisor.cs` | 343 | D | `alldocs_response<Batch>` | Actor ✗ |

#### PUT document by ID (create / update)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `SaveBatchDocument` | `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 140 | D | `document_put_response` | DAL ✗ (wrong DAL — see note) |

> **Dead code note:** `mmria.services/Controllers/backupController.cs` at line 558 references `vital_import/_all_docs` inside a `/* ... */` block-comment. This is inactive dead code and is not counted as an in-scope operation.

---

### Wrong-DAL Note (`MMRIAServicesDAL`)

`MMRIAServicesDAL.GetBatchSet` (line 118) and `MMRIAServicesDAL.SaveBatchDocument` (line 140) duplicate operations already owned by `VitalImportDAL`. Story 23.5 should consolidate both behind `IVitalImportRepository` and have `MMRIAServicesDAL` delegate through the interface rather than constructing `vital_import` URLs directly.

---

### Summary Counts (vital_import)

| Category | Call Sites |
|----------|-----------|
| GET `_all_docs` | 6 |
| PUT by ID | 1 |
| **Total in-scope call sites** | **7** |

---

## `report` Operations

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story:** 23.1
**Date:** 2026-07-16

This catalog records every distinct **application read** operation against the `{prefix}report` CouchDB database across `mmria-server` and `mmria.common`. It is the authoritative operation set for Stories 23.6 and 23.7.

> ℹ️ **Write side is out of scope:** The `report` database is written exclusively by sync and rebuild actors (`c_sync_document.cs` variants, `c_document_sync_all.cs` variants, `Process_DB_Synchronization_Set.cs`, `Process_Central_Pull_list.cs`, PMSS variants). These are sync/rebuild infrastructure and are explicitly excluded. `IReportRepository` covers **read operations only**.

---

### URL Construction Patterns

| Label | Pattern | Example |
|-------|---------|---------|
| **A** | Hand-assembled — **wrong** | `$"{config_couchdb_url}/{config_db_prefix}report/_find"` |
| **B** | Uses `Get_Prefix_DB_Url` helper — **correct** | `dbConfig.Get_Prefix_DB_Url("report/_all_docs?include_docs=true")` |

---

### In-Scope Operations (Application Reads)

#### GET all documents (`_all_docs`)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `GetReportsAsync` (aggregate report bulk read) | `mmria.common/SharedLibraries/AggregateReport/Manager/AggregateReportManager.cs` | ~36 | B | `IList<c_report_object>` | Manager ✗ |

#### View Queries

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `Get` (interactive — `interactive_aggregate_report/_view/indicator_id`) | `mmria.common/SharedLibraries/InteractiveReport/Manager/InteractiveReportManager.cs` | 30 | A | `get_sortable_view_reponse_header<report_measure_value_struct>` | Manager ✗ |
| `Get` (data summary — `data_summary_view_report/_view/year_of_death`) | `mmria-server/Controllers/api/data_summary_viewController.cs` | 75 | A | `get_sortable_view_reponse_header<FrequencySummaryDocument>` | Controller ✗ |

#### Mango `_find` Queries

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Selector |
|-----------|----------------|---------|-------------|---------------|----------|
| `Get` (DQR detail — `data_type:$eq:dqr-detail`) | `mmria-server/Controllers/api/dqrReportController.cs` | 90 | A | `DQRDetail[]` | `{data_type: {$eq: "dqr-detail"}}` |
| `Get` (overdose measure — opioid-report-index) | `mmria-server/Controllers/api/overdose_measureController.cs` | 78 | A | `c_opioid_report_object[]` | Opioid report selector |
| `Get` (PowerBI measure — `_id:{$regex:"^powerbi"}`) | `mmria-server/Controllers/api/powerbi_measureController.cs` | 80 | A | `c_opioid_report_object[]` | PowerBI selector |

---

### Infrastructure / Out of Scope

All write operations against `{prefix}report` are sync/rebuild infrastructure and are **not** targeted by `IReportRepository`.

| File | Operation | Reason |
|------|-----------|--------|
| `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_sync_document.cs` | PUT `{prefix}report/{document_id}` (sync write) | Sync infrastructure |
| `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | POST `{prefix}report/_index`, PUT `{prefix}report/_design/*` | Sync infrastructure |
| `mmria.services/Actors/populate-cdc-instance/c_sync_document.cs` | PUT `{prefix}report/{document_id}` (CDC sync write) | Sync infrastructure |
| `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | GET/PUT `{prefix}report` (CDC sync) | Sync infrastructure |
| `mmria-server/util/c_sync_document.pmss.cs` | PUT `{prefix}report/{id}` (PMSS sync) | Sync infrastructure |
| `mmria-server/util/c_document_sync_all.cs` | GET/PUT `{prefix}report` (tenant rebuild) | Sync infrastructure |
| `mmria-server/util/c_document_sync_all.pmss.cs` | DELETE/PUT `{prefix}report` (PMSS rebuild) | Sync infrastructure |
| `mmria-server/util/c_document_sync_all_legacy.cs` | POST `{prefix}report/_index`, rebuild ops | Sync infrastructure |
| `mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` (line 193) | GET `{prefix}report/_all_docs` (sync ID reconciliation) | Sync infrastructure |
| `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | DELETE/PUT `{prefix}report` (CDC rebuild) | CDC rebuild infrastructure |
| `mmria-server/util/c_db_setup.cs` | PUT `{prefix}report`, `_security`, `_design/*` | DB initialization |

---

### Summary Counts (report)

| Category | Call Sites |
|----------|-----------|
| GET `_all_docs` | 1 |
| View queries | 2 |
| Mango `_find` | 3 |
| **Total in-scope call sites (reads)** | **6** |
| Sync/rebuild infrastructure (out-of-scope) | 11+ |

---

### Boundary Decisions (report)

**Story:** 23.6
**Date:** 2026-07-16

`IReportRepository` covers **read operations only**. The following write/rebuild operations against the `report` database are declared **infrastructure out-of-scope** and are intentionally excluded from this interface:

- **DROP DB / CREATE DB** — performed by rebuild actors (`c_document_sync_all.cs` variants, `c_document_sync_all.pmss.cs`, `Process_Central_Pull_list.cs`)
- **Bulk PUT report documents** — performed by sync actors (`c_sync_document.cs` variants, rebuild manager classes)
- **`_index` creation** — performed by `c_document_sync_all_legacy.cs`, `c_document_sync_all.cs`
- **Design document PUT** — performed by `c_document_sync_all_legacy.cs`, `c_db_setup.cs`

These are sync/rebuild infrastructure concerns. A SQL migration of the `report` read path requires only a new `IReportRepository` implementation — sync/rebuild actors are addressed separately as part of the SQL migration infrastructure work.

---

## `logging` Operations

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story:** 23.1
**Date:** 2026-07-16

This catalog records every distinct operation against the `{prefix}logging` CouchDB database across `mmria-server`, `mmria.common`, and `mmria.services`. It is the authoritative operation set for Story 23.8.

---

### URL Construction Patterns

| Label | Pattern | Example |
|-------|---------|---------|
| **A** | Hand-assembled — **wrong** | `$"{db_config.url}/{db_config.prefix}logging"` |

> **No Pattern B calls:** All application `logging` database calls in the codebase use hand-assembled URLs (Pattern A). No `Get_Prefix_DB_Url` usage observed.

---

### In-Scope Operations

#### Document Write (POST — CouchDB-assigned ID)

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `SaveLog` (POST new log entry — CouchDB assigns `_id`) | `mmria-server/Controllers/loggerController.cs` | 653 | A | `document_put_response` | Controller ✗ |

#### View Queries

| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type | Layer |
|-----------|----------------|---------|-------------|---------------|-------|
| `LoadLoggingByOfflineSessionAsync` (GET `sortable/_view/by-offline-session`) | `mmria-server/Controllers/loggerController.cs` | 93 | A | `dynamic` | Controller ✗ |
| `GetLogs` (dynamic view selection — one of: `by-timestamp`, `by-offline-session`, `by-user`, `by-context`, `by-level`, `all-fields`) | `mmria-server/Controllers/loggerController.cs` | 283 | A | `dynamic` | Controller ✗ |

> **`GetLogs` dynamic view note:** `GetLogs` selects one of six views at runtime based on filter query parameters. All queries target `{prefix}logging/_design/sortable/_view/{view-name}`. Story 23.8's `ILoggingRepository` must expose a query method that accepts filter parameters and maps them to the appropriate view, or expose per-filter methods — the catalog records this as a single dynamic operation.

---

### Infrastructure / Out of Scope

| File | Line(s) | Operation | Reason |
|------|---------|-----------|--------|
| `mmria-server/util/c_db_setup.cs` | 298–315 | Check existence, PUT `{prefix}logging`, PUT `{prefix}logging/_security`, PUT `{prefix}logging/_design/sortable` | DB initialization |

---

### Summary Counts (logging)

| Category | Call Sites |
|----------|-----------|
| Document write (POST) | 1 |
| View queries (static + dynamic) | 2 |
| **Total in-scope call sites** | **3** |
| Infrastructure / out-of-scope | 4+ |

---

## Migration Readiness Gate

**All six remaining databases are now cataloged. When Epic 23 is complete, every CouchDB database access routes through a repository interface. SQL DAL implementation work can begin immediately after.**

| Database | Repository interface | Story |
|----------|---------------------|-------|
| `session` | `ISessionRepository` | 23.2 |
| `offline_cases` | `IOfflineCaseRepository` | 23.3 |
| `export_queue` | `IExportQueueRepository` | 23.4 |
| `vital_import` | `IVitalImportRepository` | 23.5 |
| `report` | `IReportRepository` (read-only) | 23.6 / 23.7 |
| `logging` | `ILoggingRepository` | 23.8 |

---

## Infrastructure Consolidation Operations

**Epic:** 24 — Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)
**Story:** 24.1
**Date:** 2026-07-16

This catalog records every distinct database operation in the eleven in-scope infra files across `mmria-server`, `mmria.common`, and `mmria.services`. It is the authoritative operation set for Stories 24.2–24.9.

---

### URL Construction Patterns (Epic 24)

| Label | Pattern | Example |
|-------|---------|---------|
| **D** | Direct infra string interpolation — `{url}/{prefix}{db}/...` | `$"{db_config.url}/{db_config.prefix}de_id"` |
| **S** | ScheduleInfo-parameterized — `{scheduleInfo.couch_db_url}/{scheduleInfo.db_prefix}{db}/...` | `$"{scheduleInfo.couch_db_url}/{scheduleInfo.db_prefix}mmrds"` |
| **CDC-src** | Source CDC instance URL — `{db_info.url}/{db_info.prefix}mmrds/...` | `$"{db_info.url}/{db_info.prefix}mmrds/_all_docs"` |

> **Note on Pattern D:** Unlike Pattern A in the mmrds application-CRUD section, Pattern D is not "wrong" here. These files manage database lifecycle and sync orchestration — they legitimately own raw URL construction. The purpose of Epic 24 routing is SQL-migration readiness (interface extraction), not URL correctness.

---

### In-Scope Files

| File | Project | Category | Routed by Story |
|------|---------|----------|-----------------|
| `source-code/mmria/mmria-server/util/c_db_setup.cs` | mmria-server | DB lifecycle — startup initialization | 24.5 |
| `source-code/mmria/mmria-server/model/actor/quartz/Rebuild_Export_Queue.cs` | mmria-server | DB lifecycle — nightly export_queue rebuild (Akka actor) | 24.4 |
| `source-code/mmria/mmria-server/model/rebuild_export_queue_job.cs` | mmria-server | DB lifecycle — legacy Quartz IJob (dead code — see AC-5) | 24.4 |
| `source-code/mmria/mmria-server/util/c_sync_document.pmss.cs` | mmria-server | Per-doc writes to de_id and report (PMSS only) | 24.6 |
| `source-code/mmria/mmria-server/util/c_document_sync_all.cs` | mmria-server | Full rebuild orchestrator (non-PMSS) | 24.7 |
| `source-code/mmria/mmria-server/util/c_document_sync_all_legacy.cs` | mmria-server | Legacy individual-document rebuild (non-PMSS) | 24.7 |
| `source-code/mmria/mmria-server/util/c_document_sync_all.pmss.cs` | mmria-server | Full rebuild orchestrator (PMSS variant) | 24.7 |
| `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | mmria.common | Shared rebuild — barrier queries, progress tracking | 24.7 |
| `source-code/mmria/mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | mmria-server | Change-feed sync actor | 24.8 |
| `source-code/mmria/mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | mmria-server | CDC data integration actor | 24.9 |
| `nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | mmria.services | CDC bulk rebuild (modern, bulk-write path) | 24.9 |

---

### DB Lifecycle Operations

#### CREATE Database (PUT `{prefix}{db}`)

| Operation | File | ~Line | URL Pattern | Target DB | Owning Interface / Story |
|-----------|------|-------|-------------|-----------|--------------------------|
| PUT `_users` (first-time setup) | `mmria-server/util/c_db_setup.cs` | ~116 | `{url}/_users` | `_users` | `IDatabaseLifecycleService` — 24.5 |
| PUT `_replicator` (first-time setup) | `mmria-server/util/c_db_setup.cs` | ~117 | `{url}/_replicator` | `_replicator` | `IDatabaseLifecycleService` — 24.5 |
| PUT `_global_changes` (first-time setup) | `mmria-server/util/c_db_setup.cs` | ~118 | `{url}/_global_changes` | `_global_changes` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}mmrds` (startup repair) | `mmria-server/util/c_db_setup.cs` | ~141 | D | `mmrds` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}audit` (startup repair) | `mmria-server/util/c_db_setup.cs` | ~195 | D | `audit` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}session` (startup repair) | `mmria-server/util/c_db_setup.cs` | ~225 | D | `session` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}export_queue` (startup, always recreated) | `mmria-server/util/c_db_setup.cs` | ~265 | D | `export_queue` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}offline_cases` (startup repair) | `mmria-server/util/c_db_setup.cs` | ~283 | D | `offline_cases` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}logging` (startup repair) | `mmria-server/util/c_db_setup.cs` | ~303 | D | `logging` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}db_rebuild` (startup repair) | `mmria-server/util/c_db_setup.cs` | ~325 | D | `db_rebuild` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}jurisdiction` (UpdateJurisdiction) | `mmria-server/util/c_db_setup.cs` | ~412 | D | `jurisdiction` | `IDatabaseLifecycleService` — 24.5 |
| PUT `metadata` (UpdateMetadata) | `mmria-server/util/c_db_setup.cs` | ~485 | `{url}/metadata` | `metadata` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}export_queue` (Akka nightly rebuild) | `mmria-server/model/actor/quartz/Rebuild_Export_Queue.cs` | ~73 | D | `export_queue` | `IExportQueueRepository.PurgeAndReinitializeAsync` — 24.4 |
| PUT `{prefix}export_queue` (legacy Quartz IJob — dead code) | `mmria-server/model/rebuild_export_queue_job.cs` | ~87 | D | `export_queue` | `IExportQueueRepository.PurgeAndReinitializeAsync` — 24.4 (dead) |
| PUT `{prefix}de_id` (full rebuild — reset) | `mmria-server/util/c_document_sync_all.cs` | ~820 | D | `de_id` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 |
| PUT `{prefix}report` (full rebuild — reset) | `mmria-server/util/c_document_sync_all.cs` | ~833 | D | `report` | `IReportRepository` lifecycle — 24.2 / 24.7 |
| PUT `{prefix}db_rebuild` (ensure exists) | `mmria-server/util/c_document_sync_all.cs` | ~381 | D | `db_rebuild` | Stays in orchestration (internal rebuild-state) — 24.7 |
| PUT `{prefix}de_id` (legacy rebuild — reset) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~222 | D | `de_id` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 |
| PUT `{prefix}report` (legacy rebuild — reset) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~232 | D | `report` | `IReportRepository` lifecycle — 24.2 / 24.7 |
| PUT `{prefix}de_id` (PMSS full rebuild — reset) | `mmria-server/util/c_document_sync_all.pmss.cs` | ~131 | D | `de_id` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 |
| PUT `{prefix}report` (PMSS full rebuild — reset) | `mmria-server/util/c_document_sync_all.pmss.cs` | ~155 | D | `report` | `IReportRepository` lifecycle — 24.2 / 24.7 |
| PUT `{prefix}de_id` (common legacy rebuild) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~228 | D | `de_id` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 |
| PUT `{prefix}report` (common legacy rebuild) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~238 | D | `report` | `IReportRepository` lifecycle — 24.2 / 24.7 |
| PUT `{scheduleInfo.db_prefix}mmrds` (CDC target rebuild) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~78 | S | `mmrds` (CDC target) | `IDatabaseLifecycleService` extension — 24.9 |
| PUT `{scheduleInfo.db_prefix}de_id` (CDC target rebuild) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~130 | S | `de_id` (CDC target) | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.9 |
| PUT `{scheduleInfo.db_prefix}report` (CDC target rebuild) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~155 | S | `report` (CDC target) | `IReportRepository` lifecycle — 24.2 / 24.9 |
| PUT `{prefix}de_id` (CDC services rebuild) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~453 | D | `de_id` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.9 |
| PUT `{prefix}report` (CDC services rebuild — if not exists) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~493 | D | `report` | `IReportRepository` lifecycle — 24.2 / 24.9 |

#### DELETE Database (DELETE `{prefix}{db}`)

| Operation | File | ~Line | URL Pattern | Target DB | Owning Interface / Story |
|-----------|------|-------|-------------|-----------|--------------------------|
| DELETE `{prefix}export_queue` (Akka nightly rebuild) | `mmria-server/model/actor/quartz/Rebuild_Export_Queue.cs` | ~68 | D | `export_queue` | `IExportQueueRepository.PurgeAndReinitializeAsync` — 24.4 |
| DELETE `{prefix}export_queue` (startup, always) | `mmria-server/util/c_db_setup.cs` | ~255 | D | `export_queue` | `IDatabaseLifecycleService` — 24.5 |
| DELETE `{prefix}export_queue` (legacy Quartz IJob — dead code) | `mmria-server/model/rebuild_export_queue_job.cs` | ~74 | D | `export_queue` | `IExportQueueRepository.PurgeAndReinitializeAsync` — 24.4 (dead) |
| DELETE `{prefix}de_id` (full rebuild — reset) | `mmria-server/util/c_document_sync_all.cs` | ~806 | D | `de_id` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 |
| DELETE `{prefix}report` (full rebuild — reset) | `mmria-server/util/c_document_sync_all.cs` | ~806 | D | `report` | `IReportRepository` lifecycle — 24.2 / 24.7 |
| DELETE `{prefix}de_id` (legacy rebuild — reset) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~218 | D | `de_id` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 |
| DELETE `{prefix}report` (legacy rebuild — reset) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~225 | D | `report` | `IReportRepository` lifecycle — 24.2 / 24.7 |
| DELETE `{prefix}de_id` (PMSS full rebuild — reset) | `mmria-server/util/c_document_sync_all.pmss.cs` | ~109 | D | `de_id` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 |
| DELETE `{prefix}report` (PMSS full rebuild — reset) | `mmria-server/util/c_document_sync_all.pmss.cs` | ~120 | D | `report` | `IReportRepository` lifecycle — 24.2 / 24.7 |
| DELETE `{prefix}de_id` (common legacy rebuild) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~218 | D | `de_id` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 |
| DELETE `{prefix}report` (common legacy rebuild) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~225 | D | `report` | `IReportRepository` lifecycle — 24.2 / 24.7 |
| DELETE `{scheduleInfo.db_prefix}mmrds` (CDC target rebuild) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~73 | S | `mmrds` (CDC target) | `IDatabaseLifecycleService` extension — 24.9 |
| DELETE `{scheduleInfo.db_prefix}de_id` (CDC target rebuild) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~113 | S | `de_id` (CDC target) | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.9 |
| DELETE `{scheduleInfo.db_prefix}report` (CDC target rebuild) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~121 | S | `report` (CDC target) | `IReportRepository` lifecycle — 24.2 / 24.9 |
| DELETE `{prefix}de_id` (CDC services rebuild) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~434 | D | `de_id` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.9 |

#### Security Setup (PUT `{prefix}{db}/_security`)

| Operation | File | ~Line | URL Pattern | Target DB | Owning Interface / Story |
|-----------|------|-------|-------------|-----------|--------------------------|
| PUT `{prefix}mmrds/_security` | `mmria-server/util/c_db_setup.cs` | ~144 | D | `mmrds` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}audit/_security` | `mmria-server/util/c_db_setup.cs` | ~200 | D | `audit` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}session/_security` | `mmria-server/util/c_db_setup.cs` | ~229 | D | `session` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}export_queue/_security` | `mmria-server/util/c_db_setup.cs` | ~267 | D | `export_queue` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}offline_cases/_security` | `mmria-server/util/c_db_setup.cs` | ~288 | D | `offline_cases` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}logging/_security` | `mmria-server/util/c_db_setup.cs` | ~307 | D | `logging` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}db_rebuild/_security` | `mmria-server/util/c_db_setup.cs` | ~328 | D | `db_rebuild` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}jurisdiction/_security` | `mmria-server/util/c_db_setup.cs` | ~416 | D | `jurisdiction` | `IDatabaseLifecycleService` — 24.5 |
| PUT `metadata/_security` | `mmria-server/util/c_db_setup.cs` | ~488 | `{url}/metadata/_security` | `metadata` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}export_queue/_security` (Akka actor) | `mmria-server/model/actor/quartz/Rebuild_Export_Queue.cs` | ~74 | D | `export_queue` | `IExportQueueRepository.PurgeAndReinitializeAsync` — 24.4 |
| PUT `{prefix}export_queue/_security` (legacy IJob — dead code) | `mmria-server/model/rebuild_export_queue_job.cs` | ~88 | D | `export_queue` | `IExportQueueRepository.PurgeAndReinitializeAsync` — 24.4 (dead) |
| PUT `{prefix}db_rebuild/_security` (sync_all ensure-exists) | `mmria-server/util/c_document_sync_all.cs` | ~389 | D | `db_rebuild` | Stays in orchestration — 24.7 |
| PUT `{scheduleInfo.db_prefix}mmrds/_security` (CDC target) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~83 | S | `mmrds` (CDC target) | `IDatabaseLifecycleService` extension — 24.9 |

#### PUT Design Document (PUT `{prefix}{db}/_design/...`)

| Operation | File | ~Line | URL Pattern | Target DB / Design | Owning Interface / Story |
|-----------|------|-------|-----------|--------------------|--------------------------|
| PUT `{prefix}mmrds/_design/sortable` | `mmria-server/util/c_db_setup.cs` | ~154 | D | `mmrds/_design/sortable` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}mmrds/_design/auth` | `mmria-server/util/c_db_setup.cs` | ~163 | D | `mmrds/_design/auth` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}audit/_design/sortable` | `mmria-server/util/c_db_setup.cs` | ~210 | D | `audit/_design/sortable` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}session/_design/session_event_sortable` | `mmria-server/util/c_db_setup.cs` | ~243 | D | `session/_design/session_event_sortable` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}session/_design/session_sortable` | `mmria-server/util/c_db_setup.cs` | ~250 | D | `session/_design/session_sortable` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}offline_cases/_design/sortable` | `mmria-server/util/c_db_setup.cs` | ~296 | D | `offline_cases/_design/sortable` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}logging/_design/sortable` | `mmria-server/util/c_db_setup.cs` | ~315 | D | `logging/_design/sortable` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}jurisdiction/_design/sortable` | `mmria-server/util/c_db_setup.cs` | ~424 | D | `jurisdiction/_design/sortable` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}jurisdiction/_design/auth` | `mmria-server/util/c_db_setup.cs` | ~430 | D | `jurisdiction/_design/auth` | `IDatabaseLifecycleService` — 24.5 |
| PUT `metadata/_design/auth` | `mmria-server/util/c_db_setup.cs` | ~493 | `{url}/metadata/_design/auth` | `metadata/_design/auth` | `IDatabaseLifecycleService` — 24.5 |
| PUT `metadata/_design/sortable` | `mmria-server/util/c_db_setup.cs` | ~545 | `{url}/metadata/_design/sortable` | `metadata/_design/sortable` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}de_id/_design/sortable` (full rebuild) | `mmria-server/util/c_document_sync_all.cs` | ~855 | D | `de_id/_design/sortable` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| PUT `{prefix}report/_design/interactive_aggregate_report` | `mmria-server/util/c_document_sync_all.cs` | ~893 | D | `report/_design/interactive_aggregate_report` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| PUT `{prefix}report/_design/data_summary_view_report` | `mmria-server/util/c_document_sync_all.cs` | ~908 | D | `report/_design/data_summary_view_report` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| PUT `{prefix}de_id/_design/sortable` (legacy rebuild) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~248 | D | `de_id/_design/sortable` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| PUT `{prefix}report/_design/interactive_aggregate_report` (legacy) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~303 | D | `report/_design/interactive_aggregate_report` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| PUT `{prefix}report/_design/data_summary_view_report` (legacy) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~316 | D | `report/_design/data_summary_view_report` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| PUT `{prefix}de_id/_design/sortable` (PMSS rebuild) | `mmria-server/util/c_document_sync_all.pmss.cs` | ~140 | D | `de_id/_design/sortable` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| PUT `{prefix}report/_design/interactive_aggregate_report` (PMSS) | `mmria-server/util/c_document_sync_all.pmss.cs` | ~188 | D | `report/_design/interactive_aggregate_report` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| PUT `{prefix}report/_design/data_summary_view_report` (PMSS) | `mmria-server/util/c_document_sync_all.pmss.cs` | ~198 | D | `report/_design/data_summary_view_report` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| PUT `{prefix}de_id/_design/sortable` (common legacy rebuild) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~248 | D | `de_id/_design/sortable` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| PUT `{prefix}report/_design/interactive_aggregate_report` (common legacy) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~303 | D | `report/_design/interactive_aggregate_report` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| PUT `{prefix}report/_design/data_summary_view_report` (common legacy) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~316 | D | `report/_design/data_summary_view_report` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| PUT `{scheduleInfo.db_prefix}mmrds/_design/sortable` (CDC target) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~92 | S | `mmrds/_design/sortable` (CDC target) | `IDatabaseLifecycleService` extension — 24.9 (**AC-4**) |
| PUT `{scheduleInfo.db_prefix}mmrds/_design/auth` (CDC target) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~98 | S | `mmrds/_design/auth` (CDC target) | `IDatabaseLifecycleService` extension — 24.9 (**AC-4**) |
| PUT `{scheduleInfo.db_prefix}de_id/_design/sortable` (CDC target) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~143 | S | `de_id/_design/sortable` (CDC target) | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.9 (**AC-4**) |
| PUT `{prefix}de_id/_design/sortable` (CDC services rebuild) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~468 | D | `de_id/_design/sortable` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.9 (**AC-4**) |
| PUT `{prefix}report/_design/interactive_aggregate_report` (CDC services) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~522 | D | `report/_design/interactive_aggregate_report` | `IReportRepository` lifecycle — 24.2 / 24.9 (**AC-4**) |
| PUT `{prefix}report/_design/data_summary_view_report` (CDC services) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~556 | D | `report/_design/data_summary_view_report` | `IReportRepository` lifecycle — 24.2 / 24.9 (**AC-4**) |

#### POST Mango Index (POST `{prefix}{db}/_index`)

| Operation | File | ~Line | URL Pattern | Target DB / Index | Owning Interface / Story |
|-----------|------|-------|-------------|-------------------|--------------------------|
| POST `{prefix}report/_index` (opioid-report-index) | `mmria-server/util/c_document_sync_all.cs` | ~869 | D | `report/_index` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| POST `{prefix}report/_index` (powerbi-report-index) | `mmria-server/util/c_document_sync_all.cs` | ~880 | D | `report/_index` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| POST `{prefix}report/_index` (opioid, legacy) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~273 | D | `report/_index` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| POST `{prefix}report/_index` (powerbi, legacy) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~289 | D | `report/_index` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| POST `{prefix}report/_index` (opioid, PMSS) | `mmria-server/util/c_document_sync_all.pmss.cs` | ~166 | D | `report/_index` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| POST `{prefix}report/_index` (powerbi, PMSS) | `mmria-server/util/c_document_sync_all.pmss.cs` | ~177 | D | `report/_index` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| POST `{prefix}report/_index` (opioid, common legacy) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~273 | D | `report/_index` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| POST `{prefix}report/_index` (powerbi, common legacy) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~289 | D | `report/_index` | `IReportRepository` lifecycle — 24.2 / 24.7 (**AC-4**) |
| POST `{scheduleInfo.db_prefix}report/_index` (opioid, CDC target) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~176 | S | `report/_index` (CDC target) | `IReportRepository` lifecycle — 24.2 / 24.9 (**AC-4**) |
| POST `{prefix}report/_index` (opioid, CDC services) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~501 | D | `report/_index` | `IReportRepository` lifecycle — 24.2 / 24.9 (**AC-4**) |
| POST `{prefix}report/_index` (powerbi, CDC services) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~512 | D | `report/_index` | `IReportRepository` lifecycle — 24.2 / 24.9 (**AC-4**) |

#### Barrier Queries / Index Warmup (GET `{prefix}{db}/_design/.../_view/...?update=true`)

These `GET` calls are not data reads — they block until the named index/view has finished building after a fresh `_design` or `_index` PUT. Owned by the same lifecycle interfaces as the PUT/POST operations above.

| Operation | File | ~Line | URL Pattern | Target DB / View | Owning Interface / Story |
|-----------|------|-------|-------------|------------------|--------------------------|
| GET `{prefix}de_id/_design/sortable/_view/by_date_created?limit=1&update=true` | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~510 | D | `de_id` | `IDeIdentifiedRepository` lifecycle — 24.2 / 24.7 |
| GET `{prefix}report/_design/interactive_aggregate_report/_view/indicator_id?limit=1&update=true` | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~520 | D | `report` | `IReportRepository` lifecycle — 24.2 / 24.7 |
| GET `{prefix}report/_design/data_summary_view_report/_view/year_of_death?limit=1&update=true` | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~530 | D | `report` | `IReportRepository` lifecycle — 24.2 / 24.7 |
| POST `{prefix}report/_find` (with `use_index` barrier — opioid/powerbi) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~480 | D | `report` | `IReportRepository` lifecycle — 24.2 / 24.7 |

---

### Paged Bulk Read (`_all_docs` with cursor/skip)

These are the source-read operations that drive rebuild orchestration. See also **Boundary Decision 2** (Story 17.7) which explicitly declared the mmrds sync reads infrastructure-only. For Epic 24, the cursor-based reads are extended into `ICaseRepository` as new paged-read methods.

| Operation | File | ~Line | URL Pattern | Source DB | Owning Interface / Story |
|-----------|------|-------|-------------|-----------|--------------------------|
| GET `{prefix}mmrds/_all_docs?include_docs=true&startkey=...&limit=...` (cursor-based, full rebuild) | `mmria-server/util/c_document_sync_all.cs` | ~936 | D | `mmrds` | `ICaseRepository.GetPagedCasesAsync` — 24.3 / 24.7 |
| POST `{prefix}de_id/_all_docs?include_docs=false` + keys body (rev lookup before bulk write) | `mmria-server/util/c_document_sync_all.cs` | ~994 | D | `de_id` | `IDeIdentifiedRepository.GetBulkRevisionsAsync` — 24.2 / 24.7 |
| POST `{prefix}report/_all_docs?include_docs=false` + keys body (rev lookup before bulk write) | `mmria-server/util/c_document_sync_all.cs` | ~994 | D | `report` | `IReportRepository.GetBulkRevisionsAsync` — 24.2 / 24.7 |
| GET `{prefix}mmrds/_all_docs?skip=...&limit=...` (skip-based, legacy rebuild) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~155 | D | `mmrds` | `ICaseRepository.GetPagedCasesAsync` — 24.3 / 24.7 |
| GET `{prefix}mmrds/_all_docs?skip=...&limit=...` (skip-based, PMSS rebuild) | `mmria-server/util/c_document_sync_all.pmss.cs` | ~241 | D | `mmrds` | `ICaseRepository.GetPagedCasesAsync` — 24.3 / 24.7 |
| GET `{prefix}mmrds/_all_docs?skip=...&limit=...` (skip-based, common legacy rebuild) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~155 | D | `mmrds` | `ICaseRepository.GetPagedCasesAsync` — 24.3 / 24.7 |
| GET `{prefix}mmrds/_all_docs` (ID reconciliation — streaming JsonDocument) | `mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | ~170 | D | `mmrds` | `ICaseRepository.GetAllCaseIdsAsync` — 24.3 / 24.8 |
| GET `{prefix}de_id/_all_docs` (ID reconciliation) | `mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | ~180 | D | `de_id` | `IDeIdentifiedRepository.GetAllIdsAsync` — 24.2 / 24.8 |
| GET `{prefix}report/_all_docs` (ID reconciliation) | `mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | ~198 | D | `report` | `IReportRepository.GetAllIdsAsync` — 24.2 / 24.8 |
| GET `{db_info.prefix}mmrds/_all_docs?include_docs=true` (CDC source read — **reads from source CDC instance**) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~200 | CDC-src | `mmrds` (source) | CDC-specific read — stays infra or new `ICdcCaseSourceReader` — 24.9 |
| GET `{prefix}mmrds/_all_docs?include_docs=true&startkey=...&limit=...` (cursor-based, CDC services) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~202 | D | `mmrds` | `ICaseRepository.GetPagedCasesAsync` — 24.3 / 24.9 |
| GET `{prefix}mmrds/_all_docs?limit=0` (total count probe, CDC services) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~260 | D | `mmrds` | `ICaseRepository.GetCaseCountAsync` — 24.3 / 24.9 |
| GET `{prefix}mmrds/_all_docs?startkey=_design/&endkey=_design0` (design doc count, CDC services) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~272 | D | `mmrds` | `ICaseRepository.GetCaseCountAsync` (folded into count logic) — 24.3 / 24.9 |

> **CDC-src note (Process_Central_Pull_list):** The source `_all_docs` read at line ~200 reads from `db_info.url/{db_info.prefix}mmrds`, where `db_info` is a source CDC instance — a completely different CouchDB server. This cannot share the same `ICaseRepository` instance as the target without CDC-specific config branching. Story 24.9 must decide whether to wrap this in a CDC-specific interface or leave as a direct infra call.

---

### Change-Stream Read (`_changes`)

| Operation | File | ~Line | URL Pattern | Source DB | Owning Interface / Story |
|-----------|------|-------|-------------|-----------|--------------------------|
| GET `{prefix}mmrds/_changes` (startup last_seq probe) | `mmria-server/util/c_db_setup.cs` | ~342 | D | `mmrds` | `IDatabaseLifecycleService` — 24.5 |
| GET `{prefix}mmrds/_changes` (since=last_seq, sync feed) | `mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | ~220 | D | `mmrds` | `ICaseRepository.GetChangesSinceAsync` — 24.3 / 24.8 |
| GET `{prefix}mmrds/_changes` (no since — full feed initial call) | `mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | ~224 | D | `mmrds` | `ICaseRepository.GetChangesSinceAsync` — 24.3 / 24.8 |

---

### Per-Document CRUD

#### Per-Document Read (GET `{prefix}{db}/{id}`)

| Operation | File | ~Line | URL Pattern | Source DB | Owning Interface / Story |
|-----------|------|-------|-------------|-----------|--------------------------|
| GET `{prefix}de_id/{id}` (revision probe before PUT/DELETE) | `mmria-server/util/c_sync_document.pmss.cs` | ~68 | D | `de_id` | `IDeIdentifiedRepository` — 24.2 / 24.6 |
| GET `{prefix}report/freq-{id}` (revision probe) | `mmria-server/util/c_sync_document.pmss.cs` | ~186 | D | `report` | `IReportRepository` — 24.2 / 24.6 |
| GET `{prefix}mmrds/{id}` (per-doc fetch inside PMSS rebuild loop) | `mmria-server/util/c_document_sync_all.pmss.cs` | ~257 | D | `mmrds` | `ICaseRepository.GetCaseDocumentJsonAsync` — 24.7 |
| GET `{prefix}mmrds/{id}` (per-doc fetch inside legacy rebuild loop) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~180 | D | `mmrds` | `ICaseRepository.GetCaseDocumentJsonAsync` — 24.7 |
| GET `{prefix}mmrds/{id}` (per-doc fetch inside common legacy rebuild loop) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~180 | D | `mmrds` | `ICaseRepository.GetCaseDocumentJsonAsync` — 24.7 |
| GET `{prefix}mmrds/{id}` (change-feed per-case sync — PUT branch) | `mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | ~113 | D | `mmrds` | `ICaseRepository.GetCaseDocumentJsonAsync` — 24.8 |
| GET `{target}/{db_prefix}mmrds/{id}` (revision probe before CDC write) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~225 | S | `mmrds` (CDC target) | `ICaseRepository.GetCaseAsync` (target instance) — 24.9 |
| GET `{prefix}db_rebuild/startup-run-summary` (run summary read) | `mmria-server/util/c_document_sync_all.cs` | ~451 | D | `db_rebuild` | Internal orchestration state — stays in 24.7 |
| GET `{prefix}db_rebuild/startup-rebuild-status` (legacy checkpoint read) | `mmria-server/util/c_document_sync_all.cs` | ~404 | D | `db_rebuild` | Internal orchestration state — stays in 24.7 |

#### Per-Document Write (PUT / DELETE `{prefix}{db}/{id}`)

| Operation | File | ~Line | URL Pattern | Target DB | Owning Interface / Story |
|-----------|------|-------|-------------|-----------|--------------------------|
| PUT/DELETE `{prefix}de_id/{id}` (per-case sync) | `mmria-server/util/c_sync_document.pmss.cs` | ~174 | D | `de_id` | `IDeIdentifiedRepository.WriteDocumentAsync` — 24.2 / 24.6 |
| PUT/DELETE `{prefix}report/freq-{id}` (per-case freq-detail report sync) | `mmria-server/util/c_sync_document.pmss.cs` | ~228 | D | `report` | `IReportRepository.WriteDocumentAsync` — 24.2 / 24.6 |
| PUT `{prefix}de_id/{id}` (per-case write, legacy rebuild compatibility path) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~348 | D | `de_id` | `IDeIdentifiedRepository.WriteDocumentAsync` — 24.2 / 24.7 |
| PUT `{prefix}report/{id}` (per-case write, legacy rebuild compatibility path) | `mmria-server/util/c_document_sync_all_legacy.cs` | ~348 | D | `report` | `IReportRepository.WriteDocumentAsync` — 24.2 / 24.7 |
| PUT `{prefix}de_id/{id}` (per-case write, common legacy rebuild) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~348 | D | `de_id` | `IDeIdentifiedRepository.WriteDocumentAsync` — 24.2 / 24.7 |
| PUT `{prefix}report/{id}` (per-case write, common legacy rebuild) | `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs` | ~348 | D | `report` | `IReportRepository.WriteDocumentAsync` — 24.2 / 24.7 |
| PUT `{prefix}de_id/{id}` (per-case compatibility path, main sync_all) | `mmria-server/util/c_document_sync_all.cs` | ~1220 | D | `de_id` | `IDeIdentifiedRepository.WriteDocumentAsync` — 24.2 / 24.7 |
| PUT `{prefix}report/{id}` (per-case compatibility path, main sync_all) | `mmria-server/util/c_document_sync_all.cs` | ~1220 | D | `report` | `IReportRepository.WriteDocumentAsync` — 24.2 / 24.7 |
| PUT `{target}/{db_prefix}mmrds/{id}` (CDC de-ided case write to target) | `mmria-server/model/actor/quartz/Process_Central_Pull_list.cs` | ~241 | S | `mmrds` (CDC target) | `ICaseRepository.SaveCaseAsync` (target instance) — 24.9 |
| DELETE `{prefix}de_id/{id}?rev=...` (orphan cleanup — dead branch, Union bug) | `mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | ~206 | D | `de_id` | Dead branch (see Process_DB_Synchronization_Set note) — 24.8 |
| DELETE `{prefix}report/{id}?rev=...` (orphan cleanup — dead branch, Union bug) | `mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | ~214 | D | `report` | Dead branch (see Process_DB_Synchronization_Set note) — 24.8 |
| PUT `{prefix}db_rebuild/startup-run-summary` (run summary write) | `mmria-server/util/c_document_sync_all.cs` | ~636 | D | `db_rebuild` | Internal orchestration state — stays in 24.7 |
| DELETE `{prefix}db_rebuild/startup-rebuild-status?rev=...` (legacy checkpoint delete) | `mmria-server/util/c_document_sync_all.cs` | ~430 | D | `db_rebuild` | Internal orchestration state — stays in 24.7 |
| PUT `metadata/{id}` (migration plan document PUT) | `mmria-server/util/c_db_setup.cs` | ~560+ | `{url}/metadata/{id}` | `metadata` | `IDatabaseLifecycleService` — 24.5 |
| PUT `metadata/2016-06-12T13:49:24.759Z` (metadata seed document) | `mmria-server/util/c_db_setup.cs` | ~510 | `{url}/metadata/...` | `metadata` | `IDatabaseLifecycleService` — 24.5 |
| PUT `metadata/2016-06-12T13:49:24.759Z/mmria-check-code.js` (metadata attachment) | `mmria-server/util/c_db_setup.cs` | ~520 | `{url}/metadata/.../attachment` | `metadata` | `IDatabaseLifecycleService` — 24.5 |
| PUT `metadata/de-identified-list` | `mmria-server/util/c_db_setup.cs` | ~610 | `{url}/metadata/de-identified-list` | `metadata` | `IDatabaseLifecycleService` — 24.5 |
| PUT `{prefix}jurisdiction/jurisdiction_tree` (seed doc) | `mmria-server/util/c_db_setup.cs` | ~440 | D | `jurisdiction` | `IDatabaseLifecycleService` — 24.5 |

> **Process_DB_Synchronization_Set dead-branch note:** The orphan-cleanup DELETE calls at ~206 and ~214 are inside loops that never execute. The preceding `deleted_id_set.Union(...)` call discards its return value — `deleted_id_set` remains empty. These loops are preserved from the original code for future correctness and must be included in the interface design even though they currently never fire.

---

### Bulk Write (`_bulk_docs`)

| Operation | File | ~Line | URL Pattern | Target DB | Owning Interface / Story |
|-----------|------|-------|-------------|-----------|--------------------------|
| POST `{prefix}de_id/_bulk_docs` (batch write — bulk mode) | `mmria-server/util/c_document_sync_all.cs` | ~1098 | D | `de_id` | `IDeIdentifiedRepository.BulkWriteAsync` — 24.2 / 24.7 |
| POST `{prefix}report/_bulk_docs` (batch write — bulk mode) | `mmria-server/util/c_document_sync_all.cs` | ~1098 | D | `report` | `IReportRepository.BulkWriteAsync` — 24.2 / 24.7 |
| POST `{prefix}de_id/_bulk_docs` (CDC services rebuild) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~325 | D | `de_id` | `IDeIdentifiedRepository.BulkWriteAsync` — 24.2 / 24.9 |
| POST `{prefix}report/_bulk_docs` (CDC services rebuild) | `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` | ~325 | D | `report` | `IReportRepository.BulkWriteAsync` — 24.2 / 24.9 |

---

### AC-5 Finding: `rebuild_export_queue_job.cs` Registration Status

**Finding:** `rebuild_export_queue_job.cs` (class `rebuild_queue_job : IJob`) is **dead code** — not actively registered in the Quartz scheduler.

- The Quartz registration block for `rebuild_queue_job` in `QuartzSupervisor.cs` (lines ~258–272) is inside a large `/* ... */` comment block. The opening `/*` is approximately at line ~220 and the closing `*/` is at approximately line ~292.
- The active export queue rebuild is handled by the `Rebuild_Export_Queue` Akka actor (in `Rebuild_Export_Queue.cs`), which is registered and called via the Akka actor system.

**Story 24.4 approach:** `Rebuild_Export_Queue.cs` (Akka actor) is the **live** call site to route through `IExportQueueRepository.PurgeAndReinitializeAsync`. `rebuild_export_queue_job.cs` may be left in place (it will remain dead) or deleted — the story should note this and leave the decision to the developer.

---

### AC-4 Routing Decision: Design Doc and Index Operations

Design document PUTs and Mango index POSTs in all sync/rebuild files are **DB-lifecycle operations**, NOT application CRUD. They are routed as follows:

| Database | Operation type | Owning Interface | Story |
|----------|---------------|-----------------|-------|
| `de_id` | PUT `_design/sortable` | `IDeIdentifiedRepository` lifecycle method | 24.2 |
| `report` | POST `_index` (opioid, powerbi) | `IReportRepository` lifecycle method | 24.2 |
| `report` | PUT `_design/interactive_aggregate_report` | `IReportRepository` lifecycle method | 24.2 |
| `report` | PUT `_design/data_summary_view_report` | `IReportRepository` lifecycle method | 24.2 |
| `mmrds` | PUT `_design/sortable`, PUT `_design/auth` | `IDatabaseLifecycleService` | 24.5 / 24.9 |
| `report` barrier queries | GET `_design/.../_view/...?update=true` | `IReportRepository` lifecycle method | 24.2 |
| `de_id` barrier queries | GET `_design/sortable/_view/...?update=true` | `IDeIdentifiedRepository` lifecycle method | 24.2 |

These operations are **not** `IReportRepository.GetReportAsync` / `IDeIdentifiedRepository.GetAsync` — they are lifecycle setup methods added to each interface as part of Story 24.2. The SQL migration replaces them with schema-migration tooling; they are not part of the runtime data access path.

---

### AC-3 Note: CDC Services `c_document_sync_all.cs` Characteristics

**File:** `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs`

This file has three CDC-specific characteristics that distinguish it from the mmria-server sync_all variants:

1. **Cursor-based pagination:** Source reads use `startkey`/`limit` cursor paging (not skip-based), matching the mmria-server non-PMSS `c_document_sync_all.cs`. This maps cleanly to `ICaseRepository.GetPagedCasesAsync`.

2. **Bulk-write throttling:** Writes to de_id and report use `bulk_write_async` with configurable `chunk_size`, `retry_count`, and `retry_delay_ms` via `PopulateCdcThrottleSettings`. The `IDeIdentifiedRepository.BulkWriteAsync` and `IReportRepository.BulkWriteAsync` signatures must accept these throttle parameters or accept them via a separate throttle config overload.

3. **Metadata already routed through DAL:** Both `_metadataRepository.GetAppDocumentAsync(...)` and `_metadataRepository.GetDeIdentifiedListAsync(...)` are already correct — they use `IMetadataRepository` (via `MetadataVersionDAL`). These calls require **no change** in Epic 24. Only the direct `de_id`/`report`/`mmrds` `ExecuteAsync` calls require routing.

---

### Summary Counts (Epic 24)

| Category | Distinct call sites (across 11 files) |
|----------|--------------------------------------|
| DB lifecycle — CREATE database | 28 |
| DB lifecycle — DELETE database | 16 |
| DB lifecycle — SECURITY | 13 |
| DB lifecycle — PUT design doc | 28 |
| DB lifecycle — POST Mango index | 11 |
| DB lifecycle — barrier queries / warmup | 4 |
| Paged bulk read (`_all_docs`) | 14 |
| Change-stream read (`_changes`) | 3 |
| Per-document read | 10 |
| Per-document write (PUT/DELETE) | 18 |
| Bulk write (`_bulk_docs`) | 4 |
| **Total in-scope call sites** | **~149** |

> Dead-code call sites (rebuild_export_queue_job.cs Quartz IJob — 3 calls; Process_DB_Synchronization_Set orphan-cleanup DELETE loop — 2 calls) are included in the count above since they are present in source code and must be addressed in each story even if they produce no runtime effect.
