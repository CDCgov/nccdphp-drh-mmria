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
}
