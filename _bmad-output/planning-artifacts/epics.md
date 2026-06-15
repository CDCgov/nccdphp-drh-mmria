---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-mmria-2026-06-12/prd.md
  - _bmad-output/planning-artifacts/architecture-mmria-v4.1.md
---

# mmria - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for mmria V4.1, decomposing the requirements from the PRD and Architecture into implementable stories.

## Requirements Inventory

### Functional Requirements

FR-1.1: When a reviewer saves and reopens a case narrative, all explicit line breaks are preserved in the editor view. Display is consistent between editor, print view, and PDF output.
FR-1.2: Underline, horizontal rule, and font size formatting applied in the editor are retained after save and reload, rendering consistently across editor view, print view, and PDF output.
FR-1.3: Cut and paste operations insert content at the current cursor position within the current paragraph. Multiple sequential pastes each land at the cursor. No paste inserts at an unintended position.
FR-2.1: When a reviewer leaves a vitals field (blur, tab-out, or paste) with a value outside the configured valid range, the field value is cleared.
FR-2.2: When a vitals field value is rejected per FR-2.1, a modal is displayed: "The value entered for the [field label] field falls outside of the permitted range. Please enter a valid input between {min}–{max}." Focus returns to the cleared field on dismiss.
FR-2.3: Valid ranges for all vitals fields are stored in a single CouchDB configuration document, loaded once at server startup. A developer can update ranges by editing the config document and running the production update script — no code deployment required.
FR-2.4: Out-of-range vitals values are displayed as empty string in print view and PDF view. Stored value is not affected.
FR-2.5: Out-of-range vitals values are excluded from graph and table views within the case form. They are not plotted and not shown in the table. The case form input field continues to display the stored value.
FR-2.6: On entering edit mode and on form selector navigation while in edit mode, the system re-validates all vitals values. If any out-of-range values are found: (1) a modal is shown, (2) a red text indicator is applied at the top of each affected vitals record.
FR-2.7: The PDF view currently displays "/ /" for empty or invalid vitals date values. Replace with empty string. Scoped to vitals date fields in the PDF rendering path only.
FR-3.1: The OMB expiration date is read from the CouchDB configuration document at render time. Displays correctly in the OMB block on the Home page and on the Committee Decisions form.
FR-3.2: The MMRIA version number is read from the CouchDB configuration document at render time. Displays correctly in the application footer.
FR-3.3: Both FR-3.1 and FR-3.2 values are updated by a developer editing the CouchDB config document and running the existing production update script. No admin UI required.
FR-4.1: The "Core Elements Only" option (section key: `core-summary`) is removed from all three affected MMRIA print dropdowns. The option does not appear for any user role.
FR-4.2: Dead code related to `core-summary` in `pdf-version/index.js` (TitleMap entry, getReportTabName case, formatContent case, and `core_summary()` function) is removed.
FR-5.1: On the Case Narrative form, the two existing instruction lines are removed and replaced with the approved replacement text (preserving line breaks). No behavior, configuration, or data changes.

### NonFunctional Requirements

NFR-1: All changes must function correctly in Microsoft Edge and Google Chrome. No other browsers are in scope.
NFR-2: The vitals validation modals (FR-2.2, FR-2.6) must meet Section 508 accessibility requirements — role, aria-modal, aria-labelledby, focus management, keyboard dismissal, and focus return.
NFR-3: Vitals range configuration is loaded once at server startup and held in memory. Field-level blur validation is synchronous against the in-memory config. No per-event network requests are introduced.

### Additional Requirements

