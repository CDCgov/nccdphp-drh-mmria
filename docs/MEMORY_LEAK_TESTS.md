# Memory Leak Testing Guide

## Overview

The MMRIA project includes automated memory leak detection tests for both the server application and background services. These tests verify that:

- LINQ operations don't accumulate memory over time
- CouchDB connection pools properly recycle connections
- Event subscriptions clean up properly
- Async operations don't leak resources
- Large collection processing remains stable

## Prerequisites

Before running the tests, ensure you have:

1. **.NET 9.0 SDK** installed
2. **CouchDB running locally** on port 5984
   - Default URL: `http://localhost:5984`
   - Can be configured in `appsettings.test.json`
3. Access to the workspace folders:
   - `c:\repos\nccdphp-drh-mmria` (server tests)
   - `c:\repos\nccdphp-drh-mmria-services` (services tests)

### Verify CouchDB is Running

```powershell
# Test CouchDB connectivity
curl http://localhost:5984/

# Should return: {"couchdb":"Welcome",...}
```

## Running the Tests

### 1. Server Tests (mmria-server.tests)

**From PowerShell:**

```powershell
cd c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server.tests
dotnet test
```

**Using VS Code Test Explorer:**
- Open the VS Code Explorer sidebar
- Navigate to the Test view
- Find `mmria_server_tests` in the test tree
- Click "Run All Tests"

**With Detailed Output:**

```powershell
cd c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server.tests
dotnet test --logger "console;verbosity=detailed"
```

### 2. Services Tests (mmria.services.tests)

**From PowerShell:**

```powershell
cd c:\repos\nccdphp-drh-mmria\nccdphp-drh-mmria-services\mmria.services.tests
dotnet test
```

**Using VS Code Test Explorer:**
- Open the VS Code Explorer sidebar
- Navigate to the Test view
- Find `mmria.services.tests` in the test tree
- Click "Run All Tests"

### 3. Run Both Test Suites

```powershell
# Server tests
cd c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server.tests
dotnet test

# Services tests
cd c:\repos\nccdphp-drh-mmria\nccdphp-drh-mmria-services\mmria.services.tests
dotnet test
```

## Test Results Interpretation

### Successful Test Run

```
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 2 s
```

All 5 memory leak tests should pass:
1. ✅ LINQ Filtering Pattern Test
2. ✅ CouchDB Connection Pool Test
3. ✅ Event Subscription Test
4. ✅ Async Operations Test
5. ✅ Large Collection Processing Test

### Test Output Details

Each test displays memory metrics:

```
LINQ Filtering Pattern Test:
  Single-pass growth: 0 MB
  Chained LINQ growth: 0 MB
  Ratio (chained/optimal): 0.07
```

**Key Metrics:**
- `Single-pass growth`: Expected to be ≤ 1 MB
- `Chained LINQ growth`: Expected to be ≤ 1 MB
- `Ratio`: Should be < 0.15 (15% overhead acceptable)

```
CouchDB Connection Pool Test:
  Database URL: http://tenant1-couchdb.local:6984/mmria_test_...
  Initial Memory: 1 MB
  Final Memory: 1 MB
  Leaked Memory: 0 MB
  Connection Attempts: 100
```

**Key Metrics:**
- `Leaked Memory`: Should be 0 MB
- `Connection Attempts`: Tests 100 rapid connections
- `Initial/Final Memory`: Should match (no growth)

### Test Database Naming

Tests create temporary databases with timestamped names:

```
mmria_test_jurisdiction1_memory_leaks_20260224_193839
mmria_test_tenant1_memory_leaks_20260224_194121
```

These databases are created for testing and logged errors (404 "not found") are expected if the database doesn't exist yet - they do not indicate test failure.

## Configuration

### Local Development Configuration

Tests use `appsettings.test.json` in each test project:

