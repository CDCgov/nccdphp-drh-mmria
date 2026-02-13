# TAMU Geocoding Service Integration

**Referenced by:** [AI_CONTEXT.md](./AI_CONTEXT.md)

## Overview

TAMU (Texas A&M Geocoding Service) is an external API service that provides geocoding data for address fields throughout the MMRIA application. The service converts street addresses into precise geographic coordinates and census data.

**Critical Rule for Case Generator:** TAMU geocode fields must NEVER be generated with fake data. They should be left empty ("") because they require real external API calls to the Texas A&M Geocoding Service.

## Service Details

**Namespace:** `mmria.common.texas_am`

**C# Data Structures:**
- `geocode_response.cs` - Response wrapper
- `OutputGeocode` - Geocode result with coordinates
- `CensusValue` - Census tract and demographic data
- `Census_Variables.cs` - Census variable definitions

**JavaScript Integration:**
- **validator.js** (lines 1920-2100) - Auto-geocode on address changes for 8 locations
- **MMRIA_calculations.js** (lines 880-2700) - Manual geocoding button handlers for all 10 locations

## The 16 Standard Geocode Fields

Most geocode locations populate all 16 fields:

### Core Location Fields (2)
- `latitude` - Geographic latitude coordinate
- `longitude` - Geographic longitude coordinate

### Feature Matching Fields (2)
- `feature_matching_result_type` - Match quality indicator (FeatureMatchingResultType)
- `feature_matching_geography_type` - Geography type of match (FeatureMatchingGeographyType)

### NAACCR GIS Fields (4)
- `naaccr_gis_coordinate_quality_code` - NAACCR coordinate quality code (1-9)
- `naaccr_gis_coordinate_quality_type` - NAACCR coordinate quality description
- `naaccr_census_tract_certainty_code` - NAACCR census tract certainty code (1-9)
- `naaccr_census_tract_certainty_type` - NAACCR census tract certainty description

### Census FIPS Fields (6)
- `census_state_fips` - State FIPS code (2 digits)
- `census_county_fips` - County FIPS code (3 digits)
- `census_tract_fips` - Census tract FIPS code (6 digits)
- `census_cbsa_fips` - Core-Based Statistical Area FIPS code
- `census_cbsa_micro` - Micropolitan indicator (0 or 1)
- `census_met_div_fips` - Metropolitan Division FIPS code

### Calculated Fields (2)
- `urban_status` - Urban classification (calculated from census data)
  - Values: "Metropolitan Division", "Metropolitan", "Micropolitan", "Rural", "Undetermined"
- `state_county_fips` - Combined state + county FIPS (calculated: census_state_fips + census_county_fips)

## All Geocode Locations (10 Total)

### Single-Form Locations (6 locations)

#### 1. Death Certificate - Place of Last Residence
**Path:** `death_certificate/place_of_last_residence/`
**Fields:** All 16 standard geocode fields
**Source:** validator.js x2f_ocl(), MMRIA_calculations.js geocode_dc_last_res()

#### 2. Death Certificate - Address of Injury
**Path:** `death_certificate/address_of_injury/`
**Fields:** `latitude`, `longitude` only (2 fields)
**Source:** validator.js x6d_ocl()

#### 3. Death Certificate - Address of Death
**Path:** `death_certificate/address_of_death/`
**Fields:** `latitude`, `longitude` only (2 fields)
**Source:** validator.js x82_ocl()

#### 4. Birth Certificate - Facility of Delivery Location
**Path:** `birth_fetal_death_certificate_parent/facility_of_delivery_location/`
**Fields:** `latitude`, `longitude` only (2 fields)
**Source:** validator.js xa9_ocl()

#### 5. Birth Certificate - Location of Residence
**Path:** `birth_fetal_death_certificate_parent/location_of_residence/`
**Fields:** `latitude`, `longitude` only (2 fields)
**Source:** validator.js xe1_ocl()

