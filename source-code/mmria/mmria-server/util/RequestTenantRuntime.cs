using System;

namespace mmria.server.util;

public sealed class RequestTenantRuntime
{
    public RequestTenantRuntime(
        string? hostPrefix,
        mmria.common.couchdb.OverridableConfiguration? configuration,
        mmria.common.couchdb.ConfigurationSet? configurationSet,
        mmria.common.couchdb.DBConfigurationDetail? dbConfig,
        bool isTenantAvailable)
    {
        HostPrefix = hostPrefix;
        Configuration = configuration;
        ConfigurationSet = configurationSet;
        DbConfig = dbConfig;
        IsTenantAvailable = isTenantAvailable;
    }

    public string? HostPrefix { get; }
    public mmria.common.couchdb.OverridableConfiguration? Configuration { get; }
    public mmria.common.couchdb.ConfigurationSet? ConfigurationSet { get; }
    public mmria.common.couchdb.DBConfigurationDetail? DbConfig { get; }
    public bool IsTenantAvailable { get; }
    public string EffectiveHostPrefix => HostPrefix ?? string.Empty;

    public mmria.common.couchdb.OverridableConfiguration RequireConfiguration()
    {
        return Configuration ?? throw new InvalidOperationException("Tenant configuration is not available for the current request.");
    }

    public mmria.common.couchdb.ConfigurationSet RequireConfigurationSet()
    {
        return ConfigurationSet ?? throw new InvalidOperationException("Tenant configuration set is not available for the current request.");
    }

    public mmria.common.couchdb.DBConfigurationDetail RequireDbConfig()
    {
        return DbConfig ?? throw new InvalidOperationException("Tenant database configuration is not available for the current request.");
    }
}
