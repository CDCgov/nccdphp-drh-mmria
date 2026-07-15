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
FR-2.2: When a vitals field value is rejected per FR-2.1, a modal is displayed: "The value entered for the [field label] field falls outside of the permitted range. Please enter a valid input between {min}â€“{max}." Focus returns to the cleared field on dismiss.
FR-2.3: Valid ranges for all vitals fields are stored in a single CouchDB configuration document, loaded once at server startup. A developer can update ranges by editing the config document and running the production update script â€” no code deployment required.
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
FR-8.1: A `system-offline-config` document in the CDC instance `metadata` CouchDB database stores: `warn_date`, `warn_message`, `offline_date`, `offline_modal_message`, `offline_page_message`. Saved and fetched via mmria-server controller â†’ mmria-services. Config is global across all tenants.
FR-8.2: At or after `warn_date`, logged-in users see a warning modal (displaying `warn_message`) once per browser session. Triggered on login and by the periodic check. Gated by `sessionStorage` flag.
FR-8.3: At or after `offline_date`, logged-in users see a going-offline modal (displaying `offline_modal_message`) with a single OK button. OK invokes best-effort save if a case is in edit mode, then signs the user out. Shown only once, gated by `localStorage` flag.
FR-8.4: At or after `offline_date`, the login page hides the login form fields and displays `offline_page_message` in white text in place of the login form area.
FR-8.5: While logged in, the client polls mmria-server every 2 minutes for current offline config and evaluates thresholds to trigger FR-8.2 or FR-8.3 as applicable.
FR-8.6: When a user navigates to the login page, the server checks the offline config and renders the page in offline state (FR-8.4) if `now >= offline_date`.
FR-8.7: An installation-admin-only admin page (modeled on `/broadcast-message`, linked from installation admin nav) allows editing and saving all five offline config fields. Saves via mmria-server â†’ mmria-services â†’ CDC instance `metadata` DB.
FR-9.1: On the Data Summary Checks page, when a Form is selected and the user toggles "ALL" in the Field dropdown, only fields belonging to the selected Form are shown and enabled. The no-Form-selected default state (all fields shown) is preserved unchanged.
FR-10.1: On the Manage Users page, clicking "Export User List" when a Role or Username filter is active exports only the currently displayed users. When no filter is active, all users are exported (existing default preserved).

### NonFunctional Requirements

NFR-1: All changes must function correctly in Microsoft Edge and Google Chrome. No other browsers are in scope.
NFR-2: The vitals validation modals (FR-2.2, FR-2.6) must meet Section 508 accessibility requirements â€” role, aria-modal, aria-labelledby, focus management, keyboard dismissal, and focus return.
NFR-3: Vitals range configuration is loaded once at server startup and held in memory. Field-level blur validation is synchronous against the in-memory config. No per-event network requests are introduced.

### Additional Requirements

- FR-1: All fixes are JavaScript-only in `wwwroot/scripts/case/index.js`. No server-side changes for FR-1.
- FR-1 (overriding constraint): The generated HTML structure must be identical to what the editor produces today. No new tags, no changed nesting, no reformatting. Stop stripping only â€” do not replace tags or modernize structure.
- FR-1.3: Use the Range API (`window.getSelection().getRangeAt(0)`, `range.insertNode`) directly. Do not use `document.execCommand('insertHTML')`. Strip XSS vectors only (onclick, onerror, javascript: hrefs) â€” preserve all structural tags.
- FR-2 scope: Apply validation and display-time exclusion to every vitals grid that renders the graph/table toggle control. Identify all such grids at implementation time â€” do not hardcode a form list.
- FR-2 server-side: `NestedStringDictionaryConverter` (custom `JsonConverter`) required to handle the nested `vital_sign_range` JSON object inside `string_keys.shared`. Applied via `[JsonConverter]` attribute on `OverridableConfiguration.string_keys`. `VitalSignRangeHelper` static class in `mmria-server/util/` deserializes the raw JSON string into a typed model with hardcoded defaults matching confirmed ranges.
- FR-2 config key: `vital_sign_range` (nested under `string_keys.shared`). OI-4 (exact HTML `name` attributes for vitals inputs) remains open â€” developer confirms at implementation time.
- FR-3: Inline `GetString ?? default` pattern in controller only. No helper class, no new service. Hardcoded defaults inline: `omb_expiration_date` = `"05/31/2026"`, `mmria_version` = `"MMRIA V 4.1"`.
- FR-3.3: Developer also patches `omb_expiration_label.prompt` in `metadata.json` via the production update script when the OMB date changes. No client-side render-time substitution.
- FR-4: Surgical deletions only. No new code. Before removing `de-identified/index.js` redirect guard (~line 933), grep to confirm it is `core-summary`-specific.
- FR-4: Confirm `core_summary()` function has no remaining references before deleting the declaration.
- FR-5: Developer locates the render source by searching for the first distinctive phrase of the existing text. If the text originates from `metadata.json` or a CouchDB document, update via the database-scripts update path. Do not change surrounding markup or field structure.
- All open items (OI-3, OI-4, OI-5, OI-dev-B, OI-dev-C): do not block story creation but must be resolved before the affected implementation begins.
- FR-9: Client-side only. Locate both the Form-select event handler and the ALL-toggle event handler in the Data Summary Checks page JS. Both handlers must enforce form-scoped field population when a Form is selected. Developer confirms the form-to-field association mechanism (metadata-driven or hardcoded) at implementation time.
- FR-10: Client-side only. In `export_user_list_click()` in `manage-users/index.js`, replace the join target from `g_ui.user_summary_list` to `g_filtered_user_list`. No server-side changes.

### UX Design Requirements

N/A â€” no UX design document exists for this release. All UI patterns follow existing site conventions.

### FR Coverage Map

FR-1.1: Epic 1 â€” Save-path line break stripping fix
FR-1.2: Epic 1 â€” Save-path formatting stripping fix (underline, HR, font size)
FR-1.3: Epic 1 â€” Paste handler cursor integrity (Range API rewrite)
FR-2.1: Epic 2 â€” On-blur field-level hard block (clear + reject)
FR-2.2: Epic 2 â€” Field-level invalid entry modal with range text
FR-2.3: Epic 2 â€” Config-driven valid ranges in CouchDB + server-side loading
FR-2.4: Epic 2 â€” Print/PDF display-time exclusion â†’ empty string
FR-2.5: Epic 2 â€” Graph/table display-time exclusion
FR-2.6: Epic 2 â€” Historical data detection on edit-mode entry + form navigation
FR-2.7: Epic 2 â€” PDF vitals date "/ /" fix â†’ empty string
FR-3.1: Epic 3 â€” OMB expiration date config-driven (controller + Razor + DB doc)
FR-3.2: Epic 3 â€” MMRIA version config-driven (controller + Razor + DB doc)
FR-3.3: Epic 3 â€” Developer update workflow (no admin UI, script-driven)
FR-4.1: Epic 3 â€” Remove core-summary option from three print dropdowns
FR-4.2: Epic 3 â€” Remove core-summary dead code from pdf-version/index.js
FR-5.1: Epic 1 â€” Case Narrative instruction text replacement
FR-7.1: Epic 7 â€” Audit log entry for Year of Death admin change
FR-7.2: Epic 7 â€” Audit log entry for Maiden Name admin change
FR-7.3: Epic 7 â€” Audit log entry for Unlock and Clear Case Status
FR-7.4: Epic 7 â€” Audit log entry for Recover Deleted Case
FR-7.5: Epic 7 â€” Audit log entry for Delete Case
FR-8.1: Epic 8 â€” System offline config document, mmria-services, controller
FR-8.2: Epic 8 â€” Warning modal (warn date, session-gated)
FR-8.3: Epic 8 â€” Going offline modal (offline date, localStorage-gated, save + sign out)
FR-8.4: Epic 8 â€” Login page offline state (hide form, show message)
FR-8.5: Epic 8 â€” Periodic status check (2-minute client poll)
FR-8.6: Epic 8 â€” Login page server-side offline check
FR-8.7: Epic 8 â€” Installation admin page for offline config
FR-9.1: Standalone Bug Fix â€” Data Summary Checks "ALL" toggle scoped to selected Form
FR-10.1: Standalone Bug Fix â€” Manage Users Export scoped to active filter
FR-11.1: Epic 10 â€” Fix BatchSupervisor busy-wait CPU spin (mmria-services)
FR-11.2: Epic 10 â€” Server-side CVS structured error handling (CVSManager, CVSDAL, CVSModels, cvsAPIController)
FR-11.3: Epic 10 â€” Client-side CVS retry loop with countdown and try-again button
FR-11.4: Epic 10 â€” BroadcastChannel CVS status and parent-page button state (mmria.js)

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

The Community Vital Signs PDF export tool is hardened against transient failures at every layer â€” services, server, and client. Users receive actionable status messages, automatic retries with visible countdown, and a "Try again" path instead of a browser refresh. The parent case page button reflects in-progress state via BroadcastChannel.
**FRs covered:** FR-11.1, FR-11.2, FR-11.3, FR-11.4

- FR-10: Client-side only. In `export_user_list_click()` in `manage-users/index.js`, replace the join target from `g_ui.user_summary_list` to `g_filtered_user_list`. No server-side changes.

### Epic 16: Controller Pattern Remediation

Stories in Epics 7 and 8 were authored against an earlier version of `project-context.md`. This epic pays down the resulting SharedLibraries `{Feature}/Manager/DAL` debt for the two remaining controller surfaces that still do direct CouchDB calls: `system_offlineController` (Epic 8) and the Wave 9 `CaseWorkflowAdmin` pair (`clear_case_status`, `recover_deleted_case`).
**Architecture rule:** project-context.md Â§2.2 SharedLibraries pattern

### Epic 22: .NET 10 Upgrade

All mmria projects (server, services, common, utilities) are upgraded from .NET 9 to .NET 10. The .NET 10 SDK is installed on the developer machine, all project target frameworks are updated, NuGet packages are verified for .NET 10 compatibility, and both production Dockerfiles are updated to use the .NET 10 trusted base images from the EcPaaS registry.
**Stories:** 22.1 — Compatibility Analysis & Risk Assessment, 22.2 — Upgrade Execution

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
**Then** only executable attributes (`onclick`, `onerror`, `javascript:` hrefs) are removed â€” structural tags (`<br>`, `<u>`, `<hr>`, `<font>`) are preserved unchanged

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
**Then** it uses the Range API (`window.getSelection().getRangeAt(0)`, `range.deleteContents()`, `range.insertNode()`) to capture selection state synchronously at the top of the handler â€” `document.execCommand('insertHTML')` is not used

**Given** content pasted from an external source (Word, another application)
**When** the paste is processed
**Then** only executable XSS attributes (`onclick`, `onerror`, `javascript:` hrefs) are stripped â€” all structural HTML tags are preserved

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
**Then** no surrounding markup or field structure is changed â€” text content only

**Given** the text originates from a CouchDB document or `metadata.json`
**When** the update is applied
**Then** it is applied via the database-scripts update path, not a Razor/JS edit

---

## Epic 2: Vitals Field Validation

Reviewers entering vitals data are immediately alerted when values fall outside clinical ranges, preventing unreliable data from entering graphs, tables, print, and PDF views. Existing cases with out-of-range values are flagged at review time.

### Story 2.1: Add Vitals Range Config â€” CouchDB Document and Server-Side Loading

As a developer,
I want the valid ranges for all vitals fields stored in CouchDB and loaded into memory at server startup,
So that vitals validation and display-time exclusion can read ranges synchronously without network requests, and a developer can update ranges by script without a code deployment.

**Acceptance Criteria:**

**Given** the CouchDB config document in `database-scripts/`
**When** the developer updates it
**Then** it contains a `vital_sign_range` nested object under `string_keys.shared` with the confirmed ranges: Temperature 0â€“110, Heart Rate 0â€“400, Respiration 0â€“60, Systolic BP 0â€“300, Diastolic BP 0â€“300, Oxygen Saturation 0â€“100 â€” each entry carrying `min`, `max`, and `label` keys

**Given** `OverridableConfiguration.string_keys` is typed as `Dictionary<string, Dictionary<string, string>>`
**When** the config document is deserialized at server startup
**Then** a `NestedStringDictionaryConverter` (custom `JsonConverter`) is applied via `[JsonConverter]` attribute on `string_keys`, storing the nested `vital_sign_range` object as its raw JSON string

**Given** the raw JSON string for `vital_sign_range` is held in `OverridableConfiguration`
**When** `VitalSignRangeHelper.GetVitalSignRangeConfig(configuration, host_prefix)` is called from `CaseController`
**Then** it deserializes the raw JSON into a typed `VitalSignRangeConfig` model and returns it; if the key is absent or unparseable, it returns the hardcoded defaults matching the confirmed ranges

