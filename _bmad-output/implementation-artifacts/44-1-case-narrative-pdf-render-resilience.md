---
baseline_commit: c608db41b41b4060cbb343164e240aaed212ea47
---

# Story 44.1 — Case Narrative PDF Render Resilience (Section-Scoped Fallback)

**Epic:** 44 — Case Narrative PDF Render Resilience (v4.2)
**Story ID:** 44.1
**Status:** review
**Date added:** 2026-08-20
**Source:** BUG 118794 — Rel 4.1, P-Low, TA: "Unable to create PDF on Case Narrative Case: NJ-2024-7102", reported by NJ (MMRIA\ITDM 25-26 - Option Yr 4). Reproduced against tenant1 record `TENENAT1-2010-7462`.
**PRD:** FR-14.1 – FR-14.6 in `_bmad-output/planning-artifacts/prds/prd-mmria-2026-08-06/prd.md`

---

## User Story

As a case reviewer generating a PDF report of a case,
When the case narrative contains malformed HTML that the current PDF pipeline cannot render (e.g., a table with a partial final row),
I want the PDF to still be generated with every other section intact and a short neutral message in place of the narrative,
So that I can proceed with committee review instead of being blocked entirely by one bad section.

---

## Acceptance Criteria

**AC-1 — Baseline: a well-formed narrative still renders identically to today**
Given a case whose `case_narrative.case_opening_overview` HTML is well-formed (no missing `<td>`s, no orphan tags, no other structural defects),
When the user opens or saves the PDF from the case view,
Then the PDF renders with the narrative content byte-for-byte the same as the pre-story output — no visual regression, no reordering, no extra whitespace, no missing tables/lists/inline formatting.

**AC-2 — Malformed narrative table triggers the fallback**
Given a case whose narrative contains a `<table>` where at least one `<tr>` in `<tbody>` has fewer `<td>` cells than the header row (i.e., the exact repro shape from BUG 118794 — a partial final row),
When the user opens or saves the PDF,
Then the PDF is delivered successfully (no infinite "Please wait" spinner, no unhandled pdfmake exception surfacing to the user), and the narrative section body is replaced by a single placeholder line.

**AC-3 — Placeholder wording is neutral and does not disclose the cause**
The fallback placeholder text does not mention: tables, HTML, malformed content, parse errors, cell counts, row counts, encoding, stack traces, or any diagnostic detail. Default text (subject to OI-v42-5 confirmation with Vilma):

> _"Case Narrative could not be included in this report. Please review the Case Narrative in the case and try again."_

If a different final wording is confirmed via OI-v42-5 before implementation, use that instead. Wording confirmation is *not* a blocker for implementation start; ship with the default and swap the string when OI-v42-5 resolves.

**AC-4 — Placeholder is styled consistently with surrounding section text**
The placeholder uses the same section-body style used for narrative text on a successful render (existing `tableDetail` / narrative style). No red text, no bold-alert framing, no console-error indicator inside the PDF. The section header (e.g., "Case Narrative") appears above the placeholder exactly as it does on a successful render.

**AC-5 — All other sections render normally on the fallback path**
On a fallback PDF (AC-2), every non-narrative section (demographic, death certificate, birth/fetal-death certificate, prenatal care, ER visits, medical transport, social/environmental profile, mental health profile, informant interviews, committee review, and any other configured section) renders identically to a full-success PDF. No section is dropped or corrupted as a side effect of the narrative fallback.

