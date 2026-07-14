---
baseline_commit: 5628e689e9e63273b662f9b7037e8d17e269908f
---

# Story 8.3: Warning Modal and Going Offline Modal

Status: done

## Story

As a user,
I want to be shown a warning when the system is approaching its offline date and a final prompt before the system goes offline,
so that I have the opportunity to save my work and sign out gracefully before access is lost.

## Acceptance Criteria

1. After a user logs in (on first authenticated page load), the client fetches the `/api/system-offline/status` endpoint and evaluates the current state.
2. **Warning Modal:** If `DateTime.now >= warn_date` and `DateTime.now < offline_date`, display a dismissable modal containing `warn_message`. This modal is shown once per session — use `sessionStorage["warn_modal_shown"] = "1"` as the gate so it does not reappear if the user navigates within the same tab session.
3. **Going Offline Modal:** If `DateTime.now >= offline_date`, display a non-dismissable modal containing `offline_modal_message`. The modal has one button: "OK". Clicking OK: (a) if the user is on a case editing page, trigger the existing save operation first, then (b) sign the user out by navigating to the sign-out URL. This modal is gated by `localStorage["offline_modal_shown"] = "1"` so it does not re-trigger on page reload after sign-out, but clears on next login.
4. If both `warn_date` and `offline_date` are null/empty, no modals are shown.
5. The check described in AC #1 is performed once on initial page load (not on a poll interval — polling is covered in Story 8.4). Polling results that cross a threshold also trigger these modals.
6. Modal styling follows the existing modal patterns in the application (Bootstrap or the existing modal component).
7. The warning modal can be dismissed by the user (close button or backdrop click). The going-offline modal cannot be dismissed — only the "OK" action button closes it.

## Tasks / Subtasks