**Given** `CaseController.Index()` calls `VitalSignRangeHelper.GetVitalSignRangeConfig()`
**When** the Case page is served
**Then** the serialized config is set as `TempData["vital_sign_range_config"]` and emitted into the `HeadScripts` block as `window.mmria_vital_sign_range = @Html.Raw(TempData["vital_sign_range_config"]);` â€” following the same pattern as `window.case_edit_inactivity_config`

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
**Then** a modal is displayed with the message: "The value entered for the [field label] field falls outside of the permitted range. Please enter a valid input between {min}â€“{max}." using the existing site modal pattern (purple header, OK button)

**Given** the modal is dismissed
**When** the reviewer clicks OK or presses Escape/Enter
**Then** focus returns to the cleared field (NFR-2)

**Given** `window.mmria_vital_sign_range` is `null`
**When** blur fires on a vitals field
**Then** no validation runs and no modal appears â€” silent skip

**Given** the field value is empty or not a number
**When** blur fires
**Then** no validation runs â€” only non-empty parseable numeric values are validated

**Given** Save & Continue, Save & Finish, or autosave fires
**When** a vitals field is in any state
**Then** no validation runs at save time â€” whatever is in the field is saved as-is

**Given** the validation function `mmria_vitals_validate_field(inputElement)` is implemented in `chart.js`
**When** it is attached
**Then** it attaches to blur, keydown (Tab key), and paste events on every vitals input in scope â€” identified by the presence of the graph/table toggle on the same grid, not by a hardcoded form list (OI-4: developer confirms exact `name` attributes at implementation time)

### Story 2.3: Display-Time Exclusion â€” Print, PDF, and Vitals Date Fix

As a CDC analyst reviewing submitted case data,
I want out-of-range vitals values to appear as blank in printed reports and PDFs,
So that printed output does not surface unreliable data.

**Acceptance Criteria:**

**Given** a vitals record with a value outside the configured range for that field
**When** the case is rendered in print view
**Then** that vitals field renders as empty string â€” the stored database value is not affected

**Given** the same out-of-range value
**When** the case is rendered as a PDF
**Then** that vitals field renders as empty string in the PDF output â€” stored value unchanged

**Given** the PDF rendering path for vitals date fields currently outputs `/ /` for empty or invalid dates
**When** a vitals date field is empty or invalid
**Then** the PDF renders an empty string instead of `/ /` â€” this fix is scoped to vitals date fields in the PDF rendering path only

**Given** all exclusion above
**When** the reviewer views the same case in the case form editor
**Then** the case form input field continues to display the stored value unchanged

### Story 2.4: Display-Time Exclusion â€” Graph and Table Views

As a case reviewer,
I want out-of-range vitals values excluded from graphs and tables in the case form,
So that visual trends and tabular summaries only reflect clinically plausible data.

**Acceptance Criteria:**

**Given** a vitals record with a value outside the configured range
**When** the graph view renders for that vitals grid
**Then** the out-of-range data point is not plotted â€” no point, no line segment to/from it

**Given** the same out-of-range value
**When** the table view renders for that vitals grid
**Then** the cell for that field renders as empty â€” not the raw value

**Given** all exclusion above
**When** the reviewer views the case form input field for that same record
**Then** the input continues to display the stored value â€” exclusion is display-time only, not stored

**Given** `window.mmria_vital_sign_range` is `null`
**When** graph and table views render
**Then** all values render normally â€” no exclusion applied

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
**Then** the indicator is re-evaluated on each render â€” it is not a one-time write

**Given** `window.mmria_vital_sign_range` is `null`
**When** edit mode is entered or form navigation occurs
**Then** no re-validation runs, no modal appears, no indicators are applied

---

## Epic 3: System Configuration & Print Cleanup

Developers can update the OMB expiration date and MMRIA version number without a code deployment. The "Core Elements Only" unauthorized print option is removed from all affected dropdowns and dead code is cleaned up.

### Story 3.1: Config-Driven OMB Expiration Date

As a developer,
I want the OMB expiration date read from the CouchDB configuration document at render time,
So that the next date change can be applied by running the update script â€” no code deployment required.

**Acceptance Criteria:**

**Given** the CouchDB config document in `database-scripts/`
**When** the developer updates it
**Then** it contains a flat `omb_expiration_date` string key under `string_keys.shared` with default value `"05/31/2026"`

**Given** the relevant controller action(s) serving the Home page and Committee Decisions form (OI-5: developer identifies during implementation)
**When** those actions execute
**Then** they set a TempData or ViewBag entry for the OMB date using the pattern: `configuration.GetString("omb_expiration_date", host_prefix) ?? "05/31/2026"` â€” no helper class, no new service

**Given** `Views/Shared/_BurdenStatement.cshtml` currently contains the hardcoded string `Exp. Date 05/31/2026`
**When** the partial renders
**Then** it reads the OMB date from the TempData/ViewBag entry set by the controller, following the existing path that already provides data to this partial

**Given** the `omb_expiration_label` field in `metadata.json` carries `"Exp. Date 05/31/2026"` as its `prompt` value
**When** the OMB date is updated
**Then** the developer also patches `omb_expiration_label.prompt` in the metadata document via the production update script â€” no client-side render-time substitution is required

**Given** the `omb_expiration_date` key is absent from the config document
**When** the controller reads it
**Then** the hardcoded default `"05/31/2026"` is used and the page renders correctly

### Story 3.2: Config-Driven MMRIA Version Number

As a developer,
I want the MMRIA version number read from the CouchDB configuration document at render time,
So that the next version change can be applied by running the update script â€” no code deployment required.

**Acceptance Criteria:**

**Given** the CouchDB config document in `database-scripts/`
**When** the developer updates it
**Then** it contains a flat `mmria_version` string key under `string_keys.shared` with default value `"MMRIA V 4.1"`

**Given** the relevant controller action(s) serving the application layout (OI-5: developer identifies during implementation)
**When** those actions execute
**Then** they set a TempData or ViewBag entry for the version using the pattern: `configuration.GetString("mmria_version", host_prefix) ?? "MMRIA V 4.1"` â€” no helper class, no new service

**Given** `Views/Shared/_Footer.cshtml` line 7 currently contains two occurrences of the hardcoded string `MMRIA V4.0.1` (in both the `aria-label` attribute and the visible text)
**When** the footer renders
**Then** both occurrences are replaced with the TempData/ViewBag value â€” no hardcoded version string remains

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
**Then** the option is absent and the redirect guard block is also removed if (and only if) it exclusively guards the `core-summary` case â€” if it guards other cases, only the `core-summary` branch is removed

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
**Then** they follow the identical pattern used by existing case-edit audit entries â€” no new fields, no schema changes

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
**Then** they follow the identical pattern used by existing case-edit audit entries â€” no new fields, no schema changes

**Given** any of the above admin actions fails
**When** the failure occurs
**Then** no audit entry is written

---

## Epic 8: System Going Offline

Installation administrators can schedule a planned system outage. Logged-in users receive advance warning, are guided to save their work and sign out before the system goes offline, and are prevented from logging in once the offline date is reached.

### Story 8.1: System Offline Config â€” Document, mmria-services, Controller, and Admin Page

As an installation administrator,
I want a dedicated admin page where I can configure warn and offline dates and messages,
So that I can schedule a planned outage and control the messaging users see at each stage.

**Acceptance Criteria:**

**Given** the CDC instance `metadata` CouchDB database
**When** the developer creates the config document
**Then** a document with `_id: "system-offline-config"` exists carrying these five fields: `warn_date` (ISO 8601 string), `warn_message` (string), `offline_date` (ISO 8601 string), `offline_modal_message` (string), `offline_page_message` (string)

**Given** mmria-services
**When** a get or save request arrives for the offline config
**Then** mmria-services fetches from or writes to the `system-offline-config` document in the CDC instance `metadata` database â€” following the existing pattern for other metadata documents

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
**Then** the page returns 403 / unauthorized â€” same access control pattern as `/broadcast-message`

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

**Given** the "please contact your jurisdiction adminâ€¦" text currently appears on the login page
**When** the login page renders in offline state
**Then** that text is replaced by `offline_page_message`; no other login page elements are changed

**Given** a user navigates to the login page and `now < offline_date` (or `offline_date` is null)
**When** the server renders the login page
**Then** the login page renders normally â€” no offline state applied

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
**Then** the flag is set â€” the modal cannot reappear (login is also disabled by this point)

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
**Then** the failure is silently swallowed â€” no error is surfaced to the user and polling continues on the next interval

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
**Then** the Field dropdown shows all fields across all forms (existing default behavior â€” preserved unchanged)

**Given** a Form is selected and the user manually selects or deselects individual fields ("ALL" not toggled)
**When** the Field dropdown is populated
**Then** only fields belonging to the selected Form are shown (existing working behavior â€” preserved unchanged)

**Given** a Form is selected in the Form dropdown
**When** the user toggles "ALL" ON in the Field dropdown
**Then** the Field dropdown enables and displays only the fields belonging to the selected Form â€” not all fields globally

**Given** the ALL-toggle event handler in the Data Summary Checks page JS
**When** ALL is toggled ON while a Form is selected
**Then** the handler re-populates the field list from the currently selected Form's fields only â€” not from the global field list; both the Form-select handler and the ALL-toggle handler enforce form-scoped field population when a Form is active

**Given** a Form is selected and "ALL" is toggled ON
**When** the user then clears the Form selection
**Then** the Field dropdown reverts to showing all fields (the default no-Form state)

**Given** the fix is validated in Edge and Chrome (NFR-1)
**When** tested in both browsers
**Then** behavior is consistent and correct in both

---

## Epic 10: CVS PDF Export Tool Reliability

The Community Vital Signs PDF export tool is hardened against transient failures at every layer â€” services, server, and client. Users receive actionable status messages, automatic retries with visible countdown, and a "Try again" path instead of a browser refresh. The parent case page button reflects in-progress state via BroadcastChannel.

### Story 10.1: Fix BatchSupervisor Busy-Wait CPU Spin

As a system operator,
When the CVS service is not yet available at startup,
I want the mmria-services BatchSupervisor to wait without consuming CPU,
So that the server remains responsive while retrying the CVS ping.

**Acceptance Criteria:**

**Given** the CVS service ping returns a non-ready result
**When** BatchSupervisor waits before the next retry
**Then** the wait is `await Task.Delay(CvsServerRetryDelayMs)` â€” not a spin loop â€” and CPU utilization during the wait is negligible

**Given** `BatchSupervisor` previously called `GetBatchSet(...).Result` synchronously inside its constructor
**When** the actor is created
**Then** the constructor no longer blocks on a CouchDB round-trip; the batch-list load is deferred via `Self.Tell(InitializeBatchList.Instance)` in `PreStart()`

**Given** a message arrives before the initial batch-list load has finished
**When** `BatchSupervisor` receives it in the `Initializing` behavior
**Then** the message is stashed; after `GetBatchSet` returns, `Become(Ready)` and `Stash.UnstashAll()` are called so no messages are lost

**Given** `GetBatchSet` throws during initialization
**When** the exception is caught
**Then** the actor logs the error, transitions to `Ready`, and releases the stash â€” subsequent messages are handled normally

### Story 10.2: Server-Side CVS Error Hardening

As a case reviewer generating a CVS PDF,
When the external CVS service fails for any reason,
I want the server to return a structured, descriptive result instead of an unhandled exception,
So that the client can display a meaningful message and react appropriately.

**Acceptance Criteria:**

**Given** any failure condition (network error, non-2xx HTTP, empty body, JSON parse error, Base64 decode error)
**When** `CVSManager.GetDashboardAsync` encounters it
**Then** a `CVSFileStatusResult` is returned with appropriate `file_status` and human-readable `message` â€” no unhandled exception propagates

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
**Then** polling is a bounded `for` loop up to `CVS_MAX_ATTEMPTS` â€” not a `while (!is_finished)` loop

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

---

## Epic 17: mmrds CRUD Consolidation (SQL Migration Foundation)

All case-document reads and writes against the `{prefix}mmrds` CouchDB database are consolidated behind a single `CaseDAL` surface, backed by an `ICaseRepository` interface. Duplicated URL construction and scattered direct HTTP calls are eliminated across `mmria-server`, `mmria.common`, and `mmria.services`. After this epic, swapping CouchDB for SQL requires changing `CaseDAL` only — no Manager, controller, or services actor code changes are required.

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness.

**Scope summary (verified 2026-07-14):**

Duplicate mmrds operations were found across 16 files in three projects:

