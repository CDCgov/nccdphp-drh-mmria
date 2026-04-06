# Authentication, Session, and Timeout Context

- Status: Active
- Scope: Password login, SAMS login, application session persistence, timeout behavior, and durable login/session transport guidance.
- When to use: Read this before changing `AccountController`, `AccountController.OIDC`, `CustomAuthHandler`, session persistence, or client re-auth flows.
- Last verified: 2026-04-06
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Offline Mode Documentation](./offline_mode.md), [Historical Account Login Regression Note](./archive/account_login_session_auth_context.md)

## What is current today

### Session model

- The browser cookie is `sid`.
- The authoritative application session record is the CouchDB document stored under `session/{sid}`.
- Expiration enforcement is based on the session document, not the cookie alone.

Primary code locations:

- [Session model](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Session/Model/Session_Message.cs)
- [Password login flow](../../source-code/mmria/mmria-server/Controllers/AccountController.cs)
- [SAMS login flow](../../source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs)
- [Authentication handler](../../source-code/mmria/mmria-server/CustomAuthHandler.cs)

## Login flows

### Password login

- `AccountController` reads `session_idle_timeout_minutes`.
- It creates a new session document with `date_created`, `date_last_updated`, `date_expired`, and `is_active`.
- It sets the `sid` cookie to the same expiration window.

### SAMS login

- `AccountController.OIDC` performs the OIDC flow and then creates the same application session document.
- The post-authentication app session model is shared with password login.

### Durable rule

- Password login and SAMS are different entry paths.
- After sign-in, both rely on the same MMRIA session document pattern.

## Timeout behavior

### Configuration

- Session timeout is driven by `session_idle_timeout_minutes`.
- The value is surfaced into the shared config payload through [Controllers/_config.cs](../../source-code/mmria/mmria-server/Controllers/_config.cs).

### Runtime enforcement

`CustomAuthHandler` currently:

1. reads the `sid` cookie
2. loads `session/{sid}` from CouchDB
3. rejects the request if the session is missing or expired
4. refreshes `date_expired` when the session remains active using the same shared timeout resolver used by session creation

### Offline mode

- Offline mode now also uses `session_idle_timeout_minutes` for client-side inactivity re-auth.
- This is a local offline-session idle timer that redirects the user to `/Account/OfflineLogin` when inactivity exceeds the configured timeout.
- In the current v1 implementation, this does not change the longer-lived offline server auth token created by `create-offline-auth-token`.
- That split is intentional for now so overnight offline resume behavior is preserved.

### Current contract

- `session_idle_timeout_minutes` remains the source-of-truth config key.
- Password login, SAMS session creation, and sliding refresh now resolve it through the same shared helper.
- Offline mode uses the same config key for local inactivity re-auth while preserving the current longer offline server-token lifetime.
- Tenant-specific overrides apply consistently during login and later authenticated traffic.
- Shared config and the code default are only used as fallbacks when a tenant-specific value is absent.

Treat that as the current implementation contract when changing auth/session timeout code.

## Logout and expired-session behavior

### Logout

- `AccountController` loads the session document, marks it expired, saves it, and clears the `sid` cookie.

### Expired sessions during normal navigation

- Full-page navigation is redirected server-side to `/Account/Login` or `/Account/SignIn`, depending on the auth path.

### Expired sessions during JavaScript API calls

- `fetch()` follows redirects automatically.
- A redirected API call can look superficially successful while actually returning HTML from an account page.
- Some client paths already check for that and redirect the browser, but there is no universal wrapper yet.

## `/account/auto-login`

`AccountController.AutoLogin(...)` is the current server-side abstraction for client code that needs to re-enter authentication without assuming the exact provider.

Behavior:

- If SAMS is enabled, it redirects to `SignIn`.
- Otherwise, it redirects to `Login`.
- `returnUrl` is preserved.

Use this pattern when client-side code needs to send the user back through authentication and the code should work for both SAMS and non-SAMS tenants.

## Durable login/session transport guidance

This is the durable lesson folded in from the earlier login/session regression investigation.

### What we learned

- `/_session` authentication and later CouchDB writes can interfere if they share persisted HTTP cookie state.
- The repository-wide `CouchDbHttpClient` creates its HTTP client through `CreateClient("CouchDb")`.
- `Program.cs` configures the named `CouchDb` client with `UseCookies = false`, which avoids reusing end-user CouchDB auth cookies on later service-credential requests.

Relevant code:

- [Program.cs](../../source-code/mmria/mmria-server/Program.cs)
- [CouchDbHttpClient.cs](../../nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs)

### Stable guardrails

- Keep `/_session` authentication work in DAL or manager-backed flows rather than controller-level ad hoc HTTP calls.
- Do not re-enable cookies on the named `CouchDb` client unless there is a specific, tested reason.
- Keep session document reads and writes on configured service or admin credentials, not on an end-user credential fallback path.
- If login behavior differs by user role, inspect both the `/_session` result and the session-document write result before changing broader auth flow code.

## Current client-side handling

- Some flows use `mmria_check_if_need_to_redirect(...)` in [wwwroot/scripts/mmria.js](../../source-code/mmria/mmria-server/wwwroot/scripts/mmria.js).
- There is still no app-wide fetch wrapper that standardizes redirected-account handling for every API call.

## Preferred direction for future work

This is future-state guidance, not current implementation.

- Introduce a shared fetch helper for API calls that may be redirected to account pages.
- Check redirect or auth-failure conditions before attempting JSON parsing.
- Preserve `returnUrl` for SAMS and non-SAMS re-auth flows.
- Do not auto-retry non-idempotent writes silently after re-authentication.

## Quick checklist

- If client code needs to trigger login, prefer `/account/auto-login` over hard-coding `/account/login`.
- If you change session persistence, confirm the named `CouchDb` client still uses `UseCookies = false`.
- If timeout behavior changes, inspect the shared timeout resolver and every auth path that consumes it.

