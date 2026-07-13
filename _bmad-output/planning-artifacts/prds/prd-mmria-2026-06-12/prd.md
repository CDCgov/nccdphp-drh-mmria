---
title: "PRD: MMRIA V4.1"
status: final
created: 2026-06-12
updated: 2026-07-13 (FR-12 added — Vitals Import NAT/FET Numeric Field Type Normalization; FR-13 added — Data Migration Project Environment Configuration; FR-14 added — Vitals Import Retrospective Data Correction Migration; FR-15 added — data-migration cURL to CouchDbHttpClient Migration; FR-16 added — Replication cURL to CouchDbHttpClient Migration; FR-17 added — VitalsTypeCorrectionMigration Hardening; FR-18 added — Case Rev Endpoint; FR-19 added and clarified — Stale Tab UX modal reload behavior; FR-20 added — Tenant Database Counts Open Cases Visibility; FR-21 added — Case Narrative Instructions Panel Reformatting)
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

> _Note: A prior fix has been applied to the save path. This requirement remains active — the fix is a candidate solution, not a confirmed resolution. Verification through testing is required._

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

| Field             | field_path pattern    | Min | Max | Unit        |
| ----------------- | --------------------- | --- | --- | ----------- |
| Temperature       | `*/temperature`       | 80  | 115 | °F          |
| Heart Rate        | `*/heart_rate`        | 20  | 250 | bpm         |
| Respiration Rate  | `*/respiration_rate`  | 4   | 80  | breaths/min |
| Systolic BP       | `*/systolic*`         | 40  | 300 | mmHg        |
| Diastolic BP      | `*/diastolic*`        | 20  | 200 | mmHg        |
| Oxygen Saturation | `*/oxygen_saturation` | 0   | 100 | %           |

The evaluation engine applies the rule `severity` as written during active input (on-blur). When evaluating persisted case data at load time (historical scan), all rule severities are downgraded one step: `hard → warning`. This ensures hard rules block new entry but only warn on historical data.

Overrides in the `overrides` array shadow the base rule's `severity` at runtime. An operator can change severity or disable a rule entirely by editing the document — no code deployment required. No `review-pending` rule may be promoted to `severity: "hard"` before its `review_status` reaches `"reviewed"`.

**FR-2.4 — Display-time exclusion — print and PDF views**
Out-of-range vitals values are displayed as empty string in print view and PDF view. For each vitals record row where one or more values are excluded, the Comment(s) field for that row is appended with an out-of-range notice in the format: `** Out of range. [Field name] removed.` — one field-name clause per excluded value in a single appended string. If multiple values are excluded from the same row, the clauses are concatenated: e.g., `** Out of range. Temperature removed. Heart rate removed.` If the Comment(s) field already contains text, the out-of-range notice is appended to the existing content. The stored database value is not affected.

**FR-2.5 — Display-time exclusion — graph and table views**
Out-of-range vitals values are excluded from graph and table views within the case form. They are not plotted and not shown in the table. The case form input field continues to display the stored value.

**FR-2.6 — Print/View/PDF validation gate**
When a user initiates a View, View PDF, or Save PDF action, the system applies the following logic before proceeding:

**Closed-state bypass.** If the case status is one of the following, the action is performed directly — no validation runs:

- _Review complete and decision entered_
- _Out of Scope and death certificate entered_
- _False Positive and death certificate entered_

**Open-state validation.** Otherwise, all vitals values in the case are evaluated against the `field-validation-rules` document.

- If no violations exist: perform the action directly.
- If one or more **hard** (`severity: "hard"`) violations exist: the action is **blocked entirely**. The modal is displayed with only a Close button — no proceed path exists. The user must resolve errors before continuing.
- If no hard violations exist but one or more **soft** (`severity: "soft"` / `"warning"`) violations exist: the action requires explicit acknowledgment. The modal is displayed listing the warning count and messages. The user may proceed by confirming. Confirmation is UI-only — no record is persisted to the case document.

> Historical out-of-range vitals data (values persisted before rule enforcement) are evaluated as `severity: warning`. These trigger the soft-acknowledgment path, not the hard-block path.

**Validation modal.**

- Style: matches the existing site modal pattern (purple header, white body, two-button footer).
- Hard-block message: _"This case contains vital sign records with values outside the permitted range. These values must be corrected before printing or viewing."_ Close button only.
- Soft-acknowledgment message: _"This case contains vital sign records with values outside the permitted range. These values are excluded from graphs, tables, print and pdf views."_ Two buttons: **Close** (action not performed) and **[Contextual action]** (_View_, _View PDF_, or _Save PDF_).
- Modal meets Section 508 requirements (see NFR-2).

> _The prior FR-2.6 behavior — modal on edit-mode entry, modal on form navigation, and red text indicator per vitals record — is removed. Story 2.5 covers the implementation that was built under the prior requirement; the prior behavior is removed as part of the story implementing this requirement. This requirement fully supersedes the prior FR-2.6._

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

---

### FR-7 — Admin Action Audit Logging

Five admin actions are not currently captured in the case audit log. Each action is added to the log using the existing audit log pattern: **Update Date/Time, Update By, Update Action, MMRIA Field Prompt, MMRIA Field Path, Old Value, New Value**.