- `mmria.common/SharedLibraries`: `CaseDAL`, `CaseManager`, `CaseWorkflowAdminDAL`, `AuditRecoveryDAL`, `CVSDAL`, `VitalImportDAL`, `AttachmentDAL`, `OfflineCaseManager`, `MMRIAServicesDAL`
- `mmria-server/model/actor`: `JurisdictionSummary`, `VROSummary`, `c_db_setup`, `c_document_sync_all`, `c_document_sync_all_legacy`
- `mmria.services`: `BatchProcessor`, `BatchItemProcessingService`, `PagedCaseIdLoader`, `core_element_exporter`, `exporter`, `mmrds_exporter`, `c_document_sync_all`

Three URL construction patterns are in active simultaneous use:

- **Pattern A** (wrong — leaks prefix logic): `$"{dbConfig.url}/{dbConfig.prefix}mmrds/{id}"`
- **Pattern B** (correct): `dbConfig.Get_Prefix_DB_Url($"mmrds/{id}")`
- **Pattern C** (CDC special-case, inconsistent separator): `$"{dbInfo.url}/{dbInfo.prefix}_mmrds"` — used only in `MMRIAServicesDAL`

---

### Story 17.1: mmrds Operation Catalog

As a developer,
I want a definitive catalog of every operation against the `mmrds` database across all three projects,
So that Stories 17.2–17.7 have an agreed-upon, complete operation set before any code changes begin.

**Acceptance Criteria:**

**Given** all `.cs` files in `mmria-server`, `mmria.common`, and `mmria.services`
**When** the developer completes the catalog
**Then** `docs/ai/mmrds_operation_catalog.md` exists and contains a table of every distinct operation grouped into: Case CRUD (GET/PUT/DELETE by ID), versioned reads (GET at revision, GET all revisions), view queries (`by_date_created`, `by_date_last_updated`, `by_jurisdiction_id`, `by_last_name`, `by_pmss_number`, `record_id_list`), Mango `_find` queries, bulk operations (`_bulk_docs`, `_all_docs`), and admin/infra operations (`_security`, `_design/*`, `_changes`)

**Given** each catalog entry
**When** the catalog is complete
**Then** each entry records: operation name, calling file(s), the URL pattern in use (A, B, or C), and the response type expected

**Given** admin/infra operations (`_security`, `_design/*`, `_changes`, sync `_all_docs`)
**When** the catalog is written
**Then** they are listed but marked **out of scope** for Stories 17.2–17.7 — these operations are infrastructure-only and do not belong behind `ICaseRepository`

---

### Story 17.2: Canonicalize CaseDAL and Extract ICaseRepository

As a developer,
I want a single `ICaseRepository` interface over all mmrds CRUD operations,
So that every caller in mmria-server and mmria.services can depend on the interface and a SQL migration requires changing only the `CaseDAL` implementation.

**Acceptance Criteria:**

**Given** the existing `CaseDAL` in `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs`
**When** this story is complete
**Then** all existing methods in `CaseDAL` use `dbConfig.Get_Prefix_DB_Url(...)` uniformly — no Pattern A strings remain

**Given** the operation catalog from Story 17.1
**When** the developer adds missing operations to `CaseDAL`
**Then** `CaseDAL` contains methods for every in-scope operation: `GetCaseAsync`, `GetCaseDocumentJsonAsync`, `UpdateCaseAsync`, `PutCaseDocumentJsonAsync`, `DeleteCaseAsync`, `GetCaseAtRevisionAsync`, `GetCaseRevisionsAsync`, all required view query methods, and the `_find` overloads needed by other stories

**Given** the full operation set is in `CaseDAL`
**When** the interface is extracted
**Then** `ICaseRepository` is defined in `mmria.common/SharedLibraries/Case/` with async method signatures matching every `CaseDAL` method; `CaseDAL` implements `ICaseRepository`

**Given** `ICaseRepository` is defined
**When** DI registration is updated in `mmria-server`
**Then** `ICaseRepository` is registered as `CaseDAL` in the server's service collection; all existing callers of the concrete `CaseDAL` compile without changes

**Given** no callers are changed in this story
**When** the build runs after this story
**Then** `mmria-server` and `mmria.common` build with zero errors

---

### Story 17.3: Route CaseManager Direct mmrds Calls Through CaseDAL

As a developer,
I want `CaseManager` to stop calling `CouchDbHttpClient.ExecuteAsync` with mmrds URLs directly,
So that all case document access in the manager layer routes through `ICaseRepository`.

**Acceptance Criteria:**

**Given** the following direct mmrds HTTP calls in `CaseManager.cs` (approximately lines 900, 1025, 1205, 1330, 1369, 1519, 1723, 2098, 2280, 2294, 2392)
**When** this story is complete
**Then** each call is replaced with the corresponding `ICaseRepository` method from Story 17.2; no `$"{dbConfig...}mmrds/..."` strings remain in `CaseManager.cs`

**Given** each replacement
**When** the developer implements it
**Then** the HTTP verb, URL path, request body, response deserialization type, and error handling are identical to the original — this is a mechanical substitution only

**Given** the build after all substitutions
**When** verified
**Then** `mmria-server`, `mmria.common`, and `mmria.services` all build with zero errors

**Given** no controller action signatures or response shapes change
**When** verified
**Then** no changes are made outside of `CaseManager.cs` in this story

---

### Story 17.4: Eliminate Duplicate mmrds CRUD in CaseWorkflowAdminDAL

As a developer,
I want `CaseWorkflowAdminDAL` to delegate case document operations to `ICaseRepository` instead of reimplementing them,
So that the five duplicate mmrds methods in this DAL are removed.

**Acceptance Criteria:**

**Given** the following methods in `CaseWorkflowAdminDAL` that duplicate `CaseDAL` operations:
`GetCaseDocumentAsync`, `UpdateCaseDocumentAsync`, `GetCaseRevisionsRawAsync`, `GetCaseAtRevisionAsync`, `RestoreCaseDocumentAsync`
**When** this story is complete
**Then** `ICaseRepository` is injected into `CaseWorkflowAdminDAL` and each of the five methods delegates to the corresponding repository method; the duplicate implementations are removed

**Given** the audit write methods in `CaseWorkflowAdminDAL` (operations against the `audit` database)
**When** this story is complete
**Then** they are unchanged — audit writes are not mmrds operations and are out of scope

**Given** `CaseWorkflowAdminManager` calls the above DAL methods
**When** the DAL signatures are preserved
**Then** `CaseWorkflowAdminManager` and all controllers that use it compile without changes

---

### Story 17.5: Eliminate Duplicate mmrds Calls in AuditRecoveryDAL, CVSDAL, VitalImportDAL, and AttachmentDAL

As a developer,
I want the remaining SharedLibraries DAL files that independently call mmrds URLs to delegate to `ICaseRepository`,
So that mmrds access is fully consolidated within the common library layer.

**Acceptance Criteria:**

**Given** the following direct mmrds calls:
- `AuditRecoveryDAL.cs` lines 24, 75 — case view `by_id` query and case GET at revision
- `CVSDAL.cs` lines 73, 84 — `by_date_last_updated` view and case GET by ID
- `VitalImportDAL.cs` lines 26, 33 — case GET by ID ×2
- `AttachmentDAL.cs` line 21 — mmrds `by_pmss_number` view query
**When** this story is complete
**Then** each is replaced with the corresponding `ICaseRepository` method; `ICaseRepository` is injected into each DAL via constructor injection

**Given** each DAL's existing constructor and DI registration
**When** `ICaseRepository` is added as a constructor parameter
**Then** the DI registration in `mmria-server` is updated to satisfy the new dependency; no other registration changes are made

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors and no manager or controller code changes are required

---

### Story 17.5b: Route mmria-services Case Reads Through ICaseRepository

As a developer,
I want the background-job and exporter code in `mmria.services` to stop constructing mmrds URLs directly,
So that the services project is covered by the same `ICaseRepository` contract as the server.

**Acceptance Criteria:**

**Given** the following direct mmrds URL constructions in `mmria.services`:
- `BatchProcessor.cs` — `_all_docs` and case GET at revision
- `BatchItemProcessingService.cs` — case GET by ID
- `PagedCaseIdLoader.cs` — `by_date_created` view
- `core_element_exporter.cs` — case GET by ID
- `exporter.cs` — `_all_docs` and case GET by ID
- `mmrds_exporter.cs` — case GET by ID
**When** this story is complete
**Then** each is replaced with the corresponding `ICaseRepository` method; since `mmria.services` already references `mmria.common`, no new project reference is needed

**Given** the `_all_docs` usages in `BatchProcessor` and `exporter`
**When** the developer evaluates them
**Then** if a corresponding `ICaseRepository` method does not exist, it is added to `CaseDAL` and `ICaseRepository` as part of this story (following the same rules as Story 17.2)

**Given** `c_document_sync_all.cs` in `mmria.services` (bulk sync `_all_docs`)
**When** evaluated
**Then** it is treated as an infrastructure/sync operation — documented in the catalog as out of scope and left unchanged in this story

**Given** the build after all changes
**When** verified
**Then** `mmria.services`, `mmria.common`, and `mmria-server` all build with zero errors

---

### Story 17.6: Eliminate Direct mmrds Calls in OfflineCaseManager

As a developer,
I want `OfflineCaseManager` to stop issuing raw HTTP requests to mmrds URLs,
So that the offline case path follows the same Manager → DAL boundary as every other feature.

**Acceptance Criteria:**

**Given** the three direct mmrds HTTP calls in `OfflineCaseManager.cs` (lines 104, 298, 398) that assemble URLs as `$"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}"`
**When** this story is complete
**Then** each is replaced with the corresponding `ICaseRepository` method; `ICaseRepository` is injected into `OfflineCaseManager` via constructor injection

**Given** the `OfflineCase` feature in `SharedLibraries` already has an `OfflineCaseDAL`
**When** the developer evaluates whether to route through `OfflineCaseDAL` or inject `ICaseRepository` directly into the manager
**Then** `ICaseRepository` is injected directly into the manager — `OfflineCaseDAL` owns offline-specific document types, not generic case CRUD

**Given** the DI registration for `OfflineCaseManager`
**When** `ICaseRepository` is added as a constructor parameter
**Then** the registration is updated in `mmria-server` to satisfy the new dependency

**Given** the build and existing offline sync tests (if any)
**When** verified
**Then** all three projects build with zero errors and offline case behavior is unchanged

---

### Story 17.7: MMRIAServicesDAL and Sync Boundary Decision

As a developer,
I want a written architecture decision on whether the CDC populate path and bulk sync operations in `MMRIAServicesDAL` and `c_document_sync_all` should be unified with `ICaseRepository` or formally declared as separate infrastructure concerns,
So that the boundary is explicit and future contributors do not try to merge them incorrectly.

**Acceptance Criteria:**

**Given** `MMRIAServicesDAL` has its own `GetMmrdsDatabaseUrl()` helper (lines 553–557) that uses a different prefix separator convention from `Get_Prefix_DB_Url`
**When** the developer evaluates the CDC populate path
**Then** a decision is recorded in `docs/ai/mmrds_operation_catalog.md` under a "Boundary Decisions" section: either (a) unify prefix logic and route through `ICaseRepository` or (b) formally declare the CDC bulk path as a separate infrastructure concern that `ICaseRepository` does not cover

**Given** `c_document_sync_all` in both `mmria-server` and `mmria.services` uses bulk `_all_docs` for change-feed synchronization
**When** the developer evaluates it
**Then** the same decision document records whether sync bulk reads belong behind `ICaseRepository` or remain as infrastructure-only operations; recommendation is **out of scope** given the change-feed architecture

**Given** the decision document is complete
**When** it recommends unification (option a)
**Then** the prefix inconsistency in `MMRIAServicesDAL` is fixed in this story and a follow-on story is created if full interface adoption is needed

**Given** the decision document is complete
**When** it recommends keeping as separate concerns (option b)
**Then** no code changes are made to `MMRIAServicesDAL` or `c_document_sync_all` in this epic; the catalog marks them explicitly as out-of-scope infrastructure

---

## Epic 17 — Story Sequencing

| Wave | Story | Risk | Dependencies |
|---|---|---|---|
| 17 | 17.1 — mmrds Operation Catalog | None | None — discovery only |
| 17 | 17.7 — Boundary Decision | None | Can run in parallel with 17.1 |
| 17 | 17.2 — ICaseRepository + CaseDAL | Low | 17.1 |
| 17 | 17.3 — CaseManager direct calls | Medium | 17.2 |
| 17 | 17.4 — CaseWorkflowAdminDAL | Low | 17.2 |
| 17 | 17.5 — AuditRecovery / CVS / VitalImport / Attachment | Low | 17.2 |
| 17 | 17.5b — mmria.services | Medium | 17.2 |
| 17 | 17.6 — OfflineCaseManager | Medium | 17.2 |

