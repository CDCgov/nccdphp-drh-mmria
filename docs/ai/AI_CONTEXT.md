# AI Context Index

- Status: Active
- Scope: Repo-wide rules, current architecture notes, and document routing for AI-assisted work in this repository.
- When to use: Read this file first before planning or making changes, then jump to the feature-specific doc that matches the task.
- Last verified: 2026-03-24
- Related docs: [Authentication, Session, and Timeout Context](./authentication_session_timeout.md), [Offline Mode Documentation](./offline_mode.md), [Case Summary Rendering Context](./case_summary_rendering_context.md), [Controller to SharedLibraries Migration Matrix](./controller_sharedlibraries_migration_matrix.md), [Historical Notes](./archive/)

## How to use this pack

1. Start here for stable repo-wide constraints.
2. Use the routing table below to pick the feature doc that matches the task.
3. Treat files under [`archive/`](./archive/) as historical investigation notes, not canonical guidance for new work.

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
- For new or touched server-side logic, prefer `mmria.common/SharedLibraries/<Feature>/Model`, `/Manager`, and `/DAL`.
- Do not add new direct CouchDB calls in controllers. Controllers should call Managers; Managers should call DAL classes.
- Do not call another feature's DAL directly. Cross-feature reuse should happen through Managers or clearly shared helpers.
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
- Controllers commonly resolve the tenant from `Request.Host.GetPrefix()` and then load tenant-specific configuration with `MultiTenantConfigHelper.GetConfigurationForTenant(...)` and `GetDBConfigForTenant(...)`.

Preferred pattern for new work:
- Preserve the existing host-prefix and helper-based resolution unless the task explicitly includes tenancy refactoring.
- If a cleaner accessor abstraction is introduced later, treat that as future-state work rather than current repo truth.

### `mmria.services` split

Current implementation:
- Shared service logic lives in `mmria.common/SharedLibraries/MMRIAServices`.
- The service host, actors, and service startup still live in `nccdphp-drh-mmria-services/mmria.services`.
- `mmria.services` does not currently use the same multi-tenant configuration loading flow as `mmria-server`; avoid documenting them as already aligned.

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

If a doc and the code disagree, treat the code as current implementation and update the docs accordingly

Task routing

| If you are working on... | Read first | Notes |
| --- | --- | --- |
| Login, sessions, timeout behavior, SAMS, or re-auth UX | [Authentication, Session, and Timeout Context](./authentication_session_timeout.md) | Includes the durable login/session transport lessons and the `/account/auto-login` pattern. |
| Offline mode, offline sync, service worker, or cache integrity | [Offline Mode Documentation](./offline_mode.md) | Canonical active doc for offline architecture. |
| Case summary rendering, `p_post_html_render`, pinned cases, or hashchange behavior | [Case Summary Rendering Context](./case_summary_rendering_context.md) | Use this instead of older inline notes. |
| SharedLibraries migrations or controller cleanup | [Controller to SharedLibraries Migration Matrix](./controller_sharedlibraries_migration_matrix.md) | Tracks completed waves and remaining migration targets. |
| Background jobs, actors, Quartz, or the services host | [MMRIA Services and Background Jobs Documentation](./MMRIA_Background_Jobs_Documentation.md) | Covers both `mmria-server` and `mmria.services`. |
| Multi-tenant runtime rebuild behavior | [Multi-Tenant Rebuild Process](./multi_tenant_rebuild_process.md) | Focused on runtime tenant add and rebuild flow. |
| Populate CDC Instance or de-identification rules | [Populate CDC Instance and De-identification Context](./populate_cdc_deidentification_context.md) | Covers CDC database rebuild behavior and de-identification inputs. |
| Aggregate report flow | [Aggregate Report Architecture](./aggregate_report.md) | Covers MVC route, API route, and data pipeline. |
| Data summary report flow | [Data Summary Report Feature](./data_summary_report.md) | Covers freq-doc generation and report database view usage. |
| TAMU geocoding integration | [TAMU Geocoding Service Integration](./TAMU_Geocoding_Context.md) | Includes the current API controller location. |
| CVS integration | [CVS Community Vital Signs Context](./CVS_Community_Vital_Signs_Context.md) | External data enrichment guidance. |
| Strongly typed case model generation | [Strongly Typed Case Generator Workflow](./strongly_typed_case_generator.md) | References an external utility repo; read the external dependency notes first. |
| Sensitive-data scan guidance | [Security Scan Sensitive Data Heap Guidance](./security_scan_sensitive_data_heap_guidance.md) | Use when addressing heap or sensitive-data findings. |


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

## Historical notes

- Historical investigation timelines now live under [`docs/ai/archive/`](./archive/).
- Use archived docs when you need implementation history, incident context, or rationale from earlier debugging work.
- Do not treat archived notes as canonical guidance when current code or active docs say otherwise.




