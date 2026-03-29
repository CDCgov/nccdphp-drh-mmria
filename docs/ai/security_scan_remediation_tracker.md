# Security Scan Remediation Tracker

- Status: Active
- Scope: Working tracker for the Fortify remediation effort driven by `docs/ai/logs/mmria-security-scan-3.csv`.
- When to use: Update this file before starting a remediation batch, after verification, and after every fresh scan export.
- Last verified: 2026-03-29
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Security Scan Sensitive Data Heap Guidance](./security_scan_sensitive_data_heap_guidance.md), [Fortify Scan Scope Exclusions](./fortify_scan_scope_exclusions.txt)

This document is the source of truth for the current Fortify cleanup. It tracks the latest authoritative export, the code paths already validated by that export, the follow-up scanner-friendly STEVE hardening added after `scan-3`, and the remaining trace-review queue.

## Scan Source

- Source file: [`docs/ai/logs/mmria-security-scan-3.csv`](./logs/mmria-security-scan-3.csv)
- Scan date in workspace: `2026-03-29`
- Explicit exclusions:
  - rows tagged `False Positive`
  - any finding under `mmria-server.tests`
  - all `Low` findings
- Future scan handling:
  - save the next rescan as `docs/ai/logs/mmria-security-scan-4.csv`
  - do not append new exports into an existing CSV

## Current Summary

- Included findings after exclusions: `16`
- Critical: `1`
- High: `15`
- Medium: `0`
- Distinct category/file/line groups: `11`
- No new category/file groups appeared in `mmria-security-scan-3.csv`; the only relocated survivors are the remaining STEVE findings inside `SteveAPI_Instance.cs`.

## Resolved In Scan-3 Compared To Scan-2

These category/file/line groups were present in `mmria-security-scan-2.csv` and are no longer present in `mmria-security-scan-3.csv`:

- `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/manage-case-folders/index.js | line 399`
- `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/mmria.js | line 22`
- `Critical | Path Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 272`
- `High | Header Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 75`
- `High | Header Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 215`
- `High | Header Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 226`
- `Medium | Path Manipulation: Base Path Overwriting | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 394`

Previously closed groups from `mmria-security-scan.csv` and `mmria-security-scan-2.csv` remain closed unless they reappear in a later export.

## Batch Table

| Batch | Status | Raw Rows | Primary Focus | Notes |
| --- | --- | ---: | --- | --- |
| 0 | `completed` | 16 | Tracker rebaseline | Tracker now points at `mmria-security-scan-3.csv`; future rescans should be saved as `scan-4`, `scan-5`, and so on. |
| 1 | `implemented, pending exclusion-enabled rescan` | 6 | Scan scope exclusion for test-only IJE generation | `scan-3` still includes these rows, which means the Fortify run did not consume `docs/ai/fortify_scan_scope_exclusions.txt`. |
| 2 | `validated in scan-3` | 0 | Remaining frontend DOM findings | The `mmria.js` and `manage-case-folders` DOM findings dropped out of the active results. |
| 3 | `partially validated, follow-up implemented` | 2 | Remaining STEVE header/path findings | `scan-3` cleared five prior STEVE groups. Two surviving hits moved to helper lines, so the follow-up refactor now wraps validated auth and file targets in dedicated types pending `scan-4`. |
| 4 | `pending trace review` | 8 | Residual heap-inspection findings | Eight distinct groups remain across seven non-IJE code areas. No local Fortify trace artifacts are present in the workspace. |

## Active Batch Checklists

### Batch 1: Scope Exclusion

- [ ] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/Testing/IJEGeneration/Generators/TestIJEFileGenerator.cs | line 1508 | rows 6`
- Working notes:
  - `Testing/IJEGeneration/**` remains compiled only into `mmria-server.tests`.
  - The agreed Fortify exclusion path is stored in [`docs/ai/fortify_scan_scope_exclusions.txt`](./fortify_scan_scope_exclusions.txt).
  - `scan-3` still contains this finding, so the exclusion manifest was not applied during that run.
  - This batch closes only when a fresh Fortify run confirms these rows are gone from the active results.

### Batch 2: Frontend DOM Findings

- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/manage-case-folders/index.js | line 399 | rows 1`
- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/mmria.js | line 22 | rows 3`
- Working notes:
  - `scan-3` validates the prior DOM cleanup. These groups no longer appear in the active results.
  - The `manage-case-folders` add-child flow no longer uses the flagged jQuery `.before(...)` sink for the top-level insert path.
  - The remaining tainted dialog data in `mmria.js` now populates `textarea.value` and summary `textContent` after the dialog shell renders, so server response text no longer flows through `DOMParser.parseFromString(...)`.

### Batch 3: Remaining STEVE Transport Findings

- [ ] `Critical | Path Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 264 | rows 1`
- [ ] `High | Header Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 362 | rows 1`
- Working notes:
  - `scan-3` removed the prior STEVE findings at lines `75`, `215`, `226`, `272`, and `394`.
  - The surviving STEVE hits moved to helper-level lines after the first hardening pass rather than introducing a new category/file regression.
  - The current follow-up refactor wraps bearer-token validation plus header construction in `SteveAuthorizationHeader` and wraps validated file targets plus file-open behavior in `SteveContainedFile`.
  - This batch closes only when `scan-4` removes both remaining STEVE groups or when Fortify trace review shows they are residual false-positive dataflow.

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
  - These are still treated as trace-review candidates rather than blind code changes.
  - There are no Fortify trace exports, screenshots, or bug-detail artifacts in the workspace for these survivors.
  - Current likely classifications:
    - `CouchDbHttpClient`: host normalization and SSRF guard state, likely scanner taint carryover.
    - `CaseManager` and `OfflineCaseManager`: case/tab/session conflict identifiers used for lock-state decisions.
    - `MMRIAServicesHelper` and `c_document_sync_all`: document `_id` and `_rev` cleanup and bookkeeping.
    - `AccountController`: redirect target handling around offline key entry.
    - `loggerController`: abbreviated session display strings for troubleshooting UI.
  - Do not change these blindly. Each survivor must end as either code-fixed or trace-backed justified after the next scan-and-trace pass.

## Rescan Results

| Date | Scope | Result | Notes |
| --- | --- | --- | --- |
| 2026-03-29 | Prior baseline | `61` included findings after exclusions | Original remediation baseline from `mmria-security-scan.csv`. |
| 2026-03-29 | Rebaseline checkpoint | `23` included findings after exclusions | Rebaseline from `mmria-security-scan-2.csv`. |
| 2026-03-29 | Latest authoritative export | `16` included findings after exclusions | `scan-3` validated the DOM fixes and most of the STEVE hardening, but did not apply the IJE exclusion manifest. |
| 2026-03-29 | Post-scan-3 follow-up implementation | `pending Fortify rescan` | Scanner-friendly STEVE follow-up is now in repo and should be validated by `mmria-security-scan-4.csv`. |

## Verification Notes

- Local repo review found no Fortify trace artifacts for the remaining STEVE or heap-inspection survivors.
- The next verification step should be:
  - run `dotnet build` for `mmria-server`
  - rerun the Fortify export with `docs/ai/fortify_scan_scope_exclusions.txt` applied
  - capture Fortify traces or screenshots for any surviving STEVE or heap-inspection findings
  - record the next CSV as `docs/ai/logs/mmria-security-scan-4.csv`
  - update this tracker with the `scan-4` counts, survivor list, and final disposition for each remaining item
