---
baseline_commit: e5009f9b79f61b736ae6e39facf8272c56817f4c
---

# Story 2.7: Fix Missing Range Validation — Prenatal Care Record BP Fields

**Epic:** Epic 2 — Vitals Field Validation (regression fix)
**Story ID:** 2.7
**Status:** ready-for-dev
**Date added:** 2026-07-02

---

## Story

As a case reviewer,
I want Systolic BP and Diastolic BP on the Prenatal Care Record to be validated against their configured ranges,
So that out-of-range blood pressure values are caught and blocked, consistent with the other vitals fields already working on that page.

## Acceptance Criteria

**AC-1 — Systolic BP and Diastolic BP must validate on Prenatal Care Record**

Given validation rules for Systolic BP (0–300), Diastolic BP (0–300), Heart Rate (0–400), and Oxygen Saturation (0–100) are **disabled** on the Case Management Rule page
And the Prenatal Care Record page is completed with out-of-range values for all four fields while rules are disabled (Systolic BP=305, Diastolic BP=320, Heart Rate=405, Oxygen Saturation=105)
When the four rules are re-enabled and the reviewer enters new out-of-range values (Systolic BP=307, Diastolic BP=327, Heart Rate=407, Oxygen Saturation=107)
Then the system displays the correct out-of-range error message for **Systolic BP and Diastolic BP**, matching the behavior already working correctly for Heart Rate and Oxygen Saturation on this page

**AC-2 — Out-of-range Systolic BP / Diastolic BP values must not be silently saved**

Given the scenario in AC-1
When an out-of-range Systolic BP or Diastolic BP value is entered and validation fires
Then the value is handled the same way Story 2.2 handles any other out-of-range vitals field (cleared/blocked with modal, per the active-edit validation behavior) — it must not be saved to the case record unvalidated

**AC-3 — No regression to fields already validating correctly**

Given Heart Rate and Oxygen Saturation on the Prenatal Care Record already validate correctly
When this fix is applied
Then Heart Rate and Oxygen Saturation validation behavior is unchanged

**AC-3a — Consistent flag/indicator treatment across all four Prenatal vitals fields**

Given Systolic BP and Diastolic BP now validate per AC-1/AC-2
When an out-of-range value exists on any of the four Prenatal vitals fields (Systolic BP, Diastolic BP, Heart Rate, Oxygen Saturation)
Then all four fields receive identical treatment from Story 2.5's historical-detection indicator/modal (per Story 2.5 AC #3/#4) — a reviewer must not see BP fields flagged differently (or not at all) compared to Heart Rate/Oxygen Saturation on the same record

**AC-4 — Rule-key enumeration test (prevents this bug class from recurring silently)**

Given the seeded `case-validation-rules` document contains one entry per validated vitals field across all forms
When an automated test enumerates every seeded rule key
Then each rule key resolves to a real HTML `name`/field path actually rendered on its target form — the test fails loudly if any rule key cannot be matched to a real field, instead of the current behavior of silently skipping validation

**AC-5 — Unit test on the rule-key resolution function itself**

Given `mmria_vitals_validate_field()`'s rule-lookup logic (exact match, then `endsWith('/' + fieldName)` fallback)
When a unit test exercises that lookup function directly with both the corrected Prenatal BP keys and a deliberately mismatched key
Then the test asserts correct resolution and correct "no rule found" behavior — independent of the browser-level UI test in AC-1

**AC-6 — Story 2.5 regression gate**

Given Story 2.5's original historical-detection scenarios (edit-mode entry re-validation, form-navigation re-validation, red indicator display, historical modal)
When this fix is applied
Then all of Story 2.5's original acceptance scenarios still pass unchanged

## Tasks / Subtasks

