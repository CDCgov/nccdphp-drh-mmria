using System;
using System.Collections.Generic;
using mmria.common.couchdb;

namespace mmria.server.util;

internal static class DbRebuildSettings
{
    internal const string StartupRebuildEnabledKey = "multi_tenant_db_rebuild";
    internal const string StartupRebuildMaxConcurrentTenantsKey = "startup_rebuild_max_concurrent_tenants";
    internal const string StartupRebuildIndexAddBeginningKey = "startup_rebuild_index_add_beginning";
    internal const string StartupRebuildIndexRestoreModeKey = "startup_rebuild_index_restore_mode";
    internal const string StartupRebuildTenantsKey = "multi_tenant_jurisdictions_rebuild";
    internal const string MultiTenantJurisdictionsKey = "multi_tenant_jurisdictions";
    internal const string IndexRestoreModeBeginningNoWait = "beginning_no_wait";
    internal const string IndexRestoreModeEndNoWait = "end_no_wait";
    internal const string IndexRestoreModeEndWaitInline = "end_wait_inline";
    internal const string IndexRestoreModeEndWaitStaggered = "end_wait_staggered";
    internal const string IndexRestoreModeDeferredBackground = "deferred_background";

    internal static bool ResolveStartupRebuildEnabled(string? rawValue, bool defaultValue = true)
    {
        return bool.TryParse(rawValue, out bool parsedValue)
            ? parsedValue
            : defaultValue;
    }

    internal static bool ResolveStartupRebuildEnabled(OverridableConfiguration? configuration, string hostPrefix, bool defaultValue = true)
    {
        return configuration?.GetBoolean(StartupRebuildEnabledKey, hostPrefix) ?? defaultValue;
    }

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

    internal static bool ResolveStartupRebuildIndexAddBeginning(OverridableConfiguration? configuration, string hostPrefix, bool defaultValue = true)
    {
        return configuration?.GetBoolean(StartupRebuildIndexAddBeginningKey, hostPrefix) ?? defaultValue;
    }

    internal static string ResolveStartupRebuildIndexRestoreMode(string? rawValue, bool startupRebuildIndexAddBeginning)
    {
        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            string normalizedValue = rawValue.Trim().ToLowerInvariant().Replace("-", "_", StringComparison.Ordinal);
            switch (normalizedValue)
            {
                case "beginning":
                case IndexRestoreModeBeginningNoWait:
                    return IndexRestoreModeBeginningNoWait;
                case "end_no_wait":
                    return IndexRestoreModeEndNoWait;
                case "end":
                case "end_wait":
                case IndexRestoreModeEndWaitInline:
                    return IndexRestoreModeEndWaitInline;
                case IndexRestoreModeEndWaitStaggered:
                    return IndexRestoreModeEndWaitStaggered;
                case IndexRestoreModeDeferredBackground:
                    return IndexRestoreModeDeferredBackground;
            }
        }

        return startupRebuildIndexAddBeginning
            ? IndexRestoreModeBeginningNoWait
            : IndexRestoreModeEndWaitInline;
    }

    internal static string ResolveStartupRebuildIndexRestoreMode(OverridableConfiguration? configuration, string hostPrefix, bool startupRebuildIndexAddBeginning)
    {
        return ResolveStartupRebuildIndexRestoreMode(
            configuration?.GetString(StartupRebuildIndexRestoreModeKey, hostPrefix),
            startupRebuildIndexAddBeginning);
    }

    internal static bool WaitsForIndexWarmup(string indexRestoreMode)
    {
        return string.Equals(indexRestoreMode, IndexRestoreModeEndWaitInline, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(indexRestoreMode, IndexRestoreModeEndWaitStaggered, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool DefersIndexWarmup(string indexRestoreMode)
    {
        return string.Equals(indexRestoreMode, IndexRestoreModeBeginningNoWait, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(indexRestoreMode, IndexRestoreModeEndNoWait, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(indexRestoreMode, IndexRestoreModeDeferredBackground, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool WarmsIndexesAfterDocumentWrites(string indexRestoreMode)
    {
        return WaitsForIndexWarmup(indexRestoreMode) || DefersIndexWarmup(indexRestoreMode);
    }

    internal static bool StaggersIndexWarmup(string indexRestoreMode)
    {
        return string.Equals(indexRestoreMode, IndexRestoreModeEndWaitStaggered, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool DelaysIndexWarmup(string indexRestoreMode)
    {
        return StaggersIndexWarmup(indexRestoreMode) || DefersIndexWarmup(indexRestoreMode);
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

    internal static string ResolveStartupSummaryHostPrefix(string? configuredSummaryHost, IEnumerable<string>? configuredTenants, string? fallbackHostPrefix = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredSummaryHost))
        {
            return configuredSummaryHost.Trim();
        }

        var normalizedTenants = NormalizeTenantListPreservingOrder(configuredTenants);
        if (normalizedTenants.Count > 0)
        {
            return normalizedTenants[0];
        }

        return string.IsNullOrWhiteSpace(fallbackHostPrefix)
            ? "shared"
            : fallbackHostPrefix.Trim();
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
