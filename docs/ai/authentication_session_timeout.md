# Authentication, Session, and Timeout Context

This document captures how authentication sessions are currently configured and enforced, how client-side code reacts to session expiration, and the recommended direction for improving the experience in production.

## Scope
- Password login flow
- SAMS login flow
- `sid` application session handling
- Session timeout enforcement
- Client-side redirect behavior for expired sessions

## Configuration

### Session timeout setting
- Config key: `mmria_settings.session_idle_timeout_minutes`
- Default in local config: `70`
- This value is surfaced into the shared config payload in [`_config.cs`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/Controllers/_config.cs#L240).
- Local appsettings also includes the value in [`appsettings.json`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/appsettings.json).

### Production expectation
- Production tenants are commonly configured to `720` minutes.
- That is a 12-hour idle timeout.

## Server-side Session Model

### Session document
The application uses its own CouchDB-backed session document, not just the browser cookie.

Model:
- [`Session_Message.cs`](/c:/repos/nccdphp-drh-mmria/nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Session/Model/Session_Message.cs)

Important fields:
- `_id`
- `date_created`
- `date_last_updated`
- `date_expired`
- `is_active`
- `user_id`
- `role_list`
- `data`

### Source of truth
- The browser cookie is `sid`
- The real session authority is the `session/{sid}` document in CouchDB
- Expiration enforcement is based on `date_expired`

## Login Flows

### Password login
- Controller: [`AccountController.cs`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/Controllers/AccountController.cs#L166)
- Reads `session_idle_timeout_minutes`
- Creates a session document with:
  - `date_created = now`
  - `date_last_updated = now`
  - `date_expired = now + timeout`
  - `is_active = true`
- Sets the `sid` cookie with the same expiration

### SAMS login
- Controller: [`AccountController.OIDC.cs`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs#L264)
- After successful SAMS/OIDC processing, it creates the same application session document
- Sets the same `sid` cookie

### Important conclusion
- Password and SAMS use different authentication entry flows
- After login, both rely on the same MMRIA application session model

## Runtime Timeout Enforcement

### Authentication handler
- File: [`CustomAuthHandler.cs`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/CustomAuthHandler.cs#L74)

Current behavior:
1. Read `sid` cookie
2. Load `session/{sid}` from CouchDB
3. Reject if:
   - session document missing
   - `date_expired` is null
   - `date_expired < now`
4. If valid, build claims principal and continue

### Sliding expiration
The handler extends the session when requests continue to arrive.

Current logic:
- It computes remaining time from `date_expired - now`
- It reads `session_idle_timeout_minutes`
- If the remaining time is under the timeout and more than about one minute has elapsed, it updates:
  - `date_expired = now + timeout`

Code:
- [`CustomAuthHandler.cs:127`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/CustomAuthHandler.cs#L127)

### Important caveat
Login reads timeout by tenant prefix, but the sliding refresh path currently reads timeout from `"shared"`:
- [`CustomAuthHandler.cs:128`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/CustomAuthHandler.cs#L128)

If a tenant overrides timeout, login and refresh may not use the same value.

## Logout

- Controller: [`AccountController.cs`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/Controllers/AccountController.cs#L332)

Current behavior:
- Load the session document
- Set `date_expired = now`
- Save session
- Clear `sid` cookie

## What Happens When a Session Expires

### Server behavior
When the app decides the session is expired, auth challenge redirects to:
- `/Account/Login`, or
- `/Account/SignIn`

Code:
- [`CustomAuthHandler.cs:214`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/CustomAuthHandler.cs#L214)

### Route navigation
For normal page navigation, this works acceptably:
- user clicks a route
- server redirects to login/SAMS

### JavaScript API calls
This is less consistent.

Because `fetch` follows redirects automatically, an expired API request may return:
- `response.ok === true`
- `response.redirected === true`
- `response.url` pointing at `/Account/...`

The client may then try to parse HTML as JSON unless it explicitly checks for this.

## Current Client-side Handling

### Redirect helper
- Helper: [`mmria_check_if_need_to_redirect()`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/wwwroot/scripts/mmria.js#L2639)

Current behavior:
- If response is redirected to `/Account/...`, perform full browser redirect

### Known current call sites
- [`case/index.js:2498`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L2498)
- [`case/index.js:2883`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L2883)
- [`manage-users/index.js:1390`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/wwwroot/scripts/manage-users/index.js#L1390)
- [`manage-users/index.js:1427`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/wwwroot/scripts/manage-users/index.js#L1427)
- [`manage-users/index.js:1460`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/wwwroot/scripts/manage-users/index.js#L1460)
- [`manage-case-folders/index.js:621`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/wwwroot/scripts/manage-case-folders/index.js#L621)

### Important limitation
There is no app-wide fetch wrapper.

Result:
- some API calls redirect cleanly
- many other API calls can fail as:
  - JSON parse errors
  - generic fetch failures
  - generic save-error dialogs

## Existing Session Warning UI

There is older page-level warning UI on the case and de-identified pages:
- [`case/index.js:1365`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/wwwroot/scripts/case/index.js#L1365)
- [`de-identified/index.js:231`](/c:/repos/nccdphp-drh-mmria/source-code/mmria/mmria-server/wwwroot/scripts/de-identified/index.js#L231)

It shows a warning dialog and eventually calls `profile.logout()`.

Important note:
- this is not a general API-timeout handling solution
- it is page-specific UI

## Observed User Experience

### Home page idle overnight
- Next route navigation should redirect to login/SAMS

### Case page idle overnight, then save
- Save should not succeed
- In the known save path, the client redirects to login because it explicitly calls `mmria_check_if_need_to_redirect(...)`
- Unsaved edits may still be present in memory, but the server save does not complete

## Proposed Improvement Plan

### Goal
Make session-expiration handling predictable and consistent, especially for SAMS production usage.

### Recommended plan
1. Add a shared client wrapper such as `mmria_fetch(...)` and `mmria_fetch_json(...)`
   - perform `fetch`
   - detect redirected `/Account/...`
   - detect `401` or `403` if introduced later
   - redirect once in a single shared place
   - only parse JSON after the redirect/session check

2. Migrate high-risk flows first
   - case load
   - case save
   - case summary
   - major admin screens
   - offline sync/save endpoints

3. Improve API contract for expired sessions
   - Keep redirect behavior for full page requests
   - Prefer explicit `401` JSON responses for `/api/*`
   - Example:
     - `code: "session_expired"`
     - `reauth_url: "/Account/SignIn?returnUrl=..."`

4. Optimize for SAMS re-authentication UX
   - preserve `returnUrl`
   - send the user back to the exact route/hash after re-auth
   - for edit/save flows, show a modal before redirect:
     - `Your session expired. Sign in again to continue.`

5. Do not auto-retry non-idempotent writes by default
   - for POST/save, safer pattern is:
     - preserve client state
     - re-authenticate
     - let the user retry save

### Why this is better for SAMS
- If the user is still authenticated at the identity provider, re-entry is often immediate
- Redirecting with a preserved return URL makes the transition feel intentional instead of broken
- It avoids HTML-login-page-as-JSON failure modes

## Suggested Future Work Order
1. Add shared fetch wrapper
2. Convert case page to wrapper
3. Convert other high-risk fetch callers
4. Decide whether `/api/*` should return `401` JSON instead of redirecting
5. Align timeout refresh in `CustomAuthHandler` with tenant-specific timeout lookup if tenant overrides matter
