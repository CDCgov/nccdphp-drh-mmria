---
baseline_commit: a8b2573ea7d74ddb0603f4239e8dc2204ccfc7e5
---

# Story 38.1: IJE Import Observability at the Case Level

Status: done

## Story

As a vital importer,
when I re-upload an IJE file whose records were already imported,
I want each duplicate record to be skipped and logged with enough context to trace it,
so that the import history stays clean and I can tell which records were skipped and why — without the system rejecting a fresh file that happens to share a name with a prior upload.

## Relationship to Epic 29 (Record ID Uniqueness)

This story does **not** duplicate Epic 29. The two epics guard three different identifiers at different points in the pipeline:

| Identifier | What it identifies | Guarded by |
|---|---|---|
| `CDCUniqueID` | The individual vital record inside the file | Pre-existing `BatchItemProcessingService.IsCaseAlreadyPresent` case-skip logic — this story adds observability around that skip |
| `mmria_record_id` (`STATE-YEAR-NNNN`) | The MMRIA case document | Epic 29 (format + uniqueness at case write) |
| `nat_file_name` / `fet_file_name` / `mor_file_name` | The uploaded IJE file | **Explicitly out of scope** (see OI-3 decision below) |

Epic 29 already prevents a re-upload from creating duplicate cases, and the pre-existing case-level `ExistingCaseSkipped` guard prevents redundant case-document writes. What was missing was observability: when a batch encountered per-case duplicates, no structured trail was written for the case worker or support engineer to inspect. This story adds that structured trail.

## Acceptance Criteria

1. A structured log entry is written for each `BatchItem` that is marked `ExistingCaseSkipped` during batch processing. The entry includes at minimum: `CDCUniqueID`, `mmria_record_id`, `ImportFileName`.
2. The pre-existing case-level duplicate guard in `BatchItemProcessingService` (`IsCaseAlreadyPresent` → `ExistingCaseSkipped`) is not changed. Detection remains keyed on the case, not the file name.
3. `ije_messageController.Post()` does **not** reject uploads based on file-name comparison against prior batches. A user regenerating IJE data and uploading a fresh set of records must succeed even if the file names match a prior upload.
4. `dotnet build mmria-server.csproj` — zero errors.
5. `dotnet build mmria.services.csproj` — zero errors.

> **OI-3 (resolved 2026-08-21, option (a) — case-level dedup):** Given the IJE generator emits file names that encode the generation date (e.g. `2025_2026_08_20_TENANT1.MOR`), a batch-level file-name guard is too strict: regenerating the day-2 test data and uploading it triggers a false positive against the day-1 upload's file names. The uniqueness contract lives at the case level (`CDCUniqueID`), not the file level. This story therefore relies on the existing per-case guard rather than adding a batch-level pre-check.

## Tasks / Subtasks

