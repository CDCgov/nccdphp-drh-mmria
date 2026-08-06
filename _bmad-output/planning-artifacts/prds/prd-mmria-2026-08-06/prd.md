---
title: "PRD: MMRIA V4.2"
status: draft
created: 2026-08-06
updated: 2026-08-06
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

### FR-6 — CouchDB `/_users/` URL Encoding Fix (Plus-Sign Username Support)

`AccountDAL` constructs `/_users/org.couchdb.user:{username}` paths using incorrect or absent encoding. `GetCouchDbUserAsync` uses `HtmlEncode` (which encodes HTML characters, not URL path characters). `CheckUserAsync`, `GetAsync`, and `PutUserAsync` use raw string interpolation with no encoding. Both patterns happen to work for common usernames but are semantically wrong and will break for usernames containing characters that require percent-encoding.

The practical motivator is enabling Gmail-style `+` test accounts (e.g., `testaccount+abstractor@gmail.com`) — a common pattern for creating multiple test identities from one inbox.

**FR-6.1 — `Uri.EscapeDataString` applied to all `/_users/` path construction**
`GetCouchDbUserAsync`, `CheckUserAsync`, `GetAsync`, and `PutUserAsync` in `AccountDAL` use `Uri.EscapeDataString(userDocId)` when constructing `/_users/` URL path segments. No other encoding function (`HtmlEncode`, `UrlEncode`, raw interpolation) is used for this purpose.

**FR-6.2 — Login payload encoding is not changed**
`BuildSessionAuthFormPayload` correctly encodes `+` as `%2B` in the `application/x-www-form-urlencoded` request body via `WriteFormUrlEncoded`. This behavior is preserved unchanged.

**FR-6.3 — Usernames containing `+` are fully supported**
After the fix, a user with the email address `testaccount+abstractor@gmail.com` can be created in the `_users` database and authenticate without URL construction errors.

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

### FR-9 — Case Narrative Editor: Post-v4.1 Follow-up Tweaks

QA feedback on the v4.1 case narrative paste and save-path changes identified two items requiring follow-up. The v4.1 changes are working well overall; these are targeted adjustments.

**FR-9.1 — Strip emoji characters from case narrative on save**
Emoji characters (Unicode range U+1F300 and above, and related symbol ranges) entered or pasted into the case narrative editor are removed from the stored value on save. Emojis are not part of the approved narrative format and can cause encoding issues in IJE exports, CSV de-identified exports, and downstream data consumers. The save-path sanitizer already strips XSS vectors; emoji stripping is added to the same path. Characters within the standard ASCII and supported extended Latin ranges are not affected.

**FR-9.2 — Explicitly strip strikethrough from case narrative on save**
Strikethrough markup (`<s>`, `<strike>`, `<del>`) is stripped from the case narrative on the save path, consistent with the v4.1 decision to preserve only the explicitly approved formatting tags (`<br>`, `<u>`, `<hr>`, `<font>`). Strikethrough is not an approved narrative format and was retained as an unintentional side effect of the v4.1 save-path fix broadening the tag preservation. This is a narrowing correction — no other currently preserved tags are affected.

> **Accepted behavior (not a bug):** Background colors/highlights are not retained on paste into the narrative editor. This is intentional — background color cannot be rendered consistently across print view, PDF export, and IJE/CSV data output. No action required.

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

---

### FR-2 — Record ID Uniqueness Enforcement

_(Carried from v4.1 backlog — Epic 29. Full FR definition: see `epics.md` FR-29.1–FR-29.3.)_

Abstractors creating new cases are protected against duplicate MMRIA Record IDs by a defense-in-depth strategy: server-side format validation and uniqueness guard, client-side per-candidate API check before save, and a functional `record_id_list` CouchDB view replacing the broken bulk-list dependency.

---

### FR-3 — Form Designer Removal — Static HTML Form Rendering

