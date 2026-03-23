using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.server.model.actor;
using mmria.server.utils;
using Newtonsoft.Json.Linq;

namespace mmria.server.util;

public sealed class MultiTenantSetupPageModel
{
    public string current_host_prefix { get; set; }
    public string summary_host_prefix { get; set; }
    public string template_couchdb_url { get; set; }
    public List<string> loaded_tenants { get; set; } = new();
}

public sealed class MultiTenantSetupResult
{
    public bool success { get; set; }
    public int status_code { get; set; }
    public string tenant { get; set; }
    public string action { get; set; }
    public string message { get; set; }
    public string error { get; set; }
    public bool setup_completed { get; set; }
    public bool quartz_supervisor_created { get; set; }
    public bool rebuild_started { get; set; }
    public string rebuild_mode { get; set; }
    public List<string> loaded_tenants { get; set; } = new();
}

public sealed class MultiTenantSetupService
{
    private readonly IConfiguration _configuration;
    private readonly List<OverridableConfiguration> _overridableConfigSets;
    private readonly List<ConfigurationSet> _dbConfigSets;
    private readonly OverridableConfiguration _fallbackConfiguration;
    private readonly CouchDbHttpClient _couchDbHttpClient;
    private readonly ActorSystem _actorSystem;
    private readonly ILogger<MultiTenantSetupService> _logger;
    private readonly MultiTenantConfigurationLoader _configLoader;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tenantLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _activeRebuilds = new(StringComparer.OrdinalIgnoreCase);

    public MultiTenantSetupService
    (
        IConfiguration configuration,
        List<OverridableConfiguration> overridableConfigSets,
        List<ConfigurationSet> dbConfigSets,
        OverridableConfiguration fallbackConfiguration,
        CouchDbHttpClient couchDbHttpClient,
        ActorSystem actorSystem,
        ILogger<MultiTenantSetupService> logger
    )
    {
        _configuration = configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        _fallbackConfiguration = fallbackConfiguration;
        _couchDbHttpClient = couchDbHttpClient;
        _actorSystem = actorSystem;
        _logger = logger;
        _configLoader = new MultiTenantConfigurationLoader(configuration);
    }

    public MultiTenantSetupPageModel BuildPageModel(string currentHostPrefix)
    {
        return new MultiTenantSetupPageModel
        {
            current_host_prefix = currentHostPrefix,
            summary_host_prefix = GetSummaryHostPrefix(currentHostPrefix),
            template_couchdb_url = GetTemplateCouchDbUrl(),
            loaded_tenants = GetLoadedTenantNames()
        };
    }

    public bool IsTenantLoaded(string tenant)
    {
        string normalizedTenant = NormalizeTenant(tenant);
        if (string.IsNullOrWhiteSpace(normalizedTenant))
        {
            return false;
        }

        lock (_dbConfigSets)
        {
            return _dbConfigSets.Any(configSet => string.Equals(
                GetTenantName(configSet),
                normalizedTenant,
                StringComparison.OrdinalIgnoreCase));
        }
    }

