# Story 19.2 — Define `IJurisdictionRepository` and Create `JurisdictionDAL`

**Epic:** 19 — `jurisdiction` Consolidation (SQL Migration Foundation)
**Story ID:** 19.2
**Status:** done
**Date added:** 2026-07-15
**Depends on:** 19.1
**Source requirements:** epics.md §Epic 19 Story 19.2; project-context.md §2.2

---

## User Story

As a developer,
I want a single `IJurisdictionRepository` interface over all application-layer `jurisdiction` CRUD operations,
So that every feature manager depends on the interface and not on CouchDB URL construction.

---

## Acceptance Criteria

**AC-1 — `JurisdictionDAL` created**
Given `ManageUsersDAL` currently owns `jurisdiction` CRUD (8 hits)
When this story is complete
Then `mmria.common/SharedLibraries/Jurisdiction/DAL/JurisdictionDAL.cs` is created containing all in-scope jurisdiction CRUD operations; `IJurisdictionRepository` is defined in the same `Jurisdiction` feature directory; `JurisdictionDAL` implements `IJurisdictionRepository`

**AC-2 — `ManageUsersDAL` delegates to interface**
Given `ManageUsersDAL` currently duplicates jurisdiction operations
When `JurisdictionDAL` is created
Then `ManageUsersDAL` is refactored to inject `IJurisdictionRepository` and delegate — it does not duplicate the implementation

**AC-3 — Interface covers full scope**
Given jurisdiction operations belonging to other features (session, case view, jurisdiction tree)
When the interface is scoped
Then `IJurisdictionRepository` covers all jurisdiction document types — user-role-jurisdiction docs, jurisdiction tree, vitals-related reads — so that a single interface is the SQL migration seam for the whole database

**AC-4 — DI registration**
Given `IJurisdictionRepository` is defined
When DI registration is updated in `mmria-server`
Then `IJurisdictionRepository` is registered as `JurisdictionDAL` in the server's service collection

**AC-5 — Build succeeds**
Given the changes are complete
When `dotnet build` runs
Then `mmria-server`, `mmria.common`, and `mmria.services` all build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/Jurisdiction/IJurisdictionRepository.cs` | **CREATE** — interface |
| `mmria.common/SharedLibraries/Jurisdiction/DAL/JurisdictionDAL.cs` | **CREATE** — implementation |
| `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | **UPDATE** — inject `IJurisdictionRepository`, delegate jurisdiction calls |
| `mmria-server/Program.cs` | **UPDATE** — register `IJurisdictionRepository` as `JurisdictionDAL` |

---

## Sequencing

Depends on 19.1. Can proceed in parallel with 19.3 after 19.1 is done. 19.4 depends on this story.
