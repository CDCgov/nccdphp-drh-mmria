# Story 2.2: On-Blur Vitals Validation and Invalid Entry Modal

Status: ready-for-dev

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

- [ ] Resolve OI-4 before writing validation (AC: #7)
  - [ ] Inspect the DOM rendered by `chart.js` for a vitals grid — identify the exact HTML `name` attribute values on vitals input fields
  - [ ] Confirm whether an Oxygen Saturation field exists in the vitals grids
  - [ ] Update `vital_sign_range` config doc keys (from Story 2.1) to match confirmed `name` attributes
- [ ] Identify scope: grids with graph/table toggle (AC: #7)
  - [ ] In `chart.js`, find the code that renders the graph/table toggle control
  - [ ] Use the toggle's presence as the selector to identify which grids are in scope
  - [ ] Do not hardcode a list of form names or field names
- [ ] Implement `mmria_vitals_validate_field(inputElement)` in `chart.js` (AC: #1–#6)
  - [ ] Null-check `window.mmria_vital_sign_range` → return if null
  - [ ] Look up `window.mmria_vital_sign_range[inputElement.name]` → return if no range entry
  - [ ] Parse `parseFloat(inputElement.value)` — skip if empty string or NaN
  - [ ] If value < range.min or value > range.max: clear field, call modal function
  - [ ] Modal function passes `range.label`, `range.min`, `range.max` to the existing site modal
- [ ] Attach events using post-render pattern (AC: #7)
  - [ ] Use `p_post_html_render.push(function() { ... })` pattern to attach after DOM insertion
  - [ ] Attach to blur event on each in-scope vitals input
  - [ ] Attach to keydown: check `event.key === 'Tab'` then call validate
  - [ ] Attach to paste: call validate on `setTimeout` (after paste content lands)
- [ ] Implement modal display (AC: #2, #3)
  - [ ] Find the existing site modal invocation pattern (search for existing modal usage in `chart.js` or `case/index.js`)
  - [ ] Reuse that pattern — do not create a new modal
  - [ ] Message format: `"The value entered for the {range.label} field falls outside of the permitted range. Please enter a valid input between {range.min}–{range.max}."`
  - [ ] On modal OK dismiss: `inputElement.focus()`
- [ ] Verify save paths are unaffected (AC: #6)
  - [ ] Confirm no validation call added to save, Save & Continue, Save & Finish, or autosave handlers

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

### Debug Log References

### Completion Notes List

### File List
