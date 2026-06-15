# Story 2.1: Add Vitals Range Config — CouchDB Document and Server-Side Loading

Status: ready-for-dev

## Story

As a developer,
I want the valid ranges for all vitals fields stored in CouchDB and loaded into memory at server startup,
so that vitals validation and display-time exclusion can read ranges synchronously without network requests, and a developer can update ranges by script without a code deployment.

## Acceptance Criteria

1. The CouchDB config document in `database-scripts/` contains a `vital_sign_range` nested object under `string_keys.shared` with confirmed ranges: Temperature 0–110, Heart Rate 0–400, Respiration 0–60, Systolic BP 0–300, Diastolic BP 0–300, Oxygen Saturation 0–100 — each entry carrying `min`, `max`, and `label` keys.
2. A `NestedStringDictionaryConverter` custom `JsonConverter` is applied via `[JsonConverter]` attribute on `OverridableConfiguration.string_keys`, storing nested JSON objects as raw JSON strings.
3. `VitalSignRangeHelper.GetVitalSignRangeConfig(configuration, host_prefix)` deserializes the raw JSON into a typed `VitalSignRangeConfig` model; returns hardcoded defaults if key is absent or unparseable.
4. `CaseController.Index()` calls `VitalSignRangeHelper.GetVitalSignRangeConfig()`, serializes the result, and sets it as `TempData["vital_sign_range_config"]`.
5. The Case/Index Razor view emits `window.mmria_vital_sign_range = @Html.Raw(TempData["vital_sign_range_config"]);` in the `HeadScripts` block — `null` if config unavailable.
6. If `window.mmria_vital_sign_range` is `null`, all downstream client-side validation silently skips.

## Tasks / Subtasks

