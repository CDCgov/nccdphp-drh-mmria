# Story 27.2 — BatchProcessor Assessment + Migration Actor Classification

**Epic:** 27 — Services Utility Repository Activation
**Story ID:** 27.2
**Status:** ready-for-dev
**Date added:** 2026-07-17
**Depends on:** None (can proceed in parallel with 27.1)
**Source requirements:** epics.md §Epic 27 Story 27.2; project-context.md §2.2

---

## User Story

As a developer,
I want every remaining non-DAL `CouchDbHttpClient.ExecuteAsync` call to have an explicit documented disposition,
So that the codebase has zero ambiguous direct database calls and the non-DAL boundary analysis is fully closed.

---

## Acceptance Criteria

**AC-1 — `BatchProcessor.cs` DELETE call classified and resolved**
Given `mmria.services/Actors/BatchProcessor.cs` at approximately line 512 calls `_couchDbHttpClient.ExecuteAsync("DELETE", request_string, ...)` to delete a document from an unconfirmed database
When this story begins
Then the developer reads the surrounding code to identify: (a) the target database name from the URL construction, (b) whether a repository interface exists for that database
Then:
- If a matching repository exists: the DELETE call is replaced with the corresponding `IRepository.DeleteAsync(...)` method; the repo is injected via constructor
- If no matching repository exists: a comment is added at the call site documenting the database target and why no repository covers it; the story completion notes record the decision and whether a follow-on story is needed

**AC-2 — `Process_Migrate_Charactor_to_Numeric.cs` classified as intentional**
Given `mmria-server/model/actor/quartz/Process_Migrate_Charactor_to_Numeric.cs` contains direct CouchDB calls for data migration purposes
When this story is complete
Then the following comment is added at the top of the class declaration:
```
// Data migration actor — direct CouchDB access is intentional.
// This actor performs one-time bulk data corrections and is not used in production case-management flows.
// Excluded from the repository pattern by design. See epics.md §Epic 27 Story 27.2.
```
No other changes are made to this file

**AC-3 — `Process_Migrate_Data.cs` classified as intentional**
Given `mmria-server/model/actor/quartz/Process_Migrate_Data.cs` similarly contains migration-purpose direct CouchDB calls
When this story is complete
Then the same classification comment is added at the top of the class declaration; no other changes are made

**AC-4 — Final non-DAL scan confirms zero unclassified calls**
Given all stories in Epics 25–27 have been completed (or this story is the last)
When this story closes
Then the developer runs a scan of all `CouchDbHttpClient.ExecuteAsync` calls across `mmria-server`, `mmria.common`, and `mmria.services` (excluding utilities repo, `_bmad-output`, `obj/`, `bin/`) and confirms every call is one of:
- Inside a `DAL/` file (expected) ✓
- Inside an intentional infrastructure exception (`c_db_setup.cs`, `Check_DB_Install.cs`, `MultiTenantSetupService.cs`, `MMRIARebuildWorker.cs`) ✓
- A service-endpoint call using `CouchDbHttpClient` as a general HTTP transport (not a database call) ✓
- Formally documented as an intentional exception per ACs 2 and 3 ✓
The scan result (file count, call count, and disposition summary) is recorded in the story completion notes

**AC-5 — Build passes with zero errors**
Given any changes from AC-1
When the build runs
Then all three projects build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `nccdphp-drh-mmria-services/mmria.services/Actors/BatchProcessor.cs` | **ASSESS and UPDATE or COMMENT** per AC-1 |
| `source-code/mmria/mmria-server/model/actor/quartz/Process_Migrate_Charactor_to_Numeric.cs` | **ADD COMMENT** per AC-2 |
| `source-code/mmria/mmria-server/model/actor/quartz/Process_Migrate_Data.cs` | **ADD COMMENT** per AC-3 |

**Scan command for AC-4 (PowerShell):**
```powershell
Get-ChildItem -Recurse -Path "c:\repos\nccdphp-drh-mmria" -Include "*.cs" |
  Where-Object { $_.FullName -notlike "*_bmad*" -and $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*utilities*" } |
  Select-String "CouchDbHttpClient.ExecuteAsync" |
  Where-Object { $_.Path -notlike "*\DAL\*" } |
  Select-Object Path, LineNumber, Line |
  Format-Table -AutoSize
```
After completing Epics 25–27, the output should contain only: infrastructure files, service-endpoint calls, and the two formally classified migration actors.

**`rebuild_export_queue_job.cs` note:** This file already has a superseded-dead-code comment added in Story 24.4. It does not need classification in this story.
