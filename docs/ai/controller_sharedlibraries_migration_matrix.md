# Controller -> SharedLibraries Migration Matrix

This document captures the current migration plan for moving server-side controller logic into `mmria.common/SharedLibraries` using the feature-based `Model/Manager/DAL` pattern required by [AI_CONTEXT.md](./AI_CONTEXT.md).

Use this document before refactoring controller code in:
- `source-code/mmria/mmria-server/Controllers`
- `source-code/mmria/mmria-server/Controllers/api`

## Scope Summary

Current inventory from the controller analysis:

- `53` non-API controllers under `Controllers/`
- `52` API controllers under `Controllers/api/`
- `38/53` non-API controllers have no direct `CouchDbHttpClient.ExecuteAsync` usage and are mostly route/view shells
- `40/52` API controllers still make direct CouchDB calls from controller code

The migration target is not "all controllers". The target is the subset still mixing:
- route handling
- tenant resolution
- authorization checks tied to data
- CouchDB access
- file or external-service orchestration
- business rules

## Refactoring Rules For This Project

These rules are derived from [AI_CONTEXT.md](./AI_CONTEXT.md) and the current server/common project split.

- Preserve routes, action signatures, return types, and response shapes.
- Move business logic into `mmria.common/SharedLibraries/<Feature>/Manager`.
- Move CouchDB calls into `mmria.common/SharedLibraries/<Feature>/DAL`.
- Keep `HttpContext`, `User`, `View()`, `Json()`, `File()`, cookies, headers, and actor dispatch in controllers on the first pass.
- Keep tenant resolution in controllers on the first pass. Resolve `host_prefix`, `configuration`, and `db_config` in the controller, then pass them into managers.
- Do not move Akka actor creation or `ActorSystem` dependencies into `mmria.common` on the first pass.
- Do not do namespace cleanup or architectural cleanup that is not required for the mechanical move.
- When extracting code, do not add outer `try/catch` blocks in Manager or DAL methods.

## Key Things Learned During Analysis

- The common project already has SharedLibraries for `Account`, `AggregateReport`, `Case`, `CaseView`, `InteractiveReport`, `ManageUsers`, `MMRIAServices`, `OfflineCase`, and `Session`.
- Existing adoption is partial. Several controllers still bypass those libraries and call CouchDB directly.
- `manage_usersController` currently constructs other controllers directly to assemble initial page data. This is a good early target for an orchestration manager because it is high-friction and low-risk.
- Tenant resolution currently depends on server-side helpers:
  - `mmria.server.extension.GetPrefix()`
  - `mmria.server.util.MultiTenantConfigHelper`
  These should stay in the controller for the first migration pass.
- Actor-driven side effects are still server-owned. Controllers such as case save/import/export endpoints should keep actor dispatch in `mmria-server` until an explicit abstraction is introduced.
- Several common libraries already use older namespaces such as `mmria.common.Manager` and `mmria.common.Model.*`. Do not stop the migration to normalize those unless explicitly requested.
- PMSS split files (`*.pmss.cs`) should be handled as follow-on mirrors of the non-PMSS refactor rather than as the first implementation target.
- File-backed and external-service-backed endpoints should only move their business logic and request-building code. File streaming and MVC response plumbing should stay in the controller.
- Round 1 implementation completed for the `ManageUsers` wave. The touched controllers now delegate to `ManageUsersManager`, and direct `CouchDbHttpClient.ExecuteAsync(...)` calls were removed from the Round 1 target controllers.
- Round 1 also required a compatibility fix in legacy `Controllers/_usersController.cs` because it manually instantiated the refactored API controllers with the old constructor shape.
- Round 2 implementation completed for the `Session` wave. The touched controllers now delegate to `SessionManager`, and direct session CouchDB/WebRequest logic was removed from the Round 2 target controllers.
- Round 2 kept cookie/header access, route shapes, and `ViewBag`/`View()` composition in `mmria-server` while moving session list retrieval, `_session` orchestration, and password-expiration data lookup into `Session/Manager` and `Session/DAL`.
- Round 3 implementation completed for the metadata/version wave using a new `MetadataVersion` feature in `mmria.common/SharedLibraries`.
- Round 3 moved metadata document CRUD, attachment reads/writes, revision lookup, version save gating, UI specification CRUD, and validator/check-code attachment orchestration into `MetadataVersion/Manager` and `MetadataVersion/DAL`.
- Round 3 intentionally left the `versionController` `export-names` action using the existing server-side utility because that path depends on server-only code and moving it would broaden the refactor beyond the “move as-is” goal.

