# Data Summary Report Feature

- Status: Active
- Scope: Frequency-summary document generation, report-database view usage, and the `/api/data-summary/{skip}` pipeline.
- When to use: Read this before changing data-summary generation, report indexing, or the view-data-summary UI.
- Last verified: 2026-04-14
- Related docs: [AI Context Index](./AI_CONTEXT.md), [MMRIA Services and Background Jobs Documentation](./MMRIA_Background_Jobs_Documentation.md)


## Overview

The Data Summary Report (`/view-data-summary` route) provides frequency analysis and statistical summaries of MMRIA case data. It displays aggregate counts and distributions across various case fields, enabling analysts to identify patterns and trends without exposing individual case details.

## Architecture

### Document flow

| Step | Current behavior |
| --- | --- |
| Case save or sync trigger | `c_sync_document` or related sync/setup paths trigger summary generation. |
| Frequency document generation | `c_generate_frequency_summary_report` writes `freq-` documents into the tenant-scoped `report` database. |
| Report view indexing | CouchDB indexes those `freq-` documents through `data_summary_view_report`. |
| API query | `/api/data-summary/{skip}` reads the indexed report view with 100-record paging. |
| Frontend filtering | The `view-data-summary` frontend loads report rows and applies client-side filter logic. |

### Key Components

| Component | Role | Primary files |
| --- | --- | --- |
| Frequency document generator | Transforms saved case data into `freq-` summary documents in the report database. | [c_generate_frequency_summary_report.cs](../../source-code/mmria/mmria-server/util/c_generate_frequency_summary_report.cs), [c_sync_document.cs](../../source-code/mmria/mmria-server/util/c_sync_document.cs) |
| Report database view | Emits indexed report rows from `freq-` documents for API paging. | [data-summary-view.json](../../source-code/mmria/mmria-server/database-scripts/data-summary-view.json) |
| Backend API | Exposes `/api/data-summary/{skip}` with jurisdiction-aware access control and 100-row paging. | [data_summary_viewController.cs](../../source-code/mmria/mmria-server/Controllers/api/data_summary_viewController.cs) |
| Frontend filter UI | Loads report rows and applies date/status filters in the browser. | [view-data-summary/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/index.js), [view-data-summary/renderer.js](../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/renderer.js) |

Important current details:

- Frequency documents still flatten important top-level date fields such as `year_of_death` and `year_of_case_review` for reporting.
- Sentinel values remain part of the contract: `9999` represents blank or not-entered numeric date parts, and `"(-)"` is used for some not-applicable detail values.
- Date filtering is still primarily client-side. "Include dates not entered" keeps blank/sentinel-backed rows; explicit date-range mode excludes them.

## Historical Bug and Fix

The key historical regression was a property-name mismatch between the generated frequency documents and the CouchDB view.

- Symptom: date-range filtering for review dates failed because rows with valid dates were not emitted under the names the frontend expected.
- Root cause: the view emitted `case_review_year/month/day` while the generator and frontend had already standardized on `year_of_case_review/month/day`.
- Fix: update [data-summary-view.json](../../source-code/mmria/mmria-server/database-scripts/data-summary-view.json) to emit the same property names already used by the generator and frontend.
- Why this was the correct fix: the stored `freq-` documents and frontend contract were already aligned, so no data migration or JavaScript workaround was needed.

## Deployment Notes

### View update process

1. Update the `data_summary_view_report` design document in CouchDB.
2. Let CouchDB rebuild the view index.
3. No application restart is required.
4. No freq-document regeneration is required when the underlying documents already use the correct field names.

### Verification

- Open `/view-data-summary`.
- Use an explicit review-date range that should include known rows.
- Confirm that records with populated review dates appear when blank dates are excluded.

## Key Files Reference

### Backend
- Model: [SummaryDetail.cs](../../source-code/mmria/mmria-server/model/SummaryDetail.cs)
- Generator: [c_generate_frequency_summary_report.cs](../../source-code/mmria/mmria-server/util/c_generate_frequency_summary_report.cs)
- Sync: [c_sync_document.cs](../../source-code/mmria/mmria-server/util/c_sync_document.cs)
- API: [data_summary_viewController.cs](../../source-code/mmria/mmria-server/Controllers/api/data_summary_viewController.cs)
- View: [data-summary-view.json](../../source-code/mmria/mmria-server/database-scripts/data-summary-view.json)

### Frontend
- Main Logic: [view-data-summary/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/index.js)
- UI Rendering: [view-data-summary/renderer.js](../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/renderer.js)

### Services (CDC sync)
- Generator: [c_generate_frequency_summary_report.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/c_generate_frequency_summary_report.cs)
- Sync: [c_sync_document.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/c_sync_document.cs)

## Performance Considerations

- **Pagination:** API returns 100 records per page
- **View Indexing:** CouchDB rebuilds index when view definition changes
- **Large Datasets:** Frontend loads all data into memory before filtering
- **Sync Process:** Background actor system prevents blocking case saves
- **Manual Sync:** `/api/sync` regenerates all freq- documents (long-running, admin-only)



