using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace mmria.server.util;

public static class MultiTenantConfigHelper
{
    public static bool IsTenantAvailable
    (
        List<mmria.common.couchdb.OverridableConfiguration> configList,
        List<mmria.common.couchdb.ConfigurationSet> configSetList,
        mmria.common.couchdb.OverridableConfiguration fallbackConfig,
        string hostPrefix
    )
    {
        if (!IsMultiTenantMode(fallbackConfig, hostPrefix))
        {
            return true;
        }

        return HasExactConfigurationForTenant(configList, hostPrefix) &&
            HasExactConfigurationSetForTenant(configSetList, hostPrefix);
    }

    public static mmria.common.couchdb.ConfigurationSet GetConfigurationSetForTenant
    (
        List<mmria.common.couchdb.ConfigurationSet> configSetList,
        mmria.common.couchdb.ConfigurationSet fallbackConfig,
        string hostPrefix
    )
    {
        string normalizedHostPrefix = NormalizeHostPrefix(hostPrefix);

        if (configSetList != null)
        {
            lock (configSetList)
            {
                foreach (var configSet in configSetList)
                {
                    if (HasExactConfigurationSetForTenant(configSet, normalizedHostPrefix))
                    {
                        return configSet;
                    }
                }
            }
        }

        if (IsMultiTenantMode(null, normalizedHostPrefix))
        {
            return null;
        }

        return fallbackConfig;
    }

    /// <summary>
    /// Gets the appropriate configuration for multi-tenant or single-tenant mode
    /// </summary>
    /// <param name="configList">List of multi-tenant configurations</param>
    /// <param name="fallbackConfig">Single-tenant configuration to use as fallback</param>
    /// <param name="hostPrefix">Host prefix to match against</param>
    /// <returns>Matched configuration or fallback</returns>
    public static mmria.common.couchdb.OverridableConfiguration GetConfigurationForTenant
    (
        List<mmria.common.couchdb.OverridableConfiguration> configList,
        mmria.common.couchdb.OverridableConfiguration fallbackConfig,
        string hostPrefix
    )
    {
        string normalizedHostPrefix = NormalizeHostPrefix(hostPrefix);

        // If we have multi-tenant configurations, try to find a match
        if (configList != null && configList.Count > 0)
        {
            Log.Information($"GetConfigurationForTenant: Searching for tenant with hostPrefix '{normalizedHostPrefix}' in {configList.Count} configurations");

            mmria.common.couchdb.OverridableConfiguration matchingConfig = null;
            lock (configList)
            {
                if (IsMultiTenantMode(fallbackConfig, normalizedHostPrefix))
                {
                    matchingConfig = configList.FirstOrDefault(c => MatchesExactTenantConfiguration(c, normalizedHostPrefix));
                }
                else
                {
                    matchingConfig = configList.FirstOrDefault(c => MatchesTenant(c, normalizedHostPrefix));
                }
            }
            
            if (matchingConfig != null)
            {
                Log.Information($": Found matching configuration for hostPrefix '{normalizedHostPrefix}'");
                return matchingConfig;
            }
            
            if (IsMultiTenantMode(fallbackConfig, normalizedHostPrefix))
            {
                Log.Warning($"GetConfigurationForTenant: No matching configuration found for hostPrefix '{normalizedHostPrefix}'. Returning null in multi-tenant mode.");
                return null;
            }

            Log.Warning($"GetConfigurationForTenant: No matching configuration found for hostPrefix '{normalizedHostPrefix}'. Falling back to default configuration.");
        }
        else
        {
            if (IsMultiTenantMode(fallbackConfig, normalizedHostPrefix))
            {
                Log.Warning($"GetConfigurationForTenant: configList is {(configList == null ? "null" : "empty")}. Returning null for hostPrefix '{normalizedHostPrefix}' in multi-tenant mode.");
                return null;
            }

            Log.Warning($"GetConfigurationForTenant: configList is {(configList == null ? "null" : "empty")}. Falling back to default configuration for hostPrefix '{normalizedHostPrefix}'.");
        }
        
        // Fall back to single-tenant configuration
        return fallbackConfig;
    }

