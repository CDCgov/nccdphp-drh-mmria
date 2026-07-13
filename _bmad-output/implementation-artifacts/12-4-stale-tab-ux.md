# Story 12.4 — Stale Tab UX

**Epic:** 12 — Data Migration Tool Modernization
**Story ID:** 12.4
**Status:** review — defect 12.4-D1, follow-up requirements 12.4-R1 / 12.4-R2, and edit-mode polling scope update implemented
**Date added:** 2026-07-08
**Depends on:** Story 12.3 (Case Rev Endpoint — needed for `_rev` polling)
**Source requirements:** FR-19.1–FR-19.4

---

## User Story

As a case coordinator with a stale browser tab,
I want to be proactively notified when a case has been updated since I opened it, and to receive a clear recovery message if I attempt to save stale data,
So that I never lose work silently or see a confusing technical error on a medical case record.

---

## Acceptance Criteria

**AC-1 — 409 on case save triggers a clear, non-dismissable recovery modal**
Given a user clicks Save on a case
When the server returns HTTP 409
Then a non-dismissable modal appears with the text:
`"This case was updated elsewhere. Reload to get the latest version before saving."`
And the modal contains a single **[Reload Case]** button that invokes the case reload helper
And the helper reloads the open case in-place when the case page hook is available, falling back to `window.location.reload()`
And the generic save error handler does NOT also fire for this 409

**AC-2 — `_rev` polling detects staleness while the current tab is editing**
Given a case has been loaded for editing
And the current tab owns the active edit checkout
When 45 seconds have elapsed since the last poll
Then the client calls `GET /api/case/{id}/rev`
And if the returned `_rev` differs from the `_rev` captured at case load time
Then a stale-case modal appears with the text:
`"This case has been updated. Reload to see the latest version."`
And the modal contains a single **[Reload]** button and no dismiss action
And the **[Reload]** button invokes the case reload helper
And autosave is paused until the case reloads
And the poll continues while the modal is open without creating duplicate modal instances

**AC-3 — Poll interval remains lightweight and fixed**
Given `_rev` polling is active
When the poll is scheduled
Then the client polls `GET /api/case/{id}/rev` every 45 seconds
And the client does not depend on an `X-Offline-Date` header from the rev endpoint
And offline-window detection remains owned by `/api/system-offline/status`

**AC-4 — Polling scope is active edit-lock ownership**
Given the current tab owns the active edit checkout for the case
And the loaded case has `_id` and `_rev`
When the case is loaded, checkout is acquired, or an edit-mode save succeeds with a new `_rev`
Then `_rev` polling is active and uses the latest known `_rev` as its comparison value

Given the current tab does NOT own the active edit checkout for the case
When the case loads or the user is viewing the case in read-only mode
Then no polling interval is started
And no `GET /api/case/{id}/rev` calls are made

Given `_rev` polling is active
When the user leaves edit mode through Save & Close, lock-release navigation, checkout conflict, offline-processing mode, auth failure, load failure, or page unload
Then any active `_rev` polling interval is stopped
And the read-only tab does not show proactive `_rev` warnings for changes made by another user

**AC-5 — Poll stops on navigation**
Given the `_rev` poll is active
When the user navigates away from the case (page unload)
Then the polling interval is cleared
And no further `GET /api/case/{id}/rev` calls are made

**AC-6 — Section 508 compliance**
Given the stale-case polling modal (AC-2) or 409 modal (AC-1) appears
When a screen reader user is on the page
Then the notification is announced to the screen reader via alert-dialog semantics
And all buttons are keyboard-accessible

---

## Dev Notes — Implementation

### Overview

Two changes:
1. **Reactive (AC-1):** Intercept HTTP 409 in the existing case save error handler in client JS.
2. **Proactive (AC-2–AC-5):** Add `_rev` polling to `system-offline-check.js` or a co-located JS module.

---

### Part 1 — 409 Intercept (Reactive)

**Finding the case save handler:**

The case is edited in `CaseController.Index` view (`source-code/mmria/mmria-server/Views/Case/Index.cshtml`). The actual save is performed by client-side JavaScript. Search the wwwroot JS for the save handler:

