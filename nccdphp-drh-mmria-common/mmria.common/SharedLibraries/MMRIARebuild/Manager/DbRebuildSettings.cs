using System;
using System.Collections.Generic;
using mmria.common.couchdb;

namespace mmria.common.SharedLibraries.MMRIARebuild.Manager;

internal static class DbRebuildSettings
{
    internal const string StartupRebuildMaxConcurrentTenantsKey = "startup_rebuild_max_concurrent_tenants";
    internal const string StartupRebuildTenantsKey = "multi_tenant_jurisdictions_rebuild";
    internal const string MultiTenantJurisdictionsKey = "multi_tenant_jurisdictions";

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

    internal static List<string> ResolveStartupRebuildTenants(string? rebuildTenantsCsv, string? allTenantsCsv)
    {
        var requestedTenants = ParseTenantListPreservingOrder(rebuildTenantsCsv);
        if (requestedTenants.Count > 0)
        {
            return requestedTenants;
        }

        return ParseTenantListPreservingOrder(allTenantsCsv);
    }

    internal static List<string> ResolveStartupRebuildTenants(OverridableConfiguration? configuration, string hostPrefix)
    {
        return ResolveStartupRebuildTenants(
            configuration?.GetString(StartupRebuildTenantsKey, hostPrefix),
            configuration?.GetString(MultiTenantJurisdictionsKey, hostPrefix));
    }

    internal static string ToCsv(IEnumerable<string>? tenants)
    {
        return string.Join(",", tenants ?? Array.Empty<string>());
    }

    internal static List<string> ParseTenantListPreservingOrder(string? csv)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(csv))
        {
            return result;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string rawTenant in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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


