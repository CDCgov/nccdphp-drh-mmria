using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace mmria.server.util;

public static class MultiTenantConfigHelper
{
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
        // If we have multi-tenant configurations, try to find a match
        if (configList != null && configList.Count > 0)
        {
            Log.Information($"GetConfigurationForTenant: Searching for tenant with hostPrefix '{hostPrefix}' in {configList.Count} configurations");
            
            var matchingConfig = configList.FirstOrDefault(c => 
                c.GetString("app_instance_name", hostPrefix) == hostPrefix ||
                c.GetString("config_id", hostPrefix) == hostPrefix
            );
            
            if (matchingConfig != null)
            {
                Log.Information($": Found matching configuration for hostPrefix '{hostPrefix}'");
                return matchingConfig;
            }
            
            Log.Warning($"GetConfigurationForTenant: No matching configuration found for hostPrefix '{hostPrefix}'. Falling back to default configuration.");
        }
        else
        {
            Log.Warning($"GetConfigurationForTenant: configList is {(configList == null ? "null" : "empty")}. Falling back to default configuration for hostPrefix '{hostPrefix}'.");
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
        // If we have multi-tenant configurations, try to find a match
        if (configSetList != null && configSetList.Count > 0)
        {
            Log.Information($"GetDBConfigForTenant: Searching for tenant with hostPrefix '{hostPrefix}' in {configSetList.Count} configuration sets");
            
            foreach (var configSet in configSetList)
            {
                if (configSet.detail_list != null && configSet.detail_list.ContainsKey(hostPrefix))
                {
                    Log.Information($"GetDBConfigForTenant: Found matching DB configuration for hostPrefix '{hostPrefix}'");
                    return configSet.detail_list[hostPrefix];
                }
            }
            
            Log.Warning($"GetDBConfigForTenant: No matching DB configuration found for hostPrefix '{hostPrefix}' in any configuration set. Falling back to default configuration.");
        }
        else
        {
            Log.Warning($"GetDBConfigForTenant: configSetList is {(configSetList == null ? "null" : "empty")}. Falling back to default configuration for hostPrefix '{hostPrefix}'.");
        }
        
        // Fall back to single-tenant configuration
        return fallbackConfig?.GetDBConfig(hostPrefix);
    }
}