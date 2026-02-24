# Functional Integration Tests - Getting Started

## Status: ✅ Ready to Implement

All base infrastructure is in place. **31 placeholder tests** are discoverable and awaiting implementation.

### Test Counts by Project

| Project | Tests | Status |
|---------|-------|--------|
| **mmria-server.tests** (FunctionalIntegrationTests) | 16 | ✅ Discoverable |
| **mmria.services.tests** (ServiceFunctionalIntegrationTests) | 15 | ✅ Discoverable |
| **Memory Leak Tests** (existing) | 10 | ✅ Running |
| **Total** | **31** | ✅ Ready |

## Quick Start: Run Placeholder Tests

```powershell
# Server functional tests (all passing placeholders)
cd c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server.tests
dotnet test --filter "Category=CaseOperations"
dotnet test --filter "Category=Configuration"
dotnet test --filter "Category=MultiTenant"
dotnet test --filter "Category=Search"
dotnet test --filter "Category=Validation"

# Services functional tests (all passing placeholders)
cd c:\repos\nccdphp-drh-mmria\nccdphp-drh-mmria-services\mmria.services.tests
dotnet test --filter "Category=BackgroundJobs"
dotnet test --filter "Category=BatchProcessing"
dotnet test --filter "Category=EventProcessing"
dotnet test --filter "Category=ImportExport"
dotnet test --filter "Category=ServiceIntegration"
```

## Next Steps: Implement Tests

### Step 1: Choose a Test to Implement

**Server Tests (recommended to start):**
```csharp
// TestCreateCase - Medium complexity (good starting point)
[Test]
[Category("CaseOperations")]
public async Task TestCreateCase()
{
    // TODO: Implement case creation test
}
```

**Services Tests (more complex):**
```csharp
// TestBackgroundJobExecution - Medium complexity
[Test]
[Category("BackgroundJobs")]
public async Task TestBackgroundJobExecution()
{
    // TODO: Implement background job execution test
}
```

### Step 2: Use Helper Classes

**For Server Tests:**
```csharp
var helper = new CaseDataHelper(_httpClient, _testDatabaseUrl);

// Create case
var case_data = helper.CreateCompleteCase("case-001");
await helper.SaveCaseAsync(case_data);

// Query cases
var query = helper.CreateQuery()
    .WithStatus("open")
    .WithJurisdiction("jurisdiction1")
    .Build();

// Validate
var errors = helper.ValidateCaseData(case_data);
```

**For Services Tests:**
```csharp
var helper = new JobDataHelper(_httpClient, _testDatabaseUrl);

// Create job/event
var job = helper.CreateScheduledJob("cleanup", "0 2 * * *");
var @event = helper.CreateEvent("case_updated", payload);

// Save and track
await helper.SaveJobAsync(job);
await helper.UpdateJobStatusAsync(jobId, "completed");
```

### Step 3: Write Assertions

```csharp
// Verify creation
Assert.That(result.Status, Is.EqualTo("created"));

// Verify retrieval
Assert.That(retrieved, Is.Not.Null);
Assert.That(retrieved["case_number"], Is.EqualTo("2024-001"));

// Verify queries
Assert.That(results, Has.Count.EqualTo(1));
Assert.Multiple(() =>
{
    Assert.That(result.Status, Is.EqualTo("open"));
    Assert.That(result.Jurisdiction, Is.EqualTo("jurisdiction1"));
});
```

## Test Implementation Checklist

For each test you implement:

- [ ] Read the TODO comment in the test
- [ ] Understand what business logic is being tested
- [ ] Create test data using helper classes
- [ ] Execute the operation (create, update, query, etc.)
- [ ] Add multiple assertions to verify behavior
- [ ] Test both success and error paths when applicable
- [ ] Clean up created data in teardown (or rely on DatabaseTestHelper cleanup)
- [ ] Run: `dotnet test --filter "Name=TestYourTestName"`
- [ ] Verify test passes and all assertions execute
- [ ] Commit with clear message: "Implement TestYourTestName - validates [behavior]"

