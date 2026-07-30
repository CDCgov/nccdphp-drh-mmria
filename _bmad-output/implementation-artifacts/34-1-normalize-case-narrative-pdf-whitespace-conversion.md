# Story 34.1 — Normalize Case Narrative PDF Whitespace Conversion

**Epic:** 34 — Case Narrative PDF Spacing Fidelity
**Story ID:** 34.1
**Status:** done

---

## User Story

As a case reviewer,
I want edited case narrative HTML to render in the PDF with normal paragraph spacing,
So that adding a line in the narrative editor does not make the exported PDF harder to read.

---

## Context

### Root Cause: Two Distinct Defects in `ConvertHTMLDOMWalker`

**Evidence files** (do not modify — use as regression fixtures):

| File | Description |
|---|---|
| `docs/ai/local/case-narrative-spacing/unchanged-prod-data.txt` | Original narrative HTML saved before any v4.1 edit. 70 newlines, no inter-tag spaces, no `<p><br></p>`. PDF is correct. |
| `docs/ai/local/case-narrative-spacing/changed-prod-data-v4.1.txt` | Same narrative HTML after adding one line in v4.1 and saving. Zero newlines, 29 literal `> <` inter-tag spaces, one `<p><br></p>`. PDF has extra spacing throughout. |
| `docs/ai/local/case-narrative-spacing/GA-2026-8808_2026-07-29T15_23_00.228Z-v4.1.pdf` | Buggy PDF from v4.1 (extra vertical spacing). |
| `docs/ai/local/case-narrative-spacing/GA-2026-8808_2026-07-29T15_23_54.394Z-prod.pdf` | Correct PDF from production. |

---

#### Defect 1 — Structural whitespace-only `#TEXT` nodes between block tags

Trumbowyg v4.1 changed how it serializes the narrative HTML on save. The pre-v4.1 output used actual newlines between tags:

```
</p><br><p>\nShe...
```

The v4.1 output serializes the entire narrative as a **single line**, with literal space characters between sibling block tags:

```
</p><br> <p>She...
       ^
       literal space — becomes a #TEXT child of BODY
```

When `node.innerHTML` is parsed by the browser DOM, that literal space becomes a `#TEXT` child of the outer `BODY` element, between two block-level siblings (`<br>` and `<p>`). The changed narrative has **29 such inter-tag spaces**.

The current `#TEXT` case in `ConvertHTMLDOMWalker` pushes every text node unconditionally:

```javascript
case "#TEXT":
    p_result.push({ text: p_node.textContent.replace(/\u00a0/g, ' ').replace(/[\n\r\t]/g, '').replace('<br>', '\n') });
    return;
```

A space character `" "` survives all three replacements and is pushed as `{ text: " " }`. pdfMake renders this as a non-empty text block that occupies a full line height, producing a visible blank row before every paragraph in the edited narrative.

**Why the unchanged HTML does not trigger this**: Newline characters between tags (`\n`) are stripped by `.replace(/[\n\r\t]/g, '')`, producing `{ text: "" }`. An empty text object in pdfMake does not create a visible line. The v4.1 space character survives because it is not `\n`, `\r`, or `\t`.

---

#### Defect 2 — `<p><br></p>` emits two newlines instead of one

Trumbowyg represents an intentional blank paragraph as `<p><br></p>`. The `P` case processes children then appends its own trailing newline:

```javascript
case "P":
    let text_array = [];
    for (...) { ConvertHTMLDOMWalker(text_array, child); }
    text_array.push({ text: "\n" });   // paragraph trailing newline
    // ... push to result
```

For `<p><br></p>`:
1. `BR` child → `text_array = [{ text: "\n" }]`
2. Trailing push → `text_array = [{ text: "\n" }, { text: "\n" }]`

Result: **two newlines** in the PDF for one intentional blank paragraph.

The unchanged production narrative has no `<p><br></p>` elements, so it was never affected. The changed narrative has one, which is compounded by the 29 structural separators above.

---

#### Inline spacing must be preserved

The narrative contains inline constructs such as:

```html
<p><strong>This</strong> is a <strong>test</strong>.</p>
```

The `#TEXT` node `" is a "` is a child of `<p>`, not of `BODY`. Any fix must distinguish structural whitespace (children of the outer BODY container between block siblings) from meaningful inline whitespace (children of inline-containing elements like `<p>`, `<span>`, `<li>`).

