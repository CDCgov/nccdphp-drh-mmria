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
FR-7.1: When an admin updates the year of death for a case, an audit entry is written with Update Action `admin change, year of death updated`, recording old and new values.
FR-7.2: When an admin updates the maiden name for a case, an audit entry is written with Update Action `admin change, maiden name updated`, recording old and new values.
FR-7.3: When an admin unlocks a case and clears its status, an audit entry is written with Update Action `admin change, case unlocked, case status cleared`, recording old status as Old Value and empty string as New Value.
FR-7.4: When an admin recovers a deleted case, an audit entry is written with Update Action `admin change, case recovered`. Field prompt, field path, old value, and new value are blank.
FR-7.5: When an admin deletes a case, an audit entry is written with Update Action `case deleted`. Field prompt, field path, old value, and new value are blank.
FR-8.1: A `system-offline-config` document in the CDC instance `metadata` CouchDB database stores: `warn_date`, `warn_message`, `offline_date`, `offline_modal_message`, `offline_page_message`. Saved and fetched via mmria-server controller → mmria-services. Config is global across all tenants.
FR-8.2: At or after `warn_date`, logged-in users see a warning modal (displaying `warn_message`) once per browser session. Triggered on login and by the periodic check. Gated by `sessionStorage` flag.
FR-8.3: At or after `offline_date`, logged-in users see a going-offline modal (displaying `offline_modal_message`) with a single OK button. OK invokes best-effort save if a case is in edit mode, then signs the user out. Shown only once, gated by `localStorage` flag.
FR-8.4: At or after `offline_date`, the login page hides the login form fields and displays `offline_page_message` in white text in place of the login form area.
FR-8.5: While logged in, the client polls mmria-server every 2 minutes for current offline config and evaluates thresholds to trigger FR-8.2 or FR-8.3 as applicable.
FR-8.6: When a user navigates to the login page, the server checks the offline config and renders the page in offline state (FR-8.4) if `now >= offline_date`.
FR-8.7: An installation-admin-only admin page (modeled on `/broadcast-message`, linked from installation admin nav) allows editing and saving all five offline config fields. Saves via mmria-server → mmria-services → CDC instance `metadata` DB.
FR-9.1: On the Data Summary Checks page, when a Form is selected and the user toggles "ALL" in the Field dropdown, only fields belonging to the selected Form are shown and enabled. The no-Form-selected default state (all fields shown) is preserved unchanged.
FR-10.1: On the Manage Users page, clicking "Export User List" when a Role or Username filter is active exports only the currently displayed users. When no filter is active, all users are exported (existing default preserved).

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
- FR-9: Client-side only. Locate both the Form-select event handler and the ALL-toggle event handler in the Data Summary Checks page JS. Both handlers must enforce form-scoped field population when a Form is selected. Developer confirms the form-to-field association mechanism (metadata-driven or hardcoded) at implementation time.
- FR-10: Client-side only. In `export_user_list_click()` in `manage-users/index.js`, replace the join target from `g_ui.user_summary_list` to `g_filtered_user_list`. No server-side changes.

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
FR-7.1: Epic 7 — Audit log entry for Year of Death admin change
FR-7.2: Epic 7 — Audit log entry for Maiden Name admin change
FR-7.3: Epic 7 — Audit log entry for Unlock and Clear Case Status
FR-7.4: Epic 7 — Audit log entry for Recover Deleted Case
FR-7.5: Epic 7 — Audit log entry for Delete Case
FR-8.1: Epic 8 — System offline config document, mmria-services, controller
FR-8.2: Epic 8 — Warning modal (warn date, session-gated)
FR-8.3: Epic 8 — Going offline modal (offline date, localStorage-gated, save + sign out)
FR-8.4: Epic 8 — Login page offline state (hide form, show message)
FR-8.5: Epic 8 — Periodic status check (2-minute client poll)
FR-8.6: Epic 8 — Login page server-side offline check
FR-8.7: Epic 8 — Installation admin page for offline config
FR-9.1: Standalone Bug Fix — Data Summary Checks "ALL" toggle scoped to selected Form
FR-10.1: Standalone Bug Fix — Manage Users Export scoped to active filter
FR-11.1: Epic 10 — Fix BatchSupervisor busy-wait CPU spin (mmria-services)
FR-11.2: Epic 10 — Server-side CVS structured error handling (CVSManager, CVSDAL, CVSModels, cvsAPIController)
FR-11.3: Epic 10 — Client-side CVS retry loop with countdown and try-again button
FR-11.4: Epic 10 — BroadcastChannel CVS status and parent-page button state (mmria.js)

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

