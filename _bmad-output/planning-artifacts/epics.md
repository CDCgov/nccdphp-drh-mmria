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
FR-31.1: The "View/Download Informant Interview Summary Template" button (`#view-informant-interview-summary-template-button`) on the Home page displays a clearly visible, high-contrast focus outline when reached via keyboard navigation (`:focus-visible`). The outline must be visually distinct from the button's default appearance.
FR-31.2: The "View/Download MMRIA Committee Decisions Form (CDF) Template PDF" button (`#view-cdf-template-button`) on the Home page displays the same clearly visible, high-contrast focus outline when reached via keyboard navigation (`:focus-visible`).
FR-33.1: The `mmria-case-generator` produces parseable, type-appropriate numeric values for metadata `number` fields, respecting metadata decimal precision where available and using plausible ranges for high-risk clinical/date-adjacent fields.
FR-33.2: The `mmria-case-generator` produces valid date, datetime, time, and grouped month/day/year values, with core maternal-mortality timeline relationships kept plausible across date of death, date of birth, pregnancy, prenatal, admission, discharge, and visit fields.
FR-33.3: When `ValidateBeforeSave = true`, generated cases are recursively validated by full metadata path before JSON output or CouchDB save, and invalid date/number values block output instead of being silently written.
FR-33.4: Focused regression tests cover generator date and number plausibility across simple fields, groups, grids, multiforms, and fixed random seeds.
FR-34.1: When the case narrative PDF export renders saved Trumbowyg HTML that has been reserialized after editing, whitespace-only inter-tag separator text nodes do not create visible blank rows or extra paragraph spacing in the PDF.
FR-34.2: When the saved narrative contains an intentional blank paragraph such as `<p><br></p>`, the PDF export renders it as exactly one intentional blank line rather than multiplying the `<br>` newline with paragraph trailing newline behavior.
FR-34.3: The spacing fix preserves the stored `g_data.case_narrative.case_opening_overview` HTML and constrains behavior changes to PDF conversion unless implementation evidence proves that scope cannot satisfy the defect.
FR-34.4: When saved case narrative HTML contains a standalone `<br>` followed by a whitespace-only empty paragraph before the next section, the PDF export collapses that separator sequence to one intentional break instead of rendering duplicate blank rows.
FR-34.5: When clipboard content contains `text/plain` only (no `text/html`), pasting into the case narrative editor converts each newline-delimited line to a separate `<p>` paragraph. Empty lines become `<p><br></p>`. No raw newline characters remain embedded in text nodes after the paste.
FR-34.6: When the cursor is at the end of a narrative paragraph whose text begins with the pattern `^\d+\.\s` followed by non-whitespace content, pressing Enter (no modifier keys) inserts a new paragraph beginning with the next sequential integer followed by `. `, with the cursor positioned immediately after that prefix.
FR-35.1: While a user is in offline mode, the "Exit Offline Mode" button is not shown on any page, while the "You're Offline" text/icon indicator remains visible and is right-aligned within its host container. The underlying widget, modal, and cleanup code are preserved (hidden, not deleted) for potential reuse in a future release.
FR-35.2: Every user-visible occurrence of the "Go Online" label — both primary action buttons, their busy/connectivity state text, and the confirmation modal title and button — reads "Go Online & Sync Changes" instead.
FR-35.3: Both "Go Online & Sync Changes" entry points (Offline Case List table and Cases Selected for Offline Work table) show the same confirmation modal before syncing changes and returning online; neither entry point bypasses confirmation.

### NonFunctional Requirements

NFR-1: All changes must function correctly in Microsoft Edge and Google Chrome. No other browsers are in scope.
NFR-2: The vitals validation modals (FR-2.2, FR-2.6) must meet Section 508 accessibility requirements â€” role, aria-modal, aria-labelledby, focus management, keyboard dismissal, and focus return.
NFR-3: Vitals range configuration is loaded once at server startup and held in memory. Field-level blur validation is synchronous against the in-memory config. No per-event network requests are introduced.
NFR-33.1: Generator improvements must remain a low-impact utilities change: no metadata schema changes, no generated strong-case model edits, no new external services, and no broad rewrite of the case generation pipeline.
NFR-34.1: The case narrative PDF spacing fix remains surgical, with no new client-side dependencies, no bundler changes, no storage migration, and no broad rewrite of `pdf-version/index.js`.
NFR-34.2: The plain-text paste fix (FR-34.5) is scoped to the `else if (pastedText)` branch in `attach_narrative_paste_handler` in `textarea.js`. The `text/html` paste path, `textarea_control_strip_html_attributes`, the save path, and `pdf-version/index.js` are not changed.
NFR-35.1: The Epic 35 change is UI/copy-scoped only — no server-side changes, no CouchDB schema changes, and no removal of `OfflineExitManager`, `offline-modals.js` exit-modal markup, or their cleanup/audit behavior. All hidden code remains fully intact and reachable via a future toggle.
FR-37.1: A C# console application (`mmria-form-html-generator`) in `nccdphp-drh-mmria-utilities` reads `metadata.json` and `ui_specification.json` from configurable file paths and generates one HTML fragment file per top-level form section (14 sections). Output is written to a configurable output directory, committed to `mmria-server/wwwroot/generated-forms/`.
FR-37.2: Each generated HTML fragment renders all fields with inline `position:absolute` styles sourced from `ui_specification.form_design[path].control.style`. Every input, select, and textarea carries `data-path` (slash-separated metadata path), `data-field-type` (metadata type string), and `data-prompt` attributes. Conditionally visible fields carry `data-show-when="path=value"`. Label elements carry positioning from `ui_specification.form_design[path].prompt.style`.
FR-37.3: Grid controls (`metadata.type == "grid"`) are generated with a `<tbody data-grid-body="path">` container and a `<template data-grid-template="path">` row template. Multi-cardinality forms (`cardinality == "+"` or `"*"`) are generated with a `<template data-form-template="path">` instance template and a `[data-form-instances="path"]` container. `{{index}}` is used as the array-index placeholder in data-path values inside templates. List fields carry `data-list-id` matching the metadata list name so the binder can resolve option values at runtime.
FR-37.4: The generator logs any metadata path that has no entry in `ui_specification.form_design` to stderr. Chart fields (`metadata.type == "chart"`) are emitted as `<div data-field-type="chart" data-path="...">` placeholder elements. The existing `chart.js` is retained as a specialized component invoked by the data binder for those elements only.
FR-37.5: A new `wwwroot/scripts/form-binder.js` exposes `formBinder.bind(sectionEl, gData, options)` and `formBinder.collect(sectionEl, gData)`. `bind()` walks all `[data-path]` elements in the section, populates values from gData, evaluates `[data-show-when]` rules, clones grid and multi-form templates for array data, and delegates `[data-field-type="chart"]` elements to the existing chart rendering code. `collect()` reads all `[data-path]` element current values and writes them back to gData with type coercion appropriate to `data-field-type`. `options.readOnly = true` disables all inputs after binding and suppresses change listener wiring.
FR-37.6: When `options.readOnly` is not set, `formBinder.bind()` attaches a single delegated `change`/`input` listener on the section container that writes changed values back to `g_data` at the correct path immediately. This replaces per-field onChange wiring done by the type renderers. Dependent list filtering (parent-child dropdowns) and `[data-show-when]` re-evaluation are triggered by the same listener.
FR-37.7: All three case rendering contexts — Case editor (`Case/Index.cshtml`), analyst read-only (`AnalystCase/Index.cshtml`), and committee member (`CaseVRO/Index.cshtml`) — embed all 14 generated section HTML fragments in the initial page load. Section navigation shows/hides the target section div and calls `formBinder.bind(sectionEl, g_data)` (edit) or `formBinder.bind(sectionEl, g_data, {readOnly: true})` (read-only). No call to `page_render()` or any type renderer remains in any case view JS.
FR-37.8: `de-identified/index.js` uses the same static HTML sections with `formBinder.bind(..., {readOnly: true})`. `case/search_view.js` rendering that uses `g_default_ui_specification` for field display is replaced with position-independent rendering (field prompt + value text) that does not depend on layout coordinates.
FR-37.9: The `/form-designer` route, `form_designerController.cs`, `Views/form_designer/Index.cshtml`, all JS in `wwwroot/scripts/form-designer/`, and all assets in `wwwroot/form-designer/` are deleted. The single nav link `<li><a href="/form-designer">Open form designer</a></li>` in `Views/Home/Index.cshtml` is removed. The `form_designer` role and its authorization policy in `Program.cs` are **not removed** — the role gates access to the entire Form Designer admin section (Metadata Management, De-Identified List, Data Migration, Substance Lists, Session Logs, and more).
FR-37.10: The POST `/api/ui_specification/{id}` action is removed from `ui_specificationController.cs`. The POST `/api/metadata` action is removed from `metadataController.cs`. GET actions on both controllers are retained until Story 37.6 confirms no consumers remain, then `ui_specificationController.cs` is deleted entirely.
FR-37.11: `page_renderer.js`, `page_renderer.committee_member.js`, and all 30 files in `wwwroot/scripts/editor/page_renderer/` are deleted. `g_default_ui_specification`, the `/api/version/{ver}/ui_specification` fetch, and all calls to `page_render()`, `app_render()`, `form_render()`, `group_render()`, `grid_render()`, and individual type renderer functions are removed from `case/index.js`, `de-identified/index.js`, and `case/search_view.js`. `ui_specificationController.cs` is deleted.

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
- FR-34 defect scope: fix PDF interpretation in `wwwroot/scripts/pdf-version/index.js` only unless implementation evidence proves otherwise; do not modify Trumbowyg save output or stored narrative HTML.
- FR-34 evidence: use `docs/ai/local/case-narrative-spacing/changed-prod-data-v4.1.txt` and `docs/ai/local/case-narrative-spacing/unchanged-prod-data.txt` as regression fixtures or manual verification inputs.
- FR-34 parser guard: preserve meaningful inline spaces and NBSP while ignoring structural whitespace-only nodes produced by edited one-line HTML between block tags.
- FR-34 QA follow-up evidence: use `docs/ai/local/case-narrative-spacing/qa/html.txt` as the regression fixture for Story 34.2; it contains repeated `<br>` plus empty-paragraph separators that were not covered by Story 34.1.
- FR-34.5 implementation: Split `pastedText` by `/\r?\n/`; non-empty lines → `createElement('p')` with `textContent`; empty lines → `createElement('p')` with `innerHTML = '<br>'`. Use the block-safe sibling insertion path (same as `fragmentHasBlocks` path) — do NOT use `range.insertNode` for multi-paragraph plain text, and do NOT insert `<br>` tags as line separators.
- FR-34.6 implementation: Attach a `keydown` listener on the editor element within the same init block as the paste handler in `attach_narrative_paste_handler`. Match the current paragraph's `textContent` against `/^(\d+)\.\s\S/` before firing. If the cursor is at the end of the paragraph and the pattern matches with real content after the prefix, call `preventDefault()`, create a new `<p>` with text `(n+1) + '. '`, insert it after the current paragraph, and move the cursor to the end. Do NOT convert paragraphs to `<ol>/<li>` structure.
- FR-35 origin: Rel 4.1 P-Immediate change request from Katrina's feedback, tracked as ADO #119294 (Marta Puskarz). Ships before Rel 4.1 deployment.
- FR-35 scope decisions confirmed with the user (2026-08-03): (1) hide only — do not delete `OfflineExitManager`, the exit-offline confirmation modal, or the server cleanup calls; (2) rename "Go Online" everywhere, including the confirmation modal in `offline-modals.js`, not just the two primary buttons; (3) also fix the confirmation-modal inconsistency between the two "Go Online" entry points as part of this epic.
- FR-35.1 host locations: `Views/Home/Index.cshtml`, `Views/Account/OfflineLogin.cshtml`, `Views/Shared/_BreadCrumbs.cshtml` (all render `[data-offline-exit-host]` divs populated by `offline-exit-manager.js`).
- FR-35.2 rename sites: `wwwroot/scripts/editor/page_renderer/app.mmria.js` (2 static button labels), `wwwroot/scripts/offline/offline-transition-manager.js` (`set_go_online_button_state`), `wwwroot/scripts/offline/offline-network-monitor.js` (connectivity-driven state text), `wwwroot/scripts/offline/offline-modals.js` (`show_go_online_modal` title + confirm button). The busy-state text `'Going Online...'` is unchanged unless the user requests otherwise at implementation time.
- FR-35.3: `app.mmria.js` line ~728 (`show_go_online_modal(event)`) already goes through the confirmation modal; line ~790 (`go_online_clicked(event)` called directly) bypasses it. Align the second entry point to route through the same confirmation modal as the first, rather than removing confirmation from the first.

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
FR-8.8: Epic 8 Story 8.5 - SAMS-aware offline entry points (SignIn/Login/Logout guard; AppOffline redirect)
FR-8.9: Epic 8 Story 8.5 - Dedicated AppOffline page + anonymous /api/account/offline-status endpoint
FR-8.10: Epic 8 Story 8.6 - Precision offline detection (setTimeout at exact offline_date/warn_date)
FR-8.11: Epic 8 Story 8.6 - Countdown/OK re-check and date-change recovery UX
FR-8.12: Epic 8 Story 8.6 - mmria-services resilience (\_lastKnownConfig fallback; assume online on no data)
FR-8.13: Epic 8 Story 8.6 - Page-refresh redirect to AppOffline (bypasses localStorage modal gate)FR-9.1: Standalone Bug Fix â€” Data Summary Checks "ALL" toggle scoped to selected Form
FR-10.1: Standalone Bug Fix â€” Manage Users Export scoped to active filter
FR-11.1: Epic 10 â€” Fix BatchSupervisor busy-wait CPU spin (mmria-services)
FR-11.2: Epic 10 â€” Server-side CVS structured error handling (CVSManager, CVSDAL, CVSModels, cvsAPIController)
FR-11.3: Epic 10 â€” Client-side CVS retry loop with countdown and try-again button
FR-11.4: Epic 10 â€” BroadcastChannel CVS status and parent-page button state (mmria.js)

FR-29.1: Epic 29 — Server-side record ID format validation and uniqueness guard in SaveCaseAsync
FR-29.2: Epic 29 — Client-side per-candidate uniqueness check via /api/record_id before case save
FR-29.3: Epic 29 — Add record_id_list CouchDB view and remove broken bulk-list dependency from case creation flow
FR-29.4: Epic 29 — Extract `CaseManager.GenerateUniqueRecordIdAsync` primitive and expose `error_code` on collision/format rejections
FR-29.5: Epic 29 — Online case creation collapses pre-flight loop into save-then-retry-on-collision (Path A)
FR-29.6: Epic 29 — Offline placeholder record IDs (`STATE-OFFLINE-CASE-XX`) with server-generated real ID at sync (Path B)
FR-29.7: Epic 29 — IJE batch import routes writes through `SaveCaseAsync` with collision-retry (Path C)