**FR-7.1 — Update Year of Death**
When an admin updates the year of death for a case, an audit entry is written with Update Action `admin change, year of death updated`. Old Value is the previous year value; New Value is the updated year value.

**FR-7.2 — Update Maiden Name**
When an admin updates the maiden name for a case, an audit entry is written with Update Action `admin change, maiden name updated`. Old Value is the previous maiden name value; New Value is the updated maiden name value.

**FR-7.3 — Unlock and Clear Case Status**
When an admin unlocks a case and clears its status, an audit entry is written with Update Action `admin change, case unlocked, case status cleared`. Old Value is the previous case status value; New Value is empty string.

**FR-7.4 — Recover Deleted Case**
When an admin recovers a deleted case, an audit entry is written with Update Action `admin change, case recovered`. MMRIA Field Prompt, MMRIA Field Path, Old Value, and New Value are all blank — the entry records actor and timestamp only.

**FR-7.5 — Delete Case**
When an admin deletes a case, an audit entry is written with Update Action `case deleted`. MMRIA Field Prompt, MMRIA Field Path, Old Value, and New Value are all blank — the entry records actor and timestamp only. The delete is a hard delete; recovery is handled separately (FR-7.4).

> FR-7 scope: write the audit entry at the point each admin action succeeds. No changes to the audit log display, filtering, or admin UI are required.

---

### FR-8 — System Going Offline

A configuration-driven mechanism allows administrators to schedule a planned system outage. The system transitions through three states — normal, warning, and offline — controlled by two date/time thresholds and three configurable messages.

**FR-8.1 — Config Document and Storage**
A `system-offline-config` document is stored in the CouchDB `metadata` database on the CDC instance. The document carries the following fields:

| Field                   | Type                     | Purpose                                                                    |
| ----------------------- | ------------------------ | -------------------------------------------------------------------------- |
| `warn_date`             | ISO 8601 datetime string | Threshold for warning modal                                                |
| `warn_message`          | Multiline string         | Body text of warning modal                                                 |
| `offline_date`          | ISO 8601 datetime string | Threshold for going-offline modal; login is disabled at or after this date |
| `offline_modal_message` | Multiline string         | Body text of going-offline modal                                           |
| `offline_page_message`  | Multiline string         | Text shown on login page in place of login form when offline               |

Config is fetched and saved via mmria-server controller → mmria-services → CDC instance `metadata` DB. The config is global and applies to all tenants.

**FR-8.2 — Warning Modal**
At or after `warn_date`, logged-in users are shown a warning modal once per browser session.

- **Trigger — on login:** immediately after a successful login, if `now >= warn_date`, show the modal.
- **Trigger — periodic check:** if the periodic check (FR-8.5) determines `now >= warn_date` and the session flag is not set, show the modal.
- The modal displays `warn_message` with an OK/dismiss button.
- After dismissal, a `sessionStorage` flag is set. The modal does not reappear until the browser tab is closed and a new session starts.
- If `warn_date` is null or empty, no modal is shown.

**FR-8.3 — Going Offline Modal**
At or after `offline_date`, logged-in users are shown a "going offline" modal.

- **Trigger — periodic check (FR-8.5):** when `now >= offline_date` and a `localStorage` flag is not set, show the modal.
- The modal displays `offline_modal_message` with a single **OK** button (no dismiss or cancel path).
- On OK:
  1. If a case is currently open in edit mode: invoke save (best-effort — same behavior as autosave; sign out proceeds regardless of save outcome).
  2. Sign the user out and navigate to the login page (which renders in offline state per FR-8.4).
- After the OK action, a `localStorage` flag is set to prevent re-display. This is a safety net — pressing OK signs the user out, making re-display effectively impossible under normal flow.
- If `offline_date` is null or empty, this modal never appears.

**FR-8.4 — Login Page Offline State**
When `now >= offline_date`, the login page renders in offline state:

- The login form fields (username input, password input, login button) are hidden.
- The "please contact your jurisdiction admin…" text is replaced with `offline_page_message`, displayed in white text.
- No other login page elements are affected. Login is effectively disabled — no authentication path is presented.

This state is evaluated server-side when a user navigates to the login page (FR-8.6).

**FR-8.5 — Periodic Status Check**
While a user is logged in, the client polls the mmria-server every **2 minutes** for the current offline config.

- The poll endpoint returns the current `warn_date`, `offline_date`, and messages.
- On each poll response, the client evaluates the date thresholds and triggers FR-8.2 or FR-8.3 as applicable.
- The poll is active from login to logout, regardless of which page the user is on.

**FR-8.6 — Login Page Server-Side Check**
When a user navigates to the login page, the server checks the current offline config:

- If `now >= offline_date`: render the login page in offline state (FR-8.4).
- If `now >= warn_date` but `< offline_date`: render the login page normally; the warning modal fires client-side after successful login per FR-8.2.
- If neither threshold is met or the config document is absent: render the login page normally.

**FR-8.7 — Admin Page**
An admin page is added, modeled on the existing `/broadcast-message` page.

- Accessible to **installation admin** role only.
- A link to this page is added in the installation admin navigation alongside the broadcast-message link.
- The form presents all five config fields as editable inputs (two datetime pickers, three multiline text areas).
- Save calls the mmria-server controller, which delegates to mmria-services to write the config to the CDC instance `metadata` DB.
- The form loads current values from the same path on page load.

