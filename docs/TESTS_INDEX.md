# Testing Documentation Index

Complete testing framework for MMRIA with memory leak detection and functional integration tests.

## 🎯 Where To Start

### 1. **I just want to run tests quickly**
→ Read: [`QUICK_START_TESTS.md`](./QUICK_START_TESTS.md)
- Copy-paste commands to run memory leak tests
- 5-minute reference

### 2. **I want to understand all testing options**
→ Read: [`MEMORY_LEAK_TESTS.md`](./MEMORY_LEAK_TESTS.md)
- Comprehensive testing guide (2000+ words)
- Configuration options, troubleshooting, CI/CD integration

### 3. **I want to add functional tests to existing code**
→ Read: [`FUNCTIONAL_TESTS_GETTING_STARTED.md`](./FUNCTIONAL_TESTS_GETTING_STARTED.md)
- Quick start guide for functional integration tests
- 31 test placeholders ready for implementation
- Step-by-step implementation process

### 4. **I need detailed functional testing patterns**
→ Read: [`FUNCTIONAL_INTEGRATION_TESTS.md`](./FUNCTIONAL_INTEGRATION_TESTS.md)
- Complete reference (1500+ words)
- Helper classes, patterns, best practices
- Troubleshooting guide

### 5. **I want overview of new functional tests**
→ Read: [`FUNCTIONAL_TESTS_SUMMARY.md`](./FUNCTIONAL_TESTS_SUMMARY.md)
- Summary of what's been created
- Architecture overview
- Next steps and priorities

## 📁 Files by Purpose

### Memory Leak Tests

| File | Purpose |
|------|---------|
| [QUICK_START_TESTS.md](./QUICK_START_TESTS.md) | Quick reference (5 min read) |
| [MEMORY_LEAK_TESTS.md](./MEMORY_LEAK_TESTS.md) | Complete guide (15 min read) |

**Status:** ✅ 10 tests running successfully
- LINQ Filtering Pattern Test
- CouchDB Connection Pool Test
- Event Subscription Test
- Async Operations Test
- Large Collection Processing Test

**Location:** 
- Server: `source-code/mmria/mmria-server.tests/MemoryLeakTests.cs`
- Services: `nccdphp-drh-mmria-services/mmria.services.tests/MemoryLeakTests.cs`

### Functional Integration Tests

| File | Purpose |
|------|---------|
| [FUNCTIONAL_TESTS_GETTING_STARTED.md](./FUNCTIONAL_TESTS_GETTING_STARTED.md) | Implementation guide (10 min read) |
| [FUNCTIONAL_INTEGRATION_TESTS.md](./FUNCTIONAL_INTEGRATION_TESTS.md) | Complete reference (15 min read) |
| [FUNCTIONAL_TESTS_SUMMARY.md](./FUNCTIONAL_TESTS_SUMMARY.md) | Overview (5 min read) |

**Status:** 📝 31 placeholder tests ready for implementation
- Server: 16 tests in 5 categories
- Services: 15 tests in 5 categories

**Location:**
- Server Tests: `source-code/mmria/mmria-server.tests/FunctionalIntegrationTests.cs`
- Services Tests: `nccdphp-drh-mmria-services/mmria.services.tests/ServiceFunctionalIntegrationTests.cs`

### Helper Classes

| File | Purpose |
|------|---------|
| `source-code/mmria/mmria-server.tests/CaseDataHelper.cs` | Case CRUD operations for server tests |
| `nccdphp-drh-mmria-services/mmria.services.tests/JobDataHelper.cs` | Job/event operations for services tests |
| `source-code/mmria/mmria-server.tests/DatabaseTestHelper.cs` | Database connectivity setup (existing) |
| `source-code/mmria/mmria-server.tests/TestConfigurationLoader.cs` | Configuration loading (existing) |

## 🚀 Quick Command Reference

### Run Different Test Types

