# Story 4.0: Validation Engine Foundation

Status: not-started

## Story

As a developer,
I want the case validation engine and vitals validation rules ported from the v4.1-case-data-validation-mode branch with field_rules seeded at server startup,
So that validation rules are available to the client via API and the three vitals rendering functions (chart, print, PDF) can read rules by field_path instead of by field name.

## Context and Scope

This story ports the **validation engine foundation only** — the `CaseValidationManager`, models, and vitals field rules seeding logic from the branch. The actual validation UI, gating logic, and form panels are **explicitly out of scope**. Only the engine and callsite wiring are implemented.

### What IS Included

1. **CaseValidationManager and Models** — Full port from branch
   - `CaseValidationManager.cs` (manager, DAL, models)
   - `CaseValidationDAL.cs` (database access)
   - `CaseValidationModels.cs` (all rule and evaluation types)
   - Port GetSeededNumericRange method with 6 confirmed vitals rules

2. **Server-Side Seeding** — Seed at startup
   - Call `GetOrCreateRuleDocumentAsync()` at server startup
   - Seed exactly 6 vitals: temperature, heart_rate, respiration_rate, systolic_bp, diastolic_bp, oxygen_saturation
   - All seeded rules: `severity: "hard"`, `review_status: "reviewed"`

3. **API Endpoint** — Expose field_rules to client
   - New `api/validation_rules` endpoint
   - Returns serialized field_rules keyed by field_path (not field name)
   - Example: `{ "er_visit_and_hospital_medical_records/vital_signs/temperature": { min, max, ... }, ... }`

4. **Client-Side Wiring** — Update three rendering functions
   - `chart.js` — `mmria_vitals_is_out_of_range()` reads from `window.mmria_validation_rules` by field_path
   - `print_version_renderer.js` — same update
   - `pdf-version/index.js` — same update
   - Update pass-through calls in `case/index.js`, `print-version/index.js`, `pdf-version/index.js`

5. **Evaluation Context Flag** — Engine support only (no UI gate)
   - Add `evaluation_context` parameter to validation functions
   - `"active-input"` — hard enforced on blur (server-side code ready, not wired to UI)
   - `"historical"` — hard downgraded to warning at load time (server-side code ready, not wired to UI)
   - No panel UI, no form validation check, no gating — engine support only

6. **Cleanup** — Remove old vitals range code
   - Delete `VitalSignRangeHelper.cs`
   - Remove two TempData vitals range lines from `CaseController.cs` (keep the new validation_rules TempData line)
   - Stop calling `VitalSignRangeHelper` methods

### What IS NOT Included

- **No validation UI** — No modals, no red indicators, no form panels
- **No validation gate on View/PDF** — No modal before print/PDF rendering
- **No admin/editor UI** — Form designer UI, rule management pages not in scope
- **No form validation checks** — No on-save validation logic
- **No connected field rules** — Only field_rules (no connected_field_rules or form_status_rules used)
- **No gating of evaluation context** — Flag is engine-only; caller wiring will happen in future stories

## Acceptance Criteria

### Server-Side (AC: #1–#8)

**AC #1: CaseValidationManager ported**
When the codebase is built,
Then `CaseValidationManager`, `CaseValidationDAL`, and all models from the branch are present in `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseValidation/`

**AC #2: Startup seeding runs**
When the server starts up,
Then `GetOrCreateRuleDocumentAsync()` is called for the current metadata version (ensuring the validation rule document exists or is created with defaults)

**AC #3: 6 vitals seeded correctly**
When seeding completes,
Then the rule document contains exactly these field_rules (or a superset) with the confirmed ranges:
- `temperature`: 80–115 °F
- `heart_rate`: 20–250 bpm
- `respiration_rate`: 4–80 bpm
- `systolic_bp`: 40–300 mmHg
- `diastolic_bp`: 20–200 mmHg
- `oxygen_saturation`: 0–100 %

All seeded rules have `severity: "hard"`, `review_status: "reviewed"`, `validation_level: "plausibility"`, `source: "logical-seed"`

