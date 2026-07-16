# Story 19.3 — Define `IJurisdictionAuthorizationReader` and Route Auth Middleware

**Epic:** 19 — `jurisdiction` Consolidation (SQL Migration Foundation)
**Story ID:** 19.3
**Status:** done
**Date added:** 2026-07-15
**Depends on:** 19.1
**Source requirements:** epics.md §Epic 19 Story 19.3; project-context.md §2.2

---

## User Story

As a developer,
I want the per-request authorization view query against `jurisdiction` to be behind a dedicated read-only interface,
So that the auth middleware does not construct CouchDB URLs directly and the query can be swapped for a SQL implementation without touching authorization handler code.

---

## Acceptance Criteria

**AC-1 — `IJurisdictionAuthorizationReader` defined**
Given all six `authorization*.cs` files and `AuthorizationRoleCache.cs` query the same view: `jurisdiction/_design/sortable/_view/by_user_id`
When this story is complete
Then `IJurisdictionAuthorizationReader` is defined in `mmria.common/SharedLibraries/Jurisdiction/` with a single method: `Task<IReadOnlyList<JurisdictionRoleEntry>> GetRolesByUserIdAsync(string userId, DBConfigurationDetail dbConfig)` and a separate `JurisdictionAuthorizationDAL` implements it

**AC-2 — Separate class from `JurisdictionDAL`**
Given `JurisdictionAuthorizationDAL` is created
When it is implemented
Then it is a separate class from `JurisdictionDAL` — the auth read path is not mixed with application CRUD

**AC-3 — Auth handler files updated**
Given the six `authorization*.cs` handler files currently construct the URL directly
When this story is complete
Then each injects `IJurisdictionAuthorizationReader` and calls `GetRolesByUserIdAsync`; URL construction is removed from all six files

**AC-4 — `AuthorizationRoleCache` updated**
Given `AuthorizationRoleCache.cs` wraps the query with in-memory caching
When this story is complete
Then `AuthorizationRoleCache` injects `IJurisdictionAuthorizationReader`; cache management remains in `AuthorizationRoleCache` — not in the DAL

**AC-5 — PMSS files follow same pattern**
Given the PMSS split files (`authorization.pmss.cs`, `authorization_case.pmss.cs`, `authorization_user.pmss.cs`)
When they are updated
Then they follow the same pattern as their non-PMSS counterparts; no PMSS-specific divergence is introduced

**AC-6 — DAL is a thin wrapper**
Given this is the hot path for every authorized request
When the implementation is reviewed
Then `JurisdictionAuthorizationDAL.GetRolesByUserIdAsync` is a thin, non-caching HTTP wrapper — no business logic, no side effects

**AC-7 — DI registration**
Given `IJurisdictionAuthorizationReader` is registered in DI
When the server's service collection is updated
Then it is registered as `JurisdictionAuthorizationDAL` and is scoped appropriately for the authorization pipeline

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/Jurisdiction/IJurisdictionAuthorizationReader.cs` | **CREATE** — interface |
| `mmria.common/SharedLibraries/Jurisdiction/DAL/JurisdictionAuthorizationDAL.cs` | **CREATE** — implementation |
| `authorization.cs` | **UPDATE** — inject and use `IJurisdictionAuthorizationReader` |
| `authorization_case.cs` | **UPDATE** |
| `authorization_user.cs` | **UPDATE** |
| `authorization.pmss.cs` | **UPDATE** |
| `authorization_case.pmss.cs` | **UPDATE** |
| `authorization_user.pmss.cs` | **UPDATE** |
| `AuthorizationRoleCache.cs` | **UPDATE** — inject `IJurisdictionAuthorizationReader` |
| `mmria-server/Program.cs` | **UPDATE** — register `IJurisdictionAuthorizationReader` as `JurisdictionAuthorizationDAL` |

---

## Sequencing

Depends on 19.1. Independent of 19.2 — can proceed in parallel.