17.3, 17.4, 17.5, 17.5b, and 17.6 can proceed in parallel once 17.2 is complete. 17.7 can run alongside 17.1.
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
**Then** `endCvsReportRequest(id)` is called immediately â€” no orphaned in-progress state

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
**Then** `TempData["CVS_MAX_ATTEMPTS"]` and `TempData["CVS_RETRY_DELAY_SECONDS"]` are set using `configuration.GetInteger(key, host_prefix) ?? default` â€” no helper class

**Given** `Views/cvs/Index.cshtml` renders
**When** the `<head>` is emitted
**Then** an inline `<script>` placed before the `cvs/index.js` tag emits `window.CVS_MAX_ATTEMPTS` and `window.CVS_RETRY_DELAY_SECONDS` from TempData

**Given** `cvs/index.js` loads
**When** module-level constants are evaluated
**Then** `const CVS_MAX_ATTEMPTS = window.CVS_MAX_ATTEMPTS ?? 10` and `const CVS_RETRY_DELAY_SECONDS = window.CVS_RETRY_DELAY_SECONDS ?? 60` are used

---

## Epic 11 â€” Vitals Import Integer Type Fix

**Source requirements:** FR-12.1, FR-12.2
**Status:** not-started

### Summary
Dropdown fields written during NAT/FET vitals import (MARN, ACKN, and adjacent coded fields) are stored as JSON strings instead of JSON integers. The front-end dropdown resolver expects integers, causing imported cases to display "Select Value" for fields that were successfully imported.

The defect is in `C_Get_Set_Value.set_value(string, string, ...)` in `mmria.common` â€” it always assigns a .NET `string`, which Newtonsoft.Json serializes as a JSON string. mmria-server stores the same fields as .NET `int`, which serializes as a JSON number.

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

## Epic 12 â€” Data Migration Tool Modernization

**Source requirements:** FR-13.1â€“13.4, FR-14.1â€“14.5
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

### Story 12.3: Migration Tool Hardening

As a database administrator running a data migration,
I want the migration tool to retry on CouchDB 409 conflicts and halt on unrecoverable errors,
So that every case document is guaranteed to be processed and no case is silently skipped.

**Acceptance Criteria:**

**Given** a CouchDB PUT returns 409 (conflict)
**When** the migration processes that document
**Then** the migration fetches the fresh document, re-applies the transform, and retries up to 3 times
**And** on retry exhaustion the failure is counted and the loop continues to the next document

**Given** any retry has exhausted all 3 attempts for a document
**When** the run finishes
**Then** exit code is 3 and the final summary includes `Failed (retries exhausted): N`

**Given** a CouchDB PUT returns a non-409 error (network, auth, 500)
**When** the migration encounters it
**Then** the migration logs the `_id`, HTTP status, and response body to stderr and calls `Environment.Exit(1)` immediately

**Given** `DateTime.UtcNow < offline_date`
**When** the migration starts
**Then** it writes `"PRE-FLIGHT FAIL: system is not offline. Aborting."` to stderr and calls `Environment.Exit(2)`

**Given** the migration completes normally
**When** `failed_count == 0`
**Then** exit code is 0 and stdout shows `Processed: N | Already migrated: N | Failed (retries exhausted): 0`

### Story 12.4: Case Rev Endpoint

As the client-side case polling module,
I want a lightweight endpoint that returns only the current `_rev` of a case document,
So that I can detect whether the open case has been modified without fetching the full document.

**Acceptance Criteria:**

**Given** an authenticated GET to `/api/case/{id}/rev`
**When** the document exists in CouchDB
**Then** the response is `200 { "_id": "<id>", "_rev": "<rev>" }` only

**Given** an authenticated GET to `/api/case/{id}/rev`
**When** the document does not exist
**Then** the response is `404`

**Given** an unauthenticated GET to `/api/case/{id}/rev`
**When** the request is received
**Then** the response is `401` â€” auth is required

### Story 12.5: Stale Tab UX

As a case coordinator with a stale browser tab,
I want to be proactively notified when a case has been updated since I opened it, and to receive a clear recovery message if I attempt to save stale data,
So that I never lose work silently or see a confusing technical error.

**Acceptance Criteria:**

**Given** a case is open for editing, the current tab owns the active checkout, and its `_rev` changes on the server
**When** the 45-second poll detects the mismatch
**Then** a non-dismissable modal appears: "This case has been updated. Reload to see the latest version." with a single [Reload] action

**Given** a user attempts to save and the server returns 409
**When** the case save error handler processes the response
**Then** a non-dismissable modal appears: "This case was updated elsewhere. Reload to get the latest version before saving." with a single [Reload Case] button
**And** the generic error handler does not fire for the 409 case

**Given** `_rev` polling is active
**When** the poll interval is evaluated
**Then** the poll interval remains 45 seconds and does not depend on offline-status headers

**Given** the current tab does not own the active edit checkout for the case
**When** the case loads
**Then** no polling is started
**And** if the user leaves edit mode through Save & Close or another lock-release path
**Then** any active `_rev` polling interval is stopped

---

## Epic 16 â€” Controller Pattern Remediation

**Source requirements:** project-context.md Â§2.2 SharedLibraries pattern; controller_sharedlibraries_migration_matrix.md Wave 9
**Status:** not-started
**Depends on:** none

### Summary

Epics 7 and 8 were authored before `project-context.md` was updated. Two anti-patterns remain in shipping code:

1. `system_offlineController` (Epic 8) calls `_couchDbHttpClient.ExecuteAsync(...)` directly and owns message-substitution logic that belongs in a Manager â€” no `SharedLibraries/SystemOffline/` feature exists.
2. `clear_case_status.cs` and `recover_deleted_case.cs` (Wave 9, migration matrix) still own raw CouchDB calls. Epic 7 audit stories (7.1 and 7.2) were implemented on top of these controllers at `verification` status, compounding the debt.

Two earlier remediation stories (VitalSignRangeHelper relocation and Case Rev endpoint) were superseded before this epic was finalized:
- VitalSignRangeHelper is deleted by Story 4.0 (replaced by the validation engine).
- Story 12.3 (Case Rev Endpoint) was implemented as `done`.

The two stories in this epic are independent of each other.

---

### Story 16.1: Establish SystemOffline SharedLibraries Feature

As a developer working on the system offline feature,
I want the system offline business logic and CouchDB/service access to live in a SharedLibraries Manager and DAL,
So that `system_offlineController` contains only routing, authorization, and response shaping â€” no direct service calls.

**Acceptance Criteria:**

**Given** the current `system_offlineController` calls `_couchDbHttpClient.ExecuteAsync(...)` directly in `SaveConfig()` and in the private `LoadConfigFromServicesAsync()` helper
**When** this story is complete
**Then** both of those calls have been moved into `SharedLibraries/SystemOffline/DAL/SystemOfflineDAL.cs`; the controller delegates through `SharedLibraries/SystemOffline/Manager/SystemOfflineManager.cs`

**Given** `mmria.server.util.SystemOfflineMessageFormatter` currently lives in server-only utility code
**When** this story is complete
**Then** the message-substitution logic has been moved into `SystemOfflineManager`; the `mmria.server.util.SystemOfflineMessageFormatter` class is deleted

**Given** `SystemOfflineManager` and `SystemOfflineDAL` are created
**When** registered in the server DI container
**Then** they follow the same registration pattern as other SharedLibraries features (e.g., `ManageUsersManager`, `CVSManager`)

**Given** `system_offlineController`'s public actions (`Index`, `GetConfig`, `GetJurisdictions`, `GetStatus`, `SaveConfig`) and the `/api/system-offline/status` route
**When** the refactor is complete
**Then** route paths, action signatures, HTTP method attributes, auth attributes (`[Authorize(Roles = ...)]`), and response JSON shapes are byte-for-byte identical to pre-refactor â€” no client-side changes required

**Given** the controller still needs tenant resolution (`host_prefix`, `configuration`, `ConfigDB`)
**When** the story is implemented
**Then** tenant resolution stays in the controller per project-context.md Â§2.2 first-pass rule â€” it is not moved into `SystemOfflineManager`

**Given** `GetJurisdictions()` filters `ConfigDB.detail_list.Keys` in the controller
**When** the refactor is complete
**Then** that key-list filtering stays in the controller â€” it is lightweight config-reading, not CouchDB access, and moving it would violate the first-pass rule

**Given** the refactor is complete
**When** `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` runs
**Then** the build succeeds with exit code 0

---

### Story 16.2: CaseWorkflowAdmin Wave 9 â€” Refactor clear_case_status and recover_deleted_case

As a developer maintaining the case administration workflow,
I want `clear_case_status.cs` and `recover_deleted_case.cs` to delegate their CouchDB work through a `CaseWorkflowAdmin` Manager and DAL,
So that these controllers follow the SharedLibraries pattern and the audit-write code added by Epic 7 is in the correct layer.

**Acceptance Criteria:**

**Given** `Controllers/clear_case_status.cs` calls `_couchDbHttpClient.ExecuteAsync(...)` directly at multiple points (case view query, case document GET, case document PUT) including the audit-write logic added by Story 7.2
**When** this story is complete
**Then** all CouchDB calls â€” including the audit-write â€” have been moved into `SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs`; the controller delegates via `SharedLibraries/CaseWorkflowAdmin/Manager/CaseWorkflowAdminManager.cs`

**Given** `Controllers/recover_deleted_case.cs` calls `_couchDbHttpClient.ExecuteAsync(...)` directly for deleted-case lookup, revision fetches, audit lookup, recovery PUT, audit cleanup DELETE, and the audit-write logic added by Story 7.2
**When** this story is complete
**Then** all CouchDB calls have been moved into `CaseWorkflowAdminDAL`; the controller delegates via `CaseWorkflowAdminManager`

**Given** the migration matrix rates both controllers as Wave 9 `planned` with High risk
**When** the refactor is implemented
**Then** the project-context.md Â§2.2 first-pass rules are followed exactly: tenant resolution (`host_prefix`, `configuration`, `db_config`, authorized-state resolution via `ResolveAuthorizedStateDatabase`) stays in the controller; business logic and CouchDB calls move to Manager/DAL; no outer `try/catch` blocks are added in Manager or DAL methods

**Given** `ConfigurationSet.detail_list` is accessed in the controllers
**When** the Manager or DAL needs a database URL
**Then** `db_info.Get_Prefix_DB_Url(path)` is used â€” never a hand-assembled URL; `detail_list` is always accessed via `TryGetValue` â€” never the direct indexer

**Given** `Change_Stack` audit writes were added by Stories 7.1 and 7.2 directly into the controller body
**When** this refactor is complete
**Then** those audit writes have been moved into `CaseWorkflowAdminManager` alongside the rest of the business logic

**Given** `clear_case_statusController` MVC view actions and `recover_deleted_caseController` MVC view actions
**When** the refactor is complete
**Then** route paths, action signatures, HTTP method attributes, view names, `ViewBag` keys, and response shapes are identical to pre-refactor

**Given** the refactor is complete
**When** `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` runs
**Then** the build succeeds with exit code 0

---

## Epic 18: `_users` and `configuration` Consolidation (SQL Migration Foundation)

All CouchDB reads and writes against the `_users` and `configuration` databases are consolidated behind `IUserRepository` and `IConfigurationRepository` interfaces in `mmria.common`. Scattered direct HTTP calls in controllers and manager files are replaced with repository method calls. After this epic, migrating these two databases to SQL requires changing only the two DAL implementations — no controller or manager changes are needed.

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness.

**Scope summary (verified 2026-07-14):**

| Database | Files with direct calls | Total hits | Already in DAL/Manager | Out-of-DAL leakage | Infra/out-of-scope |
|---|---|---|---|---|---|
| `_users` | 9 | 14 | `AccountDAL`, `AccountManager`, `ManageUsersDAL` | `AccountController.OIDC.cs`, `passwordChangeController.cs`, `JurisdictionSummary.cs`, `VROSummary.cs` | `c_db_setup.cs`, `Check_DB_Install.cs` |
| `configuration` | 3 | 12 | none | `_config.cs`, `MMRIAServicesDAL.cs` | `MultiTenantConfigurationLoader.cs` (startup) |

**SQL migration note:** `_users` is CouchDB's built-in authentication database. The `IUserRepository` interface established here is the seam for a future migration to ASP.NET Identity or an IAM system. `IConfigurationRepository` is the seam for a future SQL configuration table.

---

### Story 18.1: `_users` Operation Catalog

As a developer,
I want a definitive catalog of every operation against the `_users` database across all three projects,
So that Story 18.2 has an agreed-upon, complete operation set before any code changes begin.

**Acceptance Criteria:**