```json
{
  "mmria_settings": {
    "is_environment_based": "false",
    "couchdb_url": "http://localhost:5984",
    "multi_tenant_jurisdictions": "jurisdiction1",
    "timer_user_name": "mmrds",
    "timer_password": "mmrds",
    "config_id": "config_jurisdiction1",
    "shared_config_id": "shared_config",
    "test_db_prefix": "mmria_test_"
  }
}
```

### Environment Variable Override (CI/CD)

For containerized environments, set environment variables:

```bash
export is_environment_based=true
export couchdb_url=http://couchdb-service:5984
export multi_tenant_jurisdictions=jurisdiction1,jurisdiction2
export timer_user_name=mmrds
export timer_password=mmrds
export config_id=config_mmria
export shared_config_id=shared_config_mmria
```

## Troubleshooting

### Tests Fail to Connect to CouchDB

**Error:** `HTTP 404 - CouchDB Error: not_found`

**Solution:**
1. Verify CouchDB is running: `curl http://localhost:5984/`
2. Check `appsettings.test.json` has correct `couchdb_url`
3. If using Docker: ensure port 5984 is exposed

### Stack Overflow or Infinite Recursion

**Error:** `Stack overflow` in `IsEnvironmentBased()` or `GetConfig()`

**Solution:**
1. Ensure mmria.common is rebuilt: `dotnet clean && dotnet build`
2. Check MultiTenantConfigurationLoader.cs is the latest version
3. Rebuild all projects: `dotnet build --force`

### Tests Hang or Timeout

**Error:** Tests hang indefinitely

**Solution:**
1. Check CouchDB process isn't hung: `Get-Process | grep couchdb`
2. Restart CouchDB service
3. Run tests with timeout: `dotnet test --diag <logfile>`

### Not Finding appsettings.test.json

**Error:** Configuration defaults are used instead of test config

**Solution:**
1. Verify file exists in test project root:
   - `source-code/mmria/mmria-server.tests/appsettings.test.json`
   - `nccdphp-drh-mmria-services/mmria.services.tests/appsettings.test.json`
2. Check file is set to "Copy if newer" in project properties
3. Rebuild solution to include configuration files

## Advanced Usage

### Run Specific Test

```powershell
cd c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server.tests

# Run only LINQ test
dotnet test --filter "Name~LINQ"

# Run only CouchDB test
dotnet test --filter "Name~CouchDB"
```

### Generate Test Report

```powershell
cd c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server.tests

# Generate TRX report
dotnet test --logger "trx;LogFileName=test-results.trx"

# View results in VS Code Test Results Viewer
```

### Run with Code Coverage

```powershell
cd c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server.tests

# Requires: dotnet add package coverlet.collector
dotnet test /p:CollectCoverage=true
```

## Expected Behavior

### Memory Metrics

For properly functioning code:
- Initial memory: ~1 MB per test
- Peak memory: ~1-2 MB (minor GC allocations)
- Final memory: ~1 MB (cleaned up by GC.Collect())
- Growth: **0 MB** (no leaks detected)

### Connection Pooling

- 100 connection attempts should complete in < 1s
- No connection timeouts or resets
- Connection pool reuses handlers efficiently

### Event Handling

- 500 subscribe/unsubscribe cycles should be leak-free
- Event handler delegates properly dereferenced
- Memory returned to OS after cleanup

## Continuous Integration

For CI/CD pipelines (e.g., OpenShift, GitHub Actions):

```bash
#!/bin/bash
# Build and test both projects
cd source-code/mmria/mmria-server.tests
dotnet test --logger "trx;LogFileName=../../test-results-server.trx"

cd ../../../nccdphp-drh-mmria-services/mmria.services.tests
dotnet test --logger "trx;LogFileName=../../test-results-services.trx"

# Exit with error if any test failed
if [ $? -ne 0 ]; then exit 1; fi
```

## See Also

- [MultiTenantConfigurationLoader Documentation](./ai/MMRIA_Background_Jobs_Documentation.md)
- [Memory Leak Detection Strategy](./ai/offline_mode.md)
- [CouchDB Multi-Tenant Architecture](./ai/CVS_Community_Vital_Signs_Context.md)