---

### FR-9 — Data Summary Checks Field Filter

The MMRIA Data Summary Checks page includes a Form dropdown and a Field dropdown with an "ALL" toggle option.

**Current correct behavior (must be preserved):**

- When no Form is selected, the Field dropdown shows all fields across all forms (default state).
- When a Form is selected and "ALL" is unchecked, the Field dropdown correctly filters to show only fields belonging to that Form.

**FR-9.1 — "ALL" selection must respect the active Form filter**
When a Form is selected and the user selects the "ALL" option in the Field dropdown, the Field dropdown enables and displays only the fields belonging to the selected Form — not all fields across all forms. The "ALL" toggle is scoped to the current Form context.

> _This is a bug fix. The defect: selecting "ALL" while a Form is chosen causes the Field dropdown to show all fields regardless of the selected Form, bypassing the form-scoped filter. Unchecking "ALL" already correctly filters by the selected Form — that behavior is working and must not be changed. No other behavior, layout, or data changes are in scope._

---

### FR-10 — Manage Users Export Respects Active Filter

**FR-10.1 — Export User List must export only the currently displayed (filtered) users**
When an admin has applied a Role or Username filter on the Manage Users page, clicking "Export User List" must download an XLSX that contains only the users visible in the filtered table — not all users in the system. When no filter is active, the export continues to include all users.

> _This is a bug fix (added 2026-06-25). The defect: `export_user_list_click()` in `index.js` joins against `g_ui.user_summary_list` (always the full unfiltered list) instead of `g_filtered_user_list` (the active filter result maintained by `summary_renderer.js`). Fix: one-token substitution. No server-side or XLSX format changes are in scope._

---

### FR-11 — CVS PDF Export Tool Reliability

The Community Vital Signs (CVS) PDF export tool periodically fails when the external CVS service is unavailable, still generating the report, or returns unexpected responses. V4.1 hardens the integration at the services, server, and client layers; introduces an automatic retry mechanism with user-visible countdown; and improves status feedback to the user and the parent page.

**FR-11.1 — Services Layer: Replace Busy-Wait with Async Delay**
The `BatchSupervisor` actor in mmria-services replaces the 40-second busy-wait loop — which caused 100% CPU spin — with `await Task.Delay`. The actor's initial batch-list load is moved out of the constructor into a deferred `PreStart` → async message pattern using `IWithStash` so that construction does not block on a CouchDB round-trip and incoming messages are held in the stash until initialization completes.

**FR-11.2 — Server-Side Error Hardening**
`CVSManager.GetDashboardAsync` returns a structured result for all failure conditions rather than allowing exceptions to propagate unhandled. The result carries both a `file_status` field and a human-readable `message` field. The following failure conditions are handled explicitly:

| Failure condition | `file_status` | `message` |
|---|---|---|
| `HttpRequestException` or `TaskCanceledException` | `"unavailable"` | "The CVS service did not respond." |
| HTTP 5xx / 408 / 429 | `"unavailable"` | "The CVS service returned HTTP {code}." |
| Other non-2xx HTTP status | `"error"` | "The CVS service returned HTTP {code}." |
| Empty or whitespace response body | `"unavailable"` | "The CVS service returned an empty response." |
| JSON parse failure — body matches generating pattern | `"generating"` | "The CVS service is preparing the PDF." |
| JSON parse failure — body matches unavailable pattern | `"unavailable"` | "The CVS service is unavailable." |
| JSON parse failure — other | `"error"` | "The CVS service returned an unexpected response." |
| Base64 decode failure | `"error"` | "The CVS service returned an invalid PDF response." |

The `message` field is propagated through the API controller to the client response.

**FR-11.3 — Client-Side Retry Mechanism**
The CVS page polling loop retries automatically when the service reports `"generating"` or `"unavailable"`. The loop behavior:

- Makes up to `CVS_MAX_ATTEMPTS` total attempts before stopping.
- Waits `CVS_RETRY_DELAY_SECONDS` between attempts and displays a live second-by-second countdown to the user.
- Shows attempt progress: _"Generating PDF... Checking Community Vital Signs service, attempt N of MAX."_
- On exhausting all attempts without a terminal result, displays a **Try again** button that restarts the polling loop without requiring a page refresh.
- A `g_is_running` guard prevents concurrent polling runs if the user clicks **Try again** before the current run finalizes.

`CVS_MAX_ATTEMPTS` and `CVS_RETRY_DELAY_SECONDS` are read from the CouchDB configuration document at request time via `CvsController.Index()` and emitted into the page as `window` globals. Default values (10 attempts, 60-second delay) are applied when the keys are absent from the configuration document. No helper class is required — the controller follows the inline `configuration.GetInteger(key, host_prefix) ?? default` / `TempData` pattern used throughout the application.

**FR-11.4 — Status Feedback and Parent-Page Button State**
The CVS page and the parent case page maintain consistent, user-visible status:

- The `message` field returned by the server is included in the activity log rendered on the CVS page.
- The CVS page broadcasts structured status events to the parent page via `BroadcastChannel('cvs_channel')`: `"started"`, `"ready"`, `"failed"`, `"max_retries"`, `"validation_error"`.
- The parent page button that opened the CVS window is disabled (`aria-busy="true"`) while a report is in progress and re-enabled when a terminal status is received.
- A fallback timer (20 minutes) re-enables the button if no terminal BroadcastChannel message is received — for example, when the CVS window is closed unexpectedly.
- The server logs a structured telemetry entry on each completed request: `"CVS dashboard request completed. status={Status} duration_ms={DurationMs}"`.

