---
baseline_commit: ccc7fe50950e432ff6a195acfb5425ec2c4961ae
---

# Story 1.4: Update Case Narrative Guidelines Panel

Status: done

## Story

As a case reviewer,
I want the Case Narrative guidelines panel to show updated text and formatting,
so that the guidance is clear, consistent with current style, and free from capitalization errors.

## Acceptance Criteria

1. The panel title is changed from `"Case Narrative Template Guidelines:"` to `"Case Narrative"` — colon and the words "Template Guidelines" are removed. Bold heading formatting is retained.
2. All eight list items (three introductory items and five "Remember to:" items) are changed from bullet-point (•) style to dash prefix (`-`).
3. In the second introductory item, `"or Into an external document"` is corrected to `"or into an external document"` (lowercase `i`).
4. The `"Remember to:"` label is changed from bold to plain text.
5. In the third "Remember to:" item, `"Use Inclusive and non-stigmatizing language"` is corrected to `"Use inclusive and non-stigmatizing language"` (lowercase `i`).
6. Trailing periods are removed from all five "Remember to:" list items. The three introductory list items retain their trailing periods.
7. No surrounding markup, element structure, CSS class, or ID is changed — text and formatting content only.

The complete final panel content after all changes:

```
Case Narrative

-You may use this template as a guide, deleting any portions that are not applicable.
-Alternatively, you may copy the reviewer's notes sections below into the final case narrative field or into an external document. You may also use your own template.
-Ensure any narrative you want to copy and paste into the final case narrative field is in plain text without formatting (ctrl+shift+v).

Remember to:
-Focus on the most relative information to the cause of death (see Cause of Death Modules)
-Humanize the story using a story-telling approach
-Use inclusive and non-stigmatizing language
-Spell out acronyms or explain in plain text clinical terminology
-Incorporate interview(s) and CVS throughout (as applicable)
```

## Tasks / Subtasks