- FR-1: All fixes are JavaScript-only in `wwwroot/scripts/case/index.js`. No server-side changes for FR-1.
- FR-1 (overriding constraint): The generated HTML structure must be identical to what the editor produces today. No new tags, no changed nesting, no reformatting. Stop stripping only — do not replace tags or modernize structure.
- FR-1.3: Use the Range API (`window.getSelection().getRangeAt(0)`, `range.insertNode`) directly. Do not use `document.execCommand('insertHTML')`. Strip XSS vectors only (onclick, onerror, javascript: hrefs) — preserve all structural tags.
- FR-2 scope: Apply validation and display-time exclusion to every vitals grid that renders the graph/table toggle control. Identify all such grids at implementation time — do not hardcode a form list.
- FR-2 server-side: `NestedStringDictionaryConverter` (custom `JsonConverter`) required to handle the nested `vital_sign_range` JSON object inside `string_keys.shared`. Applied via `[JsonConverter]` attribute on `OverridableConfiguration.string_keys`. `VitalSignRangeHelper` static class in `mmria-server/util/` deserializes the raw JSON string into a typed model with hardcoded defaults matching confirmed ranges.
- FR-2 config key: `vital_sign_range` (nested under `string_keys.shared`). OI-4 (exact HTML `name` attributes for vitals inputs) remains open — developer confirms at implementation time.
- FR-3: Inline `GetString ?? default` pattern in controller only. No helper class, no new service. Hardcoded defaults inline: `omb_expiration_date` = `"05/31/2026"`, `mmria_version` = `"MMRIA V 4.1"`.
- FR-3.3: Developer also patches `omb_expiration_label.prompt` in `metadata.json` via the production update script when the OMB date changes. No client-side render-time substitution.
- FR-4: Surgical deletions only. No new code. Before removing `de-identified/index.js` redirect guard (~line 933), grep to confirm it is `core-summary`-specific.
- FR-4: Confirm `core_summary()` function has no remaining references before deleting the declaration.
- FR-5: Developer locates the render source by searching for the first distinctive phrase of the existing text. If the text originates from `metadata.json` or a CouchDB document, update via the database-scripts update path. Do not change surrounding markup or field structure.
- All open items (OI-3, OI-4, OI-5, OI-dev-B, OI-dev-C): do not block story creation but must be resolved before the affected implementation begins.

### UX Design Requirements

N/A — no UX design document exists for this release. All UI patterns follow existing site conventions.

### FR Coverage Map

FR-1.1: Epic 1 — Save-path line break stripping fix
FR-1.2: Epic 1 — Save-path formatting stripping fix (underline, HR, font size)
FR-1.3: Epic 1 — Paste handler cursor integrity (Range API rewrite)
FR-2.1: Epic 2 — On-blur field-level hard block (clear + reject)
FR-2.2: Epic 2 — Field-level invalid entry modal with range text
FR-2.3: Epic 2 — Config-driven valid ranges in CouchDB + server-side loading
FR-2.4: Epic 2 — Print/PDF display-time exclusion → empty string
FR-2.5: Epic 2 — Graph/table display-time exclusion
FR-2.6: Epic 2 — Historical data detection on edit-mode entry + form navigation
FR-2.7: Epic 2 — PDF vitals date "/ /" fix → empty string
FR-3.1: Epic 3 — OMB expiration date config-driven (controller + Razor + DB doc)
FR-3.2: Epic 3 — MMRIA version config-driven (controller + Razor + DB doc)
FR-3.3: Epic 3 — Developer update workflow (no admin UI, script-driven)
FR-4.1: Epic 3 — Remove core-summary option from three print dropdowns
FR-4.2: Epic 3 — Remove core-summary dead code from pdf-version/index.js
FR-5.1: Epic 1 — Case Narrative instruction text replacement

## Epic List

### Epic 1: Case Narrative Editor Fidelity
Reviewers can write, format, and paste content in the case narrative editor with confidence that what they enter is what gets saved and printed. Updated instructions guide users toward better narrative practices.
**FRs covered:** FR-1.1, FR-1.2, FR-1.3, FR-5.1

### Epic 2: Vitals Field Validation
Reviewers entering vitals data are immediately alerted when values fall outside clinical ranges, preventing unreliable data from entering graphs, tables, print, and PDF views. Existing cases with out-of-range values are flagged at review time.
**FRs covered:** FR-2.1, FR-2.2, FR-2.3, FR-2.4, FR-2.5, FR-2.6, FR-2.7

