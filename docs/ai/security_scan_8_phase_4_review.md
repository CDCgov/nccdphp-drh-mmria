# Security Scan 8 Phase 4 Review

- Review date: `2026-04-02`
- Source export: `docs/ai/local/scans/mmria-security-scan-8.csv`
- Scope used for this review:
  - `Critical`, `High`, and `Medium` only
  - `Tagged` is blank
  - `Has Comments` is `FALSE`
  - `mmria-server.tests` is excluded
- Export note:
  - the Fortify file is tab-delimited even though it ends in `.csv`

## Summary

- In-scope raw rows: `170`
- Critical: `37`
- High: `82`
- Medium: `51`
- Distinct category/file/line groups: `135`
- Distinct category/file groups: `78`
- Delta from `scan-7`: `-24` rows, `-24` line groups, `-12` category/file groups

`scan-8` is a good Phase 4 starting point because it shows the earlier hardening work actually moved the queue down. The biggest signal from this rescan is that several remaining findings are now landing on the validation helpers and wrapper sinks we introduced, which is usually a sign that the next safe step is trace-backed justification rather than more code shaping.

## What Scan-8 Validated

The following `scan-7` groups are gone in `scan-8`:

- `Open Redirect` in `AccountController`
- `Mass Assignment: Insecure Binder Configuration` in:
  - `clear_case_status.cs`
  - `recover_deleted_case.cs`
  - `update_maiden_name.cs`
  - `update_year_of_death.cs`
- `Mass Assignment: Sensitive Field Exposure` in the four case-status request models
- `Server-Side Request Forgery` in `AccountController.OIDC.cs`
- `Denial of Service: Regular Expression` in `powerbi_measureController.cs`
- `Path Manipulation: Base Path Overwriting` in:
  - `backup_managerController.cs`
  - `cvsAPIController.cs`
  - `pdfCentralController.cs`
  - `steveMMRIAController.cs`
  - `stevePRAMSController.cs`

That lines up with the work we already did in Phases 1-3 and gives us a clean boundary for this phase: justify the wrapper/helper carryover items, and avoid a disruptive late-cycle rewrite.

## Phase 4 Findings

### 1. Helper and wrapper sinks are now the main carryover cluster

The strongest Phase 4 justification candidates are:

- `nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs`
- `source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs`
- `source-code/mmria/mmria-server/util/ContainedPathHelper.cs`

Why these look like justification candidates:

- `CouchDbHttpClient` already centralizes the protections Fortify normally wants to see:
  - header-name cleanup
  - header-value sanitization
  - `AuthSession` normalization
  - `Uri.EscapeDataString(...)` for the outbound cookie value
  - URL validation and SSRF guardrails before the request is sent
- `SessionDAL` no longer builds raw `WebRequest` objects. It now passes `AuthSessionValue` into `CouchDbRequestOptions` and reads `Set-Cookie` values back from captured response headers.
- `SteveAPI_Instance` already validates contained names before combining paths, checks the resolved path stays under the trusted root, and restricts the bearer token before creating the auth header.
- `ContainedPathHelper` is the extracted version of the same validation pattern, and the new `Path Manipulation` finding now lands on the helper's `FileStream` constructor instead of the old controller sinks.

What this means:

- The scan is still following taint to the sink, but the code structure is now much closer to the correct end-state.
- The right next action is to pull the Fortify traces and justify these findings where the trace confirms the sink only receives validated values.
- Another broad round of helper rewrites would add risk without much confidence of clearing more rows.

### 2. Cookie-scope findings still look policy-style, not exploit-style

The remaining cookie findings are clustered in:

- `source-code/mmria/mmria-server/Controllers/AccountController.cs`
- `source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs`
- `source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs`

Why they look like justification candidates:

- the cookies are session/application cookies, so `Path = "/"` is expected
- `SameSite = Strict` is already set on the main auth/session cookies
- the code does not explicitly set a `Domain`, which means the browser keeps the cookie host-scoped by default

This looks much more like a Fortify policy mismatch than a late-stage security defect. If trace review is available, record that evidence and justify the rows instead of changing cookie scope in a way that could break login or offline behavior.

### 3. Frontend privacy and structured logging survivors are still low-disruption justification targets

The main examples are:

- `source-code/mmria/mmria-server/wwwroot/scripts/manage-case-folders/index.js`
- `source-code/mmria/mmria-server/util/MultiTenantSetupService.cs`

Why they look low-risk to justify:

- the `manage-case-folders` redirect uses `location.protocol + '//' + location.host`, so it stays on the current origin rather than sending data to an external destination
- `MultiTenantSetupService` uses structured `ILogger` templates with `{Tenant}` and `{Action}` placeholders, which is the correct logging pattern and not raw string concatenation into the template itself

These are exactly the kinds of findings that should be trace-backed and justified rather than "fixed" by awkward code changes that do not materially improve the application.

## What Is Not A Phase 4 Justification Candidate

The rescan still contains some real-looking backlog that should not be folded into this phase:

- reflected-XSS clusters in `_config.cs`, `_usersController.cs`, `broadcast_messageController.cs`, `zipController.cs`, and a handful of single-line MVC actions
- remaining binder/input-formatter findings in `OfflineCaseController`, `caseController`, `manage_usersController`, and several API endpoints
- `Controller Action Not Restricted to POST` findings in the workflow controllers and `manage_usersController`

Those are separate contract-sensitive cleanup batches. They do not fit the "minimal disruption near end of development" goal as cleanly as the Phase 4 trace/justification work.

## Minimal-Risk Plan

1. Pull Fortify traces for the helper/wrapper/cookie/privacy survivors.
2. Add trace-backed justifications for:
   - `CouchDbHttpClient`
   - `SessionDAL`
   - `SteveAPI_Instance`
   - `ContainedPathHelper`
   - cookie-scope rows in `AccountController`, `AccountController.OIDC`, and `OfflineCaseController`
   - `manage-case-folders/index.js`
   - `MultiTenantSetupService`
3. If Fortify still refuses to collapse a few path rows after justification, limit any extra code work to very small internal refactors only:
   - remove or narrow `backup_managerController.ReadFile(string s)` so the helper remains the only file-open sink
   - consider a tiny login DTO for `AccountController` if we want to chip away at the remaining binder flags without changing behavior
4. Defer the broader reflected-XSS and binder queue until after release unless a trace shows an actually exploitable path.

## Recommendation

Phase 4 should stay documentation-and-trace-led unless new trace evidence proves one of the helper/wrapper survivors is still genuinely unsafe. `scan-8` shows the code is moving in the right direction, and the safest way to finish this cycle is to justify the scanner carryover cleanly instead of over-modifying stable controller behavior.

