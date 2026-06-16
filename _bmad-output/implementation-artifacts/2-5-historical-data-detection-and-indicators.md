---
baseline_commit: 3d907ee8d92473811ad909f4e6924e06106d7934
---

# Story 2.5: Historical Data Detection and Record Indicators

Status: done

## Story

As a case reviewer,
I want to be notified when a case I'm editing contains vitals values that fall outside permitted ranges,
so that I understand why those values are absent from graphs, tables, and printed output.

## Acceptance Criteria

1. When a reviewer enters edit mode for a case, all vitals values across all vitals records are re-validated against `window.mmria_vital_sign_range`.
2. When the reviewer selects a different form from the "Select case form" dropdown while in edit mode, re-validation runs again.
3. If re-validation finds one or more out-of-range values, a modal displays: "This case contains vital sign records with values outside the permitted range. These values are excluded from graphs, tables, print and pdf views." — using the existing site modal pattern; on dismiss, no focus change required.
4. A red text indicator is applied at the top of each vitals record that contains at least one out-of-range value.
5. The red text indicator is re-evaluated on each render — it is not a one-time write.
6. If `window.mmria_vital_sign_range` is `null`, no re-validation runs, no modal appears, no indicators are applied.

## Tasks / Subtasks

- [x] Resolve OI-dev-B: find the edit-mode entry hook (AC: #1)
  - [x] Search `wwwroot/scripts/case/index.js` for the function or event that signals transition into case edit mode
  - [x] This is the attach point for the re-validation call
  - [x] Document the function name and approximate line number
- [x] Resolve OI-dev-C: find the DOM target for record indicator (AC: #4)
  - [x] In `wwwroot/scripts/editor/page_renderer/chart.js`, identify the DOM element at the top of each rendered vitals record
  - [x] Confirm whether the indicator should be inserted before that element or as a child
  - [x] Confirm it is re-rendered on each chart.js render pass (expected behavior)
- [x] Implement re-validation function (AC: #1, #2, #6)
  - [x] Write `mmria_vitals_revalidate_all()` — iterates all vitals inputs currently in the DOM, checks each against `window.mmria_vital_sign_range`
  - [x] Returns list of out-of-range field/record references (or boolean if only modal needed)
  - [x] Null-check `window.mmria_vital_sign_range` at top — return immediately if null
- [x] Attach to edit-mode entry trigger (AC: #1)
  - [x] At the hook identified in OI-dev-B, call `mmria_vitals_revalidate_all()`
- [x] Attach to form selector navigation (AC: #2)
  - [x] Find the "Select case form" dropdown change handler in `case/index.js`
  - [x] Add call to `mmria_vitals_revalidate_all()` on change while in edit mode
- [x] Show historical data modal on findings (AC: #3)
  - [x] If `mmria_vitals_revalidate_all()` finds any out-of-range values, invoke existing site modal
  - [x] Message: `"This case contains vital sign records with values outside the permitted range. These values are excluded from graphs, tables, print and pdf views."`
  - [x] On dismiss: no focus change
- [x] Apply red text indicator per affected record (AC: #4, #5)
  - [x] For each record containing at least one out-of-range value, insert/update red text indicator at the DOM target confirmed in OI-dev-C
  - [x] Indicator text: "Contains excluded values — one or more readings fall outside the permitted range."
  - [x] Indicator is written as part of `chart.js` render pass — re-evaluated on each render

## Dev Notes

**Primary files:**
- `wwwroot/scripts/case/index.js` — edit-mode entry hook, form selector handler
- `wwwroot/scripts/editor/page_renderer/chart.js` — red indicator DOM insertion

**Prerequisite:** Story 2.1 must be complete — `window.mmria_vital_sign_range` must be available.

**Two open items to resolve before implementation:**
- **OI-dev-B:** Confirm the function or event in the codebase that signals transition into case edit mode. This is the attach point for the re-validation call.
- **OI-dev-C:** Confirm the DOM element target in `chart.js` for the per-record red text indicator. Confirm whether indicator is re-evaluated on each render (expected) or written once.

**Historical modal vs. field-level modal (Story 2.2):** These are two distinct uses of the same existing modal pattern:
- Story 2.2 (field-level): fires on blur/paste, message mentions specific field and range, focus returns to cleared field on dismiss
- This story (historical): fires on edit-mode entry and form nav, message is general, no focus change on dismiss

**Re-validation scope:** Check all vitals inputs currently rendered in the DOM. If the form change re-renders the vitals grids, the indicator will naturally be re-applied on the next render pass.

**Indicator re-evaluation:** The red indicator must be written as part of the `chart.js` rendering logic, not as a one-time DOM manipulation. This ensures it persists correctly across form navigation and re-renders.

**No DB changes.** Out-of-range values remain in CouchDB as-is. All detection and indication is display-time only.

### Project Structure Notes

- Changes span `case/index.js` (triggers) and `chart.js` (indicator)
- No new files created
- No build step required for JS changes

### References

- [Source: architecture-mmria-v4.1.md#2.8 — Historical data detection]
- [Source: architecture-mmria-v4.1.md#2.9 — Section 508 modal — historical data use]
- [Source: architecture-mmria-v4.1.md#2.10 — DB storage (as-is)]
- [Source: prd-mmria-2026-06-12/prd.md#FR-2.6]

## Dev Agent Record

### Agent Model Used
Claude Sonnet 4.6

### Debug Log References

- OI-dev-B resolved: Edit-mode entry hook is in the enable-edit button handler (`case/index.js` ~line 4573). After `g_render()` and the `$global.case_document_begin_edit()` callback, the revalidation call was inserted.
- OI-dev-C resolved: `vital_signs` is an **array** within each `er_visit_and_hospital_medical_records` element (multiple timed measurements per visit). The chart container `<div id='${map_key}'>` is the per-vitals-record DOM unit; the indicator is a `<tr>` row inserted between the title header row and the chart body, re-evaluated on every `chart_render()` and `chart_switch_to_table()` call.
- Form navigation: hash-based via `window_on_hash_change`. Revalidation attaches after `g_render()` in the same-case section-navigation branch, gated on `g_data_is_checked_out`.

### Completion Notes List

- `mmria_chart_has_excluded_values(p_metadata, p_object_path)` added to `chart.js`: checks one chart's data array for any out-of-range value using the same data-access pattern as the existing chart table renderer.
- `mmria_vitals_revalidate_all()` added to `chart.js`: iterates `g_data.er_visit_and_hospital_medical_records[i].vital_signs[j]` for all range keys; returns `true` on first hit; returns `false` immediately if `window.mmria_vital_sign_range` is null (AC #6).
- `mmria_vitals_show_historical_modal()` added to `chart.js`: purple header, OK button only, no focus change on dismiss — matches site modal pattern (AC #3).
- Red indicator `<tr>` inserted in `chart_render()` HTML template and in `chart_switch_to_table()` tbody — re-evaluated on every render pass, not a one-time DOM write (AC #4, #5).
- Both trigger sites call revalidate → show modal if any out-of-range found (AC #1, #2).

### File List

- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`

### Change Log

- 2026-06-15: Implemented Story 2.5. Added `mmria_chart_has_excluded_values`, `mmria_vitals_revalidate_all`, and `mmria_vitals_show_historical_modal` to `chart.js`. Added red excluded-values indicator row to `chart_render()` and `chart_switch_to_table()`. Added revalidation + modal trigger to `case/index.js` at edit-mode entry and at section-navigation `g_render()` call.
