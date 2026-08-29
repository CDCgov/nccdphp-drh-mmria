using System.Security.Claims;

namespace mmria.server.util;

public static class AuthorizedWorkflowScopeHelper
{
    public static bool IsCdcAdmin(ClaimsPrincipal user) =>
        user?.IsInRole("cdc_admin") == true;

    public static string ResolveAuthorizedStateDatabase(
        ClaimsPrincipal user,
        string requestedStateDatabase,
        string hostPrefix,
        mmria.common.couchdb.ConfigurationSet configurationSet)
    {
        if (IsCdcAdmin(user) &&
            !string.IsNullOrWhiteSpace(requestedStateDatabase) &&
            configurationSet?.detail_list != null &&
            configurationSet.detail_list.ContainsKey(requestedStateDatabase))
        {
            return requestedStateDatabase;
        }

        if (!string.IsNullOrWhiteSpace(hostPrefix) &&
            configurationSet?.detail_list != null &&
            configurationSet.detail_list.ContainsKey(hostPrefix))
        {
            return hostPrefix;
        }

        return hostPrefix;
    }

    public static mmria.common.couchdb.DBConfigurationDetail ResolveAuthorizedDbConfig(
        ClaimsPrincipal user,
        string requestedStateDatabase,
        string hostPrefix,
        mmria.common.couchdb.DBConfigurationDetail currentDbConfig,
        mmria.common.couchdb.ConfigurationSet configurationSet)
    {
        if (!IsCdcAdmin(user))
        {
            return currentDbConfig;
        }

        var effectiveStateDatabase = ResolveAuthorizedStateDatabase(
            user,
            requestedStateDatabase,
            hostPrefix,
            configurationSet);

        if (!string.IsNullOrWhiteSpace(effectiveStateDatabase) &&
            configurationSet?.detail_list != null &&
            configurationSet.detail_list.TryGetValue(effectiveStateDatabase, out var dbInfo))
        {
            return dbInfo;
        }

        return currentDbConfig;
    }
}
