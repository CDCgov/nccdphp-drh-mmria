# Story 2.3: Display-Time Exclusion — Print, PDF, and Vitals Date Fix

Status: done

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

- [x] Identify print view rendering path (AC: #1)
  - [x] Determine whether print view is rendered server-side (Razor) or client-side JS
  - [x] Find where vitals field values are written to print output
  - [x] Add out-of-range check before each vitals value is rendered: if `window.mmria_vital_sign_range` and value is out of range → render empty string
- [x] Identify PDF rendering path for vitals values (AC: #2)
  - [x] In `wwwroot/scripts/pdf-version/index.js`, find where vitals field values are rendered
  - [x] Add the same out-of-range check pattern: check against `window.mmria_vital_sign_range`, substitute empty string if out of range
- [x] Fix vitals date `/ /` display in PDF (AC: #3)
  - [x] In `wwwroot/scripts/pdf-version/index.js`, find where vitals date fields are formatted/rendered
  - [x] Identify the code path that produces `/ /` for empty or invalid dates
  - [x] Replace `/ /` output with empty string for vitals date fields — scope fix to vitals date rendering only
- [x] Confirm case form unaffected (AC: #4)
  - [x] Verify no change made to `chart.js` rendering of in-form input values
  - [x] Input fields continue to show stored value
- [x] Null-guard all exclusion checks (AC: #5)
  - [x] Every out-of-range check: `if (window.mmria_vital_sign_range) { /* check */ }` — if null, skip and render value as-is

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
Claude Sonnet 4.6

### Debug Log References
- Print view is client-side JS rendered via `wwwroot/scripts/print-version/print_version_renderer.js` — `print_version_render()` function, `default:` case handles number/string/time fields.
- PDF vitals rendering is in `wwwroot/scripts/pdf-version/index.js` in the `vital_signs` / `transport_vital_signs` grid branch (~line 2293).
- `fmtDateTime()` at line 675 was the source of `  /  /    ` output for empty vitals dates.
- **Root cause of suppression not working**: Both `/print-version/index.html` and `/pdf-version/index.html` are standalone static HTML pages opened via `window.open()` from the case editor. They do NOT receive `window.mmria_vital_sign_range` (which is only set in the Razor-served Case/Index.cshtml). Fix: pass the config as an additional `p_vital_sign_range` parameter through `openTab` → `create_print_version`, and set it on `window.mmria_vital_sign_range` in the child window before rendering.
- Build failed with file-lock error (MSB3027/MSB3021) — pre-existing environment issue, running debug server holds `mmria.common.dll`. C# compilation itself succeeded. JS files unaffected by .NET build.

### Completion Notes List
- Print view is client-side JS. No Razor involvement for vitals fields.
- Added `mmria_vitals_is_out_of_range()` helper to both `pdf-version/index.js` and `print-version/print_version_renderer.js`.
- Added `fmtDateTimeVitals()` to `pdf-version/index.js` — returns `''` for blank/invalid dates; delegates to `fmtDateTime()` otherwise. Used only for the vitals date column to scope the fix.
- Out-of-range check applied to `string`/`number`/`time`/`hidden` fields in the PDF vitals grid loop. If out of range → `''`; otherwise existing `|| '-'` fallback preserved.
- Out-of-range check applied in the `default:` case of `print_version_render()` — safe because `mmria_vitals_is_out_of_range` returns `false` for any field not in the range config.
- No changes to `chart.js` or any case form input rendering.

### File List
- `source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/print-version/print_version_renderer.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/print-version/index.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`

### Change Log
- `pdf-version/index.js`: Added `fmtDateTimeVitals()` and `mmria_vitals_is_out_of_range()` helper functions after `fmtStrDate`.
- `pdf-version/index.js`: Changed vitals date column from `fmtDateTime()` to `fmtDateTimeVitals()`.
- `pdf-version/index.js`: Applied `mmria_vitals_is_out_of_range` check in `string`/`number`/`time`/`hidden` case of vitals grid loop.
- `pdf-version/index.js`: Added `p_vital_sign_range` parameter to `create_print_version`; sets `window.mmria_vital_sign_range` on the child window before rendering.
- `print-version/print_version_renderer.js`: Added `mmria_vitals_is_out_of_range()` helper at end of file.
- `print-version/print_version_renderer.js`: Applied check in `default:` case of `print_version_render()` for the non-`case_opening_overview` branch.
- `print-version/index.js`: Added `p_vital_sign_range` parameter to `create_print_version`; sets `window.mmria_vital_sign_range` on the child window before rendering.
- `case/index.js`: Both `openTab` `create_print_version` call sites now pass `window.mmria_vital_sign_range` as the 8th argument.
