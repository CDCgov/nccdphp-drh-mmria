---
baseline_commit: 8c3ee1c08758912c02928633b0d52e4a5c1e2b9c
---

# Story 10.1 — Fix BatchSupervisor Busy-Wait CPU Spin

**Epic:** 10 — CVS PDF Export Tool Reliability
**Story ID:** 10.1
**Status:** review
**Date added:** 2026-07-06

---

## User Story

As a system operator,
When the CVS service is not yet available at startup,
I want the mmria-services BatchSupervisor to wait without consuming CPU,
So that the server remains responsive while retrying the CVS ping.

---

## Acceptance Criteria

**AC-1 — Busy-wait loop replaced with async delay**
Given the CVS service ping returns a non-ready result
When BatchSupervisor waits before the next retry
Then the wait is implemented as `await Task.Delay(CvsServerRetryDelayMs)` — not as a spin loop on `DateTime.Now`
And CPU utilization during the wait is negligible (the thread yields)

**AC-2 — Retry delay duration unchanged**
Given the prior wait was 40 seconds
When the new `await Task.Delay` fires
Then `CvsServerRetryDelayMs` is set to `40 * 1000` ms — the retry interval is unchanged

**AC-3 — Initial batch-list load does not block the constructor**
Given `BatchSupervisor` previously called `GetBatchSet(...).Result` synchronously inside its constructor
When the actor is created
Then the constructor no longer blocks on a CouchDB round-trip
And the batch-list load is deferred via `Self.Tell(InitializeBatchList.Instance)` in `PreStart()`

**AC-4 — Incoming messages are stashed until initialization completes**
Given a message arrives before the initial batch-list load has finished
When `BatchSupervisor` receives it in the `Initializing` behavior
Then the message is stashed via `IWithStash.Stash.Stash()`
And after `GetBatchSet` returns (success or error), `Become(Ready)` and `Stash.UnstashAll()` are called so no messages are lost

**AC-5 — Startup failure is tolerated**
Given `GetBatchSet` throws an exception during `InitializeBatchList` handling
When the exception is caught
Then the actor logs the error and transitions to `Ready` regardless, releasing the stash
And the actor continues to handle subsequent messages normally

---

## Dev Notes — Root Cause and Fix

### Root Cause

`BatchSupervisor` had two related problems introduced before this story:

**Problem 1 — Constructor sync-over-async**
```csharp
// OLD: blocks the construction thread on a CouchDB network call
var alldocs = _mmriaServicesManager.GetBatchSet(
    Program.couchdb_url,
    Program.timer_user_name,
    Program.timer_value
).Result;
foreach (var row in alldocs.rows) { ... }
```

**Problem 2 — Busy-wait loop during CVS ping retry**
```csharp
// OLD: 100% CPU spin for 40 seconds waiting for CVS server
const int Milliseconds_In_Second = 1000;
var next_date = DateTime.Now.AddMilliseconds(40 * Milliseconds_In_Second);
while (DateTime.Now < next_date)
{
    // do nothing
}
```

### Fix

**Problem 1 fix — Deferred init via IWithStash**
```csharp
public sealed class BatchSupervisor : ReceiveActor, IWithStash
{
    private sealed class InitializeBatchList { public static readonly InitializeBatchList Instance = new(); }
    private const int CvsServerRetryDelayMs = 40 * 1000;

    public IStash Stash { get; set; }

    protected override void PreStart()
    {
        Self.Tell(InitializeBatchList.Instance);
    }

    public BatchSupervisor(CouchDbHttpClient couchDbHttpClient)
    {
        // ... field assignments ...
        Become(Initializing);
    }

    private void Initializing()
    {
        ReceiveAsync<InitializeBatchList>(async _ =>
        {
            try
            {
                var alldocs = await _mmriaServicesManager.GetBatchSet(...);
                foreach (var row in alldocs.rows) { ... }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BatchSupervisor failed to load initial batch list: {ex}");
            }
            finally
            {
                Become(Ready);
                Stash.UnstashAll();
            }
        });
        ReceiveAny(_ => Stash.Stash());
    }

    private void Ready()
    {
        // ... existing message handlers ...
    }
}
```

**Problem 2 fix — Async delay**
```csharp
// NEW: yields the thread for 40 seconds
Console.WriteLine($"{DateTime.Now:o} BatchSupervisor: waiting {CvsServerRetryDelayMs / 1000}s before retry attempt {ping_count + 1}...");
await Task.Delay(CvsServerRetryDelayMs);
Console.WriteLine($"{DateTime.Now:o} BatchSupervisor: wait complete, retrying CVS ping (attempt {ping_count + 1}).");
```

### Files Changed

| File | Change |
|------|--------|
| `nccdphp-drh-mmria-services/mmria.services/Actors/BatchSupervisor.cs` | Implement `IWithStash`, add `InitializeBatchList` inner class, add `CvsServerRetryDelayMs` constant, move batch-list load from constructor to deferred async handler, replace busy-wait with `await Task.Delay` |

### Sequencing

This story is independent of all other Epic 10 stories — it touches only the mmria-services layer.
