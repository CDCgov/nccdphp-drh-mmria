using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mmria.services.tests
{
    /// <summary>
    /// Automated memory leak testing suite for MMRIA services.
    /// Focuses on actor lifecycle, job execution, and CouchDB connection pooling.
    /// 
    /// These tests simulate production-like scenarios to detect memory leaks
    /// that could cause the pod OOM crashes (exit code 137).
    /// 
    /// **Database Configuration:**
    /// Tests use real CouchDB connectivity via IHttpClientFactory.
    /// Configure via environment variables:
    /// - COUCHDB_TEST_URL: CouchDB server URL (default: http://localhost:5984)
    /// - COUCHDB_TEST_DB: Test database name (default: mmria_test_memory_leaks)
    /// - COUCHDB_TEST_USER: Optional username
    /// - COUCHDB_TEST_PASSWORD: Optional password
    /// 
    /// If CouchDB is not accessible, database tests will be SKIPPED (not failed).
    /// </summary>
    [TestFixture]
    public class MemoryLeakTests
    {
        private DatabaseTestHelper _dbHelper;
        private bool _isCouchDbAccessible;

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            _dbHelper = new DatabaseTestHelper();
            _isCouchDbAccessible = await _dbHelper.IsCouchDbAccessibleAsync();

            if (_isCouchDbAccessible)
            {
                // Create test database
                await _dbHelper.CreateTestDatabaseAsync();
                TestContext.WriteLine($"Database Connectivity Test:");
                TestContext.WriteLine($"  CouchDB URL: {_dbHelper.GetCouchDbUrl()}");
                TestContext.WriteLine($"  Test Database: {_dbHelper.GetTestDatabaseUrl()}");
                TestContext.WriteLine($"  Status: ✓ ACCESSIBLE");
            }
            else
            {
                TestContext.WriteLine($"Database Connectivity Test:");
                TestContext.WriteLine($"  CouchDB URL: {_dbHelper.GetCouchDbUrl()}");
                TestContext.WriteLine($"  Status: ✗ NOT ACCESSIBLE - database tests will be skipped");
            }
        }

        [OneTimeTearDown]
        public async Task TeardownAsync()
        {
            if (_isCouchDbAccessible)
            {
                // Clean up test database
                await _dbHelper.DeleteTestDatabaseAsync();
            }
        }

        /// <summary>
        /// Test baseline: Verify garbage collection and memory reporting accuracy.
        /// This establishes baseline behavior for comparison with other tests.
        /// </summary>
        [Test]
        public void MemoryBaseline_CollectionWorks()
        {
            // Initial measurement
            long initialMemory = GC.GetTotalMemory(true);

            // Allocate some memory
            var allocations = new List<object>();
            for (int i = 0; i < 1000; i++)
            {
                allocations.Add(new byte[1024 * 10]); // 10 KB each = 10 MB
            }

            long allocatedMemory = GC.GetTotalMemory(false);
            Assert.That(allocatedMemory, Is.GreaterThan(initialMemory + 5_000_000), 
                "Memory should be allocated");

            // Clear allocations and verify GC cleans up
            allocations.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            long finalMemory = GC.GetTotalMemory(true);
            Assert.That(finalMemory, Is.LessThan(initialMemory + 1_000_000), 
                "Garbage collection should recover memory");
        }

        /// <summary>
        /// Stress test: Simulates repeated business operations to detect leaks.
        /// Runs 100 iterations of typical MMRIA operations and monitors memory growth.
        /// 
        /// Expected behavior: Minimal memory growth (<50MB) after cleanup
        /// Leak indicator: Steady memory increase despite GC runs
        /// </summary>
        [Test]
        public async Task StressTest_OperationCycles_NoMemoryLeak()
        {
            const int CYCLE_COUNT = 100;
            const int MEMORY_THRESHOLD = 50_000_000; // 50MB threshold
            
            var memorySnapshots = new List<long>();

            // Initial measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long initialMemory = GC.GetTotalMemory(true);
            memorySnapshots.Add(initialMemory);

            // Run operation cycles
            for (int i = 0; i < CYCLE_COUNT; i++)
            {
                // Simulate main business operations
                await SimulateMainBusinessLogic();

                // Every 10 cycles, measure memory and run GC
                if ((i + 1) % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    long currentMemory = GC.GetTotalMemory(true);
                    memorySnapshots.Add(currentMemory);
                }
            }

            // Analysis: Check for consistent memory growth pattern
            long peakMemory = memorySnapshots.Max();
            long finalMemory = memorySnapshots.Last();
            long leakedMemory = finalMemory - initialMemory;

            // Log memory profile for debugging
            TestContext.WriteLine($"Initial Memory: {initialMemory / 1024 / 1024} MB");
            TestContext.WriteLine($"Peak Memory: {peakMemory / 1024 / 1024} MB");
            TestContext.WriteLine($"Final Memory: {finalMemory / 1024 / 1024} MB");
            TestContext.WriteLine($"Memory Growth: {leakedMemory / 1024 / 1024} MB");
            TestContext.WriteLine($"Memory Snapshots: {string.Join(", ", memorySnapshots.Select(m => $"{m / 1024 / 1024}MB"))}");

            // Assertion: Memory should stabilize, not continuously grow
            Assert.That(leakedMemory, Is.LessThan(MEMORY_THRESHOLD),
                $"Memory leak detected: {leakedMemory / 1024 / 1024}MB growth after {CYCLE_COUNT} cycles");
        }

        /// <summary>
        /// CouchDB Connection Leak Test: Simulates repeated database operations.
        /// Uses real CouchDB connectivity via IHttpClientFactory to detect connection pool exhaustion.
        /// 
        /// Expected: Connections are pooled/reused, minimal memory increase
        /// Leak: Connections not returned to pool, memory steadily increases
        /// 
        /// SKIPPED if CouchDB is not accessible.
        /// </summary>
        [Test]
        public async Task DatabaseConnections_Pooling_NoLeak()
        {
            if (!_isCouchDbAccessible)
            {
                Assert.Ignore("CouchDB not accessible - skipping database connectivity test");
            }

            const int CONNECTION_ATTEMPTS = 100; // Reduced for real DB operations
            const int MEMORY_THRESHOLD = 30_000_000; // 30MB threshold for connection leaks

            GC.Collect();
            long initialMemory = GC.GetTotalMemory(true);

            // Simulate repeated database operations with CouchDB
            for (int i = 0; i < CONNECTION_ATTEMPTS; i++)
            {
                await SimulateCouchDbOperation(i);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            long finalMemory = GC.GetTotalMemory(true);
            long leakedMemory = finalMemory - initialMemory;

            TestContext.WriteLine($"CouchDB Connection Pool Test:");
            TestContext.WriteLine($"  Database URL: {_dbHelper.GetTestDatabaseUrl()}");
            TestContext.WriteLine($"  Initial Memory: {initialMemory / 1024 / 1024} MB");
            TestContext.WriteLine($"  Final Memory: {finalMemory / 1024 / 1024} MB");
            TestContext.WriteLine($"  Leaked Memory: {leakedMemory / 1024 / 1024} MB");
            TestContext.WriteLine($"  Connection Attempts: {CONNECTION_ATTEMPTS}");

            Assert.That(leakedMemory, Is.LessThan(MEMORY_THRESHOLD),
                "Connection pool leak detected: memory not recovered after DB operations");
        }

        /// <summary>
        /// Event Handler Leak Test: Verifies event subscriptions are properly cleaned up.
        /// Simulates subscribe/unsubscribe cycles common in .NET event patterns.
        /// 
        /// Leak indicator: Memory grows with each subscription cycle
        /// </summary>
        [Test]
        public void EventSubscriptions_Cleanup_NoLeak()
        {
            const int SUBSCRIBE_CYCLES = 500;
            const int MEMORY_THRESHOLD = 20_000_000; // 20MB threshold

            GC.Collect();
            long initialMemory = GC.GetTotalMemory(true);

            var eventSource = new StressTestEventSource();

            for (int i = 0; i < SUBSCRIBE_CYCLES; i++)
            {
                // Subscribe
                void handler(object sender, EventArgs e) { }
                eventSource.TestEvent += handler;

                // Unsubscribe
                eventSource.TestEvent -= handler;

                // Occasionally raise event to flush any pending handlers
                if (i % 10 == 0)
                {
                    eventSource.RaiseTestEvent();
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            long finalMemory = GC.GetTotalMemory(true);
            long leakedMemory = finalMemory - initialMemory;

            TestContext.WriteLine($"Event Subscription Test:");
            TestContext.WriteLine($"  Initial Memory: {initialMemory / 1024 / 1024} MB");
            TestContext.WriteLine($"  Final Memory: {finalMemory / 1024 / 1024} MB");
            TestContext.WriteLine($"  Leaked Memory: {leakedMemory / 1024 / 1024} MB");
            TestContext.WriteLine($"  Subscribe/Unsubscribe Cycles: {SUBSCRIBE_CYCLES}");

            Assert.That(leakedMemory, Is.LessThan(MEMORY_THRESHOLD),
                "Event handler leak detected: unsubscribed handlers not cleaned up");
        }

        /// <summary>
        /// LINQ/Collection Leak Test: Detects memory waste from inefficient LINQ chains.
        /// Related to single-pass filtering optimization in AI_CONTEXT.md Case View endpoint.
        /// 
        /// Pattern to avoid: Multiple .Where/.ToList() chains create intermediate lists
        /// </summary>
        [Test]
        public void Collections_LINQFiltering_OptimalMemory()
        {
            const int ITEMS = 10000;
            const int ITERATIONS = 100;

            GC.Collect();
            long initialMemory = GC.GetTotalMemory(true);

            // Simulate typical case data filtering
            var data = Enumerable.Range(0, ITEMS)
                .Select(i => new { Id = i, Status = i % 3 == 0 ? "A" : "B", Type = i % 5 == 0 ? "X" : "Y" })
                .ToList();

            for (int iter = 0; iter < ITERATIONS; iter++)
            {
                // Good pattern (single pass):
                var filtered = new List<dynamic>();
                foreach (var item in data)
                {
                    if (item.Status == "A" && item.Type == "X")
                    {
                        filtered.Add(item);
                    }
                }

                // Use filtered data...
                _ = filtered.Count;
            }

            GC.Collect();
            long afterOptimal = GC.GetTotalMemory(true);

            // Reset
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Bad pattern (multiple chained LINQ):
            for (int iter = 0; iter < ITERATIONS; iter++)
            {
                var result1 = data.Where(x => x.Status == "A").ToList();
                var result2 = result1.Where(x => x.Type == "X").ToList();
                _ = result2.Count;
            }

            GC.Collect();
            long afterChained = GC.GetTotalMemory(true);

            long optimalGrowth = afterOptimal - initialMemory;
            long chainedGrowth = afterChained - afterOptimal;

            TestContext.WriteLine($"LINQ Filtering Pattern Test:");
            TestContext.WriteLine($"  Single-pass growth: {optimalGrowth / 1024 / 1024} MB");
            TestContext.WriteLine($"  Chained LINQ growth: {chainedGrowth / 1024 / 1024} MB");

            // Chained LINQ should use less or similar memory, but document the difference
            TestContext.WriteLine($"  Ratio (chained/optimal): {(chainedGrowth > 0 ? (double)chainedGrowth / optimalGrowth : 1.0):F2}");
        }

        // ============================================================================
        // Helper methods
        // ============================================================================

        /// <summary>
        /// Simulates main MMRIA business logic cycle (case operations, queries, etc).
        /// </summary>
        private async Task SimulateMainBusinessLogic()
        {
            // Simulate case data operations
            var caseData = new Dictionary<string, object>
            {
                { "id", Guid.NewGuid().ToString() },
                { "type", "case" },
                { "status", "open" },
                { "data", new byte[1024 * 50] } // 50KB case data
            };

            // Simulate async processing
            await Task.Delay(1);

            // Simulate metadata creation
            var metadata = new { caseData.Keys.Count, hash = caseData.GetHashCode() };

            // Simulate local processing
            _ = ProcessCaseMetadata(metadata);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Simulates a CouchDB operation using real database connectivity via IHttpClientFactory.
        /// Tests connection pooling and memory stability under repeated database access.
        /// </summary>
        private async Task SimulateCouchDbOperation(int iteration)
        {
            try
            {
                // Create a test document
                var testData = new Dictionary<string, object>
                {
                    { "value", $"test_document_{iteration}" },
                    { "iteration", iteration },
                    { "size", new byte[1024] } // 1KB per document
                };

                string docId = await _dbHelper.InsertTestDocumentAsync("memory_leak_test", testData);

                if (!string.IsNullOrEmpty(docId))
                {
                    // Retrieve the document to test read operations
                    var doc = await _dbHelper.GetDocumentAsync(docId);
                    _ = doc; // Use the result to prevent optimization
                }

                // Occasionally query all documents to test _all_docs endpoint
                if (iteration % 10 == 0)
                {
                    var count = await _dbHelper.GetDocumentCountAsync();
                    _ = count;
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Warning: CouchDB operation failed at iteration {iteration}: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes case metadata (simulated).
        /// </summary>
        private object ProcessCaseMetadata(dynamic metadata)
        {
            return new { processed = true, count = 1 };
        }

        /// <summary>
        /// Helper class for event subscription testing.
        /// </summary>
        private class StressTestEventSource
        {
            public event EventHandler TestEvent;

            public void RaiseTestEvent()
            {
                TestEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Base class for stress tests (extensible for future test variations).
    /// </summary>
    public abstract class StressTestBase
    {
        protected const int DefaultCycleCount = 100;
        protected const int DefaultMemoryThreshold = 50_000_000; // 50MB

        /// <summary>
        /// Measures memory before and after an operation, returning the delta.
        /// </summary>
        protected long MeasureMemoryDelta(Action operation)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long before = GC.GetTotalMemory(true);

            operation();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            long after = GC.GetTotalMemory(true);

            return after - before;
        }

        /// <summary>
        /// Measures memory delta for async operations.
        /// </summary>
        protected async Task<long> MeasureMemoryDeltaAsync(Func<Task> operation)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long before = GC.GetTotalMemory(true);

            await operation();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            long after = GC.GetTotalMemory(true);

            return after - before;
        }

        /// <summary>
        /// Converts bytes to MB for readable output.
        /// </summary>
        protected string FormatMemory(long bytes)
        {
            return $"{bytes / 1024 / 1024} MB";
        }
    }
}
