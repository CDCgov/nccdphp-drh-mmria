# Story 24.8 — Route `Process_DB_Synchronization_Set` Through Repository Interfaces

**Epic:** 24 — Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)
**Story ID:** 24.8
**Status:** done
**Date added:** 2026-07-16
**Depends on:** 24.2, 24.3, 24.6
**Source requirements:** epics.md §Epic 24 Story 24.8; project-context.md §2.2

---

## User Story

As a developer,
I want `Process_DB_Synchronization_Set.cs` to route its mmrds change-stream reads and its per-document de_id/report sync writes through repository interfaces,
So that the real-time change-feed synchronization actor has no direct CouchDB calls.

---

## Acceptance Criteria

**AC-1 — `mmrds/_changes` feed replaced with `ICaseRepository.GetCaseChangesSinceAsync`**
Given `Process_DB_Synchronization_Set.cs` polls `mmrds/_changes?since={last_seq}` to detect case mutations
When this story is complete
Then that call is replaced with `await _caseRepository.GetCaseChangesSinceAsync(lastSeq, dbConfig)`; the returned `CaseChangeFeedResult.LastSeq` is stored in `TenantChangeSequenceState` as before; `ICaseRepository` is injected into the actor via Akka.NET actor props factory

**AC-2 — Per-document mmrds reads replaced with `ICaseRepository`**
Given `Process_DB_Synchronization_Set.cs` fetches the full case document for UPDATE events via `GET mmrds/{id}`
When this story is complete
Then that call is replaced with the appropriate `ICaseRepository.GetCaseDocumentJsonAsync(id, dbConfig)` (or equivalent GET method from Story 17.2); no direct `mmrds/{id}` URL construction remains

**AC-3 — de_id and report per-doc writes replaced**
Given `Process_DB_Synchronization_Set.cs` writes to de_id and report as part of change-event processing (either directly or via `c_sync_document`):
- For UPDATE events: PUT de-identified document to de_id, PUT report variants to report
- For DELETE events: DELETE document from de_id, DELETE report variants from report
When this story is complete
Then if writes go through `c_sync_document` (which Story 24.6 already routes through interfaces), no further changes are needed at this layer; if this file writes directly to de_id or report, those calls are replaced with `IDeIdentifiedRepository` and `IReportRepository` methods respectively

**AC-4 — `_all_docs` pagination calls handled per catalog**
Given `Process_DB_Synchronization_Set.cs` may have `_all_docs` calls (noted as potentially commented-out in the union/cleanup area)
When Story 24.1 catalog is reviewed
Then: active (non-commented) `_all_docs` calls are replaced with `ICaseRepository.GetCasesPagedAsync(...)`, `IDeIdentifiedRepository` equivalent, or `IReportRepository` equivalent as appropriate per the database targeted; commented-out calls are left as comments and are not refactored

**AC-5 — Sequence state management stays in actor**
Given `TenantChangeSequenceState` stores the last-seen sequence for each tenant
When this story is implemented
Then sequence state management remains in the actor class; the `sinceSeq` value passed to `GetCaseChangesSinceAsync` comes from `TenantChangeSequenceState` as before; `CaseDAL` does not store or manage sequences

**AC-6 — Actor structure and parallelism unchanged**
Given `Process_DB_Synchronization_Set.cs` uses `Parallel.ForEachAsync` with max 4 concurrent workers for fan-out processing
When this story is implemented
Then the parallelism, actor message-handling, and actor hierarchy remain unchanged; only CouchDB call sites are replaced

**AC-7 — Build passes**
Given the build after all changes
When verified
Then all three projects build with zero errors; real-time change-feed sync behavior is identical to pre-change

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs` | **UPDATE** — inject `ICaseRepository`, `IDeIdentifiedRepository` (if needed), `IReportRepository` (if needed); replace all direct CouchDB calls |

**Injection pattern:**
`Process_DB_Synchronization_Set.cs` is an Akka actor. Use the same Akka.NET actor props-factory DI pattern used elsewhere in the codebase (see `docs/ai/MMRIA_Background_Jobs_Documentation.md`). Do not use `new` to construct repository instances inside the actor.

**Design notes:**
- Confirm from Story 24.1 whether this file uses `c_sync_document` for all writes (in which case Story 24.6 handles the write routing) or writes directly to de_id/report at some call sites. The AC-3 approach depends on this.
- The CouchDB `_changes` feed returns `"seq"` values that are opaque strings (not integers) in CouchDB 2.x+. `CaseChangeFeedResult.LastSeq` is typed as `string` to handle this. Confirm the seq type used by the existing code in `TenantChangeSequenceState`.
- The `Parallel.ForEachAsync` with `MaxDegreeOfParallelism = 4` wraps the per-document processing. The repository calls inside the parallel block must be thread-safe. `CaseDAL` and `DeIdentifiedDAL` use `HttpClient` which is thread-safe for concurrent calls — verify this is also true for the injected DAL instances.
- The commented-out union/delete cleanup code (noted in epic analysis): do not uncomment or restructure it. Leave as-is. Only replace active CouchDB call sites.

---

## Sequencing

Depends on 24.2 (`IDeIdentifiedRepository` + `IReportRepository`), 24.3 (`ICaseRepository.GetCaseChangesSinceAsync`), and 24.6 (`c_sync_document.pmss.cs` if used by this actor). Can proceed in parallel with 24.7 once all three prerequisites are complete. Story 24.9 does not depend on this story.