### Epic 3: System Configuration & Print Cleanup
Developers can update the OMB expiration date and MMRIA version number without a code deployment. The "Core Elements Only" unauthorized print option is removed from all affected dropdowns and dead code is cleaned up.
**FRs covered:** FR-3.1, FR-3.2, FR-3.3, FR-4.1, FR-4.2

---

## Epic 1: Case Narrative Editor Fidelity

Reviewers can write, format, and paste content in the case narrative editor with confidence that what they enter is what gets saved and printed. Updated instructions guide users toward better narrative practices.

### Story 1.1: Fix Save-Path HTML Stripping

As a case reviewer,
I want formatting I apply in the case narrative editor to be preserved when I save and reopen a case,
So that line breaks, underline, horizontal rules, and font sizes render consistently in the editor, print view, and PDF.

**Acceptance Criteria:**

**Given** a case narrative containing explicit line breaks (`<br>`), underline (`<u>`), horizontal rule (`<hr>`), and font size (`<font size="...">`) markup
**When** the reviewer saves the case and reopens it
**Then** all of the above formatting elements are present in the editor view and the stored HTML is byte-for-byte identical to what the editor produced

**Given** the save path previously called a stripping function (candidate: `textarea_control_strip_html_attributes()` near line 4356 in `case/index.js`)
**When** the developer audits the narrative save path in `mmria_get_narrative_save_snapshot()`
**Then** the stripping function is no longer called on the narrative field value (or is scoped to exclude the narrative field)

**Given** any sanitization still required on the save path (XSS vector removal)
**When** the narrative is sanitized before save
**Then** only executable attributes (`onclick`, `onerror`, `javascript:` hrefs) are removed — structural tags (`<br>`, `<u>`, `<hr>`, `<font>`) are preserved unchanged

**Given** existing case data in CouchDB that was saved in the stripped form (pre-fix)
**When** that case is opened
**Then** the editor renders the existing stored content as-is (no reprocessing or migration of old data)

### Story 1.2: Fix Paste Handler Cursor Integrity

As a case reviewer,
I want pasted content to land at my cursor position in the case narrative editor,
So that multiple sequential pastes from Word or other sources each land exactly where I intend.

**Acceptance Criteria:**

**Given** the cursor is positioned within a paragraph in the case narrative editor
**When** the reviewer pastes content (Ctrl+V)
**Then** the pasted content is inserted at the cursor position, not at a random location in the document

**Given** multiple sequential paste operations targeting different cursor positions
**When** each paste is performed
**Then** each piece of content lands at the cursor position active at the time of that paste

**Given** the paste handler in `page_render_create_onpaste_event()` in `case/index.js`
**When** the developer rewrites it
**Then** it uses the Range API (`window.getSelection().getRangeAt(0)`, `range.deleteContents()`, `range.insertNode()`) to capture selection state synchronously at the top of the handler — `document.execCommand('insertHTML')` is not used

**Given** content pasted from an external source (Word, another application)
**When** the paste is processed
**Then** only executable XSS attributes (`onclick`, `onerror`, `javascript:` hrefs) are stripped — all structural HTML tags are preserved

**Given** the fix is validated in Edge and Chrome (NFR-1)
**When** tested in both browsers with multiple sequential pastes
**Then** behavior is consistent and correct in both

### Story 1.3: Update Case Narrative Instruction Text

As a case reviewer,
I want the Case Narrative form to display updated guidance text,
So that I understand how to write an effective, compliant case narrative using the available tools.

**Acceptance Criteria:**

**Given** the Case Narrative form currently shows two instruction lines:
  - "Use the pre-fill text below, and copy and paste from Reviewer's Notes below to create a comprehensive case narrative. Whatever you type here is what will be printed in the Print Version."
  - "CTRL+B to bold, CTRL+I to italicize, CTRL+U to underline"
**When** the developer locates the render source (Razor view, metadata.json, or CouchDB document) by searching for the first distinctive phrase
**Then** both lines are removed and replaced with the approved replacement text (preserving line breaks as specified in FR-5.1)

**Given** the surrounding markup and field structure for the instruction text
**When** the replacement is made
**Then** no surrounding markup or field structure is changed — text content only

