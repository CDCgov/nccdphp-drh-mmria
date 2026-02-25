#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria_case_generator.Models;

namespace mmria_server.tests;

/// <summary>
/// Test-specific configuration loader that mirrors multi-tenant setup from production Program.cs
/// Supports both local development (appsettings.test.json) and CI/CD pod environments (environment variables)
/// </summary>
public sealed class TestConfigurationLoader
{
    private readonly MultiTenantConfigurationLoader _configLoader;
    private readonly IConfiguration? _appSettingsConfig;

    public string TestTenant { get; private set; } = "tenant1";
    public string TestTenantCouchDbUrl { get; private set; } = "http://localhost:5984";
    public string TestTenantMetadataUrl { get; private set; } = "";
    public string TestMetadataVersion { get; private set; } = "26.01.20";

    public string[] Tenants { get; private set; } = [];
    public string CouchDbTemplateUrl { get; private set; } = "http://localhost:5984";
    public string? TimerUserName { get; private set; }
    public string? TimerPassword { get; private set; }
    public string? ConfigId { get; private set; }
    public string? SharedConfigId { get; private set; }
    public string TestDatabasePrefix { get; private set; } = "mmria_test_";

    // Test Data Generation Configuration
    public bool GenerationEnabled { get; private set; } = false;
    public int GenerationCaseCount { get; private set; } = 10;
    public string GenerationStrategy { get; private set; } = "complete";
    public string GenerationMetadataVersion { get; private set; } = "25.10.14";
    public int? GenerationRandomSeed { get; private set; }
    public bool GenerationSaveToDatabase { get; private set; } = false;
    public bool GenerationCreateDatabaseIfNotExists { get; private set; } = true;
    public bool GenerationValidateBeforeSave { get; private set; } = false;
    public bool GenerationPreserveTestDatabases { get; private set; } = false;
    public bool GenerationAuthEnabled { get; private set; } = false;
    public string? GenerationApiKey { get; private set; }

    /// <summary>
    /// Databases to clear during test cleanup (GUID documents only, preserves auth/config docs)
    /// Default: mmrds, reports, de_id
    /// </summary>
    public string[] ClearDatabaseNames { get; private set; } = ["mmrds", "reports", "de_id"];

    /// <summary>
    /// Initialize by loading configuration from appsettings.test.json (local) or environment variables (CI/CD)
    /// </summary>
    public TestConfigurationLoader()
    {
        // Load appsettings.test.json if it exists (local development)
        string? testSettingsPath = FindTestSettingsFile();
        
        if (!string.IsNullOrEmpty(testSettingsPath) && File.Exists(testSettingsPath))
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile(testSettingsPath, optional: false, reloadOnChange: false);
            
            _appSettingsConfig = builder.Build();
        }

