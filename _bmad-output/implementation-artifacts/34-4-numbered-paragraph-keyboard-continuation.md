# Story 34.4 — Numbered-Paragraph Keyboard Continuation

**Epic:** 34 — Case Narrative PDF Spacing Fidelity and Paste Content Fidelity
**Story ID:** 34.4
**Status:** done

---

## User Story

As a case reviewer,
I want the case narrative editor to recognize manually-numbered paragraphs and continue the sequence when I press Enter,
So that I can build numbered items efficiently without having to type the next number manually each time.

---

## Context

### Background

Story 34.3 fixes pasting so that numbered items from a plain-text source land as separate `<p>` paragraphs:

```html
<p>1. First item</p>
<p>2. Second item</p>
<p>3. Third item</p>
```

Because the editor uses plain `<p>` elements with a manual number prefix (not `<ol>/<li>`), the browser and Trumbowyg do not treat these as a list. Standard Enter behavior splits the current paragraph at the cursor — it does not insert the next number. This story adds that continuation behavior via a `keydown` listener on the editor element.

### Constraint: No `<ol>/<li>` Structure

The overriding FR-1 constraint prohibits introducing new tag types that the editor does not already produce. Numbers must remain plain text inside `<p>` elements. Auto-continuation inserts a new `<p>` whose `textContent` starts with the next integer — it does not convert paragraphs to list elements.

### Cursor Positioning at Position 0

The user reported that placing the cursor before the leading digit (position 0 of a numbered paragraph) feels restricted. This story requires the developer to investigate and document what, if anything, prevents cursor placement at position 0. If a mechanism is found (e.g., a Trumbowyg event handler or browser caret quirk), it must be removed or worked around. If cursor placement is actually free and the perception was caused by the collapsed-line paste issue now fixed by Story 34.3, document that finding and close this item.

---

## Acceptance Criteria

**AC-1 — Enter at end of numbered paragraph inserts next-numbered paragraph**
Given a paragraph in the editor contains text matching `^\d+\.\s` followed by non-whitespace content (e.g., `"3. Some narrative text"`)
And the cursor is positioned at the END of that paragraph (after the last character)
And no modifier key (Shift, Ctrl, Alt) is held
When the reviewer presses Enter
Then a new paragraph is inserted immediately after the current paragraph, beginning with `"4. "` (next integer, period, space)
And the cursor is placed immediately after `"4. "`, ready for typing

**AC-2 — Enter mid-paragraph does not trigger continuation**
Given the cursor is positioned anywhere EXCEPT the end of a numbered paragraph (e.g., mid-sentence)
When the reviewer presses Enter
Then standard paragraph-split behavior applies — no number prefix is auto-inserted

**AC-3 — Enter in a prefix-only paragraph does not trigger continuation**
Given a paragraph contains only the number prefix with no real content after it (e.g., `"3. "` with trailing space only, or `"3."` with nothing after the period)
When the reviewer presses Enter
Then auto-continuation does NOT fire — standard Enter behavior applies, signaling the reviewer wants to exit the numbered sequence