- [ ] Add `vital_sign_range` to CouchDB config document (AC: #1)
  - [ ] Locate the config document source in `source-code/mmria/mmria-server/database-scripts/`
  - [ ] Add `vital_sign_range` nested object under `string_keys.shared` with all 6 confirmed field entries
  - [ ] Each entry: `{ "min": N, "max": N, "label": "..." }` — field key names to be confirmed against HTML `name` attributes in `chart.js` (OI-4)
- [ ] Create `NestedStringDictionaryConverter` (AC: #2)
  - [ ] New file: `nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/NestedStringDictionaryConverter.cs`
  - [ ] Implement `JsonConverter<Dictionary<string, Dictionary<string, string>>>`
  - [ ] `Read()`: when a dictionary value is a JSON object (not a string), serialize the object back to its raw JSON string and store it as the string value
  - [ ] `Write()`: standard dictionary serialization
- [ ] Apply converter to `OverridableConfiguration` (AC: #2)
  - [ ] File: `nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs`
  - [ ] Add `[JsonConverter(typeof(NestedStringDictionaryConverter))]` attribute on the `string_keys` property
- [ ] Create `VitalSignRangeHelper` (AC: #3)
  - [ ] New file: `source-code/mmria/mmria-server/util/VitalSignRangeHelper.cs`
  - [ ] Static class following the pattern of `SessionTimeoutHelper`, `CaseEditInactivityConfigHelper` in the same `util/` directory
  - [ ] `GetVitalSignRangeConfig(configuration, host_prefix)` method:
    - Call `configuration.GetString("vital_sign_range", host_prefix)` to get raw JSON string
    - Deserialize into `VitalSignRangeConfig` model (dict of field name → `{ min, max, label }`)
    - On null/empty/parse failure: return hardcoded defaults (see Dev Notes)
  - [ ] Define `VitalSignRangeConfig` and `VitalSignRangeEntry` types in the same file or alongside
- [ ] Wire into `CaseController` (AC: #4)
  - [ ] File: `source-code/mmria/mmria-server/Controllers/CaseController.cs`
  - [ ] In `Index()` action (or whichever action serves the Case editor page): call helper, serialize result to JSON, set `TempData["vital_sign_range_config"]`
  - [ ] Follow exactly the same pattern used for `window.case_edit_inactivity_config` in the existing controller
- [ ] Emit global in Razor view (AC: #5, #6)
  - [ ] Identify the Case editor Razor view (likely `Views/Case/Index.cshtml`)
  - [ ] In the `@section HeadScripts { }` block, add: `window.mmria_vital_sign_range = @Html.Raw(TempData["vital_sign_range_config"] ?? "null");`
  - [ ] Confirm placement is after the existing config globals
- [ ] Build and verify (AC: #1–#6)
  - [ ] Run `build-server` task — zero errors
  - [ ] Load Case page in browser, open DevTools console, confirm `window.mmria_vital_sign_range` is a non-null object with the 6 field entries
  - [ ] Temporarily remove config key from doc, reload — confirm `window.mmria_vital_sign_range` has defaults, no console errors

## Dev Notes

**Files to create:**
- `nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/NestedStringDictionaryConverter.cs`
- `source-code/mmria/mmria-server/util/VitalSignRangeHelper.cs`

**Files to modify:**
- `nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs` — add `[JsonConverter]` on `string_keys`
- `source-code/mmria/mmria-server/Controllers/CaseController.cs` — add TempData population
- `Views/Case/Index.cshtml` (or equivalent) — add HeadScripts global
- `source-code/mmria/mmria-server/database-scripts/` — add `vital_sign_range` to config doc

**`OverridableConfiguration` current shape** (`configuration.cs`):
```csharp
Dictionary<string, Dictionary<string, string>> string_keys  // ← add [JsonConverter] here
Dictionary<string, DBConfigurationDetail>      detail_list
```
Config access: `tenantRuntime.RequireConfiguration()` → `configuration.GetString(key, host_prefix)`

**Existing helper pattern to follow** (e.g., `CaseEditInactivityConfigHelper` in `mmria-server/util/`):
- Static class, static method receiving `configuration` and `host_prefix`
- Returns typed config object; hardcoded defaults if config key missing

**Hardcoded defaults for `VitalSignRangeHelper`:**
```
Temperature:      min=0, max=110, label="Temperature"
Heart Rate:       min=0, max=400, label="Heart Rate"
Respiration:      min=0, max=60,  label="Respiration"
Systolic BP:      min=0, max=300, label="Systolic BP"
Diastolic BP:     min=0, max=300, label="Diastolic BP"
Oxygen Saturation: min=0, max=100, label="Oxygen Saturation"
```

**Open item OI-4 (do not block story):** Exact HTML `name` attributes for vitals inputs must be confirmed by inspecting the DOM rendered by `chart.js`. The config keys in `vital_sign_range` must match these `name` attributes exactly. Use placeholder keys matching the logical names above and update when OI-4 is resolved before Story 2.2 implementation.

**HeadScripts pattern reference:** Search `Views/Case/Index.cshtml` for `window.case_edit_inactivity_config` to find the exact placement pattern to follow.

**Server-side implementation rules (architecture §4.1):**
- No `try/catch` wrappers in Manager or DAL methods — let failures propagate
- No `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` — async all the way
- Use `configuration.GetString()` — do not bypass with raw CouchDB reads

### Project Structure Notes

- `NestedStringDictionaryConverter` belongs in `mmria.common` alongside `configuration.cs` — same namespace
- `VitalSignRangeHelper` belongs in `mmria-server/util/` alongside other helpers — same namespace pattern
- No new NuGet packages — uses `System.Text.Json` already referenced

### References

- [Source: architecture-mmria-v4.1.md#2.3 — Config document schema confirmed values]
- [Source: architecture-mmria-v4.1.md#2.3 — Server-side loading (NestedStringDictionaryConverter)]
- [Source: architecture-mmria-v4.1.md#2.4 — Client-side delivery (HeadScripts pattern)]
- [Source: architecture-mmria-v4.1.md#2.2 — Existing OverridableConfiguration shape]
- [Source: architecture-mmria-v4.1.md#4.1 — Server-side implementation patterns]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
