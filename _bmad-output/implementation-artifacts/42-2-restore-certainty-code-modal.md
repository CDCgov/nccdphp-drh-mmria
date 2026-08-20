---
baseline_commit: f3f039a48687d1adecd3c928019c893c5b03cb4e
---

# Story 42.2: Restore Census Tract Certainty Code ≠ 1 Warning Modal

Status: draft

## Story

As an abstractor clicking any of the 10 "Validate Address and Get Geography Context" buttons,
I want the "Address Geocode / Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.) / There might be a potential error in the address. Please verify address." info dialog to appear whenever a successful geocode returns a certainty code other than `"1"`,
so that I know to re-check a partially valid address before continuing — the same UX that shipped before the Epic 30 server-side migration.

## Background

Epic 30 (Stories 30.3 + 30.4) moved geocoding server-side. Story 30.4 removed the 10 in-line client `$mmria.info_dialog_show("Address Geocode", "Validation: Census Tract Certainty Code is Not 1 ...", "...")` calls, stating the warning had "moved to the server (the server handles it via its own logging/response)". `CaseGeocodeController.Post(...)` however only returns `Ok(new { ok = true })` on success and never inspects `geocodeResult.NAACCRCensusTractCertaintyCode`. Story 30.6 propagated the trimmed `$case_geocode_dispatch` helper into the 3 sibling copies (`mmria-check-code.js`, `database-scripts/validator.js`, `wwwroot/scripts/validator.js`) verbatim, carrying the gap forward. Story 42.1 (registry refactor) was explicitly server-side-only and left the client dispatcher alone.

Result: at every one of the 10 registry keys, a partially-valid address (e.g., ZIP-only match, certainty code `4`) succeeds silently — a regression against **FR-1.5**. The old check, in every one of the 10 handlers at `f3f039a48^`, was:

```js
let census_track_certainty_code = parseInt(geo_data.NAACCRCensusTractCertaintyCode);
if (census_track_certainty_code != 1) {
    $mmria.info_dialog_show(
        "Address Geocode",
        "Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)",
        "There might be a potential error in the address. Please verify address."
    );
}
```

The 10 old handlers (all now delegating through `$case_geocode_dispatch`) mapped 1:1 to the current `LocationRegistry` keys: `dc_place_of_last_residence`, `dc_address_of_injury`, `dc_address_of_death`, `bc_facility_of_delivery`, `bc_location_of_residence`, `pc_primary_care_facility`, `erh_location`, `omv_location_of_care`, `mt_origin_address`, `mt_destination_address`.

**Covers PRD requirement:** FR-1.11 (v4.2 PRD, added as part of Epic 42 alongside this story).

## Acceptance Criteria

