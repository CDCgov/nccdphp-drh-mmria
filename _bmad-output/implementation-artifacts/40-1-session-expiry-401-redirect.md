# Story 40.1: Session Expiry — Automatic Logout Redirect on 401

Status: ready-for-dev

## Story

As a user returning to the browser after a session has expired,
I want the application to automatically redirect me to the logout page,
so that I see the login screen rather than a frozen or broken UI.

## Acceptance Criteria

1. A global `window.fetch` wrapper in `_LayoutBase.cshtml` intercepts any 401 response. When detected, the user is redirected to `/Account/Logout`.
2. A global `$(document).ajaxError()` handler in the same script block intercepts any 401 from jQuery `$.ajax` calls. When detected, the user is redirected to `/Account/Logout`.
3. Neither interceptor fires when: (a) the current page path already starts with `/Account/`, or (b) `localStorage.getItem('is_offline') === 'true'`.
4. A shared `_sessionExpiredRedirectPending` flag (set before `window.location.href = '/Account/Logout'`) prevents a double-redirect when simultaneous polling and user-initiated calls both return 401.
5. The fetch wrapper always returns the original response to the caller — existing per-call error handlers still receive it.
6. `mmria.js` `mmria_check_if_need_to_redirect()` is updated: if `p_input.status === 401`, return immediately (the global interceptor handles it); existing redirect-on-302 logic is preserved.
7. The existing offline mode auth flow, SAMS logout flow, and `/Account/AppOffline` page are not changed.

## Tasks / Subtasks

- [ ] Add shared flag and fetch interceptor to `_LayoutBase.cshtml` (AC: #1, #3, #4, #5)
  - [ ] Add a `<script>` block in `_LayoutBase.cshtml` that loads after jQuery but before page-specific scripts
  - [ ] Declare `var _sessionExpiredRedirectPending = false;` at module scope
  - [ ] Wrap `window.fetch`: capture `const _originalFetch = window.fetch;`, reassign `window.fetch` to a function that calls `_originalFetch`, and on 401 — if not on `/Account/` and not offline — sets flag and redirects to `/Account/Logout`; always returns the response
- [ ] Add jQuery `ajaxError` handler to the same script block (AC: #2, #3, #4)
  - [ ] `$(document).ajaxError(function(event, jqXHR) { ... })` — same guard conditions and shared flag as the fetch wrapper
- [ ] Update `mmria_check_if_need_to_redirect` in `mmria.js` (AC: #6)
  - [ ] At the start of the function, add: `if (p_input.status === 401) return;`
  - [ ] Existing `if (p_input.ok && p_input.redirected && p_input.url.indexOf('/Account/') > -1)` logic is unchanged
- [ ] Build and smoke test (AC: #1–#7)
  - [ ] Expire a session manually (delete the CouchDB session doc or wait for expiry)
  - [ ] Let the `/api/system-offline/status` poll fire → verify browser redirects to login
  - [ ] Click a case in the case list with an expired session → verify browser redirects to login
  - [ ] Verify offline mode page is NOT affected (no redirect when `is_offline === 'true'`)

## Dev Notes

**Files to modify:**
- `source-code/mmria/mmria-server/Views/Shared/_LayoutBase.cshtml`
- `source-code/mmria/mmria-server/wwwroot/scripts/mmria.js`

**Script block to add in `_LayoutBase.cshtml`** (place after jQuery loads, before closing `</body>` or in `<head>` after jQuery bundle):

```javascript
<script>
(function () {
    var _sessionExpiredRedirectPending = false;

    function _handleSessionExpiry() {
        if (_sessionExpiredRedirectPending) return;
        if (window.location.pathname.indexOf('/Account/') === 0) return;
        if (localStorage.getItem('is_offline') === 'true') return;
        _sessionExpiredRedirectPending = true;
        window.location.href = '/Account/Logout';
    }

    // Fetch interceptor
    var _originalFetch = window.fetch;
    window.fetch = function (input, init) {
        return _originalFetch.call(this, input, init).then(function (response) {
            if (response.status === 401) _handleSessionExpiry();
            return response;
        });
    };

    // jQuery XHR interceptor
    $(document).ajaxError(function (event, jqXHR) {
        if (jqXHR.status === 401) _handleSessionExpiry();
    });
})();
</script>
```

**Why `/Account/Logout` not `/Account/Login`:** `Logout` clears the expired CouchDB session document and the `sid` cookie before routing to the appropriate login path (SAMS or local). Going directly to login leaves a zombie session. `AccountController.Logout()` already handles both SAMS and local paths correctly.

**Server-side is already correct:** `CustomAuthHandler` returns bare 401 for `/api/` paths and page-redirects for page navigations. No server changes needed.

**`navigator.sendBeacon` calls** — 5 fire-and-forget calls in `case/index.js` for offline close-events. No callback exists for these; they are intentionally not covered.

**`_sessionExpiredRedirectPending` scope** — module-scoped IIFE, not exposed globally. The flag is reset implicitly by the page navigation.

**MMRIA has no bundler/build step** — plain JS, changes are live immediately after save.
