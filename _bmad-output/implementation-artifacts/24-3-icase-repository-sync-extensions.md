# Story 24.3 — Extend `ICaseRepository` with Paged Bulk Read and Change-Stream Read

**Epic:** 24 — Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)
**Story ID:** 24.3
**Status:** done
**Date added:** 2026-07-16
**Depends on:** 24.1
**Source requirements:** epics.md §Epic 24 Story 24.3; project-context.md §2.2

---

## User Story

As a developer,
I want `ICaseRepository` to expose the paged bulk read and change-stream read patterns needed by rebuild and sync orchestrators,
So that `c_document_sync_all` variants and `Process_DB_Synchronization_Set` can replace their direct `mmrds` calls with interface calls.

---

## Acceptance Criteria

**AC-1 — Paged bulk read added to `ICaseRepository`**
Given `ICaseRepository` from Story 17.2 covers per-document CRUD and view queries but not cursor-based bulk reads
When this story is complete
Then `ICaseRepository` gains:
- `GetCasesPagedAsync(string? startKey, int limit, DBConfigurationDetail dbConfig)` → `CasePage`
- `CasePage` contains: `IReadOnlyList<JObject> Documents` and `string? LastId` (the `_id` of the last returned document, used as `startKey` for the next page)
- `startKey` null means start from the beginning of the database
- Implemented in `CaseDAL` as `GET {prefix}mmrds/_all_docs?include_docs=true&startkey={startKey}&limit={limit}`
- SQL migration equivalent: `SELECT * FROM cases WHERE id > @startKey ORDER BY id FETCH NEXT @limit ROWS ONLY`

**AC-2 — Change-stream read added to `ICaseRepository`**
Given `Process_DB_Synchronization_Set` polls `mmrds/_changes` to detect mutations
When this story is complete
Then `ICaseRepository` gains:
- `GetCaseChangesSinceAsync(string sinceSeq, DBConfigurationDetail dbConfig)` → `CaseChangeFeedResult`
- `CaseChangeFeedResult` contains: `string LastSeq` and `IReadOnlyList<CaseChangeEntry>`
- `CaseChangeEntry` contains: `string Id`, `string Seq`, `bool Deleted`, `JObject? Doc` (full document for updates; null for deletes)
- Implemented in `CaseDAL` as `GET {prefix}mmrds/_changes?since={sinceSeq}&include_docs=true`
- SQL migration equivalent: polls SQL change-tracking or CDC table using the same interface contract

**AC-3 — CDC drop-and-reset added to `ICaseRepository`**
Given `Process_Central_Pull_list` drops and recreates the target `mmrds` database at the start of each CDC pull
When this story is complete
Then `ICaseRepository` gains:
- `DropAndResetAsync(DBConfigurationDetail dbConfig)` — drops the tenant-prefixed mmrds database and recreates it empty; SQL equivalent: `TRUNCATE TABLE cases` (scoped to the target tenant)
- This method is used exclusively by the CDC populate path; this is documented in the interface with an XML summary comment noting the limited use case

**AC-3b — CDC count probe methods added to `ICaseRepository`**
Given `mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs` uses two special count-probe operations not covered by `GetCasesPagedAsync`:
- Line ~260: `GET {prefix}mmrds/_all_docs?limit=0` — total document count probe used to initialize throttle calculations
- Line ~272: `GET {prefix}mmrds/_all_docs?startkey=_design/&endkey=_design0` — design document count probe
When this story is complete
Then `ICaseRepository` gains:
- `GetCaseTotalCountAsync(DBConfigurationDetail dbConfig)` → `int` — total doc count excluding design docs; implemented as `GET {prefix}mmrds/_all_docs?limit=0` and reads `total_rows` from response
- `GetDesignDocCountAsync(DBConfigurationDetail dbConfig)` → `int` — design doc count only; implemented as `GET {prefix}mmrds/_all_docs?startkey=_design/&endkey=_design0`
SQL migration equivalents: `SELECT COUNT(*) FROM cases` and a count of index objects respectively

**AC-4 — New model types live alongside interface**
Given `CasePage`, `CaseChangeFeedResult`, and `CaseChangeEntry` are new model types
When they are created
Then they live in `mmria.common/SharedLibraries/Case/` alongside the existing `ICaseRepository`

**AC-5 — `CaseDAL` implements all new methods**
Given the new interface methods
When `CaseDAL` is updated
Then it implements all three new methods using Pattern B (`dbConfig.Get_Prefix_DB_Url(...)`) throughout; existing methods are unchanged

**AC-6 — Build passes with no caller changes**
Given no existing callers are changed in this story
When the build runs after this story
Then all three projects build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/Case/ICaseRepository.cs` | **UPDATE** — add 3 method signatures |
| `mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs` | **UPDATE** — implement 3 new methods |
| `mmria.common/SharedLibraries/Case/CasePage.cs` | **CREATE** — new model type |
| `mmria.common/SharedLibraries/Case/CaseChangeFeedResult.cs` | **CREATE** — new model types (`CaseChangeFeedResult` + `CaseChangeEntry`) |

**New interface method signatures:**
```csharp
// Paged bulk read for rebuild orchestrators
Task<CasePage> GetCasesPagedAsync(string? startKey, int limit, DBConfigurationDetail dbConfig);

// Change-stream polling for real-time sync
Task<CaseChangeFeedResult> GetCaseChangesSinceAsync(string sinceSeq, DBConfigurationDetail dbConfig);

// CDC-only: drop and recreate target mmrds (used by Process_Central_Pull_list)
Task DropAndResetAsync(DBConfigurationDetail dbConfig);

// CDC services: total case count and design-doc count probes
Task<int> GetCaseTotalCountAsync(DBConfigurationDetail dbConfig);
Task<int> GetDesignDocCountAsync(DBConfigurationDetail dbConfig);
```

**`CasePage` model:**
```csharp
public record CasePage(IReadOnlyList<JObject> Documents, string? LastId);
```

**`CaseChangeFeedResult` / `CaseChangeEntry` models:**
```csharp
public record CaseChangeFeedResult(string LastSeq, IReadOnlyList<CaseChangeEntry> Changes);
public record CaseChangeEntry(string Id, string Seq, bool Deleted, JObject? Doc);
```

**Design notes:**
- `GetCasesPagedAsync` cursor approach: `startKey` is the raw `_id` string. In CouchDB `_all_docs`, `startkey` must be JSON-encoded (i.e., `"\"docId\""` with quotes). `CaseDAL` handles the encoding internally — callers pass the raw `_id` without quotes.
- `GetCaseChangesSinceAsync` initial seq: the first poll uses `"0"` as `sinceSeq`. `TenantChangeSequenceState` in the actor stores the returned `LastSeq` for subsequent polls. This state management stays in the actor — not in the DAL.
- `DropAndResetAsync` scoping: the method uses `dbConfig.Get_Prefix_DB_Url("mmrds")` with a database-level DELETE and PUT. The prefix in `dbConfig` determines which tenant's mmrds is affected. Caller (Process_Central_Pull_list actor) is responsible for passing the correct target `dbConfig`.
- Confirm exact URL parameters for `_changes` (e.g., `limit`, `heartbeat`, `timeout` options) against Story 24.1 catalog before implementing.

---

## Sequencing

Depends on 24.1. Can proceed in parallel with 24.2, 24.4, 24.5 once 24.1 is complete. Stories 24.7, 24.8, 24.9 all depend on this story.
