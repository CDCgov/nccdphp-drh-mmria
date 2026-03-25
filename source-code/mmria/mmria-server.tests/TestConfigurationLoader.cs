#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using mmria.common.couchdb;
using mmria.server.util;

namespace mmria_server.tests;

/// <summary>
/// Test-specific configuration loader for mmria-server tests.
/// Mirrors multi-tenant setup from production Program.cs.
/// Supports both local development (appsettings.local.json) and CI/CD pod environments (environment variables).
/// </summary>
public sealed class TestConfigurationLoader
{
    private const string PlaceholderPrefix = "__set_in_appsettings.local.json:";
    private readonly MultiTenantConfigurationLoader _configLoader;
    private readonly IConfiguration? _appSettingsConfig;
    private readonly string? _settingsRootDirectory;

    public string[] Tenants { get; private set; } = [];
    public string CouchDbTemplateUrl { get; private set; } = "http://localhost:5984";
    public string? TimerUserName { get; private set; }
    public string? TimerPassword { get; private set; }
    public string CentralCouchDbUrl { get; private set; } = "http://localhost:5984";
    public string CdcInstanceConfigId { get; private set; } = "mmria-services";
    public string? ConfigId { get; private set; }
    public string? SharedConfigId { get; private set; }
    public string TargetTestTenant { get; private set; } = "tenant5";
    public string[] StartupRebuildTenants { get; private set; } = [];
    public int StartupRebuildMaxConcurrentTenants { get; private set; } = 1;
    public string MetadataVersion { get; private set; } = "26.01.20";
    public string TestDatabasePrefix { get; private set; } = "mmria_test_";
    public int CaseLockMinutes { get; private set; } = 120;
    public int IjeNumberToGenerate { get; private set; } = 5;
    public string[] IjeJurisdicationSampling { get; private set; } = [];
    public int[] IjeYearOfDeathSampling { get; private set; } = [];
    public TestCredentialSettings TestCredentials { get; private set; } = new();
    public bool IsExampleSettingsLoaded { get; private set; }
    public bool IsLocalSettingsLoaded { get; private set; }

    /// <summary>
    /// Initialize by loading configuration from appsettings.local.example.json and appsettings.local.json
    /// (local development) or environment variables (CI/CD).
    /// </summary>
    public TestConfigurationLoader(string? settingsRootDirectory = null)
    {
        _settingsRootDirectory = settingsRootDirectory;

        string? exampleSettingsPath = FindSettingsFile("appsettings.local.example.json");
        string? localSettingsPath = FindSettingsFile("appsettings.local.json");

        if (!string.IsNullOrEmpty(exampleSettingsPath) || !string.IsNullOrEmpty(localSettingsPath))
        {
            var builder = new ConfigurationBuilder();

            if (!string.IsNullOrEmpty(exampleSettingsPath) && File.Exists(exampleSettingsPath))
            {
                builder.AddJsonFile(exampleSettingsPath, optional: false, reloadOnChange: false);
                IsExampleSettingsLoaded = true;
            }

            if (!string.IsNullOrEmpty(localSettingsPath) && File.Exists(localSettingsPath))
            {
                builder.AddJsonFile(localSettingsPath, optional: false, reloadOnChange: false);
                IsLocalSettingsLoaded = true;
            }

            _appSettingsConfig = builder.Build();
        }

        // Initialize MultiTenantConfigurationLoader with appsettings config (or null for env var fallback)
        _configLoader = new MultiTenantConfigurationLoader(_appSettingsConfig);
    }

