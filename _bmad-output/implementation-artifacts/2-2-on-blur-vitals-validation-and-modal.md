---
baseline_commit: 1ebbb6482c05fbb7a9f2aacf61e1e30cf98174fe
---

# Story 2.2: On-Blur Vitals Validation and Invalid Entry Modal

Status: done

## Story

As a case reviewer,
I want to be immediately alerted when I enter a vitals value outside the permitted range,
so that out-of-range values never silently enter the form and I can correct them before saving.

## Acceptance Criteria

1. When a reviewer leaves a vitals field (blur, tab-out, or paste) with a value outside `[min, max]` for that field, the field value is cleared to empty string.
2. When a vitals field is cleared per AC #1, a modal displays: "The value entered for the [field label] field falls outside of the permitted range. Please enter a valid input between {min}–{max}." using the existing site modal pattern (purple header, OK button).
3. On modal dismiss, focus returns to the cleared field (NFR-2).
4. If `window.mmria_vital_sign_range` is `null`, no validation runs and no modal appears.
5. If the field value is empty or not a parseable number, no validation runs.
6. Save & Continue, Save & Finish, and autosave have no validation behavior — they save current field state as-is.
7. The validation attaches to blur, keydown (Tab key), and paste events on every vitals input in scope — scope is defined by the presence of the graph/table toggle on the same grid.

## Tasks / Subtasks

