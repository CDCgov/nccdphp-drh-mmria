using System;

namespace mmria.server.util;

public static class SessionTimeoutHelper
{
    public static int GetSessionIdleTimeoutMinutes(
        mmria.common.couchdb.OverridableConfiguration? tenantConfiguration,
        mmria.common.couchdb.OverridableConfiguration fallbackConfiguration,
        string? hostPrefix,
        int defaultMinutes = 30)
    {
        var normalizedHostPrefix = string.IsNullOrWhiteSpace(hostPrefix)
            ? "shared"
            : hostPrefix.Trim();

        var tenantValue = TryGetTimeout(tenantConfiguration, normalizedHostPrefix);
        if (tenantValue.HasValue)
        {
            return tenantValue.Value;
        }

        var fallbackValue = TryGetTimeout(fallbackConfiguration, normalizedHostPrefix);
        if (fallbackValue.HasValue)
        {
            return fallbackValue.Value;
        }

        return defaultMinutes;
    }

    private static int? TryGetTimeout(
        mmria.common.couchdb.OverridableConfiguration? configuration,
        string normalizedHostPrefix)
    {
        if (configuration == null)
        {
            return null;
        }

        try
        {
            return configuration.GetInteger("session_idle_timeout_minutes", normalizedHostPrefix);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
