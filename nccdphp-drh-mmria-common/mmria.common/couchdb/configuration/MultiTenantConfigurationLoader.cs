#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using mmria.common.getset;

namespace mmria.common.couchdb;

/// <summary>
/// Loads multi-tenant configuration following the pattern defined in mmria-server Program.cs
/// Supports both environment variable and appsettings.json configuration sources.
/// </summary>
public sealed class MultiTenantConfigurationLoader
{
    private readonly IConfiguration? _appSettingsConfiguration;

    /// <summary>
    /// Initialize with optional appsettings configuration (for local development)
    /// </summary>
    public MultiTenantConfigurationLoader(IConfiguration? appSettingsConfiguration = null)
    {
        _appSettingsConfiguration = appSettingsConfiguration;
    }

    /// <summary>
    /// Determines whether to use environment variables or appsettings for configuration.
    /// Checks is_environment_based flag with fallback: environment variable > appsettings.
    /// </summary>
    public bool IsEnvironmentBased()
    {
        // Read is_environment_based directly without calling GetConfig to avoid circular dependency
        string? envValue = Environment.GetEnvironmentVariable("is_environment_based");
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue.ToLower() == "true" || envValue == "1";
        }

        if (_appSettingsConfiguration != null)
        {
            string? configValue = _appSettingsConfiguration["mmria_settings:is_environment_based"];
            if (!string.IsNullOrEmpty(configValue))
            {
                return configValue.ToLower() == "true" || configValue == "1";
            }
        }

