---
baseline_commit: 330d0773ce058eeff17b6115a964eadf04be9090
---

# Story 42.1: Convert CaseGeocodingManager to Declarative LocationRegistry

Status: done

## Story

As a developer maintaining or extending case-geocoding,
I want the location→field mapping to be a single declarative registry inside `CaseGeocodingManager` and one `Apply(caseDoc, locationKey, result, listIndex?)` method,
so that adding a new geocode-enabled location is a single-entry change and every caller (controller, batch service, future callers) is data-driven off the same source of truth.

## Background

Epic 30 shipped end-to-end unification of the geocode/apply/save path across the browser button click and the vital-import batch service — both callers hit the same `GeocodingManager` and `CaseGeocodingManager`. However, the shape of `CaseGeocodingManager` shipped as imperative code rather than the declarative registry that was the original design intent:

- 10 hand-written `Apply_DC_PlaceOfLastResidence_Geocode` / `Apply_DC_AddressOfInjury_Geocode` / etc. methods, each with the target path (e.g., `"death_certificate/place_of_last_residence"`) hard-coded as a string literal in the method body.
- `CaseGeocodeController` maintains a separate hand-maintained `_validKeys` HashSet (10 entries) and `_listKeys` HashSet (4 entries), plus a private `ApplyGeocode` helper with a switch/if-chain over `locationKey`.
- `BatchItemProcessingService` has 5 call sites (~lines 1162, 1203, 1588, 1904, 1919), each naming the specific method (e.g., `_caseGeocodingManager.Apply_DC_AddressOfDeath_Geocode(new_case, geo_result)`). The batch service therefore must know method identifiers, not just location keys, and cannot iterate a data-driven list of forms.

Adding a new form today requires four coordinated edits across three files: (1) new method in `CaseGeocodingManager`, (2) new entry in controller `_validKeys` (and `_listKeys` if list-shaped), (3) new branch in controller `ApplyGeocode` switch, (4) new client `locationKey`. This story reduces that to a single-line registry addition.

**Covers PRD requirement:** FR-1.10 (v4.2 PRD, added as part of Epic 42).

## Acceptance Criteria

1. **Registry exists.** `CaseGeocodingManager.LocationRegistry` is a `public static readonly IReadOnlyDictionary<string, GeocodeTarget>` with exactly 10 entries, and its keys are identical (as a set, with ordinal comparison) to the 10 keys previously listed in `CaseGeocodeController._validKeys` at HEAD before this story.
2. **Single `Apply` method.** `CaseGeocodingManager.Apply(ExpandoObject caseDoc, string locationKey, GeocodeResult result, int listIndex = 0)` exists and produces case-document mutations identical to the pre-refactor per-location methods for every entry in the registry.
3. **Per-location methods removed.** The 10 `Apply_*_Geocode` public methods on `CaseGeocodingManager` are deleted. No thin wrappers are left behind.
4. **Controller key lists are derived.** `CaseGeocodeController` no longer holds hand-maintained `_validKeys` or `_listKeys` HashSet literals; both are derived from `CaseGeocodingManager.LocationRegistry` (either as static computed properties or as fields initialized from the registry). A `grep` for `_validKeys = new` and `_listKeys = new` in `CaseGeocodeController.cs` returns zero HashSet-with-string-literals matches.
5. **Controller switch is gone.** The `ApplyGeocode` switch/if-chain in `CaseGeocodeController` is deleted; the single-line `_caseGeocodingManager.Apply(caseDoc, locationKey, geocodeResult, listIndex)` call replaces it.
6. **Batch service is data-driven.** `BatchItemProcessingService` has zero references to any `Apply_*_Geocode` method name (verified by grep). Every geocode-apply call is `_caseGeocodingManager.Apply(new_case, "<key>", geo_result)` with the location key as a string literal argument.
7. **Both builds succeed.** `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` succeeds with zero errors. `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj` succeeds with zero errors.
8. **Single-entry extension property, verified in writing.** The Dev Agent Record includes a written statement enumerating the exact code change required to add a hypothetical eleventh location (e.g., `"cc_something_new"` with base path `"custom_form/something_new"`). The answer must be a single line: a new `["cc_something_new"] = GeocodeTarget.Static("custom_form/something_new"),` entry in `LocationRegistry`. If the answer requires touching any other file, the design fails AC #8 and the story is not done.

## Tasks / Subtasks