- [x] ~~Reproduce the defect using the exact repro steps in AC-1~~
- [x] ~~Root-cause the missing validation via static inspection~~ — **done 2026-07-02**: confirmed the seeded rule key and actual field path both use `prenatal/routine_monitoring/diastolic` (no mismatch). Confirmed Story 6.1 removed rule auto-generation (`BuildDefaultRuleDocument` returns empty); rules now only exist if created in CouchDB via the Story 6.2 admin UI.
- [x] ~~Confirm the live rules document~~ — **done 2026-07-02, live check complete (see Dev Notes "Update 2026-07-02 (live investigation)"):** queried `GET api/case-validation/rules/current` against the running local server. All four Prenatal vitals rules (`systolic_bp`, `diastolic`, `heart_rate`, `oxygen_saturation`) currently **exist and are `enabled: true`** in the live document. The "missing/disabled rule entries" hypothesis is **disproven** in this environment — the reported symptom does not currently reproduce here. However, a real, confirmed, previously-undocumented lookup fragility was found (see Dev Notes) that would reproduce this exact symptom if the Prenatal-specific rule entries are ever removed/disabled again (e.g., after a metadata version bump, or accidental admin action).
- [x] ~~Decide the fix approach~~ — **superseded, new fix approach identified 2026-07-02:** Neither original Option A (rename seeded data — moot, no mismatch) nor a data-only fix (add missing rules — moot, rules already exist and are enabled) applies. The real, actionable fix is **hardening the rule-lookup logic** in both `mmria_vitals_validate_field()` (chart.js) and the two independent range-checks inside `g_set_data_object_from_path()` (case/index.js — see Dev Notes) to key on the **full, form-qualified field path** instead of the short field name + `endsWith()` fallback. This removes the latent cross-form name-collision risk (confirmed live for `oxygen_saturation`, see Dev Notes) and the complete-silent-failure risk for fields with no naming collision (`systolic_bp`, `diastolic`) if their rule entry is ever missing.
  - [ ] ~~Option A: correct the seeded data~~ — moot, no naming mismatch exists
  - [ ] ~~Option B: normalize the lookup in `mmria_vitals_validate_field()` to tolerate both naming forms~~ — superseded by the broader fix below
  - [ ] **New Option C (recommended):** change the rule-lookup key from short field name to the full dictionary/object path (e.g., `/prenatal/routine_monitoring/systolic_bp`) consistently across all three duplicate validation call sites (see Dev Notes) so a field can only ever match its own form's rule — never another form's rule of the same short name, and never silently fall back to nothing
  - [ ] Record which option was chosen and why in Dev Notes
- [ ] Implement the chosen fix (harden rule-path matching across all three duplicate validation call sites — see Dev Notes)
  - [ ] Confirm no other form's field-name matching is affected by the correction
  - [ ] Specifically re-test that `oxygen_saturation` on Prenatal and ER Visits each validate using **their own** rule, not each other's (currently silently interchangeable — confirmed live 2026-07-02)
- [ ] Add the rule-key enumeration test (AC-4) — walks all seeded rule keys, asserts each resolves to a real field
- [ ] Add the rule-key resolution unit test (AC-5) — exercises `mmria_vitals_validate_field()`'s lookup logic directly, including a deliberately mismatched key case
- [ ] Data audit (blast-radius check, traceability): query existing cases for Prenatal Systolic BP / Diastolic BP values outside the configured range that were saved while this bug was live — flag any found for clinical/QA follow-up outside this story's dev scope; record the finding (even if "none found") in Dev Notes
- [ ] Add regression coverage
  - [ ] Playwright/manual test reproducing AC-1 (Prenatal Systolic BP and Diastolic BP validate and block out-of-range values)
  - [ ] Regression test confirming AC-3 (Heart Rate / Oxygen Saturation on Prenatal unaffected)
  - [ ] Regression test confirming AC-3a (all four Prenatal vitals fields get consistent indicator/modal treatment per Story 2.5)
  - [ ] Re-run Story 2.5's original acceptance scenarios (AC-6, referencing Story 2.5 AC #1–#6) — confirm all still pass

## Dev Notes

**Primary files (suspected):**

- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js` — `mmria_vitals_validate_field()` rule-lookup logic
- Seeded `case-validation-rules-{metadata_version}` CouchDB document / seeding script (see `Controllers/api/case_validationController.cs`)
- `source-code/mmria/mmria-server/database-scripts/validator.js` — field path map (`dictionary_path_to_path_map`) for reference on canonical field paths

**Suspected root cause:** ~~OI-4 (exact HTML `name` attributes for vitals inputs)... rule-key mismatch...~~ **Disproven 2026-07-02.** Static investigation (Dev/Amelia) confirmed `prenatal/routine_monitoring/diastolic` (no `_bp` suffix) is the correct, intentional field name — it matches exactly in the schema (`mmria_case.cs`), chart.js's own reference comment, and `default-ui-specification.json`. There is no naming mismatch to fix.

**Real likely cause (unconfirmed pending live check):** ~~Story 6.1 changed `CaseValidationManager.BuildDefaultRuleDocument`... rule entries for `prenatal/routine_monitoring/systolic_bp`/`diastolic` are missing or disabled in the live rules document...~~ **Disproven 2026-07-02, live check complete** — see "Update 2026-07-02 (live investigation)" below.

**Update 2026-07-02 (live investigation, browser session against running local dev server, authenticated as `user5`):**

1. **Live rules document check (resolves the prior open question):** `GET /api/case-validation/rules/current` was queried directly against the running server (metadata_version `26.01.20`). The document currently contains **11 field rules total**, including all 4 Prenatal vitals fields — `prenatal/routine_monitoring/heart_rate`, `systolic_bp`, `diastolic`, `oxygen_saturation` — **all with `enabled: true`**, structurally identical in shape to the working ER Visits rules. The "missing/disabled data" hypothesis is **disproven** in this environment.
2. **Confirmed the validation function itself works correctly for all 4 fields when the rule exists:** calling `mmria_vitals_validate_field()` directly against each of the 4 Prenatal input elements (with out-of-range test values) correctly showed the "Out of Range" modal with the correct field-specific message and threshold for **all four**, including Systolic BP and Diastolic BP. In this environment's current state, Story 2.7's reported symptom (BP silently not validating) **does not currently reproduce**.
3. **However, a real, confirmed, latent lookup-fragility defect was found** that fully explains how the reported symptom _would_ occur, and would recur if the Prenatal-specific rule entries are ever removed/disabled again:
   - `mmria_vitals_validate_field()`'s rule lookup (chart.js ~L283) — and **two separate, independent, duplicate range-checks inside `g_set_data_object_from_path()`** in `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` (~L1050 and ~L1152, not previously identified in this story or Story 2.6's investigation) — all key on the input's **short field name** (e.g. `"systolic_bp"`, `"oxygen_saturation"`) against a **flat, form-agnostic** `window.mmria_validation_rules` map, using a `key.endsWith('/' + fieldName)` fallback scan (one of the three call sites — the `g_set_data_object_from_path` "no validator registered" branch — instead keys on the exact dictionary path with no fallback at all, a third, inconsistent variant).
   - **Confirmed live, empirically:** temporarily disabling Prenatal's own `oxygen_saturation` rule via the API (`PUT api/case-validation/rules/{metadata_version}`) did **not** stop the Prenatal Oxygen Saturation field from showing the Out of Range modal — it silently fell back to and matched **ER Visits' identically-named `oxygen_saturation` rule** instead (both currently have the same 0–100 threshold, masking the leak). Prenatal's `systolic_bp`/`diastolic` have **no such cross-form name collision** (ER Visits uses `bp_systolic`/`bp_diastolic`), so when I disabled those two rules, they correctly received **zero fallback and zero validation** — exactly matching the originally reported bug's symptom.
   - **Conclusion:** the original bug report's asymmetry ("BP never validates, HR/O2Sat do") is best explained not by BP having a broken/mismatched key, but by BP having **no accidental cross-form naming safety net** while HR/O2Sat do (HR only partially — `heart_rate` doesn't collide with ER's `pulse`, so this is likely coincidental/environment-specific and should not be assumed stable). If the Prenatal BP rule entries are ever disabled/removed (as environments outside this session's local dev DB may currently be), BP would silently stop validating with zero fallback, while HR/O2Sat might mask the same underlying gap via accidental fallback to ER's rules of the same name — an unreliable, unintentional behavior, not a safety net that should be relied upon.
4. **Recommended fix (see Tasks):** make the rule lookup form/path-aware (key on the full field path, not the short name) across all three duplicate call sites, eliminating both the complete-silent-failure mode (BP) and the accidental-cross-form-borrowing mode (O2Sat) in one change.

**Scope boundary:** This story does NOT cover the data-retention/wipe defect on Prenatal Heart Rate / Oxygen Saturation or ER Visits — that is tracked separately in Story 2.6. Do not merge fixes across these two stories; they have different root causes.

**Fix approach constraint (team decision, 2026-07-02):** Pick exactly one of seeded-data correction or lookup normalization — not both. Doing both creates two overlapping code paths that can silently drift apart the next time a rule is seeded. The enumeration test (AC-4) is deliberately scoped to this bug fix only — it is not a request for a full startup-time rule-validation framework; that would be separate, larger scope if pursued later.

**Traceability note (deferred scope, team decision):** The Analyst flagged that a rule-key-to-field mismatch is a defect _class_, not a one-off typo, and argued the enumeration test (AC-4) should be a permanent CI-level integrity gate rather than a one-time test written to accompany this fix. The team's decision stands: AC-4/AC-5 in this story are scoped only to the Prenatal BP fix. Promoting the enumeration test to a standing CI gate covering all forms/all rules is tracked as a candidate follow-up story, not implemented here — flag it to the PM for backlog placement if not already captured.

### References

- Bug report filed 2026-07-02 (Prenatal Care Record — system incorrectly saving data that's over the max)
- [Source: 2-2-on-blur-vitals-validation-and-modal.md]
- [Source: validator.js#L3684-L3690 — prenatal/routine_monitoring field path map]

## Dev Agent Record

_To be completed by the developer during implementation._

### File List

_To be completed._

### Change Log

_To be completed._
