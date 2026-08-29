# Story 30.1: Create GeocodingManager in SharedLibraries

Status: done

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

- [x] Create `GeocodeResult` (AC: #2)
  - [x] Define as a record or class in `mmria.common/SharedLibraries/Geocoding/` (or a sibling file in `Manager/`)
  - [x] 15 properties matching the field names in AC-2
- [x] Create `GeocodingManager` (AC: #1, #3, #6)
  - [x] Method: `public GeocodeResult FetchGeocode(string geocodeApiKey, string street, string city, string state, string zip, string censusYear)`
  - [x] Split state on `-` before sending to TAMU (e.g. `"GA-Georgia".Split('-')[0]`)
  - [x] Call TAMU HTTP endpoint using existing model types from `mmria.common/texas_am/` — adapt from `TAMUGeoCode` in `mmria.services/Utilities/TAMUGeocode.cs` without creating an assembly dependency on services
- [x] Implement urban-status derivation (AC: #4, #5)
  - [x] After mapping TAMU response fields, compute `UrbanStatus` per the branching logic
  - [x] Compute `StateCountyFips = CensusStateFips + CensusCountyFips`
- [x] Handle error/unmatchable cases (AC: #7)
  - [x] `FeatureMatchingResultType == "Unmatchable"` → return empty result
  - [x] HTTP error or exception → catch, return empty result with `UrbanStatus = "Undetermined"`
- [x] Build (AC: #9)
  - [x] `dotnet build nccdphp-drh-mmria-common/mmria.common/mmria.common.csproj` — zero errors

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

## Dev Agent Record

**Agent:** Amelia (bmad-agent-dev)
**Completion date:** 2026-08-19

### Files created

- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Geocoding/GeocodeResult.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Geocoding/Manager/GeocodingManager.cs`

### Files modified

None (other than this story file's Status, task checkboxes, and this Dev Agent Record).

### Build result

`dotnet build nccdphp-drh-mmria-common/mmria.common/mmria.common.csproj --nologo`
→ **Build succeeded. 0 Warning(s). 0 Error(s).**

### Deviations from ACs

None. All 9 ACs satisfied as written. Notes on implementation choices:

- **AC-5 wording** ("`StateCountyFips = CensusStateFips + CensusCountyFips` is computed here") is implemented as an unconditional string concatenation on the result object. The legacy code in `BatchItemProcessingService.Set_facility_of_delivery_Geocode` only concatenated when both parts were non-empty; the story wording is explicit ("computed here" without a conditional), so the manager always concatenates. When both parts are empty the result is `""`, which matches legacy behavior in that case.
- **Census-year normalization** was preserved from `TAMUGeoCode` (`GetCensusYear` → `NormalizeCensusYear`) so arbitrary year strings still resolve to `1990|2000|2010|2020` for the TAMU URL. Not required by an AC but preserves parity with the legacy call site so downstream stories don't regress on caller inputs like `"2018"` or `""`.
- **HttpClient reuse:** a single static `HttpClient` is held on `GeocodingManager` (legacy `TAMUGeoCode` created one per call). Prevents socket exhaustion under batch load; behavior otherwise identical.
- **Method is synchronous** per AC-3 signature (`GeocodeResult FetchGeocode(...)`). Uses `.GetStringAsync(...).Result` — same pattern as the legacy `TAMUGeoCode.execute`. If a downstream story needs an async variant, it can be added without breaking this one.

### Notes for downstream Epic 30 stories (30.2+)

**Public surface locked in by 30.1** — 30.2 authors should target exactly this:

- Namespace of manager: `mmria.common.SharedLibraries.Geocoding.Manager`
- Namespace of result: `mmria.common.SharedLibraries.Geocoding`
- Class: `GeocodingManager` (sealed, parameterless ctor; can be `new`-ed or DI-registered as a singleton — safe because state is limited to the static `HttpClient`)
- Method: `GeocodeResult FetchGeocode(string geocodeApiKey, string street, string city, string state, string zip, string censusYear)` — synchronous
- `GeocodeResult` is a class (not a record); all 15 properties are `string` and default to `""` except `UrbanStatus` which defaults to `"Undetermined"`. Consumers can safely read any property without null-checking.
- On failure/unmatchable, all 15 fields on the returned `GeocodeResult` are empty strings **except** `UrbanStatus = "Undetermined"`. Callers should NOT treat "empty latitude" as an exception — it is the documented failure signal (AC-7).
- The manager takes the raw state value (e.g. `"GA-Georgia"`) and splits internally. Downstream call sites should pass the raw case field value; they do **not** need to pre-split.
- The legacy `mmria.services.vitalsimport.Utilities.TAMUGeoCode` class is intentionally still in place (AC-8) and still referenced by `BatchItemProcessingService`. Story 30.5 removes it. Do not delete it in 30.2-30.4.
- No DI registration was added in 30.1 — the manager is stateless from the caller's perspective and can be either newed at the call site or registered as a singleton by the story that introduces the first DI-based consumer.
