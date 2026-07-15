# Story 21.5 — Route Controller-Level Audit Calls Through `IAuditRepository`

**Epic:** 21 — `audit` Consolidation (SQL Migration Foundation)
**Story ID:** 21.5
**Status:** ready-for-dev
**Date added:** 2026-07-15
**Depends on:** 21.2
**Source requirements:** epics.md §Epic 21 Story 21.5; project-context.md §2.2

---

## User Story

As a developer,
I want all direct audit URL construction in controllers eliminated,
So that controllers never touch the audit database directly.

---

## Acceptance Criteria

**AC-1 — All controller audit call sites replaced**
Given the following direct audit calls in controller/util files:
- `_auditController.cs` line 107: `$"{db_config.url}/{db_config.prefix}audit/_find"` — builds `_find` URL in a private helper method, passes it to `AuditRecoveryManager`
- `AuditRecoverUtilController.cs` line 54: `$"{configuration.url}/{configuration.prefix}audit/_find"` — `_find` URL passed to a service
- `caseController.pmss.cs` line 261: `db_config.Get_Prefix_DB_Url($"audit/{audit_data._id}")` — audit PUT
- `caseController.pmss.cs` line 418: `db_config.Get_Prefix_DB_Url($"audit/{audit_data._id}")` — audit PUT

When this story is complete
Then all four call sites are replaced with `IAuditRepository` method calls; `IAuditRepository` is injected into each controller via constructor injection; no controller constructs an `audit/` URL

**AC-2 — `_auditController` `get_find_url()` helper removed**
Given `_auditController.cs` `get_find_url()` helper method (line ~90–110) that currently builds the `_find` URL tuple `(url, postData)` and passes both to `AuditRecoveryManager.GetAuditViewDataAsync`
When replaced
Then the URL construction is removed from the controller; `FindAuditsByCaseAsync` in `IAuditRepository` accepts the `caseId` directly and handles the `_find` POST internally; the manager receives the result, not the URL

**AC-3 — PMSS controller business logic preserved**
Given `caseController.pmss.cs` audit writes at lines 261 and 418
When replaced
Then only the CouchDB URL construction and `ExecuteAsync` calls move to the DAL — the surrounding PMSS business logic and error handling remain in the controller

**AC-4 — Build succeeds**
Given the build after all changes
When verified
Then all three projects build with zero errors

---

## Dev Notes — Files to Change

| File | Line(s) | Change |
|------|---------|--------|
| `_auditController.cs` | ~90–110 | **UPDATE** — inject `IAuditRepository`, remove `get_find_url()` helper, replace `_find` URL construction |
| `AuditRecoverUtilController.cs` | 54 | **UPDATE** — inject `IAuditRepository`, replace `_find` URL construction |
| `caseController.pmss.cs` | 261, 418 | **UPDATE** — inject `IAuditRepository`, replace audit PUT calls |

---

## Sequencing

Depends on 21.2. Can proceed in parallel with 21.3 and 21.6.
