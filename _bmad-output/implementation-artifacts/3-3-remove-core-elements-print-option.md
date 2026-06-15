# Story 3.3: Remove Core Elements Only Print Option

Status: ready-for-dev

## Story

As a case reviewer,
I want the "Core Elements Only" option removed from all affected print dropdowns,
so that users can no longer select an unauthorized print format.

## Acceptance Criteria

1. `<option value="core-summary">Core Elements Only</option>` is absent from the print dropdown in `form.mmria.js` (~line 2047).
2. The same option is absent from the print dropdown in `form.committee_member.mmria.js` (~line 1851).
3. The same option is absent from the print dropdown in `de-identified/index.js` (~line 1131), and the `core-summary` redirect guard (~line 933) is removed if (and only if) it exclusively guards that case.
4. All four dead-code items in `pdf-version/index.js` are removed: `"core-summary": "Core"` in `TitleMap`, `case 'core-summary'` in `getReportTabName()`, `case 'core-summary':` dispatch in `formatContent()`, and the `core_summary()` function (after confirming zero remaining references).
5. PMSS files are not modified — the `core-summary` option in PMSS files is intentionally commented out and must remain untouched.
6. After all removals, a grep of `wwwroot/scripts` for `core-summary` returns zero matches outside of PMSS files.

## Tasks / Subtasks

- [ ] Remove option from `form.mmria.js` (AC: #1)
  - [ ] Open `wwwroot/scripts/editor/page_renderer/form.mmria.js`
  - [ ] Navigate to ~line 2047, find `<option value="core-summary">Core Elements Only</option>`
  - [ ] Remove that line only — do not touch surrounding option elements
- [ ] Remove option from `form.committee_member.mmria.js` (AC: #2)
  - [ ] Open `wwwroot/scripts/editor/page_renderer/form.committee_member.mmria.js`
  - [ ] Navigate to ~line 1851, remove the same option element
- [ ] Remove option and redirect guard from `de-identified/index.js` (AC: #3)
  - [ ] Open `wwwroot/scripts/de-identified/index.js`
  - [ ] Navigate to ~line 1131, remove `<option value="core-summary">Core Elements Only</option>`
  - [ ] Navigate to ~line 933, read the redirect guard block
  - [ ] **If the block exclusively guards `core-summary`:** remove the entire guard block
  - [ ] **If the block guards multiple cases including `core-summary`:** remove only the `core-summary` branch, leave other branches intact
- [ ] Clean up dead code in `pdf-version/index.js` (AC: #4)
  - [ ] Open `wwwroot/scripts/pdf-version/index.js`
  - [ ] ~line 38: remove `"core-summary": "Core"` entry from `TitleMap`
  - [ ] ~line 729: remove `case 'core-summary': return 'Core Elements Only'` branch from `getReportTabName()`
  - [ ] ~lines 774 and 1148: remove `case 'core-summary':` dispatch and its call to `core_summary()` from `formatContent()`
  - [ ] Grep `pdf-version/index.js` for `core_summary` to confirm all call sites are removed
  - [ ] Remove the `core_summary()` function body and declaration — only after confirming zero remaining references
- [ ] Confirm PMSS files untouched (AC: #5)
  - [ ] Do not open or modify any PMSS-related JS files
  - [ ] Intentional `core-summary` comments in PMSS files must remain
- [ ] Final grep verification (AC: #6)
  - [ ] Run: grep `wwwroot/scripts` recursively for `core-summary`
  - [ ] Expected: zero matches outside PMSS files
  - [ ] If any non-PMSS matches remain, address them before completing the story

## Dev Notes

**Files to modify (surgical deletions only — no new code):**
- `wwwroot/scripts/editor/page_renderer/form.mmria.js` (~line 2047)
- `wwwroot/scripts/editor/page_renderer/form.committee_member.mmria.js` (~line 1851)
- `wwwroot/scripts/de-identified/index.js` (~lines 1131 and 933)
- `wwwroot/scripts/pdf-version/index.js` (~lines 38, 729, 774, 1148 + `core_summary()` function)

**Do NOT touch:**
- PMSS-related JS files (the `core-summary` option there is intentionally commented out)

**`de-identified/index.js` redirect guard check:** Before removing the guard at ~line 933, read it carefully. It may guard multiple cases (e.g., checking for various report type values). If so, remove only the `core-summary` branch. If it is a simple `if (reportType === 'core-summary') { redirect; }` — remove the whole block.

**`core_summary()` function removal sequence:**
1. Remove the four call sites in steps above first
2. Grep the entire file for `core_summary` — confirm zero results
3. Only then remove the function declaration

**`pdf-version/index.js` note:** This file is also modified in Story 2.3 (vitals date fix and out-of-range exclusion in PDF). If both stories are worked on the same branch, coordinate to avoid conflicts — the changes are in different sections of the file.

**PMSS confirmation:** After finishing, search for PMSS-related filenames (search for `pmss` in `wwwroot/scripts`) and verify none of those files were modified.

### Project Structure Notes

- All changes are pure deletions in existing JS files
- No new files
- No build step required for JS changes
- Line numbers are approximate — use grep/search to locate exact positions

### References

- [Source: architecture-mmria-v4.1.md#4.1 — Drop the option from three print dropdowns]
- [Source: architecture-mmria-v4.1.md#4.2 — Clean up pdf-version/index.js]
- [Source: architecture-mmria-v4.1.md#4.3 — PMSS scope exclusion]
- [Source: prd-mmria-2026-06-12/prd.md#FR-4.1, FR-4.2]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
