# Security Scan 7: Medium+ Findings Without Justification

- Review date: `2026-04-02`
- Source export: `docs/ai/local/scans/mmria-security-scan-7.csv`
- Scope used for this review:
  - `Critical`, `High`, and `Medium` only
  - `Tagged` is blank
  - `Has Comments` is `FALSE`
- Assumption for "no justification":
  - In this export, the rows above also had `Is Reviewed = FALSE`.
  - The only reviewed medium+ rows were already tagged `False Positive` and had comments.

## Summary

- In-scope raw rows: `194`
- Critical: `39`
- High: `92`
- Medium: `63`
- Distinct category/file/line groups: `159`
- Distinct category/file groups: `96`

The scan is noisy, but it is not random. Most of the unresolved items cluster into a small number of recurring patterns:

1. Trusting client-supplied role, state, file name, or record metadata in admin and file-download flows.
2. Building file paths, download names, or external request URLs directly from request values.
3. Using broad inbound DTOs and persistence models as binder targets.
4. A smaller set of likely scanner carryover findings where the code already contains validation or the behavior is intentionally app-wide.

The safest low-disruption path is to fix the real trust-boundary problems first, then add trace-backed justifications for the items that are already mitigated in code.

## Highest-Signal Findings

| Priority | Finding pattern | Why it matters | Representative files |
| --- | --- | --- | --- |
| `1` | Client-controlled role/database/path decisions | A tampered form body can influence which tenant/database is queried or updated. | `Controllers/clear_case_status.cs`, `Controllers/recover_deleted_case.cs`, `Controllers/update_maiden_name.cs`, `Controllers/update_year_of_death.cs` |
| `1` | Path and download handling from route/query/body values | Enables path traversal, unintended overwrite, unsafe download names, or remote path injection. | `Controllers/backup_managerController.cs`, `Controllers/steveMMRIAController.cs`, `Controllers/stevePRAMSController.cs`, `util/WriteCSV.cs`, `Controllers/api/cvsAPIController.cs` |
| `2` | Manual external URL/header construction | Creates SSRF/header-manipulation noise and leaves a few real encoding gaps. | `mmria.common/getset/CouchDbHttpClient.cs`, `SharedLibraries/Session/DAL/SessionDAL.cs`, `Controllers/api/tamuGeoCodeController.cs`, `Controllers/AccountController.OIDC.cs` |
| `2` | Overbinding and sensitive field exposure | Server-owned fields are accepted from the client because rich models are used directly as request models. | `Controllers/api/OfflineCaseController.cs`, `Controllers/api/userController.cs`, `Controllers/manage_usersController.cs`, `model/case-status/*.cs`, `mmria.common/couchdb/user.cs` |
| `3` | Reflected content, error echo, and regex/log noise | Some findings are probably false positives, but a few can be reduced safely by avoiding raw error/detail reflection. | `Controllers/loggerController.cs`, `Controllers/_config.cs`, `Controllers/_usersController.cs`, `Controllers/broadcast_messageController.cs`, `Controllers/api/powerbi_measureController.cs`, `util/MultiTenantSetupService.cs` |

## Findings By Mitigation Bucket

### 1. Real trust-boundary issues in workflow controllers

These are the most important unresolved findings because the controller logic currently accepts role or tenant-selection data from the request model.

Examples:

- `source-code/mmria/mmria-server/Controllers/clear_case_status.cs`
  - `FindRecord(...)` uses `Model.Role` and `Model.StateDatabase` to decide which database to query.
  - `ClearCaseStatus(...)` uses `Model.Role` and `Model.StateDatabase` again during update flow.
- `source-code/mmria/mmria-server/Controllers/recover_deleted_case.cs`
  - `FindRecord(...)` and `UpdateDeletedCase(...)` trust `Model.Role` and `Model.StateDatabase`.
