# cURL Migration Issues - Critical Problems

## Problem Summary

The migration from `cURL` to `CouchDbHttpClient` introduced **critical violations** of AI_CONTEXT.md guidelines:

### ❌ Issue 1: Using `.Result` (FORBIDDEN)

**AI_CONTEXT.md explicitly states:**
- ❌ **NEVER** use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`
- This applies to **all code**, especially Akka.NET actors
- Using `.Result` causes deadlocks and blocks threads

**What I did wrong:**
```csharp
// WRONG - Blocking async call with .Result
string response = _couchDbHttpClient.ExecuteAsync("GET", url, null, user, pass).Result;
```

**Files affected:**
- Process_Central_Pull_list.cs (multiple calls)
- BatchItemProcessor.cs (1 call)
- All export utilities (exporter.cs, mmrds_exporter.cs, core_element_exporter.cs)
- All sync utilities (c_sync_document.cs, c_sync_document.pmss.cs)

### ❌ Issue 2: Incorrect Migration Context

**Original code context:**
- Export utilities: Background jobs (synchronous by design)
- Sync documents: Called on case save (synchronous)
- Process_Central_Pull_list: Akka.NET actor receiving messages

**What should have happened:**
1. **Export/Sync utilities**: Keep synchronous OR convert entire call chain to async
2. **Akka.NET actors**: MUST use proper async patterns (ReceiveAsync + await)

### ❌ Issue 3: Compilation Errors

Current build fails with:
- "Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement"
- Missing httpClientFactory parameter errors
- Type mismatch on timeout parameter

## Recommended Fix Strategy

### Option 1: Rollback to cURL (SAFEST - Recommended)

**Revert the migration for now** and plan it properly:

1. **Keep cURL** in these files temporarily:
   - All export utilities (background jobs)
   - c_sync_document.cs and variants (case save operations)
   - Process_Central_Pull_list.cs (actor - needs async refactor)

2. **Keep CouchDbHttpClient** in these files (already properly async):
   - CustomAuthHandler.cs ✅ (uses await properly)
   - Controllers that were already async

3. **Plan proper async migration:**
   - Export utilities: Convert entire export job system to async
   - Sync documents: Convert case save pipeline to async
   - Actors: Use ReceiveAsync + await patterns

### Option 2: Fix Forward (RISKY)

Convert all affected code to proper async/await patterns:

#### For Akka.NET Actors (Process_Central_Pull_list, BatchProcessor, etc.):

```csharp
// BEFORE (wrong)
public Process_Central_Pull_list(...)
{
    Receive<ScheduleInfoMessage>(message =>
    {
        string result = _couchDbHttpClient.ExecuteAsync(...).Result; // ❌ DEADLOCK RISK
    });
}

// AFTER (correct)
public Process_Central_Pull_list(...)
{
    ReceiveAsync<ScheduleInfoMessage>(async message =>
    {
        string result = await _couchDbHttpClient.ExecuteAsync(...); // ✅ Proper async
    });
}
```

#### For Export Utilities (exporter.cs, mmrds_exporter.cs, etc.):

```csharp
// Current Execute() method is synchronous
public void Execute(export_queue_item queue_item)
{
    string metadata = _couchDbHttpClient.ExecuteAsync("GET", url, ...).Result; // ❌ WRONG
}

// Should be:
public async Task ExecuteAsync(export_queue_item queue_item)
{
    string metadata = await _couchDbHttpClient.ExecuteAsync("GET", url, ...); // ✅ Proper async
}
```

**This requires changing:**
- Export job schedulers to call `await ExecuteAsync()`
- Quartz job configuration
- All callers up the chain

#### For Sync Document Utilities:

```csharp
// Current execute() is synchronous
public void execute(...)
{
    string response = _couchDbHttpClient.ExecuteAsync(...).Result; // ❌ WRONG
}

// Should be:
public async Task ExecuteAsync(...)
{
    string response = await _couchDbHttpClient.ExecuteAsync(...); // ✅ Proper async
}
```

**This requires changing:**
- Case save controllers to await
- De-identification pipeline
- Report generation pipeline

## Immediate Action Required

### Build Fix (Temporary)

To get build passing immediately, revert these files to use cURL:

**Priority 1 - Actors (breaks Akka.NET):**
- model/actor/quartz/Process_Central_Pull_list.cs
- model/actor/quartz/vital-import/BatchProcessor.cs
- model/actor/quartz/vital-import/BatchItemProcessor.cs
- model/actor/quartz/vital-import/PMSS_ItemProcessor.cs

**Priority 2 - Background Jobs:**
- util/exporter/exporter.cs
- util/exporter/mmrds_exporter.cs
- util/core_element_export/core_element_exporter.cs
- util/exporter/export_all_generate_name_map.cs

**Priority 3 - Case Save Pipeline:**
- util/c_sync_document.cs
- util/c_sync_document.pmss.cs

### Successful Migrations (Keep)

These files properly use async/await - DO NOT revert:
- CustomAuthHandler.cs ✅
- Controllers/api/ije_messageController.cs ✅  
- Controllers/api/populate_cdc_instanceController.cs ✅

## Long-term Plan

### Phase 1: Foundation (Next Sprint)
1. Create async variants of export/sync utilities
2. Update schedulers to call async versions
3. Test thoroughly

### Phase 2: Actor Refactoring
1. Convert actor message handlers to ReceiveAsync
2. Use proper PipeTo patterns for async operations
3. Test actor system stability

### Phase 3: Complete Migration
1. Deprecate cURL class entirely
2. Remove all cURL references
3. Update documentation

## Lessons Learned

1. **Never use `.Result`** - It's not just bad practice, it's explicitly forbidden
2. **Context matters** - Actors, background jobs, and controllers have different async requirements
3. **Migration != Simple replacement** - Changing HTTP client requires architectural changes
4. **Test as you go** - Build failures caught this, but earlier testing would have helped

## References

- AI_CONTEXT.md - Line 113: "❌ **NEVER** use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` in actors"
- AI_CONTEXT.md - Line 241: "❌ No `.Result`/`.Wait()`/`.GetAwaiter().GetResult()`"
- Akka.NET Documentation: Async Patterns
- CouchDbHttpClient: mmria.common/getset/CouchDbHttpClient.cs
