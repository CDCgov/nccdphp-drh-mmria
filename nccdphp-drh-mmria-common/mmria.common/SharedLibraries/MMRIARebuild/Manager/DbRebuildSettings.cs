using System;
using System.Collections.Generic;
using mmria.common.couchdb;

namespace mmria.common.SharedLibraries.MMRIARebuild.Manager;

internal static class DbRebuildSettings
{
    internal const string StartupRebuildMaxConcurrentTenantsKey = "startup_rebuild_max_concurrent_tenants";
    internal const string StartupRebuildIndexAddBeginningKey = "startup_rebuild_index_add_beginning";
    internal const string StartupRebuildExcludeFromRebuildKey = "startup_rebuild_exclude_from_rebuild";

    internal static int ResolveMaxConcurrentTenants(string? rawValue)
    {
        return int.TryParse(rawValue, out int parsedValue)
            ? Math.Max(1, parsedValue)
            : 1;
    }

    internal static int ResolveMaxConcurrentTenants(OverridableConfiguration? configuration, string hostPrefix)
    {
        int configuredValue = configuration?.GetInteger(StartupRebuildMaxConcurrentTenantsKey, hostPrefix) ?? 1;
        return Math.Max(1, configuredValue);
    }

    internal static bool ResolveStartupRebuildIndexAddBeginning(string? rawValue, bool defaultValue = true)
    {
        return bool.TryParse(rawValue, out bool parsedValue)
            ? parsedValue
            : defaultValue;
    }

    internal static bool ResolveStartupRebuildIndexAddBeginning(OverridableConfiguration? configuration, string hostPrefix, bool defaultValue = true)
    {
        return configuration?.GetBoolean(StartupRebuildIndexAddBeginningKey, hostPrefix) ?? defaultValue;
    }

    internal static List<string> NormalizeTenantListPreservingOrder(IEnumerable<string>? tenants)
    {
        var result = new List<string>();
        if (tenants == null)
        {
            return result;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string rawTenant in tenants)
        {
            if (string.IsNullOrWhiteSpace(rawTenant))
            {
                continue;
            }

            string tenant = rawTenant.Trim();
            if (seen.Add(tenant))
            {
                result.Add(tenant);
            }
        }

        return result;
    }

    internal static List<string> ResolveExcludedTenants(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return new List<string>();
        }

        return NormalizeTenantListPreservingOrder(rawValue.Split(','));
    }
}
