#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mmria.common.getset;
using mmria.common.couchdb;

namespace mmria_server.tests;

/// <summary>
/// Database connectivity helper for mmria-server tests.
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
    /// When couchDbUrl is provided, uses it directly for single-tenant testing (no template resolution).
    /// </summary>
    public DatabaseTestHelper(string? tenantName = null, string? purposeName = null, string? couchDbUrl = null)
    {
        var configLoader = new TestConfigurationLoader();
        configLoader.Load();

        // Use provided CouchDB URL directly for single-tenant testing, or resolve via template for multi-tenant
        if (!string.IsNullOrEmpty(couchDbUrl))
        {
            _couchDbUrl = couchDbUrl;
        }
        else
        {
            _couchDbUrl = configLoader.ResolveTenantUrl(tenantName);
        }

        // Generate descriptive test database name
        string purpose = purposeName ?? "test";
        _testDatabaseName = configLoader.GenerateTestDatabaseName(purpose);
        _testDatabaseUrl = $"{_couchDbUrl}/mmrds";  // Use standard mmrds database for tests

        _userName = configLoader.TimerUserName;
        _password = configLoader.TimerPassword;

        // Initialize CouchDbHttpClient with SimpleHttpClientFactory
        var httpClientFactory = new mmria.common.SimpleHttpClientFactory();
        _couchDbHttpClient = new CouchDbHttpClient(httpClientFactory);

        Console.WriteLine($"[DatabaseTestHelper] Initialized:");
        Console.WriteLine($"  CouchDB URL: {_couchDbUrl}");
        Console.WriteLine($"  Test DB URL: {_testDatabaseUrl}");
        Console.WriteLine($"  Auth: {(!string.IsNullOrEmpty(_userName) ? "Yes" : "No")}");
    }

    /// <summary>
    /// Execute a raw CouchDB request and return the response.
    /// </summary>
    public async Task<string> ExecuteAsync(string method, string url, string? payload = null)
    {
        return await _couchDbHttpClient.ExecuteAsync(
            method,
            url,
            payload: payload,
            userName: _userName,
            password: _password,
            throwOnError: false
        );
    }

    /// <summary>
    /// Check if CouchDB is accessible and return connection info.
    /// </summary>
    public async Task<bool> IsCouchDbAccessibleAsync()
    {
        try
        {
            var response = await ExecuteAsync("GET", $"{_couchDbUrl}/");

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
            var response = await ExecuteAsync("PUT", _testDatabaseUrl);

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
    /// Delete the test database. Throws if database does not exist.
    /// </summary>
    public async Task<bool> DeleteTestDatabaseAsync()
    {
        try
        {
            var response = await ExecuteAsync("DELETE", _testDatabaseUrl);

            // Returns OK or not_found error
            return response.Contains("ok") || response.Contains("not_found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete test database: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if test database exists.
    /// </summary>
    public async Task<bool> TestDatabaseExistsAsync()
    {
        try
        {
            var response = await ExecuteAsync("HEAD", _testDatabaseUrl);

            // HEAD returns empty response on success
            return !response.Contains("not_found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to check database existence: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if an ID matches GUID format (with or without hyphens).
    /// Patterns: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx or xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
    /// </summary>
    private bool IsGuidFormatId(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        // Pattern with hyphens: 8-4-4-4-12 hex characters
        var guidWithHyphens = new Regex(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", RegexOptions.IgnoreCase);
        
        // Pattern without hyphens: 32 hex characters
        var guidWithoutHyphens = new Regex(@"^[0-9a-f]{32}$", RegexOptions.IgnoreCase);

        return guidWithHyphens.IsMatch(id) || guidWithoutHyphens.IsMatch(id);
    }

    /// <summary>
    /// Clear all documents from the test database that have GUID-formatted IDs.
    /// Safe operation that preserves auth, config, design, and other system documents.
    /// Only deletes test-generated documents (identified by GUID IDs).
    /// </summary>
    public async Task<bool> ClearTestDatabaseAsync()
    {
        try
        {
            // Get all documents in the database
            var allDocsUrl = $"{_testDatabaseUrl}/_all_docs?include_docs=true";
            var response = await ExecuteAsync("GET", allDocsUrl);

            var jsonDoc = JsonDocument.Parse(response);
            if (!jsonDoc.RootElement.TryGetProperty("rows", out var rows))
            {
                return true;  // Empty database
            }

            // Build deletion list for documents with GUID-formatted IDs only
            var docsToDelete = new List<(string id, string rev)>();
            foreach (var row in rows.EnumerateArray())
            {
                if (row.TryGetProperty("doc", out var docElement))
                {
                    var idProp = docElement.GetProperty("_id").GetString();
                    var revProp = docElement.GetProperty("_rev").GetString();

                    // Only delete documents with GUID-formatted IDs
                    if (idProp != null && revProp != null && IsGuidFormatId(idProp))
                    {
                        docsToDelete.Add((idProp, revProp));
                    }
                }
            }

            // Bulk delete documents
            if (docsToDelete.Count > 0)
            {
                var deletePayload = new
                {
                    docs = docsToDelete.ConvertAll(d => new { id = d.id, rev = d.rev, _deleted = true })
                };

                var deleteUrl = $"{_testDatabaseUrl}/_bulk_docs";
                var deleteBody = JsonSerializer.Serialize(deletePayload);
                var deleteResponse = await ExecuteAsync("POST", deleteUrl, deleteBody);

                return deleteResponse.Contains("ok") || !deleteResponse.Contains("error");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear test database: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Insert or update a document in the test database.
    /// </summary>
    public async Task<string> InsertDocumentAsync(string documentId, string documentBody)
    {
        var docUrl = $"{_testDatabaseUrl}/{documentId}";
        return await ExecuteAsync("PUT", docUrl, documentBody);
    }

    /// <summary>
    /// Get a document from the test database.
    /// </summary>
    public async Task<string> GetDocumentAsync(string documentId)
    {
        var docUrl = $"{_testDatabaseUrl}/{documentId}";
        return await ExecuteAsync("GET", docUrl);
    }

    /// <summary>
    /// Delete a document from the test database.
    /// </summary>
    public async Task<string> DeleteDocumentAsync(string documentId, string revision)
    {
        var docUrl = $"{_testDatabaseUrl}/{documentId}?rev={revision}";
        return await ExecuteAsync("DELETE", docUrl);
    }

    /// <summary>
    /// Get the CouchDB HTTP client for direct access if needed.
    /// </summary>
    public CouchDbHttpClient GetCouchDbHttpClient()
    {
        return _couchDbHttpClient;
    }

    /// <summary>
    /// Get the test database URL.
    /// </summary>
    public string GetTestDatabaseUrl()
    {
        return _testDatabaseUrl;
    }

    /// <summary>
    /// Get the test database name.
    /// </summary>
    public string GetTestDatabaseName()
    {
        return _testDatabaseName;
    }

    /// <summary>
    /// Load multi-tenant ConfigurationSets and OverridableConfigurations from CouchDB.
    /// Helper method for loading configurations in tests that need multi-tenant setup.
    /// </summary>
    /// <returns>Tuple containing (ConfigurationSets, OverridableConfigurations)</returns>
    public async Task<(List<ConfigurationSet> ConfigurationSets, List<OverridableConfiguration> OverridableConfigurations)> LoadMultiTenantConfigurationsAsync()
    {
        var configLoader = new TestConfigurationLoader();
        configLoader.Load();

        var multiTenantLoader = new MultiTenantConfigurationLoader(null);

        // Load ConfigurationSets for all tenants
        var configurationSets = await multiTenantLoader.LoadConfigurationSetsAsync(
            configLoader.Tenants,
            configLoader.CouchDbTemplateUrl,
            configLoader.TimerUserName,
            configLoader.TimerPassword,
            configLoader.ConfigId,
            _couchDbHttpClient);

        // Load OverridableConfigurations for all tenants
        var overridableConfigs = await multiTenantLoader.LoadOverridableConfigurationsAsync(
            configLoader.Tenants,
            configLoader.CouchDbTemplateUrl,
            configLoader.TimerUserName,
            configLoader.TimerPassword,
            configLoader.SharedConfigId,
            configLoader.ConfigId,
            _couchDbHttpClient);

        return (configurationSets, overridableConfigs);
    }
}