_(Carried from v4.1 backlog — Epic 37. Full FR definition: see `epics.md` FR-37.1–FR-37.11.)_

The `/form-designer` WYSIWYG tool, its JS/CSS assets, its API write endpoints, and the case form's runtime dependency on `g_default_ui_specification` are permanently removed. A build-time generator produces static HTML form sections from metadata and UI specification. A `form-binder.js` module handles data population, conditional visibility, grid cloning, and change tracking for all case form views.

---

### FR-4 — IJE Upload Duplicate Prevention and Logging

When an IJE/vitals file is uploaded, the upload controller currently prevents duplicate cases from being created in the `mmrds` case database. However, the same batch data is still written to the `vital_import` CouchDB database a second time, creating duplicate import records. Additionally, no meaningful log output is produced to help a developer or operator understand what was skipped and why.

**FR-4.1 — Prevent duplicate entries in vital_import database**
When an IJE file is submitted for upload, the server checks the `vital_import` database before writing the batch. If the batch (or the individual case records within it) already exists in `vital_import`, the duplicate entries are not written. The existing case-level duplicate guard for `mmrds` is not affected.

**FR-4.2 — Meaningful logging for duplicate detection**
When cases or batch records are identified as duplicates during IJE upload processing, a structured log entry is written for each skipped item. The log entry includes at minimum: the case identifier, the reason skipped (duplicate), and the existing `vital_import` record reference. The logging is sufficient for a developer to audit what happened on any given upload run without inspecting the database directly.

**FR-4.3 — User feedback on duplicate upload**
When a re-upload is detected (all cases in the file are already present in `vital_import`), the upload response communicates the outcome clearly — indicating how many records were skipped as duplicates vs. processed as new. The existing success/error response shape is preserved; duplicate-skip information is added to it.

> **OI-3 (open):** Confirm partial-batch behavior — if a file contains 3 new cases and 2 already-existing cases, should the system (a) process the 3 new cases and skip the 2 duplicates, or (b) reject the entire file and require the user to resolve before re-submitting? Determine at implementation time.

---

NFR-1: All changes must function correctly in Microsoft Edge and Google Chrome.

NFR-2: The geocoding refactor introduces no new client-side dependencies, no bundler changes, and no metadata schema changes.

NFR-3: Architecture rule — no direct CouchDB calls in controllers. Geocoding follows the Feature/Manager/DAL pattern: `GeocodingManager` in SharedLibraries; controller resolves config and delegates.

NFR-4: The TAMU API key is resolved at server startup from the existing CouchDB configuration document (`geocode_api_key`) and is never serialized into any client-served file.

---

## Open Items

- OI-1: Additional bug fix epics to be identified and added. List is not final.
- OI-2: Confirm Epic 30 story amendments needed for server-side CVS and case-reload behavior vs. the field-update-in-place design in existing Epic 30 stories.

---

## FR Coverage Map

FR-1.1 – FR-1.9: Epic 30 — Unified Server-Side Geocoding (TAMU Refactor)  
FR-2.1 – FR-2.3: Epic 29 — Record ID Uniqueness Enforcement  
FR-3.1 – FR-3.11: Epic 37 — Form Designer Removal — Static HTML Form Rendering  
FR-4.1 – FR-4.3: Epic TBD — IJE Upload Duplicate Prevention and Logging  
FR-5.1 – FR-5.3: Epic TBD — Update Year of Death Record ID Regression Fix  
FR-6.1 – FR-6.3: Epic TBD — CouchDB `/_users/` URL Encoding Fix  
FR-7.1 – FR-7.6: Epic TBD — Session Expiry Automatic Logout Redirect on 401  
FR-8.1 – FR-8.5: Epic TBD — Per-Tenant Authentication Mode (SAMS + Password Co-existence)  
FR-9.1 – FR-9.2: Epic TBD — Case Narrative Post-v4.1 Tweaks (Emoji Strip, Strikethrough Strip)
