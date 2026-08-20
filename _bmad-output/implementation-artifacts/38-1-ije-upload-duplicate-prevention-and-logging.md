# Story 38.1: IJE Upload Duplicate Prevention and Logging

Status: ready-for-dev

## Story

As a vital importer,
when I re-upload an IJE file that has already been processed,
I want the system to detect the duplicate and not create redundant entries in the `vital_import` database,
so that the import history stays clean and I receive clear feedback about what was skipped.

## Acceptance Criteria

1. Before calling the external vitals service (`vitals_url`), `ije_messageController.Post()` queries `vital_import` via `IVitalImportRepository` for any existing batch with the same file name(s) (matching on `nat_file_name`, `fet_file_name`, and/or `mor_file_name`). If a matching batch with status `Finished` or `FinishedSynchronized` is found, the upload is rejected without calling the external service.
2. When a duplicate upload is rejected, the response includes: how many records would have been skipped, which file name matched, and the date of the original batch.
3. A structured log entry is written when a duplicate batch is detected. The entry includes: file name(s), matched batch ID, matched batch date, and the uploading user.
4. A structured log entry is written for each `BatchItem` with status `ExistingCaseSkipped` during normal batch processing — including `CDCUniqueID`, `mmria_record_id`, and `ImportFileName`. This covers cases where individual cases within a new batch are duplicates.
5. The existing case-level duplicate guard in `BatchItemProcessingService` (`ExistingCaseSkipped`) is not changed.
6. `dotnet build mmria-server.csproj` — zero errors.

> **OI-3 (open — resolve before implementing AC-1):** If a file contains 3 new cases and 2 already-existing cases, should the system (a) process the new cases and skip the duplicates, or (b) reject the entire batch? This story implements option (b) — full batch rejection when a matching prior batch is found — as the conservative default. Confirm with Nick before starting. If option (a) is chosen, AC-1 changes to per-case deduplication, which is a larger scope.

## Tasks / Subtasks

- [ ] **Confirm OI-3 with Nick before starting implementation**
- [ ] Add batch-level duplicate check to `Post()` in `ije_messageController.cs` (AC: #1, #2)
  - [ ] After request parsing, before calling `vitals_url`: call `IVitalImportRepository.GetAllBatchesAsync(config)` to retrieve existing batches
  - [ ] Check if any existing batch has a `Status` of `Finished` or `FinishedSynchronized` AND matches on any of the file name fields from the incoming request
  - [ ] On match: return early with a response containing the duplicate batch ID, file name, and date; do not call `vitals_url`
  - [ ] Ensure `IVitalImportRepository` is already injected in the controller (it is — confirmed in code)
- [ ] Add structured logging for batch-level duplicate detection (AC: #3)
  - [ ] Use the injected `ILogger` (add if not present) to write `LogWarning` with file name, matched batch ID, matched batch date, and user name
- [ ] Add structured logging for individual `ExistingCaseSkipped` items (AC: #4)
  - [ ] Locate where `BatchItemProcessingService` marks items as `ExistingCaseSkipped` in `mmria.services`
  - [ ] Replace any `Console.WriteLine` at that point with `ILogger.LogInformation` logging `CDCUniqueID`, `mmria_record_id`, `ImportFileName`
- [ ] Build (AC: #6)
  - [ ] `dotnet build mmria-server.csproj` — zero errors

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
