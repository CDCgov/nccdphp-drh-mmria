# Addendum — prd-mmria-2026-06-12

Technical context and implementation notes captured during discovery. This material belongs in architecture and solution design, not the PRD.

---

## Editor Fidelity (FR-1)

**Bug characterization — FR-1.1 (line breaks):**
The defect is on the save/serialization path, not the display path. Content renders correctly in the editor while the user is typing. Line breaks are lost after save and reopen. Print and PDF views display breaks correctly, suggesting the serialization step strips `<br>` or paragraph tags that the print/PDF renderer handles differently. A prior fix has been applied to the save path; status is unconfirmed pending test coverage.

**Bug characterization — FR-1.3 (cut/paste):**
Ctrl+X / Ctrl+V behavior is erratic — content pastes at random positions (observed at lines 1, 4, 5, 6, 8 in a single session) or between words in a different paragraph rather than at the cursor. This is a cursor/selection state management issue in the rich text editor component.

---

## OMB Expiration Date (FR-3.1)

**Current implementation:**
The OMB expiration date is hardcoded as a `label` type field in `metadata.json` (loaded into CouchDB):
```json
{
  "prompt": "Exp. Date 05/31/2026",
  "name": "omb_expiration_label",
  "type": "label",
  "tags": []
}
```
Located in `source-code/mmria/mmria-server/database-scripts/metadata.json`.

The OMB block renders on the Home page and inline in case forms (confirmed: Committee Decisions form). Architect needs to determine the correct mechanism for making this label's value dynamic at render time without breaking the form definition structure.

**Render surfaces confirmed:**
- Home page — OMB block (Form Approved / OMB No. / Exp. Date)
- Committee Decisions form — same OMB block appears inline

---

## MMRIA Version (FR-3.2)

**Render surface confirmed:**
- Application footer only — "MMRIA V4.0.1" (current value as of 2026-06-12)

---

## Core Elements Removal (FR-4)

**Current implementation:**
`core-summary` section key maps to "Core Elements Only" in `getReportTabName()` in `wwwroot/scripts/pdf-version/index.js` (line ~775). Also present in `TitleMap` as `"core-summary": "Core"`. The `formatContent()` function handles `case 'core-summary':` which dispatches to `core_summary()`. Three client-side print dropdown render locations need to be identified before implementation.

PMSS-specific dropdowns are excluded from scope.

---

## Vitals Validation (FR-2)

**Forms and grid names (from index.js):**
- `transport_vital_signs` — Medical Transport form
- `vital_signs` — appears on multiple forms
- `routine_monitoring` — Prenatal form

Architect should confirm the exact grid names on all four targeted forms before implementation.

**Config loading:** Ranges loaded once at server startup into memory. No per-request CouchDB lookups for validation.
