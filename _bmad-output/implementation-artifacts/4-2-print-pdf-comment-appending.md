# Story 4.2: Print/PDF Out-of-Range Comment Appending

Status: not-started

## Story

As a CDC analyst reviewing printed case reports,
I want the Comment(s) column of a vitals record row to show which fields were removed due to out-of-range values,
so that printed output is legible and readers understand why fields appear blank.

## Acceptance Criteria

1. In print view, when a vitals record row has one or more out-of-range values excluded (rendered as empty string), the Comment(s) cell for that row is appended with a notice in the format: `** Out of range. [Field name] removed.` — one clause per excluded field, concatenated into a single string.
2. In PDF view, the same notice is appended to the Comment(s) cell of the affected vitals record row using the identical format and logic.
3. If the Comment(s) field already contains text, the notice is appended to the existing content with a single space separator. The appended result only affects the rendered output — the stored database value is not modified.
4. If multiple vitals values in the same row are excluded, the clauses are concatenated into one notice string: e.g., `** Out of range. Temperature removed. Heart rate removed.` (a single `** Out of range.` prefix, one clause per excluded field).
5. If zero vitals values in a row are excluded, the Comment(s) cell renders exactly as stored — no notice, no change.
6. If `window.mmria_vital_sign_range` is `null`, no notice is appended and the Comment(s) field renders its stored value unchanged.
7. The stored database value for the Comment(s) field is never written to by this change — exclusion is display-time only.

## Tasks / Subtasks