```powershell
Select-String -Path "c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server\wwwroot\**\*.js" `
    -Pattern "PUT|save_case|save.*case|api/case" -Recurse | Select-Object Path, LineNumber, Line | Select-Object -First 20
```

The save likely calls `PUT /api/case/{id}` or routes through `api/caseController.cs`'s PUT action. Find the fetch/XHR call and its error handler.

**Change to make:**

In the error/response handler for the case save fetch/XHR call, add a 409-specific branch **before** the generic error handler:

```javascript
// In the save response handler:
if (response.status === 409) {
    showStaleCaseModal();
    return; // Do NOT fall through to generic error handling
}
// ... existing generic error handling below
```

**`showStaleCaseModal()` function — add to `system-offline-check.js` or the same JS file:**

```javascript
/**
 * Shows the non-dismissable stale case modal.
 * Called when a case save returns HTTP 409 (conflict).
 */
function showStaleCaseModal() {
    var modalId = 'mmria-stale-case-modal';
    var existing = document.getElementById(modalId);

    // Create modal if it doesn't already exist in the DOM
    if (!existing) {
        var html =
            '<div id="mmria-stale-case-modal-backdrop" style="display:none;position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.5);z-index:10000;"></div>' +
            '<div id="' + modalId + '" role="alertdialog" aria-modal="true" aria-labelledby="mmria-stale-case-modal-title" aria-describedby="mmria-stale-case-modal-msg" ' +
            '     style="display:none;position:fixed;top:0;left:0;width:100%;height:100%;align-items:center;justify-content:center;z-index:10001;">' +
            '  <div style="background:#fff;padding:24px;max-width:480px;width:90%;border-radius:4px;">' +
            '    <h2 id="mmria-stale-case-modal-title" style="margin-top:0;">This case was updated</h2>' +
            '    <p id="mmria-stale-case-modal-msg">This case was updated elsewhere. Reload to get the latest version before saving.</p>' +
            '    <button id="mmria-stale-case-reload-btn" style="margin-top:12px;">Reload Case</button>' +
            '  </div>' +
            '</div>';
        document.body.insertAdjacentHTML('beforeend', html);
        document.getElementById('mmria-stale-case-reload-btn').addEventListener('click', function () {
            window.location.reload();
        });
    }

    var backdrop = document.getElementById('mmria-stale-case-modal-backdrop');
    var modal = document.getElementById(modalId);
    if (backdrop) backdrop.style.display = 'block';
    if (modal) {
        modal.style.display = 'flex';
        // Announce to screen readers
        modal.setAttribute('aria-hidden', 'false');
        var btn = document.getElementById('mmria-stale-case-reload-btn');
        if (btn) setTimeout(function () { btn.focus(); }, 0);
    }
}
```

**Preferred approach:** If the project already has a modal component pattern (Bootstrap `modal()`, or the existing `mmria-offline-modal` pattern in `system-offline-check.js`), match that pattern exactly rather than injecting raw HTML. Study `showOfflineModal()` and `showWarnModal()` in `system-offline-check.js` as the reference — they use pre-existing `<div>` elements in `_Layout.cshtml`/`_LayoutBase.cshtml`. In that case:
1. Add the stale-case modal markup to the shared layout alongside the existing offline modals.
2. `showStaleCaseModal()` just shows it by ID (same pattern as `showOfflineModal()`).

**Check `_LayoutBase.cshtml` or `_Layout.cshtml` for existing modal containers:**
```powershell
Select-String -Path "c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server\Views\Shared\*.cshtml" `
    -Pattern "mmria-offline-modal|mmria-warn-modal" | Select-Object Path, LineNumber, Line
```

---

### Part 2 — `_rev` Polling (Proactive)

**File to modify: `source-code/mmria/mmria-server/wwwroot/js/system-offline-check.js`**

Add the following functions at the bottom of the file (before any closing IIFE if applicable):

