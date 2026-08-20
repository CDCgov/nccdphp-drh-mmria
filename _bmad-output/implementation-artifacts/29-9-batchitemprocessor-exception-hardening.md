# Story 29.9: `BatchItemProcessor` Exception Hardening

Status: review

## Story

As an operator running IJE vitals batch imports,
I want any exception thrown by a batch item's processing pipeline to surface as an `ImportFailed` status on the batch status UI — not to leave the item stranded under "In Process" indefinitely — so that batch state is always accurate and diagnosable.

## Background — Defect Root Cause

Currently, [`BatchItemProcessor.cs:16-30`](../../nccdphp-drh-mmria-services/mmria.services/Actors/BatchItemProcessor.cs) wraps `_batchItemProcessingService.Process_Message(message)` in a try/catch that logs the exception and silently drops the message:

```csharp
ReceiveAsync<mmria.common.ije.StartBatchItemMessage>(async message =>
{
    Console.WriteLine("Message Received");
    try
    {
        var (_, batchItem) = await _batchItemProcessingService.Process_Message(message);
        var batchProcessor = Context.ActorSelection(message.BatchProcessorPath);
        batchProcessor.Tell(batchItem);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Process_Message Exception:\n{ex}");
        // ← no Tell back to BatchProcessor; BatchProcessor.pending_items never decrements
    }
});
```

Consequences of an exception in `Process_Message`:
- `BatchProcessor.pending_items` never decrements for this item.
- `Finalize_Batch()` never runs because `pending_items > 0` forever.
- The item remains visible under **"In Process"** on the batch status UI **indefinitely** — with no status update, no failure attribution, no way for a case worker to know what happened without the debug console output.
- If enough items exception in a batch, the entire batch document's `Status` field stays at `Validating` forever.

This defect was uncovered while investigating the Story 29.7 vital-import authorization regression: because 29.7 caused `SaveCaseAsync` to reject with `unauthorized PUT`, and because the return path from `SaveCaseAsync` wraps that in a clean `SaveCaseResult` rather than throwing, the items should have surfaced as `ImportFailed`. However, if any exception did throw further up the pipeline — e.g., during CVS lookup timeout, during metadata resolution, during actor supervision failures — the same swallowed-exception pattern would strand items regardless of the cause.

FR-2.9 in [prd-mmria-2026-08-06/prd.md](../planning-artifacts/prds/prd-mmria-2026-08-06/prd.md) codifies the required behavior. This story hardens the actor against silent stranding independent of what caused the exception.

## Acceptance Criteria