---

### FR-12 — Vitals Import NAT/FET Numeric Field Type Normalization

Source: Bug 117351 (VA-reported, 2/6/2026). Cases created via vitals import display "Select Value" in dropdown fields instead of the coded label. Root cause: `mmria.services` `BatchItemProcessingService` writes certain NAT/FET fields to CouchDB as JSON strings (e.g., `"mother_married": "0"`) while mmria-server writes the same fields as JSON numbers (`"mother_married": 0`). The front-end dropdown renderer expects integer values and silently falls back to "Select Value" when it finds a string. The Rule methods (`MARN_Rule`, `ACKN_Rule`, etc.) correctly map IJE characters to numeric string codes but those codes are stored as JSON strings because `C_Get_Set_Value.set_value` always writes `string` to the document. The fix requires that integer-coded fields are stored as integers in the JSON document.

**FR-12.1 — Integer storage for coded dropdown fields at `set_value` call sites**
For `MARN` (`birth_fetal_death_certificate_parent/demographic_of_mother/mother_married`) and `ACKN` (`birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital`), and all other NAT/FET fields whose MMRIA metadata type is `number` and whose values are always numeric strings, the call to `gs.set_value()` in `BatchItemProcessingService` stores the value as a JSON integer in the CouchDB document — not as a JSON string. The implementation approach (adding an object overload to `C_Get_Set_Value`, modifying the existing method, or directly setting the dictionary key) is developer discretion, subject to: no regressions on string-typed fields, and matching the type that mmria-server produces for the same fields.

**FR-12.2 — Post-fix verification**
After FR-12.1, a newly imported NAT record with a Y/N value for `MARN` or `ACKN` results in a JSON integer stored in CouchDB (e.g., `"mother_married": 0`). The front-end dropdown for both affected fields displays the correct coded label. The developer audits adjacent non-wrapped fields (MEDUC, FEDUC, ATTEND, TRAN, PAY, WIC) and applies the same integer storage fix to any that the front-end expects as integers.

---

### FR-13 — Data Migration Project Environment Configuration

The `data-migration` project requires source-code edits to target a different environment — CouchDB URL, credentials, and the jurisdiction prefix list are all hardcoded in `Program.cs` and `appsettings.json`. The `Replication` project solved this problem with a layered `appsettings.json`/`appsettings.local.json` pattern backed by typed configuration classes (`EnvironmentSettings`, `CouchDBSettings.DatabaseUrlTemplates`, per-environment `Credentials`, per-environment `JurisdictionLists`). `data-migration` must adopt the same pattern.

**FR-13.1 — Layered appsettings pattern**
`data-migration/appsettings.json` is restructured to define the full configuration schema with blank/safe defaults. `appsettings.local.json` (gitignored, documented in `HTTPS-SETUP-INSTRUCTIONS.md` or an equivalent README section) holds local credentials and active run settings. `Program.cs` loads both files via `ConfigurationBuilder.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)`.

**FR-13.2 — Typed configuration model**
A `Configuration.cs` class is added to `data-migration` defining a root `DataMigrationAppConfiguration` with the following sections — modeled directly on the Replication project's `Configuration.cs`:
- `MigrationSettings`: `RunType` (string), `IsReportOnlyMode` (bool, default `true`), `DatabaseName` (string, default `"mmrds"`)
- `EnvironmentSettings`: `ConfigEnvironment` (string), `IntOrProd` (string) — identical shape to Replication
- `CouchDBSettings`: `DatabaseUrlTemplates` with properties `Localhost`, `Development`, `QA`, `Integration`, `Production`
- `Credentials`: `Dictionary<string, CredentialConfig>` keyed by environment name, each with `Username` and `Password`
- `JurisdictionLists`: `Dictionary<string, List<string>>` with keys `Localhost`, `Development`, `QA`, `Integration`, `Production`, `Alternate`, `Filtered`

**FR-13.3 — Hardcoded list removal**
`run_list`, `test_list`, `prefix_list`, and the `is_test_list` boolean in `Program.cs` are removed. The active prefix list is selected at startup by reading `JurisdictionLists[ConfigEnvironment]`. The `has_been_done_set` skip mechanism is retained.

**FR-13.4 — URL and credential construction**
The active CouchDB URL per jurisdiction is constructed from `CouchDBSettings.DatabaseUrlTemplates[ConfigEnvironment]`, with `{prefix}` substituted. Credentials are sourced from `Credentials[ConfigEnvironment]`. The legacy flat keys `data_migration:couchdb_url`, `data_migration:timer_user_name`, and `data_migration:timer_value` are removed from `appsettings.json` and from `Program.cs`.

---

### FR-14 — Vitals Import Retrospective Data Correction Migration

Cases processed by vitals import before the FR-12 fix are persisted in CouchDB with string values for fields that must be integers. The front-end cannot self-correct these — they remain broken until the underlying document is fixed. The `data-migration` project is the correct tool for this correction.