```javascript
// ── Case Rev Polling ──────────────────────────────────────────────────────────

var _caseRevPollInterval = null;
var _caseRevPollIntervalMs = 45000; // normal: 45s

/**
 * Starts polling /api/case/{caseId}/rev every _caseRevPollIntervalMs milliseconds.
 * If the returned _rev differs from loadedRev, shows a stale-case modal.
 *
 * @param {string} caseId    - The CouchDB document ID of the open case.
 * @param {string} loadedRev - The _rev captured when the case was loaded.
 */
function startCaseRevPolling(caseId, loadedRev) {
    if (!caseId || !loadedRev) return;
    stopCaseRevPolling();

    function poll() {
        fetch('/api/case/' + encodeURIComponent(caseId) + '/rev', { credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) return; // 404 or other error — do nothing
                return response.json();
            })
            .then(function (data) {
                if (!data) return;
                if (data._rev && data._rev !== loadedRev) {
                    showStaleCaseBanner();
                }
            })
            .catch(function (err) {
                console.warn('[CaseRevPoll] poll failed:', err);
            });
    }

    _caseRevPollInterval = setInterval(poll, _caseRevPollIntervalMs);
}

/**
 * Stops the case rev polling interval.
 */
function stopCaseRevPolling() {
    if (_caseRevPollInterval !== null) {
        clearInterval(_caseRevPollInterval);
        _caseRevPollInterval = null;
    }
}

/**
 * Shows a stale-case modal when _rev polling detects the case has been
 * updated server-side. The legacy function name is retained for callers.
 */
function showStaleCaseBanner() {
    if (document.getElementById('mmria-stale-case-banner')) return; // Already shown

    if (typeof window.mmria_mark_case_stale === 'function') window.mmria_mark_case_stale();

    // Create a Bootstrap-style modal/backdrop with a single Reload button.
    // Reload invokes mmria_do_case_reload(), which reloads the open case
    // in-place when the case page hook is available and falls back to a
    // full page reload otherwise.
}
```

**Wiring `startCaseRevPolling()` from the case edit page:**

The case edit view (`Views/Case/Index.cshtml`) must call `startCaseRevPolling(caseId, loadedRev)` only while the current tab owns the active edit checkout.

In `Views/Case/Index.cshtml` (or the co-located JS), after the case document is fetched and available:

```javascript
// After case data is loaded:
var caseId = /* the case ID from the URL or loaded doc */;
var loadedRev = caseDoc._rev; // _rev is part of every CouchDB document response

// Only start polling if this tab owns the active edit checkout.
// In the current implementation this is centralized through
// mmria_sync_case_rev_polling(), which gates on g_data_is_checked_out.
if (g_data_is_checked_out === true) {
    startCaseRevPolling(caseId, loadedRev);
}

// Stop polling on page unload
window.addEventListener('beforeunload', function () {
    stopCaseRevPolling();
});
```

**Finding the edit-mode gate:**

The case page uses `g_data_is_checked_out` and `is_case_checked_out(g_data)` to determine whether the current tab owns the active edit checkout. Search:
```powershell
Select-String -Path "c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server\Views\Case\Index.cshtml" `
    -Pattern "g_data_is_checked_out|is_case_checked_out|startCaseRevPolling" | Select-Object LineNumber, Line | Select-Object -First 10
```

**Finding where `_rev` is available in the client:**

The case GET at `GET /api/case/{id}` (in `api/caseController.cs`) returns the full `mmria_case` object. The `_rev` property is part of this response. Find where the case is loaded in the page JS:
```powershell
Select-String -Path "c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server\wwwroot\**\*.js" `
    -Pattern "_rev|loadCase|GetCase|api/case" -Recurse | Select-Object Path, LineNumber, Line | Select-Object -First 20
```

Capture `_rev` at the point where the case is loaded and store it for polling.

**Stop polling on save success (optional but good practice):**

After a successful save, the `_rev` of the document changes server-side. The stored `loadedRev` is now stale. Either:
- Update `loadedRev` with the new `_rev` returned in the save response, **and** restart polling with the new rev, OR
- Stop polling after save and restart with the fresh rev

This prevents false-positive stale modals after the user's own save.

---

### Layout changes for modal container

If following the existing modal pattern from `system-offline-check.js` (preferred), add the stale-case modal markup to the shared layout. Find where the offline modals are defined:

```powershell
Select-String -Path "c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server\Views\Shared\*.cshtml" `
    -Pattern "mmria-offline-modal|mmria-warn-modal" | Select-Object Path, LineNumber, Line