- [x] **Task 1 — Snapshot the current mapping.** (Prerequisite)
  - [x] Read the 10 `Apply_*_Geocode` methods in [`CaseGeocodingManager.cs`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseGeocodingManager.cs) and record each `(locationKey, isList, basePath | listPath+subPath)` triple in a working notes block inside the Dev Agent Record. The 10 keys and target paths are the source of truth for the registry; do not derive them from anywhere else.

- [x] **Task 2 — Introduce `GeocodeTarget`.** (AC: #1, #2)
  - [x] In `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/`, add a `GeocodeTarget` type. Preferred shape: `public readonly record struct GeocodeTarget(bool IsList, string BasePath, string ListPath, string SubPath)` with two factory helpers `public static GeocodeTarget Static(string basePath)` and `public static GeocodeTarget List(string listPath, string subPath)`. Either put it in the same file as `CaseGeocodingManager` or in a sibling `GeocodeTarget.cs` — one file per developer preference, no separate concern.

- [x] **Task 3 — Build `LocationRegistry`.** (AC: #1)
  - [x] Add the static readonly `IReadOnlyDictionary<string, GeocodeTarget>` field to `CaseGeocodingManager`. Use `new Dictionary<string, GeocodeTarget>(StringComparer.Ordinal) { ... }` for the initializer. Populate all 10 entries from the Task 1 snapshot. Verify against the current `_validKeys` set in [`CaseGeocodeController.cs`](../../source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs) — the two must match exactly.

- [x] **Task 4 — Implement `Apply(caseDoc, locationKey, result, listIndex = 0)`.** (AC: #2)
  - [x] Add the public method. On unknown key, return silently (matches current controller behavior of "invalid key → 400 BadRequest before reaching manager"; the manager itself should be tolerant so it is safe to call from batch code without pre-validation). On known key, dispatch to `ApplyStatic` or `ApplyList` based on `target.IsList`. Preserve the existing private `ApplyStatic` and `ApplyList` helpers unchanged.

- [x] **Task 5 — Delete the 10 per-location methods.** (AC: #3)
  - [x] Delete `Apply_DC_PlaceOfLastResidence_Geocode`, `Apply_DC_AddressOfInjury_Geocode`, `Apply_DC_AddressOfDeath_Geocode`, `Apply_BC_FacilityOfDelivery_Geocode`, `Apply_BC_LocationOfResidence_Geocode`, `Apply_PC_PrimaryCareFacility_Geocode`, `Apply_ERH_Location_Geocode`, `Apply_OMV_LocationOfCare_Geocode`, `Apply_MT_OriginAddress_Geocode`, `Apply_MT_DestinationAddress_Geocode`. No wrappers.

- [x] **Task 6 — Migrate `CaseGeocodeController`.** (AC: #4, #5)
  - [x] Replace the `_validKeys` HashSet literal with a computed value derived from `CaseGeocodingManager.LocationRegistry.Keys` (e.g., `private static readonly HashSet<string> _validKeys = new(CaseGeocodingManager.LocationRegistry.Keys, StringComparer.Ordinal);`).
  - [x] Replace `_listKeys` with `new(CaseGeocodingManager.LocationRegistry.Where(kv => kv.Value.IsList).Select(kv => kv.Key), StringComparer.Ordinal)`.
  - [x] Delete the `ApplyGeocode(caseDoc, locationKey, geocodeResult, listIndex)` helper method.
  - [x] Replace the call site (currently `ApplyGeocode(caseDoc, safeLocationKey, geocodeResult, request.listIndex ?? 0);`) with `_caseGeocodingManager.Apply(caseDoc, safeLocationKey, geocodeResult, request.listIndex ?? 0);`.

- [x] **Task 7 — Migrate `BatchItemProcessingService`.** (AC: #6)
  - [x] Change each of the 5 call sites from `_caseGeocodingManager.Apply_*_Geocode(new_case, geo_result[, listIndex])` to `_caseGeocodingManager.Apply(new_case, "<locationKey>", geo_result[, listIndex])`. The 5 key mappings are:
    - Line ~1162: `Apply_DC_AddressOfDeath_Geocode` → key `"dc_address_of_death"`
    - Line ~1203: `Apply_DC_PlaceOfLastResidence_Geocode` → key `"dc_place_of_last_residence"`
    - Line ~1588: `Apply_BC_LocationOfResidence_Geocode` → key `"bc_location_of_residence"`
    - Line ~1904: `Apply_BC_LocationOfResidence_Geocode` → key `"bc_location_of_residence"`
    - Line ~1919: `Apply_BC_FacilityOfDelivery_Geocode` → key `"bc_facility_of_delivery"`
  - [x] Do not restructure surrounding logic. Only replace the method call itself.

- [x] **Task 8 — Grep guardrails.** (AC: #3, #6)
  - [x] `Select-String -Path "nccdphp-drh-mmria-common\mmria.common\SharedLibraries\Case\Manager\CaseGeocodingManager.cs","nccdphp-drh-mmria-services\mmria.services\Services\BatchItemProcessingService.cs","source-code\mmria\mmria-server\Controllers\api\CaseGeocodeController.cs" -Pattern "Apply_\w+_Geocode"` → zero matches.
  - [x] `Select-String -Path "source-code\mmria\mmria-server\Controllers\api\CaseGeocodeController.cs" -Pattern "new HashSet.*\{ ""(dc_|bc_|pc_|erh_|omv_|mt_)"` → zero matches (no hand-maintained key list literals remain).

- [x] **Task 9 — Build.** (AC: #7)
  - [x] Run the `build-server` VS Code task (dotnet build of `mmria-server.csproj`). Must succeed with zero errors.
  - [x] Run `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj`. Must succeed with zero errors.

- [x] **Task 10 — Write the single-line-extension proof.** (AC: #8)
  - [x] In the Dev Agent Record, write a short section titled "Adding an 11th location — verification of AC #8." Answer the following prompt: *"Suppose we need to add a new geocode-enabled location with key `cc_something_new` targeting case-document path `custom_form/something_new`. What is the complete list of source-code changes required?"* The answer must be exactly one bullet: *add `["cc_something_new"] = GeocodeTarget.Static("custom_form/something_new"),` to `LocationRegistry`.* If the answer contains any other file change, AC #8 fails and the story is not done.

## Dev Notes

**One caller pattern is intentionally out of scope: the client.** The browser POSTs `POST /api/case-geocode/{caseId}/{locationKey}` with the same body it has since Story 30.3. The registry lives entirely server-side. The `$case_geocode_dispatch` helper introduced in Story 30.4 (which lives in `MMRIA_calculations.js`, `wwwroot/scripts/validator.js`, `database-scripts/validator.js`, `database-scripts/mmria-check-code.js`) is not modified in this story.

**Do not modify `GeocodingManager` or `GeocodeResult`.** This story restructures only how `CaseGeocodingManager` maps location keys to case-document paths. The geocode call itself, the result DTO, and every other layer are unchanged.

**Do not modify `ApplyStatic` / `ApplyList` private helpers in `CaseGeocodingManager`.** The dispatch surface is the new `Apply` method; the existing document-mutation helpers already handle both shapes correctly and are the right primitives.

**Static registry, ordinal comparer.** The keys are ASCII identifiers; use `StringComparer.Ordinal` (not `OrdinalIgnoreCase`) to match the current `_validKeys` comparer in `CaseGeocodeController` at HEAD.

**Ordering of registry entries.** Prefer the same order as the FR-1.1 table in the v4.2 PRD (`dc_place_of_last_residence`, `dc_address_of_injury`, `dc_address_of_death`, `bc_facility_of_delivery`, `bc_location_of_residence`, `pc_primary_care_facility`, `erh_location`, `omv_location_of_care`, `mt_origin_address`, `mt_destination_address`). Not functionally required, but keeps future review pleasant.

**Depends on:** Epic 30 fully landed (all Epic 30 stories in `review` or `done`, all Epic 30 files committed). Do not start until Epic 30's commit is in `git log`.

## Dev Agent Record

### Task 1 — Snapshot of the pre-refactor mapping

Extracted verbatim from `CaseGeocodingManager.cs` at baseline commit `330d0773`:

| locationKey                     | isList | basePath / (listPath, subPath)                                                            |
| ------------------------------- | ------ | ----------------------------------------------------------------------------------------- |
| `dc_place_of_last_residence`    | no     | `death_certificate/place_of_last_residence`                                               |
| `dc_address_of_injury`          | no     | `death_certificate/address_of_injury`                                                     |
| `dc_address_of_death`           | no     | `death_certificate/address_of_death`                                                      |
| `bc_facility_of_delivery`       | no     | `birth_fetal_death_certificate_parent/facility_of_delivery_location`                      |
| `bc_location_of_residence`      | no     | `birth_fetal_death_certificate_parent/location_of_residence`                              |
| `pc_primary_care_facility`      | no     | `prenatal_care_record/location_of_primary_prenatal_care_facility`                         |
| `erh_location`                  | yes    | (`er_visit_and_hospital_medical_records`, `location`)                                     |
| `omv_location_of_care`          | yes    | (`other_medical_office_visits`, `location_of_medical_care_facility`)                      |
| `mt_origin_address`             | yes    | (`medical_transport`, `origin_information/address`)                                       |
| `mt_destination_address`        | yes    | (`medical_transport`, `destination_information/address`)                                  |

Verified against the `_validKeys` HashSet literal in `CaseGeocodeController.cs` at HEAD — the 10 keys match exactly with ordinal comparison.

### Implementation Plan

1. Added `GeocodeTarget` `readonly record struct` in the same file as `CaseGeocodingManager` with `Static(basePath)` and `List(listPath, subPath)` factory helpers.
2. Added `public static readonly IReadOnlyDictionary<string, GeocodeTarget> LocationRegistry` populated from the Task 1 snapshot, using `StringComparer.Ordinal` and PRD FR-1.1 ordering.
3. Added `public void Apply(ExpandoObject caseDoc, string locationKey, GeocodeResult result, int listIndex = 0)` — dispatches through the registry to the existing (unmodified) private `ApplyStatic` / `ApplyList` helpers. Unknown keys return silently so batch code can call without pre-validation.
4. Deleted the 10 `Apply_*_Geocode` public methods. No wrappers left behind.
5. In `CaseGeocodeController.cs`: replaced the `_validKeys` and `_listKeys` hand-maintained HashSet literals with initializers derived from `CaseGeocodingManager.LocationRegistry` (`.Keys` and `.Where(kv => kv.Value.IsList).Select(kv => kv.Key)`), deleted the private `ApplyGeocode(caseDoc, locationKey, geocodeResult, listIndex)` switch helper, and replaced its call site with `_caseGeocodingManager.Apply(caseDoc, safeLocationKey, geocodeResult, request.listIndex ?? 0)`.
6. In `BatchItemProcessingService.cs`: converted all 5 call sites (lines 1162, 1203, 1588, 1904, 1919) to the string-literal `Apply(new_case, "<key>", geo_result)` form. Surrounding logic untouched.

### Completion Notes

- Both grep guardrails from Task 8 return zero matches:
  - `Apply_\w+_Geocode` across the three touched files → 0 hits.
  - `new HashSet.*\{ "(dc_|bc_|pc_|erh_|omv_|mt_)` in `CaseGeocodeController.cs` → 0 hits.
- `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` → **0 errors** (153 pre-existing warnings unchanged).
- `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj` → **0 errors** (built to alternate output directory because the local dev-loop was holding a file lock on `bin/Debug/net10.0/mmria.common.dll`; the compile phase itself is clean).
- Behavior parity confirmed by construction: every registry entry dispatches to the same private `ApplyStatic` / `ApplyList` helper with the same `basePath` / (`listPath`, `subPath`) values the deleted per-location methods used. Those helpers are unmodified. No test-behavior surface changed.

### Adding an 11th location — verification of AC #8

**Prompt:** *Suppose we need to add a new geocode-enabled location with key `cc_something_new` targeting case-document path `custom_form/something_new`. What is the complete list of source-code changes required?*

**Answer:**

- Add `["cc_something_new"] = GeocodeTarget.Static("custom_form/something_new"),` to `CaseGeocodingManager.LocationRegistry`.

No controller change. No batch-service change. No key-list edit. No switch update. The controller's `_validKeys` and `_listKeys` are computed from the registry at type-init; the controller's dispatch is the single `_caseGeocodingManager.Apply(...)` call, which itself dispatches from the same registry. The batch service is data-driven off the string literal key. AC #8 holds.

## File List

- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseGeocodingManager.cs` — added `GeocodeTarget`, `LocationRegistry`, and `Apply(...)`; deleted 10 per-location `Apply_*_Geocode` methods; private `ApplyStatic` / `ApplyList` helpers unchanged.
- `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs` — replaced literal `_validKeys` / `_listKeys` HashSets with registry-derived initializers; deleted private `ApplyGeocode` switch; call site now `_caseGeocodingManager.Apply(...)`.
- `nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs` — 5 call sites converted from named `Apply_*_Geocode(...)` to `Apply(new_case, "<key>", geo_result)`.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — Epic 42 and Story 42.1 status updates.
- `_bmad-output/implementation-artifacts/42-1-geocoding-location-registry-refactor.md` — Status, tasks, Dev Agent Record, File List, Change Log.

## Change Log

| Date       | Change                                                                                                     |
| ---------- | ---------------------------------------------------------------------------------------------------------- |
| 2026-08-19 | Converted `CaseGeocodingManager` to declarative `LocationRegistry` + single `Apply` method; deleted 10 per-location methods; migrated controller and batch service to data-driven dispatch. |
