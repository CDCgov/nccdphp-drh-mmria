# Story 23.2 — `ISessionRepository` over `SessionDAL`

**Epic:** 23 — Remaining Database Consolidation Gap Analysis (SQL Migration Foundation)
**Story ID:** 23.2
**Status:** done
**Date added:** 2026-07-16
**Depends on:** 23.1
**Source requirements:** epics.md §Epic 23 Story 23.2; project-context.md §2.2

---

## User Story

As a developer,
I want a single `ISessionRepository` interface over all `session` database operations, with `SessionDAL` as the sole canonical implementation,
So that every caller depends on the interface and a SQL session-store migration requires changing only `SessionDAL`.

---

## Acceptance Criteria

**AC-1 — SessionDAL URL canonicalization**
Given `SessionDAL` currently uses Pattern A (`$"{dbConfig.url}/{dbConfig.prefix}session/{id}"`) for all CRUD methods
When this story is complete
Then all `SessionDAL` methods use `dbConfig.Get_Prefix_DB_Url($"session/{...}")` (Pattern B) throughout — no direct string interpolation remains

**AC-2 — `ISessionRepository` interface extracted**
Given the full operation set in `SessionDAL`
When the interface is extracted
Then `ISessionRepository` is defined in `mmria.common/SharedLibraries/Session/` with async method signatures matching every `SessionDAL` method; `SessionDAL` implements `ISessionRepository`

**AC-3 — DI registration**
Given `ISessionRepository` is defined
When DI registration is updated in `mmria-server/Program.cs`
Then `ISessionRepository` is registered as `SessionDAL` in the service collection

**AC-4 — SessionManager leaks routed**
Given `SessionManager.cs` has 2 direct `session/` URL constructions (Pattern A session document writes in the Manager layer)
When this story is complete
Then each is replaced with the corresponding `ISessionRepository` method; `ISessionRepository` is injected into `SessionManager` via constructor injection

**AC-5 — Controller and actor leaks routed**
Given the following direct `session/` calls outside the Session feature:
- `AccountController.cs` — 1 hit (session document DELETE on logout)
- `AccountController.OIDC.cs` — 1 hit (session document PUT on OIDC login)
- `Post_Session_Actor.cs` — 1 hit (session document PUT via actor)
- `Record_Session_Event.cs` — 1 hit (session event document PUT via actor)
- `SessionSummary.cs` — 1 hit (session view GET for summary page)
When this story is complete
Then each is replaced with the corresponding `ISessionRepository` method; `ISessionRepository` is injected into each class via constructor injection or Akka.NET actor props factory as appropriate

**AC-6 — AccountDAL cross-feature session calls routed**
Given `AccountDAL.cs` has 4 session-database calls (all already Pattern B):
- Line ~323: session-event sortable view GET by user ID
- Lines ~374, 403, 431: session document GET, GET, DELETE
When this story is complete
Then `AccountDAL` injects `ISessionRepository` and delegates those 4 calls to it; `AccountDAL` constructs no `session/` URLs directly

