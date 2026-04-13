using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using mmria.common.SharedLibraries.MMRIARebuild.DAL;
using mmria.common.SharedLibraries.MMRIARebuild.Model;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.MMRIARebuild.Manager;

public sealed class MMRIARebuildManager
{
    private readonly MMRIARebuildDAL _mmriaRebuildDal;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly List<mmria.common.couchdb.ConfigurationSet> _configurationSets;
    private readonly mmria.common.couchdb.MultiTenantConfigurationLoader _configLoader;

    public MMRIARebuildManager(
        MMRIARebuildDAL mmriaRebuildDal,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        IConfiguration configuration,
        mmria.common.couchdb.ConfigurationSet configurationSet)
        : this(
            mmriaRebuildDal,
            couchDbHttpClient,
            configuration,
            configurationSet == null
                ? new List<mmria.common.couchdb.ConfigurationSet>()
                : new List<mmria.common.couchdb.ConfigurationSet> { configurationSet })
    {
    }

    public MMRIARebuildManager(
        MMRIARebuildDAL mmriaRebuildDal,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        IConfiguration configuration,
        List<mmria.common.couchdb.ConfigurationSet> configurationSets)
    {
        _mmriaRebuildDal = mmriaRebuildDal ?? throw new ArgumentNullException(nameof(mmriaRebuildDal));
        _couchDbHttpClient = couchDbHttpClient ?? throw new ArgumentNullException(nameof(couchDbHttpClient));
        ArgumentNullException.ThrowIfNull(configuration);
        _configurationSets = configurationSets ?? throw new ArgumentNullException(nameof(configurationSets));
        _configLoader = new mmria.common.couchdb.MultiTenantConfigurationLoader(configuration);
    }