**Given** the text originates from a CouchDB document or `metadata.json`
**When** the update is applied
**Then** it is applied via the database-scripts update path, not a Razor/JS edit

---

## Epic 2: Vitals Field Validation

Reviewers entering vitals data are immediately alerted when values fall outside clinical ranges, preventing unreliable data from entering graphs, tables, print, and PDF views. Existing cases with out-of-range values are flagged at review time.

### Story 2.1: Add Vitals Range Config — CouchDB Document and Server-Side Loading

As a developer,
I want the valid ranges for all vitals fields stored in CouchDB and loaded into memory at server startup,
So that vitals validation and display-time exclusion can read ranges synchronously without network requests, and a developer can update ranges by script without a code deployment.

**Acceptance Criteria:**

**Given** the CouchDB config document in `database-scripts/`
**When** the developer updates it
**Then** it contains a `vital_sign_range` nested object under `string_keys.shared` with the confirmed ranges: Temperature 0–110, Heart Rate 0–400, Respiration 0–60, Systolic BP 0–300, Diastolic BP 0–300, Oxygen Saturation 0–100 — each entry carrying `min`, `max`, and `label` keys

**Given** `OverridableConfiguration.string_keys` is typed as `Dictionary<string, Dictionary<string, string>>`
**When** the config document is deserialized at server startup
**Then** a `NestedStringDictionaryConverter` (custom `JsonConverter`) is applied via `[JsonConverter]` attribute on `string_keys`, storing the nested `vital_sign_range` object as its raw JSON string

**Given** the raw JSON string for `vital_sign_range` is held in `OverridableConfiguration`
**When** `VitalSignRangeHelper.GetVitalSignRangeConfig(configuration, host_prefix)` is called from `CaseController`
**Then** it deserializes the raw JSON into a typed `VitalSignRangeConfig` model and returns it; if the key is absent or unparseable, it returns the hardcoded defaults matching the confirmed ranges

**Given** `CaseController.Index()` calls `VitalSignRangeHelper.GetVitalSignRangeConfig()`
**When** the Case page is served
**Then** the serialized config is set as `TempData["vital_sign_range_config"]` and emitted into the `HeadScripts` block as `window.mmria_vital_sign_range = @Html.Raw(TempData["vital_sign_range_config"]);` — following the same pattern as `window.case_edit_inactivity_config`

**Given** the config key is absent or the config document has not been updated yet
**When** the page loads
**Then** `window.mmria_vital_sign_range` is `null` and all client-side validation silently skips

### Story 2.2: On-Blur Vitals Validation and Invalid Entry Modal

As a case reviewer,
I want to be immediately alerted when I enter a vitals value outside the permitted range,
So that out-of-range values never silently enter the form and I can correct them before saving.

**Acceptance Criteria:**

**Given** a vitals input field in any grid that renders the graph/table toggle
**When** the reviewer leaves the field (blur, tab-out, or paste) with a value outside `[min, max]` for that field
**Then** the field value is cleared to empty string

**Given** a vitals field value is cleared per the above
**When** the field is cleared
**Then** a modal is displayed with the message: "The value entered for the [field label] field falls outside of the permitted range. Please enter a valid input between {min}–{max}." using the existing site modal pattern (purple header, OK button)

**Given** the modal is dismissed
**When** the reviewer clicks OK or presses Escape/Enter
**Then** focus returns to the cleared field (NFR-2)

**Given** `window.mmria_vital_sign_range` is `null`
**When** blur fires on a vitals field
**Then** no validation runs and no modal appears — silent skip

**Given** the field value is empty or not a number
**When** blur fires
**Then** no validation runs — only non-empty parseable numeric values are validated

**Given** Save & Continue, Save & Finish, or autosave fires
**When** a vitals field is in any state
**Then** no validation runs at save time — whatever is in the field is saved as-is

**Given** the validation function `mmria_vitals_validate_field(inputElement)` is implemented in `chart.js`
**When** it is attached
**Then** it attaches to blur, keydown (Tab key), and paste events on every vitals input in scope — identified by the presence of the graph/table toggle on the same grid, not by a hardcoded form list (OI-4: developer confirms exact `name` attributes at implementation time)