## Spreadsheet-Style Matrix

Column definitions:

- `Wave`: suggested implementation order
- `Status`: current migration tracking state
- `Controller`: current controller file
- `Target Feature`: destination SharedLibraries feature
- `Current State`: existing level of manager/DAL adoption
- `Move To Manager`: business/orchestration logic to extract
- `Move To DAL`: CouchDB or remote data access to extract
- `Leave In Controller`: code that should remain in `mmria-server` in pass 1
- `Risk`: relative migration difficulty
- `Notes`: dependency or implementation notes

Suggested status values:

- `planned`: not started yet
- `in progress`: actively being migrated
- `partially migrated`: some important logic already moved, but controller still contains direct business/data logic
- `largely aligned`: already close to target shape; only small follow-up work remains
- `defer`: intentionally not a current migration target

| Wave | Status | Controller | Target Feature | Current State | Move To Manager | Move To DAL | Leave In Controller | Risk | Notes |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `largely aligned` | `Controllers/api/userController.cs` | `ManageUsers` | Round 1 extracted remaining user lookup, filtered list, and delete-authorization fetch logic into `ManageUsers` | user lookup, user list filtering, app-prefix logic, save/delete orchestration | `/_users` GET/PUT/DELETE calls | route/actions and final HTTP responses | Low | Completed in Round 1; no direct CouchDB calls remain in this controller |
| 1 | `largely aligned` | `Controllers/api/user_role_jurisdictionController.cs` | `ManageUsers` | Round 1 extracted list/save/delete orchestration into `ManageUsers` | auth-aware orchestration, bulk save flow, single-record save flow | jurisdiction doc GET/PUT and `_bulk_docs` operations | route/actions and auth result handling | Low | Completed in Round 1; delete now delegates through manager/DAL |
| 1 | `largely aligned` | `Controllers/api/user_role_jurisdiction_viewController.cs` | `ManageUsers` | Round 1 moved `my-roles` and filtered sortable-view logic into `ManageUsers` | role list loading, filtering, current-user view assembly | sortable view queries against `jurisdiction` DB | route/actions and current `User` access | Medium | Completed in Round 1; controller remains a thin wrapper around manager calls |
| 1 | `largely aligned` | `Controllers/manage_usersController.cs` | `ManageUsers` | Round 1 replaced controller-to-controller composition with manager orchestration | `GetInitialData()` aggregation, form-access load/save orchestration | form-access doc reads/writes in `jurisdiction` DB | `View()` actions and final `JsonResult` | Medium | Completed in Round 1; form-access models are now shared in `ManageUsers/Model` |
| 2 | `largely aligned` | `Controllers/api/sessionController.cs` | `Session` | Round 2 moved session list/search/filter and session document save orchestration into `Session` | session list/search/filter logic and session save orchestration | session sortable view and session document GET/PUT calls | route/actions and response shaping | Low | Completed in Round 2; controller no longer performs direct session CouchDB access |
| 2 | `largely aligned` | `Controllers/api/sessionDBController.cs` | `Session` | Round 2 moved `_session` orchestration into `Session` | auth-session inspection and session DB orchestration | direct `_session` calls and cookie-aware request creation | request cookie/header handling and final action results | Medium | Completed in Round 2; preserved cookie pass-through semantics while moving the HTTP work into DAL |
| 2 | `largely aligned` | `Controllers/HomeController.cs` | `Session` and `ManageUsers` | Round 2 replaced direct session-event and `_users` lookups with shared manager calls | password expiration calculation and power-BI user lookup orchestration | session event and `_users` queries | `ViewBag`, `View()`, route action | Medium | Completed in Round 2; page composition remains in controller and user lookup now reuses `ManageUsersManager` |
| 3 | `largely aligned` | `Controllers/api/metadataController.cs` | `MetadataVersion` | Round 3 extracted metadata doc and check-code orchestration into `MetadataVersion` | metadata load/save orchestration, version-spec save passthrough, check-code save orchestration | metadata doc GET/PUT and revision lookup | request-body reads and final response formatting | Low | Completed in Round 3; controller no longer performs direct CouchDB calls |
| 3 | `largely aligned` | `Controllers/api/versionController.cs` | `MetadataVersion` | Round 3 extracted version list/load/save, validator fetch, and attachment save orchestration into `MetadataVersion` | version list/load/save, attachment save orchestration | metadata/version doc and attachment reads/writes | `FileResult` creation, request-body plumbing, and `export-names` server utility orchestration | Medium | Completed in Round 3; `export-names` intentionally remains controller-owned because the helper is server-only |
| 3 | `largely aligned` | `Controllers/api/version_attachController.cs` | `MetadataVersion` | Round 3 moved version attachment save orchestration into `MetadataVersion` | version attachment save workflow and publish-state gating | metadata attachment GET/PUT calls and revision-dependent save path | body reading and manual form-body parsing | Low | Completed in Round 3; raw request parsing stays in controller for safety |
| 3 | `largely aligned` | `Controllers/api/version_code_genController.cs` | `MetadataVersion` | Round 3 moved validator fetch to `MetadataVersion` but left schema code generation in controller | validator fetch orchestration only | validator attachment GET | route/action, final `ContentResult`, and `NJsonSchema` code generation | Low | Completed in Round 3 with codegen intentionally left in server because `NJsonSchema` is not referenced by `mmria.common` |
| 3 | `largely aligned` | `Controllers/api/ui_specificationController.cs` | `MetadataVersion` | Round 3 extracted UI specification CRUD into `MetadataVersion` | UI specification load/save/delete orchestration | metadata doc reads/writes and delete calls | route/action and final result | Low | Completed in Round 3; controller is now a thin wrapper |
| 3 | `largely aligned` | `Controllers/api/checkcodeController.cs` | `MetadataVersion` | Round 3 extracted check-code attachment get/put and revision handling into `MetadataVersion` | check-code load/save orchestration | metadata attachment/doc access and revision lookup | route/action and final result | Low | Completed in Round 3; namespace oddity left unchanged |
| 3 | `largely aligned` | `Controllers/api/validatorController.cs` | `MetadataVersion` | Round 3 extracted validator attachment get/put and revision handling into `MetadataVersion` | validator asset orchestration | metadata attachment/doc access and revision lookup | route/action, request-body reads, and final `FileResult` | Low | Completed in Round 3; file response behavior preserved in controller |
| 4 | `largely aligned` | `Controllers/_auditController.cs` | `AuditRecovery` | Round 4 moved audit query/load/detail orchestration into `AuditRecovery` | audit query orchestration, change-stack sorting/filtering, metadata-node lookup prep, audit master doc load/save | audit `_find`, case lookup, metadata fetch, audit document reads/writes | MVC view selection and view-model assembly | Medium | Completed in Round 4; Razor rendering stays in controller |
| 4 | `largely aligned` | `Controllers/api/AuditRecoverUtilController.cs` | `AuditRecovery` | Round 4 replaced duplicated audit query logic with `AuditRecoveryManager` | audit recovery workflows and shared audit list/detail orchestration | audit and case view data access | route/action and final results | Medium | Completed in Round 4; preserves jurisdiction-based config resolution |
| 4 | `largely aligned` | `Controllers/api/caseRevisionController.cs` | `AuditRecovery` | Round 4 moved active GET revision retrieval into `AuditRecovery` | revision retrieval orchestration | revision fetches from case DB | route/actions and actor-side dependency | Medium | Completed in Round 4; POST remains intentionally stubbed |
| 5 | `largely aligned` | `Controllers/api/case_viewController.cs` | `CaseView` | Round 5 moved record-id list and offline-documents orchestration into `CaseView` | record-id list, offline-documents filtering/orchestration | case view reads now flow through `CaseViewDAL` | route/actions and response shaping | Low | Completed in Round 5; controller no longer performs direct CouchDB calls |
| 5 | `largely aligned` | `Controllers/api/de_id_viewController.cs` | `CaseView` | `de_id_viewController` already delegated list behavior through `CaseViewManager`; Round 5 aligned manager data access through DAL | de-id view orchestration remains in existing manager path | de-id sortable view access now goes through `CaseViewDAL` via `CaseViewManager.execute(...)` | route/actions and final responses | Low | Completed in Round 5 without controller contract changes |
| 5 | `largely aligned` | `Controllers/api/pinned_casesController.cs` | `CaseView` | Round 5 moved pinned-case load/update workflows into `CaseView` | pinned case workflows | pinned case doc reads/writes | route/actions, request-body reads, and final responses | Low | Completed in Round 5; 404 create-default behavior preserved |
| 5 | `largely aligned` | `Controllers/api/isDuplicateCaseController.cs` | `CaseView` | Round 5 moved duplicate-case query and comparison logic into `CaseView` | duplicate detection workflow | duplicate-case CouchDB queries and case document fetches | route/action and final responses | Medium | Completed in Round 5; duplicate matching rules were moved as-is |
| 5 | `largely aligned` | `Controllers/api/caseRevisionList_case_viewController.cs` | `CaseView` | Round 5 moved revision-list query/filter orchestration into `CaseView` | revision-list query orchestration | list queries against case revision sources | route/action and final responses | Low | Completed in Round 5; controller is now a thin wrapper |
| 6 | `largely aligned` | `Controllers/api/vital_importController.cs` | `VitalImport` | Round 6 moved case-view search and case CRUD orchestration into `VitalImport` | authorization-aware import orchestration, case lookup/update/delete workflow | import-related case queries and writes | header access, actor dispatch, final action responses | High | Completed in Round 6; service-key check and sync actor dispatch remain in controller |
| 6 | `largely aligned` | `Controllers/api/pmss_csv_importController.cs` | `VitalImport` | Round 6 moved batch-list GET into `VitalImport` while leaving actor-driven import submission in the controller | PMSS batch-list orchestration only | import batch list access | actor dispatch and final responses | Medium | Completed in Round 6; POST actor flow and stubbed DELETE remain in server |
| 6 | `largely aligned` | `Controllers/api/export_queueController.cs` | `ExportQueue` | Round 6 moved export queue list/save/service-handoff orchestration into `ExportQueue` | export queue state transitions and orchestration | export queue doc reads/writes and service POST | route/actions and final responses | Medium | Completed in Round 6; controller still owns current-user extraction and HTTP surface |
| 6 | `largely aligned` | `Controllers/api/zipController.cs` | `ExportQueue` | Round 6 moved export item lookup/status update into `ExportQueue` | export item retrieval/update workflow | export queue document access | file/response handling | Medium | Completed in Round 6; file streaming remains in controller |
| 6 | `largely aligned` | `Controllers/api/populate_cdc_instanceController.cs` | `MMRIAServices` | Round 6 extended `MMRIAServices` to own Populate CDC Instance document/service orchestration | service-call orchestration and merged status assembly | service-facing data access and metadata doc reads/writes | route/action, request-body reads, and final responses | Medium | Completed in Round 6; kept raw body parsing in controller for minimal change |
| 7 | `planned` | `Controllers/api/attachmentController.cs` | `Attachment` | No SharedLibraries feature yet | attachment validation and operation sequencing | document metadata reads/writes if applicable | file system paths, file writes/deletes, `FileResult` | High | Keep local file operations in controller on first pass |
| 7 | `planned` | `Controllers/api/cvsAPIController.cs` | `CVS` | No SharedLibraries feature yet | request-building, response normalization, validation rules | external CVS API calls and any backing document access | file download responses and local file management | High | External API + file cache + auth role branching |
| 7 | `planned` | `Controllers/backup_managerController.cs` | `BackupAdmin` or `MMRIAServices` | No SharedLibraries feature yet | backup admin orchestration and service-call wrapping | remote backup service calls | file download/temp file handling and MVC responses | High | Strong file and external-service coupling |