- [ ] Add `mmria_vitals_build_out_of_range_notice()` to `pdf-version/index.js` (AC: #1–#6)
  - [ ] Place the new function immediately after the closing `}` of `mmria_vitals_is_out_of_range()` at line 707 (insert after ~line 717)
  - [ ] Function iterates `p_meta_children[1]` through `p_meta_children[p_meta_children.length - 2]` (skipping first/date child at index 0 and last/comment child)
  - [ ] For each child, calls `mmria_vitals_is_out_of_range(child.name, p_data_row[child.name])`
  - [ ] Concatenates `child.prompt + ' removed. '` for each out-of-range field into `clauses`
  - [ ] Returns `'** Out of range. ' + clauses.trim()` if any clauses exist, otherwise `''`
  - [ ] If `!window.mmria_vital_sign_range`, returns `''` immediately
- [ ] Modify comment cell rendering in `pdf-version/index.js` vitals branch (AC: #1–#7)
  - [ ] Locate the comment push at ~line 2284: `row.push({ text: chkNull(dataChild[metaChild[metaChild.length - 1].name]), style: ['tableDetail'], },);` inside the `ctx.data.forEach` loop of the `vital_signs`/`transport_vital_signs` branch
  - [ ] Before that push, compute: `var vitals_oor_notice = mmria_vitals_build_out_of_range_notice(metaChild, dataChild);`
  - [ ] Compute: `var vitals_comment_text = chkNull(dataChild[metaChild[metaChild.length - 1].name]);`
  - [ ] If notice is non-empty: `vitals_comment_text = vitals_comment_text ? vitals_comment_text + ' ' + vitals_oor_notice : vitals_oor_notice;`
  - [ ] Replace the original push with: `row.push({ text: vitals_comment_text, style: ['tableDetail'], },);`
- [ ] Add `mmria_vitals_build_out_of_range_notice()` to `print_version_renderer.js` (AC: #1–#6)
  - [ ] Place the new function immediately after the closing `}` of `mmria_vitals_is_out_of_range()` at line 1084 (insert after ~line 1095)
  - [ ] Identical logic to the pdf-version counterpart (same signature, same implementation)
- [ ] Modify `grid` case in `print_version_renderer.js` to append notice to comment cell (AC: #2–#7)
  - [ ] In the `grid` case (line 60), immediately before the row loop `for (let i = 0; i < p_data.length; i++)` at ~line 83, add: `let is_vitals_grid = window.mmria_vital_sign_range && (p_metadata.name === 'vital_signs' || p_metadata.name === 'transport_vital_signs');`
  - [ ] Inside the row loop, before the column loop `for (let j = 0; j < p_metadata.children.length; j++)`, add: `let row_oor_notice = is_vitals_grid ? mmria_vitals_build_out_of_range_notice(p_metadata.children, p_data[i]) : '';`
  - [ ] Inside the column loop, replace the existing `if (p_data[i][child.name] != null) { ... }` block with logic that: (a) reads `let cell_data = p_data[i][child.name];` (b) if this is the last column and the grid is a vitals grid with a notice (`is_vitals_grid && j === p_metadata.children.length - 1 && row_oor_notice`), applies `cell_data = cell_data ? cell_data + ' ' + row_oor_notice : row_oor_notice;` (c) calls `print_version_render` with `cell_data` if `cell_data != null`

## Dev Notes

### Prerequisite
Story 2.3 must be complete and merged. This story adds the comment-appending behavior on top of the value-suppression behavior implemented in Story 2.3. Both behaviors operate on the same rendering paths. Story 2.3 is currently marked **done**.

### Primary files
- `source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/print-version/print_version_renderer.js`

### No other files touched
- No server-side C# changes
- No changes to `case/index.js`, `chart.js`, or `print-version/index.js`
- No changes to any database document
- No build step required for JS changes

### Out-of-range notice helper function (add to both files)

Place immediately after `mmria_vitals_is_out_of_range()`:
- In `pdf-version/index.js`: after line 717 (closing `}` of the function at line 707)
- In `print_version_renderer.js`: after line 1095 (closing `}` of the function at line 1084)

```javascript
// Builds the out-of-range notice string for a vitals record row.
// p_meta_children: the metadata children array for the vitals grid
// p_data_row: the data object for one vitals record
// Returns '' if no values are out of range or if mmria_vital_sign_range is null.
function mmria_vitals_build_out_of_range_notice(p_meta_children, p_data_row) {
    if (!window.mmria_vital_sign_range) return '';
    var clauses = '';
    // Skip first child (date/time, index 0) and last child (comment, index length-1)
    for (var i = 1; i < p_meta_children.length - 1; i++) {
        var child = p_meta_children[i];
        if (mmria_vitals_is_out_of_range(child.name, p_data_row[child.name])) {
            clauses += child.prompt + ' removed. ';
        }
    }
    if (!clauses) return '';
    return '** Out of range. ' + clauses.trim();
}
```

**Field name source:** Use `child.prompt` from the metadata child entry — this is the human-readable field label (e.g., "Temperature", "Heart Rate"). At implementation time, verify these match the PRD example labels. If `child.prompt` values include trailing colons or other punctuation, strip them before appending.

### PDF view — exact change location

In `pdf-version/index.js`, inside the `vital_signs`/`transport_vital_signs` branch, inside the `ctx.data.forEach` loop, the current comment cell at ~line 2284:

```javascript
						// Put it into a table
						row.push({ columns: [colPrompt, colData], },);
						row.push({ text: chkNull(dataChild[metaChild[metaChild.length - 1].name]), style: ['tableDetail'], },);
						gridBody.push(row)
```

Replace with:

```javascript
						// Put it into a table
						row.push({ columns: [colPrompt, colData], },);
						var vitals_oor_notice = mmria_vitals_build_out_of_range_notice(metaChild, dataChild);
						var vitals_comment_text = chkNull(dataChild[metaChild[metaChild.length - 1].name]);
						if (vitals_oor_notice) {
							vitals_comment_text = vitals_comment_text ? vitals_comment_text + ' ' + vitals_oor_notice : vitals_oor_notice;
						}
						row.push({ text: vitals_comment_text, style: ['tableDetail'], },);
						gridBody.push(row)
```

### Print view — exact change location

In `print_version_renderer.js`, the `grid` case starts at line 60. The current row/column loop structure at ~line 83:

```javascript
      for (let i = 0; i < p_data.length; i++) 
      {
        result.push('<tr>');
        for (let j = 0; j < p_metadata.children.length; j++) 
        {
          result.push("<td data-child='td-2'>");
          let child = p_metadata.children[j];

          if (p_data[i][child.name] != null)
          {
              Array.prototype.push.apply
              (
                  result,
                  print_version_render
                  (
                      child,
                      p_data[i][child.name],
                      p_path + '.' + child.name,
                      p_ui,
                      p_metadata_path,
                      p_object_path + '[' + i + '].' + child.name,
                      p_post_html_render,
                      p_multiform_index,
                      true
                  )
              );
          }
          result.push('</td>');
        }
        result.push('</tr>');
      }
```

Replace with:

```javascript
      let is_vitals_grid = window.mmria_vital_sign_range &&
          (p_metadata.name === 'vital_signs' || p_metadata.name === 'transport_vital_signs');

      for (let i = 0; i < p_data.length; i++) 
      {
        result.push('<tr>');
        let row_oor_notice = is_vitals_grid
            ? mmria_vitals_build_out_of_range_notice(p_metadata.children, p_data[i])
            : '';

        for (let j = 0; j < p_metadata.children.length; j++) 
        {
          result.push("<td data-child='td-2'>");
          let child = p_metadata.children[j];
          let cell_data = p_data[i][child.name];

          // For vitals grids, append out-of-range notice to the comment cell (last child)
          if (is_vitals_grid && j === p_metadata.children.length - 1 && row_oor_notice) {
              cell_data = cell_data ? cell_data + ' ' + row_oor_notice : row_oor_notice;
          }

          if (cell_data != null)
          {
              Array.prototype.push.apply
              (
                  result,
                  print_version_render
                  (
                      child,
                      cell_data,
                      p_path + '.' + child.name,
                      p_ui,
                      p_metadata_path,
                      p_object_path + '[' + i + '].' + child.name,
                      p_post_html_render,
                      p_multiform_index,
                      true
                  )
              );
          }
          result.push('</td>');
        }
        result.push('</tr>');
      }
```

### Vitals grids in scope
The `vital_signs` and `transport_vital_signs` grid names cover:
- `vital_signs` — appears on multiple forms (ER visit, hospital medical records)
- `transport_vital_signs` — Medical Transport form

The PDF renderer's vitals branch already handles both names in the same `else if` block at ~line 2222. The print renderer's generic `grid` case processes all grids, so the `is_vitals_grid` check scopes the new logic to only those two names.

### `laboratory_tests` and `routine_monitoring`
These grid names are included in the PDF vitals branch but are **not** clinical vitals grids — they have a different field structure and do not share the same Comment(s) column layout. The out-of-range notice must not be applied to them.

**Confirm at implementation time:** Check whether `laboratory_tests` and `routine_monitoring` grids have a last-child comment field and whether `mmria_vitals_is_out_of_range` would ever fire for their field names. If either is at risk, restrict the PDF branch comment-appending to `vital_signs` and `transport_vital_signs` only (matching the print view guard), rather than applying it to all grids in the `else if` condition.

### `chkNull` in PDF vs raw value in print
- In the PDF branch, the comment value is read via `chkNull(...)` — preserve this call when computing `vitals_comment_text`.
- In the print branch, `p_data[i][child.name]` may be `null` or `undefined` — `cell_data` handles both. When notice is appended and `cell_data` was `null`, the rendered cell will contain only the notice string (passed to `print_version_render` as the data argument).

### Context from Story 2.3 (prerequisite)
Story 2.3 completion confirmed:
- Print view is client-side JS rendered via `print_version_renderer.js` — `print_version_render()` function.
- PDF vitals rendering is in `pdf-version/index.js` in the `vital_signs` / `transport_vital_signs` grid branch.
- Both rendering paths load `window.mmria_vital_sign_range` via the `openTab` → `create_print_version` call chain — it is available in both windows before rendering begins.
- `mmria_vitals_is_out_of_range()` was added to both files and is confirmed working.

### DB storage rule
Out-of-range values are saved to CouchDB as-is. The comment appending is render-time only. No write path is touched by this story.

### Null-guard
Every notice-related code path is protected: `mmria_vitals_build_out_of_range_notice` returns `''` immediately if `!window.mmria_vital_sign_range`, and `is_vitals_grid` is `false` when `window.mmria_vital_sign_range` is `null`.

### Project Structure Notes
- No new files created
- No build step required
- No server-side changes

### References
- [Source: prd-mmria-2026-06-12/prd.md#FR-2.4]
- [Source: Story 2.3 completion notes — rendering paths confirmed, mmria_vitals_is_out_of_range location verified]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

### Change Log
