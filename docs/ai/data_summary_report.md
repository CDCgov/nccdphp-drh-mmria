# Data Summary Report Feature

- Status: Active
- Scope: Frequency-summary document generation, report-database view usage, and the `/api/data-summary/{skip}` pipeline.
- When to use: Read this before changing data-summary generation, report indexing, or the view-data-summary UI.
- Last verified: 2026-03-24


Overview

The Data Summary Report (`/view-data-summary` route) provides frequency analysis and statistical summaries of MMRIA case data. It displays aggregate counts and distributions across various case fields, enabling analysts to identify patterns and trends without exposing individual case details.

## Architecture

### Document Flow

```
1. Case Save (mmrds database)
   ↓
2. c_sync_document triggers
   ↓
3. c_generate_frequency_summary_report creates freq- document
   ↓
4. freq- document stored in report database
   ↓
5. CouchDB view indexes freq- documents
   ↓
6. API /api/data-summary/{skip} queries view
   ↓
7. Frontend filters and displays results
```

### Key Components

#### 1. Freq Document Generation (`c_generate_frequency_summary_report.cs`)

**Location:** `source-code/mmria/mmria-server/util/c_generate_frequency_summary_report.cs`

**Purpose:** Transforms case records into frequency summary documents for reporting.

**When Triggered:**
- Every time a case is saved (via `c_sync_document.cs`)
- Manual sync via `/api/sync` endpoint (installation_admin only)
- Database startup/setup via `c_db_setup.cs`

**Document Structure:**
```csharp
FrequencySummaryDocument {
    _id: "freq-{case_id}",
    year_of_death: int?,
    month_of_death: int?,
    day_of_death: int?,
    year_of_case_review: int?,     // Extracted from committee_review/date_of_review
    month_of_case_review: int?,
    day_of_case_review: int?,
    case_folder: string,           // Jurisdiction ID
    case_status: int?,
    pregnancy_relatedness: int?,
    path_to_detail: Dictionary<string, List<Detail>>  // Field-level frequency data
}
```

**Date Field Population:**
```csharp
// Parses committee_review/date_of_review and populates top-level fields
val = gs.get_value(source_object, "committee_review/date_of_review").result;
if (val != null && val.ToString() != "")
{
    FrequencySummaryDocument.day_of_case_review = Convert.ToDateTime(val).Day;
    FrequencySummaryDocument.year_of_case_review = Convert.ToDateTime(val).Year;
    FrequencySummaryDocument.month_of_case_review = Convert.ToDateTime(val).Month;
}
```

**Sentinel Values:**
- `9999` = blank/null/not entered for numeric fields
- `"(-)"` = not applicable in path_to_detail

#### 2. CouchDB View (`data_summary_view_report`)

**Location:** `source-code/mmria/mmria-server/database-scripts/data-summary-view.json`

**Database:** `{prefix}report`

**View Name:** `year_of_death`

**Map Function:**
```javascript
function(doc) { 
    if(0==doc._id.indexOf('freq-')) { 
        emit( doc.year_of_death, {
            type:'freq-measure',
            host_state:doc.host_state,
            case_id:doc._id.replace('freq-',''),
            record_id:doc.record_id,
            case_folder:doc.case_folder,
            case_status: doc.case_status,
            year_of_death:doc.year_of_death,
            month_of_death:doc.month_of_death,
            day_of_death:doc.day_of_death,
            year_of_case_review:doc.year_of_case_review,    // Fixed: was case_review_year
            month_of_case_review:doc.month_of_case_review,  // Fixed: was case_review_month
            day_of_case_review:doc.day_of_case_review,      // Fixed: was case_review_day
            pregnancy_relatedness:doc.pregnancy_relatedness,
            path_to_detail:doc.path_to_detail
        })
    }
}
```

#### 3. Backend API (`data_summary_viewController.cs`)

**Location:** `source-code/mmria/mmria-server/Controllers/api/data_summary_viewController.cs`

**Route:** `/api/data-summary/{skip}`

**Authorization:** `abstractor, data_analyst`

**Pagination:** 100 records per page

**Query:**
```csharp
string find_url = $"{url}/{prefix}report/_design/data_summary_view_report/_view/year_of_death?skip={skip}&limit=100";
```

**Filters:** Jurisdiction-based access control applied to results

#### 4. Frontend (`index.js`, `renderer.js`)

**Location:** `source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/`

**Filter Implementation:**

Date filtering uses radio button selections:
- "Include dates not entered" - includes records with null/9999 dates
- "Select dates" - filters to records within begin/end date range, excludes blanks

**Date Comparison Functions:**