- `source-code/mmria/mmria-server/Controllers/update_maiden_name.cs`
  - `FindRecord(...)`, `ConfirmUpdateMaidenNameRequest(...)`, and `UpdateMaidenName(...)` trust client-posted `Role` and `StateDatabase`.
- `source-code/mmria/mmria-server/Controllers/update_year_of_death.cs`
  - Same pattern as the maiden-name flow.

Why this matters:

- These endpoints are role-protected, but they still let the client supply role and state-routing hints.
- Hidden form fields and posted JSON are easy to tamper with.
- Even if authorization blocks most abuse, this is the exact kind of pattern Fortify keeps flagging until the role and tenant choice are derived on the server.

Safest mitigation:

1. Stop binding `Role` from the request for these flows.
2. Derive the effective role from `User.IsInRole(...)`.
3. Treat `StateDatabase` as an identifier to validate against the authenticated user's allowed scope, not as trusted routing input.
4. Replace the current rich postback models with minimal command DTOs that include only the values the user is allowed to change.
5. Add a regression test that posts a valid request while tampering `Role` and `StateDatabase` and confirms the server ignores or rejects the tampered values.

### 2. Path handling and download flows need a shared containment helper

This is the next best remediation batch because the fixes are surgical and easy to verify.

Examples:

- `source-code/mmria/mmria-server/Controllers/backup_managerController.cs`
  - Route values `id`, `folder`, and `file_name` are used in outbound URLs, local file paths, `Directory.CreateDirectory`, `FileStream`, `PhysicalFileResult`, and `FileDownloadName`.
- `source-code/mmria/mmria-server/Controllers/steveMMRIAController.cs`
  - `GetFileResult(string FileName)` and `DeleteFileResult(string FileName)` combine raw `FileName` with `download_directory`.
- `source-code/mmria/mmria-server/Controllers/stevePRAMSController.cs`
  - Same pattern as `steveMMRIAController`.
- `source-code/mmria/mmria-server/util/WriteCSV.cs`
  - Constructor writes to `folder_name + "/" + this.file_name`.
- `source-code/mmria/mmria-server/Controllers/api/cvsAPIController.cs`
  - Uses `post_payload.id` to form `CVS-{id}.pdf`, write it to disk, and return it as a download name.

Important context:

- `source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs` already contains good contained-path building helpers.
- Those helpers look like the right design direction, but they are not yet used across the older controllers.

Safest mitigation:

1. Extract the contained-path logic from `SteveAPI_Instance` into a shared reusable utility.
2. Use a single helper for:
   - validating a single path segment
   - resolving a file under a trusted base directory
   - resolving a child directory under a trusted base directory
3. Sanitize every download name with a single-segment allowlist before returning it in `File(...)` or `PhysicalFileResult`.
4. Encode outbound path segments with `Uri.EscapeDataString(...)` before appending them to backup/service URLs.
5. Prefer server-generated temporary file names for remote downloads instead of persisting the caller-supplied name directly.

### 3. External request and header construction is mixed: some real gaps, some likely carryover

Examples of real cleanup opportunities:

