# Story 30.2: Create CaseGeocodingManager with Per-Location Apply Methods

Status: done

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

- [x] Create `CaseGeocodingManager.cs` (AC: #1, #5)
  - [x] Place in `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/`
- [x] Implement all 10 apply methods (AC: #2, #3, #4)
  - [x] `Apply_DC_PlaceOfLastResidence_Geocode(ExpandoObject caseDoc, GeocodeResult result)` → path: `death_certificate/place_of_last_residence/`
  - [x] `Apply_DC_AddressOfInjury_Geocode(ExpandoObject caseDoc, GeocodeResult result)` → path: `death_certificate/address_of_injury/`
  - [x] `Apply_DC_AddressOfDeath_Geocode(ExpandoObject caseDoc, GeocodeResult result)` → path: `death_certificate/address_of_death/`
  - [x] `Apply_BC_FacilityOfDelivery_Geocode(ExpandoObject caseDoc, GeocodeResult result)` → path: `birth_fetal_death_certificate_parent/facility_of_delivery_location/`
  - [x] `Apply_BC_LocationOfResidence_Geocode(ExpandoObject caseDoc, GeocodeResult result)` → path: `birth_fetal_death_certificate_parent/location_of_residence/`
  - [x] `Apply_PC_PrimaryCareFacility_Geocode(ExpandoObject caseDoc, GeocodeResult result)` → path: `prenatal_care_record/location_of_primary_prenatal_care_facility/`
  - [x] `Apply_ERH_Location_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)` → path: `er_visit_and_hospital_medical_records[listIndex]/location/`
  - [x] `Apply_OMV_LocationOfCare_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)` → path: `other_medical_office_visits[listIndex]/location_of_medical_care_facility/`
  - [x] `Apply_MT_OriginAddress_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)` → path: `medical_transport[listIndex]/origin_information/address/`
  - [x] `Apply_MT_DestinationAddress_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)` → path: `medical_transport[listIndex]/destination_information/address/`
- [x] Build (AC: #6)
  - [x] `dotnet build mmria.common.csproj` — zero errors

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

## Dev Agent Record

**Agent:** Amelia (bmad-agent-dev)

**Status:** complete

**Files created:**
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseGeocodingManager.cs`

**Files modified:** none.

**Build result:** `dotnet build nccdphp-drh-mmria-common/mmria.common/mmria.common.csproj` — Build succeeded, 0 errors, 144 warnings (all pre-existing; no warnings from the new file).

**Design decisions:**
- Sealed class with parameterless ctor to mirror Story 30.1's `GeocodingManager`. Safe as a DI singleton — no mutable state, no external dependencies.
- Instance methods (not static) so the class can be registered/injected via DI.
- Uses `migrate.C_Get_Set_Value` (the existing `set_value` partial class in `mmria.common.getset`) — same helper `BatchItemProcessingService` uses. A fresh `C_Get_Set_Value` is constructed per apply call (its constructor takes a `StringBuilder` sink; we pass a throwaway one). This matches the invocation style used in `BatchItemProcessingService.cs` line 825.
- List-index paths are resolved manually because `migrate.C_Get_Set_Value.set_value` does not support numeric segments in the path string — for list locations we resolve the item dictionary at `listIndex` and then hand `set_value` the sub-path within that dictionary. The final `set_value` calls still follow the existing `path/leaf` convention.
- 10 methods total: 6 static-location, 4 dynamic-list. Each writes the same 15 geocode fields.
- Unmatchable/empty guard: `FeatureMatchingGeographyType` is either empty (Story 30.1 failure contract) or literally `"Unmatchable"` (JS legacy) → write `""` to every field. Otherwise write `result.<field> ?? ""`. This satisfies AC #4 and covers both failure signals Winston described.
- Null-safety guards: if `caseDoc` or `result` is `null`, methods are no-ops. Dynamic-list methods also no-op on missing list, missing sub-dictionary, or out-of-range `listIndex`. No exceptions are thrown from this class.

**Deviations from ACs:** none — all six ACs met.

**Public surface for downstream stories (30.3 → 30.7):**

Namespace: `mmria.common.SharedLibraries.Case.Manager`
Class: `public sealed class CaseGeocodingManager` — parameterless ctor, no dependencies, safe as singleton.

Method signatures:
```csharp
void Apply_DC_PlaceOfLastResidence_Geocode(ExpandoObject caseDoc, GeocodeResult result);
void Apply_DC_AddressOfInjury_Geocode(ExpandoObject caseDoc, GeocodeResult result);
void Apply_DC_AddressOfDeath_Geocode(ExpandoObject caseDoc, GeocodeResult result);
void Apply_BC_FacilityOfDelivery_Geocode(ExpandoObject caseDoc, GeocodeResult result);
void Apply_BC_LocationOfResidence_Geocode(ExpandoObject caseDoc, GeocodeResult result);
void Apply_PC_PrimaryCareFacility_Geocode(ExpandoObject caseDoc, GeocodeResult result);
void Apply_ERH_Location_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex);
void Apply_OMV_LocationOfCare_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex);
void Apply_MT_OriginAddress_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex);
void Apply_MT_DestinationAddress_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex);
```

**DI expectations (30.3):** Register both `GeocodingManager` and `CaseGeocodingManager` as singletons in `Program.cs`. Inject both into the geocode API endpoint / controller.

**`locationKey` → apply-method mapping (for 30.3 endpoint):**

| `locationKey` (suggested) | Apply method | Takes `listIndex`? |
|---|---|---|
| `dc_place_of_last_residence` | `Apply_DC_PlaceOfLastResidence_Geocode` | no |
| `dc_address_of_injury` | `Apply_DC_AddressOfInjury_Geocode` | no |
| `dc_address_of_death` | `Apply_DC_AddressOfDeath_Geocode` | no |
| `bc_facility_of_delivery` | `Apply_BC_FacilityOfDelivery_Geocode` | no |
| `bc_location_of_residence` | `Apply_BC_LocationOfResidence_Geocode` | no |
| `pc_primary_care_facility` | `Apply_PC_PrimaryCareFacility_Geocode` | no |
| `erh_location` | `Apply_ERH_Location_Geocode` | **yes** |
| `omv_location_of_care` | `Apply_OMV_LocationOfCare_Geocode` | **yes** |
| `mt_origin_address` | `Apply_MT_OriginAddress_Geocode` | **yes** |
| `mt_destination_address` | `Apply_MT_DestinationAddress_Geocode` | **yes** |

Recommended endpoint contract for 30.3:
- Request carries `locationKey`, `street`/`city`/`state`/`zip`/`censusYear`, and (when the location is dynamic) `listIndex`.
- Endpoint calls `GeocodingManager.FetchGeocode(...)`, loads the case, dispatches on `locationKey` to the matching `CaseGeocodingManager.Apply_..._Geocode(...)`, then saves the case.
- The four dynamic keys require a non-negative `listIndex`; return 400 if missing.

**Notes for 30.3–30.7:**
- The apply methods are silent on invalid inputs (null case, missing list, out-of-range index). If 30.3 wants to surface those to the API caller, it must validate *before* invoking `Apply_...` — the manager will not throw.
- Do **not** call `Apply_..._Geocode` on a legacy strongly-typed case model; these methods target `ExpandoObject` case documents (same shape `CaseManager.SaveCaseAsync` operates on).
- `migrate.C_Get_Set_Value.set_value` requires each intermediate dictionary segment (e.g. `origin_information`, `address`, `location`) to already exist on the case document. This matches how the JS layer wrote these fields, but 30.3 should be aware — if a caller invokes the manager on a case where the target sub-object was never initialized, the write silently no-ops. If 30.7 (or 30.3) needs to create missing sub-objects, that scaffolding must live in the calling code, not here.
- Story 30.5 will retire `TAMUGeoCode` in `mmria.services` and its `BatchItemProcessingService` caller — the field-write patterns in `BatchItemProcessingService.Set_place_of_last_residence_Gecocode` / `Set_address_of_death_Gecocode` (lines 3353 and 3469) are the ground-truth reference for the geocode path strings used above; they should be replaced by calls into `CaseGeocodingManager` at that time.
- No unit tests were added, per Winston's directive (Nick handles E2E after all seven stories land).
