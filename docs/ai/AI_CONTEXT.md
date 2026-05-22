# AI Context Index

- Status: Active
- Scope: Repo-wide rules, current architecture notes, and document routing for AI-assisted work in this repository.
- When to use: Read this file first before planning or making changes, then jump to the feature-specific doc that matches the task.
- Last verified: 2026-04-27
- Related docs: [Refactor Risk Review Context](./local/refactor_risk_review_context.md), [Authentication, Session, and Timeout Context](./authentication_session_timeout.md), [Offline Mode Documentation](./offline_mode.md), [Case Summary Rendering Context](./case_summary_rendering_context.md), [Case View/Edit Playwright Testing Context](./case_view_edit_playwright_testing_context.md), [Case Validation Context](./case_validation_context.md), [Controller to SharedLibraries Migration Matrix](./controller_sharedlibraries_migration_matrix.md), [Security Scan Remediation Tracker](./local/security_scan_remediation_tracker.md), [Security Scan Sensitive Data Heap Guidance](./local/security_scan_sensitive_data_heap_guidance.md), [Historical Notes](./local/archive/)

## How to use this pack

1. Start here for stable repo-wide constraints.
2. Use the task-routing table below to pick the feature doc that matches the task before reading deeper narrative sections.
3. Treat files under [`local/archive/`](./local/archive/) as historical investigation notes, not canonical guidance for new work.
4. For broad refactors touching startup, tenancy, offline sync, or typed case persistence, also read [Refactor Risk Review Context](./local/refactor_risk_review_context.md).
5. Use [`local/`](./local/) for local-only AI working files and recent investigation notes. Link to them when needed, but do not assume they are the durable source of truth for new work unless the doc says so explicitly.
6. Generator and test-tooling work now lives in the sibling `nccdphp-drh-mmria-utilities` repo; use the utilities repo AI context docs for `mmria-tools` and the moved `mmria-server.tests` project when the task crosses that boundary.

## Task routing

| If you are working on... | Read first | Notes |
| --- | --- | --- |
| Repo-wide refactor risk review, multi-tenant regression hotspots, startup/bootstrap risk | [Refactor Risk Review Context](./local/refactor_risk_review_context.md) | Cross-cutting risk map for ongoing SharedLibraries and tenancy refactors. |
| Login, sessions, timeout behavior, SAMS, or re-auth UX | [Authentication, Session, and Timeout Context](./authentication_session_timeout.md) | Includes the durable login/session transport lessons and the `/account/auto-login` pattern. |
| Offline mode, offline sync, service worker, or cache integrity | [Offline Mode Documentation](./offline_mode.md) | Canonical active doc for offline architecture. |
| Case summary rendering, `p_post_html_render`, pinned cases, or hashchange behavior | [Case Summary Rendering Context](./case_summary_rendering_context.md) | Use this instead of older inline notes. |
| Case view/edit Playwright coverage, metadata-driven form walking, or case-page selectors | [Case View/Edit Playwright Testing Context](./case_view_edit_playwright_testing_context.md) | Focused on `/Case#/summary`, `#selected_form`, edit-mode/save flows, and field-renderer contracts. |
| Case validation rules, validation tab, metadata editor, all-field logical validation catalog, or quick-edit validation saves | [Case Validation Context](./case_validation_context.md), [Case Validation Field Logic Catalog](./case_validation_field_logic_catalog.md) | Covers version-scoped validation rule docs, warning-only findings, form-status mapping, human-reviewed logical field rules, and the `#/{caseIndex}/case_validation` route. |
| SharedLibraries migrations or controller cleanup | [Controller to SharedLibraries Migration Matrix](./controller_sharedlibraries_migration_matrix.md) | Tracks completed waves and remaining migration targets. |
| Background jobs, actors, Quartz, or the services host | [MMRIA Services and Background Jobs Documentation](./MMRIA_Background_Jobs_Documentation.md) | Covers both `mmria-server` and `mmria.services`. |
| Multi-tenant runtime rebuild behavior | [Multi-Tenant Rebuild Process](./multi_tenant_rebuild_process.md) | Focused on runtime tenant add and rebuild flow. |
| Populate CDC Instance or de-identification rules | [Populate CDC Instance and De-identification Context](./populate_cdc_deidentification_context.md) | Covers CDC database rebuild behavior and de-identification inputs. |
| Aggregate report flow | [Aggregate Report Architecture](./aggregate_report.md) | Covers MVC route, API route, and data pipeline. |
| Data summary report flow | [Data Summary Report Feature](./data_summary_report.md) | Covers freq-doc generation and report database view usage. |
| TAMU geocoding integration | [TAMU Geocoding Service Integration](./TAMU_Geocoding_Context.md) | Includes the current API controller location. |
| CVS integration | [CVS Community Vital Signs Context](./CVS_Community_Vital_Signs_Context.md) | External data enrichment guidance. |
| Strongly typed case model generation and tooling-repo generators | [Strongly Typed Case Generator Workflow](./strongly_typed_case_generator.md) | Covers the external utilities repo boundary, including `strongly-typed-case`, `mmria-case-generator`, `mmria-ije-generator`, and `mmria-tools`. |
| Sensitive-data scan guidance | [Security Scan Sensitive Data Heap Guidance](./local/security_scan_sensitive_data_heap_guidance.md) | Local investigation note for heap or sensitive-data findings. |
| Security scan tracker | [Security Scan Remediation Tracker](./local/security_scan_remediation_tracker.md) | Local working tracker for current remediation batches and rescan notes. |