```

Add adjacent to the existing offline modal `<div>` blocks:

```html
<!-- Stale Case Modal (non-dismissable, shown on 409 conflict) -->
<div id="mmria-stale-case-modal-backdrop"
     style="display:none;position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.5);z-index:10000;"></div>
<div id="mmria-stale-case-modal"
     role="alertdialog" aria-modal="true"
     aria-labelledby="mmria-stale-case-modal-title"
     aria-describedby="mmria-stale-case-modal-msg"
     style="display:none;position:fixed;top:0;left:0;width:100%;height:100%;align-items:center;justify-content:center;z-index:10001;">
    <div style="background:#fff;padding:24px;max-width:480px;width:90%;border-radius:4px;">
        <h2 id="mmria-stale-case-modal-title" style="margin-top:0;">This case was updated</h2>
        <p id="mmria-stale-case-modal-msg">This case was updated elsewhere. Reload to get the latest version before saving.</p>
        <button id="mmria-stale-case-reload-btn" onclick="window.location.reload()">Reload Case</button>
    </div>
</div>
```

If using the pre-existing modal container approach, simplify `showStaleCaseModal()` to just show/hide by ID (same as `showOfflineModal()`).

---

### Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/wwwroot/js/system-offline-check.js` | Add `startCaseRevPolling()`, `stopCaseRevPolling()`, `showStaleCaseBanner()`, `showStaleCaseModal()` |
| `source-code/mmria/mmria-server/Views/Case/Index.cshtml` | Wire `startCaseRevPolling(caseId, loadedRev)` after case load; wire `stopCaseRevPolling()` on `beforeunload`; intercept 409 in save handler to call `showStaleCaseModal()` |
| Shared layout (`_Layout.cshtml` or `_LayoutBase.cshtml`) | Add stale-case modal `<div>` markup adjacent to existing offline modals (if following pre-existing modal container pattern) |

---

### Playwright Test Guidance (AC-6 from party mode recommendation)

Two E2E tests (add to `e2e/tests/` in `nccdphp-drh-mmria-utilities`):

1. **Stale modal within 50s of remote save:**
   - Open case in tab A
   - In tab B (or via direct API), PUT an updated version of the same case
   - Wait up to 50s in tab A
   - Assert stale modal appears with the single **Reload** button

