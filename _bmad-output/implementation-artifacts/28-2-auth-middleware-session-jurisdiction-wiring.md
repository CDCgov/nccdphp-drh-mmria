# Story 28.2 — Auth Middleware Session and Jurisdiction Wiring

**Epic:** 28 — mmria-server Non-DAL Remnants
**Story ID:** 28.2
**Status:** done
**Date added:** 2026-07-17
**Depends on:** Epic 23 story 23.2 (ISessionRepository), Epic 19 story 19.3 (IJurisdictionAuthorizationReader)
**Source requirements:** epics.md §Epic 28 Story 28.2; project-context.md §2.2

---

## User Story

As a developer,
I want `JurisdictionAuthorizationRequirement.cs` and `CustomAuthHandler.cs` to use existing repository interfaces instead of constructing CouchDB URLs directly,
So that the auth middleware pipeline has the same SQL migration seam as all other database-touching code.

---

## Acceptance Criteria

**AC-1 — `JurisdictionAuthorizationRequirement.cs` jurisdiction view call replaced**
Given `JurisdictionAuthorizationRequirement.cs` at approximately line 45 calls `_couchDbHttpClient.ExecuteAsync("POST", jurisdicion_view_url, ...)` to query the `jurisdiction/_design/sortable/_view/by_user_id` view
When this story is complete
Then that call is replaced with `IJurisdictionAuthorizationReader.GetRolesByUserIdAsync(userId, dbConfig)` (the interface established in Epic 19 Story 19.3); `IJurisdictionAuthorizationReader` is injected into the requirement handler via constructor injection; the handler uses the returned role entries to evaluate the claim as before

**AC-2 — `IJurisdictionAuthorizationReader` injection is DI-lifetime-safe**
Given `JurisdictionAuthorizationRequirement.cs` is registered in the ASP.NET Core authorization pipeline
When the service lifetime is evaluated
Then `IJurisdictionAuthorizationReader` is injected with a lifetime compatible with the handler's registration (Scoped or Transient, matching the existing `JurisdictionAuthorizationDAL` registration from Epic 19.3)

**AC-3 — `CustomAuthHandler.cs` session PUT replaced**
Given `CustomAuthHandler.cs` at approximately line 171 calls `_couchDbHttpClient.ExecuteAsync("PUT", request_string, session_message_json, ...)` to write a refreshed session expiration back to `session/{sid}`
When this story is complete
Then that PUT is replaced with the appropriate write method on `ISessionRepository`; `ISessionRepository` is injected into `CustomAuthHandler` via constructor injection

**AC-4 — No behavioral change in auth pipeline**
Given the auth middleware executes on every authorized request
When this story is implemented
Then the observable behavior of jurisdiction-role validation and session-expiration refresh is identical to pre-change; no new error-handling paths are added; the existing `try/catch` structure is preserved

**AC-5 — Build passes with zero errors**
Given the changes above
When the build runs
Then `mmria-server` builds with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/util/JurisdictionAuthorizationRequirement.cs` | **UPDATE** — inject `IJurisdictionAuthorizationReader`; replace `ExecuteAsync` POST with `GetRolesByUserIdAsync` |
| `source-code/mmria/mmria-server/CustomAuthHandler.cs` | **UPDATE** — inject `ISessionRepository`; replace `ExecuteAsync` PUT with write method |

**`IJurisdictionAuthorizationReader` context:** This interface was defined in Epic 19 Story 19.3 specifically for the auth middleware hot path. `JurisdictionAuthorizationRequirement.cs` is an `IAuthorizationRequirement` handler — it was not listed in Epic 19.3's explicit file list (which named the six `authorization*.cs` files and `AuthorizationRoleCache.cs`) but queries the same view and fits the same pattern. Use the same `GetRolesByUserIdAsync` method.

**`ISessionRepository` write method:** Story 23.2 established `ISessionRepository` with full session CRUD. The call in `CustomAuthHandler.cs` does a PUT to update the `date_expired` field on an existing session document. Confirm the write method signature (likely `UpdateSessionAsync(string sid, session_message session, DBConfigurationDetail dbConfig)` or a raw-JSON variant). Do NOT create a new interface method if an existing write method covers the operation.

**Finding the session write method:** Check `ISessionRepository` in `mmria.common/SharedLibraries/Session/ISessionRepository.cs` for a PUT/update method. The session document type is `session_message`. The document ID is the `sid` cookie value.