## Repo snapshot

- `source-code/mmria/mmria-server` is the main .NET 9 MVC web app with controllers, Razor views, Akka.NET actors, Quartz jobs, and a large `wwwroot/` JavaScript surface.
- `nccdphp-drh-mmria-common/mmria.common` holds shared server-side libraries and cross-cutting models.
- `nccdphp-drh-mmria-services/mmria.services` is the standalone services host for background processing. Shared service logic belongs in `mmria.common/SharedLibraries/MMRIAServices`, while host-specific actors, scheduling, and service startup stay in `mmria.services`.
- CouchDB is the system of record. The app is logically multi-tenant by jurisdiction, with tenant-aware configuration and database selection.

## Non-negotiables

- Preserve routes unless the user explicitly asks for a routing change.
- Do not change controller action signatures, HTTP method attributes, or return shapes without explicit discussion.
- Default to refactor-only behavior. Avoid incidental feature work, UX changes, or contract changes.
- Keep all data access jurisdiction-scoped. Do not introduce cross-jurisdiction reads or writes without explicit design approval.
- For new or touched server-side logic, prefer `mmria.common/SharedLibraries/<Feature>/Model`, `/Manager`, and `/DAL`. Use the controller migration matrix for the detailed refactor rules.
- Do not add outer `try/catch` wrappers around Manager or DAL methods. Let failures propagate so controllers can return meaningful errors.
- Use `CouchDbHttpClient` for CouchDB HTTP work. Do not introduce new `cURL` usage.
- In Akka.NET actors, use async patterns. Do not use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
- Avoid storing sensitive values such as SSNs or other PII in intermediate strings when the value can be handled inline.
- Remove PII from logs and error messages.


## Contract-sensitive surfaces

These areas are easy to break with "small" changes and should be treated as contracts unless the task explicitly says otherwise:

- controller routes and action names
- JSON response shapes consumed by legacy JavaScript
- view models passed into Razor views
- hash-based navigation and post-render callback behavior on the case page
- session cookie plus CouchDB session-document pairing
- jurisdiction-aware database selection and report-database naming
- lock, offline, and session fields written into case or session documents

When in doubt, inspect the current caller before changing any of these surfaces

Current implementation vs preferred pattern

### Server-side business logic

Current implementation:
- The SharedLibraries migration is in progress. Many endpoints already use feature Managers and DALs, but some controllers still contain legacy orchestration or direct host-specific plumbing.

