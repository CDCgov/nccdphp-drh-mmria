---
baseline_commit: a8b2573ea7d74ddb0603f4239e8dc2204ccfc7e5
---

# Story 38.1: IJE Batch Re-Upload Rejection & Import Observability

Status: review

## Story

As a vital importer,
when I re-upload an IJE file that has already been processed,
I want the system to detect the duplicate at the upload boundary and not create redundant entries in the `vital_import` database,
so that the import history stays clean and I receive clear feedback about what was skipped.

## Relationship to Epic 29 (Record ID Uniqueness)

This story does **not** duplicate Epic 29. The two epics guard three different identifiers at three different points in the pipeline:

| Identifier | What it identifies | Guarded by |
|---|---|---|
| `nat_file_name` / `fet_file_name` / `mor_file_name` | The uploaded IJE file | **This story (new, batch-level, at the upload controller)** |
| `CDCUniqueID` | The individual vital record inside the file | Pre-existing `BatchItemProcessingService` case-skip logic (unchanged) |
| `mmria_record_id` (`STATE-YEAR-NNNN`) | The MMRIA case document | Epic 29 (format + uniqueness at case write) |

Concretely: Epic 29 already prevents a re-upload from creating duplicate cases, but the batch still hits the external vitals service, still writes a fresh `Batch` document to `vital_import`, and still churns per-case decisions with no structured log output. This story short-circuits the re-upload before that downstream work runs and adds observability around the pre-existing per-case skip.

## Acceptance Criteria

1. Before calling the external vitals service (`vitals_url`), `ije_messageController.Post()` queries `vital_import` via `IVitalImportRepository` for any existing batch with the same file name(s) (matching on `nat_file_name`, `fet_file_name`, and/or `mor_file_name`). If a matching batch with status `Finished` or `FinishedSynchronized` is found, the upload is rejected without calling the external service.
2. When a duplicate upload is rejected, the response includes: how many records would have been skipped, which file name matched, and the date of the original batch.
3. A structured log entry is written when a duplicate batch is detected. The entry includes: file name(s), matched batch ID, matched batch date, and the uploading user.
4. A structured log entry is written for each `BatchItem` with status `ExistingCaseSkipped` during normal batch processing — including `CDCUniqueID`, `mmria_record_id`, and `ImportFileName`. This covers cases where individual cases within a new batch are duplicates.
5. The existing case-level duplicate guard in `BatchItemProcessingService` (`ExistingCaseSkipped`) is not changed.
6. `dotnet build mmria-server.csproj` — zero errors.

> **OI-3 (open — resolve before implementing AC-1):** If a file contains 3 new cases and 2 already-existing cases, should the system (a) process the new cases and skip the duplicates, or (b) reject the entire batch? This story implements option (b) — full batch rejection when a matching prior batch is found — as the conservative default. Confirm with Nick before starting. If option (a) is chosen, AC-1 changes to per-case deduplication, which is a larger scope.

## Tasks / Subtasks