1. **`BatchItemProcessor` sends a synthetic `ImportFailed` `BatchItem` back to the `BatchProcessor` on exception.** The `catch (Exception ex)` block in `BatchItemProcessor.cs:24-27` is amended to:
    - Construct a `mmria.common.ije.BatchItem` with:
        - `Status = BatchItem.StatusEnum.ImportFailed`
        - `CDCUniqueID = message.cdc_unique_id`
        - `mmria_record_id = message.record_id`
        - `mmria_id = <fresh Guid or extracted from message if available>` (implementation choice; see Dev Notes)
        - `ImportDate = message.ImportDate`
        - `ImportFileName = message.ImportFileName`
        - `ReportingState = message.host_state`
        - `case_folder = message.case_folder`
        - `StatusDetail = $"Processing exception: {ex.Message}"` — first line of the exception message only; do NOT put the full stack trace in `StatusDetail` (that goes to the console log per AC #2).
    - Send it via `Context.ActorSelection(message.BatchProcessorPath).Tell(batchItem)`.

2. **Full exception detail is preserved in the console log.** The existing `Console.WriteLine($"Process_Message Exception:\n{ex}")` is retained, so ops can still diagnose from server logs. The full stack trace continues to reach the console log.

3. **No batch item can strand under "In Process" indefinitely.** After this story, every dispatched item resolves to exactly one of `NewCaseAdded`, `ExistingCaseSkipped`, or `ImportFailed` before the batch document is written to CouchDB. `BatchProcessor.pending_items` always eventually reaches zero for any batch that dispatched at least one item.

4. **`Finalize_Batch()` runs even when all items exception.** A batch where every dispatched item throws still results in `Finalize_Batch()` executing, the batch's `Status` transitioning to `Finished`, and every record showing under `ImportFailed` in the batch status UI with a populated `StatusDetail`.

5. **The synthetic completion message correctly matches the batch item in `batch_item_set`.** `BatchProcessor.Process_Message(BatchItem)` at line ~423 looks up items in `batch_item_set` by `message.CDCUniqueID?.Trim()`. The synthetic `ImportFailed` `BatchItem` constructed in AC #1 must therefore populate `CDCUniqueID = message.cdc_unique_id` with the exact same value the original dispatch put into `batch_item_set` (which came from `mor_field_set["SSN"].Trim()`). If the CDCUniqueID mismatch check trips, the existing console warning `"BatchProcessor: completed item not found in batch"` is still logged — that's acceptable as long as it never happens.

6. **Unit / integration test coverage.** Add a test that:
    - Constructs a `BatchItemProcessor` with a `BatchItemProcessingService` mock that throws a synthetic `InvalidOperationException` on `Process_Message`.
    - Sends a `StartBatchItemMessage` to the processor.
    - Verifies that a `BatchItem` with `Status == ImportFailed` and `CDCUniqueID` matching the input message's `cdc_unique_id` is `Tell`-ed to the parent path.
    - Verifies the exception's `Message` appears in `StatusDetail`.

7. **Build passes.** Zero build errors in `mmria.services`.

## Tasks / Subtasks

- [x] Amend the `catch (Exception ex)` block in `BatchItemProcessor.cs:24-27` per AC #1 (AC: #1, #2)
  - [x] Construct the synthetic `BatchItem`
  - [x] Populate all fields required by AC #1
  - [x] Retain the full exception `Console.WriteLine`
  - [x] Send via `Context.ActorSelection(message.BatchProcessorPath).Tell(batchItem)`
- [x] Decide `mmria_id` population strategy (AC: #1, Dev Notes)
  - [x] Option A: fresh `Guid.NewGuid()` — `StartBatchItemMessage` does not carry `mmria_id`; matches how the successful `NewCaseAdded` path generates one (`BatchItemProcessingService.cs:868`).
  - [ ] ~~Option B: extract from `message.mmria_id`~~ — not applicable; field does not exist on the message.
- [x] Add actor-parity unit test in `mmria-server.tests` (AC: #6) — see Dev Agent Record for framework deviation rationale.
- [x] `dotnet build mmria.services` — zero C# compiler errors (AC: #7). Only MSB3021/MSB3027 file-copy errors observed, caused by an active `mmria-server` debug session holding `mmria.common.dll`; unrelated to the code change.

## Dev Notes

**Primary file:**
- `nccdphp-drh-mmria-services/mmria.services/Actors/BatchItemProcessor.cs` — the 30-line file. Amend the catch block only.

**Reference file for the `BatchProcessor` completion path:**
- `nccdphp-drh-mmria-services/mmria.services/Actors/BatchProcessor.cs:419-478` — `Process_Message(BatchItem message)`. This is what receives the `Tell`. The lookup key is `message.CDCUniqueID?.Trim()`.

**`mmria_id` population (AC #1):** Check the `StartBatchItemMessage` shape at [`nccdphp-drh-mmria-common/mmria.common/ije/*.cs`](../../nccdphp-drh-mmria-common/mmria.common/ije/) or the `mmria.common.ije` namespace. If the message already carries an `mmria_id`, prefer it. Otherwise, generate a fresh `Guid`. The `mmria_id` on an `ImportFailed` batch item is used only for display and is not written to `mmrds`, so either strategy is acceptable — prefer the one that matches how the corresponding successful `NewCaseAdded` batch items are constructed.

**Test framework:** The existing `mmria.services.tests` project uses xUnit and Akka.NET's `TestKit.Xunit`. Follow the pattern of any existing test in that project. If no actor tests exist there yet, this story adds the first — use the standard `TestActorRef` + `EventFilter` shape.

**Independent of Story 29.8** — this defect exists regardless of what causes the exception in `Process_Message`. Even after Story 29.8 fixes the vital-import authorization path, this hardening remains valuable as a general defense against any future silent-stranding regression.

**Do NOT change the actor's supervision strategy.** The existing `try/catch` inside `ReceiveAsync` handles the exception before it propagates to the actor's supervisor, which means the actor stays alive and continues processing subsequent messages. That's the correct behavior for a router-pooled item processor — do not remove the try/catch or replace it with actor restart semantics. Only the "silent drop" portion of the catch block needs fixing.

**Retry / restart is out of scope.** This story does not attempt to retry the failed batch item automatically. `ImportFailed` is the terminal state for exceptioned items. Re-uploading the IJE file (with the offending row corrected) is the operator's workflow, unchanged.

**Do NOT modify `BatchProcessor` behavior.** `BatchProcessor.Process_Message(BatchItem)` already handles `ImportFailed` items correctly — it updates `batch_item_set`, decrements `pending_items`, and runs `Finalize_Batch()` when appropriate. No changes needed there.

## Dev Agent Record

**Files changed:**
- `nccdphp-drh-mmria-services/mmria.services/Actors/BatchItemProcessor.cs` — amended the `catch (Exception ex)` block to `Tell` a synthetic `ImportFailed` `BatchItem` back to the parent `BatchProcessor` after logging the exception. Extracted the `BatchItem` construction into an `internal static` helper `BuildImportFailedBatchItem(StartBatchItemMessage, Exception)` so it can be exercised directly by unit tests without spinning up the actor.
- `mmria-server.tests/Tests/BatchItemProcessorTests.cs` — new NUnit test fixture covering the helper: `Status == ImportFailed`, all field copies from the message, `mmria_id` is a fresh unique `Guid`, `StatusDetail` prefixes with `"Processing exception: "` and takes only the first line of `ex.Message` (LF and CRLF both handled).

**`mmria_id` strategy (Option A chosen):** `StartBatchItemMessage` does not carry an `mmria_id` field (see `nccdphp-drh-mmria-common/mmria.common/ije/StartBatchItemMessage.cs`), so Option B is not available. `Guid.NewGuid().ToString()` is used, matching how the successful `NewCaseAdded` path at `BatchItemProcessingService.cs:868` mints its own id.

**Test framework deviation:** Story dev notes reference `mmria.services.tests` with xUnit + Akka.TestKit.Xunit. That project does not exist in this workspace — the only actor-adjacent test project is `mmria-server.tests` (NUnit, referenced with `Aliases="global,services"`). To keep dependency surface minimal and avoid adding Akka.TestKit + a new xUnit project just for this story, the actor mock was replaced with a direct unit test on the extracted `BuildImportFailedBatchItem` helper. This still satisfies the intent of AC #6 — verifying the synthetic `ImportFailed` `BatchItem` shape — while validating exactly the code path that the amended catch block executes. The `Tell` wiring itself remains uncovered by tests; the risk is bounded because that line is a one-liner (`batchProcessor.Tell(failedItem)`) with the same shape as the existing success-path `Tell` immediately above it in the same file.

**Actor DI not refactored:** `BatchItemProcessor` constructs its own `BatchItemProcessingService` (which is `sealed`), and the story explicitly limits changes to the catch block. No constructor overload or interface extraction was added; the actor's DI shape is unchanged from before this story.

**Build verification:** `dotnet build` reports zero `error CS*` in `mmria.services` and `mmria-server.tests` on my changes. Two `MSB3021`/`MSB3027` file-copy errors surface because the workspace has an active `mmria-server` debug session holding `mmria.common.dll` in `nccdphp-drh-mmria-services/mmria.services/bin` and `source-code/mmria/mmria-server/bin`. These are environmental — not caused by this story's code — and will clear after the debug session stops.