- [x] Create shared JS module for offline status evaluation (AC: #1–#5)
  - [x] New file: `source-code/mmria/mmria-server/wwwroot/js/system-offline-check.js` (or `.ts` if project uses TypeScript)
  - [x] Export function `checkOfflineStatus(config)` that:
    - Evaluates `config.warn_date` and `config.offline_date` against `Date.now()`
    - Returns `{ state: "normal" | "warn" | "offline" }`
  - [x] Export function `handleOfflineState(state, config)` that:
    - If `state == "warn"` and `sessionStorage.getItem("warn_modal_shown") != "1"`: show warning modal; `sessionStorage.setItem("warn_modal_shown", "1")`
    - If `state == "offline"` and `localStorage.getItem("offline_modal_shown") != "1"`: show going-offline modal; `localStorage.setItem("offline_modal_shown", "1")`
- [x] Implement warning modal (AC: #2, #6, #7)
  - [x] Modal content: `warn_message` (from config); dismissable via X / backdrop
  - [x] Insert modal markup into the shared layout (e.g., `_Layout.cshtml`) so it is available on all authenticated pages, hidden by default
- [x] Implement going-offline modal (AC: #3, #6, #7)
  - [x] Modal content: `offline_modal_message`; single "OK" button; no close/X/backdrop dismiss
  - [x] OK handler:
    - Check if the current page has an unsaved case (use existing indicator or `window.hasUnsavedChanges` flag)
    - If yes: call existing save function, then sign out
    - If no: sign out immediately
    - Sign out: navigate to `/account/logout` (or the existing sign-out URL; confirm by checking `_Layout.cshtml`)
  - [x] Insert modal markup into shared layout alongside warning modal
- [x] Wire up initial page load check (AC: #1, #4, #5)
  - [x] In the shared layout JS (or a `site.js` entry point), after DOM ready and user is authenticated:
    - Fetch `/api/system-offline/status`
    - Call `checkOfflineStatus(config)` → `handleOfflineState(state, config)`
  - [x] Skip fetch if user is not authenticated (check a server-rendered auth flag or rely on 401 response)
- [x] Clear `offline_modal_shown` on login (AC: #3)
  - [x] On the login success redirect (or in the post-login JS), call `localStorage.removeItem("offline_modal_shown")`
- [x] Build and verify (AC: #1–#7)
  - [x] Set `warn_date` to now-1 hour, `offline_date` to now+1 hour; log in — warning modal appears once per session tab
  - [x] Set `offline_date` to now-1 hour; log in — going-offline modal appears, cannot be dismissed, OK signs out
  - [x] Reload after OK is clicked — going-offline modal does not reappear (localStorage gate)

## Dev Notes

**Shared layout file:** Find `_Layout.cshtml` or the authenticated layout. Modal markup goes here (hidden `<div id="warn-modal" ...>` and `<div id="offline-modal" ...>`). Use Bootstrap `modal` classes matching the rest of the app.

**Save-before-signout:** Confirm the existing case editing page's save mechanism. Look for a global function like `saveCase()`, `submitCaseForm()`, or check if there is a `window.hasUnsavedChanges` flag. The OK handler should call the appropriate function and wait for its promise to resolve before redirecting.

**Date parsing:** Config dates from `/api/system-offline/status` are ISO strings. Parse with `new Date(config.offline_date)`. Compare with `Date.now()`. Handle empty/null gracefully (`if (!config.offline_date) return "normal"`).

**Going-offline modal `localStorage` gate:** Using `localStorage` (persists across sessions) vs `sessionStorage` (tab-only) is intentional — the offline modal should fire once per user once the offline date passes, then not repeat after reload. However, it should fire again on next login after they were signed out. Hence: set localStorage gate when the modal is first shown, clear it on login.

**Sign-out URL:** Check `_Layout.cshtml` for the existing "Sign Out" link href. Use that same URL.

### Project Structure Notes

- New files: `wwwroot/js/system-offline-check.js`
- Modified files: `_Layout.cshtml` (add modals, wire up JS), login success page or shared JS (clear localStorage)
- No new C# files (endpoints already in Story 8.1)
- No new NuGet packages

### References

- [Source: prd-mmria-2026-06-12/prd.md#FR-8.3, FR-8.5, FR-8.6]
- Depends on Story 8.1 (`/api/system-offline/status` endpoint)

## Dev Agent Record

### Agent Model Used
Claude Sonnet 4.6

### Debug Log References

### Completion Notes List
- Created `wwwroot/js/system-offline-check.js` with `checkOfflineStatus`, `handleOfflineState`, `showWarnModal`, `closeWarnModal`, `showOfflineModal`, and `mmria_offline_modal_ok_handler`. All exposed on `window`.
- Added warning modal (`#mmria-warn-modal`) and going-offline modal (`#mmria-offline-modal`) markup to `_LayoutBase.cshtml` (the authenticated layout used by all main views). Both follow the Bootstrap modal pattern used elsewhere in the codebase.
- Warning modal is dismissable via X button, "Dismiss" button, or backdrop click. Going-offline modal has only an "OK" button with no close/backdrop dismiss.
- `mmria_offline_modal_ok_handler` checks `window.hasUnsavedChanges` and calls `window.mmria_save_before_signout()` if both are present; otherwise signs out immediately by submitting a POST form to `/Account/Logout` with the antiforgery token.
- Initial page load fetch of `/api/system-offline/status` is wired in a `DOMContentLoaded` script block in `_LayoutBase.cshtml`. 401/error responses are silently ignored.
- `offline_modal_shown` localStorage gate is cleared in `account_login.js` on every login page load (the page users land on after logout), satisfying AC #3.
- Build succeeded with 0 errors (82 pre-existing warnings, unchanged from baseline).

### File List
- `source-code/mmria/mmria-server/wwwroot/js/system-offline-check.js` (new)
- `source-code/mmria/mmria-server/Views/Shared/_LayoutBase.cshtml` (modified — added modal markup, `system-offline-check.js` script tag, and `DOMContentLoaded` fetch/check block)
- `source-code/mmria/mmria-server/wwwroot/scripts/Account/account_login.js` (modified — clear `offline_modal_shown` localStorage key on page load)
