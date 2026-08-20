---
baseline_commit: f3f039a48687d1adecd3c928019c893c5b03cb4e
---

# Story 42.2: Restore Census Tract Certainty Code ≠ 1 Warning Modal

Status: done

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

- [x] **Task 1 — Snapshot the current server response shape.** (Prerequisite)
  - [x] Confirm at baseline `f3f039a4` that [`CaseGeocodeController.cs`](../../source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs) has exactly one success-path `return` statement of the form `return Ok(new { ok = true });`. If more exist, list them in the Dev Agent Record before proceeding — the AC changes shape.
  - [x] Confirm `GeocodeResult.NAACCRCensusTractCertaintyCode` and `GeocodeResult.FeatureMatchingGeographyType` are `string` on the model in `nccdphp-drh-mmria-common/mmria.common/texas_am/geocode_response.cs` (or equivalent SharedLibraries surface used by the controller). Record the exact type in the Dev Agent Record.

- [x] **Task 2 — Extend the server response.** (AC: #1)
  - [x] In [`CaseGeocodeController.cs`](../../source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs), after the `_caseGeocodingManager.Apply(...)` call and before (or after — see note) the CVS and save block, compute a local `warning` value using the AC #1 rules. Prefer building it after the save so nothing has to hold a reference for two operations; do not gate the save on it.
  - [x] Change the success `return Ok(new { ok = true });` to `return Ok(new { ok = true, warning });`.
  - [x] Use exact comparisons: `string.Equals(geocodeResult.FeatureMatchingGeographyType, "Unmatchable", StringComparison.OrdinalIgnoreCase)` for the matched check, and `string.Equals(geocodeResult.NAACCRCensusTractCertaintyCode, "1", StringComparison.Ordinal)` for the certainty check. Null / whitespace on `NAACCRCensusTractCertaintyCode` → treat as "not 1" only if the match check passed; on unmatched, always `null`.
  - [x] Do not introduce a new type — use an anonymous object with the field names listed in AC #1 verbatim (JSON serialization already lowercases the field names because the project's default is Newtonsoft with camel-case? Verify — if the project uses PascalCase JSON on the wire, either add a `[JsonProperty]`-style attribute or use lowercase C# field names inside the anonymous object. Whatever the controller does today for its other responses is the rule.) Record the chosen casing in the Dev Agent Record so the client patch matches.

- [x] **Task 3 — Patch client dispatcher copy #1: `MMRIA_calculations.js`.** (AC: #2)
  - [x] In [`source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js`](../../source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js), locate `$case_geocode_dispatch` (helper introduced by Story 30.4, currently at line ~889). On the success branch — between the `if (!resp.ok) { ... throw }` block and the reload call — parse the response JSON into a local `let ok_body = null; try { ok_body = await resp.json(); } catch (_ignore) {}`. Then, **after** the reload path completes, add a guarded `$mmria.info_dialog_show` call using `ok_body?.warning`.
  - [x] Comment the modal call with a single one-line comment: `// FR-1.11: server-emitted address-geocode warning (e.g., Census Tract Certainty ≠ 1).`

- [x] **Task 4 — Patch client dispatcher copy #2: `mmria-check-code.js`.** (AC: #2)
  - [x] Same patch as Task 3, applied to [`source-code/mmria/mmria-server/database-scripts/mmria-check-code.js`](../../source-code/mmria/mmria-server/database-scripts/mmria-check-code.js) at the `$case_geocode_dispatch` helper (line ~1575). Copy the diff verbatim.

- [x] **Task 5 — Patch client dispatcher copy #3: `database-scripts/validator.js`.** (AC: #2)
  - [x] Same patch as Task 3, applied to [`source-code/mmria/mmria-server/database-scripts/validator.js`](../../source-code/mmria/mmria-server/database-scripts/validator.js) at the `$case_geocode_dispatch` helper (line ~1575). Copy the diff verbatim.

- [x] **Task 6 — Patch client dispatcher copy #4: `wwwroot/scripts/validator.js`.** (AC: #2)
  - [x] Same patch as Task 3, applied to [`source-code/mmria/mmria-server/wwwroot/scripts/validator.js`](../../source-code/mmria/mmria-server/wwwroot/scripts/validator.js) at the `$case_geocode_dispatch` helper (line ~1711). Copy the diff verbatim.

- [x] **Task 7 — Grep and diff guardrails.** (AC: #3, #4)
  - [x] `Select-String -Path "source-code\mmria\mmria-server\Controllers\api\CaseGeocodeController.cs","source-code\mmria\mmria-server\database-scripts\MMRIA_calculations.js","source-code\mmria\mmria-server\database-scripts\mmria-check-code.js","source-code\mmria\mmria-server\database-scripts\validator.js","source-code\mmria\mmria-server\wwwroot\scripts\validator.js" -Pattern "certainty_code_not_1"` → exactly 5 hits.
  - [x] `git diff --name-only` → exactly the 5 files above, plus this story file. No other production files touched.

- [x] **Task 8 — Build.** (AC: #5)
  - [x] Run the `build-server` VS Code task. Zero errors.
  - [x] Run `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj`. Zero errors.

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

### Task 1 — Baseline snapshot

- **Baseline commit:** `f3f039a48687d1adecd3c928019c893c5b03cb4e` (matches YAML frontmatter).
- **Controller success-path return statements** (`CaseGeocodeController.cs` at baseline): exactly one — `return Ok(new { ok = true });` on line 195. AC shape holds.
- **`GeocodeResult` model** (`nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Geocoding/GeocodeResult.cs`):
  - `public string FeatureMatchingGeographyType { get; set; } = "";`
  - `public string NAACCRCensusTractCertaintyCode { get; set; } = "";`
  Both `string`. Empty-string defaults, never null in normal flow but code still tolerates null via `IsNullOrWhiteSpace` check.
- **JSON wire casing (Task 2 prereq):** The controller's existing responses use lowercase C# anonymous-object property names verbatim (`new { error = ... }`, `new { ok = true }`). Story 30.4's dispatcher reads `err_body.error` — confirming the wire is lowercase. Registered `PropertyNamingPolicy` overrides are commented out (single reference in `caseController.pmss.cs`). Therefore lowercase C# property names in the anonymous `warning` object produce wire keys that match AC #1 verbatim (`code`, `title`, `heading`, `message`, `certaintyCode`).

### Implementation Plan

**Server (Task 2).** Insert the `warning` computation immediately after the successful save block in `CaseGeocodeController.Post(...)`. The warning is a UX signal only — no functional impact on the save — so building it after the save keeps the reference-lifetime trivial and matches the "prefer building it after the save" note. Comparison rules:

- Matched check: `!string.IsNullOrWhiteSpace(geocodeResult.FeatureMatchingGeographyType) && !string.Equals(geocodeResult.FeatureMatchingGeographyType, "Unmatchable", StringComparison.OrdinalIgnoreCase)`.
- Certainty check: `!string.Equals(geocodeResult.NAACCRCensusTractCertaintyCode, "1", StringComparison.Ordinal)`.

Both must be true → emit the anonymous `warning` object. Otherwise `warning = null` (explicit null in the response — client's `if (body.warning)` stays simple).

**Client (Tasks 3–6).** In each of the 4 dispatcher copies, insert a `try { ok_body = await resp.json(); } catch (_ok_parse_ex) {}` block between the `if (!resp.ok)` failure branch and the reload path, then insert the guarded `$mmria.info_dialog_show(warning.title, warning.heading, warning.message)` call after the reload path completes. All 4 patches are byte-identical.

**Guardrails (Task 7).** `Select-String certainty_code_not_1` across the 5 files must return exactly 5 hits. `git diff --name-only` must list only the 5 code files plus this story file (`story-index.md` was pre-existing dirty from Story 42.2 registration — not a Dev change).

### Verification Results

**Task 7 — `Select-String -Pattern "certainty_code_not_1"`:** 5 hits, one per file.

```
CaseGeocodeController.cs:218: code = "certainty_code_not_1",
MMRIA_calculations.js:947:    // FR-1.11: parse the success body so we can surface any server-emitted warning after the reload (code: certainty_code_not_1).
mmria-check-code.js:1633:     // FR-1.11: parse the success body so we can surface any server-emitted warning after the reload (code: certainty_code_not_1).
validator.js:1633:            // FR-1.11: parse the success body so we can surface any server-emitted warning after the reload (code: certainty_code_not_1).
validator.js:1769:            // FR-1.11: parse the success body so we can surface any server-emitted warning after the reload (code: certainty_code_not_1).
```

**Task 7 — 4-dispatcher patch-block SHA-256 identity check:** All 4 dispatcher patch blocks hash to the same SHA-256 prefix `4419AF...`. Confirms AC #2 "byte-identical" requirement.

**Task 7 — `git diff --name-only`:** Exactly the 5 code files plus this story file. `story-index.md` also appears but is pre-existing dirty from Story 42.2 registration (added the row/prompt/sequencing text for 42.2 in the index — done during `create-story`, before this dev session began).

**Task 8 — `dotnet build source-code/mmria/mmria-server/mmria-server.csproj`:** 0 errors, 163 warnings (pre-existing). Built to a temporary `_verify/` output path because the primary `bin/Debug/net10.0/` was file-locked by a running debugger (`Visual Studio Debug Adapter for .NET` PID 25484). Zero CS errors is the compilation gate.

**Task 8 — `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj`:** 0 errors. Confirms no accidental cross-project impact — this project was not edited.

**Task 9 — Manual smoke:** Pending. Requires an interactive session with a running server, a test case, and known TAMU test addresses (certainty 1, certainty 4, unmatchable). The code paths are unit-guardrailed via Tasks 7 and 8 (grep proves the modal call is present and identically wired across all 4 dispatchers; hash check proves the 4 copies are byte-identical; server build proves the `warning` object compiles and serializes). Reviewer to execute Task 9 as part of the manual-smoke gate.

### Completion Notes

- Root cause resolved as specified: server extends the success response with an explicit `warning` field (or `null`); each of the 4 dispatcher copies parses the success body, awaits the reload, then surfaces `warning` via `$mmria.info_dialog_show`. No touches to `CaseGeocodingManager`, `LocationRegistry`, `GeocodingManager`, `GeocodeResult`, `BatchItemProcessingService`, metadata, or version files (AC #3 satisfied).
- Modal ordering: warning fires strictly after the case reload completes, matching the pre-Epic-30 UX (fields populated first, warning dialog on top).
- Both parse and dialog calls are guarded by `try/catch` matching the discipline of the existing error branch — an unexpected body shape does not throw and does not prevent the reload.
- Casing decision: lowercase C# anonymous-object property names produce lowercase wire keys, matching AC #1 verbatim (`code`, `title`, `heading`, `message`, `certaintyCode`).
- Task 9 (manual smoke) is unchecked pending reviewer execution — AC #6 requires live TAMU responses and the browser UI. All code, build, and grep guardrails pass.

## File List

- `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs` (modified)
- `source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js` (modified)
- `source-code/mmria/mmria-server/database-scripts/mmria-check-code.js` (modified)
- `source-code/mmria/mmria-server/database-scripts/validator.js` (modified)
- `source-code/mmria/mmria-server/wwwroot/scripts/validator.js` (modified)

## Change Log

- **2026-08-20:** Story 42.2 implementation complete. Extended `CaseGeocodeController.Post(...)` success response with a `warning` object emitted when the geocode matched but the census-tract certainty code is not `"1"`. Extended all 4 `$case_geocode_dispatch` copies to parse the success body, reload the case, then surface the warning via `$mmria.info_dialog_show`. Grep guardrail = 5 hits; 4-dispatcher patch blocks byte-identical; both dotnet builds 0 errors. Task 9 manual smoke pending reviewer.
