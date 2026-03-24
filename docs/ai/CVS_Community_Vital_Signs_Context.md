# CVS (Community Vital Signs) API Integration

- Status: Active
- Scope: CVS enrichment fields, integration touchpoints, and case-generator guardrails for CVS-backed values.
- When to use: Read this before changing CVS imports, CVS-backed case fields, or generated test data that touches CVS values.
- Last verified: 2026-03-24
- Related docs: [AI Context Index](./AI_CONTEXT.md), [TAMU Geocoding Service Integration](./TAMU_Geocoding_Context.md)
**Referenced by:** [AI_CONTEXT.md](./AI_CONTEXT.md)

## Overview

CVS (Community Vital Signs) is an external API service that provides social determinant of health metrics at the county and census tract levels. The service enriches MMRIA cases with contextual health and socioeconomic data for the geographic area where the maternal death occurred.

**Critical Rule for Case Generator:** CVS fields must NEVER be generated with fake data. They should be left empty ("") because they require real external API calls to the Community Vital Signs service.

## Service Details

**API Base URL:** `/api/cvsAPI`

**Data Structure:**
- **Form:** `cvs`
- **Grid:** `cvs_grid` (contains rows with CVS metrics)
- **Integration:** Button-triggered API call from MMRIA UI

**JavaScript Integration:**
- **mmria.js** (lines 67-566) - CVS API functions
  - `get_cvs_api_data_info()` - Fetch CVS data for specific geoids
  - `get_cvs_api_dashboard_info()` - Fetch CVS data for dashboard display
  - `get_cvs_api_server_info()` - Initialize CVS service connection
  - `dc_plc_cvs_button_click()` - Button handler to trigger CVS data fetch
- **mmria.js** (lines 200-279) - CVS callback functions
  - `callback_cvs_data_success()` - Populate cvs_grid with API response
  - `callback_cvs_data_error()` - Handle API errors

## The 49 CVS Fields

All fields are located in the `cvs/cvs_grid` structure (grid with multiple rows).

### API Request Metadata Fields (6 fields)

These track the API request details:
- `cvs_api_request_url` - Full API URL used for request
- `cvs_api_request_date_time` - Timestamp of API call
- `cvs_api_request_c_geoid` - County GEOID parameter
- `cvs_api_request_t_geoid` - Tract GEOID parameter
- `cvs_api_request_year` - Year parameter for data
- `cvs_api_request_result_message` - Success/error message from API

### County-Level Metric Fields (29 fields)

Social determinant metrics aggregated at the county level:

**Health Access:**
- `cvs_mdrate_county` - Maternal Deprivation Rate
- `cvs_pctnoins_fem_county` - % Uninsured Females
- `cvs_cnmrate_county` - Certified Nurse Midwife Rate
- `cvs_obgynrate_county` - OB/GYN Provider Rate
- `cvs_mhproviderrate` - Mental Health Provider Rate
- `cvs_rtmhpract_county` - Mental Health Practitioner Rate

**Housing & Transportation:**
- `cvs_pctnovehicle_county` - % Households Without Vehicle
- `cvs_pctmove_county` - % Moved in Last Year
- `cvs_pctsphh_county` - % Single-Parent Households
- `cvs_pctovercrowdhh_county` - % Overcrowded Households
- `cvs_pctowner_occ_county` - % Owner-Occupied Housing
- `cvs_pcthouse_distress_county` - % Housing Distress

**Economic Indicators:**
- `cvs_pct_less_well_county` - % Less Well-Off
- `cvs_pctpov_county` - % Below Poverty Line
- `cvs_medhhinc_county` - Median Household Income
- `cvs_ice_income_all_county` - Index of Concentration at Extremes (Income)
- `cvs_racialized_pov` - Racialized Poverty Index

**Health Outcomes:**
- `cvs_pctobese_county` - % Obese Adults
- `cvs_fi_county` - Food Insecurity Rate
- `cvs_rtteenbirth_county` - Teen Birth Rate
- `cvs_rtstd_county` - STD Rate
- `cvs_rtdrugodmortality_county` - Drug Overdose Mortality Rate
- `cvs_rtopioidprescript_county` - Opioid Prescription Rate

**Community & Safety:**
- `cvs_soccap_county` - Social Capital Index
- `cvs_rtsocassoc_county` - Social Association Rate
- `cvs_rtviolentcr_icpsr_county` - Violent Crime Rate
- `cvs_isolation_county` - Social Isolation Index

