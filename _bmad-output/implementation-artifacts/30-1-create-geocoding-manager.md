# Story 30.1: Create GeocodingManager in SharedLibraries

Status: ready-for-dev

## Story

As a developer,
I want a single injectable service in `mmria.common` that calls the TAMU geocoding API and returns a fully-resolved `GeocodeResult` DTO,
so that all geocoding paths in the codebase share one implementation and urban-status logic is never duplicated again.

## Acceptance Criteria

1. `mmria.common/SharedLibraries/Geocoding/Manager/GeocodingManager.cs` exists and compiles. (The `Manager/` and `DAL/` folders already exist and are empty.)
2. `GeocodeResult` record/class holds all 15 output fields: `Latitude`, `Longitude`, `FeatureMatchingGeographyType`, `NAACCRGISCoordinateQualityCode`, `NAACCRGISCoordinateQualityType`, `NAACCRCensusTractCertaintyCode`, `NAACCRCensusTractCertaintyType`, `CensusStateFips`, `CensusCountyFips`, `CensusTractFips` (from `CensusTract`), `CensusCbsaFips`, `CensusCbsaMicro`, `CensusMetDivFips`, `UrbanStatus`, `StateCountyFips`.
3. `GeocodingManager.FetchGeocode(string geocodeApiKey, string street, string city, string state, string zip, string censusYear)` returns a `GeocodeResult`. `geocodeApiKey` is passed in at call time — the manager has no config dependency.
4. Urban-status derivation is correct:
   - `NAACCRCensusTractCertaintyCode` in [1–6] AND `CensusCbsaFips > 0` AND `CensusMetDivFips` non-empty → `"Metropolitan Division"`
   - `NAACCRCensusTractCertaintyCode` in [1–6] AND `CensusCbsaFips > 0` AND `CensusCbsaMicro == "0"` → `"Metropolitan"`
   - `NAACCRCensusTractCertaintyCode` in [1–6] AND `CensusCbsaFips > 0` AND `CensusCbsaMicro == "1"` → `"Micropolitan"`
   - `NAACCRCensusTractCertaintyCode` in [1–6] AND `CensusCbsaFips == ""` → `"Rural"`
   - Anything else → `"Undetermined"`
5. `StateCountyFips = CensusStateFips + CensusCountyFips` is computed here.
6. State value with `-` separator (e.g. `"GA-Georgia"`) is split on `-` and only the first part is sent to TAMU.
7. On `FeatureMatchingResultType == "Unmatchable"` or any TAMU HTTP error, returns a `GeocodeResult` with all fields empty and `UrbanStatus = "Undetermined"` — no exception thrown.
8. `TAMUGeoCode` in `mmria.services/Utilities/` is left in place (not deleted) until Story 30.5 removes it.
9. `dotnet build mmria.common.csproj` — zero errors.

## Tasks / Subtasks

- [ ] Create `GeocodeResult` (AC: #2)
  - [ ] Define as a record or class in `mmria.common/SharedLibraries/Geocoding/` (or a sibling file in `Manager/`)
  - [ ] 15 properties matching the field names in AC-2
- [ ] Create `GeocodingManager` (AC: #1, #3, #6)
  - [ ] Method: `public GeocodeResult FetchGeocode(string geocodeApiKey, string street, string city, string state, string zip, string censusYear)`
  - [ ] Split state on `-` before sending to TAMU (e.g. `"GA-Georgia".Split('-')[0]`)
  - [ ] Call TAMU HTTP endpoint using existing model types from `mmria.common/texas_am/` — adapt from `TAMUGeoCode` in `mmria.services/Utilities/TAMUGeocode.cs` without creating an assembly dependency on services
- [ ] Implement urban-status derivation (AC: #4, #5)
  - [ ] After mapping TAMU response fields, compute `UrbanStatus` per the branching logic
  - [ ] Compute `StateCountyFips = CensusStateFips + CensusCountyFips`
- [ ] Handle error/unmatchable cases (AC: #7)
  - [ ] `FeatureMatchingResultType == "Unmatchable"` → return empty result
  - [ ] HTTP error or exception → catch, return empty result with `UrbanStatus = "Undetermined"`
- [ ] Build (AC: #9)
  - [ ] `dotnet build nccdphp-drh-mmria-common/mmria.common/mmria.common.csproj` — zero errors

## Dev Notes

**New files:**
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Geocoding/Manager/GeocodingManager.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Geocoding/GeocodeResult.cs` (or inline in Manager)

**Existing model types** (use these — don't reinvent):
`mmria.common/texas_am/` contains `geocode_response`, `OutputGeocode`, `CensusValue`, etc.

**TAMU HTTP call reference** — adapt from `mmria.services/Utilities/TAMUGeocode.cs`:
```
GET https://geoservices.tamu.edu/Services/Geocode/WebService/GeocoderWebServiceHttpNonParsed_V04_01.aspx
    ?streetAddress=...&city=...&state=...&zip=...&censusYear=...&format=json&version=4.01&apiKey=...
```
Do NOT import or reference the `mmria.services` assembly from `mmria.common`.

**Urban-status branching** (mirrors the JS in `MMRIA_calculations.js` `geocode_dc_last_res`):
```csharp
int certaintyCode = int.TryParse(result.NAACCRCensusTractCertaintyCode, out var c) ? c : 0;
int cbsaFips = int.TryParse(result.CensusCbsaFips, out var f) ? f : 0;
bool inRange = certaintyCode >= 1 && certaintyCode <= 6;

if (inRange && cbsaFips > 0 && !string.IsNullOrEmpty(result.CensusMetDivFips))
    urbanStatus = "Metropolitan Division";
else if (inRange && cbsaFips > 0 && result.CensusCbsaMicro == "0")
    urbanStatus = "Metropolitan";
else if (inRange && cbsaFips > 0 && result.CensusCbsaMicro == "1")
    urbanStatus = "Micropolitan";
else if (inRange && result.CensusCbsaFips == "")
    urbanStatus = "Rural";
else
    urbanStatus = "Undetermined";
```
