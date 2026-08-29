---
title: "PRD: MMRIA V4.2"
status: draft
created: 2026-08-06
updated: 2026-08-07
---

# PRD: MMRIA V4.2

> **Draft in progress — Discovery not yet complete. Additional epics expected.**

---

## Vision & Goals

MMRIA V4.2 continues the v4.1 work of hardening the platform for production reliability and long-term maintainability. The centerpiece of this release is consolidating all address geocoding into a single server-side implementation, eliminating scattered client-side TAMU calls, a live API key exposure in the browser, and non-atomic save behavior. Additional epics carry over unstarted v4.1 backlog work (Record ID uniqueness, Form Designer removal) and will be supplemented by new bug fix items as identified.

**Success looks like:**
- Address geocoding is fully server-side — no TAMU API key in browser JS, no partial-save data loss risk on network failure
- All 10 geocode button locations and the vital import batch path share one implementation
- The record ID duplicate defect is eliminated with a defense-in-depth strategy
- The dynamic JS rendering engine is retired and replaced with static HTML sections

---

### FR-5 — Update Year of Death: Record ID Assignment Regression Fix

The Update Year of Death pages (`/update_year_of_death/FindRecord?role=cdc_admin` and `?role=jurisdiction_admin`) have two record ID defects introduced by the v4.1 SharedLibraries refactoring of `GetRecordIdReplacementForYearOfDeathAsync` in `CaseManager`. Pre-v4.1 behavior is the reference for correct behavior.

**FR-5.1 — Generate record ID when case has none and year is unchanged**
When the user submits the Update Year of Death form with the same year as the current year of death, and the case has no existing record ID (null or empty), the system generates a new valid record ID using the current year. The confirmation screen shows the new record ID. Returning an empty string is incorrect behavior.

**FR-5.2 — Preserve existing record ID when year is unchanged**
When the user submits with the same year and the case already has a valid record ID (matching the pattern `{state}-{year}-{4-digit}`), the system preserves the existing record ID rather than generating a new one. The root cause: `RecordIdExistsAsync` currently finds the current case's own record ID as an existing conflict and loops to assign a different random ID. The uniqueness check must exclude the case being updated from the conflict detection.

**FR-5.3 — Both role variants are fixed**
The fix applies equally to the `cdc_admin` and `jurisdiction_admin` role variants of the page. There is no behavioral difference between the two paths for this fix.

---

### FR-7 — Session Expiry: Automatic Logout Redirect on 401

When a user leaves the application with the browser open and returns after the session has expired (e.g., overnight with a locked workstation), the browser resumes background polling and the user may click UI elements — all of which receive HTTP 401 responses. Currently 401 responses from both `window.fetch` and jQuery `$.ajax` calls are swallowed silently: the UI freezes or shows blank panels with no guidance. The user must manually navigate to the login page.

The server-side behavior is already correct: `CustomAuthHandler` returns bare 401 for API paths and redirects to login/SignIn for page navigations. The gap is entirely client-side.

**FR-7.1 — `window.fetch` 401 interceptor**
A global `window.fetch` wrapper is added to `_LayoutBase.cshtml` that intercepts any 401 response from any fetch-based API call (~85 call sites across 28 files). When a 401 is detected, the user is redirected to `/Account/Logout`. The wrapper always returns the original response to the caller so existing error-handling paths are not disrupted.

**FR-7.2 — jQuery `$(document).ajaxError()` 401 handler**
A global `$(document).ajaxError()` handler is added to `_LayoutBase.cshtml` that intercepts any 401 from any jQuery `$.ajax` call (~149 call sites across 58 files). When a 401 is detected, the user is redirected to `/Account/Logout`.

**FR-7.3 — Redirect target is `/Account/Logout`, not `/Account/Login`**
Redirecting directly to the login page leaves an expired session document in CouchDB and the `sid` cookie intact. `/Account/Logout` clears the expired session, clears the cookie, then routes to the appropriate login path (SAMS or local). This is the correct cleanup path.

**FR-7.4 — Guards prevent redirect loops and offline-mode interference**
Neither interceptor fires when:
- The current page is already an `/Account/` path (prevents redirect loops)
- `localStorage.getItem('is_offline') === 'true'` (offline mode manages its own auth state)
- A redirect is already in progress (a shared `_sessionExpiredRedirectPending` flag deduplicates concurrent 401s from simultaneous polling and user-initiated calls)

**FR-7.5 — No server-side changes required**
`CustomAuthHandler`, `AccountController.Logout()`, and CouchDB session management are already correct. All changes are confined to client-side JS in `_LayoutBase.cshtml` and a polish update to `mmria_check_if_need_to_redirect` in `mmria.js`.

**FR-7.6 — `navigator.sendBeacon` calls are explicitly out of scope**
Five fire-and-forget beacon calls in `case/index.js` send offline case close-events on page unload. There is no callback mechanism for these — any 401 on a beacon is intentionally unobservable. These calls are not in scope for this fix.

---

### FR-8 — Per-Tenant Authentication Mode (SAMS + Password Co-existence)

In multi-tenant mode, the server currently applies a single global authentication method — either SAMS (`sams.is_enabled: true`) or password (`sams.is_enabled: false`) — uniformly across all tenants. This prevents having a SAMS-authenticated production tenant on the same server as a password-authenticated demo or pre-onboarding training tenant.

Single-tenant mode (where each jurisdiction runs its own pod) is being phased out. The goal is all jurisdictions on a single shared multi-tenant server. The blocking issue is that SAMS and password authentication cannot currently co-exist on the same server instance.

The production use case: a demo/training tenant accepts password logins so that users can practice before receiving SAMS credentials. All other tenants authenticate via SAMS only.

**FR-8.1 — Per-tenant authentication method configuration**
Each tenant's authentication method (SAMS or password) is configured independently. A per-tenant configuration value determines which login path is presented and enforced for that tenant. The global `sams.is_enabled` setting is superseded or supplemented by this per-tenant value when the server is running in multi-tenant mode.

