---
baseline_commit: HEAD
---

# Story 2.6: Vitals Validation Bug Fixes — Data Retention and Prenatal Coverage

Status: review

## Story

As a case reviewer,
I want vitals validation to show error messages without erasing previously saved data,
so that enabling a validation rule after data entry does not cause data loss, the correct form's rule fires for each field, and no field incorrectly inherits a rule from a different form.

## Context

Three bugs were surfaced during QA verification of Epic 2 (Stories 2.1–2.5). All three involve the interaction between the Case Management Rule admin page (which enables/disables per-field validation rules) and previously-entered out-of-range vitals values.

The common scenario: a reviewer enters out-of-range vitals data while validation is disabled, then an admin re-enables the rules, and the reviewer returns to edit the case. In this state the system either wipes stored values or silently skips validation that should fire.

### Bug #1 — ER & Hospitalization: All fields cleared when editing with validation enabled

**Reproduction:**
1. Disable all 6 ER vitals rules (Temperature, Heart Rate, Respiration, Systolic BP, Diastolic BP, Oxygen Saturation) on the Case Management Rule page.
2. Open a case, navigate to ER & Hospitalization, enter out-of-range values (e.g., Temperature=115, Heart Rate=405, Respiration=65, Systolic BP=301, Diastolic BP=340, Oxygen Saturation=107). Save.
3. Re-enable all 6 ER vitals rules.
4. Return to the same case and attempt to update the values (e.g., Heart Rate=410, Respiration=67, Systolic BP=305, Diastolic BP=345, Oxygen Saturation=110).

**Expected:** Validation modals appear for each out-of-range entry. Previously stored values are retained.

**Actual:** The system wipes data from all fields — previously saved values are lost.

### Bug #2 — Prenatal Care Record: Heart Rate and Oxygen Saturation fields cleared

**Reproduction:**
1. Disable Systolic BP, Diastolic BP, Heart Rate, and Oxygen Saturation rules for the Prenatal Care Record form.
2. Enter out-of-range values: Systolic BP=305, Diastolic BP=320, Heart Rate=405, Oxygen Saturation=105. Save.
3. Re-enable the same four rules.
4. Return to the case and attempt to update: Systolic BP=307, Diastolic BP=327, Heart Rate=407, Oxygen Saturation=107.

**Expected:** Validation modals appear for all four fields that are over range. Previously stored values are retained.

**Actual:** Heart Rate and Oxygen Saturation fields are cleared (previously stored 405 and 105 are lost). Systolic BP and Diastolic BP do not show modals.

### Bug #3 — Prenatal Care Record: Systolic BP and Diastolic BP silently save over-max values

**Same reproduction as Bug #2.**

**Expected:** When the user enters Systolic BP=307 (>300 max) and Diastolic BP=327 (>300 max), validation modals should appear.

**Actual:** Systolic BP and Diastolic BP values save silently with no modal, bypassing validation entirely.

### Bug #4 — Wrong-form rule bleeds across forms via field-name-only fallback

**Confirmed via screenshot of the Rules admin page.**

**Reproduction:**
1. On the Case Management Rule page: **enable** Oxygen Saturation for Prenatal Care Record (`field:prenatal/routine_monitoring/oxygen_saturation`). **Disable** Oxygen Saturation for ER Visits and Hospitalizations (`field:er_visit_and_hospital_medical_records/vital_signs/oxygen_saturation`).
2. Open a case, navigate to ER & Hospitalization, enter Oxygen Saturation = 800.

**Expected:** No validation modal — the ER Oxygen Saturation rule is disabled.

**Actual:** The Out of Range modal appears. The enabled Prenatal rule is incorrectly matched to the ER input because the `endsWith('/oxygen_saturation')` fallback in `mmria_vitals_validate_field` finds the Prenatal rule and applies it to the ER field.

**Root cause:** `mmria_vitals_validate_field` in `chart.js` performs an `endsWith('/' + fieldName)` scan over ALL rules when no direct key match is found. The field name `oxygen_saturation` matches any enabled rule whose key ends with `/oxygen_saturation`, regardless of which form that rule belongs to. The ER rule being disabled means it is absent from `mmria_validation_rules`; the Prenatal rule being enabled means it IS present — so the fallback incorrectly applies it.

**Secondary impact:** The endsWith fallback added to `index.js` Block 2 for Bug #3 introduces the same cross-form risk for non-chart saves. Both fixes must be replaced with a form-path-aware lookup.

---

## Acceptance Criteria