### Story 2.3: Display-Time Exclusion — Print, PDF, and Vitals Date Fix

As a CDC analyst reviewing submitted case data,
I want out-of-range vitals values to appear as blank in printed reports and PDFs,
So that printed output does not surface unreliable data.

**Acceptance Criteria:**

**Given** a vitals record with a value outside the configured range for that field
**When** the case is rendered in print view
**Then** that vitals field renders as empty string — the stored database value is not affected

**Given** the same out-of-range value
**When** the case is rendered as a PDF
**Then** that vitals field renders as empty string in the PDF output — stored value unchanged

**Given** the PDF rendering path for vitals date fields currently outputs `/ /` for empty or invalid dates
**When** a vitals date field is empty or invalid
**Then** the PDF renders an empty string instead of `/ /` — this fix is scoped to vitals date fields in the PDF rendering path only

**Given** all exclusion above
**When** the reviewer views the same case in the case form editor
**Then** the case form input field continues to display the stored value unchanged

### Story 2.4: Display-Time Exclusion — Graph and Table Views

As a case reviewer,
I want out-of-range vitals values excluded from graphs and tables in the case form,
So that visual trends and tabular summaries only reflect clinically plausible data.

**Acceptance Criteria:**

**Given** a vitals record with a value outside the configured range
**When** the graph view renders for that vitals grid
**Then** the out-of-range data point is not plotted — no point, no line segment to/from it

**Given** the same out-of-range value
**When** the table view renders for that vitals grid
**Then** the cell for that field renders as empty — not the raw value

**Given** all exclusion above
**When** the reviewer views the case form input field for that same record
**Then** the input continues to display the stored value — exclusion is display-time only, not stored

**Given** `window.mmria_vital_sign_range` is `null`
**When** graph and table views render
**Then** all values render normally — no exclusion applied

### Story 2.5: Historical Data Detection and Record Indicators

As a case reviewer,
I want to be notified when a case I'm editing contains vitals values that fall outside permitted ranges,
So that I understand why those values are absent from graphs, tables, and printed output.

**Acceptance Criteria:**

**Given** a reviewer enters edit mode for a case
**When** edit mode is entered
**Then** all vitals values across all vitals records in the case are re-validated against `window.mmria_vital_sign_range`

**Given** the reviewer is in edit mode and selects a different form from the "Select case form" dropdown
**When** the form navigation occurs
**Then** re-validation of all vitals values runs again (OI-dev-B: developer confirms the technical hook for both triggers at implementation time)

**Given** re-validation finds one or more out-of-range values
**When** the check completes
**Then** a modal is displayed with the message: "This case contains vital sign records with values outside the permitted range. These values are excluded from graphs, tables, print and pdf views." using the existing site modal pattern; on dismiss, no focus change is required

**Given** re-validation finds one or more out-of-range values
**When** the check completes
**Then** a red text indicator is applied at the top of each vitals record that contains at least one out-of-range value (OI-dev-C: developer confirms the DOM target in `chart.js` at implementation time)

**Given** the red text indicator is applied
**When** the case form re-renders (e.g., on form navigation)
**Then** the indicator is re-evaluated on each render — it is not a one-time write

**Given** `window.mmria_vital_sign_range` is `null`
**When** edit mode is entered or form navigation occurs
**Then** no re-validation runs, no modal appears, no indicators are applied

---

## Epic 3: System Configuration & Print Cleanup

Developers can update the OMB expiration date and MMRIA version number without a code deployment. The "Core Elements Only" unauthorized print option is removed from all affected dropdowns and dead code is cleaned up.

### Story 3.1: Config-Driven OMB Expiration Date

As a developer,
I want the OMB expiration date read from the CouchDB configuration document at render time,
So that the next date change can be applied by running the update script — no code deployment required.

**Acceptance Criteria:**

**Given** the CouchDB config document in `database-scripts/`
**When** the developer updates it
**Then** it contains a flat `omb_expiration_date` string key under `string_keys.shared` with default value `"05/31/2026"`

