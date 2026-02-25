# Aggregate Report Architecture

## Overview
The Aggregate Report is a jurisdiction-level reporting feature that provides summary statistics on maternal mortality cases. It pulls data from the CouchDB `report` database and transforms it into structured reporting objects.

## Frontend to API Flow

### 1. User Interaction
**Navigation:** Actions menu in case editor
- **File:** [committee-member/navigation_renderer.js](../../source-code/mmria/mmria-server/wwwroot/scripts/committee-member/navigation_renderer.js#L122)
- **Trigger:** User clicks "View Aggregate Report" link
- **Function Called:** `open_aggregate_report_version()` (location TBD in codebase)

### 2. View Routing
**MVC Controller:** [aggregate_reportController.cs](../../source-code/mmria/mmria-server/Controllers/aggregate_reportController.cs)
- **Route:** `/aggregate-report` → `Index()` action
- **Route:** `/aggregate-report/pdf` → `pdf()` action
- **Authorization:** Requires `Roles = "abstractor,data_analyst"`
- **Returns:** Razor view with loaded JavaScript

### 3. View Rendering
**View:** [aggregate_report/Index.cshtml](../../source-code/mmria/mmria-server/Views/aggregate_report/Index.cshtml)
- Loads layout from `_LayoutBase`
- Includes breadcrumbs and title
- Loads JavaScript bundles (d3, c3 charting libraries)
- Renders into `<div id="report_output_id">`

### 4. JavaScript Initialization
**Main Entry:** [aggregate-report/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/aggregate-report/index.js)
- Initializes report on page load
- Calls `get_release_version()` to fetch version info
- Calls metadata API: `/api/version/{release_version}/metadata`
- Sets up UI with filter options and navigation tabs

### 5. Report Data Loading
**Data Source:** [aggregate-report/report-metadata.js](../../source-code/mmria/mmria-server/wwwroot/scripts/aggregate-report/report-metadata.js)
- Function: `get_indicator_values(p_indicator_id)`
- **API Call:** `GET /api/measure-indicator/{indicator_id}`
- Returns array of report objects filtered by:
  - Pregnancy relatedness
  - Date of review range
  - Date of death range
- Populates global `g_data` object

### 6. Report Rendering
**Render Modules:** Multiple JavaScript renderers  
- `render0.js` through `render12.js` — Each renders a different report view/tab
- [report_renderer.js](../../source-code/mmria/mmria-server/wwwroot/scripts/aggregate-report/report_renderer.js) — Core rendering logic
- **Charts:** Uses d3/c3 for visualization
- **PDF Export:** `view_pdf_click()` opens `/aggregate-report/pdf` in new window

## Backend API Endpoint

### Current Implementation (API Controller)

**Controller:** [aggregate_reportController.cs (API)](../../source-code/mmria/mmria-server/Controllers/api/aggregate_reportController.cs)
- **Route:** `GET /api/aggregate_report`
- **Returns:** `IList<c_report_object>`
- **Dependency Injection:**
  - `CouchDbHttpClient` — for HTTP calls to CouchDB
  - `AggregateReportManager` — business logic service
  - Multi-tenant configuration helpers

**Business Logic:** [AggregateReportManager.cs](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/AggregateReport/Manager/AggregateReportManager.cs)
- Located in: `mmria.common/SharedLibraries/AggregateReport/Manager/`
- Follows feature-based architecture pattern from AI_CONTEXT.md

#### Manager Methods:
1. **`GetReportsAsync(DBConfigurationDetail dbConfig)`**
   - Fetches all documents from `report` database via `_all_docs?include_docs=true`
   - Converts raw CouchDB JSON to strongly-typed objects
   - Filters for valid entries (year_of_death != 9999, year_of_case_review present)
   - Returns `IList<c_report_object>`

2. **`Convert(JsonElement docElement)`**
   - Parses CouchDB JSON document structure
   - Extracts all fields including:
     - Numeric fields: year, month, case review info
     - Struct fields: pregnancy relatedness, ethnicity, age distributions, timing of death
     - Dictionary fields: underlying causes, preventability, mental health, substance use, suicide, homicide

3. **Helper Methods:**
   - `PopulatePregnancyRelatednessStruct()` — Parses pregnancy relatedness data
   - `PopulateEthnicityStruct()` — Parses ethnicity breakdown (17 ethnicity categories)
   - `PopulateAgeStruct()` — Parses age cohorts (7 categories)
   - `PopulateTimingOfDeathStruct()` — Parses timing relative to pregnancy
   - `PopulateList()` — Dictionary population for categorical data
   - `GetIntValue()` — Safe integer extraction from JSON

### Data Model

**Model File:** [c_report_object.cs](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/AggregateReport/Model/c_report_object.cs)
- Located in: `mmria.common/SharedLibraries/AggregateReport/Model/`
- Moved from: `mmria.server.model.c_report_object`

**Key Structs:**
```csharp
public struct total_number_of_cases_by_pregnancy_relatedness_struct
public struct ethnicity_struct (17 categories)
public struct age_at_death_struct (7 age cohorts)
public struct timing_of_death_in_relation_to_pregnancy_struct
public struct total_value_struct
```

**Main Class:**
```csharp
public sealed class c_report_object
{
    public string _id;
    public int? year_of_death;
    public int? month_of_case_review;
    public int? year_of_case_review;
    
    // Aggregate data by various dimensions
    public total_number_of_cases_by_pregnancy_relatedness_struct total_number_of_cases_by_pregnancy_relatedness;
    public ethnicity_struct total_number_of_pregnancy_related_deaths_by_ethnicity;
    public ethnicity_struct total_number_of_pregnancy_associated_by_ethnicity;
    // ... and 11 more similar fields
    
    // Dictionary-based metrics (for detailed breakdowns)
    public Dictionary<string, int> distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm;
    public Dictionary<string, int> total_pregnancy_related_determined_to_be_preventable;
    // ... and 7 more dictionary fields
}
```

## CouchDB Database

### Report Database Structure
- **Database:** `{prefix}report` (e.g., `mmrds_report` or just `report`)
- **Design Doc:** `_design/aggregate_report` — traditional view
- **Design Doc:** `_design/interactive_aggregate_report` — interactive reporting view
- **Documents:** Individual report aggregates per jurisdiction/time period

### Synchronization
- Created/updated by: [c_document_sync_all.cs](../../source-code/mmria/mmria-server/util/c_document_sync_all.cs)
- Design docs pushed during database synchronization
- Accessed with CouchDB authentication credentials from config

## Configuration

**Jurisdiction Resolution:**
- DB Configuration: `DBConfigurationDetail` - contains:
  - `url` — Base CouchDB server URL (e.g., `http://tenant5-couchdb.local:6984`)
  - `prefix` — Database name prefix
  - `user_name` — CouchDB username
  - `user_value` — CouchDB password

**Multi-tenant Handling:**
- Controller calls `MultiTenantConfigHelper.GetDBConfigForTenant()` in constructor
- Passes resolved config to the manager method
- All database calls are jurisdiction-scoped

## Architecture Notes

### Service Layer Pattern
Following AI_CONTEXT.md guidelines:
- **Controller:** Thin HTTP handler, only calls manager
- **Manager:** All business logic (filtering, conversion, transformation)
- **Model:** Data contracts (c_report_object and related structs)
- **No DAL:** CouchDB calls made directly in manager (acceptable for reports)

### No Server-Specific Dependencies
- Previously located in `mmria.server.model` (server-scoped)
- Now in `mmria.common` (testable from external projects)
- Uses `System.Text.Json` (not Newtonsoft.Json) for JSON parsing
- Can be instantiated directly in tests without controller context

### Jurisdiction Isolation
- All data queries filtered to single jurisdiction
- Database URL resolved from multi-tenant configuration
- No cross-jurisdiction data access possible

## Related APIs

### Measure Indicator Endpoint
- **Route:** `GET /api/measure-indicator/{indicator_id}`
- **Purpose:** Returns specific indicators for dashboard visualization
- **Data Source:** Same report database, filtered by indicator ID

### Version APIs
- **Release Version:** `GET /api/version/release-version`
- **Metadata:** `GET /api/version/{version}/metadata`
- Used to fetch supported indicators and metadata structure

## Testing Considerations

### Unit Testing AggregateReportManager
- Requires: `CouchDbHttpClient` (can mock HTTP responses)
- Requires: `DBConfigurationDetail` (can inject test values)
- Parser logic can be tested independently with mock JSON documents
- Filter logic can be tested with various case data scenarios

### Integration Testing
- Requires: Running CouchDB instance with test database
- Can test end-to-end: Controller → Manager → CouchDB → Response

### Test Data Generation
- See: [mmria-case-generator](../../nccdphp-drh-mmria-utilities/mmria-case-generator/) for test case data generation
- Report database structure can be populated via test data builders

## Files Changed in Refactor (Feb 2026)

1. **Created:** `mmria.common/SharedLibraries/AggregateReport/Model/c_report_object.cs`
2. **Created:** `mmria.common/SharedLibraries/AggregateReport/Manager/AggregateReportManager.cs`
3. **Updated:** `aggregate_reportController.cs` (API) — now calls manager
4. **Updated:** `c_convert_to_report_object.cs` — uses `mmria.common.Model.AggregateReport` namespace
5. **Moved:** Model from `mmria.server.model` to `mmria.common.Model.AggregateReport`
6. **Registered:** `AggregateReportManager` in `Program.cs` dependency injection