        // Initialize MultiTenantConfigurationLoader with appsettings config (or null for env var fallback)
        _configLoader = new MultiTenantConfigurationLoader(_appSettingsConfig);
    }

    /// <summary>
    /// Locate appsettings.test.json by searching up from current directory or test assembly location
    /// </summary>
    private static string? FindTestSettingsFile()
    {
        // Try current directory first
        if (File.Exists("appsettings.test.json"))
        {
            return Path.GetFullPath("appsettings.test.json");
        }

        // Try test project directory
        string? testProjectDir = Path.GetDirectoryName(typeof(TestConfigurationLoader).Assembly.Location);
        if (testProjectDir != null)
        {
            string testSettingsPath = Path.Combine(testProjectDir, "appsettings.test.json");
            if (File.Exists(testSettingsPath))
            {
                return testSettingsPath;
            }
        }

        // Try workspace root (relative path from bin/Debug or bin/Release)
        string[] possiblePaths = new[]
        {
            "../../../../appsettings.test.json",
            "../../../appsettings.test.json",
            "../../appsettings.test.json",
            "../appsettings.test.json",
        };

        foreach (var relativePath in possiblePaths)
        {
            string fullPath = Path.GetFullPath(relativePath);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Load test configuration following production pattern: environment > appsettings
    /// </summary>
    public void Load()
    {
        // Load test execution configuration
        TestTenant = _configLoader.GetConfig("test_execution:target_test_tenant") ?? "tenant1";
        TestTenantCouchDbUrl = _configLoader.GetConfig("test_execution:target_test_tenant_couchdb_url") ?? "http://localhost:5984";
        TestTenantMetadataUrl = _configLoader.GetConfig("test_execution:target_test_tenant_metadata_url") ?? "";
        TestMetadataVersion = _configLoader.GetConfig("test_execution:metadata_version") ?? "26.01.20";

        // Load multi-tenant configuration (for backward compatibility)
        string? multiTenantJurisdictions = _configLoader.GetConfig("multi_tenant_jurisdictions");
        Tenants = _configLoader.ParseTenants(multiTenantJurisdictions);

        // Use target_test_tenant_couchdb_url for template URL (single-tenant test focus)
        CouchDbTemplateUrl = _configLoader.GetConfig("test_execution:target_test_tenant_couchdb_url") ?? "http://localhost:5984";

        TimerUserName = _configLoader.GetConfig("timer_user_name");
        TimerPassword = _configLoader.GetConfig("timer_password") 
            ?? _configLoader.GetConfig("timer_value");

        ConfigId = _configLoader.GetConfig("config_id", "configuration");
        SharedConfigId = _configLoader.GetConfig("shared_config_id", "shared_config");

        TestDatabasePrefix = _configLoader.GetConfig("test_db_prefix", "mmria_test_");

        // Load test data generation configuration
        GenerationEnabled = bool.TryParse(_configLoader.GetConfig("test_data_generation:enabled", "true"), out var enabled) && enabled;
        GenerationCaseCount = int.TryParse(_configLoader.GetConfig("test_data_generation:case_count", "25"), out var caseCount) ? caseCount : 25;
        GenerationStrategy = _configLoader.GetConfig("test_data_generation:strategy", "complete") ?? "complete";
        GenerationMetadataVersion = _configLoader.GetConfig("test_execution:metadata_version") ?? "26.01.20";
        GenerationRandomSeed = int.TryParse(_configLoader.GetConfig("test_data_generation:random_seed"), out var seed) ? seed : null;
        GenerationSaveToDatabase = bool.TryParse(_configLoader.GetConfig("test_data_generation:output:save_to_database", "false"), out var saveDb) && saveDb;
        GenerationCreateDatabaseIfNotExists = bool.TryParse(_configLoader.GetConfig("test_data_generation:output:create_database_if_not_exists", "true"), out var createDb) && createDb;
        GenerationValidateBeforeSave = bool.TryParse(_configLoader.GetConfig("test_data_generation:output:validate_before_save", "false"), out var validate) && validate;
        GenerationPreserveTestDatabases = bool.TryParse(_configLoader.GetConfig("test_data_generation:output:preserve_test_databases", "false"), out var preserve) && preserve;
        GenerationAuthEnabled = bool.TryParse(_configLoader.GetConfig("test_data_generation:authentication:enabled", "false"), out var authEnabled) && authEnabled;
        GenerationApiKey = _configLoader.GetConfig("test_data_generation:authentication:api_key");

        Console.WriteLine($"[TestConfigurationLoader] Configuration loaded:");
        Console.WriteLine($"  Mode: {(_configLoader.IsEnvironmentBased() ? "Environment Variables" : "AppSettings")}");
        Console.WriteLine($"  Tenants: {string.Join(",", Tenants.Length > 0 ? Tenants : ["(single-tenant)"])}");
        Console.WriteLine($"  CouchDB Template URL: {CouchDbTemplateUrl}");
        Console.WriteLine($"  Test DB Prefix: {TestDatabasePrefix}");
        Console.WriteLine($"  Generation Enabled: {GenerationEnabled}");
        Console.WriteLine($"  Generation Cases: {GenerationCaseCount}");
        Console.WriteLine($"  Generation Save to DB: {GenerationSaveToDatabase}");
        Console.WriteLine($"  Generation Preserve Databases: {GenerationPreserveTestDatabases}");
    }

    /// <summary>
    /// Generate test database name following pattern: {prefix}{tenant}_{purpose}_{timestamp}
    /// Example: mmria_test_jurisdiction1_memory_leaks_20260224_143025
    /// </summary>
    public string GenerateTestDatabaseName(string purpose, string? tenantName = null)
    {
        string tenant = !string.IsNullOrEmpty(tenantName) ? tenantName : "default";
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return $"{TestDatabasePrefix}{tenant}_{purpose}_{timestamp}".ToLower();
    }

    /// <summary>
    /// Resolve tenant-specific CouchDB URL
    /// </summary>
    public string ResolveTenantUrl(string tenantName)
    {
        return _configLoader.ResolveTenantUrl(CouchDbTemplateUrl, tenantName);
    }

    /// <summary>
    /// Get list of tenant URLs for iteration
    /// </summary>
    public List<(string TenantName, string CouchDbUrl)> GetTenantUrls()
    {
        var result = new List<(string, string)>();

        if (Tenants.Length == 0)
        {
            // Single tenant: use template URL directly
            result.Add(("default", CouchDbTemplateUrl));
        }
        else
        {
            // Multi-tenant: resolve URL for each tenant
            foreach (var tenant in Tenants)
            {
                string tenantUrl = ResolveTenantUrl(tenant);
                result.Add((tenant, tenantUrl));
            }
        }

        return result;
    }

    /// <summary>
    /// Create a GenerationConfig from test configuration settings.
    /// Can be used to programmatically configure case generation for testing.
    /// </summary>
    public GenerationConfig CreateGenerationConfig(string? tenantName = null, string? metadataUrl = null)
    {
        var tenant = !string.IsNullOrEmpty(tenantName) ? tenantName : (Tenants.Length > 0 ? Tenants[0] : "TESTJURISDICTION");
        
        var config = new GenerationConfig
        {
            Jurisdiction = tenant,
            CaseCount = GenerationCaseCount,
            MetadataVersion = GenerationMetadataVersion,
            MetadataUrl = metadataUrl ?? TestTenantMetadataUrl,
            Strategy = mmria_case_generator.Models.GenerationStrategy.FromName(GenerationStrategy),
            RandomSeed = GenerationRandomSeed,
            CreatedBy = "test-data-generator",
            LastUpdatedBy = "test-data-generator",
            HostState = tenant,
            JurisdictionId = "/",
            
            // Output Configuration
            OutputConfig = new TestDataGenerationOutputConfig
            {
                SaveToDatabase = GenerationSaveToDatabase,
                DatabaseName = GenerateTestDatabaseName("test-data", tenant),
                CouchDbUrl = ResolveTenantUrl(tenant),
                CouchDbUsername = TimerUserName,
                CouchDbPassword = TimerPassword,
                CreateDatabaseIfNotExists = GenerationCreateDatabaseIfNotExists,
                ValidateBeforeSave = GenerationValidateBeforeSave,
                TestDatabaseNamePrefix = TestDatabasePrefix
            },

            // Authentication Configuration
            AuthConfig = new TestDataGenerationAuthConfig
            {
                Enabled = GenerationAuthEnabled,
                ApiKey = GenerationApiKey
            },

            // Demographic Weights (use defaults)
            DemographicWeights = new DemographicWeights()
        };

        return config;
    }
}
