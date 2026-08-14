# Story 30.3: New API Endpoint — POST /api/case-geocode/{caseId}/{locationKey}

Status: ready-for-dev

## Story

As an abstractor,
when I click "Validate Address and Get Geography Context" on a case form,
I want the geocoding, optional CVS lookup, and case save to happen in a single server-side operation,
so that geocode data is never lost due to a mid-operation network failure.

## Acceptance Criteria

1. Route `POST /api/case-geocode/{caseId}/{locationKey}` exists. Request body (JSON): `{ street, city, state, zip, listIndex? }`.
2. Action flow:
   a. Resolve tenant config; get `geocode_api_key` via `configuration.GetSharedString`
   b. Call `GeocodingManager.FetchGeocode(geocodeApiKey, street, city, state, zip, censusYear)` — `censusYear` from request body or derived from case document's `home_record.date_of_death.year`
   c. Load current case document from CouchDB
   d. Call the matching `CaseGeocodingManager.Apply_*_Geocode(caseDoc, result[, listIndex])` based on `locationKey`
   e. If `locationKey == "dc_place_of_last_residence"` and geocode was not Unmatchable: call the CVS API using `state_county_fips` and `census_tract_fips` from the result and write CVS fields to the case document
   f. Save updated case document to CouchDB
   g. Return `{ ok: true }` on success — **do not return the GeocodeResult fields**; the client will reload the full case
3. `[Authorize(Roles = "abstractor")]` — unauthorized requests return 401/403.
4. Invalid `locationKey` → `400 BadRequest`.
5. Case not found → `404 NotFound`.
6. For dynamic-list `locationKey` values (`erh_location`, `omv_location_of_care`, `mt_origin_address`, `mt_destination_address`): `listIndex` is required in the request body → `400 BadRequest` if absent.
7. The existing `GET /api/tamuGeoCode` endpoint (in `tamuGeoCodeController`) is **not removed** — kept for backward compatibility during migration.
8. Architecture rule: no direct CouchDB calls in the controller — load case via `ICaseRepository`, save via `ICaseRepository`. `GeocodingManager` and `CaseGeocodingManager` handle business logic.

## Tasks / Subtasks

- [ ] Create `CaseGeocodeController` (or add to an existing appropriate controller) (AC: #1, #3, #7, #8)
  - [ ] Route: `[HttpPost("{caseId}/{locationKey}")]` under `/api/case-geocode/`
  - [ ] Inject: `ICaseRepository`, `GeocodingManager`, `CaseGeocodingManager`, tenant config
  - [ ] Authorize: `[Authorize(Roles = "abstractor")]`
- [ ] Implement `locationKey` dispatch (AC: #4, #6)
  - [ ] Define the valid location key set (10 keys from Epic 30 inventory)
  - [ ] Return `400` for unknown keys
  - [ ] Return `400` if `listIndex` is absent for the 4 dynamic-list keys
- [ ] Implement action flow (AC: #2)
  - [ ] Read `geocode_api_key` from tenant config via `configuration.GetSharedString`
  - [ ] Derive `censusYear` — accept from request body if provided; otherwise load case doc and read `home_record.date_of_death.year`
  - [ ] Call `GeocodingManager.FetchGeocode(...)`
  - [ ] Load case doc from CouchDB via `ICaseRepository`
  - [ ] Call matching `CaseGeocodingManager.Apply_*_Geocode(caseDoc, result[, listIndex])`
  - [ ] **CVS step**: if `locationKey == "dc_place_of_last_residence"` and `result.FeatureMatchingGeographyType != "Unmatchable"`: call CVS API with `state_county_fips` + `census_tract_fips` + `home_record.date_of_death.year`; write returned CVS fields to the case document
  - [ ] Save updated case doc via `ICaseRepository`
  - [ ] Return `{ ok: true }` (not the geocode result fields — client reloads the case)
- [ ] Wire DI registration for `GeocodingManager` and `CaseGeocodingManager` in `Program.cs`
- [ ] Build and smoke test (AC: #1–#8)
  - [ ] `POST /api/case-geocode/{caseId}/dc_place_of_last_residence` with a valid GA address — verify case doc updated in CouchDB
  - [ ] `POST` with unknown `locationKey` — verify 400
  - [ ] `POST` with `erh_location` and no `listIndex` — verify 400
  - [ ] CVS call fires for `dc_place_of_last_residence` (check network/logs)
  - [ ] CVS call does NOT fire for `dc_address_of_death` (non-CVS location)

## Dev Notes

**New file:** `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs`

**Valid location keys and dynamic-list flag:**
```csharp
private static readonly HashSet<string> _validKeys = new()
{
    "dc_place_of_last_residence", "dc_address_of_injury", "dc_address_of_death",
    "bc_facility_of_delivery", "bc_location_of_residence", "pc_primary_care_facility",
    "erh_location", "omv_location_of_care", "mt_origin_address", "mt_destination_address"
};
private static readonly HashSet<string> _listKeys = new()
{
    "erh_location", "omv_location_of_care", "mt_origin_address", "mt_destination_address"
};
```

**CVS call** — use the existing CVS service/manager already wired from Epic 10 (`CVSManager`). Pass `state_county_fips`, `census_tract_fips`, and year. Write CVS response fields to the case document at the `death_certificate/place_of_last_residence/` path (same paths CVS populated before). If CVS call fails, log and continue — the geocode data is already saved.

**Response shape** (client reloads — no need to return field values):
```json
{ "ok": true }
```

**Architecture rule:** Controller resolves tenant config (`host_prefix`, `db_config`), delegates to managers. No `CouchDbHttpClient.ExecuteAsync` calls in this controller.

**Depends on:** Stories 30.1 and 30.2.
