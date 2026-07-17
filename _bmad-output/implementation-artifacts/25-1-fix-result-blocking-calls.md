# Story 25.1 — Fix `.Result` Blocking Calls

**Epic:** 25 — Async Safety + Metadata Reader Consolidation
**Story ID:** 25.1
**Status:** ready-for-dev
**Date added:** 2026-07-17
**Depends on:** None
**Source requirements:** epics.md §Epic 25 Story 25.1; non-DAL boundary analysis 2026-07-16

---

## User Story

As a developer,
I want `JurisdictionAuthorizationRequirement.cs` and `VROSummary.cs` to use `await` instead of `.Result` for their CouchDB calls,
So that these request-path methods cannot deadlock the ASP.NET thread pool under concurrent load.

---

## Acceptance Criteria

**AC-1 — `JurisdictionAuthorizationRequirement.cs` made async**
Given `JurisdictionAuthorizationRequirement.cs` at approximately line 45 calls `_couchDbHttpClient.ExecuteAsync("POST", jurisdicion_view_url, ...).Result` — a synchronous block on an async call inside an ASP.NET authorization handler
When this story is complete
Then the call site uses `await _couchDbHttpClient.ExecuteAsync(...)` and the enclosing method signature is compatible with async execution (e.g., `async Task` where the interface permits); the authorization behavior — read jurisdiction view, evaluate result, succeed/fail requirement — is identical to pre-change

**AC-2 — `VROSummary.cs` blocking calls removed**
Given `VROSummary.cs` calls `_couchDbHttpClient.ExecuteAsync(...).Result` at approximately line 188 (and any adjacent `.Result` calls in the same class)
When this story is complete
Then every `.Result` call is replaced with `await`; all enclosing methods that contain these calls are made `async`; callers are updated to `await` as needed so async propagates correctly up the call chain; no behavior change occurs

**AC-3 — No new try/catch added**
Given the project convention of not adding outer try/catch in methods where none existed
When this story is implemented
Then no new outer try/catch blocks are introduced around the awaited calls; existing error-handling patterns are preserved unchanged

**AC-4 — Build passes with zero errors**
Given the changes above
When the build runs
Then `mmria-server` builds with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/util/JurisdictionAuthorizationRequirement.cs` | **UPDATE** — make enclosing method async; replace `.Result` with `await` |
| `source-code/mmria/mmria-server/util/VROSummary.cs` | **UPDATE** — make enclosing method(s) async; replace all `.Result` with `await`; update callers |

**Key implementation notes:**

- `.Result` on a `Task` inside an ASP.NET synchronized context deadlocks the thread pool when all threads are busy. Making the method `async` and using `await` is the correct fix — **not** `Task.Run(() => ...)` and **not** `.ConfigureAwait(false)`.
- `JurisdictionAuthorizationRequirement.cs` implements an ASP.NET Core `IAuthorizationRequirement` handled by a corresponding handler. The handler's `HandleRequirementAsync` method already returns `Task` — async propagation should be clean.
- `VROSummary.cs` is a utility class. Confirm what calls `VROSummary` — if a controller action calls it, that action must also be made `async Task<IActionResult>` if it isn't already.
- Do NOT add `ConfigureAwait(false)` — the project does not use it consistently and it is out of scope.
- After making methods async, run the build immediately to catch any compile errors from callers that need to be updated.
