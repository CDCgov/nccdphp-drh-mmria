---
baseline_commit: e5009f9b79f61b736ae6e39facf8272c56817f4c
---

# Story 2.6: Fix Vitals Data Wipe on Re-Validation

**Epic:** Epic 2 — Vitals Field Validation (regression fix)
**Story ID:** 2.6
**Status:** ready-for-dev
**Date added:** 2026-07-02

---

## Story

As a case reviewer,
I want previously saved out-of-range vitals values to be retained when validation rules are re-enabled and I edit other fields,
So that historical data is never destroyed by a validation state change unrelated to that data.

## Acceptance Criteria

**AC-0 — Confirm the actual mechanism before implementing a fix (investigation gate)**

**Update 2026-07-02 (static investigation complete, live repro still required):** Both originally-named hypotheses have been **disproven** by direct code inspection (see Dev Notes for citations):

- `mmria_vitals_validate_field()`, `mmria_vitals_show_field_modal()`, and `validation-state.js`'s `evaluateHistoricalVitals` are all **read-only** — none of them write to `inputEl.value` or mutate `g_data`.
- `mmria_vitals_revalidate_all()` only scans `window.g_data.er_visit_and_hospital_medical_records` — it **never touches Prenatal data**, ruling it out as the cause of the Prenatal HR/O2Sat wipe specifically.

The true wipe trigger is still unconfirmed. Given static analysis is exhausted, the next step is a **live browser repro** (breakpoint on the vitals `input` elements, or a Playwright trace capturing DOM/`g_data` state before and after the wipe) to catch the mutation in the act — most likely candidates now are a full grid/chart re-render that rebuilds input DOM from `g_data`/metadata before a typed value is synced back, or a server-side save-time issue.

Given the two competing hypotheses for this defect:
(a) the `inputEl.value === inputEl.defaultValue` "untouched historical data" guard in `mmria_vitals_validate_field()` misidentifies historical data as "touched" after a re-render, or
(b) `mmria_vitals_revalidate_all()` (Story 2.5) itself mutates/loses sibling field values during its scan instead of only flagging them
When the developer investigates using the repro scenarios in AC-4
Then the story's Dev Notes are updated with the _confirmed_ mechanism (not a hypothesis) before the fix is written — the fix must target the confirmed mechanism, not just the first plausible guess

**Split contingency (traceability) — RESOLVED, no split needed:** Investigation confirmed ER Visits and Prenatal vitals inputs are wired through the same shared, form-agnostic code path in chart.js (a generic post-render scan that attaches blur/tab/paste listeners to any `input.number` element, regardless of form). Both forms call the same `mmria_vitals_validate_field(inputEl)`. This story remains a single fix; no Story 2.8 split is warranted on the call-path question.

**AC-1 — ER Visits & Hospitalizations (full-form regression, all 6 fields)**

Given validation rules for Temperature (0–110), Heart Rate (0–400), Respiration (0–60), Systolic BP (0–300), Diastolic BP (0–300), and Oxygen Saturation (0–100) are **disabled** on the Case Management Rule page
And a new case is created with out-of-range values entered for all six fields on the ER Visits & Hospitalization page while rules are disabled (Temperature=115, Heart Rate=405, Respiration=65, Systolic BP=301, Diastolic BP=340, Oxygen Saturation=107)
When the six rules are re-enabled and the reviewer returns to the same case and enters new out-of-range values (Temperature=115, Heart Rate=410, Respiration=67, Systolic BP=305, Diastolic BP=345, Oxygen Saturation=110)
Then the system displays the correct out-of-range error messages for the newly entered values
And the previously saved out-of-range values for all six fields are **retained** — not cleared/wiped

**AC-2 — Prenatal Care Record (partial-field regression, Heart Rate + Oxygen Saturation)**

Given validation rules for Systolic BP (0–300), Diastolic BP (0–300), Heart Rate (0–400), and Oxygen Saturation (0–100) are **disabled** on the Case Management Rule page
And the Prenatal Care Record page is completed with out-of-range values for all four fields while rules are disabled (Systolic BP=305, Diastolic BP=320, Heart Rate=405, Oxygen Saturation=105)
When the four rules are re-enabled and the reviewer enters new out-of-range values (Systolic BP=307, Diastolic BP=327, Heart Rate=407, Oxygen Saturation=107)
Then the system displays the correct out-of-range error messages for Heart Rate and Oxygen Saturation
And the previously saved out-of-range values for Heart Rate and Oxygen Saturation are **retained** — not cleared/wiped

**AC-3 — No regression to existing clear-on-active-edit behavior**