- [x] Resolve OI-4 before writing validation (AC: #7)
  - [x] Inspect the DOM rendered by `chart.js` for a vitals grid — identify the exact HTML `name` attribute values on vitals input fields
  - [x] Confirm whether an Oxygen Saturation field exists in the vitals grids
  - [x] Update `vital_sign_range` config doc keys (from Story 2.1) to match confirmed `name` attributes
- [x] Identify scope: grids with graph/table toggle (AC: #7)
  - [x] In `chart.js`, find the code that renders the graph/table toggle control
  - [x] Use the toggle's presence as the selector to identify which grids are in scope
  - [x] Do not hardcode a list of form names or field names
- [x] Implement `mmria_vitals_validate_field(inputElement)` in `chart.js` (AC: #1–#6)
  - [x] Null-check `window.mmria_vital_sign_range` → return if null
  - [x] Look up `window.mmria_vital_sign_range[inputElement.name]` → return if no range entry
  - [x] Parse `parseFloat(inputElement.value)` — skip if empty string or NaN
  - [x] If value < range.min or value > range.max: clear field, call modal function
  - [x] Modal function passes `range.label`, `range.min`, `range.max` to the existing site modal
- [x] Attach events using post-render pattern (AC: #7)
  - [x] Use `p_post_html_render.push(function() { ... })` pattern to attach after DOM insertion
  - [x] Attach to blur event on each in-scope vitals input
  - [x] Attach to keydown: check `event.key === 'Tab'` then call validate
  - [x] Attach to paste: call validate on `setTimeout` (after paste content lands)
- [x] Implement modal display (AC: #2, #3)
  - [x] Find the existing site modal invocation pattern (search for existing modal usage in `chart.js` or `case/index.js`)
  - [x] Reuse that pattern — do not create a new modal
  - [x] Message format: `"The value entered for the {range.label} field falls outside of the permitted range. Please enter a valid input between {range.min}–{range.max}."`
  - [x] On modal OK dismiss: `inputElement.focus()`
- [x] Verify save paths are unaffected (AC: #6)
  - [x] Confirm no validation call added to save, Save & Continue, Save & Finish, or autosave handlers

## Dev Notes

**Primary file:** `wwwroot/scripts/editor/page_renderer/chart.js`

**Prerequisite:** Story 2.1 must be complete — `window.mmria_vital_sign_range` must be available.

**OI-4 must be resolved before this story:** The `name` attributes on vitals inputs must be known before the config keys and validation logic can be wired correctly.

**Validation function reference (from architecture §2.5):**
```javascript
function mmria_vitals_validate_field(inputElement) {
    if (!window.mmria_vital_sign_range) return;
    var fieldName = inputElement.name; // must match config key — confirmed via OI-4
    var range = window.mmria_vital_sign_range[fieldName];
    if (!range) return;
    var value = parseFloat(inputElement.value);
    if (inputElement.value !== '' && !isNaN(value) &&
        (value < parseFloat(range.min) || value > parseFloat(range.max))) {
        inputElement.value = '';
        mmria_vitals_show_field_modal(range);
    }
}
```

**Post-render pattern** (architecture §4.2):
```javascript
p_post_html_render.push(function() {
    // attach events here after DOM insertion
});
```

**Scope detection:** Identify vitals inputs by the presence of the graph/table toggle on the same grid — do not hardcode a form list. The same validation attachment point covers all in-scope grids.

**Existing modal pattern:** Search `chart.js` and `case/index.js` for existing modal invocations (likely a function such as `show_modal(title, message, callback)` or similar). Use that exact pattern — do not introduce a new modal implementation.

**Section 508 (NFR-2):** On modal dismiss, focus must return to the cleared field. This is the `callback` or `onclose` handler of the existing modal.

**Save path safety:** The architecture explicitly states save & autosave have NO special validation behavior. Do not add validation calls to any save handler.

**No new libraries.** Vanilla JS only — follow existing patterns in `wwwroot/scripts`.

### Project Structure Notes

- Change is entirely within `wwwroot/scripts/editor/page_renderer/chart.js`
- No new files created
- No build step required for JS changes

### References

- [Source: architecture-mmria-v4.1.md#2.5 — Field-level validation triggers and behavior]
- [Source: architecture-mmria-v4.1.md#2.9 — Section 508 modal — field-level use]
- [Source: architecture-mmria-v4.1.md#4.2 — Client-side implementation patterns]
- [Source: architecture-mmria-v4.1.md#2.1 — Scope: grids with graph/table toggle]
- [Source: prd-mmria-2026-06-12/prd.md#FR-2.1, FR-2.2]

## Dev Agent Record

### Agent Model Used
Claude Sonnet 4.6

### Debug Log References
- Build succeeded with 0 errors (85 pre-existing warnings only; file-lock warning is due to running server process, not a code error)

### Completion Notes List
- OI-4 resolved: confirmed field `name` attributes from `home_record.json` metadata — `temperature`, `pulse`, `respiration`, `bp_systolic`, `bp_diastolic`. No `oxygen_saturation` field exists in either vitals grid.
- `VitalSignRangeHelper.cs` updated: return type changed from flat `Dictionary<string, string>` to `Dictionary<string, VitalSignRangeEntry>` (with `Min`, `Max`, `Label`). Supports both nested per-field JSON (new format) and flat key JSON (existing DB format) via `BuildFromFlatKeys`. Defaults keyed by confirmed `name` attributes.
- `mmria_vitals_validate_field(inputEl)` added to `chart.js`: guards on null range, empty value, NaN; clears field and shows modal on out-of-range.
- `mmria_vitals_show_field_modal(range, inputEl)` added to `chart.js`: follows `offline-modals.js` pattern exactly — `insertAdjacentHTML('beforeend', ...)`, `setTimeout` fade-in, DOM removal on close, Escape key support, focus return to cleared field via closure.
- Event attachment IIFE pushed to `p_post_html_render` at end of `chart_render()`: finds all `input.number` elements in the chart div's parent container, guards against duplicate attachment via `dataset.vitalsValidationAttached`, attaches blur/keydown(Tab)/paste events.
- Scope detection: uses `chartEl.parentElement` to find the containing form section — the vitals grid fieldset and chart divs are siblings in the same parent. No form names hardcoded.
- Save paths confirmed unaffected: `mmria_vitals_validate_field` not present in `case/index.js` save handlers.

### File List
- `source-code/mmria/mmria-server/util/VitalSignRangeHelper.cs` — updated (new `VitalSignRangeEntry` type, nested per-field return structure, flat-key fallback parser)
- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js` — updated (`mmria_vitals_validate_field`, `mmria_vitals_show_field_modal` functions added; event-attachment IIFE pushed to `p_post_html_render` in `chart_render`)

### Change Log
| Date | Change |
|---|---|
| 2026-06-15 | Implemented Story 2.2: on-blur vitals validation, out-of-range modal with focus return, event attachment via post-render pattern; updated VitalSignRangeHelper to produce per-field nested structure |
