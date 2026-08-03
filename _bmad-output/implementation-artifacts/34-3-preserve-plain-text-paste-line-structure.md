# Story 34.3 — Preserve Line Structure When Pasting Plain Text

**Epic:** 34 — Case Narrative PDF Spacing Fidelity and Paste Content Fidelity
**Story ID:** 34.3
**Status:** todo

---

## User Story

As a case reviewer,
I want pasted plain-text content to maintain its line breaks in the case narrative editor,
So that text copied from notes, email, or other plain-text sources does not collapse into a single paragraph.

---

## Context

### Root Cause

When clipboard content has **no `text/html`** representation (plain-text sources such as Notepad, email clients, terminal output, or code editors configured to copy as plain text), `attach_narrative_paste_handler` falls into this branch:

```javascript
else if (pastedText)
{
    fragment.appendChild(document.createTextNode(pastedText));
}
```

`pastedText` is a raw string such as `"Line 1\nLine 2\nLine 3"`. A single `Text` node containing raw `\n` characters is inserted directly into the editor's DOM. The browser renders inline `\n` characters as collapsing whitespace — all lines appear on one row.

The same root cause collapses numbered items: pasting `"1. First\n2. Second\n3. Third"` from a plain-text source produces the single-line output `"1. First 2. Second 3. Third"`.

### Why the Existing Save Path is Not the Fix

`textarea_control_strip_html_attributes` replaces `\n` with space (`.replace(crlf_regex," ")`). That line was written to neutralize raw newlines that might survive into serialized HTML. It is not the source of the collapse — the collapse happens at DOM-render time before save. Converting newlines to `<p>` elements in the paste handler means `tbw_onchange` will serialize clean `<p>` elements with no embedded `\n`, so the strip function has nothing to act on and does not interfere.

### Constraint: Do Not Shift the Line-Break Problem

Stories 34.1 and 34.2 fixed PDF rendering of structural whitespace. The fix here must produce `<p>` elements — **not** `<br>` tags — as line separators. Bare `<br>` tags at block level are the exact construct those stories eliminated from the PDF path. Using `<p>` elements matches what the user produces by pressing Enter in the editor and is safe for the PDF converter.

---

## Acceptance Criteria

**AC-1 — Plain-text multi-line paste preserves line structure**
Given clipboard contains `text/plain` only (no `text/html`) with content `"Line 1\nLine 2\nLine 3"`
When the reviewer pastes into the narrative editor
Then three separate `<p>` paragraphs appear in the editor: `<p>Line 1</p>`, `<p>Line 2</p>`, `<p>Line 3</p>`

**AC-2 — Empty lines in pasted plain text become blank paragraphs**
Given clipboard plain text is `"Line 1\n\nLine 3"` (one empty line between content)
When the reviewer pastes
Then the editor contains `<p>Line 1</p>`, `<p><br></p>`, `<p>Line 3</p>` — the blank line is represented as `<p><br></p>`, consistent with Trumbowyg's standard blank-line representation

**AC-3 — Plain-text numbered items land on separate lines**
Given clipboard plain text is `"1. First item\n2. Second item\n3. Third item"`
When the reviewer pastes
Then the editor contains three separate `<p>` paragraphs: `<p>1. First item</p>`, `<p>2. Second item</p>`, `<p>3. Third item</p>`

**AC-4 — Rich-text (HTML) paste path is unchanged**
Given the clipboard contains `text/html` (e.g., content copied from a browser or Word)
When the reviewer pastes
Then the existing `text/html` code path runs unchanged — no regression to rich-text paste behavior

**AC-5 — No raw newlines survive into saved HTML**
Given a plain-text paste produces multiple `<p>` paragraphs and `tbw_onchange` serializes the result
When the content is saved via the standard change path
Then the stored HTML contains `<p>` elements for each pasted line — no raw `\n` characters are embedded in text node content in the serialized HTML

**AC-6 — No nested `<p>` elements are created**
Given the cursor is inside an existing `<p>` paragraph when the paste occurs
When the plain-text paste produces multiple paragraphs
Then the pasted `<p>` elements are inserted as siblings of the cursor's block ancestor, not as children inside it — no `<p><p>…</p></p>` nesting results

**AC-7 — Paste with Windows CRLF line endings is handled**
Given clipboard plain text uses Windows line endings (`\r\n`) between lines
When the reviewer pastes
Then line structure is preserved identically to the `\n` case — CRLF is treated the same as LF

---

## Tasks / Subtasks

### Fix: Convert plain-text paste lines to `<p>` elements (AC-1, AC-2, AC-3, AC-5, AC-6, AC-7)

- [ ] In `attach_narrative_paste_handler` in `textarea.js`, locate the `else if (pastedText)` branch (currently: `fragment.appendChild(document.createTextNode(pastedText))`)
- [ ] Replace the single `createTextNode` call with a loop that splits by `/\r?\n/` and builds one `<p>` per line:
  - Non-empty line → `createElement('p')` with `textContent = line`
  - Empty line → `createElement('p')` with `innerHTML = '<br>'`
  - Append each `<p>` to `fragment`
- [ ] After building `fragment`, set `fragmentHasBlocks = true` so the block-safe insertion path runs (pasted `<p>` elements inserted as siblings, not children — satisfies AC-6)
- [ ] If the split produces only one line (no `\n` in plain text), fall through to the single-text-node path for a single short paste — or consistently use a single `<p>` wrapper; verify behavior is natural for one-line plain-text paste

**Intended transformation:**

