# Story 30.5: Refactor BatchItemProcessingService to Use Shared GeocodingManager

Status: ready-for-dev

## Story

As a developer,
I want the vital import batch processing service to use the shared `GeocodingManager` and `CaseGeocodingManager` from SharedLibraries,
so that geocoding logic is not duplicated between the batch service and the web layer.

## Acceptance Criteria

1. The private `get_geocode_info(string street, ...)` method and the `GeocodeTuple` inner class are removed from `BatchItemProcessingService`.
2. The four private `Set_*_Geocode` methods are removed: `Set_facility_of_delivery_location_Geocode`, `Set_location_of_residence_Geocode`, `Set_place_of_last_residence_Geocode`, `Set_address_of_death_Geocode`.
3. Calls to those private methods are replaced with the corresponding `CaseGeocodingManager.Apply_*_Geocode()` methods.
4. `geocode_api_key` resolution stays in `BatchItemProcessingService` (from `db_config_set.name_value["geocode_api_key"]` as today) — not moved to the manager.
5. `TAMUGeoCode` class at `mmria.services/Utilities/TAMUGeocode.cs` is **deleted** after this story — its logic now lives in `GeocodingManager`.
6. Existing IJE import test suite (`mmria.services.tests`) passes: `dotnet test mmria.services.tests.csproj`.
7. `dotnet build mmria.services.csproj` — zero errors.

## Tasks / Subtasks

- [ ] Replace private geocode infrastructure (AC: #1, #2, #3)
  - [ ] Locate `get_geocode_info(...)` in `BatchItemProcessingService.cs` and remove it
  - [ ] Locate `GeocodeTuple` inner class and remove it
  - [ ] Inject `GeocodingManager` and `CaseGeocodingManager` — they are in `mmria.common` which `mmria.services` already references; no new project reference needed
  - [ ] Replace each call to a private `Set_*_Geocode` method with the corresponding `CaseGeocodingManager.Apply_*_Geocode(caseDoc, geocodeResult)` call
- [ ] Preserve `geocode_api_key` resolution (AC: #4)
  - [ ] Keep: `db_config_set.name_value["geocode_api_key"]` as the key source
  - [ ] Pass it as the `geocodeApiKey` parameter to `GeocodingManager.FetchGeocode(...)`
- [ ] Delete `TAMUGeocode.cs` (AC: #5)
  - [ ] Delete `mmria.services/Utilities/TAMUGeocode.cs`
  - [ ] Confirm no remaining references to `TAMUGeoCode` type
- [ ] Build and test (AC: #6, #7)
  - [ ] `dotnet test nccdphp-drh-mmria-services/mmria.services.tests/mmria.services.tests.csproj` — all previously passing tests pass
  - [ ] `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj` — zero errors

## Dev Notes

**Files to modify:**
- `nccdphp-drh-mmria-services/mmria.services/Actors/BatchItemProcessingService.cs`

**File to delete:**
- `nccdphp-drh-mmria-services/mmria.services/Utilities/TAMUGeocode.cs`

**Private methods being replaced** (in `BatchItemProcessingService.cs`):
```csharp
// REMOVE these:
private async Task<GeocodeTuple> get_geocode_info(string street, string city, string state, string zip)
private class GeocodeTuple { ... }
private void Set_facility_of_delivery_location_Geocode(ExpandoObject caseDoc, GeocodeTuple t)
private void Set_location_of_residence_Geocode(ExpandoObject caseDoc, GeocodeTuple t)
private void Set_place_of_last_residence_Geocode(ExpandoObject caseDoc, GeocodeTuple t)
private void Set_address_of_death_Geocode(ExpandoObject caseDoc, GeocodeTuple t)
```

**Replacement pattern:**
```csharp
// Before:
var geo = await get_geocode_info(street, city, state, zip);
Set_place_of_last_residence_Geocode(caseDoc, geo);

// After:
var result = _geocodingManager.FetchGeocode(geocodeApiKey, street, city, state, zip, censusYear);
_caseGeocodingManager.Apply_DC_PlaceOfLastResidence_Geocode(caseDoc, result);
```

**`geocode_api_key` source** (keep as-is):
```csharp
string geocodeApiKey = "";
if (db_config_set.name_value.TryGetValue("geocode_api_key", out var key))
    geocodeApiKey = key;
```

**Depends on:** Stories 30.1 and 30.2. Can proceed in parallel with Story 30.3.
