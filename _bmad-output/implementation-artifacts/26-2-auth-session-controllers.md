# Story 26.2 — Auth and Session Controllers

**Epic:** 26 — Controller API Direct-Call Remediation
**Story ID:** 26.2
**Status:** ready-for-dev
**Date added:** 2026-07-17
**Depends on:** Epic 23 story 23.2 (ISessionRepository), Epic 18 story 18.2 (IUserRepository)
**Source requirements:** epics.md §Epic 26 Story 26.2; project-context.md §2.2

---

## User Story

As a developer,
I want auth and session-related middleware and utilities to call `ISessionRepository` instead of constructing CouchDB URLs directly,
So that authentication-path database access has a SQL migration seam.

---

## Acceptance Criteria

**AC-1 — `CustomAuthHandler.cs` session read replaced**
Given `CustomAuthHandler.cs` at approximately line 86 constructs `{prefix}session/{sid}` and calls `_couchDbHttpClient.ExecuteAsync("GET", ...)` to load the session document for each request
When this story is complete
Then that call is replaced with `await _sessionRepository.GetSessionDocumentRawAsync(Request.Cookies["sid"], db_config)` (returns raw JSON string, matching the current deserialization flow) or `GetSessionDocumentAsync(...)` if the typed return value is preferable; `ISessionRepository` is injected into the auth handler

**AC-2 — `passwordChangeController.cs` session lookup replaced**
Given `passwordChangeController.cs` at approximately line 80 constructs a session-event URL and calls `_couchDbHttpClient.ExecuteAsync("GET", ...)` to look up session state before allowing a password change
When this story is complete
Then that call is replaced with the appropriate `ISessionRepository` method; `ISessionRepository` is injected via the controller constructor

**AC-3 — `OfflineSessionHelper.cs` session read replaced**
Given `OfflineSessionHelper.cs` at approximately line 42 reads session state to determine offline eligibility via a direct GET
When this story is complete
Then that call is replaced with the appropriate `ISessionRepository` method; `ISessionRepository` is injected via the helper's constructor

**AC-4 — `AccountController.cs` confirmed not a CouchDB call — no change**
Given `AccountController.cs` at approximately line 664 calls `_couchDbHttpClient.ExecuteAsync("GET", ...)` with a `vitals_url`-based URL targeting `/api/systemOffline/GetSystemOfflineConfig` — a call to the mmria.services HTTP endpoint, not a CouchDB database
When this story begins
Then the developer confirms this is a service-to-service call and takes no action; a comment is added inline: `// Service endpoint call — not a CouchDB direct access. No repository routing needed.` if absent

**AC-5 — DI lifetime compatibility confirmed**
Given `CustomAuthHandler.cs` registers with ASP.NET Core's authorization pipeline and may have a specific DI lifetime
When this story is implemented
Then the developer confirms the lifetime of the auth handler before injecting `ISessionRepository`; if the handler is registered as `Transient` or `Scoped`, the repo injection is compatible; if it is registered as `Singleton`, a `IServiceScopeFactory` pattern is used to avoid captive dependency

**AC-6 — Build passes with zero errors**
Given the changes above
When the build runs
Then `mmria-server` builds with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/CustomAuthHandler.cs` | **UPDATE** — inject `ISessionRepository`; replace session GET with `GetSessionDocumentRawAsync` or `GetSessionDocumentAsync` |
| `source-code/mmria/mmria-server/Controllers/api/passwordChangeController.cs` | **UPDATE** — inject `ISessionRepository`; replace session event GET |
| `source-code/mmria/mmria-server/util/OfflineSessionHelper.cs` | **UPDATE** — inject `ISessionRepository`; replace session GET |
| `source-code/mmria/mmria-server/Controllers/AccountController.cs` | **NO CHANGE** — call is to service endpoint, not CouchDB; add comment if absent |

**`ISessionRepository` relevant methods:**
- `GetSessionDocumentAsync(string id, DBConfigurationDetail dbConfig)` → `session` (typed)
- `GetSessionDocumentRawAsync(string id, DBConfigurationDetail dbConfig)` → `string?` (raw JSON)
- `GetSessionEventsByUserIdAsync(string userName, DBConfigurationDetail dbConfig)` → (for session-event lookups)

**Session database access note:** The session database uses `{prefix}session/...`. Confirm `ISessionRepository` methods build the correct URL with `db_config.Get_Prefix_DB_Url("session/...")` (Pattern B) — this should already be correct from Story 23.2.

**Architecture rule reminder:** `HttpContext`, `User`, cookies, and request headers stay in the controller and auth handler. Only the CouchDB call site moves.