### Epic 7: Admin Action Audit Logging

Admin actions that modify case data or case lifecycle state are fully captured in the existing case audit log, giving reviewers and administrators a complete record of who changed what and when.
**FRs covered:** FR-7.1, FR-7.2, FR-7.3, FR-7.4, FR-7.5

### Epic 8: System Going Offline

Installation administrators can schedule a planned system outage. Logged-in users receive advance warning, are guided to save their work and sign out before the system goes offline, and are prevented from logging in once the offline date is reached.
**FRs covered:** FR-8.1, FR-8.2, FR-8.3, FR-8.4, FR-8.5, FR-8.6, FR-8.7

### Epic 10: CVS PDF Export Tool Reliability

The Community Vital Signs PDF export tool is hardened against transient failures at every layer — services, server, and client. Users receive actionable status messages, automatic retries with visible countdown, and a "Try again" path instead of a browser refresh. The parent case page button reflects in-progress state via BroadcastChannel.
**FRs covered:** FR-11.1, FR-11.2, FR-11.3, FR-11.4

- FR-10: Client-side only. In `export_user_list_click()` in `manage-users/index.js`, replace the join target from `g_ui.user_summary_list` to `g_filtered_user_list`. No server-side changes.

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

---

## Epic 7: Admin Action Audit Logging

Admin actions that modify case data or case lifecycle state are fully captured in the existing case audit log, giving reviewers and administrators a complete record of who changed what and when.

### Story 7.1: Audit Logging for Year of Death and Maiden Name Admin Changes

As an installation administrator,
I want my Year of Death and Maiden Name updates to appear in the case audit log,
So that there is a complete record of who changed these fields and what the values were before and after.

**Acceptance Criteria:**

**Given** an admin updates the year of death for a case
**When** the action succeeds
**Then** an audit entry is written with Update Action `admin change, year of death updated`, Old Value set to the previous year value, and New Value set to the updated year value

**Given** an admin updates the maiden name for a case
**When** the action succeeds
**Then** an audit entry is written with Update Action `admin change, maiden name updated`, Old Value set to the previous maiden name value, and New Value set to the updated maiden name value

**Given** the existing audit log pattern (Update Date/Time, Update By, Update Action, MMRIA Field Prompt, MMRIA Field Path, Old Value, New Value)
**When** these entries are written
**Then** they follow the identical pattern used by existing case-edit audit entries — no new fields, no schema changes

**Given** the admin action fails (e.g., save error)
**When** the failure occurs
**Then** no audit entry is written

### Story 7.2: Audit Logging for Case Status and Lifecycle Admin Actions

As an installation administrator,
I want Unlock/Clear, Recover, and Delete actions to appear in the case audit log,
So that there is a verifiable record of every case lifecycle change.

**Acceptance Criteria:**

**Given** an admin unlocks a case and clears its case status
**When** the action succeeds
**Then** an audit entry is written with Update Action `admin change, case unlocked, case status cleared`, Old Value set to the previous case status value, and New Value set to empty string

**Given** an admin recovers a deleted case
**When** the action succeeds
**Then** an audit entry is written with Update Action `admin change, case recovered`; MMRIA Field Prompt, MMRIA Field Path, Old Value, and New Value are all blank

**Given** an admin deletes a case
**When** the delete action succeeds (hard delete)
**Then** an audit entry is written with Update Action `case deleted`; MMRIA Field Prompt, MMRIA Field Path, Old Value, and New Value are all blank

**Given** the existing audit log pattern
**When** these entries are written
**Then** they follow the identical pattern used by existing case-edit audit entries — no new fields, no schema changes

**Given** any of the above admin actions fails
**When** the failure occurs
**Then** no audit entry is written

---

## Epic 8: System Going Offline

Installation administrators can schedule a planned system outage. Logged-in users receive advance warning, are guided to save their work and sign out before the system goes offline, and are prevented from logging in once the offline date is reached.

### Story 8.1: System Offline Config — Document, mmria-services, Controller, and Admin Page

As an installation administrator,
I want a dedicated admin page where I can configure warn and offline dates and messages,
So that I can schedule a planned outage and control the messaging users see at each stage.

**Acceptance Criteria:**