**FR-8.2 — SAMS and password tenants operate correctly on the same server**
A user accessing a SAMS-configured tenant is redirected to SAMS for authentication. A user accessing a password-configured tenant sees the local login form and authenticates with username and password. Both flows operate correctly from the same running server process with no interference between tenants.

**FR-8.3 — `is_environment_based` is not changed**
`is_environment_based: true` (production — config from environment/CouchDB) and `is_environment_based: false` (local development — config from `appsettings.local.json`) continue to function as today. The per-tenant auth configuration is resolved through the same config loading path that already provides tenant-specific values.

**FR-8.4 — Existing SAMS tenants are unaffected**
For tenants configured for SAMS, the existing authentication flow — `GET /Account/SignIn` → SAMS federation → OIDC callback → session creation — is unchanged. No SAMS tenant requires any configuration change to continue working.

**FR-8.5 — Login page renders appropriate form per tenant**
When a user navigates to the login page, the form rendered matches the configured authentication method for that tenant. SAMS tenants do not show the username/password form. Password tenants do not show a SAMS redirect. A tenant misconfigured with neither method falls back to password login and logs a warning.

> **OI-4 (open):** Determine the storage location for the per-tenant auth method config: (a) per-tenant CouchDB configuration document (already tenant-scoped, accessed via `is_environment_based` path), or (b) `appsettings.json` / environment variable with tenant key prefix. Option (a) is the natural fit for the existing multi-tenant config architecture. Confirm at architecture/investigation time.

> **OI-5 (open):** Confirm behavior when a user on a SAMS tenant has an existing password account in `_users`. The expected behavior is that SAMS auth takes precedence and the password account is not usable for that tenant while SAMS is configured.

---

Ten "Validate Address and Get Geography Context" buttons are distributed across case forms. Currently each button calls the TAMU geocoding API client-side, applies results to `g_data`, then saves — non-atomically, with duplicated urban-status logic in every callback, and with the TAMU API key exposed in `mmria.committee_member.js`. Additionally, the vital import batch service contains a separate isolated geocoding implementation with duplicated field-mapping logic.

**FR-1.1 — Single server endpoint, all 10 locations**
All 10 validate-address button locations POST to a single mmria-server endpoint (`POST /api/case-geocode/{caseId}/{locationKey}`). No button calls the TAMU geocoding service directly from the browser. The 10 location keys and their case document paths are:

| Location Key | Form | List? |
|---|---|---|
| `dc_place_of_last_residence` | Death Certificate | — |
| `dc_address_of_injury` | Death Certificate | — |
| `dc_address_of_death` | Death Certificate | — |
| `bc_facility_of_delivery` | Birth/Fetal Death Cert (parent) | — |
| `bc_location_of_residence` | Birth/Fetal Death Cert (parent) | — |
| `pc_primary_care_facility` | Prenatal Care Record | — |
| `erh_location` | ER Visits & Hospitalizations | ✓ |
| `omv_location_of_care` | Other Medical Office Visits | ✓ |
| `mt_origin_address` | Medical Transport | ✓ |
| `mt_destination_address` | Medical Transport | ✓ |

**FR-1.2 — Atomic server-side geocode + CVS + save**
The server endpoint performs all of the following in one atomic operation:
1. Calls the TAMU geocoding service
2. If CVS lookup is configured for the location, calls the CVS API using the geocode result
3. Writes all 15 geocode output fields to the correct path in the case document
4. Saves the updated case document to CouchDB

A network failure between any of these steps leaves the case document in a consistent pre-geocode state — no partial geocode data is written.

**FR-1.3 — CVS is configured per location, server-side**
The CVS lookup is controlled by a server-side configuration per location key. `dc_place_of_last_residence` is the only location currently configured for CVS. The configuration is not hardcoded in client JS.

**FR-1.4 — Client flow: save → busy modal → POST → reload**
When the user clicks a validate-address button:
1. The current case is saved
2. A busy modal is displayed using the existing site modal pattern, preventing further editing
3. A POST request is sent to the geocode endpoint
4. On completion (success or Unmatchable), the case reloads in edit mode with the updated geocode fields visible
5. The busy modal is dismissed

**FR-1.5 — Error and validation states preserved**
The following user-visible behaviors from the current implementation are preserved:
- **Census Tract Certainty Code warning**: When `NAACCRCensusTractCertaintyCode != 1` on a successful geocode, a non-blocking info dialog is shown: _"Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.) There might be a potential error in the address. Please verify address."_
- **Unmatchable path**: When TAMU returns no match or an error, all 15 geocode fields are cleared to empty string. No error dialog is shown — the field clear and reload communicate the result.

**FR-1.6 — TAMU API key never in client-side code**
The TAMU API key is not present in any file served to the browser. `mmria.committee_member.js` contains a `get_geocode_info` implementation that calls `geoservices.tamu.edu` directly with the key embedded. However, the committee member view is read-only — the validate-address button is always disabled in that context, making this code unreachable. The fix is to remove the dead code and the embedded API key from `mmria.committee_member.js`; no functional replacement is required for that view.

**FR-1.7 — Vital import batch service uses shared implementation**
The vital import batch service (`BatchItemProcessingService`) geocodes cases during IJE import. This path uses the same shared `GeocodingManager` and `CaseGeocodingManager` from SharedLibraries as the web layer. Duplicated field-mapping and urban-status logic in the batch service is eliminated.

**FR-1.8 — Legacy geocode calls updated with census_year**
8 geocode call sites in `mmria-check-code.js` and 8 in `validator.js` use an older 4-argument signature missing `census_year`, potentially assigning stale census tracts. These are updated to use the server endpoint with the correct census year derived from `g_data.home_record.date_of_death.year`.

