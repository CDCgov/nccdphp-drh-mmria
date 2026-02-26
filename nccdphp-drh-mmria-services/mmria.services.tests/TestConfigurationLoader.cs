#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using mmria.common.couchdb;
using mmria.common.getset;

namespace mmria.services.tests;

/// <summary>
/// Test-specific configuration loader that mirrors multi-tenant setup from production Program.cs
/// Supports both local development (appsettings.test.json) and CI/CD pod environments (environment variables)
/// </summary>
public sealed class TestConfigurationLoader
{
    private readonly MultiTenantConfigurationLoader _configLoader;
    private readonly IConfiguration? _appSettingsConfig;

    public string[] Tenants { get; private set; } = [];
    public string CouchDbTemplateUrl { get; private set; } = "http://localhost:5984";
    public string? TimerUserName { get; private set; }
    public string? TimerPassword { get; private set; }
    public string? ConfigId { get; private set; }
    public string? SharedConfigId { get; private set; }
    public string TestDatabasePrefix { get; private set; } = "mmria_test_";

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
        // Load configuration values using the same precedence as production
        string? multiTenantJurisdictions = _configLoader.GetConfig("multi_tenant_jurisdictions");
        Tenants = _configLoader.ParseTenants(multiTenantJurisdictions);

        CouchDbTemplateUrl = _configLoader.GetConfig(
            "multi_tenant_shared_config_id_template_couchdb_url") 
            ?? _configLoader.GetConfig("couchdb_url") 
            ?? "http://localhost:5984";

        TimerUserName = _configLoader.GetConfig("timer_user_name");
        TimerPassword = _configLoader.GetConfig("timer_password") 
            ?? _configLoader.GetConfig("timer_value");

        ConfigId = _configLoader.GetConfig("config_id", "configuration");
        SharedConfigId = _configLoader.GetConfig("shared_config_id", "dev_cluster");

        TestDatabasePrefix = _configLoader.GetConfig("test_db_prefix", "mmria_test_");

        Console.WriteLine($"[TestConfigurationLoader] Configuration loaded:");
        Console.WriteLine($"  Mode: {(_configLoader.IsEnvironmentBased() ? "Environment Variables" : "AppSettings")}");
        Console.WriteLine($"  Tenants: {string.Join(",", Tenants.Length > 0 ? Tenants : ["(single-tenant)"])}");
        Console.WriteLine($"  CouchDB Template URL: {CouchDbTemplateUrl}");
        Console.WriteLine($"  Test DB Prefix: {TestDatabasePrefix}");
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
}