**Given** the relevant controller action(s) serving the Home page and Committee Decisions form (OI-5: developer identifies during implementation)
**When** those actions execute
**Then** they set a TempData or ViewBag entry for the OMB date using the pattern: `configuration.GetString("omb_expiration_date", host_prefix) ?? "05/31/2026"` — no helper class, no new service

**Given** `Views/Shared/_BurdenStatement.cshtml` currently contains the hardcoded string `Exp. Date 05/31/2026`
**When** the partial renders
**Then** it reads the OMB date from the TempData/ViewBag entry set by the controller, following the existing path that already provides data to this partial

**Given** the `omb_expiration_label` field in `metadata.json` carries `"Exp. Date 05/31/2026"` as its `prompt` value
**When** the OMB date is updated
**Then** the developer also patches `omb_expiration_label.prompt` in the metadata document via the production update script — no client-side render-time substitution is required

**Given** the `omb_expiration_date` key is absent from the config document
**When** the controller reads it
**Then** the hardcoded default `"05/31/2026"` is used and the page renders correctly

### Story 3.2: Config-Driven MMRIA Version Number

As a developer,
I want the MMRIA version number read from the CouchDB configuration document at render time,
So that the next version change can be applied by running the update script — no code deployment required.

**Acceptance Criteria:**

**Given** the CouchDB config document in `database-scripts/`
**When** the developer updates it
**Then** it contains a flat `mmria_version` string key under `string_keys.shared` with default value `"MMRIA V 4.1"`

**Given** the relevant controller action(s) serving the application layout (OI-5: developer identifies during implementation)
**When** those actions execute
**Then** they set a TempData or ViewBag entry for the version using the pattern: `configuration.GetString("mmria_version", host_prefix) ?? "MMRIA V 4.1"` — no helper class, no new service

**Given** `Views/Shared/_Footer.cshtml` line 7 currently contains two occurrences of the hardcoded string `MMRIA V4.0.1` (in both the `aria-label` attribute and the visible text)
**When** the footer renders
**Then** both occurrences are replaced with the TempData/ViewBag value — no hardcoded version string remains

**Given** the `mmria_version` key is absent from the config document
**When** the controller reads it
**Then** the hardcoded default `"MMRIA V 4.1"` is used and the footer renders correctly

### Story 3.3: Remove Core Elements Only Print Option

As a case reviewer,
I want the "Core Elements Only" option removed from all affected print dropdowns,
So that users can no longer select an unauthorized print format.

**Acceptance Criteria:**

**Given** the print dropdown in `wwwroot/scripts/editor/page_renderer/form.mmria.js` (~line 2047)
**When** the developer removes the option
**Then** `<option value="core-summary">Core Elements Only</option>` is absent from the rendered dropdown

**Given** the same option in `form.committee_member.mmria.js` (~line 1851)
**When** removed
**Then** the option does not appear in that dropdown either

**Given** the same option in `de-identified/index.js` (~line 1131), plus a redirect guard (~line 933)
**When** removed
**Then** the option is absent and the redirect guard block is also removed if (and only if) it exclusively guards the `core-summary` case — if it guards other cases, only the `core-summary` branch is removed

**Given** `wwwroot/scripts/pdf-version/index.js` contains dead code for `core-summary`
**When** the developer removes it
**Then** all four of the following are removed: the `"core-summary": "Core"` entry in `TitleMap` (~line 38), the `case 'core-summary': return 'Core Elements Only'` branch in `getReportTabName()` (~line 729), the `case 'core-summary':` dispatch in `formatContent()` (~lines 774 and 1148), and the `core_summary()` function body and declaration (confirmed with a grep that no remaining references exist after the above removals)

**Given** PMSS-related print dropdowns already have `core-summary` commented out
**When** the developer makes the above changes
**Then** PMSS files are not modified

**Given** all removals are complete
**When** the developer greps `wwwroot/scripts` for `core-summary`
**Then** zero matches exist outside of PMSS files (where the intentional comment is acceptable)
