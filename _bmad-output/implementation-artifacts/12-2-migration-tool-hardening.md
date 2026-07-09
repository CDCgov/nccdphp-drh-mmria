# Story 12.2 — Migration Tool Hardening

**Epic:** 12 — Data Migration Tool Modernization
**Story ID:** 12.2 (hardening)
**Status:** not-started
**Date added:** 2026-07-08
**Depends on:** Story 12.1 (data-migration environment config), Story 12.2-vitals-type-correction (must exist — this story hardens it)
**Source requirements:** FR-17.1–FR-17.6

---

## User Story

As a database administrator running a data migration,
I want the migration tool to retry on CouchDB 409 conflicts and halt on unrecoverable errors,
So that every case document is guaranteed to be processed and no case is silently skipped.

---

## Acceptance Criteria

**AC-1 — `SaveRecord.save_case()` returns `SaveResult` enum, not `bool`**
Given any call to `SaveRecord.save_case()`
When the CouchDB PUT returns HTTP 409
Then the method returns `SaveResult.Conflict`
When the PUT returns HTTP 2xx
Then the method returns `SaveResult.Success`
When the PUT returns any other non-success code
Then the method returns `SaveResult.Error`
And no `bool` return path remains in `SaveRecord.cs`

**AC-2 — `ApplyVitalsTypeCorrection()` is extracted as a pure, re-applicable static method**
Given the field correction logic currently inline in `VitalsTypeCorrectionMigration.execute()`
When it is extracted to `private static bool ApplyVitalsTypeCorrection(ExpandoObject doc, C_Get_Set_Value gs, System.Text.StringBuilder log, string doc_id)`
Then calling it on a document where fields are already integers produces no change and returns `false`
And calling it on a document where fields are string-integers converts them and returns `true`
And the method has no side effects beyond modifying the doc and appending to the log

**AC-3 — Retry loop: on `SaveResult.Conflict`, fetch fresh doc and retry up to 3 times**
Given a save returns `SaveResult.Conflict`
When the migration retries
Then it calls `GET {host_db_url}/{db_name}/{doc_id}` to fetch the fresh document snapshot
And re-applies `ApplyVitalsTypeCorrection()` to the fresh snapshot
And attempts the save again
And this retry happens at most 3 times total
And on retry exhaustion the document `_id` is logged with `"FAILED after 3 retries"`, `failed_count` is incremented, and the loop **continues** to the next document — it does NOT abort the entire run

**AC-4 — Hard stop on `SaveResult.Error`**
Given a save returns `SaveResult.Error` (non-conflict, non-retryable)
When this occurs on any attempt
Then the migration logs the case `_id`, the HTTP status code, and the response body to stderr
And calls `Environment.Exit(1)` immediately
And no further documents are processed

**AC-5 — Pre-flight offline date check**
Given the migration is about to start processing documents
When `DateTime.UtcNow < offline_date` (from `config.MigrationSettings.OfflineDate` or from app config)
Then the migration writes `"PRE-FLIGHT FAIL: system is not offline. Aborting."` to stderr
And calls `Environment.Exit(2)`
And no CouchDB reads or writes have been issued

**AC-6 — Already-migrated detection is unchanged**
Given a case document already has `"VitalsTypeCorrectionMigration"` in its `data_migration_history`
When the migration processes it
Then it is skipped and `already_migrated_count` is incremented
And the existing `force_write = false` guard in `SaveRecord.save_case()` continues to handle this

**AC-7 — Run summary emitted to stdout**
Given the migration completes processing all documents
When the run finishes normally (no `Environment.Exit(1)`)
Then stdout includes: `Processed: N | Already migrated: N | Failed (retries exhausted): N`
And if `failed_count == 0`, exit code is 0
And if `failed_count > 0`, exit code is 3

**AC-8 — Unit tests for new logic**
Given the test project at `nccdphp-drh-mmria-utilities/mmria-server.tests/` (or a dedicated migration test class)
When the following test names are run
Then they all pass:
- `ApplyVitalsTypeCorrection_CorrectlyTransforms` — string "0" becomes int 0
- `ApplyVitalsTypeCorrection_IsIdempotent` — int 0 input → returns false, no change
- `RetryLoop_FetchesFreshRevOnConflict` — mock 409 on first attempt, mock success on second; verify fresh fetch called
- `RetryExhaustion_ContinuesLoop` — mock 3 consecutive 409s; verify failed_count incremented and next doc processed

---

## Dev Notes — Implementation

### File: `data-migration/SaveRecord.cs`

**Change:** Replace `bool` return type with `SaveResult` enum.