2. **409 modal on stale save attempt:**
   - Open case in tab A, capture current `_rev`
   - Directly PUT an updated version of the case via API (bypassing tab A's save)
   - In tab A, attempt to save
   - Assert 409 modal appears with "Reload Case" button

---

### Important Notes

- `system-offline-check.js` is the correct home for the polling functions — it is already loaded on authenticated pages and has the pattern for `startOfflineStatusPolling()` from Story 8.4.
- The case edit view is a complex SPA-like page. The JS entry point for case loading may be in a Razor `@section Scripts` block in `Views/Case/Index.cshtml` or in a bundled JS file. Trace carefully before modifying.
- Do not disable the Save button when the stale modal is shown (AC-2) — the 409 intercept (AC-1) is the last-resort gate. Autosave is paused until reload.
- After a successful save, the `_rev` changes. Update the polling reference rev to avoid a false-positive stale modal on the next poll cycle.

---

## Dev Agent Record

_To be completed by dev agent after implementation._

### Completion Notes

Implemented following the existing `mmria-offline-modal` pattern exactly:

- **409 modal** created dynamically via `showStaleCaseModal()` using the Bootstrap `.modal-dialog`/`.modal-content` structure and the same purple header (`background-color:#7b2d8e`). Non-dismissable (no close button), single **Reload Case** button invokes the case reload helper.
- **Proactive stale modal** created dynamically via `showStaleCaseBanner()` using the same Bootstrap modal/backdrop pattern. Contains a single **Reload** button and no dismiss action; autosave is paused until reload.
- **Polling** (`startCaseRevPolling` / `stopCaseRevPolling`) added to `system-offline-check.js` following the `startOfflineStatusPolling` pattern. Poll interval: fixed 45 s; offline-window behavior remains owned by `/api/system-offline/status`.
- **Edit-mode gate**: polling starts only when `g_is_data_analyst_mode == null` and the current tab owns the active checkout lock (`g_data_is_checked_out === true`). Data analyst (`/analyst-case` route sets `g_is_data_analyst_mode = 'da'`) and read-only case views get no polling.
- **409 intercept**: replaced existing `$mmria.save_error_500_dialog_show()` call for `(409) Conflict` with `window.showStaleCaseModal()`. Does not fall through to generic error handler.
- **Poll sync on save**: after successful edit-mode saves, polling is restarted with the updated `_rev` from `case_response.rev`; Save & Close and other lock-release saves stop polling.
- **Explicit non-polling states**: no `_rev` polling occurs for data analyst/read-only routes, write-capable users who are only viewing the case, the read-only state after Save & Close, checkout conflicts, offline-processing mode, auth failure, load failure, or after navigation/page unload.
- **Stop on unload**: `stopCaseRevPolling()` called at the top of `navigation_away()` (the `window.onbeforeunload` handler).

AC-6 (508): both stale modals use `role="alertdialog"`, `aria-modal="true"`, `aria-labelledby`/`aria-describedby`; focus moves to the reload button. All buttons are keyboard-accessible.

Follow-up implementation completed 2026-07-10:

- **12.4-D1**: Confirmed all successful save queue paths, including autosave, restart `_rev` polling with the returned `case_response.rev`; clarified the code comment so the autosave coverage is explicit.
- **12.4-R1**: Moved the system offline checker from `wwwroot/js/system-offline-check.js` to `wwwroot/js/scripts/system-offline-check.js` and updated the shared layout include.
- **12.4-R2**: Extracted case stale-tab polling and recovery UI into `wwwroot/scripts/case/case-rev-check.js`; `system-offline-check.js` now retains only system offline/warn behavior.
- Extracted the prior proactive stale notification as an actual Bootstrap-style modal with a single **Reload** action and no dismiss action; retained the 409 recovery modal as non-dismissable.
- Restored reload button behavior to use `mmria_do_case_reload()`, which reloads the case in-place through `window.mmria_reload_case_data()` when available and falls back to `window.location.reload()`.
- Loaded `case-rev-check.js` before `index.js` on `Views/Case/Index.cshtml` and `Views/abstractorDeidentifiedCase/Index.cshtml`.
- Follow-up scope update completed 2026-07-13: `_rev` polling now syncs to active edit-lock ownership. Save & Close, lock-release navigation, checkout conflicts, offline processing, auth failure, and load failure stop polling; successful checkout or restored checkout after failed release resumes polling.
- 2026-07-13 validation: `node --check` passed for `wwwroot/scripts/case/case-rev-check.js` and `wwwroot/scripts/case/index.js`; `dotnet build source-code\mmria\mmria-server\mmria-server.csproj -o c:\repos\nccdphp-drh-mmria\artifacts\round4-build-check` passed with existing warnings and 0 errors.
- Validation: `node --check` passed for `wwwroot/js/scripts/system-offline-check.js`, `wwwroot/scripts/case/case-rev-check.js`, and `wwwroot/scripts/case/index.js`; reference scans confirmed the old script path is gone and the case globals are exported only by `case-rev-check.js`; `dotnet build source-code\mmria\mmria-server\mmria-server.csproj -o c:\repos\nccdphp-drh-mmria\artifacts\round4-build-check` passed with pre-existing warnings.

### File List

- `source-code/mmria/mmria-server/Views/Case/Index.cshtml`
- `source-code/mmria/mmria-server/Views/Shared/_LayoutBase.cshtml`
- `source-code/mmria/mmria-server/Views/abstractorDeidentifiedCase/Index.cshtml`
- `source-code/mmria/mmria-server/wwwroot/js/scripts/system-offline-check.js`
- `source-code/mmria/mmria-server/wwwroot/js/system-offline-check.js` (removed)
- `source-code/mmria/mmria-server/wwwroot/scripts/case/case-rev-check.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`

### Change Log

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/Views/Shared/_LayoutBase.cshtml` | Added stale-case modal `<div>` markup adjacent to existing offline modals |
| `source-code/mmria/mmria-server/wwwroot/js/system-offline-check.js` | Added `showStaleCaseModal`, `showStaleCaseBanner`, `stopCaseRevPolling`, `startCaseRevPolling`; exposed all four on `window` |
| `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` | (1) Replaced 409 conflict error handler with `showStaleCaseModal()`. (2) Sync `_rev` polling after online case load, gated on active edit-lock ownership. (3) Restart polling with new `_rev` after successful edit-mode saves. (4) Call `stopCaseRevPolling()` in `navigation_away`. |
| `source-code/mmria/mmria-server/Views/Shared/_LayoutBase.cshtml` | Updated moved system offline checker include to `/js/scripts/system-offline-check.js` |
| `source-code/mmria/mmria-server/Views/Case/Index.cshtml` | Added `/scripts/case/case-rev-check.js` before `/scripts/case/index.js` |
| `source-code/mmria/mmria-server/Views/abstractorDeidentifiedCase/Index.cshtml` | Added `/scripts/case/case-rev-check.js` before `/scripts/case/index.js` |
| `source-code/mmria/mmria-server/wwwroot/js/system-offline-check.js` | Removed old flat-path copy after relocation |
| `source-code/mmria/mmria-server/wwwroot/js/scripts/system-offline-check.js` | Retained only system offline/warn functions and window exports |
| `source-code/mmria/mmria-server/wwwroot/scripts/case/case-rev-check.js` | Added extracted case stale-tab polling, 409 modal, proactive stale modal, reload helper, and window exports |
| `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` | Clarified that successful manual and autosave writes restart `_rev` polling with the returned revision |
| `source-code/mmria/mmria-server/wwwroot/scripts/case/case-rev-check.js` | Restored pre-extraction modal/backdrop UX for proactive staleness and restored reload helper button behavior |
| `_bmad-output/planning-artifacts/prds/prd-mmria-2026-06-12/prd.md` | Clarified FR-19 modal UX, single reload action, autosave pause, and reload helper fallback behavior |
| `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` | Centralized `_rev` polling eligibility on active edit-lock ownership and synced polling when edit mode starts or stops |
| `source-code/mmria/mmria-server/wwwroot/scripts/case/case-rev-check.js` | Added a defensive eligibility guard before polling or showing a stale modal |
| `_bmad-output/implementation-artifacts/12-4-stale-tab-ux.md` | Clarified AC-4 and completion notes so polling scope is active edit mode, not role-only eligibility |
| `_bmad-output/planning-artifacts/prds/prd-mmria-2026-06-12/prd.md` | Updated FR-19.3 poll scope to active edit-lock ownership |
| `_bmad-output/planning-artifacts/epics.md` | Updated stale-tab epic acceptance criteria to active edit-lock polling scope |

---

## Post-Implementation Defects & Follow-Up Requirements

**Status:** Identified after Story 12.4 was marked done. Added 2026-07-10.

---

### Defect 12.4-D1 — Rev polling fires stale modal after autosave increments `_rev`

**Severity:** High — functional regression; stale modal fires every autosave cycle even though the user is the one who caused the `_rev` change.

**Root cause:**  
`startCaseRevPolling` captures `loadedRev` at case-load time (or after a manual save). The autosave path in `wwwroot/scripts/case/index.js` also calls `PUT /api/case/{id}`, which increments `_rev` server-side. Because the polling loop still compares against the original `loadedRev`, every successful autosave causes a mismatch, triggering the proactive stale modal through `showStaleCaseBanner()`.

**Required fix — Autosave must update the polling reference rev:**

After every successful autosave, call `startCaseRevPolling(caseId, newRev)` with the `_rev` returned in the autosave response — the same restart-with-new-rev pattern already used for manual saves (see Change Log entry for `scripts/case/index.js` item 3). The polling reference must stay in sync with any write the current tab performs, whether triggered manually or automatically.

**Acceptance criteria:**

- **AC-D1-1** — Given the autosave timer fires and the autosave PUT succeeds  
  When the `/api/case/{id}/rev` poll next fires  
  Then the stale modal does NOT appear (the polled `_rev` matches the reference rev updated by autosave)

- **AC-D1-2** — Given autosave completes successfully  
  When a _different_ browser session subsequently saves the same case  
  And the `/api/case/{id}/rev` poll fires  
  Then the stale modal DOES appear (staleness from another session is still detected)

- **AC-D1-3** — The autosave code path must call `startCaseRevPolling(caseId, updatedRev)` with the new `_rev` from the autosave response — the same pattern already implemented for manual saves.

---

### Requirement 12.4-R1 — Move `system-offline-check.js` into a `scripts/` subfolder under `/js/`

**Rationale:** As JS responsibilities grow, co-locating all scripts at the root of `/js/` creates a flat, hard-to-navigate structure. Offline-check concerns belong in an organized folder hierarchy.

**Required change — File relocation:**

Move:
```
wwwroot/js/system-offline-check.js
→
wwwroot/js/scripts/system-offline-check.js
```

**Required downstream changes:**

- Any `<script src="...">` references in Razor layouts or views that reference the old path must be updated.
- Any bundle definitions (e.g. `bundleconfig.json`, webpack config, or `_Layout.cshtml` script include blocks) must be updated to the new path.
- Search for all references before moving:
  ```powershell
  Select-String -Path "c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server\**\*" `
      -Pattern "system-offline-check" -Recurse | Select-Object Path, LineNumber, Line
  ```

**Acceptance criteria:**

- **AC-R1-1** — The file `wwwroot/js/system-offline-check.js` no longer exists at the old path.
- **AC-R1-2** — The file exists at `wwwroot/js/scripts/system-offline-check.js`.
- **AC-R1-3** — All Razor views and layout files reference the new path and the script loads without a 404 in the browser.
- **AC-R1-4** — No other JS behavior changes; all existing offline-check and polling functionality works identically after the move.

---

### Requirement 12.4-R2 — Extract case `_rev` polling into its own file `wwwroot/scripts/case/case-rev-check.js`

**Rationale:** `system-offline-check.js` is responsible for system-wide offline/warn status. Case `_rev` staleness detection is a case-level concern and should not live in a system-level file. Decoupling these responsibilities improves maintainability and makes each file's single purpose clear.

**Required change — Extract and move:**

Extract from `wwwroot/js/system-offline-check.js` (after it has been moved per R1):

- `_caseRevPollInterval`
- `_caseRevPollIntervalMs`
- `startCaseRevPolling()`
- `stopCaseRevPolling()`
- `showStaleCaseBanner()`
- `showStaleCaseModal()`
- `mmria_do_case_reload()`
- All `window.*` exposure lines for these symbols

Place them in a new file:
```
wwwroot/scripts/case/case-rev-check.js
```

Remove the extracted code from `system-offline-check.js`. Confirm that `system-offline-check.js` retains only offline/warn status functions: `checkOfflineStatus`, `handleOfflineState`, `showWarnModal`, `closeWarnModal`, `showOfflineModal`, `clearAutoLogoutTimer`, `mmria_offline_modal_ok_handler`, `startOfflineStatusPolling`.

**Required downstream changes:**

- Add a `<script src="~/scripts/case/case-rev-check.js">` include to the Razor layout or view that serves the case edit page — it must be loaded on case edit pages only (or all authenticated pages, consistent with how `system-offline-check.js` is included).
- `wwwroot/scripts/case/index.js` calls `startCaseRevPolling`, `stopCaseRevPolling`, and `showStaleCaseModal` via `window.*` — those bindings must remain in `case-rev-check.js`.
- If the project uses bundling, update `bundleconfig.json` (or equivalent) to include the new file.

**Acceptance criteria:**

- **AC-R2-1** — `case-rev-check.js` exists at `wwwroot/scripts/case/case-rev-check.js`.
- **AC-R2-2** — `system-offline-check.js` no longer contains `startCaseRevPolling`, `stopCaseRevPolling`, `showStaleCaseBanner`, `showStaleCaseModal`, `mmria_do_case_reload`, or their `window.*` exposure lines.
- **AC-R2-3** — `case-rev-check.js` exposes `window.startCaseRevPolling`, `window.stopCaseRevPolling`, `window.showStaleCaseBanner`, `window.showStaleCaseModal`, and `window.mmria_do_case_reload`.
- **AC-R2-4** — The case edit page loads `case-rev-check.js` (verified via browser DevTools Network tab — no 404, script is present in DOM).
- **AC-R2-5** — All existing AC-1 through AC-6 from the original story still pass after the file extraction.