**AC-6 — Save-download path and open-in-window path both use the fallback**
Both output modes exercised in [`index.js`](../../source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js#L497) — `pdfMake.createPdf(doc).download(pdfName)` (`g_type_output == 'save'`) and `pdfMake.createPdf(doc).open(window)` (view) — produce the same fallback behavior. The user experience is identical regardless of which path was invoked.

**AC-7 — Malformed-shape guard covers the observed defect and structurally-similar shapes**
The pre-flight validator on narrative-derived pdfmake `{table}` items detects at minimum:
- A `body` row whose length is less than `widths.length`.
- A `body` row that contains an `undefined` or `null` cell reference in any position (not a `{text: ''}` placeholder — a genuine hole).

Any of these conditions triggers the fallback for the whole narrative section (not just the offending table — implementation simplicity, per Nick's direction).

**AC-8 — Fallback also fires on any unexpected narrative-conversion error**
The narrative content-build path (call to `convert_html_to_pdf(pdf_version_index_to_string(ctx))` and the subsequent loop that pushes into `ctx.content` — see [`index.js` around line 2626](../../source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js#L2626-L2696)) is wrapped so that a thrown JS error from any node visitor collapses to the same fallback. Belt-and-suspenders alongside the shape-check in AC-7.

**AC-9 — Console notice on fallback (developer-facing only)**
When the fallback fires, the browser emits exactly one `console.warn` entry naming the case's `record_id` (or `_id` when `record_id` is empty) and a short constant tag (e.g., `"[pdf] case_narrative fallback"`). No excerpt of narrative content is logged. No `console.error`, no `alert()`, no modal — the PDF just renders with the placeholder.

**AC-10 — Empty / whitespace-only narrative is unchanged**
When `case_opening_overview` is empty, null, or whitespace-only, the PDF today emits a section header with an empty body (no fallback). That behavior is preserved — the placeholder text must **not** appear on empty narratives. The fallback fires only on a genuine render failure.

**AC-11 — Narrative HTML is not modified in storage**
No code path touched by this story writes to `g_data.case_narrative.case_opening_overview`, calls `save`/`put_case`, or otherwise persists the narrative content. This is a render-side change only. (Project-context §2.4.)

**AC-12 — Manual repro from BUG 118794 passes**
The dev/PO manual verification uses the copied Prod defect payload from tenant1 record `TENENAT1-2010-7462`. Steps: open the case → click PDF → confirm the PDF opens with all non-narrative sections rendered and the placeholder in the narrative section (no infinite spinner, no thrown exception in the browser console at layout time). Nick or QA runs this against the reproduced case; no automated test is required for this AC.

**AC-13 — No changes to non-PDF renderers**
The Print View (page-print) path and the committee-member read-only view path are outside the scope of this story. If either shares any code with the PDF walker, do not introduce behavioral drift there; if unavoidable, note it as an addendum in the completion notes for follow-up. (Investigation may confirm they use a separate walker.)

---

## Dev Notes — Root Cause and Fix

### Root Cause (Recap)

The client-side PDF pipeline in [`source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js`](../../source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js) walks the narrative HTML into a pdfmake doc-def via `convert_html_to_pdf` → `ConvertHTMLDOMWalker` (line ~1224 and line ~1379). In the `TABLE` branch (line ~1379–1478):

- `widths` is computed from the **first** body row and never reconciled against subsequent rows.
- Each subsequent `<tr>` is pushed into `body` verbatim — no shape check.
- When a `<tr>` has fewer `<td>`s than `widths.length`, `body[row][col]` is `undefined` for the missing column(s).
- pdfmake's `tableLayouts` processor (in [`pdfmake.min.js`](../../source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/pdfmake.min.js)) throws exactly:

  ```
  Malformed table row, a cell is undefined.
  Row index: N
  Column index: M
  Row data: ...
  ```

  The throw happens inside `pdfMake.createPdf(doc)` — after the doc-def is assembled, during layout. See the console screenshot from BUG 118794 discussion (Laboffe, Jun 3).

Because the throw is inside `createPdf(doc)` (which is invoked via `setTimeout` from [`index.js` line 497 and 510](../../source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js#L497-L515)), a try/catch around the narrative content-build alone will **not** intercept the error. This is why the primary guard is a pre-flight shape check on narrative-derived `{table}` items, applied *before* the items are pushed into `ctx.content`.

### Fix Location — Two Guards

**Primary guard — pre-flight shape validator on narrative tables.**
In the `case "textarea"` block where `ctx.metadata.name == 'case_opening_overview'` — [`index.js` line ~2626](../../source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js#L2626-L2696) — after calling `convert_html_to_pdf(...)` and before the loop that pushes items into `ctx.content`, walk the returned narrative array. For each item where `item.hasOwnProperty('table')`:

- Determine expected column count as `Math.max(item.table.widths.length, ...item.table.body.map(r => r.length))` — or simply `item.table.widths.length` when non-zero, otherwise the max row length.
- If **any** `body[row]` has fewer cells than the expected column count, or contains an `undefined`/`null` slot anywhere, treat the narrative as unrenderable → fall through to the placeholder path.

Do not attempt to pad-and-recover rows. Nick's chosen direction is "just show a message and don't say why" — repairing shifted cells risks a subtly-wrong PDF that misleads the reviewer. Fallback is preferable to partial-truth.

**Secondary guard — try/catch around the narrative content-build.**
Wrap the entire narrative build-and-push block (the `narrative = convert_html_to_pdf(...)` call and the ensuing `for (let i = 0; i < narrative.length; i++)` loop) in a `try` block. On any thrown exception:

1. Discard any items already pushed to `ctx.content` from the narrative in this build (roll back to a snapshot of `ctx.content.length` captured before the try block, and `ctx.content.splice(...)` back to it).
2. Emit the placeholder row (see below).
3. Emit the `console.warn` (AC-9).
4. Continue to the `break;` at the end of the `case "textarea":` branch — do not rethrow.

### Placeholder Row Structure

The `case_opening_overview` narrative body uses two-column rows with `colSpan: '2'` (see [line 2696](../../source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js#L2690-L2696) for the regular-item shape). The placeholder should follow the same shape:

```js
ctx.content.push([
  { text: '<placeholder text from FR-14.2>', style: ['tableDetail'], colSpan: '2' },
  {},
]);
```

Use `'tableDetail'` (not `'narrativeDetail'`) to match the regular-item style already emitted in the non-`ul`/`ol`/`table`/`canvas` branch. Do not add a new style entry.

The section header ("Case Narrative") is emitted upstream by the section-header path and is unaffected — no code change is needed to preserve it.

### Console Warn Format

```js
console.warn('[pdf] case_narrative fallback', {
  record_id: g_data?.home_record?.record_id || g_data?._id || '(unknown)',
});
```

One entry per PDF generation. Do not include narrative content, table dimensions, or the thrown Error object in the log payload (PII avoidance; also keeps the message stable for developer grep).

### Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js` | Add pre-flight shape validator + try/catch fallback in the `case "textarea":` block for `case_opening_overview` (line ~2626). Emit placeholder row and one `console.warn` on fallback. |

No other files should require edits. Do **not** touch `ConvertHTMLDOMWalker` (line ~1379), `convert_html_to_pdf` (line ~1223), or `pdfmake.min.js`. Do **not** modify any server-side controller, `case_narrative` model, or save path.

### Test Approach

Manual verification (AC-12) is the primary check for this story — the automated E2E stack does not currently drive the PDF generation window. If a lightweight JS-level unit test can be added for the shape validator (pure function operating on a pdfmake `{table}` object), do so; otherwise, manual repro against `TENENAT1-2010-7462` (BUG 118794 dev copy) is sufficient.

Suggested manual steps:

1. Open the tenant1 case `TENENAT1-2010-7462` in a browser.
2. Trigger the PDF (both "save" and "open in window" — AC-6).
3. Confirm the PDF opens/downloads with all non-narrative sections rendered.
4. Confirm the "Case Narrative" section shows only the placeholder line — no rendered table content.
5. Open the browser console; confirm exactly one `[pdf] case_narrative fallback` warning is emitted with the case's `record_id`.
6. Open a case with a normal narrative (any case in tenant1 whose narrative is not the BUG 118794 repro payload); confirm the PDF renders the narrative content unchanged (AC-1).

### Non-Goals

- **Do not** repair, sanitize, or normalize the stored narrative HTML.
- **Do not** modify the Trumbowyg editor, save-path sanitizer, or paste handler (that's FR-9 territory).
- **Do not** add a blocking modal, confirmation dialog, or pre-PDF validation UI before the PDF is generated. The user's chosen UX is: PDF opens; narrative section shows placeholder.
- **Do not** attempt to detect and pad short rows to make them render. Nick's direction is fallback, not silent repair.
- **Do not** widen the fallback to other sections. The known defect and Nick's request are scoped to `case_opening_overview` only.
- **Do not** ship a related change to the Print View path in this story — see AC-13.

### Related Prior Art

- FR-9.1 / FR-9.2 (case narrative editor save-path tweaks) — related but separate concern (input sanitization vs. render resilience).
- Project-context §2.4 — the guardrail that keeps this fix render-side.
- Discussion trail on BUG 118794 (Vilma Jun 3 → Nicholas Jun 3/Jun 10 → Vilma Jun 10/Jul 13) — Vilma's Jun 10 draft message wording is captured in OI-v42-5 as a comparison candidate.

---

## Open Items

- **OI-v42-5** — Confirm final placeholder wording with Vilma. Ship with the FR-14.2 default; swap the string when OI-v42-5 resolves. Not an implementation blocker.

---

## Tasks / Subtasks

- [x] **T1 — Add pre-flight shape validator** (`has_malformed_narrative_table`) as a pure helper in [source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js), placed adjacent to `convert_html_to_pdf` for locality. Detects any `{table}` item whose `body` row is shorter than the max column count or contains a genuine `undefined`/`null` slot (empty `{text:''}` placeholders explicitly OK). Covers AC-7.
- [x] **T2 — Snapshot `ctx.content.length` before the narrative build** in the `case "textarea"` branch where `ctx.metadata.name == 'case_opening_overview'`, so partial pushes can be rolled back on failure. Covers AC-2, AC-5.
- [x] **T3 — Wrap the narrative build-and-push block in try/catch** (secondary guard). Runs the pre-flight validator first, then the existing per-item loop. Any thrown exception from a node visitor or the validator flips a single `narrativeFallback` flag. Covers AC-8.
- [x] **T4 — On fallback, roll back to snapshot, emit placeholder row, warn once, and continue.** Placeholder uses the two-column `colSpan: '2'` shape and `style: ['tableDetail']` (matching regular-item style). `console.warn('[pdf] case_narrative fallback', { record_id })` uses `g_d.home_record.record_id` → `g_d._id` → `'(unknown)'`. No `console.error`, no `alert()`, no modal. Covers AC-2, AC-3, AC-4, AC-9.
- [x] **T5 — Preserve empty-narrative behavior.** `convert_html_to_pdf('')` returns `[]`; validator returns `false` on `[]`; the loop is a no-op; no placeholder pushed. Verified by inspection. Covers AC-10.
- [x] **T6 — Verify no writes to `g_data.case_narrative.case_opening_overview`.** Change touches only client-side render code; no `save`/`put_case`/PUT paths involved. Covers AC-11.
- [x] **T7 — Verify save-download and open-in-window paths both benefit.** Both paths use `pdfMake.createPdf(doc)` on the same `doc.content`. Because the fallback is applied while `doc.content` is being assembled (upstream of both branches at [index.js line 494-518](../../source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js#L494-L518)), both `download(pdfName)` and `open(window)` see the placeholder. Covers AC-6.
- [x] **T8 — Sanity-check validator against 9 shape scenarios** in a standalone Node harness (well-formed, short row, undefined slot, null slot, empty-text placeholder OK, empty narrative, no-table items, expected-max derivation, non-table object). All 9 pass. Automated unit tests are not wired for this client-side script (no test harness exists for `pdf-version/*.js`); story allows manual verification per AC-12.
- [x] **T9 — No changes to Print View or committee-member read-only view.** The fallback lives inside the `case "textarea"` branch of the PDF-only walker (`ctx.content` mutation, `pdfMake` doc-def). Print View uses a separate rendering path and is not touched. Covers AC-13.

## Dev Agent Record

### Debug Log

- Verified line-of-throw is in `pdfmake.min.js` `tableLayouts` processor, invoked from `pdfMake.createPdf(doc)` at [index.js line 498 and 513](../../source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js#L494-L518) — inside a `setTimeout`, so an outer try/catch at the call site would not intercept the layout throw. Confirmed the required guard is a pre-flight check applied while `doc.content` is being built.
- Confirmed `g_d` (not `g_data` as the story recap phrased) is the case-data global at [index.js line 3](../../source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js#L3), set from `p_data` at line 115/200. Adjusted the console.warn payload accordingly.
- Ran a Node harness on the pure `has_malformed_narrative_table` function against 9 shape scenarios (see T8). All passed on first run.

### Implementation Plan

Applied Nick's chosen direction — fallback over silent repair — in two guards:

1. **Primary guard (`has_malformed_narrative_table`)** — a pure function that inspects each narrative item with a `table` key. Computes expected column count as `max(widths.length, max(body[r].length))` and flags: (a) short rows, (b) `undefined`/`null` cell slots, (c) non-array rows, (d) missing `body` array. Empty `{text:''}` placeholders are explicitly not flagged. Runs immediately after `convert_html_to_pdf`.
2. **Secondary guard (try/catch)** — wraps the same call plus the existing per-item push loop. Belt-and-suspenders for any node visitor that might throw before or after the pre-flight check.

On fallback: roll back `ctx.content` to a length snapshot taken before entry, push a single placeholder row using the existing two-column `colSpan: '2'` shape and `tableDetail` style, emit one `console.warn('[pdf] case_narrative fallback', { record_id })`, and let the branch fall through to the natural `break;`. No rethrow, no modal, no `console.error`.

Empty narrative flow is preserved by construction: `convert_html_to_pdf('')` returns `[]`; the validator returns `false`; the loop iterates zero times; no placeholder is pushed.

### Completion Notes

- All 13 acceptance criteria satisfied via inspection and the pure-function sanity harness (T8). AC-1, AC-2, AC-6, AC-12 require the manual repro against `TENENAT1-2010-7462` as documented in the Test Approach section — the code path is in place and ready for that verification.
- Placeholder wording ships with the FR-14.2 default per AC-3. Swap the string when OI-v42-5 resolves — a single-line edit in the fallback block of [index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js).
- No changes to server-side controllers, `case_narrative` model, save paths, `ConvertHTMLDOMWalker`, `convert_html_to_pdf` internals, or `pdfmake.min.js` — per non-goals.
- Global case-data variable in this file is `g_d` (not `g_data` as the Dev Notes recap phrased). The console.warn payload uses `g_d.home_record.record_id` → `g_d._id` → `'(unknown)'` fallback chain, matching the AC-9 spec.
- No addendum for AC-13 needed — the Print View renderer is a separate path from the PDF walker; nothing shared was touched.

### File List

- `source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js` — added `has_malformed_narrative_table` helper (~40 lines after `convert_html_to_pdf`); wrapped the `case_opening_overview` narrative build in a snapshot / try-catch / fallback block inside the `case "textarea":` branch.

## Change Log

| Date       | Change                                                                                                   | Author |
|------------|----------------------------------------------------------------------------------------------------------|--------|
| 2026-08-20 | Story 44.1 implemented: pre-flight table-shape validator + try/catch fallback for `case_opening_overview`; placeholder row + one `console.warn` on fallback. Status → review. | Dev    |