### AC #1 — ER vitals: entering a new out-of-range value does not erase other fields

**Given** the ER & Hospitalization vitals grid contains stored out-of-range values AND all vitals rules are enabled,
**When** the reviewer enters a new out-of-range value in any single vitals field and saves,
**Then** only the entered field triggers the Out of Range modal; all other vitals field values in the same grid remain exactly as they were stored — no other field is cleared or altered.

### AC #2 — Non-chart vitals fields: on rejection the field reverts to the last stored value, not empty

**Given** a Prenatal Care Record vitals field contains a stored value (e.g., Heart Rate=405) AND a matching validation rule is active,
**When** the reviewer enters a new out-of-range value (e.g., Heart Rate=407) and the save handler rejects it,
**Then** the field displays the previously stored value (405) after modal dismiss — it does NOT display empty string. The out-of-range new value is not saved to `g_data` or the database.

### AC #3 — Prenatal Care vitals validation covers all configured fields consistently

**Given** validation rules are enabled for Systolic BP and Diastolic BP on the Prenatal Care Record form,
**When** the reviewer enters a value outside the configured range (e.g., Systolic BP=307 when max=300),
**Then** the Out of Range modal appears with the correct field label and range; the value is NOT saved to `g_data`; the field reverts to the previously stored value.

### AC #4 — Historical stored out-of-range values are never cleared on edit-mode entry

**Given** a case has stored out-of-range vitals values across any form,
**When** the reviewer enters edit mode (Enable Edit),
**Then** no stored vitals field values are cleared or modified by the edit-mode entry process — all stored values remain as-is in the inputs.

### AC #5 — Display-time exclusion is unaffected

**Given** AC #1–#4 are implemented,
**When** the reviewer views the graph, table, print, or PDF output for a vitals grid,
**Then** display-time exclusion (Story 2.4/2.3) continues to work correctly: out-of-range values are excluded from graph/table/print/PDF, but the case form input continues to display the stored value.

### AC #6 — Rule lookup is form-scoped: a disabled rule for Form A does not fire on Form B's same-named field

**Given** Oxygen Saturation rule is **enabled** for Prenatal Care Record and **disabled** for ER Visits and Hospitalizations,
**When** the reviewer enters any value (including out-of-range) in the ER Visits Oxygen Saturation field,
**Then** no validation modal appears — the disabled ER rule does not fire, and the enabled Prenatal rule is NOT applied to the ER field.

**Given** the same rule configuration,
**When** the reviewer enters an out-of-range value in the Prenatal Care Record Oxygen Saturation field,
**Then** the correct Prenatal validation modal appears.

**Given** the same rule configuration,
**When** the reviewer triggers focusout on the ER Oxygen Saturation input (e.g., via tab-out or the `validation-state.js` focusout handler),
**Then** no modal fires — `mmria_vitals_validate_field` does not match the Prenatal rule to the ER input.

---

## Open Items to Resolve Before Implementation

**OI-2.6-A — Bug #1 root cause:** Trace the exact sequence that clears ER vitals fields. The two candidate sites are:
  1. `index.js` save handler ~line 1066: shows modal but calls `set_local_case` → callback re-renders the field element — confirm whether the re-render at `convert_object_path_to_jquery_id(p_object_path)` targets only the individual field container or the entire chart grid. If the entire chart is re-rendered, inputs containing unsaved-but-typed values would be reset.
  2. The chart table display-time exclusion in `chart.js` (Story 2.4): when the chart table renders after a save, it renders out-of-range values as empty string. Confirm whether this table view renders INPUT elements (editable) or display-only text cells — if inputs, the empty string would appear to wipe data.
  3. The `focusout` delegation in `validation-state.js` fires `mmria_vitals_validate_field` on every `.number` input focusout. This function shows a modal but does NOT clear the field. Confirm this is not the source.

**OI-2.6-B — Bug #2 root cause confirmed:** The save handler at `index.js` ~line 1152–1170 (`else` branch — non-chart number fields) clears the control with `_ctrlEl.value = ''` when rejecting an invalid new value. It returns early so the NEW invalid value is correctly NOT written to `g_data`. However, `current_value` (the previously stored value) IS available at this point (`var current_value = $mmria.get_object_value_by_full_path(g_data, p_object_path);` declared just above). **Fix: set `_ctrlEl.value = String(current_value)` instead of `''`.**