## Controllers Already Largely Aligned

These do not need broad restructuring right now. They should only be touched incrementally when a specific action still contains leftover direct logic.

| Controller | Feature | Notes |
|---|---|---|
| `Controllers/AccountController.cs` | `Account` | Status: `largely aligned`. Already uses `AccountManager`; remaining controller work is mostly auth/session/cookie glue |
| `Controllers/AccountController.OIDC.cs` | `Account` | Status: `largely aligned`. Keep OIDC/SAMS HTTP flow and cookie handling in controller for now |
| `Controllers/api/caseController.cs` | `Case` | Status: `largely aligned`. Already uses `CaseManager`; keep actor dispatch in controller |
| `Controllers/api/OfflineCaseController.cs` | `OfflineCase` | Status: `largely aligned`. Already manager-backed and close to target shape |
| `Controllers/api/aggregate_reportController.cs` | `AggregateReport` | Status: `largely aligned`. Already manager-backed |
| `Controllers/api/interactive_report_viewController.cs` | `InteractiveReport` | Status: `largely aligned`. Already manager-backed |
| `Controllers/update_year_of_death.cs` | `Case` | Status: `largely aligned`. Already uses `CaseManager` for key operations |
| `Controllers/update_maiden_name.cs` | `Case` | Status: `largely aligned`. Already uses `CaseManager` for key operations |

