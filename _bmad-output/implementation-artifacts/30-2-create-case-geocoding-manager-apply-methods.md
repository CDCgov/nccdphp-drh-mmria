# Story 30.2: Create CaseGeocodingManager with Per-Location Apply Methods

Status: ready-for-dev

## Story

As a developer,
I want named methods in SharedLibraries that apply a `GeocodeResult` to a specific case document location,
so that both the web layer and the batch service write geocode fields using a single implementation with no field-path duplication.

## Acceptance Criteria

1. `mmria.common/SharedLibraries/Case/Manager/CaseGeocodingManager.cs` exists with 10 public methods — one per geocoding location.
2. Static-location methods signature: `void Apply_[LocationKey]_Geocode(ExpandoObject caseDoc, GeocodeResult result)`. Dynamic-list methods signature: `void Apply_[LocationKey]_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)`.
3. Each method writes all 15 geocode fields to the correct case document path using `C_Get_Set_Value` patterns consistent with the existing codebase.
4. When `result.FeatureMatchingResultType == "Unmatchable"` (or result is the empty/error result), all 15 fields are written as empty string — mirrors current JS Unmatchable path.
5. No CouchDB access in this class — pure document mutation only.
6. `dotnet build mmria.common.csproj` — zero errors.

## Tasks / Subtasks

- [ ] Create `CaseGeocodingManager.cs` (AC: #1, #5)
  - [ ] Place in `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/`
- [ ] Implement all 10 apply methods (AC: #2, #3, #4)
  - [ ] `Apply_DC_PlaceOfLastResidence_Geocode(ExpandoObject caseDoc, GeocodeResult result)` → path: `death_certificate/place_of_last_residence/`
  - [ ] `Apply_DC_AddressOfInjury_Geocode(ExpandoObject caseDoc, GeocodeResult result)` → path: `death_certificate/address_of_injury/`
  - [ ] `Apply_DC_AddressOfDeath_Geocode(ExpandoObject caseDoc, GeocodeResult result)` → path: `death_certificate/address_of_death/`
  - [ ] `Apply_BC_FacilityOfDelivery_Geocode(ExpandoObject caseDoc, GeocodeResult result)` → path: `birth_fetal_death_certificate_parent/facility_of_delivery_location/`
  - [ ] `Apply_BC_LocationOfResidence_Geocode(ExpandoObject caseDoc, GeocodeResult result)` → path: `birth_fetal_death_certificate_parent/location_of_residence/`
  - [ ] `Apply_PC_PrimaryCareFacility_Geocode(ExpandoObject caseDoc, GeocodeResult result)` → path: `prenatal_care_record/location_of_primary_prenatal_care_facility/`
  - [ ] `Apply_ERH_Location_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)` → path: `er_visit_and_hospital_medical_records[listIndex]/location/`
  - [ ] `Apply_OMV_LocationOfCare_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)` → path: `other_medical_office_visits[listIndex]/location_of_medical_care_facility/`
  - [ ] `Apply_MT_OriginAddress_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)` → path: `medical_transport[listIndex]/origin_information/address/`
  - [ ] `Apply_MT_DestinationAddress_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)` → path: `medical_transport[listIndex]/destination_information/address/`
- [ ] Build (AC: #6)
  - [ ] `dotnet build mmria.common.csproj` — zero errors

## Dev Notes

**New file:** `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseGeocodingManager.cs`

**15 geocode fields to write at each location** (same set for all 10):
`latitude`, `longitude`, `feature_matching_geography_type`, `naaccr_gis_coordinate_quality_code`, `naaccr_gis_coordinate_quality_type`, `naaccr_census_tract_certainty_code`, `naaccr_census_tract_certainty_type`, `census_state_fips`, `census_county_fips`, `census_tract_fips`, `census_cbsa_fips`, `census_cbsa_micro`, `census_met_div_fips`, `urban_status`, `state_county_fips`

**Path-based setter pattern** — use `C_Get_Set_Value` consistent with existing managers. Example for `dc_place_of_last_residence`:
```csharp
C_Get_Set_Value.set_value(caseDoc,
    "death_certificate/place_of_last_residence/latitude",
    result.Latitude ?? "");
// repeat for all 15 fields
```

**Dynamic list paths** — for `listIndex` locations, the path includes the array index:
```csharp
C_Get_Set_Value.set_value(caseDoc,
    $"er_visit_and_hospital_medical_records/{listIndex}/location/latitude",
    result.Latitude ?? "");
```

**Unmatchable / empty-result guard:**
```csharp
// result.FeatureMatchingGeographyType == "Unmatchable" or all fields are empty
// In both cases, write "" for all 15 fields — do not short-circuit
```

**Reference for existing field-write patterns:** `BatchItemProcessingService.cs` methods `Set_place_of_last_residence_Geocode`, `Set_address_of_death_Geocode`, etc. — use those as the path reference but implement through `C_Get_Set_Value` not direct property assignment.

**Depends on:** Story 30.1 (`GeocodeResult` type must exist).
