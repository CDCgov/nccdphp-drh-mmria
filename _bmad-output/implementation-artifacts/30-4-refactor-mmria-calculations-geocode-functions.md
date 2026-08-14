# Story 30.4: Refactor MMRIA_calculations.js Geocode Button Handlers

Status: ready-for-dev

## Story

As an abstractor,
when I click a "Validate Address and Get Geography Context" button,
I want the geocoding to happen server-side with a busy modal preventing edits while it runs,
so that the operation is atomic and no urban-status logic lives in the browser.

## Acceptance Criteria

1. Each of the 10 geocode handler functions POSTs to `POST /api/case-geocode/{g_data._id}/{locationKey}` with address fields and optional `listIndex`. No function calls `GET /api/tamuGeoCode` directly.
2. Client flow for every button:
   a. Save the current case (`$mmria.save_current_record()`)
   b. Show the existing busy/processing modal (reuse existing site modal pattern — non-dismissible while request is in-flight)
   c. POST to the geocode endpoint
   d. On success (`{ ok: true }`): dismiss modal, reload the case in edit mode (trigger the existing case reload path)
   e. On failure (network error, non-2xx): dismiss modal, show error dialog via `$mmria.info_dialog_show`
3. The "Census Tract Certainty Code ≠ 1" info dialog is **not shown** by the client — it has moved to the server (the server handles it via its own logging/response; AC-2e covers server-returned errors).
4. For dynamic-list locations, `$global.get_current_multiform_index()` provides the `listIndex` in the POST body.
5. The `get_geocode_info` function in `mmria.js` is **not removed** — it is still used by Layers B and C until Stories 30.6 and 30.7.
6. The ~100-line urban-status calculation and field-setting blocks are removed from all 10 handler functions.
7. `census_year` is included in the POST body from `g_data.home_record.date_of_death.year`.

## Tasks / Subtasks

- [ ] Refactor all 10 geocode functions in `MMRIA_calculations.js` (AC: #1, #2, #4, #6, #7)
  - [ ] `geocode_dc_last_res` — locationKey: `"dc_place_of_last_residence"` (static)
  - [ ] `geocode_dc_injury_place` — locationKey: `"dc_address_of_injury"` (static)
  - [ ] `geocode_dc_death_place` — locationKey: `"dc_address_of_death"` (static)
  - [ ] `geocode_bc_delivery_place` — locationKey: `"bc_facility_of_delivery"` (static)
  - [ ] `geocode_bc_residence` — locationKey: `"bc_location_of_residence"` (static)
  - [ ] `geocode_pc_primary_care_location` — locationKey: `"pc_primary_care_facility"` (static)
  - [ ] `geocode_erh_location` — locationKey: `"erh_location"` (dynamic — include `listIndex`)
  - [ ] `geocode_omov_location` — locationKey: `"omv_location_of_care"` (dynamic — include `listIndex`)
  - [ ] `medical_transport_origin_information_address_get_coordinates` — locationKey: `"mt_origin_address"` (dynamic)
  - [ ] `medical_transport_destination_information_address_get_coordinates` — locationKey: `"mt_destination_address"` (dynamic)
  - [ ] For each: replace the entire body with the new flow (save → busy modal → POST → reload / error)
  - [ ] Remove urban-status calculation blocks (lines ~920–985 per function)
  - [ ] Remove `$mmria.set_control_value(...)` field-setting blocks
- [ ] Implement case reload on success (AC: #2d)
  - [ ] Trigger the existing case reload path that keeps the case in edit mode — confirm the exact reload mechanism used elsewhere in `case/index.js` (e.g., re-invoking the case load with `g_data._id`) and use the same pattern
- [ ] Busy modal (AC: #2b)
  - [ ] Show on POST start — reuse the existing overlay/spinner modal pattern used in other blocking operations
  - [ ] Dismiss on POST complete (success or failure)
- [ ] Build and smoke test (AC: #1–#7)
  - [ ] Click each of the 10 geocode buttons in the local environment
  - [ ] Verify network tab shows `POST /api/case-geocode/...` (not `GET /api/tamuGeoCode`)
  - [ ] Verify case reloads in edit mode after successful geocode
  - [ ] Verify busy modal appears and dismisses correctly
  - [ ] Verify error dialog appears on endpoint failure

## Dev Notes

**File to modify:** `source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js`

**New handler body pattern:**
```javascript
async function geocode_dc_last_res(p_control) {
    const street = this.street;
    const city = this.city;
    const state = this.state;
    const zip = this.zip_code;
    const censusYear = g_data.home_record.date_of_death.year;

    $mmria.save_current_record();            // save current edits first
    mmria_show_busy_modal();                 // show overlay (reuse existing pattern)

    try {
        const resp = await fetch(`/api/case-geocode/${g_data._id}/dc_place_of_last_residence`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ street, city, state, zip, censusYear })
        });
        if (!resp.ok) throw new Error(`Geocode failed: ${resp.status}`);
        // reload case in edit mode
        await mmria_reload_case_in_edit_mode(g_data._id);  // confirm exact function name
    } catch (err) {
        $mmria.info_dialog_show("Address Geocode", "Geocode failed.", err.message);
    } finally {
        mmria_hide_busy_modal();
    }
}
```

**Dynamic-list pattern** — add `listIndex: $global.get_current_multiform_index()` to POST body.

**Case reload** — search `case/index.js` for the existing reload-in-edit-mode pattern (used after certain admin actions). Use that exact mechanism, not a full page refresh.

**CVS note:** CVS is now server-side (Story 30.3). Remove the `$mmria.get_cvs_api_data_info(...)` call from `geocode_dc_last_res` — the server handles it.

**Census Tract Certainty Code dialog** — remove from client. Server-side logging replaces it.

**Depends on:** Story 30.3.