**Given** all `.cs` files in `mmria-server`, `mmria.common`, and `mmria.services`
**When** the developer completes the catalog
**Then** `docs/ai/mmrds_operation_catalog.md` gains a `_users` section listing every distinct operation grouped into: user GET by ID or name, user PUT/POST (create or update), user DELETE, user list queries, password-related queries, and role/group reads

**Given** each catalog entry
**When** the catalog is complete
**Then** each entry records: operation name, calling file(s), URL pattern in use, and response type expected

**Given** `c_db_setup.cs` and `Check_DB_Install.cs` references to `_users`
**When** they are evaluated
**Then** they are listed but marked **out of scope** — these are one-time setup and health-check operations, not application CRUD

---

### Story 18.2: Define `IUserRepository` and Canonicalize `AccountDAL`

As a developer,
I want a single `IUserRepository` interface over all `_users` CRUD operations,
So that every application-layer caller depends on the interface and not on CouchDB-specific URL construction.

**Acceptance Criteria:**

**Given** the existing `AccountDAL` in `mmria.common/SharedLibraries/Account/`
**When** this story is complete
**Then** `AccountDAL` contains all in-scope `_users` operations identified in Story 18.1, using consistent URL construction throughout — no manual URL string assembly remains

**Given** the full operation set is in `AccountDAL`
**When** the interface is extracted
**Then** `IUserRepository` is defined in `mmria.common/SharedLibraries/Account/` with async method signatures matching every `AccountDAL` method; `AccountDAL` implements `IUserRepository`

**Given** `ManageUsersDAL` also contains `_users` operations (5 hits)
**When** the developer evaluates them
**Then** operations that are generic user CRUD are moved to `AccountDAL` / `IUserRepository`; operations specific to the manage-users workflow that require `ManageUsers` feature context remain in `ManageUsersDAL` — the split is documented in the catalog

**Given** `IUserRepository` is defined
**When** DI registration is updated in `mmria-server`
**Then** `IUserRepository` is registered as `AccountDAL` in the server's service collection; all existing callers of the concrete `AccountDAL` compile without changes

---

### Story 18.3: Route Leaking `_users` Calls Through `IUserRepository`

As a developer,
I want all out-of-DAL `_users` calls in controllers and actor files to delegate to `IUserRepository`,
So that no file outside `AccountDAL` or `ManageUsersDAL` constructs a `_users` URL directly.

**Acceptance Criteria:**

**Given** the following direct `_users` HTTP calls outside of DAL files:
- `AccountController.OIDC.cs` — 2 hits (OIDC user lookup/provision during SAMS login)
- `passwordChangeController.cs` — 1 hit (user document GET/PUT for password change)
- `JurisdictionSummary.cs` — 1 hit (actor-side user lookup for jurisdiction summary)
- `VROSummary.cs` — 1 hit (actor-side user lookup for VRO summary)
**When** this story is complete
**Then** each is replaced with the corresponding `IUserRepository` method; `IUserRepository` is injected via constructor injection where needed

**Given** `AccountController.OIDC.cs` OIDC-specific user provisioning logic
**When** replaced
**Then** only the CouchDB URL construction is moved to `AccountDAL`; OIDC token handling, cookie management, and claims extraction remain in the controller

**Given** `JurisdictionSummary.cs` and `VROSummary.cs` (actor classes in `mmria-server/model/actor/`)
**When** they require `IUserRepository`
**Then** it is injected via the Akka.NET actor constructor or props factory — no `new AccountDAL(...)` instantiation inside the actor

**Given** the build after all changes
**When** verified
**Then** `mmria-server`, `mmria.common`, and `mmria.services` all build with zero errors

---

### Story 18.4: Define `IConfigurationRepository` and Create `SystemConfigDAL`

As a developer,
I want a single `IConfigurationRepository` interface over all `configuration` database CRUD,
So that the files currently accessing the configuration database directly can be replaced with interface calls.

**Acceptance Criteria:**

**Given** no existing SharedLibraries `SystemConfig` feature exists
**When** this story creates one
**Then** `mmria.common/SharedLibraries/SystemConfig/DAL/SystemConfigDAL.cs` is created containing all in-scope `configuration` database operations from the catalog; `IConfigurationRepository` is defined in the same feature directory; `SystemConfigDAL` implements `IConfigurationRepository`

**Given** the following direct `configuration` database accesses:
- `_config.cs` — 3 hits (admin configuration document GET/PUT)
- `MMRIAServicesDAL.cs` — 3 hits (configuration reads for service orchestration)
**When** this story is complete
**Then** each is replaced with the corresponding `IConfigurationRepository` method; `IConfigurationRepository` is injected via constructor injection

**Given** `MultiTenantConfigurationLoader.cs` (6 hits) reads the `configuration` database at startup to build the in-memory tenant map
**When** evaluated
**Then** it is marked **out of scope** — startup infrastructure loaders are not application CRUD and must not be behind an application repository interface; this is documented in the catalog

**Given** `IConfigurationRepository` is defined
**When** DI registration is updated in `mmria-server`
**Then** `IConfigurationRepository` is registered as `SystemConfigDAL` in the server's service collection

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors

---

### Story 18.5: Extract `IConfigurationBootstrapLoader` over `MultiTenantConfigurationLoader`

As a developer,
I want `MultiTenantConfigurationLoader` to be registered behind an interface in DI,
So that the startup tenant-registry and shared-config loading path can be swapped for a SQL implementation without editing `Program.cs`.

**Acceptance Criteria:**

**Given** `MultiTenantConfigurationLoader` is currently a concrete class instantiated directly in `Program.cs` with no interface
**When** this story is complete
**Then** `IConfigurationBootstrapLoader` is defined in `mmria.common/couchdb/configuration/` with async method signatures covering the public surface of `MultiTenantConfigurationLoader` used by `Program.cs`:
- `LoadRequiredConfigurationSetsAsync(...)`
- `LoadRequiredOverridableConfigurationsAsync(...)`
- `LoadTenantOverridableConfigurationAsync(...)`
- `LoadTenantConfigurationSetAsync(...)`
- Any other public methods called from `Program.cs` or startup paths

**Given** `IConfigurationBootstrapLoader` is defined
**When** `MultiTenantConfigurationLoader` is updated
**Then** it implements `IConfigurationBootstrapLoader`; no public method signatures change; all existing callers (`Program.cs`, `TestConfigurationLoader`, tests) compile without changes

**Given** `Program.cs` currently calls `new MultiTenantConfigurationLoader(appSettingsConfig)` directly
**When** this story is complete
**Then** `IConfigurationBootstrapLoader` is registered in the DI service collection as `MultiTenantConfigurationLoader`; `Program.cs` resolves it through the interface

**Given** `TestConfigurationLoader` in the utilities repo also instantiates `MultiTenantConfigurationLoader` concretely
**When** evaluated
**Then** it is updated to use `IConfigurationBootstrapLoader` if the DI context is available; if `TestConfigurationLoader` instantiates directly for test isolation, that is acceptable and documented — test helpers are not required to go through DI

**Given** the internal CouchDB URL construction inside `MultiTenantConfigurationLoader` (`$"{couchDbUrl}/configuration/{configId}"`)
**When** this story is complete
**Then** it remains in the concrete class — the interface exposes the public loading contract, not the URL construction mechanism; the SQL migration implementation will replace the concrete class, not the URL strings

**Given** the build after all changes
**When** verified
**Then** `mmria-server`, `mmria.common`, and `mmria.services` all build with zero errors; existing `MultiTenantConfigurationLoaderTests` pass without modification

---

## Epic 18 — Story Sequencing

| Wave | Story | Risk | Dependencies |
|---|---|---|---|
| 18 | 18.1 — `_users` Operation Catalog | None | None — discovery only |
| 18 | 18.2 — `IUserRepository` + `AccountDAL` | Low | 18.1 |
| 18 | 18.3 — Route leaking `_users` calls | Low–Medium | 18.2 |
| 18 | 18.4 — `IConfigurationRepository` + `SystemConfigDAL` | Low | 18.1 |
| 18 | 18.5 — `IConfigurationBootstrapLoader` over `MultiTenantConfigurationLoader` | Low | None — independent of all other stories |

18.3 and 18.4 can proceed in parallel once 18.2 is complete. 18.4 and 18.5 have no dependency on 18.2 and can be done at any time.

---

## Epic 19: `jurisdiction` Consolidation (SQL Migration Foundation)

All CouchDB reads and writes against the `jurisdiction` database are consolidated behind two distinct interfaces: `IJurisdictionRepository` for application CRUD, and `IJurisdictionAuthorizationReader` for the per-request authorization query. After this epic, migrating the jurisdiction database to SQL requires changing only the two DAL implementations.

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness.

**Scope summary (verified 2026-07-14):**

| Category | Files | Hits | Notes |
|---|---|---|---|
| Already in a DAL/Manager | `ManageUsersDAL`, `ManageUsersManager`, `SessionDAL` | 13 | Partial coverage — DAL methods exist but no interface |
| Application CRUD (out-of-DAL) | `jurisdiction_treeController`, `vitalsController`, `_usersController`, `CaseViewManager`, `CaseViewSearch.pmss`, `JurisdictionSummary`, `VROSummary` | 15 | Mix of controllers, managers, and actors |
| Auth middleware (hot path) | `authorization.cs`, `authorization_case.cs`, `authorization_user.cs`, `authorization.pmss.cs`, `authorization_case.pmss.cs`, `authorization_user.pmss.cs`, `AuthorizationRoleCache.cs`, `JurisdictionAuthorizationRequirement.cs` | 11 | **Special concern** — runs on every authorized request |
| Infra/out-of-scope | `c_db_setup.cs` | 5 | DB setup only |

**Two-interface design:** The auth middleware files all query a single read-only view (`jurisdiction/_design/sortable/_view/by_user_id`). This is architecturally different from application CRUD — it is a high-frequency, read-only authorization lookup. Mixing it with general CRUD behind one interface would create unacceptable coupling between the auth pipeline and the data layer. Two interfaces are required:

- **`IJurisdictionRepository`** — full CRUD for application features (manage users, session, jurisdiction tree, case view, vitals)
- **`IJurisdictionAuthorizationReader`** — single read method (`GetRolesByUserIdAsync`) used exclusively by auth middleware; intentionally narrow

---

### Story 19.1: `jurisdiction` Operation Catalog

As a developer,
I want a definitive catalog of every operation against the `jurisdiction` database,
So that Stories 19.2–19.4 have an agreed-upon, complete operation set before any code changes begin.

**Acceptance Criteria:**

**Given** all `.cs` files in `mmria-server`, `mmria.common`, and `mmria.services`
**When** the developer completes the catalog
**Then** `docs/ai/mmrds_operation_catalog.md` gains a `jurisdiction` section listing every distinct operation grouped into: user-role-jurisdiction document CRUD, jurisdiction tree document CRUD, vitals-related jurisdiction reads, session-related jurisdiction reads, authorization view queries, and bulk/admin operations

**Given** each catalog entry
**When** the catalog is complete
**Then** each entry records: operation name, calling file(s), URL pattern in use, response type, and whether it belongs to `IJurisdictionRepository` or `IJurisdictionAuthorizationReader`

**Given** `c_db_setup.cs` references to `jurisdiction`
**When** evaluated
**Then** they are listed but marked **out of scope**

---

### Story 19.2: Define `IJurisdictionRepository` and Create `JurisdictionDAL`

As a developer,
I want a single `IJurisdictionRepository` interface over all application-layer `jurisdiction` CRUD operations,
So that every feature manager depends on the interface and not on CouchDB URL construction.

**Acceptance Criteria:**

**Given** `ManageUsersDAL` currently owns `jurisdiction` CRUD (8 hits)
**When** this story is complete
**Then** a new `mmria.common/SharedLibraries/Jurisdiction/DAL/JurisdictionDAL.cs` is created containing all in-scope jurisdiction CRUD operations; `IJurisdictionRepository` is defined in the same `Jurisdiction` feature directory; `JurisdictionDAL` implements `IJurisdictionRepository`

**Given** `ManageUsersDAL` currently duplicates jurisdiction operations
**When** `JurisdictionDAL` is created
**Then** `ManageUsersDAL` is refactored to inject `IJurisdictionRepository` and delegate — it does not duplicate the implementation

**Given** jurisdiction operations belonging to other features (session, case view, jurisdiction tree)
**When** the interface is scoped
**Then** `IJurisdictionRepository` covers all jurisdiction document types — user-role-jurisdiction docs, jurisdiction tree, vitals-related reads — so that a single interface is the SQL migration seam for the whole database

**Given** `IJurisdictionRepository` is defined
**When** DI registration is updated in `mmria-server`
**Then** `IJurisdictionRepository` is registered as `JurisdictionDAL` in the server's service collection