**FR-1.9 — Shared geocoding logic in SharedLibraries**
Urban-status derivation (Metropolitan Division / Metropolitan / Micropolitan / Rural / Undetermined) and `state_county_fips` calculation are implemented once in `mmria.common/SharedLibraries/Geocoding/GeocodingManager`. No client-side JS calculates urban status.

**FR-1.10 — Declarative location-to-field registry (Epic 42, post-Epic-30 refactor)**
The mapping from location key to case-document target path is expressed as a single **declarative registry** (`LocationRegistry`) inside `mmria.common/SharedLibraries/Case/Manager/CaseGeocodingManager`. Each entry captures the location key, the base path (for static locations) or list path + subpath (for list-shaped locations), and the list flag. There is one public `Apply(caseDoc, locationKey, result, listIndex)` method on `CaseGeocodingManager` that looks up the target in the registry and applies the geocode result — not ten per-location `Apply_*_Geocode` methods.

Both callers are data-driven off the same registry:
- `CaseGeocodeController` derives `_validKeys` (all registry keys) and `_listKeys` (registry keys where `IsList` is true) from `LocationRegistry` directly; no separate hand-maintained key lists.
- `BatchItemProcessingService` calls `Apply(caseDoc, "dc_address_of_death", result)` and similar by-key invocations at every geocode call site; the batch service never references a per-location method name.

Adding a new geocode-enabled location requires exactly **one code change**: a new entry in `LocationRegistry`. No controller switch update, no new manager method, no separate valid-key list update. This FR was implicit in the original Epic 30 planning discussion but was not captured in the shipped Epic 30 stories; Epic 42 delivers it as a follow-up.

**FR-1.11 — Restore the Census Tract Certainty Code warning modal (regression fix, Epic 42)**
FR-1.5 requires that when `NAACCRCensusTractCertaintyCode != 1` on a successful geocode, a non-blocking info dialog is shown with the exact text: _"Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.) There might be a potential error in the address. Please verify address."_ Epic 30 (Stories 30.3 and 30.4) moved the geocode flow server-side but did not wire the warning through the new response, and Story 30.4 explicitly removed the client-side dialog on the assumption that the server would surface it. The server currently only returns `{ ok: true }` on success, so the modal never fires — a regression against FR-1.5 at all 10 "Validate Address and Get Geography Context" buttons.

To close the gap:

- **Server response.** `POST /api/case-geocode/{caseId}/{locationKey}` returns a structured `warning` field on the 200 OK response when the geocode matched (i.e., `FeatureMatchingGeographyType` is present and not `"Unmatchable"`) and `NAACCRCensusTractCertaintyCode != "1"`. The warning object carries `code = "certainty_code_not_1"`, `title = "Address Geocode"`, `heading` and `message` matching the FR-1.5 wording verbatim, and the raw `certaintyCode` for diagnostic use. When there is no warning (matched with certainty `"1"`, or unmatchable), the `warning` field is `null` or omitted.
- **Client dispatcher.** The `$case_geocode_dispatch` helper (defined in `MMRIA_calculations.js`, `mmria-check-code.js`, `database-scripts/validator.js`, and `wwwroot/scripts/validator.js` — 4 copies) parses the response body on the success path and, if `warning` is present, invokes `$mmria.info_dialog_show(warning.title, warning.heading, warning.message)` **after** the case-reload completes so the just-saved certainty-code field is visible behind the modal.
- **Coverage.** Behavior is identical across all 10 registry keys — no per-location branching in either the server or client dispatcher.
- **Batch scope explicitly excluded.** `BatchItemProcessingService` does not surface UI warnings; its 5 geocode call sites already log at the server. A batch-report summary of low-certainty imports is a separate feature, not part of this FR.

---

### FR-2 — Record ID Uniqueness Enforcement

_(Carried from v4.1 backlog — Epic 29. Full FR definition: see `epics.md` FR-29.1–FR-29.7.)_

Abstractors creating new cases are protected against duplicate MMRIA Record IDs by a defense-in-depth strategy: server-side format validation and uniqueness guard, client-side per-candidate API check before save, and a functional `record_id_list` CouchDB view replacing the broken bulk-list dependency.

**FR-2.4 — Shared `GenerateUniqueRecordIdAsync` primitive and structured collision error code.**
`CaseManager` exposes a public `GenerateUniqueRecordIdAsync(state, year, dbConfig, maxAttempts)` method that produces a jurisdiction-scoped unique record ID by generating a `STATE-YEAR-NNNN` candidate and calling `RecordIdExistsAsync` until a free suffix is found or `maxAttempts` is exhausted (throws `RecordIdGenerationExhaustedException`). `document_put_response` gains a nullable `error_code` string field. `SaveCaseAsync` populates `error_code = "record_id_format"` on format-guard rejection and `error_code = "record_id_conflict"` on uniqueness-guard rejection. Downstream callers detect collisions via the code rather than matching English error text.

**FR-2.5 — Online case creation uses save-then-retry-on-collision (Path A).**
The Story 29.2 client pre-flight loop against `/api/record_id` is replaced by a single POST to `/api/case`. When the response's `error_code === "record_id_conflict"`, `add_new_case()` regenerates the 4-digit suffix and re-POSTs, up to 5 total attempts. On exhaustion, the same user-facing error message defined in FR-29.2 is surfaced and no case is created. Applies to both `index.mmria.js` and `index.pmss.js`. `record_idController` retains no shipped callers after this FR and is tagged for cleanup.

**FR-2.6 — Offline case creation uses placeholder record IDs generated on server at sync (Path B).**
Offline `add_new_case()` writes `home_record.record_id = "{STATE}-OFFLINE-CASE-{XX}"` where `XX` is a two-digit per-offline-session sequence maintained by `OfflineSessionManager`. The prior `-OFFLINE` suffix and `generateOfflineRecordId` helper are removed. At sync time, `OfflineCaseManager.ApplyOfflineDocumentAsync` detects the placeholder pattern (`/^([A-Z0-9]+)-OFFLINE-CASE-\d+$/i`), calls `CaseManager.GenerateUniqueRecordIdAsync(state, year, dbConfig)` where `state` is the captured prefix and `year` is `home_record.date_of_death.year`, assigns the result, and then invokes `SaveCaseAsync`. Legacy `STATE-YEAR-NNNN-OFFLINE` cases still in offline caches continue to be accepted transitionally with a structured log entry recording which format was seen.