**Geography:**
- `cvs_pctrural` - % Rural Population
- `cvs_ndi_raw_county` - Neighborhood Deprivation Index (Raw)

### Tract-Level Metric Fields (14 fields)

Social determinant metrics aggregated at the census tract level:

**Housing & Transportation:**
- `cvs_pctnovehicle_tract` - % Households Without Vehicle
- `cvs_pctmove_tract` - % Moved in Last Year
- `cvs_pctsphh_tract` - % Single-Parent Households
- `cvs_pctovercrowdhh_tract` - % Overcrowded Households
- `cvs_pctowner_occ_tract` - % Owner-Occupied Housing

**Economic Indicators:**
- `cvs_pct_less_well_tract` - % Less Well-Off
- `cvs_ndi_raw_tract` - Neighborhood Deprivation Index (Raw)
- `cvs_pctpov_tract` - % Below Poverty Line
- `cvs_ice_income_all_tract` - Index of Concentration at Extremes (Income)
- `cvs_medhhinc_tract` - Median Household Income

**Health Access:**
- `cvs_pctnoins_fem_tract` - % Uninsured Females

## CVS Data Categories

The CVS metrics provide comprehensive social determinant data across key domains:

1. **Health Infrastructure** - Provider availability, insurance coverage
2. **Economic Conditions** - Income, poverty, deprivation indices
3. **Housing Stability** - Crowding, ownership, housing distress
4. **Social Environment** - Social capital, isolation, community associations
5. **Health Outcomes** - Obesity, food insecurity, teen births, STDs, drug mortality
6. **Geographic Context** - Urban/rural classification
7. **Safety** - Violent crime rates

## Implementation in Case Generator

**Method:** `PostProcessCVS(Dictionary<string, object?> caseData)`
**Location:** CaseDataGenerator.cs (lines 453-495)

The method clears all CVS fields after case generation:

1. Access `cvs` form → `cvs_grid` grid structure
2. Iterate through each row in the grid
3. Set all 49 CVS fields to empty string ("")

**Pattern:**
```csharp
foreach (var row in grid)
{
    // Clear API request fields
    if (row.ContainsKey("cvs_api_request_url")) row["cvs_api_request_url"] = "";
    
    // Clear all metric fields
    if (row.ContainsKey("cvs_mdrate_county")) row["cvs_mdrate_county"] = "";
    // ... clear remaining 47 fields
}
```

## Why This Matters

**Problem:** Generating fake CVS data creates invalid social determinant metrics that:
- Cannot be correlated with real geographic locations
- Produces meaningless statistical analysis for maternal mortality patterns
- Breaks research workflows that rely on accurate social determinant data
- Causes confusion when comparing cases (users can't tell which data is real)

**Solution:** Leave CVS fields empty ("") so they can be populated via the actual Community Vital Signs API when:
- Users click the "Get CVS Data" button in the MMRIA UI
- Batch CVS data population runs for multiple cases
- Research exports are generated with real social determinant context

## CVS Data Flow

1. **User Action:** Clicks "CVS" button in Place of Last Residence form
2. **JavaScript:** `dc_plc_cvs_button_click()` triggers API call
3. **API Request:** Sends county/tract GEOID + year to CVS API
4. **API Response:** Returns JSON with 43 metric values
5. **Population:** `callback_cvs_data_success()` populates `cvs_grid` rows
6. **Storage:** Case saved with real CVS data

## Related Documentation

- **TAMU Geocoding:** [TAMU_Geocoding_Context.md](./TAMU_Geocoding_Context.md)
- **Main Context:** [AI_CONTEXT.md](./AI_CONTEXT.md)
- **Background Jobs:** [MMRIA_Background_Jobs_Documentation.md](./MMRIA_Background_Jobs_Documentation.md)

## Source Files

**C# Backend:**
- `source-code/mmria/mmria-server/Controllers/CvsController.cs` - CVS API controller
- `nccdphp-drh-mmria-utilities/mmria-case-generator/Generators/CaseDataGenerator.cs` (PostProcessCVS method)

**JavaScript Frontend:**
- `source-code/mmria/mmria-server/wwwroot/scripts/mmria.js` (lines 67-566) - CVS API integration
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` (line 1232) - CVS initialization

**API Integration:**
- External CVS API service URL configured in appsettings.json
- Requires valid API credentials and geoid parameters



