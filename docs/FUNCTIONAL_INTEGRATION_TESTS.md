# Functional Integration Tests Guide

## Overview

Functional Integration Tests validate business logic and real-world workflows. Unlike memory leak tests that focus on resource stability, functional tests verify:

- ✅ CRUD operations work correctly
- ✅ Configuration is properly applied
- ✅ Data isolation per tenant is enforced
- ✅ Search and filtering function as expected
- ✅ Business rules are enforced
- ✅ Background jobs process correctly
- ✅ Events are handled in order
- ✅ Service integrations work end-to-end

## Test Structure

### Server Tests: FunctionalIntegrationTests
Located: `source-code/mmria/mmria-server.tests/FunctionalIntegrationTests.cs`

**Test Categories:**
1. **Case CRUD Operations** - Create, Read, Update, Delete cases
2. **Configuration & Metadata** - Config loading, validation, overrides
3. **Multi-Tenant Isolation** - Tenant data separation, config per tenant
4. **Search & Query** - Finding and filtering cases
5. **Data Validation** - Business rules and constraints

### Services Tests: ServiceFunctionalIntegrationTests
Located: `nccdphp-drh-mmria-services/mmria.services.tests/ServiceFunctionalIntegrationTests.cs`

**Test Categories:**
1. **Background Job Processing** - Job execution, retry, scheduling
2. **Batch Operations** - Batch processing, error handling, large batches
3. **Event Processing** - Event handling, ordering, dead letter queues
4. **Data Import/Export** - Moving data in and out
5. **Service Integration** - CDC notifications, API communication

## Helper Classes

### CaseDataHelper (Server)
Utilities for case operations:

```csharp
// Create test case
var case_data = helper.CreateCompleteCase("case-123", new Dictionary<string, object>
{
    { "abstractor", "test_user" }
});

// Save to database
await helper.SaveCaseAsync(case_data);

// Query cases
var query = helper.CreateQuery()
    .WithStatus("open")
    .WithJurisdiction("jurisdiction1")
    .Build();
```

### JobDataHelper (Services)
Utilities for job and event operations:

```csharp
// Create job
var job = helper.CreateScheduledJob("cleanup_old_cases", "0 2 * * *");
await helper.SaveJobAsync(job);

// Create event
var @event = helper.CreateEvent("case_updated", new Dictionary<string, object>
{
    { "case_id", "case-123" },
    { "updated_fields", new[] { "status" } }
});
await helper.SaveEventAsync(@event);

// Update progress
await helper.UpdateBatchProgressAsync(batchId, processed: 50, failed: 2, skipped: 0);
```

## Implementing a Functional Test

### Step 1: Inherit from Test Base Class

```csharp
[Test]
[Category("CaseOperations")]
public async Task TestCreateCase()
{
    // Test implementation
}
```

### Step 2: Use OneTimeSetUp for Initialization

The test framework automatically:
- Creates test database
- Loads configuration
- Initializes HTTP client
- Populates seed data

### Step 3: Implement Test Logic

Example: Testing case creation

```csharp
[Test]
[Category("CaseOperations")]
public async Task TestCreateCase()
{
    // 1. Create test case object
    var caseData = new Dictionary<string, object>
    {
        { "_id", "test-case-001" },
        { "case_number", "2024-001" },
        { "jurisdiction", "jurisdiction1" },
        { "status", "open" },
        { "created_date", DateTime.UtcNow }
    };

    // 2. Persist to CouchDB
    var caseId = await _httpClient.CreateDocumentAsync(_config.name_value["database_url"], caseData);

    // 3. Verify creation success
    Assert.That(caseId, Is.EqualTo("test-case-001"));

    // 4. Retrieve and verify data
    var retrievedCase = await _httpClient.GetDocumentAsync(_config.name_value["database_url"], caseId);
    Assert.That(retrievedCase["case_number"], Is.EqualTo("2024-001"));
}
```

### Step 4: Use Helpers for Common Operations

```csharp
[Test]
[Category("CaseOperations")]
public async Task TestCaseWorkflow()
{
    var helper = new CaseDataHelper(_httpClient, _config.name_value["database_url"]);

    // Create case
    var caseData = helper.CreateCompleteCase("case-workflow-test");
    await helper.SaveCaseAsync(caseData);

    // Assign to abstractor
    await helper.AssignCaseToAbstractorAsync("case-workflow-test", "john_doe");

    // Verify assignment
    var updated = await helper.GetCaseAsync("case-workflow-test");
    Assert.That(updated["abstractor"], Is.EqualTo("john_doe"));
    Assert.That(updated["status"], Is.EqualTo("assigned"));
}
```

## Running Functional Tests

### Run All Functional Tests

```powershell
cd c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server.tests
dotnet test --filter "FullyQualifiedName~FunctionalIntegrationTests"
```

### Run Specific Category

```powershell
# Run only Case CRUD tests
dotnet test --filter "Category=CaseOperations"

# Run only Configuration tests
dotnet test --filter "Category=Configuration"

# Run only Multi-Tenant tests
dotnet test --filter "Category=MultiTenant"
```

### Run Specific Test

```powershell
dotnet test --filter "Name=TestCreateCase"
```

### Run Services Functional Tests

```powershell
cd c:\repos\nccdphp-drh-mmria\nccdphp-drh-mmria-services\mmria.services.tests
dotnet test --filter "FullyQualifiedName~ServiceFunctionalIntegrationTests"
```

