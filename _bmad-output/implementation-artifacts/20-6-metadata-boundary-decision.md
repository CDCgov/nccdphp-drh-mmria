# Story 20.6 — `metadata` Boundary Decision — Bulk `_all_docs` and Sync

**Epic:** 20 — `metadata` Consolidation (SQL Migration Foundation)
**Story ID:** 20.6
**Status:** ready-for-dev
**Date added:** 2026-07-15
**Depends on:** 20.1 (can run in parallel with 20.1)
**Source requirements:** epics.md §Epic 20 Story 20.6; project-context.md §2.2

---

## User Story

As a developer,
I want a written architecture decision on whether bulk `metadata/_all_docs` reads and sync-driven metadata access belong behind `IMetadataRepository` or are separate infrastructure concerns,
So that the boundary is explicit and consistent with the decision made for `mmrds` in Story 17.7.

---

## Acceptance Criteria

**AC-1 — Boundary decision recorded**
Given `MetadataVersionManager` uses `GET metadata/_all_docs?include_docs=true` in two places for loading the full version list
When the developer evaluates these
Then a decision is recorded in `docs/ai/mmrds_operation_catalog.md` under the `metadata` Boundary Decisions section: either (a) add `GetAllMetadataDocumentsAsync` to `IMetadataRepository` or (b) keep these as manager-level reads not in the interface

**AC-2 — Consistent with Story 17.7**
Given the recommendation from Story 17.7 treated sync `_all_docs` as out-of-scope infrastructure
When the same question is evaluated for `metadata`
Then the decision is consistent with Story 17.7 — bulk reads for version list enumeration are part of the application interface (`IMetadataRepository`) since `MetadataVersionManager` already owns them; sync-driven reads in `c_document_sync_all` remain infrastructure

---

## Dev Notes

This is a documentation-only story — no code changes required. The output is an architecture decision record appended to `docs/ai/mmrds_operation_catalog.md`.

**Expected decision (per epics.md):** Bulk `_all_docs` version-list reads in `MetadataVersionManager` belong in `IMetadataRepository` (application-owned). Sync-driven reads in `c_document_sync_all` remain infrastructure.

---

## Sequencing

Can proceed in parallel with 20.1. Results feed into 20.2 scope decisions. Does not block 20.2 if the decision is clear from the 20.1 catalog.
