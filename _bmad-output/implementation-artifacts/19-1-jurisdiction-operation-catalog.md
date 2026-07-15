# Story 19.1 — `jurisdiction` Operation Catalog

**Epic:** 19 — `jurisdiction` Consolidation (SQL Migration Foundation)
**Story ID:** 19.1
**Status:** done
**Date added:** 2026-07-15
**Depends on:** none — discovery only
**Source requirements:** epics.md §Epic 19 Story 19.1; project-context.md §2.2

---

## User Story

As a developer,
I want a definitive catalog of every operation against the `jurisdiction` database,
So that Stories 19.2–19.4 have an agreed-upon, complete operation set before any code changes begin.

---

## Acceptance Criteria

**AC-1 — `jurisdiction` section added to operation catalog**
Given all `.cs` files in `mmria-server`, `mmria.common`, and `mmria.services`
When the developer completes the catalog
Then `docs/ai/mmrds_operation_catalog.md` gains a `jurisdiction` section listing every distinct operation grouped into: user-role-jurisdiction document CRUD, jurisdiction tree document CRUD, vitals-related jurisdiction reads, session-related jurisdiction reads, authorization view queries, and bulk/admin operations

**AC-2 — Per-entry detail**
Given each catalog entry
When the catalog is complete
Then each entry records: operation name, calling file(s), URL pattern in use, response type, and whether it belongs to `IJurisdictionRepository` or `IJurisdictionAuthorizationReader`

**AC-3 — Infrastructure operations scoped out**
Given `c_db_setup.cs` references to `jurisdiction`
When evaluated
Then they are listed but marked **out of scope** — DB setup is not application CRUD

---

## Dev Notes — Scope Context

| Category | Files | Hits | Notes |
|---|---|---|---|
| Already in a DAL/Manager | `ManageUsersDAL`, `ManageUsersManager`, `SessionDAL` | 13 | Partial coverage — DAL methods exist but no interface |
| Application CRUD (out-of-DAL) | `jurisdiction_treeController`, `vitalsController`, `_usersController`, `CaseViewManager`, `CaseViewSearch.pmss`, `JurisdictionSummary`, `VROSummary` | 15 | Mix of controllers, managers, and actors |
| Auth middleware (hot path) | `authorization.cs`, `authorization_case.cs`, `authorization_user.cs`, `authorization.pmss.cs`, `authorization_case.pmss.cs`, `authorization_user.pmss.cs`, `AuthorizationRoleCache.cs`, `JurisdictionAuthorizationRequirement.cs` | 11 | Runs on every authorized request — dedicated `IJurisdictionAuthorizationReader` interface |
| Infra/out-of-scope | `c_db_setup.cs` | 5 | DB setup only |

**Two-interface design:** Auth middleware exclusively queries `jurisdiction/_design/sortable/_view/by_user_id`. This is architecturally distinct from application CRUD — it is a high-frequency, read-only authorization lookup. Two interfaces are required:

- **`IJurisdictionRepository`** — full CRUD for application features
- **`IJurisdictionAuthorizationReader`** — single `GetRolesByUserIdAsync` used exclusively by auth middleware

---

## Sequencing

19.1 is a discovery-only story. 19.2 and 19.3 are unblocked once this is complete and can proceed in parallel.