## Best Practices

### 1. Use Separate Test Databases

Each test class gets its own database with timestamp:
```
mmria_test_jurisdiction1_functional_integration_20260224_143025
mmria_test_jurisdiction1_services_functional_20260224_143026
```

### 2. Seed Data in OneTimeSetUp

Populate test database once at start:
```csharp
private async Task SeedTestDataAsync()
{
    // Insert reference data
    // Create lookup tables
    // Populate configuration
}
```

### 3. Clean Up in OneTimeTearDown

Automatic cleanup:
```csharp
[OneTimeTearDown]
public async Task OneTimeTearDownAsync()
{
    if (_dbHelper != null)
    {
        await _dbHelper.CleanupAsync();
    }
}
```

### 4. Use Meaningful Test Names

```csharp
// Good
TestCreateCaseWithValidData()
TestCreateCaseWithInvalidStatus()
TestTenantCannotAccessOtherTenantCase()

// Poor
Test1()
TestCase()
TestDB()
```

### 5. Test Both Happy Path and Error Cases

```csharp
[Test]
public async Task TestCreateCaseWithValidData() { ... }

[Test]
public async Task TestCreateCaseWithMissingRequiredField() { ... }

[Test]
public async Task TestCreateCaseWithInvalidStatus() { ... }
```

### 6. Use Assertions Clearly

```csharp
// Good
Assert.That(result.Status, Is.EqualTo("created"));
Assert.That(result.CaseId, Is.Not.Empty);
Assert.That(errors, Is.Empty);

// Avoid
Assert.IsTrue(result);
Assert.IsNotNull(result);
```

## Common Test Patterns

### Testing CRUD Operations

```csharp
[Test]
public async Task TestCreateReadUpdateDelete()
{
    // CREATE
    var case_data = CreateCase("crud-test");
    var caseId = await SaveCaseAsync(case_data);

    // READ
    var retrieved = await GetCaseAsync(caseId);
    Assert.That(retrieved, Is.Not.Null);

    // UPDATE
    retrieved["status"] = "completed";
    await SaveCaseAsync(retrieved);

    // VERIFY UPDATE
    var updated = await GetCaseAsync(caseId);
    Assert.That(updated["status"], Is.EqualTo("completed"));

    // DELETE
    await DeleteCaseAsync(caseId);

    // VERIFY DELETION
    var deleted = await GetCaseAsync(caseId);
    Assert.That(deleted, Is.Null);
}
```

### Testing Multi-Tenant Isolation

```csharp
[Test]
public async Task TestTenantIsolation()
{
    // Create in Tenant A
    var caseA = CreateCase("case-tenant-a", "jurisdiction1");
    await SaveCaseAsync(caseA);

    // Create in Tenant B
    var caseB = CreateCase("case-tenant-b", "jurisdiction2");
    await SaveCaseAsync(caseB);

    // Query Tenant A
    var resultsA = await QueryCasesByJurisdiction("jurisdiction1");
    Assert.That(resultsA, Has.Count.EqualTo(1));
    Assert.That(resultsA[0]["_id"], Is.EqualTo("case-tenant-a"));

    // Query Tenant B
    var resultsB = await QueryCasesByJurisdiction("jurisdiction2");
    Assert.That(resultsB, Has.Count.EqualTo(1));
    Assert.That(resultsB[0]["_id"], Is.EqualTo("case-tenant-b"));
}
```

### Testing Configuration Loading

```csharp
[Test]
public async Task TestConfigurationPrecedence()
{
    // Set environment variable
    Environment.SetEnvironmentVariable("couchdb_url", "http://env-couchdb:5984");

    // Load configuration (should use env var)
    var configLoader = new MultiTenantConfigurationLoader(null);
    var url = configLoader.GetConfig("couchdb_url");
    Assert.That(url, Is.EqualTo("http://env-couchdb:5984"));

    // Clean up
    Environment.SetEnvironmentVariable("couchdb_url", null);
}
```

## Troubleshooting

### Test Database Not Created

**Issue:** Tests fail with "database not found"

**Solution:**
1. Verify CouchDB is running
2. Check `appsettings.test.json` has correct URL
3. Ensure `DatabaseTestHelper` initializes before tests

```csharp
[OneTimeSetUp]
public async Task SetUp()
{
    _dbHelper = new DatabaseTestHelper("jurisdiction1", "test_category");
    await _dbHelper.InitializeAsync(); // Explicitly initialize
}
```

### Configuration Not Loading Correctly

**Issue:** Tests use wrong configuration values

**Solution:**
1. Verify configuration file exists in test project root
2. Check appsettings.test.json syntax is valid JSON
3. Use `TestConfigurationLoader` for auto-discovery

```csharp
var loader = new TestConfigurationLoader();
var config = await loader.LoadAsync();
```

### Tests Timeout

**Issue:** Tests hang or timeout after N seconds

**Solution:**
1. Increase test timeout in project settings
2. Check if waiting for external resource (add timeout)
3. Verify no deadlocks in async operations

```csharp
[Test]
[Timeout(30000)] // 30 seconds
public async Task LongRunningTest()
{
    // ...
}
```

## See Also

- [QUICK_START_TESTS.md](./QUICK_START_TESTS.md) - Running memory leak tests
- [MEMORY_LEAK_TESTS.md](./MEMORY_LEAK_TESTS.md) - Memory leak testing details
- [AI Context](./ai/AI_CONTEXT.md) - Architecture and design patterns
