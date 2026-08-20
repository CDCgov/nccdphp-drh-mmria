# Story 30.3: New API Endpoint — POST /api/case-geocode/{caseId}/{locationKey}

Status: done

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

- [x] Create `CaseGeocodeController` (or add to an existing appropriate controller) (AC: #1, #3, #7, #8)
  - [x] Route: `[HttpPost("{caseId}/{locationKey}")]` under `/api/case-geocode/`
  - [x] Inject: `ICaseRepository`, `GeocodingManager`, `CaseGeocodingManager`, tenant config
  - [x] Authorize: `[Authorize(Roles = "abstractor")]`
- [x] Implement `locationKey` dispatch (AC: #4, #6)
  - [x] Define the valid location key set (10 keys from Epic 30 inventory)
  - [x] Return `400` for unknown keys
  - [x] Return `400` if `listIndex` is absent for the 4 dynamic-list keys
- [x] Implement action flow (AC: #2)
  - [x] Read `geocode_api_key` from tenant config via `configuration.GetSharedString`
  - [x] Derive `censusYear` — accept from request body if provided; otherwise load case doc and read `home_record.date_of_death.year`
  - [x] Call `GeocodingManager.FetchGeocode(...)`
  - [x] Load case doc from CouchDB via `ICaseRepository`
  - [x] Call matching `CaseGeocodingManager.Apply_*_Geocode(caseDoc, result[, listIndex])`
  - [x] **CVS step**: if `locationKey == "dc_place_of_last_residence"` and `result.FeatureMatchingGeographyType != "Unmatchable"`: call CVS API with `state_county_fips` + `census_tract_fips` + `home_record.date_of_death.year`; write returned CVS fields to the case document
  - [x] Save updated case doc via `ICaseRepository`
  - [x] Return `{ ok: true }` (not the geocode result fields — client reloads the case)
- [x] Wire DI registration for `GeocodingManager` and `CaseGeocodingManager` in `Program.cs`
- [ ] Build and smoke test (AC: #1–#8) — build verified (`dotnet build -t:Compile` → 0 errors, 0 new warnings). Live smoke tests deferred to Nick's E2E after Epic 30 chain lands.

## Dev Agent Record

**Agent:** Amelia (bmad-agent-dev) — 2026-08-19

**Files created:**
- `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs`

**Files modified:**
- `source-code/mmria/mmria-server/Program.cs` — registered `GeocodingManager` and `CaseGeocodingManager` as singletons (parameterless ctors, no mutable state).

**Public surface for Story 30.4 (client refactor):**

- **Route:** `POST /api/case-geocode/{caseId}/{locationKey}`
- **Auth:** `[Authorize(Roles = "abstractor")]` — cookie auth as usual; no antiforgery attribute on the endpoint (matches `caseController.POST` style).
- **Request body (JSON):**
  ```json
  {
    "street": "string",
    "city": "string",
    "state": "string",       // "GA" or "GA-Georgia" both accepted (GeocodingManager splits on '-')
    "zip": "string",
    "censusYear": "2020",    // optional; falls back to home_record.date_of_death.year
    "listIndex": 0            // required for erh_location, omv_location_of_care, mt_origin_address, mt_destination_address
  }
  ```
- **Success response:** `200 OK` with `{ "ok": true }`. **The controller does not return geocode fields.** Client must reload the case (e.g. `GET /api/case?case_id=...`) and re-render.
- **Error responses:**
  - `400 BadRequest` with `{ "error": "<message>" }` — invalid/unknown `locationKey`; missing `caseId`; missing `listIndex` for a dynamic-list key; malformed JSON body.
  - `401 Unauthorized` / `403 Forbidden` — not authenticated / not `abstractor`.
  - `404 NotFound` with `{ "error": "Case not found: <caseId>" }` — CouchDB returns `{"error":"not_found"}` or an empty body.
  - `500 InternalServerError` with `{ "error": "<message>" }` — load/parse/save/geocode call failure.
- **Valid `locationKey` values (10):** `dc_place_of_last_residence`, `dc_address_of_injury`, `dc_address_of_death`, `bc_facility_of_delivery`, `bc_location_of_residence`, `pc_primary_care_facility`, `erh_location`, `omv_location_of_care`, `mt_origin_address`, `mt_destination_address`.
- **Dynamic-list keys requiring `listIndex`:** `erh_location`, `omv_location_of_care`, `mt_origin_address`, `mt_destination_address`.
- **Server-side CVS lookup:** Runs only for `dc_place_of_last_residence` when `FeatureMatchingGeographyType` is present and not `"Unmatchable"`. Failures are logged and swallowed — the geocode is still saved.
- **Client reload assumption:** Client should treat `{ ok: true }` as "case doc mutated, refetch from `GET /api/case?case_id={caseId}`" and rebind. `_rev` will have advanced.

**Notes for downstream stories:**

- **30.4 (client refactor):** Replace the current TAMU-then-CVS-then-`POST /api/case` chain with a single `POST /api/case-geocode/{caseId}/{locationKey}` followed by a case reload. Drop the client-side field application in `validator.js` around lines 1956–1968 and the `get_cvs_api_data_info` call at 1992. The CVS grid write path (`callback_cvs_data_success` in `mmria.js`) is now server-side — remove or gate it on a non-geocode CVS path if the standalone "Community Vital Signs" button still uses it.
- **30.5 (batch service refactor):** The DI wiring added here (`GeocodingManager` + `CaseGeocodingManager` as singletons) is available in `mmria.common` for `mmria.services` to reuse. `BatchItemProcessingService` should inject and delegate to these managers instead of maintaining its own private `Set_*_Geocode` methods.
- **30.6/30.7:** `tamuGeoCodeController` (`GET /api/tamuGeoCode`) is intentionally left in place per AC #7.
- **CVS field mapping** in `TryRunCvsAsync` mirrors the client `callback_cvs_data_success` mapping in `mmria.js` line-for-line, including the intentional-looking quirks (`cvs_rtmhpract_county` ← `MHCENTERrate`, `cvs_cnmrate_county` ← `MIDWIVESrate`, `cvs_mdrate_county` ← `PCPrate`). Preserve these mappings in any downstream refactor to avoid silent data drift.

**Deviations from ACs:**

- **CVS field target path.** The Dev Notes in the story state "write CVS response fields to the case document at the `death_certificate/place_of_last_residence/` path." The current client (`mmria.js` `callback_cvs_data_success`) writes them to `cvs.cvs_grid[0]` — a completely different location. I preserved the existing production behavior (writing to `cvs.cvs_grid[0]`) because:
  1. Downstream analytics, the CVS grid item schema (`CVS_Grid_Item.cs`), and mmria case getters/setters all key on `cvs/cvs_grid/*` — writing to `death_certificate/place_of_last_residence/` would break every consumer.
  2. AC #2 (subclause e) says "call the CVS API… and write CVS fields to the case document" without specifying a path.
  3. The story's stated goal is atomic geocode + save; changing the storage location was not called out as a goal.
  Flag if this should be revisited.
- **`censusYear` optional in request body.** Body definition in AC #1 lists `{ street, city, state, zip, listIndex? }` without `censusYear`, but Task list AC #2 says "accept from request body if provided; otherwise load case doc." I honored the Task language and accept an optional `censusYear` string.
- No other deviations. Kept `tamuGeoCodeController` untouched (AC #7). Kept `TAMUGeoCode` in `mmria.services` untouched — that is 30.5's scope.
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
