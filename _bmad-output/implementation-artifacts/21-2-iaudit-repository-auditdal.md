# Story 21.2 — Create `AuditDAL` and Extract `IAuditRepository`

**Epic:** 21 — `audit` Consolidation (SQL Migration Foundation)
**Story ID:** 21.2
**Status:** ready-for-dev
**Date added:** 2026-07-15
**Depends on:** 21.1
**Source requirements:** epics.md §Epic 21 Story 21.2; project-context.md §2.2

---

## User Story

As a developer,
I want a single `IAuditRepository` interface over all audit CRUD operations,
So that every caller can depend on the interface and a SQL migration requires changing only `AuditDAL`.

---

## Acceptance Criteria

**AC-1 — `Audit` SharedLibraries feature created**
Given no canonical `Audit` SharedLibraries feature exists
When this story creates one
Then the following structure exists:
```
mmria.common/SharedLibraries/Audit/
  IAuditRepository.cs
  DAL/
    AuditDAL.cs
```

**AC-2 — `AuditDAL` contains all in-scope operations**
Given the operation catalog from Story 21.1
When `AuditDAL` is created
Then it contains async methods for every in-scope operation, including at minimum:
- `WriteAuditEntryAsync(Change_Stack entry, DBConfigurationDetail dbConfig)`
- `GetAuditEntryAsync(string auditId, DBConfigurationDetail dbConfig)` → `Change_Stack`
- `DeleteAuditEntryAsync(string auditId, string rev, DBConfigurationDetail dbConfig)`
- `GetDeletedCasesViewAsync(DBConfigurationDetail dbConfig)` → `get_sortable_view_reponse_header<Audit_Detail_View>`
- `GetAuditManageUserAsync(DBConfigurationDetail dbConfig)` → `Audit_Manage_User?`
- `SaveAuditManageUserAsync(Audit_Manage_User doc, DBConfigurationDetail dbConfig)`
- `FindAuditsByCaseAsync(string caseId, DBConfigurationDetail dbConfig)` → `ChangeStackResult`

**AC-3 — Pattern B URL construction only**
Given all `AuditDAL` methods
When written
Then all use `dbConfig.Get_Prefix_DB_Url(...)` (Pattern B) — no `$"{dbConfig.url}/{dbConfig.prefix}audit/..."` string interpolations

**AC-4 — DI registration**
Given `IAuditRepository` is defined
When DI registration is updated in `mmria-server/Program.cs`
Then `services.AddScoped<IAuditRepository, AuditDAL>()` is present

**AC-5 — No callers changed yet**
Given no callers are changed in this story
When the build runs
Then `mmria-server`, `mmria.common`, and `mmria.services` build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/Audit/IAuditRepository.cs` | **CREATE** — interface |
| `mmria.common/SharedLibraries/Audit/DAL/AuditDAL.cs` | **CREATE** — implementation with all in-scope operations |
| `mmria-server/Program.cs` | **UPDATE** — `services.AddScoped<IAuditRepository, AuditDAL>()` |

**Design note:** The existing `AuditRecoveryDAL` is scoped to the audit recovery workflow. A new canonical `AuditDAL` is the correct home for all audit CRUD. After this epic, `AuditRecoveryDAL` becomes a workflow-specific DAL that delegates to `IAuditRepository`.

---

## Sequencing

Depends on 21.1. Once done, 21.3, 21.5, 21.6 can proceed in parallel. 21.4 also depends on this story but additionally requires Epic 17 Story 17.4 to be done.
