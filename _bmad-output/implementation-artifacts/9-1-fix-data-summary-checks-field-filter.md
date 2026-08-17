# Story 9.1: Fix Data Summary Checks Field Filter for ALL Toggle

Status: done

## Story

As a user of the Data Summary Checks page,
I want the Field dropdown to show only the fields from the selected Form when I select "ALL",
so that my data summary reflects the correct form-scoped fields and not all fields across all forms.

## Acceptance Criteria

1. When no Form is selected (`g_filter.selected_form` is `''` or `'all'`), the Field dropdown shows all fields across all forms — existing default behavior is preserved unchanged.
2. When a Form is selected and individual fields are manually checked/unchecked (ALL not toggled), the Field dropdown shows only fields belonging to the selected Form — existing working behavior is preserved unchanged.
3. When a Form is selected and the user toggles the "All Fields" checkbox ON, the Field dropdown re-renders showing only the fields belonging to the selected Form with all of them checked — fields from other forms are not shown.
4. When a Form is selected and "All Fields" is toggled ON, the underlying `g_filter.field_selection` continues to hold `"all"` as its value — no change to the selection state model, only to what is rendered.
5. When a Form is selected, "All Fields" is ON, and the user then clears the Form selection (selects `(Any Form)`), the Field dropdown reverts to showing all fields across all forms with all checked — the default no-form state is restored.
6. Build produces zero JS errors in the browser console (Edge and Chrome — NFR-1) for the described interactions.

## Tasks / Subtasks

- [x] Locate and fix the form-filter guard in `render_field_filter` (AC: #3, #4)
  - [x] File: `source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/renderer.js`
  - [x] Find the guard at approximately line 270 inside `render_field_filter(p_filter)`:
    ```js
    if(!all_selected && selected_form && selected_form != '' && selected_form != 'all' && k != selected_form)
    {
        continue;
    }
    ```
  - [x] Remove the `!all_selected &&` prefix so the guard always enforces the form scope:
    ```js
    if(selected_form && selected_form != '' && selected_form != 'all' && k != selected_form)
    {
        continue;
    }
    ```
  - [x] No other changes to `render_field_filter` are required.
- [x] Verify no-form default is preserved (AC: #1)
  - [x] Confirm `selected_form` is `''` on page load — the guard condition is false, all forms render.
- [x] Verify form-clear revert (AC: #5)
  - [x] `on_form_filter_changed('all')` sets `g_filter.selected_form = 'all'` and calls `render_field_filter` — guard becomes false, all forms render.
- [x] Manual smoke test in Edge and Chrome (AC: #6)
  - [x] No form selected → all fields visible ✓
  - [x] Form selected, ALL unchecked → only selected form's fields shown ✓ (pre-existing)
  - [x] Form selected, ALL toggled ON → only selected form's fields shown, all checked ✓ (bug fix)
  - [x] Form cleared after above → all fields restored ✓
- [x] Playwright regression tests (AC: #1, #3, #5, #6)
  - [x] File: `../nccdphp-drh-mmria-utilities/e2e/tests/view-data-summary.field-form-filter.spec.ts`
  - [x] `AC-1` test: no form selected → all form groups visible
  - [x] `AC-3` test: form selected + ALL ON → only selected form's group visible (primary regression guard)
  - [x] `AC-5` test: form cleared after ALL ON → all form groups restored
  - [x] Run with: `cd ../nccdphp-drh-mmria-utilities/e2e && npx playwright test view-data-summary.field-form-filter --project=chromium`
  - [x] All three tests pass green

## Dev Notes

**Root cause:** `render_field_filter(p_filter)` in `renderer.js` contains a guard that skips form-scoping when the `all_selected` flag is true:

```js
const all_selected = p_filter.field_selection && p_filter.field_selection.has("all");

for(const [k, v] of g_form_field_map)
{
    if(!all_selected && selected_form && ...)  // ← !all_selected bypasses form filter when ALL is checked
    {
        continue;
    }
    ...
}
```

When the user toggles ALL ON while a form is selected, `g_filter.field_selection` gets `"all"` added, making `all_selected = true`. The guard's `!all_selected` condition then short-circuits the entire `if`, so all forms' fields are rendered. Unchecking ALL removes `"all"` from `field_selection`, making `all_selected = false`, so the guard fires correctly — which is why unchecking already works.

**The fix is a single-token deletion** — remove `!all_selected &&` from the guard condition. The `all_selected` flag is still used correctly on the next line to decide whether individual field checkboxes render as checked:

```js
if (all_selected) {
  is_checked = "checked";
}
```

That line is unchanged and continues to work correctly.

**Form-to-field association:** Fields are associated to forms via `g_form_field_map` (a `Map<form_name, Map<field_name, field_info>>`) populated in `index.js` at lines ~1849–2014. No changes to `index.js` are required.

**Files to modify:**

- `source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/renderer.js` — one line change only

**Files to NOT modify:**

- `index.js` — no changes
- `Index.cshtml` — no changes
- Any server-side files — client-side fix only

**Commented-out code notice:** `on_form_filter_changed` (lines 113–114) contains two commented-out lines that previously reset `field_selection` to `['all']` on form change. These are intentionally left commented — do not remove or uncomment them.
