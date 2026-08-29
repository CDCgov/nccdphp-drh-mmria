# Story 21.3 — Route CaseManager Audit Writes Through `IAuditRepository`

**Epic:** 21 — `audit` Consolidation (SQL Migration Foundation)
**Story ID:** 21.3
**Status:** done
**Date added:** 2026-07-15
**Depends on:** 21.2
**Source requirements:** epics.md §Epic 21 Story 21.3; project-context.md §2.2

---

## User Story

As a developer,
I want `CaseManager`'s 6 direct audit write calls to delegate to `IAuditRepository`,
So that audit access in the case manager layer follows the Manager → DAL boundary.

---

## Acceptance Criteria

**AC-1 — All 6 `CaseManager` audit writes replaced**
Given the following 6 direct audit PUT calls in `CaseManager.cs` (all using `Get_Prefix_DB_Url`, Pattern B):
- Line 318: `auditDbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}")`
- Line 537: `auditDbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}")`
- Line 1180: `dbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}")`
- Line 1330: `dbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}")`
- Line 1831: `dbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}")`
- Line 2330: `dbConfig.Get_Prefix_DB_Url($"audit/{audit_data._id}")`

When this story is complete
Then each is replaced with `IAuditRepository.WriteAuditEntryAsync(changeStack, dbConfig)`; `IAuditRepository` is injected into `CaseManager` via constructor injection

**AC-2 — Both repository dependencies registered**
Given `CaseManager` will now depend on both `ICaseRepository` and `IAuditRepository`
When DI registration is updated
Then both dependencies are registered and `CaseManager` resolves correctly

**AC-3 — No controller changes required**
Given the build after all changes
When verified
Then all three projects build with zero errors and no controller code changes are required

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` | **UPDATE** — inject `IAuditRepository`, replace 6 audit PUT calls |
| `mmria-server/Program.cs` | **VERIFY** — `IAuditRepository` already registered from 21.2; `CaseManager` registration resolves both dependencies |

---

## Sequencing

Depends on 21.2. Can proceed in parallel with 21.5 and 21.6.
