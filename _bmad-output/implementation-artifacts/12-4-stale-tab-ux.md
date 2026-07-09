# Story 12.4 — Stale Tab UX

**Epic:** 12 — Data Migration Tool Modernization
**Story ID:** 12.4
**Status:** done
**Date added:** 2026-07-08
**Depends on:** Story 12.3 (Case Rev Endpoint — needed for the `_rev` polling and `X-Offline-Date` header)
**Source requirements:** FR-19.1–FR-19.5

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
And the modal contains a single **[Reload Case]** button that calls `window.location.reload()`
And the generic save error handler does NOT also fire for this 409

**AC-2 — `_rev` polling detects staleness while case is open**
Given a case has been loaded for editing
When 45 seconds have elapsed since the last poll
Then the client calls `GET /api/case/{id}/rev`
And if the returned `_rev` differs from the `_rev` captured at case load time
Then a dismissable banner appears with the text:
`"This case has been updated. Reload to see the latest version."`
And the banner contains **[Reload]** and **[Dismiss]** actions
And the poll continues regardless of whether the banner was dismissed

**AC-3 — Poll interval accelerates during migration window**
Given the `GET /api/case/{id}/rev` response includes the `X-Offline-Date` header
When `Date.now() > new Date(X-Offline-Date).getTime()`
Then the poll interval is reduced to 10 seconds for the remainder of the page session
When `Date.now() <= new Date(X-Offline-Date).getTime()` or the header is absent
Then the poll interval remains at 45 seconds

**AC-4 — Polling only starts for users with write access**
Given the current user does NOT have write access to the case (read-only view)
When the case loads
Then no polling interval is started
And no `GET /api/case/{id}/rev` calls are made

**AC-5 — Poll stops on navigation**
Given the `_rev` poll is active
When the user navigates away from the case (page unload)
Then the polling interval is cleared
And no further `GET /api/case/{id}/rev` calls are made

**AC-6 — Section 508 compliance**
Given the stale-case banner (AC-2) or 409 modal (AC-1) appears
When a screen reader user is on the page
Then the notification is announced to the screen reader via an appropriate ARIA role (`role="alert"` or `aria-live="assertive"`)
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
var _caseRevFastIntervalMs = 10000; // migration window: 10s

/**
 * Starts polling /api/case/{caseId}/rev every _caseRevPollIntervalMs milliseconds.
 * If the returned _rev differs from loadedRev, shows a dismissable stale banner.
 * Adjusts poll interval to 10s when X-Offline-Date header indicates migration is active.
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
                // Check X-Offline-Date for migration window
                var offlineDateHeader = response.headers.get('X-Offline-Date');
                if (offlineDateHeader) {
                    var offlineMs = new Date(offlineDateHeader).getTime();
                    if (!isNaN(offlineMs) && Date.now() > offlineMs) {
                        // Switch to fast polling if not already
                        if (_caseRevPollIntervalMs !== _caseRevFastIntervalMs) {
                            _caseRevPollIntervalMs = _caseRevFastIntervalMs;
                            stopCaseRevPolling();
                            _caseRevPollInterval = setInterval(poll, _caseRevPollIntervalMs);
                        }
                    }
                }

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
 * Shows a dismissable stale-case banner at the top of the page.
 * Called when _rev polling detects the case has been updated server-side.
 */
function showStaleCaseBanner() {
    var bannerId = 'mmria-stale-case-banner';
    if (document.getElementById(bannerId)) return; // Already shown

    var banner = document.createElement('div');
    banner.id = bannerId;
    banner.setAttribute('role', 'alert');
    banner.setAttribute('aria-live', 'assertive');
    banner.style.cssText = 'position:fixed;top:0;left:0;width:100%;background:#fffbcc;color:#333;' +
        'padding:10px 16px;z-index:9999;display:flex;align-items:center;justify-content:space-between;' +
        'border-bottom:2px solid #e6c200;box-sizing:border-box;';

    var msg = document.createElement('span');
    msg.textContent = 'This case has been updated. Reload to see the latest version.';

    var btnWrap = document.createElement('span');

    var reloadBtn = document.createElement('button');
    reloadBtn.textContent = 'Reload';
    reloadBtn.style.marginRight = '8px';
    reloadBtn.addEventListener('click', function () { window.location.reload(); });

    var dismissBtn = document.createElement('button');
    dismissBtn.textContent = 'Dismiss';
    dismissBtn.addEventListener('click', function () {
        var el = document.getElementById(bannerId);
        if (el) el.parentNode.removeChild(el);
    });

    btnWrap.appendChild(reloadBtn);
    btnWrap.appendChild(dismissBtn);
    banner.appendChild(msg);
    banner.appendChild(btnWrap);
    document.body.insertBefore(banner, document.body.firstChild);
}
```

**Wiring `startCaseRevPolling()` from the case edit page:**

The case edit view (`Views/Case/Index.cshtml`) must call `startCaseRevPolling(caseId, loadedRev)` after the case loads and only when the user has write access.

In `Views/Case/Index.cshtml` (or the co-located JS), after the case document is fetched and available:

```javascript
// After case data is loaded:
var caseId = /* the case ID from the URL or loaded doc */;
var loadedRev = caseDoc._rev; // _rev is part of every CouchDB document response

