---
title: "PRD: MMRIA V4.1"
status: final
created: 2026-06-12
updated: 2026-06-16 (OI-4 resolved — validation mode design)
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

Vitals fields appear in repeating-record grids on case forms. Validation and display-time exclusion apply to every vitals grid that includes the graph/table toggle control. Each grid allows reviewers to enter as many vitals records as needed. The same validation rules and display behavior apply consistently across all in-scope grids.

**FR-2.1 — On-blur field-level hard block**
When a reviewer **actively edits** a vitals field and leaves it (blur event, tab-out, or paste) with a value outside the configured valid range, the value is cleared from the field and a validation modal is displayed (FR-2.2). This is the `severity: hard` path. Save & Continue, Save & Finish, and autosave have no special validation behavior — they save the current field state as-is.

Vitals values already persisted in CouchDB that fall outside the valid range are **not cleared retroactively**. They are surfaced as `severity: warning` in the Validation Errors Panel (FR-6) at case load time and do not block save or auto-save. The stored database value is not modified by this process.

**FR-2.2 — Modal on invalid entry**
When a vitals field value is rejected per FR-2.1, a modal dialog is displayed with the message: "The value entered for the [field label] field falls outside of the permitted range. Please enter a valid input between {min}–{max}." On dismiss, focus returns to the cleared field. The modal uses the existing site modal pattern and meets Section 508 accessibility requirements.

**FR-2.3 — Field validation rules document**
Vitals validation rules are stored in a dedicated CouchDB document with `_id: "case-validation-rules-{metadata_version}"`, separate from the general application configuration document. This document is seeded automatically at server startup if it does not exist for the current metadata version. A developer can update rules by editing the document directly — no code deployment required.

The document schema:
- `schema_version` — semver string (required)
- `seeded_version` — sprint version that generated the seed (e.g., `"4.1"`); startup seeder skips re-seeding if version matches
- `rules` — array of typed field rule entries, each carrying: `rule_id`, `field_path` (dot-notation), `severity` (`"hard"` | `"soft"`), `rule_type` (`"range"` | `"range-list"`), `min_value`, `max_value`, `message`, `enabled`, `confidence`, `review_status`, `source`, `rationale`
- `overrides` — array of runtime severity overrides: `rule_id`, `severity`, `reason`, `overridden_by`, `overridden_date`

V4.1 seeds the following six vitals fields with `severity: "hard"` and `review_status: "reviewed"`:

| Field | field_path pattern | Min | Max | Unit |
|---|---|---|---|---|
| Temperature | `*/temperature` | 80 | 115 | °F |
| Heart Rate | `*/heart_rate` | 20 | 250 | bpm |
| Respiration Rate | `*/respiration_rate` | 4 | 80 | breaths/min |
| Systolic BP | `*/systolic*` | 40 | 300 | mmHg |
| Diastolic BP | `*/diastolic*` | 20 | 200 | mmHg |
| Oxygen Saturation | `*/oxygen_saturation` | 0 | 100 | % |

The evaluation engine applies the rule `severity` as written during active input (on-blur). When evaluating persisted case data at load time (historical scan), all rule severities are downgraded one step: `hard → warning`. This ensures hard rules block new entry but only warn on historical data.

Overrides in the `overrides` array shadow the base rule's `severity` at runtime. An operator can change severity or disable a rule entirely by editing the document — no code deployment required. No `review-pending` rule may be promoted to `severity: "hard"` before its `review_status` reaches `"reviewed"`.

**FR-2.4 — Display-time exclusion — print and PDF views**
Out-of-range vitals values are displayed as empty string in print view and PDF view. For each vitals record row where one or more values are excluded, the Comment(s) field for that row is appended with an out-of-range notice in the format: `** Out of range. [Field name] removed.` — one field-name clause per excluded value in a single appended string. If multiple values are excluded from the same row, the clauses are concatenated: e.g., `** Out of range. Temperature removed. Heart rate removed.` If the Comment(s) field already contains text, the out-of-range notice is appended to the existing content. The stored database value is not affected.

**FR-2.5 — Display-time exclusion — graph and table views**
Out-of-range vitals values are excluded from graph and table views within the case form. They are not plotted and not shown in the table. The case form input field continues to display the stored value.

**FR-2.6 — Print/View/PDF validation gate**
When a user initiates a View, View PDF, or Save PDF action, the system applies the following logic before proceeding:

**Closed-state bypass.** If the case status is one of the following, the action is performed directly — no validation runs:
- *Review complete and decision entered*
- *Out of Scope and death certificate entered*
- *False Positive and death certificate entered*

**Open-state validation.** Otherwise, all vitals values in the case are evaluated against the `field-validation-rules` document.