```javascript
// BEFORE:
else if (pastedText)
{
    fragment.appendChild(document.createTextNode(pastedText));
}

// AFTER:
else if (pastedText)
{
    var _ptLines = pastedText.split(/\r?\n/);
    for (var _pli = 0; _pli < _ptLines.length; _pli++)
    {
        var _ptP = document.createElement('p');
        if (_ptLines[_pli].length > 0)
        {
            _ptP.textContent = _ptLines[_pli];
        }
        else
        {
            _ptP.innerHTML = '<br>';
        }
        fragment.appendChild(_ptP);
    }
}
```

- [ ] Because `fragment` now contains `<p>` elements, ensure `fragmentHasBlocks` is computed AFTER the `pastedHtml` / `pastedText` branching, or set it explicitly after building the plain-text fragment. Confirm the block-safe sibling insertion path is taken.
- [ ] Verify: no `<br>` tags are used as line separators in this path — `<p>` elements only

### Regression check (AC-4)

- [ ] Paste HTML-source content (copy a paragraph from the narrative template in the editor itself, or from a browser) and confirm it still lands correctly — rich-text paste path is unchanged

### Manual verification (AC-1, AC-2, AC-3, AC-5, AC-6, AC-7)

- [ ] Paste `"Line 1\nLine 2\nLine 3"` from Notepad — verify three paragraphs in editor
- [ ] Paste `"1. First\n2. Second\n3. Third"` from Notepad — verify three numbered paragraphs
- [ ] Paste text with an empty line between content — verify blank paragraph appears as `<p><br></p>`
- [ ] Inspect editor DOM to confirm no nested `<p>` elements
- [ ] Save and reopen case — confirm line structure persists

---

## Dev Notes

### File to Touch

```
source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/textarea.js
```

No other files require changes.

### Relevant Functions

| Function | Location | Role |
|---|---|---|
| `attach_narrative_paste_handler` | `textarea.js` | Intercepts paste; contains the `else if (pastedText)` branch to fix |
| `tbw_onchange` | `textarea.js` | Reads editor `.html()` and saves via `g_textarea_oninput` — called at end of paste handler; no change needed |
| `textarea_control_strip_html_attributes` | `textarea.js` | Save-path sanitizer; replaces raw `\n` with space; unchanged — no raw `\n` will survive after the fix |

### Block-Safe Insertion Path

The paste handler already has block-safe sibling insertion for the `text/html` case. The key variable is `fragmentHasBlocks`:

```javascript
var PASTE_BLOCK_TAGS = { 'p':true, 'div':true, 'ul':true, 'ol':true, 'table':true };
var fragmentHasBlocks = false;
for (var _fci = 0; _fci < fragment.childNodes.length; _fci++)
{
    var _fcn = fragment.childNodes[_fci];
    if (_fcn.nodeType === Node.ELEMENT_NODE && PASTE_BLOCK_TAGS[_fcn.nodeName.toLowerCase()])
    {
        fragmentHasBlocks = true;
        break;
    }
}
```

The plain-text fix appends `<p>` elements to `fragment`, so this loop will set `fragmentHasBlocks = true` automatically when it runs. Confirm the loop runs after both the `pastedHtml` and `pastedText` branches — if it currently runs before, move it or set `fragmentHasBlocks = true` explicitly in the plain-text branch.

### What Does Not Change

- `tbw_change_paste` — not called in the current code path (Trumbowyg's bubble-phase handler is suppressed)
- `textarea_control_strip_html_attributes` — unchanged; no `\n` will be present in serialized HTML after this fix
- `pdf-version/index.js` — not touched; `<p>` elements are handled correctly by the existing PDF converter
- Any server-side file — not touched
- The `text/html` paste path — not touched

### Scope Boundaries

- Change is **confined to the `else if (pastedText)` branch** in `attach_narrative_paste_handler`
- Do NOT insert `<br>` as line separators — `<p>` elements only
- Do NOT modify stored narrative HTML on save
- Do NOT change the rich-text (`text/html`) paste path
- Do NOT introduce a build step or external dependency

### Edge Cases

| Scenario | Expected behavior |
|---|---|
| Single-line plain text (no `\n`) | Split produces one element; one `<p>` inserted — functionally identical to text node, but wrapped in `<p>` |
| All-empty paste (empty string) | `pastedText` is falsy; existing guard (`else if (pastedText)`) means this branch does not run |
| Trailing newline in pasted text | Split produces a trailing empty-string element → one trailing `<p><br></p>`; acceptable since the user's clipboard had a trailing newline |

### References

- Epic: `_bmad-output/planning-artifacts/epics.md` — Epic 34, Story 34.3
- FR coverage: FR-34.5, NFR-34.2
- Paste handler: `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/textarea.js` — `attach_narrative_paste_handler`
- Prior PDF stories: `34-1-normalize-case-narrative-pdf-whitespace-conversion.md`, `34-2-collapse-br-empty-paragraph-separators.md`
- Save-path fix context: `1-1-fix-save-path-html-stripping.md` (Story 1.1)

---

## Testing

Manual verification is the primary test path. No automated test harness currently exists for the paste handler.

**Test sequence:**

1. Open a case in edit mode and navigate to the Case Narrative form.
2. Open Notepad (or any plain-text source). Type or paste:
   ```
   Line 1
   Line 2
   Line 3
   ```
3. Copy the three lines and paste into the narrative editor. Verify three separate paragraphs appear.
4. Repeat with numbered content:
   ```
   1. First item
   2. Second item
   3. Third item
   ```
   Verify three separate numbered paragraphs appear.
5. Include an empty line between content. Verify the blank paragraph renders.
6. Inspect the editor DOM (browser DevTools) and confirm no `<p><p>` nesting.
7. Save the case and reopen. Verify the line structure survived the save/reload round trip.
8. Paste content copied from a browser page (rich-text source) and confirm it still works correctly — regression test for the HTML path.

---

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List