#### 6. Prenatal - Location of Primary Prenatal Care Facility
**Path:** `prenatal/location_of_primary_prenatal_care_facility/`
**Fields:** `latitude`, `longitude` only (2 fields)
**Source:** validator.js x19f_ocl()

### Grid/Multiform Locations (4 grids)

These are arrays where each item can have geocode fields.

#### 7. ER Visit/Hospital Medical Records Grid
**Path:** `er_visit_and_hospital_medical_records[index]/name_and_location_facility/`
**Fields:** All 16 standard geocode fields
**Grid Type:** Multiform (0-N items)
**Source:** validator.js x289_ocl(), MMRIA_calculations.js geocode_erh_name_and_location_facility()

#### 8. Other Medical Office Visits Grid
**Path:** `other_medical_office_visits[index]/location_of_medical_care_facility/`
**Fields:** All 16 standard geocode fields
**Grid Type:** Multiform (0-N items)
**Source:** validator.js x31e_ocl(), MMRIA_calculations.js geocode_omov_location_of_medical_care_facility()

#### 9. Medical Transport Grid - Origin Address
**Path:** `medical_transport[index]/origin_information/address/`
**Fields:** All 16 standard geocode fields
**Grid Type:** Multiform (0-N items)
**Source:** MMRIA_calculations.js medical_transport_origin_information_address_get_coordinates()

#### 10. Medical Transport Grid - Destination Address
**Path:** `medical_transport[index]/destination_information/address/`
**Fields:** All 16 standard geocode fields
**Grid Type:** Multiform (0-N items)
**Source:** MMRIA_calculations.js medical_transport_destination_information_address_get_coordinates()

## Implementation in Case Generator

**Method:** `PostProcessTAMU(Dictionary<string, object?> caseData)`
**Location:** CaseDataGenerator.cs

The method clears all geocode fields after case generation:

1. **Single-Form Locations:** Directly access and clear fields
2. **Grid Locations:** Iterate through each array item and clear fields
3. **Helper Function:** `ClearGeocodeFields()` clears all 16 standard fields

**Pattern:**
```csharp
void ClearGeocodeFields(Dictionary<string, object?> dict)
{
    if (dict.ContainsKey("latitude")) dict["latitude"] = "";
    if (dict.ContainsKey("longitude")) dict["longitude"] = "";
    // ... clear remaining 14 fields
}
```

## Why This Matters

**Problem:** Generating fake geocode data creates invalid coordinates and census data that:
- Cannot be validated against real geographic data
- Produces meaningless social determinant analysis
- Causes confusion when reviewing test cases
- Breaks geocoding workflows (users don't know fields are fake)

**Solution:** Leave geocode fields empty ("") so they can be populated via the actual TAMU geocoding API when:
- Users click "Get Coordinates" buttons in MMRIA UI
- Address fields are changed and auto-geocoding triggers (validator.js)
- Batch geocoding operations run (MMRIA_calculations.js)

## Related Documentation

- **CVS Integration:** [CVS_Community_Vital_Signs_Context.md](./CVS_Community_Vital_Signs_Context.md)
- **Main Context:** [AI_CONTEXT.md](./AI_CONTEXT.md)
- **Background Jobs:** [MMRIA_Background_Jobs_Documentation.md](./MMRIA_Background_Jobs_Documentation.md)

## Source Files

**C# Backend:**
- `nccdphp-drh-mmria-common/mmria.common/texas_am/geocode_response.cs`
- `nccdphp-drh-mmria-common/mmria.common/texas_am/Census_Variables.cs`
- `nccdphp-drh-mmria-utilities/mmria-case-generator/Generators/CaseDataGenerator.cs` (PostProcessTAMU method)

**JavaScript Frontend:**
- `source-code/mmria/mmria-server/wwwroot/scripts/validator.js` (lines 1920-2100)
- `source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js` (lines 880-2700)
- `source-code/mmria/mmria-server/wwwroot/scripts/mmria.js` (get_geocode_info function)

**API Controller:**
- TBD - Geocoding API controller location not yet documented
