# Story 2.3: Display-Time Exclusion — Print, PDF, and Vitals Date Fix

Status: ready-for-dev

## Story

As a CDC analyst reviewing submitted case data,
I want out-of-range vitals values to appear as blank in printed reports and PDFs,
so that printed output does not surface unreliable data.

## Acceptance Criteria

1. A vitals field value outside the configured range renders as empty string in print view — stored database value is not affected.
2. The same out-of-range value renders as empty string in PDF output — stored value unchanged.
3. The PDF rendering path for vitals date fields currently outputs `/ /` for empty or invalid dates — this is replaced with empty string. Scoped to vitals date fields in the PDF rendering path only.
4. The case form input field continues to display the stored value unchanged — exclusion is display-time only.
5. If `window.mmria_vital_sign_range` is `null`, all values render normally — no exclusion applied.

## Tasks / Subtasks

- [ ] Identify print view rendering path (AC: #1)
  - [ ] Determine whether print view is rendered server-side (Razor) or client-side JS
  - [ ] Find where vitals field values are written to print output
  - [ ] Add out-of-range check before each vitals value is rendered: if `window.mmria_vital_sign_range` and value is out of range → render empty string
- [ ] Identify PDF rendering path for vitals values (AC: #2)
  - [ ] In `wwwroot/scripts/pdf-version/index.js`, find where vitals field values are rendered
  - [ ] Add the same out-of-range check pattern: check against `window.mmria_vital_sign_range`, substitute empty string if out of range
- [ ] Fix vitals date `/ /` display in PDF (AC: #3)
  - [ ] In `wwwroot/scripts/pdf-version/index.js`, find where vitals date fields are formatted/rendered
  - [ ] Identify the code path that produces `/ /` for empty or invalid dates
  - [ ] Replace `/ /` output with empty string for vitals date fields — scope fix to vitals date rendering only
- [ ] Confirm case form unaffected (AC: #4)
  - [ ] Verify no change made to `chart.js` rendering of in-form input values
  - [ ] Input fields continue to show stored value
- [ ] Null-guard all exclusion checks (AC: #5)
  - [ ] Every out-of-range check: `if (window.mmria_vital_sign_range) { /* check */ }` — if null, skip and render value as-is

## Dev Notes

**Primary files:**
- `wwwroot/scripts/pdf-version/index.js` — PDF output + vitals date fix
- Print view rendering path — identify during implementation (may be server-side Razor or client-side JS)

**Prerequisite:** Story 2.1 must be complete — `window.mmria_vital_sign_range` must be available.

**Out-of-range check pattern:**
```javascript
function mmria_vitals_is_out_of_range(fieldName, value) {
    if (!window.mmria_vital_sign_range) return false;
    var range = window.mmria_vital_sign_range[fieldName];
    if (!range) return false;
    var v = parseFloat(value);
    if (value === '' || isNaN(v)) return false;
    return (v < parseFloat(range.min) || v > parseFloat(range.max));
}
```
Apply this pattern (or inline equivalent) at each render site.

**Vitals date fix:** The existing PDF view outputs `/ /` for vitals dates that are empty or invalid. Find the specific code path in `pdf-version/index.js` that formats vitals date values and produces this output. Replace the `/ /` result with an empty string. Do not change date formatting logic for non-vitals date fields.

**DB storage rule:** Out-of-range values are saved to CouchDB as-is. No special handling on the save path. All exclusion is render-time only.

**Note on `pdf-version/index.js`:** This file is also modified in Story 3.3 (core-summary dead code removal). If both stories are worked sequentially, coordinate on branch management — the changes are in different sections of the file and should not conflict.

**If print view is server-side (Razor):** The out-of-range check must be done client-side or the range config must be passed to the server. Check existing architecture for how the print view is served before proceeding — if server-side Razor, raise this as a question before implementing.

### Project Structure Notes

- Primary change: `wwwroot/scripts/pdf-version/index.js`
- Secondary: print view path (identify during implementation)
- No new files expected

### References

- [Source: architecture-mmria-v4.1.md#2.6 — Display-time exclusion table]
- [Source: architecture-mmria-v4.1.md#2.7 — PDF date fix]
- [Source: architecture-mmria-v4.1.md#2.10 — DB storage (as-is)]
- [Source: prd-mmria-2026-06-12/prd.md#FR-2.4]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
