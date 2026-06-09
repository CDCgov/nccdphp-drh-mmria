using System;
using System.Collections.Generic;
using mmria.common.couchdb;

namespace mmria.common.SharedLibraries.MMRIARebuild.Manager;

internal static class DbRebuildSettings
{
    internal const string StartupRebuildMaxConcurrentTenantsKey = "startup_rebuild_max_concurrent_tenants";
    internal const string StartupRebuildIndexAddBeginningKey = "startup_rebuild_index_add_beginning";
    internal const string StartupRebuildIndexRestoreModeKey = "startup_rebuild_index_restore_mode";
    internal const string StartupRebuildIndexWarmDelayMsKey = "startup_rebuild_index_warm_delay_ms";
    internal const string StartupRebuildIndexWarmPollDelayMsKey = "startup_rebuild_index_warm_poll_delay_ms";
    internal const string StartupRebuildIndexWarmTimeoutMsKey = "startup_rebuild_index_warm_timeout_ms";
    internal const string StartupRebuildIndexWarmMaxSurfacesPerRunKey = "startup_rebuild_index_warm_max_surfaces_per_run";
    internal const string StartupRebuildHeartbeatIntervalSecondsKey = "startup_rebuild_heartbeat_interval_seconds";
    internal const string StartupRebuildLeaseSecondsKey = "startup_rebuild_lease_seconds";
    internal const string StartupRebuildStaleAfterSecondsKey = "startup_rebuild_stale_after_seconds";
    internal const string StartupRebuildExcludeFromRebuildKey = "startup_rebuild_exclude_from_rebuild";
    internal const string IndexRestoreModeBeginningNoWait = "beginning_no_wait";
    internal const string IndexRestoreModeEndNoWait = "end_no_wait";
    internal const string IndexRestoreModeEndWaitInline = "end_wait_inline";
    internal const string IndexRestoreModeEndWaitStaggered = "end_wait_staggered";
    internal const string IndexRestoreModeDeferredBackground = "deferred_background";

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

    internal static bool RestoresIndexesAtBeginning(string indexRestoreMode)
    {
        return string.Equals(indexRestoreMode, IndexRestoreModeBeginningNoWait, StringComparison.OrdinalIgnoreCase);
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