**AC #4: API endpoint returns field_rules by field_path**
When `/api/validation_rules` is called,
Then it returns a JSON object keyed by field_path (e.g., `"er_visit_and_hospital_medical_records/vital_signs/temperature"`) with rule details (min, max, severity, review_status, etc.)

**AC #5: CaseController sets window.mmria_validation_rules**
When the Case page loads,
Then `TempData["validation_rules"]` is set to the serialized field_rules (keyed by field_path) and emitted as `window.mmria_validation_rules = @Html.Raw(validation_rules ?? "null");` in the HeadScripts section

**AC #6: VitalSignRangeHelper deleted**
When the codebase is built,
Then `VitalSignRangeHelper.cs` does not exist

**AC #7: CaseController TempData lines updated**
When `CaseController.Index()` executes,
Then the two old lines (`TempData["vital_sign_range_config"]`) are removed, and a new line `TempData["validation_rules"] = JsonSerializer.Serialize(fieldRulesByPath);` is called instead

**AC #8: No other references to VitalSignRangeHelper**
When a full grep is run,
Then no remaining references to `VitalSignRangeHelper` exist in the codebase (except in comments or deleted code artifacts)

### Client-Side (AC: #9–#14)

**AC #9: mmria_vitals_is_out_of_range reads from window.mmria_validation_rules by field_path**
In `chart.js`, `print_version_renderer.js`, and `pdf-version/index.js`:
- The function receives `fieldPath` (not just `fieldName`)
- It looks up `window.mmria_validation_rules[fieldPath]` instead of `window.mmria_vital_sign_range[fieldName]`
- Returns true if value is out of range or if `window.mmria_validation_rules` is null

**AC #10: Pass-through calls updated in case/index.js**
In `openTab()` function:
- Call remains `window.mmria_validation_rules` (not `vital_sign_range`)
- Third rendering function call passes `window.mmria_validation_rules` as parameter

**AC #11: Pass-through calls updated in print-version/index.js**
In `create_print_version()` function:
- Rename parameter from `p_vital_sign_range` to `p_validation_rules`
- Set `window.mmria_validation_rules = p_validation_rules;`

**AC #12: Pass-through calls updated in pdf-version/index.js**
In `createPDF()` function:
- Rename parameter from `p_vital_sign_range` to `p_validation_rules`
- Set `window.mmria_validation_rules = p_validation_rules;`

**AC #13: window.mmria_validation_rules null guard works**
When `window.mmria_validation_rules` is `null` or `undefined`,
Then all three rendering functions (`mmria_vitals_is_out_of_range`) return `false` (no exclusion applied)

**AC #14: No other references to mmria_vital_sign_range**
When a full grep is run on wwwroot/scripts/,
Then the only remaining references to `mmria_vital_sign_range` are: (a) old comments/documentation, (b) this story's deleted code list, (c) no active code paths

### Evaluation Context (Engine-Only) (AC: #15–#17)

**AC #15: EvaluateCase accepts evaluation_context parameter**
In `CaseValidationManager.EvaluateCase()`:
- Signature includes optional `evaluation_context` parameter (default: `null`)
- Passed through to rule evaluation logic (no gating at this layer)

**AC #16: Rule bands respect evaluation_context**
In the band matching logic:
- When `evaluation_context == "active-input"`, hard rules are enforced as-is
- When `evaluation_context == "historical"`, hard rules are downgraded to warning severity before band matching
- No UI gating or conditional returns at this layer (caller will implement)

**AC #17: No client-side context flag**
When the client-side rendering functions execute,
Then no evaluation_context is passed or checked (wiring deferred to future stories)

## Tasks / Subtasks

### Phase 1 — Server-Side: Port CaseValidation Libraries