    public static string BuildServiceUrl(string vitalsUrl)
    {
        return string.IsNullOrWhiteSpace(vitalsUrl)
            ? null
            : vitalsUrl.Replace("Message/IJESet", "MMRIARebuild", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<MMRIARebuildResponse> QueueRebuildOnServiceAsync(
        MMRIARebuildRequest request,
        string serviceUrl,
        string vitalServiceKey)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        string objectString = Newtonsoft.Json.JsonConvert.SerializeObject(request);
        return await _mmriaRebuildDal.PostRebuildToServiceAsync(serviceUrl, objectString, vitalServiceKey);
    }

    public async Task<JObject> TryGetStartupRunSummaryDocumentAsync(mmria.common.couchdb.DBConfigurationDetail dbConfig)
    {
        return await _mmriaRebuildDal.TryGetStartupRunSummaryDocumentAsync(dbConfig);
    }

    public int ResolveMaxConcurrentTenants()
    {
        return DbRebuildSettings.ResolveMaxConcurrentTenants(
            _configLoader.GetConfig(DbRebuildSettings.StartupRebuildMaxConcurrentTenantsKey));
    }

    public async Task<MMRIARebuildResponse> EnqueueInProcessRebuildAsync(MMRIARebuildRequest request)
    {
        string normalizedTenant = NormalizeTenant(request?.tenant);
        string normalizedSource = NormalizeSource(request?.source);
        List<string> normalizedConfiguredTenants = DbRebuildSettings.NormalizeTenantListPreservingOrder(request?.configured_tenants);
        string normalizedSummaryHostPrefix = NormalizeTenant(request?.summary_host_prefix);

        if (string.IsNullOrWhiteSpace(normalizedTenant))
        {
            return new MMRIARebuildResponse
            {
                success = false,
                status_code = 400,
                tenant = normalizedTenant,
                source = normalizedSource,
                message = "Tenant is required.",
                error = "tenant is required"
            };
        }

        if (string.Equals(normalizedSource, "startup", StringComparison.OrdinalIgnoreCase))
        {
            if (normalizedConfiguredTenants.Count == 0)
            {
                return new MMRIARebuildResponse
                {
                    success = false,
                    status_code = 400,
                    tenant = normalizedTenant,
                    source = normalizedSource,
                    message = "Startup rebuild requests must include configured_tenants.",
                    error = "configured_tenants is required when source is startup"
                };
            }

            if (string.IsNullOrWhiteSpace(normalizedSummaryHostPrefix))
            {
                return new MMRIARebuildResponse
                {
                    success = false,
                    status_code = 400,
                    tenant = normalizedTenant,
                    source = normalizedSource,
                    message = "Startup rebuild requests must include summary_host_prefix.",
                    error = "summary_host_prefix is required when source is startup"
                };
            }
        }

        if (!TenantRebuildCoordinator.TryAcquire(
            normalizedTenant,
            normalizedSource,
            "legacy",
            "queued",
            out var lease,
            out var existingReservation))
        {
            return new MMRIARebuildResponse
            {
                success = false,
                status_code = 409,
                tenant = normalizedTenant,
                source = normalizedSource,
                message = $"A rebuild is already running or queued for tenant '{normalizedTenant}'.",
                error = existingReservation == null
                    ? "rebuild already queued"
                    : $"Existing rebuild source='{existingReservation.source}', status='{existingReservation.status}'."
            };
        }

        try
        {
            var runtime = await ResolveRuntimeAsync(normalizedTenant);
            if (runtime == null || runtime.db_config == null || runtime.configuration == null)
            {
                lease.Dispose();
                return new MMRIARebuildResponse
                {
                    success = false,
                    status_code = 404,
                    tenant = normalizedTenant,
                    source = normalizedSource,
                    message = $"Unable to resolve rebuild configuration for tenant '{normalizedTenant}'.",
                    error = "tenant configuration could not be resolved"
                };
            }

            var worker = new MMRIARebuildWorker(
                runtime.db_config.url,
                runtime.db_config.user_name,
                runtime.db_config.user_value,
                runtime.metadata_version,
                runtime.db_config,
                _couchDbHttpClient,
                runtime.configuration,
                normalizedTenant,
                lease,
                normalizedSource,
                normalizedConfiguredTenants,
                normalizedSummaryHostPrefix);

            await worker.PersistQueuedSummaryAsync();

            _ = Task.Run(async () =>
            {
                try
                {
                    await worker.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"MMRIARebuildManager.EnqueueInProcessRebuildAsync background failure for '{normalizedTenant}': {ex}");
                    lease.Dispose();
                }
            });

            return new MMRIARebuildResponse
            {
                success = true,
                status_code = 202,
                tenant = normalizedTenant,
                source = normalizedSource,
                message = $"Started a fresh rebuild for tenant '{normalizedTenant}'.",
                rebuild_started = true
            };
        }
        catch (Exception ex)
        {
            lease.Dispose();
            return new MMRIARebuildResponse
            {
                success = false,
                status_code = 500,
                tenant = normalizedTenant,
                source = normalizedSource,
                message = $"Failed to start a rebuild for tenant '{normalizedTenant}'.",
                error = ex.Message
            };
        }
    }

    private async Task<RuntimeTenantRebuildContext> ResolveRuntimeAsync(string tenant)
    {
        tenant = NormalizeTenant(tenant);
        if (string.IsNullOrWhiteSpace(tenant))
        {
            return null;
        }

        var dbConfig = ResolveDbConfigFromDetailList(tenant);
        mmria.common.couchdb.OverridableConfiguration overrideConfiguration = null;

        if (dbConfig == null)
        {
            var fallback = await LoadTenantFromSharedConfigAsync(tenant);
            if (fallback == null)
            {
                return null;
            }

            dbConfig = fallback.db_config;
            overrideConfiguration = fallback.configuration;
        }
        else
        {
            overrideConfiguration = await TryLoadSharedOverridableConfigurationAsync(tenant)
                ?? BuildRuntimeConfiguration(tenant, dbConfig);
        }

        EnsureRuntimeConfigurationShape(overrideConfiguration);
        ApplyRuntimeOverrides(overrideConfiguration, tenant, dbConfig);

        string metadataVersion = overrideConfiguration?.GetString("metadata_version", tenant)
            ?? _configLoader.GetConfig("metadata_version")
            ?? ResolveMetadataVersion()
            ?? string.Empty;

        return new RuntimeTenantRebuildContext
        {
            tenant = tenant,
            db_config = dbConfig,
            configuration = overrideConfiguration,
            metadata_version = metadataVersion
        };
    }

