# Story 17.7 — MMRIAServicesDAL and Sync Boundary Decision

**Epic:** 17 — mmrds CRUD Consolidation (SQL Migration Foundation)
**Story ID:** 17.7
**Status:** ready-for-dev
**Date added:** 2026-07-14
**Depends on:** 17.1 (mmrds catalog — can also run in parallel with 17.1)
**Source requirements:** epics.md §Epic 17 Story 17.7; project-context.md §2.2

---

## User Story

As a developer,
I want a written architecture decision on whether the CDC populate path and bulk sync operations in `MMRIAServicesDAL` and `c_document_sync_all` should be unified with `ICaseRepository` or formally declared as separate infrastructure concerns,
So that the boundary is explicit and future contributors do not try to merge them incorrectly.

---

## Acceptance Criteria

**AC-1 — Boundary decision document written**
Given `MMRIAServicesDAL` has a private `GetMmrdsDatabaseUrl()` helper that uses a different prefix separator convention from `Get_Prefix_DB_Url`
When the developer evaluates the CDC populate path
Then a decision is recorded in `docs/ai/mmrds_operation_catalog.md` under the "Boundary Decisions" section, choosing one of:
- **(a) Unify** — fix prefix inconsistency in `MMRIAServicesDAL`, route its regular CRUD calls through `ICaseRepository`
- **(b) Separate concerns** — formally declare the CDC bulk path as a separate infrastructure concern that `ICaseRepository` does not cover

**AC-2 — c_document_sync_all decision recorded**
Given `c_document_sync_all` in both `mmria-server` and `mmria.services` uses bulk `_all_docs` for change-feed synchronization
When the developer evaluates it
Then the same decision document records whether sync bulk reads belong behind `ICaseRepository` or remain as infrastructure-only operations

**AC-3 — If unify (option a): prefix bug fixed**
Given the decision document recommends unification
When it is implemented
Then the `GetMmrdsDatabaseUrl()` helper in `MMRIAServicesDAL` is replaced with `Get_Prefix_DB_Url`; lines 307, 335, 359 use Pattern B; a follow-on story is created if full interface adoption requires additional work beyond this story's scope

**AC-4 — If separate concerns (option b): calls marked**
Given the decision document recommends keeping as separate concerns
When it is implemented
Then no code changes are made to `MMRIAServicesDAL` or `c_document_sync_all` in this epic; a code comment is added to `GetMmrdsDatabaseUrl()` noting "Infrastructure — CDC populate path; out of scope per Epic 17"

**AC-5 — Build succeeds**
Given any code changes made in this story
When the build runs
Then all three projects build with zero errors

---

## Dev Notes — Architecture Analysis

### The inconsistency to decide on

`MMRIAServicesDAL` has a private helper (lines 553–557):

```csharp
private static string GetMmrdsDatabaseUrl(DBConfigurationDetail dbInfo)
{
    return string.IsNullOrWhiteSpace(dbInfo?.prefix)
        ? $"{dbInfo?.url}/mmrds"
        : $"{dbInfo.url}/{dbInfo.prefix}_mmrds";  // ← underscore separator
}
```

Compare to `Get_Prefix_DB_Url`:
```csharp
// Produces: {url}/{prefix}mmrds/{path}   (no separator between prefix and "mmrds")
dbConfig.Get_Prefix_DB_Url("mmrds/xxx")
```

When a prefix is present:
- `GetMmrdsDatabaseUrl` → `{url}/{prefix}_mmrds` (underscore between prefix and "mmrds")
- `Get_Prefix_DB_Url("mmrds")` → `{url}/{prefix}mmrds` (no separator)

These point to **different CouchDB database names**. This is not just a code style issue — it is a functional difference in which database is accessed.

---

### All mmrds call sites in MMRIAServicesDAL (verified 2026-07-14)

| Line | Call type | URL produced | Notes |
|------|-----------|-------------|-------|
| 27 | Pattern A direct | `{url}/{prefix}mmrds/_design/.../by_last_name` | No separator |
| 79 | Pattern A direct | `{url}/{prefix}mmrds/_all_docs` | No separator |
| 83 | Pattern A direct | `{url}/{prefix}mmrds/{case_id}` | No separator |
| 189 | Pattern A direct | `{url}/{prefix}mmrds/_design/.../by_date_created` | No separator |
| 307 | Via `GetMmrdsDatabaseUrl` helper | `{url}/{prefix}_mmrds/_design/.../by_date_created` | **Underscore separator** |
| 335 | Via `GetMmrdsDatabaseUrl` helper | `{url}/{prefix}_mmrds/{caseId}` | **Underscore separator** |
| 359 | Via `GetMmrdsDatabaseUrl` helper | `{url}/{prefix}_mmrds/_all_docs` | **Underscore separator** |
| 468 | CDC write path | `{cdcConnection.url}/mmrds/_bulk_docs` | Different connection (`cdcConnection`, not `dbInfo`) |

Lines 27, 79, 83, 189 are inconsistent even with each other — they mix Pattern A with the helper. The helper itself is inconsistent with both Pattern A and Pattern B.

---

### Questions to answer in the decision

1. **Does the CDC populate path actually target a different database?** If `{prefix}_mmrds` and `{prefix}mmrds` are two different CouchDB databases with different purposes, then keeping them separate is correct. If they should be the same database and the underscore is a bug, then it must be fixed.

2. **What are lines 307, 335, 359 used for?** Read the calling methods at those lines to understand if they are CDC populate operations or regular application CRUD.

3. **Is line 468 (`cdcConnection.url/mmrds/_bulk_docs`) the actual CDC write?** If yes, this is a separate connection entirely (a different CouchDB instance), not the same database as application CRUD.

---

### Recommended default position

The underscore convention in `GetMmrdsDatabaseUrl` was likely intentional for a specific CDC scenario. **Recommend option (b)** unless investigation proves the `{prefix}_mmrds` database names are wrong. The CDC populate path is a bulk, infra-level concern that should not go through the same interface as application CRUD.

Document the reasoning in `docs/ai/mmrds_operation_catalog.md` under "Boundary Decisions" so future contributors understand why `MMRIAServicesDAL` is excluded from `ICaseRepository`.