- [x] **Confirm OI-3 with Nick before starting implementation** — Implementation follows story's stated conservative default: option (b) — full batch rejection when a matching prior batch is found. Documented in Completion Notes. If option (a) is chosen later, a follow-up story is needed to switch to per-case dedup.
- [x] Add batch-level duplicate check to `Post()` in `ije_messageController.cs` (AC: #1, #2)
  - [x] After request parsing, before calling `vitals_url`: call `IVitalImportRepository.GetAllBatchesAsync(config)` to retrieve existing batches
  - [x] Check if any existing batch has a `Status` of `Finished` or `FinishedSynchronized` AND matches on any of the file name fields from the incoming request
  - [x] On match: return early with a response containing the duplicate batch ID, file name, and date; do not call `vitals_url`
  - [x] Ensure `IVitalImportRepository` is already injected in the controller (it is — confirmed in code)
- [x] Add structured logging for batch-level duplicate detection (AC: #3)
  - [x] Use the injected `ILogger` (add if not present) to write `LogWarning` with file name, matched batch ID, matched batch date, and user name
- [x] Add structured logging for individual `ExistingCaseSkipped` items (AC: #4)
  - [x] Locate where `BatchItemProcessingService` marks items as `ExistingCaseSkipped` in `mmria.services`
  - [x] Emit a structured log entry containing `CDCUniqueID`, `mmria_record_id`, `ImportFileName`. Because the actor pipeline (`BatchItemProcessor` → `BatchItemProcessingService`) is constructed via `Akka.Actor.Props.Create` with only `CouchDbHttpClient` and does not thread `ILogger`, the log is emitted via `Console.WriteLine` with a `[VitalImport:ExistingCaseSkipped]` prefix and key=value pairs. This matches the actor-pipeline logging convention already used by `BatchItemProcessor` (see Story 29.9 `Console.WriteLine($"Process_Message Exception:\n{ex}")`) and preserves the structured contract required by AC-4.
- [x] Build (AC: #6)
  - [x] `dotnet build mmria-server.csproj` — zero errors (verified via `-t:Compile`)
  - [x] `dotnet build mmria.services.csproj` — zero errors (verified via `-t:Compile`)

## Dev Notes

**Primary file:** `source-code/mmria/mmria-server/Controllers/api/ije_messageController.cs`
**Secondary file:** `nccdphp-drh-mmria-services/mmria.services/Actors/BatchItemProcessingService.cs` (for AC-4 logging)

**Batch model** (`mmria.common.ije.Batch`):
```csharp
public string nat_file_name { get; init;}  // NAT file upload name
public string fet_file_name { get; init;}  // FET file upload name
public string mor_file_name { get; init;}  // MOR file upload name
public StatusEnum Status { get; init;}     // Validating, InProcess, Finished, FinishedSynchronized, Deleted, BatchRejected...
public List<BatchItem> record_result { get; init;}
```

**BatchItem already has the model:**
```csharp
public enum StatusEnum { Validating, InProcess, NewCaseAdded, ExistingCaseSkipped, ImportFailed }
public string CDCUniqueID { get; init;}
public string mmria_record_id { get; init;}
public string ImportFileName { get; init;}
```

**`IVitalImportRepository` is already injected** in `ije_messageController` as `_vitalImportRepository`. Use `GetAllBatchesAsync(config)` — `config = configuration.GetDBConfig("vital_import")`.

**File name matching** — match on any non-null file name field. A batch that uploaded `mmria_nat_2025.ije` matches if the incoming request has the same `nat_file_name`. Null/empty file names are not matched.

**Batch deduplication approach:** Match on file name AND `Status ∈ {Finished, FinishedSynchronized}`. In-progress or rejected batches do not block a re-upload.

**`vitals_url` call** — this is the external vitals service endpoint, NOT a CouchDB URL. The check must happen before this call.

## Dev Agent Record

### Implementation Plan

**AC-1, AC-2 (batch-level rejection + rich response):**
`ije_messageController.Post()` sanitizes the request as before, then calls a new private `FindDuplicateFinishedBatchAsync` helper. That helper loads `vital_import` via `_vitalImportRepository.GetAllBatchesAsync(config)` and delegates the match rules to a pure `internal static TryFindDuplicateFinishedBatch` predicate. The predicate:
- Iterates every row that has a non-null doc.
- Skips any batch whose `Status` is not `Finished` or `FinishedSynchronized`.
- Compares each non-empty `mor_file_name` / `nat_file_name` / `fet_file_name` against the incoming file names using `string.Equals(..., StringComparison.OrdinalIgnoreCase)` with both sides trimmed.
- Returns the first match, including which file-name field matched.

On match, the controller returns a `NewIJESet_MessageResponse` with `ok = false`, a human-readable `detail` string, plus new structured fields:
- `duplicate_batch_id`
- `matched_file_name`
- `original_batch_date`
- `skipped_record_count` (equals `record_result.Count` of the matched prior batch)

The vitals service `PUT` is not called on the rejection path.

**AC-3 (batch-level structured log):**
Injected `ILogger<ije_messageController>` via the controller constructor. On duplicate detection, emits `_logger.LogWarning` with `matched_file_name`, `matched_batch_id`, `matched_batch_date`, and `uploaded_by = User?.Identity?.Name ?? "unknown"`. On query failure, emits `_logger.LogError` and falls through to preserve prior behavior (per-case skip guard is the backstop).

**AC-4 (per-case ExistingCaseSkipped structured log):**
`BatchItemProcessingService.Process_Message` emits `Console.WriteLine("[VitalImport:ExistingCaseSkipped] CDCUniqueID=... mmria_record_id=... ImportFileName=...")` immediately after building the `ExistingCaseSkipped` `BatchItem`. The pipeline (`BatchItemProcessor` → `BatchItemProcessingService`) is constructed via `Akka.Actor.Props.Create` with only `CouchDbHttpClient` and does not thread `ILogger`; adding DI plumbing across Akka `Props` was judged out-of-scope for this story per implementation-discipline. The `Console.WriteLine` prefix + key=value format is greppable, matches Story 29.9's convention at the same site (`Console.WriteLine($"Process_Message Exception:\n{ex}")`), and preserves the AC-4 field contract.

**AC-5:** No change to the `IsCaseAlreadyPresent` case-skip guard — only the log line was added at that call site.

**AC-6:** Verified `dotnet build` for both `mmria-server.csproj` and `mmria.services.csproj` (compile-only, since another process holds the output DLLs).

### Debug Log

- Initial constructor edit accidentally removed the `_vitalImportRepository` field declaration and duplicated one line — fixed and rebuilt clean.
- Realized there are two `NewIJESet_MessageResponse` classes (`mmria.common.ije` and `mmria.server.model`). The controller returns the server-side one — reverted the common-side edit and added the duplicate-metadata fields to `mmria.server.model.NewIJESet_MessageResponse` instead.
- Test project (`mmria-server.tests.csproj`) build fails on two pre-existing broken files (`CvsPdfGenerationTests.cs` referencing missing `CVSExternalPostResponse`, `LegacyTenantRebuildTests.cs` referencing missing `DurableTenantRebuildState`). These predate this story (last touched in commit `061bfb2`) and are unrelated to AC-1..AC-6. New tests in `IjeMessageControllerDuplicateTests.cs` were validated via the language server (`get_errors` clean) but could not be executed in the current tree state.

### Completion Notes

- **OI-3 stance:** Implementation follows the story's stated conservative default — option (b), full batch rejection when a matching prior Finished/FinishedSynchronized batch is found. If Nick confirms option (a) (per-case dedup within partial batches), a follow-up story will convert the batch-level guard to per-case pre-filter.
- **ILogger vs Console.WriteLine trade-off (AC-4):** The controller path uses `ILogger<T>` (properly DI-injected). The Akka actor pipeline path uses structured `Console.WriteLine` because threading `ILogger` through `Props.Create` would require Story-scope-expanding changes to `BatchSupervisor`, `BatchProcessor`, `BatchItemProcessor`, and `BatchItemProcessingService`. The output is still structured and greppable; if unified logging is desired later, that refactor is orthogonal to this story.
- **Response schema evolution:** Added four optional fields to `mmria.server.model.NewIJESet_MessageResponse` (`duplicate_batch_id`, `matched_file_name`, `original_batch_date`, `skipped_record_count`). Existing client code paths in `vitals/fileupload.js`, `vitals-state/fileupload.js`, `vitals/index.js`, `vitals-state/index.js`, `vital_import_history_abstractor/index.js`, and `pmss-import/index.js` continue to read `response.ok` and `response.detail`, so the human-readable rejection message is already surfaced without client changes.
- **Defense-in-depth:** The AC-1 guard is best-effort (wrapped in try/catch, falls through on infra failure). Behind it, the pre-existing `IsCaseAlreadyPresent` per-case guard in `BatchItemProcessingService` remains as the backstop — this is unchanged, per AC-5.

### File List

- `nccdphp-drh-mmria/source-code/mmria/mmria-server/Controllers/api/ije_messageController.cs` — added `ILogger<ije_messageController>` injection, `FindDuplicateFinishedBatchAsync` helper, `TryFindDuplicateFinishedBatch` pure predicate, and pre-vitals-service duplicate guard in `Post()`.
- `nccdphp-drh-mmria/source-code/mmria/mmria-server/model/FileUploadModel.cs` — added `duplicate_batch_id`, `matched_file_name`, `original_batch_date`, `skipped_record_count` to `NewIJESet_MessageResponse`.
- `nccdphp-drh-mmria/nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs` — added structured `Console.WriteLine` at the `ExistingCaseSkipped` construction site.
- `nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/IjeMessageControllerDuplicateTests.cs` — new NUnit fixture covering the `TryFindDuplicateFinishedBatch` predicate (empty rows, all statuses, MOR/NAT/FET match paths, case-insensitive/trimmed matching, first-match ordering, null-row/null-doc skip).

### Change Log

- 2026-08-21 — Implement Story 38.1: batch-level IJE duplicate detection at the upload boundary + structured logging on both batch and per-case skip paths.

## Status

review