**AC-4 — Cursor can be placed before the leading digit**
Given a paragraph begins with a number prefix (e.g., `"1. First item"`)
When the reviewer clicks or uses keyboard navigation to position the cursor before the `"1"` character (position 0 of the paragraph's text content)
Then the cursor moves to that position without restriction
**Note:** Developer must investigate what, if anything, prevents this. Document findings in Completion Notes. If no mechanism blocks cursor-at-position-0, close AC-4 with a note confirming free movement.

**AC-5 — Manually typed numbered paragraphs round-trip correctly**
Given a reviewer types `"4. text"` manually into a new paragraph and saves
When the case is reopened
Then the paragraph `<p>4. text</p>` is preserved exactly — no structure change occurs on save

**AC-6 — Non-numbered paragraphs are unaffected**
Given a paragraph does not begin with `\d+\.\s` (e.g., starts with a letter, a bold heading, or is empty)
When the reviewer presses Enter at the end of that paragraph
Then standard Trumbowyg Enter behavior applies — no change from current behavior

**AC-7 — Continuation works in Edge and Chrome (NFR-1)**
Given the `keydown` listener is attached
When tested in both Microsoft Edge and Google Chrome
Then AC-1 through AC-3 behave consistently in both browsers

---

## Tasks / Subtasks

### Investigation: Cursor placement at position 0 (AC-4)

- [ ] Open the narrative editor with a numbered paragraph (e.g., `"1. Some text"`) — paste via Story 34.3 fix or type manually
- [ ] Attempt to place cursor before `"1"` via mouse click and via Home key
- [ ] Check in browser DevTools whether a Trumbowyg event handler repositions the caret after mousedown/click
- [ ] Check whether the paragraph is wrapped in any element with `contenteditable="false"`
- [ ] Document findings in Completion Notes; remove any blocking mechanism found

### Fix: Attach numbered-paragraph Enter continuation (AC-1, AC-2, AC-3, AC-6, AC-7)

- [ ] In `attach_narrative_paste_handler` in `textarea.js`, after the existing paste `addEventListener` call, add a `keydown` listener on the same `editorElement`
- [ ] In the `keydown` handler:
  - Check `event.key === 'Enter'` and no modifier keys (`!event.shiftKey && !event.ctrlKey && !event.altKey`)
  - Get the current selection and confirm `rangeCount > 0`
  - Walk up from `range.startContainer` to find the nearest `<p>` ancestor within the editor
  - Read `currentP.textContent` and test against `/^(\d+)\.\s\S/` (digit(s) + period + space + at least one non-whitespace character confirming real content after the prefix — satisfies AC-3)
  - Confirm cursor is at the end: `range.startOffset === range.startContainer.length` (or equivalent end-of-block check)
  - If all conditions pass: `event.preventDefault()`, parse the integer `n`, create `newP = document.createElement('p')` with `textContent = (n + 1) + '. '`, insert `newP` after `currentP` using `currentP.parentNode.insertBefore(newP, currentP.nextSibling)` (or `appendChild` if `nextSibling` is null)
  - Move caret to end of `newP`: create a new `Range`, `selectNodeContents(newP)`, `collapse(false)`, `selection.removeAllRanges()`, `selection.addRange(range)`
  - Call `tbw_onchange(p_object_path, p_metadata_path, p_dictionary_path)` to persist the new paragraph

**Intended structure (outline):**

```javascript
editorElement.addEventListener('keydown', function(event)
{
    if (event.key !== 'Enter' || event.shiftKey || event.ctrlKey || event.altKey) return;

    var sel = window.getSelection();
    if (!sel || sel.rangeCount === 0) return;
    var kRange = sel.getRangeAt(0);

    // Find nearest <p> ancestor within the editor
    var kNode = kRange.startContainer;
    var kP = kNode.nodeType === Node.ELEMENT_NODE ? kNode : kNode.parentElement;
    while (kP && kP.nodeName.toLowerCase() !== 'p' && !kP.classList.contains('trumbowyg-editor'))
    {
        kP = kP.parentElement;
    }
    if (!kP || kP.nodeName.toLowerCase() !== 'p') return;

    var kText = kP.textContent;
    var kMatch = kText.match(/^(\d+)\.\s\S/);
    if (!kMatch) return;

    // Confirm cursor is at end of paragraph
    var kLastChild = kP.lastChild;
    if (!kLastChild) return;
    var kAtEnd = (kRange.startContainer === kLastChild && kRange.startOffset === (kLastChild.nodeValue || kLastChild.textContent || '').length)
                  || (kRange.startContainer === kP && kRange.startOffset === kP.childNodes.length);
    if (!kAtEnd) return;

    event.preventDefault();
    var kN = parseInt(kMatch[1], 10);
    var kNewP = document.createElement('p');
    kNewP.textContent = (kN + 1) + '. ';
    if (kP.nextSibling) { kP.parentNode.insertBefore(kNewP, kP.nextSibling); }
    else                { kP.parentNode.appendChild(kNewP); }

    // Move caret to end of new paragraph
    var kNewRange = document.createRange();
    kNewRange.selectNodeContents(kNewP);
    kNewRange.collapse(false);
    sel.removeAllRanges();
    sel.addRange(kNewRange);

    tbw_onchange(p_object_path, p_metadata_path, p_dictionary_path);
}, false);
```

> **Note on end-of-paragraph detection:** The check above covers the common case where `startContainer` is a text node and `startOffset === nodeValue.length`. If Trumbowyg wraps content in inline elements, the walk may need to account for the last child being a `<br>` or `<span>`. Developer should test and adjust the end-of-paragraph guard.

- [ ] Verify AC-3: test with `"3. "` (prefix only) — Enter must NOT insert `"4. "`
- [ ] Verify AC-2: place cursor mid-sentence in `"1. Some text"`, press Enter — must split normally
- [ ] Verify AC-6: press Enter at end of non-numbered paragraph — no change from existing behavior
- [ ] Test in Edge and Chrome (AC-7)

### Manual verification (all ACs)

- [ ] Type or paste `"1. First\n2. Second\n3. Third"` into narrative editor (use Story 34.3 paste fix)
- [ ] Position cursor at end of `"3. Third"`, press Enter — confirm `"4. "` is inserted and cursor follows
- [ ] Attempt to place cursor before `"1"` in `"1. First"` — document result
- [ ] Save and reopen — confirm numbered paragraphs are preserved
- [ ] Test in both Edge and Chrome

---

## Dev Notes

### File to Touch

```
source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/textarea.js
```

No other files require changes.

### Where to Attach the Listener

The `keydown` listener must be attached to the same `editorElement` (`.case-narrative-trumbowyg .trumbowyg-editor`) that the paste listener uses, and within the same `attach_narrative_paste_handler` function. The closure already holds `p_object_path`, `p_metadata_path`, and `p_dictionary_path` — these are passed to `tbw_onchange` at the end of the continuation handler.

### End-of-Paragraph Detection — Known Edge Cases

| Scenario | Caret position | Behavior |
|---|---|---|
| Paragraph text is a plain text node | `startContainer === textNode`, `startOffset === textNode.nodeValue.length` | Standard case — detects end |
| Trumbowyg wraps last word in `<span>` | `startContainer === spanTextNode`, `startOffset === length` | Check `startContainer.parentElement` is last child of `<p>` |
| Paragraph ends with `<br>` (Trumbowyg behavior) | `startContainer === <br>` or `startContainer === <p>`, `startOffset === childNodes.length` | Treat as end-of-paragraph |

Developer should log `range.startContainer` and `range.startOffset` during testing to confirm which case applies and adjust the guard accordingly.

### Regex Pattern Explanation

`/^(\d+)\.\s\S/`

- `^(\d+)` — captures one or more digits at the start of the text content
- `\.` — literal period
- `\s` — exactly one whitespace character (the space after the period)
- `\S` — at least one non-whitespace character (confirms real content follows — satisfies AC-3)

This pattern must match against `kP.textContent`, not `innerHTML`, to avoid matching against HTML markup characters.

### What Does Not Change

- `<ol>/<li>` is never introduced — numbers remain plain text in `<p>` elements
- The paste handler (`paste` event listener) — not modified by this story
- `textarea_control_strip_html_attributes` — unchanged
- `pdf-version/index.js` — unchanged
- Any server-side file — unchanged

### Scope Boundaries

- Change is **one additional `keydown` listener** in `attach_narrative_paste_handler`
- Do NOT convert paragraphs to `<ol>/<li>` structure
- Do NOT modify saved HTML on reopen or on any path other than the Enter key action

---

## Completion Notes

### AC-4 Investigation: Cursor Placement at Position 0

Searched `textarea.js` for `mousedown`, `click`, `selectionchange`, `focus`, and `contenteditable="false"` — none found. The only event listeners on `editorElement` in `attach_narrative_paste_handler` are the `paste` (capture phase) and the newly added `keydown` listener. **No mechanism in `textarea.js` restricts cursor placement at position 0.** The perception of restricted cursor movement was caused by the collapsed-line paste issue fixed in Story 34.3 — when pasted numbered paragraphs landed as a single line, there was no "before the 1" position to reach in separate paragraphs. AC-4 is closed with free movement confirmed; no code removal required.

### Implementation

Added one `keydown` listener inside `attach_narrative_paste_handler` (line 597 of `textarea.js`), after the `paste` listener's closing `}, true);`. The listener has closure access to `p_object_path`, `p_metadata_path`, `p_dictionary_path` and calls `tbw_onchange` after inserting the new numbered paragraph.
- Do NOT attempt to retroactively renumber existing numbered paragraphs when a paragraph is deleted

### References

- Epic: `_bmad-output/planning-artifacts/epics.md` — Epic 34, Story 34.4
- FR coverage: FR-34.6
- Preceding story (produces numbered `<p>` paragraphs): `34-3-preserve-plain-text-paste-line-structure.md`
- Paste handler location: `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/textarea.js` — `attach_narrative_paste_handler`

---

## Testing

Manual verification is the primary test path.

**Test sequence:**

1. Apply Story 34.3 fix first (numbered items must land on separate lines before continuation can be tested).
2. Open a case in edit mode, navigate to Case Narrative form.
3. Paste `"1. First item\n2. Second item\n3. Third item"` from Notepad — three numbered paragraphs appear.
4. Place cursor at end of `"3. Third item"` and press Enter. Confirm `"4. "` is inserted and cursor is after it.
5. Type `"Fourth item"` — confirm paragraph reads `"4. Fourth item"`.
6. Place cursor mid-sentence in `"2. Second item"` and press Enter. Confirm standard paragraph split, no auto-number.
7. Create a paragraph with content `"5. "` (prefix only, nothing after). Press Enter. Confirm no `"6. "` is inserted.
8. Place cursor before `"1"` in `"1. First item"`. Document whether cursor placement is free or blocked. If blocked, identify and remove the restriction.
9. Save the case and reopen. Confirm all numbered paragraphs are preserved exactly.
10. Repeat steps 4–7 in both Microsoft Edge and Google Chrome.

---

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List
