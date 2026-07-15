# Story 21.4 — Route CaseWorkflowAdminDAL Audit Calls Through `IAuditRepository`

**Epic:** 21 — `audit` Consolidation (SQL Migration Foundation)
**Story ID:** 21.4
**Status:** ready-for-dev
**Date added:** 2026-07-15
**Depends on:** 21.2 **+ Epic 17 Story 17.4 done** (pre-condition)
**Source requirements:** epics.md §Epic 21 Story 21.4; project-context.md §2.2

---

## User Story

As a developer,
I want `CaseWorkflowAdminDAL`'s 4 direct audit calls to delegate to `IAuditRepository`,
So that the workflow-admin DAL no longer constructs audit URLs directly.

---

## ⚠️ Pre-condition

**This story must not be started until Epic 17 Story 17.4 (`17-4-caseworkflowadmindal-duplicates`) is `done`.** Epic 17 Story 17.4 modifies `CaseWorkflowAdminDAL.cs` and this story also modifies the same file. Running them in parallel would create a merge conflict.

---

## Acceptance Criteria

**AC-1 — All 4 `CaseWorkflowAdminDAL` audit calls replaced**
Given the following 4 audit calls in `CaseWorkflowAdminDAL.cs` (all Pattern B):
- Line 49: `WriteAuditEntryAsync` — PUT `audit/{auditEntry._id}`
- Line 57: `GetDeletedCasesViewAsync` — GET `audit/_design/sortable/_view/by_deleted`
- Line 67: `GetAuditDocumentAsync` — GET `audit/{auditId}`
- Line 92: `DeleteAuditDocumentAsync` — DELETE `audit/{auditId}?rev={rev}`

When this story is complete
Then each is replaced with the corresponding `IAuditRepository` method; `IAuditRepository` is injected into `CaseWorkflowAdminDAL` via constructor injection

**AC-2 — `_couchDbHttpClient` removed from `CaseWorkflowAdminDAL`**
Given `CaseWorkflowAdminDAL` after Epic 17 Story 17.4 already delegates mmrds calls to `ICaseRepository`
When this story is implemented
Then `CaseWorkflowAdminDAL` depends on both `ICaseRepository` and `IAuditRepository`; `_couchDbHttpClient` is removed from the class entirely (all its calls will have been moved to repository dependencies)

**AC-3 — Build succeeds**
Given the build after all changes
When verified
Then all three projects build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | **UPDATE** — inject `IAuditRepository`, replace 4 audit calls, remove `_couchDbHttpClient` |

---

## Sequencing

Depends on 21.2 **and** Epic 17 Story 17.4. 21.3, 21.5, and 21.6 can proceed independently once 21.2 is done.