```powershell
# Memory leak tests (10 tests, ~2 seconds each)
cd source-code/mmria/mmria-server.tests
dotnet test --filter "Category~Memory"

# Functional tests (31 placeholder tests)
cd source-code/mmria/mmria-server.tests
dotnet test --filter "FullyQualifiedName~FunctionalIntegrationTests"

# Specific functional category
dotnet test --filter "Category=CaseOperations"
dotnet test --filter "Category=Configuration"
dotnet test --filter "Category=MultiTenant"

# A single test by name
dotnet test --filter "Name=TestCreateCase"
```

### Build and Test All

```powershell
# Build both test projects
cd source-code/mmria/mmria-server.tests && dotnet build
cd ../../nccdphp-drh-mmria-services/mmria.services.tests && dotnet build

# Run all tests
cd source-code/mmria/mmria-server.tests && dotnet test
cd ../../nccdphp-drh-mmria-services/mmria.services.tests && dotnet test
```

## 📊 Test Coverage Map

### Memory Leak Tests (10 total - ✅ All Passing)
```
MemoryLeakTests
├── TestLinqFilteringPattern
├── TestCouchDBConnectionPerformance
├── TestEventSubscriptionCleanup
├── TestAsyncOperationMemoryGrowth
└── TestLargeCollectionProcessing
```

### Functional Integration Tests (31 total - 📝 Ready for Implementation)
```
FunctionalIntegrationTests (Server - 16 tests)
├── Case CRUD Operations (4)
│   ├── TestCreateCase
│   ├── TestRetrieveCase
│   ├── TestUpdateCase
│   └── TestDeleteCase
├── Configuration & Metadata (3)
│   ├── TestConfigurationLoading
│   ├── TestMetadataSchemaValidation
│   └── TestConfigurationOverrides
├── Multi-Tenant Isolation (3)
│   ├── TestCaseIsolationBetweenTenants
│   ├── TestConfigurationIsolationBetweenTenants
│   └── TestTenantUrlResolution
├── Search & Query (3)
│   ├── TestCaseSearch
│   ├── TestCaseFilteringByMetadata
│   └── TestCasePaginationListings
└── Data Validation (3)
    ├── TestCaseDataValidation
    ├── TestDateFieldHandling
    └── TestCaseStatusTransitions

ServiceFunctionalIntegrationTests (Services - 15 tests)
├── Background Job Processing (3)
│   ├── TestBackgroundJobExecution
│   ├── TestJobRetryWithBackoff
│   └── TestJobSchedulingWithCron
├── Batch Processing (3)
│   ├── TestBatchProcessing
│   ├── TestBatchProcessingWithErrors
│   └── TestLargeBatchProcessing
├── Event Processing (3)
│   ├── TestEventProcessing
│   ├── TestEventOrderPreservation
│   └── TestDeadLetterQueue
├── Import/Export (3)
│   ├── TestDataImport
│   ├── TestDataExport
│   └── TestImportDuplicateDetection
└── Service Integration (3)
    ├── TestCentralCouchDBConnection
    ├── TestCDCNotification
    └── TestVitalsImportIntegration
```

## 💻 Implementation Guide

### To Implement a Functional Test

1. **Read the Getting Started Guide**
   ```
   docs/FUNCTIONAL_TESTS_GETTING_STARTED.md
   ```

2. **Pick a test with lowest complexity first**
   - Recommended: `TestCreateCase` (Medium complexity, good starting point)

3. **Open the test file**
   - Server: `source-code/mmria/mmria-server.tests/FunctionalIntegrationTests.cs`
   - Services: `nccdphp-drh-mmria-services/mmria.services.tests/ServiceFunctionalIntegrationTests.cs`

4. **Replace placeholder**
   ```csharp
   // From:
   Assert.Pass("Placeholder: Add case creation logic");
   
   // To:
   var helper = new CaseDataHelper(_httpClient, _testDatabaseUrl);
   var caseData = helper.CreateCompleteCase("test-case-001");
   await helper.SaveCaseAsync(caseData);
   Assert.That(caseId, Is.Not.Empty);
   ```

