# Security Scan Remediation Tracker

- Status: Active
- Scope: Working tracker for the Fortify remediation effort driven by `docs/ai/logs/mmria-security-scan.csv`.
- When to use: Update this file before starting a remediation batch, after verification, and after every fresh scan export.
- Last verified: 2026-03-29
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Security Scan Sensitive Data Heap Guidance](./security_scan_sensitive_data_heap_guidance.md)

This document is the source of truth for the current security scan cleanup. It tracks what is in scope, how findings are grouped into batches, what changed in code, and what still needs either a rescan or a formal justification.

## Scan Source

- Source file: [`docs/ai/logs/mmria-security-scan.csv`](./logs/mmria-security-scan.csv)
- Scan date in workspace: `2026-03-29`
- Explicit exclusions:
  - rows tagged `False Positive`
  - any finding under `mmria-server.tests`

## Current Summary

- Included findings after exclusions: `61`
- Critical: `31`
- High: `27`
- Medium: `3`
- Distinct category/file/line groups: `34`

## Batch Table

| Batch | Status | Raw Rows | Distinct Groups | Primary Focus | Notes |
| --- | --- | ---: | ---: | --- | --- |
| 1 | `completed` | 7 | 2 | Scope cleanup and config secrets | Test-only IJE generator removed from production build; tracked dev cert password removed from checked-in HTTPS settings. |
| 2 | `implemented, pending rescan` | 12 | 11 | Frontend DOM/eval/cookie hardening | Flagged `innerHTML` flows moved to sanitized DOM insertion, confirm callbacks switched to direct closures, duplicate-tab cookies removed. |
| 3 | `implemented, pending rescan` | 11 | 10 | HTTP boundary hardening | STEVE URI/path handling hardened and flagged CouchDB header flows moved to typed request options. |
| 4 | `implemented, pending rescan` | 24 | 4 | Auth and credential handling | Session/basic-auth handling consolidated and sensitive payload construction reduced. |
| 5 | `pending rescan` | 7 | 7 | Heap-inspection re-triage | Revisit only after a fresh scan export. |

## Batch Checklists

### Batch 1: Scope Cleanup and Config Secrets

- [x] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/Testing/IJEGeneration/Generators/TestIJEFileGenerator.cs | line 1508 | rows 6`
- [x] `High | Password Management: Hardcoded Password | source-code/mmria/mmria-server/appsettings.Development.https.json | line 17 | rows 1`
- Working notes:
  - `Testing/IJEGeneration/**` is only referenced from `mmria-server.tests`, so it was excluded from the production `mmria.common` build and compiled explicitly into the test project.
  - `appsettings.Development.https.json` no longer carries the dev certificate password. Local development can continue to supply the same value from user secrets or `appsettings.local.json`.

### Batch 2: Frontend Injection Cleanup

- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/committee-member/index.js | line 287 | rows 1`
- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/committee-member/index.js | line 330 | rows 1`
- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/committee-member/index.js | line 366 | rows 1`
- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/committee-member/index.js | line 409 | rows 1`
- [x] `Medium | Header Manipulation: Cookies | source-code/mmria/mmria-server/wwwroot/scripts/duplicate/Duplicate.js | line 34 | rows 2`
- [x] `High | Dynamic Code Evaluation: Code Injection | source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/list.js | line 2034 | rows 1`
- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/manage-case-folders/index.js | line 399 | rows 1`
- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/manage-case-folders/jurisdiction_renderer.js | line 216 | rows 1`
- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/mmria.js | line 1988 | rows 1`
- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/mmria.js | line 2072 | rows 1`
- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/mmria.js | line 2157 | rows 1`
- Working notes:
  - Shared DOM helpers now sanitize generated HTML before insertion and can accept direct `Node` content where needed.
  - The flagged committee-member and manage-case-folders render paths now go through sanitized DOM insertion instead of raw `innerHTML`.
  - The flagged confirm-dialog callback sites in `editor/page_renderer/list.js` now use direct closures instead of `new Function`.
  - Duplicate-tab tracking no longer uses cookies; it now uses `sessionStorage` and `localStorage`.

### Batch 3: HTTP Boundary Hardening

- [x] `High | Server-Side Request Forgery | nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs | line 57 | rows 1`
- [x] `High | Header Manipulation | nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs | line 79 | rows 2`
- [x] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs | line 327 | rows 1`
- [x] `High | Header Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 69 | rows 1`
- [x] `High | Server-Side Request Forgery | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 205 | rows 1`
- [x] `High | Header Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 206 | rows 1`
- [x] `High | Header Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 217 | rows 1`
- [x] `Medium | Path Manipulation: Base Path Overwriting | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 233 | rows 1`
- [x] `High | Server-Side Request Forgery | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 248 | rows 1`
- [x] `Critical | Path Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 264 | rows 1`
- Working notes:
  - `CouchDbHttpClient` now validates URIs centrally, applies auth/session/`If-Match`/service-key values through typed request options, and sanitizes remaining safe headers.
  - STEVE integration now normalizes the base URI, uses typed bearer auth, escapes mailbox/message IDs, and constrains download/zip paths to the intended directory tree.
  - Remaining `customHeaders` handling is retained only as a compatibility shim inside `CouchDbHttpClient`; the flagged internal callers were moved to typed request options.

### Batch 4: Auth and Credential Handling

- [x] `Critical | Privacy Violation | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs | line 156 | rows 7`
- [x] `Critical | Privacy Violation | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs | line 165 | rows 7`
- [x] `Critical | Privacy Violation | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs | line 166 | rows 7`
- [x] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs | line 220 | rows 3`
- Working notes:
  - Session-auth form payload construction now writes directly into a single byte buffer and zeros sensitive buffers after use.
  - Shared basic-auth creation now routes through `CouchDbHttpClient.CreateBasicAuthHeaderValue(...)` instead of ad hoc `username:password` base64 assembly.
  - Service-key and `If-Match` flows touched during this batch were also moved onto typed request options to reduce header manipulation drift.

### Batch 5: Residual Heap-Inspection Re-triage

- [ ] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs | line 1173 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Helper/MMRIAServicesHelper.cs | line 169 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs | line 76 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs | line 82 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | source-code/mmria/mmria-server/Controllers/AccountController.cs | line 443 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | source-code/mmria/mmria-server/Controllers/loggerController.cs | line 94 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | source-code/mmria/mmria-server/util/c_document_sync_all.cs | line 1049 | rows 1`

## Rescan Results

| Date | Scope | Result | Notes |
| --- | --- | --- | --- |
| 2026-03-29 | Baseline plan/import | `61` included findings after exclusions | Starting point captured from the current CSV export. |
| 2026-03-29 | Code implementation verification | `dotnet build` passed for `mmria-server`; `mmria-server.tests` build passed with `--no-restore -m:1` | The default parallel test-project restore path intermittently failed without compiler errors, but single-node/no-restore verification succeeded after restore was warm. Fresh Fortify rescan still pending. |

## Notes and Justification Queue

- Batch 5 should not be actioned from the baseline CSV alone. Several flagged sinks point at revision IDs, session-display strings, or redirect targets rather than obvious sensitive values.
- Batch 2 through Batch 4 are implemented in code and build-verified, but they still need a fresh Fortify export before they should be considered closed.
- If a finding survives after a fresh rescan, capture the Fortify trace, decide whether the issue is real, then either implement the narrow fix or record the approved justification here.
