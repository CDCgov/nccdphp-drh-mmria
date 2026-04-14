using System;

namespace mmria.common.SharedLibraries.OfflineCase.Model;

public static class OfflineAuthSessionDefaults
{
    public static readonly TimeSpan ServerSessionLifetime = TimeSpan.FromDays(30);

    public static DateTime GetExpirationDateTime(DateTime currentDateTime)
    {
        return currentDateTime.Add(ServerSessionLifetime);
    }
}