**AC-7 — Build passes**
Given the build after all changes
When verified
Then `mmria-server`, `mmria.common`, and `mmria.services` all build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/Session/ISessionRepository.cs` | **CREATE** — interface |
| `mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs` | **UPDATE** — fix all Pattern A URLs to Pattern B; implement `ISessionRepository` |
| `mmria.common/SharedLibraries/Session/Manager/SessionManager.cs` | **UPDATE** — inject `ISessionRepository`; replace 2 direct `session/` URL constructions |
| `mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs` | **UPDATE** — inject `ISessionRepository`; replace 4 cross-feature session calls |
| `mmria-server/Controllers/AccountController.cs` | **UPDATE** — inject `ISessionRepository`; replace 1 direct session DELETE |
| `mmria-server/Controllers/AccountController.OIDC.cs` | **UPDATE** — inject `ISessionRepository`; replace 1 direct session PUT |
| `mmria-server/model/actor/Post_Session_Actor.cs` | **UPDATE** — inject `ISessionRepository` via actor props factory; replace 1 direct session PUT |
| `mmria-server/model/actor/Record_Session_Event.cs` | **UPDATE** — inject `ISessionRepository` via actor props factory; replace 1 direct session PUT |
| `mmria-server/util/SessionSummary.cs` | **UPDATE** — inject `ISessionRepository`; replace 1 direct session view GET |
| `mmria-server/Program.cs` | **UPDATE** — add `services.AddScoped<ISessionRepository, SessionDAL>()` |

**Design notes:**
- `SessionDAL` already owns the majority of session operations. The interface extraction is mechanical — no new operations need to be added to `SessionDAL`, only the URL pattern needs fixing.
- `AccountDAL` session calls are cross-feature: they already use Pattern B (`dbConfig.Get_Prefix_DB_Url($"session/...")`), so no URL fix is needed there — only the delegation wiring.
- Akka.NET actors (`Post_Session_Actor`, `Record_Session_Event`): inject `ISessionRepository` via the actor constructor or props factory following the same pattern used in other actors that already accept repository dependencies.
- This story carries the highest risk in Epic 23 due to the spread of call sites across actors, controllers, and a cross-feature DAL.

---

## Sequencing

Depends on 23.1. Can proceed in parallel with 23.3, 23.4, 23.5, 23.6, 23.8.

---

## Dev Agent Record

**Completed:** 2026-07-16  
**Build result:** mmria.common — 0 errors, mmria-server — 0 errors

**Completion Notes:**
- Created `ISessionRepository` with all `SessionDAL` method signatures plus two additional methods: `GetSessionDocumentRawAsync` (raw JSON GET, preserves `role_list` for Logout) and `DeleteSessionAsync`.
- `SessionDAL` implements `ISessionRepository` throughout with Pattern B URLs.
- `SessionManager` constructor changed from `SessionDAL` to `ISessionRepository`; Pattern A `request_string` leaks in `PostSessionAsync` and `PostSessionDocumentAsync` removed.
- `AccountDAL` now injects `ISessionRepository`; all 4 cross-feature session calls delegate to the repository.
- `AccountController.cs` injects `ISessionRepository`; Logout GET fixed (Pattern A → `GetSessionDocumentRawAsync`); Profile session-events view fixed (Pattern B → `GetSessionEventsByUserIdAsync`).
- `AccountController.OIDC.cs` injects `ISessionRepository`; OIDC login session PUT fixed (Pattern A → `SaveSessionRawAsync`).
- `Post_Session_Actor` constructor changed to `ISessionRepository`; GET and PUT routed through repository.
- `Record_Session_Event` constructor changed to `ISessionRepository`; session-event PUT routed through `SaveSessionEventAsync`.
- `SessionSummary` constructor updated with `ISessionRepository`; `GetSessionCount` uses `GetSessionByDateCreatedViewAsync`.
- `sessionSummaryController` injects `ISessionRepository` and passes to `SessionSummary`.
- `Program.cs`: Added `services.AddScoped<ISessionRepository, SessionDAL>()`.

**Changed Files:**
- `mmria.common/SharedLibraries/Session/ISessionRepository.cs` — CREATED
- `mmria.common/SharedLibraries/Session/DAL/SessionDAL.cs` — UPDATED
- `mmria.common/SharedLibraries/Session/Manager/SessionManager.cs` — UPDATED
- `mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs` — UPDATED
- `mmria-server/Controllers/AccountController.cs` — UPDATED
- `mmria-server/Controllers/AccountController.OIDC.cs` — UPDATED
- `mmria-server/model/actor/Post_Session_Actor.cs` — UPDATED
- `mmria-server/model/actor/Record_Session_Event.cs` — UPDATED
- `mmria-server/util/SessionSummary.cs` — UPDATED
- `mmria-server/Controllers/sessionSummaryController.cs` — UPDATED
- `mmria-server/Program.cs` — UPDATED