    private async Task<mmria.common.couchdb.OverridableConfiguration?> TryLoadSharedOverridableConfigurationAsync(string tenant)
    {
        string templateUrl = ResolveTenantTemplateUrl();
        string sharedConfigId = _configLoader.GetConfig("multi_tenant_shared_config_id")
            ?? _configLoader.GetConfig("shared_config_id")
            ?? "shared_config";
        string timerUserName = _configLoader.GetConfig("timer_user_name");
        string timerPassword = _configLoader.GetConfig("timer_password") ?? _configLoader.GetConfig("timer_value");

        if (string.IsNullOrWhiteSpace(templateUrl) ||
            string.IsNullOrWhiteSpace(timerUserName) ||
            string.IsNullOrWhiteSpace(timerPassword))
        {
            return null;
        }

        return await _configLoader.LoadTenantOverridableConfigurationAsync(
            tenant,
            templateUrl,
            timerUserName,
            timerPassword,
            sharedConfigId,
            _couchDbHttpClient);
    }

    private void ApplyRuntimeOverrides(
        mmria.common.couchdb.OverridableConfiguration configuration,
        string tenant,
        mmria.common.couchdb.DBConfigurationDetail dbConfig)
    {
        configuration.SetString("shared", "multi_tenant_shared_config_id_template_couchdb_url", ResolveTenantTemplateUrl());
        configuration.SetInteger("shared", DbRebuildSettings.StartupRebuildMaxConcurrentTenantsKey, ResolveMaxConcurrentTenants());
        configuration.SetString(tenant, "couchdb_url", dbConfig.url);
        configuration.SetString(tenant, "db_prefix", dbConfig.prefix ?? string.Empty);
        configuration.SetString(tenant, "timer_user_name", dbConfig.user_name);
        configuration.SetString(tenant, "timer_value", dbConfig.user_value);
        configuration.SetString(tenant, "metadata_version",
            _configLoader.GetConfig("metadata_version")
            ?? ResolveMetadataVersion()
            ?? string.Empty);

        MirrorIntegerIfPresent(configuration, "startup_rebuild_page_size");
        MirrorIntegerIfPresent(configuration, "startup_rebuild_batch_delay_ms");
        MirrorIntegerIfPresent(configuration, "startup_rebuild_bulk_write_retry_count");
        MirrorIntegerIfPresent(configuration, "startup_rebuild_bulk_write_retry_delay_ms");
        MirrorIntegerIfPresent(configuration, "startup_rebuild_progress_persist_every_batches");
    }

    private void MirrorIntegerIfPresent(mmria.common.couchdb.OverridableConfiguration configuration, string key)
    {
        string rawValue = _configLoader.GetConfig(key);
        if (int.TryParse(rawValue, out int parsedValue))
        {
            configuration.SetInteger("shared", key, parsedValue);
        }
    }