Add the enum (can be in the same file or a separate file in `data-migration/`):
```csharp
public enum SaveResult
{
    Success = 0,
    Conflict = 409,
    Error = -1
}
```

In `save_case()`, change the return type from `Task<bool>` to `Task<SaveResult>`. Map HTTP response codes:
```csharp
// After the ExecuteAsync PUT call:
if (put_result.ok)
    return SaveResult.Success;

// Check raw status before deserialization for 409:
// Note: CouchDB returns {"error":"conflict","reason":"Document update conflict."} on 409
// The current code deserializes to document_put_response — on 409, ok == false
// You'll need to check the raw response or response status code.
```

**Important:** The current `save_case()` in `SaveRecord.cs` uses `_couchDbHttpClient.ExecuteAsync()` which returns the response body as a string. You need to also capture the HTTP status code to distinguish 409 from 500. Check if `CouchDbHttpClient.ExecuteAsync` exposes the status code — if not, you may need to check the deserialized error response:

```csharp
// CouchDB 409 response body: {"error":"conflict","reason":"Document update conflict."}
// Check: if !put_result.ok AND response body contains "conflict" → return SaveResult.Conflict
// Otherwise if !put_result.ok → return SaveResult.Error
var is_conflict = responseFromServer.Contains("\"conflict\"");
if (!put_result.ok && is_conflict)
    return SaveResult.Conflict;
if (!put_result.ok)
    return SaveResult.Error;
return SaveResult.Success;
```

All existing callers of `save_case()` that used the `bool` return must be updated to handle `SaveResult`. Check for other callers beyond `VitalsTypeCorrectionMigration`:
```powershell
Select-String -Path "c:\repos\nccdphp-drh-mmria-utilities\data-migration\**\*.cs" -Pattern "\.save_case\(" -Recurse
```

### File: `data-migration/migration-set/VitalsTypeCorrectionMigration.cs`

**Step 1 — Extract `ApplyVitalsTypeCorrection()`:**

```csharp
/// <summary>
/// Applies the vitals type correction transform to a case document.
/// Pure and idempotent: if all target fields are already integers, returns false.
/// </summary>
private static bool ApplyVitalsTypeCorrection(
    System.Dynamic.ExpandoObject doc,
    C_Get_Set_Value gs,
    System.Text.StringBuilder log,
    string doc_id)
{
    bool changed = false;
    var target_paths = new[]
    {
        "birth_fetal_death_certificate_parent/demographic_of_mother/mother_married",
        "birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital"
    };

    foreach (var path in target_paths)
    {
        var value_result = gs.get_value(doc, path);
        if (value_result.is_error || value_result.result == null) continue;

        if (value_result.result is string str_value)
        {
            if (int.TryParse(str_value, out int int_value))
            {
                gs.set_objectvalue(path, int_value, doc);
                log.AppendLine($"record_id: {doc_id} [{path}] converted: \"{str_value}\" => {int_value}");
                changed = true;
            }
            else
            {
                log.AppendLine($"WARNING: record_id: {doc_id} [{path}] value \"{str_value}\" is not a parseable integer — skipping");
            }
        }
        // else: already int or other non-string — skip silently
    }
    return changed;
}
```

**Step 2 — Add `FetchDocAsync()`:**

```csharp
private async Task<System.Dynamic.ExpandoObject?> FetchDocAsync(string doc_id)
{
    string url = $"{host_db_url}/{db_name}/{Uri.EscapeDataString(doc_id)}";
    string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, config_timer_user_name, config_timer_value);
    return Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(response);
}
```

**Step 3 — Add pre-flight check at start of `execute()`:**

```csharp
// Pre-flight: system must be offline
if (!string.IsNullOrWhiteSpace(offline_date_string))
{
    if (DateTime.TryParse(offline_date_string, null,
        System.Globalization.DateTimeStyles.RoundtripKind, out var offlineDt)
        && DateTime.UtcNow < offlineDt)
    {
        Console.Error.WriteLine("PRE-FLIGHT FAIL: system is not offline. Aborting.");
        Environment.Exit(2);
    }
}
```

The `offline_date_string` should come from config. Add a new constructor parameter `string p_offline_date` and pass it from `Program.cs`. In `Program.cs`, read it from `config.MigrationSettings.OfflineDate` (add this property to `DataMigrationAppConfiguration.MigrationSettings` — a nullable string, defaults to `null`, meaning the check is skipped if not set).

**Step 4 — Rewrite the per-document loop:**