    /// <summary>
    /// Gets the appropriate DB configuration detail for multi-tenant or single-tenant mode
    /// </summary>
    /// <param name="configSetList">List of multi-tenant configuration sets</param>
    /// <param name="fallbackConfig">Configuration to extract DBConfigurationDetail from as fallback</param>
    /// <param name="hostPrefix">Host prefix to match against</param>
    /// <returns>Matched DBConfigurationDetail or fallback</returns>
    public static mmria.common.couchdb.DBConfigurationDetail GetDBConfigForTenant
    (
        List<mmria.common.couchdb.ConfigurationSet> configSetList,
        mmria.common.couchdb.OverridableConfiguration fallbackConfig,
        string hostPrefix
    )
    {
        string normalizedHostPrefix = NormalizeHostPrefix(hostPrefix);

        // If we have multi-tenant configurations, try to find a match
        if (configSetList != null && configSetList.Count > 0)
        {
            Log.Information($"GetDBConfigForTenant: Searching for tenant with hostPrefix '{normalizedHostPrefix}' in {configSetList.Count} configuration sets");

            lock (configSetList)
            {
                foreach (var configSet in configSetList)
                {
                    if (TryGetExactConfigurationSetDetail(configSet, normalizedHostPrefix, out var dbConfig))
                    {
                        Log.Information($"GetDBConfigForTenant: Found matching DB configuration for hostPrefix '{normalizedHostPrefix}'");
                        return dbConfig;
                    }
                }
            }
            
            if (IsMultiTenantMode(fallbackConfig, normalizedHostPrefix))
            {
                Log.Warning($"GetDBConfigForTenant: No matching DB configuration found for hostPrefix '{normalizedHostPrefix}' in any configuration set. Returning null in multi-tenant mode.");
                return null;
            }

            Log.Warning($"GetDBConfigForTenant: No matching DB configuration found for hostPrefix '{normalizedHostPrefix}' in any configuration set. Falling back to default configuration.");
        }
        else
        {
            if (IsMultiTenantMode(fallbackConfig, normalizedHostPrefix))
            {
                Log.Warning($"GetDBConfigForTenant: configSetList is {(configSetList == null ? "null" : "empty")}. Returning null for hostPrefix '{normalizedHostPrefix}' in multi-tenant mode.");
                return null;
            }

            Log.Warning($"GetDBConfigForTenant: configSetList is {(configSetList == null ? "null" : "empty")}. Falling back to default configuration for hostPrefix '{normalizedHostPrefix}'.");
        }

        // Final fallback to legacy single-tenant configuration shape
        return fallbackConfig?.GetDBConfig(normalizedHostPrefix);
    }

    private static bool IsMultiTenantMode(mmria.common.couchdb.OverridableConfiguration fallbackConfig, string hostPrefix)
    {
        bool? configValue = TryGetBoolean(fallbackConfig, "is_multi_tenant_mode", hostPrefix)
            ?? TryGetBoolean(fallbackConfig, "is_multi_tenant_mode", "shared");

        if (configValue.HasValue)
        {
            return configValue.Value;
        }

        string configModeValue = TryGetString(fallbackConfig, "is_multi_tenant_mode", hostPrefix)
            ?? TryGetString(fallbackConfig, "is_multi_tenant_mode", "shared");
        if (bool.TryParse(configModeValue, out bool parsedModeValue))
        {
            return parsedModeValue;
        }

        string multiTenantJurisdictions =
            TryGetString(fallbackConfig, "multi_tenant_jurisdictions", hostPrefix) ??
            TryGetString(fallbackConfig, "multi_tenant_jurisdictions", "shared") ??
            System.Environment.GetEnvironmentVariable("multi_tenant_jurisdictions");
        string multiTenantTemplateUrl =
            TryGetString(fallbackConfig, "multi_tenant_shared_config_id_template_couchdb_url", hostPrefix) ??
            TryGetString(fallbackConfig, "multi_tenant_shared_config_id_template_couchdb_url", "shared") ??
            System.Environment.GetEnvironmentVariable("multi_tenant_shared_config_id_template_couchdb_url");
        string multiTenantRebuildSource =
            TryGetString(fallbackConfig, "multi_tenant_re_build_src", hostPrefix) ??
            TryGetString(fallbackConfig, "multi_tenant_re_build_src", "shared") ??
            System.Environment.GetEnvironmentVariable("multi_tenant_re_build_src");

        return
            !string.IsNullOrWhiteSpace(multiTenantJurisdictions) ||
            !string.IsNullOrWhiteSpace(multiTenantTemplateUrl) ||
            !string.IsNullOrWhiteSpace(multiTenantRebuildSource);
    }

