using System;
using System.Collections.Generic;
using mmria.common.couchdb;

namespace mmria.common.SharedLibraries.MMRIARebuild.Manager;

internal static class DbRebuildSettings
{
    internal const string StartupRebuildMaxConcurrentTenantsKey = "startup_rebuild_max_concurrent_tenants";

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
}