- If no violations exist: perform the action directly.
- If one or more **hard** (`severity: "hard"`) violations exist: the action is **blocked entirely**. The modal is displayed with only a Close button — no proceed path exists. The user must resolve errors before continuing.
- If no hard violations exist but one or more **soft** (`severity: "soft"` / `"warning"`) violations exist: the action requires explicit acknowledgment. The modal is displayed listing the warning count and messages. The user may proceed by confirming. Confirmation is UI-only — no record is persisted to the case document.

> Historical out-of-range vitals data (values persisted before rule enforcement) are evaluated as `severity: warning`. These trigger the soft-acknowledgment path, not the hard-block path.

**Validation modal.**
- Style: matches the existing site modal pattern (purple header, white body, two-button footer).
- Hard-block message: *"This case contains vital sign records with values outside the permitted range. These values must be corrected before printing or viewing."* Close button only.
- Soft-acknowledgment message: *"This case contains vital sign records with values outside the permitted range. These values are excluded from graphs, tables, print and pdf views."* Two buttons: **Close** (action not performed) and **[Contextual action]** (*View*, *View PDF*, or *Save PDF*).
- Modal meets Section 508 requirements (see NFR-2).

> *The prior FR-2.6 behavior — modal on edit-mode entry, modal on form navigation, and red text indicator per vitals record — is removed. Story 2.5 covers the implementation that was built under the prior requirement; the prior behavior is removed as part of the story implementing this requirement. This requirement fully supersedes the prior FR-2.6.*

---

### FR-3 — Configuration-Driven System Values

**FR-3.1 — OMB expiration date**
The OMB expiration date is read from the CouchDB configuration document at render time. It displays correctly in the OMB block on the Home page and on the Committee Decisions form. When the value is updated in the configuration document and the production script is run, all render surfaces reflect the new date without a code deployment.

**FR-3.2 — MMRIA version number**
The MMRIA version number is read from the CouchDB configuration document at render time. It displays correctly in the application footer. When the value is updated in the configuration document and the production script is run, the footer reflects the new version without a code deployment.

**FR-3.3 — Developer-managed update mechanism**
Both values are updated by a developer editing the CouchDB configuration document and running the existing production update script. No admin UI is required or in scope.

**FR-3.4 — OMB block right-alignment on Home page**
The OMB block on the Home page is right-aligned to the page. Text content within the block remains left-aligned. The Committee Decisions form is already correctly aligned and is not modified. No content, behavior, or configuration changes are required — this is a layout-only change.

---

### FR-4 — Core Elements Print Dropdown Removal

**FR-4.1 — Remove from affected dropdowns**
The "Core Elements Only" option (section key: `core-summary`) is removed from all three affected MMRIA print dropdowns. The option does not appear for any user role.

**FR-4.2 — PMSS scope exclusion**
PMSS-related print dropdowns are not in scope and must not be modified.

---

### FR-5 — Case Narrative Instruction Text Replacement

**FR-5.1 — Replace instruction text**
On the Case Narrative form, remove both existing instruction lines:
- `"Use the pre-fill text below, and copy and paste from Reviewer's Notes below to create a comprehensive case narrative. Whatever you type here is what will be printed in the Print Version."`
- `"CTRL+B to bold, CTRL+I to italicize, CTRL+U to underline"`

Replace with the following text, preserving line breaks as shown:

```
-You may use this template as a guide, deleting any portions that are not applicable.
-Alternatively, you may copy the reviewer’s notes sections below into the final case narrative field or into an external document. You may also use your own template.
-Ensure any narrative you want to copy and paste into the final case narrative field is in plain text without formatting (ctrl+shift+v).

Remember to:
-Focus on the most relative information to the cause of death (see Cause of Death Modules)
-Humanize the story using a story-telling approach
-Use inclusive and non-stigmatizing language
-Spell out acronyms or explain in plain text clinical terminology
-Incorporate interview(s) and CVS throughout (as applicable)
```

No behavior, configuration, or data changes are required. This is a static text replacement only.

---

### FR-6 — Validation Errors Panel

**FR-6.1 — Button visibility**
While in edit mode, a "Validation Errors" link button is displayed above the red line in the case header area. The button is visible when at least one validation error or warning exists across the case; it is hidden when there are no violations and when not in edit mode. The button label displays both counts independently when both are non-zero (e.g., `"2 Errors · 1 Warning"`); when only one category is non-zero, only that count is shown.

**FR-6.2 — Validation Errors modal**
Clicking the button opens a modal (existing site modal pattern) containing a Close button and a scrollable panel with two visually distinct sections:

**Errors section** (hard violations):
- Section header: **"Errors"** with count badge on a red background.
- Each item carries a filled red circle icon.
- Rendered first, always visible when hard violations exist.