---

### Story 19.3: Define `IJurisdictionAuthorizationReader` and Route Auth Middleware

As a developer,
I want the per-request authorization view query against `jurisdiction` to be behind a dedicated read-only interface,
So that the auth middleware does not construct CouchDB URLs directly and the query can be swapped for a SQL implementation without touching authorization handler code.

**Acceptance Criteria:**

**Given** all six `authorization*.cs` files and `AuthorizationRoleCache.cs` query the same view: `jurisdiction/_design/sortable/_view/by_user_id`
**When** this story is complete
**Then** `IJurisdictionAuthorizationReader` is defined in `mmria.common/SharedLibraries/Jurisdiction/` with a single method: `Task<IReadOnlyList<JurisdictionRoleEntry>> GetRolesByUserIdAsync(string userId, DBConfigurationDetail dbConfig)` and a separate `JurisdictionAuthorizationDAL` implements it

**Given** `JurisdictionAuthorizationDAL` is created
**When** it is implemented
**Then** it is a separate class from `JurisdictionDAL` — the auth read path is not mixed with application CRUD

**Given** the six `authorization*.cs` handler files currently construct the URL directly
**When** this story is complete
**Then** each injects `IJurisdictionAuthorizationReader` and calls `GetRolesByUserIdAsync`; URL construction is removed from all six files

**Given** `AuthorizationRoleCache.cs` wraps the query with in-memory caching
**When** this story is complete
**Then** `AuthorizationRoleCache` injects `IJurisdictionAuthorizationReader`; cache management remains in `AuthorizationRoleCache` — not in the DAL

**Given** the PMSS split files (`authorization.pmss.cs`, `authorization_case.pmss.cs`, `authorization_user.pmss.cs`)
**When** they are updated
**Then** they follow the same pattern as their non-PMSS counterparts; no PMSS-specific divergence is introduced

**Given** this is the hot path for every authorized request
**When** the implementation is reviewed
**Then** `JurisdictionAuthorizationDAL.GetRolesByUserIdAsync` is a thin, non-caching HTTP wrapper — no business logic, no side effects

**Given** `IJurisdictionAuthorizationReader` is registered in DI
**When** the server's service collection is updated
**Then** it is registered as `JurisdictionAuthorizationDAL` and is scoped appropriately for the authorization pipeline

---

### Story 19.4: Route Out-of-DAL Application CRUD Through `IJurisdictionRepository`

As a developer,
I want all application-layer files that directly construct `jurisdiction` URLs outside of a DAL to delegate to `IJurisdictionRepository`,
So that the interface established in Story 19.2 is the only path for application jurisdiction CRUD.

**Acceptance Criteria:**

**Given** the following direct `jurisdiction` HTTP calls outside of DAL files:
- `jurisdiction_treeController.cs` — 5 hits (tree document GET/PUT — Wave 8 planned migration target)
- `vitalsController.cs` — 4 hits (jurisdiction reads for vitals context)
- `_usersController.cs` — 2 hits (user-role-jurisdiction reads)
- `CaseViewManager.cs` — 5 hits (jurisdiction reads for case view filtering)
- `CaseViewSearch.pmss.cs` — 1 hit (PMSS variant of case view search)
- `JurisdictionSummary.cs` — 1 hit (actor-side jurisdiction read)
- `VROSummary.cs` — 1 hit (actor-side jurisdiction read)
- `SessionDAL.cs` — 1 hit (session-related jurisdiction read)
- `ManageUsersManager.cs` — 4 hits (any remaining direct construction after Story 19.2)
**When** this story is complete
**Then** each is replaced with the corresponding `IJurisdictionRepository` method; `IJurisdictionRepository` is injected via constructor injection in each class

**Given** `jurisdiction_treeController.cs` is also a Wave 8 migration target (planned move to `JurisdictionTree` SharedLibrary)
**When** this story touches it
**Then** only the URL construction is replaced — the Wave 8 SharedLibraries extraction is deferred; this story does not restructure the controller's business logic

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors and no route, action signature, or response shape changes are made

---

## Epic 19 — Story Sequencing

| Wave | Story | Risk | Dependencies |
|---|---|---|---|
| 19 | 19.1 — `jurisdiction` Operation Catalog | None | None — discovery only |
| 19 | 19.2 — `IJurisdictionRepository` + `JurisdictionDAL` | Low–Medium | 19.1 |
| 19 | 19.3 — `IJurisdictionAuthorizationReader` (auth middleware) | Medium | 19.1 |
| 19 | 19.4 — Route out-of-DAL application CRUD | Low–Medium | 19.2 |

19.2 and 19.3 can proceed in parallel after 19.1. 19.4 depends on 19.2. Story 19.3 is independent of 19.2 — the auth reader DAL and the CRUD DAL are separate classes.

---

## Epic 20: `metadata` Consolidation (SQL Migration Foundation)

All CouchDB reads and writes against the `metadata` database are consolidated behind an `IMetadataRepository` interface in `mmria.common`. The existing `MetadataVersionDAL` is the canonical implementation; the 25 files that currently bypass it are routed through the interface. After this epic, migrating the metadata database to SQL requires changing only `MetadataVersionDAL`.

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness.

**Scope summary (verified 2026-07-14):**

| Category | Files | Hits | Notes |
|---|---|---|---|
| `MetadataVersionManager` (already DAL-backed) | `MetadataVersionManager.cs` | 22 | Canonical owner — but builds URLs directly in manager, not all through DAL |
| Controllers bypassing DAL | `broadcast_messageController`, `de_identified_listController`, `export_list_managerController`, `substance_mappingController`, `abstractorDeidentifiedCaseController`, `CaseController`, `versionController`, `record_idController`, `systemOfflineController` | ~14 | Mix of planned and unplanned Wave targets |
| SharedLibraries bypassing DAL | `AuditRecoveryDAL`, `CaseValidationDAL`, `MMRIAServicesDAL` | ~8 | Within common — still bypass the canonical DAL |
| Services actors/exporters | `c_convert_to_report_object`, `c_convert_to_opioid_report_object`, `c_convert_to_dqr_detail`, `c_de_identifier`, `c_cdc_de_identifier`, `c_document_sync_all`, `c_document_sync_all_legacy`, `c_generate_frequency_summary_report`, `c_sync_document`, `BatchItemProcessingService`, `core_element_exporter`, `exporter`, `mmrds_exporter`, `export_all_generate_name_map`, `PopulateCDCInstanceSupervisor` | ~39 | Mostly read-only: `GET version_specification-{v}/metadata` and `GET de-identified-list` |
| Infra/out-of-scope | `c_db_setup.cs`, `Process_Migrate_*` | ~15 | DB setup and one-time migration scripts |

**Key observation:** The services layer makes two operations overwhelmingly — `GET metadata/version_specification-{version}/metadata` and `GET metadata/de-identified-list` — accounting for the majority of the 39 services hits and all read-only.

---

### Story 20.1: `metadata` Operation Catalog

As a developer,
I want a definitive catalog of every operation against the `metadata` database,
So that Stories 20.2–20.6 have an agreed-upon, complete operation set before any code changes begin.

**Acceptance Criteria:**

**Given** all `.cs` files in `mmria-server`, `mmria.common`, and `mmria.services`
**When** the developer completes the catalog
**Then** `docs/ai/mmrds_operation_catalog.md` gains a `metadata` section listing every distinct operation grouped into: version specification CRUD, de-identification list reads, metadata document GET/PUT (by ID), UI specification CRUD, attachment reads/writes, broadcast/offline/populate-CDC config document CRUD, export list and substance mapping CRUD, and bulk reads (`_all_docs`)

**Given** each catalog entry
**When** the catalog is complete
**Then** each entry records: operation name, calling file(s), URL pattern in use, and response type expected

**Given** `c_db_setup.cs` and `Process_Migrate_*` references
**When** evaluated
**Then** they are listed but marked **out of scope** — DB setup and one-time migration scripts are not application CRUD

---

### Story 20.2: Define `IMetadataRepository` and Canonicalize `MetadataVersionDAL`

As a developer,
I want a single `IMetadataRepository` interface over all `metadata` database operations,
So that every caller depends on the interface and not on CouchDB URL construction.

**Acceptance Criteria:**

**Given** the existing `MetadataVersionDAL` in `mmria.common/SharedLibraries/MetadataVersion/`
**When** this story is complete
**Then** `MetadataVersionDAL` contains all in-scope `metadata` operations from the catalog using consistent URL construction throughout — no Pattern A strings remain

**Given** `MetadataVersionManager.cs` currently builds 22 `metadata` URLs directly instead of routing all through `MetadataVersionDAL`
**When** this story is complete
**Then** every `metadata` URL in `MetadataVersionManager` is replaced with a `MetadataVersionDAL` method call; the manager does not construct CouchDB URLs directly

**Given** the full operation set is in `MetadataVersionDAL`
**When** the interface is extracted
**Then** `IMetadataRepository` is defined in `mmria.common/SharedLibraries/MetadataVersion/` with async method signatures matching every `MetadataVersionDAL` method; `MetadataVersionDAL` implements `IMetadataRepository`

**Given** `IMetadataRepository` is defined
**When** DI registration is updated in `mmria-server`
**Then** `IMetadataRepository` is registered as `MetadataVersionDAL`; all existing callers of the concrete `MetadataVersionDAL` compile without changes

---

### Story 20.3: Route SharedLibraries DAL Files Through `IMetadataRepository`

As a developer,
I want the SharedLibraries DAL files that directly access the `metadata` database to delegate to `IMetadataRepository`,
So that no DAL file outside of `MetadataVersionDAL` constructs a `metadata` URL.

**Acceptance Criteria:**

**Given** the following direct `metadata` HTTP calls in SharedLibraries DAL files:
- `AuditRecoveryDAL.cs` — 1 hit (`GET metadata/version_specification-{v}/metadata`)
- `CaseValidationDAL.cs` — 2 hits (metadata document GET/PUT for case validation)
- `MMRIAServicesDAL.cs` — 3 hits (de-id export list and populate-CDC config reads)
**When** this story is complete
**Then** each is replaced with the corresponding `IMetadataRepository` method; `IMetadataRepository` is injected into each DAL via constructor injection

**Given** `MMRIAServicesDAL` handles cross-tenant and CDC-scoped metadata reads
**When** these are replaced
**Then** the tenant/CDC connection context (`DBConfigurationDetail`) is passed through to the repository method — no implicit global state is introduced

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors

---

### Story 20.4: Route Controller Direct `metadata` Calls Through `IMetadataRepository`

As a developer,
I want controllers that directly access the `metadata` database to delegate to `IMetadataRepository` or the existing `MetadataVersionManager`,
So that controllers contain no `metadata` URL construction.

**Acceptance Criteria:**

**Given** the following controllers with direct `metadata` URL construction:
- `broadcast_messageController.cs` — 3 hits (broadcast-message-list GET/PUT — Wave 9 planned migration target)
- `de_identified_listController.cs` — 2 hits (de-id and de-id-export list GET/PUT — Wave 8 planned target)
- `export_list_managerController.cs` — 2 hits (export-standard-list GET/PUT)
- `substance_mappingController.cs` — 2 hits (substance-mapping GET/PUT)
- `abstractorDeidentifiedCaseController.cs` — 1 hit (duplicate-multiform-list GET)
- `CaseController.cs` — 1 hit (duplicate-multiform-list GET)
- `versionController.cs` — 1 hit (metadata document GET by ID)
- `record_idController.cs` — 1 hit (record ID document GET)
- `systemOfflineController.cs` — 1 hit (system-offline-config URL builder)
**When** this story is complete
**Then** each is replaced with the corresponding `IMetadataRepository` or `MetadataVersionManager` method call; `IMetadataRepository` is injected where no manager intermediary already exists

**Given** `broadcast_messageController` and `de_identified_listController` are also Wave 8/9 SharedLibraries migration targets
**When** this story touches them
**Then** only the URL construction is replaced; the Wave 8/9 manager extraction is deferred — this story does not restructure controller business logic

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors and no route, action signature, or response shape changes are made

---

### Story 20.5: Route `mmria.services` Read-Only `metadata` Calls Through `IMetadataRepository`

As a developer,
I want the background-job and exporter code in `mmria.services` that reads `metadata` documents to delegate to `IMetadataRepository`,
So that the services project is covered by the same interface contract as the server.

**Acceptance Criteria:**

