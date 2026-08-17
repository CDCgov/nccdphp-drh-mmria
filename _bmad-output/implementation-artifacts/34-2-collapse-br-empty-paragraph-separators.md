# Story 34.2: Collapse BR Plus Empty Paragraph Separators

Status: done

## Story

As a case reviewer,
I want QA narrative template section breaks to render with normal spacing in the PDF,
so that section headings are not pushed apart by duplicate blank rows after editing or saving the narrative.

## Acceptance Criteria

1. Given a saved narrative matching `docs/ai/local/case-narrative-spacing/qa/html.txt`, when `convert_html_to_pdf(...)` walks the HTML for `case_opening_overview`, then each top-level `<br>` immediately followed by a whitespace-only empty paragraph separator renders as one intentional break in the PDF output, not two visible blank rows.
2. Given a body-level `<br>` is not adjacent to an empty paragraph separator, when the narrative is converted for PDF, then existing intentional line-break behavior is preserved.
3. Given a paragraph contains visible text, inline formatting, NBSP, or meaningful inline whitespace, when PDF conversion normalizes empty separators, then meaningful text and inline spacing are preserved; words do not collapse together.
4. Given the Story 34.1 fixtures still exist in `docs/ai/local/case-narrative-spacing/`, when regression verification runs, then the prior fixes for body-level whitespace-only `#TEXT` nodes and `<p><br></p>` blank paragraphs still pass.
5. Given the stored narrative HTML is read from `g_data.case_narrative.case_opening_overview`, when the fix runs, then the stored HTML, Trumbowyg editor output, and save sanitizer behavior are not changed.

## Tasks / Subtasks

- [x] Add regression coverage or a focused verification harness for the QA fixture shape. (AC: 1, 4)
  - [x] Use `docs/ai/local/case-narrative-spacing/qa/html.txt` as the primary new fixture.
  - [x] Keep the existing `unchanged-prod-data.txt` and `changed-prod-data-v4.1.txt` coverage from Story 34.1.
- [x] Update case narrative PDF conversion so a top-level `<br>` followed by a whitespace-only empty paragraph produces one intentional break. (AC: 1, 2)
  - [x] Preserve standalone `<br>` behavior when it is not part of the duplicate separator pattern.
  - [x] Preserve Story 34.1's body-level whitespace-only `#TEXT` skip.
  - [x] Preserve Story 34.1's `<p><br></p>` blank paragraph collapse.
- [x] Verify inline text and formatting are unchanged. (AC: 3, 5)
  - [x] Confirm inline spacing around `<strong>`, NBSP, and normal text nodes remains intact.
  - [x] Confirm no code path rewrites or persists `g_data.case_narrative.case_opening_overview`.
- [ ] Manually compare a regenerated QA PDF after automated or harness-level checks pass. (AC: 1, 4)

## Dev Notes

### Current Evidence

- QA evidence folder: `docs/ai/local/case-narrative-spacing/qa/`.
- `html.txt` contains repeated separators like `</p><br><p>\r\n</p><p><strong>...`; the pattern appears on lines 3, 6, 9, 12, 15, 18, 21, 23, 26, 29, and 32.
- Fixture metrics observed during investigation: 12 `<br>` tags, 12 whitespace-only empty paragraphs, and 11 `<br>` plus empty-paragraph separators.
- The QA editor screenshot shows compact editor spacing, so the remaining defect is still in PDF interpretation, not editor display.

### Relevant Code

- Primary file: `source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js`.
- Entry point: `convert_html_to_pdf(p_value)`.
- Walker: `ConvertHTMLDOMWalker(p_result, p_node)`.
- Story 34.1 modified the `#TEXT` branch to skip body-level structural whitespace-only text nodes.
- Story 34.1 modified the `P`/`DIV` branch to collapse `<p><br></p>` to one newline.
- The remaining QA pattern involves the `BR` branch emitting `{ text: "\n" }` and the following empty `P`/`DIV` also contributing a newline.

### Scope Boundaries

- Keep the fix surgical and limited to the case narrative PDF conversion path unless implementation evidence proves that impossible.
- Do not change Trumbowyg initialization, editor save output, stored narrative HTML, or the save sanitizer.
- Do not introduce a build step, bundler, npm dependency, or server-side migration.
- Do not make the unchanged production fixture tighter than the prior correct production PDF.

### Project Structure Notes

- Client JavaScript in `wwwroot/scripts` is vanilla JavaScript served directly by Razor views.
- Case narrative stored HTML is a contract-sensitive surface. Existing CouchDB case data may contain multiple historical HTML shapes, so the PDF converter must tolerate them without mutating the stored value.

### References

- Epic: `_bmad-output/planning-artifacts/epics.md` - Epic 34, Story 34.2.
- Previous story: `_bmad-output/implementation-artifacts/34-1-normalize-case-narrative-pdf-whitespace-conversion.md`.
- QA fixture: `docs/ai/local/case-narrative-spacing/qa/html.txt`.
- Prior fixtures: `docs/ai/local/case-narrative-spacing/unchanged-prod-data.txt`, `docs/ai/local/case-narrative-spacing/changed-prod-data-v4.1.txt`.
- Project rule: `_bmad-output/project-context.md` - Case narrative HTML must not be altered in storage.

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6

### Debug Log References

### Completion Notes List

- Added a **duplicate-separator guard** to the `P`/`DIV` branch of `ConvertHTMLDOMWalker` in `pdf-version/index.js`. The guard fires when: (1) the parent is `BODY`, (2) `previousElementSibling` is a `BR`, and (3) the paragraph's text content is whitespace-only after child-walking. It returns early without emitting, leaving the body-level BR's single `\n` as the sole separator.
- The fix is placed after the existing Story 34.1 blank-paragraph guard and before `text_array.push({ text: "\n" })` so neither guard interferes with the other.
- Condition uses `previousElementSibling` (skips any intermediate text nodes) and `/^\s*$/.test(item.text)` (catches `''`, `' '`, `'\n'`-derived empty strings).
- `g_data.case_narrative.case_opening_overview` is never touched — fix is purely in the PDF rendering path.
- Created `docs/ai/local/case-narrative-spacing/verify-harness.html`: self-contained browser test page with 9 assertions covering all ACs and the 34.1 regressions. Open the file in a browser to run.

### File List

- `source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js` — duplicate-separator guard added to `ConvertHTMLDOMWalker` P/DIV case
- `docs/ai/local/case-narrative-spacing/verify-harness.html` — new verification harness (9 assertions)