**FR-2.7 — IJE batch imports route through `SaveCaseAsync` with collision-retry (Path C).**
`BatchItemProcessingService.Process_Message` persists each new case via `CaseManager.SaveCaseAsync` rather than directly via `_caseRepository.PutCaseDocumentJsonAsync`. On `error_code === "record_id_conflict"`, the batch item processor calls `GenerateUniqueRecordIdAsync` for a fresh record ID, updates `home_record.record_id`, and retries; cap at 5 attempts; on exhaustion, marks the item `ImportFailed`. `BatchItem.mmria_record_id` reports the final, post-retry value so users can trace the case. The stale 25 000-row `ExistingRecordIds` HashSet pattern in `MMRIAServicesHelper.ConvertLineToBatchItem` is retired for cross-writer uniqueness; a small batch-local dedup Set is preserved to guard intra-file suffix collisions.

> **Implementation amendment (2026-08-20):** FR-2.7's "route batch writes through `SaveCaseAsync`" strategy caused an authorization regression — `SaveCaseAsync` runs the application-layer authorization check against the caller's `ClaimsPrincipal`, but the batch runs as a server-side integration with a synthetic `vital-import` principal that has no role/jurisdiction assignments. Every batch save returned `unauthorized PUT`. FR-2.8 supersedes the "call `SaveCaseAsync` directly" portion of FR-2.7 with a dedicated `VitalImportCaseWriter`. The Story 29.1 record-id format/uniqueness guards and Story 29.4 collision-retry loop remain in force via a shared private helper on `CaseManager`. See decision-log entry `2026-08-20 — FR-2.8 added` for design rationale.

**FR-2.8 — Vital-import batch writes use a dedicated `VitalImportCaseWriter`.**
`nccdphp-drh-mmria-services/mmria.services/SharedLibraries/VitalImport/Manager/VitalImportCaseWriter.cs` exposes a single method, `SaveNewVitalImportCaseAsync(caseData, changeStack, dbConfig, configuration, hostPrefix)`, that writes a new case using the CouchDB service credentials without invoking the user-request authorization check. The method:
- Takes no `ClaimsPrincipal` parameter — controllers have no legitimate reason to use it, and the missing parameter is a visible signal in code review that this is not a user-request path.
- Runs the Story 29.1 record-id format guard (STATE-YEAR-NNNN, four-digit year 1900-2100, four-digit suffix, non-empty jurisdiction prefix) and the Story 29.1 uniqueness check via `ICaseRepository.RecordIdExistsAsync`.
- Runs the Story 29.4 collision-retry loop keyed on `error_code === "record_id_conflict"`, up to 5 attempts, calling `CaseManager.GenerateUniqueRecordIdAsync` on each collision. On exhaustion, returns a `SaveCaseResult` whose `document_put_response.error_description` reports the exhaustion reason.
- Writes an `IAuditRepository` change-stack entry stamped with `created_by = "vital-import"` and `last_updated_by = "vital-import"` so imports are attributable in the audit log.