**Given** the CDC instance `metadata` CouchDB database
**When** the developer creates the config document
**Then** a document with `_id: "system-offline-config"` exists carrying these five fields: `warn_date` (ISO 8601 string), `warn_message` (string), `offline_date` (ISO 8601 string), `offline_modal_message` (string), `offline_page_message` (string)

**Given** mmria-services
**When** a get or save request arrives for the offline config
**Then** mmria-services fetches from or writes to the `system-offline-config` document in the CDC instance `metadata` database — following the existing pattern for other metadata documents

**Given** a GET endpoint on the mmria-server controller
**When** called
**Then** it delegates to mmria-services and returns the current config as JSON; if the document does not exist, it returns an object with all five fields as null/empty

**Given** a POST/PUT endpoint on the mmria-server controller
**When** called with a valid config payload
**Then** it delegates to mmria-services to write the document and returns success

**Given** an installation admin navigates to the system offline admin page
**When** the page loads
**Then** a form is displayed with: two datetime picker inputs (`warn_date`, `offline_date`) and three multiline text areas (`warn_message`, `offline_modal_message`, `offline_page_message`), pre-populated with current values from the GET endpoint

**Given** the installation admin submits the form
**When** save succeeds
**Then** a success confirmation is displayed; the saved values are reflected on reload

**Given** a non-installation-admin user attempts to access the page
**When** the request is made
**Then** the page returns 403 / unauthorized — same access control pattern as `/broadcast-message`

**Given** a link to the new admin page is needed
**When** the installation admin nav is rendered
**Then** a link to the system offline admin page appears alongside the broadcast-message link

### Story 8.2: Login Page Offline State and Server-Side Check

As a user attempting to log in during a planned outage,
I want to see a clear offline message instead of a non-functional login form,
So that I understand the system is unavailable and what to do.

**Acceptance Criteria:**

**Given** a user navigates to the login page and `now >= offline_date`
**When** the server renders the login page
**Then** the login form fields (username input, password input, login button) are hidden; `offline_page_message` is displayed in white text in the area where the login form was

**Given** the "please contact your jurisdiction admin…" text currently appears on the login page
**When** the login page renders in offline state
**Then** that text is replaced by `offline_page_message`; no other login page elements are changed

**Given** a user navigates to the login page and `now < offline_date` (or `offline_date` is null)
**When** the server renders the login page
**Then** the login page renders normally — no offline state applied

**Given** the `system-offline-config` document is absent or `offline_date` is null/empty
**When** the login page is requested
**Then** the login page renders normally

### Story 8.3: Warning Modal and Going Offline Modal

As a logged-in user,
I want to be warned before the system goes offline and prompted to save my work before the offline date,
So that I can complete my work and sign out cleanly without losing data.

**Acceptance Criteria:**

**Given** a user has just logged in and `now >= warn_date` and `now < offline_date`
**When** the post-login page loads
**Then** a warning modal is displayed showing `warn_message` with an OK/dismiss button; a `sessionStorage` flag is set so the modal does not reappear during the same browser session

**Given** the warning modal's `sessionStorage` flag is already set for this session
**When** the post-login check runs
**Then** no modal is shown

**Given** `warn_date` is null/empty or `now < warn_date`
**When** the post-login check runs
**Then** no warning modal is shown

**Given** the periodic check (Story 5.4) fires and `now >= warn_date` and `now < offline_date` and the session flag is not set
**When** the check result is evaluated
**Then** the warning modal is displayed and the `sessionStorage` flag is set

**Given** the periodic check fires and `now >= offline_date` and the `localStorage` flag is not set
**When** the check result is evaluated
**Then** the going-offline modal is displayed showing `offline_modal_message` with a single **OK** button (no dismiss/cancel path)

**Given** the user clicks OK on the going-offline modal
**When** OK is clicked
**Then** (1) if a case is currently open in edit mode, save is invoked (best-effort autosave behavior; sign-out proceeds regardless of save outcome); (2) the user is signed out and navigated to the login page (which will render in offline state per Story 5.2)

**Given** the going-offline modal has been shown (OK was clicked and user signed out)
**When** a `localStorage` flag is checked on the next session attempt
**Then** the flag is set — the modal cannot reappear (login is also disabled by this point)

**Given** `offline_date` is null/empty
**When** the periodic check evaluates
**Then** the going-offline modal never fires

### Story 8.4: Periodic Status Check

