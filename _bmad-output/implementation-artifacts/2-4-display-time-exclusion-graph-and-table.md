# Story 2.4: Display-Time Exclusion — Graph and Table Views

Status: done

## Story

As a case reviewer,
I want out-of-range vitals values excluded from graphs and tables in the case form,
so that visual trends and tabular summaries only reflect clinically plausible data.

## Acceptance Criteria

1. An out-of-range vitals value is not plotted in the graph view — no data point and no line segment connecting to/from it.
2. An out-of-range vitals value renders as empty string in the table view cell — not the raw value.
3. The case form input field for that same record continues to display the stored value — exclusion is display-time only, not stored.
4. If `window.mmria_vital_sign_range` is `null`, all values render normally — no exclusion applied.

## Tasks / Subtasks

- [x] Locate graph rendering in `chart.js` (AC: #1)
  - [x] Find the function(s) in `chart.js` that plot data points onto the graph
  - [x] Identify where the vitals value is read before being passed to the charting logic
  - [x] Add out-of-range check: if value is out of range, skip that data point entirely (no point plotted, no line to/from it)
- [x] Locate table rendering in `chart.js` (AC: #2)
  - [x] Find the function(s) that render the tabular view of vitals records
  - [x] Identify where each cell value is written
  - [x] Add out-of-range check: if value is out of range, render empty string instead
- [x] Confirm input fields unaffected (AC: #3)
  - [x] Verify the case form input field rendering path is not touched by these changes
  - [x] Input fields continue to show stored value regardless of range
- [x] Null-guard all checks (AC: #4)
  - [x] Wrap every exclusion check with `if (window.mmria_vital_sign_range) { ... }`
  - [x] If null: render all values normally in graph and table

## Dev Notes

**Primary file:** `wwwroot/scripts/editor/page_renderer/chart.js`

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

If this function was already defined in Story 2.2 or 2.3, reuse it — do not duplicate.

**Graph exclusion:** When a data point is out of range, the point must be omitted entirely from the dataset passed to the charting library. Also omit any line segment that would connect to/from the omitted point — do not leave a gap in the line that visually indicates a missing point unless that is the library's default behavior.

**Table exclusion:** Replace the out-of-range value with `""` (empty string) before writing the cell content — do not apply CSS hide or strike-through, just an empty cell.

**Three-way split:** For each vitals record:
- Graph/table: exclude out-of-range (this story)
- Case form input: show stored value (no change — this story confirms no regression)
- Print/PDF: exclude out-of-range (Story 2.3)

**DB storage rule:** Out-of-range values are saved to CouchDB as-is. No special handling on the save path.

### Project Structure Notes

- Change is entirely within `wwwroot/scripts/editor/page_renderer/chart.js`
- No new files created
- No build step required for JS changes

### References

- [Source: architecture-mmria-v4.1.md#2.6 — Display-time exclusion table]
- [Source: architecture-mmria-v4.1.md#2.10 — DB storage (as-is)]
- [Source: prd-mmria-2026-06-12/prd.md#FR-2.5]

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6

### Debug Log References

### Completion Notes List

- Added `mmria_vitals_is_out_of_range()` helper function to `chart.js` immediately before `mmria_vitals_validate_field`. Not duplicated — reuses same pattern as `print_version_renderer.js` and `pdf-version/index.js`.
- Graph exclusion: In `get_chart_y_range_from_path()`, out-of-range values now push `'null'` instead of the numeric value. The existing `y_has_value` null-filtering logic in `chart_render()` then excludes both that x and y data point from the plotted columns, satisfying AC #1.
- Axis range: In `get_chart_y_values_from_path()`, out-of-range values are skipped so the y-axis min/max is computed only from in-range data.
- Table exclusion: In `chart_switch_to_table()`, the `y_axis.forEach` cell render now extracts `fieldName` and `rawVal` explicitly and calls `mmria_vitals_is_out_of_range(fieldName, rawVal)` — renders `''` when out of range, satisfying AC #2.
- Input fields unaffected: No changes were made to any input field rendering path (`page_renderer` form rendering, save path, etc.), satisfying AC #3.
- Null-guard: `mmria_vitals_is_out_of_range` returns `false` immediately when `window.mmria_vital_sign_range` is null/undefined, satisfying AC #4.

### File List

- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js`