    /// <summary>
    /// Locate a settings file by searching from the configured root, current directory, and test assembly location.
    /// </summary>
    private string? FindSettingsFile(string fileName)
    {
        if (!string.IsNullOrWhiteSpace(_settingsRootDirectory))
        {
            string directPath = Path.Combine(_settingsRootDirectory, fileName);
            if (File.Exists(directPath))
            {
                return Path.GetFullPath(directPath);
            }

            return null;
        }

        var searchRoots = new List<string>();

        if (!string.IsNullOrWhiteSpace(Environment.CurrentDirectory))
        {
            searchRoots.Add(Environment.CurrentDirectory);
        }

        string? assemblyDirectory = Path.GetDirectoryName(typeof(TestConfigurationLoader).Assembly.Location);
        if (!string.IsNullOrWhiteSpace(assemblyDirectory))
        {
            searchRoots.Add(assemblyDirectory);
        }

        foreach (string searchRoot in searchRoots)
        {
            string? current = Path.GetFullPath(searchRoot);
            for (int i = 0; i < 12 && !string.IsNullOrWhiteSpace(current); i++)
            {
                string candidate = Path.Combine(current, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                string? parent = Directory.GetParent(current)?.FullName;
                if (string.IsNullOrWhiteSpace(parent) || string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                current = parent;
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
        string? startupRebuildTenants = _configLoader.GetConfig(DbRebuildSettings.StartupRebuildTenantsKey);
        StartupRebuildTenants = DbRebuildSettings.ResolveStartupRebuildTenants(startupRebuildTenants, multiTenantJurisdictions).ToArray();
        StartupRebuildMaxConcurrentTenants = DbRebuildSettings.ResolveMaxConcurrentTenants(
            _configLoader.GetConfig(DbRebuildSettings.StartupRebuildMaxConcurrentTenantsKey));

        CouchDbTemplateUrl = _configLoader.GetConfig("multi_tenant_template_couchdb_url") 
            ?? _configLoader.GetConfig("couchdb_url") 
            ?? "http://localhost:5984";

        TimerUserName = _configLoader.GetConfig("timer_user_name");
        TimerPassword = _configLoader.GetConfig("timer_password")
            ?? _configLoader.GetConfig("timer_value");

        CentralCouchDbUrl = _configLoader.GetConfig("central_couchdb_url")
            ?? _configLoader.GetConfig("cdc_instance_couchdb_url")
            ?? _configLoader.GetConfig("couchdb_url")
            ?? "http://localhost:5984";
        CdcInstanceConfigId = _configLoader.GetConfig("cdc_instance_config_id", "mmria-services")
            ?? "mmria-services";

        ConfigId = _configLoader.GetConfig("config_id", "configuration");
        SharedConfigId = _configLoader.GetConfig("multi_tenant_shared_config_id", "dev_cluster");
        TargetTestTenant = _configLoader.GetConfig("target_test_tenant", "tenant5") ?? "tenant5";
        MetadataVersion = _configLoader.GetConfig("metadata_version", "26.01.20") ?? "26.01.20";
        TestDatabasePrefix = _configLoader.GetConfig("test_db_prefix", "") ?? "";

        // Load case_lock_minutes for tests (default 120 minutes)
        var caseLockStr = _configLoader.GetConfig("case_lock_minutes");
        if (!string.IsNullOrWhiteSpace(caseLockStr) && int.TryParse(caseLockStr, out var parsedMinutes))
        {
            CaseLockMinutes = parsedMinutes;
        }

        var ijeNumberToGenerate = _configLoader.GetConfig("ije_number_to_generate");
        if (!string.IsNullOrWhiteSpace(ijeNumberToGenerate) && int.TryParse(ijeNumberToGenerate, out var parsedIjeNumberToGenerate))
        {
            IjeNumberToGenerate = parsedIjeNumberToGenerate;
        }

        IjeJurisdicationSampling = ParseStringArray(_configLoader.GetConfig("ije_jurisdication_sampling"));
        IjeYearOfDeathSampling = ParseIntArray(_configLoader.GetConfig("ije_year_of_death_sampling"));
        TestCredentials = LoadTestCredentials();

        Console.WriteLine($"[TestConfigurationLoader] Configuration loaded: Mode: {(_configLoader.IsEnvironmentBased() ? "Environment Variables" : "AppSettings")}, Tenants: {string.Join(",", Tenants.Length > 0 ? Tenants : new[] { "(single-tenant)" })}, Startup Rebuild Tenants: {string.Join(",", StartupRebuildTenants.Length > 0 ? StartupRebuildTenants : new[] { "(fallback)" })}, Startup Rebuild Max Concurrent Tenants: {StartupRebuildMaxConcurrentTenants}, CouchDB Template URL: {CouchDbTemplateUrl}, Target Test Tenant: {TargetTestTenant}, Test DB Prefix: {TestDatabasePrefix}, Case Lock Minutes: {CaseLockMinutes}, IJE Count: {IjeNumberToGenerate}, IJE Jurisdictions: {string.Join(",", IjeJurisdicationSampling)}, IJE Years: {string.Join(",", IjeYearOfDeathSampling)}");
    }

    public bool HasResolvedSensitiveSettings()
    {
        return GetUnsetSensitiveSettings().Count == 0;
    }

    public IReadOnlyList<string> GetUnsetSensitiveSettings()
    {
        var unsetKeys = new List<string>();

        AddIfUnset(unsetKeys, "mmria_settings:timer_user_name", TimerUserName);
        AddIfUnset(unsetKeys, "mmria_settings:timer_password", TimerPassword);
        AddIfUnset(unsetKeys, "test_credentials:shared_users:primary_user_name", TestCredentials.SharedUsers.PrimaryUserName);
        AddIfUnset(unsetKeys, "test_credentials:shared_users:secondary_user_name", TestCredentials.SharedUsers.SecondaryUserName);
        AddIfUnset(unsetKeys, "test_credentials:shared_users:password", TestCredentials.SharedUsers.Password);
        AddIfUnset(unsetKeys, "test_credentials:shared_users:invalid_password_for_primary_user", TestCredentials.SharedUsers.InvalidPasswordForPrimaryUser);
        AddIfUnset(unsetKeys, "test_credentials:sample_credentials:test_harness_user_name", TestCredentials.SampleCredentials.TestHarnessUserName);
        AddIfUnset(unsetKeys, "test_credentials:sample_credentials:test_harness_password", TestCredentials.SampleCredentials.TestHarnessPassword);
        AddIfUnset(unsetKeys, "test_credentials:sample_credentials:stub_db_user_name", TestCredentials.SampleCredentials.StubDbUserName);
        AddIfUnset(unsetKeys, "test_credentials:sample_credentials:stub_db_password", TestCredentials.SampleCredentials.StubDbPassword);
        AddIfUnset(unsetKeys, "test_credentials:sample_credentials:form_url_encoded_password", TestCredentials.SampleCredentials.FormUrlEncodedPassword);
        AddIfUnset(unsetKeys, "test_credentials:sample_credentials:user_creation_password", TestCredentials.SampleCredentials.UserCreationPassword);
        AddIfUnset(unsetKeys, "test_credentials:sample_credentials:alternate_user_creation_password", TestCredentials.SampleCredentials.AlternateUserCreationPassword);

        return unsetKeys;
    }

    public string GetSensitiveSettingsSetupMessage()
    {
        var unsetKeys = GetUnsetSensitiveSettings();
        return $"Local test credentials are not fully configured. Copy appsettings.local.example.json to appsettings.local.json and preserve the existing values. Unresolved settings: {string.Join(", ", unsetKeys)}";
    }

    private TestCredentialSettings LoadTestCredentials()
    {
        return new TestCredentialSettings
        {
            SharedUsers = new SharedTestUsers
            {
                PrimaryUserName = GetAppSettingValue("test_credentials:shared_users:primary_user_name"),
                SecondaryUserName = GetAppSettingValue("test_credentials:shared_users:secondary_user_name"),
                Password = GetAppSettingValue("test_credentials:shared_users:password"),
                InvalidPasswordForPrimaryUser = GetAppSettingValue("test_credentials:shared_users:invalid_password_for_primary_user")
            },
            SampleCredentials = new SampleCredentialSettings
            {
                TestHarnessUserName = GetAppSettingValue("test_credentials:sample_credentials:test_harness_user_name"),
                TestHarnessPassword = GetAppSettingValue("test_credentials:sample_credentials:test_harness_password"),
                StubDbUserName = GetAppSettingValue("test_credentials:sample_credentials:stub_db_user_name"),
                StubDbPassword = GetAppSettingValue("test_credentials:sample_credentials:stub_db_password"),
                FormUrlEncodedPassword = GetAppSettingValue("test_credentials:sample_credentials:form_url_encoded_password"),
                UserCreationPassword = GetAppSettingValue("test_credentials:sample_credentials:user_creation_password"),
                AlternateUserCreationPassword = GetAppSettingValue("test_credentials:sample_credentials:alternate_user_creation_password")
            }
        };
    }

    private string GetAppSettingValue(string key)
    {
        return _appSettingsConfig?[key] ?? string.Empty;
    }

    private static void AddIfUnset(List<string> unsetKeys, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith(PlaceholderPrefix, StringComparison.Ordinal))
        {
            unsetKeys.Add(key);
        }
    }

    private static string[] ParseStringArray(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static int[] ParseIntArray(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        var result = new List<int>();

        foreach (var item in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(item, out var parsed))
            {
                result.Add(parsed);
            }
        }

        return result.ToArray();
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