As a logged-in user,
I want the system to automatically check for planned outage status while I'm working,
So that I receive warning and going-offline notifications without needing to reload the page.

**Acceptance Criteria:**

**Given** a user is logged in
**When** every 2 minutes elapses
**Then** the client sends a request to the mmria-server offline config endpoint and receives the current config (warn_date, offline_date, messages)

**Given** the poll response is received
**When** the client evaluates thresholds
**Then** it applies the same logic as the post-login check: warn modal if `now >= warn_date` and session flag not set; going-offline modal if `now >= offline_date` and localStorage flag not set

**Given** the user signs out or the session ends
**When** the sign-out completes
**Then** the periodic poll is stopped

**Given** the poll request fails (network error, server unavailable)
**When** the error is received
**Then** the failure is silently swallowed — no error is surfaced to the user and polling continues on the next interval

---

## Standalone Bug Fixes

_Single-story bug fixes that do not warrant a full epic._

### Story 9.1: Fix Data Summary Checks Field Filter for ALL Toggle

As a user of the Data Summary Checks page,
I want the Field dropdown to show only the fields from the selected Form when I select "ALL",
So that my data summary reflects the correct form-scoped fields and not all fields across all forms.

**Acceptance Criteria:**

**Given** no Form is selected on the Data Summary Checks page
**When** the page loads or the Form selection is cleared
**Then** the Field dropdown shows all fields across all forms (existing default behavior — preserved unchanged)

**Given** a Form is selected and the user manually selects or deselects individual fields ("ALL" not toggled)
**When** the Field dropdown is populated
**Then** only fields belonging to the selected Form are shown (existing working behavior — preserved unchanged)

**Given** a Form is selected in the Form dropdown
**When** the user toggles "ALL" ON in the Field dropdown
**Then** the Field dropdown enables and displays only the fields belonging to the selected Form — not all fields globally

**Given** the ALL-toggle event handler in the Data Summary Checks page JS
**When** ALL is toggled ON while a Form is selected
**Then** the handler re-populates the field list from the currently selected Form's fields only — not from the global field list; both the Form-select handler and the ALL-toggle handler enforce form-scoped field population when a Form is active

**Given** a Form is selected and "ALL" is toggled ON
**When** the user then clears the Form selection
**Then** the Field dropdown reverts to showing all fields (the default no-Form state)

**Given** the fix is validated in Edge and Chrome (NFR-1)
**When** tested in both browsers
**Then** behavior is consistent and correct in both

---

## Epic 10: CVS PDF Export Tool Reliability

The Community Vital Signs PDF export tool is hardened against transient failures at every layer — services, server, and client. Users receive actionable status messages, automatic retries with visible countdown, and a "Try again" path instead of a browser refresh. The parent case page button reflects in-progress state via BroadcastChannel.

### Story 10.1: Fix BatchSupervisor Busy-Wait CPU Spin

As a system operator,
When the CVS service is not yet available at startup,
I want the mmria-services BatchSupervisor to wait without consuming CPU,
So that the server remains responsive while retrying the CVS ping.

**Acceptance Criteria:**

**Given** the CVS service ping returns a non-ready result
**When** BatchSupervisor waits before the next retry
**Then** the wait is `await Task.Delay(CvsServerRetryDelayMs)` — not a spin loop — and CPU utilization during the wait is negligible

**Given** `BatchSupervisor` previously called `GetBatchSet(...).Result` synchronously inside its constructor
**When** the actor is created
**Then** the constructor no longer blocks on a CouchDB round-trip; the batch-list load is deferred via `Self.Tell(InitializeBatchList.Instance)` in `PreStart()`

**Given** a message arrives before the initial batch-list load has finished
**When** `BatchSupervisor` receives it in the `Initializing` behavior
**Then** the message is stashed; after `GetBatchSet` returns, `Become(Ready)` and `Stash.UnstashAll()` are called so no messages are lost

**Given** `GetBatchSet` throws during initialization
**When** the exception is caught
**Then** the actor logs the error, transitions to `Ready`, and releases the stash — subsequent messages are handled normally

### Story 10.2: Server-Side CVS Error Hardening

As a case reviewer generating a CVS PDF,
When the external CVS service fails for any reason,
I want the server to return a structured, descriptive result instead of an unhandled exception,
So that the client can display a meaningful message and react appropriately.

**Acceptance Criteria:**

