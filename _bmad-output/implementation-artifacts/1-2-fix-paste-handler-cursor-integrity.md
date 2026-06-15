# Story 1.2: Fix Paste Handler Cursor Integrity

Status: ready-for-dev

## Story

As a case reviewer,
I want pasted content to land at my cursor position in the case narrative editor,
so that multiple sequential pastes from Word or other sources each land exactly where I intend.

## Acceptance Criteria

1. A paste operation (Ctrl+V) inserts content at the active cursor position, not at a random location in the document.
2. Multiple sequential paste operations each land at the cursor position active at the time of that paste.
3. The paste handler uses the Range API (`window.getSelection().getRangeAt(0)`, `range.deleteContents()`, `range.insertNode()`) — `document.execCommand('insertHTML')` is not used.
4. Only executable XSS attributes (`onclick`, `onerror`, `javascript:` hrefs) are stripped from pasted content — all structural HTML tags are preserved.
5. Behavior is consistent and correct in both Edge and Chrome (NFR-1).

## Tasks / Subtasks

- [ ] Locate and understand the existing paste handler (AC: #1, #2, #3)
  - [ ] Find `page_render_create_onpaste_event()` in `case/index.js`
  - [ ] Identify root cause: selection state not captured before paste executes, or stale selection range used
  - [ ] Note any `document.execCommand('insertHTML')` calls — these must be replaced
- [ ] Rewrite paste handler with Range API (AC: #1, #2, #3)
  - [ ] At the top of the event handler, synchronously capture: `var selection = window.getSelection(); var range = selection.getRangeAt(0);`
  - [ ] Delete any currently selected content: `range.deleteContents();`
  - [ ] Build a DocumentFragment from the cleaned paste content
  - [ ] Insert at captured range: `range.insertNode(fragment);`
  - [ ] Collapse range to end of inserted content: `range.collapse(false);`
  - [ ] Restore selection: `selection.removeAllRanges(); selection.addRange(range);`
- [ ] Implement XSS-safe paste cleaning (AC: #4)
  - [ ] Strip `onclick`, `onerror`, and `javascript:` href attributes from pasted HTML
  - [ ] Preserve all structural tags (`<b>`, `<i>`, `<u>`, `<br>`, `<p>`, `<font>`, etc.)
- [ ] Validate in Edge and Chrome (AC: #5)
  - [ ] Test: single paste at cursor mid-paragraph
  - [ ] Test: multiple sequential pastes at different cursor positions
  - [ ] Test: paste after selecting/cutting existing text
  - [ ] Test: paste from Word document with formatting

## Dev Notes

**Primary file:** `wwwroot/scripts/case/index.js`

**No server-side changes.** FR-1 is JavaScript-only.

**Target function:** `page_render_create_onpaste_event()`

**Root cause (per architecture):** Cursor/selection state is not captured before the paste operation executes. The `window.getSelection()` / `document.execCommand('insertHTML')` sequence loses selection state if the paste event handling re-focuses the element or if a stale selection range is used.

**Required algorithm:**
```javascript
// Step 1: Capture selection synchronously at TOP of handler — before any DOM manipulation
var selection = window.getSelection();
var range = selection.getRangeAt(0);

// Step 2: Delete selected content (if any)
range.deleteContents();

// Step 3: Build cleaned fragment from paste data
// (strip XSS vectors: onclick, onerror, javascript: hrefs)
var fragment = /* build from clipboard data */;

// Step 4: Insert at captured range
range.insertNode(fragment);

// Step 5: Collapse to end of insertion and restore selection
range.collapse(false);
selection.removeAllRanges();
selection.addRange(range);
```

**FORBIDDEN:** `document.execCommand('insertHTML')` — its selection behavior is non-deterministic across browsers. Use the Range API directly.

**XSS cleaning:** Remove only `onclick`, `onerror`, `javascript:` hrefs. Do NOT strip structural tags. The overriding HTML constraint from FR-1 applies here too — no tag substitution.

**Overriding constraint:** Do not change the generated HTML structure. Same rule as Story 1.1 — no tag substitution during paste cleaning.

**Do NOT touch:** `pdf-version/index.js`, print CSS, server-side files.

### Project Structure Notes

- Change is entirely within `wwwroot/scripts/case/index.js`
- No new files created
- No build step required

### References

- [Source: architecture-mmria-v4.1.md#FR-1.3 — Cut/paste cursor integrity]
- [Source: architecture-mmria-v4.1.md#FR-1 — Overriding constraint: do not change the generated HTML structure]
- [Source: architecture-mmria-v4.1.md#4.2 — Client-side implementation patterns]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