    private static void EnsureRuntimeConfigurationShape(mmria.common.couchdb.OverridableConfiguration configuration)
    {
        configuration.boolean_keys ??= new Dictionary<string, Dictionary<string, bool>>(StringComparer.OrdinalIgnoreCase);
        configuration.string_keys ??= new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        configuration.integer_keys ??= new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        if (!configuration.boolean_keys.ContainsKey("shared"))
        {
            configuration.boolean_keys["shared"] = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }

        if (!configuration.string_keys.ContainsKey("shared"))
        {
            configuration.string_keys["shared"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        if (!configuration.integer_keys.ContainsKey("shared"))
        {
            configuration.integer_keys["shared"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private mmria.common.couchdb.DBConfigurationDetail ResolveDbConfigFromDetailList(string tenant)
    {
        foreach (var configurationSet in _configurationSets)
        {
            if (configurationSet?.detail_list == null)
            {
                continue;
            }

            if (string.Equals(configurationSet._id, tenant, StringComparison.OrdinalIgnoreCase) &&
                configurationSet.detail_list.TryGetValue(tenant, out var exactMatch) &&
                exactMatch != null)
            {
                return CloneDbConfig(exactMatch);
            }

            if (configurationSet.detail_list.TryGetValue(tenant, out var keyedMatch) &&
                keyedMatch != null)
            {
                return CloneDbConfig(keyedMatch);
            }
        }

        return null;
    }

    private async Task<RuntimeTenantRebuildContext> LoadTenantFromSharedConfigAsync(string tenant)
    {
        string templateUrl = ResolveTenantTemplateUrl();
        string sharedConfigId = _configLoader.GetConfig("multi_tenant_shared_config_id")
            ?? _configLoader.GetConfig("shared_config_id")
            ?? "shared_config";
        string timerUserName = _configLoader.GetConfig("timer_user_name");
        string timerPassword = _configLoader.GetConfig("timer_password") ?? _configLoader.GetConfig("timer_value");

        if (string.IsNullOrWhiteSpace(templateUrl) ||
            string.IsNullOrWhiteSpace(timerUserName) ||
            string.IsNullOrWhiteSpace(timerPassword))
        {
            return null;
        }

        var overrideConfiguration = await _configLoader.LoadTenantOverridableConfigurationAsync(
            tenant,
            templateUrl,
            timerUserName,
            timerPassword,
            sharedConfigId,
            _couchDbHttpClient);

        var configurationSet = await _configLoader.LoadTenantConfigurationSetAsync(
            tenant,
            templateUrl,
            timerUserName,
            timerPassword,
            _couchDbHttpClient);

        if (overrideConfiguration == null ||
            configurationSet?.detail_list == null ||
            !configurationSet.detail_list.TryGetValue(tenant, out var dbConfig))
        {
            return null;
        }

        return new RuntimeTenantRebuildContext
        {
            tenant = tenant,
            db_config = dbConfig,
            configuration = overrideConfiguration,
            metadata_version = overrideConfiguration.GetString("metadata_version", tenant)
                ?? _configLoader.GetConfig("metadata_version")
                ?? string.Empty
        };
    }

    private mmria.common.couchdb.OverridableConfiguration BuildRuntimeConfiguration(
        string tenant,
        mmria.common.couchdb.DBConfigurationDetail dbConfig)
    {
        var configuration = new mmria.common.couchdb.OverridableConfiguration
        {
            _id = $"{tenant}_runtime_rebuild"
        };
        EnsureRuntimeConfigurationShape(configuration);
        return configuration;
    }

    private string ResolveTenantTemplateUrl()
    {
        return _configLoader.GetConfig("multi_tenant_shared_config_id_template_couchdb_url")
            ?? _configLoader.GetConfig("multi_tenant_template_couchdb_url")
            ?? _configLoader.GetConfig("couchdb_url");
    }

    private string? ResolveMetadataVersion()
    {
        foreach (var configurationSet in _configurationSets)
        {
            if (configurationSet?.name_value == null)
            {
                continue;
            }

            if (configurationSet.name_value.TryGetValue("metadata_version", out var metadataVersion) &&
                !string.IsNullOrWhiteSpace(metadataVersion))
            {
                return metadataVersion;
            }
        }

        return null;
    }

    private static mmria.common.couchdb.DBConfigurationDetail CloneDbConfig(mmria.common.couchdb.DBConfigurationDetail dbConfig)
    {
        return new mmria.common.couchdb.DBConfigurationDetail
        {
            url = dbConfig.url,
            prefix = dbConfig.prefix ?? string.Empty,
            user_name = dbConfig.user_name,
            user_value = dbConfig.user_value
        };
    }

    private static string NormalizeTenant(string tenant)
    {
        return string.IsNullOrWhiteSpace(tenant) ? null : tenant.Trim();
    }

    private static string NormalizeSource(string source)
    {
        return string.IsNullOrWhiteSpace(source) ? "manual" : source.Trim().ToLowerInvariant();
    }

    private sealed class RuntimeTenantRebuildContext
    {
        public string tenant { get; init; }
        public mmria.common.couchdb.DBConfigurationDetail db_config { get; init; }
        public mmria.common.couchdb.OverridableConfiguration configuration { get; init; }
        public string metadata_version { get; init; }
    }
}
