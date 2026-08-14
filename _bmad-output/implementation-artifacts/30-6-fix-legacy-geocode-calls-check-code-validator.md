# Story 30.6: Fix Legacy Geocode Calls in mmria-check-code.js and validator.js

Status: ready-for-dev

## Story

As a developer,
I want the legacy geocode functions in `mmria-check-code.js` and `validator.js` to use the new server endpoint with `census_year`,
so that census tract results are not stale and geocoding logic is consistent across all call sites.

## Acceptance Criteria

1. All 8 geocode call sites in `mmria-check-code.js` and all 8 in `validator.js` use `POST /api/case-geocode/{id}/{locationKey}` instead of `$mmria.get_geocode_info(street, city, state, zip, callback)`.
2. `census_year` from `g_data.home_record.date_of_death.year` is included in every POST body.
3. No 4-argument `get_geocode_info` calls remain in either file. A PowerShell grep confirms zero.
4. The function names (`x2f_ocl`, `x6b_ocl`, etc.) are not changed — they are event handler names bound by the metadata.
5. Case reloads in edit mode after a successful geocode (same as Story 30.4 pattern).

## Tasks / Subtasks

- [ ] Audit all call sites in `mmria-check-code.js` (AC: #1, #2)
  - [ ] Run: `Select-String -Path "wwwroot\scripts\mmria-check-code.js" -Pattern "get_geocode_info"` — identify all 8 call sites
  - [ ] Map each call site to its `locationKey` (examine surrounding context to determine which address form it geocodes)
  - [ ] Replace each with `POST /api/case-geocode/{g_data._id}/{locationKey}` with the address fields and `censusYear: g_data.home_record.date_of_death.year`
  - [ ] Add busy modal + reload pattern (same as Story 30.4)
- [ ] Audit all call sites in `validator.js` (AC: #1, #2)
  - [ ] Run: `Select-String -Path "wwwroot\scripts\validator.js" -Pattern "get_geocode_info"` — identify all 8 call sites
  - [ ] Same mapping and replacement process
- [ ] Verify no 4-argument calls remain (AC: #3)
  - [ ] `Select-String -Path "wwwroot\scripts\mmria-check-code.js","wwwroot\scripts\validator.js" -Pattern "get_geocode_info"` → zero results
- [ ] Build and smoke test (AC: #5)
  - [ ] Trigger a geocode from a `mmria-check-code.js` handler — verify case reloads correctly

## Dev Notes

**Files to modify:**
- `source-code/mmria/mmria-server/wwwroot/scripts/mmria-check-code.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/validator.js`

**Location key mapping** — when replacing each call site, read the surrounding function context to identify which address section is being geocoded (e.g., if it reads `g_data.death_certificate.address_of_injury.*`, the locationKey is `"dc_address_of_injury"`). The 10 valid keys are the same set as Story 30.3.

**Census year** — the old 4-argument signature `get_geocode_info(street, city, state, zip, callback)` lacked `censusYear`. Add `censusYear: g_data.home_record.date_of_death.year` to every new POST body.

**Handler names** — `x2f_ocl`, `x6b_ocl`, etc. are event handler names registered in `home_record.json` metadata. Do NOT rename them.

**Busy modal + reload** — reuse the exact same pattern established in Story 30.4. No new modal pattern.

**`get_geocode_info` in `mmria.js`** — do NOT remove. It's still referenced by `mmria.committee_member.js` (removed in Story 30.7).

**Depends on:** Story 30.3. Can run in parallel with Story 30.4.