// Only start polling if user has write access
// Check: is the form in edit mode? Is the user an abstractor/data_analyst?
// Use whatever flag the view already has for edit vs. read-only mode.
// Example: if (window.IS_CASE_EDITABLE) { ... }
if (IS_CASE_EDITABLE) { // replace with actual read-only gate
    startCaseRevPolling(caseId, loadedRev);
}

// Stop polling on page unload
window.addEventListener('beforeunload', function () {
    stopCaseRevPolling();
});
```

**Finding the write-access gate:**

The `CaseController.cs` (non-api, `Controllers/CaseController.cs`) is role-restricted to `"abstractor,data_analyst"`. There may be a view-level flag for read-only mode. Search:
```powershell
Select-String -Path "c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server\Views\Case\Index.cshtml" `
    -Pattern "read.only|IS_EDITABLE|canEdit|ViewBag\." | Select-Object LineNumber, Line | Select-Object -First 10
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

This prevents false-positive stale banners after the user's own save.

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

1. **Stale banner within 50s of remote save:**
   - Open case in tab A
   - In tab B (or via direct API), PUT an updated version of the same case
   - Wait up to 50s in tab A
   - Assert stale banner appears

2. **409 modal on stale save attempt:**
   - Open case in tab A, capture current `_rev`
   - Directly PUT an updated version of the case via API (bypassing tab A's save)
   - In tab A, attempt to save
   - Assert 409 modal appears with "Reload Case" button

---

### Important Notes

- `system-offline-check.js` is the correct home for the polling functions — it is already loaded on authenticated pages and has the pattern for `startOfflineStatusPolling()` from Story 8.4.
- The case edit view is a complex SPA-like page. The JS entry point for case loading may be in a Razor `@section Scripts` block in `Views/Case/Index.cshtml` or in a bundled JS file. Trace carefully before modifying.
- Do not disable the Save button when the stale banner is shown (AC-2) — the 409 intercept (AC-1) is the last-resort gate. Disabling the button would prevent saving after a legitimate concurrent update from a different user.
- After a successful save, the `_rev` changes. Update the polling reference rev to avoid a false-positive stale banner on the next poll cycle.

---

## Dev Agent Record

_To be completed by dev agent after implementation._

### Completion Notes

Implemented following the existing `mmria-offline-modal` pattern exactly:

- **Modal markup** added to `_LayoutBase.cshtml` adjacent to the going-offline modal, using the same Bootstrap `.modal-dialog`/`.modal-content` structure and the same purple header (`background-color:#7b2d8e`). Non-dismissable (no close button), single **Reload Case** button calls `window.location.reload()`.
- **Banner** created dynamically via `showStaleCaseBanner()` – appended as `position:fixed` at the top of the page with `role="alert"` and `aria-live="assertive"`. Contains **Reload** and **Dismiss** buttons.
- **Polling** (`startCaseRevPolling` / `stopCaseRevPolling`) added to `system-offline-check.js` following the `startOfflineStatusPolling` pattern. Poll interval: 45 s normal, 10 s when `X-Offline-Date` header indicates migration window is active.
- **Write-access gate**: polling starts only when `g_is_data_analyst_mode == null` (abstracter/write role). Data analyst (`/analyst-case` route sets `g_is_data_analyst_mode = 'da'`) gets no polling.
- **409 intercept**: replaced existing `$mmria.save_error_500_dialog_show()` call for `(409) Conflict` with `window.showStaleCaseModal()`. Does not fall through to generic error handler.
- **Poll restart on save**: after successful save, `startCaseRevPolling` is called with the updated `_rev` from `case_response.rev` to avoid false-positive stale banners.
- **Stop on unload**: `stopCaseRevPolling()` called at the top of `navigation_away()` (the `window.onbeforeunload` handler).

AC-6 (508): stale modal uses `role="alertdialog"`, `aria-modal="true"`, `aria-labelledby`/`aria-describedby`; focus moves to **Reload Case** button. Banner uses `role="alert"` and `aria-live="assertive"`. All buttons are keyboard-accessible.

### Change Log

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/Views/Shared/_LayoutBase.cshtml` | Added stale-case modal `<div>` markup adjacent to existing offline modals |
| `source-code/mmria/mmria-server/wwwroot/js/system-offline-check.js` | Added `showStaleCaseModal`, `showStaleCaseBanner`, `stopCaseRevPolling`, `startCaseRevPolling`; exposed all four on `window` |
| `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` | (1) Replaced 409 conflict error handler with `showStaleCaseModal()`. (2) Start `startCaseRevPolling` after online case load, gated on write-access. (3) Restart polling with new `_rev` after successful save. (4) Call `stopCaseRevPolling()` in `navigation_away`. |