**Given** any failure condition (network error, non-2xx HTTP, empty body, JSON parse error, Base64 decode error)
**When** `CVSManager.GetDashboardAsync` encounters it
**Then** a `CVSFileStatusResult` is returned with appropriate `file_status` and human-readable `message` — no unhandled exception propagates

**Given** the `message` field is set on `CVSFileStatusResult`
**When** `cvsAPIController` builds the response
**Then** `file_status_result.message = dashboardResult.message` is mapped and serialized in the JSON response

**Given** a CVS dashboard request completes
**When** the controller logs it
**Then** a structured log entry is written: `"CVS dashboard request completed. status={Status} duration_ms={DurationMs}"`

### Story 10.3: Client-Side CVS Retry Mechanism with Countdown

As a case reviewer generating a CVS PDF,
When the service is still preparing the report,
I want the page to automatically retry and show me a countdown between attempts,
So that I don't have to refresh the browser and I can see the system is actively working.

**Acceptance Criteria:**

**Given** the CVS page starts polling
**When** `run_cvs_report_polling` executes
**Then** polling is a bounded `for` loop up to `CVS_MAX_ATTEMPTS` — not a `while (!is_finished)` loop

**Given** the service returns `"generating"` or `"unavailable"` and more attempts remain
**When** `wait_for_next_attempt` is called
**Then** a live countdown (`CVS_RETRY_DELAY_SECONDS` down to 0) is shown in the UI; the retry fires automatically when the countdown reaches zero

**Given** max attempts are exhausted without a terminal result
**When** the loop exits
**Then** a **Try again** button is shown; clicking it restarts the loop without a page refresh

**Given** a polling run is already in progress
**When** `run_cvs_report_polling` is called again
**Then** the second call returns immediately (`g_is_running` guard)

### Story 10.4: CVS Parent-Page Button State via BroadcastChannel

As a case reviewer on the case form,
When I click the CVS report button and the report is generating in a separate tab,
I want the button to show it is busy and re-enable automatically when the report finishes,
So that I cannot accidentally open duplicate CVS windows and I know when the report is ready.

**Acceptance Criteria:**

**Given** the user clicks a CVS report button
**When** `beginCvsReportRequest(record_id, p_control)` is called
**Then** the button is disabled, `aria-busy="true"` is set, and the label changes to indicate in-progress state

**Given** the CVS window broadcasts a terminal status (`"ready"`, `"failed"`, `"max_retries"`, `"validation_error"`)
**When** the `BroadcastChannel('cvs_channel')` message handler receives it
**Then** the matching button is re-enabled, `aria-busy` is removed, and the original label is restored

**Given** no terminal BroadcastChannel message arrives within 20 minutes
**When** the fallback timer fires
**Then** the button is re-enabled automatically

**Given** `window.open` returns `null` (popup blocked)
**When** the null return is detected
**Then** `endCvsReportRequest(id)` is called immediately — no orphaned in-progress state

### Story 10.5: Config-Driven CVS Retry Constants

As a system administrator,
I want the CVS retry attempt count and delay interval to be configurable via the CouchDB configuration document,
So that these values can be tuned per environment without a code deployment.

**Acceptance Criteria:**

**Given** the CouchDB configuration document
**When** applied
**Then** `integer_keys.shared` contains `CVS_MAX_ATTEMPTS: 10` and `CVS_RETRY_DELAY_SECONDS: 60`

**Given** `CvsController.Index()` executes
**When** the view is served
**Then** `TempData["CVS_MAX_ATTEMPTS"]` and `TempData["CVS_RETRY_DELAY_SECONDS"]` are set using `configuration.GetInteger(key, host_prefix) ?? default` — no helper class

**Given** `Views/cvs/Index.cshtml` renders
**When** the `<head>` is emitted
**Then** an inline `<script>` placed before the `cvs/index.js` tag emits `window.CVS_MAX_ATTEMPTS` and `window.CVS_RETRY_DELAY_SECONDS` from TempData

**Given** `cvs/index.js` loads
**When** module-level constants are evaluated
**Then** `const CVS_MAX_ATTEMPTS = window.CVS_MAX_ATTEMPTS ?? 10` and `const CVS_RETRY_DELAY_SECONDS = window.CVS_RETRY_DELAY_SECONDS ?? 60` are used

---

## Epic 11 — Vitals Import Integer Type Fix

**Source requirements:** FR-12.1, FR-12.2
**Status:** not-started

