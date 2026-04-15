# Aggregate Report Architecture

- Status: Active
- Scope: MVC route, API route, shared-library manager, and client-side rendering flow for the aggregate report feature.
- When to use: Read this before changing aggregate report routes, report loading, or the report data pipeline.
- Last verified: 2026-04-14
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Controller to SharedLibraries Migration Matrix](./controller_sharedlibraries_migration_matrix.md), [MMRIA Services and Background Jobs Documentation](./MMRIA_Background_Jobs_Documentation.md)

## Overview

The Aggregate Report is a jurisdiction-level reporting feature that reads report data from CouchDB and renders the reporting UI at `/aggregate-report`.

## Route and controller flow

| Surface | Current contract | Primary file |
| --- | --- | --- |
| MVC route | `/aggregate-report` | [aggregate_reportController.cs](../../source-code/mmria/mmria-server/Controllers/aggregate_reportController.cs) |
| MVC export route | `/aggregate-report/pdf` | [aggregate_reportController.cs](../../source-code/mmria/mmria-server/Controllers/aggregate_reportController.cs) |
| API route | `GET /api/aggregate_report` | [api/aggregate_reportController.cs](../../source-code/mmria/mmria-server/Controllers/api/aggregate_reportController.cs) |

## Server-side implementation

### Shared-library manager

- [AggregateReportManager.cs](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/AggregateReport/Manager/AggregateReportManager.cs) contains the report retrieval logic.
- The manager currently lives under the legacy namespace `mmria.common.Manager`, even though the file is stored under `SharedLibraries/AggregateReport/Manager/`.
- The API controller injects the manager and passes tenant-aware database configuration into it.

### Data source

- The report reads from the tenant-scoped `report` database.
- The manager currently uses `_all_docs?include_docs=true` and converts the result into `c_report_object` values.

Key model:

- [c_report_object.cs](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/AggregateReport/Model/c_report_object.cs)

## Client-side flow

### View shell

- [Views/aggregate_report/Index.cshtml](../../source-code/mmria/mmria-server/Views/aggregate_report/Index.cshtml) loads the JavaScript entrypoints and report render modules.

### Main scripts

- [aggregate-report/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/aggregate-report/index.js)
- [aggregate-report/report-metadata.js](../../source-code/mmria/mmria-server/wwwroot/scripts/aggregate-report/report-metadata.js)
- [aggregate-report/report_renderer.js](../../source-code/mmria/mmria-server/wwwroot/scripts/aggregate-report/report_renderer.js)
- [aggregate-report/navigation_renderer.js](../../source-code/mmria/mmria-server/wwwroot/scripts/aggregate-report/navigation_renderer.js)

### UI entry points

- The aggregate report route itself is the stable entry point.
- If you are tracing one legacy menu path from the case editor, the menu item is rendered in [committee-member/navigation_renderer.js](../../source-code/mmria/mmria-server/wwwroot/scripts/committee-member/navigation_renderer.js).
- Do not rely on undocumented function names as the canonical contract; the stable contract is the route and API surface above.

## Notes for refactoring

- Preserve `/aggregate-report` and `/api/aggregate_report` unless the task explicitly changes routes.
- Treat the current namespace mismatch in `AggregateReportManager` as existing repo state, not a cleanup task to bundle into unrelated work.
- Keep tenant resolution in the controller unless the task explicitly broadens into tenancy refactoring.
- Treat the route/API surface as the stable contract; helper names and renderer internals are secondary unless the task explicitly targets them.