## Controllers Probably Not Worth Moving Right Now

These are mostly route/view shells. Leave them alone unless another task already requires touching them.

- thin MVC view controllers under `Controllers/` that just return views
- simple route shims such as case landing pages and report shells
- controllers with no CouchDB or business logic beyond view selection

Suggested status for this group: `defer`

## Suggested Execution Order

1. `ManageUsers`
2. `Session`
3. `MetadataVersion`
4. `AuditRecovery`
5. `CaseView` follow-up
6. `VitalImport` and `ExportQueue`
7. `Attachment`, `CVS`, `BackupAdmin`

## Round 1 Update

Round 1 was implemented with the following outcomes:

- Feature home used: `mmria.common/SharedLibraries/ManageUsers`
- Added shared form-access models under `ManageUsers/Model`
- Expanded `ManageUsersManager` and `ManageUsersDAL` to own the remaining Round 1 user-management CouchDB and orchestration logic
- Registered `ManageUsersDAL` and `ManageUsersManager` in server DI
- Preserved routes, action signatures, and response shapes for the four target controllers
- Verified by build: `dotnet build source-code/mmria/mmria-server/mmria-server.csproj`

Round 1 implementation note:

- A compatibility update was required in legacy `Controllers/_usersController.cs` because it manually instantiated the refactored API controllers using the old constructor signature
- This was not a scope expansion of business logic; it was a compile-time compatibility fix caused by the Round 1 constructor changes