FR-31.1: Epic 31 — CSS :focus-visible outline for Informant Interview Summary Template button (#view-informant-interview-summary-template-button)
FR-31.2: Epic 31 — CSS :focus-visible outline for CDF Template PDF button (#view-cdf-template-button)

FR-33.1: Epic 33 — Metadata-aware numeric generation and plausible numeric ranges
FR-33.2: Epic 33 — Date group validity and core timeline plausibility
FR-33.3: Epic 33 — Recursive validation gate before JSON output or CouchDB save
FR-33.4: Epic 33 — Focused generator regression tests for date and number fields
FR-34.1: Epic 34 — Ignore structural whitespace-only text nodes in case narrative PDF conversion
FR-34.2: Epic 34 — Render empty Trumbowyg paragraphs as one intentional blank line
FR-34.3: Epic 34 — Preserve stored narrative HTML and constrain fix to PDF conversion
FR-34.4: Epic 34 — Collapse BR-plus-empty-paragraph section separators in case narrative PDF conversion
FR-34.5: Epic 34 — Plain-text paste newline-to-paragraph conversion in the narrative editor
FR-34.6: Epic 34 — Numbered-paragraph Enter continuation in the narrative editor

FR-35.1: Epic 35 — Hide 'Exit Offline Mode' widget while offline (code preserved, not deleted)
FR-35.2: Epic 35 — Rename 'Go Online' to 'Go Online & Sync Changes' everywhere, including the confirmation modal
FR-35.3: Epic 35 — Align the two 'Go Online & Sync Changes' entry points to both confirm via modal before syncing

FR-36.1: Epic 36 — Reconcile false-positive suppression: distinguish already-committed items from genuine mismatch
FR-36.2: Epic 36 — Change stack snapshot deduplication on enqueue (optional optimization)
FR-36.3: Epic 36 — Rev poll generation guard verification and regression test during transient network outage
FR-36.4: Epic 36 — Autosave-drop awaited-save data carry-through verification

FR-37.1: Epic 37 — HTML generator tool: reads metadata + ui_specification, outputs 14 static HTML section fragments
FR-37.2: Epic 37 — Generated HTML attribute contract: data-path, data-field-type, data-show-when, inline position styles
FR-37.3: Epic 37 — Grid and multi-cardinality form template patterns in generated HTML
FR-37.4: Epic 37 — Generator diagnostic: unmapped path logging; chart field placeholder delegation
FR-37.5: Epic 37 — form-binder.js: bind(), collect(), readOnly mode
FR-37.6: Epic 37 — form-binder.js: delegated change listener, dependent list re-evaluation, show-when re-evaluation
FR-37.7: Epic 37 — Wire Case editor view: static HTML sections + formBinder, remove page_render() calls
FR-37.8: Epic 37 — Wire read-only views (AnalystCase, CaseVRO, de-identified, search_view)
FR-37.9: Epic 37 — Remove /form-designer route, controller, views, JS assets, CSS assets, auth policy
FR-37.10: Epic 37 — Remove form designer write endpoints (POST ui_specification, POST metadata)
FR-37.11: Epic 37 — Remove page_renderer.js + 30 type renderers, g_default_ui_specification, ui_specificationController

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

Installation administrators can schedule a planned system outage. Logged-in users receive advance warning, are guided to save their work and sign out before the system goes offline, and are prevented from logging in once the offline date is reached. SAMS-enabled deployments redirect to AppOffline during an outage. Offline detection fires at the exact scheduled time and is resilient to mmria-services downtime.
**FRs covered:** FR-8.1 through FR-8.13
**Stories:** 8.1 — Config/services/admin page, 8.2 — Login offline state, 8.3 — Warn/offline modals, 8.4 — Periodic poll, 8.5 — SAMS-aware offline entry points, 8.6 — Precision detection and resilience

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

### Epic 23: Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)

The final SQL migration foundation epic. Every CouchDB database access in `mmria-server`, `mmria.common`, and `mmria.services` is placed behind a repository interface. Six databases not covered by Epics 17–21 (`session`, `offline_cases`, `export_queue`, `vital_import`, `report`, `logging`) receive interfaces and canonical DAL implementations. After this epic, a SQL migration requires only swapping DAL implementations — no manager, controller, or services actor changes are needed.
**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness.

### Epic 24: Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)

The eight files classified "infra out-of-scope" across Epics 17–23 — `c_db_setup.cs`, `Rebuild_Export_Queue.cs`, `rebuild_export_queue_job.cs`, `Process_Central_Pull_list.cs`, `Process_DB_Synchronization_Set.cs`, `c_document_sync_all*.cs`, `c_document_sync_all_legacy.cs`, and `c_sync_document.pmss.cs` — contain ~150+ direct CouchDB HTTP calls. This epic routes every one of them through typed repository interfaces without restructuring any orchestration logic. New interfaces introduced: `IDeIdentifiedRepository`, write extensions to `IReportRepository`, sync-oriented extensions to `ICaseRepository`, `IDatabaseLifecycleService`, and a `PurgeAndReinitializeAsync` extension to `IExportQueueRepository`. After this epic, every CouchDB call in the entire codebase routes through an interface — completing SQL migration readiness.
**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness. Lift-and-shift: orchestration logic, actor hierarchies, Quartz schedules, and rebuild pipelines are not restructured.
**Stories:** 23.1 — Remaining Database Gap Scan, 23.2 — `ISessionRepository`, 23.3 — `IOfflineCaseRepository`, 23.4 — `IExportQueueRepository`, 23.5 — `IVitalImportRepository`, 23.6 — `IReportRepository` + `ReportDAL`, 23.7 — Route report reads, 23.8 — `ILoggingRepository` + `LoggingDAL`

### Epic 25: Async Safety + Metadata Reader Consolidation

Two fast-payoff passes that eliminate a production deadlock risk and reduce direct CouchDB call count by ~12 in a single mechanical injection. Story 25.1 fixes two files that call `CouchDbHttpClient.ExecuteAsync(...).Result` (synchronous blocking in an async context) — a thread-pool deadlock risk under load on ASP.NET. Story 25.2 injects `IMetadataRepository` (established in Epic 20) into the six transform-helper classes — in both `mmria-server/util/` and `mmria.common/SharedLibraries/MMRIARebuild/Manager/` — that still read metadata directly.
**Architecture rule:** project-context.md §2.2 SharedLibraries pattern
**Stories:** 25.1 — Fix `.Result` blocking calls, 25.2 — Metadata reader `IMetadataRepository` injection pass

### Epic 26: Controller API Direct-Call Remediation

Four stories completing the controller migration started in Epics 17–21. The fifteen controllers and utility files that still call `CouchDbHttpClient.ExecuteAsync` directly are grouped by repository and addressed in waves: Case API controllers (26.1), Auth and Session controllers (26.2), Export and Broadcast controllers (26.3), Jurisdiction and Summary utilities (26.4). Each story injects an already-existing repository interface — no new interfaces are introduced in this epic.
**Architecture rule:** project-context.md §2.2 SharedLibraries pattern
**Stories:** 26.1 — Case API controllers, 26.2 — Auth/Session controllers, 26.3 — Export/Broadcast controllers, 26.4 — Jurisdiction/Summary utilities

### Epic 27: Services Utility Repository Activation

The null-fallback scaffolding placed in `exporter.cs`, `mmrds_exporter.cs`, and `core_element_exporter.cs` during Epic 24 (Stories 24.10–24.11) is activated by wiring real repository instances from actor supervisors down through the export-utility pipeline. Story 27.2 classifies the remaining `BatchProcessor.cs` DELETE call and formally documents the `Process_Migrate_*` actors as intentional direct-access paths excluded from the repository pattern by design.
**Architecture rule:** project-context.md §2.2 SharedLibraries pattern
**Stories:** 27.1 — Activate export-utility repository wiring, 27.2 — BatchProcessor assessment + migration actor classification

### Epic 29: Record ID Uniqueness Enforcement

Abstractors creating new cases are protected against duplicate MMRIA Record IDs (`{jurisdiction}-{year-of-death}-{4-digit-number}`) by a defense-in-depth strategy. The server rejects any new-case save where the record ID already exists in the database. The client verifies uniqueness per-candidate against the server before saving, eliminating the TOCTOU race condition. The broken bulk-list CouchDB view dependency is removed from the case creation flow and a functioning design-document view is added in its place.

A 2026-08-19 audit uncovered three follow-on defects that Stories 29.1–29.3 did not address: (a) a bug in `CaseDAL.RecordIdExistsAsync` was causing every uniqueness check to falsely return "unique" — hot-fixed as part of Story 29.1 verification; (b) the online client's 20-call pre-flight loop leaves a residual race between the last check and the save; (c) offline case creation still writes a `STATE-YEAR-NNNN-OFFLINE` record ID that is stripped and blindly written at sync time, with no collision recovery; and (d) IJE batch imports bypass `SaveCaseAsync` entirely, relying on a stale 25 000-row HashSet fetched once per batch and silently allowing duplicates when the DB is larger than 25 k rows or another writer wins a race. Stories 29.4–29.7 remediate these by extracting the "generate a unique record ID" primitive onto `CaseManager` and routing all three creation paths (online UI, offline sync, IJE batch) through a save-then-retry-on-collision pattern.

**FRs covered:** FR-29.1, FR-29.2, FR-29.3, FR-29.4, FR-29.5, FR-29.6, FR-29.7
**Stories:** 29.1 — Server-side format validation and uniqueness guard, 29.2 — Client-side per-candidate API check, 29.3 — Add record_id_list view and remove broken bulk-list call, 29.4 — `GenerateUniqueRecordIdAsync` primitive + structured `error_code`, 29.5 — Path A save-then-retry-on-collision, 29.6 — Path B offline placeholder record IDs, 29.7 — Path C IJE batch collision-retry

### Epic 31: Section 508 — Home Page General Section Keyboard Focus Indicators

Two `btn-link`-styled buttons in the Home page General section lack a visible keyboard focus indicator when reached via keyboard navigation. A targeted CSS `:focus-visible` rule is added to `index.scss` for both elements by ID, providing a high-contrast outline that satisfies Section 508 and WCAG 2.1 SC 2.4.7 (Focus Visible) requirements. CSS-only change — no server-side or JavaScript modifications required.
**FRs covered:** FR-31.1, FR-31.2
**Stories:** 31.1 — Add `:focus-visible` outline styles to General section buttons in `index.scss`

### Epic 32: Export Consistency — Date Format, De-identification Parity, and Hospital Code Normalization

De-identified CSV exports produced from any MMRIA tenant are byte-consistent in date formatting, PII suppression, and coded-field rendering, regardless of which environment triggers the export. Closes three classes of compliance and data-quality risk identified by comparing FL production and T1 local de-identified exports.
**Story 32.2 CLOSED** — Global de-id list updated (86 paths matching FL production). Eliminated 1,024 field differences; `certificate_infant_fetal_section.csv` and `data-dictionary.csv` now byte-for-byte identical with FL.
**Remaining:** 32.1 — Normalize datetime serialization in exporter, 32.3 — Investigate hospital paternity field code discrepancy

### Epic 33: Case Generator Date and Number Plausibility

Generated test cases from `mmria-case-generator` should remain broad enough for regression coverage while avoiding obviously invalid dates and non-numeric or implausible number values. This epic tightens the existing metadata-driven generator with targeted date/number improvements, recursive validation, and focused tests, without redesigning the utility or changing production metadata.
**FRs covered:** FR-33.1, FR-33.2, FR-33.3, FR-33.4
**Stories:** 33.1 — Metadata-aware numeric generation, 33.2 — Date and timeline plausibility, 33.3 — Recursive validation gate, 33.4 — Regression coverage

### Epic 34: Case Narrative PDF Spacing Fidelity

The case narrative PDF export renders edited rich-text narrative HTML without adding extra vertical spacing between paragraphs. The fix is constrained to PDF HTML conversion so the editor's stored Trumbowyg HTML remains unchanged.
**FRs covered:** FR-34.1, FR-34.2, FR-34.3, FR-34.4
**Stories:** 34.1 — Normalize case narrative PDF whitespace conversion, 34.2 — Collapse BR-plus-empty-paragraph separators

### Epic 35: Offline Exit/Go Online UX Cleanup (Rel 4.1 P-Immediate)

While a user is in offline mode, the confusing "Exit Offline Mode" escape hatch is hidden (not deleted) so it can be revisited in a future release. The "Go Online" action is renamed everywhere to "Go Online & Sync Changes" for clarity, and both entry points to that action now consistently confirm via modal before syncing.
**FRs covered:** FR-35.1, FR-35.2, FR-35.3
**Stories:** 35.1 — Hide Exit Offline Mode widget, 35.2 — Rename Go Online to Go Online & Sync Changes, 35.3 — Align Go Online confirmation behavior

### Epic 36: Case Save Queue Reconcile — Idle Network Recovery Fix

When a case is left idle during a transient network disruption, the autosave retry and the inactivity-triggered save (either the "Continue Editing" path after the warning threshold, or the lock-release path after the lock threshold) can both capture identical `g_change_stack` snapshots. When the autosave eventually succeeds and clears those items, the subsequent inactivity save's post-completion reconcile emits a false-positive warning implying edits were not saved. No data loss occurs, but the warning creates user and developer confusion. This epic eliminates the false-positive, verifies the rev poll generation guard handles network recovery correctly, and confirms awaited-save data carry-through when a retrying autosave is dropped.
**FRs covered:** FR-36.1, FR-36.2, FR-36.3, FR-36.4
**Stories:** 36.1 — Fix false-positive reconcile warning in `mmria_reconcile_live_save_state_after_success`, 36.2 — Verify rev poll generation guard during transient network outage, 36.3 — Verify autosave-drop data carry-through when inactivity save is awaited, 36.4 (optional) — Change stack snapshot deduplication on enqueue

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

### Story 8.5: SAMS-Aware App Offline Entry Points

As a user whose only login path is SAMS,
I want to see a clear offline page instead of being redirected to the SAMS login service,
So that I understand the system is unavailable without being bounced to an external identity provider.

**Acceptance Criteria:**

**Given** `sams_is_enabled = true` and `now >= offline_date`
**When** any unauthenticated request reaches `GET /Account/SignIn`
**Then** the server checks the offline state before building the SAMS redirect URL; if offline for this tenant, the user is redirected to `/Account/AppOffline` instead of SAMS

**Given** `sams_is_enabled = true` and `now >= offline_date`
**When** a user's auto-logout countdown completes and `POST /Account/Logout` runs
**Then** the logout action checks offline state before issuing the SAMS logout redirect; if offline, the user is sent to `/Account/AppOffline` instead of `sams:logout_url`

**Given** `sams_is_enabled = true` and `now >= offline_date`
**When** a user navigates directly to `GET /Account/Login`
**Then** the server checks offline state first; if offline, redirects to `/Account/AppOffline`; if not offline, redirects to `/Account/SignIn` (SAMS)

**Given** a dedicated `/Account/AppOffline` page (`[AllowAnonymous]`)
**When** the page is rendered
**Then** it shows `offline_page_message` using the same purple-panel styling as the Login page offline state; CDC conditions-of-use footer is included

**Given** the server reaches `/Account/AppOffline` but the app is no longer offline (admin cleared dates)
**When** the action evaluates offline state
**Then** it immediately redirects to `/Account/AutoLogin`

**Given** a new anonymous API endpoint `GET /api/account/offline-status`
**When** called without authentication
**Then** it returns `{ "is_offline": bool }` for this tenant; used by the AppOffline page polling loop

---

### Story 8.6: Offline Detection Precision, Resilience, and Recovery UX

As a logged-in user,
I want the offline modal to fire at exactly the scheduled offline time and the system to handle mmria-services outages gracefully,
So that the offline transition is predictable and a services restart does not cause false sign-outs.

**Acceptance Criteria:**

**Given** the client receives a config with a future `offline_date` or `warn_date`
**When** `runOfflineCheck` stores the config
**Then** a `setTimeout` fires at exactly `new Date(offline_date) - Date.now()` ms; a separate `setTimeout` fires at `warn_date` if future

**Given** the 2-minute poll returns an updated config with a changed `offline_date`
**When** the poll handler runs
**Then** any previously scheduled precision `setTimeout` is cleared and rescheduled for the new date

**Given** a user is on `/Account/AppOffline`
**When** the page loads or the user refreshes
**Then** the page immediately calls `/api/account/offline-status`; if `is_offline: false`, redirects to `/Account/AutoLogin`; otherwise starts a 30-second polling loop

**Given** the offline modal countdown reaches zero OR the user clicks OK
**When** the re-check fires before sign-out
**Then** (a) still offline: sign out proceeds; (b) date pushed to future: modal closes, warn modal re-shown, precision timer rescheduled; (c) dates cleared: modal closes silently

**Given** the status fetch fails (mmria-services down) during countdown or OK handling
**When** the failure is caught
**Then** the handler uses `_lastKnownConfig` (last successful fetch); if that indicates offline, sign out proceeds; if online or no prior data, logout is cancelled and user remains in session

**Given** a logged-in user refreshes any page while `now >= offline_date`
**When** the `_LayoutBase` initial status check returns `state: offline`
**Then** the browser immediately redirects to `/Account/AppOffline` -- the localStorage modal gate is not consulted for page-load/refresh

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

| Wave | Story                                                 | Risk   | Dependencies                  |
| ---- | ----------------------------------------------------- | ------ | ----------------------------- |
| 17   | 17.1 — mmrds Operation Catalog                        | None   | None — discovery only         |
| 17   | 17.7 — Boundary Decision                              | None   | Can run in parallel with 17.1 |
| 17   | 17.2 — ICaseRepository + CaseDAL                      | Low    | 17.1                          |
| 17   | 17.3 — CaseManager direct calls                       | Medium | 17.2                          |
| 17   | 17.4 — CaseWorkflowAdminDAL                           | Low    | 17.2                          |
| 17   | 17.5 — AuditRecovery / CVS / VitalImport / Attachment | Low    | 17.2                          |
| 17   | 17.5b — mmria.services                                | Medium | 17.2                          |
| 17   | 17.6 — OfflineCaseManager                             | Medium | 17.2                          |

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

| Database        | Files with direct calls | Total hits | Already in DAL/Manager                           | Out-of-DAL leakage                                                                                    | Infra/out-of-scope                            |
| --------------- | ----------------------- | ---------- | ------------------------------------------------ | ----------------------------------------------------------------------------------------------------- | --------------------------------------------- |
| `_users`        | 9                       | 14         | `AccountDAL`, `AccountManager`, `ManageUsersDAL` | `AccountController.OIDC.cs`, `passwordChangeController.cs`, `JurisdictionSummary.cs`, `VROSummary.cs` | `c_db_setup.cs`, `Check_DB_Install.cs`        |
| `configuration` | 3                       | 12         | none                                             | `_config.cs`, `MMRIAServicesDAL.cs`                                                                   | `MultiTenantConfigurationLoader.cs` (startup) |

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

| Wave | Story                                                                        | Risk       | Dependencies                            |
| ---- | ---------------------------------------------------------------------------- | ---------- | --------------------------------------- |
| 18   | 18.1 — `_users` Operation Catalog                                            | None       | None — discovery only                   |
| 18   | 18.2 — `IUserRepository` + `AccountDAL`                                      | Low        | 18.1                                    |
| 18   | 18.3 — Route leaking `_users` calls                                          | Low–Medium | 18.2                                    |
| 18   | 18.4 — `IConfigurationRepository` + `SystemConfigDAL`                        | Low        | 18.1                                    |
| 18   | 18.5 — `IConfigurationBootstrapLoader` over `MultiTenantConfigurationLoader` | Low        | None — independent of all other stories |

18.3 and 18.4 can proceed in parallel once 18.2 is complete. 18.4 and 18.5 have no dependency on 18.2 and can be done at any time.

---

## Epic 19: `jurisdiction` Consolidation (SQL Migration Foundation)

All CouchDB reads and writes against the `jurisdiction` database are consolidated behind two distinct interfaces: `IJurisdictionRepository` for application CRUD, and `IJurisdictionAuthorizationReader` for the per-request authorization query. After this epic, migrating the jurisdiction database to SQL requires changing only the two DAL implementations.

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness.

**Scope summary (verified 2026-07-14):**

| Category                      | Files                                                                                                                                                                                                                             | Hits | Notes                                                  |
| ----------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---- | ------------------------------------------------------ |
| Already in a DAL/Manager      | `ManageUsersDAL`, `ManageUsersManager`, `SessionDAL`                                                                                                                                                                              | 13   | Partial coverage — DAL methods exist but no interface  |
| Application CRUD (out-of-DAL) | `jurisdiction_treeController`, `vitalsController`, `_usersController`, `CaseViewManager`, `CaseViewSearch.pmss`, `JurisdictionSummary`, `VROSummary`                                                                              | 15   | Mix of controllers, managers, and actors               |
| Auth middleware (hot path)    | `authorization.cs`, `authorization_case.cs`, `authorization_user.cs`, `authorization.pmss.cs`, `authorization_case.pmss.cs`, `authorization_user.pmss.cs`, `AuthorizationRoleCache.cs`, `JurisdictionAuthorizationRequirement.cs` | 11   | **Special concern** — runs on every authorized request |
| Infra/out-of-scope            | `c_db_setup.cs`                                                                                                                                                                                                                   | 5    | DB setup only                                          |

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

| Wave | Story                                                       | Risk       | Dependencies          |
| ---- | ----------------------------------------------------------- | ---------- | --------------------- |
| 19   | 19.1 — `jurisdiction` Operation Catalog                     | None       | None — discovery only |
| 19   | 19.2 — `IJurisdictionRepository` + `JurisdictionDAL`        | Low–Medium | 19.1                  |
| 19   | 19.3 — `IJurisdictionAuthorizationReader` (auth middleware) | Medium     | 19.1                  |
| 19   | 19.4 — Route out-of-DAL application CRUD                    | Low–Medium | 19.2                  |

19.2 and 19.3 can proceed in parallel after 19.1. 19.4 depends on 19.2. Story 19.3 is independent of 19.2 — the auth reader DAL and the CRUD DAL are separate classes.

---

## Epic 20: `metadata` Consolidation (SQL Migration Foundation)

All CouchDB reads and writes against the `metadata` database are consolidated behind an `IMetadataRepository` interface in `mmria.common`. The existing `MetadataVersionDAL` is the canonical implementation; the 25 files that currently bypass it are routed through the interface. After this epic, migrating the metadata database to SQL requires changing only `MetadataVersionDAL`.

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness.

**Scope summary (verified 2026-07-14):**

| Category                                      | Files                                                                                                                                                                                                                                                                                                                                                                                                       | Hits | Notes                                                                                   |
| --------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---- | --------------------------------------------------------------------------------------- |
| `MetadataVersionManager` (already DAL-backed) | `MetadataVersionManager.cs`                                                                                                                                                                                                                                                                                                                                                                                 | 22   | Canonical owner — but builds URLs directly in manager, not all through DAL              |
| Controllers bypassing DAL                     | `broadcast_messageController`, `de_identified_listController`, `export_list_managerController`, `substance_mappingController`, `abstractorDeidentifiedCaseController`, `CaseController`, `versionController`, `record_idController`, `systemOfflineController`                                                                                                                                              | ~14  | Mix of planned and unplanned Wave targets                                               |
| SharedLibraries bypassing DAL                 | `AuditRecoveryDAL`, `CaseValidationDAL`, `MMRIAServicesDAL`                                                                                                                                                                                                                                                                                                                                                 | ~8   | Within common — still bypass the canonical DAL                                          |
| Services actors/exporters                     | `c_convert_to_report_object`, `c_convert_to_opioid_report_object`, `c_convert_to_dqr_detail`, `c_de_identifier`, `c_cdc_de_identifier`, `c_document_sync_all`, `c_document_sync_all_legacy`, `c_generate_frequency_summary_report`, `c_sync_document`, `BatchItemProcessingService`, `core_element_exporter`, `exporter`, `mmrds_exporter`, `export_all_generate_name_map`, `PopulateCDCInstanceSupervisor` | ~39  | Mostly read-only: `GET version_specification-{v}/metadata` and `GET de-identified-list` |
| Infra/out-of-scope                            | `c_db_setup.cs`, `Process_Migrate_*`                                                                                                                                                                                                                                                                                                                                                                        | ~15  | DB setup and one-time migration scripts                                                 |

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

| Wave | Story                                               | Risk       | Dependencies                  |
| ---- | --------------------------------------------------- | ---------- | ----------------------------- |
| 20   | 20.1 — `metadata` Operation Catalog                 | None       | None — discovery only         |
| 20   | 20.6 — Boundary Decision                            | None       | Can run in parallel with 20.1 |
| 20   | 20.2 — `IMetadataRepository` + `MetadataVersionDAL` | Low–Medium | 20.1                          |
| 20   | 20.3 — SharedLibraries DAL files                    | Low        | 20.2                          |
| 20   | 20.4 — Controller direct calls                      | Low–Medium | 20.2                          |
| 20   | 20.5 — `mmria.services` read-only calls             | Medium     | 20.2                          |

20.3, 20.4, 20.5, and 20.6 can proceed in parallel once 20.2 is complete.

---

## Epic 21: `audit` Consolidation (SQL Migration Foundation)

All CouchDB reads and writes against the `audit` database are consolidated behind a single `IAuditRepository` interface implemented by a new canonical `AuditDAL`. The 19 in-scope call sites currently scattered across controllers, managers, and DAL files are routed through the interface. After this epic, migrating the audit database to SQL requires changing only `AuditDAL` — no manager, controller, or workflow-admin code changes are needed.

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness.

**Scope summary (verified 2026-07-15):**

| Location                        | # Calls | Layer            | URL Pattern     | Notes                                                             |
| ------------------------------- | ------- | ---------------- | --------------- | ----------------------------------------------------------------- |
| `AuditRecoveryDAL.cs`           | 3       | DAL ✓            | **A** (wrong)   | GET by ID, GET audit-manage-user, PUT audit-manage-user           |
| `CaseWorkflowAdminDAL.cs`       | 4       | DAL ✓            | **B** (correct) | WriteAuditEntry, GetDeletedCasesView, GetAuditDoc, DeleteAuditDoc |
| `ManageUsersDAL.cs`             | 1       | DAL ✓            | **A** (wrong)   | GET audit-manage-user (duplicate of AuditRecoveryDAL)             |
| `CaseManager.cs`                | 6       | **Manager ✗**    | **B** (correct) | All audit PUT (Change_Stack writes) — wrong layer                 |
| `AuditRecoveryManager.cs`       | 1       | **Manager ✗**    | **A** (wrong)   | Builds `_find` URL directly in manager                            |
| `_auditController.cs`           | 1       | **Controller ✗** | **A** (wrong)   | `_find` by case_id — wrong layer                                  |
| `AuditRecoverUtilController.cs` | 1       | **Controller ✗** | **A** (wrong)   | `_find` — wrong layer                                             |
| `caseController.pmss.cs`        | 2       | **Controller ✗** | **B** (correct) | Audit PUT (Change_Stack writes) — wrong layer                     |
| `c_db_setup.cs`                 | 5       | Infra            | —               | DB setup/security — **out of scope**                              |

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

| Wave | Story                                                           | Risk       | Dependencies                       |
| ---- | --------------------------------------------------------------- | ---------- | ---------------------------------- |
| 21   | 21.1 — `audit` Operation Catalog                                | None       | None — discovery only              |
| 21   | 21.2 — `AuditDAL` + `IAuditRepository`                          | Low        | 21.1                               |
| 21   | 21.3 — CaseManager audit writes                                 | Low        | 21.2                               |
| 21   | 21.5 — Controller-level audit calls                             | Low–Medium | 21.2                               |
| 21   | 21.6 — ManageUsersDAL + AuditRecoveryDAL + AuditRecoveryManager | Low        | 21.2                               |
| 21   | 21.4 — CaseWorkflowAdminDAL audit calls                         | Low        | 21.2 **+ Epic 17 Story 17.4 done** |

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

| Package                                                               | Current Version | Risk Notes                                               |
| --------------------------------------------------------------------- | --------------- | -------------------------------------------------------- |
| `Akka` / `Akka.Hosting` / `Akka.Cluster` / `Akka.DependencyInjection` | 1.5.52          | Check NuGet for .NET 10 TFM support                      |
| `Akka.Quartz.Actor`                                                   | 1.5.13          | Transitively depends on Quartz 3.x; verify compatibility |
| `Akka.DI.Core` / `Akka.DI.Extensions.DependencyInjection`             | 1.4.51 / 1.4.22 | Older release train; may not declare net10.0 support     |
| `Quartz`                                                              | 3.13.1          | Check for .NET 10 support                                |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson`                             | 9.0.0           | Must be updated to 10.0.x                                |
| `Microsoft.Extensions.Http`                                           | 9.0.0           | Must be updated to 10.0.x                                |
| `Serilog.Extensions.Logging`                                          | 9.0.0           | Check for 10.0.x release                                 |
| `System.Text.Encoding.CodePages`                                      | 9.0.0           | Likely in-box for .NET 10; confirm                       |
| `Microsoft.CodeAnalysis.CSharp`                                       | 4.12.0          | Verify .NET 10 compiler support                          |
| `NJsonSchema` / `NJsonSchema.CodeGeneration.CSharp`                   | 11.0.2          | Check for compatibility                                  |
| `FastExcel`                                                           | 3.0.13          | Low risk (no framework coupling)                         |
| `SharpZipLib`                                                         | 1.4.2           | Low risk                                                 |
| `TinyCsvParser`                                                       | 2.7.1           | Low risk                                                 |
| `Newtonsoft.Json`                                                     | 13.0.3          | Low risk (framework-agnostic)                            |

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

#### Story 22.2 — Bugs Discovered and Fixed During Testing (2026-07-18)

The following defects were found during the first full local test run after the `.vscode/launch.json` paths were corrected to `net10.0`. All are considered part of the 22.2 execution scope.

| #       | Location                                       | Defect                                                                                                                                                                                                                                                                                                                                                                                                                                                               | Fix Applied                                                                                                                                                |
| ------- | ---------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 22.2-B1 | `.vscode/launch.json`                          | All six `program` paths referenced `net9.0` DLL paths; `.NET 10` builds output to `net10.0`. The debugger launched the stale `net9.0` binary silently, so all breakpoints missed and all code changes since the TFM upgrade went untested locally.                                                                                                                                                                                                                   | All `net9.0` path segments replaced with `net10.0`.                                                                                                        |
| 22.2-B2 | `Controllers/_config.cs`                       | `sams_is_enabled` was read from `configuration["mmria_settings:sams:is_enabled"]` (nested colon path that does not exist in `appsettings.json`) instead of `configuration["mmria_settings:sams_is_enabled"]`. The key resolved to null, `sams_is_enabled` defaulted to `false`, and `OverridableConfiguration.boolean_keys["shared"]["sams:is_enabled"]` was stored as `false` — making `AccountController.use_sams` always `false` regardless of appsettings value. | Corrected key to `mmria_settings:sams_is_enabled`.                                                                                                         |
| 22.2-B3 | `Controllers/AccountController.cs` `GET Login` | The `GET /Account/Login` action had no SAMS guard. The `POST` handler correctly redirected to `SignIn` when `use_sams == true`, but the `GET` rendered the login form unconditionally, allowing SAMS-only deployments to display and submit the password form.                                                                                                                                                                                                       | Added `if (use_sams.HasValue && use_sams.Value) return RedirectToAction("SignIn");` at the top of the `GET` action (offline check first, then SAMS check). |
| 22.2-B4 | `Program.cs`                                   | `builder.Services.AddScoped<IDatabaseLifecycleService, c_db_setup>()` was registered but unresolvable — `c_db_setup` requires `OverridableConfiguration` (not a DI-registered type) and a raw `string` host prefix. .NET 10's stricter `ValidateOnBuild` rejects this at startup. The registration was flagged in a comment as "for architectural documentation only"; actual usage is always direct `new c_db_setup(...)` instantiation.                            | Registration removed. The `IDatabaseLifecycleService` interface and both `new c_db_setup(...)` call sites are unchanged.                                   |

---

| Wave | Story                                           | Risk                  | Dependencies                                  |
| ---- | ----------------------------------------------- | --------------------- | --------------------------------------------- |
| 22   | 22.1 — Compatibility Analysis & Risk Assessment | None — discovery only | None                                          |
| 22   | 22.2 — Upgrade Execution                        | Medium                | 22.1 complete, no blockers in findings report |

22.1 must fully complete and produce a clean findings report before 22.2 begins. The two stories must not run in parallel.

---

## Epic 23: Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)

Epics 17–21 established repository interfaces for `mmrds`, `_users`, `configuration`, `jurisdiction`, `metadata`, and `audit`. Six additional CouchDB databases used by the application have no repository interface. After this epic, every application-layer database call routes through an interface, and a SQL migration requires changing only DAL implementations — no manager, controller, or services actor code changes are needed.

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness.

**Scope summary (verified 2026-07-16):**

| Database        | Existing DAL                                                | Interface | Out-of-DAL application leaks                                                                                                                                                            | Notes                                                                             |
| --------------- | ----------------------------------------------------------- | --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| `session`       | `SessionDAL` ✓                                              | None      | `SessionManager` (2), `AccountController` (1), `AccountController.OIDC` (1), `Post_Session_Actor` (1), `Record_Session_Event` (1), `SessionSummary` (1); `AccountDAL` cross-feature (4) | All SessionDAL URLs are Pattern A — need canonicalization                         |
| `offline_cases` | `OfflineCaseDAL` ✓                                          | None      | `loggerController` (1 view read)                                                                                                                                                        | DAL CRUD uses Pattern A; view queries already Pattern B — need CRUD canonicalized |
| `export_queue`  | `ExportQueueDAL` ✓                                          | None      | `core_element_exporter.cs` (1); Rebuild actors = infra out-of-scope                                                                                                                     | DAL uses Pattern B throughout — no URL fixes needed                               |
| `vital_import`  | `VitalImportDAL` partial (1 op), `MMRIAServicesDAL` (3 ops) | None      | `ije_messageController` (3); `MMRIAServicesDAL` (3) must delegate to canonical DAL                                                                                                      | No prefix separator in URL — special non-tenant config DB                         |
| `report`        | None                                                        | None      | `AggregateReportManager` (1), `InteractiveReportManager` (1), 4 controllers (4)                                                                                                         | Read-only interface only; sync/rebuild actors = infra out-of-scope                |
| `logging`       | None                                                        | None      | `loggerController` (3 reads/writes)                                                                                                                                                     | No SharedLibraries representation at all                                          |

**Infra out-of-scope across all stories:** `c_db_setup.cs`, `Rebuild_Export_Queue.cs`, `rebuild_export_queue_job.cs`, `Process_Central_Pull_list.cs`, `Process_DB_Synchronization_Set.cs`, `c_document_sync_all*.cs`, `c_document_sync_all_legacy.cs`, `c_sync_document.pmss.cs` — these perform DB lifecycle (create/delete/index) and bulk document writes. SQL migration will address these as infrastructure replacement, not application interface substitution.

---

### Story 23.1: Remaining Database Gap Scan

As a developer,
I want a definitive catalog of every operation against the six remaining databases (`session`, `offline_cases`, `export_queue`, `vital_import`, `report`, `logging`) across all three projects,
So that Stories 23.2–23.8 have an agreed-upon, complete operation set and no call sites are missed before any code changes begin.

**Acceptance Criteria:**

**Given** all `.cs` files in `mmria-server`, `mmria.common`, and `mmria.services`
**When** the developer completes the catalog
**Then** `docs/ai/mmrds_operation_catalog.md` gains a section for each of the six databases, listing every distinct operation grouped by: document CRUD (GET/PUT/DELETE by ID), view queries, Mango `_find` queries, list reads (`_all_docs`), and bulk/admin operations

**Given** each catalog entry
**When** the catalog is complete
**Then** each entry records: operation name, calling file(s), URL pattern in use (A, B, or other), response type expected, and layer classification (DAL ✓, Manager ✗, Controller ✗, Actor ✗)

**Given** infra operations (`c_db_setup`, rebuild actors, sync actors, `Rebuild_Export_Queue`)
**When** encountered
**Then** they are listed but marked **out of scope** — DB lifecycle and bulk-write infrastructure do not belong behind application repository interfaces

**Given** the `vital_import` database
**When** cataloged
**Then** the entry notes that URLs use `config.url/vital_import/...` with no prefix separator, and this is intentional (non-tenant special config DB); all callers preserve this pattern

---

### Story 23.2: `ISessionRepository` over `SessionDAL`

As a developer,
I want a single `ISessionRepository` interface over all `session` database operations, with `SessionDAL` as the sole canonical implementation,
So that every caller depends on the interface and a SQL session-store migration requires changing only `SessionDAL`.

**Acceptance Criteria:**

**Given** `SessionDAL` in `mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs` currently uses Pattern A (`$"{dbConfig.url}/{dbConfig.prefix}session/{id}"`) for all CRUD methods (lines ~80, 90, 96, 103, 111)
**When** this story is complete
**Then** all `SessionDAL` methods use `dbConfig.Get_Prefix_DB_Url($"session/{...}")` (Pattern B) throughout — no direct string interpolation remains

**Given** the full operation set in `SessionDAL`
**When** the interface is extracted
**Then** `ISessionRepository` is defined in `mmria.common/SharedLibraries/Session/` with async method signatures matching every `SessionDAL` method; `SessionDAL` implements `ISessionRepository`

**Given** `ISessionRepository` is defined
**When** DI registration is updated in `mmria-server/Program.cs`
**Then** `ISessionRepository` is registered as `SessionDAL` in the service collection

**Given** `SessionManager.cs` has 2 direct `session/` URL constructions (lines ~60, 201 — Pattern A session document writes in the Manager layer)
**When** this story is complete
**Then** each is replaced with the corresponding `ISessionRepository` method; `ISessionRepository` is injected into `SessionManager` via constructor injection

**Given** the following direct `session/` calls outside the Session feature:

- `AccountController.cs` — 1 hit (session document DELETE on logout)
- `AccountController.OIDC.cs` — 1 hit (session document PUT on OIDC login)
- `Post_Session_Actor.cs` — 1 hit (session document PUT via actor)
- `Record_Session_Event.cs` — 1 hit (session event document PUT via actor)
- `SessionSummary.cs` — 1 hit (session view GET for summary page)
  **When** this story is complete
  **Then** each is replaced with the corresponding `ISessionRepository` method; `ISessionRepository` is injected into each class via constructor injection or Akka.NET actor props factory as appropriate

**Given** `AccountDAL.cs` has 4 session-database calls (all already Pattern B — `dbConfig.Get_Prefix_DB_Url($"session/...")`):

- Line ~323: session-event sortable view GET by user ID
- Lines ~374, 403, 431: session document GET, GET, DELETE
  **When** this story is complete
  **Then** `AccountDAL` injects `ISessionRepository` and delegates those 4 calls to it; `AccountDAL` constructs no `session/` URLs directly

**Given** the build after all changes
**When** verified
**Then** `mmria-server`, `mmria.common`, and `mmria.services` all build with zero errors

---

### Story 23.3: `IOfflineCaseRepository` over `OfflineCaseDAL`

As a developer,
I want a single `IOfflineCaseRepository` interface over all `offline_cases` database operations,
So that the offline case path can be migrated to SQL by changing only `OfflineCaseDAL`.

**Acceptance Criteria:**

**Given** `OfflineCaseDAL` in `mmria.common/SharedLibraries/OfflineCase/DAL/OfflineCaseDAL.cs` uses Pattern A for CRUD methods (lines ~46, 55, 197, 206) and Pattern B for view queries (lines ~183, 216)
**When** this story is complete
**Then** all `OfflineCaseDAL` CRUD methods use `dbConfig.Get_Prefix_DB_Url($"offline_cases/{...}")` (Pattern B) — no Pattern A strings remain in the file

**Given** the full operation set in `OfflineCaseDAL`
**When** the interface is extracted
**Then** `IOfflineCaseRepository` is defined in `mmria.common/SharedLibraries/OfflineCase/` with async method signatures matching every `OfflineCaseDAL` method; `OfflineCaseDAL` implements `IOfflineCaseRepository`

**Given** `IOfflineCaseRepository` is defined
**When** DI registration is updated in `mmria-server/Program.cs`
**Then** `IOfflineCaseRepository` is registered as `OfflineCaseDAL` in the service collection

**Given** `loggerController.cs` line ~101 reads `offline_cases/_design/sortable/_view/lightweight-status-only` directly using `dbConfig.Get_Prefix_DB_Url(...)`
**When** this story is complete
**Then** that call is replaced with the corresponding `IOfflineCaseRepository` method; `IOfflineCaseRepository` is injected into `loggerController` via constructor injection

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors

---

### Story 23.4: `IExportQueueRepository` over `ExportQueueDAL`

As a developer,
I want a single `IExportQueueRepository` interface over all `export_queue` database operations,
So that the export queue can be migrated to SQL (or a dedicated job-queue store) by changing only `ExportQueueDAL`.

**Acceptance Criteria:**

**Given** `ExportQueueDAL` in `mmria.common/SharedLibraries/ExportQueue/DAL/ExportQueueDAL.cs` already uses `dbConfig.Get_Prefix_DB_Url(...)` throughout
**When** the interface is extracted
**Then** `IExportQueueRepository` is defined in `mmria.common/SharedLibraries/ExportQueue/` with async method signatures matching every `ExportQueueDAL` method; `ExportQueueDAL` implements `IExportQueueRepository`; no URL changes are required in `ExportQueueDAL`

**Given** `IExportQueueRepository` is defined
**When** DI registration is updated in `mmria-server/Program.cs`
**Then** `IExportQueueRepository` is registered as `ExportQueueDAL` in the service collection

**Given** `core_element_exporter.cs` in `mmria-server/util/` line ~804 reads an export queue document directly (`$"{db_config.url}/{db_config.prefix}export_queue/{item_id}"` — Pattern A)
**When** this story is complete
**Then** that call is replaced with the corresponding `IExportQueueRepository` method; `IExportQueueRepository` is injected

**Given** `Rebuild_Export_Queue.cs` and `rebuild_export_queue_job.cs` perform DROP/CREATE on the export_queue database
**When** evaluated
**Then** they are listed in the catalog as out of scope — DB lifecycle operations are not application CRUD and do not belong behind `IExportQueueRepository`

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors

---

### Story 23.5: Canonicalize `VitalImportDAL` for `vital_import` DB and Extract `IVitalImportRepository`

As a developer,
I want all `vital_import` database operations consolidated in `VitalImportDAL` behind `IVitalImportRepository`,
So that the vital import batch store can be migrated by changing only `VitalImportDAL`.

**Acceptance Criteria:**

**Given** the `vital_import` database is currently accessed in three places:

- `VitalImportDAL.cs` line ~47: `GET vital_import/_all_docs` (1 operation — already in DAL)
- `MMRIAServicesDAL.cs` lines ~118, 141, 156: `GET vital_import/_all_docs`, `PUT vital_import/{batch_id}`, `PUT vital_import/{_id}` (3 operations)
- `ije_messageController.cs` lines ~73, 107, 148: `GET vital_import/_all_docs`, `DELETE vitals_url` (external service), `PUT vitals_url` (external service) — the external `vitals_url` calls are **not** CouchDB and are out of scope
  **When** this story is complete
  **Then** `VitalImportDAL` contains all in-scope `vital_import` CRUD operations: GET all docs, PUT batch document, PUT/DELETE individual document; `IVitalImportRepository` is defined in `mmria.common/SharedLibraries/VitalImport/` with async method signatures for every operation

**Given** the `vital_import` database URL uses no prefix separator (`$"{config.url}/vital_import/..."` — intentional, non-tenant DB)
**When** `VitalImportDAL` methods are written or updated
**Then** all methods preserve this exact URL construction — no `Get_Prefix_DB_Url` is used for the `vital_import` database because it is not a tenant-prefixed database; this is documented as a deliberate exception in the catalog

**Given** `IVitalImportRepository` is defined
**When** DI registration is updated in `mmria-server/Program.cs`
**Then** `IVitalImportRepository` is registered as `VitalImportDAL` in the service collection

**Given** `MMRIAServicesDAL.cs` lines ~118, 141, 156 construct `vital_import` URLs directly
**When** this story is complete
**Then** each is replaced with the corresponding `IVitalImportRepository` method; `IVitalImportRepository` is injected into `MMRIAServicesDAL` via constructor injection

**Given** `ije_messageController.cs` line ~73 constructs `vital_import/_all_docs` directly
**When** this story is complete
**Then** that call is replaced with `IVitalImportRepository.GetAllBatchesAsync(...)`; `IVitalImportRepository` is injected into the controller via constructor injection; the external `vitals_url` POST/DELETE calls at lines ~107 and ~148 are **not changed** — they are external-service calls, not CouchDB operations

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors

---

### Story 23.6: `IReportRepository` + `ReportDAL` (Application Read Interface)

As a developer,
I want a single `IReportRepository` interface over all application-layer `report` database read operations,
So that report query controllers and managers depend on the interface and a SQL migration requires changing only `ReportDAL`.

**Acceptance Criteria:**

**Given** no `Report` SharedLibraries feature exists
**When** this story creates one
**Then** the following structure exists:

```
mmria.common/SharedLibraries/Report/
  IReportRepository.cs
  DAL/
    ReportDAL.cs
```

**Given** the in-scope application read operations from the catalog:

- `GET report/_all_docs?include_docs=true` — used by `AggregateReportManager`
- `GET report/_design/interactive_aggregate_report/_view/indicator_id?...` — used by `InteractiveReportManager`
- `GET report/_design/data_summary_view_report/_view/year_of_death?skip=N&limit=N` — used by `data_summary_viewController`
- `POST report/_find` — used by `dqrReportController`, `overdose_measureController`, `powerbi_measureController`
  **When** `ReportDAL` is created
  **Then** it contains async methods for each: `GetAllReportDocumentsAsync(DBConfigurationDetail dbConfig)`, `GetIndicatorByIdAsync(string indicatorId, DBConfigurationDetail dbConfig)`, `GetDataSummaryViewAsync(int skip, int take, DBConfigurationDetail dbConfig)`, `FindReportDocumentsAsync(string selectorJson, DBConfigurationDetail dbConfig)` — each uses Pattern B via `dbConfig.Get_Prefix_DB_Url($"report/...")`

**Given** the sync/rebuild actors in `mmria-server/util/` and `mmria.common/SharedLibraries/MMRIARebuild/` write to and manage the `report` database
**When** they are evaluated
**Then** a boundary decision is recorded in `docs/ai/mmrds_operation_catalog.md` under the `report` Boundary Decisions section: write/rebuild operations (DROP DB, CREATE DB, bulk PUT report documents, `_index` creation, design document PUT) are declared **infrastructure out-of-scope** — these will be addressed as part of the SQL migration implementation, not as application interface changes; `IReportRepository` covers read operations only

**Given** `IReportRepository` is defined
**When** DI registration is updated in `mmria-server/Program.cs`
**Then** `IReportRepository` is registered as `ReportDAL` in the service collection

**Given** no callers are changed in this story
**When** the build runs
**Then** all three projects build with zero errors

---

### Story 23.7: Route Report Read Calls Through `IReportRepository`

As a developer,
I want all application-layer files that directly construct `report` database read URLs to delegate to `IReportRepository`,
So that no manager or controller constructs a `report/` URL directly.

**Acceptance Criteria:**

**Given** `AggregateReportManager.cs` line ~35 uses `dbConfig.Get_Prefix_DB_Url("report/_all_docs?include_docs=true")` directly in the Manager layer
**When** this story is complete
**Then** that call is replaced with `IReportRepository.GetAllReportDocumentsAsync(dbConfig)`; `IReportRepository` is injected into `AggregateReportManager` via constructor injection

**Given** `InteractiveReportManager.cs` line ~30 constructs `report/_design/interactive_aggregate_report/_view/indicator_id?...` directly using Pattern A
**When** this story is complete
**Then** that call is replaced with `IReportRepository.GetIndicatorByIdAsync(indicatorId, dbConfig)`; `IReportRepository` is injected into `InteractiveReportManager` via constructor injection

**Given** the following controllers with direct `report` URL construction:

- `data_summary_viewController.cs` — 1 hit (view GET — Wave 8 planned migration target)
- `dqrReportController.cs` — 1 hit (`_find` POST)
- `overdose_measureController.cs` — 1 hit (`_find` POST)
- `powerbi_measureController.cs` — 1 hit (`_find` POST)
  **When** this story is complete
  **Then** each is replaced with the corresponding `IReportRepository` method; `IReportRepository` is injected into each controller via constructor injection; no controller constructs a `report/` URL

**Given** `data_summary_viewController` is also a Wave 8 SharedLibraries migration target
**When** this story touches it
**Then** only the URL construction is replaced — the Wave 8 `DataSummary` feature extraction is deferred; this story does not restructure the controller's business logic

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors and no route, action signature, or response shape changes are made

---

### Story 23.8: `ILoggingRepository` + `LoggingDAL`

As a developer,
I want all `logging` database operations consolidated in a new `LoggingDAL` behind `ILoggingRepository`,
So that the logging store can be migrated (SQL, Elasticsearch, or other) by changing only `LoggingDAL`.

**Acceptance Criteria:**

**Given** no `Logging` SharedLibraries feature exists
**When** this story creates one
**Then** the following structure exists:

```
mmria.common/SharedLibraries/Logging/
  ILoggingRepository.cs
  DAL/
    LoggingDAL.cs
```

**Given** the in-scope `logging` database operations in `loggerController.cs`:

- Line ~93: `GET {prefix}logging` — reads the list of logging modules (Pattern A)
- Lines ~283, 653: one is a filtered view read, one is a document write (Pattern A throughout)
  **When** `LoggingDAL` is created
  **Then** it contains async methods for each in-scope operation using `dbConfig.Get_Prefix_DB_Url($"logging/...")` (Pattern B) throughout; `ILoggingRepository` is defined in the same directory; `LoggingDAL` implements `ILoggingRepository`

**Given** `c_db_setup.cs` creates the `logging` database on first install
**When** evaluated
**Then** it is listed in the catalog but marked **out of scope** — DB creation is infrastructure

**Given** `ILoggingRepository` is defined
**When** DI registration is updated in `mmria-server/Program.cs`
**Then** `ILoggingRepository` is registered as `LoggingDAL` in the service collection

**Given** `loggerController.cs` currently owns all direct `logging` database access (3 hits, all Pattern A)
**When** this story is complete
**Then** each is replaced with the corresponding `ILoggingRepository` method; `ILoggingRepository` is injected into `loggerController` via constructor injection; `loggerController` constructs no `logging/` URLs

**Given** `loggerController` also reads from `offline_cases` (Story 23.3) and now from `ILoggingRepository`
**When** this story and Story 23.3 are both complete
**Then** `loggerController` injects both `IOfflineCaseRepository` and `ILoggingRepository`; DI registration satisfies both dependencies

**Given** the build after all changes
**When** verified
**Then** all three projects build with zero errors

---

## Epic 23 — Story Sequencing

| Wave | Story                                                               | Risk       | Dependencies          |
| ---- | ------------------------------------------------------------------- | ---------- | --------------------- |
| 23   | 23.1 — Remaining Database Gap Scan                                  | None       | None — discovery only |
| 23   | 23.2 — `ISessionRepository` + `SessionDAL`                          | Medium     | 23.1                  |
| 23   | 23.3 — `IOfflineCaseRepository` + `OfflineCaseDAL`                  | Low        | 23.1                  |
| 23   | 23.4 — `IExportQueueRepository` + `ExportQueueDAL`                  | Low        | 23.1                  |
| 23   | 23.5 — `IVitalImportRepository` + `VitalImportDAL` canonicalization | Low–Medium | 23.1                  |
| 23   | 23.6 — `IReportRepository` + `ReportDAL` (read interface)           | Low        | 23.1                  |
| 23   | 23.7 — Route report read calls through `IReportRepository`          | Low–Medium | 23.6                  |
| 23   | 23.8 — `ILoggingRepository` + `LoggingDAL`                          | Low        | 23.1                  |

23.2, 23.3, 23.4, 23.5, 23.6, and 23.8 can all proceed in parallel once 23.1 is complete. 23.7 depends on 23.6. Story 23.2 carries medium risk due to the number of call sites (actors + controllers + cross-feature DAL injection); all others are low or low–medium.

**Migration readiness gate:** When Epic 23 is complete, every CouchDB database access in `mmria-server`, `mmria.common`, and `mmria.services` routes through a repository interface. SQL migration work begins here — swap each CouchDB DAL implementation for a SQL implementation one database at a time, with no changes required to managers, controllers, or services actors.

---

## Epic 24: Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness.

**Summary:** The eight files classified "infra out-of-scope" across Epics 17–23 contain direct CouchDB HTTP calls for full-database rebuild orchestration, real-time change-feed synchronization, CDC data integration, nightly export-queue lifecycle management, and system startup initialization. After this epic, every CouchDB access call in the entire codebase routes through a repository interface. SQL migration then requires: (a) replacing each CouchDB DAL implementation with a SQL DAL implementation one database at a time, and (b) replacing `IDatabaseLifecycleService` with SQL schema-migration tooling — no orchestration code requires modification.

**Guiding principle — lift and shift:** This epic moves CouchDB HTTP calls into DAL files. It does NOT restructure orchestration logic, change actor message protocols, modify Akka actor hierarchies, alter Quartz schedules, or change the multi-tenant rebuild process or CDC data flow. The sync and rebuild orchestration files change only at their CouchDB call sites — surrounding coordination, error handling, retry logic, and control flow are not touched.

**New interfaces introduced:**

| Interface                                          | Location                                       | Purpose                                                                                           |
| -------------------------------------------------- | ---------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `IDeIdentifiedRepository`                          | `mmria.common/SharedLibraries/DeIdentified/`   | de_id database: per-doc CRUD, bulk write, and DB lifecycle (drop/reset, design docs, indexes)     |
| `IReportRepository` write ext.                     | Extends Epic 23 Story 23.6 read-only interface | Adds per-doc write, bulk write, drop/reset, design docs, and index operations                     |
| `ICaseRepository` sync ext.                        | Extends Epic 17 Story 17.2 interface           | Adds `GetCasesPagedAsync` (cursor-based bulk read) and `GetCaseChangesSinceAsync` (change-stream) |
| `IDatabaseLifecycleService`                        | `mmria-server/`                                | Interface seam over `c_db_setup.cs` for full system startup initialization                        |
| `IExportQueueRepository.PurgeAndReinitializeAsync` | Extends Epic 23 Story 23.4 interface           | Adds nightly drop/recreate/security operation to the export-queue interface                       |

**Scope table — files in scope and which story owns them:**

| File                                | Project                                            | Primary operations                                                                                | Story | Risk                            |
| ----------------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------------------------------- | ----- | ------------------------------- |
| `c_db_setup.cs`                     | mmria-server/util/                                 | ALL DBs: CREATE, SECURITY, DESIGN, INDEX, seed data                                               | 24.5  | Low (interface extraction only) |
| `Rebuild_Export_Queue.cs`           | mmria-server/model/actor/quartz/                   | export_queue: DELETE DB, CREATE DB, SECURITY                                                      | 24.4  | Low                             |
| `rebuild_export_queue_job.cs`       | mmria-server/model/                                | export_queue: DELETE DB, CREATE DB, SECURITY (legacy IJob)                                        | 24.4  | Low                             |
| `c_sync_document.pmss.cs`           | mmria-server/util/                                 | de_id: PUT/DELETE per-doc; report: 4 document-type variant PUTs                                   | 24.6  | Low                             |
| `c_document_sync_all.cs`            | mmria-server/util/                                 | mmrds: paged bulk read; metadata: via DAL ✓; de_id: bulk write; report: bulk write, design, index | 24.7  | Medium                          |
| `c_document_sync_all_legacy.cs`     | mmria-server/util/                                 | mmrds: paged read; de_id: individual PUT; report: design + individual writes                      | 24.7  | Medium                          |
| `c_document_sync_all.pmss.cs`       | mmria-server/util/                                 | mmrds: paged bulk read; de_id: design + writes; report: design + index + writes                   | 24.7  | Medium                          |
| `c_document_sync_all_legacy.cs`     | mmria.common/SharedLibraries/MMRIARebuild/Manager/ | metadata: via DAL ✓; mmrds: paged; de_id: bulk write; report: bulk write + design                 | 24.7  | Medium                          |
| `Process_DB_Synchronization_Set.cs` | mmria-server/model/actor/quartz/                   | mmrds: `_changes` feed + doc GET; de_id: per-doc PUT/DELETE; report: per-doc PUT/DELETE           | 24.8  | Medium                          |
| `Process_Central_Pull_list.cs`      | mmria-server/model/actor/quartz/                   | mmrds (multi-source): bulk read; de_id: bulk write; report: design + index + bulk write           | 24.9  | High                            |
| `c_document_sync_all.cs`            | mmria.services/Actors/populate-cdc-instance/       | metadata: via DAL ✓; mmrds: cursor paged; de_id: bulk write; report: design + bulk write          | 24.9  | High                            |

---

### Story 24.1: Infra Operations Catalog

As a developer,
I want a definitive catalog of every database operation in all in-scope infra files,
So that Stories 24.2–24.9 have an agreed-upon, complete operation set and every call site is identified before any code changes begin.

**Acceptance Criteria:**

**Given** all eleven in-scope files (ten files in the scope table above plus the mmria.services `c_document_sync_all.cs`)
**When** the developer completes the catalog
**Then** `docs/ai/mmrds_operation_catalog.md` gains an "Epic 24 — Infrastructure Consolidation" section documenting every distinct operation grouped by: DB lifecycle (CREATE database, DELETE database, SECURITY, PUT design document, POST `_index`), paged bulk read (`_all_docs` with cursor/skip), change-stream read (`_changes`), per-document CRUD (GET/PUT/DELETE by ID), and bulk write (`_bulk_docs`)

**Given** each catalog entry
**When** the catalog is complete
**Then** each entry records: file name, approximate line number, operation type, target database, URL pattern in use (A, B, or other), and which new interface from the "New interfaces introduced" table will own the call

**Given** the `c_document_sync_all.cs` in `mmria.services/Actors/populate-cdc-instance/`
**When** cataloged
**Then** its CDC-specific characteristics are noted: cursor-based pagination, bulk-write throttling, and metadata already routed through DAL (already correct — no change needed for that subset)

**Given** design document PUT and Mango index POST operations in sync/rebuild files
**When** cataloged
**Then** they are explicitly marked as DB-lifecycle operations to be routed through `IDeIdentifiedRepository` or `IReportRepository` lifecycle methods (not through `IDatabaseLifecycleService`) — the catalog records this routing decision

---

### Story 24.2: `IDeIdentifiedRepository` and Extend `IReportRepository` for Sync Writes and Lifecycle

As a developer,
I want repository interfaces covering all de_id and report database operations — including write, bulk-write, and DB-lifecycle — that sync and rebuild files require,
So that Stories 24.6–24.9 can route every call through a typed interface instead of direct HTTP calls.

**Acceptance Criteria:**

**Given** no `DeIdentified` SharedLibraries feature exists
**When** this story creates one
**Then** the following structure exists:

```
mmria.common/SharedLibraries/DeIdentified/
  IDeIdentifiedRepository.cs
  DAL/
    DeIdentifiedDAL.cs
```

**Given** the de_id operations identified in Story 24.1
**When** `DeIdentifiedDAL` is created
**Then** it contains async methods covering:

- `GetRevisionAsync(string id, DBConfigurationDetail dbConfig)` → `string? rev` — used to check before write/delete
- `UpsertDocumentAsync(string id, JObject doc, DBConfigurationDetail dbConfig)` → `document_put_response`
- `DeleteDocumentAsync(string id, string rev, DBConfigurationDetail dbConfig)` → `document_put_response`
- `BulkUpsertAsync(IEnumerable<JObject> docs, DBConfigurationDetail dbConfig)` → `IEnumerable<document_put_response>`
- `DropAndResetAsync(DBConfigurationDetail dbConfig)` — drops the de_id database and recreates it empty; used by rebuild flows; in SQL migration, the implementation executes `TRUNCATE TABLE de_id`
- `EnsureDesignDocumentAsync(string designName, string designDocJson, DBConfigurationDetail dbConfig)` — PUT `de_id/_design/{designName}`; in SQL migration, this is a no-op or triggers index creation
- `EnsureIndexAsync(string indexJson, DBConfigurationDetail dbConfig)` — POST `de_id/_index`; in SQL migration, this creates or verifies a SQL index

All CRUD and bulk methods use `dbConfig.Get_Prefix_DB_Url($"de_id/...")` (Pattern B). The `DropAndResetAsync` method uses `dbConfig.Get_Prefix_DB_Url("de_id")` for the database-level DELETE and PUT.

**Given** `IDeIdentifiedRepository` is defined
**When** DI registration is updated in `mmria-server/Program.cs`
**Then** `IDeIdentifiedRepository` is registered as `DeIdentifiedDAL` in the service collection; `mmria.services` already references `mmria.common` so no new project reference is needed

**Given** `IReportRepository` from Story 23.6 is currently read-only
**When** this story extends it
**Then** `IReportRepository` gains these additional methods implemented in `ReportDAL`:

- `GetRevisionAsync(string id, DBConfigurationDetail dbConfig)` → `string? rev`
- `UpsertDocumentAsync(string id, JObject doc, DBConfigurationDetail dbConfig)` → `document_put_response`
- `DeleteDocumentAsync(string id, string rev, DBConfigurationDetail dbConfig)` → `document_put_response`
- `BulkUpsertAsync(IEnumerable<JObject> docs, DBConfigurationDetail dbConfig)` → `IEnumerable<document_put_response>`
- `DropAndResetWithSystemDocPreservationAsync(DBConfigurationDetail dbConfig)` — drops and recreates the report database while preserving system/config documents that must survive; in SQL migration, the implementation executes a targeted `DELETE FROM report_documents WHERE type NOT IN ('system', 'config')`
- `EnsureDesignDocumentAsync(string designName, string designDocJson, DBConfigurationDetail dbConfig)`
- `EnsureIndexAsync(string indexJson, DBConfigurationDetail dbConfig)`

**Given** the note in Story 23.6 that write/rebuild operations on `report` were declared "out of scope" for that story
**When** this story adds the write methods
**Then** the catalog entry for `report` in `docs/ai/mmrds_operation_catalog.md` is updated to reflect that write operations are now covered by `IReportRepository`; the boundary decision recorded in Story 23.6 is superseded

**Given** no existing callers are changed in this story
**When** the build runs after this story
**Then** all three projects build with zero errors

---

### Story 24.3: Extend `ICaseRepository` with Paged Bulk Read and Change-Stream Read

As a developer,
I want `ICaseRepository` to expose the paged bulk read and change-stream read patterns needed by rebuild and sync orchestrators,
So that `c_document_sync_all` variants and `Process_DB_Synchronization_Set` can replace their direct `mmrds` calls with interface calls.

**Acceptance Criteria:**

**Given** `ICaseRepository` from Story 17.2 covers per-document CRUD and view queries but not bulk paged reads or change-feed access
**When** this story is complete
**Then** `ICaseRepository` gains:

- `GetCasesPagedAsync(string? startKey, int limit, DBConfigurationDetail dbConfig)` → `CasePage` containing `IReadOnlyList<JObject> documents` and `string? lastId`
- `startKey` null means start from beginning; `lastId` of the returned page is passed as `startKey` for the next page
- Implemented in `CaseDAL` as `GET {prefix}mmrds/_all_docs?include_docs=true&startkey={startKey}&limit={limit}` (cursor-based pagination)
- In SQL migration: `SELECT * FROM cases WHERE id > @startKey ORDER BY id FETCH NEXT @limit ROWS ONLY`

**Given** `Process_DB_Synchronization_Set` polls `mmrds/_changes` to detect mutations
**When** this story is complete
**Then** `ICaseRepository` gains:

- `GetCaseChangesSinceAsync(string sinceSeq, DBConfigurationDetail dbConfig)` → `CaseChangeFeedResult` containing `string lastSeq` and `IReadOnlyList<CaseChangeEntry>`
- `CaseChangeEntry` holds: `string id`, `string seq`, `bool deleted`, `JObject? doc` (full document for updates; null for deletes)
- Implemented in `CaseDAL` as `GET {prefix}mmrds/_changes?since={sinceSeq}&include_docs=true`
- In SQL migration: the implementation polls a SQL change-tracking query or CDC table instead of a `_changes` feed; the interface contract is identical

**Given** `CasePage` and `CaseChangeFeedResult` are new model types
**When** they are created
**Then** they live in `mmria.common/SharedLibraries/Case/` alongside the existing interface

**Given** `CaseDAL` implements the new methods
**When** the build runs after this story
**Then** all three projects build with zero errors; no existing callers require modification

---

### Story 24.4: Route Export Queue Rebuild Actors Through `IExportQueueRepository`

As a developer,
I want `Rebuild_Export_Queue` and `rebuild_export_queue_job` to route their database lifecycle operations through `IExportQueueRepository`,
So that the nightly export-queue drop/recreate is fully behind the repository interface established in Story 23.4.

**Acceptance Criteria:**

**Given** `IExportQueueRepository` from Story 23.4 covers application CRUD but not database-lifecycle operations
**When** this story extends it
**Then** `IExportQueueRepository` gains:

- `PurgeAndReinitializeAsync(DBConfigurationDetail dbConfig)` — drops the `export_queue` database, recreates it empty, and restores the security document (`abstractor` role only); in SQL migration, the implementation executes `TRUNCATE TABLE export_queue` and resets row-level permissions

**Given** `ExportQueueDAL` implements `PurgeAndReinitializeAsync`
**When** implemented
**Then** the DELETE database, PUT database, and PUT `_security` calls that were previously assembled in `Rebuild_Export_Queue.cs` are moved into `ExportQueueDAL.PurgeAndReinitializeAsync`; the method uses `dbConfig.Get_Prefix_DB_Url("export_queue")` and `dbConfig.Get_Prefix_DB_Url("export_queue/_security")` (Pattern B)

**Given** `Rebuild_Export_Queue.cs` currently assembles `{url}/{prefix}export_queue` and `{url}/{prefix}export_queue/_security` URLs directly
**When** this story is complete
**Then** all direct `CouchDbHttpClient.ExecuteAsync` calls in this file are replaced with `await _exportQueueRepository.PurgeAndReinitializeAsync(dbConfig)`; `IExportQueueRepository` is injected into the actor via Akka.NET actor props factory; the Akka message-handling logic, scheduling conditions, and midnight-only check are **not changed**

**Given** `rebuild_export_queue_job.cs` is a legacy `IJob` implementation containing identical lifecycle calls
**When** the developer evaluates it against the Quartz scheduler registration in `Program.cs`
**Then** if the job is actively registered: it is updated to inject and use `IExportQueueRepository.PurgeAndReinitializeAsync` following the same pattern; if it is not registered in the scheduler and is unreachable: it is left unchanged and a comment is added marking it as superseded by the `Rebuild_Export_Queue` Akka actor — the catalog records which applies

**Given** the filesystem directory cleanup in `Rebuild_Export_Queue` (deletes the export output directory)
**When** evaluated
**Then** it remains in the actor class — filesystem operations are not CouchDB operations and do not belong in `ExportQueueDAL`

**Given** the build and nightly export-queue rebuild behavior
**When** verified after all changes
**Then** all three projects build with zero errors; the export-queue rebuild actor's external behavior is identical to pre-change

---

### Story 24.5: Extract `IDatabaseLifecycleService` over `c_db_setup.cs`

As a developer,
I want `c_db_setup.cs` to be registered behind an `IDatabaseLifecycleService` interface,
So that the entire system startup database initialization path has a clean SQL migration seam and a SQL implementation can substitute it with schema-migration tooling without touching `Program.cs`.

**Acceptance Criteria:**

**Given** `c_db_setup.cs` in `mmria-server/util/` is currently instantiated or called directly from `Program.cs` startup code
**When** this story is complete
**Then** `IDatabaseLifecycleService` is defined with the public async method(s) that `Program.cs` calls on startup; `c_db_setup` implements `IDatabaseLifecycleService`; `Program.cs` resolves via `IDatabaseLifecycleService` — the concrete `c_db_setup` type does not appear in `Program.cs`

**Given** the internals of `c_db_setup.cs`
**When** this story is implemented
**Then** the internal `CouchDbHttpClient.ExecuteAsync` calls inside `c_db_setup.cs` are NOT moved or changed — `c_db_setup` remains the complete CouchDB implementation; this story introduces the interface seam only, exactly one layer above `c_db_setup`

**Given** `IDatabaseLifecycleService` is defined
**When** DI registration is updated in `Program.cs`
**Then** `services.AddScoped<IDatabaseLifecycleService, c_db_setup>()` (or `AddSingleton` if that matches the current lifetime) is present; the service lifetime matches how `c_db_setup` is currently used

**Given** the PMSS-specific conditional compilation paths in `c_db_setup.cs` (e.g., `#if IS_PMSS_ENHANCED`)
**When** the interface is extracted
**Then** the `IDatabaseLifecycleService` method signatures are identical regardless of compile-time flag; `c_db_setup` continues to branch internally on `IS_PMSS_ENHANCED` as before; no interface member is conditional

**Given** `IDatabaseLifecycleService` as the SQL migration seam
**When** a future SQL migration story implements it
**Then** the SQL implementation handles schema creation via EF Core migrations or equivalent tooling, role/permission setup, and initial seed data without touching `Program.cs` — the `Program.cs` call to `IDatabaseLifecycleService` remains unchanged

**Note:** This story does NOT route the design-doc and index operations embedded in the sync/rebuild files through `IDatabaseLifecycleService`. Those calls will be routed through `IDeIdentifiedRepository` and `IReportRepository` lifecycle methods in Stories 24.6–24.9. `IDatabaseLifecycleService` is the seam for the startup-only initialization path in `c_db_setup.cs` exclusively.

**Given** the build after this story
**When** verified
**Then** `mmria-server` builds with zero errors; startup database initialization behavior is identical to pre-change

---

### Story 24.6: Route `c_sync_document.pmss.cs` Through Repository Interfaces

As a developer,
I want `c_sync_document.pmss.cs` to route its de_id and report writes through repository interfaces,
So that this leaf-level per-document sync utility has no direct CouchDB calls — completing the foundation that Stories 24.7 and 24.8 build on.

**Acceptance Criteria:**

**Given** `c_sync_document.pmss.cs` in `mmria-server/util/` writes de-identified and report documents per case using `CouchDbHttpClient.ExecuteAsync` directly
**When** this story is complete
**Then** every direct CouchDB call in this file is replaced with the corresponding repository method call; `IDeIdentifiedRepository` and `IReportRepository` are injected via constructor injection

**Given** the de_id operations in this file (identified in Story 24.1):

- GET to check existing document revision before overwrite or delete
- PUT de-identified document (Pattern B or A — as discovered)
- DELETE de-identified document
  **When** this story is complete
  **Then** each is replaced with: `IDeIdentifiedRepository.GetRevisionAsync(...)`, `IDeIdentifiedRepository.UpsertDocumentAsync(...)`, `IDeIdentifiedRepository.DeleteDocumentAsync(...)` respectively

**Given** the report operations in this file — writes to four document-type variants per case (`freq-{id}`, `opioid-{id}`, `powerbi-{id}`, `dqr-{id}`):

- GET revision for each variant before overwrite
- PUT each variant
- DELETE each variant (when a case is deleted from mmrds)
  **When** this story is complete
  **Then** each revision GET is replaced with `IReportRepository.GetRevisionAsync(...)` and each PUT/DELETE is replaced with `IReportRepository.UpsertDocumentAsync(...)` / `IReportRepository.DeleteDocumentAsync(...)` using the full document-type-prefixed ID (e.g., `"freq-{caseId}"`) as the `id` parameter — the document-type prefix is preserved in the ID, not extracted as a separate concept

**Given** the PMSS-specific de-identification and report-generation logic in this file (`c_de_identifier`, `c_generate_frequency_summary_report`, etc.)
**When** this story is implemented
**Then** the transformation and generation logic remains in `c_sync_document.pmss.cs` — only the CouchDB HTTP calls are replaced; the transformation pipeline is not touched

**Given** `c_sync_document.pmss.cs` is called by `c_document_sync_all.pmss.cs` and `Process_DB_Synchronization_Set.cs`
**When** this story completes
**Then** those callers continue to work unchanged — the constructor signature of `c_sync_document.pmss.cs` gains the two repository parameters; callers that instantiate it directly must pass the injected repositories (resolved at the point this story is implemented: either callers are updated in this story or via DI)

**Given** the build after all changes
**When** verified
**Then** `mmria-server` builds with zero errors

---

### Story 24.7: Route `c_document_sync_all` Variants Through Repository Interfaces

As a developer,
I want all four `c_document_sync_all` variants (server main, server legacy, server PMSS, and common/SharedLibraries legacy) to route their mmrds reads, de_id writes, report writes, and DB-lifecycle operations through the repository interfaces established in Stories 24.2–24.3,
So that the full-database rebuild orchestration has no direct CouchDB calls.

**Acceptance Criteria:**

**Given** the four files in scope:

1. `mmria-server/util/c_document_sync_all.cs`
2. `mmria-server/util/c_document_sync_all_legacy.cs`
3. `mmria-server/util/c_document_sync_all.pmss.cs`
4. `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_document_sync_all_legacy.cs`
   **When** this story is complete
   **Then** every direct `CouchDbHttpClient.ExecuteAsync` call in each file is replaced with the corresponding interface method; the four interfaces used are `ICaseRepository`, `IDeIdentifiedRepository`, `IReportRepository`, and (for metadata reads not yet routed via DAL, if any) `IMetadataRepository`

**Given** the mmrds paged bulk read operations in all four files
**When** this story is complete
**Then** each is replaced with `ICaseRepository.GetCasesPagedAsync(startKey, limit, dbConfig)`; cursor-loop logic stays in the orchestrator — the repository returns one page; the orchestrator advances the cursor; no orchestration logic changes

**Given** the de_id operations:

- Drop/recreate de_id (during full rebuild start)
- PUT design document on de_id
- Bulk write de-identified documents (`_bulk_docs`)
- Individual per-document writes (legacy variant)
  **When** this story is complete
  **Then** drop/recreate → `IDeIdentifiedRepository.DropAndResetAsync(dbConfig)`; design doc → `IDeIdentifiedRepository.EnsureDesignDocumentAsync(name, json, dbConfig)`; bulk write → `IDeIdentifiedRepository.BulkUpsertAsync(docs, dbConfig)`; individual write → `IDeIdentifiedRepository.UpsertDocumentAsync(id, doc, dbConfig)`

**Given** the report operations:

- Drop/recreate report (during full rebuild, with system-doc preservation where applicable)
- PUT design documents (`interactive_aggregate_report`, `data_summary_view_report`, `powerbi-report-index`, etc.)
- POST Mango indexes (`opioid`, `powerbi` partial-filter indexes)
- Bulk write report documents
  **When** this story is complete
  **Then** drop/recreate with system-doc preservation → `IReportRepository.DropAndResetWithSystemDocPreservationAsync(dbConfig)`; design docs → `IReportRepository.EnsureDesignDocumentAsync(name, json, dbConfig)`; indexes → `IReportRepository.EnsureIndexAsync(json, dbConfig)`; bulk write → `IReportRepository.BulkUpsertAsync(docs, dbConfig)`

**Given** barrier queries in `c_document_sync_all_legacy.cs` (common) — these query `de_id/_design/sortable/_view/by_date_created?limit=1&update=true` and `report/_find` purely to wait for index readiness
**When** evaluated
**Then** a `WaitForIndexReadyAsync(DBConfigurationDetail dbConfig)` method is added to each of `IDeIdentifiedRepository` and `IReportRepository`; the barrier query call sites are replaced with these methods; the waiting and retry logic in the orchestrator remains unchanged

**Given** the `c_document_sync_all.cs` (server) calls `c_sync_document.build_documents_async()` for per-document transformation
**When** this story is complete
**Then** the calls to `c_sync_document` remain unchanged — `c_sync_document` is a transformation utility, not a CouchDB caller; only the wrapping CouchDB calls in `c_document_sync_all.cs` itself are replaced

**Given** the PMSS variant (`c_document_sync_all.pmss.cs`) has `#if IS_PMSS_ENHANCED` guarding
**When** this story is implemented
**Then** PMSS-specific paths use the same repository interfaces; no PMSS-specific divergence is introduced in the interface signatures; both PMSS and non-PMSS code paths route through `IDeIdentifiedRepository` and `IReportRepository`

**Given** the rebuild orchestration, progress-tracking, retry logic, and parallel-processing logic in all four files
**When** this story is implemented
**Then** none of it changes — the `db_rebuild` progress document writes (if any — as discovered in Story 24.1) are routed through whichever interface owns the `db_rebuild` database; the retry decorators, progress callbacks, and startup-checkpoint handling stay exactly as they are

**Given** the build after all changes
**When** verified
**Then** `mmria-server`, `mmria.common`, and `mmria.services` all build with zero errors; no rebuild behavior changes

**Pre-condition:** Stories 24.2 and 24.3 must be complete before this story begins. Story 24.6 must be complete if `c_sync_document.pmss` is used by `c_document_sync_all.pmss.cs` with constructor injection.

---

### Story 24.8: Route `Process_DB_Synchronization_Set` Through Repository Interfaces

As a developer,
I want `Process_DB_Synchronization_Set.cs` to route its `mmrds` change-stream reads and its `de_id`/`report` per-document sync writes through repository interfaces,
So that the real-time change-feed synchronization actor has no direct CouchDB calls.

**Acceptance Criteria:**

**Given** `Process_DB_Synchronization_Set.cs` polls `mmrds/_changes?since={last_seq}` to detect case mutations
**When** this story is complete
**Then** that call is replaced with `ICaseRepository.GetCaseChangesSinceAsync(lastSeq, dbConfig)`; the returned `CaseChangeFeedResult.lastSeq` is stored in `TenantChangeSequenceState` as before; the actor's polling timer, message-handling, and fan-out parallelism logic (`Parallel.ForEachAsync`) are not changed

**Given** `Process_DB_Synchronization_Set.cs` fetches the full case document for UPDATE events via `GET mmrds/{id}`
**When** this story is complete
**Then** that call is replaced with `ICaseRepository.GetCaseDocumentJsonAsync(id, dbConfig)` (or the equivalent ICaseRepository GET method from Story 17.2); `ICaseRepository` is injected via the Akka.NET actor props factory

**Given** `Process_DB_Synchronization_Set.cs` uses `c_sync_document` utility for per-document transformation and writes to `de_id` and `report`
**When** evaluated
**Then** if `c_sync_document` itself is already using repository interfaces after Story 24.6, the dependency chain is already satisfied; if `c_sync_document` (non-PMSS variant) still has direct CouchDB calls, those calls are routed through `IDeIdentifiedRepository` and `IReportRepository` in this story following the same pattern as Story 24.6

**Given** DELETE events from the `_changes` feed (case deleted from mmrds) that require DELETE from `de_id` and `report`
**When** this story is complete
**Then** those DELETE calls are replaced with `IDeIdentifiedRepository.DeleteDocumentAsync(id, rev, dbConfig)` and `IReportRepository.DeleteDocumentAsync(id, rev, dbConfig)` respectively; revision lookups use `GetRevisionAsync(...)` on each repository

**Given** the paged `_all_docs` calls in `Process_DB_Synchronization_Set` (used for union/cleanup of deleted documents — noted as a commented code area)
**When** evaluated per Story 24.1 catalog
**Then** active (non-commented) `_all_docs` calls are replaced with `ICaseRepository.GetCasesPagedAsync(...)` or `IDeIdentifiedRepository`/`IReportRepository` equivalent; commented-out calls are left as comments — dead code is not refactored

**Given** `TenantChangeSequenceState` sequence tracking in the actor
**When** this story is implemented
**Then** sequence state management remains in the actor — it is not moved to a repository; the sequence value passed to `GetCaseChangesSinceAsync` comes from the actor's state as before

**Given** the build and real-time sync behavior
**When** verified after all changes
**Then** all three projects build with zero errors; the change-feed polling actor operates identically to pre-change

**Pre-condition:** Stories 24.2 and 24.3 must be complete. Story 24.6 must be complete if the non-PMSS `c_sync_document` is used by this actor.

---

### Story 24.9: Route `Process_Central_Pull_list` and CDC `c_document_sync_all` Through Repository Interfaces

As a developer,
I want `Process_Central_Pull_list.cs` and the CDC populate `c_document_sync_all.cs` (in `mmria.services`) to route all their CouchDB calls through repository interfaces,
So that the CDC data integration path — the most complex infra flow — has no direct HTTP calls and is SQL-migration-ready.

**Acceptance Criteria:**

**Given** `Process_Central_Pull_list.cs` is a conditional actor (`!IS_PMSS_ENHANCED` guard) that pulls case data from multiple CDC source instances, de-identifies it, and writes to the local `mmrds`, `de_id`, and `report` databases
**When** this story is complete
**Then** all direct `CouchDbHttpClient.ExecuteAsync` calls in `Process_Central_Pull_list.cs` are replaced with repository interface calls; the CDC source-instance iteration loop, de-identification delegation (`c_cdc_de_identifier`), and `Synchronize_Case` actor dispatch remain exactly as they are

**Given** the source-instance reads in `Process_Central_Pull_list.cs` — paged `_all_docs?include_docs=true` from each source `mmrds` database
**When** this story is complete
**Then** each is replaced with `ICaseRepository.GetCasesPagedAsync(startKey, limit, sourceDbConfig)` where `sourceDbConfig` is the `DBConfigurationDetail` for the source instance; the multi-instance loop over `cdc_instance_pull_list` entries is unchanged

**Given** the target writes in `Process_Central_Pull_list.cs`:

- DELETE and recreate target `mmrds`, `de_id`, `report` databases at the start of each CDC pull
- PUT design documents on target `de_id` (sortable)
- POST Mango indexes on target `report` (opioid, powerbi)
- Per-document writes to target `mmrds` (via `Synchronize_Case` actor dispatch — these are already abstracted by the actor)
  **When** this story is complete
  **Then** mmrds target lifecycle → `ICaseRepository` lifecycle method (add `DropAndResetAsync(DBConfigurationDetail)` to `ICaseRepository` and `CaseDAL`); de_id lifecycle → `IDeIdentifiedRepository.DropAndResetAsync(...)` and `EnsureDesignDocumentAsync(...)`; report lifecycle → `IReportRepository.DropAndResetWithSystemDocPreservationAsync(...)` and `EnsureIndexAsync(...)`

**Given** adding `DropAndResetAsync` to `ICaseRepository`
**When** evaluated
**Then** this is a CDC-specific operation. It is added to `ICaseRepository` with a note that this method is used exclusively by the CDC populate path; in SQL migration, the implementation executes `TRUNCATE TABLE cases` scoped to the target tenant prefix; the standard Story 17.2 CRUD methods are not affected

**Given** `c_document_sync_all.cs` in `mmria.services/Actors/populate-cdc-instance/` is the modern bulk-sync implementation used by the CDC populate flow
**When** this story is complete
**Then** its direct mmrds cursor-paged reads are replaced with `ICaseRepository.GetCasesPagedAsync(...)`; its de_id bulk writes are replaced with `IDeIdentifiedRepository.BulkUpsertAsync(...)`; its report bulk writes are replaced with `IReportRepository.BulkUpsertAsync(...)`; its design document and index operations are replaced with `IDeIdentifiedRepository.EnsureDesignDocumentAsync(...)`, `IReportRepository.EnsureDesignDocumentAsync(...)`, and `IReportRepository.EnsureIndexAsync(...)` — its metadata reads are already routed through the metadata DAL and are **not changed**

**Given** the `c_cdc_de_identifier` de-identification actor used by `Process_Central_Pull_list`
**When** evaluated
**Then** it is evaluated for direct CouchDB calls in Story 24.1; if it has direct calls, they are routed through the appropriate repository in this story; if it has none, it is confirmed as clean and noted

**Given** `Report_Opioid_Index_Struct` and other index-definition structures defined in the original `c_document_sync_all.cs` and referenced by `Process_Central_Pull_list`
**When** Story 24.7 may relocate them
**Then** this story resolves any reference breakage introduced by Story 24.7 — the struct definitions remain accessible to `Process_Central_Pull_list` via whatever location Story 24.7 establishes them in

**Given** the IS_PMSS_ENHANCED guard on `Process_Central_Pull_list`
**When** this story is implemented
**Then** only the non-PMSS code path (`!IS_PMSS_ENHANCED`) is modified — the PMSS path, if it exists, is evaluated independently and noted in the catalog

**Given** the sensitivity of the CDC data integration path
**When** this story is implemented
**Then** the developer runs the full CDC populate integration flow in the multi-tenant test environment before marking the story complete; a regression test confirming de-identification is preserved through the refactor is documented

**Given** the build and CDC populate behavior
**When** verified after all changes
**Then** all three projects build with zero errors; the CDC pull actor operates identically to pre-change

**Pre-condition:** Stories 24.2, 24.3, and 24.7 must all be complete before this story begins.

---

## Epic 24 — Story Sequencing

| Wave | Story                                                                       | Risk   | Dependencies                  |
| ---- | --------------------------------------------------------------------------- | ------ | ----------------------------- |
| 24   | 24.1 — Infra Operations Catalog                                             | None   | None — discovery only         |
| 24   | 24.2 — `IDeIdentifiedRepository` + `IReportRepository` write/lifecycle ext. | Low    | 24.1                          |
| 24   | 24.3 — `ICaseRepository` paged bulk read + change stream                    | Low    | 24.1                          |
| 24   | 24.4 — Export queue rebuild routing                                         | Low    | 24.1; Epic 23 Story 23.4 done |
| 24   | 24.5 — `IDatabaseLifecycleService` over `c_db_setup`                        | Low    | 24.1                          |
| 24   | 24.6 — `c_sync_document.pmss.cs` routing                                    | Low    | 24.2                          |
| 24   | 24.7 — `c_document_sync_all` variants routing                               | Medium | 24.2, 24.3, 24.6              |
| 24   | 24.8 — `Process_DB_Synchronization_Set` routing                             | Medium | 24.2, 24.3, 24.6              |
| 24   | 24.9 — `Process_Central_Pull_list` + CDC `c_document_sync_all` routing      | High   | 24.2, 24.3, 24.7              |

24.2, 24.3, 24.4, and 24.5 can all proceed in parallel once 24.1 is complete.
24.6 depends only on 24.2 and can start as soon as 24.2 is done.
24.7 and 24.8 can proceed in parallel once 24.2, 24.3, and 24.6 are complete.
24.9 must wait for 24.7 due to shared struct definitions and file proximity.

**Final migration readiness gate:** When Epic 24 is complete, every CouchDB HTTP call in the entire mmria codebase — application, infrastructure, sync, rebuild, and lifecycle — routes through a typed repository interface. SQL migration is reduced to: swap each DAL implementation, replace `IDatabaseLifecycleService` with schema-migration tooling, and update the SQL `GetCaseChangesSinceAsync` to use SQL change-tracking instead of `_changes`. No orchestration code, no actor logic, no controller code, and no rebuild pipelines require modification during SQL migration.

---

## Epic 25: Async Safety + Metadata Reader Consolidation

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern

**Summary:** Two targeted cleanup passes. Story 25.1 fixes a production correctness bug: two files call `CouchDbHttpClient.ExecuteAsync(...).Result` inside what is ultimately an async ASP.NET request context, which can deadlock the thread pool under load. Story 25.2 routes the remaining direct `metadata` database reads in six transform-helper classes through the `IMetadataRepository` interface (established in Epic 20) — eliminating ~12 non-DAL call sites in a single low-risk injection pass.

**Non-DAL files remediated:**

| File                                                                                       | Call type                                         | Story |
| ------------------------------------------------------------------------------------------ | ------------------------------------------------- | ----- |
| `mmria-server/util/JurisdictionAuthorizationRequirement.cs`                                | `.Result` blocking call                           | 25.1  |
| `mmria-server/util/VROSummary.cs`                                                          | `.Result` blocking call                           | 25.1  |
| `mmria-server/util/c_convert_to_dqr_detail.cs`                                             | `metadata/version_specification-{v}/metadata` GET | 25.2  |
| `mmria-server/util/c_convert_to_opioid_report_object.cs`                                   | `metadata/version_specification-{v}/metadata` GET | 25.2  |
| `mmria-server/util/c_convert_to_report_object.cs`                                          | `metadata/version_specification-{v}/metadata` GET | 25.2  |
| `mmria-server/util/c_generate_frequency_summary_report.cs`                                 | `metadata/version_specification-{v}/metadata` GET | 25.2  |
| `mmria-server/util/c_de_identifier.cs`                                                     | `metadata/de-identified-list` GET                 | 25.2  |
| `mmria-server/util/c_cdc_de_identifier.cs`                                                 | `metadata/de-identified-export-list` GET          | 25.2  |
| `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_convert_to_dqr_detail.cs`             | same as server variant                            | 25.2  |
| `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_convert_to_opioid_report_object.cs`   | same as server variant                            | 25.2  |
| `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_convert_to_report_object.cs`          | same as server variant                            | 25.2  |
| `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_generate_frequency_summary_report.cs` | same as server variant                            | 25.2  |
| `mmria.common/SharedLibraries/MMRIARebuild/Manager/c_de_identifier.cs`                     | `metadata/de-identified-list` GET                 | 25.2  |
| `mmria.common/SharedLibraries/MMRIAServices/Helper/c_cdc_de_identifier.cs`                 | `metadata/de-identified-export-list` GET          | 25.2  |

---

### Story 25.1: Fix `.Result` Blocking Calls

**Story ID:** 25.1
**Depends on:** None
**Source requirements:** Non-DAL analysis; project-context.md §2.2

As a developer,
I want `JurisdictionAuthorizationRequirement.cs` and `VROSummary.cs` to use `await` instead of `.Result` for CouchDB calls,
So that these request-path methods cannot deadlock the ASP.NET thread pool under concurrent load.

**Acceptance Criteria:**

**AC-1 — `JurisdictionAuthorizationRequirement.cs` made async**
Given `JurisdictionAuthorizationRequirement.cs` calls `_couchDbHttpClient.ExecuteAsync(...).Result` at approximately line 45
When this story is complete
Then the call site uses `await _couchDbHttpClient.ExecuteAsync(...)` and the enclosing method is `async Task` or `async Task<bool>`; the behavior (read jurisdiction view, evaluate result) is identical; no exception-handling patterns are added or removed

**AC-2 — `VROSummary.cs` blocking calls removed**
Given `VROSummary.cs` calls `_couchDbHttpClient.ExecuteAsync(...).Result` at multiple lines (~188, ~190)
When this story is complete
Then every `.Result` call is replaced with `await`; the enclosing method(s) are made `async`; callers are updated to `await` as needed to propagate async correctly up the call chain; no behavior change occurs

**AC-3 — Build passes**
Given the changes above
When the build runs
Then `mmria-server` builds with zero errors; no other call sites are changed

**Dev Notes:**

- `.Result` on a Task inside an async-context method causes a deadlock on ASP.NET's synchronized context under certain thread-pool contention conditions. Making the method `async` and using `await` is the correct fix — not `Task.Run(() => ...)`.
- The enclosing types may implement interfaces that constrain the method signature (e.g., `IAuthorizationHandler`). Check the interface — ASP.NET Core authorization handlers use `Task HandleRequirementAsync(...)` which already returns `Task`, so the async change should propagate cleanly.
- Do NOT add `ConfigureAwait(false)` — the project does not use it elsewhere.

---

### Story 25.2: Metadata Reader `IMetadataRepository` Injection Pass

**Story ID:** 25.2
**Depends on:** 25.1 (can proceed in parallel)
**Source requirements:** Non-DAL analysis; Epic 20 establishes `IMetadataRepository`

As a developer,
I want the six transform-helper classes in `mmria-server/util/` and `mmria.common/.../MMRIARebuild/Manager/` to read metadata through `IMetadataRepository` instead of calling `CouchDbHttpClient.ExecuteAsync` directly,
So that the metadata database access in the rebuild and de-identification pipeline has a SQL migration seam.

**Acceptance Criteria:**

**AC-1 — `IMetadataRepository` injected into all target files**
Given each target file currently constructs a metadata URL and calls `_couchDbHttpClient.ExecuteAsync("GET", metadata_url, ...)` to read one of two documents (`version_specification-{version}/metadata` or `de-identified-list`)
When this story is complete
Then `IMetadataRepository` is injected via constructor parameter (optional with null fallback) into each of the fourteen target files

**AC-2 — `version_specification` reads replaced**
Given `c_convert_to_dqr_detail`, `c_convert_to_opioid_report_object`, `c_convert_to_report_object`, and `c_generate_frequency_summary_report` each call `GET metadata/version_specification-{version}/metadata`
When this story is complete
Then each is replaced with `IMetadataRepository.GetAppDocumentAsync(metadata_version, db_config)` (or the equivalent method that returns `mmria.common.metadata.app`); if the existing call returns raw JSON and the caller deserializes it, the caller is updated to use the typed return value from the repository method

**AC-3 — `de-identified-list` reads replaced**
Given `c_de_identifier` (server + common) calls `GET metadata/de-identified-list`
When this story is complete
Then each is replaced with the appropriate `IMetadataRepository` method for the de-identified list (confirm whether `GetDeIdentifiedListAsync` exists in `IMetadataRepository`; if not, add it to the interface and implement it in `MetadataVersionDAL` before replacing call sites)

**AC-4 — `de-identified-export-list` reads replaced**
Given `c_cdc_de_identifier` (server + common) calls `GET metadata/de-identified-export-list`
When this story is complete
Then each is replaced with the appropriate `IMetadataRepository` method (confirm whether `GetDeIdentifiedExportListAsync` exists; add to interface and DAL if needed, following AC-3 approach)

**AC-5 — Null fallback preserved**
Given callers of the transform helpers that do not yet pass an `IMetadataRepository`
When this story is complete
Then the null fallback (use direct `_couchDbHttpClient.ExecuteAsync`) preserves existing behavior; no caller changes are required in this story

**AC-6 — Build passes**
Given the changes above
When the build runs
Then `mmria-server`, `mmria.common`, and `mmria.services` all build with zero errors

**Dev Notes:**

| File group                                              | Method on `IMetadataRepository`                                              |
| ------------------------------------------------------- | ---------------------------------------------------------------------------- |
| `c_convert_to_*`, `c_generate_frequency_summary_report` | `GetAppDocumentAsync(version, dbConfig)` → `mmria.common.metadata.app`       |
| `c_de_identifier`                                       | `GetDeIdentifiedListAsync(dbConfig)` → `ExpandoObject` (add if absent)       |
| `c_cdc_de_identifier`                                   | `GetDeIdentifiedExportListAsync(dbConfig)` → `ExpandoObject` (add if absent) |

Note: `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` already uses `_metadataRepository.GetDeIdentifiedListAsync(connection)` — confirm that method signature before adding to the interface to avoid duplication.

---

## Epic 25 — Story Sequencing

| Story                                 | Risk | Dependencies                    |
| ------------------------------------- | ---- | ------------------------------- |
| 25.1 — Fix `.Result` blocking calls   | Low  | None                            |
| 25.2 — Metadata reader injection pass | Low  | Epic 20 complete (already done) |

25.1 and 25.2 are independent and can proceed in parallel.

---

## Epic 26: Controller API Direct-Call Remediation

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern

**Summary:** Fifteen controllers and utility files in `mmria-server` and `mmria.services` still call `CouchDbHttpClient.ExecuteAsync` directly. All target repository interfaces were established in Epics 17–23. This epic is entirely a wiring pass — no new interfaces, no new DALs. Stories are grouped by the repository each batch of files needs, enabling parallel execution once the group's repo is confirmed available.

**Non-DAL files remediated:**

| Story | File                                                         | Repository needed                                                              |
| ----- | ------------------------------------------------------------ | ------------------------------------------------------------------------------ |
| 26.1  | `mmria-server/Controllers/api/caseController.cs`             | `ICaseRepository` (Epic 17 story 17.2)                                         |
| 26.1  | `mmria-server/Controllers/api/case_viewController.pmss.cs`   | `ICaseRepository`                                                              |
| 26.1  | `mmria-server/Controllers/api/caseRevisionListController.cs` | `ICaseRepository`                                                              |
| 26.1  | `mmria-server/Controllers/api/de_idController.cs`            | `IDeIdentifiedRepository` (Epic 24 story 24.2)                                 |
| 26.1  | `mmria-server/Controllers/api/record_idController.cs`        | `ICaseRepository`                                                              |
| 26.2  | `mmria-server/Controllers/AccountController.cs`              | `IUserRepository` (Epic 18 story 18.2)                                         |
| 26.2  | `mmria-server/CustomAuthHandler.cs`                          | `ISessionRepository` (Epic 23 story 23.2)                                      |
| 26.2  | `mmria-server/Controllers/api/passwordChangeController.cs`   | `ISessionRepository`                                                           |
| 26.2  | `mmria-server/util/OfflineSessionHelper.cs`                  | `ISessionRepository`                                                           |
| 26.3  | `mmria-server/Controllers/api/queueController.cs`            | `IExportQueueRepository` (Epic 23 story 23.4)                                  |
| 26.3  | `mmria.services/Controllers/ExportQueueController.cs`        | `IExportQueueRepository`                                                       |
| 26.3  | `mmria-server/Controllers/broadcast_messageController.cs`    | `IBroadcastMessageRepository` or direct via `mmria.services` — confirm pattern |
| 26.3  | `mmria.services/Controllers/broadcastMessageController.cs`   | same                                                                           |
| 26.3  | `mmria-server/Controllers/api/ije_messageController.cs`      | `ICaseRepository` or `IOfflineCaseRepository` — confirm DB target              |
| 26.4  | `mmria-server/util/JurisdictionSummary.cs`                   | `IJurisdictionRepository` (Epic 19 story 19.2)                                 |
| 26.4  | `mmria.services/Utilities/authorization.cs`                  | `IJurisdictionRepository`                                                      |
| 26.4  | `mmria-server/util/CaseViewSearch.pmss.cs`                   | `ICaseRepository` (PMSS path)                                                  |
| 26.4  | `mmria-server/util/exporter/export_all_generate_name_map.cs` | `IMetadataRepository` (Epic 20)                                                |

**Not in scope:** `mmria-server/Controllers/api/nioshController.cs` — the `CouchDbHttpClient.ExecuteAsync` call at line 72 targets an external NIOSH URL with null credentials, not a CouchDB database. This is a general-purpose HTTP call that happens to use `CouchDbHttpClient` as a transport. No repository routing required.

---

### Story 26.1: Case API Controllers

**Story ID:** 26.1
**Depends on:** Epic 17 story 17.2 (ICaseRepository), Epic 24 story 24.2 (IDeIdentifiedRepository)

As a developer,
I want the five case-data API controllers to call `ICaseRepository` or `IDeIdentifiedRepository` instead of constructing CouchDB URLs directly,
So that the remaining case-data controller layer has the same SQL migration seam as the managers.

**Acceptance Criteria:**

**AC-1 — `caseController.cs` direct call replaced**
Given `caseController.cs` calls `_couchDbHttpClient.ExecuteAsync` directly at approximately line 114 (a GET on `mmrds/{id}`)
When this story is complete
Then that call is replaced with `ICaseRepository.GetCaseDocumentJsonAsync(id, dbConfig)` or equivalent; `ICaseRepository` is injected via the existing DI pattern

**AC-2 — `case_viewController.pmss.cs` direct call replaced**
Given `case_viewController.pmss.cs` (PMSS-guarded) calls `_couchDbHttpClient.ExecuteAsync` at approximately lines 113–114
When this story is complete
Then each is replaced with the corresponding `ICaseRepository` method; the `#if IS_PMSS_ENHANCED` guard is preserved unchanged

**AC-3 — `caseRevisionListController.cs`, `de_idController.cs`, `record_idController.cs` calls replaced**
Given each of these controllers has one direct call (revision list, de_id read, and record_id check respectively)
When this story is complete
Then `caseRevisionListController` uses `ICaseRepository.GetCaseRevisionsAsync`; `de_idController` uses `IDeIdentifiedRepository.GetRevisionAsync` (or equivalent read method); `record_idController` uses `ICaseRepository.RecordIdExistsAsync`

**AC-4 — No response shape or route changes**
Given the controllers' existing HTTP method attributes, route paths, and response shapes
When this story is implemented
Then none are changed — only the internal CouchDB call site is replaced

**AC-5 — Build passes**
Given the changes above
When the build runs
Then `mmria-server` builds with zero errors

**Dev Notes:**

- `de_idController.cs` reads a de_id document. If `IDeIdentifiedRepository` does not yet have a read method for individual de_id documents (it has `GetRevisionAsync` but may not have a full `GetDocumentAsync`), add `GetDocumentAsync(string id, DBConfigurationDetail dbConfig)` → `JObject?` to the interface and implement it in `DeIdentifiedDAL` before replacing the call site.
- All five controllers already have `ICaseRepository` or DAL injection available via DI — confirm the existing DI wiring before adding new registrations.

---

### Story 26.2: Auth and Session Controllers

**Story ID:** 26.2
**Depends on:** Epic 23 story 23.2 (ISessionRepository), Epic 18 story 18.2 (IUserRepository)

As a developer,
I want auth and session-related controllers to call `ISessionRepository` and `IUserRepository` instead of constructing CouchDB URLs directly,
So that authentication-path database access has the same SQL migration seam as other features.

**Acceptance Criteria:**

**AC-1 — `CustomAuthHandler.cs` session reads replaced**
Given `CustomAuthHandler.cs` reads from the session database directly at approximately lines 86+
When this story is complete
Then each session GET is replaced with `ISessionRepository.GetSessionAsync(sessionId, dbConfig)` or equivalent; `ISessionRepository` is injected into the auth handler

**AC-2 — `AccountController.cs` call replaced**
Given `AccountController.cs` calls `_couchDbHttpClient.ExecuteAsync` at approximately line 664 (a user-record read)
When this story is complete
Then the call is replaced with the appropriate `IUserRepository` method; the existing behavior — including error handling and response shape — is unchanged

**AC-3 — `passwordChangeController.cs` session lookup replaced**
Given `passwordChangeController.cs` reads session state at approximately line 80 to validate the session before allowing a password change
When this story is complete
Then the read is replaced with `ISessionRepository.GetSessionAsync(...)` or equivalent

**AC-4 — `OfflineSessionHelper.cs` call replaced**
Given `OfflineSessionHelper.cs` at approximately line 42 reads session state to determine offline eligibility
When this story is complete
Then the read is replaced with `ISessionRepository.GetSessionAsync(...)` or equivalent; `ISessionRepository` is injected via the helper's constructor

**AC-5 — Build passes**
Given the changes above
When the build runs
Then `mmria-server` builds with zero errors

**Dev Notes:**

- `CustomAuthHandler.cs` registers with ASP.NET Core's authorization pipeline — its constructor injection must be compatible with the DI lifetime of the handler. Confirm lifetime (usually `Scoped`) before injecting a scoped repository.
- The session database uses `_users`-style access with credentials different from application databases on some tenants — confirm `ISessionRepository.GetSessionAsync` uses the correct dbConfig for the session database, not the main application dbConfig.

---

### Story 26.3: Export Queue and Broadcast Controllers

**Story ID:** 26.3
**Depends on:** Epic 23 story 23.4 (IExportQueueRepository); confirm broadcast message DB target

As a developer,
I want export queue and broadcast message controllers to call repository interfaces instead of constructing CouchDB URLs directly,
So that these controller-layer database accesses have SQL migration seams.

**Acceptance Criteria:**

**AC-1 — `queueController.cs` export_queue write replaced**
Given `queueController.cs` constructs an export_queue URL and calls `_couchDbHttpClient.ExecuteAsync("PUT", ...)` at approximately line 78
When this story is complete
Then the PUT is replaced with the appropriate `IExportQueueRepository` save method

**AC-2 — `mmria.services/Controllers/ExportQueueController.cs` call replaced**
Given `ExportQueueController.cs` in `mmria.services` calls `_couchDbHttpClient.ExecuteAsync` at approximately line 112
When this story is complete
Then that call is replaced with `IExportQueueRepository`; the repository is injected via the services DI registration established in Story 24.10

**AC-3 — `broadcast_messageController.cs` calls assessed and replaced**
Given both `mmria-server/Controllers/broadcast_messageController.cs` and `mmria.services/Controllers/broadcastMessageController.cs` call `_couchDbHttpClient.ExecuteAsync` for broadcast message writes
When this story begins
Then the developer first confirms which database the broadcast message data is written to (check the URL construction in both files); if a `IBroadcastMessageRepository` exists, use it; if not, assess whether to add the method to an existing interface or create `IBroadcastMessageRepository`; document the decision in the story completion notes

**AC-4 — `ije_messageController.cs` call assessed and replaced**
Given `ije_messageController.cs` at approximately line 106 calls `_couchDbHttpClient.ExecuteAsync`
When this story begins
Then the developer confirms which database the IJE message is read from (mmrds, offline_cases, or other); the call is replaced with the appropriate existing repository method; no new interface is created unless no suitable method exists

**AC-5 — Build passes**
Given the changes above
When the build runs
Then `mmria-server` and `mmria.services` both build with zero errors

---

### Story 26.4: Jurisdiction, Summary, and Remaining Utility Leakers

**Story ID:** 26.4
**Depends on:** Epic 19 story 19.2 (IJurisdictionRepository), Epic 20 IMetadataRepository

As a developer,
I want the remaining utility files that read jurisdiction, VRO summary, or metadata data directly to route through existing repository interfaces,
So that these non-controller call sites complete the controller migration wave.

**Acceptance Criteria:**

**AC-1 — `JurisdictionSummary.cs` call replaced**
Given `JurisdictionSummary.cs` reads a jurisdiction view directly at approximately line 342
When this story is complete
Then the call is replaced with `IJurisdictionRepository.GetJurisdictionSummaryAsync(...)` or equivalent existing method; `IJurisdictionRepository` is injected

**AC-2 — `mmria.services/Utilities/authorization.cs` call replaced**
Given `authorization.cs` in mmria.services reads a jurisdiction view at approximately line 61 (and possibly 63+)
When this story is complete
Then each call is replaced with the appropriate `IJurisdictionRepository` method; injection follows the services DI pattern

**AC-3 — `CaseViewSearch.pmss.cs` PMSS call replaced**
Given `CaseViewSearch.pmss.cs` (PMSS-guarded) constructs an mmrds view URL and calls `_couchDbHttpClient.ExecuteAsync` at approximately line 1998
When this story is complete
Then the call is replaced with the appropriate `ICaseRepository` view method; if no suitable view query method exists on `ICaseRepository`, one is added; the `#if IS_PMSS_ENHANCED` guard is preserved

**AC-4 — `export_all_generate_name_map.cs` metadata call replaced**
Given `export_all_generate_name_map.cs` reads a metadata document at approximately line 53 to build an export name map
When this story is complete
Then the call is replaced with `IMetadataRepository.GetAppDocumentAsync(...)` or equivalent; `IMetadataRepository` is injected

**AC-5 — Build passes**
Given the changes above
When the build runs
Then `mmria-server` and `mmria.services` both build with zero errors

---

## Epic 26 — Story Sequencing

| Story                                 | Risk       | Dependencies                                |
| ------------------------------------- | ---------- | ------------------------------------------- |
| 26.1 — Case API controllers           | Low        | Epic 17 story 17.2, Epic 24 story 24.2      |
| 26.2 — Auth/Session controllers       | Low        | Epic 23 story 23.2, Epic 18 story 18.2      |
| 26.3 — Export/Broadcast controllers   | Low-Medium | Epic 23 story 23.4; broadcast DB assessment |
| 26.4 — Jurisdiction/Summary utilities | Low        | Epic 19 story 19.2, Epic 20                 |

All four stories are independent and can proceed in parallel. 26.3 has a minor assessment gate (broadcast message DB target) that should be resolved at the start of the story.

---

## Epic 27: Services Utility Repository Activation

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern

**Summary:** Epics 24, 25, and 26 establish all repository interfaces and inject null-fallback scaffolding into the export-utility pipeline. This epic activates those null-fallbacks by wiring real repository instances from the Akka actor supervisors into `exporter.cs`, `mmrds_exporter.cs`, and `core_element_exporter.cs`. Story 27.2 closes the analysis loop: classifies the `BatchProcessor.cs` DELETE call and formally designates the `Process_Migrate_*` actors as intentional out-of-scope direct-access paths.

**Files in scope:**

| Story | File                                                                             | Action                                                                                           |
| ----- | -------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| 27.1  | `mmria.services/Utilities/Exporter/exporter.cs`                                  | Activate null-fallback: pass real `IExportQueueRepository` + `IReportRepository` from supervisor |
| 27.1  | `mmria.services/Utilities/Exporter/mmrds_exporter.cs`                            | Activate null-fallback: same repos                                                               |
| 27.1  | `mmria.services/Utilities/CoreElementExport/core_element_exporter.cs` (services) | Activate null-fallback: `IExportQueueRepository`                                                 |
| 27.2  | `mmria.services/Actors/BatchProcessor.cs`                                        | Classify DELETE target; replace with repo method or document as intentional                      |
| 27.2  | `mmria-server/model/actor/quartz/Process_Migrate_Charactor_to_Numeric.cs`        | Formal classification as intentional out-of-scope migration actor                                |
| 27.2  | `mmria-server/model/actor/quartz/Process_Migrate_Data.cs`                        | Formal classification as intentional out-of-scope migration actor                                |

---

### Story 27.1: Activate Export Utility Repository Wiring

**Story ID:** 27.1
**Depends on:** Epic 24 stories 24.10, 24.11; Epic 26 story 26.3

As a developer,
I want the export-utility classes in `mmria.services` to receive real repository instances from their supervisors instead of using null fallbacks,
So that the export pipeline's database access routes fully through repository interfaces rather than falling back to direct HTTP calls.

**Acceptance Criteria:**

**AC-1 — `exporter.cs` receives real `IExportQueueRepository`**
Given `exporter.cs` was given a null-fallback constructor param in Story 24.10
When this story is complete
Then the actor or supervisor that instantiates `exporter.cs` passes a real `IExportQueueRepository` instance resolved from DI; the null-fallback branch is no longer exercised at runtime

**AC-2 — `mmrds_exporter.cs` receives real `IExportQueueRepository`**
Given `mmrds_exporter.cs` was given a null-fallback constructor param in Story 24.10
When this story is complete
Then its instantiation site passes a real `IExportQueueRepository`; null-fallback not exercised at runtime

**AC-3 — `core_element_exporter.cs` (services) receives real `IExportQueueRepository`**
Given `core_element_exporter.cs` in mmria.services was given a null-fallback param in Story 24.10
When this story is complete
Then its instantiation site passes a real `IExportQueueRepository`; null-fallback not exercised at runtime

**AC-4 — IReportRepository wired where applicable**
Given export jobs may also write to the report database via utility helpers
When this story begins
Then the developer confirms whether `IReportRepository` null-fallbacks exist in any of the three utility files; if yes, those are also activated; if no, this AC is marked not-applicable

**AC-5 — Build passes and export queue job runs end-to-end**
Given the wiring changes
When the build runs and a CVS export job is triggered in the multi-tenant test environment
Then the build succeeds with zero errors and the export job completes normally without falling back to the direct HTTP path

**Dev Notes:**

- Trace the instantiation chain: `PopulateCDCInstanceSupervisor` → CDC actor → exporter utilities. The supervisor already has access to repos from its own DI injection (Story 24.11). Follow the chain and pass repos through.
- The null-fallback (direct HTTP) path is still valid as a safety net — do not remove it. The goal is that runtime code always reaches the repo branch.

---

### Story 27.2: BatchProcessor Assessment + Migration Actor Classification

**Story ID:** 27.2
**Depends on:** None (assessment story; can proceed in parallel with 27.1)

As a developer,
I want to understand and classify the remaining three direct-call sites not covered by earlier stories,
So that every non-DAL CouchDB call in the codebase has an explicit disposition — either routed through a repository or formally documented as an intentional exception.

**Acceptance Criteria:**

**AC-1 — `BatchProcessor.cs` DELETE call classified and resolved**
Given `BatchProcessor.cs` in `mmria.services/Actors/` calls `_couchDbHttpClient.ExecuteAsync("DELETE", ...)` at approximately line 512
When this story begins
Then the developer reads the file to identify the target database (check the URL construction); the call is either replaced with the appropriate existing repository method, or documented as an intentional exception with a rationale comment if it targets a lifecycle/admin database with no interface coverage

**AC-2 — `Process_Migrate_Charactor_to_Numeric.cs` classified**
Given `Process_Migrate_Charactor_to_Numeric.cs` calls `_couchDbHttpClient.ExecuteAsync` for case data migration operations
When this story is complete
Then a comment is added at the top of the class: `// Data migration actor. Direct CouchDB access is intentional — migration actors read and write raw case data in bulk and are excluded from the repository pattern by design. These actors are not used in production case-management flows.`; no other changes are made

**AC-3 — `Process_Migrate_Data.cs` classified**
Given `Process_Migrate_Data.cs` similarly contains migration-purpose direct CouchDB calls
When this story is complete
Then the same classification comment is added; no other changes are made

**AC-4 — Non-DAL call count confirmed zero (excluding documented exceptions)**
Given all previous stories in Epics 25–27 have been completed
When this story closes
Then a final scan confirms that every `CouchDbHttpClient.ExecuteAsync` call in `mmria-server`, `mmria.common`, and `mmria.services` (excluding utilities repo) is one of: (a) inside a DAL file, (b) inside an infrastructure exception file (`c_db_setup.cs`, `Check_DB_Install.cs`, `MultiTenantSetupService.cs`, `MMRIARebuildWorker.cs`), (c) a null-fallback path that is never exercised at runtime, or (d) formally documented as an intentional exception per ACs 2 and 3; the scan result is recorded in the story completion notes

**AC-5 — Build passes**
Given any changes in AC-1
When the build runs
Then all three projects build with zero errors

---

## Epic 27 — Story Sequencing

| Story                                                  | Risk | Dependencies             |
| ------------------------------------------------------ | ---- | ------------------------ |
| 27.1 — Activate export utility wiring                  | Low  | Epics 24.10, 24.11, 26.3 |
| 27.2 — BatchProcessor + migration actor classification | Low  | None                     |

27.1 and 27.2 are independent and can proceed in parallel.

---

## Epic 28: mmria-server Non-DAL Remnants (SQL Migration Foundation)

**Architecture rule:** project-context.md §2.2 SharedLibraries pattern + SQL migration readiness.

**Summary:** Post-Epic-27 scan identified four files in `mmria-server` that still call `CouchDbHttpClient.ExecuteAsync` directly against CouchDB databases for which repository interfaces were established in Epics 17–23. All required interfaces already exist; this epic is a pure wiring pass. After this epic, the only remaining non-DAL CouchDB calls in the entire codebase are formally classified infrastructure exceptions (sync utilities, rebuild actors, `c_db_setup.cs`, migration actors) — zero unclassified application calls remain.

**Non-DAL files remediated:**

| File                                                             | Calls | Database                                             | Repository needed                                             | Story |
| ---------------------------------------------------------------- | ----- | ---------------------------------------------------- | ------------------------------------------------------------- | ----- |
| `mmria-server/util/VROSummary.cs`                                | 3     | `mmrds` (2 per-doc GETs + 1 `_all_docs` for ID list) | `ICaseRepository` (Epic 17)                                   | 28.1  |
| `mmria-server/util/JurisdictionAuthorizationRequirement.cs`      | 1     | `jurisdiction/_design/sortable/_view/by_user_id`     | `IJurisdictionAuthorizationReader` (Epic 19 story 19.3)       | 28.2  |
| `mmria-server/CustomAuthHandler.cs`                              | 1     | `session/{sid}` PUT (refresh session expiration)     | `ISessionRepository` (Epic 23 story 23.2)                     | 28.2  |
| `mmria-server/util/core_element_export/core_element_exporter.cs` | 4     | `metadata` (2 GETs) + `mmrds` (2 GETs)               | `IMetadataRepository` (Epic 20) + `ICaseRepository` (Epic 17) | 28.3  |

**Note:** This is the mmria-server copy of `core_element_exporter.cs` at `mmria-server/util/core_element_export/`. The `mmria.services` copy was fully remediated in Epics 24 and 27. The server copy was not in scope for those epics.

---

### Story 28.1: `VROSummary.cs` Case Reads Through `ICaseRepository`

As a developer,
I want `VROSummary.cs` to read case documents through `ICaseRepository` instead of constructing `mmrds` URLs directly,
So that the VRO summary actor has the same SQL migration seam as all other case-data consumers.

**Acceptance Criteria:**

**AC-1 — Per-document case GET replaced**
Given `VROSummary.cs` at approximately line 188 calls `_couchDbHttpClient.ExecuteAsync("GET", $"{db_config.url}/{db_config.prefix}mmrds/{id}", ...)` inside a `foreach` loop over `id_list`
When this story is complete
Then that call is replaced with `ICaseRepository.GetCaseDocumentJsonAsync(id, db_config)` (or equivalent method returning raw JSON); `ICaseRepository` is injected into `VROSummary` via constructor injection

**AC-2 — Case count GET replaced**
Given `VROSummary.cs` at approximately line 341 calls `_couchDbHttpClient.ExecuteAsync("GET", request_string, ...)` to read a case document in the `GetUserCount`/`GetCaseCount` methods
When this story is complete
Then that call is replaced with the corresponding `ICaseRepository` method; `ICaseRepository` is passed to or injected into the method that owns that call

**AC-3 — `_all_docs` ID-list call replaced**
Given `VROSummary.cs` `GetIdList()` at approximately line 502 calls `_couchDbHttpClient.ExecuteAsync("GET", $"{db_config.url}/{db_config.prefix}mmrds/_all_docs", ...)` to build the case ID set
When this story is complete
Then that call is replaced with `ICaseRepository.GetCasesPagedAsync(null, int.MaxValue, db_config)` (or a dedicated `GetAllCaseIdsAsync` if that already exists on the interface); the resulting ID set is assembled from the returned page's document IDs as before; if paging is needed for large datasets, a loop is added using the cursor pattern — but for the VRO summary use case, a single large-page call matching the existing behavior is acceptable

**AC-4 — `_couchDbHttpClient` removed from `VROSummary` if no other calls remain**
Given `VROSummary.cs` currently injects `CouchDbHttpClient` alongside `IUserRepository` and `IJurisdictionRepository`
When this story is complete
Then if all three CouchDB call sites are replaced with repository calls, `CouchDbHttpClient _couchDbHttpClient` is removed from the constructor and the field; the constructor signature is updated and all callers that instantiate `VROSummary` are updated accordingly

**AC-5 — Build passes**
Given the changes above
When the build runs
Then `mmria-server` builds with zero errors

**Dev Notes — Files to Change:**

| File                                                | Change                                                                                                          |
| --------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| `source-code/mmria/mmria-server/util/VROSummary.cs` | **UPDATE** — inject `ICaseRepository`; replace 3 direct calls; remove `_couchDbHttpClient` if no calls remain   |
| Caller(s) that instantiate `VROSummary`             | **UPDATE** — pass `ICaseRepository` instance from DI scope; remove `CouchDbHttpClient` arg if removed from ctor |

**`ICaseRepository` note:** Check `GetCasesPagedAsync` signature from Epic 24 Story 24.3 before implementing AC-3. If the method returns `JObject` documents, extract IDs from the `_id` field. If `VROSummary` uses a `_design` filter (line 505: `if(_id.IndexOf("_design") > -1) continue`), preserve that filter in the loop.

---

### Story 28.2: Auth Middleware Session and Jurisdiction Wiring

As a developer,
I want `JurisdictionAuthorizationRequirement.cs` and `CustomAuthHandler.cs` to read and write through existing repository interfaces instead of constructing CouchDB URLs directly,
So that the auth middleware pipeline has the same SQL migration seam as all other database-touching code.

**Acceptance Criteria:**

**AC-1 — `JurisdictionAuthorizationRequirement.cs` jurisdiction view call replaced**
Given `JurisdictionAuthorizationRequirement.cs` at approximately line 45 calls `_couchDbHttpClient.ExecuteAsync("POST", jurisdicion_view_url, ...)` to query the `jurisdiction/_design/sortable/_view/by_user_id` view
When this story is complete
Then that call is replaced with `IJurisdictionAuthorizationReader.GetRolesByUserIdAsync(userId, dbConfig)` (the interface established in Epic 19 Story 19.3); `IJurisdictionAuthorizationReader` is injected into the requirement handler via constructor injection; the handler uses the returned role entries to populate the claim as before

**AC-2 — `IJurisdictionAuthorizationReader` injection is DI-lifetime-safe**
Given `JurisdictionAuthorizationRequirement.cs` is registered in the ASP.NET Core authorization pipeline
When the service lifetime is chosen
Then `IJurisdictionAuthorizationReader` is injected with a lifetime compatible with the handler's registration (confirm `Scoped` or `Transient` per the existing `JurisdictionAuthorizationDAL` registration from Epic 19.3)

**AC-3 — `CustomAuthHandler.cs` session PUT replaced**
Given `CustomAuthHandler.cs` at approximately line 171 calls `_couchDbHttpClient.ExecuteAsync("PUT", request_string, session_message_json, ...)` to write a refreshed session expiration back to `session/{sid}`
When this story is complete
Then that PUT is replaced with `ISessionRepository.UpdateSessionAsync(sid, session_message, dbConfig)` (or the equivalent write method on `ISessionRepository`); `ISessionRepository` is injected into `CustomAuthHandler` via constructor injection

**AC-4 — No behavioral change in auth pipeline**
Given the auth middleware executes on every authorized request
When this story is implemented
Then the observable behavior of jurisdiction-role validation and session-expiration refresh is identical to pre-change; no new error-handling paths are added; the existing `try/catch` structure is preserved

**AC-5 — Build passes**
Given the changes above
When the build runs
Then `mmria-server` builds with zero errors

**Dev Notes — Files to Change:**

| File                                                                          | Change                                                                                                           |
| ----------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `source-code/mmria/mmria-server/util/JurisdictionAuthorizationRequirement.cs` | **UPDATE** — inject `IJurisdictionAuthorizationReader`; replace `ExecuteAsync` POST with `GetRolesByUserIdAsync` |
| `source-code/mmria/mmria-server/CustomAuthHandler.cs`                         | **UPDATE** — inject `ISessionRepository`; replace `ExecuteAsync` PUT with `UpdateSessionAsync` (or equivalent)   |

**`ISessionRepository` write method note:** Story 23.2 established `ISessionRepository` with session CRUD. Confirm the exact method name for writing/updating a session document. If a write method that accepts `session_message` exists, use it directly. If it only accepts raw JSON, use the raw-JSON PUT overload. Do NOT create a new interface method if an existing write method covers the operation.

---

### Story 28.3: mmria-server `core_element_exporter.cs` Remaining Calls

As a developer,
I want the mmria-server copy of `core_element_exporter.cs` to read metadata and case data through `IMetadataRepository` and `ICaseRepository` instead of constructing CouchDB URLs directly,
So that the server-side core-element export path has the same SQL migration seam as its mmria.services counterpart (which was remediated in Epics 24–27).

**Acceptance Criteria:**

**AC-1 — Metadata version-spec read replaced**
Given `mmria-server/util/core_element_export/core_element_exporter.cs` at approximately line 132 calls `_couchDbHttpClient.ExecuteAsync("GET", metadata_url, ...)` where `metadata_url = db_config.url + $"/metadata/version_specification-{version}/metadata"`
When this story is complete
Then that call is replaced with `IMetadataRepository.GetAppDocumentAsync(version, db_config)` (the same method used by the `c_convert_to_*` files in Epic 25 Story 25.2); `IMetadataRepository` is injected via constructor injection

**AC-2 — De-identified list read replaced**
Given the same file at approximately line 213 calls `_couchDbHttpClient.ExecuteAsync("GET", db_config.url + "/metadata/de-identified-list", ...)` to load the de-identification field list
When this story is complete
Then that call is replaced with `IMetadataRepository.GetDeIdentifiedListAsync(db_config)` (the same method used by `c_de_identifier` in Story 25.2)

**AC-3 — Case view read replaced**
Given the same file at approximately line 246 calls `_couchDbHttpClient.ExecuteAsync("GET", request_string, ...)` where `request_string` is a `mmrds` view query URL (case view for export filtering)
When this story is complete
Then that call is replaced with the appropriate `ICaseRepository` view query method; if no view query method covering this specific view exists on `ICaseRepository`, one is added to the interface and implemented in `CaseDAL` before replacing the call site (following the same pattern as Story 26.1 AC-2)

**AC-4 — Per-case document GET replaced**
Given the same file at approximately line 265 calls `_couchDbHttpClient.ExecuteAsync("GET", URL, ...)` where `URL = $"{db_config.url}/{db_config.prefix}mmrds/{id}"` to fetch the full case document for export
When this story is complete
Then that call is replaced with `ICaseRepository.GetCaseDocumentJsonAsync(id, db_config)` or equivalent

**AC-5 — `_couchDbHttpClient` removed from `core_element_exporter` (server copy) if no calls remain**
Given the server copy currently injects `CouchDbHttpClient` as `_couchDbHttpClient`
When this story is complete
Then if all four call sites are replaced, `_couchDbHttpClient` is removed from the constructor and the field; callers that pass `CouchDbHttpClient` to this constructor are updated

**AC-6 — Build passes**
Given the changes above
When the build runs
Then `mmria-server` builds with zero errors

**Dev Notes — Files to Change:**

| File                                                                               | Change                                                                                                                                |
| ---------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `source-code/mmria/mmria-server/util/core_element_export/core_element_exporter.cs` | **UPDATE** — inject `IMetadataRepository` + `ICaseRepository`; replace 4 direct calls; remove `_couchDbHttpClient` if no calls remain |
| Caller(s) that instantiate this exporter                                           | **UPDATE** — resolve and pass repo instances from DI scope                                                                            |

**mmria.services comparison note:** The `mmria.services` version of `core_element_exporter.cs` was remediated across Epics 24–27. The server copy lives at `mmria-server/util/core_element_export/core_element_exporter.cs` and is a separate file. Verify method signatures match what is now available on `ICaseRepository` and `IMetadataRepository` before implementing — they should match exactly what the services copy now uses.

---

## Epic 28 — Story Sequencing

| Story                                                          | Risk | Dependencies                                                                                   |
| -------------------------------------------------------------- | ---- | ---------------------------------------------------------------------------------------------- |
| 28.1 — `VROSummary.cs` case reads                              | Low  | Epic 17 story 17.2 (ICaseRepository), Epic 24 story 24.3 (GetCasesPagedAsync)                  |
| 28.2 — Auth middleware session and jurisdiction wiring         | Low  | Epic 23 story 23.2 (ISessionRepository), Epic 19 story 19.3 (IJurisdictionAuthorizationReader) |
| 28.3 — mmria-server `core_element_exporter.cs` remaining calls | Low  | Epic 17 story 17.2 (ICaseRepository), Epic 20 story 20.2 (IMetadataRepository)                 |

All three stories are independent and can proceed in parallel.

**Final non-DAL gate:** When Epic 28 is complete, every `CouchDbHttpClient.ExecuteAsync` call in `mmria-server` and `mmria.services` that touches a CouchDB application database routes through a repository interface. The only remaining non-DAL calls are in formally classified infrastructure files (sync utilities, rebuild actors, `c_db_setup.cs`, migration actors) which are addressed as a unit when SQL migration implementation begins.

---

## Epic 29: Record ID Uniqueness Enforcement

**Summary:** Production cases were created with duplicate MMRIA Record IDs (`{jurisdiction}-{year-of-death}-{4-digit-number}`). Root-cause analysis identified three compounding defects: no server-side uniqueness enforcement on save, a TOCTOU race condition in the client-side generation loop, and a broken CouchDB view dependency that silently left the client with no uniqueness data. This epic implements defense-in-depth: the server is the authoritative last line of defense, the client validates per-candidate before saving, and the broken infrastructure is repaired. The 4-digit numeric suffix is the focus of uniqueness — the full record ID format is validated end-to-end but the suffix is where collisions occur.

**Files in scope:**

| Story | File                                                                                | Action                                                                                 |
| ----- | ----------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| 29.1  | `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | Add format validation + uniqueness guard in `SaveCaseAsync`                            |
| 29.2  | `source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js`                | Replace stale-Set loop with per-candidate `/api/record_id` calls                       |
| 29.2  | `source-code/mmria/mmria-server/wwwroot/scripts/case/index.pmss.js`                 | Same change for PMSS variant                                                           |
| 29.3  | `source-code/mmria/mmria-server/database-scripts/case_design_sortable.json`         | Add `record_id_list` view                                                              |
| 29.3  | `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`                      | Remove `Get_Record_Id_List` function and `g_record_id_list` Set (dead code after 29.2) |
| 29.4  | `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | Extract `GenerateUniqueRecordIdAsync` primitive; populate `error_code` on rejection    |
| 29.4  | `nccdphp-drh-mmria-common/mmria.common/model/couchdb/document_put_response.cs`      | Add `error_code` string property (nullable, non-breaking)                              |
| 29.5  | `source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js`                | Remove pre-flight loop; single POST + collision-retry using `error_code`               |
| 29.5  | `source-code/mmria/mmria-server/wwwroot/scripts/case/index.pmss.js`                 | Same change for PMSS variant                                                           |
| 29.6  | `source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js`                | Offline branch generates `STATE-OFFLINE-CASE-XX` placeholder                           |
| 29.6  | `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-session-manager.js` | Per-session placeholder counter                                                        |
| 29.6  | `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-case-manager.js`    | Remove `generateOfflineRecordId` (no more `-offline` suffix)                           |
| 29.6  | `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-sync-manager.js`    | Update client sync-side pattern detection                                              |
| 29.6  | `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-ui-renderer.js`     | Update offline-case detection to accept both patterns                                  |
| 29.6  | `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs` | Server-side sync: detect placeholder, generate real record ID, then save            |
| 29.7  | `nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs`  | Route write through `SaveCaseAsync`; retry on `record_id_conflict`                     |
| 29.7  | `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Helper/MMRIAServicesHelper.cs` | Remove stale-HashSet uniqueness in `ConvertLineToBatchItem`                       |
| 29.7  | `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Manager/MMRIAServicesManager.cs` | Deprecate or remove `GetExistingRecordIds` if unused                            |

---

### Story 29.1: Server-Side Record ID Format Validation and Uniqueness Guard

**Story ID:** 29.1
**Depends on:** None

As a system,
I want new case saves to be rejected when the MMRIA Record ID is already in use or does not follow the required format,
So that no two cases in the same jurisdiction database can ever share a Record ID, regardless of how the client behaves.

**Background:** `SaveCaseAsync` in `CaseManager.cs` performs authorization, lock, and rev-conflict checks but does not validate `record_id`. The infrastructure to check uniqueness already exists: `RecordIdExistsAsync` (used in `GetRecordIdReplacementForYearOfDeathAsync`) calls a Mango `_find` query per candidate and is accurate under concurrency. The `record_idController` (`GET /api/record_id`) also uses this method but is never called from the case creation path.

**Acceptance Criteria:**

**AC-1 — Guard is scoped to new cases only**
Given a case save request where the existing CouchDB document returned HTTP 404 (new case, no `_rev`)
When `SaveCaseAsync` reaches the record ID check
Then the guard runs; for existing cases (HTTP 200 from CouchDB probe) the guard is skipped entirely — record ID is set at creation time and is immutable via this path

**AC-2 — Record ID format is validated**
Given a new case save where `home_record.record_id` is not null and not empty
When `SaveCaseAsync` validates the record ID
Then it verifies the ID matches the pattern `{jurisdiction}-{year}-{4-digit-number}`: splitting on `-` the last segment is exactly 4 decimal digits (`\d{4}`), the second-to-last segment is a 4-digit year between 1900 and 2100 (`\d{4}`, value in range), and the jurisdiction prefix (everything before those two segments) is non-empty; if validation fails, the save is rejected with `ok = false` and a descriptive `error_description` naming which part failed

**AC-3 — Record ID uniqueness is enforced**
Given a new case with a validly formatted record ID
When `SaveCaseAsync` calls `RecordIdExistsAsync(mmria_record_id, dbConfig)`
Then if the method returns `true`, the save is rejected with `ok = false` and `error_description` = `"Record ID '{record_id}' is already in use. Please generate a new Record ID."` — the case document is not written to CouchDB

**AC-4 — Empty or null record ID is not blocked (backward compat)**
Given a new case save where `home_record.record_id` is null, empty, or whitespace
When the guard runs
Then the save proceeds normally — the format and uniqueness check is skipped; the record ID is assigned by the client and may legitimately be absent on first save in some workflows

**AC-5 — Error on RecordIdExistsAsync failure defaults safe**
Given `RecordIdExistsAsync` throws an exception (CouchDB unreachable)
When the exception propagates
Then the existing `catch` in `SaveCaseAsync` handles it; the save does not silently succeed — the caller receives an error response per existing exception-handling behavior

**AC-6 — Build passes**
Given the guard is added
When all three projects (`mmria-server`, `mmria.common`, `mmria.services`) are built
Then zero build errors

**Dev Notes:**

- Insert the guard after the existing CouchDB document probe (the `if (checkStatusCode == 404)` branch), before the write. Keep it inside the `404` branch only.
- `RecordIdExistsAsync` is already injected via `_caseRepository` — no new dependencies needed.
- Regex for last segment: `^\d{4}$`. Regex for year segment: `^\d{4}$` with `int.Parse` range check. Split strategy: `recordId.Split('-')` then validate `array[^1]` (4-digit suffix) and `array[^2]` (year) and confirm `array.Length >= 3`.
- `mmria_record_id` is set at line ~923 from `caseData.home_record.record_id` — use that variable directly.

---

### Story 29.2: Client-Side Per-Candidate Uniqueness Check via API

**Story ID:** 29.2
**Depends on:** 29.1 (server guard is the safety net; this story eliminates the primary race condition)

As an abstractor,
I want the "Generate Record ID" flow to confirm with the server that each candidate ID is unique before using it,
So that the generated ID is guaranteed unique at the moment of selection, not just against a stale in-memory snapshot.

**Background:** Currently `add_new_case()` loops `while(g_record_id_list.has(new_record_id))` against a client-side `Set` populated by `Get_Record_Id_List()`. That Set is a point-in-time snapshot: if two abstractors generate IDs simultaneously, both see the same snapshot and may produce the same ID. The server-side endpoint `GET /api/record_id?record_id=X` (served by the existing `record_idController`) already performs a per-candidate Mango query and returns `{ ok: true, is_unique: true|false }`. It exists but is never called from the case creation flow.

**Acceptance Criteria:**

**AC-1 — Online mode: generation loop uses per-candidate API call**
Given the user is in online mode (not offline) and clicks "Generate Record ID & Continue" and confirms
When `add_new_case()` generates the initial candidate `{jurisdiction}-{year}-{NNNN}`
Then it calls `GET /api/record_id?record_id={candidate}` and checks `response.is_unique`; if `false`, a new candidate is generated and the API is called again; the loop continues until `is_unique === true`

**AC-2 — Candidate generation format is preserved**
Given the loop in AC-1
When a new candidate is generated on each retry
Then it uses the same format: `reporting_state.trim() + '-' + year.trim() + '-' + $mmria.getRandomCryptoValue().toString().substring(2, 6)` — the 4-digit suffix is regenerated each time; the jurisdiction and year components are not changed

**AC-3 — Max-retry guard prevents infinite loop**
Given the API repeatedly returns `is_unique: false` (pathological case)
When the retry count reaches 20
Then the loop exits, an error is surfaced to the user ("Unable to generate a unique Record ID after multiple attempts. Please try again."), and `add_new_case()` does not proceed

**AC-4 — Offline mode: existing behavior is preserved unchanged**
Given the user is in offline mode (`window.OfflineStatus.isOffline() === true`)
When `add_new_case()` runs
Then it loads offline record IDs from `window.OfflineSessionManager.loadOfflineRecordIds(g_ui)` into a local Set and uses the existing `while(localSet.has(candidate))` loop — no API calls are made in offline mode

**AC-5 — `Get_Record_Id_List` is no longer called for the online confirm path**
Given Story 29.2 is complete for `index.mmria.js`
When the confirm handler fires in online mode
Then `Get_Record_Id_List` is not called; the per-candidate API loop in `add_new_case()` provides the uniqueness guarantee directly

**AC-6 — Same change applied to `index.pmss.js`**
Given the PMSS variant has the same race condition (`index.pmss.js` line ~424)
When this story is complete
Then `index.pmss.js` applies the same per-candidate API loop for online mode, with the same offline-mode preservation and max-retry guard

**AC-7 — No regressions in case creation flow**
Given the change is made
When an abstractor creates a new case in a local multi-tenant environment
Then the case saves successfully, navigates to the home_record form, and the assigned Record ID is unique in the database

**Dev Notes:**

- The API call is `$.ajax({ url: \`${location.protocol}//${location.host}/api/record_id?record_id=${encodeURIComponent(candidate)}\` })`—`record_idController` is already wired and requires auth.
- Keep `g_record_id_list.add(new_record_id.toUpperCase())` after confirming uniqueness — the Set still guards against within-session duplicates while the API guards against cross-session duplicates.
- The offline branch should call `window.OfflineSessionManager.loadOfflineRecordIds(g_ui)` directly at the point of generation, not rely on a prior `Get_Record_Id_List` call.
- `index.mmria.js` and `index.pmss.js` share `index.js` as a dependency — `g_record_id_list` and `Get_Record_Id_List` are defined there. Do not remove them in this story (Story 29.3 handles cleanup).

---

### Story 29.3: Add record_id_list CouchDB View and Remove Dead Bulk-List Code

**Story ID:** 29.3
**Depends on:** 29.2 (bulk-list code is only safe to remove after per-candidate checks replace it)

As a developer,
I want the `record_id_list` CouchDB view to exist in the tracked design document and the unused `Get_Record_Id_List` client function to be removed,
So that the `/api/case_view/record-id-list` endpoint is functional (for any future use) and dead client code does not mislead future developers.

**Background:** `CaseViewManager.GetRecordIdListAsync()` and `CaseDAL.GetCaseRecordIdListViewJsonAsync()` both call `mmrds/_design/sortable/_view/record_id_list`. This view is not present in `case_design_sortable.json` — the tracked design document lists 17 views but not `record_id_list`. As a result, the API endpoint always returns HTTP 404 or empty, and the client's `Get_Record_Id_List` silently fails, calling its callback with an empty Set. After Story 29.2, `Get_Record_Id_List` is no longer called from the online case creation path, making it dead code.

**Acceptance Criteria:**

**AC-1 — `record_id_list` view added to `case_design_sortable.json`**
Given `case_design_sortable.json` currently has no `record_id_list` view
When this story is complete
Then a `record_id_list` view is added to the `views` object with the map function:

```javascript
function(doc) {
  if (doc.home_record && doc.home_record.record_id) {
    emit(doc.home_record.record_id, { record_id: doc.home_record.record_id });
  }
}
```

The view name matches the string used in `CaseViewManager.GetRecordIdListAsync()` exactly: `record_id_list`

**AC-2 — `Get_Record_Id_List` function removed from `index.js`**
Given `Get_Record_Id_List` is defined in `index.js` (around line 2388) and is no longer called from `index.mmria.js` or `index.pmss.js` after Story 29.2
When this story is complete
Then the `Get_Record_Id_List` async function declaration and its entire body are removed from `index.js`; no call sites remain (confirmed by grep)

**AC-3 — `g_record_id_list` Set retained for offline-mode use**
Given `g_record_id_list` (declared in `index.js`) is still used by the offline branch of `add_new_case()` after Story 29.2
When this story is complete
Then `g_record_id_list` is **not** removed — it remains as the within-session duplicate guard for offline mode; a comment is added: `// Used in offline mode only — online mode uses per-candidate /api/record_id checks (Story 29.2)`

**AC-4 — Design document update script is run**
Given the design document change in AC-1
When this story is complete
Then the developer confirms the view is deployed to the local multi-tenant CouchDB instance and `GET /api/case_view/record-id-list` returns a valid (possibly empty) response rather than 404

**AC-5 — Build and smoke test pass**
Given the dead code removal and design doc change
When the server is built and a case creation is performed in the local environment
Then zero build errors, and the case creation flow completes normally for both online and offline modes

**Dev Notes:**

- The design document is deployed via the existing `case_design_sortable.json` update path used by `db-redeploy`. Confirm the exact script invocation with the existing production update process.
- Before removing `Get_Record_Id_List`, run: `Select-String -Path "source-code\mmria\mmria-server\wwwroot\scripts\**\*.js" -Pattern "Get_Record_Id_List"` to confirm no remaining call sites.
- `GetRecordIdListAsync` in `CaseViewManager` and `GetCaseRecordIdListViewJsonAsync` in `CaseDAL` are **not** removed — they serve the `/api/case_view/record-id-list` endpoint which is now functional and may be called by future features or utilities.

---

### Story 29.4: Extract `GenerateUniqueRecordIdAsync` Primitive and Structured `error_code`

**Story ID:** 29.4
**Depends on:** 29.1 (the DAL fix landed with Story 29.1's smoke-test follow-up on 2026-08-19)

As a developer,
I want a single manager-level method that generates a jurisdiction-scoped unique MMRIA Record ID and a machine-readable `error_code` returned when `SaveCaseAsync` rejects one on format or collision grounds,
So that every case-creation path (online UI, offline sync, IJE batch) can share one implementation and detect collisions without string-matching English error text.

**Background:** `CaseManager.GetRecordIdReplacementForYearOfDeathAsync` (line ~685) already contains the exact "generate 4-digit random suffix, check via `RecordIdExistsAsync`, retry" loop that Paths A, B, and C all need. Extracting it lets the three creation paths call one primitive. Additionally, Story 29.1's `SaveCaseAsync` guard returns English-language rejection messages; downstream callers currently would have to string-match them to know whether to regenerate or surface a hard error. A structured `error_code` field solves that.

**Acceptance Criteria:**

**AC-1 — New public method exists on `CaseManager`**
Given the primitive is extracted
When `CaseManager.GenerateUniqueRecordIdAsync(string statePrefix, string year, DBConfigurationDetail dbInfo, int maxAttempts = 20)` is called
Then it returns a `STATE-YEAR-NNNN` record ID whose 4-digit suffix does not currently exist in the database identified by `dbInfo`, or throws a `RecordIdGenerationExhaustedException` if `maxAttempts` random suffixes all collide

**AC-2 — Existing `GetRecordIdReplacementForYearOfDeathAsync` delegates to the new method**
Given the extraction preserves behavior for the Story 39.1 flow
When `GetRecordIdReplacementForYearOfDeathAsync` runs
Then it computes the state and target-year segments and calls `GenerateUniqueRecordIdAsync`; net behavior for that flow is unchanged (verified by existing test coverage plus at least one new unit test)

**AC-3 — `document_put_response` gains a nullable `error_code` field**
Given `document_put_response` is the canonical error carrier for case-save responses
When a rejection carries a machine-readable reason
Then `document_put_response.error_code` is populated with one of the string constants `record_id_format` or `record_id_conflict` (defined in one central location, e.g. `mmria.common.model.couchdb.SaveErrorCodes`); `error_code` is `null` on success and on rejections that predate this story (backward compatible)

**AC-4 — `SaveCaseAsync` populates `error_code` on Story 29.1 guard rejections**
Given the format guard rejects (bad suffix / bad year / missing prefix)
When `SaveCaseAsync` returns
Then `error_code = "record_id_format"` and the human-readable `error_description` is preserved
And given the uniqueness guard rejects (`RecordIdExistsAsync` returned true)
When `SaveCaseAsync` returns
Then `error_code = "record_id_conflict"` and the human-readable `error_description` is preserved

**AC-5 — Unit tests cover the new method**
Given `CaseManager.GenerateUniqueRecordIdAsync` is added
When the test suite runs
Then unit tests cover: (a) happy path returns a valid `STATE-YEAR-NNNN` id, (b) collision retry advances the suffix, (c) exhaustion after `maxAttempts` throws `RecordIdGenerationExhaustedException`, (d) year and state segments are echoed unchanged

**AC-6 — Build passes**
Given the change is added
When all projects build
Then zero errors across `mmria.common`, `mmria-server`, `mmria.services`, and the utilities test project

**Dev Notes:**

- Put the constants class in `mmria.common.model.couchdb.SaveErrorCodes` (or similar single-source-of-truth location) so both server and JavaScript callers can reference a documented value.
- `RecordIdGenerationExhaustedException` should carry `statePrefix`, `year`, and `attempts` for diagnostic use.
- No changes to `record_idController` in this story — that endpoint is not deprecated until Story 29.5 removes its last caller.

---

### Story 29.5: Path A — Online Save-Then-Retry-on-Collision

**Story ID:** 29.5
**Depends on:** 29.4

As an abstractor,
I want the "Generate Record ID" flow to attempt the save with a single locally-generated candidate and, if the server reports a collision, automatically try again with a fresh suffix,
So that the case-creation flow does not do 20 preflight round trips in the common case and does not surface a hard failure when a rare race condition occurs.

**Background:** Story 29.2 shipped a 20-call pre-flight loop against `/api/record_id` before the POST to `/api/case`. The pre-flight consumes network round-trips even when there is no contention and leaves a residual race between the last "unique" reply and the save. Story 29.1's server-side guard is the authoritative check; the client only needs to detect a collision reply and regenerate.

**Acceptance Criteria:**

**AC-1 — Online path no longer pre-flights `/api/record_id`**
Given the user is in online mode and clicks "Generate Record ID & Continue" and confirms
When `add_new_case()` runs
Then it generates one candidate `STATE-YEAR-NNNN` locally and issues a single POST to `/api/case` without calling `/api/record_id` first

**AC-2 — Collision response triggers regeneration and retry**
Given the server's response body contains `error_code === "record_id_conflict"`
When the client processes the response
Then `add_new_case()` regenerates the 4-digit suffix (same `generateCandidate` closure as Story 29.2), reassigns `home_record.record_id`, and re-POSTs to `/api/case`

**AC-3 — Retry cap prevents infinite loop**
Given the server returns `record_id_conflict` five consecutive times
When the client's collision-retry loop reaches its cap
Then the loop exits, the user sees `"Unable to generate a unique Record ID after multiple attempts. Please try again."`, and no case is created

**AC-4 — Non-collision errors surface immediately**
Given the server returns any `error_code` other than `record_id_conflict` (or `error_code` is absent)
When the client processes the response
Then the client does not retry; the response is surfaced as an ordinary save error via the existing `save_case_and_wait` failure path

**AC-5 — PMSS variant kept consistent**
Given the PMSS confirm handler at `index.pmss.js` line ~424 no longer wraps in `Get_Record_Id_List` (Story 29.2 removed it)
When this story completes
Then any residual pre-flight logic is removed and the PMSS unique-number flow uses its existing server-authoritative `/api/case_view/next-pmss-number` endpoint as the single source of truth — no additional client changes required, but confirm with a diff review that no pre-flight remains

**AC-6 — Build and smoke test pass**
Given the changes are applied
When the server builds and an abstractor creates a case in the local multi-tenant environment
Then zero build errors; a case creates successfully with one round trip in the common case; a forced-collision test (DevTools instrumentation returning `record_id_conflict` twice) confirms the retry loop advances and eventually succeeds

**Dev Notes:**

- Story 29.2's `alert("Unable to generate a unique Record ID…")` + `throw new Error(..., __handled=true)` pattern is preserved — only the trigger changes (server response vs. exhausted pre-flight loop).
- Keep `g_record_id_list.add(new_record_id.toUpperCase())` after a successful save — still guards within-session duplicates for other case-list refresh code paths.
- After this story, `record_idController` has no remaining callers in the shipped client. Do not delete in this story; add a `[Obsolete]` marker or a comment tagging Story 29.3's cleanup pass.

---

### Story 29.6: Path B — Offline Placeholder Record IDs (`STATE-OFFLINE-CASE-XX`)

**Story ID:** 29.6
**Depends on:** 29.4

As an abstractor working offline,
I want offline-created cases to hold a clearly-marked placeholder record ID until sync, at which point the server assigns a real jurisdiction-scoped unique ID,
So that offline case creation cannot silently pick a record ID that another user or another tab already used, and the sync collision recovery is centralized on the server.

**Background:** Today the offline branch of `add_new_case()` generates `STATE-YEAR-NNNN` against a local `g_ui` Set and appends `-OFFLINE`. At sync time the suffix is stripped and the case is written. If `STATE-YEAR-NNNN` collides with a case created by any other user (or by another offline tab of the same user) while the client was offline, the sync fails and the offline case is stuck. Story 29.4 provides the server-side primitive to generate a fresh unique record ID; this story replaces the client-side generation with a purely local placeholder.

**Acceptance Criteria:**

**AC-1 — Offline mode generates a placeholder record ID**
Given `window.OfflineStatus.isOffline() === true`
When `add_new_case()` reaches the record-ID assignment step
Then it sets `home_record.record_id = ${STATE}-OFFLINE-CASE-${XX}` where `STATE` is the jurisdiction prefix derived from `window.location.host` (same source as today) and `XX` is a per-offline-session sequence number formatted `NN` (2 digits, `01`–`99`, zero-padded, small-value case as the team has confirmed few offline cases are permitted per session)

**AC-2 — Sequence counter is scoped to the offline session and persists across tab reload**
Given `OfflineSessionManager` maintains offline-session state (crypto key, offline case list, session id) in memory + service-worker storage
When the offline case-creation flow needs a new `XX`
Then `OfflineSessionManager.getNextOfflineCaseSequence()` returns the next integer for this offline session, persists it, and is stable across a tab reload within the same offline session

**AC-3 — `generateOfflineRecordId` and the `-OFFLINE` suffix are removed from the offline creation path**
Given Story 29.6 is complete
When an abstractor creates an offline case
Then `home_record.record_id` never carries `-OFFLINE`; the `generateOfflineRecordId` helper in `offline-case-manager.js` is deleted and its callers updated to use the placeholder pattern from AC-1

**AC-4 — Sync-side detection converts placeholder to real ID before `SaveCaseAsync`**
Given an offline case with `home_record.record_id` matching `/^([A-Z0-9]+)-OFFLINE-CASE-\d+$/i`
When `OfflineCaseManager.ApplyOfflineDocumentAsync` processes it
Then it extracts the state prefix from group 1, reads the year from `home_record.date_of_death.year` (fallback: current year if absent, logged as a warning), calls `CaseManager.GenerateUniqueRecordIdAsync(state, year, dbConfig)`, assigns the result to `home_record.record_id`, and then calls `SaveCaseAsync`

**AC-5 — Legacy `-OFFLINE` suffix still accepted (transitional)**
Given a client cache still holds offline cases with the pre-29.6 `STATE-YEAR-NNNN-OFFLINE` format
When those cases sync
Then the existing suffix-strip path (`offline-sync-manager.js` L205–208, `OfflineCaseManager.cs` L619) remains functional; a structured log entry records which format was seen so we can measure when the legacy path is safe to remove

**AC-6 — UI displays placeholder as-is while offline**
Given an offline case has `home_record.record_id = "TENANT1-OFFLINE-CASE-01"`
When it is displayed in the case list or case header
Then the placeholder text is rendered verbatim; no attempt is made to synthesize a fake `STATE-YEAR-NNNN` for display

**AC-7 — Build and smoke test pass**
Given the changes are applied
When the server builds, an offline session is entered, a case is created offline, and the client goes back online
Then zero build errors; the placeholder appears in the offline UI; on sync, `SaveCaseAsync` receives a real `STATE-YEAR-NNNN` (verified via the audit log), and the offline case is no longer stuck if a `record_id_conflict` occurs (since the server now regenerates before writing)

**Dev Notes:**

- `OfflineSessionManager` already persists offline session state in service-worker storage. Adding a `next_offline_case_sequence` field alongside is a small extension. Start at `01` on new offline session; do not reset until the session ends.
- Detection regex must be case-insensitive (existing code paths uppercase the record ID at various points).
- The `year` fallback (AC-4) — if `home_record.date_of_death.year` is missing, using `DateTime.UtcNow.Year.ToString()` is the pragmatic choice; log it so we can measure occurrence.
- Two tabs offline at the same time in the same offline session are not a supported scenario per the team ("only a few cases created offline"). No cross-tab locking is required for `XX`.
- After enough time in production to know no legacy `-OFFLINE` caches remain, the transitional suffix-strip path can be removed in a follow-up housekeeping story.

---

### Story 29.7: Path C — IJE Batch Collision-Retry via `SaveCaseAsync`

**Story ID:** 29.7
**Depends on:** 29.4

As an operator running IJE batch imports,
I want each new-case write in the batch to route through the same `SaveCaseAsync` guard that the online UI uses, and to auto-regenerate the record ID on collision,
So that the batch cannot silently write duplicates when the tenant DB has more than 25 000 rows or when another writer wins a race against the batch's stale HashSet.

**Background:** `BatchItemProcessingService.Process_Message` currently writes each imported case via `_caseRepository.PutCaseDocumentJsonAsync(mmria_id, object_string, db_info)` — a raw DAL PUT that bypasses `CaseManager.SaveCaseAsync` entirely. Uniqueness relies on the stale `ExistingRecordIds` HashSet fetched once at batch start by `MMRIAServicesDAL.GetExistingRecordIds` (up to 25 000 rows from `_view/by_date_created`). If the DB exceeds 25 k rows, or if another process creates a case during the batch, the HashSet is stale and CouchDB's `_id` fresh-GUID PUT succeeds silently even though `home_record.record_id` duplicates another case.

**Acceptance Criteria:**

**AC-1 — Batch write path routes through `CaseManager.SaveCaseAsync`**
Given a batch item reaches the write step in `BatchItemProcessingService.Process_Message`
When the case is persisted
Then it is persisted via `CaseManager.SaveCaseAsync(caseData, changeStack, dbConfig, user, configuration, hostPrefix)` (with a synthetic `changeStack` note like `"vital_import"` and a service-account `ClaimsPrincipal`) rather than directly via `_caseRepository.PutCaseDocumentJsonAsync`

**AC-2 — Collision response triggers regeneration and retry**
Given the response's `error_code === "record_id_conflict"`
When the batch item processor handles the rejection
Then it calls `CaseManager.GenerateUniqueRecordIdAsync(state, year, dbConfig)`, updates `home_record.record_id` in the case document, and retries the save; cap at 5 attempts; on exhaustion, marks the batch item `ImportFailed` with `StatusDetail = "unable to generate unique record id after N attempts"`

**AC-3 — Final batch item record_id reflects post-retry value**
Given a batch item's record ID was regenerated during retry
When the batch item's final status is recorded
Then `BatchItem.mmria_record_id` carries the value that was actually persisted (not the pre-retry candidate) so users can trace the case in the UI

**AC-4 — Stale `GetExistingRecordIds` path is retired**
Given the DB-wide HashSet is no longer needed for uniqueness
When Story 29.7 is complete
Then `MMRIAServicesHelper.ConvertLineToBatchItem` no longer consumes `ExistingRecordIds` as a uniqueness guard; the parameter is either removed or documented as batch-local dedup only (within a single MOR file); `MMRIAServicesManager.GetExistingRecordIds` and `MMRIAServicesDAL.GetExistingRecordIds` are marked `[Obsolete]` if any other caller exists, or deleted if no other callers remain (grep the utilities repo test project first)

**AC-5 — In-batch dedup preserved**
Given two rows in the same MOR file happen to generate the same random suffix (rare but possible)
When the batch initialization loop assigns record IDs
Then a batch-local `HashSet<string>` (fresh, initialized empty each batch) prevents the collision within the file, so the AC-2 retry path only fires against cross-writer / cross-DB collisions, not intra-batch ones

**AC-6 — Build and IJE smoke test pass**
Given the changes are applied
When `mmria.services` and `mmria.common` build and a small IJE batch is imported against the local multi-tenant environment
Then zero build errors; every batch item that reports `NewCaseAdded` has a unique `home_record.record_id` in the DB; a forced-collision test (pre-seeding a duplicate case with a known suffix) confirms the retry path picks a fresh suffix and completes

**Dev Notes:**

- The `ClaimsPrincipal` for the batch service can be built from `mmria.services.vitalsimport.Program.timer_user_name` (already the identity used by other batch operations); confirm what `SaveCaseAsync` requires and mock the minimum claims needed.
- `changeStack` for a new-case save through `SaveCaseAsync` cannot be empty — reuse the existing `Change_Stack` shape used by `OfflineCaseManager.ApplyOfflineDocumentAsync` (single item, `object_path = "vital_import"`).
- `SaveCaseAsync` also writes `Change_Stack` audit entries via `IAuditRepository`. Confirm this is acceptable for IJE-imported cases (probably desired — audit trail attributed to the vitals import service account).
- Do not remove `GetExistingRecordIds` in this story if the utilities repo test project references it; deletion is a small follow-up.

---

## Epic 29 — Story Sequencing

| Story                                                | Risk   | Dependencies                                                                                                                       |
| ---------------------------------------------------- | ------ | ---------------------------------------------------------------------------------------------------------------------------------- |
| 29.1 — Server-side uniqueness guard                  | Low    | None — uses existing `RecordIdExistsAsync`                                                                                         |
| 29.2 — Client-side per-candidate API check           | Low    | None — uses existing `record_idController`                                                                                         |
| 29.3 — Add CouchDB view + remove dead code           | Low    | 29.2 must complete first (ensures `Get_Record_Id_List` has no call sites)                                                          |
| 29.4 — `GenerateUniqueRecordIdAsync` + `error_code`  | Low    | 29.1 (the guard populates the new field)                                                                                           |
| 29.5 — Path A save-then-retry-on-collision           | Low    | 29.4                                                                                                                               |
| 29.6 — Path B offline placeholder record IDs         | Medium | 29.4; touches offline session storage + sync path                                                                                  |
| 29.7 — Path C IJE batch collision-retry              | Medium | 29.4; changes IJE write path from raw DAL to `SaveCaseAsync`                                                                       |

29.1 and 29.2 can proceed in parallel. 29.3 depends on 29.2. 29.4 depends on 29.1 shipping. 29.5, 29.6, and 29.7 are independent of each other once 29.4 lands. 29.3 and 29.5 both touch `index.mmria.js`/`index.pmss.js` — sequence them or coordinate on the same file.

---

## Epic 30: Unified Server-Side Geocoding (TAMU Refactor)

**Goal:** Replace all scattered TAMU geocoding with a single `GeocodingManager` in SharedLibraries, per-location apply-methods in `CaseGeocodingManager`, and a unified API endpoint that geocodes and saves atomically. The vital import batch service shares the same manager. Client-side JS is reduced to thin wrappers. All duplicated urban-status logic is consolidated server-side.

---

### Background and Problem Statement

TAMU geocoding currently operates across four isolated layers with no shared logic:

**Layer A — Client-triggered (MMRIA_calculations.js):** 10 button-click handlers each call `$mmria.get_geocode_info()` → `GET /api/tamuGeoCode` → then apply results to `g_data` in ~100-line callback bodies. Each callback independently duplicates the urban-status calculation (Metropolitan / Micropolitan / Rural / Undetermined branching). The geocode + save is non-atomic — a network failure between the TAMU response and `$mmria.save_current_record()` leaves geocode data unwritten.

**Layer B — Legacy paths (mmria-check-code.js / validator.js):** 8 geocode calls in each file using the old 4-argument `get_geocode_info(street, city, state, zip, callback)` — the `census_year` parameter is absent, potentially producing stale census tract assignments.

**Layer C — Direct browser→TAMU (mmria.committee_member.js):** One `get_geocode_info` implementation that calls `geoservices.tamu.edu` directly from the browser, exposing the API key to the client. ⚠️ Security risk.

**Layer D — Server-side batch (BatchItemProcessingService.cs):** Private `get_geocode_info()` method instantiates `TAMUGeoCode` (which lives in `mmria.services`) and applies results through four private `Set_*_Geocode()` methods. Entirely isolated from Layers A–C with duplicated field-mapping logic.

**Existing assets to build on:**

- `mmria.common/texas_am/` — model types (`geocode_response`, `OutputGeocode`, `CensusValue`, etc.) already in common
- `mmria.common/SharedLibraries/Geocoding/Manager/` and `.../DAL/` — both folders exist and are **empty** — the intended home is already scaffolded
- `tamuGeoCodeController.cs` — secure server proxy with input sanitization and compiled regex guards — stays in place

---

### Geocoding Location Inventory

**Static form locations (10 total across Layers A and D):**

| Location Key                              | Case Document Path                                                       | Layer A JS Function                                                 | Layer D Method                              |
| ----------------------------------------- | ------------------------------------------------------------------------ | ------------------------------------------------------------------- | ------------------------------------------- |
| `dc_place_of_last_residence`              | `death_certificate/place_of_last_residence/...`                          | `geocode_dc_last_res`                                               | `Set_place_of_last_residence_Geocode`       |
| `dc_address_of_injury`                    | `death_certificate/address_of_injury/...`                                | `geocode_dc_injury_place`                                           | —                                           |
| `dc_address_of_death`                     | `death_certificate/address_of_death/...`                                 | `geocode_dc_death_place`                                            | `Set_address_of_death_Geocode`              |
| `bc_facility_of_delivery`                 | `birth_fetal_death_certificate_parent/facility_of_delivery_location/...` | `geocode_bc_delivery_place`                                         | `Set_facility_of_delivery_location_Geocode` |
| `bc_location_of_residence`                | `birth_fetal_death_certificate_parent/location_of_residence/...`         | `geocode_bc_residence`                                              | `Set_location_of_residence_Geocode`         |
| `pc_primary_care_facility`                | `prenatal_care_record/location_of_primary_prenatal_care_facility/...`    | `geocode_pc_primary_care_location`                                  | —                                           |
| `erh_location` _(dynamic list)_           | `er_visit_and_hospital_medical_records[i]/location/...`                  | `geocode_erh_location`                                              | —                                           |
| `omv_location_of_care` _(dynamic list)_   | `other_medical_office_visits[i]/location_of_medical_care_facility/...`   | `geocode_omov_location`                                             | —                                           |
| `mt_origin_address` _(dynamic list)_      | `medical_transport[i]/origin_information/address/...`                    | `medical_transport_origin_information_address_get_coordinates`      | —                                           |
| `mt_destination_address` _(dynamic list)_ | `medical_transport[i]/destination_information/address/...`               | `medical_transport_destination_information_address_get_coordinates` | —                                           |

**Geocode fields written at each location (same set for all 10):**
`latitude`, `longitude`, `feature_matching_geography_type`, `naaccr_gis_coordinate_quality_code`, `naaccr_gis_coordinate_quality_type`, `naaccr_census_tract_certainty_code`, `naaccr_census_tract_certainty_type`, `census_state_fips`, `census_county_fips`, `census_tract_fips`, `census_cbsa_fips`, `census_cbsa_micro`, `census_met_div_fips`, `urban_status`, `state_county_fips`

---

### Story 30.1 — Create `GeocodingManager` in SharedLibraries

**User Story:** As a developer, I need a single injectable service in `mmria.common` that calls the TAMU geocoding API and returns a fully-resolved `GeocodeResult` DTO (including derived `UrbanStatus` and `StateCountyFips`), so that all geocoding paths in the codebase share one implementation.

**Scope:**

- Create `mmria.common/SharedLibraries/Geocoding/Manager/GeocodingManager.cs`
- Create `GeocodeResult` record/class (can live in the same file or a sibling) holding all 15 output fields
- The urban-status derivation logic (Metropolitan Division / Metropolitan / Micropolitan / Rural / Undetermined / Unmatchable) moves here — calculated once
- `StateCountyFips` = `CensusStateFips + CensusCountyFips` derivation also moves here
- Method signature: `GeocodeResult FetchGeocode(string geocodeApiKey, string street, string city, string state, string zip, string censusYear)`
  - `geocodeApiKey` passed in at call time — manager has no config dependency (Architecture Rule 2.3)
  - State value split on `-` (e.g. `"GA-Georgia"` → `"GA"`) handled here
  - Returns an `Unmatchable` result (all fields empty, `UrbanStatus = "Undetermined"`) on any TAMU error rather than throwing
- The TAMU HTTP call is extracted from `TAMUGeoCode` in `mmria.services` into a private helper inside this manager (or the manager wraps the existing class — whichever avoids assembly coupling)
- `TAMUGeoCode` in `mmria.services/Utilities/` is left in place (not deleted) until Story 30.5 removes it

**Acceptance Criteria:**

**AC-1 — `GeocodingManager` exists in SharedLibraries**
Given the `mmria.common/SharedLibraries/Geocoding/Manager/` folder
When Story 30.1 is complete
Then `GeocodingManager.cs` exists and compiles in `mmria.common`

**AC-2 — `GeocodeResult` contains all required fields**
Given a successful TAMU response
When `FetchGeocode` is called with a valid address
Then the returned `GeocodeResult` has non-null/non-empty values for: `Latitude`, `Longitude`, `UrbanStatus`, `StateCountyFips`, and at least one Census FIPS field

**AC-3 — Urban status derivation is correct**
Given geocode results with `NAACCRCensusTractCertaintyCode` in range 1–6:

- When `CensusCbsaFips > 0` and `CensusMetDivFips` is non-empty → `UrbanStatus = "Metropolitan Division"`
- When `CensusCbsaFips > 0` and `CensusCbsaMicro == "0"` → `UrbanStatus = "Metropolitan"`
- When `CensusCbsaFips > 0` and `CensusCbsaMicro == "1"` → `UrbanStatus = "Micropolitan"`
- When `CensusCbsaFips` is empty → `UrbanStatus = "Rural"`
- When certainty code is 0 or outside 1–6 → `UrbanStatus = "Undetermined"`

**AC-4 — Unmatchable / error cases return gracefully**
Given a TAMU response with `FeatureMatchingResultType = "Unmatchable"` or an HTTP error
When `FetchGeocode` is called
Then a `GeocodeResult` is returned with `FeatureMatchingResultType = "Unmatchable"` and all other fields empty — no exception is thrown

**AC-5 — Build passes**
When `dotnet build mmria.common.csproj` is run
Then zero build errors

---

### Story 30.2 — Create `CaseGeocodingManager` with Per-Location Apply Methods

**User Story:** As a developer, I need named methods in SharedLibraries that apply a `GeocodeResult` to a specific case document location, so that both the web layer and the batch service can write geocode fields using a single implementation.

**Scope:**

- Create `mmria.common/SharedLibraries/Case/Manager/CaseGeocodingManager.cs`
- 10 public methods — one per location from the inventory table above
- Method signature for static locations: `void Apply_[LocationKey]_Geocode(ExpandoObject caseDoc, GeocodeResult result)`
- Method signature for dynamic-list locations: `void Apply_[LocationKey]_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)`
- Each method writes the 15 geocode fields to the correct case document path using path-based setters consistent with existing `C_Get_Set_Value` patterns
- When `result.FeatureMatchingResultType == "Unmatchable"`, all fields are written as empty strings (mirrors current JS behavior)
- No CouchDB access in this class — pure document mutation

**Acceptance Criteria:**

**AC-1 — All 10 methods exist and compile**
When `dotnet build mmria.common.csproj` is run
Then zero errors and all 10 `Apply_*_Geocode` methods are present

**AC-2 — Static location field paths are correct**
Given a valid `GeocodeResult` applied to `Apply_DC_PlaceOfLastResidence_Geocode`
When the resulting `ExpandoObject` is inspected
Then `death_certificate.place_of_last_residence.latitude` equals `result.Latitude` and all 15 fields are written to the correct path

**AC-3 — Dynamic list location uses `listIndex` correctly**
Given `Apply_ERH_Location_Geocode(caseDoc, result, listIndex: 2)`
When the resulting document is inspected
Then `er_visit_and_hospital_medical_records[2].location.latitude` equals `result.Latitude` — index 0 and index 1 entries are unmodified

**AC-4 — Unmatchable clears all fields**
Given `result.FeatureMatchingResultType == "Unmatchable"`
When any `Apply_*_Geocode` method is called
Then all 15 geocode fields at that path are written as empty string

---

### Story 30.3 — New API Endpoint: `POST /api/case-geocode/{caseId}/{locationKey}`

**User Story:** As an abstractor, when I click "Get Coordinates" on a case form, the geocoding and the case save should happen in a single server-side operation, so that geocode data is never lost due to a mid-operation network failure.

**Scope:**

- New `CaseGeocodeController` (or action added to existing case controller — confirm with team)
- Route: `POST /api/case-geocode/{caseId}/{locationKey}`
- Request body: `{ street, city, state, zip, listIndex? }` (JSON)
- Action flow:
  1. Resolve tenant config, get `geocode_api_key` via `configuration.GetSharedString`
  2. Call `GeocodingManager.FetchGeocode(...)`
  3. Load current case document from CouchDB
  4. Call the matching `CaseGeocodingManager.Apply_*_Geocode(caseDoc, result, listIndex?)` based on `locationKey`
  5. Save updated case document
  6. Return `GeocodeResult` as JSON (for the JS to update form fields)
- Requires `[Authorize(Roles = "abstractor")]`
- `tamuGeoCodeController` GET endpoint is **not removed** — kept for backward compatibility during migration
- Invalid `locationKey` → `400 BadRequest`
- Case not found → `404 NotFound`
- `listIndex` required when `locationKey` is a dynamic-list location → `400 BadRequest` if absent

**Acceptance Criteria:**

**AC-1 — Endpoint exists and returns geocode result**
Given a valid `caseId`, `locationKey = "dc_place_of_last_residence"`, and a valid US address
When `POST /api/case-geocode/{caseId}/dc_place_of_last_residence` is called
Then the response contains a `GeocodeResult` JSON object with `latitude` and `longitude` populated

**AC-2 — Case document is updated in CouchDB**
Given the request from AC-1
When the case is fetched from CouchDB after the POST
Then `death_certificate.place_of_last_residence.latitude` matches the returned value

**AC-3 — Dynamic list location requires `listIndex`**
Given `locationKey = "erh_location"` and no `listIndex` in the body
When the POST is made
Then the response is `400 BadRequest`

**AC-4 — Unknown `locationKey` returns 400**
Given `locationKey = "not_a_real_location"`
When the POST is made
Then the response is `400 BadRequest`

**AC-5 — Unauthorized request is rejected**
Given a request without an `abstractor` role
When the POST is made
Then the response is `401` or `403`

---

### Story 30.4 — Refactor `MMRIA_calculations.js` Geocode Functions

**User Story:** As a developer, I need the 10 client-side geocode handler functions in `MMRIA_calculations.js` to use the new server endpoint, so that geocoding and case save are atomic and urban-status calculation logic no longer lives in the browser.

**Scope:**

- Each of the 10 geocode functions is replaced with a thin wrapper:
  1. Read address fields from `this` / `g_data` as before
  2. `POST /api/case-geocode/{g_data._id}/{locationKey}` with address + optional `listIndex`
  3. On success: call `$mmria.set_control_value(...)` for the 15 geocode fields from the response (or re-render the form section)
  4. On failure: show an error dialog (reuse `$mmria.info_dialog_show`)
- The `get_geocode_info` function in `mmria.js` is **not removed** — it remains for backward compat (still used by Layer B until Story 30.6)
- The `geocode_dc_last_res` function (DC Place of Last Residence) retains the post-geocode CVS community vital signs lookup call — this call stays client-side after the geocode response returns
- The `census_year` argument passed to `get_geocode_info` previously is now included in the POST body
- For dynamic-list locations, `$global.get_current_multiform_index()` provides the `listIndex`
- The ~100-line urban-status calculation and field-setting blocks are removed from all 10 functions

**Acceptance Criteria:**

**AC-1 — All 10 functions POST to the new endpoint**
Given a button click on any geocode button in the case form
When the network tab is observed
Then a `POST /api/case-geocode/...` request is made (not `GET /api/tamuGeoCode`)

**AC-2 — Form fields are updated from response**
Given a successful geocode POST response
When the callback completes
Then the 15 geocode fields visible on the form reflect the values from the server response

**AC-3 — CVS lookup still fires for DC Place of Last Residence**
Given a successful geocode for `dc_place_of_last_residence`
When the form updates
Then `$mmria.get_cvs_api_data_info(...)` is still called with the returned `state_county_fips` and `census_tract_fips`

**AC-4 — Dynamic list index is sent correctly**
Given the user is on `medical_transport` list item at index 1
When the origin address geocode button is clicked
Then the POST body includes `listIndex: 1`

**AC-5 — Build and smoke test pass**
When the server is built and a geocode button is clicked in the local environment
Then the request succeeds, the case is updated, and no JS errors appear in the console

---

### Story 30.5 — Refactor `BatchItemProcessingService` to Use Shared `GeocodingManager`

**User Story:** As a developer, I need the vital import batch processing service to use the shared `GeocodingManager` and `CaseGeocodingManager` from SharedLibraries, so that geocoding logic is not duplicated between the batch service and the web layer.

**Scope:**

- Remove the private `get_geocode_info(string street, ...)` method from `BatchItemProcessingService`
- Remove the `GeocodeTuple` inner class — it is replaced by `GeocodeResult` from SharedLibraries
- Replace calls to private `Set_facility_of_delivery_location_Geocode`, `Set_location_of_residence_Geocode`, `Set_place_of_last_residence_Geocode`, `Set_address_of_death_Geocode` with calls to the corresponding `CaseGeocodingManager.Apply_*_Geocode()` methods
- `geocode_api_key` resolution stays in `BatchItemProcessingService` (resolved from `db_config_set.name_value["geocode_api_key"]` as today — per Architecture Rule 2.3)
- `TAMUGeoCode` class in `mmria.services/Utilities/TAMUGeocode.cs` is **deleted** after this story — its logic now lives in `GeocodingManager`
- The four private `Set_*_Geocode` methods in `BatchItemProcessingService` are removed

**Acceptance Criteria:**

**AC-1 — Private geocode methods and `GeocodeTuple` removed**
When a search is run for `GeocodeTuple` and `Set_facility_of_delivery_location_Geocode` in `BatchItemProcessingService.cs`
Then zero results are found

**AC-2 — `TAMUGeocode.cs` is deleted**
When `Get-ChildItem -Recurse -Filter TAMUGeocode.cs` is run
Then no file is found

**AC-3 — Batch processing produces correct geocode output**
Given the existing IJE import test suite (`mmria.services.tests`)
When the tests are run with `dotnet test`
Then all tests that previously passed continue to pass

**AC-4 — Build passes**
When `dotnet build mmria.services.csproj` is run
Then zero build errors

---

### Story 30.6 — Fix Legacy Geocode Calls in `mmria-check-code.js` / `validator.js`

**User Story:** As a developer, I need the legacy geocode functions (`x2f_ocl`, `x6b_ocl`, etc.) in `mmria-check-code.js` and `validator.js` to use the new server endpoint and pass `census_year`, so that census tract results are not stale and logic is consistent.

**Scope:**

- Identify all 8 call sites in each file
- Replace each `$mmria.get_geocode_info(street, city, state, zip, callback)` call with a POST to `/api/case-geocode/{id}/{locationKey}` (same pattern as Story 30.4)
- Add `census_year` from `g_data.home_record.date_of_death.year` (same source as `MMRIA_calculations.js`)
- The function names (`x2f_ocl`, etc.) are not changed — they are event handler names bound by the metadata

**Acceptance Criteria:**

**AC-1 — No 4-argument `get_geocode_info` calls remain**
When `Select-String` is run for `get_geocode_info` across `mmria-check-code.js` and `validator.js`
Then all remaining calls include `census_year` or use the new POST endpoint

**AC-2 — Census tract results use correct year**
Given a case with `date_of_death.year = 2015`
When a legacy geocode function fires
Then the census year sent to TAMU is `"2010"` (the correct bracket)

---

### Story 30.7 — Fix Direct Browser→TAMU Call in `mmria.committee_member.js` (Security)

**User Story:** As a security engineer, I need the `get_geocode_info` function in `mmria.committee_member.js` to route through the server proxy, so that the TAMU API key is not exposed in client-side code.

**Scope:**

- The `get_geocode_info` function in `mmria.committee_member.js` currently builds a direct `geoservices.tamu.edu` URL with the API key embedded
- Replace this implementation with a call to `GET /api/tamuGeoCode?...` (the existing secure proxy in `tamuGeoCodeController`) — same pattern as `mmria.js`
- Confirm no other client-side JS files contain a hardcoded `geoservices.tamu.edu` URL or API key
- The `tamuGeoCodeController` is retained as the proxy endpoint

**Acceptance Criteria:**

**AC-1 — No hardcoded TAMU URL in client JS**
When `Select-String -Recurse -Path wwwroot/scripts -Pattern "geoservices.tamu.edu"` is run
Then zero results are found

**AC-2 — API key not present in client JS**
When `Select-String -Recurse -Path wwwroot/scripts -Pattern "geocode_api_key|apikey="` is run
Then zero results are found in files served to the browser

**AC-3 — Committee member geocode still functions**
Given a committee member form with a geocodeable address field
When the geocode button is clicked
Then a request goes to `GET /api/tamuGeoCode` (observable in the network tab) and the fields are populated

---

## Epic 30 — Story Sequencing

| Story                                                    | Risk   | Dependencies                                                       |
| -------------------------------------------------------- | ------ | ------------------------------------------------------------------ |
| 30.1 — Create `GeocodingManager`                         | Low    | None                                                               |
| 30.2 — Create `CaseGeocodingManager` apply methods       | Low    | 30.1 must complete first                                           |
| 30.3 — New `POST /api/case-geocode` endpoint             | Medium | 30.1 and 30.2 must complete first                                  |
| 30.4 — Refactor `MMRIA_calculations.js`                  | Medium | 30.3 must complete first                                           |
| 30.5 — Refactor `BatchItemProcessingService`             | Low    | 30.1 and 30.2 must complete first; can run parallel to 30.3        |
| 30.6 — Fix legacy `mmria-check-code.js` / `validator.js` | Low    | 30.3 must complete first; can run parallel to 30.4                 |
| 30.7 — Fix `mmria.committee_member.js` (security)        | Low    | None — independent; only requires existing `tamuGeoCodeController` |

30.1 → 30.2 → 30.3 is the critical path. 30.5 can be worked in parallel with 30.3 once 30.1 and 30.2 are done. 30.6 and 30.7 are independent tail stories.

---

## Epic 31: Section 508 — Home Page General Section Keyboard Focus Indicators

Two `btn-link`-styled buttons in the General section of the Home page (`Views/Home/Index.cshtml`) have no visible keyboard focus indicator. A 508 accessibility review identified both as non-compliant with Section 508 and WCAG 2.1 SC 2.4.7 (Focus Visible).

### Root Cause

Bootstrap's `.btn-link:focus` rule in `index.css` sets `box-shadow: none`, canceling the `.btn:focus` box-shadow indicator. The only remaining focus style is `text-decoration: underline` — but both buttons already carry `text-decoration: underline` unconditionally via their inline `style` attribute. The result is **zero visible change** when either button receives keyboard focus.

**Affected elements (from `Views/Home/Index.cshtml`):**

| Element ID                                          | Text                                                            |
| --------------------------------------------------- | --------------------------------------------------------------- |
| `#view-informant-interview-summary-template-button` | View/Download Informant Interview Summary Template              |
| `#view-cdf-template-button`                         | View/Download MMRIA Committee Decisions Form (CDF) Template PDF |

### Story 31.1: Add `:focus-visible` Outline to General Section Buttons

As a keyboard-only user navigating the MMRIA Home page,
I want to see a clear visual indicator when either General section download button has keyboard focus,
So that I can tell which element is active and navigate the page with confidence.

**Acceptance Criteria:**

**Given** the user presses Tab to move keyboard focus to the "View/Download Informant Interview Summary Template" button (`#view-informant-interview-summary-template-button`)
**When** the button receives focus
**Then** a clearly visible, high-contrast outline (minimum 3 px, sufficient contrast against both the control and the surrounding background) is rendered around the button — the outline must be visually distinct from the button's non-focused appearance

**Given** the user presses Tab to move keyboard focus to the "View/Download MMRIA Committee Decisions Form (CDF) Template PDF" button (`#view-cdf-template-button`)
**When** the button receives focus
**Then** the same clearly visible, high-contrast outline is rendered around that button

**Given** the user navigates via mouse (no keyboard focus)
**When** the buttons are hovered or clicked
**Then** no new outline appears — the `:focus-visible` rule must not apply to mouse-initiated focus, preserving existing hover and click appearance

**Given** the fix is implemented by adding `:focus-visible` rules targeting `#view-informant-interview-summary-template-button` and `#view-cdf-template-button` in `index.scss`
**When** the developer adds the rules
**Then** the compiled `index.css` contains corresponding `:focus-visible` declarations — no inline styles, no JavaScript, no server-side changes

**Given** the site is verified in Edge and Chrome (NFR-1)
**When** the developer tabs through the General section
**Then** the focus outline is visible and consistent in both browsers

**Implementation note:** Add the following to `index.scss` (alongside existing focus rules such as `.info-icon:focus`):

```scss
#view-informant-interview-summary-template-button:focus-visible,
#view-cdf-template-button:focus-visible {
  outline: 3px solid #0056b3;
  outline-offset: 3px;
}
```

The color `#0056b3` (Bootstrap link-hover blue) provides ≥ 3:1 contrast against the white card background. No changes to `Views/Home/Index.cshtml`, controller code, or JavaScript are required.

---

## Epic 31 — Story Sequencing

| Story                                                          | Risk | Dependencies                             |
| -------------------------------------------------------------- | ---- | ---------------------------------------- |
| 31.1 — Add `:focus-visible` outline to General section buttons | Low  | None — CSS-only, no server or JS changes |

Single-story epic. No sequencing constraints.

---

## Epic 32: Export Consistency — Date Format, De-identification Parity, and Hospital Code Normalization

De-identified CSV exports produced from any MMRIA tenant are byte-consistent in date formatting, PII suppression, and coded-field rendering, regardless of which environment (FL production, multi-tenant dev, local) triggers the export.

This epic addressed three classes of discrepancy observed when comparing `fl_all` and `tenant1_all` de-identified exports of the same 1,695-case dataset:

- **Date format** _(open — Story 32.1)_: FL renders timestamps as `MM/dd/yyyy HH:mm:ss` (zero-padded, 24-hour); T1 renders them as `M/d/yyyy h:mm:ss AM/PM` (locale-dependent). Consuming tools cannot reliably parse both forms.
- **PII suppression** _(CLOSED — Story 32.2)_: FL's "fl" de-id list suppressed 6 PII field paths not present in the "global" fallback list. The global list was updated to 86 paths matching FL production. This eliminated 1,024 field differences and made `certificate_infant_fetal_section.csv` and `data-dictionary.csv` byte-for-byte identical with FL.
- **Hospital paternity code rendering** _(open — Story 32.3)_: `bfdcpdom_imnmhpabsit_hospi` shows specific coded values (1, 2, 7777) in FL but `9999` in T1 for 213 cases. Root cause is ambiguous; likely a data discrepancy tied to the NAT import integer type conversion (see Story 11.1).

**Remaining open differences after Story 32.2 closed** (4,115 real field differences, excluding cosmetic `export_jurisdiction_name`):
| Remaining Column | Rows | Root Cause | Story |
|---|---|---|---|
| `d_creat` | 1,695 | DateTime locale serialization | 32.1 |
| `dl_updat` | 1,695 | DateTime locale serialization | 32.1 |
| `hr_vitals_imp_date` | 459 | DateTime locale serialization | 32.1 |
| `dlc_out` | 53 | DateTime locale serialization | 32.1 |
| `bfdcpdom_imnmhpabsit_hospi` | 213 | Data or import discrepancy | 32.3 |

### Story 32.1: Normalize Datetime Serialization in CSV Export

As a data consumer,
I want all timestamp columns in every MMRIA CSV export to use a fixed, unambiguous format,
So that date parsing never depends on which server locale or culture produced the file.

**Background:**
`d_creat`, `dl_updat`, `dlc_out`, and `hr_vitals_imp_date` (and any other `datetime`-typed metadata fields) are deserialized from CouchDB JSON by Newtonsoft.Json into `System.DateTime` objects. Those objects are then assigned to `DataRow` columns of type `string`. Without an explicit format string, `DataRow` calls the system default `DateTime.ToString()`, which is culture-dependent — producing `MM/dd/yyyy HH:mm:ss` on FL's server and `M/d/yyyy h:mm:ss AM/PM` on T1's server. The agreed canonical output format is `MM/dd/yyyy HH:mm:ss` (matching current FL production output).

**Acceptance Criteria:**

**Given** any MMRIA CSV export is generated
**When** a field has metadata type `"datetime"` and the value in the CouchDB document is a parseable ISO 8601 timestamp
**Then** the exported CSV cell contains the value formatted as `MM/dd/yyyy HH:mm:ss` regardless of server locale or timezone

**Given** `d_creat`, `dl_updat`, `dlc_out`, `hr_vitals_imp_date` in `mmria_case_export.csv`
**When** exported from any tenant (FL, T1, local dev)
**Then** the format is `MM/dd/yyyy HH:mm:ss` (zero-padded month/day, 24-hour time) — matching existing FL production output

**Given** the flat-field loop in `mmrds_exporter.cs` processes a path whose `path_to_node_map[path].type.ToLower() == "datetime"`
**When** the deserialized value is a `System.DateTime` object
**Then** the row cell is set to `val.ToString("MM/dd/yyyy HH:mm:ss")` — not the default `val.ToString()`

**Given** `exporter.cs` also processes datetime-typed fields
**When** a datetime value is assigned to a CSV row
**Then** it uses the same explicit format string

**Given** the value in the CouchDB document is null or absent for a datetime field
**When** exported
**Then** the CSV cell is empty (existing behavior preserved)

**Implementation Notes:**

- Primary change: add an explicit `case "datetime":` branch in the flat-field `switch` in `mmrds_exporter.cs` (around line 640+, before `default:`) that calls `val.ToString("MM/dd/yyyy HH:mm:ss")` when `val` is `System.DateTime`, or passes through the raw string if val is already a string
- Verify `exporter.cs` flat-field loop applies the same pattern
- The grid-row switch in `mmrds_exporter.cs` (line ~2226) already uses `val.ToString("o")` for DateTime — leave that unchanged as it is a different output path

---

### Story 32.2: Add Missing PII Fields to Global Standard De-identification List — **CLOSED**

> **Status: Closed.** The global de-id list was updated manually to 86 paths matching FL production. Re-export of T1 confirmed: `certificate_infant_fetal_section.csv` and `data-dictionary.csv` are now byte-for-byte identical with FL; 1,024 field differences in `mmria_case_export.csv` eliminated. No further code or configuration work required for this story.

As a data steward,
I want the standard de-identified export to suppress the same PII fields on every tenant,
So that a de-identified export from any environment carries equivalent privacy protection.

**Background:**
The de-identified field list is stored in a CouchDB document at `/metadata/de-identified-list`. It is keyed by state code (e.g., `"fl"`) with a `"global"` fallback. The export-queue UI (`index.js`) calls `/api/de_identified_list?id=export`, extracts the host-state prefix from `location.host`, and selects that state's list or falls back to `"global"`.

FL (`fl-mmria.cdc.gov`) uses the `"fl"` list, which includes paths for Medical Examiner identifiers, delivery facility names, and birth certificate record numbers. T1 (`tenant1-mmria.local:12345`) has no matching key, so it falls back to `"global"`, which omits all of these fields. The `"global"` list must be updated to include all fields that appear in any state-specific list and represent PII.

**Fields confirmed suppressed in FL but absent from global (from export diff analysis):**

| Export Column       | MMRIA Path                                                                             | Description                                                   |
| ------------------- | -------------------------------------------------------------------------------------- | ------------------------------------------------------------- |
| `arrc_juris`        | `autopsy_report/report_coversheet/jurisdiction`                                        | Medical Examiner case number + name — highest-sensitivity PII |
| `bfdcpfodd_f_name`  | `birth_fetal_death_certificate_parent/facility_of_delivery_demographics/facility_name` | Delivery facility name                                        |
| `bcifsri_nmr_numbe` | `birth_fetal_death_certificate/record_identification/medical_record_number`            | Birth certificate / medical record number                     |
| `bcifsbad_fc_state` | `birth_fetal_death_certificate/birth_attendant_demographics/facility_city_state`       | Delivery facility city and state                              |
| `bfdcpdom_co_birth` | `birth_fetal_death_certificate_parent/demographic_of_mother/city_of_birth`             | Mother's birth city                                           |
| `bfdcpdof_co_birth` | `birth_fetal_death_certificate_parent/demographic_of_father/city_of_birth`             | Father's birth city                                           |

**Acceptance Criteria:**

**Given** the standard de-identification list is applied to a de-identified export
**When** the host-state key is absent from the list document (i.e., `"global"` fallback is used)
**Then** the export suppresses all six fields listed in the table above (cells are empty)

**Given** the `de-identified-list` CouchDB document is updated
**When** the global `paths` array is inspected
**Then** it contains entries for all six MMRIA paths above

**Given** a de-identified export is generated from T1 (local dev tenant)
**When** `arrc_juris` is compared against the same case in an FL de-identified export
**Then** both are empty (suppressed)

**Given** the `de-identified-list` document is seeded during environment setup
**When** the database initialization scripts run
**Then** the updated global list with all six paths is applied

**Given** the FL state-specific list already contains these paths
**When** the global list is updated
**Then** the FL state-specific list is left unchanged — no regression to FL behavior

**Implementation Notes:**

- Locate the `de-identified-list` document in the `metadata` CouchDB database (accessible via `/api/de_identified_list`)
- Add the six MMRIA paths to the `global` → `paths` array in that document
- Update the corresponding database-scripts seed file (check `source-code/mmria/mmria-server/database-scripts/` for the seeding JSON)
- Verify paths against `source-code/mmria/mmria-server/database-scripts/metadata.json` using `sass_export_name` to confirm exact path strings
- No code change is required in the exporter — `de_identified_set` already suppresses any path present in `de_identified_field_set`

---

### Story 32.3: Investigate and Resolve Hospital Paternity Field Code Discrepancy

As a data quality officer,
I want to understand why `bfdcpdom_imnmhpabsit_hospi` shows specific coded values in FL exports but `9999` in T1 exports for the same cases,
So that the correct behavior can be documented and both environments produce consistent output.

**Background:**
The export diff shows `bfdcpdom_imnmhpabsit_hospi` ("if mother not married, has paternity acknowledgement been signed in the hospital") differs in 213 cases: FL has specific codes (`1`, `2`, `7777`) while T1 has `9999` (the MMRIA sentinel for "blank/not answered"). Two root causes are possible:

- **Data discrepancy**: T1's test data was entered with this field left blank for those 213 cases, while FL's production cases have actual answers. In this case the exporter is correct and the fix is a data migration/seeding update.
- **De-identification transform**: FL or T1 applies a post-read transform that replaces specific values with `9999` (or vice versa) for this field. In this case the exporter needs to be corrected to apply the transform consistently.

**Acceptance Criteria:**

**Given** the investigation is complete
**When** the root cause is confirmed
**Then** a documented determination is recorded: "data discrepancy" OR "exporter transform inconsistency"

**Given** root cause is confirmed as "exporter transform inconsistency"
**When** a de-identified export is generated from T1
**Then** `bfdcpdom_imnmhpabsit_hospi` renders coded values (`1`, `2`, `7777`) matching FL output — not `9999`

**Given** root cause is confirmed as "data discrepancy"
**When** T1 test case data is examined for the 213 affected cases
**Then** the field is confirmed blank (`9999` or absent) in the T1 CouchDB documents, and the FL CouchDB documents have specific coded values — proving the delta is data, not code

**Given** root cause is "data discrepancy" and T1 is being used as a parity test environment
**When** the 213 case documents in T1 are examined
**Then** a data correction plan is documented (e.g., update the T1 seed data to include representative coded values for this field)

**Investigation Steps:**

1. Fetch one of the 213 differing case documents directly from both the FL CouchDB instance and the T1 CouchDB instance (use `_id` as the case key)
2. Inspect `birth_fetal_death_certificate_parent/demographic_of_mother/if_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital` in both documents
3. If FL has `"1"` / `"2"` / `"7777"` and T1 has `"9999"` or null → data discrepancy confirmed
4. If both have the same raw value but export differently → exporter transform confirmed
5. Check whether this path appears in any de-identification list or any special-casing in `mmrds_exporter.cs`

---

## Epic 32 — Story Sequencing

| Story                                                   | Risk   | Status     | Dependencies                                                |
| ------------------------------------------------------- | ------ | ---------- | ----------------------------------------------------------- |
| 32.1 — Normalize datetime serialization in exporter     | Low    | Open       | None — isolated change in `mmrds_exporter.cs`/`exporter.cs` |
| 32.2 — Add missing PII fields to global de-id list      | Medium | **Closed** | Resolved manually — global list updated to 86 paths         |
| 32.3 — Investigate hospital paternity field discrepancy | Low    | Open       | Requires CouchDB access; likely data-only finding           |

32.1 and 32.3 are independent and can be worked in parallel. 32.3 is investigation-first; if it confirms the field values in T1 are simply missing data (9999 = blank from NAT import), no code change is needed and the story closes with a documented finding.

---

## Epic 33: Case Generator Date and Number Plausibility

`mmria-case-generator` produces test cases that are useful for regression testing without filling date and number fields with impossible or type-hostile values. The generator remains metadata-driven and low-impact: no production metadata changes, no generated strong-case model edits, no new external service, and no broad rewrite of the case generation pipeline.

### Background and Problem Statement

The CLI in `../nccdphp-drh-mmria-utilities/mmria-case-generator` is a thin wrapper around `mmria-tools/Testing/CaseGeneration`. The core paths are:

| Area               | Current File                                                                                                           |
| ------------------ | ---------------------------------------------------------------------------------------------------------------------- |
| Case orchestration | `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/CaseDataGenerator.cs`                    |
| Numeric values     | `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/ValueGenerators/NumberValueGenerator.cs` |
| Date/time values   | `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/ValueGenerators/DateValueGenerator.cs`   |
| Validation         | `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Utilities/MetadataConstraintValidator.cs`           |
| Workflow gate      | `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Services/CaseGeneratorService.cs`                   |

The current generator already avoids many hard failures by using numeric-looking strings and `DateTime`-derived date groups, but it has four data-quality gaps:

- Populated `number` fields are emitted mostly as strings rather than JSON numbers.
- Numeric plausibility is driven by broad field-name heuristics, so fields like `height_feet` can receive inch-like values.
- Many date groups are valid calendar dates but are generated independently, so pregnancy, prenatal, admission, discharge, and death timelines can be clinically implausible.
- `ValidateBeforeSave` is shallow: it does not recursively validate nested forms, groups, grids, or multiforms by full metadata path, and validation errors do not currently block JSON/CouchDB output.

### Scope

Included:

- `mmria-case-generator` CLI behavior only where needed to exercise or report generation validity.
- `mmria-tools/Testing/CaseGeneration` generation and validation internals.
- Unit/integration-style generator tests in `../nccdphp-drh-mmria-utilities/mmria-server.tests`.

Excluded:

- Production form metadata changes.
- Hand edits to generated `mmria_case*.cs` files.
- A full clinical scenario engine.
- New CouchDB documents or admin UI.
- Test data distribution tuning unrelated to dates and numbers.

### Story 33.1: Metadata-Aware Numeric Generation

As a developer or tester using generated MMRIA cases,
I want populated numeric fields to contain numeric, plausible values,
So that generated cases exercise realistic workflows without poisoning tests with obvious invalid data.

**Acceptance Criteria:**

**Given** a metadata node has `type = "number"`
**When** the generator populates the field
**Then** the emitted JSON value is numeric (`int`, `double`, or nullable numeric), not an arbitrary text string

**Given** a non-required numeric field is intentionally left blank by strategy completeness
**When** the case is serialized
**Then** the existing blank convention is preserved only for intentional blank values, and validation does not treat intentional blanks as invalid numbers

**Given** a numeric metadata node has `decimal_precision = "0"`
**When** the generator produces a populated value
**Then** the value is an integer-compatible number with no fractional component

**Given** a numeric metadata node has `decimal_precision = "1"` or another supported precision
**When** the generator produces a populated value
**Then** the value is rounded to that precision using invariant-culture numeric formatting/parsing rules

**Given** a high-risk clinical or date-adjacent numeric field is generated
**When** the field name/path matches known patterns
**Then** the generator uses plausible ranges, including:

| Field Pattern                                                              | Plausible Range                                                 |
| -------------------------------------------------------------------------- | --------------------------------------------------------------- |
| `height_feet`                                                              | 4-6                                                             |
| `height_inches`                                                            | 0-11                                                            |
| generic adult `height` in inches                                           | 58-74                                                           |
| `weight`, `pre_pregnancy_weight`, `weight_at_delivery`, `admission_weight` | 90-350                                                          |
| `birth_weight`, `fetal_weight` in grams                                    | 500-5000                                                        |
| `bmi`                                                                      | 15.0-60.0                                                       |
| `age`, `maternal_age`, `mother_age`                                        | maternal 12-55 edge / 18-45 normal; generic age remains bounded |
| `gestational_age_weeks` / `gestational_age`                                | 0-45, with pregnancy-specific defaults favoring 24-42           |
| `gestational_age_days`                                                     | 0-6                                                             |
| `days_postpartum`                                                          | 0-365                                                           |
| Apgar score fields (`minute_5`, `minute_10`)                               | 0-10                                                            |
| systolic blood pressure                                                    | 70-250                                                          |
| diastolic blood pressure                                                   | 30-150                                                          |
| pulse / heart rate                                                         | 30-220                                                          |
| respiration                                                                | 6-60                                                            |
| oxygen saturation                                                          | 50-100                                                          |
| temperature                                                                | 90.0-107.0                                                      |

**Given** no special range applies
**When** a populated number is generated
**Then** the existing broad fallback behavior remains bounded and numeric.

**Implementation Notes:**

- Prefer a small range-selection helper inside the existing numeric generator path over a new subsystem.
- Use metadata path when available; field name alone is not enough for repeated names such as `age`, `value`, `weight`, `month`, and `day`.
- Preserve current strategy behavior for blank optional fields.

---

### Story 33.2: Date Group Validity and Timeline Plausibility

As a developer or tester using generated MMRIA cases,
I want date values to be valid and mostly chronological,
So that generated data supports workflow and reporting tests without impossible timelines.

**Acceptance Criteria:**

**Given** a metadata group contains `month`, `day`, and `year` children
**When** the generator populates the group
**Then** the combined components always form a valid calendar date, or all populated components use the accepted blank sentinel convention

**Given** a date group contains only `month` and `year`
**When** the generator populates the group
**Then** only the metadata-defined components are emitted; the generator does not add a `day` component that is absent from metadata

**Given** `home_record/date_of_death` is generated
**When** related fields are generated or post-processed
**Then** maternal date of birth, death certificate date of birth, and record ID year remain consistent with date of death

**Given** pregnancy-related date groups are present
**When** `date_of_last_normal_menses`, prenatal visit dates, delivery dates, and estimated confinement dates are generated
**Then** they are internally plausible relative to the case timeline: LMP precedes prenatal visits and delivery-related dates; prenatal visit dates do not occur after date of death; gestational age fields align with nearby date groups when both are populated

**Given** ER/hospital admission and discharge date groups are present in a generated record
**When** both fields are populated
**Then** admission is not after discharge, and both dates remain near the case's pregnancy/death timeline

**Given** generic `date`, `datetime`, and `time` metadata fields are generated
**When** the strategy is not explicitly edge-case focused
**Then** future dates are avoided unless the field semantics require a future projection

**Given** the edge strategy is used
**When** edge dates are emitted
**Then** edge values remain valid calendar dates and are constrained to intentional edge cases documented in the generator tests.

**Implementation Notes:**

- Keep the current post-processing approach but centralize reusable date-group helpers enough to avoid component drift.
- Use `DateOnly`/`DateTime` construction before assigning components; never construct components independently.
- Do not create a comprehensive clinical scenario engine in this epic.

---

### Story 33.3: Recursive Date and Number Validation Gate

As a developer running the case generator with `ValidateBeforeSave = true`,
I want invalid generated date and number values to stop the run before output,
So that bad generated cases are caught at the generator boundary instead of being written to JSON files or CouchDB.

**Acceptance Criteria:**

**Given** a generated case contains nested forms, groups, grids, or multiform instances
**When** validation runs
**Then** every generated value is validated recursively using the full metadata path from `MetadataManager.NodeDictionary`

**Given** a field has metadata `type = "number"`
**When** a populated value is not a JSON number or cannot be parsed as a number under invariant-culture rules
**Then** validation records an error with the full path and case number

**Given** a field has metadata `type = "date"`, `type = "datetime"`, or `type = "time"`
**When** a populated value cannot be parsed into the expected date/time type
**Then** validation records an error with the full path and case number

**Given** a group has date components
**When** the populated month/day/year combination is impossible (for example February 30) or partial in an unsupported way
**Then** validation records an error with the full path and component values

**Given** `ValidateBeforeSave = true` and validation errors exist
**When** `CaseGeneratorService.GenerateCasesAsync` completes validation
**Then** the result is unsuccessful and JSON/CouchDB output is skipped

**Given** `ValidateBeforeSave = false`
**When** generated data contains validation errors
**Then** existing permissive behavior is preserved, but validation is not silently implied.

**Implementation Notes:**

- Do not collect metadata by bare node name only; repeated names collide.
- Validation warnings may still be reported for suspicious-but-allowed values, but number/date parse failures should be errors.
- Keep the validation result consumable by the CLI summary.

---

### Story 33.4: Generator Regression Coverage for Date and Number Fields

As a maintainer,
I want focused tests around date and number generation,
So that future generator changes do not reintroduce invalid dates, non-numeric numbers, or shallow validation.

**Acceptance Criteria:**

**Given** the generator runs with a fixed random seed
**When** test metadata includes simple number fields, grouped date fields, grids, and multiforms
**Then** tests assert populated numeric fields are numeric and populated date fields are parseable

**Given** test metadata includes `decimal_precision = "0"` and `decimal_precision = "1"`
**When** numeric values are generated
**Then** tests assert generated values honor the requested precision

**Given** test metadata includes `height_feet`, `height_inches`, gestational age, Apgar, vital-sign, and BMI-like fields
**When** numeric values are generated
**Then** tests assert values fall within the agreed plausible ranges

**Given** intentionally invalid nested values are passed to the validator
**When** validation runs
**Then** tests prove recursive validation catches the invalid values and reports full metadata paths

**Given** `ValidateBeforeSave = true`
**When** validation errors are present
**Then** tests prove output writers are not called.

**Implementation Notes:**

- Prefer unit tests that construct minimal metadata objects over tests requiring a live CouchDB instance.
- Add one integration-style generator test only if needed to cover `CaseGeneratorService` gating.
- Existing broad `Scenario_A_CaseGenerator` coverage is not sufficient for this epic because it depends on CouchDB and asserts generation/save success, not date/number plausibility.

---

## Epic 33 — Story Sequencing

| Story                                                | Risk       | Dependencies                                                                            |
| ---------------------------------------------------- | ---------- | --------------------------------------------------------------------------------------- |
| 33.1 — Metadata-aware numeric generation             | Low-Medium | None                                                                                    |
| 33.2 — Date group validity and timeline plausibility | Low-Medium | None; coordinate with 33.1 only where date groups contain gestational numeric fields    |
| 33.3 — Recursive validation gate                     | Medium     | Can start after validator path design is agreed; benefits from 33.1 and 33.2 test cases |
| 33.4 — Generator regression coverage                 | Low        | Develop alongside 33.1-33.3; must be complete before epic close                         |

33.1 and 33.2 can be implemented independently. 33.3 should be integrated after the generator changes are understood so the validator distinguishes intentional blanks from invalid generated values. 33.4 runs throughout the epic and is the closeout proof that generated date and number data is mostly plausible.

---

## Epic 34: Case Narrative PDF Spacing Fidelity and Paste Content Fidelity

Reviewers can edit a case narrative, paste content from plain-text sources with line structure intact, use Enter to continue numbered paragraphs, and export the Narrative PDF — all without extra spacing, collapsed line breaks, or numbered-list handling issues. PDF fixes stay in `pdf-version/index.js`; paste fixes stay in `attach_narrative_paste_handler` in `textarea.js`.

### Story 34.1: Normalize Case Narrative PDF Whitespace Conversion

As a case reviewer,
I want edited case narrative HTML to render in the PDF with normal paragraph spacing,
So that adding a line in the narrative editor does not make the exported PDF harder to read.

**Acceptance Criteria:**

**Given** a saved narrative matching `docs/ai/local/case-narrative-spacing/changed-prod-data-v4.1.txt`
**When** `convert_html_to_pdf(...)` walks the HTML for `case_opening_overview`
**Then** whitespace-only text nodes that exist only between top-level or block tags do not produce visible PDF rows or extra paragraph spacing.

**Given** an edited narrative contains `<p><br></p>` as an intentional blank paragraph
**When** the narrative is converted for PDF
**Then** the PDF representation contains one intentional blank line for that paragraph, not both the `<br>` newline and the paragraph trailing newline.

**Given** inline formatting such as `<strong>This</strong> is a <strong>test</strong>`
**When** PDF conversion normalizes whitespace
**Then** meaningful inline text spacing is preserved and words do not collapse together.

**Given** the stored narrative HTML is read from `g_data.case_narrative.case_opening_overview`
**When** the fix runs
**Then** the stored HTML, Trumbowyg editor output, and save sanitizer behavior are not changed.

**Given** the unchanged production narrative fixture in `docs/ai/local/case-narrative-spacing/unchanged-prod-data.txt`
**When** the PDF conversion runs
**Then** its paragraph spacing does not become tighter than the original production PDF.

**Implementation Notes:**

- Primary file: `source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js`.
- Primary functions: `convert_html_to_pdf(...)` and `ConvertHTMLDOMWalker(...)`.
- The likely fix point is the `#TEXT` branch in `ConvertHTMLDOMWalker`: ignore structural whitespace-only separator nodes, but do not globally trim or drop inline spacing.
- Treat `<p><br></p>` and equivalent empty paragraph nodes as one blank line in the PDF conversion path.
- Do not change `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/textarea.js` unless a later implementation investigation proves the PDF-only fix cannot satisfy the acceptance criteria.

**Evidence:**

- `changed-prod-data-v4.1.txt` has zero newlines, one `<p><br></p>`, and 29 literal `> <` inter-tag spaces after the edit.
- `unchanged-prod-data.txt` has 70 newlines, no `<p><br></p>`, and zero literal `> <` inter-tag spaces.
- `pdf-version/index.js` currently pushes `#TEXT`, `P`/`DIV`, and `BR` nodes as PDF text/newline content, which explains why edited structural whitespace can inflate PDF spacing.

### Story 34.2: Collapse BR Plus Empty Paragraph Separators

As a case reviewer,
I want QA narrative template section breaks to render with normal spacing in the PDF,
So that section headings are not pushed apart by duplicate blank rows after editing or saving the narrative.

**Acceptance Criteria:**

**Given** a saved narrative matching `docs/ai/local/case-narrative-spacing/qa/html.txt`
**When** `convert_html_to_pdf(...)` walks the HTML for `case_opening_overview`
**Then** each top-level `<br>` immediately followed by a whitespace-only empty paragraph separator renders as one intentional break in the PDF output, not two visible blank rows.

**Given** a body-level `<br>` is not adjacent to an empty paragraph separator
**When** the narrative is converted for PDF
**Then** existing intentional line-break behavior is preserved.

**Given** a paragraph contains visible text, inline formatting, NBSP, or meaningful inline whitespace
**When** PDF conversion normalizes empty separators
**Then** meaningful text and inline spacing are preserved; words do not collapse together.

**Given** the Story 34.1 fixtures still exist in `docs/ai/local/case-narrative-spacing/`
**When** regression verification runs
**Then** the prior fixes for body-level whitespace-only `#TEXT` nodes and `<p><br></p>` blank paragraphs still pass.

**Given** the stored narrative HTML is read from `g_data.case_narrative.case_opening_overview`
**When** the fix runs
**Then** the stored HTML, Trumbowyg editor output, and save sanitizer behavior are not changed.

**Implementation Notes:**

- Primary file remains `source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js`.
- Primary functions remain `convert_html_to_pdf(...)` and `ConvertHTMLDOMWalker(...)`.
- Story 34.1 handled two shapes: body-level whitespace-only `#TEXT` nodes and `<p><br></p>` blank paragraphs.
- The QA fixture introduces a third shape: repeated `</p><br><p>\r\n</p><p><strong>...` separators.
- The current `BR` branch emits `{ text: "\n" }`, and the current `P`/`DIV` branch appends its own trailing newline. That means a `<br>` followed by an empty paragraph can still become two visible blank rows.
- Keep the change in the PDF conversion path. Do not normalize stored narrative HTML, editor output, Trumbowyg configuration, or save-path sanitizer behavior.

**Evidence:**

- `docs/ai/local/case-narrative-spacing/qa/html.txt` has 12 `<br>` tags, 12 whitespace-only empty paragraphs, and 11 repeated `<br>` plus empty-paragraph separators.
- The QA editor screenshot shows compact editor spacing, supporting the conclusion that the remaining defect is in PDF interpretation rather than editor display.
- Story 34.1 was marked complete, and the QA symptom still reproduces with a new fixture shape.

### Story 34.3: Preserve Line Structure When Pasting Plain Text

As a case reviewer,
I want pasted plain-text content to maintain its line breaks in the case narrative editor,
So that text copied from notes, email, or other plain-text sources does not collapse into a single paragraph.

**Acceptance Criteria:**

**Given** clipboard contains `text/plain` only (no `text/html`) with content such as "Line 1\nLine 2\nLine 3"
**When** the reviewer pastes into the narrative editor
**Then** each line appears as a separate `<p>` paragraph — the line structure of the pasted content is preserved

**Given** clipboard plain text contains an empty line between content (e.g., "Line 1\n\nLine 3")
**When** the reviewer pastes
**Then** the empty line becomes `<p><br></p>`, consistent with Trumbowyg's standard blank-line representation

**Given** clipboard plain text contains manually-numbered items (e.g., "1. First item\n2. Second item\n3. Third item")
**When** the reviewer pastes
**Then** each numbered item appears on its own line (separate `<p>` element), preserving the number prefix

**Given** the clipboard contains rich-text HTML (e.g., from Word or a browser)
**When** the reviewer pastes
**Then** the existing `text/html` paste path is unchanged — no regression to rich-text paste behavior

**Given** the paste produces multiple `<p>` paragraphs and `tbw_onchange` serializes the result
**When** the content is saved
**Then** the stored HTML contains `<p>` elements for each pasted line — no raw `\n` characters embedded in text node content

**Implementation Notes:**

- Change is confined to the `else if (pastedText)` branch in `attach_narrative_paste_handler` in `textarea.js`
- Split `pastedText` by `/\r?\n/`; non-empty lines → `createElement('p')` with `textContent`; empty lines → `createElement('p')` with `innerHTML = '<br>'`
- Use the block-safe sibling insertion path (same as the existing `fragmentHasBlocks` path) — do NOT use `range.insertNode` for multi-paragraph plain text
- Do NOT insert `<br>` tags as line separators — `<p>` elements only, so the PDF spacing behavior from Stories 34.1 and 34.2 is not disturbed
- Do NOT change `textarea_control_strip_html_attributes`, the save path, or `pdf-version/index.js`

**Files:** `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/textarea.js`

| Dependency                                                                                             | Risk |
| ------------------------------------------------------------------------------------------------------ | ---- |
| Stories 34.1 and 34.2 complete — `<p>` paragraphs created here will not produce new PDF spacing issues | Low  |
| Story 1.1 save-path fix — structural tags preserved on save path, no regression                        | Low  |

### Story 34.4: Numbered-Paragraph Keyboard Continuation

As a case reviewer,
I want the case narrative editor to recognize manually-numbered paragraphs and continue the sequence when I press Enter,
So that I can build numbered items efficiently without typing the next number manually.

**Acceptance Criteria:**

**Given** a paragraph in the editor begins with `\d+. ` (a number, period, space) followed by real content (e.g., "3. Some text")
**And** the cursor is at the END of that paragraph
**When** the reviewer presses Enter (no Shift, Ctrl, or Alt modifier keys)
**Then** a new paragraph is inserted immediately after, beginning with the next sequential number followed by ". " (e.g., "4. ")
**And** the cursor is placed immediately after that prefix, ready for typing

**Given** the cursor is mid-paragraph (not at the end) in a numbered paragraph
**When** the reviewer presses Enter
**Then** standard paragraph-split behavior applies — no number prefix is auto-inserted

**Given** a paragraph begins with `\d+. ` but its content after the prefix is empty or whitespace-only (e.g., "3. " with nothing following)
**When** the reviewer presses Enter
**Then** auto-continuation does NOT fire — standard Enter behavior applies, signaling exit from the numbered sequence

**Given** a paragraph begins with a number prefix (e.g., "1. text")
**When** the reviewer clicks or keyboards to any character position in that paragraph, including position 0 (before the digit)
**Then** the cursor moves freely to that position without restriction
**Note:** If implementation investigation finds a browser or Trumbowyg mechanism preventing cursor-at-position-0, it must be identified and removed. Document findings either way.

**Given** a reviewer types a number prefix manually (e.g., "4. text") and saves
**When** the case is reopened
**Then** the paragraph `<p>4. text</p>` is preserved exactly — no structure change occurs on save

**Implementation Notes:**

- Attach a `keydown` listener on the editor element within the same init block as the paste handler in `attach_narrative_paste_handler`
- Pattern to match: current paragraph `textContent` matches `/^(\d+)\.\s\S/` (digit(s) + period + space + at least one non-whitespace character)
- If AC-1 triggers: call `event.preventDefault()`, create a new `<p>` element with text `(parsedN + 1) + '. '`, insert it after the current paragraph using `insertBefore` / `appendChild` on the parent, then move caret to end of new `<p>` via `Range`
- AC-3 exit guard: if `paragraph.textContent.trim()` matches only the prefix with no further content, skip auto-continuation
- Do NOT use `<ol>/<li>` structure — numbers remain plain text in `<p>` elements, consistent with the no-new-tags constraint
- Verify cursor-at-position-0 behavior; remove any blocking mechanism found; add a comment if no blocker exists

**Files:** `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/textarea.js`

| Dependency                                                                                       | Risk |
| ------------------------------------------------------------------------------------------------ | ---- |
| Story 34.3 (plain-text paste creates numbered `<p>` paragraphs that this story then operates on) | Low  |

## Epic 34 - Story Sequencing


| Story | Risk | Dependencies |
|---|---|---|
| 34.1 - Normalize case narrative PDF whitespace conversion | Medium | Existing case narrative editor fidelity behavior from Epic 1; supplied spacing fixtures |
| 34.2 - Collapse BR plus empty paragraph separators | Medium | Story 34.1 PDF converter changes; QA spacing fixture in `docs/ai/local/case-narrative-spacing/qa/html.txt` |
| 34.3 - Preserve line structure when pasting plain text | Low | Stories 34.1 and 34.2 complete; Story 1.1 save-path fix |
| 34.4 - Numbered-paragraph keyboard continuation | Low | Story 34.3 (paste creates numbered paragraphs that 34.4 operates on) |

Stories 34.1 and 34.2 (complete) address PDF rendering. Story 34.3 fixes the editor paste path and is independent of PDF behavior. Story 34.4 builds on the clean numbered paragraphs that Story 34.3 produces and should follow it.

---

## Epic 35: Offline Exit/Go Online UX Cleanup (Rel 4.1 P-Immediate)

While a user is in offline mode, the confusing "Exit Offline Mode" escape hatch is hidden (not deleted) so it can be revisited in a future release. The "Go Online" action is renamed everywhere to "Go Online & Sync Changes" for clarity, and both entry points to that action now consistently confirm via modal before syncing. Originated from Katrina's Rel 4.1 feedback, tracked as ADO #119294 (Marta Puskarz). Investigation case file: `_bmad-output/implementation-artifacts/investigations/epic-35-remove-exit-offline-rename-go-online-investigation.md`.

### Story 35.1: Hide Exit Offline Mode Widget

As a case reviewer working in offline mode,
I want the "Exit Offline Mode" button to no longer appear while still seeing a clear "You're Offline" indicator,
So that I am not tempted into an escape hatch that discards my offline changes, while the underlying code stays available for a future release.

**Acceptance Criteria:**

**Given** a user is in offline mode (`isOfflineModeActive()`, `isProcessingOfflineCases()`, or `isOfflineModeServerSession()` is true)
**When** any page renders a `[data-offline-exit-host]` widget host (Home/Index, Account/OfflineLogin, Shared/_BreadCrumbs)
**Then** no "Exit Offline Mode" button is visible anywhere on the page.

**Given** a user is in offline mode
**When** the widget renders
**Then** the "You're Offline" text and icon indicator remain visible — the widget is not hidden in its entirety, only the button is removed from view.

**Given** the button is no longer shown
**When** the "You're Offline" indicator renders in any of the 3 host locations
**Then** it is right-aligned within its host container, consistent with each host's existing right-side layout conventions (e.g. the breadcrumbs row's `justify-content: space-between` and the offline-login row's `justify-content-end`).

**Given** the "Exit Offline Mode" button is hidden
**When** the codebase is reviewed
**Then** `OfflineExitManager`'s underlying logic (`confirmExitOfflineMode`, cleanup/audit calls) and the `show_exit_offline_mode_modal` confirmation modal in `offline-modals.js` remain fully present and unmodified in behavior — only the button's markup/visibility and the indicator's alignment change.

**Given** a user was mid-flow with a pending deferred cleanup (`mmria_exit_offline_cleanup_pending`) before this change ships
**When** they return online
**Then** `finishPendingCleanup()` still runs unaffected, since only the button's rendering is suppressed, not the cleanup pipeline.

**Implementation Notes:**
- Primary file: `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-exit-manager.js`.
- Fix point is `renderWidgetMarkup()`: remove the `<button data-action="show-exit-offline-mode">` element from the returned template string; keep the icon + `<span>You're Offline</span>` div. Do **not** change `shouldShowExitWidget()` — it correctly continues to gate the remaining indicator's visibility to "while offline."
- Add right-alignment styling to the outer `.mmria-offline-exit-widget` wrapper (e.g. `width: 100%; justify-content: flex-end;`) so the remaining text/icon sits flush right now that the button no longer occupies that space — most impactful on `Views/Home/Index.cshtml`, whose host `<div>` has no existing flex/alignment styling of its own.
- Do not remove the `[data-offline-exit-host]` host `<div>` elements from `Views/Home/Index.cshtml`, `Views/Account/OfflineLogin.cshtml`, or `Views/Shared/_BreadCrumbs.cshtml` — leave them in place.
- Do not delete `offline-modals.js`'s `show_exit_offline_mode_modal`, `OfflineExitManager.confirmExitOfflineMode`, or any server cleanup call (`/api/OfflineCase/update-offline-state`, `/api/OfflineCase/release-case-locks`) — this is a hide-only change per explicit user decision.

**Evidence:**
- `offline-exit-manager.js:404-421` (`renderWidgetMarkup`), `:426-441` (`initializeWidgetHosts`), `:576-580` (`shouldShowExitWidget`).
- Host divs: `Views/Home/Index.cshtml:167`, `Views/Account/OfflineLogin.cshtml:43`, `Views/Shared/_BreadCrumbs.cshtml:25`.

### Story 35.2: Rename Go Online to Go Online & Sync Changes

As a case reviewer working in offline mode,
I want the action to return online to be labeled "Go Online & Sync Changes",
So that it's clear the action both reconnects me and syncs my offline work.

**Acceptance Criteria:**

**Given** the "Offline Case List" table footer or the "Cases Selected for Offline Work" table footer is rendered while offline
**When** the primary action button is displayed
**Then** its label reads "Go Online & Sync Changes" instead of "Go Online".

**Given** the button is clicked and connectivity is being checked or the transition is in progress
**When** `set_go_online_button_state(isBusy)` or the connectivity-driven handler in `offline-network-monitor.js` updates the button text
**Then** the idle-state text reads "Go Online & Sync Changes" (the busy-state text `"Going Online..."` is unchanged).

**Given** the confirmation modal (`show_go_online_modal`) is opened
**When** the modal title and confirm button are rendered
**Then** both read "Go Online & Sync Changes" (or a natural title-cased variant of it), consistent with the button label.

**Implementation Notes:**
- Files to update in lockstep: `wwwroot/scripts/editor/page_renderer/app.mmria.js` (2 static button labels, lines ~728 and ~790), `wwwroot/scripts/offline/offline-transition-manager.js` (`set_go_online_button_state`, line ~208), `wwwroot/scripts/offline/offline-network-monitor.js` (connectivity state text, lines ~69 and ~80), `wwwroot/scripts/offline/offline-modals.js` (`show_go_online_modal` title line ~416 and confirm button line ~434).
- Do not change the busy-state string `"Going Online..."` unless separately requested.
- No server-side or metadata changes — this is a client-side JS string change only, consistent with project-context.md's no-build-step vanilla JS rule for `wwwroot/`.

**Evidence:**
- `app.mmria.js:728,790` (`id="go-online-btn"`, two independent render sites).
- `offline-transition-manager.js:196-209`, `offline-network-monitor.js:54-86` (dynamic state text writers on the same DOM elements).
- `offline-modals.js:409-459` (`show_go_online_modal`, modal title + confirm button).

### Story 35.3: Align Go Online Confirmation Behavior

As a case reviewer working in offline mode,
I want the same confirmation experience regardless of which table's button I click,
So that going online and syncing changes is predictable no matter where I trigger it from.

**Acceptance Criteria:**

**Given** the "Go Online & Sync Changes" button in the "Offline Case List" table (`show_go_online_modal(event)`, line ~728)
**When** it is clicked
**Then** the confirmation modal appears before `go_online_clicked` runs (existing, unchanged behavior).

**Given** the "Go Online & Sync Changes" button in the "Cases Selected for Offline Work" table (currently `go_online_clicked(event)` directly, line ~790)
**When** it is clicked
**Then** the same confirmation modal (`show_go_online_modal`) now appears first, and `go_online_clicked` only runs after the user confirms — matching the first entry point.

**Given** either entry point is confirmed
**When** `go_online_clicked` executes
**Then** existing connectivity checks, diagnostic logging, and sync behavior are unchanged — only the missing confirmation step is added to the second entry point.

**Implementation Notes:**
- Primary file: `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js`.
- Change the second button's `onclick` from `go_online_clicked(event)` to `show_go_online_modal(event)`, matching the first button, so both routes go through the confirmation modal before syncing.
- No change to `go_online_clicked` itself or to the modal's own confirm handler.

**Evidence:**
- Side finding from the Epic 35 investigation case file: the two "Go Online" buttons in `app.mmria.js` have inconsistent click handlers — one confirms via modal, one does not.

## Epic 35 — Story Sequencing

| Story | Risk | Dependencies |
|---|---|---|
| 35.1 - Hide Exit Offline Mode widget | Low | None — pure visibility change, code untouched otherwise |
| 35.2 - Rename Go Online to Go Online & Sync Changes | Low | None — string-only change across 4 files |
| 35.3 - Align Go Online confirmation behavior | Low | Story 35.2 label change should land first so the aligned button shows the correct new label |

All three stories are low-risk, surgical, client-side-only changes with no server or metadata impact. Recommended sequencing: 35.1 and 35.2 can proceed in parallel; 35.3 should follow 35.2 so the newly-aligned button already carries the renamed label.

---

## Epic 36: Case Save Queue Reconcile — Idle Network Recovery Fix

When a case is left idle during a transient network disruption, the autosave retry and the inactivity-triggered save (either the "Continue Editing" path after the warning threshold, or the lock-release path after the lock threshold) can both capture identical `g_change_stack` snapshots. When the autosave eventually succeeds and clears those items, the subsequent inactivity save's post-completion reconcile emits a false-positive warning implying edits were not saved. No data loss occurs, but the warning creates user and developer confusion.

### Background: The Race Condition

The save queue serializes HTTP requests one at a time. The active item is protected from pruning while its HTTP request is in-flight (`save_queue.active_item` is set). During a network disruption:

1. Autosave A is sent — `active_item = A`. The request stalls (ERR_NETWORK_CHANGED / ERR_CONNECTION_RESET).
2. The inactivity threshold passes (`case_edit_inactivity_warning_minutes_before_lock` and/or `case_edit_inactivity_lock_minutes`). An awaited save B is enqueued via `save_case_and_wait(...)`. Because A is still the active item, the prune guard protects A. Queue = `[A (active, in-flight), B (awaited)]`.
3. A and B both snapshot `g_change_stack` at their respective enqueue times. Since no new edits were made during the idle period, both snapshots are identical.
4. Network recovers. A's request succeeds. A reconciles: prefix match passes → `g_change_stack` items are spliced out.
5. B then processes and succeeds. B reconciles: `g_change_stack` is now empty (shorter than B's snapshot) → `mmria_is_change_stack_prefix_match` returns false → **false-positive warning fires**.

Note: When A is in its retry backoff window (`active_item = null`), the prune guard does NOT protect A, and enqueuing B would instead drop A. In that case the warning does not fire. The race window exists only while A's HTTP request is actively in-flight.

### Story 36.1: Fix False-Positive Reconcile Warning

As a case reviewer,
I want to have confidence that the absence of console warnings means my edits may not be saved,
So that developers and reviewers are not misled by false-positive "unmatched edits" alerts during normal network-recovery save sequences.

**Acceptance Criteria:**

**Given** autosave A succeeds and reconciles, clearing `g_change_stack` to `[]`
**And** a second save B (inactivity-triggered) has a snapshot of the same items A cleared
**When** B completes and `mmria_reconcile_live_save_state_after_success` runs for B
**Then** no `console.warn` is emitted — the function recognises the snapshot items are already committed and treats the reconcile as clean

**Given** a save completes and `g_change_stack` currently has items `[D, E]` (not matching the save's snapshot `[A, B, C]`)
**When** `mmria_reconcile_live_save_state_after_success` runs
**Then** the warning **is** emitted and the items are left intact (genuine mismatch path unchanged)

**Given** a save completes and `g_change_stack` is empty but the snapshot is also empty (`snapshot_items.length === 0`)
**When** `mmria_reconcile_live_save_state_after_success` runs
**Then** no warning is emitted and no splice is attempted

**Implementation note:** The distinction between "already-committed" and "genuine mismatch" is:
- **Already-committed**: `g_change_stack.length < snapshot_items.length` AND the items at the live stack head do not match snapshot position 0 (or the live stack is empty). Treat as clean — no warn.
- **Genuine mismatch**: `g_change_stack.length >= snapshot_items.length` but at least one position differs. Warn and leave intact.

All changes are in `wwwroot/scripts/case/index.js`. No server-side changes.

**Files:** `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`

| Dependency | Risk |
|---|---|
| None — isolated reconcile function change | Low |

### Story 36.2: Verify Rev Poll Generation Guard During Network Outage

As a case reviewer,
I want assurance that a brief network outage does not trigger a false "This case was updated" stale-case banner,
So that I am not forced to reload a case that I was the only one editing.

**Acceptance Criteria:**

**Given** a case is in edit mode and the rev poll is running
**When** the network drops and one or more poll requests return `TypeError: Failed to fetch`
**Then** only `console.warn('[CaseRevPoll] poll failed:', err)` is logged — no stale-case banner is shown

**Given** the network recovers and autosave succeeds, calling `mmria_sync_case_rev_polling()` with the new `_rev`
**When** an in-flight poll response from before the save arrives late
**Then** the generation guard (`_caseRevPollGeneration`) discards the stale response — no false-positive banner fires

**Given** the above two scenarios hold at the `case_edit_inactivity_warning_minutes_before_lock` threshold (configurable) and at the `case_edit_inactivity_lock_minutes` threshold (configurable)
**Then** behaviour is identical regardless of those configuration values

**Implementation note:** No code change expected. Developer reads `case-rev-check.js` `startCaseRevPolling` and confirms the generation-guard assignment (`var myGeneration = _caseRevPollGeneration`) and the discard check (`if (_caseRevPollGeneration !== myGeneration) return`) cover the late-arrival scenario. If any gap is found, fix it in `case-rev-check.js`. Add a comment confirming the guard is intentional.

**Files:** `source-code/mmria/mmria-server/wwwroot/scripts/case/case-rev-check.js`

| Dependency | Risk |
|---|---|
| None — read/verify only unless gap found | Low |

### Story 36.3: Verify Autosave-Drop Data Carry-Through

As a case reviewer,
I want assurance that when a retrying autosave is dropped in favour of an awaited inactivity save, none of my edits are silently lost,
So that I can trust the inactivity save captures my full work.

**Acceptance Criteria:**

**Given** autosave A is retrying and an awaited inactivity save B is enqueued
**When** `schedule_retry_or_fail` detects the awaited save and drops A (`awaited_save_is_queued` guard)
**Then** B's `data` clone (captured from `g_data` at B's enqueue time) contains all field values present in `g_data` at that moment — no values from A's earlier snapshot are preferenced over B's

**Given** B processes and the server returns `ok: true`
**Then** `g_data._rev` is updated to the new revision and `g_change_stack` is correctly reconciled against B's snapshot

**Given** both the inactivity warning path (`save_case_and_wait(..., 'edit_inactivity_continue')`) and the lock-release path (`save_case_and_wait(..., 'edit_inactivity_lock_release', { authRefreshPolicy: 'suppress' })`)
**Then** data carry-through holds for both — the configuration values `case_edit_inactivity_warning_minutes_before_lock` and `case_edit_inactivity_lock_minutes` do not affect this property

**Implementation note:** Verification story. Developer traces `get_new_save_queue_item` → `mmria_safe_clone(p_data)` and confirms `p_data` is always `g_data` (not A's stale clone) when B is enqueued. If `g_data` is correctly the live reference in both inactivity paths, no code change is needed — add a confirming code comment. If a gap is found, fix it.

**Files:** `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`, `source-code/mmria/mmria-server/wwwroot/scripts/case/edit-inactivity-manager.js`

| Dependency | Risk |
|---|---|
| Story 36.1 (reconcile fix in same file) | Low |

### Story 36.4 (Optional): Change Stack Snapshot Deduplication on Enqueue

As a developer,
I want the save queue to avoid capturing overlapping `g_change_stack` snapshots when an active save is already committed to those items,
So that the reconcile false-positive cannot occur even if Story 36.1's reconcile fix is ever removed or regressed.

**Acceptance Criteria:**

**Given** autosave A is the active item with `change_stack_items: [item1, item2, item3]` (in-flight)
**When** an inactivity save B is enqueued via `get_new_save_queue_item`
**Then** B's `change_stack_items` snapshot starts from the offset after the items already captured in A (i.e., captures only items added after A's snapshot — or is empty if no new items exist)

**Given** no active save exists for the case when B is enqueued
**Then** B captures the full `g_change_stack` as normal (existing behaviour preserved)

**Implementation note:** This story is a defence-in-depth optimization. It is blocked on Story 36.1 and should only be scheduled if the team decides the two-layer defence is worth the added complexity. Developer must ensure the offset logic handles the edge case where the active save's snapshot is a prefix of `g_change_stack` but new items have since been appended.

**Files:** `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`

| Dependency | Risk |
|---|---|
| Story 36.1 must be complete | Medium — offset logic must be tested against all enqueue paths |

## Epic 36 - Story Sequencing

| Story | Risk | Dependencies |
|---|---|---|
| 36.1 — Fix false-positive reconcile warning | Low | None |
| 36.2 — Verify rev poll generation guard | Low | None (parallel with 36.1) |
| 36.3 — Verify autosave-drop data carry-through | Low | 36.1 (same file) |
| 36.4 — Snapshot deduplication on enqueue (optional) | Medium | 36.1 complete |

Stories 36.1 and 36.2 are independent and can be implemented in parallel. Story 36.3 should follow 36.1 since both touch `index.js`. Story 36.4 is optional and should only be scheduled if the team decides the defence-in-depth layer is warranted.

---

### Epic 37: Form Designer Removal — Static HTML Form Rendering

The `/form-designer` WYSIWYG tool, its supporting JavaScript and CSS assets, its API write endpoints, and the case form's runtime dependency on `g_default_ui_specification` are permanently removed. A standalone build-time generator replaces the dynamic JS rendering engine by producing static HTML fragments from the metadata and UI specification. A lightweight `form-binder.js` handles data population, conditional visibility, grid row cloning, and change tracking for all case form views. The rendered form is pixel-identical to the current output. The OMB-certified layout is preserved exactly.

**FRs covered:** FR-37.1 through FR-37.11
**Stories:** 37.1 — HTML generator, 37.2 — form-binder.js, 37.3 — Wire case editor view, 37.4 — Wire read-only views, 37.5 — Remove form designer, 37.6 — Remove rendering engine

**Architecture constraints:**
- Generated HTML files are committed as permanent source artifacts in `wwwroot/generated-forms/`. Generation is a **one-time operation** run during Story 37.1. After that, the committed HTML files are the source of truth for form layout. Any future label or layout change is made by editing the HTML files directly.
- Field labels in the generated HTML come from the `prompt` property in the versioned metadata (`metadata_26.06.15.json`). The versioning concept will be simplified in a future effort; for this epic the versioned metadata document is the authoritative label source.
- `chart.js` is the only type-renderer file retained; it is called as a specialized component by form-binder.js.
- `page_renderer.js` and all 30 files in `editor/page_renderer/` are deleted on completion of Story 37.4.
- Metadata API endpoints (`/api/metadata`, `/api/version/{ver}/metadata`) are not touched — they remain for validation and other consumers.

---

### Story 37.1: HTML Generator — Build Tool

As a developer,
I want a build-time code generator in the utilities repo that produces static HTML form sections from the metadata and UI specification,
So that case form rendering no longer depends on a runtime JavaScript rendering engine and the OMB-certified layout is captured as a versioned source artifact.

**Acceptance Criteria:**

**Given** metadata.json and ui_specification.json file paths are provided as command-line arguments
**When** `mmria-form-html-generator` runs
**Then** one HTML fragment file per top-level form section is written to the output directory — 14 files named `{section-name}.html`

**Given** a metadata field with an entry in `ui_specification.form_design`
**When** the generator emits that field
**Then** the HTML element carries the exact inline CSS from `ui_specification.form_design[path].control.style` (position:absolute, top, left, width, height)
**And** the element carries `data-path="{slash/separated/path}"`, `data-field-type="{metadata.type}"`, and `data-prompt="{metadata.prompt}"`
**And** the corresponding label element carries inline CSS from `ui_specification.form_design[path].prompt.style`

**Given** a list field (`metadata.type == "list"`)
**When** the generator emits it
**Then** a `<select data-path="..." data-list-id="{metadata.name}">` is emitted
**And** `<option>` elements are populated from `metadata.values` with their display text and stored value
**And** if `metadata.is_multiselect == true` or `control_style` contains `"checkbox"`, the control is emitted as a checkbox group with `data-multiselect="true"`

**Given** a grid control (`metadata.type == "grid"`)
**When** the generator emits it
**Then** the output contains a `<fieldset class="grid-control" data-path="{path}">` with a `<tbody data-grid-body="{path}">` container
**And** a `<template data-grid-template="{path}">` element containing one row, with `{{index}}` as the array index placeholder in all data-path values within the template row

**Given** a form section with `metadata.cardinality == "+"` or `"*"` (multi-cardinality)
**When** the generator emits it
**Then** a `<div data-form-instances="{path}">` container and a `<template data-form-template="{path}">` containing one complete form instance are emitted
**And** the template contains all child field HTML for that form instance, with `{{formIndex}}` as the instance index placeholder in data-path values

**Given** a metadata path with no entry in `ui_specification.form_design`
**When** the generator encounters it
**Then** the field is still emitted (positioned at 0,0) and the unmapped path is written to stderr with format: `UNMAPPED: {path}`

**Given** a field with `metadata.type == "chart"`
**When** the generator emits it
**Then** a `<div data-field-type="chart" data-path="{path}" class="chart-placeholder">` is emitted instead of an input control

**Implementation note:** Generator is a new project at `nccdphp-drh-mmria-utilities/mmria-form-html-generator/`. This is a **one-time-use tool**. It runs once, the output is reviewed and committed, and the tool is then archived — it will not be run again as a build step or pipeline.

The generator accepts `--base-url` (e.g. `https://tenant1-mmria.local:12345`) and `--output` directory as arguments. At startup it calls `GET {base-url}/api/version/release-version` to resolve the current version string dynamically — the version is never hardcoded. It then fetches `GET {base-url}/api/version/{version}/metadata` and `GET {base-url}/api/version/{version}/ui_specification` using that resolved version. The locally saved `artifacts/metadata_26.06.15.json` and `artifacts/ui_specification_26.06.15.json` are reference snapshots only. Output: `{output}/generated-forms/{section-name}.html` (14 files); default output targets `wwwroot/generated-forms/` in `mmria-server`.

The metadata tree is the primary structural input. The generator recursively walks `metadata.children` at each node level. Each node carries: `name` (used to build the slash-separated path, e.g. `home_record/date_of_death/month`), `type` (determines HTML element), `prompt` (label text), `children` (sub-nodes for group/form/grid types), `values` (option list for list-type fields), `max_length`, `is_read_only`, `cardinality`, `is_multiselect`, `control_style`, and `other_specify_list`. The ui_specification is consulted as a secondary lookup: for each path the generator builds from the metadata tree, it reads `ui_specification.form_design["{path}"]` to get `.control.style` (input position/size) and `.prompt.style` (label position/size). Handles all types dispatched by `page_renderer.js`: string, textarea/address, date, datetime, time, number, boolean, list, grid, group, form, label, button, always_enabled_button, html_area, hidden, jurisdiction, chart. Developer reviews stderr output at the end to confirm no critical fields are unmapped.

**Files (new):** `nccdphp-drh-mmria-utilities/mmria-form-html-generator/mmria-form-html-generator.csproj`, `Program.cs`, `HtmlGenerator.cs`, `FieldRenderer.cs`

| Dependency | Risk |
|---|---|
| None — standalone utility | Low |

---

### Story 37.2: form-binder.js — Data Binding Layer

As a developer,
I want a lightweight JavaScript data binding module that replaces page_renderer.js's data binding responsibility,
So that the static HTML form sections can display, edit, and collect case data without the rendering engine infrastructure.

**Acceptance Criteria:**

**Given** a section HTML element and a `g_data` object
**When** `formBinder.bind(sectionEl, g_data)` is called
**Then** every `[data-path]` `<input>`, `<select>`, and `<textarea>` in the section is populated with the value at the corresponding path in `g_data`
**And** list `<select>` elements have the correct `option` selected
**And** multi-select checkbox groups reflect the stored array values
**And** datetime, date, and time inputs are formatted using the same display format as the current renderers

**Given** an element with `data-show-when="path=value"` or `data-show-when="path!=value"`
**When** `formBinder.bind()` is called
**Then** the element's visibility matches the condition evaluated against `g_data`
**And** when any field matching a `data-show-when` condition path changes, all dependent elements in the section are re-evaluated immediately

**Given** a `<tbody data-grid-body="path">` and a `<template data-grid-template="path">` in the section
**When** `formBinder.bind()` is called and `g_data` contains an array at `path`
**Then** one `<tr>` row is cloned from the template for each array item
**And** each row's `[data-path]` values have `{{index}}` replaced with the actual array index
**And** each row's fields are bound to the corresponding array item values
**And** add/remove row buttons on the grid operate correctly, updating the `g_data` array and re-rendering the body

**Given** a `[data-form-instances="path"]` container and a `[data-form-template="path"]`
**When** `formBinder.bind()` is called and `g_data` contains an array at `path`
**Then** one form instance div is cloned from the template for each array item
**And** `{{formIndex}}` placeholders in cloned data-path values are replaced with the instance index
**And** add/remove instance buttons operate correctly

**Given** `options.readOnly == true` is passed to `formBinder.bind()`
**When** binding completes
**Then** all `<input>`, `<select>`, and `<textarea>` elements in the section have the `disabled` attribute applied
**And** no change event listeners are wired

**Given** an input or select with a `data-path` attribute and `options.readOnly` is not set
**When** the user changes the value
**Then** a single delegated listener on the section container writes the new value back to `g_data` at the correct path immediately
**And** dependent-list child dropdowns are re-filtered if the changed field is a `data-parent-list` parent

**Given** `formBinder.collect(sectionEl, g_data)` is called before a save
**When** the function runs
**Then** all current input values are written back to `g_data` with type coercion appropriate to `data-field-type` (string stays string, number fields are parsed as float/int, boolean fields are parsed as bool)

**Given** a `[data-field-type="chart"]` placeholder element
**When** `formBinder.bind()` encounters it
**Then** the existing `chart.js` rendering function is called with the element and the data at `data-path`, producing the chart in-place (no change to chart.js internals)

**Implementation note:** `form-binder.js` is a plain JS module (no build step, consistent with the existing no-bundler convention). Path navigation into `g_data` uses a `getByPath(obj, 'section/group/field')` helper that splits on `/` and walks the object tree. `data-show-when` evaluation uses the same helper. Dependent parent-child list re-filtering replaces the logic currently in `list.js` that reads `g_dependent_parent_to_child` and `g_dependent_child_to_parent` — those maps are retained and consulted by the binder's change listener. Developer audits all existing per-field onChange patterns in `case/index.js` and the type renderers to ensure full coverage before Story 37.3 begins.

**Files (new):** `source-code/mmria/mmria-server/wwwroot/scripts/form-binder.js`

| Dependency | Risk |
|---|---|
| Story 37.1 must be complete (binder tested against real generated HTML) | Low |

---

### Story 37.3: Wire Case Editor View

As an abstractor,
I want the case data entry form to render from static HTML sections using form-binder.js,
So that the form displays and behaves identically to today without the JS rendering engine.

**Acceptance Criteria:**

**Given** an abstractor opens a case in edit mode
**When** a form section is selected from the navigation sidebar
**Then** the corresponding pre-loaded static HTML section div is shown
**And** `formBinder.bind(sectionEl, g_data)` is called to populate all field values
**And** the form is visually pixel-identical to the current JS-rendered output

**Given** the case page has loaded
**When** the developer inspects the Network tab in DevTools
**Then** no request to `/api/version/{ver}/ui_specification` is made
**And** `g_default_ui_specification` is not declared or referenced in `case/index.js`

**Given** the abstractor changes a field value
**When** the change event fires via form-binder's delegated listener
**Then** `g_data` is updated at the correct path
**And** the change is queued via the existing `g_change_stack` mechanism (unchanged)

**Given** the developer audits all calls to `page_render()`, `app_render()`, `form_render()`, `group_render()`, `grid_render()` in `case/index.js` and all scripts loaded by `Case/Index.cshtml`
**When** the audit is complete
**Then** zero calls to those functions remain — all are replaced by `formBinder.bind()` invocations

**Given** all 14 generated section HTML files
**When** `Case/Index.cshtml` renders
**Then** all 14 section fragments are embedded in the initial page HTML (via Razor `@Html.Partial` or static `<div>` includes), initially hidden, with their section-name as the `id`

**Implementation note:** Developer loads all 14 `wwwroot/generated-forms/{section}.html` files as Razor partials in `Case/Index.cshtml`. The existing section-navigation logic in `case/index.js` that shows/hides section containers is adapted to target the static divs by section name. Per-field `onChange` wiring from type renderers is replaced entirely by the form-binder delegated listener. Existing validation integration, `g_change_stack`, save/load, checkout/checkin, and inactivity management in `case/index.js` are not changed. Developer runs Playwright E2E tests after wiring each section before proceeding to the next.

**Files:** `source-code/mmria/mmria-server/Views/Case/Index.cshtml`, `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`

| Dependency | Risk |
|---|---|
| Story 37.1 (generated HTML must exist) | — |
| Story 37.2 (form-binder.js must exist) | — |

---

### Story 37.4: Wire Read-Only Views (AnalystCase, CaseVRO, De-identified, Search View)

As an analyst, committee member, or de-identification reviewer,
I want the read-only case views to render from static HTML sections using form-binder.js,
So that they display correct case data without the JS rendering engine.

**Acceptance Criteria:**

**Given** an analyst opens a case in the AnalystCase view
**When** a form section is selected
**Then** `formBinder.bind(sectionEl, g_data, {readOnly: true})` populates all fields
**And** all inputs are disabled
**And** the display is visually identical to the current rendered output

**Given** a committee member opens a case in the CaseVRO view
**Then** the same static HTML sections and read-only binding apply
**And** no reference to `page_renderer.committee_member.js` or its type renderer variants (`datetime.committee_member.js`, `grid.committee_member.js`, `form.committee_member.mmria.js`, `form.committee_member.pmss.js`) remains in the CaseVRO JS

**Given** the de-identified view is rendered
**Then** the same static HTML sections and `formBinder.bind(..., {readOnly: true})` apply
**And** `de-identified/index.js` no longer fetches or references `g_default_ui_specification`

**Given** the case search view (`case/search_view.js`) currently uses `g_default_ui_specification` for field display in search results
**When** this story is implemented
**Then** search result rendering is replaced with position-independent display (field prompt + value, without absolute-positioned layout)
**And** `case/search_view.js` contains no reference to `g_default_ui_specification` or `form_design`

**Given** each of these four view contexts has been migrated
**When** the developer greps those view files for `g_default_ui_specification`, `page_render`, `form_render`, `group_render`, `grid_render`
**Then** zero matches are found

**Files:** `source-code/mmria/mmria-server/Views/AnalystCase/Index.cshtml`, `source-code/mmria/mmria-server/Views/CaseVRO/Index.cshtml`, `source-code/mmria/mmria-server/wwwroot/scripts/de-identified/index.js`, `source-code/mmria/mmria-server/wwwroot/scripts/case/search_view.js`, and their corresponding JS index files

| Dependency | Risk |
|---|---|
| Story 37.3 complete (pattern established) | Low |

---

### Story 37.5: Remove Form Designer

As a developer,
I want the /form-designer admin tool and all its supporting code deleted from the codebase,
So that there is no dead code, no unused admin role, and no API write surface for form layout.

**Acceptance Criteria:**

**Given** the form designer has been removed
**When** a browser navigates to `/form-designer` (even as a `form_designer`-role user)
**Then** a 404 response is returned

**Given** all form designer assets have been deleted
**When** the developer greps the repository for `form_designerController` or `href="/form-designer"`
**Then** zero matches remain in any C# file or Razor view

**Given** the `form_designer` role and policy are deliberately preserved
**When** the developer opens the application as a form_designer-role user
**Then** the Form Designer admin section is still visible in the navigation
**And** the "Open form designer" link no longer appears in that section
**And** all other links (Metadata Management, De-Identified List, etc.) continue to work

**Given** the POST endpoints have been removed
**When** `dotnet build` runs
**Then** the build succeeds with zero errors

**Delete list:**
- `Controllers/form_designerController.cs`
- `Views/form_designer/Index.cshtml` (entire directory)
- `wwwroot/scripts/form-designer/` (entire directory — all JS files including `app.js`, `index.js`, `dataService.js`, `spec.js`, `canvas/`, `interact-*.js`)
- `wwwroot/form-designer/` (entire directory — `app.html`, `style-formDesigner.css`, `style-interact.css`, `style-multiselection.css`)
- The single `<li><a href="/form-designer">Open form designer</a></li>` line from `Views/Home/Index.cshtml` (line 461)
- POST action on `ui_specificationController.cs` (`[HttpPost("{id}")]`)
- POST action on `metadataController.cs`

**Do NOT remove:**
- The `form_designer` role or its authorization policy in `Program.cs` — it gates the entire Form Designer admin panel section, which includes Metadata Management, De-Identified List, Data Migration, Substance Lists, Export List Manager, Session Activity Report, and Offline Session Logs. These features remain.
- Any `[Authorize(Roles = "form_designer")]` attribute on other controllers

**Files:** `source-code/mmria/mmria-server/Controllers/form_designerController.cs`, `source-code/mmria/mmria-server/Controllers/api/ui_specificationController.cs`, `source-code/mmria/mmria-server/Controllers/api/metadataController.cs`, `source-code/mmria/mmria-server/Views/Home/Index.cshtml`

| Dependency | Risk |
|---|---|
| None — independent deletion pass | Low |

---

### Story 37.6: Remove JS Rendering Engine

As a developer,
I want `page_renderer.js`, all 30 type renderer files, `g_default_ui_specification`, and `ui_specificationController` deleted from the codebase,
So that the dynamic JS rendering engine is completely eliminated and the codebase carries no orphaned rendering infrastructure.

**Acceptance Criteria:**

**Given** Stories 37.3 and 37.4 are complete
**When** the developer greps the entire `wwwroot/scripts/` tree for `g_default_ui_specification`
**Then** zero matches are found

**Given** all rendering engine files have been deleted
**When** the developer greps for `page_render(`, `group_render(`, `grid_render(`, `form_render(`, `list_render(`, `string_render(`, `date_render(`, `boolean_render(`, `label_render(`, `chart_render(`
**Then** zero matches are found in any file outside the generator utility

**Given** `ui_specificationController.cs` has been deleted
**When** `dotnet build` runs on `mmria-server.csproj`
**Then** the build succeeds with zero errors

**Given** the case form is exercised in a browser after all changes
**When** the developer inspects the Network tab
**Then** no request to `/api/version/{ver}/ui_specification` is made
**And** no request to `/api/ui_specification` is made

**Delete list:**
- `wwwroot/scripts/editor/page_renderer.js`
- `wwwroot/scripts/editor/page_renderer.committee_member.js`
- All 30 files in `wwwroot/scripts/editor/page_renderer/` (confirm count before deleting)
- `Controllers/api/ui_specificationController.cs` (entire file)

**Remove from `case/index.js`:**
- `var g_default_ui_specification = null;` declaration
- The `/api/version/{ver}/ui_specification` fetch and `g_default_ui_specification` assignment
- All `<script>` tags referencing `page_renderer.js` or any `page_renderer/` file (remove from `Case/Index.cshtml`, `AnalystCase/Index.cshtml`, `CaseVRO/Index.cshtml`, and any Shared layout that loads them)

**Remove from `de-identified/index.js` and `case/search_view.js`:**
- Any remaining `g_default_ui_specification` declarations and fetches (should be zero if Story 37.4 was complete, but verify)

**Implementation note:** Run `dotnet build` and then exercise each case view context (edit, analyst, committee, de-identified) with a real case before marking done. Playwright E2E suite should pass before this story is closed.

**Files:** All files in `wwwroot/scripts/editor/page_renderer/`, `wwwroot/scripts/editor/page_renderer.js`, `wwwroot/scripts/editor/page_renderer.committee_member.js`, `Controllers/api/ui_specificationController.cs`, `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`, `Views/Case/Index.cshtml`, `Views/AnalystCase/Index.cshtml`, `Views/CaseVRO/Index.cshtml`

| Dependency | Risk |
|---|---|
| Stories 37.3 and 37.4 must be complete | High — deleting before views are wired breaks the app |
| Story 37.5 (form designer deleted) | Low — independent, but cleaner to complete first |

## Epic 37 — Story Sequencing

| Story | Risk | Dependencies |
|---|---|---|
| 37.1 — HTML generator build tool | Medium | None — standalone utility |
| 37.2 — form-binder.js | Medium | 37.1 (test against real generated HTML) |
| 37.5 — Remove form designer | Low | None — independent deletion |
| 37.3 — Wire case editor view | High | 37.1, 37.2 complete |
| 37.4 — Wire read-only views | Medium | 37.3 pattern established |
| 37.6 — Remove rendering engine | High | 37.3, 37.4 complete; verify all views before deleting |

---

## Epic 42: Geocoding Location Registry (Declarative Refactor) _(v4.2, follow-up to Epic 30)_

**Goal:** Deliver the declarative location→field mapping intent that was discussed during Epic 30 planning but was not captured in the Epic 30 story specs. Convert `CaseGeocodingManager` from ten hand-written per-location apply methods into a single `LocationRegistry` (10 entries) + one `Apply(caseDoc, locationKey, result, listIndex?)` method. Update both callers (`CaseGeocodeController` and `BatchItemProcessingService`) to consume the registry. After this epic, adding a new geocode-enabled location is a **single-entry addition to `LocationRegistry`** — no controller switch update, no new manager method, no separate valid-key list.

**Background — the gap being closed**

Epic 30 delivered end-to-end unification of the geocode/apply/save path across the browser button click and the vital-import batch service. Both callers hit the same `GeocodingManager` and `CaseGeocodingManager`. However the *shape* of `CaseGeocodingManager` shipped as imperative code rather than the declarative registry that was the original design intent:

- 10 hand-written `Apply_DC_PlaceOfLastResidence_Geocode` / `Apply_DC_AddressOfInjury_Geocode` / etc. methods, each with the target path (e.g., `"death_certificate/place_of_last_residence"`) hard-coded as a string literal in the method body.
- `CaseGeocodeController` maintains a separate hand-maintained `_validKeys` HashSet (10 entries) and `_listKeys` HashSet (4 entries), and dispatches to the right method via a switch/if-chain in a private `ApplyGeocode` helper.
- `BatchItemProcessingService` has 5 call sites, each hard-coding the specific method name (e.g., `_caseGeocodingManager.Apply_DC_AddressOfDeath_Geocode(new_case, geo_result)`). The batch service therefore must know method identifiers, not just location keys, and cannot iterate a data-driven list of forms.

Adding a new form today requires: (1) new method in `CaseGeocodingManager`, (2) new entry in controller `_validKeys` (and `_listKeys` if it's a list-shaped location), (3) new branch in controller `ApplyGeocode` switch, (4) new client `locationKey`. Four coordinated edits across three files for what should be a single-line registry addition.

**Requirement covered:** FR-1.10 (added to v4.2 PRD as part of this epic).

---

### Story 42.1 — Convert `CaseGeocodingManager` to Declarative `LocationRegistry`

**User Story:** As a developer maintaining or extending case-geocoding, I need the location→field mapping to be a single declarative registry and one `Apply(caseDoc, locationKey, result, listIndex?)` method, so that adding a new geocode-enabled location is a single-entry change and every caller (controller, batch service, future callers) is data-driven off the same source of truth.

**Scope:**

- Introduce a `GeocodeTarget` value type (or `readonly record struct`) in `mmria.common/SharedLibraries/Case/Manager/` with fields `IsList`, `BasePath` (used when `IsList` is false), `ListPath` and `SubPath` (used when `IsList` is true), plus factory helpers `GeocodeTarget.Static(basePath)` and `GeocodeTarget.List(listPath, subPath)`.
- Add `public static readonly IReadOnlyDictionary<string, GeocodeTarget> LocationRegistry` on `CaseGeocodingManager` with all 10 current entries, ordinal-comparer, values matching the current per-method target paths verbatim.
- Add `public void Apply(ExpandoObject caseDoc, string locationKey, GeocodeResult result, int listIndex = 0)` that looks up the target and dispatches to the existing `ApplyStatic` / `ApplyList` private helpers.
- Delete the 10 `Apply_DC_*_Geocode` / `Apply_BC_*_Geocode` / `Apply_PC_*_Geocode` / `Apply_ERH_*_Geocode` / `Apply_OMV_*_Geocode` / `Apply_MT_*_Geocode` public methods after all call sites are migrated. (Do NOT keep them as thin wrappers — the callers within this epic are exhaustive; leaving wrappers would preserve the exact anti-pattern this epic removes.)
- Update `CaseGeocodeController`: derive `_validKeys` from `CaseGeocodingManager.LocationRegistry.Keys`; derive `_listKeys` from `LocationRegistry.Where(kv => kv.Value.IsList).Select(kv => kv.Key)`; replace the `ApplyGeocode` switch/if-chain with a single `_caseGeocodingManager.Apply(caseDoc, locationKey, geocodeResult, listIndex)` call.
- Update `BatchItemProcessingService`: each of the 5 call sites currently naming `Apply_DC_AddressOfDeath_Geocode`, `Apply_DC_PlaceOfLastResidence_Geocode`, `Apply_BC_LocationOfResidence_Geocode` (x2), and `Apply_BC_FacilityOfDelivery_Geocode` becomes `_caseGeocodingManager.Apply(new_case, "<locationKey>", geo_result)` using the corresponding registry key.

**Acceptance Criteria:**

1. `CaseGeocodingManager.LocationRegistry` exists with exactly 10 entries, and their keys match the 10 keys previously listed in `CaseGeocodeController._validKeys`.
2. `CaseGeocodingManager.Apply(caseDoc, locationKey, result, listIndex)` exists and produces byte-identical case-document mutations to the pre-refactor per-location methods for every entry in the registry.
3. The 10 `Apply_*_Geocode` public methods on `CaseGeocodingManager` are deleted.
4. `CaseGeocodeController` no longer holds hand-maintained `_validKeys` or `_listKeys` HashSet literals; both are derived from `LocationRegistry`.
5. `CaseGeocodeController._ApplyGeocode` (or equivalent switch/if-chain) is deleted; the single-line `Apply(...)` call replaces it.
6. `BatchItemProcessingService` has zero references to any `Apply_*_Geocode` method name; every geocode-apply call is `Apply(new_case, "<key>", geo_result)` with the location key as a string literal.
7. `dotnet build` on `mmria-server.csproj` and `mmria.services.csproj` succeeds with zero errors.
8. Adding a new geocode-enabled location to the codebase is verified to require exactly one change: a new key/value pair in `LocationRegistry`. (Verified via a written note in the story's Dev Agent Record, not a code change — this is a design property, not a runtime behavior.)

**Callers to update — enumeration for the story author:**

- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseGeocodingManager.cs` — full rewrite.
- `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs` — `_validKeys`, `_listKeys`, and `ApplyGeocode` helper.
- `nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs` — lines ~1162, ~1203, ~1588, ~1904, ~1919 (each named `_caseGeocodingManager.Apply_*_Geocode(...)`).

**Out of scope:** Client JS. The client contract remains `POST /api/case-geocode/{caseId}/{locationKey}` with the same body shape — the registry lives entirely server-side and is invisible to the browser. The `$case_geocode_dispatch` helper introduced in Story 30.4 is unchanged.

---

### Story 42.2 — Restore Census Tract Certainty Code ≠ 1 Warning Modal (regression fix)

**User Story:** As an abstractor validating an address on any of the 10 "Validate Address and Get Geography Context" buttons, I want the "Address Geocode / Validation: Census Tract Certainty Code is Not 1 ..." info dialog to appear whenever a successful geocode returns a certainty code other than `"1"`, so that I know to re-check a partially valid address before continuing — the same UX that shipped before the Epic 30 server-side migration.

**Background — the regression being closed**

Epic 30 (Stories 30.3 + 30.4) moved geocoding server-side. Story 30.4 removed the 10 in-line client `$mmria.info_dialog_show("Address Geocode", "Validation: Census Tract Certainty Code is Not 1 ...", "...")` calls on the stated assumption that the server would surface the warning through the response. `CaseGeocodeController.Post(...)` however only returns `Ok(new { ok = true })` and never inspects `geocodeResult.NAACCRCensusTractCertaintyCode`. Story 30.6 propagated the same trimmed `$case_geocode_dispatch` helper into the 3 sibling copies (`mmria-check-code.js`, `database-scripts/validator.js`, `wwwroot/scripts/validator.js`) without noticing the missing warning field. Story 42.1 (registry refactor) was explicitly server-side-only and did not touch the client dispatcher either. Result: the modal never fires anywhere — a regression against **FR-1.5** at every one of the 10 registry keys. The verbatim old-code check was `if (parseInt(geo_data.NAACCRCensusTractCertaintyCode) != 1) { $mmria.info_dialog_show("Address Geocode", "Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)", "There might be a potential error in the address. Please verify address."); }` — repeated in all 10 geocode handlers at commit `f3f039a48^` (`ba4e36147` for the != 1 form).

**Requirement covered:** FR-1.11 (added to v4.2 PRD alongside this story).

**Scope:**

- **Server (`CaseGeocodeController`).** After the `_caseGeocodingManager.Apply(...)` call and before the save, compute an optional warning: when `geocodeResult.FeatureMatchingGeographyType` is present and not equal (ordinal-ignore-case) to `"Unmatchable"` **and** `geocodeResult.NAACCRCensusTractCertaintyCode` (ordinal-compared to `"1"`) is not `"1"`, produce a small object with `code = "certainty_code_not_1"`, `title = "Address Geocode"`, `heading = "Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)"`, `message = "There might be a potential error in the address. Please verify address."`, and `certaintyCode = geocodeResult.NAACCRCensusTractCertaintyCode`. Otherwise the warning is `null`. Return `Ok(new { ok = true, warning })` — `null` warnings must be serialized as an explicit `null` (or omitted — either shape is acceptable so long as the client's `if (body.warning)` check remains truthy only for real warnings). Do not put this logic in `CaseGeocodingManager` or `GeocodingManager` — this is a UX concern; the manager stays pure so the batch service is unaffected.
- **Client (`$case_geocode_dispatch`) — 4 files, identical patch.**
  - `source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js` (helper at line ~889)
  - `source-code/mmria/mmria-server/database-scripts/mmria-check-code.js` (helper at line ~1575)
  - `source-code/mmria/mmria-server/database-scripts/validator.js` (helper at line ~1575)
  - `source-code/mmria/mmria-server/wwwroot/scripts/validator.js` (helper at line ~1711)

  On the success branch of the dispatcher, parse `resp.json()` into a local (guarded try/catch — an unexpected non-JSON body must not crash the reload). Run the existing case-reload path first. **After** the reload completes, if the parsed body has a truthy `warning.title`, call `$mmria.info_dialog_show(warning.title, warning.heading, warning.message)` inside a try/catch (matches the error-branch guarding already in place). Order matters: fire after the reload so the just-populated certainty-code field is visible behind the modal (mirrors the pre-Epic-30 UX).

- **Batch path — explicitly excluded.** `BatchItemProcessingService` has no UI to render a modal. The batch service already writes `_logger` entries at geocode failures; adding a low-certainty summary to the batch report is a separate future story and not part of FR-1.11.
- **No changes to `CaseGeocodingManager`, `LocationRegistry`, `GeocodingManager`, or `GeocodeResult`.** The certainty code is already carried on `GeocodeResult` — only the controller and the 4 client copies change.

**Acceptance Criteria:**

1. `CaseGeocodeController.Post(...)` returns a 200 OK body with a `warning` field. When the geocode matched (`FeatureMatchingGeographyType` non-empty and not `"Unmatchable"`) and `NAACCRCensusTractCertaintyCode` (ordinal-compared) is not `"1"`, `warning` is an object with `code = "certainty_code_not_1"`, `title = "Address Geocode"`, `heading = "Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)"`, `message = "There might be a potential error in the address. Please verify address."`, and `certaintyCode` equal to the raw code. Otherwise `warning` is `null` (or omitted). The `ok = true` field is preserved.
2. Each of the 4 `$case_geocode_dispatch` copies parses the success-branch response body, and — after the case reload completes — if the parsed body contains a truthy `warning.title`, invokes `$mmria.info_dialog_show(warning.title, warning.heading, warning.message)` inside a try/catch guard. The 4 patches are byte-identical to each other after formatting.
3. `CaseGeocodingManager`, `LocationRegistry`, `GeocodingManager`, `GeocodeResult`, `BatchItemProcessingService`, and every other server-side file except `CaseGeocodeController.cs` are unchanged. Verified via `git diff --name-only` in the Dev Agent Record.
4. Manual smoke against a running server: at three representative buttons — DC place of last residence (static base path), BC facility of delivery (static base path, different form), MT origin address (list-shaped path with `listIndex`) — a TAMU response with certainty code `4` triggers the modal with the FR-1.11 wording verbatim, and a TAMU response with certainty code `1` does not trigger it. The 7 remaining registry keys share the same controller code path and are therefore covered by the three probes plus AC #1's response-shape guarantee.
5. Unmatchable geocode: the response has `warning = null` (matching the current pre-Epic-30 UX of silently clearing the fields). No modal fires.
6. `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` succeeds with zero errors. `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj` succeeds with zero errors (unchanged, but confirms no accidental cross-project impact).
7. Grep guardrail: `Select-String -Pattern "certainty_code_not_1"` returns exactly 5 matches — one in `CaseGeocodeController.cs` and one in each of the 4 dispatcher copies. Any other count indicates missing files or accidental duplication.

**Callers to update — enumeration for the story author:**

- `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs` — extend the success-path response body only.
- `source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js` — inside `$case_geocode_dispatch` on the success branch.
- `source-code/mmria/mmria-server/database-scripts/mmria-check-code.js` — same edit.
- `source-code/mmria/mmria-server/database-scripts/validator.js` — same edit.
- `source-code/mmria/mmria-server/wwwroot/scripts/validator.js` — same edit.

**Out of scope:** `BatchItemProcessingService`; `CaseGeocodingManager` and its `LocationRegistry`; `GeocodingManager` and `GeocodeResult`; the shape of the request payload; any new modal component or new client helper — the existing `$mmria.info_dialog_show` covers the requirement.

---

## Epic 42 — Story Sequencing

| Story | Risk | Dependencies |
|---|---|---|
| 42.1 — Registry refactor | Low | Epic 30 must be `done` (all Epic 30 stories in `review` or `done` is sufficient to start; commit the review-status stories before starting 42.1 to avoid the uncommitted-work class of failure Epic 30 hit) |
| 42.2 — Certainty code modal restoration | Low | Story 42.1 `done`. (Not strictly required — the controller is edited either way — but running after 42.1 avoids a rebase against the registry refactor's controller changes.) |

Two-story epic. Both are small, in-process changes with no user-facing behavior change beyond restoring FR-1.5 at all 10 buttons.

---

## Epic 46: Case Route Migration — Numeric Index to Case `_id`

**Goal:** Replace the numeric-index first segment of the case-page hash route (`/Case#/0/home_record`) with the CouchDB case `_id` (GUID) so the URL identifies the case as a stable resource. Route shape is preserved verbatim except for that one segment. Legacy numeric URLs and unauthorized/unknown case ids redirect to the case list; no silent index → id translation and no landing page in this epic.

**Requirement covered:** FR-15.1 through FR-15.8.

**Background — the flaw being closed**

The case editor's hash route today treats `path_array[0]` as a **numeric index into `g_ui.case_view_list`**, resolved at hashchange time via `case_view_list[caseIndex].id`. The list mutates constantly — sort order changes, filter changes, offline sync inserts cases, another user adds a case — so the same URL can silently resolve to a different case. Refresh must load the list before it can resolve the URL, which forces `OfflineNavigationManager.getTargetCaseIdForHashChange` and sibling code to reconcile index → id under offline and mid-transition conditions. Bookmarks and share links are effectively unreliable.

Winston's architectural review (2026-08-21) evaluated five alternatives — Option A (id-in-URL, preserve hash shape), Option B (id + position query), Option C (id in sessionStorage), Option D (History API path routing), Option E (in-memory only) — and Nick selected Option A. History API migration is deferred as a separate future epic.

**Identifier choice — CouchDB `_id`, not `record_id`**

Cases are identified by their CouchDB document `_id` (GUID/UUID). The human-readable `STATE-YEAR-NNNN` record id is **not** used because it can be null or malformed on cases mid-creation (see project-context §3.3), while `_id` is always present and immutable. The offline store already keys by `_id`, so no storage schema change is required.

**Scope boundary — case-adjacent only**

*In scope:* `wwwroot/scripts/case/`, `wwwroot/scripts/de-identified/`, `wwwroot/scripts/committee-member/`, case-facing navigation renderers under `wwwroot/scripts/editor/` (`navigation_renderer.js`, `navigation_renderer.abstractor.committee.js`, `navigation_renderer.committee_member.js`, `preview.js`), `wwwroot/scripts/offline/` navigation reconciliation, `wwwroot/scripts/url_monitor.js`, and verification-only sweep of `wwwroot/scripts/print-version/`.

*Out of scope:* aggregate reports, overdose data summary, data dictionary, export-list-manager, admin pages, `editor/page_renderer/app.mmria.js` (metadata form-designer tool, not the case editor), server-side C# `core_element_export`/`exporter` (their `record_index` mentions are data-model column names, not routing), and the offline store's `case_index` localStorage key (a storage-schema constant unrelated to URL positional indexing).

**Non-goal:** Migrating from hash routing (`#/…`) to History API path routing (`/case/{id}/{form}`). That is a separate future epic.

---

### Story 46.1 — Migrate Case Routing from Numeric Index to Case `_id`

**User Story:** As a case abstractor or reviewer, I need the URL for a case page to identify the case by its stable CouchDB `_id` (GUID) instead of its position in the current case list, so that refresh, back/forward navigation, and shared links always resolve to the same case regardless of list mutations.

**Scope:**

- Update `url_monitor.get_url_state` to return a `selected_case_id` field alongside `selected_form_name`. Discriminator: if `path_array[0]` is one of the known form keywords (`summary`, `field_search`, `notifications`, `pinned`, and any others enumerated at implementation time via a full grep of `path_array[0] ==` comparisons), populate `selected_form_name` as today. If `path_array[0]` is purely numeric (`/^\d+$/`), leave both null and set a `legacy_numeric_index` flag. Otherwise treat it as `selected_case_id`. `path_array`, `selected_id`, `selected_child_id` are preserved exactly.
- Rewrite the two hashchange branches in `case/index.js` (the `isTrusted` branch around line 3060 and the fallback branch around line 3251) to consume `url_state.selected_case_id` directly. Delete the `parseInt(path_array[0]) >= 0` index probes and the `case_view_list[caseIndex].id` lookup. When `url_state.legacy_numeric_index` is set, call a shared redirect helper and return.
- Update every `window.location.hash = '#/{index}/…'` writer in `case/index.js`, `case/index.mmria.js`, `case/index.pmss.js`, `case/search_view.js`, `de-identified/index.js`, `committee-member/index.js`, and case-facing `editor/` navigation renderers to emit `#/${caseId}/…`. `case/case-validation.js` L1798 already re-emits whatever is at `path_array[0]` and inherits the id automatically after upstream writers are fixed; verify only.
- Simplify `OfflineNavigationManager.getTargetCaseIdForHashChange` to accept a case id (or null) rather than an index. `g_offline_case_index_map` continues to exist as an offline id-list for UI affordances but is not read to resolve URL → case. `offline-case-storage.js` `case_index` localStorage key is not renamed.
- Add a shared redirect helper (in or alongside `url_monitor`) that issues `history.replaceState(null, '', '#/summary')` plus a single `console.info` tagged with the redirect reason (`legacy-numeric-url`, `unauthorized-case`, `case-not-found`). At the unauthorized-case call site, leave a `// TODO(46.x): show landing page / modal for unauthorized case access` stub.
- Verify `print-version/index.js` and `print-version/print_version_renderer.js` have no active index-based URL construction (their `path_array` references are commented out; server-side rendering supplies the case). No code changes expected; if active coupling is found, apply the same treatment.

**Acceptance Criteria:**

**AC-1 — Route shape preserved except for the numeric segment**
Given a user opens a case in the editor
When the URL is inspected
Then it matches `/Case#/{caseId}/{form}/{child}` where `{caseId}` is the CouchDB `_id` GUID; non-case routes (`#/summary`, `#/field_search/…`, `#/notifications`, `#/pinned`) are unchanged.

**AC-2 — Hashchange resolves case by id, not by list position**
Given the case list is re-sorted or re-filtered after the user opens a case
When the browser re-navigates via back/forward or the user refreshes
Then the same case loads regardless of the current list ordering — no `case_view_list[index].id` lookup happens on the URL-resolution path.

**AC-3 — Discriminator is deterministic**
Given `url_monitor.get_url_state` is called with a URL whose `path_array[0]` is a form keyword
When the state is inspected
Then `selected_form_name` is populated and `selected_case_id` is null; given a URL whose `path_array[0]` is a GUID-shaped case id, `selected_case_id` is populated and `selected_form_name` is null; given a URL whose `path_array[0]` is purely numeric, both are null and `legacy_numeric_index` is true.

**AC-4 — Legacy numeric URLs redirect to the case list**
Given a legacy URL like `/Case#/0/home_record`
When the page loads
Then the redirect helper calls `history.replaceState` to `#/summary`, a single `console.info` is logged with `reason: 'legacy-numeric-url'`, no case document is fetched, and the back-stack does not contain the legacy URL.

**AC-5 — Unauthorized / unknown case id redirects to the case list**
Given the URL carries a case id the current user cannot open (unauthorized, wrong tenant, or not found)
When the hashchange handler runs
Then the redirect helper is called with `reason: 'unauthorized-case'` (or `'case-not-found'`), the URL becomes `#/summary` via `history.replaceState`, and a `// TODO(46.x): show landing page / modal for unauthorized case access` comment is present at the redirect call site.

**AC-6 — Offline navigation reconciliation simplified**
Given the user is in offline mode
When the hashchange handler runs
Then `OfflineNavigationManager.getTargetCaseIdForHashChange` receives the case id directly from `url_state.selected_case_id` (not an index) and returns without consulting `g_offline_case_index_map` as a URL-resolution source. `g_offline_case_index_map` and the `case_index` localStorage key are unmodified in shape.

**AC-7 — Next/prev navigation derives position at click time**
Given a case is open and the UI offers next/prev navigation
When the button is clicked
Then the target case id is computed via `case_view_list.findIndex(c => c.id === currentCaseId) ± 1` and emitted as `#/${nextCaseId}/${currentForm}`; list position is not persisted in the URL.

**AC-8 — Case-adjacent scope only; admin/reports untouched**
Given the full grep list for the in-scope directories is exercised
When `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` runs
Then the build succeeds with zero errors and no C# controller, Razor view, or route attribute is modified. `aggregate-report/`, `overdose-data-summary/`, `data-dictionary/`, `export-list-manager/`, and admin pages are byte-unchanged.

**AC-9 — Playwright smoke regression**
Given the case-editor Playwright smoke suite is updated to expect `_id`-shaped URLs
When the suite runs
Then the existing scenarios pass, plus new coverage: open case → refresh → same case loads; sort/filter list → previously-open case's URL still resolves; legacy `#/0/home_record` → lands on `#/summary`; bogus `#/does-not-exist-guid/home_record` → lands on `#/summary`.

**Callers to update — enumeration for the story author:**

- `source-code/mmria/mmria-server/wwwroot/scripts/url_monitor.js` — discriminator, `selected_case_id` field, redirect helper.
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` — two hashchange branches (L3060, L3251), the writer at L3107 already emits `#/summary` (unchanged); verify each `window.location.hash =` writer.
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js` — L262 (`'#/' + g_ui.selected_record_index + '/home_record'`).
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.pmss.js` — equivalent write site.
- `source-code/mmria/mmria-server/wwwroot/scripts/case/search_view.js` — L28 (`record_index = path_array[0]`).
- `source-code/mmria/mmria-server/wwwroot/scripts/case/case-validation.js` — L1798, verify only.
- `source-code/mmria/mmria-server/wwwroot/scripts/de-identified/index.js` — L124 and its hash writers.
- `source-code/mmria/mmria-server/wwwroot/scripts/committee-member/index.js` — L71 and navigation renderer.
- `source-code/mmria/mmria-server/wwwroot/scripts/committee-member/navigation_renderer.js` — L43.
- `source-code/mmria/mmria-server/wwwroot/scripts/editor/navigation_renderer.js`, `navigation_renderer.abstractor.committee.js`, `navigation_renderer.committee_member.js`, `preview.js` — case-list navigation only, not the metadata form designer.
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-navigation-manager.js` — `getTargetCaseIdForHashChange` signature.
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-case-manager.js` — L483, keep id-list, stop using for URL resolution.
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-modals.js` — L960, same treatment.
- `source-code/mmria/mmria-server/wwwroot/scripts/print-version/index.js`, `print_version_renderer.js` — verify only, no changes expected.

**Out of scope:** aggregate/overdose reports, data-dictionary, export-list-manager, admin pages, server-side C# `core_element_export`/`exporter` code (their `record_index` mentions are export column names, not routing), the `case_index` localStorage key in `offline-case-storage.js`, and any migration to History API path routing.

---

## Epic 46 — Story Sequencing

| Story | Risk | Dependencies |
|---|---|---|
| 46.1 — Migrate case routing from numeric index to case `_id` | Medium | None — self-contained client-side change. Optional but recommended to land after Epic 45's Playwright suite is stable so smoke coverage is trustworthy. |

Single-story epic. The change is concentrated but touches ~10 files; risk is medium because the hashchange handlers coordinate with autosave, offline reconciliation, and next/prev navigation. Playwright smoke coverage on `_id`-shaped URLs is the primary safety net.

