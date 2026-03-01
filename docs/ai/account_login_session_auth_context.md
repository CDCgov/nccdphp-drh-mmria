# Account Login + Session Auth Context

Related baseline guidance: [AI_CONTEXT.md](./AI_CONTEXT.md)

## Why this file exists
This captures the key lessons from a login/session regression investigation so future work can move faster and avoid reintroducing the same issue.

## Problem summary
- Symptom: non-admin login failed in app flow.
- CouchDB direct auth checks for the same user/password succeeded.
- App logs showed login auth passed, but session document write failed with CouchDB `forbidden`.
- Admin login succeeded.

## What we learned
1. `/_session` authentication and subsequent CouchDB writes can interfere when they share the same HTTP handler cookie state.
2. If cookies are enabled, user `AuthSession` cookies may be reused unintentionally on later requests that are expected to use admin/service credentials.
3. In this codebase, `CouchDbHttpClient` uses `CreateClient("CouchDb")`, so auth and write calls share the same named client configuration.

## Minimal, stable fix
1. Keep `AccountDAL.AuthenticateWithSessionAsync` as a DAL call (no controller CouchDB calls).
2. Use `CouchDbHttpClient` for `/_session` POST with `application/x-www-form-urlencoded` payload.
3. Configure the named `CouchDb` handlers with `UseCookies = false` in `Program.cs` so auth cookies are never persisted across requests.
4. Keep session reads/writes on configured service/admin credentials (existing `dbConfig.user_name/user_value` behavior).

## Why this is preferred
- Aligns with AI context architecture: Controllers -> Manager -> DAL and centralized HTTP client usage.
- Avoids mixed transport behavior (`WebRequest` in one place, `CouchDbHttpClient` elsewhere).
- Eliminates hidden state from cookies and keeps credential intent explicit per request.

## Guardrails for future changes
- Do not re-enable cookies on the `CouchDb` named client unless there is a clear, tested requirement.
- Do not add fallback write paths that switch from admin to end-user credentials for session persistence.
- If login behavior differs by user role, inspect:
  - `/_session` auth result
  - session document PUT result
  - which credential identity CouchDB evaluated the write as

## Quick troubleshooting checklist
1. Verify `/_session` auth response for the login user.
2. Verify session PUT response body from CouchDB (`ok` vs `forbidden`).
3. Confirm `Program.cs` still has `UseCookies = false` for `CouchDb` handlers.
4. Confirm session writes are using service/admin credentials from config.

## Scope note
This context is about account login/session persistence behavior only. Keep unrelated refactors separate from auth transport changes.