- [x] Add structured logging for individual `ExistingCaseSkipped` items (AC: #1)
  - [x] Locate where `BatchItemProcessingService` marks items as `ExistingCaseSkipped` in `mmria.services`
  - [x] Emit a structured log entry containing `CDCUniqueID`, `mmria_record_id`, `ImportFileName`. Because the actor pipeline (`BatchItemProcessor` → `BatchItemProcessingService`) is constructed via `Akka.Actor.Props.Create` with only `CouchDbHttpClient` and does not thread `ILogger`, the log is emitted via `Console.WriteLine` with a `[VitalImport:ExistingCaseSkipped]` prefix and key=value pairs. This matches the actor-pipeline logging convention already used at the same site (Story 29.9).
- [x] Confirm no batch-level file-name guard is present in `ije_messageController.Post()` (AC: #3)
- [x] Confirm the pre-existing case-level `ExistingCaseSkipped` guard is unchanged (AC: #2)
- [x] Build (AC: #4, #5)
  - [x] `dotnet build mmria-server.csproj` — zero errors (verified via `-t:Compile`)
  - [x] `dotnet build mmria.services.csproj` — zero errors (verified via `-t:Compile`)

## Dev Notes

**Primary file:** `nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs`

**BatchItem shape:**
```csharp
public enum StatusEnum { Validating, InProcess, NewCaseAdded, ExistingCaseSkipped, ImportFailed }
public string CDCUniqueID { get; init;}
public string mmria_record_id { get; init;}
public string ImportFileName { get; init;}
```

**Pre-existing case-level guard:** `_mmriaServicesManager.IsCaseAlreadyPresent(...)` in `BatchItemProcessingService.Process_Message` returns `(is_case_already_present, mmria_id, record_id)`. When `is_case_already_present == true` the service builds a `BatchItem` with `Status = ExistingCaseSkipped` and returns. This story adds a `Console.WriteLine` at that construction site — nothing else changes about the guard.

**Why not `ILogger.LogInformation`:** the actor pipeline uses `Props.Create<T>(couchDbHttpClient)` and does not thread an `ILogger` down. Adding DI plumbing across `BatchSupervisor`/`BatchProcessor`/`BatchItemProcessor`/`BatchItemProcessingService` was judged out of scope. The chosen `Console.WriteLine` format is greppable (`[VitalImport:ExistingCaseSkipped] key=value ...`) and matches the pipeline's existing convention.

## Dev Agent Record

### Implementation Plan

**AC-1 (per-case ExistingCaseSkipped structured log):**
In `BatchItemProcessingService.Process_Message`, immediately after building the `ExistingCaseSkipped` `BatchItem`, emit:

```
Console.WriteLine($"[VitalImport:ExistingCaseSkipped] CDCUniqueID={...} mmria_record_id={...} ImportFileName={...}");
```

**AC-2, AC-3 (do-no-harm):** The case-level guard in `BatchItemProcessingService` is unchanged (only a log line was added at the return path). The controller `ije_messageController.Post()` was reverted to its baseline behavior — no `IVitalImportRepository.GetAllBatchesAsync` call, no file-name comparison, no early return based on prior batches.

**AC-4, AC-5:** `dotnet build` clean for both projects.

### Debug Log

- Initial pass implemented the batch-level file-name guard per the story's original AC-1 (option (b) from OI-3). Nick reviewed the change against the actual IJE generator output — file names include the generation date, so regenerating fresh test data with new `CDCUniqueID`s triggered a false-positive rejection because the file name matched the prior day's upload.
- OI-3 was reconsidered and resolved as option (a): uniqueness enforcement belongs at the case level, not the file level. The batch-level guard (`FindDuplicateFinishedBatchAsync`, `TryFindDuplicateFinishedBatch`), the `ILogger<ije_messageController>` injection, the four response-DTO fields on `NewIJESet_MessageResponse`, and the corresponding NUnit test fixture were all removed. Only the AC-1 per-case log line survived from the first pass.
- Both projects build clean after the revert.

### Completion Notes

- **OI-3 resolution:** Option (a) — case-level dedup. Reason recorded in Dev Notes and in the OI-3 callout under Acceptance Criteria.
- **What survived from the first pass:** The `Console.WriteLine("[VitalImport:ExistingCaseSkipped] ...")` log line in `BatchItemProcessingService`. Everything else (controller guard, DTO fields, tests) was reverted.
- **What was intentionally not built:** batch-level file-name comparison, `ILogger` injection into the controller, response-DTO extensions, batch-rejection UX. The case-level `ExistingCaseSkipped` path is the sole dedup mechanism.
- **Follow-up (optional, not tracked):** If future observability needs surface, unifying the actor pipeline on `ILogger<T>` (rather than `Console.WriteLine`) is a standalone refactor.

### File List

- `nccdphp-drh-mmria/nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs` — added structured `Console.WriteLine` at the `ExistingCaseSkipped` construction site.

### Change Log

- 2026-08-21 — Implement Story 38.1: case-level per-record `ExistingCaseSkipped` structured log.
- 2026-08-21 — Revised scope: OI-3 resolved as option (a) after Nick observed that IJE file names encode the generation date, so a batch-level file-name guard produces false positives on legitimate regenerated uploads. Reverted the file-name guard, response-DTO extensions, `ILogger` injection, and the associated NUnit fixture. Only the AC-1 per-case log line remains.

## Status

done
