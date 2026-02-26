#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using mmria.common.couchdb;

namespace mmria_server.tests;

/// <summary>
/// Test-specific configuration loader for mmria-server tests.
/// Mirrors multi-tenant setup from production Program.cs.
/// Supports both local development (appsettings.test.json) and CI/CD pod environments (environment variables).
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
    public string TargetTestTenant { get; private set; } = "tenant5";
    public string MetadataVersion { get; private set; } = "26.01.20";
    public string TestDatabasePrefix { get; private set; } = "mmria_test_";

    /// <summary>
    /// Initialize by loading configuration from appsettings.test.json (local) or environment variables (CI/CD).
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
    /// Locate appsettings.test.json by searching up from current directory or test assembly location.
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
    /// Load test configuration following production pattern: environment variables override appsettings.
    /// Uses is_environment_based flag to determine configuration source.
    /// </summary>
    public void Load()
    {
        // Load configuration values using the same precedence as production
        string? multiTenantJurisdictions = _configLoader.GetConfig("multi_tenant_jurisdictions");
        Tenants = _configLoader.ParseTenants(multiTenantJurisdictions);

        CouchDbTemplateUrl = _configLoader.GetConfig("multi_tenant_template_couchdb_url") 
            ?? _configLoader.GetConfig("couchdb_url") 
            ?? "http://localhost:5984";

        TimerUserName = _configLoader.GetConfig("timer_user_name");
        TimerPassword = _configLoader.GetConfig("timer_value");

        ConfigId = _configLoader.GetConfig("config_id", "configuration");
        SharedConfigId = _configLoader.GetConfig("multi_tenant_shared_config_id", "dev_cluster");
        TargetTestTenant = _configLoader.GetConfig("target_test_tenant", "tenant5") ?? "tenant5";
        MetadataVersion = _configLoader.GetConfig("metadata_version", "26.01.20") ?? "26.01.20";
        TestDatabasePrefix = _configLoader.GetConfig("test_db_prefix", "mmria_test_");

        Console.WriteLine($"[TestConfigurationLoader] Configuration loaded:");
        Console.WriteLine($"  Mode: {(_configLoader.IsEnvironmentBased() ? "Environment Variables" : "AppSettings")}");
        Console.WriteLine($"  Tenants: {string.Join(",", Tenants.Length > 0 ? Tenants : ["(single-tenant)"])}");
        Console.WriteLine($"  CouchDB Template URL: {CouchDbTemplateUrl}");
        Console.WriteLine($"  Target Test Tenant: {TargetTestTenant}");
        Console.WriteLine($"  Test DB Prefix: {TestDatabasePrefix}");
    }

    /// <summary>
    /// Generate test database name following pattern: {prefix}{purpose}_{timestamp}.
    /// Example: mmria_test_mmrds_20260224_143025
    /// </summary>
    public string GenerateTestDatabaseName(string purpose)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return $"{TestDatabasePrefix}{purpose}_{timestamp}".ToLower();
    }

    /// <summary>
    /// Resolve tenant-specific CouchDB URL using template and target test tenant.
    /// </summary>
    public string ResolveTenantUrl(string? tenantName = null)
    {
        string tenant = tenantName ?? TargetTestTenant;
        return _configLoader.ResolveTenantUrl(CouchDbTemplateUrl, tenant);
    }

    /// <summary>
    /// Get list of tenant URLs for iteration.
    /// </summary>
    public List<(string TenantName, string CouchDbUrl)> GetTenantUrls()
    {
        var result = new List<(string, string)>();

        if (Tenants.Length == 0)
        {
            // Single tenant: use template URL directly
            result.Add((TargetTestTenant, CouchDbTemplateUrl));
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
