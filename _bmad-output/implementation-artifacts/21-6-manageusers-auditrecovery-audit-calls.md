# Story 21.6 — Route `ManageUsersDAL` and `AuditRecoveryDAL` Through `IAuditRepository`

**Epic:** 21 — `audit` Consolidation (SQL Migration Foundation)
**Story ID:** 21.6
**Status:** ready-for-dev
**Date added:** 2026-07-15
**Depends on:** 21.2
**Source requirements:** epics.md §Epic 21 Story 21.6; project-context.md §2.2

---

## User Story

As a developer,
I want `ManageUsersDAL` and `AuditRecoveryDAL` to delegate their audit operations to `IAuditRepository`,
So that no DAL outside `AuditDAL` constructs audit URLs directly.

---

## Acceptance Criteria

**AC-1 — `ManageUsersDAL` audit call replaced**
Given `ManageUsersDAL.cs` line 165 — `$"{db_config.url}/{db_config.prefix}audit/audit-manage-user"` (GET `Audit_Manage_User`, Pattern A)
When this story is complete
Then the call is replaced with `IAuditRepository.GetAuditManageUserAsync(db_config)`; `IAuditRepository` is injected into `ManageUsersDAL`

**AC-2 — `AuditRecoveryDAL` audit calls replaced**
Given `AuditRecoveryDAL.cs` lines 39, 53, 70 (all Pattern A):
- Line 39: GET `audit/{changeId}` → `Change_Stack`
- Line 53: GET `audit/audit-manage-user` → `Audit_Manage_User`
- Line 70: PUT `audit/{auditDocument._id}` → `document_put_response`

When this story is complete
Then all three are replaced with the corresponding `IAuditRepository` methods; `AuditRecoveryDAL` injects `IAuditRepository` instead of calling `_couchDbHttpClient` for audit operations

**AC-3 — `AuditRecoveryManager` `_find` URL construction removed**
Given `AuditRecoveryManager.cs` line 158 — builds `_find` URL directly as `$"{db_config.url}/{db_config.prefix}audit/_find"` and returns it as a tuple to be passed back to the DAL
When this story is complete
Then the `_find` URL construction is removed from `AuditRecoveryManager`; the manager calls `IAuditRepository.FindAuditsByCaseAsync(caseId, dbConfig)` directly and receives the result; `IAuditRepository` is injected into `AuditRecoveryManager`

**AC-4 — `AuditRecoveryDAL` `_couchDbHttpClient` audit calls fully removed**
Given the build after all changes
When verified
Then all three projects build with zero errors; `AuditRecoveryDAL` no longer holds any direct `_couchDbHttpClient` audit calls (its `_couchDbHttpClient` field may be removed entirely if no other calls remain)

---

## Dev Notes — Files to Change

| File | Line(s) | Change |
|------|---------|--------|
| `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | 165 | **UPDATE** — inject `IAuditRepository`, replace Pattern A GET |
| `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | 39, 53, 70 | **UPDATE** — inject `IAuditRepository`, replace all 3 Pattern A calls; evaluate removing `_couchDbHttpClient` |
| `mmria.common/SharedLibraries/AuditRecovery/Manager/AuditRecoveryManager.cs` | 158 | **UPDATE** — inject `IAuditRepository`, replace `_find` URL construction, call `FindAuditsByCaseAsync` directly |

---

## Sequencing

Depends on 21.2. Can proceed in parallel with 21.3 and 21.5.