```javascript
function is_greater_than_date(p_year, p_month, p_day, p_date) {
    // Defaults null to 9999 sentinel value
    let year = p_year != null ? p_year : 9999;
    let month = p_month != null ? p_month : 9999;
    let day = p_day != null ? p_day : 9999;
    
    // Exclude blank dates from filtered results
    if(year == 9999) return false;
    
    // Compare date components
    // Returns true if record date >= filter date
}

function is_less_than_date(p_year, p_month, p_day, p_date) {
    // Similar logic for date <= comparison
}
```

**Filter State:**
```javascript
g_filter = {
    date_of_review: {
        begin: Date,
        end: Date
    },
    include_blank_date_of_reviews: boolean,  // Radio button state
    
    date_of_death: {
        begin: Date,
        end: Date  
    },
    include_blank_date_of_deaths: boolean
}
```

## Historical Bug and Fix

### The Problem

**Symptom:** Date filtering didn't work correctly - records with dates weren't appearing when "Select dates" was chosen.

**Root Cause:** Property name mismatch introduced in May 2023

### Timeline

1. **May 20, 2023** (commit `66ac8647d`)
   - C# model created with properties: `year_of_case_review`, `month_of_case_review`, `day_of_case_review`

2. **May 21, 2023** (commit `61437c242`)
   - CouchDB view created by copying old view structure
   - **Bug introduced:** Used wrong property names: `case_review_year`, `case_review_month`, `case_review_day`

3. **June 7, 2023** (commit `3762a2876` - "Data Summary - Filter Implemented")
   - JavaScript updated correctly: `case_review_year` → `year_of_case_review`
   - **Bug persisted:** View not updated, still using wrong property names

4. **July 1, 2025** (commit `67418b117`)
   - PMSS changes added date parsing code in report generator

5. **February 2026** - Bug discovered and fixed

### The Fix

**Changed in data-summary-view.json:**

```diff
- case_review_year:doc.case_review_year,
- case_review_month:doc.case_review_month,
- case_review_day:doc.case_review_day,
+ year_of_case_review:doc.year_of_case_review,
+ month_of_case_review:doc.month_of_case_review,
+ day_of_case_review:doc.day_of_case_review,
```

**Why This Works:**
- Freq documents in database already have correct property names (`year_of_case_review`)
- JavaScript already expects correct property names
- View now reads and emits correct property names
- No data migration needed

### Alternate Approaches Considered

**Option 1: JavaScript Workaround** (initially implemented, then reverted)
- Extract dates from `path_to_detail["committee_review/date_of_review"]` when top-level fields are null
- Works around the issue but treats symptom, not cause
- More complex code path

**Option 2: Change C# Model** (rejected)
- Rename properties to match incorrect view: `case_review_year`, etc.
- Would require:
  - Changing C# model and generator
  - Reverting JavaScript changes
  - Migrating thousands of existing freq- documents
  - Higher risk, more disruption

**Option 3: Fix View** (selected)
- Simplest, lowest risk
- Matches existing data structure
- Consistent naming convention with other date fields
- One-file change

## Deployment Notes

### View Update Process

1. Update view in CouchDB via Fauxton or API:
   ```bash
   PUT /{prefix}report/_design/data_summary_view_report
   {
       ...updated view definition...
   }
   ```

2. CouchDB will rebuild view index automatically

3. No application restart needed

4. No freq- document regeneration needed (documents already correct)

### Verification

Check that filtering works correctly:
1. Navigate to `/view-data-summary`
2. Select "Select dates" for Date of Review
3. Choose date range covering known records
4. Click "Apply Filters"
5. Verify cases with review dates appear

## Key Files Reference

### Backend
- Model: `source-code/mmria/mmria-server/model/SummaryDetail.cs`
- Generator: `source-code/mmria/mmria-server/util/c_generate_frequency_summary_report.cs`
- Sync: `source-code/mmria/mmria-server/util/c_sync_document.cs`
- API: `source-code/mmria/mmria-server/Controllers/api/data_summary_viewController.cs`
- View: `source-code/mmria/mmria-server/database-scripts/data-summary-view.json`

### Frontend
- Main Logic: `source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/index.js`
- UI Rendering: `source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/renderer.js`

### Services (CDC sync)
- Generator: `nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/c_generate_frequency_summary_report.cs`
- Sync: `nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/c_sync_document.cs`

## Performance Considerations

- **Pagination:** API returns 100 records per page
- **View Indexing:** CouchDB rebuilds index when view definition changes
- **Large Datasets:** Frontend loads all data into memory before filtering
- **Sync Process:** Background actor system prevents blocking case saves
- **Manual Sync:** `/api/sync` regenerates all freq- documents (long-running, admin-only)

## Future Improvements

1. **Server-side Filtering:** Move date filtering from client to API/view
2. **Incremental Loading:** Load and filter data in chunks rather than all at once
3. **View Parameters:** Use CouchDB view parameters for date range queries
4. **Status Indicator:** Show sync progress when freq- documents are regenerating
5. **Data Validation:** Add checks to ensure freq- document dates match case dates