        return false;
    }

    /// <summary>
    /// Gets configuration value with precedence: environment variable > appsettings.json > default
    /// </summary>
    public string? GetConfig(string key, string? defaultValue = null)
    {
        bool isEnvironmentBased = IsEnvironmentBased();
        
        if (isEnvironmentBased)
        {
            return Environment.GetEnvironmentVariable(key) ?? defaultValue;
        }
        else
        {
            if (_appSettingsConfiguration != null)
            {
                string? value = _appSettingsConfiguration[$"mmria_settings:{key}"];
                return !string.IsNullOrEmpty(value) ? value : defaultValue;
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Parses comma-separated tenant list into array, trimming whitespace
    /// Example: "tenant1,tenant2,cdc" => ["tenant1", "tenant2", "cdc"]
    /// </summary>
    public string[] ParseTenants(string? commaSeparatedTenants)
    {
        if (string.IsNullOrWhiteSpace(commaSeparatedTenants))
        {
            return [];
        }

        return commaSeparatedTenants
            .Split(',')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToArray();
    }

    /// <summary>
    /// Resolves tenant-specific URL by replacing {replace} token with tenant name
    /// Example: "http://{replace}-couchdb.local:5984" + "jurisdiction1" 
    ///        => "http://jurisdiction1-couchdb.local:5984"
    /// </summary>
    public string ResolveTenantUrl(string templateUrl, string tenantName)
    {
        if (string.IsNullOrEmpty(templateUrl))
        {
            throw new ArgumentNullException(nameof(templateUrl));
        }

        return templateUrl.Replace("{replace}", tenantName);
    }

    /// <summary>
    /// Loads OverridableConfiguration objects for all tenants or single tenant.
    /// Uses same pattern as mmria-server Program.cs lines 148-165
    /// </summary>
    public async Task<List<OverridableConfiguration>> LoadOverridableConfigurationsAsync(
        string[] tenants,
        string couchDbTemplateUrl,
        string? timerUserName,
        string? timerPassword,
        string? sharedConfigId,
        string? configId,
        CouchDbHttpClient httpClient)
    {
        var result = new List<OverridableConfiguration>();

        if (tenants.Length == 0)
        {
            // Single tenant mode: use template URL directly as base CouchDB URL
            var singleTenantConfig = await GetOverridableConfigurationAsync(
                couchDbTemplateUrl,
                timerUserName,
                timerPassword,
                sharedConfigId ?? "shared_config");

            singleTenantConfig._id = $"{configId}_{sharedConfigId}";
            result.Add(singleTenantConfig);
        }
        else
        {
            // Multi-tenant mode: resolve URL for each tenant, load config
            foreach (var tenant in tenants)
            {
                var tenantCouchDbUrl = ResolveTenantUrl(couchDbTemplateUrl, tenant);

                var tenantConfig = await GetOverridableConfigurationAsync(
                    tenantCouchDbUrl,
                    timerUserName,
                    timerPassword,
                    sharedConfigId ?? "shared_config");

                tenantConfig._id = $"{tenant}_{sharedConfigId}";
                result.Add(tenantConfig);
            }
        }

        return result;
    }

    /// <summary>
    /// Loads ConfigurationSet objects for all tenants or single tenant.
    /// Uses same pattern as mmria-server Program.cs lines 167-193
    /// </summary>
    public async Task<List<ConfigurationSet>> LoadConfigurationSetsAsync(
        string[] tenants,
        string couchDbTemplateUrl,
        string? timerUserName,
        string? timerPassword,
        string? configId,
        CouchDbHttpClient httpClient)
    {
        var result = new List<ConfigurationSet>();

        if (tenants.Length == 0)
        {
            // Single tenant mode
            var singleTenantConfig = await GetConfigurationSetAsync(
                couchDbTemplateUrl,
                configId ?? "configuration",
                timerUserName,
                timerPassword);

            result.Add(singleTenantConfig);
        }
        else
        {
            // Multi-tenant mode: resolve URL for each tenant, load config
            foreach (var tenant in tenants)
            {
                var tenantCouchDbUrl = ResolveTenantUrl(couchDbTemplateUrl, tenant);

                var tenantConfig = await GetConfigurationSetAsync(
                    tenantCouchDbUrl,
                    tenant,
                    timerUserName,
                    timerPassword);

                result.Add(tenantConfig);
            }
        }

        return result;
    }

    /// <summary>
    /// Fetches OverridableConfiguration from CouchDB /configuration/{sharedConfigId} endpoint
    /// Mirrors Program.cs GetOverridableConfiguration() method
    /// </summary>
    private async Task<OverridableConfiguration> GetOverridableConfigurationAsync(
        string couchDbUrl,
        string? userName,
        string? password,
        string sharedConfigId)
    {
        var result = new OverridableConfiguration();

        try
        {
            var factory = new SimpleHttpClientFactory();
            var httpClient = new CouchDbHttpClient(factory);

            string requestUrl = $"{couchDbUrl}/configuration/{sharedConfigId}";

            string responseJson = await httpClient.ExecuteAsync(
                "GET",
                requestUrl,
                null,
                userName,
                password,
                "application/json");

            result = JsonSerializer.Deserialize<OverridableConfiguration>(responseJson)
                ?? new OverridableConfiguration();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load OverridableConfiguration from {couchDbUrl}: {ex.Message}");
            // Return empty config object rather than throwing
            result = new OverridableConfiguration();
        }

        return result;
    }

    /// <summary>
    /// Fetches ConfigurationSet from CouchDB /configuration/{configId} endpoint
    /// Mirrors Program.cs GetConfiguration() method
    /// </summary>
    private async Task<ConfigurationSet> GetConfigurationSetAsync(
        string couchDbUrl,
        string configId,
        string? userName,
        string? password)
    {
        var result = new ConfigurationSet();

        try
        {
            var factory = new SimpleHttpClientFactory();
            var httpClient = new CouchDbHttpClient(factory);

            string requestUrl = $"{couchDbUrl}/configuration/{configId}";

            string responseJson = await httpClient.ExecuteAsync(
                "GET",
                requestUrl,
                null,
                userName,
                password,
                "application/json");

            result = JsonSerializer.Deserialize<ConfigurationSet>(responseJson)
                ?? new ConfigurationSet();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load ConfigurationSet from {couchDbUrl}: {ex.Message}");
            // Return empty config object rather than throwing
            result = new ConfigurationSet();
        }

        return result;
    }
}