    public async Task<MultiTenantSetupResult> LoadTenantAsync(string tenant)
    {
        string normalizedTenant = NormalizeTenant(tenant);
        if (string.IsNullOrWhiteSpace(normalizedTenant))
        {
            return CreateResult(StatusCodes.Status400BadRequest, normalizedTenant, "load", false, "Tenant is required.");
        }

        var tenantLock = _tenantLocks.GetOrAdd(normalizedTenant, _ => new SemaphoreSlim(1, 1));
        await tenantLock.WaitAsync();

        try
        {
            string templateCouchDbUrl = GetTemplateCouchDbUrl();
            string sharedConfigId = GetSharedConfigId();
            string timerUserName = GetTimerUserName();
            string timerPassword = GetTimerPassword();

            if (string.IsNullOrWhiteSpace(templateCouchDbUrl))
            {
                return CreateResult(
                    StatusCodes.Status400BadRequest,
                    normalizedTenant,
                    "load",
                    false,
                    "The multi-tenant CouchDB template URL is not configured.");
            }

            OverridableConfiguration loadedOverridableConfiguration = await _configLoader.LoadTenantOverridableConfigurationAsync(
                normalizedTenant,
                templateCouchDbUrl,
                timerUserName,
                timerPassword,
                sharedConfigId,
                _couchDbHttpClient);

            if (loadedOverridableConfiguration == null)
            {
                return CreateResult(
                    StatusCodes.Status404NotFound,
                    normalizedTenant,
                    "load",
                    false,
                    $"No shared configuration document was found for tenant '{normalizedTenant}'.");
            }

            ConfigurationSet loadedConfigurationSet = await _configLoader.LoadTenantConfigurationSetAsync(
                normalizedTenant,
                templateCouchDbUrl,
                timerUserName,
                timerPassword,
                _couchDbHttpClient);

            if (loadedConfigurationSet == null)
            {
                return CreateResult(
                    StatusCodes.Status404NotFound,
                    normalizedTenant,
                    "load",
                    false,
                    $"No configuration-set document was found for tenant '{normalizedTenant}'.");
            }

            if (!TryGetTenantDbConfig(loadedOverridableConfiguration, normalizedTenant, out var tenantDbConfig))
            {
                return CreateResult(
                    StatusCodes.Status400BadRequest,
                    normalizedTenant,
                    "load",
                    false,
                    $"The shared configuration document for tenant '{normalizedTenant}' does not contain a usable DB configuration.");
            }

            if (loadedConfigurationSet.detail_list == null || !loadedConfigurationSet.detail_list.ContainsKey(normalizedTenant))
            {
                return CreateResult(
                    StatusCodes.Status400BadRequest,
                    normalizedTenant,
                    "load",
                    false,
                    $"The configuration-set document for tenant '{normalizedTenant}' does not contain a matching tenant entry.");
            }

            loadedOverridableConfiguration._id = $"{normalizedTenant}_{sharedConfigId}";

            await new c_db_setup(
                _actorSystem,
                loadedOverridableConfiguration,
                normalizedTenant,
                _couchDbHttpClient).Setup(triggerStartupRebuild: false);

            bool alreadyLoaded = IsTenantLoaded(normalizedTenant);

            lock (_overridableConfigSets)
            {
                int existingIndex = FindOverridableConfigurationIndex(normalizedTenant);
                if (existingIndex >= 0)
                {
                    _overridableConfigSets[existingIndex] = loadedOverridableConfiguration;
                }
                else
                {
                    _overridableConfigSets.Add(loadedOverridableConfiguration);
                }
            }

            lock (_dbConfigSets)
            {
                int existingIndex = FindConfigurationSetIndex(normalizedTenant);
                if (existingIndex >= 0)
                {
                    _dbConfigSets[existingIndex] = loadedConfigurationSet;
                }
                else
                {
                    _dbConfigSets.Add(loadedConfigurationSet);
                }
            }

            UpdateRuntimeSharedKeys();
            bool quartzSupervisorCreated = EnsureQuartzSupervisor(normalizedTenant, loadedOverridableConfiguration, loadedConfigurationSet);
            string action = alreadyLoaded ? "reloaded" : "added";

            _logger.LogInformation("Tenant {Tenant} {Action} into the multi-tenant runtime.", normalizedTenant, action);

            return new MultiTenantSetupResult
            {
                success = true,
                status_code = StatusCodes.Status200OK,
                tenant = normalizedTenant,
                action = action,
                message = $"Tenant '{normalizedTenant}' was {action} successfully.",
                setup_completed = true,
                quartz_supervisor_created = quartzSupervisorCreated,
                loaded_tenants = GetLoadedTenantNames()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tenant {Tenant} into the multi-tenant runtime.", normalizedTenant);
            return CreateResult(
                StatusCodes.Status500InternalServerError,
                normalizedTenant,
                "load",
                false,
                $"Failed to load tenant '{normalizedTenant}'.",
                ex.Message);
        }
        finally
        {
            tenantLock.Release();
        }
    }

    public async Task<MultiTenantSetupResult> RebuildTenantAsync(string tenant, string mode)
    {
        string normalizedTenant = NormalizeTenant(tenant);
        if (string.IsNullOrWhiteSpace(normalizedTenant))
        {
            return CreateResult(StatusCodes.Status400BadRequest, normalizedTenant, "rebuild", false, "Tenant is required.");
        }

        string normalizedMode = string.IsNullOrWhiteSpace(mode) ? "fresh" : mode.Trim().ToLowerInvariant();
        if (normalizedMode != "fresh" && normalizedMode != "resume")
        {
            return CreateResult(
                StatusCodes.Status400BadRequest,
                normalizedTenant,
                "rebuild",
                false,
                "Rebuild mode must be either 'fresh' or 'resume'.");
        }

        if (!IsTenantLoaded(normalizedTenant))
        {
            var loadResult = await LoadTenantAsync(normalizedTenant);
            if (!loadResult.success)
            {
                loadResult.action = "rebuild";
                loadResult.rebuild_mode = normalizedMode;
                return loadResult;
            }
        }

        if (!_activeRebuilds.TryAdd(normalizedTenant, 0))
        {
            return CreateResult(
                StatusCodes.Status409Conflict,
                normalizedTenant,
                "rebuild",
                false,
                $"A rebuild is already running for tenant '{normalizedTenant}'.");
        }

        try
        {
            var tenantConfiguration = FindOverridableConfiguration(normalizedTenant);
            if (!TryGetTenantDbConfig(tenantConfiguration, normalizedTenant, out var tenantDbConfig))
            {
                _activeRebuilds.TryRemove(normalizedTenant, out _);
                return CreateResult(
                    StatusCodes.Status400BadRequest,
                    normalizedTenant,
                    "rebuild",
                    false,
                    $"The tenant '{normalizedTenant}' is loaded, but its DB configuration is not usable.");
            }

            string metadataVersion = tenantConfiguration.GetString("metadata_version", normalizedTenant);

            _ = Task.Run(async () =>
            {
                try
                {
                    if (normalizedMode == "fresh")
                    {
                        await DeleteStartupRebuildCheckpointAsync(tenantDbConfig);
                    }

                    var syncAll = new c_document_sync_all(
                        tenantDbConfig.url,
                        tenantDbConfig.user_name,
                        tenantDbConfig.user_value,
                        metadataVersion,
                        tenantDbConfig,
                        _couchDbHttpClient,
                        tenantConfiguration,
                        normalizedTenant);

                    await syncAll.executeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Manual rebuild failed for tenant {Tenant}.", normalizedTenant);
                }
                finally
                {
                    _activeRebuilds.TryRemove(normalizedTenant, out _);
                }
            });

            return new MultiTenantSetupResult
            {
                success = true,
                status_code = StatusCodes.Status202Accepted,
                tenant = normalizedTenant,
                action = "rebuild",
                message = $"Started a {normalizedMode} rebuild for tenant '{normalizedTenant}'.",
                rebuild_started = true,
                rebuild_mode = normalizedMode,
                loaded_tenants = GetLoadedTenantNames()
            };
        }
        catch (Exception ex)
        {
            _activeRebuilds.TryRemove(normalizedTenant, out _);
            _logger.LogError(ex, "Failed to start manual rebuild for tenant {Tenant}.", normalizedTenant);
            return CreateResult(
                StatusCodes.Status500InternalServerError,
                normalizedTenant,
                "rebuild",
                false,
                $"Failed to start a rebuild for tenant '{normalizedTenant}'.",
                ex.Message);
        }
    }

    private string GetTemplateCouchDbUrl()
    {
        return _configLoader.GetConfig("multi_tenant_shared_config_id_template_couchdb_url")
            ?? _configLoader.GetConfig("couchdb_url");
    }

    private string GetSharedConfigId()
    {
        return _configLoader.GetConfig("multi_tenant_shared_config_id")
            ?? _configLoader.GetConfig("shared_config_id")
            ?? "shared_config";
    }

    private string GetTimerUserName()
    {
        return _configLoader.GetConfig("timer_user_name");
    }

    private string GetTimerPassword()
    {
        return _configLoader.GetConfig("timer_password")
            ?? _configLoader.GetConfig("timer_value");
    }

    private string GetSummaryHostPrefix(string currentHostPrefix)
    {
        string configuredSummaryHost = _configLoader.GetConfig("multi_tenant_re_build_src");
        if (!string.IsNullOrWhiteSpace(configuredSummaryHost))
        {
            return configuredSummaryHost.Trim();
        }

        if (!string.IsNullOrWhiteSpace(currentHostPrefix))
        {
            return currentHostPrefix.Trim();
        }

        return GetLoadedTenantNames().FirstOrDefault() ?? "shared";
    }

    private List<string> GetLoadedTenantNames()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        lock (_dbConfigSets)
        {
            foreach (var configSet in _dbConfigSets)
            {
                string tenantName = GetTenantName(configSet);
                if (!string.IsNullOrWhiteSpace(tenantName))
                {
                    result.Add(tenantName);
                }
            }
        }

        if (result.Count == 0)
        {
            lock (_overridableConfigSets)
            {
                foreach (var config in _overridableConfigSets)
                {
                    if (config?.string_keys == null)
                    {
                        continue;
                    }

                    foreach (string key in config.string_keys.Keys)
                    {
                        if (!string.Equals(key, "shared", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(key);
                        }
                    }
                }
            }
        }

        return result
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void UpdateRuntimeSharedKeys()
    {
        string loadedTenantCsv = string.Join(",", GetLoadedTenantNames());
        string templateCouchDbUrl = GetTemplateCouchDbUrl() ?? string.Empty;
        string summaryHostPrefix = _configLoader.GetConfig("multi_tenant_re_build_src") ?? string.Empty;

        lock (_overridableConfigSets)
        {
            foreach (var config in _overridableConfigSets)
            {
                config.SetString("shared", "multi_tenant_jurisdictions", loadedTenantCsv);
                config.SetString("shared", "multi_tenant_shared_config_id_template_couchdb_url", templateCouchDbUrl);
                config.SetString("shared", "multi_tenant_re_build_src", summaryHostPrefix);
            }
        }
    }

    private int FindOverridableConfigurationIndex(string tenant)
    {
        for (int i = 0; i < _overridableConfigSets.Count; i++)
        {
            if (MatchesTenant(_overridableConfigSets[i], tenant))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindConfigurationSetIndex(string tenant)
    {
        for (int i = 0; i < _dbConfigSets.Count; i++)
        {
            if (string.Equals(GetTenantName(_dbConfigSets[i]), tenant, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool MatchesTenant(OverridableConfiguration configuration, string tenant)
    {
        if (configuration?.string_keys == null || string.IsNullOrWhiteSpace(tenant))
        {
            return false;
        }

        if (configuration.string_keys.ContainsKey(tenant))
        {
            return true;
        }

        try
        {
            return configuration.GetDBConfig(tenant) != null;
        }
        catch
        {
            return false;
        }
    }

    private OverridableConfiguration FindOverridableConfiguration(string tenant)
    {
        lock (_overridableConfigSets)
        {
            int index = FindOverridableConfigurationIndex(tenant);
            if (index >= 0)
            {
                return _overridableConfigSets[index];
            }
        }

        return MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _fallbackConfiguration, tenant);
    }

    private static string GetTenantName(ConfigurationSet configurationSet)
    {
        if (configurationSet?.detail_list == null)
        {
            return null;
        }

        foreach (var key in configurationSet.detail_list.Keys)
        {
            if (!string.Equals(key, "vital_import", StringComparison.OrdinalIgnoreCase))
            {
                return key?.Trim();
            }
        }

        return null;
    }

    private static bool TryGetTenantDbConfig(OverridableConfiguration configuration, string tenant, out DBConfigurationDetail dbConfig)
    {
        dbConfig = null;
        if (configuration == null || string.IsNullOrWhiteSpace(tenant))
        {
            return false;
        }

        try
        {
            dbConfig = configuration.GetDBConfig(tenant);
            return
                dbConfig != null &&
                !string.IsNullOrWhiteSpace(dbConfig.url) &&
                !string.IsNullOrWhiteSpace(dbConfig.user_name) &&
                !string.IsNullOrWhiteSpace(dbConfig.user_value);
        }
        catch
        {
            dbConfig = null;
            return false;
        }
    }

    private bool EnsureQuartzSupervisor(
        string tenant,
        OverridableConfiguration overridableConfiguration,
        ConfigurationSet configurationSet)
    {
        string actorName = $"QuartzSupervisor-{tenant}";

        try
        {
            var actorRef = _actorSystem.ActorOf(
                Props.Create<QuartzSupervisor>(
                    overridableConfiguration,
                    tenant,
                    configurationSet,
                    _couchDbHttpClient),
                actorName);

            actorRef.Tell("init");
            _logger.LogInformation("Created QuartzSupervisor actor for tenant {Tenant}.", tenant);
            return true;
        }
        catch (InvalidActorNameException)
        {
            _logger.LogInformation("QuartzSupervisor actor already exists for tenant {Tenant}.", tenant);
            return false;
        }
    }

    private async Task DeleteStartupRebuildCheckpointAsync(DBConfigurationDetail dbConfig)
    {
        string checkpointUrl = $"{dbConfig.url}/{dbConfig.prefix}db_rebuild/startup-rebuild-status";
        string checkpointResponse = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            checkpointUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value);

        if (string.IsNullOrWhiteSpace(checkpointResponse))
        {
            return;
        }

        var checkpointPayload = JObject.Parse(checkpointResponse);
        if (string.Equals(checkpointPayload.Value<string>("error"), "not_found", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string rev = checkpointPayload.Value<string>("_rev");
        if (string.IsNullOrWhiteSpace(rev))
        {
            return;
        }

        await _couchDbHttpClient.ExecuteAsync(
            "DELETE",
            $"{checkpointUrl}?rev={Uri.EscapeDataString(rev)}",
            null,
            dbConfig.user_name,
            dbConfig.user_value);
    }

    private static string NormalizeTenant(string tenant)
    {
        return string.IsNullOrWhiteSpace(tenant) ? null : tenant.Trim();
    }

    private static MultiTenantSetupResult CreateResult(
        int statusCode,
        string tenant,
        string action,
        bool success,
        string message,
        string error = null)
    {
        return new MultiTenantSetupResult
        {
            success = success,
            status_code = statusCode,
            tenant = tenant,
            action = action,
            message = message,
            error = error
        };
    }
}
