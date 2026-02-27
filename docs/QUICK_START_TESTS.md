# Quick Start: Running Memory Leak Tests

## TL;DR - Run Tests Now

### Server Tests
```powershell
cd c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server.tests
dotnet test
```

### Services Tests
```powershell
cd c:\repos\nccdphp-drh-mmria\nccdphp-drh-mmria-services\mmria.services.tests
dotnet test
```

## Requirements

- ✅ CouchDB running on `http://localhost:5984`
- ✅ .NET 9.0 SDK
- ✅ Windows PowerShell

## Expected Results

Both test suites should output:
```
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 2 s
```

## What Gets Tested

- **LINQ Filtering** - No memory growth from chained operations
- **Connection Pool** - 100 CouchDB connections without leaks
- **Event Subscriptions** - 500 subscribe/unsubscribe cycles clean up properly
- **Async Operations** - Large async batches don't accumulate memory
- **Collection Processing** - Memory remains stable throughout

## Test Databases

Tests create temporary databases named:
```
mmria_test_jurisdiction1_memory_leaks_[timestamp]
```

Expected CouchDB errors (404 "not found") are normal - tests handle gracefully.

## Need Help?

See [MEMORY_LEAK_TESTS.md](./MEMORY_LEAK_TESTS.md) for:
- Detailed troubleshooting
- Configuration options
- CI/CD integration
- Code coverage reports