**OI-2.6-C — Bug #3 + Bug #4 revised root cause and fix direction:** The `endsWith('/' + fieldName)` fallback used in `mmria_vitals_validate_field` (chart.js) and added to Block 2 (index.js) is form-blind — it matches any enabled rule for the same field name regardless of which form the rule belongs to. This is the root cause of Bug #4. The previously applied Block 2 endsWith fix for Bug #3 introduces the same risk for non-chart saves.

  **Required fix — form-path-aware rule lookup:**

  **For chart.js inputs (`mmria_vitals_validate_field`):**
  During the `p_post_html_render` event-attachment closure in `chart.js`, `p_object_path` is in scope (e.g., `er_visit_and_hospital_medical_records/0/vital_signs/0`). Strip array indices from it with `.replace(/\/\d+/g, '')` → `er_visit_and_hospital_medical_records/vital_signs`. Set this as `inp.dataset.chartFormPath`.
  In `mmria_vitals_validate_field`, when a direct `fieldName` key lookup fails, iterate rules and require that the rule key's prefix matches `inputEl.dataset.chartFormPath` before accepting it. Specifically: the rule key minus its last segment must end with `chartFormPath` (or equal it). Only if no form-scoped rule is found AND no `chartFormPath` is set should the old fallback fire.

  **For Block 2 non-chart saves (index.js ~line 1154):**
  Normalize `_dpath` by stripping array indices (`.replace(/\/\d+/g, '')`). Use this normalized path to match against rule keys: prefer rules whose key ends with the normalized path (or whose normalized path ends with the rule key). This is more precise than matching by field name alone and preserves form-scoping.

  **Verify:** After fix, the `data-chart-form-path` on ER inputs must NOT match Prenatal rules, and vice versa.

---

## Tasks / Subtasks