## Round 2 Update

Round 2 was implemented with the following outcomes:

- Feature home used: `mmria.common/SharedLibraries/Session`
- Expanded `SessionDAL` to own session sortable-view access, session document GET/PUT, `_session` GET/POST handling, and password-expiration event lookups
- Expanded `SessionManager` to own session list filtering, `_session` orchestration, session save orchestration, and password-expiration calculation support
- Preserved routes, action signatures, and response shapes for `sessionController` and `sessionDBController`
- Preserved `HomeController` page composition while replacing direct session and `_users` lookups with manager calls
- Verified by build: `dotnet build source-code/mmria/mmria-server/mmria-server.csproj`

Round 2 implementation notes:

- `sessionDBController` still owns request cookie access; only the actual `_session` HTTP work moved to `SessionDAL`
- `HomeController` power-BI user lookup now reuses `ManageUsersManager.GetMyUserAsync(...)` instead of adding a new home-specific data access path
- `SessionManager` preserves the existing password-expiration behavior by sorting session events the same way as the original controller logic

## Round 3 Update

Round 3 was implemented with the following outcomes:

- Feature home added: `mmria.common/SharedLibraries/MetadataVersion`
- Added `MetadataVersionDAL` to own metadata document reads/writes, attachment reads/writes, deletes, and revision lookup
- Added `MetadataVersionManager` to own metadata/version/UI-specification orchestration, validator/check-code save orchestration, and version publish-state gating
- Registered `MetadataVersionDAL` and `MetadataVersionManager` in server DI
- Preserved routes, action signatures, and response shapes for all seven Round 3 target controllers
- Verified by build: `dotnet build source-code/mmria/mmria-server/mmria-server.csproj`

Round 3 implementation notes:

- `versionController` still owns the `export-names` action because it depends on the existing server-only `export_all_generate_name_map` helper
- `version_attachController` still owns manual request-body parsing to avoid changing fragile form-body behavior
- `version_code_genController` still owns `NJsonSchema` code generation because `mmria.common` does not reference that package and adding it would be a broader dependency change

## Round 4 Update

Round 4 was implemented with the following outcomes:

- Feature home added: `mmria.common/SharedLibraries/AuditRecovery`
- Added `AuditRecoveryDAL` to own audit `_find`, case-view lookup, metadata attachment retrieval, audit document load/save, and revision fetches
- Added `AuditRecoveryManager` to own audit list/detail orchestration, metadata-node traversal, and audit recovery helpers shared between MVC and API controllers
- Registered `AuditRecoveryDAL` and `AuditRecoveryManager` in server DI
- Preserved routes, action signatures, and response/view-model shapes for `_auditController`, `AuditRecoverUtilController`, and the active GET path in `caseRevisionController`
- Verified by build: `dotnet build source-code/mmria/mmria-server/mmria-server.csproj -o c:\\repos\\nccdphp-drh-mmria\\artifacts\\round4-build-check`

