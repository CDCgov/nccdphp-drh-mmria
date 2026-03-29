# Security Scan Remediation Tracker

- Status: Active
- Scope: Working tracker for the Fortify remediation effort driven by `docs/ai/logs/mmria-security-scan-2.csv`.
- When to use: Update this file before starting a remediation batch, after verification, and after every fresh scan export.
- Last verified: 2026-03-29
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Security Scan Sensitive Data Heap Guidance](./security_scan_sensitive_data_heap_guidance.md), [Fortify Scan Scope Exclusions](./fortify_scan_scope_exclusions.txt)

This document is the source of truth for the current Fortify cleanup. It now tracks the latest scan export, the repo changes made after that export, and the remaining trace-review queue.

## Scan Source

- Source file: [`docs/ai/logs/mmria-security-scan-2.csv`](./logs/mmria-security-scan-2.csv)
- Scan date in workspace: `2026-03-29`
- Explicit exclusions:
  - rows tagged `False Positive`
  - any finding under `mmria-server.tests`
- Future scan handling:
  - save each rescan as a new file such as `docs/ai/logs/mmria-security-scan-3.csv`
  - do not append new exports into an existing CSV

## Current Summary

- Included findings after exclusions: `23`
- Critical: `5`
- High: `17`
- Medium: `1`
- Distinct category/file/line groups: `16`
- No truly new category/file groups appeared in `mmria-security-scan-2.csv`; the remaining work is residual cleanup from the prior remediation pass.

## Resolved Since Prior Scan

These category/file groups were present in the prior baseline and are no longer present in `mmria-security-scan-2.csv`:

- `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/committee-member/index.js`
- `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/manage-case-folders/jurisdiction_renderer.js`
- `Critical | Privacy Violation | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs`
- `High | Dynamic Code Evaluation: Code Injection | source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/list.js`
- `High | Header Manipulation | nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs`
- `High | Password Management: Hardcoded Password | source-code/mmria/mmria-server/appsettings.Development.https.json`
- `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs`
- `High | Server-Side Request Forgery | nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs`
- `High | Server-Side Request Forgery | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs`
- `Medium | Header Manipulation: Cookies | source-code/mmria/mmria-server/wwwroot/scripts/duplicate/Duplicate.js`

## Batch Table

| Batch | Status | Raw Rows | Primary Focus | Notes |
| --- | --- | ---: | --- | --- |
| 0 | `completed` | 23 | Tracker rebaseline | Tracker now points at `mmria-security-scan-2.csv` and future rescans are expected as new files. |
| 1 | `implemented, pending Fortify rescan` | 6 | Scan scope exclusion for test-only IJE generation | Repo-tracked exclusion manifest added at `docs/ai/fortify_scan_scope_exclusions.txt`. |
| 2 | `implemented, pending Fortify rescan` | 4 | Remaining frontend DOM findings | Remaining tainted dialog text is now written through DOM properties instead of parsed HTML; top-level case-folder insert now uses native DOM insertion. |
| 3 | `implemented, pending Fortify rescan` | 5 | Remaining STEVE header/path findings | Bearer token validation and per-request auth helpers added; download directory, file, and zip paths now use explicit contained-path helpers. |
| 4 | `pending trace review` | 8 | Residual heap-inspection findings | These should be actioned only with Fortify traces in hand. |

## Active Batch Checklists

### Batch 1: Scope Exclusion

- [x] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/Testing/IJEGeneration/Generators/TestIJEFileGenerator.cs | line 1508 | rows 6`
- Working notes:
  - `Testing/IJEGeneration/**` remains compiled only into `mmria-server.tests`.
  - The agreed Fortify exclusion path is stored in [`docs/ai/fortify_scan_scope_exclusions.txt`](./fortify_scan_scope_exclusions.txt).
  - This batch is not closed until a fresh Fortify run confirms these rows are gone from the active results.

### Batch 2: Remaining Frontend DOM Findings

- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/manage-case-folders/index.js | line 399 | rows 1`
- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/mmria.js | line 22 | rows 3`
- Working notes:
  - The `manage-case-folders` add-child flow no longer uses the flagged jQuery `.before(...)` sink for the top-level insert path.
  - The remaining tainted dialog data in `mmria.js` now populates `textarea.value` and summary `textContent` after the dialog shell renders, so server response text no longer flows through `DOMParser.parseFromString(...)`.
  - The shared sanitizer helper still exists for legacy generated markup; this batch specifically removes the currently flagged tainted flows into that helper.

### Batch 3: Remaining STEVE Transport Findings

- [x] `High | Header Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 75 | rows 1`
- [x] `High | Header Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 215 | rows 1`
- [x] `High | Header Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 226 | rows 1`
- [x] `Critical | Path Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 272 | rows 1`
- [x] `Medium | Path Manipulation: Base Path Overwriting | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 394 | rows 1`
- Working notes:
  - STEVE controllers now build a fresh internal `DownloadRequest` instead of mutating the inbound request object before sending it to the actor.
  - `SteveAPI_Instance` now validates bearer token characters and length before any `Authorization` header assignment and routes GET/PATCH mailbox calls through a single request-builder helper.
  - The actor now distinguishes trusted root directories from child directory/file names and resolves download folders, downloaded files, log files, and zip targets through explicit contained-path helpers.

### Batch 4: Residual Heap-Inspection Trace Review

- [ ] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs | line 468 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs | line 1173 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Helper/MMRIAServicesHelper.cs | line 169 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs | line 73 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs | line 79 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | source-code/mmria/mmria-server/Controllers/AccountController.cs | line 422 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | source-code/mmria/mmria-server/Controllers/loggerController.cs | line 90 | rows 1`
- [ ] `High | Privacy Violation: Heap Inspection | source-code/mmria/mmria-server/util/c_document_sync_all.cs | line 1049 | rows 1`
- Working notes:
  - Local code review suggests these are likely trace-review candidates rather than first-pass code fixes.
  - Current likely classifications:
    - `CouchDbHttpClient`: host normalization and SSRF guard state, likely scanner taint carryover.
    - `CaseManager` and `OfflineCaseManager`: case/tab/session conflict identifiers used for lock-state decisions.
    - `MMRIAServicesHelper` and `c_document_sync_all`: document `_id` and `_rev` cleanup/bookkeeping.
    - `AccountController`: redirect target handling around offline key entry.
    - `loggerController`: abbreviated session display strings for troubleshooting UI.
  - Do not change these blindly. Each survivor must end as either code-fixed or trace-backed justified after the next scan.

## Rescan Results

| Date | Scope | Result | Notes |
| --- | --- | --- | --- |
| 2026-03-29 | Prior baseline | `61` included findings after exclusions | Original remediation baseline from `mmria-security-scan.csv`. |
| 2026-03-29 | Latest authoritative export | `23` included findings after exclusions | Rebaseline from `mmria-security-scan-2.csv`. |
| 2026-03-29 | Post-rebaseline implementation | `pending Fortify rescan` | Batch 1 exclusion manifest, Batch 2 frontend fixes, and Batch 3 STEVE fixes are in repo but not yet validated by a fresh scan. |

## Verification Notes

- Build verification for the previous remediation pass remains valid, but a fresh build was not yet rerun after the latest follow-up changes captured in this tracker revision.
- The next verification step should be:
  - run `dotnet build` for `mmria-server`
  - rerun the Fortify export with `docs/ai/fortify_scan_scope_exclusions.txt` applied
  - record the next CSV as `docs/ai/logs/mmria-security-scan-3.csv`
  - update this tracker with the post-rescan counts and any surviving findings