    private static string NormalizeHostPrefix(string hostPrefix)
    {
        return string.IsNullOrWhiteSpace(hostPrefix) ? null : hostPrefix.Trim();
    }

    private static bool? TryGetBoolean(mmria.common.couchdb.OverridableConfiguration configuration, string key, string prefix)
    {
        if (configuration == null || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(prefix))
        {
            return null;
        }

        try
        {
            return configuration.GetBoolean(key, prefix);
        }
        catch
        {
            return null;
        }
    }

    private static string TryGetString(mmria.common.couchdb.OverridableConfiguration configuration, string key, string prefix)
    {
        if (configuration == null || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(prefix))
        {
            return null;
        }

        try
        {
            return configuration.GetString(key, prefix);
        }
        catch
        {
            return null;
        }
    }

    private static bool MatchesTenant(mmria.common.couchdb.OverridableConfiguration config, string hostPrefix)
    {
        if (config == null || string.IsNullOrWhiteSpace(hostPrefix))
        {
            return false;
        }

        return
            string.Equals(config.GetString("app_instance_name", hostPrefix), hostPrefix, System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(config.GetString("config_id", hostPrefix), hostPrefix, System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasConfigurationForTenant(List<mmria.common.couchdb.OverridableConfiguration> configList, string hostPrefix)
    {
        if (configList == null || string.IsNullOrWhiteSpace(hostPrefix))
        {
            return false;
        }

        lock (configList)
        {
            return configList.Any(config => MatchesTenant(config, hostPrefix));
        }
    }

    private static bool HasExactConfigurationForTenant(List<mmria.common.couchdb.OverridableConfiguration> configList, string hostPrefix)
    {
        if (configList == null || string.IsNullOrWhiteSpace(hostPrefix))
        {
            return false;
        }

        lock (configList)
        {
            return configList.Any(config => MatchesExactTenantConfiguration(config, hostPrefix));
        }
    }

    private static bool HasExactConfigurationSetForTenant(List<mmria.common.couchdb.ConfigurationSet> configSetList, string hostPrefix)
    {
        if (configSetList == null || string.IsNullOrWhiteSpace(hostPrefix))
        {
            return false;
        }

        lock (configSetList)
        {
            return configSetList.Any(configSet => HasExactConfigurationSetForTenant(configSet, hostPrefix));
        }
    }

    private static bool HasExactConfigurationSetForTenant(mmria.common.couchdb.ConfigurationSet configSet, string hostPrefix)
    {
        return TryGetExactConfigurationSetDetail(configSet, hostPrefix, out _);
    }

    private static bool TryGetExactConfigurationSetDetail
    (
        mmria.common.couchdb.ConfigurationSet configSet,
        string hostPrefix,
        out mmria.common.couchdb.DBConfigurationDetail dbConfig
    )
    {
        dbConfig = null;

        if (configSet?.detail_list == null || string.IsNullOrWhiteSpace(hostPrefix))
        {
            return false;
        }

        if (!string.Equals(configSet._id, hostPrefix, System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        foreach (var entry in configSet.detail_list)
        {
            if (!string.Equals(entry.Key, hostPrefix, System.StringComparison.OrdinalIgnoreCase) || entry.Value == null)
            {
                continue;
            }

            dbConfig = entry.Value;
            return true;
        }

        return false;
    }

    private static bool MatchesExactTenantConfiguration(mmria.common.couchdb.OverridableConfiguration config, string hostPrefix)
    {
        if (config == null || string.IsNullOrWhiteSpace(hostPrefix) || string.IsNullOrWhiteSpace(config._id))
        {
            return false;
        }

        return config._id.StartsWith($"{hostPrefix}_", System.StringComparison.OrdinalIgnoreCase);
    }
}
