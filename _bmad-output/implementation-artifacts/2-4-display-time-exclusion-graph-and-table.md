# Story 2.4: Display-Time Exclusion — Graph and Table Views

Status: ready-for-dev

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

- [ ] Locate graph rendering in `chart.js` (AC: #1)
  - [ ] Find the function(s) in `chart.js` that plot data points onto the graph
  - [ ] Identify where the vitals value is read before being passed to the charting logic
  - [ ] Add out-of-range check: if value is out of range, skip that data point entirely (no point plotted, no line to/from it)
- [ ] Locate table rendering in `chart.js` (AC: #2)
  - [ ] Find the function(s) that render the tabular view of vitals records
  - [ ] Identify where each cell value is written
  - [ ] Add out-of-range check: if value is out of range, render empty string instead
- [ ] Confirm input fields unaffected (AC: #3)
  - [ ] Verify the case form input field rendering path is not touched by these changes
  - [ ] Input fields continue to show stored value regardless of range
- [ ] Null-guard all checks (AC: #4)
  - [ ] Wrap every exclusion check with `if (window.mmria_vital_sign_range) { ... }`
  - [ ] If null: render all values normally in graph and table

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

### Debug Log References

### Completion Notes List

### File List
