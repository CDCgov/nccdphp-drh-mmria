# Story 36.2 — Verify Rev Poll Generation Guard During Network Outage

**Epic:** 36 — Case Save Queue Reconcile — Idle Network Recovery Fix
**Story ID:** 36.2
**Status:** todo
**Date added:** 2026-08-03
**Depends on:** None (can run in parallel with Story 36.1)
**Source requirements:** FR-36.3

---

## User Story

As a case reviewer,
I want assurance that a brief network outage does not trigger a false "This case was updated" stale-case banner,
So that I am not forced to reload a case that I was the only one editing.

---

## Acceptance Criteria

**AC-1 — Poll failures during network outage produce only a console.warn, no banner**

Given a case is in edit mode and `_rev` polling is active
When the network drops and one or more poll requests return `TypeError: Failed to fetch` (ERR_NETWORK_CHANGED, ERR_CONNECTION_RESET, ERR_NAME_NOT_RESOLVED)
Then only `console.warn('[CaseRevPoll] poll failed:', err)` is logged
And the stale-case banner (`showStaleCaseBanner`) is NOT shown
And `g_is_case_stale` is NOT set to `true`

**AC-2 — Late-arriving poll response from before a successful save is discarded**

Given `_rev` polling is active with generation N
And an autosave succeeds, calling `mmria_sync_case_rev_polling()` (which calls `stopCaseRevPolling()` → increments generation to N+1 → calls `startCaseRevPolling()` with new `_rev`)
When a poll response from the old session (generation N) resolves late — after the restart
Then the response is discarded by the generation check (`_caseRevPollGeneration !== myGeneration`)
And no stale-case banner is shown even though the old response may carry the previous `_rev` value

**AC-3 — Generation guard is confirmed to be intentional (code comment)**

Given the developer reads `startCaseRevPolling` in `case-rev-check.js`
When the generation assignment `var myGeneration = _caseRevPollGeneration` is found at the top of `startCaseRevPolling`
And the discard check `if (_caseRevPollGeneration !== myGeneration) return` is found inside the `.then()` callback
Then a clarifying comment is added near the discard check explaining its purpose:
```javascript
// Discard if this response belongs to a superseded polling session.
// e.g. an in-flight response from before a successful autosave restarted
// polling with the new _rev. Without this guard, the stale response would
// incorrectly trigger the stale-case banner.
```
(If an equivalent comment already exists, confirm it is adequate — no duplicate is needed.)

**AC-4 — Threshold-agnostic: behaviour holds at any inactivity configuration**

Given the site configuration has any value for `case_edit_inactivity_warning_minutes_before_lock` and `case_edit_inactivity_lock_minutes`
When a network outage occurs during the idle period
Then AC-1 and AC-2 hold regardless of when the inactivity save fires relative to the poll

---

## Background

During the network disruption captured in the screenshot (`fl-mmria.apps.ecpaas-dev.cdc.gov/Case`), the browser console showed:

```
[CaseRevPoll] poll failed: TypeError: Failed to fetch      case-rev-check.js:205
```

These poll failures are **already handled correctly** by the `catch` block in `startCaseRevPolling`. The purpose of this story is to **confirm and document** that the generation guard also covers the late-arriving response scenario, and to add a clarifying comment so future developers do not remove it thinking it is unnecessary.

If a gap is found during the code read (i.e., the discard check is missing or wrong), fix it.

---

## Dev Notes — Implementation

### File to read and potentially modify

```
source-code/mmria/mmria-server/wwwroot/scripts/case/case-rev-check.js
```

### Step 1 — Read the current implementation

Locate `startCaseRevPolling`. Confirm the structure is:

```javascript
function startCaseRevPolling(caseId, loadedRev) {
    if (!caseId || !loadedRev) return;
    stopCaseRevPolling(); // increments _caseRevPollGeneration

    var myGeneration = _caseRevPollGeneration; // <-- generation captured HERE

    function poll() {
        fetch('/api/case/' + encodeURIComponent(caseId) + '/rev', { credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) return null;
                return response.json();
            })
            .then(function (data) {
                if (_caseRevPollGeneration !== myGeneration) return; // <-- guard HERE
                if (!data) return;
                if (data._rev && data._rev !== loadedRev) {
                    // ...
                    showStaleCaseBanner();
                }
            })
            .catch(function (err) {
                console.warn('[CaseRevPoll] poll failed:', err);
            });
    }

    _caseRevPollInterval = setInterval(poll, _caseRevPollIntervalMs);
}
```

### Step 2 — Verify the discard check covers the late-response scenario

The guard `if (_caseRevPollGeneration !== myGeneration) return` is inside the `.then()` callback, which means it evaluates **when the fetch resolves**, not when it is initiated. This correctly discards any response that resolves after `stopCaseRevPolling()` incremented the generation.

Confirm this is the case by tracing the execution:

1. Poll starts in generation N → `myGeneration = N`
2. Autosave succeeds → `mmria_sync_case_rev_polling()` → `stopCaseRevPolling()` → `_caseRevPollGeneration = N+1` → `startCaseRevPolling(caseId, newRev)` → new session with `myGeneration = N+1`
3. Old poll's fetch resolves → enters `.then()` → `_caseRevPollGeneration (N+1) !== myGeneration (N)` → returns immediately → **no banner**

If the guard is present and correct, proceed to Step 3.

### Step 3 — Add or confirm the clarifying comment

Find the line `if (_caseRevPollGeneration !== myGeneration) return;` inside the `.then()` callback.

If the existing comment is absent or insufficient, replace the line with:

```javascript
// Discard if this response belongs to a superseded polling session.
// Guards against a late-arriving response from before mmria_sync_case_rev_polling()
// restarted polling with a new _rev after a successful autosave. Without this check,
// the stale response could incorrectly trigger the stale-case banner.
if (_caseRevPollGeneration !== myGeneration) return;
```

### Step 4 — Confirm mmria_sync_case_rev_polling calls stopCaseRevPolling

Search `index.js` for `mmria_sync_case_rev_polling` and confirm it calls `stopCaseRevPolling()` (which increments the generation) before calling `startCaseRevPolling()` with the new rev. This is the mechanism that makes the guard effective after a successful save.

```powershell
Select-String -Path "c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server\wwwroot\scripts\case\index.js" `
    -Pattern "mmria_sync_case_rev_polling" | Select-Object LineNumber, Line
```

### Step 5 — If a gap is found

If the discard check is missing, misplaced (e.g., in the initiating code rather than the resolving callback), or the generation is not incremented by `stopCaseRevPolling`, fix accordingly. The fix must ensure late-arriving `.then()` callbacks from superseded sessions are always no-ops.

---

## Files Changed

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/wwwroot/scripts/case/case-rev-check.js` | Add or confirm clarifying comment on the generation-guard discard check; fix any gap found |

---

## Story Sequencing

| Dependency | Risk |
|---|---|
| None — read/verify only unless a gap is found; parallel with Story 36.1 | Low |
