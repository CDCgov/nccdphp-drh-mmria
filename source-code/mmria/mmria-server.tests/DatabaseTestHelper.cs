#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using mmria.common.getset;

namespace mmria_server.tests
{
    /// <summary>
    /// Database connectivity helper for memory leak tests and functional tests.
    /// Provides CouchDB client setup and common database operations following production patterns.
    /// 
    /// Supports both local development and CI/CD environments:
    /// - Local: Configuration from appsettings.test.json or environment variables
    /// - CI/CD: Configuration from environment variables injected by ConfigMap/Secrets
    /// </summary>
    public class DatabaseTestHelper
    {
        private readonly CouchDbHttpClient _couchDbHttpClient;
        private readonly string _couchDbUrl;
        private readonly string _testDatabaseName;
        private readonly string _testDatabaseUrl;
        private readonly string? _userName;
        private readonly string? _password;

        /// <summary>
        /// Initialize database helper with production-like multi-tenant configuration.
        /// Uses TestConfigurationLoader to load configuration from appsettings or environment.
        /// </summary>
        public DatabaseTestHelper(string? tenantName = null, string? purposeName = null)
        {
            var configLoader = new TestConfigurationLoader();
            configLoader.Load();

            // Use provided tenant or first tenant from config (or "default" for single-tenant)
            string tenant = tenantName ?? (configLoader.Tenants.Length > 0 ? configLoader.Tenants[0] : "default");

            // Resolve tenant-specific CouchDB URL
            _couchDbUrl = configLoader.ResolveTenantUrl(tenant);

            // Generate descriptive test database name
            string purpose = purposeName ?? "memory_leaks";
            _testDatabaseName = configLoader.GenerateTestDatabaseName(purpose, tenant);
            _testDatabaseUrl = $"{_couchDbUrl}/{_testDatabaseName}";

            _userName = configLoader.TimerUserName;
            _password = configLoader.TimerPassword;

            // Initialize CouchDbHttpClient with SimpleHttpClientFactory
            var httpClientFactory = new mmria.common.SimpleHttpClientFactory();
            _couchDbHttpClient = new CouchDbHttpClient(httpClientFactory);

            Console.WriteLine($"[DatabaseTestHelper] Initialized for tenant '{tenant}':");
            Console.WriteLine($"  Test DB URL: {_testDatabaseUrl}");
            Console.WriteLine($"  Auth: {(!string.IsNullOrEmpty(_userName) ? "Yes" : "No")}");
        }