## Implementation Priority Order

**Recommended order for maximum value:**

1. **TestCreateCase** - Foundation for all case operations
2. **TestRetrieveCase** - Read verification
3. **TestUpdateCase** - Update workflow
4. **TestCaseSearch** - Query functionality
5. **TestConfigurationLoading** - Configuration validation
6. **TestCaseIsolationBetweenTenants** - Critical for multi-tenant
7. **TestBackgroundJobExecution** - Services core
8. **TestEventProcessing** - Services events
9. **TestBatchProcessing** - High-impact bulk operations
10. **Remaining tests** - Additional coverage

## Available Helper Methods

### CaseDataHelper (Server)
- `CreateMinimalCase(id)` - Smallest valid case
- `CreateCompleteCase(id, overrides)` - Full case with defaults
- `SaveCaseAsync(caseData)` - Persist to DB
- `GetCaseAsync(id)` - Retrieve from DB
- `UpdateCaseStatusAsync(id, status)` - Change status
- `AssignCaseToAbstractorAsync(id, abstractor)` - Assign
- `ValidateCaseStructure(data)` - Check fields exist
- `ValidateCaseData(data)` - Validate constraints
- `CreateQuery()` - Build CouchDB query
- `GenerateCaseNumber()` - Create unique number

### JobDataHelper (Services)
- `CreateJob(type, payload)` - One-time job
- `CreateScheduledJob(type, cron, payload)` - Recurring job
- `CreateEvent(type, payload)` - Create event
- `CreateBatchOperation(type, count, metadata)` - Batch
- `SaveJobAsync(job)` - Persist job
- `GetJobAsync(id)` - Retrieve job
- `UpdateJobStatusAsync(id, status, result)` - Change status
- `RetryJobAsync(id, delaySeconds)` - Queue retry
- `SaveEventAsync(event)` - Persist event
- `MarkEventProcessedAsync(id)` - Mark done
- `MoveEventToDeadLetterQueueAsync(id, reason)` - DLQ
- `UpdateBatchProgressAsync(id, processed, failed, skipped)` - Progress
- `CompleteBatchAsync(id, startTime)` - Mark complete
- `CreateJobQuery()` - Query builder for jobs
- `CreateEventQuery()` - Query builder for events

## Documentation

- **Full Guide:** [FUNCTIONAL_INTEGRATION_TESTS.md](./FUNCTIONAL_INTEGRATION_TESTS.md)
- **Memory Leak Tests:** [MEMORY_LEAK_TESTS.md](./MEMORY_LEAK_TESTS.md)
- **Quick Start:** [QUICK_START_TESTS.md](./QUICK_START_TESTS.md)

## Test Execution Examples

```powershell
# Run specific test by name
dotnet test --filter "Name=TestCreateCase"

# Run all in a category
dotnet test --filter "Category=CaseOperations"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Generate test report
dotnet test --logger "trx"

# Skip tests with timeout
dotnet test --filter "Name!=SlowTest"
```

## Benefits After Implementation

✅ **Validate CRUD Operations** - Ensure case management works correctly  
✅ **Test Multi-Tenancy** - Verify data isolation between jurisdictions  
✅ **Configuration Verification** - Confirm settings applied correctly  
✅ **Service Integration** - Validate background jobs and events  
✅ **Business Rules** - Enforce validation and state transitions  
✅ **Prevent Regressions** - Catch issues before production  

## Questions?

Refer to:
1. Comments in the placeholder tests (each has TODO with guidance)
2. [FUNCTIONAL_INTEGRATION_TESTS.md](./FUNCTIONAL_INTEGRATION_TESTS.md) - Complete reference
3. `CaseDataHelper.cs` - See existing helper implementations
4. `JobDataHelper.cs` - See job/event helper patterns
5. `MemoryLeakTests.cs` - See how real tests are structured

---

**All 31 tests are ready for implementation. Pick one, replace the `Assert.Pass()` with real logic, and commit!** 🚀