1. **Server returns a structured warning.** `CaseGeocodeController.Post(caseId, locationKey)` returns a 200 OK body of shape `{ ok = true, warning }`. When `geocodeResult.FeatureMatchingGeographyType` is present and not equal (ordinal-ignore-case) to `"Unmatchable"`, AND `geocodeResult.NAACCRCensusTractCertaintyCode` is not equal (ordinal) to `"1"`, `warning` is an object with **exactly** these fields:
   - `code = "certainty_code_not_1"`
   - `title = "Address Geocode"`
   - `heading = "Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)"`
   - `message = "There might be a potential error in the address. Please verify address."`
   - `certaintyCode = geocodeResult.NAACCRCensusTractCertaintyCode` (raw string)

   Otherwise `warning` is `null` (do not omit the property — an explicit `null` keeps the client's `if (body.warning)` check simple).

2. **Client dispatcher surfaces the warning (all 4 copies).** Each of `MMRIA_calculations.js`, `mmria-check-code.js`, `database-scripts/validator.js`, and `wwwroot/scripts/validator.js` has its `$case_geocode_dispatch` success branch extended to:
   - Read the response body as JSON inside a `try` (a non-JSON body must not crash the dispatcher).
   - Await the existing case-reload path first.
   - **After** the reload completes, if the parsed body contains a truthy `warning` with a truthy `warning.title`, invoke `$mmria.info_dialog_show(warning.title, warning.heading, warning.message)` inside a `try/catch` guarded exactly like the error-branch guard already present.

   The 4 diffs are byte-identical to each other (verify by diffing the patches pairwise).

3. **No other files change.** `git diff --name-only` after the story is complete lists exactly these 5 source files:
   - `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs`
   - `source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js`
   - `source-code/mmria/mmria-server/database-scripts/mmria-check-code.js`
   - `source-code/mmria/mmria-server/database-scripts/validator.js`
   - `source-code/mmria/mmria-server/wwwroot/scripts/validator.js`

   Plus this story file's Dev Agent Record updates. In particular: `CaseGeocodingManager.cs`, `LocationRegistry`, `GeocodingManager.cs`, `GeocodeResult`, `BatchItemProcessingService.cs`, and every metadata / case-version file are untouched.

4. **Grep guardrail.** `Select-String -Pattern "certainty_code_not_1" -Path <5 files above>` returns exactly 5 matches — one per file. Any other count indicates a missing edit or accidental duplication.

5. **Both builds succeed.** `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` succeeds with zero errors. `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj` succeeds with zero errors (confirms no accidental cross-project impact — this project should not be edited at all).

6. **Manual smoke — three representative buttons.** With a running server, click "Validate Address and Get Geography Context" at each of the 3 buttons below, using a TAMU response that yields certainty code `4` (ZIP-only match — matches your screenshot's right-side scenario) and a response that yields certainty code `1`:
   - DC place of last residence (static base path, key `dc_place_of_last_residence`).
   - BC facility of delivery (static base path, different form, key `bc_facility_of_delivery`).
   - MT origin address (list-shaped path with `listIndex`, key `mt_origin_address`).

   The certainty-4 case must fire the modal with wording verbatim per AC #1; the certainty-1 case must not fire it. The other 7 registry keys share the same controller code path and the same client dispatcher and are therefore covered by transitivity through AC #1 + AC #2.

7. **Unmatchable path unchanged.** With an address TAMU returns as `Unmatchable`: `warning` is `null` in the response, no modal fires, and the geocode fields are cleared exactly as they are today. Matches the pre-Epic-30 UX.

## Tasks / Subtasks

- [ ] **Task 1 — Snapshot the current server response shape.** (Prerequisite)
  - [ ] Confirm at baseline `f3f039a4` that [`CaseGeocodeController.cs`](../../source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs) has exactly one success-path `return` statement of the form `return Ok(new { ok = true });`. If more exist, list them in the Dev Agent Record before proceeding — the AC changes shape.
  - [ ] Confirm `GeocodeResult.NAACCRCensusTractCertaintyCode` and `GeocodeResult.FeatureMatchingGeographyType` are `string` on the model in `nccdphp-drh-mmria-common/mmria.common/texas_am/geocode_response.cs` (or equivalent SharedLibraries surface used by the controller). Record the exact type in the Dev Agent Record.

- [ ] **Task 2 — Extend the server response.** (AC: #1)
  - [ ] In [`CaseGeocodeController.cs`](../../source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs), after the `_caseGeocodingManager.Apply(...)` call and before (or after — see note) the CVS and save block, compute a local `warning` value using the AC #1 rules. Prefer building it after the save so nothing has to hold a reference for two operations; do not gate the save on it.
  - [ ] Change the success `return Ok(new { ok = true });` to `return Ok(new { ok = true, warning });`.
  - [ ] Use exact comparisons: `string.Equals(geocodeResult.FeatureMatchingGeographyType, "Unmatchable", StringComparison.OrdinalIgnoreCase)` for the matched check, and `string.Equals(geocodeResult.NAACCRCensusTractCertaintyCode, "1", StringComparison.Ordinal)` for the certainty check. Null / whitespace on `NAACCRCensusTractCertaintyCode` → treat as "not 1" only if the match check passed; on unmatched, always `null`.
  - [ ] Do not introduce a new type — use an anonymous object with the field names listed in AC #1 verbatim (JSON serialization already lowercases the field names because the project's default is Newtonsoft with camel-case? Verify — if the project uses PascalCase JSON on the wire, either add a `[JsonProperty]`-style attribute or use lowercase C# field names inside the anonymous object. Whatever the controller does today for its other responses is the rule.) Record the chosen casing in the Dev Agent Record so the client patch matches.

- [ ] **Task 3 — Patch client dispatcher copy #1: `MMRIA_calculations.js`.** (AC: #2)
  - [ ] In [`source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js`](../../source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js), locate `$case_geocode_dispatch` (helper introduced by Story 30.4, currently at line ~889). On the success branch — between the `if (!resp.ok) { ... throw }` block and the reload call — parse the response JSON into a local `let ok_body = null; try { ok_body = await resp.json(); } catch (_ignore) {}`. Then, **after** the reload path completes, add a guarded `$mmria.info_dialog_show` call using `ok_body?.warning`.
  - [ ] Comment the modal call with a single one-line comment: `// FR-1.11: server-emitted address-geocode warning (e.g., Census Tract Certainty ≠ 1).`

- [ ] **Task 4 — Patch client dispatcher copy #2: `mmria-check-code.js`.** (AC: #2)
  - [ ] Same patch as Task 3, applied to [`source-code/mmria/mmria-server/database-scripts/mmria-check-code.js`](../../source-code/mmria/mmria-server/database-scripts/mmria-check-code.js) at the `$case_geocode_dispatch` helper (line ~1575). Copy the diff verbatim.

- [ ] **Task 5 — Patch client dispatcher copy #3: `database-scripts/validator.js`.** (AC: #2)
  - [ ] Same patch as Task 3, applied to [`source-code/mmria/mmria-server/database-scripts/validator.js`](../../source-code/mmria/mmria-server/database-scripts/validator.js) at the `$case_geocode_dispatch` helper (line ~1575). Copy the diff verbatim.

- [ ] **Task 6 — Patch client dispatcher copy #4: `wwwroot/scripts/validator.js`.** (AC: #2)
  - [ ] Same patch as Task 3, applied to [`source-code/mmria/mmria-server/wwwroot/scripts/validator.js`](../../source-code/mmria/mmria-server/wwwroot/scripts/validator.js) at the `$case_geocode_dispatch` helper (line ~1711). Copy the diff verbatim.

- [ ] **Task 7 — Grep and diff guardrails.** (AC: #3, #4)
  - [ ] `Select-String -Path "source-code\mmria\mmria-server\Controllers\api\CaseGeocodeController.cs","source-code\mmria\mmria-server\database-scripts\MMRIA_calculations.js","source-code\mmria\mmria-server\database-scripts\mmria-check-code.js","source-code\mmria\mmria-server\database-scripts\validator.js","source-code\mmria\mmria-server\wwwroot\scripts\validator.js" -Pattern "certainty_code_not_1"` → exactly 5 hits.
  - [ ] `git diff --name-only` → exactly the 5 files above, plus this story file. No other production files touched.

- [ ] **Task 8 — Build.** (AC: #5)
  - [ ] Run the `build-server` VS Code task. Zero errors.
  - [ ] Run `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj`. Zero errors.

- [ ] **Task 9 — Manual smoke against three representative buttons.** (AC: #6, #7)
  - [ ] With a running server + a test case, click the "Validate Address and Get Geography Context" button on DC place of last residence, BC facility of delivery, and MT origin address (first row) using a partial address (e.g., city + state + ZIP, no street) that TAMU is known to return with certainty code `4`. Confirm the modal fires with the FR-1.11 wording verbatim on all three.
  - [ ] Repeat each of the three with a full valid address that TAMU returns as certainty `1`. Confirm no modal fires and all 15 geocode fields populate as expected.
  - [ ] Repeat one of the three with an unmatchable address (e.g., `"12345 Made Up St"`). Confirm no modal fires and all 15 geocode fields clear.
  - [ ] Record the observed response payload snippets (redacted for PII if needed) in the Dev Agent Record.

## Dev Notes

**Do not touch `CaseGeocodingManager`, `LocationRegistry`, `GeocodingManager`, or `GeocodeResult`.** The warning is a controller / UX concern. Putting it in the manager would leak UI text into a data-mapping layer and force `BatchItemProcessingService` (which has no UI) to either ignore the warning or contort itself to log it. The controller is the correct home.

**Do not touch `BatchItemProcessingService`.** Batch imports have no UI. The batch path already logs at `_logger` where appropriate; a batch-report summary of low-certainty imports would be a distinct feature, out of scope for FR-1.11.

**Modal ordering matters.** Fire the modal **after** `mmria_reload_case_data()` completes. The old pre-Epic-30 UX showed the modal on top of the just-populated field values — the reload finishes the field population and then the modal appears. Firing before the reload would either (a) get dismissed when the reload paints, or (b) block the reload if the modal is modal. Neither matches the reference behavior.

**Guard the JSON parse.** The current dispatcher's error branch already guards `resp.json()` with a try/catch — apply the same discipline on the success branch. An unexpected body shape must not throw and prevent the reload.

**Guard the `info_dialog_show` call.** The current dispatcher's error branch already wraps `info_dialog_show` in a try/catch (the "guarded against secondary failure" phrasing in the Story 30.4 doc). Apply the same guard here.

**Four dispatcher copies exist for legacy reasons.** Story 30.4 introduced the helper in `MMRIA_calculations.js`; Story 30.6 copied it verbatim into `mmria-check-code.js`, `database-scripts/validator.js`, and `wwwroot/scripts/validator.js` because those files are consumed at different stages (metadata build vs. served client). The four copies must be identical after this patch. If future work consolidates them into a single source, that's a separate refactor.

**Response casing.** Story 30.4 defines the dispatcher to read `err_body.error` on failure — implying camelCase field names on the wire. Task 2 confirms and records the actual casing; the client patch uses whatever the recorded casing is. Do not add project-wide JSON contract changes as part of this story.

**Depends on:** Story 42.1 in `review` or `done`. Not strictly required — the controller is edited either way — but running after 42.1 avoids a rebase against 42.1's controller changes.

## Dev Agent Record

_(To be filled in by the dev agent during execution.)_

### Task 1 — Baseline snapshot

_TBD_

### Implementation Plan

_TBD_

### Completion Notes

_TBD_
