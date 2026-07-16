# Story 20.2 — Define `IMetadataRepository` and Canonicalize `MetadataVersionDAL`

**Epic:** 20 — `metadata` Consolidation (SQL Migration Foundation)
**Story ID:** 20.2
**Status:** done
**Date added:** 2026-07-15
**Depends on:** 20.1
**Source requirements:** epics.md §Epic 20 Story 20.2; project-context.md §2.2

---

## User Story

As a developer,
I want a single `IMetadataRepository` interface over all `metadata` database operations,
So that every caller depends on the interface and not on CouchDB URL construction.

---

## Acceptance Criteria

**AC-1 — `MetadataVersionDAL` canonicalized**
Given the existing `MetadataVersionDAL` in `mmria.common/SharedLibraries/MetadataVersion/`
When this story is complete
Then `MetadataVersionDAL` contains all in-scope `metadata` operations from the catalog using consistent URL construction throughout — no Pattern A strings remain

**AC-2 — `MetadataVersionManager` routes through DAL**
Given `MetadataVersionManager.cs` currently builds 22 `metadata` URLs directly instead of routing all through `MetadataVersionDAL`
When this story is complete
Then every `metadata` URL in `MetadataVersionManager` is replaced with a `MetadataVersionDAL` method call; the manager does not construct CouchDB URLs directly

**AC-3 — `IMetadataRepository` extracted**
Given the full operation set is in `MetadataVersionDAL`
When the interface is extracted
Then `IMetadataRepository` is defined in `mmria.common/SharedLibraries/MetadataVersion/` with async method signatures matching every `MetadataVersionDAL` method; `MetadataVersionDAL` implements `IMetadataRepository`

**AC-4 — DI registration**
Given `IMetadataRepository` is defined
When DI registration is updated in `mmria-server`
Then `IMetadataRepository` is registered as `MetadataVersionDAL`; all existing callers of the concrete `MetadataVersionDAL` compile without changes

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/MetadataVersion/IMetadataRepository.cs` | **CREATE** — interface |
| `mmria.common/SharedLibraries/MetadataVersion/DAL/MetadataVersionDAL.cs` | **UPDATE** — add all operations, implement `IMetadataRepository`, convert any Pattern A strings to Pattern B |
| `mmria.common/SharedLibraries/MetadataVersion/MetadataVersionManager.cs` | **UPDATE** — route all 22 URL constructions through `MetadataVersionDAL` method calls |
| `mmria-server/Program.cs` | **UPDATE** — register `IMetadataRepository` as `MetadataVersionDAL` |

---

## Sequencing

Depends on 20.1. Once done, 20.3, 20.4, and 20.5 can proceed in parallel.