- [ ] Copy `CaseValidationManager.cs` from branch to `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseValidation/Manager/`
  - Include full `GetSeededNumericRange()` method with 6 vitals
  - Include `BuildDefaultRuleDocument()` and `EvaluateCase()` methods
  - Include support for `evaluation_context` parameter in EvaluateCase (AC #15, #16)

- [ ] Copy `CaseValidationDAL.cs` from branch to `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseValidation/DAL/`
  - Include full async save/load methods

- [ ] Copy `CaseValidationModels.cs` from branch to `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseValidation/Model/`
  - All rule types: CaseValidationFieldRule, CaseValidationConnectedFieldRule, CaseValidationFormStatusRule, CaseValidationRuleBand, etc.

- [ ] Add `evaluation_context` parameter to server-side rule evaluation signatures
  - Update `EvaluateCase()` signature
  - Update band matching logic to check context and downgrade hard rules to warning for "historical" context (AC #16)

### Phase 2 — Server-Side: Startup Seeding and API

- [ ] Create startup seeding in `Program.cs` or a hosted service
  - Call `caseValidationManager.GetOrCreateRuleDocumentAsync()` for the current metadata version at startup
  - Seed completes before Case page is available
  - AC #2

- [ ] Verify seeded vitals rules (AC #3)
  - temperature: 80–115 °F
  - heart_rate: 20–250 bpm
  - respiration_rate: 4–80 bpm
  - systolic_bp: 40–300 mmHg
  - diastolic_bp: 20–200 mmHg
  - oxygen_saturation: 0–100 %

- [ ] Create `api/validation_rules` endpoint in new `validationRulesController.cs` (or extend existing API controller)
  - Route: `[HttpGet("api/validation_rules")]`
  - Returns serialized field_rules keyed by field_path (not field name)
  - Example response:
    ```json
    {
      "er_visit_and_hospital_medical_records/vital_signs/temperature": {
        "id": "field:...",
        "field_path": "er_visit_and_hospital_medical_records/vital_signs/temperature",
        "min_value": 80,
        "max_value": 115,
        "severity": "hard",
        "review_status": "reviewed",
        ...
      },
      ...
    }
    ```
  - AC #4

### Phase 3 — Server-Side: CaseController Update

- [ ] Update `CaseController.Index()` to use new validation_rules
  - Remove: `var vitalSignRangeConfig = VitalSignRangeHelper.GetVitalSignRangeConfig(...);`
  - Remove: `TempData["vital_sign_range_config"] = JsonSerializer.Serialize(vitalSignRangeConfig);`
  - Add: Fetch field_rules from rule document and serialize keyed by field_path
  - Add: `TempData["validation_rules"] = JsonSerializer.Serialize(fieldRulesByPath);`
  - AC #5, #7

- [ ] Update `Views/Case/Index.cshtml`
  - Replace: `window.mmria_vital_sign_range = @Html.Raw(...);`
  - With: `window.mmria_validation_rules = @Html.Raw(validation_rules ?? "null");`

- [ ] Delete `VitalSignRangeHelper.cs`
  - AC #6

- [ ] Verify no other references to VitalSignRangeHelper remain
  - AC #8

### Phase 4 — Client-Side: Update chart.js

- [ ] Update `mmria_vitals_is_out_of_range(fieldPath, value)` signature and body
  - Change parameter from `fieldName` to `fieldPath`
  - Change lookup from `window.mmria_vital_sign_range[fieldName]` to `window.mmria_validation_rules[fieldPath]`
  - Return `false` if `window.mmria_validation_rules` is null
  - AC #9

- [ ] Update all call sites in `chart.js` that call `mmria_vitals_is_out_of_range()`
  - Pass field_path instead of field name
  - Identify field_path from metadata object path + field name

### Phase 5 — Client-Side: Update print_version_renderer.js

- [ ] Update `mmria_vitals_is_out_of_range(fieldPath, value)` to match chart.js implementation
  - AC #9

- [ ] Rename parameter in `print_version_render()` (if applicable)
  - From: `p_vital_sign_range`
  - To: `p_validation_rules`

- [ ] Update all call sites that call `mmria_vitals_is_out_of_range()`
  - Pass field_path instead of field name

### Phase 6 — Client-Side: Update pdf-version/index.js

- [ ] Update `mmria_vitals_is_out_of_range(fieldPath, value)` to match chart.js implementation
  - AC #9

- [ ] Update `createPDF()` function signature
  - From: `p_vital_sign_range`
  - To: `p_validation_rules`
  - AC #12

- [ ] Update assignment inside `createPDF()`
  - From: `window.mmria_vital_sign_range = p_vital_sign_range;`
  - To: `window.mmria_validation_rules = p_validation_rules;`

- [ ] Update all call sites that call `mmria_vitals_is_out_of_range()`
  - Pass field_path instead of field name

### Phase 7 — Client-Side: Update Pass-Through Calls

- [ ] Update `case/index.js` `openTab()` function
  - From: `window.mmria_vital_sign_range`
  - To: `window.mmria_validation_rules`
  - AC #10

- [ ] Update `print-version/index.js` `create_print_version()` function signature
  - From: `p_vital_sign_range`
  - To: `p_validation_rules`
  - AC #11

- [ ] Update `pdf-version/index.js` `createPDF()` function signature
  - From: `p_vital_sign_range`
  - To: `p_validation_rules`
  - AC #12

### Phase 8 — Cleanup and Verification

- [ ] Verify grep for `VitalSignRangeHelper` finds no active code references
  - AC #8

- [ ] Verify grep for `mmria_vital_sign_range` finds only old comments/documentation (no active code)
  - AC #14

- [ ] Run full build — no errors
  - Server-side: CaseValidation library compiles
  - Client-side: No JS syntax errors

- [ ] Manual test: Load Case page
  - Verify `window.mmria_validation_rules` is populated in browser console
  - Verify vitals out-of-range rendering still works with new schema

- [ ] Manual test: Load chart/print/PDF with vitals out of range
  - Verify out-of-range values are excluded from rendering (no change in behavior)

## Dev Notes

### File Changes Summary

**New Files:**
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseValidation/Manager/CaseValidationManager.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseValidation/DAL/CaseValidationDAL.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseValidation/Model/CaseValidationModels.cs`
- `source-code/mmria/mmria-server/Controllers/api/validationRulesController.cs` (or extend existing controller)

**Modified Files:**
- `source-code/mmria/mmria-server/Program.cs` (add startup seeding)
- `source-code/mmria/mmria-server/Controllers/CaseController.cs` (remove VitalSignRangeHelper calls, add validation_rules TempData)
- `source-code/mmria/mmria-server/Views/Case/Index.cshtml` (update window.mmria_validation_rules line)
- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js` (update mmria_vitals_is_out_of_range)
- `source-code/mmria/mmria-server/wwwroot/scripts/print-version/print_version_renderer.js` (update mmria_vitals_is_out_of_range)
- `source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js` (update mmria_vitals_is_out_of_range, createPDF signature)
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` (update openTab pass-through)
- `source-code/mmria/mmria-server/wwwroot/scripts/print-version/index.js` (update create_print_version signature)

**Deleted Files:**
- `source-code/mmria/mmria-server/util/VitalSignRangeHelper.cs`

### Key Patterns from Branch

1. **Field_path keying** — Rules are keyed by full field path (e.g., `"er_visit_and_hospital_medical_records/vital_signs/temperature"`), not by short field name (e.g., `"temperature"`)
2. **GetSeededNumericRange()** — Returns tuple with (min, max, message, level, source, rationale, unit, review_status); applied to field_rules at creation time
3. **Evaluation context** — Passed as optional parameter to EvaluateCase; used to adjust severity of hard rules (not gated at engine layer)
4. **No connected_field_rules for now** — Only field_rules are seeded and exposed; connected_field_rules and form_status_rules are built but not used

### Testing Strategy

1. **Unit test** — Verify seeded vitals rules have correct min/max/severity
2. **Integration test** — Call `GetOrCreateRuleDocumentAsync()`, verify document exists with field_rules
3. **API test** — Call `/api/validation_rules`, verify response schema and field_path keying
4. **Client test** — Load Case page, verify `window.mmria_validation_rules` populated, verify rendering still works

### Future Stories

Story 4.1 will use this foundation to implement the print/view/PDF validation gate (checking rules and showing modal before rendering).
Story 4.2+ will implement the form validation checks and evaluation context wiring on the client side.