- [ ] Locate the source of the "Case Narrative Template Guidelines:" panel (AC: #1–#7)
  - [ ] Search all source files for the string `"Case Narrative Template Guidelines"`
  - [ ] Search for the bullet-point variant phrase: `"Humanize the story"` or `"story-telling approach"` — the version with `•` bullets (distinct from the dash-prefix version already in `page_renderer.js`)
  - [ ] If not found in source JS/Razor files, check CouchDB documents via `database-scripts/` (metadata.json or a config document)
  - [ ] Note the exact file path and line of the match before proceeding
- [ ] Apply the six text/formatting changes (AC: #1–#6)
  - [ ] **If found in a JS file:** Update the string literal in place. Preserve all surrounding push/template-literal structure.
  - [ ] **If found in a `.cshtml` Razor view:** Replace the content in place. Preserve surrounding HTML tags.
  - [ ] **If found in `metadata.json` or a CouchDB document in `database-scripts/`:** Update the value and run the existing production update script.
- [ ] Verify (AC: #7)
  - [ ] Load the Case Narrative form and confirm the panel title, list formatting, and capitalization match the final content above
  - [ ] Confirm no surrounding structure or styling changed

## Dev Notes

**Source file is UNKNOWN** — must be located by search before implementing any changes.

### Key context: what Story 1.3 did (and did NOT cover)

Story 1.3 (done) replaced two instruction lines above the Trumbowyg editor with the dash-prefix instruction text. That update lives in:

- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer.js` (~line 1000) — rendered for the abstractor/reviewer role
- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer.committee_member.js` (~line 422) — rendered for the committee member role

**Both files already contain the correct dash-prefix format** and serve as the reference implementation for this story. The element they render currently looks like:

```
Case Narrative           ← <h2> injected by changeNarrativeLabel (form.mmria.js)

-You may use this template as a guide, deleting any portions that are not applicable.
-Alternatively, you may copy the reviewer's notes sections below into the final case narrative field
 or into an external document. You may also use your own template.
-Ensure any narrative you want to copy and paste into the final case narrative field is in plain text
 without formatting (ctrl+shift+v).

Remember to:
-Focus on the most relative information to the cause of death (see Cause of Death Modules)
-Humanize the story using a story-telling approach
-Use inclusive and non-stigmatizing language
-Spell out acronyms or explain in plain text clinical terminology
-Incorporate interview(s) and CVS throughout (as applicable)
[Trumbowyg editor toolbar]
```

This story is about a **separate UI element** — the "Case Narrative Template Guidelines:" boxed panel — which was NOT modified by Story 1.3 and still shows the old bullet-point format. After the customer reviewed the Story 1.3 result, they requested additional changes so this panel matches the same text and formatting already in `page_renderer.js`.

### Search strategy

Execute in this order; stop when a match is found:

1. **All editor scripts (include ignored files):** `grep -r "Case Narrative Template Guidelines" source-code/mmria/mmria-server/wwwroot/scripts/`
2. **Broader phrase search:** `grep -r "story-telling approach" source-code/` to find any bullet-point (`•`) variant not yet identified
3. **Razor views:** `grep -r "Case Narrative Template" source-code/mmria/mmria-server/Views/`
4. **database-scripts:** Search `source-code/mmria/mmria-server/database-scripts/` for the phrase
5. **nccdphp-drh-mmria-services:** Search services project for the phrase

### If found in a JS string literal

Replace the existing HTML fragment. The current (before) content uses `<ul><li>` bullet markup or equivalent bullet-character syntax. The replacement content uses plain `<p>` or `<br>`-separated dash-prefixed lines, consistent with the `page_renderer.js` pattern from Story 1.3:

```javascript
`<p class="mb-3" style="line-height: normal"><strong>Case Narrative</strong><br>-You may use this template as a guide, deleting any portions that are not applicable.<br>-Alternatively, you may copy the reviewer\'s notes sections below into the final case narrative field or into an external document. You may also use your own template.<br>-Ensure any narrative you want to copy and paste into the final case narrative field is in plain text without formatting (ctrl+shift+v).<br><br>Remember to:<br>-Focus on the most relative information to the cause of death (see Cause of Death Modules)<br>-Humanize the story using a story-telling approach<br>-Use inclusive and non-stigmatizing language<br>-Spell out acronyms or explain in plain text clinical terminology<br>-Incorporate interview(s) and CVS throughout (as applicable)</p>`
```

Adjust surrounding push/template structure to match the found file's existing pattern exactly.

### If found in metadata.json or a CouchDB database-scripts document

Follow the same database-scripts update path established for FR-3 and FR-5. Do not create a new deployment mechanism.

### What NOT to change

- Do NOT modify the `<p>` instruction block already in `page_renderer.js` or `page_renderer.committee_member.js` — those are correct.
- Do NOT modify the `changeNarrativeLabel` heading injection in `form.mmria.js`, `form.abstractor.committee.js`, or any committee member form renderer — those are out of scope.
- Do NOT change element IDs, CSS classes, or any styling properties.

### Project Structure Notes

- Source file location unknown until search is run at implementation time
- No new files expected
- If in database-scripts: run the existing production update script after the change

### References

- [Source: prd-mmria-2026-06-12/prd.md#FR-21 — Case Narrative Instructions Panel Reformatting]
- [Source: 1-3-update-case-narrative-instruction-text.md — Story 1.3 pattern (search-then-replace approach)]
- [Source: source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer.js ~line 1000 — already-correct dash-prefix instruction block (do not touch)]

## Dev Agent Record

### Agent Model Used
Claude Sonnet 4.6

### Completion Notes List
- The `<p>` paragraph in `page_renderer.js` and `page_renderer.committee_member.js` was identified as the guidelines panel. Story 1.3 had already applied ACs #2–#6; Story 1.4 required adding `<strong>Case Narrative</strong><br>` (AC #1) to the start of that paragraph.

### File List
- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer.committee_member.js`
