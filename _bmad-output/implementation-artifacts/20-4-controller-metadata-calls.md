# Story 20.4 — Route Controller Direct `metadata` Calls Through `IMetadataRepository`

**Epic:** 20 — `metadata` Consolidation (SQL Migration Foundation)
**Story ID:** 20.4
**Status:** ready-for-dev
**Date added:** 2026-07-15
**Depends on:** 20.2
**Source requirements:** epics.md §Epic 20 Story 20.4; project-context.md §2.2

---

## User Story

As a developer,
I want controllers that directly access the `metadata` database to delegate to `IMetadataRepository` or the existing `MetadataVersionManager`,
So that controllers contain no `metadata` URL construction.

---

## Acceptance Criteria

**AC-1 — Controller call sites replaced**
Given the following controllers with direct `metadata` URL construction:
- `broadcast_messageController.cs` — 3 hits (broadcast-message-list GET/PUT — Wave 9 planned migration target)
- `de_identified_listController.cs` — 2 hits (de-id and de-id-export list GET/PUT — Wave 8 planned target)
- `export_list_managerController.cs` — 2 hits (export-standard-list GET/PUT)
- `substance_mappingController.cs` — 2 hits (substance-mapping GET/PUT)
- `abstractorDeidentifiedCaseController.cs` — 1 hit (duplicate-multiform-list GET)
- `CaseController.cs` — 1 hit (duplicate-multiform-list GET)
- `versionController.cs` — 1 hit (metadata document GET by ID)
- `record_idController.cs` — 1 hit (record ID document GET)
- `systemOfflineController.cs` — 1 hit (system-offline-config URL builder)

When this story is complete
Then each is replaced with the corresponding `IMetadataRepository` or `MetadataVersionManager` method call; `IMetadataRepository` is injected where no manager intermediary already exists

**AC-2 — Wave 8/9 extraction deferred**
Given `broadcast_messageController` and `de_identified_listController` are also Wave 8/9 SharedLibraries migration targets
When this story touches them
Then only the URL construction is replaced; the Wave 8/9 manager extraction is deferred — this story does not restructure controller business logic

**AC-3 — Build succeeds with no route changes**
Given the build after all changes
When verified
Then all three projects build with zero errors and no route, action signature, or response shape changes are made

---

## Dev Notes — Files to Change

| File | Hits | Change |
|------|------|--------|
| `broadcast_messageController.cs` | 3 | **UPDATE** — inject `IMetadataRepository` |
| `de_identified_listController.cs` | 2 | **UPDATE** |
| `export_list_managerController.cs` | 2 | **UPDATE** |
| `substance_mappingController.cs` | 2 | **UPDATE** |
| `abstractorDeidentifiedCaseController.cs` | 1 | **UPDATE** |
| `CaseController.cs` | 1 | **UPDATE** |
| `versionController.cs` | 1 | **UPDATE** |
| `record_idController.cs` | 1 | **UPDATE** |
| `systemOfflineController.cs` | 1 | **UPDATE** |

---

## Sequencing

Depends on 20.2. Can proceed in parallel with 20.3 and 20.5.
