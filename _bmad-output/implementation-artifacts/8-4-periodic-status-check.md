# Story 8.4: Periodic Offline Status Check

Status: not-started

## Story

As a user,
I want the application to automatically check the system offline status while I am working,
so that if the offline date arrives during my session, I am immediately prompted to save and sign out without needing to reload the page.

## Acceptance Criteria

1. After a user is authenticated and the initial page loads, a 2-minute periodic poll (setInterval) calls `/api/system-offline/status`.
2. On each poll response, evaluate whether the `warn_date` or `offline_date` threshold has been crossed using the same `checkOfflineStatus(config)` logic from Story 8.3.
3. If the poll result returns state `"warn"` and the warning modal has not already been shown in this session (`sessionStorage` gate), show the warning modal and set the gate.
4. If the poll result returns state `"offline"`, show the going-offline modal (respecting the `localStorage` gate). The user must click OK to proceed with save + sign-out.
5. If the poll result returns state `"normal"`, no modals are shown. Already-displayed modals are not re-shown.
6. The poll is only active on authenticated pages. It is not started on the login page or other anonymous pages.
7. If the poll request fails (network error, 401), log to console and continue polling — do not surface an error to the user.

## Tasks / Subtasks

- [ ] Implement polling in `system-offline-check.js` (AC: #1–#5, #7)
  - [ ] Export function `startOfflineStatusPolling(intervalMs = 120000)` that:
    - Uses `setInterval` to call `/api/system-offline/status` every `intervalMs` milliseconds
    - On success: calls `checkOfflineStatus(config)` → `handleOfflineState(state, config)`
    - On fetch error: `console.warn("Offline status poll failed:", err)` — do not throw
  - [ ] Returns the interval ID (so the caller can clear it if needed)
- [ ] Start polling on authenticated page load (AC: #6)
  - [ ] In the shared layout JS (same location as the initial check in Story 8.3):
    - After the initial check completes, call `startOfflineStatusPolling()`
    - Only start polling on authenticated pages (check existing server-rendered auth indicator)
- [ ] Verify modal gates prevent duplicate display (AC: #3–#5)
  - [ ] Confirm `handleOfflineState` from Story 8.3 already checks `sessionStorage` / `localStorage` gates before showing each modal
  - [ ] No changes needed to modal logic — polling reuses the same handlers
- [ ] Build and verify (AC: #1–#7)
  - [ ] Log in and open browser dev tools; confirm XHR/fetch to `/api/system-offline/status` every 120 seconds
  - [ ] Set `warn_date` to now+3 minutes; wait for poll to fire after threshold passes — warning modal appears once
  - [ ] Set `offline_date` to now+3 minutes; wait for poll — going-offline modal appears
  - [ ] Simulate network failure (devtools offline mode); confirm no error modal, polling continues on reconnect

## Dev Notes

**Polling interval:** 2 minutes = 120,000 ms. Make this configurable by passing `intervalMs` so tests can use a shorter interval.

**Polling start point:** In the shared `_Layout.cshtml` JS block (or `site.js`):
```javascript
import { startOfflineStatusPolling, checkOfflineStatus, handleOfflineState } from './system-offline-check.js';

// Initial check
fetch('/api/system-offline/status')
  .then(r => r.json())
  .then(config => handleOfflineState(checkOfflineStatus(config), config))
  .catch(err => console.warn('Initial offline check failed:', err));

// Polling
startOfflineStatusPolling();
```
Or inline in a `<script>` block if the project does not use ES modules. Follow the existing JS bundling approach in the project.

**Fetch with credentials:** The `/api/system-offline/status` endpoint requires authentication. The browser's session cookie is sent automatically with `fetch` if called same-origin. No extra headers needed.

**Stopping the poll:** Not required for MVP — the modal handler will show the going-offline modal and trigger sign-out, which navigates away and stops the poll naturally.

**Do not poll on login page:** The login page is anonymous; the shared layout may or may not include the polling JS. Confirm whether the login page uses a separate layout. If it shares the authenticated layout, add an auth check before starting the poll (e.g., check a server-rendered `window.IS_AUTHENTICATED = true` flag in the layout).

### Project Structure Notes

- Modified files: `wwwroot/js/system-offline-check.js` (add `startOfflineStatusPolling`)
- Modified files: `_Layout.cshtml` (start poll after initial check)
- No new C# files
- No new NuGet packages

### References

- [Source: prd-mmria-2026-06-12/prd.md#FR-8.6, FR-8.7]
- Depends on Story 8.1 (`/api/system-offline/status` endpoint)
- Depends on Story 8.3 (`system-offline-check.js` module, modal handlers, `handleOfflineState`)

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
- `source-code/mmria/mmria-server/wwwroot/js/system-offline-check.js` (modified)
- `source-code/mmria/mmria-server/Views/Shared/_Layout.cshtml` (modified — start polling)
