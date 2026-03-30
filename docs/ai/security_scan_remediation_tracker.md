# Security Scan Remediation Tracker

- Status: Active
- Scope: Working tracker for the Fortify remediation effort driven by `docs/ai/logs/mmria-security-scan-4.csv`.
- When to use: Update this file before starting a remediation batch, after verification, and after every fresh scan export.
- Last verified: 2026-03-29
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Security Scan Sensitive Data Heap Guidance](./security_scan_sensitive_data_heap_guidance.md), [Fortify Scan Scope Exclusions](./fortify_scan_scope_exclusions.txt)

This document is the source of truth for the current Fortify cleanup. It tracks the latest authoritative export, the unchanged batch-level results in `scan-4`, the externally blocked IJE exclusion, and the remaining trace-review queue.

## Scan Source

- Source file: [`docs/ai/logs/mmria-security-scan-4.csv`](./logs/mmria-security-scan-4.csv)
- Scan date in workspace: `2026-03-29`
- Explicit exclusions:
  - rows tagged `False Positive`
  - any finding under `mmria-server.tests`
  - all `Low` findings
- Future scan handling:
  - save the next rescan as `docs/ai/logs/mmria-security-scan-5.csv`
  - do not append new exports into an existing CSV

## Current Summary

- Included findings after exclusions: `16`
- Critical: `1`
- High: `15`
- Medium: `0`
- Distinct category/file/line groups: `11`
- `scan-4` is unchanged from `scan-3` at the batch level. No category/file groups were added or removed.

## Scan-4 Delta Compared To Scan-3

- No new category/file groups appeared in `mmria-security-scan-4.csv`.
- No category/file groups were resolved relative to `mmria-security-scan-3.csv`.
- The only movement is line relocation for the same two STEVE findings:
  - `Critical | Path Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 264 -> line 468`
  - `High | Header Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 362 -> line 445`
- All other in-scope groups are unchanged from `scan-3`.

## Batch Table

| Batch | Status | Raw Rows | Primary Focus | Notes |
| --- | --- | ---: | --- | --- |
| 0 | `completed` | 16 | Tracker rebaseline | Tracker now points at `mmria-security-scan-4.csv`; future rescans should be saved as `scan-5`, `scan-6`, and so on. |
| 1 | `externally blocked` | 6 | Scan scope exclusion for test-only IJE generation | The exclusion path exists in `docs/ai/fortify_scan_scope_exclusions.txt`, but it cannot be applied today because Fortify permission is unavailable. |
| 2 | `closed, validated` | 0 | Remaining frontend DOM findings | The DOM results stayed closed in `scan-4`; reopen only if a future scan regresses. |
| 3 | `trace review required` | 2 | Remaining STEVE header/path findings | `scan-4` followed the same STEVE flows into wrapper sinks at lines `445` and `468`. Do not continue blind code-shaping without trace evidence. |
| 4 | `trace review required` | 8 | Residual heap-inspection findings | Eight distinct groups remain across seven non-IJE code areas. Trace access is available outside the repo, but no trace files are checked in. |

## Active Batch Checklists

### Batch 1: Scope Exclusion

- [ ] `High | Privacy Violation: Heap Inspection | nccdphp-drh-mmria-common/mmria.common/Testing/IJEGeneration/Generators/TestIJEFileGenerator.cs | line 1508 | rows 6`
- Working notes:
  - `Testing/IJEGeneration/**` remains compiled only into `mmria-server.tests`.
  - The agreed Fortify exclusion path is stored in [`docs/ai/fortify_scan_scope_exclusions.txt`](./fortify_scan_scope_exclusions.txt).
  - `scan-4` still contains this finding because the exclusion could not be applied by a privileged Fortify operator.
  - This batch closes only when a future Fortify run confirms these rows are gone from the active results.

### Batch 2: Frontend DOM Findings

- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/manage-case-folders/index.js | line 399 | rows 1`
- [x] `Critical | Cross-Site Scripting: DOM | source-code/mmria/mmria-server/wwwroot/scripts/mmria.js | line 22 | rows 3`
- Working notes:
  - `scan-3` validated the prior DOM cleanup and `scan-4` preserved that result.
  - These findings are closed unless a later scan reintroduces them.

### Batch 3: Remaining STEVE Transport Findings

- [ ] `Critical | Path Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 468 | rows 1`
- [ ] `High | Header Manipulation | source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs | line 445 | rows 1`
- Working notes:
  - `scan-4` marks both findings as `NEW`, but there is no category/file regression; Fortify is following the same flows into the wrapper sinks introduced by the prior hardening pass.
  - Default next action is trace review, not another code-shaping pass.
  - If the trace only shows validated wrapper values reaching framework sinks, record a trace-backed justification.
  - Only make more code changes if the trace shows an actual untrusted value bypassing validation before header assignment or file-open.

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
  - These remain trace-review candidates rather than blind code changes.
  - Trace access is available, but trace files are not checked into the repo. Pull the traces before changing code.
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
| 2026-03-29 | Follow-up validation checkpoint | `16` included findings after exclusions | `scan-3` validated the DOM fixes and most of the STEVE hardening, but did not apply the IJE exclusion manifest. |
| 2026-03-29 | Latest authoritative export | `16` included findings after exclusions | `scan-4` kept the totals flat, relocated the two STEVE findings to wrapper sinks, and still did not apply the IJE exclusion. |

## Verification Notes

- No repo code changes are required from the `scan-4` review alone.
- Build verification from the prior remediation pass remains the latest code verification because this rebaseline only updates the docs and work queue.
- The next verification step should be:
  - pull Fortify traces for the two STEVE findings and the eight non-IJE heap groups
  - have a privileged Fortify operator apply `docs/ai/fortify_scan_scope_exclusions.txt`
  - record the next CSV as `docs/ai/logs/mmria-security-scan-5.csv`
  - update this tracker with the `scan-5` counts, survivor list, and final disposition for each remaining item
