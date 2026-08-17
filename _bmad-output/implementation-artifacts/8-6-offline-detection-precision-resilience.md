---
implemented: 2026-07-18
---

# Story 8.6: Offline Detection Precision, Resilience, and Recovery UX

Status: done

## Story

As a logged-in user,
I want the offline modal to fire at exactly the scheduled offline time and the system to handle mmria-services outages gracefully,
so that the offline transition is predictable and a services restart does not cause false sign-outs or stale modal state.

## Acceptance Criteria

1. On every successful offline-status fetch, a `setTimeout` fires at exactly `new Date(offline_date) - Date.now()` ms (and separately for `warn_date`) so the modal triggers at the precise threshold rather than waiting up to 2 minutes for the next poll.
2. Each poll reschedules these precision timers so a date change pushed by an admin takes effect within the next poll cycle (≤ 2 minutes).
3. When the offline modal countdown reaches zero, the client re-fetches `/api/system-offline/status` before signing out: (a) still offline → sign out; (b) date pushed to future → close modal, re-show warn modal with updated message, reschedule precision timers; (c) dates cleared → close modal silently.
4. When the user clicks OK on the offline modal, the same re-check applies (identical behavior to AC #3).
5. When any status fetch fails (mmria-services down), the handler uses `_lastKnownConfig` (last successful fetch). If last-known indicates offline, sign-out proceeds. If online or no prior data exists, logout is cancelled and the user remains in session.
6. When a logged-in user refreshes any page while `now >= offline_date`, the `_LayoutBase` initial status check redirects directly to `/Account/AppOffline` — the `localStorage` modal gate is not consulted on page load.
7. The 2-minute poll continues as a fallback; poll failures are logged and silently swallowed.

## Tasks / Subtasks

- [x] Add `_lastKnownConfig` module state and `runOfflineCheck` entry point (AC: #5, #6)
  - [x] `var _lastKnownConfig = null` at module level in `system-offline-check.js`
  - [x] `runOfflineCheck(config)` stores config in `_lastKnownConfig`, calls `checkOfflineStatus` + `handleOfflineState` + `scheduleOfflineCheckAtDates`
  - [x] Expose `window.runOfflineCheck`
- [x] Add `scheduleOfflineCheckAtDates(config)` precision timer function (AC: #1, #2)
  - [x] Clears `_warnDateTimeout` and `_offlineDateTimeout` before rescheduling
  - [x] Calculates `setTimeout` delay as `new Date(date) - Date.now()`; skips if past or NaN
  - [x] Called from `runOfflineCheck` and from `startOfflineStatusPolling` on each successful poll
- [x] Extract `_doSignOut`, `_proceedWithSignOut`, `_cancelOfflineLogout`, `_handleFetchFailure` helpers (AC: #3, #4, #5)
  - [x] `_doSignOut()` — submits logout POST form directly; no re-check (prevents recursive fetch loops)
  - [x] `_proceedWithSignOut()` — calls `window.mmria_save_before_signout` if available, then `_doSignOut`
  - [x] `_cancelOfflineLogout(result, config, okButtonEl)` — hides modal, clears both localStorage/sessionStorage gates, calls `handleOfflineState` (re-shows warn modal if state is `warn`) and `scheduleOfflineCheckAtDates`
  - [x] `_handleFetchFailure(okButtonEl)` — uses `_lastKnownConfig || { state: 'normal' }`; calls `_proceedWithSignOut` if offline, `_cancelOfflineLogout` otherwise
- [x] Update countdown expiry re-check to use helpers (AC: #3)
  - [x] `.then` offline branch → `_proceedWithSignOut()`
  - [x] `.then` non-offline branch → `_cancelOfflineLogout(result, latestConfig, null)`
  - [x] `.catch` → `_handleFetchFailure(null)`
- [x] Update `mmria_offline_modal_ok_handler` to use helpers (AC: #4)
  - [x] Remove local `doSignOut` / `proceedWithSignOut` functions
  - [x] `.then` non-offline branch → `_cancelOfflineLogout(result, latestConfig, okBtn)`
  - [x] `.then` offline branch → `_proceedWithSignOut()`
  - [x] `.catch` → `_handleFetchFailure(okBtn)`
- [x] Update `startOfflineStatusPolling` to call `runOfflineCheck` (AC: #2, #7)
  - [x] Replace three separate calls with single `runOfflineCheck(config)` call
  - [x] Poll `.catch` logs warning; no state change (already correct)
- [x] Update `_LayoutBase.cshtml` initial fetch (AC: #6)
  - [x] If `checkOfflineStatus(config).state === 'offline'` → `window.location.href = '/Account/AppOffline'`
  - [x] Otherwise → `window.runOfflineCheck(config)`

## Files Changed

| File | Change |
|---|---|
| `wwwroot/js/scripts/system-offline-check.js` | Added `_lastKnownConfig`, `runOfflineCheck`, `scheduleOfflineCheckAtDates`, `_doSignOut`, `_proceedWithSignOut`, `_cancelOfflineLogout`, `_handleFetchFailure`; updated countdown tick, `mmria_offline_modal_ok_handler`, `startOfflineStatusPolling` |
| `Views/Shared/_LayoutBase.cshtml` | Initial fetch now redirects to AppOffline on `state: offline`; calls `runOfflineCheck` on success |

## Dev Notes

**Precision timer limit:** `setTimeout` has a maximum delay of ~24.8 days (`2^31 - 1` ms). Offline windows this far in the future are not a realistic scenario for MMRIA; no clamping is applied.

**No recursive fetch loops:** The countdown and OK paths both call `_proceedWithSignOut` (direct form submit) or `_cancelOfflineLogout` — neither triggers `mmria_offline_modal_ok_handler` again. This eliminates the infinite-retry risk introduced by the previous pattern of calling `mmria_offline_modal_ok_handler()` from within a catch block.

**Gate clearing on cancel:** Both `localStorage("offline_modal_shown")` and `sessionStorage("warn_modal_shown")` are cleared when a logout is cancelled so that the warn and offline modals can fire again if the rescheduled date is reached.
