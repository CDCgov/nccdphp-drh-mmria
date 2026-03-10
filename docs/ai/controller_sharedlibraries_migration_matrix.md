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
| 1 | `partially migrated` | `Controllers/api/userController.cs` | `ManageUsers` | Partial; manager exists but controller still does direct CouchDB and filtering | user lookup, user list filtering, app-prefix logic, save/delete orchestration | `/_users` GET/PUT/DELETE calls | route/actions and final HTTP responses | Low | Existing `ManageUsersManager` and `ManageUsersDAL` reduce scope |
| 1 | `partially migrated` | `Controllers/api/user_role_jurisdictionController.cs` | `ManageUsers` | Partial; manager exists but controller still contains list/save logic | auth-aware orchestration, bulk save flow, single-record save flow | jurisdiction doc GET/PUT and `_bulk_docs` operations | route/actions and auth result handling | Low | Good early extraction target |
| 1 | `planned` | `Controllers/api/user_role_jurisdiction_viewController.cs` | `ManageUsers` | No meaningful manager usage | role list loading, filtering, current-user view assembly | sortable view queries against `jurisdiction` DB | route/actions and current `User` access | Medium | Contains user-claim and jurisdiction-access logic that should move carefully |
| 1 | `planned` | `Controllers/manage_usersController.cs` | `ManageUsers` | No shared orchestration manager; controller constructs other controllers | `GetInitialData()` aggregation, form-access load/save orchestration | form-access doc reads/writes in `jurisdiction` DB | `View()` actions and final `JsonResult` | Medium | Strong controller-to-controller coupling today; high-value cleanup |
| 2 | `planned` | `Controllers/api/sessionController.cs` | `Session` | Shared library exists but controller bypasses it | session list/search/filter logic | session and sortable view queries | route/actions and response shaping | Low | Good mechanical extraction |
| 2 | `planned` | `Controllers/api/sessionDBController.cs` | `Session` | No manager usage | auth-session inspection and session DB orchestration | direct `_session` calls and cookie-aware request creation | request cookie/header handling and final action results | Medium | Uses `WebRequest` and cookie pass-through; keep HTTP glue in controller |
| 2 | `planned` | `Controllers/HomeController.cs` | `Session` and `Account` | Partial; no manager use here | password expiration calculation, power-BI user lookup orchestration | session event and `_users` queries | `ViewBag`, `View()`, route action | Medium | Keep page composition in controller |
| 3 | `planned` | `Controllers/api/metadataController.cs` | `MetadataVersion` | No SharedLibraries feature yet | metadata load/save orchestration, check-code load/save | metadata DB GET/PUT calls and revision fetch | request-body reads and final response formatting | Low | Strong candidate for as-is move |
| 3 | `planned` | `Controllers/api/versionController.cs` | `MetadataVersion` | No SharedLibraries feature yet | version list/load/save, export-name-map orchestration | metadata/version doc and attachment reads/writes | `FileResult` creation and request-body plumbing | Medium | Large controller but mostly mechanical extraction |
| 3 | `planned` | `Controllers/api/version_attachController.cs` | `MetadataVersion` | No SharedLibraries feature yet | version attachment save workflow | metadata attachment GET/PUT calls | body reading and final HTTP result | Low | Natural subfeature of metadata/version |
| 3 | `planned` | `Controllers/api/version_code_genController.cs` | `MetadataVersion` | No SharedLibraries feature yet | version-code-gen orchestration | metadata/version fetches | route/action and final response | Low | Small and focused |
| 3 | `planned` | `Controllers/api/ui_specificationController.cs` | `MetadataVersion` | No SharedLibraries feature yet | UI specification load/save orchestration | metadata attachment/doc reads and writes | route/action and final result | Low | Move with metadata/version wave |
| 3 | `planned` | `Controllers/api/checkcodeController.cs` | `MetadataVersion` | No SharedLibraries feature yet | check-code load/save orchestration | metadata attachment/doc access | route/action and final result | Low | Move with metadata/version wave |
| 3 | `planned` | `Controllers/api/validatorController.cs` | `MetadataVersion` | No SharedLibraries feature yet | validator asset orchestration | metadata attachment/doc access | route/action and final result | Low | Move with metadata/version wave |
| 4 | `planned` | `Controllers/_auditController.cs` | `AuditRecovery` | No SharedLibraries feature yet | audit query orchestration, change-stack sorting/filtering, metadata-node lookup prep | audit `_find`, case lookup, metadata fetch | MVC view selection and view-model assembly | Medium | Keep Razor rendering in controller |
| 4 | `planned` | `Controllers/api/AuditRecoverUtilController.cs` | `AuditRecovery` | No SharedLibraries feature yet | audit recovery workflows | audit and case view data access | route/action and final results | Medium | Pairs naturally with `_auditController` |
| 4 | `planned` | `Controllers/api/caseRevisionController.cs` | `AuditRecovery` | No SharedLibraries feature yet | revision retrieval/recovery orchestration | revision fetches from case DB | route/actions and any actor-side follow-up | Medium | Current POST is mostly stubbed, but GET belongs here |
| 5 | `partially migrated` | `Controllers/api/case_viewController.cs` | `CaseView` | Partial; main list uses `CaseViewManager`, other actions do not | record-id list, offline-documents filtering/orchestration | direct view queries still in controller | route/actions and response shaping | Low | Existing manager already proves the pattern |
| 5 | `partially migrated` | `Controllers/api/de_id_viewController.cs` | `CaseView` | Partial; uses `CaseViewManager` | de-id view follow-up logic not yet centralized | de-id sortable view access | route/actions and final responses | Low | Keep feature unified with `CaseView` |
| 5 | `planned` | `Controllers/api/pinned_casesController.cs` | `CaseView` | No meaningful manager usage | pinned case workflows | pinned case doc/view access | route/actions and final responses | Low | Should be folded into CaseView feature |
| 5 | `planned` | `Controllers/api/isDuplicateCaseController.cs` | `CaseView` or `Case` | No meaningful manager usage | duplicate detection workflow | duplicate-case CouchDB queries | route/action and final responses | Medium | Choose `CaseView` if read-heavy, `Case` if tied to case writes |
| 5 | `planned` | `Controllers/api/caseRevisionList_case_viewController.cs` | `CaseView` | No meaningful manager usage | revision-list query orchestration | list queries against case revision sources | route/action and final responses | Low | Good cleanup with CaseView wave |
| 6 | `planned` | `Controllers/api/vital_importController.cs` | `VitalImport` | No SharedLibraries feature yet | authorization-key checks, import orchestration, case lookup/update workflow | import-related case queries and writes | header access, actor dispatch, final action responses | High | Keep `ActorSystem` usage in server for pass 1 |
| 6 | `planned` | `Controllers/api/pmss_csv_importController.cs` | `VitalImport` | No SharedLibraries feature yet | PMSS CSV import orchestration | import data access | actor dispatch and final responses | High | Mirror after non-PMSS path is established |
| 6 | `planned` | `Controllers/api/export_queueController.cs` | `ExportQueue` | No SharedLibraries feature yet | export queue state transitions and orchestration | export queue doc reads/writes | route/actions, actor dispatch | Medium | Good candidate after vital import |
| 6 | `planned` | `Controllers/api/zipController.cs` | `ExportQueue` | No SharedLibraries feature yet | export item retrieval/update workflow | export queue document access | file/response handling | Medium | Keep file response creation in controller |
| 6 | `planned` | `Controllers/api/populate_cdc_instanceController.cs` | `MMRIAServices` or `VitalImport` | No SharedLibraries feature yet | service-call orchestration | service-facing data access and backing CouchDB work | route/action and final responses | Medium | Feature home depends on whether service logic expands |
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