```csharp
int processed_count = 0;
int already_migrated_count = 0;
int failed_count = 0;
const int MaxRetries = 3;

foreach (var case_item in case_response.rows)
{
    var doc = case_item.doc;
    if (doc == null) continue;

    var gs = new C_Get_Set_Value(output_builder);
    var id_result = gs.get_value(doc, "_id");
    var doc_id = id_result.result?.ToString();
    if (doc_id?.Contains("_design") == true) continue;

    if (!is_report_only_mode)
    {
        int attempt = 0;
        SaveResult result = SaveResult.Success;
        System.Dynamic.ExpandoObject current_doc = doc;

        do
        {
            if (attempt > 0)
            {
                current_doc = await FetchDocAsync(doc_id);
                if (current_doc == null)
                {
                    Console.Error.WriteLine($"FATAL: could not re-fetch doc {doc_id} on retry {attempt}. Aborting.");
                    Environment.Exit(1);
                }
            }

            bool changed = ApplyVitalsTypeCorrection(current_doc, new C_Get_Set_Value(output_builder), output_builder, doc_id);
            if (!changed) break; // nothing to save — skip retry loop

            result = await new SaveRecord(host_db_url, db_name, config_timer_user_name, config_timer_value, output_builder, _couchDbHttpClient)
                .save_case(current_doc as IDictionary<string, object>, migration_name);

            if (result == SaveResult.Error)
            {
                Console.Error.WriteLine($"FATAL: doc {doc_id} returned non-retryable error on attempt {attempt + 1}. Aborting.");
                Environment.Exit(1);
            }

            attempt++;
        }
        while (result == SaveResult.Conflict && attempt < MaxRetries);

        if (result == SaveResult.Conflict)
        {
            output_builder.AppendLine($"ERROR: record_id: {doc_id} FAILED after {MaxRetries} retries (conflict).");
            Console.Error.WriteLine($"ERROR: {doc_id} FAILED after {MaxRetries} retries.");
            failed_count++;
        }
        else
        {
            processed_count++;
        }
    }
    else
    {
        // Report-only: just apply the transform to log what would change
        ApplyVitalsTypeCorrection(doc, new C_Get_Set_Value(output_builder), output_builder, doc_id);
    }
}

// Summary
string summary = $"Processed: {processed_count} | Already migrated: {already_migrated_count} | Failed (retries exhausted): {failed_count}";
output_builder.AppendLine(summary);
Console.WriteLine(summary);

if (failed_count > 0)
    Environment.Exit(3);
```

**Note on `already_migrated_count`:** The existing skip logic in `SaveRecord.save_case()` (checking `data_migration_history`) short-circuits the save and returns `SaveResult.Success` without a write. To count already-migrated docs, either: (a) check `data_migration_history` in the migration loop before attempting save, or (b) add a new `SaveResult.AlreadyMigrated` member. Option (a) is simpler — inspect the doc's `data_migration_history` list before calling `ApplyVitalsTypeCorrection`.

### Config addition for pre-flight

In `data-migration/Configuration.cs`, add to `MigrationSettings`:
```csharp
public string OfflineDate { get; set; } // nullable — if null/empty, pre-flight check is skipped
```

In `appsettings.json`, add under `MigrationSettings`:
```json
"OfflineDate": ""
```

### Files to Change

| File | Change |
|------|--------|
| `data-migration/SaveRecord.cs` | Change return type of `save_case()` from `Task<bool>` to `Task<SaveResult>`; add `SaveResult` enum; detect 409 via response body |
| `data-migration/migration-set/VitalsTypeCorrectionMigration.cs` | Extract `ApplyVitalsTypeCorrection()`; add `FetchDocAsync()`; rewrite per-document loop with retry; add pre-flight check; emit summary |
| `data-migration/Configuration.cs` | Add `OfflineDate` (string, nullable) to `MigrationSettings` |
| `data-migration/appsettings.json` | Add `"OfflineDate": ""` under `MigrationSettings` |
| `data-migration/Program.cs` | Pass `config.MigrationSettings.OfflineDate` to `VitalsTypeCorrectionMigration` constructor |
| Other migration classes using `save_case()` | Update callers to handle `SaveResult` instead of `bool` |

### Important Notes

- The `SaveResult.Conflict` detection relies on the CouchDB response body containing `"conflict"`. Verify by inspecting an actual 409 response from your CouchDB instance.
- `Environment.Exit()` calls prevent the using blocks and dispose patterns in `Program.cs` from running — this is acceptable for an aborted migration run.
- The retry loop does NOT call `Environment.Exit(1)` on `SaveResult.Conflict` exhaustion — it continues the loop. Only `SaveResult.Error` causes an immediate exit.
- Run `dotnet build` after changes before testing.

---

## Dev Agent Record

_To be completed by dev agent after implementation._

### Completion Notes

### Change Log

| File | Change |
|------|--------|
| | |
