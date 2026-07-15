# Story 18.1 — `_users` Operation Catalog

**Epic:** 18 — `_users` and `configuration` Consolidation (SQL Migration Foundation)
**Story ID:** 18.1
**Status:** ready-for-dev
**Date added:** 2026-07-14
**Depends on:** none — discovery only
**Source requirements:** epics.md §Epic 18 Story 18.1

---

## User Story

As a developer,
I want a definitive catalog of every operation against the `_users` database across all three projects,
So that Story 18.2 has an agreed-upon, complete operation set before any code changes begin.

---

## Acceptance Criteria

**AC-1 — Catalog section added to existing document**
Given `docs/ai/mmrds_operation_catalog.md` already exists from Story 17.1
When this story is complete
Then a `_users` section is appended to that document containing a table of every distinct operation grouped into: user GET by ID, user PUT/POST (create or update), user DELETE, paginated user list (`_all_docs`), password-related operations, and role/group reads

**AC-2 — Each entry records calling context**
Given each catalog entry
When the catalog is complete
Then each entry records: operation name, calling file(s) with line number, URL construction used, and response type expected

**AC-3 — Infra/setup operations marked out of scope**
Given `c_db_setup.cs` and `Check_DB_Install.cs` references to `_users`
When they are evaluated
Then they are listed in a separate "Infrastructure / Out of Scope" section and marked accordingly

**AC-4 — ManageUsersDAL call assessment included**
Given `ManageUsersDAL.cs` contains 5 `_users` calls (GET, GET-check, PUT, DELETE, `_all_docs`) that duplicate `AccountDAL` operations
When the catalog is written
Then these 5 calls are listed with a note on which represent generic user CRUD (candidates for `IUserRepository`) vs. manage-users-workflow-specific operations

---

## Dev Notes — Implementation

### Output file

Append a `## _users Operations` section to: `docs/ai/mmrds_operation_catalog.md`

No code changes — discovery and documentation only.

---

### Known call sites (verified 2026-07-14)

#### In DAL files (already correct layer)

| File | Line | Operation | URL | Response Type |
|------|------|-----------|-----|---------------|
| `AccountDAL.cs` | 47 | GET user by doc ID | `$"{dbConfig.url}/_users/{HtmlEncode(userDocId)}"` | `user` |
| `ManageUsersDAL.cs` | 41 | GET user by user_id | `db_config.url + "/_users/" + user_id` | `user` |
| `ManageUsersDAL.cs` | 58 | GET user (check-exists) | `db_config.url + "/_users/" + user_id` | `user` (empty on not-found) |
| `ManageUsersDAL.cs` | 93 | PUT user (create/update) | `db_config.url + "/_users/" + user._id` | `document_put_response` |
| `ManageUsersDAL.cs` | 115 | DELETE user | `db_config.url + "/_users/" + user_id + "?rev=" + rev` | `ExpandoObject` |
| `ManageUsersDAL.cs` | 144 | GET all users (paginated) | `$"{db_config.url}/_users/_all_docs?include_docs=true&skip={skip}&limit={take}"` | `get_response_header<user>` |

#### Out-of-DAL leaking calls (target for Story 18.3)

| File | Line | Operation | URL | HTTP Verb |
|------|------|-----------|-----|-----------|
| `AccountController.OIDC.cs` | 255 | GET user (OIDC lookup) | `$"{config_couchdb_url}/_users/{Uri.EscapeDataString("org.couchdb.user:" + email)}"` | GET |
| `AccountController.OIDC.cs` | 301 | PUT user (OIDC provision) | `$"{config_couchdb_url}/_users/{Uri.EscapeDataString(user._id)}"` | PUT |
| `passwordChangeController.cs` | 137 | GET+PUT user (password change) | `db_config.url + "/_users/org.couchdb.user:" + userName` | GET then PUT |
| `JurisdictionSummary.cs` | 268 | GET all users | `$"{p_config_detail.url}/_users/_all_docs?include_docs=true&skip=1"` | GET |
| `VROSummary.cs` | 265 | GET all users | `$"{p_config_detail.url}/_users/_all_docs?include_docs=true&skip=1"` | GET |

#### Infra / Out of Scope

| File | Operation | Reason |
|------|-----------|--------|
| `c_db_setup.cs` | `_users` admin setup | One-time DB initialization |
| `Check_DB_Install.cs` | `_users` health check | Startup health check |

---

### Catalog document structure

```markdown
## _users Operations

### In-Scope Operations

#### User CRUD (GET/PUT/DELETE by ID)
| Operation | Calling File(s) | Line(s) | URL Pattern | Response Type |
...

#### User List Queries
...

### Infrastructure / Out of Scope
...

### Boundary Decisions
_Placeholder — no known boundary decisions for _users_
```
