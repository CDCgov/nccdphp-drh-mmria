# Story 1.2: Fix Paste Handler Cursor Integrity

Status: done

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

- [x] Locate and understand the existing paste handler (AC: #1, #2, #3)
  - [x] Find `page_render_create_onpaste_event()` in `case/index.js`
  - [x] Identify root cause: selection state not captured before paste executes, or stale selection range used
  - [x] Note any `document.execCommand('insertHTML')` calls — these must be replaced
- [x] Rewrite paste handler with Range API (AC: #1, #2, #3)
  - [x] At the top of the event handler, synchronously capture: `var selection = window.getSelection(); var range = selection.getRangeAt(0);`
  - [x] Delete any currently selected content: `range.deleteContents();`
  - [x] Build a DocumentFragment from the cleaned paste content
  - [x] Insert at captured range: `range.insertNode(fragment);`
  - [x] Collapse range to end of inserted content: `range.collapse(false);`
  - [x] Restore selection: `selection.removeAllRanges(); selection.addRange(range);`
- [x] Implement XSS-safe paste cleaning (AC: #4)
  - [x] Strip `onclick`, `onerror`, and `javascript:` href attributes from pasted HTML
  - [x] Preserve all structural tags (`<b>`, `<i>`, `<u>`, `<br>`, `<p>`, `<font>`, etc.)
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

Claude Sonnet 4.6

### Debug Log References

- Spec named `page_render_create_onpaste_event()` in `case/index.js` as target, but that function lives in `page_renderer.js` and handles max_length input attributes — unrelated to narrative editor paste. Actual narrative paste wiring is in `textarea.js` (`textarea_render` / `tbw_change_paste`). Implemented there.
- The existing `tbwpaste` → `tbw_change_paste` path handles the *save* path only (fires after Trumbowyg has already inserted content). Cursor integrity requires intercepting the native `paste` event *before* Trumbowyg inserts — done via capture-phase listener.
- `DOMWalker` (already in `textarea.js`) strips `on*` and `javascript:` attrs while preserving structural tags — reused for XSS cleaning rather than duplicating logic.

### Completion Notes List

- **Actual file changed:** `wwwroot/scripts/editor/page_renderer/textarea.js` (not `case/index.js` as spec stated — the narrative editor paste wiring lives here).
- Added `attach_narrative_paste_handler(p_object_path, p_metadata_path, p_dictionary_path)` — a capture-phase native `paste` listener on `.case-narrative-trumbowyg .trumbowyg-editor`.
- Handler synchronously captures `window.getSelection().getRangeAt(0)` at the very top (AC #1, #2), calls `range.deleteContents()`, builds a `DocumentFragment` from clipboard HTML cleaned via `DOMWalker` (AC #4), inserts with `range.insertNode(fragment)` (AC #3), then collapses and restores selection (AC #1, #2).
- `event.stopImmediatePropagation()` prevents Trumbowyg's bubble-phase handler from also processing the paste; `tbw_onchange` is called directly to save via the existing data path.
- AC #5 (Edge/Chrome validation) requires manual testing by the team.

### File List
