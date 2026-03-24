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
    private const string StartupRebuildSecurityPayload =
        "{\"admins\":{\"names\":[],\"roles\":[\"form_designer\"]},\"members\":{\"names\":[],\"roles\":[\"abstractor\",\"data_analyst\",\"timer\"]}}";
    private static readonly string[] RuntimeSharedIntegerKeys =
    [
        "startup_rebuild_page_size",
        "startup_rebuild_resumed_page_size",
        "startup_rebuild_max_parallelism",
        "startup_rebuild_bulk_doc_chunk_size",
        "startup_rebuild_batch_delay_ms",
        "startup_rebuild_bulk_write_retry_count",
        "startup_rebuild_bulk_write_retry_delay_ms",
        "startup_rebuild_progress_persist_every_batches"
    ];

    private readonly IConfiguration _configuration;
    private readonly List<OverridableConfiguration> _overridableConfigSets;
    private readonly List<ConfigurationSet> _dbConfigSets;
    private readonly OverridableConfiguration _fallbackConfiguration;
    private readonly CouchDbHttpClient _couchDbHttpClient;
    private readonly ActorSystem _actorSystem;
    private readonly ILogger<MultiTenantSetupService> _logger;
    private readonly MultiTenantConfigurationLoader _configLoader;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tenantLocks = new(StringComparer.OrdinalIgnoreCase);

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

        if (!IsMultiTenantMode())
        {
            return string.Equals(
                GetSingleTenantName(),
                normalizedTenant,
                StringComparison.OrdinalIgnoreCase);
        }

        lock (_dbConfigSets)
        {
            return _dbConfigSets.Any(configSet => string.Equals(
                configSet?._id,
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
                int existingIndex = FindExactOverridableConfigurationIndex(normalizedTenant);
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
                int existingIndex = FindExactConfigurationSetIndex(normalizedTenant);
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
            await UpsertStartupRunSummaryTenantAsync(
                normalizedTenant,
                normalizedTenant,
                loadedOverridableConfiguration.GetString("metadata_version", normalizedTenant),
                "pending",
                preserveExistingStatus: true);
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

        if (!TenantRebuildCoordinator.TryAcquire(
            normalizedTenant,
            "manual",
            normalizedMode,
            "queued",
            out var tenantRebuildLease,
            out var existingReservation))
        {
            return CreateResult(
                StatusCodes.Status409Conflict,
                normalizedTenant,
                "rebuild",
                false,
                $"A rebuild is already running or queued for tenant '{normalizedTenant}' " +
                $"from '{existingReservation?.source ?? "unknown"}' with status '{existingReservation?.status ?? "unknown"}'.");
        }

        try
        {
            var tenantConfiguration = FindOverridableConfiguration(normalizedTenant);
            if (!TryGetTenantDbConfig(tenantConfiguration, normalizedTenant, out var tenantDbConfig))
            {
                tenantRebuildLease.Dispose();
                return CreateResult(
                    StatusCodes.Status400BadRequest,
                    normalizedTenant,
                    "rebuild",
                    false,
                    $"The tenant '{normalizedTenant}' is loaded, but its DB configuration is not usable.");
            }

            string metadataVersion = tenantConfiguration.GetString("metadata_version", normalizedTenant);
            await UpsertStartupRunSummaryTenantAsync(
                normalizedTenant,
                normalizedTenant,
                metadataVersion,
                "queued");

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
                        normalizedTenant,
                        tenantRebuildLease,
                        "manual",
                        normalizedMode);

                    await syncAll.executeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Manual rebuild failed for tenant {Tenant}.", normalizedTenant);
                    try
                    {
                        await UpsertStartupRunSummaryTenantAsync(
                            normalizedTenant,
                            normalizedTenant,
                            metadataVersion,
                            "paused",
                            lastError: ex.ToString());
                    }
                    catch (Exception summaryEx)
                    {
                        _logger.LogWarning(summaryEx, "Unable to persist a paused summary state for tenant {Tenant}.", normalizedTenant);
                    }
                }
                finally
                {
                    tenantRebuildLease.Dispose();
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
            tenantRebuildLease.Dispose();
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

    public async Task<JObject> GetStartupRunSummaryAsync(string currentHostPrefix)
    {
        string effectiveHostPrefix = NormalizeTenant(currentHostPrefix)
            ?? GetLoadedTenantNames().FirstOrDefault()
            ?? "shared";

        var reservations = TenantRebuildCoordinator.GetReservations();
        string summaryHostPrefix = GetSummaryHostPrefix(effectiveHostPrefix);
        JObject summary = null;

        if (reservations.Count > 0 &&
            StartupRunSummaryCache.TryGet(summaryHostPrefix, out var cachedSummary))
        {
            summary = cachedSummary;
        }

        if (summary == null &&
            TryGetSummaryDbConfig(effectiveHostPrefix, out var summaryDbConfig))
        {
            summary = await TryGetStartupRunSummaryDocumentAsync(summaryDbConfig);
            if (summary != null)
            {
                StartupRunSummaryCache.Set(summaryHostPrefix, summary);
            }
        }

        summary ??= CreateStartupRunSummaryDocument(
            effectiveHostPrefix,
            metadataVersion: null,
            configuredTenants: GetLoadedTenantNames());

        var mergedTenants = MergeTenantNames(
            GetLoadedTenantNames(),
            GetConfiguredTenants(summary));

        summary["summary_host_prefix"] = summaryHostPrefix;
        summary["configured_tenants"] = new JArray(mergedTenants);
        EnsureSummaryTenantEntries(summary, mergedTenants);

        var tenantStatuses = GetTenantStatuses(summary);
        foreach (var reservation in reservations)
        {
            if (tenantStatuses[reservation.tenant] is not JObject tenantStatus)
            {
                tenantStatus = new JObject
                {
                    ["host_prefix"] = reservation.tenant
                };
                tenantStatuses[reservation.tenant] = tenantStatus;
            }

            if (string.IsNullOrWhiteSpace(tenantStatus.Value<string>("status")) ||
                string.Equals(tenantStatus.Value<string>("status"), "pending", StringComparison.OrdinalIgnoreCase))
            {
                tenantStatus["status"] = reservation.status;
            }
        }

        UpdateSummaryTotals(summary);
        summary["loaded_tenants"] = new JArray(GetLoadedTenantNames());
        summary["active_rebuilds"] = JArray.FromObject(reservations);
        return summary;
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

        if (!IsMultiTenantMode())
        {
            string singleTenantName = GetSingleTenantName();
            if (!string.IsNullOrWhiteSpace(singleTenantName))
            {
                result.Add(singleTenantName);
            }

            return result
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        lock (_dbConfigSets)
        {
            foreach (var configSet in _dbConfigSets)
            {
                if (!string.IsNullOrWhiteSpace(configSet?._id))
                {
                    result.Add(configSet._id.Trim());
                }
            }
        }

        if (result.Count == 0)
        {
            lock (_overridableConfigSets)
            {
                foreach (var config in _overridableConfigSets)
                {
                    string tenantName = GetTenantNameFromOverridableConfiguration(config);
                    if (!string.IsNullOrWhiteSpace(tenantName))
                    {
                        result.Add(tenantName);
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
        string startupRebuildMode = _configLoader.GetConfig("startup_rebuild_mode") ?? string.Empty;
        string isMultiTenantMode = IsMultiTenantMode() ? "true" : "false";

        lock (_overridableConfigSets)
        {
            foreach (var config in _overridableConfigSets)
            {
                config.SetString("shared", "multi_tenant_jurisdictions", loadedTenantCsv);
                config.SetString("shared", "multi_tenant_shared_config_id_template_couchdb_url", templateCouchDbUrl);
                config.SetString("shared", "multi_tenant_re_build_src", summaryHostPrefix);
                config.SetString("shared", "startup_rebuild_mode", startupRebuildMode);
                config.SetString("shared", "is_multi_tenant_mode", isMultiTenantMode);
                config.SetBoolean("shared", "is_multi_tenant_mode", IsMultiTenantMode());

                foreach (string integerKey in RuntimeSharedIntegerKeys)
                {
                    string rawValue = _configLoader.GetConfig(integerKey);
                    if (int.TryParse(rawValue, out int parsedValue))
                    {
                        config.SetInteger("shared", integerKey, parsedValue);
                    }
                }
            }
        }
    }

    private int FindExactOverridableConfigurationIndex(string tenant)
    {
        string expectedDocumentId = BuildOverridableConfigurationDocumentId(tenant);
        for (int i = 0; i < _overridableConfigSets.Count; i++)
        {
            if (MatchesOverridableConfigurationDocument(_overridableConfigSets[i], expectedDocumentId))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindExactConfigurationSetIndex(string tenant)
    {
        for (int i = 0; i < _dbConfigSets.Count; i++)
        {
            if (string.Equals(_dbConfigSets[i]?._id, tenant, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private string BuildOverridableConfigurationDocumentId(string tenant)
    {
        string sharedConfigId = GetSharedConfigId();
        return string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(sharedConfigId)
            ? null
            : $"{tenant}_{sharedConfigId}";
    }

    private static bool MatchesOverridableConfigurationDocument(
        OverridableConfiguration configuration,
        string expectedDocumentId)
    {
        return
            configuration != null &&
            !string.IsNullOrWhiteSpace(expectedDocumentId) &&
            string.Equals(configuration._id, expectedDocumentId, StringComparison.OrdinalIgnoreCase);
    }

    private OverridableConfiguration FindOverridableConfiguration(string tenant)
    {
        lock (_overridableConfigSets)
        {
            int exactIndex = FindExactOverridableConfigurationIndex(tenant);
            if (exactIndex >= 0)
            {
                return _overridableConfigSets[exactIndex];
            }
        }

        return MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _fallbackConfiguration, tenant);
    }

    private string GetTenantNameFromOverridableConfiguration(OverridableConfiguration configuration)
    {
        if (configuration == null || string.IsNullOrWhiteSpace(configuration._id))
        {
            return null;
        }

        string expectedSuffix = $"_{GetSharedConfigId()}";
        if (!configuration._id.EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string tenantName = configuration._id[..^expectedSuffix.Length];
        return string.IsNullOrWhiteSpace(tenantName) ? null : tenantName.Trim();
    }

    private string GetSingleTenantName()
    {
        string configuredTenant = _configLoader.GetConfig("config_id")
            ?? _configLoader.GetConfig("app_instance_name");
        return string.IsNullOrWhiteSpace(configuredTenant) ? null : configuredTenant.Trim();
    }

    private bool IsMultiTenantMode()
    {
        string multiTenantJurisdictions = _configLoader.GetConfig("multi_tenant_jurisdictions");
        string multiTenantTemplateUrl = _configLoader.GetConfig("multi_tenant_shared_config_id_template_couchdb_url");
        string multiTenantRebuildSource = _configLoader.GetConfig("multi_tenant_re_build_src");

        return
            !string.IsNullOrWhiteSpace(multiTenantJurisdictions) ||
            !string.IsNullOrWhiteSpace(multiTenantTemplateUrl) ||
            !string.IsNullOrWhiteSpace(multiTenantRebuildSource);
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

    private async Task UpsertStartupRunSummaryTenantAsync(
        string currentHostPrefix,
        string tenant,
        string metadataVersion,
        string status,
        string lastError = null,
        bool preserveExistingStatus = false)
    {
        if (string.IsNullOrWhiteSpace(tenant) || !TryGetSummaryDbConfig(currentHostPrefix, out var summaryDbConfig))
        {
            return;
        }

        await EnsureRebuildDatabaseExistsAsync(summaryDbConfig);

        var summary = await TryGetStartupRunSummaryDocumentAsync(summaryDbConfig)
            ?? CreateStartupRunSummaryDocument(currentHostPrefix, metadataVersion, GetLoadedTenantNames());

        var mergedTenants = MergeTenantNames(
            GetLoadedTenantNames(),
            GetConfiguredTenants(summary),
            [tenant]);

        summary["summary_host_prefix"] = GetSummaryHostPrefix(currentHostPrefix);
        if (string.IsNullOrWhiteSpace(summary.Value<string>("metadata_version")) && !string.IsNullOrWhiteSpace(metadataVersion))
        {
            summary["metadata_version"] = metadataVersion;
        }

        summary["configured_tenants"] = new JArray(mergedTenants);
        EnsureSummaryTenantEntries(summary, mergedTenants);

        var tenantStatuses = GetTenantStatuses(summary);
        if (tenantStatuses[tenant] is not JObject tenantStatus)
        {
            tenantStatus = new JObject
            {
                ["host_prefix"] = tenant
            };
            tenantStatuses[tenant] = tenantStatus;
        }

        tenantStatus["host_prefix"] = tenant;
        if (TryGetTenantDbConfig(FindOverridableConfiguration(tenant), tenant, out var tenantDbConfig))
        {
            tenantStatus["couchdb_url"] = tenantDbConfig.url;
        }

        if (!string.IsNullOrWhiteSpace(metadataVersion))
        {
            tenantStatus["metadata_version"] = metadataVersion;
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            (!preserveExistingStatus || string.IsNullOrWhiteSpace(tenantStatus.Value<string>("status"))))
        {
            tenantStatus["status"] = status;
        }

        if (!string.IsNullOrWhiteSpace(lastError))
        {
            tenantStatus["last_error"] = lastError;
        }
        else if (!string.Equals(status, "paused", StringComparison.OrdinalIgnoreCase))
        {
            tenantStatus.Remove("last_error");
        }

        if (!string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            tenantStatus.Remove("completed_utc");
        }

        tenantStatus["last_updated_utc"] = DateTime.UtcNow.ToString("o");
        UpdateSummaryTotals(summary);
        await SaveStartupRunSummaryDocumentAsync(summaryDbConfig, summary);
        StartupRunSummaryCache.Set(GetSummaryHostPrefix(currentHostPrefix), summary);
    }

    private bool TryGetSummaryDbConfig(string currentHostPrefix, out DBConfigurationDetail summaryDbConfig)
    {
        summaryDbConfig = null;
        string summaryHostPrefix = GetSummaryHostPrefix(currentHostPrefix);

        return TryGetTenantDbConfig(
            FindOverridableConfiguration(summaryHostPrefix),
            summaryHostPrefix,
            out summaryDbConfig);
    }

    private async Task EnsureRebuildDatabaseExistsAsync(DBConfigurationDetail dbConfig)
    {
        string rebuildDbUrl = $"{dbConfig.url}/{dbConfig.prefix}db_rebuild";
        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            rebuildDbUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value,
            throwOnError: false);

        bool notFound = true;
        if (!string.IsNullOrWhiteSpace(response))
        {
            var payload = JObject.Parse(response);
            notFound = string.Equals(payload.Value<string>("error"), "not_found", StringComparison.OrdinalIgnoreCase);
        }

        if (!notFound)
        {
            return;
        }

        await _couchDbHttpClient.ExecuteAsync(
            "PUT",
            rebuildDbUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value);

        await _couchDbHttpClient.ExecuteAsync(
            "PUT",
            $"{rebuildDbUrl}/_security",
            StartupRebuildSecurityPayload,
            dbConfig.user_name,
            dbConfig.user_value);
    }

    private async Task<JObject> TryGetStartupRunSummaryDocumentAsync(DBConfigurationDetail dbConfig)
    {
        string summaryUrl = $"{dbConfig.url}/{dbConfig.prefix}db_rebuild/startup-run-summary";
        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            summaryUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value,
            throwOnError: false);

        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var payload = JObject.Parse(response);
        if (string.Equals(payload.Value<string>("error"), "not_found", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return payload;
    }

    private async Task SaveStartupRunSummaryDocumentAsync(DBConfigurationDetail dbConfig, JObject summary)
    {
        string summaryUrl = $"{dbConfig.url}/{dbConfig.prefix}db_rebuild/startup-run-summary";
        summary["_id"] = "startup-run-summary";
        summary["last_updated_utc"] = DateTime.UtcNow.ToString("o");

        for (int attempt = 0; attempt < 2; attempt++)
        {
            string payload = Newtonsoft.Json.JsonConvert.SerializeObject(
                summary,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                });

            string response = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                summaryUrl,
                payload,
                dbConfig.user_name,
                dbConfig.user_value,
                throwOnError: false);

            if (!string.IsNullOrWhiteSpace(response))
            {
                var result = JObject.Parse(response);
                if (result.Value<bool?>("ok") == true)
                {
                    summary["_rev"] = result.Value<string>("rev");
                    return;
                }

                if (attempt == 0 &&
                    string.Equals(result.Value<string>("error"), "conflict", StringComparison.OrdinalIgnoreCase))
                {
                    var latestSummary = await TryGetStartupRunSummaryDocumentAsync(dbConfig);
                    summary["_rev"] = latestSummary?.Value<string>("_rev");
                    continue;
                }
            }

            return;
        }
    }

    private JObject CreateStartupRunSummaryDocument(
        string currentHostPrefix,
        string metadataVersion,
        IEnumerable<string> configuredTenants)
    {
        var mergedTenants = MergeTenantNames(configuredTenants, [currentHostPrefix]);
        var summary = new JObject
        {
            ["_id"] = "startup-run-summary",
            ["status"] = "running",
            ["metadata_version"] = metadataVersion,
            ["summary_host_prefix"] = GetSummaryHostPrefix(currentHostPrefix),
            ["configured_tenants"] = new JArray(mergedTenants),
            ["tenant_statuses"] = new JObject(),
            ["started_utc"] = DateTime.UtcNow.ToString("o"),
            ["last_updated_utc"] = DateTime.UtcNow.ToString("o")
        };

        EnsureSummaryTenantEntries(summary, mergedTenants);
        UpdateSummaryTotals(summary);
        return summary;
    }

    private static List<string> GetConfiguredTenants(JObject summary)
    {
        return summary?["configured_tenants"] is not JArray configuredTenants
            ? new List<string>()
            : configuredTenants
                .Values<string>()
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    private static List<string> MergeTenantNames(params IEnumerable<string>[] tenantGroups)
    {
        return tenantGroups
            .Where(group => group != null)
            .SelectMany(group => group)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static JObject GetTenantStatuses(JObject summary)
    {
        if (summary?["tenant_statuses"] is JObject tenantStatuses)
        {
            return tenantStatuses;
        }

        tenantStatuses = new JObject();
        summary["tenant_statuses"] = tenantStatuses;
        return tenantStatuses;
    }

    private static void EnsureSummaryTenantEntries(JObject summary, IEnumerable<string> tenants)
    {
        var tenantStatuses = GetTenantStatuses(summary);
        foreach (string tenant in tenants)
        {
            if (tenantStatuses[tenant] != null)
            {
                continue;
            }

            tenantStatuses[tenant] = new JObject
            {
                ["host_prefix"] = tenant,
                ["status"] = "pending"
            };
        }
    }

    private static void UpdateSummaryTotals(JObject summary)
    {
        var tenantStatuses = GetTenantStatuses(summary);
        var configuredTenants = GetConfiguredTenants(summary);
        if (configuredTenants.Count == 0)
        {
            configuredTenants = tenantStatuses.Properties()
                .Select(property => property.Name)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();

            summary["configured_tenants"] = new JArray(configuredTenants);
        }

        int completedTenantCount = 0;
        int pausedTenantCount = 0;
        int runningTenantCount = 0;
        int pendingTenantCount = 0;
        int totalProcessedCaseCount = 0;
        int totalSkippedCaseCount = 0;
        int totalDocumentErrorCount = 0;
        int totalDeIdBulkErrorCount = 0;
        int totalReportBulkErrorCount = 0;
        int totalDeIdDocCount = 0;
        int totalReportDocCount = 0;
        string firstError = null;

        foreach (string tenant in configuredTenants)
        {
            if (tenantStatuses[tenant] is not JObject tenantStatus)
            {
                pendingTenantCount++;
                continue;
            }

            totalProcessedCaseCount += tenantStatus.Value<int?>("processed_case_count") ?? 0;
            totalSkippedCaseCount += tenantStatus.Value<int?>("skipped_case_count") ?? 0;
            totalDocumentErrorCount += tenantStatus.Value<int?>("document_error_count") ?? 0;
            totalDeIdBulkErrorCount += tenantStatus.Value<int?>("de_id_bulk_error_count") ?? 0;
            totalReportBulkErrorCount += tenantStatus.Value<int?>("report_bulk_error_count") ?? 0;
            totalDeIdDocCount += tenantStatus.Value<int?>("total_de_id_doc_count") ?? 0;
            totalReportDocCount += tenantStatus.Value<int?>("total_report_doc_count") ?? 0;
            firstError ??= tenantStatus.Value<string>("last_error");

            switch (tenantStatus.Value<string>("status")?.ToLowerInvariant())
            {
                case "completed":
                    completedTenantCount++;
                    break;
                case "paused":
                    pausedTenantCount++;
                    break;
                case "running":
                    runningTenantCount++;
                    break;
                default:
                    pendingTenantCount++;
                    break;
            }
        }

        int totalTenantCount = configuredTenants.Count;
        summary["total_tenant_count"] = totalTenantCount;
        summary["completed_tenant_count"] = completedTenantCount;
        summary["paused_tenant_count"] = pausedTenantCount;
        summary["running_tenant_count"] = runningTenantCount;
        summary["pending_tenant_count"] = pendingTenantCount;
        summary["total_processed_case_count"] = totalProcessedCaseCount;
        summary["total_skipped_case_count"] = totalSkippedCaseCount;
        summary["total_document_error_count"] = totalDocumentErrorCount;
        summary["total_de_id_bulk_error_count"] = totalDeIdBulkErrorCount;
        summary["total_report_bulk_error_count"] = totalReportBulkErrorCount;
        summary["total_de_id_doc_count"] = totalDeIdDocCount;
        summary["total_report_doc_count"] = totalReportDocCount;
        summary["last_error"] = firstError;
        summary["last_updated_utc"] = DateTime.UtcNow.ToString("o");

        if (totalTenantCount > 0 && completedTenantCount == totalTenantCount)
        {
            summary["status"] = "completed";
            if (summary["completed_utc"] == null)
            {
                summary["completed_utc"] = DateTime.UtcNow.ToString("o");
            }
        }
        else if (runningTenantCount > 0)
        {
            summary["status"] = "running";
            summary.Remove("completed_utc");
        }
        else if (pausedTenantCount > 0)
        {
            summary["status"] = "incomplete";
            summary.Remove("completed_utc");
        }
        else
        {
            summary["status"] = "running";
            summary.Remove("completed_utc");
        }
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