**FR-14.1 — New migration run type**
`VitalsTypeCorrection` is added to the `RunTypeEnum` in `data-migration/Program.cs`. When `MigrationSettings.RunType` is `VitalsTypeCorrection`, the program executes the string-to-integer field correction logic across all case documents in the configured jurisdiction databases.

**FR-14.2 — Target field list**
The initial set of CouchDB field paths subject to correction:
- `birth_fetal_death_certificate_parent/demographic_of_mother/mother_married`
- `birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital`

Additional paths are appended as the developer confirms which other NAT/FET fields with MMRIA metadata type `number` were affected by the import defect.

**FR-14.3 — Correction logic**
For each case document retrieved from the target database: for each target field path, if the stored value is a JSON string whose trimmed content parses as a valid integer via `int.TryParse`, the stored value is replaced with the corresponding JSON number. Documents where the field already holds a JSON number, null, or is absent are skipped without modification.

**FR-14.4 — Report-only mode**
The migration uses `MigrationSettings.IsReportOnlyMode` (default `true`). In report-only mode, the document `_id`, the field path, and the current string value are written to the run output log but no CouchDB writes are issued. Setting `IsReportOnlyMode = false` applies corrections and persists the updated documents via existing CouchDB update infrastructure. This mirrors the behavior of existing migrations (`VitalsMigration01`).

**FR-14.5 — Environment dependency**
FR-14 depends on FR-13. The migration uses the `EnvironmentSettings`/`CouchDBSettings`/`Credentials` configuration added by FR-13 to connect to the target environment and iterate the configured jurisdiction list.

---

### FR-15 — data-migration cURL to CouchDbHttpClient Migration

The `data-migration` project makes all CouchDB and external HTTP calls through a local `cURL` class. The `mmria.common` project exposes `mmria.common.getset.CouchDbHttpClient`, a tested, DI-backed HTTP wrapper with the same `ExecuteAsync(method, url, payload, userName, password)` surface, already consumed by `mmria-server` and `mmria-services`. This requirement replaces the `cURL` class in `data-migration` with `CouchDbHttpClient` at every call site.

**FR-15.1 — Dependency injection wiring**
`Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Http` are added to `migrate.csproj`. In `Program.cs`, a `ServiceCollection` is constructed, `AddHttpClient()` is registered, and a `ServiceProvider` is built before the main run logic executes. A `CouchDbHttpClient` instance is resolved from the provider and passed through to all classes that currently construct a `cURL` object.

**FR-15.2 — CouchDB call-site replacement**
Every `new cURL(...)` instantiation in the following files is replaced with an equivalent `await _couchDbHttpClient.ExecuteAsync(method, url, payload, userName, password)` call, preserving the existing method (GET / PUT / POST / DELETE), URL, payload, and credential arguments:

- `SaveRecord.cs`
- `db_backup/db_backup.cs`
- `migration-set/committee_review_pregnancy_relatedness.cs`
- `migration-set/editable_list.cs`
- `migration-set/Fix_American_Indian_Recode.cs`
- `migration-set/GA-One-Time.cs`
- `migration-set/Manual-Migration.cs`
- `migration-set/MMRDS_CS_Narrative_Migration.cs`
- `migration-set/Process_Migrate_Charactor_to_Numeric.cs`
- `migration-set/SubstanceMigration.cs`
- `migration-set/v2.10-Migration.cs`
- `migration-set/CVS_Migration.cs` (CouchDB calls only — see FR-15.3)
- `common/CVS.cs` (CouchDB calls only — see FR-15.3)
- `mmrds-importer/mmria_server_api_client.cs`

After all call sites are replaced, `cURL.cs` is deleted from the project.

**FR-15.3 — External service calls (CVS service)**
`cURL` instantiations in `CVS_Migration.cs` and `CVS.cs` that target the CVS external service (non-CouchDB endpoints, typically unauthenticated POST calls to the CVS base URL) are migrated to use `CouchDbHttpClient.ExecuteAsync` with null credentials, consistent with the existing pattern for unauthenticated calls. No separate HTTP client is introduced.

**FR-15.4 — No behavior change**
FR-15 is a mechanical refactor. No migration logic, URL construction, credential sourcing, or JSON serialization is modified beyond the call-site substitution. The `has_been_done_set` skip mechanism, report-only mode, and all other runtime behaviors are unchanged.

---

### FR-16 — Replication cURL to CouchDbHttpClient Migration

The `Replication` project makes all CouchDB and external HTTP calls through the same local `cURL` class pattern. Unlike `data-migration`, `Replication` does not yet reference `mmria.common`. This requirement adds the reference and replaces the `cURL` class with `CouchDbHttpClient` at all CouchDB call sites.

**FR-16.1 — mmria.common project reference and DI wiring**
A `ProjectReference` to `mmria.common.csproj` is added to `replicate.csproj`. `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Http` packages are added. In `Program.cs`, a `ServiceCollection` is constructed, `AddHttpClient()` is registered, and a `ServiceProvider` is built before the main run logic. A `CouchDbHttpClient` instance is resolved from the provider and threaded through to all call sites — including the `OverridableConfiguration`, `utils.cs`, and `Role_Replication.cs` classes that currently accept the `IConfiguration` object to source credentials.

