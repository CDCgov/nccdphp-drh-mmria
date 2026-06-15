---
title: "PRD: MMRIA V4.1"
status: final
created: 2026-06-12
updated: 2026-06-12
---

# PRD: MMRIA V4.1

## Vision & Goals

MMRIA V4.1 restores predictable behavior to the case narrative editor, prevents unreliable vitals data from entering the system, and eliminates two categories of operational risk — hardcoded values that require code deployments to change, and an unauthorized print option that exposes system output users should not access.

**Success looks like:**
- Case reviewers stop reporting editor and formatting complaints
- CDC analysts stop receiving out-of-range vitals data from state submissions
- The next OMB expiration date change is handled by a developer running a script in minutes, not a release cycle

---

## Functional Requirements

### FR-1 — Case Narrative Editor Fidelity

The case narrative editor is a rich text field with a metadata-driven default. Reviewers write in it directly, paste from external sources (Word, other applications), and edit pasted content. The output must be consistent between the editor view, print view, and PDF output.

**FR-1.1 — Line break persistence**
When a reviewer saves and reopens a case narrative, all explicit line breaks entered or present in the content are preserved in the editor view. Display is consistent between editor, print view, and PDF output.
> *Note: A prior fix has been applied to the save path. This requirement remains active — the fix is a candidate solution, not a confirmed resolution. Verification through testing is required.*

**FR-1.2 — Formatting persistence (underline, horizontal rule, font size)**
Underline, horizontal rule, and font size formatting applied in the editor are retained after save and reload. These formatting attributes render consistently across editor view, print view, and PDF output.

**FR-1.3 — Cut/paste cursor integrity**
Cut (Ctrl+X) and paste (Ctrl+V) operations insert content at the current cursor position within the current paragraph. Multiple sequential pastes each land at the cursor. No paste operation inserts content at an unintended position — other lines, between words in a different paragraph, or at a random location in the document.

---

### FR-2 — Vitals Field Validation

Vitals fields appear in repeating-record grids on four case forms: "ER Visits & Hospitalization," "Prenatal Care Record," "Other Medical Office Visits," and "Transport Vital Signs." Each form allows reviewers to enter as many vitals records as needed. The same validation rules and behavior apply to all four forms.

**FR-2.1 — On-blur field-level hard block**
When a reviewer leaves a vitals field (blur event) with a value outside the configured valid range, the value is cleared from the field. Validation fires at field level, not at form save. Auto-save at any trigger point (timed interval, Save & Continue, Save & Finish, form navigation, browser close) is not affected.

**FR-2.2 — Section 508-compliant modal on invalid entry**
When a vitals field value is rejected per FR-2.1, a modal dialog is displayed showing the valid range for that field. The modal meets Section 508 accessibility requirements.

**FR-2.3 — Config-driven valid ranges**
Valid ranges for all vitals fields are stored in a single CouchDB configuration document. Ranges are loaded once at server startup and held in memory. A developer can update ranges by editing the configuration document and running the production update script — no code deployment required.

**FR-2.4 — Consistent behavior across all four forms**
All four targeted forms apply identical range constraints from the shared configuration and identical validation behavior. Validation logic is not duplicated per form.

---

### FR-3 — Configuration-Driven System Values

**FR-3.1 — OMB expiration date**
The OMB expiration date is read from the CouchDB configuration document at render time. It displays correctly in the OMB block on the Home page and on the Committee Decisions form. When the value is updated in the configuration document and the production script is run, all render surfaces reflect the new date without a code deployment.

**FR-3.2 — MMRIA version number**
The MMRIA version number is read from the CouchDB configuration document at render time. It displays correctly in the application footer. When the value is updated in the configuration document and the production script is run, the footer reflects the new version without a code deployment.

**FR-3.3 — Developer-managed update mechanism**
Both values are updated by a developer editing the CouchDB configuration document and running the existing production update script. No admin UI is required or in scope.

---

### FR-4 — Core Elements Print Dropdown Removal

**FR-4.1 — Remove from affected dropdowns**
The "Core Elements Only" option (section key: `core-summary`) is removed from all three affected MMRIA print dropdowns. The option does not appear for any user role.

**FR-4.2 — PMSS scope exclusion**
PMSS-related print dropdowns are not in scope and must not be modified.

---

## Non-Functional Requirements

**NFR-1 — Browser support**
All changes must function correctly in Microsoft Edge and Google Chrome. No other browsers are in scope.

**NFR-2 — Section 508 compliance**
The vitals validation modal (FR-2.2) must meet Section 508 accessibility requirements.

**NFR-3 — Validation performance**
Vitals range configuration is loaded once at server startup and held in memory. Field-level blur validation is synchronous against the in-memory config. No per-event network requests are introduced by this feature.

---

## Constraints & Dependencies

| Constraint | Detail |
|---|---|
| Auto-save must not be disrupted | Validation rejects invalid values at the field level before they enter form state. Save triggers at any point must not encounter invalid vitals values. |
| No admin UI | Configuration updates for OMB date, version, and vitals ranges are developer-managed via CouchDB document and production script only. |
| Existing out-of-range vitals data | Retroactive identification or correction of existing out-of-range data is explicitly out of scope. |
| PMSS dropdowns | PMSS-related print dropdowns must not be modified. |

---

## Open Items

| # | Item | Blocker? | Owner | Condition to Resolve |
|---|---|---|---|---|
| OI-1 | Vitals valid ranges not yet defined | **Yes — blocks FR-2 implementation** | Program team | Ranges must be defined and approved before FR-2 development begins |
| OI-2 | Three specific print dropdown render locations for `core-summary` need identification | No | Developer | Identify all client-side render sites before FR-4 implementation |
| OI-3 | FR-1.1 prior fix status | No | Developer | Determine whether existing save-path fix fully resolves the line break defect via test coverage |