### Summary
Dropdown fields written during NAT/FET vitals import (MARN, ACKN, and adjacent coded fields) are stored as JSON strings instead of JSON integers. The front-end dropdown resolver expects integers, causing imported cases to display "Select Value" for fields that were successfully imported.

The defect is in `C_Get_Set_Value.set_value(string, string, ...)` in `mmria.common` — it always assigns a .NET `string`, which Newtonsoft.Json serializes as a JSON string. mmria-server stores the same fields as .NET `int`, which serializes as a JSON number.

### Story 11.1: Vitals Import Integer Type Fix

As a case reviewer,
When a case is created via vitals import (NAT or FET file),
I want coded dropdown fields (such as Mother Married and Paternity Acknowledgement) to display their correct label values,
So that the case form does not show "Select Value" for fields that were successfully imported.

**Acceptance Criteria:**

**Given** a NAT file with `MARN = "Y"` is imported
**When** the vitals import processes the record
**Then** `mother_married` is stored as JSON number `1`, not string `"1"`, and the front-end displays "Yes"

**Given** a NAT file with `ACKN = "N"` is imported
**When** the vitals import processes the record
**Then** the paternity acknowledgement field is stored as JSON number `0`, not string `"0"`, and the front-end displays the correct label

**Given** a FET file with `MARN = "U"` is imported
**When** the vitals import processes the record
**Then** `mother_married` is stored as JSON number `7777`

**Given** the developer has audited MEDUC, FEDUC, ATTEND, TRAN, PAY, WIC at their `set_value` call sites
**When** any of those fields are expected as integers by the front-end
**Then** the same integer storage fix is applied to those call sites

**Given** free-text string fields such as MOMFNAME, MOMLNAME, ZIPCODE
**When** the import processes those fields
**Then** they continue to be stored as JSON strings with no regression

---

## Epic 12 — Data Migration Tool Modernization

**Source requirements:** FR-13.1–13.4, FR-14.1–14.5
**Status:** not-started

### Summary
The `data-migration` project has hardcoded jurisdiction lists, flat config with no credential separation, and no environment-switching mechanism. Story 12.1 refactors it to use a layered appsettings pattern matching the Replication project. Story 12.2 adds a `VitalsTypeCorrection` migration that retroactively fixes the integer type defect on historical case data.

Story 12.2 depends on Story 12.1.

### Story 12.1: Data Migration Environment Configuration Parity

As a developer running a data migration,
When I need to target a specific environment,
I want to set the environment in `appsettings.local.json` rather than editing source code,
So that I can switch environments and credentials without touching `Program.cs` or committing secrets.

**Acceptance Criteria:**

**Given** `appsettings.local.json` has `ConfigEnvironment = "QA"`
**When** the migration runs
**Then** it connects to the QA CouchDB URL, uses QA credentials, and iterates the QA jurisdiction prefix list

**Given** the developer clones the repo
**When** they open `data-migration/appsettings.json`
**Then** all `Username` and `Password` fields are empty strings and no secrets are committed

**Given** `data-migration/Configuration.cs` exists
**When** the developer reads it
**Then** it contains `DataMigrationAppConfiguration` with `MigrationSettings`, `EnvironmentSettings`, `CouchDBSettings`, `Credentials` (dict), and `JurisdictionLists` (dict)

**Given** the refactored `Program.cs`
**When** it executes
**Then** there is no static `run_list`, `test_list`, or `prefix_list` field
And `ConfigurationSet` (CouchDB-fetched config) is no longer loaded

### Story 12.2: Vitals Retrospective Type Correction Migration

As a database administrator,
After the vitals import integer type fix has been deployed (Story 11.1),
I want to run a targeted migration that converts previously imported string values to integers for the affected dropdown fields,
So that cases imported before the fix display the correct dropdown labels.

**Acceptance Criteria:**

**Given** `MigrationSettings.RunType = "VitalsTypeCorrection"` in appsettings.local.json
**When** the migration runs
**Then** `Program.cs` dispatches to `VitalsTypeCorrectionMigration`

**Given** a case document where `mother_married` is stored as `"0"` (JSON string)
**When** the migration processes it
**Then** `mother_married` is updated to `0` (JSON number) and the change is logged

**Given** `MigrationSettings.IsReportOnlyMode = true`
**When** the migration runs
**Then** no writes are issued to CouchDB and the log shows what would have changed

**Given** a case document where `mother_married` is already `0` (JSON number)
**When** the migration processes it
**Then** no write is performed (idempotent)
