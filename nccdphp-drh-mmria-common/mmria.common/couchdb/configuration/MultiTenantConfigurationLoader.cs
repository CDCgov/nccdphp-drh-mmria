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
    /// Loads required OverridableConfiguration objects for startup and throws when any required
    /// configuration cannot be loaded. This is intended for startup/bootstrap paths that must fail
    /// fast instead of continuing with empty runtime configuration.
    /// </summary>
    public async Task<List<OverridableConfiguration>> LoadRequiredOverridableConfigurationsAsync(
        string[] tenants,
        string couchDbTemplateUrl,
        string? timerUserName,
        string? timerPassword,
        string? sharedConfigId,
        string? configId,
        CouchDbHttpClient httpClient)
    {
        var result = new List<OverridableConfiguration>();
        string resolvedSharedConfigId = sharedConfigId ?? "shared_config";

        if (tenants.Length == 0)
        {
            var singleTenantConfig = await GetRequiredOverridableConfigurationAsync(
                couchDbTemplateUrl,
                timerUserName,
                timerPassword,
                resolvedSharedConfigId,
                $"single-tenant startup (config_id='{configId ?? "(null)"}')",
                httpClient);

            singleTenantConfig._id = $"{configId}_{resolvedSharedConfigId}";
            result.Add(singleTenantConfig);
            return result;
        }

        foreach (var tenant in tenants)
        {
            string normalizedTenant = tenant.Trim();
            string tenantCouchDbUrl = ResolveTenantUrl(couchDbTemplateUrl, normalizedTenant);

            var tenantConfig = await GetRequiredOverridableConfigurationAsync(
                tenantCouchDbUrl,
                timerUserName,
                timerPassword,
                resolvedSharedConfigId,
                $"tenant '{normalizedTenant}' startup",
                httpClient);

            tenantConfig._id = $"{normalizedTenant}_{resolvedSharedConfigId}";
            result.Add(tenantConfig);
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
    /// Loads required ConfigurationSet objects for startup and throws when any required
    /// configuration cannot be loaded. This is intended for startup/bootstrap paths that must fail
    /// fast instead of continuing with empty runtime configuration.
    /// </summary>
    public async Task<List<ConfigurationSet>> LoadRequiredConfigurationSetsAsync(
        string[] tenants,
        string couchDbTemplateUrl,
        string? timerUserName,
        string? timerPassword,
        string? configId,
        CouchDbHttpClient httpClient)
    {
        var result = new List<ConfigurationSet>();
        string resolvedConfigId = configId ?? "configuration";

        if (tenants.Length == 0)
        {
            var singleTenantConfig = await GetRequiredConfigurationSetAsync(
                couchDbTemplateUrl,
                resolvedConfigId,
                timerUserName,
                timerPassword,
                $"single-tenant startup (config_id='{resolvedConfigId}')",
                httpClient);

            result.Add(singleTenantConfig);
            return result;
        }

        foreach (var tenant in tenants)
        {
            string normalizedTenant = tenant.Trim();
            string tenantCouchDbUrl = ResolveTenantUrl(couchDbTemplateUrl, normalizedTenant);

            var tenantConfig = await GetRequiredConfigurationSetAsync(
                tenantCouchDbUrl,
                normalizedTenant,
                timerUserName,
                timerPassword,
                $"tenant '{normalizedTenant}' startup",
                httpClient);

            result.Add(tenantConfig);
        }

        return result;
    }

    public async Task<OverridableConfiguration?> LoadTenantOverridableConfigurationAsync(
        string tenantName,
        string couchDbTemplateUrl,
        string? timerUserName,
        string? timerPassword,
        string? sharedConfigId,
        CouchDbHttpClient httpClient)
    {
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new ArgumentException("Tenant name is required.", nameof(tenantName));
        }

        string tenantCouchDbUrl = ResolveTenantUrl(couchDbTemplateUrl, tenantName.Trim());
        return await TryGetOverridableConfigurationAsync(
            tenantCouchDbUrl,
            timerUserName,
            timerPassword,
            sharedConfigId ?? "shared_config",
            httpClient);
    }

    public async Task<ConfigurationSet?> LoadTenantConfigurationSetAsync(
        string tenantName,
        string couchDbTemplateUrl,
        string? timerUserName,
        string? timerPassword,
        CouchDbHttpClient httpClient)
    {
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new ArgumentException("Tenant name is required.", nameof(tenantName));
        }

        string tenantCouchDbUrl = ResolveTenantUrl(couchDbTemplateUrl, tenantName.Trim());
        return await TryGetConfigurationSetAsync(
            tenantCouchDbUrl,
            tenantName.Trim(),
            timerUserName,
            timerPassword,
            httpClient);
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

    private async Task<OverridableConfiguration?> TryGetOverridableConfigurationAsync(
        string couchDbUrl,
        string? userName,
        string? password,
        string sharedConfigId,
        CouchDbHttpClient httpClient)
    {
        string requestUrl = $"{couchDbUrl}/configuration/{sharedConfigId}";
        string responseJson = await httpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            userName,
            password,
            "application/json");

        if (IsNotFoundResponse(responseJson))
        {
            return null;
        }

        ThrowIfErrorResponse(responseJson, $"Failed to load OverridableConfiguration from {couchDbUrl}");

        return JsonSerializer.Deserialize<OverridableConfiguration>(responseJson);
    }

    private async Task<OverridableConfiguration> GetRequiredOverridableConfigurationAsync(
        string couchDbUrl,
        string? userName,
        string? password,
        string sharedConfigId,
        string loadContext,
        CouchDbHttpClient httpClient)
    {
        OverridableConfiguration? result;
        try
        {
            result = await TryGetOverridableConfigurationAsync(
                couchDbUrl,
                userName,
                password,
                sharedConfigId,
                httpClient);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Unable to load required OverridableConfiguration '{sharedConfigId}' for {loadContext} from '{couchDbUrl}'.",
                ex);
        }

        if (result == null)
        {
            throw new InvalidOperationException(
                $"Required OverridableConfiguration '{sharedConfigId}' was not found for {loadContext} at '{couchDbUrl}'.");
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

    private async Task<ConfigurationSet?> TryGetConfigurationSetAsync(
        string couchDbUrl,
        string configId,
        string? userName,
        string? password,
        CouchDbHttpClient httpClient)
    {
        string requestUrl = $"{couchDbUrl}/configuration/{configId}";
        string responseJson = await httpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            userName,
            password,
            "application/json");

        if (IsNotFoundResponse(responseJson))
        {
            return null;
        }

        ThrowIfErrorResponse(responseJson, $"Failed to load ConfigurationSet from {couchDbUrl}");

        return JsonSerializer.Deserialize<ConfigurationSet>(responseJson);
    }

    private async Task<ConfigurationSet> GetRequiredConfigurationSetAsync(
        string couchDbUrl,
        string configId,
        string? userName,
        string? password,
        string loadContext,
        CouchDbHttpClient httpClient)
    {
        ConfigurationSet? result;
        try
        {
            result = await TryGetConfigurationSetAsync(
                couchDbUrl,
                configId,
                userName,
                password,
                httpClient);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Unable to load required ConfigurationSet '{configId}' for {loadContext} from '{couchDbUrl}'.",
                ex);
        }

        if (result == null)
        {
            throw new InvalidOperationException(
                $"Required ConfigurationSet '{configId}' was not found for {loadContext} at '{couchDbUrl}'.");
        }

        return result;
    }

    private static bool IsNotFoundResponse(string? responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(responseJson);
            if (!document.RootElement.TryGetProperty("error", out JsonElement errorElement))
            {
                return false;
            }

            return string.Equals(errorElement.GetString(), "not_found", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void ThrowIfErrorResponse(string? responseJson, string failurePrefix)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(responseJson);
            if (!document.RootElement.TryGetProperty("error", out JsonElement errorElement))
            {
                return;
            }

            string? error = errorElement.GetString();
            if (string.IsNullOrWhiteSpace(error))
            {
                return;
            }

            string? reason = document.RootElement.TryGetProperty("reason", out JsonElement reasonElement)
                ? reasonElement.GetString()
                : null;

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(reason)
                    ? $"{failurePrefix}. CouchDB error: {error}."
                    : $"{failurePrefix}. CouchDB error: {error}. Reason: {reason}.");
        }
        catch (JsonException)
        {
            // Non-JSON responses are handled by the caller.
        }
    }
}
