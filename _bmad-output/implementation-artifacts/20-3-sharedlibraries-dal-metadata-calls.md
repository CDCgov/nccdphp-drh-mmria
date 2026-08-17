# Story 20.3 — Route SharedLibraries DAL Files Through `IMetadataRepository`

**Epic:** 20 — `metadata` Consolidation (SQL Migration Foundation)
**Story ID:** 20.3
**Status:** done
**Date added:** 2026-07-15
**Depends on:** 20.2
**Source requirements:** epics.md §Epic 20 Story 20.3; project-context.md §2.2

---

## User Story

As a developer,
I want the SharedLibraries DAL files that directly access the `metadata` database to delegate to `IMetadataRepository`,
So that no DAL file outside of `MetadataVersionDAL` constructs a `metadata` URL.

---

## Acceptance Criteria

**AC-1 — DAL call sites replaced**
Given the following direct `metadata` HTTP calls in SharedLibraries DAL files:
- `AuditRecoveryDAL.cs` — 1 hit (`GET metadata/version_specification-{v}/metadata`)
- `CaseValidationDAL.cs` — 2 hits (metadata document GET/PUT for case validation)
- `MMRIAServicesDAL.cs` — 3 hits (de-id export list and populate-CDC config reads)

When this story is complete
Then each is replaced with the corresponding `IMetadataRepository` method; `IMetadataRepository` is injected into each DAL via constructor injection

**AC-2 — Tenant/CDC connection context preserved**
Given `MMRIAServicesDAL` handles cross-tenant and CDC-scoped metadata reads
When these are replaced
Then the tenant/CDC connection context (`DBConfigurationDetail`) is passed through to the repository method — no implicit global state is introduced

**AC-3 — Build succeeds**
Given the build after all changes
When verified
Then all three projects build with zero errors

---

## Dev Notes — Files to Change

| File | Hits | Change |
|------|------|--------|
| `mmria.common/SharedLibraries/AuditRecovery/DAL/AuditRecoveryDAL.cs` | 1 | **UPDATE** — inject `IMetadataRepository`, replace `metadata` URL |
| `mmria.common/SharedLibraries/CaseValidation/DAL/CaseValidationDAL.cs` | 2 | **UPDATE** |
| `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | 3 | **UPDATE** |

---

## Sequencing

Depends on 20.2. Can proceed in parallel with 20.4 and 20.5.