The shared record-id validation + retry logic is extracted into a private helper on `CaseManager` (called from both `SaveCaseAsync` and `SaveNewVitalImportCaseAsync`) so the guards and retry semantics cannot drift between the two paths. The writer is registered in the vital-import service DI graph only — not in the main mmria-server DI graph — so it is unreachable from user-facing controllers. `BatchItemProcessingService.Process_Message` calls `SaveNewVitalImportCaseAsync` instead of `SaveCaseAsync`, and the synthetic-`ClaimsPrincipal` scaffolding (`BuildVitalImportPrincipal`, `BuildVitalImportConfiguration`'s user-facing bits) is removed.

**FR-2.9 — `BatchItemProcessor` cannot strand items under "In Process" on exception.**
When `BatchItemProcessingService.Process_Message(StartBatchItemMessage)` throws, `BatchItemProcessor.ReceiveAsync` must `Tell` a synthetic `ImportFailed` `BatchItem` back to the parent `BatchProcessor` — not silently log the exception and drop the message. Concretely:
- The `catch (Exception ex)` block in `BatchItemProcessor.cs` constructs a `BatchItem` with `Status = ImportFailed`, `CDCUniqueID = message.cdc_unique_id`, `mmria_record_id = message.record_id`, `mmria_id` populated (either from the message or a fresh `Guid`), and `StatusDetail = "Processing exception: {ex.Message}"` (full stack trace goes to the console log, not `StatusDetail`).
- The synthetic completion is sent via `Context.ActorSelection(message.BatchProcessorPath).Tell(batchItem)` so `BatchProcessor.pending_items` decrements and `Finalize_Batch()` runs.
- No batch item can remain visible under "In Process" indefinitely; every dispatched item resolves to `NewCaseAdded`, `ExistingCaseSkipped`, or `ImportFailed` before the batch closes.

---

### FR-3 — Form Designer Removal — Static HTML Form Rendering

_(Carried from v4.1 backlog — Epic 37. Full FR definition: see `epics.md` FR-37.1–FR-37.11.)_

The `/form-designer` WYSIWYG tool, its JS/CSS assets, its API write endpoints, and the case form's runtime dependency on `g_default_ui_specification` are permanently removed. A build-time generator produces static HTML form sections from metadata and UI specification. A `form-binder.js` module handles data population, conditional visibility, grid cloning, and change tracking for all case form views.

---

### FR-4 — IJE Batch Re-Upload Rejection & Import Observability

When an IJE/vitals file is uploaded, the individual case-level duplicate guard in `BatchItemProcessingService` already prevents the same vital record (`CDCUniqueID`) from being added to the `mmrds` case database twice, and Epic 29 (Record ID Uniqueness) prevents `mmria_record_id` collisions on the write path. However, re-uploading the **same file** still (a) calls the external vitals service a second time, (b) writes a new batch document to the `vital_import` CouchDB database, and (c) produces no meaningful log output describing what was skipped and why.

This FR is scoped to two things Epic 29 does not cover:

1. Rejecting a **file-level** re-upload at the controller before any downstream work runs (keyed on `nat_file_name` / `fet_file_name` / `mor_file_name` — not on `mmria_record_id` or `CDCUniqueID`).
2. Adding structured logging around batch-level rejection and around the pre-existing per-case `ExistingCaseSkipped` decision.

Neither of these behaviors is delivered by Epic 29. See the *Relationship to Epic 29* note in Story 38.1 for the identifier-by-identifier breakdown.

**FR-4.1 — Prevent duplicate entries in vital_import database**
When an IJE file is submitted for upload, the server checks the `vital_import` database before writing the batch. If the batch (or the individual case records within it) already exists in `vital_import`, the duplicate entries are not written. The existing case-level duplicate guard for `mmrds` is not affected.

**FR-4.2 — Meaningful logging for duplicate detection**
When cases or batch records are identified as duplicates during IJE upload processing, a structured log entry is written for each skipped item. The log entry includes at minimum: the case identifier, the reason skipped (duplicate), and the existing `vital_import` record reference. The logging is sufficient for a developer to audit what happened on any given upload run without inspecting the database directly.

**FR-4.3 — User feedback on duplicate upload**
When a re-upload is detected (all cases in the file are already present in `vital_import`), the upload response communicates the outcome clearly — indicating how many records were skipped as duplicates vs. processed as new. The existing success/error response shape is preserved; duplicate-skip information is added to it.

> **OI-3 (open):** Confirm partial-batch behavior — if a file contains 3 new cases and 2 already-existing cases, should the system (a) process the 3 new cases and skip the 2 duplicates, or (b) reject the entire file and require the user to resolve before re-submitting? Determine at implementation time.

---

### FR-10 — STEVE Download: Structured Logging

The `SteveAPI_Instance` Akka actor uses bare `Console.WriteLine` calls with swallowed exception details. The `ILogger` injected into `steveMMRIAController` is never threaded through to the actor. An operator diagnosing a stuck or failed STEVE download has no structured log output to work from — only coarse lifecycle markers and a few plain-text failure strings with no exception detail. The only visible failure signals are a directory aging to "Cancelled" after one hour or a missing `.zip` file.

**FR-10.1 — Structured logging replaces Console.WriteLine throughout the actor pipeline**
All bare `Console.WriteLine` calls in `SteveAPI_Instance` and `SteveAPISupervisor` are replaced with `ILogger` calls at appropriate log levels. The logger is resolved via Akka.NET's DI integration or constructor injection on the actor.

**FR-10.2 — Exception details are captured in error logs**
The three catch blocks in `SteveAPI_Instance` (file download failure, mark-as-read failure, and zip compression failure) currently swallow the exception and emit only a plain string. Each catch block logs the full exception using `logger.LogError(exception, ...)` so that root-cause diagnosis does not require a debugger or direct filesystem inspection.

**FR-10.3 — Key lifecycle events produce structured log entries**
At minimum, the following events produce a named log entry at the stated level:

| Event | Level |
|---|---|
| Download request received by supervisor, user and mailbox logged | Information |
| STEVE auth request sent | Information |
| STEVE auth token received | Information |
| Mailbox list retrieved, matching mailbox count logged | Information |
| Staging directory created, path logged | Information |
| Each file download started (messageId, fileName) | Information |
| Each file download completed (messageId, byte count) | Information |
| Each file download failed (messageId) | Error |
| Each mark-as-read succeeded (messageId) | Information |
| Each mark-as-read failed (messageId) | Warning |
| Zip compression started | Information |
| Zip compression completed, output path logged | Information |
| Zip compression failed | Error |
| Actor stopping after successful completion | Information |

**FR-10.4 — Debug leftover removed**
The `Console.WriteLine("here")` at the end of the `ReceiveAsync` handler in `SteveAPI_Instance` is removed.

**FR-10.5 — No behavioral or UI changes**
All changes are confined to the logging layer. The download workflow, the filesystem-based status mechanism (`GetQueueResult`), and the UI are unchanged.

---

### FR-11 — STEVE PRAMS Download: Structured Logging

`stevePRAMSController` has the same logging gaps as `steveMMRIAController`: `ILogger<stevePRAMSController>` is injected but never used, and all download processing runs through the same `SteveAPI_Instance` actor with the same bare `Console.WriteLine` / swallowed-exception pattern described in FR-10.

Because both `/steveMMRIA` and `/stevePRAMS` dispatch to the same shared `SteveAPI_Instance` actor via `steve-api-supervisor`, the actor-side changes in FR-10.1 through FR-10.4 cover the PRAMS download path automatically. FR-11 records the PRAMS-specific scope and differences so the implementing epic is unambiguous.

**FR-11.1 — Actor-side logging covered by FR-10**
The `SteveAPI_Instance` and `SteveAPISupervisor` changes specified in FR-10.1 through FR-10.4 apply equally to PRAMS downloads. No additional actor changes are required for FR-11.

**FR-11.2 — stevePRAMSController ILogger usage aligned with steveMMRIAController**
The `ILogger<stevePRAMSController>` injected into the controller is unused. Its usage is aligned with whatever controller-level logging pattern is established for `steveMMRIAController` under FR-10 — at minimum, logging when a download request is received and dispatched.

**FR-11.3 — PRAMS is single-mailbox; no multi-mailbox loop**
Unlike `steveMMRIAController`, the PRAMS controller only supports the `PRAMS` mailbox — there is no "All" option and no multi-mailbox iteration. The FR-10.3 log event for "mailbox count" is trivially 1 for PRAMS; no special handling is needed.

**FR-11.4 — No debug leftover to remove**
The `Console.WriteLine("here")` present in `steveMMRIAController` is already commented out in `stevePRAMSController`. FR-10.4 has no equivalent action for PRAMS.

**FR-11.5 — No behavioral or UI changes**
All changes are confined to the logging layer. The PRAMS download workflow, filesystem-based status mechanism, and UI are unchanged.

> **Implementation note:** FR-10 and FR-11 share the actor-side fix. They may be delivered as a single epic or as two sequential stories within one epic — the actor story ships first, the controller-level story for each route follows.

---

### FR-12 — Case Excel Export: Column Width Auto-Fit

The case `.xlsx` export (generated via `WriteCSV.WriteToExcel()`) produces columns at Excel's default narrow width. Users must manually widen every column to read the data. The root cause is that the current library, FastExcel 3.0.13, has no column width API.

**FR-12.1 — Replace FastExcel with ClosedXML for the case export write path**
`WriteCSV.WriteToExcel()` is rewritten using ClosedXML (MIT license). ClosedXML is added as a NuGet dependency to `mmria-server.csproj`. FastExcel is retained for the other export paths that use it (`ije_messageController`, `vro_exportController`, `manage_usersController`) — they are not changed.

**FR-12.2 — All columns auto-fit to content**
After writing the data, `worksheet.Columns().AdjustToContents()` is called so that each column is sized to the width of its widest cell value (including the header row). No column requires manual resizing to read its content.

**FR-12.3 — Column width is capped at a readable maximum**
Columns whose widest value exceeds 80 characters are capped at 80 characters wide. This prevents narrative or free-text columns from producing columns so wide they make the sheet unusable. The cap does not truncate data — it only limits the displayed column width.

**FR-12.4 — `Template.xlsx` dependency is removed from the case export path**
ClosedXML creates the workbook directly in memory — no template file is needed. The `database-scripts/Template.xlsx` file is retained on disk (still used by other paths) but is no longer loaded by `WriteCSV.WriteToExcel()`.

**FR-12.5 — No behavioral changes to CSV or other export formats**
The change is scoped to `WriteCSV.WriteToExcel()`. The CSV write path, the data content, the column set, the export queue flow, and the UI are unchanged.

---

### FR-13 — Vitals Import: Father's Race Principal Tribe Mapping Correction

_(Source: BUG 119513 — Rel 4.2, P-High, reported by Susana. Iteration: MMRIA\ITDM 25-26 - Option Yr 4.)_

When an IJE vitals file (NAT or FET) is imported, the father's race "Specify Principal Tribe" field (`bfdcpdofr_p_tribe`, MMRIA path `birth_fetal_death_certificate_parent/demographic_of_father/race/principle_tribe`) is not correctly populated from the IJE fields `FRACE16` and `FRACE17`. The documented mapping rule is: *combine `FRACE16` and `FRACE17` into one field, separated by a pipe delimiter, transferring the strings verbatim; leave the MMRIA field empty when both source fields are blank.*

Root cause: at both call sites in `BatchItemProcessingService` (NAT path around line 1682 and FET path around line 2024), `field_set["FRACE16"]` is passed twice to the `FRACE16_17_NAT_Rule` / `FRACE16_17_FET_Rule` helpers instead of passing `field_set["FRACE17"]` as the second argument. The helper methods themselves in `MMRIAServicesHelper` implement the pipe-join contract correctly — they simply never receive the `FRACE17` value. The adjacent `FRACE18_19`, `FRACE20_21`, and `FRACE22_23` calls follow the correct `(N, N+1)` pattern, so this is a localized copy/paste defect, not a design gap.

**FR-13.1 — NAT import populates `bfdcpdofr_p_tribe` from both FRACE16 and FRACE17**
When a NAT file is imported and either `FRACE16` or `FRACE17` (or both) contain non-blank values, the resulting MMRIA case document has `birth_fetal_death_certificate_parent/demographic_of_father/race/principle_tribe` populated according to the pipe-join rule:
- Both non-blank: `"{FRACE16}|{FRACE17}"`
- Only `FRACE16` non-blank: `"{FRACE16}"`
- Only `FRACE17` non-blank: `"{FRACE17}"`
- Both blank: field left empty

**FR-13.2 — FET import applies the same rule**
The identical behavior described in FR-13.1 applies to the FET (fetal death) import path.

**FR-13.3 — Verbatim string transfer**
Source values from `FRACE16` and `FRACE17` are transferred verbatim (subject only to the existing input-trim performed during IJE line parsing). No case normalization, character mapping, or dictionary lookup is applied. The MMRIA field type remains a JSON string.

**FR-13.4 — No collateral changes to other FRACE fields**
The `FRACE18_19`, `FRACE20_21`, and `FRACE22_23` call sites and their helper rules are already correct and are not modified. No changes to the mother's race `MRACE*` fields or to any other IJE-to-MMRIA mapping.

**FR-13.5 — Regression coverage**
Unit test coverage is added for the four `FRACE16` / `FRACE17` combinations (both non-blank, FRACE16-only, FRACE17-only, both blank) on both the NAT and FET rule helpers to prevent future recurrence. If integration-level coverage for `BatchItemProcessingService` NAT/FET routing exists, the four-case matrix is exercised there as well.

> **OI-v42-4 (open):** Determine whether existing MMRIA cases previously created via vitals import require a retrospective data-correction migration for `principle_tribe` (analogous to the Epic 12 Story 12.2 vitals type-correction migration). Two considerations: (a) the correct source IJE values may no longer be available if the original files are not retained; (b) the impact scope is limited to cases where both `FRACE16` and `FRACE17` were populated or where only `FRACE17` was populated. Confirm with Nick after FR-13.1/FR-13.2 ship. A follow-on story (43.2) will be added to Epic 43 if remediation is approved.

---

### FR-14 — Case Narrative PDF Render Resilience

_(Source: BUG 118794 — Rel 4.1, P-Low, TA: Unable to create PDF on Case Narrative Case NJ-2024-7102, reported by NJ. Iteration: MMRIA\ITDM 25-26 - Option Yr 4.)_

When a case PDF is generated (`/pdf-version`), rendering must not fail because of malformed HTML content in the case narrative (`g_data.case_narrative.case_opening_overview`). The client-side PDF pipeline in `source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js` walks the narrative HTML into a pdfmake doc-def; when the narrative contains structurally broken markup (observed case: a `<table>` with a `<tr>` shorter than the header row), pdfmake throws `"Malformed table row, a cell is undefined"` at layout time and the entire case PDF fails to render — the user sees the "Please wait — Your request is being processed" spinner indefinitely and never receives a document.

The fix is a section-scoped fallback: when narrative rendering cannot succeed, the narrative is omitted from the PDF, a short neutral placeholder is emitted in its place, and every other section of the case renders normally. The stored narrative HTML is not modified.

**FR-14.1 — Narrative section renders inside an isolated fallback boundary**
The case narrative HTML→pdfmake conversion for `case_opening_overview` is guarded so that any failure — from either the DOM walker (`convert_html_to_pdf` / `ConvertHTMLDOMWalker`) or a downstream pdfmake layout error caused by narrative-derived content — is caught. On failure, the narrative content is dropped from the doc-def and PDF generation for every other section proceeds to completion. A malformed narrative on one case never blocks the entire report.

**FR-14.2 — Neutral placeholder replaces narrative on fallback**
When the narrative cannot be rendered, its position in the PDF is filled with a single short, neutral placeholder line. Recommended wording (exact text confirmed at implementation time):

> _"Case Narrative could not be included in this report. Please review the Case Narrative in the case and try again."_

The placeholder must not disclose the underlying cause (no mention of tables, HTML, parse errors, etc.), must not include a stack trace or error code, and must not include an excerpt of the narrative content. It is styled consistently with the surrounding section body — no red-alert framing, no console-error affordance in the PDF.

**FR-14.3 — Save and Open output paths share the fallback**
Both output modes in `pdfMake.createPdf(doc).download(...)` (`g_type_output == 'save'`) and `pdfMake.createPdf(doc).open(window)` produce the same behavior: a complete PDF is delivered, with the placeholder standing in for the narrative body. The user experience is identical regardless of which path was invoked.

**FR-14.4 — No changes to stored narrative HTML**
The stored value at `g_data.case_narrative.case_opening_overview` is not modified. This is a render-side resilience change only, in accordance with the project-context §2.4 rule that the narrative HTML structure must not be altered. Any editor or save-path sanitization work remains out of scope for FR-14 (see FR-9 for narrative save-path items).

**FR-14.5 — Client-side notice on fallback**
When the fallback fires, the browser emits a single `console.warn` entry naming the affected `record_id` (or case `_id` when the record ID is absent), so that a developer inspecting the browser console during support triage can identify the fallback event. The console entry must not include an excerpt of the narrative content (PII avoidance). No new server-side log is required for FR-14.

**FR-14.6 — Non-narrative sections are unaffected**
No behavior change is introduced for any section other than `case_opening_overview`. The doc-def rows produced for demographic, death certificate, birth/fetal death, prenatal care, ER visit, medical transport, social/environmental profile, mental health, informant interview, committee review, or any other section are identical to today's output on cases whose narrative renders normally.

> **OI-v42-5 (open):** Confirm final placeholder wording with Vilma (Draft candidate: _"Case Narrative could not be included in this report. Please review the Case Narrative in the case and try again."_ — compare with her Jun 10 draft: _"Please check the format of Case Narrative content. The content has one or more unexpected formats. Please fix the issue before generating the PDF."_ Per Nick's direction the wording should be brief and must not describe the underlying cause.) Confirm at story kickoff.

---

### FR-15 — Case Route Migration — Numeric Index to Case `_id`

_(Source: Architectural review with Winston, 2026-08-21. Scoped to case-adjacent JS entry points only.)_

The case editor and its sibling case-facing views (de-identified, committee-member, editor navigation renderers, offline navigation reconciliation, print-version verification) route by **numeric index into `g_ui.case_view_list`** as the first hash segment (`/Case#/0/home_record`). Because that list mutates — sort/filter changes, offline sync inserting cases, a second user adding a case, add-case UI flows — the *same URL* can silently resolve to a *different case* on refresh or navigation. Refresh must also load the case list before it can resolve the index, forcing extra offline-mode reconciliation code paths.

The fix is a surgical URL-identity swap: replace the numeric segment with the CouchDB case `_id` (GUID) while preserving the rest of the URL shape verbatim. All other route segments (`summary`, `field_search`, `notifications`, form names, selected-child ids) are unchanged. Admin pages, aggregate/overdose reports, data-dictionary, and export-list-manager are explicitly out of scope.

**FR-15.1 — Case identity moves from list position to case `_id` in the URL**
The first hash segment of a case-page URL identifies the case by CouchDB `_id` (GUID). Route shape is preserved otherwise: `/Case#/{caseId}/{form}/{child}` replaces `/Case#/{numericIndex}/{form}/{child}`. Non-case routes such as `#/summary`, `#/field_search/…`, `#/notifications`, and `#/pinned` are unchanged.

**FR-15.2 — Deterministic case-id vs form-keyword discriminator**
`url_monitor.get_url_state` returns a `selected_case_id` field when `path_array[0]` is a case id, and `selected_form_name` when it is one of the known form keywords (`summary`, `field_search`, `notifications`, `pinned`, and any others enumerated at implementation time from a full grep of `path_array[0] ==` comparisons). Consumers no longer need to guess the segment's role.

**FR-15.3 — Hashchange resolves case by `_id` directly, not by list position**
The case-editor hashchange handlers consume `url_state.selected_case_id` directly. `case_view_list[index].id` lookups on the URL-resolution path are removed. Any "next/prev in list" navigation UI derives position at click time via `case_view_list.findIndex(c => c.id === currentCaseId)`; position is never persisted in the URL.

**FR-15.4 — Legacy numeric URLs redirect to the case list**
When `path_array[0]` is purely numeric (`/^\d+$/`), the hashchange handler issues `history.replaceState` to `#/summary` (no back-stack pollution) and emits a single `console.info` tagged with the redirect reason. No silent index → id translation. Old bookmarks land on the case list and the user re-picks.

**FR-15.5 — Unauthorized or unknown case id redirects to the case list**
When the URL carries a case id the current user cannot open (unauthorized, not-found, or wrong tenant), the hashchange handler redirects to `#/summary` via `history.replaceState`. A `// TODO(46.x): show landing page / modal for unauthorized case access` stub is left at the redirect site. No landing page or modal is implemented in this FR.

**FR-15.6 — Offline navigation consumes case id directly**
`OfflineNavigationManager.getTargetCaseIdForHashChange` and sibling offline reconciliation code that previously mapped list index → case id is simplified to accept a case id directly. `g_offline_case_index_map` continues to exist as an offline lookup for id-list UI affordances but is no longer read to resolve URL → case. The `case_index` localStorage key (a storage-schema constant unrelated to URL positional indexing) is not renamed.

**FR-15.7 — Scope boundary preserved**
This FR touches only case-adjacent JS entry points: `wwwroot/scripts/case/`, `de-identified/`, `committee-member/`, the case-facing navigation renderers under `editor/`, `offline/` navigation reconciliation, `url_monitor.js`, and verification-only sweep of `print-version/`. Admin pages, aggregate/overdose reports, data-dictionary, export-list-manager, and server-side C# export tooling that mentions `record_index` (an unrelated data-model concept) are out of scope. Server-side controllers, routes, and Razor views are unchanged.

**FR-15.8 — Hash-based routing shape is preserved**
The fragment prefix `#/` is retained. Migration to History API path routing (`/case/{caseId}/{form}`) is explicitly a future epic and not part of FR-15.

> **OI-v42-6 (open, minor):** The story assumes CouchDB `_id` GUIDs (36-char UUIDs) never collide with a form-keyword string. If a future form keyword is ever added that could be mistaken for a GUID prefix, the discriminator becomes ambiguous. Bulletproofing via an explicit prefix (e.g. `#/c/{caseId}/…`) is a small follow-on if the risk ever materializes; deferred as low-probability for now.

---

--- All changes must function correctly in Microsoft Edge and Google Chrome.

NFR-2: The geocoding refactor introduces no new client-side dependencies, no bundler changes, and no metadata schema changes.

NFR-3: Architecture rule — no direct CouchDB calls in controllers. Geocoding follows the Feature/Manager/DAL pattern: `GeocodingManager` in SharedLibraries; controller resolves config and delegates.

NFR-4: The TAMU API key is resolved at server startup from the existing CouchDB configuration document (`geocode_api_key`) and is never serialized into any client-served file.

---

## Open Items

- OI-1: Additional bug fix epics to be identified and added. List is not final.
- OI-2: Confirm Epic 30 story amendments needed for server-side CVS and case-reload behavior vs. the field-update-in-place design in existing Epic 30 stories.
- OI-6 (FR-10): Determine how `ILogger` is injected into `SteveAPI_Instance`. The actor currently uses a parameterless constructor and is spawned via `Context.ActorOf<SteveAPI_Instance>()` with no DI wiring. Options: (a) pass the logger through `Props.Create(() => new SteveAPI_Instance(logger))` from the supervisor, or (b) wire Akka.NET's `ServiceProvider` integration if already available in the actor system setup. Confirm at implementation time. Either approach is acceptable; the story author decides.
- OI-7 (FR-10): The FR-10.3 log entry for "STEVE auth token received" must confirm receipt only — the token value itself must not appear in any log output.
- OI-v42-4 (FR-13): Retrospective data-correction migration decision for cases previously imported with mis-mapped `principle_tribe`. See FR-13 OI callout.
- OI-v42-5 (FR-14): Final placeholder wording for the Case Narrative PDF fallback. See FR-14.2 OI callout.

---

## FR Coverage Map

FR-1.1 – FR-1.9: Epic 30 — Unified Server-Side Geocoding (TAMU Refactor)  
FR-1.10 – FR-1.11: Epic 42 — Geocoding Location Registry (Declarative Refactor) + Certainty Code Modal Restoration  
FR-2.1 – FR-2.9: Epic 29 — Record ID Uniqueness Enforcement  
FR-3.1 – FR-3.11: Epic 37 — Form Designer Removal — Static HTML Form Rendering  
FR-4.1 – FR-4.3: Epic 38 — IJE Batch Re-Upload Rejection & Import Observability  
FR-5.1 – FR-5.3: Epic TBD — Update Year of Death Record ID Regression Fix  
FR-7.1 – FR-7.6: Epic TBD — Session Expiry Automatic Logout Redirect on 401  
FR-8.1 – FR-8.5: Epic TBD — Per-Tenant Authentication Mode (SAMS + Password Co-existence)  
FR-10.1 – FR-10.5: Epic TBD — STEVE Download Structured Logging  
FR-11.1 – FR-11.5: Epic TBD — STEVE PRAMS Download Structured Logging  
FR-12.1 – FR-12.5: Epic TBD — Case Excel Export Column Width Auto-Fit (ClosedXML)  
FR-13.1 – FR-13.5: Epic 43 — Vitals Import Father's Race Principal Tribe Mapping Correction (BUG 119513)  
FR-14.1 – FR-14.6: Epic 44 — Case Narrative PDF Render Resilience (BUG 118794)  
FR-15.1 – FR-15.8: Epic 46 — Case Route Migration — Numeric Index to Case `_id`
