# Story 21.1 — `audit` Operation Catalog

**Epic:** 21 — `audit` Consolidation (SQL Migration Foundation)
**Story ID:** 21.1
**Status:** ready-for-dev
**Date added:** 2026-07-15
**Depends on:** none — discovery only
**Source requirements:** epics.md §Epic 21 Story 21.1; project-context.md §2.2

---

## User Story

As a developer,
I want a definitive catalog of every operation against the `audit` database across all three projects,
So that Story 21.2 has an agreed-upon, complete operation set before any code changes begin.

---

## Acceptance Criteria

**AC-1 — `audit` section added to operation catalog**
Given all `.cs` files in `mmria-server`, `mmria.common`, and `mmria.services`
When the developer completes the catalog
Then `docs/ai/mmrds_operation_catalog.md` gains an `audit` section listing every distinct operation grouped into: audit entry writes (PUT `Change_Stack`), audit entry reads (GET by ID), audit view queries (`by_deleted`), Mango `_find` queries (`by case_id`), special document reads/writes (`audit-manage-user`), and bulk/delete operations

**AC-2 — Per-entry detail**
Given each catalog entry
When the catalog is complete
Then each entry records: operation name, calling file(s) with line number, URL pattern in use (A or B), and response type expected

**AC-3 — Infrastructure scoped out**
Given `c_db_setup.cs` references to `audit`
When evaluated
Then they are listed but marked **out of scope** — DB setup and security configuration are infrastructure operations

---

## Dev Notes — Scope Context (verified 2026-07-15)

| Location | # Calls | Layer | URL Pattern | Notes |
|---|---|---|---|---|
| `AuditRecoveryDAL.cs` | 3 | DAL ✓ | **A** (wrong) | GET by ID, GET audit-manage-user, PUT audit-manage-user |
| `CaseWorkflowAdminDAL.cs` | 4 | DAL ✓ | **B** (correct) | WriteAuditEntry, GetDeletedCasesView, GetAuditDoc, DeleteAuditDoc |
| `ManageUsersDAL.cs` | 1 | DAL ✓ | **A** (wrong) | GET audit-manage-user (duplicate of AuditRecoveryDAL) |
| `CaseManager.cs` | 6 | **Manager ✗** | **B** (correct) | All audit PUT (Change_Stack writes) — wrong layer |
| `AuditRecoveryManager.cs` | 1 | **Manager ✗** | **A** (wrong) | Builds `_find` URL directly in manager |
| `_auditController.cs` | 1 | **Controller ✗** | **A** (wrong) | `_find` by case_id — wrong layer |
| `AuditRecoverUtilController.cs` | 1 | **Controller ✗** | **A** (wrong) | `_find` — wrong layer |
| `caseController.pmss.cs` | 2 | **Controller ✗** | **B** (correct) | Audit PUT (Change_Stack writes) — wrong layer |
| `c_db_setup.cs` | 5 | Infra | — | DB setup/security — **out of scope** |

**Total in-scope: 19 calls.** 8 are already in the DAL layer but behind no interface. 11 are leaking out of the DAL.

---

## Sequencing

Discovery only. 21.2 is unblocked once this is complete.
