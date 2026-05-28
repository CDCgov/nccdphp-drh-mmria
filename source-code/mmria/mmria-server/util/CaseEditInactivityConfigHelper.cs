using System;

namespace mmria.server.util;

public static class CaseEditInactivityConfigHelper
{
    public static (int LockMinutes, int WarningMinutes) GetEffectiveMinutes(
        int configuredLockMinutes,
        int configuredWarningMinutes,
        int sessionIdleTimeoutMinutes)
    {
        var normalizedLockMinutes = Math.Max(0, configuredLockMinutes);
        var normalizedWarningMinutes = Math.Max(0, configuredWarningMinutes);
        var normalizedSessionIdleTimeoutMinutes = Math.Max(0, sessionIdleTimeoutMinutes);

        var effectiveLockMinutes = Math.Min(
            normalizedLockMinutes,
            Math.Max(0, normalizedSessionIdleTimeoutMinutes - 1));

        var effectiveWarningMinutes = Math.Min(
            normalizedWarningMinutes,
            Math.Max(0, effectiveLockMinutes - 1));

        return (effectiveLockMinutes, effectiveWarningMinutes);
    }
}