**Given** the two dominant read operations in `mmria.services`:
- `GET metadata/version_specification-{version}/metadata` — in `c_convert_to_report_object`, `c_convert_to_opioid_report_object`, `c_convert_to_dqr_detail`, `c_de_identifier`, `c_cdc_de_identifier`, `c_document_sync_all`, `c_document_sync_all_legacy`, `c_generate_frequency_summary_report`, `c_sync_document`, `BatchItemProcessingService`, `core_element_exporter`, `exporter`, `mmrds_exporter`, `export_all_generate_name_map`
- `GET metadata/de-identified-list` and `GET metadata/de-identified-export-list` — in `c_de_identifier`, `c_cdc_de_identifier`, `c_document_sync_all`, `c_document_sync_all_legacy`, `c_sync_document`, `core_element_exporter`
**When** this story is complete
**Then** each is replaced with the corresponding `IMetadataRepository` method; since `mmria.services` already references `mmria.common`, no new project reference is needed

**Given** the remaining services files with direct `metadata` access:
- `PopulateCDCInstanceSupervisor.cs` — 2 hits (populate-CDC-instance config document)
**When** evaluated
**Then** these are replaced using the same `IMetadataRepository` method as `MMRIAServicesDAL`

**Given** `c_document_sync_all` and `c_document_sync_all_legacy` use `metadata` reads as part of sync orchestration
**When** replaced
**Then** only the URL construction is replaced — sync orchestration logic remains in the actor classes

**Given** the build after all changes
**When** verified
**Then** `mmria.services`, `mmria.common`, and `mmria-server` all build with zero errors

---

### Story 20.6: `metadata` Boundary Decision — Bulk `_all_docs` and Sync

As a developer,
I want a written architecture decision on whether bulk `metadata/_all_docs` reads and sync-driven metadata access belong behind `IMetadataRepository` or are separate infrastructure concerns,
So that the boundary is explicit and consistent with the decision made for `mmrds` in Story 17.7.

**Acceptance Criteria:**

**Given** `MetadataVersionManager` uses `GET metadata/_all_docs?include_docs=true` in two places for loading the full version list
**When** the developer evaluates these
**Then** a decision is recorded in `docs/ai/mmrds_operation_catalog.md` under the `metadata` Boundary Decisions section: either (a) add `GetAllMetadataDocumentsAsync` to `IMetadataRepository` or (b) keep these as manager-level reads not in the interface

**Given** the recommendation from Story 17.7 treated sync `_all_docs` as out-of-scope infrastructure
**When** the same question is evaluated for `metadata`
**Then** the decision is consistent with Story 17.7 — bulk reads for version list enumeration are part of the application interface (`IMetadataRepository`) since `MetadataVersionManager` already owns them; sync-driven reads in `c_document_sync_all` remain infrastructure

---

## Epic 20 — Story Sequencing

| Wave | Story | Risk | Dependencies |
|---|---|---|---|
| 20 | 20.1 — `metadata` Operation Catalog | None | None — discovery only |
| 20 | 20.6 — Boundary Decision | None | Can run in parallel with 20.1 |
| 20 | 20.2 — `IMetadataRepository` + `MetadataVersionDAL` | Low–Medium | 20.1 |
| 20 | 20.3 — SharedLibraries DAL files | Low | 20.2 |
| 20 | 20.4 — Controller direct calls | Low–Medium | 20.2 |
| 20 | 20.5 — `mmria.services` read-only calls | Medium | 20.2 |

20.3, 20.4, 20.5, and 20.6 can proceed in parallel once 20.2 is complete.

---

## Epic 21: `audit` Consolidation (SQL Migration Foundation)

All CouchDB reads and writes against the `audit` database are consolidated behind a single `IAuditRepository` interface implemented by a new canonical `AuditDAL`. The 19 in-scope call sites currently scattered across controllers, managers, and DAL files are routed through the interface. After this epic, migrating the audit database to SQL requires changing only `AuditDAL` — no manager, controller, or workflow-admin code changes are needed.

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness.

**Scope summary (verified 2026-07-15):**

| Location | # Calls | Layer | URL Pattern | Notes |
|---|---|---|---|---|
| `AuditRecoveryDAL.cs` | 3 | DAL ✓ | **A** (wrong) | GET by ID, GET audit-manage-user, PUT audit-manage-user |
| `CaseWorkflowAdminDAL.cs` | 4 | DAL ✓ | **B** (correct) | WriteAuditEntry, GetDeletedCasesView, GetAuditDoc, DeleteAuditDoc |
| `ManageUsersDAL.cs` | 1 | DAL ✓ | **A** (wrong) | GET audit-manage-user (duplicate of AuditRecoveryDAL) |
| `CaseManager.cs` | 6 | **Manager ✗** | **B** (correct) | All audit PUT (Change_Stack writes) — wrong layer |
| `AuditRecoveryManager.cs` | 1 | **Manager ✗** | **A** (wrong) | Builds `_find` URL directly in manager |
| `_auditController.cs` | 1 | **Controller ✗** | **A** (wrong) | `_find` by case_id — wrong layer |
| `AuditRecoverUtilController.cs` | 1 | **Controller ✗** | **A** (wrong) | `_find` — wrong layer |
| `caseController.pmss.cs` | 2 | **Controller ✗** | **B** (correct) | Audit PUT (Change_Stack writes) — wrong layer |
| `c_db_setup.cs` | 5 | Infra | — | DB setup/security — **out of scope** |

**Total in-scope: 19 calls.** 8 are already in the DAL layer but behind no interface. 11 are leaking out of the DAL.

**Design decision — AuditDAL vs. AuditRecoveryDAL:**
The existing `AuditRecoveryDAL` is scoped to one workflow (audit recovery / manage-user). A new canonical `SharedLibraries/Audit/DAL/AuditDAL.cs` is the correct home for all audit CRUD operations. After this epic, `AuditRecoveryDAL` becomes a workflow-specific DAL that delegates its audit reads/writes to `IAuditRepository` and keeps only recovery-specific orchestration.

**Sequencing constraint:** Story 21.4 modifies `CaseWorkflowAdminDAL.cs`. Epic 17 Story 17.4 also modifies the same file. **21.4 must run after Epic 17 is complete** to avoid conflict on that file.

---

### Story 21.1: `audit` Operation Catalog

As a developer,
I want a definitive catalog of every operation against the `audit` database across all three projects,
So that Story 21.2 has an agreed-upon, complete operation set before any code changes begin.

**Acceptance Criteria:**

**Given** all `.cs` files in `mmria-server`, `mmria.common`, and `mmria.services`
**When** the developer completes the catalog
**Then** `docs/ai/mmrds_operation_catalog.md` gains an `audit` section listing every distinct operation grouped into: audit entry writes (PUT `Change_Stack`), audit entry reads (GET by ID), audit view queries (`by_deleted`), Mango `_find` queries (`by case_id`), special document reads/writes (`audit-manage-user`), and bulk/delete operations

**Given** each catalog entry
**When** the catalog is complete
**Then** each entry records: operation name, calling file(s) with line number, URL pattern in use (A or B), and response type expected

**Given** `c_db_setup.cs` references to `audit`
**When** evaluated
**Then** they are listed but marked **out of scope** — DB setup and security configuration are infrastructure operations

---

### Story 21.2: Create `AuditDAL` and Extract `IAuditRepository`

As a developer,
I want a single `IAuditRepository` interface over all audit CRUD operations,
So that every caller can depend on the interface and a SQL migration requires changing only `AuditDAL`.

**Acceptance Criteria:**

**Given** no canonical `Audit` SharedLibraries feature exists
**When** this story creates one
**Then** the following structure exists:
```
mmria.common/SharedLibraries/Audit/
  IAuditRepository.cs
  DAL/
    AuditDAL.cs
```

**Given** the operation catalog from Story 21.1
**When** `AuditDAL` is created
**Then** it contains async methods for every in-scope operation, including at minimum:
- `WriteAuditEntryAsync(Change_Stack entry, DBConfigurationDetail dbConfig)`
- `GetAuditEntryAsync(string auditId, DBConfigurationDetail dbConfig)` → `Change_Stack`
- `DeleteAuditEntryAsync(string auditId, string rev, DBConfigurationDetail dbConfig)`
- `GetDeletedCasesViewAsync(DBConfigurationDetail dbConfig)` → `get_sortable_view_reponse_header<Audit_Detail_View>`
- `GetAuditManageUserAsync(DBConfigurationDetail dbConfig)` → `Audit_Manage_User?`
- `SaveAuditManageUserAsync(Audit_Manage_User doc, DBConfigurationDetail dbConfig)`
- `FindAuditsByCaseAsync(string caseId, DBConfigurationDetail dbConfig)` → `ChangeStackResult`

**Given** all `AuditDAL` methods
**When** written
**Then** all use `dbConfig.Get_Prefix_DB_Url(...)` (Pattern B) — no `$"{dbConfig.url}/{dbConfig.prefix}audit/..."` string interpolations

**Given** `IAuditRepository` is defined
**When** DI registration is updated in `mmria-server/Program.cs`
**Then** `services.AddScoped<IAuditRepository, AuditDAL>()` is present

**Given** no callers are changed in this story
**When** the build runs
**Then** `mmria-server`, `mmria.common`, and `mmria.services` build with zero errors

---

### Story 21.3: Route CaseManager Audit Writes Through `IAuditRepository`

As a developer,
I want `CaseManager`'s 6 direct audit write calls to delegate to `IAuditRepository`,
So that audit access in the case manager layer follows the Manager → DAL boundary.

**Acceptance Criteria:**

**Given** the following 6 direct audit PUT calls in `CaseManager.cs` (all using `Get_Prefix_DB_Url`, Pattern B):
- Line 318: `auditDbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}")`
- Line 537: `auditDbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}")`
- Line 1180: `dbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}")`
- Line 1330: `dbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}")`
- Line 1831: `dbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}")`
- Line 2330: `dbConfig.Get_Prefix_DB_Url($"audit/{audit_data._id}")`
**When** this story is complete
**Then** each is replaced with `IAuditRepository.WriteAuditEntryAsync(changeStack, dbConfig)`; `IAuditRepository` is injected into `CaseManager` via constructor injection

**Given** `CaseManager` will now depend on both `ICaseRepository` and `IAuditRepository`
**When** DI registration is updated
**Then** both dependencies are registered and `CaseManager` resolves correctly

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors and no controller code changes are required

---

### Story 21.4: Route CaseWorkflowAdminDAL Audit Calls Through `IAuditRepository`

As a developer,
I want `CaseWorkflowAdminDAL`'s 4 direct audit calls to delegate to `IAuditRepository`,
So that the workflow-admin DAL no longer constructs audit URLs directly.

**Acceptance Criteria:**

**Given** the following 4 audit calls in `CaseWorkflowAdminDAL.cs` (all Pattern B):
- Line 49: `WriteAuditEntryAsync` — PUT `audit/{auditEntry._id}`
- Line 57: `GetDeletedCasesViewAsync` — GET `audit/_design/sortable/_view/by_deleted`
- Line 67: `GetAuditDocumentAsync` — GET `audit/{auditId}`
- Line 92: `DeleteAuditDocumentAsync` — DELETE `audit/{auditId}?rev={rev}`
**When** this story is complete
**Then** each is replaced with the corresponding `IAuditRepository` method; `IAuditRepository` is injected into `CaseWorkflowAdminDAL` via constructor injection

**Given** `CaseWorkflowAdminDAL` after Epic 17 Story 17.4 already delegates mmrds calls to `ICaseRepository`
**When** this story is implemented
**Then** `CaseWorkflowAdminDAL` depends on both `ICaseRepository` and `IAuditRepository`; `_couchDbHttpClient` is removed from the class entirely (all its calls will have been moved to repository dependencies)

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors

**Pre-condition:** This story must not be started until Epic 17 Story 17.4 is `done`.

---

### Story 21.5: Route Controller-Level Audit Calls Through `IAuditRepository`

As a developer,
I want all direct audit URL construction in controllers eliminated,
So that controllers never touch the audit database directly.

**Acceptance Criteria:**

**Given** the following direct audit calls in controller/util files:
- `_auditController.cs` line 107: `$"{db_config.url}/{db_config.prefix}audit/_find"` — builds `_find` URL in a private helper method, passes it to `AuditRecoveryManager`
- `AuditRecoverUtilController.cs` line 54: `$"{configuration.url}/{configuration.prefix}audit/_find"` — `_find` URL passed to a service
- `caseController.pmss.cs` line 261: `db_config.Get_Prefix_DB_Url($"audit/{audit_data._id}")` — audit PUT
- `caseController.pmss.cs` line 418: `db_config.Get_Prefix_DB_Url($"audit/{audit_data._id}")` — audit PUT
**When** this story is complete
**Then** all four call sites are replaced with `IAuditRepository` method calls; `IAuditRepository` is injected into each controller via constructor injection; no controller constructs an `audit/` URL