---

### Code Location

**Primary file:** `source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js`

**Entry point:** `convert_html_to_pdf(p_value)` at line 1222

```javascript
function convert_html_to_pdf(p_value) {
    let result = [];
    let CommentRegex = /<!--\[[^>]+>/gi;
    let node = document.createElement("body");
    node.innerHTML = p_value.replace(CommentRegex, "");
    ConvertHTMLDOMWalker(result, node);   // node is the outer BODY element
    return result;
}
```

**Walker:** `ConvertHTMLDOMWalker(p_result, p_node)` at line 1379

Key cases to modify:

- `#TEXT` case at line 1482
- `P` / `DIV` case at line 1488

**Call site:** line 2592–2593 — the narrative field is the only caller of `convert_html_to_pdf`.

---

## Acceptance Criteria

**AC-1 — Structural whitespace-only separator nodes are skipped**
Given the edited narrative HTML (`changed-prod-data-v4.1.txt`) where Trumbowyg has placed literal spaces between block-level sibling tags
When `ConvertHTMLDOMWalker` processes a `#TEXT` node whose content is whitespace-only after stripping `\u00a0`, `\n`, `\r`, and `\t`
And the parent of that `#TEXT` node is the outer `BODY` container (or another block-only structural container)
Then that text node is not pushed to the PDF result — no extra blank rows appear in the PDF

**AC-2 — `<p><br></p>` renders as exactly one intentional blank line**
Given the edited narrative HTML contains `<p><br></p>` as an intentional blank paragraph
When `ConvertHTMLDOMWalker` processes that `P` element
Then the PDF result contains exactly one newline for that paragraph (not two)

**AC-3 — Inline spacing between inline elements is preserved**
Given inline HTML such as `<p><strong>This</strong> is a <strong>test</strong>.</p>`
When the PDF is generated
Then the words do not collapse together — the ` is a ` space text node produces visible spacing between the two bold words

**AC-4 — Unchanged narrative PDF spacing is not made tighter**
Given the unchanged production narrative (`unchanged-prod-data.txt`)
When the PDF is generated with the fix applied
Then paragraph spacing is not tighter than the correct production PDF (`GA-2026-8808_2026-07-29T15_23_54.394Z-prod.pdf`)

**AC-5 — The stored narrative HTML is not modified**
Given the fix is applied
When a case is saved or reopened
Then `g_data.case_narrative.case_opening_overview` is byte-for-byte identical to what it was before — the fix is in the PDF rendering path only

**AC-6 — No changes outside `pdf-version/index.js`**
Given the fix satisfies AC-1 through AC-5 using changes to `pdf-version/index.js` only
Then `textarea.js`, the save sanitizer, the Trumbowyg integration, and all server-side files are not modified

---

## Tasks / Subtasks

### Fix 1: Skip structural whitespace-only `#TEXT` nodes (AC-1, AC-3, AC-4)

- [x] In `ConvertHTMLDOMWalker`, locate the `#TEXT` case (line 1482)
- [x] Wrap the existing logic in a block (add `{ }` for `const` scoping) and compute `raw` once before the push
- [x] After computing `raw`, add a guard: if `raw` is whitespace-only (`/^\s*$/.test(raw)`) AND the parent node is the outer `BODY` element, `return` without pushing

**Intended transformation:**

```javascript
// BEFORE:
case "#TEXT":
    // Do NOT use .trim() here: ...
    p_result.push({ text: p_node.textContent.replace(/\u00a0/g, ' ').replace(/[\n\r\t]/g, '').replace('<br>', '\n') });
    return;
    break;

// AFTER:
case "#TEXT": {
    // Do NOT use .trim() here: it strips &nbsp; (\u00a0) to empty string, losing
    // the space between an inline element and the following text.
    // Instead: replace &nbsp; with a regular space, remove HTML formatting
    // line-breaks/tabs, and preserve all space characters.
    const raw = p_node.textContent.replace(/\u00a0/g, ' ').replace(/[\n\r\t]/g, '').replace('<br>', '\n');
    // Skip structural whitespace-only separator nodes. Trumbowyg v4.1 serializes
    // narrative HTML as a single line with literal spaces between block-level siblings
    // (e.g., "<br> <p>"). These become #TEXT children of BODY and produce visible
    // blank rows in the PDF. Inline spaces inside <p>/<span>/etc. are unaffected
    // because their parent is not BODY.
    if (/^\s*$/.test(raw) && p_node.parentNode &&
            p_node.parentNode.nodeName.toUpperCase() === 'BODY') {
        return;
    }
    p_result.push({ text: raw });
    return;
    break;
}
```