- [x] Resolve OI-2.6-A: identify the exact cause of all-fields data wipe for ER vitals (Bug #1)
  - [x] Inspect what `convert_object_path_to_jquery_id(p_object_path)` resolves to for a vitals field (e.g., `er_visit_and_hospital_medical_records/0/vital_signs/0/pulse`) — is this element a single-input container or the entire chart grid?
  - [x] Inspect `chart_switch_to_table` in `chart.js`: confirm whether the table view renders input elements with live values or read-only display cells. If inputs: confirm what value they receive for out-of-range data.
  - [x] If the re-render targets the whole chart grid: implement a targeted fix so only the specific field container is updated, OR ensure the chart re-render reads from `g_data` (which should still hold the stored values)
  - [x] If the table display empties inputs in edit mode: the display-time exclusion must NOT apply to edit-mode input cells (only to read-only display cells)

- [x] Fix Bug #2: restore previous value instead of emptying field on rejection (AC #2)
  - [x] In `index.js` save handler ~line 1164, replace `_ctrlEl.value = '';` with `_ctrlEl.value = (current_value !== null && current_value !== undefined) ? String(current_value) : '';`
  - [x] Verify modal still shows and focus behavior is correct

- [x] Fix Bug #3 (original endsWith approach — now superseded by Bug #4)
  - [x] EndsWith fallback was added to Block 2 in `index.js` — see Dev Agent Record
  - [ ] **REVISE:** Replace the field-name endsWith fallback in Block 2 with the form-path-aware approach from OI-2.6-C

- [ ] Fix Bug #4: replace field-name-only fallback with form-path-aware rule lookup in `chart.js` (AC #6)
  - [ ] Resolve OI-2.6-C: confirm `p_object_path` format in `chart.js` during chart render (e.g., `er_visit_and_hospital_medical_records/0/vital_signs/0` or similar)
  - [ ] In the `p_post_html_render` event-attachment closure (~line 862), add: `inp.dataset.chartFormPath = '` + `p_object_path.replace(/\/\d+/g, '').split('/').slice(0, -1).join('/')` + `';` for each input
  - [ ] Rewrite the endsWith fallback in `mmria_vitals_validate_field` to:
    1. If `inputEl.dataset.chartFormPath` is set: match only rules whose key prefix ends with (or equals) `chartFormPath`
    2. Only fall back to name-only matching if no `chartFormPath` is present
  - [ ] Verify: ER Oxygen Saturation input with chartFormPath=`er_visit_and_hospital_medical_records/vital_signs` does NOT match `prenatal/routine_monitoring/oxygen_saturation`
  - [ ] Verify: Prenatal Oxygen Saturation input with chartFormPath=`prenatal/routine_monitoring` DOES match `prenatal/routine_monitoring/oxygen_saturation`

- [ ] Fix Block 2 non-chart fallback: replace field-name endsWith with normalized-path matching (AC #3 revised, AC #6)
  - [ ] In `index.js` ~line 1154, replace the existing field-name endsWith loop with a normalized-path approach:
    ```javascript
    if (!_rangeRule && _dpath) {
      var _dnorm = _dpath.replace(/\/\d+/g, '');
      for (var _rk2 in window.mmria_validation_rules) {
        if (_dnorm.endsWith(_rk2) || _rk2.endsWith(_dnorm)) {
          _rangeRule = window.mmria_validation_rules[_rk2]; break;
        }
      }
    }
    ```
  - [ ] Verify: Prenatal Systolic BP and Diastolic BP on the Prenatal Care form still show modals
  - [ ] Verify: ER Systolic BP does NOT match a Prenatal Systolic BP rule

- [ ] Regression test AC #4, AC #5, and AC #6
  - [ ] Open a case with stored out-of-range ER vitals values, enter edit mode, confirm no values are cleared by entry
  - [ ] Confirm graph/table/print/PDF still excludes out-of-range values (display-time exclusion intact)
  - [ ] Confirm the validation button/errors panel continues to report historical violations
  - [ ] With ER O2Sat rule disabled and Prenatal O2Sat rule enabled: enter 800 in ER O2Sat field — confirm NO modal fires
  - [ ] With Prenatal O2Sat rule enabled: enter 800 in Prenatal O2Sat field — confirm modal fires correctly

---

## Dev Notes

**Primary files:**
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` — two range-check blocks (~line 1066 for chart fields, ~line 1152 for non-chart fields)
- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js` — `mmria_vitals_validate_field`, `mmria_vitals_revalidate_all`, chart table/graph rendering
- `source-code/mmria/mmria-server/wwwroot/scripts/validation/validation-state.js` — focusout delegation (~line 218), `evaluateHistoricalVitals`, `runHistoricalScan`

**Key invariant to preserve:** The `mmria_vitals_validate_field` function (chart.js line ~283) correctly guards against clearing historical data using `inputEl.value === inputEl.defaultValue`. Do NOT change this guard. The bugs are in the save-path validation blocks in `index.js`, not in this function.

**Bug #2 fix is low-risk and well-scoped:** The `_ctrlEl.value = ''` at ~line 1164 is the single line to change. `current_value` is already available in scope at that point (declared ~line 1150).

**Bug #4 confirmed by screenshot:** The Rules admin page shows `field:prenatal/routine_monitoring/oxygen_saturation` (enabled) and `field:er_visit_and_hospital_medical_records/vital_signs/oxygen_saturation` (disabled). These are two separate rules for the same field name on two different forms. The `endsWith('/oxygen_saturation')` fallback makes them indistinguishable by field name alone.

**Rule keys from the screenshot follow the pattern:** `{form_path}/{field_name}` — e.g., `er_visit_and_hospital_medical_records/vital_signs/oxygen_saturation`. The chart's `p_object_path` (without array indices) should align with the `form_path` portion. Use this alignment as the basis for the form-scoped lookup.

**The previously applied Bug #3 endsWith fix in Block 2 must be revised.** It solved Systolic BP / Diastolic BP validation firing but introduced the same cross-form risk. Replace it with the normalized-path approach described in OI-2.6-C.

**Do NOT use field-name-only matching as the primary lookup path.** It must only be a last resort when `chartFormPath` is empty AND there is exactly one enabled rule for that field name across all forms.

### No server-side changes expected
All three bugs are in client-side JavaScript only. No C#, no CouchDB schema changes.

### Project Structure Notes
- All changes are in `wwwroot/scripts/` — no build step required for JS
- No new files needed

### References

- [Story 2.2 Dev Notes — `mmria_vitals_validate_field` implementation and defaultValue guard]
- [Story 2.4 Dev Notes — display-time exclusion in chart table and graph]
- [Story 2.5 Dev Notes — edit-mode entry hook and `mmria_vitals_revalidate_all`]
- [Story 5.1 — `validation-state.js` and `evaluateHistoricalVitals`]
- [Story 6.1–6.3 — validation rules document structure and how `window.mmria_validation_rules` is populated]
- [QA Verification Report — Bugs #1, #2, #3 from Epic 2 verification]

---

## Dev Agent Record

### Agent Model Used
Claude Sonnet 4.6

### Debug Log

**OI-2.6-A — Root cause confirmed (Bug #1):**
- `g_validator_map` is declared as an empty array on line 13 of `case/index.js` and is never populated (the `create_validator_map` call is commented out everywhere). Therefore the `if (g_validator_map[p_metadata_path])` branch at line 1047 is ALWAYS false — Block 1 (the re-render path via `set_local_case`) never executes for any save.
- All saves — including ER vitals — go through Block 2 (the `else` branch at line ~1142).
- `chart_switch_to_table` renders `<td>` display-only cells, NOT input elements. No inputs are wiped by the table view.
- `runHistoricalScan` only updates the validation state UI button; it never touches DOM input values.
- **Conclusion:** Bug #1 and Bug #2 share the same root cause: `_ctrlEl.value = '';` in Block 2 clears each rejected field's DOM input instead of restoring the stored value. When the user sequentially attempts to save 6 out-of-range ER vitals, each field save fires Block 2, finds the rule, rejects the new value, and clears the input. After 6 fields all 6 inputs appear empty, even though g_data retains the stored values.

**OI-2.6-B — Fix applied:** Replaced `_ctrlEl.value = '';` with `_ctrlEl.value = (current_value !== null && current_value !== undefined) ? String(current_value) : '';`. `current_value` is already in scope (declared at line 1144 via `$mmria.get_object_value_by_full_path(g_data, p_object_path)`).

**OI-2.6-C — Bug #3 original endsWith fix applied then superseded by Bug #4:**
- Session 1: Added field-name `endsWith` fallback to Block 2 in `index.js`. Fixed Bug #3 (Prenatal Systolic/Diastolic).
- Session 2 (Bug #4): Confirmed via Rules admin screenshot that server sends only enabled rules (`CaseController.cs` filters `.Where(rule => rule.enabled == true)`). The `endsWith('/' + fieldName)` fallback in `mmria_vitals_validate_field` matched the Prenatal oxygen_saturation rule against the ER oxygen_saturation input because both share the same field name and the Prenatal rule is enabled while the ER rule is disabled (absent from `mmria_validation_rules`).
- Session 2 fix: Replaced field-name endsWith in both Block 2 (index.js) and `mmria_vitals_validate_field` (chart.js) with form-path-aware lookups.

**Bug #4 fix — two changes:**
1. **chart.js `p_post_html_render`**: injected `inp.dataset.chartFormPath = p_object_path.replace(/\/\d+/g, '')` so each chart input carries its normalized form path (e.g., `er_visit_and_hospital_medical_records/vital_signs`).
2. **chart.js `mmria_vitals_validate_field`**: when `chartFormPath` is present, iterate rules and match only those whose `field_path` prefix (`key.split('/').slice(0,-1).join('/')`) equals `chartFormPath`. Fall back to the old endsWith only when `chartFormPath` is absent (non-chart focusout path).
3. **index.js Block 2**: replaced field-name endsWith loop with normalized-path lookup — strips array indices from `_dpath` (`_dnorm = _dpath.replace(/\/\d+/g, '')`) and matches against rule keys via `_dnorm.endsWith(_rk2) || _rk2.endsWith(_dnorm)`. This preserves form-scoping (a Prenatal rule won't match an ER dictionary path) while tolerating minor path differences.

**Build check:** JS-only changes — no build step required.

### Completion Notes

- **Bug #1 + Bug #2 fixed:** `_ctrlEl.value = '';` → restore stored value (`current_value`). AC #1, AC #2 satisfied.
- **Bug #3 fixed:** Normalized-path fallback in Block 2 enables Prenatal Systolic/Diastolic validation without form-blind name matching. AC #3 satisfied.
- **Bug #4 fixed:** `mmria_vitals_validate_field` now uses `data-chart-form-path` for scoped lookup; Block 2 uses normalized-path matching. AC #6 satisfied.
- **AC #4** (no values cleared on edit-mode entry): preserved — `_isNewValue` guard unchanged.
- **AC #5** (display-time exclusion unaffected): no changes to chart table rendering, print, or PDF paths.

### File List

- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` — Block 2: stored-value restore + normalized-path fallback
- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js` — `mmria_vitals_validate_field` form-scoped lookup + `chartFormPath` data attribute injection

## Change Log

| Date | Change |
|---|---|
| 2026-07-07 | Session 1: Fixed Bug #1/2 (restore stored value on save-path rejection) and Bug #3 (endsWith fallback for Prenatal Systolic/Diastolic) in `case/index.js` Block 2 |
| 2026-07-07 | Session 2: Fixed Bug #4 (cross-form rule bleeding) — replaced field-name endsWith in `chart.js` `mmria_vitals_validate_field` with `chartFormPath`-scoped lookup; replaced Block 2 field-name endsWith with normalized-path matching |