Round 4 implementation notes:

- `_auditController` still owns Razor view rendering; only CouchDB/data orchestration moved to `AuditRecovery`
- `AuditRecoverUtilController` still resolves `configuration.GetDBConfig(jurisdiction_id)` in the controller to preserve current tenant/jurisdiction behavior
- `caseRevisionController` POST remains intentionally stubbed; only the active GET revision retrieval path was extracted

## Round 5 Update

Round 5 was implemented with the following outcomes:

- Feature home used: `mmria.common/SharedLibraries/CaseView`
- Added `CaseViewDAL` to own case-view reads, pinned-case reads/writes, and direct case document fetches used by duplicate detection
- Expanded `CaseViewManager` to own record-id retrieval, offline-document filtering, pinned-case orchestration, duplicate-case detection, and revision-list filtering
- Preserved routes, action signatures, and response shapes for `case_viewController`, `de_id_viewController`, `pinned_casesController`, `isDuplicateCaseController`, and `caseRevisionList_case_viewController`
- Verified by build: `dotnet build source-code/mmria/mmria-server/mmria-server.csproj -o c:\\repos\\nccdphp-drh-mmria\\artifacts\\round5-build-check`

Round 5 implementation notes:

- `de_id_viewController` did not need a route/action refactor; it became aligned because `CaseViewManager.execute(...)` now routes data access through `CaseViewDAL`
- `case_viewController` still owns tenant resolution and the public action surface; `record-id-list`, `offline-documents`, and `GetExistingRecordIds()` now delegate to `CaseViewManager`
- `pinned_casesController` still owns request-body parsing and the `everyone` authorization gate on `PUT`; the pinned-case load/save behavior moved as-is into `CaseViewManager`
- `isDuplicateCaseController` keeps its existing route and request shape; the duplicate matching algorithm was moved without changing comparison rules

## Round 6 Update

Round 6 was implemented with the following outcomes:

- Feature homes added: `mmria.common/SharedLibraries/VitalImport` and `mmria.common/SharedLibraries/ExportQueue`
- Extended existing `mmria.common/SharedLibraries/MMRIAServices` for Populate CDC Instance document/service orchestration
- Added `ExportQueueDAL` and `ExportQueueManager` to own export queue reads/writes, current-user list filtering, download status updates, and `mmria.services` export-queue handoff
- Added `VitalImportDAL` and `VitalImportManager` to own vital-import case-view search, case GET/PUT/DELETE orchestration, and PMSS batch-list retrieval
- Preserved routes, action signatures, and response shapes for `vital_importController`, `pmss_csv_importController`, `export_queueController`, `zipController`, and `populate_cdc_instanceController`
- Verified by build: `dotnet build source-code/mmria/mmria-server/mmria-server.csproj -o c:\\repos\\nccdphp-drh-mmria\\artifacts\\round6-build-check`

Round 6 implementation notes:

- `vital_importController` still owns `vitals_service_key` header access and sync actor dispatch; only the CouchDB/business orchestration moved to `VitalImportManager`
- `pmss_csv_importController` still owns the `batch-supervisor` actor `Ask(...)` flow and the stubbed DELETE path; only the batch-list GET moved
- `export_queueController` and `zipController` now share the `ExportQueue` feature, but `zipController` still owns local file reads and `FileResult`
- `populate_cdc_instanceController` now delegates document/service calls through `MMRIAServicesManager`, while keeping raw request-body parsing in the controller to avoid changing HTTP behavior

## First-Pass Refactoring Pattern

Use this pattern for each controller/action:

1. Keep constructor-time tenant resolution in the controller.
2. Create or extend a feature `Manager` in `mmria.common`.
3. Move controller helper methods and orchestration into the `Manager`.
4. Move all CouchDB request construction and execution into feature `DAL`.
5. Keep the controller's route attributes, action signatures, auth attributes, and response types unchanged.
6. Keep actor dispatch, cookies, `View()`, `Json()`, `File()`, headers, and request-body plumbing in the controller.
7. Do not reshape responses.

## Tracking Notes

When work starts on a controller listed above, update this document with:
- `Status`
- actual feature folder used
- actions completed
- notable blockers
- any route or response-shape sensitivity discovered during implementation

This document is intended to be the working index for controller-to-SharedLibraries migration planning across the repo.