> **Note on `BODY` check**: `convert_html_to_pdf` creates the container with `document.createElement("body")`, so the outer node's `nodeName` is always `"BODY"`. Structural whitespace-only nodes between top-level block siblings (P, BR, DIV, HR) are always direct children of this BODY. Inline whitespace inside P or SPAN has a parent `nodeName` of `"P"`, `"SPAN"`, etc., and is unaffected.

---

### Fix 2: Collapse blank paragraph `<p><br></p>` to a single newline (AC-2, AC-4)

- [x] In `ConvertHTMLDOMWalker`, locate the `P` / `DIV` case (line 1488)
- [x] After the child-walking loop and before `text_array.push({ text: "\n" })`, add a check: if every item in `text_array` is `{ text: "\n" }` (a blank paragraph whose only content is one or more BR-derived newlines), push a single `{ text: "\n" }` and return early — do not push the paragraph trailing newline on top

**Intended insertion** (just before `text_array.push({ text: "\n" })`):

```javascript
// Blank paragraph guard: <p><br></p> produces one BR-derived newline in text_array.
// Without this, the paragraph's own trailing "\n" stacks on top, creating a
// double blank line in the PDF for each intentional empty paragraph.
// Collapse the entire paragraph to a single blank line.
if (text_array.length > 0 &&
        text_array.every(item => Object.keys(item).length === 1 && item.text === '\n')) {
    p_result.push({ text: '\n' });
    return;
}
text_array.push({ text: "\n" });
// ... existing canvas-filtering code continues unchanged
```

> **Scope**: Only triggers when ALL children resolved to `{ text: "\n" }` — purely blank paragraphs. A paragraph with any visible content (`<p>Some text<br></p>`) evaluates `every(...)` as `false` and takes the existing path.

---

## Dev Notes

### File to Touch

```
source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js
```

No other files require changes. There is no build step — the file is served as-is from `wwwroot`.

### Regression Verification

After applying the fix, manually verify against the two fixture files:

1. Load `unchanged-prod-data.txt` into a test case's `case_opening_overview` and export the narrative PDF. Compare paragraph spacing against `GA-2026-8808_2026-07-29T15_23_54.394Z-prod.pdf`. Spacing must not be tighter.

2. Load `changed-prod-data-v4.1.txt` into a test case's `case_opening_overview` and export the narrative PDF. Extra blank rows between every paragraph should be gone. The `<p><br></p>` after the first line should render as one blank line.

3. Verify inline spacing: check that `<p><strong>This</strong> is a <strong>test</strong>.</p>` renders as "**This** is a **test**." — not "**This**is a**test**."

### What Does Not Change

- `g_data.case_narrative.case_opening_overview` — stored HTML is not touched
- `textarea.js` — save sanitizer is not touched
- Trumbowyg integration — not touched
- All server-side files — not touched
- Any other function in `pdf-version/index.js` — not touched

### Source References

- Epic: `_bmad-output/planning-artifacts/epics.md` — Epic 34, Story 34.1
- Evidence fixtures: `docs/ai/local/case-narrative-spacing/`
- Walker entry point: `pdf-version/index.js` line 1222 (`convert_html_to_pdf`)
- Walker implementation: `pdf-version/index.js` line 1379 (`ConvertHTMLDOMWalker`)
- `#TEXT` case: line 1482
- `P` / `DIV` case: line 1488
- Call site (narrative field): line 2592–2593
- FR coverage: FR-34.1, FR-34.2, FR-34.3 in `_bmad-output/planning-artifacts/epics.md`

---

## Testing

Manual verification against the two evidence PDFs is the primary test path (see Regression Verification above).

If automated test coverage is desired, a unit-level test in `pdf-version/index.js` can exercise `convert_html_to_pdf` directly in a jsdom or browser test harness, asserting on the shape of the returned pdfMake content array:

- `changed-prod-data-v4.1.txt` → no consecutive `{ text: " " }` items in the result array
- `<p><br></p>` → exactly one `{ text: "\n" }` emitted
- `<p><strong>This</strong> is a <strong>test</strong>.</p>` → result contains a text fragment with ` is a ` preserved