- `source-code/mmria/mmria-server/Controllers/api/tamuGeoCodeController.cs`
  - Builds the Texas A&M geocode URL with raw query string interpolation.
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs`
  - Adds `AuthSession` values directly to `WebRequest.Headers`.
- `source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs`
  - Builds a user-info request with token-in-query-string plus manually-added auth/client headers.

Examples that look more like justification candidates than code-fix candidates:

- `nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs`
  - Already validates URL scheme, blocks internal/private IP ranges, sanitizes headers, and strips unsafe header-name characters.
- `source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs`
  - Already validates contained paths and constrains bearer-token characters.

Safest mitigation:

1. Refactor `SessionDAL` to use `CouchDbHttpClient` instead of raw `WebRequest`.
2. Replace hand-built query strings with `UriBuilder` or `QueryHelpers.AddQueryString(...)`.
3. Use typed header APIs where possible:
   - `AuthenticationHeaderValue`
   - centralized sanitized header helper
4. Add focused tests for:
   - rejected private/internal URLs
   - CR/LF removal from header values
   - path-segment encoding for external service calls
5. After those tests are in place, collect trace evidence and justify the remaining `CouchDbHttpClient` and `SteveAPI_Instance` findings rather than continuing blind code-shaping.

### 4. Overbinding and sensitive DTO exposure is widespread but can be handled incrementally

This is the largest bucket by row count, but most of it can be reduced without touching business logic.

Examples:

- `source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs`
  - Many `[FromBody]` endpoints accept rich request types directly.
- `source-code/mmria/mmria-server/Controllers/api/userController.cs`
  - Accepts `mmria.common.model.couchdb.user` directly from the request body.
- `source-code/mmria/mmria-server/Controllers/manage_usersController.cs`
  - Accepts `FormAccessSpecification` with audit fields and metadata from the client.
- `source-code/mmria/mmria-server/model/case-status/CaseStatusRequest.cs`
- `source-code/mmria/mmria-server/model/case-status/MaidenNameRequest.cs`
- `source-code/mmria/mmria-server/model/case-status/YearOfDeathRequest.cs`
- `source-code/mmria/mmria-server/model/case-status/RecoverDeletedRequest.cs`
- `nccdphp-drh-mmria-common/mmria.common/couchdb/user.cs`
- `nccdphp-drh-mmria-common/mmria.common/couchdb/user_role_jurisdiction.cs`
- `nccdphp-drh-mmria-common/mmria.common/case-version/mmria/v260120/mmria_case.cs`

Why this matters:

- The application often uses storage models and UI models as inbound request contracts.
- That makes it easy for the scanner to see `_id`, `_rev`, `Role`, `roles`, audit fields, or hidden/generated fields as overbindable, even when the controller only reads a subset.
- In a few places, that scanner concern is justified because the controller does trust some of those fields.

Safest mitigation:

1. Create slim inbound DTOs for write endpoints.
2. Keep persistence models and generated case models out of direct model binding.
3. Mark server-owned fields as non-bindable where practical.
4. Map request DTOs to domain/storage objects server-side.
5. Start with the highest-value endpoints:
   - `api/userController`
   - `manage_usersController`
   - the case-status / recover / maiden-name / year-of-death controllers
   - the offline case write endpoints

### 5. Reflected XSS, raw error detail, log forging, and regex findings need targeted cleanup, not broad rewrites

Examples:

- `source-code/mmria/mmria-server/Controllers/loggerController.cs`
  - Returns raw `ex.Message` in several failure responses.
  - Accepts and returns user-controlled log and filter data.
- `source-code/mmria/mmria-server/Controllers/_config.cs`
- `source-code/mmria/mmria-server/Controllers/_usersController.cs`
- `source-code/mmria/mmria-server/Controllers/broadcast_messageController.cs`
  - These are mostly returning JSON or view models that include request-driven values.
- `source-code/mmria/mmria-server/Controllers/api/powerbi_measureController.cs`
  - Creates `new Regex("^" + indicator_id)` from request input; this looks unnecessary because the code later compares exact equality.
- `source-code/mmria/mmria-server/util/MultiTenantSetupService.cs`
  - Findings land on structured log statements that already use message templates.

Safest mitigation:

1. Stop returning raw exception text to the client unless it is explicitly safe and needed.
2. Replace client-visible `details = ex.Message` payloads with a generic message plus server-side logging/correlation id.
3. Remove the unused regex in `powerbi_measureController`, or replace with `Regex.Escape(...)` if matching is truly required.
4. Verify the corresponding Razor views do not use `Html.Raw(...)` for any of these values.
5. Treat the structured-logging findings in `MultiTenantSetupService` as likely justification candidates after confirming the logger remains template-based and does not concatenate untrusted strings into the message template.

## Likely Justification Candidates

These items should not be the next code-shaping target unless trace review shows a real bypass:

- `nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs`
  - The current code already rejects non-HTTP(S) URLs and blocks common private/internal addresses.
- `source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs`
  - The current code already validates contained paths and bearer-token characters.
- `source-code/mmria/mmria-server/Controllers/AccountController.cs`
- `source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs`
- `source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs`
  - The cookie "overly broad domain/path" findings are likely scanner noise or policy mismatch:
    - no `Domain` is explicitly set
    - `Path = "/"` is expected for application-wide session cookies
- `source-code/mmria/mmria-server/wwwroot/scripts/manage-case-folders/index.js`
  - The "Privacy Violation" hits at the redirect lines do not look actionable from the code shown and likely need scanner trace review rather than code change.
- `source-code/mmria/mmria-server/util/MultiTenantSetupService.cs`
  - The log-forging hits land on structured logging usage, which is usually the correct pattern.

## Low-Risk Mitigation Plan

### Phase 1: Quick wins that reduce real risk without changing behavior

Target outcome: remove the highest-confidence trust-boundary findings with minimal user-facing impact.

1. Introduce a shared path-containment helper and use it in:
   - `backup_managerController`
   - `steveMMRIAController`
   - `stevePRAMSController`
   - `WriteCSV`
   - `cvsAPIController`
2. Replace raw external query-string interpolation in:
   - `tamuGeoCodeController`
   - `AccountController.OIDC`
3. Replace `Redirect(returnUrl)` with `LocalRedirect(returnUrl)` in `AccountController`.
4. Remove or escape the regex in `powerbi_measureController`.
5. Replace raw client-facing exception detail payloads with generic error responses in:
   - `loggerController`
   - any other controller still returning `details = ex.Message`

### Phase 2: Binder hardening where the current model trust is real

Target outcome: eliminate scanner-valid overbinding patterns without rewriting the underlying business logic.

1. Create dedicated request DTOs for:
   - case status clear
   - recover deleted case
   - update maiden name
   - update year of death
   - manage users / form access
   - user save endpoints
2. Remove client-posted `Role` from those flows and derive it from the authenticated user.
3. Validate posted tenant/state identifiers against authorized scope server-side.
4. Keep `_id`, `_rev`, roles, audit metadata, and server-generated timestamps out of inbound DTOs.
5. Add tests for tampered post bodies and hidden field manipulation.

### Phase 3: Consolidate HTTP transport behavior

Target outcome: reduce repeated header/SSRF findings and make justification easier.

1. Migrate `SessionDAL` off raw `WebRequest` and onto `CouchDbHttpClient`.
2. Reuse one header-sanitization path for auth/session headers.
3. Add tests that prove:
   - private/internal URLs are rejected
   - invalid header characters are stripped
   - outbound path segments are escaped

### Phase 4: Trace review and justifications

Target outcome: close the remaining scanner carryover safely instead of over-modifying stable code.

1. Pull scanner traces for the remaining `CouchDbHttpClient`, `SteveAPI_Instance`, cookie-scope, and frontend privacy findings.
2. Record trace-backed justifications where the sink is already protected.
3. Re-run the scan and update the tracker with:
   - resolved rows
   - justified rows
   - any truly new survivors

## Recommended First Implementation Batch

If we want the best risk-reduction-to-disruption ratio, the first code batch should be:

1. Shared path/file helper extraction and rollout to the download/export controllers.
2. Request DTO hardening for the four admin workflow controllers that trust `Role` and `StateDatabase`.
3. `SessionDAL` refactor to the hardened CouchDB client.
4. `LocalRedirect` and generic error response cleanup.

That batch should remove a meaningful portion of the real attack-surface issues without forcing broad behavior changes in the rest of the app.

## Notes

- This document is a triage-and-plan review only. No repo code was changed as part of this pass.
- The scan still contains a mix of real findings and likely false positives. The plan above intentionally separates those two buckets so we do not destabilize working code just to satisfy the scanner.