**FR-16.2 — CouchDB call-site replacement**
Every `new cURL(...)` instantiation that targets a CouchDB endpoint (identifiable by the presence of `config_timer_user_name`, `config_timer_value`, `env_username`, `env_password`, or similar credential arguments) in the following files is replaced with `await _couchDbHttpClient.ExecuteAsync(method, url, payload, userName, password)`:

- `OverridableConfiguration.cs`
- `utils.cs`
- `Role_Replication.cs`
- `Program.cs` — all CouchDB-credentialed `cURL` calls (replication POSTs, user GET/PUT, config GET/PUT, design document PUT, index POST, delete operations, clear history, etc.)

**FR-16.3 — External API calls (non-CouchDB)**
`cURL` instantiations in `Program.cs` that call external, unauthenticated endpoints — including image tag lookups, redeploy URLs, resume/pause rollout URLs, trivy scan URL, scale-to-zero/scale-to-one URLs, twistlock scan URL, and environment update URLs (all with null credentials) — are migrated to use `CouchDbHttpClient.ExecuteAsync` with null credentials. These calls do not require a separate HTTP client.

After all call sites are replaced, `cURL.cs` is deleted from the project.

**FR-16.4 — No behavior change**
FR-16 is a mechanical refactor. No replication logic, jurisdiction list processing, user synchronization, or design document seeding behavior is modified beyond the call-site substitution.

---

### FR-17 — VitalsTypeCorrectionMigration Hardening

The `VitalsTypeCorrectionMigration` CLI tool (in `data-migration/migration-set/VitalsTypeCorrectionMigration.cs`) currently returns a `bool` from `SaveRecord.save_case()` and silently skips any case document that encounters a CouchDB 409 conflict. Because every case must be successfully migrated — a skipped case is a data integrity incident — the tool must be hardened with conflict retry, explicit error surfacing, a pre-flight offline gate, and a summary report.

**FR-17.1 — `SaveResult` enum replaces `bool` return in `SaveRecord`**
`data-migration/SaveRecord.save_case()` is changed to return a `SaveResult` enum with three members: `Success` (HTTP 2xx), `Conflict` (HTTP 409), and `Error` (all other non-success codes). No `bool` return path remains. All callers are updated. This change makes 409 distinguishable from other failures so the migration loop can apply the correct retry strategy.

**FR-17.2 — Retry-on-conflict with fresh `_rev` fetch**
`VitalsTypeCorrectionMigration` implements a per-document retry loop with a maximum of 3 attempts. On `SaveResult.Conflict`: (1) fetch the current document snapshot via `GET /{db}/{id}`, (2) re-apply `ApplyVitalsTypeCorrection()` to the fresh snapshot, (3) retry the save. On retry exhaustion, the failure is recorded in a `failed_count` counter and the loop continues to the next document — all cases must be attempted. `Environment.Exit(3)` is called at the end of the run if `failed_count > 0`.

**FR-17.3 — Pure, re-applicable transform method**
The field correction logic is extracted into a static method `ApplyVitalsTypeCorrection(doc)` that is side-effect-free and idempotent — calling it on a document where fields are already integers produces no change. This method is unit-testable independently of HTTP calls and is the single implementation called on both the initial snapshot and any retry snapshot.

**FR-17.4 — Hard stop on non-conflict error**
On `SaveResult.Error` (network failure, auth failure, unexpected server error): log the case `_id`, the HTTP status code, and the response body to stderr, then call `Environment.Exit(1)` immediately. Non-conflict errors are not retryable and require operator investigation before the migration proceeds.

**FR-17.5 — Pre-flight offline date check**
Before processing any documents, the migration reads `offline_date` from configuration and verifies `DateTime.UtcNow >= offline_date`. If the condition is not met, the migration writes `"PRE-FLIGHT FAIL: system is not offline. Aborting."` to stderr and calls `Environment.Exit(2)`. This prevents accidental execution against a live system.

**FR-17.6 — Run summary output**
On normal completion, the migration emits a final summary to stdout: `Processed: N | Already migrated: N | Failed (retries exhausted): N`. Exit code 0 is reserved for runs where `failed_count == 0`. Exit code 3 indicates one or more documents could not be saved after all retries.

---

### FR-18 — Case Rev Endpoint

To enable client-side staleness detection (FR-19), the mmria-server must expose a lightweight, authenticated endpoint that returns only the current `_rev` of a case document. This avoids returning the full case payload on every poll cycle.

**FR-18.1 — `GET /api/case/{id}/rev` endpoint**
A new action is added to the existing case controller in `source-code/mmria/mmria-server/`. The route is `GET /api/case/{id}/rev`. The endpoint:
- Requires authentication (same cookie-based auth as existing case GET endpoints).
- Returns `200 { "_id": "<id>", "_rev": "<current_rev>" }` when the document exists in CouchDB.
- Returns `404` when the document does not exist.
- Does not return the full document body.

**FR-18.2 — Performance**
The endpoint returns only `{ "_id": "<id>", "_rev": "<current_rev>" }` and must not call the system-offline services path or attach offline-status headers. Offline timing remains owned by `/api/system-offline/status`. Response latency target is under 200 ms on the local network.

---

### FR-19 — Stale Tab UX