Given a vitals field currently displays a value the reviewer is actively typing/pasting into (not historical/untouched data)
When that field's new value is out-of-range on blur/tab-out/paste
Then the existing Story 2.2 behavior (clear the actively-edited field, show the field-level modal) continues to apply unchanged — this fix must not disable or weaken active-input validation
**AC-4 — Isolate the trigger: same-session vs. reload-between-saves**

Given the AC-1/AC-2 repro steps
When the developer runs the repro twice — once entirely within one page session (no reload between the disabled-rules save and the re-enabled-rules edit), and once with a full page reload in between
Then the results of both runs are recorded in the story; if the wipe only reproduces in one scenario, that confirms which of AC-0's two hypotheses is correct and scopes the fix accordingly

**AC-5 — Retention display contract, and flag visibility/persistence**

Given a historical out-of-range value is retained per AC-1/AC-2
When the case form renders that value after the fix
Then it is displayed visible in the input, flagged via the red record indicator (Story 2.5 AC #4/#5) and validation errors panel — never silently shown as if valid and never cleared
And the flag/indicator survives save and page reload (it is not a one-time or session-only signal)
And the flag is visually distinguishable from a generic required-field/empty-field error, not just a shared "red text" style — a reviewer must be able to tell at a glance that this is an out-of-range historical value, not a missing-field error

**Traceability note:** Story 2.5's acceptance criteria (#1–#6, see `2-5-historical-data-detection-and-indicators.md`) specify _detection and indicator display_ (modal on edit-mode entry/form-nav, red record indicator) but contain **no explicit acceptance criterion requiring that out-of-range values are never cleared**. That "never clear" behavior was an implicit assumption carried over from Story 2.2's design intent, not a tested contract — which is very likely why this regression shipped without being caught. AC-1/AC-2/AC-5 in this story codify "must retain, never clear" as an explicit, testable requirement for the first time. This gap should also be reflected retroactively as a note on Story 2.5 once this story is accepted.

**AC-6 — Story 2.5 regression gate**

Given Story 2.5's original acceptance scenarios (AC #1 edit-mode entry re-validation, AC #2 form-navigation re-validation, AC #3 historical modal, AC #4/#5 red indicator display and re-evaluation, AC #6 null-range no-op)
When this fix is applied
Then all six of Story 2.5's original acceptance criteria still pass unchanged — this fix must not weaken or bypass historical-data flagging to solve the retention problem

## Tasks / Subtasks

- [ ] Reproduce the defect locally using the exact repro steps in AC-1 and AC-2
- [ ] Isolate the trigger per AC-4: run the repro both same-session (no reload) and with a reload between saves; record which reproduces the wipe
- [x] ~~Trace the ER Visits and Prenatal call paths independently and confirm whether they share code~~ — **done, confirmed shared** (2026-07-02 static investigation): both forms wire vitals inputs through the same generic chart.js post-render scan calling the same `mmria_vitals_validate_field()`. No split needed.
- [x] ~~Root-cause the wipe via static inspection~~ — **done, inconclusive at the time**: `mmria_vitals_validate_field()`, `mmria_vitals_show_field_modal()`, `mmria_vitals_revalidate_all()`, and `validation-state.js`'s `evaluateHistoricalVitals` are all confirmed read-only (no `inputEl.value`/`g_data` mutation found anywhere in these functions). `mmria_vitals_revalidate_all()` is additionally confirmed to never touch Prenatal data at all. Both AC-0's named hypotheses are disproven. Static code reading (limited to chart.js/validation-state.js) was exhausted at that point — superseded by the live investigation below, which found the real mutation site outside those two files.
- [x] ~~**Live repro (required next step):** capture the wipe in the act using a browser breakpoint or Playwright trace~~ — **done 2026-07-02, partial success (see Dev Notes "Update 2026-07-02 (live investigation)"):**
  - [x] Instrumented via Playwright against the running local dev server: disabled the 4 Prenatal vitals rules via the `case-validation` API, entered out-of-range values on all 4 fields, saved, re-enabled the rules via the API, reloaded, then edited **one** field (Heart Rate) to a new out-of-range value and captured DOM + `g_data` for all 4 fields before/after.
  - [x] **New root-cause site found, previously undocumented in this story:** `g_set_data_object_from_path()` in `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` (~L1018-1175) — the function actually wired to every vitals input's `onblur` attribute (not chart.js) — contains **two more independent, duplicate range-validation implementations** beyond the three chart.js/validation-state.js functions already investigated, each with its own (inconsistent) rule-lookup strategy. See Dev Notes for full detail.
  - [x] **Confirmed live:** after a field's blur commits a value via `g_set_data_object_from_path`, that field's specific DOM container is rebuilt via a targeted `innerHTML` re-render (`page_render(...)` call ~L1105-1120). The freshly-rebuilt `<input>` (a) loses its `data-vitals-validation-attached` marker (chart.js's blur/tab/paste listener is **not** re-attached to the new element), and (b) has its `defaultValue` **rebased** to the just-committed value instead of retaining the original historical baseline — meaning chart.js's `inputEl.value === inputEl.defaultValue` "historical/untouched" guard silently loses its true baseline after every single edit.
  - [x] In this specific run, sibling-field **data** (systolic_bp/diastolic/oxygen_saturation) was **not** wiped when Heart Rate alone was edited — all 4 values were retained correctly in both DOM and `g_data` after the edit+save. The exact data-wipe has not yet been caught in the act, but the mechanism above is a strong, concrete, evidence-backed candidate, and the third duplicate validation path found (the "no validator registered" branch, ~L1152-1167) contains a line that **blanks the input's DOM value to `''` without ever calling `set_object_value_by_full_path`** to sync that change to `g_data` — a concrete, confirmed candidate for a client-side wipe that is one step closer than the previous hypotheses (see Dev Notes for the exact conditions under which this branch executes).
  - [ ] **Still open:** determine exactly which fields/paths take the "no validator registered" branch (vs. the "validator passes" branch) — this determines which fields are exposed to the silent-blank-without-sync risk, and is required before writing the fix
  - [ ] Confirm whether the actual reported wipe (sibling field showing wiped/blank after re-validation) is this silent-blank-without-sync behavior compounding across multiple field edits in one session (each edit re-rendering and re-basing a different field, progressively losing attached listeners across the whole grid) rather than a single-field, single-edit event
- [ ] Implement the fix so that:
  - [ ] Only the field the reviewer is actively editing can ever be cleared (per Story 2.2 AC #1)
  - [ ] Historical / sibling-field out-of-range values are never cleared by re-validation, edit-mode entry, or form navigation (per Story 2.5 scope — display-time flagging only)
  - [ ] Retained historical values continue to render per the retention/flag-visibility contract in AC-5, including surviving save/reload and being visually distinct from a required-field error
  - [ ] **New:** consolidate or at least reconcile the three duplicate, independently-drifting range-validation implementations (`mmria_vitals_validate_field()` in chart.js; the two branches inside `g_set_data_object_from_path()` in case/index.js) so a single authoritative rule-lookup and a single clear/retain decision governs all vitals fields, instead of three code paths that can each reach a different conclusion for the same field
  - [ ] **New:** ensure the targeted single-field re-render triggered from `g_set_data_object_from_path()` re-attaches (or does not require) the chart.js blur/tab/paste listener, and does not rebase `defaultValue` away from the true historical/persisted baseline
- [ ] Add regression coverage
  - [ ] Playwright/manual test reproducing AC-1 (ER Visits, all 6 fields retained) — tested independently, not assumed to share a fix with Prenatal
  - [ ] Playwright/manual test reproducing AC-2 (Prenatal, Heart Rate + Oxygen Saturation retained) — tested independently, not assumed to share a fix with ER Visits
  - [ ] Regression test confirming AC-3 (active-edit clear-on-blur still works)
  - [ ] Test confirming AC-5's flag survives save + reload and is visually distinct from a required-field error
  - [ ] Re-run Story 2.5's original acceptance scenarios (AC-6, referencing Story 2.5 AC #1–#6) — edit-mode entry, form-navigation re-validation, red indicator, historical modal — confirm all still pass

## Dev Notes

**Primary files (suspected):**

- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js` — `mmria_vitals_validate_field()`, `mmria_vitals_revalidate_all()`, `mmria_vitals_show_field_modal()`
- `source-code/mmria/mmria-server/wwwroot/scripts/validation/validation-state.js` — `focusout` delegation calling `mmria_vitals_validate_field`
- **`source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` — `g_set_data_object_from_path()` (~L1018-1175, added 2026-07-02 live investigation)** — the actual `onblur`-wired data-commit function for every vitals input; contains two more independent, duplicate range-validation branches plus the targeted single-field re-render that rebuilds each edited input's DOM node

**Suspected root cause:** ~~The `inputEl.value === inputEl.defaultValue` guard in `mmria_vitals_validate_field()`...~~ **Disproven 2026-07-02.** Static investigation (Dev/Amelia) confirmed every vitals-adjacent function in chart.js/validation-state.js is read-only. **Superseded 2026-07-02 (live investigation) — see "Update 2026-07-02 (live investigation)" below for the confirmed new candidate mechanism, found in a third file (`case/index.js`) not covered by the earlier static pass.**

**Update 2026-07-02 (live investigation, browser session against running local dev server, authenticated as `user5`):**

The earlier static investigation only examined `chart.js` and `validation-state.js`. It did **not** examine `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`, which turns out to contain the actual `onblur` handler wired directly onto every vitals `<input>` element (`onblur="g_set_data_object_from_path(...)"`) — a separate mutation path from chart.js's listener-attached validation entirely.

`g_set_data_object_from_path()` (case/index.js ~L1018-1175) contains **three total range-validation code paths** across the whole codebase when counting chart.js's `mmria_vitals_validate_field()`:

1. **chart.js `mmria_vitals_validate_field()`** (already investigated, read-only, attached via a generic post-render scan).
2. **`g_set_data_object_from_path()`, "validator passes" branch** (~L1044-1064): saves the new value to `g_data` **first**, then does its own separate rule lookup (short field name + `endsWith()` fallback against `window.mmria_validation_rules` — same fragile pattern flagged in Story 2.7's Dev Notes), shows the Out-of-Range modal if violated, but per its own comment ("restore the old value and show the modal") does **not actually restore/revert** the already-saved out-of-range value in `g_data`. It then calls `set_local_case(...)` whose callback does a **targeted single-field DOM re-render**: `document.getElementById(convert_object_path_to_jquery_id(p_object_path)).innerHTML = page_render(...)`, rebuilding that field's `<input>` from scratch.
3. **`g_set_data_object_from_path()`, "no validator registered" branch** (~L1152-1167): a **third, independent** rule lookup, this time keyed on the **exact** dictionary path (no `endsWith()` fallback at all — inconsistent with both other paths). If the value is out of range **and** differs from the currently-stored value, it sets `_ctrlEl.value = ''` (blanks the DOM input directly) and shows the modal, then `return`s **without ever calling `set_object_value_by_full_path()`** — meaning `g_data` is never touched by this branch, but the visible DOM value is blanked.

**Confirmed live via Playwright, empirically:** after any field's blur commits through path #2 above, that field's specific `<input>` element is a **newly created DOM node** (not the same node that had the blur listener attached) — confirmed via a `data-vitals-validation-attached` marker attribute: present (`"1"`) before the edit, **absent (`null`) on the post-edit element**. The new element's `defaultValue` also reads back as the **just-committed** value, not the original historical baseline, confirming the element was rebuilt with a fresh `value` attribute rather than mutated in place.

**Practical implication:** every single vitals-field blur silently "uses up" that field's chart.js blur/tab/paste listener (the rebuilt element doesn't have it) and rebases what chart.js considers "historical/untouched" to whatever was just committed. Across a multi-field editing session (exactly AC-1/AC-2's repro shape — edit several fields in sequence), each edit potentially invalidates the listener state of the field(s) edited before it, and shifts each edited field's own historical baseline forward. This is a strong, concrete, now-evidenced explanation for why retention/flagging becomes unreliable after a validation-rule re-enable + multi-field edit sequence, though the exact data-wipe (a sibling field's **value**, not just its listener/baseline, disappearing) was not caught in this specific single-field-edit test run — values for all 4 fields were retained correctly on this attempt. The strongest remaining lead is path #3 above (the silent `_ctrlEl.value = ''` with no `g_data` sync) compounding across a longer multi-field sequence than was tested in this session.

**Still needed before the fix is written:** determine which vitals fields/paths take branch #2 ("validator passes") vs. branch #3 ("no validator registered") — this depends on whether `g_validator_map[p_metadata_path]` has an entry for that path, which was not enumerated in this session — and run the AC-1/AC-2 repro across a longer multi-field sequence (edit all 6/4 fields in the same session, not just one) to try to trigger an actual sibling-value loss, not just listener/baseline drift.

**Call path — confirmed shared:** ER Visits and Prenatal vitals inputs are both wired via a single generic post-render scan in chart.js that attaches blur/tab/paste listeners to any `input.number` element near a just-rendered chart, regardless of form (~L866-877). Both forms call the same `mmria_vitals_validate_field(inputEl)`. This is one shared mechanism — no split into a separate story is warranted on the call-path question.

**Related stories:** Story 2.2 (On-Blur Vitals Validation) and Story 2.5 (Historical Data Detection) — this story fixes a regression spanning both; do not re-implement either from scratch, only correct the retention behavior.

**Scope boundary:** This story does NOT cover Prenatal Care Record Systolic BP / Diastolic BP failing to validate at all — that is a separate defect tracked in Story 2.7.

### References

- Bug reports filed 2026-07-02 (ER Visits & Hospitalizations wipe; Prenatal Care Record partial wipe)
- [Source: 2-2-on-blur-vitals-validation-and-modal.md]
- [Source: 2-5-historical-data-detection-and-indicators.md]

## Dev Agent Record

_To be completed by the developer during implementation._

### File List

_To be completed._

### Change Log

_To be completed._
