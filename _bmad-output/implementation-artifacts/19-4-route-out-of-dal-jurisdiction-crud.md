---
baseline_commit: 237f8451bf01bf0edc07b8fdd4528f4000759805
---

# Story 19.4 — Route Out-of-DAL Application CRUD Through `IJurisdictionRepository`

**Epic:** 19 — `jurisdiction` Consolidation (SQL Migration Foundation)
**Story ID:** 19.4
**Status:** done
**Date added:** 2026-07-15
**Depends on:** 19.2
**Source requirements:** epics.md §Epic 19 Story 19.4; project-context.md §2.2

---

## User Story

As a developer,
I want all application-layer files that directly construct `jurisdiction` URLs outside of a DAL to delegate to `IJurisdictionRepository`,
So that the interface established in Story 19.2 is the only path for application jurisdiction CRUD.

---

## Acceptance Criteria

**AC-1 — All out-of-DAL call sites replaced**
Given the following direct `jurisdiction` HTTP calls outside of DAL files:
- `jurisdiction_treeController.cs` — 5 hits (tree document GET/PUT — Wave 8 planned migration target)
- `vitalsController.cs` — 4 hits (jurisdiction reads for vitals context)
- `_usersController.cs` — 2 hits (user-role-jurisdiction reads)
- `CaseViewManager.cs` — 5 hits (jurisdiction reads for case view filtering)
- `CaseViewSearch.pmss.cs` — 1 hit (PMSS variant of case view search)
- `JurisdictionSummary.cs` — 1 hit (actor-side jurisdiction read)
- `VROSummary.cs` — 1 hit (actor-side jurisdiction read)
- `SessionDAL.cs` — 1 hit (session-related jurisdiction read)
- `ManageUsersManager.cs` — 4 hits (any remaining direct construction after Story 19.2)

When this story is complete
Then each is replaced with the corresponding `IJurisdictionRepository` method; `IJurisdictionRepository` is injected via constructor injection in each class

**AC-2 — `jurisdiction_treeController` Wave 8 scope limited**
Given `jurisdiction_treeController.cs` is also a Wave 8 migration target (planned move to `JurisdictionTree` SharedLibrary)
When this story touches it
Then only the URL construction is replaced — the Wave 8 SharedLibraries extraction is deferred; this story does not restructure the controller's business logic

**AC-3 — Build succeeds with no route changes**
Given the build after all changes
When verified
Then all three projects build with zero errors and no route, action signature, or response shape changes are made

---

## Dev Notes — Files to Change

| File | Hits | Change |
|------|------|--------|
| `jurisdiction_treeController.cs` | 5 | **UPDATE** — URL construction → `IJurisdictionRepository` |
| `vitalsController.cs` | 4 | **UPDATE** |
| `_usersController.cs` | 2 | **UPDATE** |
| `CaseViewManager.cs` | 5 | **UPDATE** |
| `CaseViewSearch.pmss.cs` | 1 | **UPDATE** |
| `JurisdictionSummary.cs` | 1 | **UPDATE** (actor) |
| `VROSummary.cs` | 1 | **UPDATE** (actor) |
| `SessionDAL.cs` | 1 | **UPDATE** |
| `ManageUsersManager.cs` | ~4 | **UPDATE** — any remaining direct construction |

---

## Sequencing

Depends on 19.2. Independent of 19.3.
