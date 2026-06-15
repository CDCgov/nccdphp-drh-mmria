# Story 1.1: Fix Save-Path HTML Stripping

Status: ready-for-dev

## Story

As a case reviewer,
I want formatting I apply in the case narrative editor to be preserved when I save and reopen a case,
so that line breaks, underline, horizontal rules, and font sizes render consistently in the editor, print view, and PDF.

## Acceptance Criteria

1. A case narrative containing explicit line breaks (`<br>`), underline (`<u>`), horizontal rule (`<hr>`), and font size (`<font size="...">`) markup survives a save/reload cycle — the stored HTML is byte-for-byte identical to what the editor produced.
2. The developer audits `mmria_get_narrative_save_snapshot()` in `case/index.js` and identifies the stripping call(s) on the narrative field. The primary candidate is `textarea_control_strip_html_attributes()` near line 4356 (currently commented out). The stripping call is either removed or scoped so it no longer applies to the narrative field value.
3. If sanitization is retained on the save path (XSS vector removal), it removes only executable attributes (`onclick`, `onerror`, `javascript:` hrefs). Structural tags (`<br>`, `<u>`, `<hr>`, `<font>`) are preserved unchanged.
4. Existing case data in CouchDB that was saved in the stripped form (pre-fix) renders as-is when opened — the fix does not reprocess or migrate old stored content.

## Tasks / Subtasks

- [ ] Audit the narrative save path (AC: #1, #2)
  - [ ] Search `case/index.js` for `mmria_get_narrative_save_snapshot()` and trace every HTML transformation applied to `g_data.case_narrative.case_opening_overview` before write
  - [ ] Locate `textarea_control_strip_html_attributes()` call near line 4356 (currently commented); confirm this is the only stripping site on the narrative path or find additional ones
  - [ ] Document all stripping calls found
- [ ] Remove or scope the stripping call (AC: #2)
  - [ ] Either remove the call from the narrative path, or add a condition/parameter that excludes the narrative field from stripping
  - [ ] If multiple stripping calls exist, address each one — do not leave partial stripping in place
- [ ] Audit retained sanitization (AC: #3)
  - [ ] If any sanitization remains on the save path, verify it targets only `onclick`, `onerror`, and `javascript:` hrefs
  - [ ] Confirm `<br>`, `<u>`, `<hr>`, `<font size="...">` survive the sanitizer unchanged
- [ ] Round-trip test (AC: #1, #4)
  - [ ] Enter narrative with `<br>`, `<u>`, `<hr>`, font-size formatting → save → reload → confirm all formatting present in editor
  - [ ] Open an existing case with stripped narrative (old data) → confirm editor renders stored content without error

## Dev Notes

**Primary file:** `wwwroot/scripts/case/index.js`

**No server-side changes.** FR-1 is JavaScript-only.

**Key functions:**
- `mmria_get_narrative_save_snapshot()` — the save path entry point
- `textarea_control_strip_html_attributes()` near line 4356 — prior fix candidate (currently commented out)
- The caller that writes to `g_data.case_narrative.case_opening_overview`

**Overriding constraint — do not change the generated HTML structure:**
The PDF and HTML print generators are tightly coupled to the HTML structure stored in the narrative field. The fix must STOP stripping only. No tag substitution is permitted:
- ❌ Do NOT switch `<br>` → `<p>`
- ❌ Do NOT switch `<font>` → `<span style="...">`
- ❌ Do NOT change tag nesting or add wrapper elements
- ✅ The editor's native output is the authoritative format — preserve it exactly

**Backward compatibility:** The fix is on the save path only. Old stored data in its stripped form must not be reprocessed. When an old case is opened, the editor renders whatever is stored — no migration.

**Do NOT touch:** `pdf-version/index.js`, print CSS, any Razor views, or any server-side files for this story.

**Open item OI-3 resolved here:** Developer confirms whether the prior commented-out fix at ~line 4356 is complete or partial, and whether additional stripping calls exist elsewhere on the narrative path.

### Project Structure Notes

- Change is entirely within `wwwroot/scripts/case/index.js`
- No new files created
- No build step required for JS changes

### References

- [Source: architecture-mmria-v4.1.md#FR-1.1 — Line break persistence]
- [Source: architecture-mmria-v4.1.md#FR-1 — Overriding constraint: do not change the generated HTML structure]
- [Source: architecture-mmria-v4.1.md#2.4 — Case narrative editor]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