5. **Run and verify**
   ```powershell
   dotnet test --filter "Name=TestYourTest"
   ```

6. **Commit with clear message**
   ```
   Implement TestCreateCase - validates case creation in CouchDB
   ```

## 🔍 Finding Information

| I need to... | Read this | Takes |
|---|---|---|
| Run tests immediately | QUICK_START_TESTS.md | 5 min |
| Understand all test options | MEMORY_LEAK_TESTS.md | 15 min |
| Implement a functional test | FUNCTIONAL_TESTS_GETTING_STARTED.md | 10 min |
| See detailed patterns | FUNCTIONAL_INTEGRATION_TESTS.md | 15 min |
| Get high-level overview | FUNCTIONAL_TESTS_SUMMARY.md | 5 min |
| Debug test issues | Troubleshooting section in guide | 5 min |

## ✅ Current Status

| Component | Status | Details |
|-----------|--------|---------|
| Memory Leak Tests | ✅ Running | 10 tests all passing |
| Functional Test Framework | ✅ Complete | 31 tests discoverable, placeholders passing |
| Helper Classes | ✅ Built | CaseDataHelper, JobDataHelper ready |
| Documentation | ✅ Comprehensive | 5 guides covering all topics |
| Example Tests | ✅ Ready | Reference MemoryLeakTests.cs |
| CI/CD Integration | ✅ Supported | Environment variable configuration |

## 🎓 Learning Path

**Beginner (Want to run tests):**
1. QUICK_START_TESTS.md (5 min)
2. Run: `cd source-code/mmria/mmria-server.tests && dotnet test`

**Intermediate (Want to understand):**
1. MEMORY_LEAK_TESTS.md (15 min)
2. FUNCTIONAL_TESTS_GETTING_STARTED.md (10 min)
3. Review helper classes (10 min)

**Advanced (Want to implement):**
1. FUNCTIONAL_INTEGRATION_TESTS.md (15 min, patterns section)
2. Review existing MemoryLeakTests.cs (10 min)
3. Pick a test and implement (30-60 min per test)

## 📞 Common Questions

**Q: How do I run tests?**
A: See QUICK_START_TESTS.md

**Q: What's the difference between memory leak and functional tests?**
A: Memory leak tests verify resources don't accumulate. Functional tests verify business logic works. See comparison in FUNCTIONAL_INTEGRATION_TESTS.md

**Q: Can I run tests without CouchDB?**
A: No, you need CouchDB running on localhost:5984. See MEMORY_LEAK_TESTS.md - "Prerequisites"

**Q: How do I implement a test?**
A: See FUNCTIONAL_TESTS_GETTING_STARTED.md - "Step-by-Step" section

**Q: Where are the helper methods?**
A: CaseDataHelper.cs (server) and JobDataHelper.cs (services)

**Q: What if a test fails?**
A: See troubleshooting section in FUNCTIONAL_INTEGRATION_TESTS.md

## 📝 Next Actions

1. ✅ **Run memory leak tests** - Verify existing tests work
   ```powershell
   cd source-code/mmria/mmria-server.tests
   dotnet test --filter "Category~Memory"
   ```

2. ✅ **Run functional placeholder tests** - Verify new framework works
   ```powershell
   cd source-code/mmria/mmria-server.tests
   dotnet test --filter "FullyQualifiedName~FunctionalIntegrationTests"
   ```

3. 📝 **Implement functional tests** - Replace placeholders with real logic
   - Pick a test from FUNCTIONAL_TESTS_GETTING_STARTED.md
   - Follow the implementation checklist
   - Commit when passing

---

**Total Tests: 10 (memory leak) + 31 (functional) = 41 comprehensive tests**

**Ready to run:** ✅ Memory leak tests (10) + placeholders (31 - run but pass as placeholders)
**Ready to implement:** 31 functional tests with full framework support

**All documentation complete. Tests discoverable. Ready for team implementation!** 🚀
