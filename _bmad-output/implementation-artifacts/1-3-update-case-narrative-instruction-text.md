# Story 1.3: Update Case Narrative Instruction Text

Status: done

## Story

As a case reviewer,
I want the Case Narrative form to display updated guidance text,
so that I understand how to write an effective, compliant case narrative using the available tools.

## Acceptance Criteria

1. Both existing instruction lines are removed from the Case Narrative form:
   - "Use the pre-fill text below, and copy and paste from Reviewer's Notes below to create a comprehensive case narrative. Whatever you type here is what will be printed in the Print Version."
   - "CTRL+B to bold, CTRL+I to italicize, CTRL+U to underline"
2. The replacement text appears in their place (preserving line breaks exactly as specified):
   ```
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
3. No surrounding markup or field structure is changed — text content only.
4. If the text originates from a CouchDB document or `metadata.json`, the update is applied via the database-scripts update path, not a Razor/JS edit.

## Tasks / Subtasks

- [ ] Locate the render source (AC: #1, #4)
  - [ ] Search `wwwroot/` for the first distinctive phrase: "Use the pre-fill text below"
  - [ ] Search `Views/` (Razor .cshtml files) for the same phrase
  - [ ] Search `database-scripts/` (metadata.json and config documents) for the same phrase
  - [ ] Note the file type and path of the match
- [ ] Apply the text replacement (AC: #1, #2, #3)
  - [ ] **If found in a `.cshtml` file:** Replace both instruction lines with the approved text in place. Preserve all surrounding HTML tags and attributes. Do not change element types, IDs, classes, or structure.
  - [ ] **If found in `metadata.json` or a CouchDB document in `database-scripts/`:** Update the text value(s) in that source file. Run the production update script to deploy.
  - [ ] Preserve exact line breaks in the replacement text as specified in AC #2
- [ ] Verify (AC: #1, #2, #3)
  - [ ] Load the Case Narrative form and confirm old text is gone
  - [ ] Confirm replacement text appears with correct line breaks
  - [ ] Confirm surrounding markup is unchanged

## Dev Notes

**Unknown source file** — must be located by search before implementation.

**Search strategy:** The first distinctive phrase to search for is:
> `"Use the pre-fill text below"`

Search in order:
1. `wwwroot/scripts/` — may be injected via JavaScript template literal
2. `Views/` — may be in a `.cshtml` partial or view
3. `source-code/mmria/mmria-server/database-scripts/` — may be in `metadata.json` or a CouchDB document source

**Two text lines to remove:**
- Line 1: `"Use the pre-fill text below, and copy and paste from Reviewer's Notes below to create a comprehensive case narrative. Whatever you type here is what will be printed in the Print Version."`
- Line 2: `"CTRL+B to bold, CTRL+I to italicize, CTRL+U to underline"`

**Replacement text (preserve line breaks):**
```
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

**No behavior, configuration, or data changes.** This is a static text replacement only.

**If in database-scripts:** Follow the existing production update script path already established in the codebase. Do not create a new deployment mechanism.

### Project Structure Notes

- Source file unknown until search is run
- No new files expected
- If metadata.json: run existing production update script after change

### References

- [Source: architecture-mmria-v4.1.md#FR-5 — Case Narrative Instruction Text Replacement]
- [Source: prd-mmria-2026-06-12/prd.md#FR-5.1]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