**Warnings section** (soft / historical violations):
- Section header: **"Warnings"** with count badge on an amber background.
- Each item carries a filled amber triangle-exclamation icon.
- Rendered below Errors. Omitted entirely when warning count is zero.

Each row in either section has three columns: **Form Name**, **Field Label** (hyperlink), **Error / Warning message**.

For `severity: warning` vitals violations (historical data), the message column SHALL display: `"Value [stored value] is outside the expected range [min]–[max]."` The stored value is read from the case document at panel render time.

For `severity: hard` vitals violations (active-input, field not yet cleared), the message column SHALL display the standard out-of-range message.

The panel header displays both counts independently: e.g., `"2 Errors · 3 Warnings"`. The panel is not rendered when both counts are zero.

The modal is scoped to vitals validation violations in V4.1 and is explicitly designed to accommodate additional validation types in future iterations.

**Load-time warning detection:** On case document load, the system evaluates all stored vitals field values against the seeded `field-validation-rules` (in `historical` evaluation context, where all hard severities are downgraded to warning). Any violations found are loaded into the panel state and persist until the case is reloaded or the value is corrected.

**FR-6.3 — Field navigation**
Clicking a Field Label link closes the modal, changes to the form containing the error if not already on it, and scrolls to the specific field. For vitals within ER Visits, the navigation must identify the correct visit record, open/expand it, and scroll to the specific vital sign row within that visit. Multiple errors within the same visit each appear as separate rows in the modal list.

**NFR-1 — Browser support**
All changes must function correctly in Microsoft Edge and Google Chrome. No other browsers are in scope.

**NFR-2 — Section 508 compliance**
The vitals validation modals (FR-2.2, FR-2.6, FR-6.2) must meet Section 508 accessibility requirements.

**NFR-3 — Validation performance**
The `field-validation-rules` document is seeded and loaded once at server startup and held in memory. Field-level blur validation (active-input path) is synchronous against the in-memory rules — no per-event network requests. Load-time historical evaluation runs once per case document load, client-side, against the in-memory rules snapshot delivered with the page.

**NFR-4 — Rules governance process**
Before V4.2 production deployment, the project SHALL establish and document a rules governance process covering: (1) the workflow by which a new or modified validation rule is proposed, reviewed, and approved prior to inclusion in the `field-validation-rules` document; (2) the role(s) authorized to modify `severity` values or add `overrides` entries in production environments; (3) the change-management communication path to notify jurisdiction administrators when rules change. This process documentation is a prerequisite acceptance criterion for the V4.2 scale-out epic. A named clinical reviewer must be designated and available before any `review-pending` seeded rules are promoted to `severity: "hard"`.

---

## Constraints & Dependencies

| Constraint | Detail |
|---|---|
| Auto-save must not be disrupted | Hard validation rejects invalid values at the field level (on-blur) before they enter form state. Save triggers at any point must not encounter invalid vitals values. Auto-save is never blocked. |
| Admin UI scope | Configuration updates for OMB date and MMRIA version are developer-managed via CouchDB document and production script only. The `field-validation-rules` document is also developer-managed for V4.1. The rule management admin UI delivered by the POC (`/case_validation_metadata`) is operator tooling available in V4.1 but is not a V4.1 end-user deliverable and has no formal acceptance criteria in this sprint — acceptance criteria will be added in V4.2. |
| Existing out-of-range vitals data | Historical out-of-range vitals values are not cleared or corrected. They are surfaced as `severity: warning` in the Validation Errors Panel (FR-6) and trigger the soft-acknowledgment print gate path. The stored database value is not modified. |
| PMSS dropdowns | PMSS-related print dropdowns must not be modified. |

---

## Open Items

| # | Item | Blocker? | Owner | Condition to Resolve |
|---|---|---|---|---|
| OI-1 | Vitals valid ranges not yet defined | **Resolved** | Program team | Ranges confirmed — see FR-2.3. |
| OI-2 | Three specific print dropdown render locations for `core-summary` need identification | No | Developer | Identify all client-side render sites before FR-4 implementation |
| OI-3 | FR-1.1 prior fix status | **Resolved** | Developer | Implementation complete; going to formal verification. |
| OI-4 | Validation mode design discussion | **Resolved** | Architect + Analyst | Validation mode is implemented as a `severity` property (`hard` \| `soft`) on each rule in a dedicated version-scoped `case-validation-rules-{metadata_version}` CouchDB document; no user-facing mode toggle exists; active-input hard validation clears fields on blur; historical data is downgraded to `warning` at load time; soft-warning acknowledgment is UI-only with no case-document persistence. FR-2.3 updated. |