Preferred pattern for new work:
- Keep controllers thin and route-compatible.
- Put business logic in `mmria.common/SharedLibraries/<Feature>/Manager`.
- Put CouchDB access in `mmria.common/SharedLibraries/<Feature>/DAL`.

### Jurisdiction resolution

Current implementation:
- Request paths now resolve current-tenant state through `RequestTenantRuntime`.
- Explicit cross-tenant request work resolves other tenants through `TenantCatalog`.

Preferred pattern for new work:
- Prefer `RequestTenantRuntime` for current-tenant request work.
- Add `TenantCatalog` only when a request path intentionally resolves another tenant by route or payload input.
- Do not reintroduce helper-based or raw config-list tenant resolution in controllers.

### `mmria.services` split

Current implementation:
- Shared service logic lives in `mmria.common/SharedLibraries/MMRIAServices`.
- The service host, actors, and service startup still live in `nccdphp-drh-mmria-services/mmria.services`.
- Startup configuration loading in both `mmria-server` and `mmria.services` now uses strict fail-fast loader entry points. `mmria.services` remains single-tenant in scope, but its startup loading and service-provider shape are now aligned with the stricter server startup pattern documented in the refactor-risk review.

Preferred pattern for new work:
- Put reusable service business logic in `MMRIAServices`.
- Keep host-specific scheduling, actor wiring, and web-host startup in the service project unless the task explicitly broadens scope.

### Client-side case summary rendering

Current implementation:
- The case summary screen uses `page_render(...)` plus `post_html_call_back` arrays that are later executed with `eval(...)`.
- Some state changes are deferred through `p_post_html_render`, but not every flag mutation currently follows that pattern.

Preferred pattern for new work:
- Verify current runtime behavior in code before relying on older render-cycle notes.
- Use the dedicated [Case Summary Rendering Context](./case_summary_rendering_context.md) for current details and historical caveats.


## Common code locations

Use these repo paths as quick bearings when you are orienting yourself:

- `source-code/mmria/mmria-server/Controllers` for MVC and API controllers
- `source-code/mmria/mmria-server/wwwroot/scripts` for legacy front-end JavaScript
- `source-code/mmria/mmria-server/model/actor` for server-host actor logic
- `source-code/mmria/mmria-server/util` for older server-side orchestration and helper code
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries` for feature-scoped shared logic
- `nccdphp-drh-mmria-services/mmria.services` for the standalone background-services host
- `../nccdphp-drh-mmria-utilities` for external tooling such as `strongly-typed-case`, `mmria-case-generator`, `mmria-ije-generator`, `mmria-tools`, and the moved `mmria-server.tests` project

If a doc and the code disagree, treat the code as current implementation and update the docs accordingly

## Quick refactor checklist

Use this checklist before opening a large refactor or AI-assisted extraction:

1. Identify the feature doc that matches the task.
2. Verify the live code path before trusting older notes.
3. Preserve route, action-signature, and response-shape contracts.
4. Keep host-specific plumbing in the host app unless the task explicitly broadens scope.
5. Move reusable business logic toward feature-scoped SharedLibraries where practical.
6. Recheck jurisdiction resolution and data-source scope before changing CouchDB access.
7. Update or archive docs when the implementation truth changes

External dependencies

- Some workflows depend on sibling repositories that may not be present in this workspace. Active docs should label those as `External dependency` instead of assuming the repo exists locally.
- The strongest example is the strongly typed case generator utility repo, which is documented in [Strongly Typed Case Generator Workflow](./strongly_typed_case_generator.md).
- Utilities-repo AI context docs:
  - External dependency: `../../../nccdphp-drh-mmria-utilities/ai/mmria-tools_AI_CONTEXT.md`
  - External dependency: `../../../nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md`

## Historical notes

- Historical investigation timelines now live under [`docs/ai/local/archive/`](./local/archive/).
- Use archived docs when you need implementation history, incident context, or rationale from earlier debugging work.
- Do not treat archived notes as canonical guidance when current code or active docs say otherwise.