        /// <summary>
        /// Check if CouchDB is accessible and return connection info.
        /// </summary>
        public async Task<bool> IsCouchDbAccessibleAsync()
        {
            try
            {
                var response = await _couchDbHttpClient.ExecuteAsync(
                    "GET",
                    $"{_couchDbUrl}/",
                    userName: _userName,
                    password: _password,
                    throwOnError: false
                );

                // If response contains version info, CouchDB is accessible
                return response.Contains("\"version\"") || response.Contains("couchdb");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CouchDB connectivity check failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create the test database. Does not throw if database already exists.
        /// </summary>
        public async Task<bool> CreateTestDatabaseAsync()
        {
            try
            {
                var response = await _couchDbHttpClient.ExecuteAsync(
                    "PUT",
                    _testDatabaseUrl,
                    userName: _userName,
                    password: _password,
                    throwOnError: false
                );

                // Returns OK or file_exists error (both are acceptable)
                return response.Contains("ok") || response.Contains("file_exists");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create test database: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete the test database for cleanup.
        /// </summary>
        public async Task<bool> DeleteTestDatabaseAsync()
        {
            try
            {
                var response = await _couchDbHttpClient.ExecuteAsync(
                    "DELETE",
                    _testDatabaseUrl,
                    userName: _userName,
                    password: _password,
                    throwOnError: false
                );

                return response.Contains("ok");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete test database: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Insert a test document into the database.
        /// Returns the document ID if successful.
        /// </summary>
        public async Task<string> InsertTestDocumentAsync(string docType, Dictionary<string, object> data)
        {
            try
            {
                // Create document with ID and type
                var doc = new Dictionary<string, object>(data)
                {
                    { "_id", $"{docType}_{Guid.NewGuid():N}" },
                    { "type", docType },
                    { "created_at", DateTime.UtcNow.ToString("O") }
                };

                var json = JsonSerializer.Serialize(doc);
                var response = await _couchDbHttpClient.ExecuteAsync(
                    "POST",
                    _testDatabaseUrl,
                    payload: json,
                    userName: _userName,
                    password: _password,
                    throwOnError: false
                );

                // Parse response to get document ID
                if (response.Contains("\"ok\":true") || response.Contains("\"id\""))
                {
                    using var jsonDoc = JsonDocument.Parse(response);
                    if (jsonDoc.RootElement.TryGetProperty("id", out var idElement))
                    {
                        return idElement.GetString();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to insert test document: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieve a document by ID from the database.
        /// </summary>
        public async Task<Dictionary<string, object>> GetDocumentAsync(string docId)
        {
            try
            {
                var response = await _couchDbHttpClient.ExecuteAsync(
                    "GET",
                    $"{_testDatabaseUrl}/{docId}",
                    userName: _userName,
                    password: _password,
                    throwOnError: false
                );

                if (response.Contains("\"_id\""))
                {
                    var doc = JsonSerializer.Deserialize<Dictionary<string, object>>(response);
                    return doc;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get document: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Query all documents in the test database (using _all_docs view).
        /// </summary>
        public async Task<List<string>> GetAllDocumentIdsAsync()
        {
            try
            {
                var response = await _couchDbHttpClient.ExecuteAsync(
                    "GET",
                    $"{_testDatabaseUrl}/_all_docs",
                    userName: _userName,
                    password: _password,
                    throwOnError: false
                );

                var docIds = new List<string>();
                if (response.Contains("\"rows\""))
                {
                    using var jsonDoc = JsonDocument.Parse(response);
                    if (jsonDoc.RootElement.TryGetProperty("rows", out var rowsElement))
                    {
                        foreach (var row in rowsElement.EnumerateArray())
                        {
                            if (row.TryGetProperty("id", out var idElement))
                            {
                                var id = idElement.GetString();
                                if (!id.StartsWith("_")) // Skip design docs
                                {
                                    docIds.Add(id);
                                }
                            }
                        }
                    }
                }

                return docIds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get all documents: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get number of documents in the test database.
        /// </summary>
        public async Task<int> GetDocumentCountAsync()
        {
            try
            {
                var response = await _couchDbHttpClient.ExecuteAsync(
                    "GET",
                    _testDatabaseUrl,
                    userName: _userName,
                    password: _password,
                    throwOnError: false
                );

                using var jsonDoc = JsonDocument.Parse(response);
                if (jsonDoc.RootElement.TryGetProperty("doc_count", out var countElement))
                {
                    return countElement.GetInt32();
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get document count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Clear all documents from the test database.
        /// </summary>
        public async Task<bool> ClearAllDocumentsAsync()
        {
            try
            {
                var docIds = await GetAllDocumentIdsAsync();
                
                if (docIds.Count == 0)
                {
                    return true; // Already empty
                }

                // Delete each document
                foreach (var docId in docIds)
                {
                    var doc = await GetDocumentAsync(docId);
                    if (doc != null && doc.ContainsKey("_rev"))
                    {
                        var rev = doc["_rev"].ToString();
                        await _couchDbHttpClient.ExecuteAsync(
                            "DELETE",
                            $"{_testDatabaseUrl}/{docId}?rev={rev}",
                            userName: _userName,
                            password: _password,
                            throwOnError: false
                        );
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clear documents: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get the CouchDB server URL (for diagnostic purposes).
        /// </summary>
        public string GetCouchDbUrl() => _couchDbUrl;

        /// <summary>
        /// Get the test database URL (for diagnostic purposes).
        /// </summary>
        public string GetTestDatabaseUrl() => _testDatabaseUrl;

        /// <summary>
        /// Get the test database name (for diagnostic purposes).
        /// </summary>
        public string GetTestDatabaseName() => _testDatabaseName;
    }
}