A browser tab that was backgrounded or frozen before `offline_date` and foregrounded after a data migration has run will hold a stale in-memory case snapshot with a stale CouchDB `_rev`. If the user attempts to save, they will receive a CouchDB 409 conflict. Two mechanisms address this: a proactive `_rev` poll that detects staleness before a save attempt, and a reactive 409 intercept that surfaces a clear recovery path if the save is attempted anyway.

**FR-19.1 — 409 intercept on case save (reactive)**
The existing case save error handler in the client is updated to intercept HTTP 409 responses specifically. On 409, a non-dismissable Bootstrap-style modal is displayed with the following message: *"This case was updated elsewhere. Reload to get the latest version before saving."* The modal contains a single **[Reload Case]** button. The button invokes the case reload helper, which reloads the open case in-place when the case page hook is available and falls back to `window.location.reload()` otherwise. The generic error handler does not fire for 409 — this branch takes over exclusively. No server-side change is required for this sub-feature.

**FR-19.2 — `_rev` polling while a case is open (proactive)**
After a case loads for editing and the current tab owns the active checkout, the client starts a `setInterval` polling `GET /api/case/{id}/rev` (FR-18) every 45 seconds. On each response, the returned `_rev` is compared to the `_rev` captured at case load time or the latest successful save. If the values differ, a Bootstrap-style stale-case modal is displayed with the message: *"This case has been updated. Reload to see the latest version."* The modal has a single **[Reload]** button and no dismiss action. The button invokes the case reload helper, which reloads the open case in-place when possible and falls back to `window.location.reload()` otherwise. Autosave is paused while this stale state is active and resumes only after the case reloads. Polling stops when the user leaves edit mode, releases the checkout through Save & Close, or navigates away from the case.

**FR-19.3 — Poll scope**
Polling is only active while the current tab owns the open case's active edit checkout. Users with write access who are viewing the case read-only, including immediately after Save & Close, do not poll and do not receive proactive `_rev` warnings for changes made by another user.

The client polls only when all of the following are true:

- The page is in the case-editing flow for a write-capable abstractor user.
- The current tab owns the active, non-expired checkout lock for the case.
- The loaded case has both `_id` and `_rev`.

The client does not poll for `_rev` when any of the following are true:

- The user is on a data analyst or other read-only case route.
- The user has write access but is only viewing the case, including the read-only state after Save & Close.
- Checkout has not yet been acquired, checkout acquisition failed, or a checkout conflict keeps the tab in view mode.
- The case is in offline-processing mode, failed to load, hit an auth failure, or the user navigated away/unloaded the page.

After a successful save while edit mode remains active, the polling reference `_rev` is updated to the save response revision and polling continues. After Save & Close or another successful lock release, polling stops.

**FR-19.4 — Section 508**
The proactive stale-case modal (FR-19.2) and the 409 recovery modal (FR-19.1) meet Section 508 accessibility requirements consistent with NFR-2. Each modal is announced to screen readers when it appears, uses alert-dialog semantics, and moves focus to its reload button.

---

### FR-20 — Tenant Database Counts: Open Cases Visibility

The `/tenant-database-counts` page is an installation-admin tool for monitoring database health across all tenants. Adding open-case visibility gives administrators a real-time signal of active checkout lock activity and orphaned checkout states without requiring a separate query or tool.

A case document in the MMRDS database is considered **open for editing** when the field `checked_out_by_tab_id` is present and non-null in the CouchDB document. The presence of this field is the existing checkout mechanism used by the case edit lock system.

**FR-20.1 — Active vs. possibly-stale classification**
Open cases are classified at query time using a fixed 10-minute boundary applied to `date_last_updated` (UTC):

- **Active**: `checked_out_by_tab_id` is present AND `date_last_updated` is within the past 10 minutes. This indicates a user is actively editing the case or has edited it very recently.
- **Possibly stale**: `checked_out_by_tab_id` is present AND `date_last_updated` is more than 10 minutes in the past. These cases likely represent orphaned checkouts — browser crashes, session expiry, or tab closes that did not trigger a clean unlock. They are informational signals, not errors.

The 10-minute threshold is a fixed constant and is not read from the CouchDB configuration document.

**FR-20.2 — Per-tenant open case query**
For each tenant entry, the system issues a CouchDB Mango query (`POST /{mmrds_db}/_find`) against the tenant's MMRDS database:

```json
{
  "selector": { "checked_out_by_tab_id": { "$exists": true } },
  "fields": ["_id", "date_last_updated"],
  "limit": 1000
}
```

No pre-built index is required. Because `/tenant-database-counts` is an on-demand, installation-admin-only page (not a polling loop), a full collection scan is acceptable at current data volumes. The query runs in parallel with the existing mmrds/de_id/report count queries already issued per tenant.

The server-side C# layer partitions the returned document stubs into active and possibly-stale counts using the 10-minute threshold.

**FR-20.3 — Summary tile**
A fifth summary tile is added to the page header row alongside the existing Entries, MMRDS Threshold, and De-ID Mismatch tiles. The tile is labeled **Open Cases** and displays system-wide totals:

- `{N} active` — sum of active open cases across all tenant entries.
- `{N} possibly stale` — sum of possibly-stale open cases across all tenant entries, displayed in amber text when non-zero.

When both counts are zero, the tile displays a single `0` with no further classification.

**FR-20.4 — Table column**
A new **Open Cases** column is added to the Counts by Entry table. For each tenant row:

- When the query succeeds and both counts are zero: display `0`.
- When active count is non-zero and stale is zero: display the active count (e.g. `2`).
- When both counts are non-zero: display active count with stale count in amber parentheses (e.g. `2 (1)`).
- When only stale cases exist: display `0 (1)` with the stale count in amber.
- When the query fails (timeout, network error, permission error): display `-`, consistent with the `-` convention used in other error-state cells in the table.

**FR-20.5 — Error handling and status isolation**
An open-case query failure for a single tenant does not affect that tenant's `status` field — `status` remains computed solely from the existing mmrds/de_id/report error logic. The open-case error is captured in a separate `open_case_error` field on the per-entry response model and surfaced only as `-` in the table cell. It does not contribute to the EntriesWithErrors summary count.

---

### FR-21 — Case Narrative Instructions Panel Reformatting

The Case Narrative form displays a guidelines/instructions panel with usage guidance for reviewers. The content of this panel is updated with text corrections and formatting changes only. No behavior, configuration, data, or structural changes are required.

**FR-21.1 — Title change**
The panel title `"Case Narrative Template Guidelines:"` is replaced with `"Case Narrative"`. The colon and the words "Template Guidelines" are removed. Bold heading formatting is retained.

**FR-21.2 — List format: bullet points replaced with dash prefix**
All eight list items in the panel — the three introductory items and the five "Remember to:" items — are reformatted from bullet point (•) style to plain-text dash (`-`) prefix. No `<ul>`/`<li>` list wrapper is used.

**FR-21.3 — Text correction: lowercase "into"**
In the second introductory item, `"or Into an external document"` is corrected to `"or into an external document"` (lowercase `i`).

**FR-21.4 — "Remember to:" formatting change**
The `"Remember to:"` label is changed from bold to plain text.

**FR-21.5 — Text correction: lowercase "inclusive"**
In the third "Remember to:" item, `"Use Inclusive and non-stigmatizing language"` is corrected to `"Use inclusive and non-stigmatizing language"` (lowercase `i`).

**FR-21.6 — Trailing periods removed from "Remember to:" items**
The trailing period is removed from each of the five "Remember to:" list items. The three introductory list items retain their trailing periods.

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

No other content, behavior, or data changes are in scope for this requirement.

---


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

| Constraint                        | Detail                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| --------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Auto-save must not be disrupted   | Hard validation rejects invalid values at the field level (on-blur) before they enter form state. Save triggers at any point must not encounter invalid vitals values. Auto-save is never blocked.                                                                                                                                                                                                                                                                   |
| Admin UI scope                    | Configuration updates for OMB date and MMRIA version are developer-managed via CouchDB document and production script only. The `field-validation-rules` document is also developer-managed for V4.1. The rule management admin UI delivered by the POC (`/case_validation_metadata`) is operator tooling available in V4.1 but is not a V4.1 end-user deliverable and has no formal acceptance criteria in this sprint — acceptance criteria will be added in V4.2. |
| Existing out-of-range vitals data | Historical out-of-range vitals values are not cleared or corrected. They are surfaced as `severity: warning` in the Validation Errors Panel (FR-6) and trigger the soft-acknowledgment print gate path. The stored database value is not modified.                                                                                                                                                                                                                   |
| PMSS dropdowns                    | PMSS-related print dropdowns must not be modified.                                                                                                                                                                                                                                                                                                                                                                                                                   |
| CVS retry constants               | `CVS_MAX_ATTEMPTS` and `CVS_RETRY_DELAY_SECONDS` are compile-time constants in `cvs/index.js` for V4.1. They are not runtime-configurable via CouchDB in this release.                                                                                                                                                                                                                                                                                               |

---

## Open Items

| #    | Item                                                                                  | Blocker?     | Owner               | Condition to Resolve                                                                                                                                                                                                                                                                                                                                                                                                         |
| ---- | ------------------------------------------------------------------------------------- | ------------ | ------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| OI-1 | Vitals valid ranges not yet defined                                                   | **Resolved** | Program team        | Ranges confirmed — see FR-2.3.                                                                                                                                                                                                                                                                                                                                                                                               |
| OI-2 | Three specific print dropdown render locations for `core-summary` need identification | No           | Developer           | Identify all client-side render sites before FR-4 implementation                                                                                                                                                                                                                                                                                                                                                             |
| OI-3 | FR-1.1 prior fix status                                                               | **Resolved** | Developer           | Implementation complete; going to formal verification.                                                                                                                                                                                                                                                                                                                                                                       |
| OI-4 | Validation mode design discussion                                                     | **Resolved** | Architect + Analyst | Validation mode is implemented as a `severity` property (`hard` \| `soft`) on each rule in a dedicated version-scoped `case-validation-rules-{metadata_version}` CouchDB document; no user-facing mode toggle exists; active-input hard validation clears fields on blur; historical data is downgraded to `warning` at load time; soft-warning acknowledgment is UI-only with no case-document persistence. FR-2.3 updated. |
| OI-5 | CVS retry constant values (`CVS_MAX_ATTEMPTS`, `CVS_RETRY_DELAY_SECONDS`)             | No           | Developer           | Confirm final values with program team before release. Current implementation uses compile-time constants; confirm whether runtime configurability is needed for V4.2.                                                                                                                                                                                                                                                        |