**Given** `_auditController.cs` `get_find_url()` helper method (line ~90–110) that currently builds the `_find` URL tuple `(url, postData)` and passes both to `AuditRecoveryManager.GetAuditViewDataAsync`
**When** replaced
**Then** the URL construction is removed from the controller; `FindAuditsByCaseAsync` in `IAuditRepository` accepts the `caseId` directly and handles the `_find` POST internally; the manager receives the result, not the URL

**Given** `caseController.pmss.cs` audit writes at lines 261 and 418
**When** replaced
**Then** only the CouchDB URL construction and `ExecuteAsync` calls move to the DAL — the surrounding PMSS business logic and error handling remain in the controller

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors

---

### Story 21.6: Route `ManageUsersDAL` and `AuditRecoveryDAL` Through `IAuditRepository`

As a developer,
I want `ManageUsersDAL` and `AuditRecoveryDAL` to delegate their audit operations to `IAuditRepository`,
So that no DAL outside `AuditDAL` constructs audit URLs directly.

**Acceptance Criteria:**

**Given** `ManageUsersDAL.cs` line 165 — `$"{db_config.url}/{db_config.prefix}audit/audit-manage-user"` (GET `Audit_Manage_User`, Pattern A)
**When** this story is complete
**Then** the call is replaced with `IAuditRepository.GetAuditManageUserAsync(db_config)`; `IAuditRepository` is injected into `ManageUsersDAL`

**Given** `AuditRecoveryDAL.cs` lines 39, 53, 70 (all Pattern A):
- Line 39: GET `audit/{changeId}` → `Change_Stack`
- Line 53: GET `audit/audit-manage-user` → `Audit_Manage_User`
- Line 70: PUT `audit/{auditDocument._id}` → `document_put_response`
**When** this story is complete
**Then** all three are replaced with the corresponding `IAuditRepository` methods; `AuditRecoveryDAL` injects `IAuditRepository` instead of calling `_couchDbHttpClient` for audit operations

**Given** `AuditRecoveryManager.cs` line 158 — builds `_find` URL directly as `$"{db_config.url}/{db_config.prefix}audit/_find"` and returns it as a tuple to be passed back to the DAL
**When** this story is complete
**Then** the `_find` URL construction is removed from `AuditRecoveryManager`; the manager calls `IAuditRepository.FindAuditsByCaseAsync(caseId, dbConfig)` directly and receives the result; `IAuditRepository` is injected into `AuditRecoveryManager`

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors; `AuditRecoveryDAL` no longer holds any direct `_couchDbHttpClient` audit calls (its `_couchDbHttpClient` field may be removed entirely if no other calls remain)

---

## Epic 21 — Story Sequencing

| Wave | Story | Risk | Dependencies |
|---|---|---|---|
| 21 | 21.1 — `audit` Operation Catalog | None | None — discovery only |
| 21 | 21.2 — `AuditDAL` + `IAuditRepository` | Low | 21.1 |
| 21 | 21.3 — CaseManager audit writes | Low | 21.2 |
| 21 | 21.5 — Controller-level audit calls | Low–Medium | 21.2 |
| 21 | 21.6 — ManageUsersDAL + AuditRecoveryDAL + AuditRecoveryManager | Low | 21.2 |
| 21 | 21.4 — CaseWorkflowAdminDAL audit calls | Low | 21.2 **+ Epic 17 Story 17.4 done** |

21.3, 21.5, and 21.6 can proceed in parallel once 21.2 is complete. 21.4 must wait for Epic 17 Story 17.4 to avoid file conflict on `CaseWorkflowAdminDAL.cs`.

---

## Epic 22: .NET 10 Upgrade

All mmria projects are upgraded from .NET 9 to .NET 10. The developer machine receives the .NET 10 SDK, all project target frameworks are updated, third-party NuGet packages are verified for .NET 10 compatibility (with any necessary version bumps applied), and both production Dockerfiles are updated to reference the .NET 10 trusted base images from the EcPaaS registry.

**Projects in scope (nccdphp-drh-mmria repo):**
- `source-code/mmria/mmria-server/mmria-server.csproj`
- `nccdphp-drh-mmria-common/mmria.common/mmria.common.csproj`
- `nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj`

**Projects in scope (nccdphp-drh-mmria-utilities repo):**
- `mmria-server.tests/mmria-server.tests.csproj`
- `mmria-case-generator/mmria-case-generator.csproj`
- `strongly-typed-case/strongcase.csproj`
- `data-migration/migrate.csproj`
- `Replication/replicate.csproj`
- `mmria-ije-generator/mmria-ije-generator.csproj`
- `mmria-tools/mmria-tools.csproj`
- `mmria-tenant-database-counts/mmria-tenant-database-counts.csproj`

**Dockerfiles in scope:**
- `source-code/mmria/mmria-server/Dockerfile` — build image `dotnet-90`, runtime image `dotnet-90-runtime`
- `nccdphp-drh-mmria-services/mmria.services/Dockerfile` — same images
- `.s2i/dockerfile` — legacy file, currently references `dotnet-80`; assess whether to update or retire

---

### Story 22.1: .NET 10 Compatibility Analysis and Risk Assessment

As a developer,
I want a documented analysis of all compatibility risks before upgrading to .NET 10,
So that the upgrade execution story has a clear, evidence-based remediation plan and no surprises block CI/CD.

**Acceptance Criteria:**

**Given** the Microsoft .NET 10 breaking-changes documentation
**When** the developer reviews it against the mmria codebase
**Then** a written findings report is produced listing every breaking change that applies (or is suspected to apply) to this codebase, its severity (High / Medium / Low / None), and the affected file(s)

**Given** the key third-party NuGet packages used across the in-scope projects:

| Package | Current Version | Risk Notes |
|---|---|---|
| `Akka` / `Akka.Hosting` / `Akka.Cluster` / `Akka.DependencyInjection` | 1.5.52 | Check NuGet for .NET 10 TFM support |
| `Akka.Quartz.Actor` | 1.5.13 | Transitively depends on Quartz 3.x; verify compatibility |
| `Akka.DI.Core` / `Akka.DI.Extensions.DependencyInjection` | 1.4.51 / 1.4.22 | Older release train; may not declare net10.0 support |
| `Quartz` | 3.13.1 | Check for .NET 10 support |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | 9.0.0 | Must be updated to 10.0.x |
| `Microsoft.Extensions.Http` | 9.0.0 | Must be updated to 10.0.x |
| `Serilog.Extensions.Logging` | 9.0.0 | Check for 10.0.x release |
| `System.Text.Encoding.CodePages` | 9.0.0 | Likely in-box for .NET 10; confirm |
| `Microsoft.CodeAnalysis.CSharp` | 4.12.0 | Verify .NET 10 compiler support |
| `NJsonSchema` / `NJsonSchema.CodeGeneration.CSharp` | 11.0.2 | Check for compatibility |
| `FastExcel` | 3.0.13 | Low risk (no framework coupling) |
| `SharpZipLib` | 1.4.2 | Low risk |
| `TinyCsvParser` | 2.7.1 | Low risk |
| `Newtonsoft.Json` | 13.0.3 | Low risk (framework-agnostic) |

**When** the developer checks each package on NuGet.org for .NET 10 TFM listings, open issues, and release notes
**Then** the findings report records the latest compatible version for each package (or "no upgrade needed" if the current version is compatible) and flags any packages with no .NET 10 support path as blockers

**Given** the EcPaaS trusted-image registry currently has `dotnet-90` and `dotnet-90-runtime` images
**When** the developer contacts the EcPaaS platform team or inspects the registry
**Then** the findings report records whether `dotnet-100` and `dotnet-100-runtime` images exist in the registry, their tag/digest format, and (if absent) the estimated availability timeline and any interim workaround (e.g., use `mcr.microsoft.com/dotnet/aspnet:10.0` with a waiver)

**Given** the suppressed compiler warnings in `mmria-server.csproj` (`SYSLIB0014`, `CS8632`, etc.)
**When** the developer reviews them against .NET 10 release notes
**Then** the report notes whether any suppressed warning escalates to an error in .NET 10 and recommends the remediation action (fix the call site or retain the suppression)

**Given** the test suite in `mmria-server.tests`
**When** the developer reviews the test project's dependencies and test patterns
**Then** the report notes any test-framework or assertion-library changes needed for .NET 10

**Deliverable:** A markdown findings report committed to `docs/ai/dotnet10-compatibility-analysis.md` covering:
1. Breaking-change audit results
2. Per-package compatibility status table with recommended versions
3. Docker image availability status and path forward
4. Suppressed-warning review
5. Recommended story 22.2 task checklist derived from the above findings

---

### Story 22.2: .NET 10 Upgrade Execution

As a developer,
I want all mmria projects running on .NET 10,
So that the codebase is on the current LTS release with continued Microsoft support and access to .NET 10 platform improvements.

**Pre-condition:** Story 22.1 is complete and the findings report in `docs/ai/dotnet10-compatibility-analysis.md` shows no unresolved blocker items. Any package with no .NET 10 support path must be resolved before this story begins.

**Acceptance Criteria:**

**Given** the developer machine does not yet have the .NET 10 SDK installed
**When** the developer runs the upgrade
**Then** the .NET 10 SDK is installed via `winget install Microsoft.DotNet.SDK.10` (or the equivalent official installer) and `dotnet --list-sdks` confirms the `10.x` SDK is present alongside the existing 9.x SDK

**Given** all eleven in-scope `.csproj` files currently declare `<TargetFramework>net9.0</TargetFramework>` (or `<TargetFrameworks>net9.0</TargetFrameworks>` for mmria-server)
**When** the developer updates them
**Then** every in-scope `.csproj` declares `net10.0` and `dotnet build` succeeds with no new errors for each project

**Given** the version-locked Microsoft packages (`Microsoft.AspNetCore.Mvc.NewtonsoftJson 9.0.0`, `Microsoft.Extensions.Http 9.0.0`, `System.Text.Encoding.CodePages 9.0.0`, `Serilog.Extensions.Logging 9.0.0`)
**When** the developer updates packages
**Then** each is updated to its `.NET 10`-aligned version (10.0.x or latest stable that lists `net10.0` support) and the projects restore without errors

**Given** the compatibility analysis report's per-package recommended versions
**When** remaining packages require version bumps (e.g., Akka, Quartz, NJsonSchema as identified in Story 22.1)
**Then** each is updated to the version specified in the report and the affected projects build and restore cleanly

**Given** the `source-code/mmria/mmria-server/Dockerfile` build stage currently references:
```
FROM .../trusted-images/dotnet-90:9.0-<tag>@sha256:<digest> AS build
```
and the runtime stage references:
```
FROM .../trusted-images/dotnet-90-runtime:9.0-<tag>@sha256:<digest> AS runtime
```
**When** the developer updates the Dockerfile
**Then** both `FROM` lines reference the `.NET 10` trusted images (`dotnet-100` / `dotnet-100-runtime`) with the correct tag and digest as identified in Story 22.1, and the `-f net9.0` flags in `dotnet build` and `dotnet publish` commands are updated to `-f net10.0`

**Given** the `nccdphp-drh-mmria-services/mmria.services/Dockerfile` contains the same `dotnet-90` / `dotnet-90-runtime` image references and `-f net9.0` flags
**When** the developer updates it
**Then** both `FROM` lines and both `-f` flags are updated identically to the server Dockerfile

**Given** the `.s2i/dockerfile` currently references `dotnet-80` and is largely commented out
**When** the developer reviews it per Story 22.1 findings
**Then** either (a) it is updated to reference `dotnet-100` if the file is still used, or (b) a comment is added to the top of the file documenting that it is retired and not used in the active build pipeline — whichever the analysis in Story 22.1 recommends

**Given** the full build pipeline after all changes
**When** the developer runs:
- `dotnet build` on `mmria-server.csproj` (Release, net10.0)
- `dotnet build` on `mmria.services.csproj` (Release, net10.0)
- `dotnet build` on `mmria.common.csproj`
- `dotnet test` on `mmria-server.tests.csproj`
**Then** all builds succeed and all tests pass with no new failures (pre-existing failures, if any, are noted but do not block this story)

**Given** the `vscode/tasks.json` build tasks reference `net9.0` in `-f` arguments (if any)
**When** the developer searches task definitions
**Then** any hardcoded `-f net9.0` flags in `.vscode/tasks.json` or `tasks.json` are updated to `net10.0`

---

## Epic 22 — Story Sequencing

| Wave | Story | Risk | Dependencies |
|---|---|---|---|
| 22 | 22.1 — Compatibility Analysis & Risk Assessment | None — discovery only | None |
| 22 | 22.2 — Upgrade Execution | Medium | 22.1 complete, no blockers in findings report |

22.1 must fully complete and produce a clean findings report before 22.2 begins. The two stories must not run in parallel.

