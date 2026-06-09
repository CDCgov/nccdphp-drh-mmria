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

    private sealed class RebuildStartDecision
    {
        public string decision { get; init; }
        public DurableTenantRebuildState state { get; init; }
        public bool starts_worker { get; init; }
        public bool resumes_existing_run { get; init; }
    }

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

    private List<string> ResolveExcludedTenants()
    {
        return DbRebuildSettings.ResolveExcludedTenants(
            _configLoader.GetConfig(DbRebuildSettings.StartupRebuildExcludeFromRebuildKey));
    }

    public async Task<MMRIARebuildResponse> EnqueueInProcessRebuildAsync(MMRIARebuildRequest request)
    {
        string normalizedTenant = NormalizeTenant(request?.tenant);
        string normalizedSource = NormalizeSource(request?.source);
        List<string> normalizedConfiguredTenants = DbRebuildSettings.NormalizeTenantListPreservingOrder(request?.configured_tenants);
        string normalizedSummaryHostPrefix = NormalizeTenant(request?.summary_host_prefix);
        string requestedBehavior = NormalizeRequestedBehavior(request, normalizedSource);
        bool allowResume = request?.allow_resume ?? string.Equals(normalizedSource, "startup", StringComparison.OrdinalIgnoreCase);
        List<string> excludedTenants = ResolveExcludedTenants();

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

        TenantRebuildCoordinator.TenantRebuildLease lease = null;

        try
        {
            var runtime = await ResolveRuntimeAsync(normalizedTenant);
            if (runtime == null || runtime.db_config == null || runtime.configuration == null)
            {
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

            await _mmriaRebuildDal.EnsureRebuildDatabaseExistsAsync(runtime.db_config);

            string ownerId = CreateOwnerId();
            string requestFingerprint = BuildRequestFingerprint(
                normalizedTenant,
                runtime.metadata_version,
                normalizedConfiguredTenants,
                normalizedSummaryHostPrefix);
            int leaseSeconds = GetRuntimeInteger(
                runtime.configuration,
                normalizedTenant,
                DbRebuildSettings.StartupRebuildLeaseSecondsKey,
                300,
                60);
            var durableDecision = await ResolveDurableStartDecisionAsync(
                runtime.db_config,
                normalizedTenant,
                normalizedSource,
                requestedBehavior,
                allowResume,
                runtime.metadata_version,
                request?.request_id,
                requestFingerprint,
                ownerId,
                leaseSeconds);

            if (!durableDecision.starts_worker)
            {
                return BuildNonStartingResponse(normalizedTenant, normalizedSource, durableDecision);
            }

            if (!TenantRebuildCoordinator.TryAcquire(
                normalizedTenant,
                normalizedSource,
                "legacy",
                "queued",
                out lease,
                out var existingReservation))
            {
                return new MMRIARebuildResponse
                {
                    success = false,
                    status_code = 409,
                    tenant = normalizedTenant,
                    source = normalizedSource,
                    run_id = durableDecision.state?.run_id,
                    decision = "already_active",
                    message = $"A rebuild is already running or queued for tenant '{normalizedTenant}'.",
                    error = existingReservation == null
                        ? "rebuild already queued"
                        : $"Existing rebuild source='{existingReservation.source}', status='{existingReservation.status}'."
                };
            }

            if (excludedTenants.Contains(normalizedTenant, StringComparer.OrdinalIgnoreCase))
            {
                var excludedWorker = new MMRIARebuildWorker(
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
                    normalizedSummaryHostPrefix,
                    _mmriaRebuildDal,
                    durableDecision.state,
                    ownerId,
                    durableDecision.decision,
                    durableDecision.resumes_existing_run);

                await excludedWorker.PersistExcludedSummaryAsync();
                await MarkDurableRunTerminalAsync(runtime.db_config, durableDecision.state, ownerId, "excluded", null);
                lease.Dispose();

                return new MMRIARebuildResponse
                {
                    success = true,
                    status_code = 200,
                    tenant = normalizedTenant,
                    source = normalizedSource,
                    run_id = durableDecision.state?.run_id,
                    decision = durableDecision.decision,
                    message = $"Rebuild for tenant '{normalizedTenant}' was excluded by '{DbRebuildSettings.StartupRebuildExcludeFromRebuildKey}'.",
                    rebuild_started = false
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
                normalizedSummaryHostPrefix,
                _mmriaRebuildDal,
                durableDecision.state,
                ownerId,
                durableDecision.decision,
                durableDecision.resumes_existing_run);

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
                run_id = durableDecision.state?.run_id,
                decision = durableDecision.decision,
                message = durableDecision.resumes_existing_run
                    ? $"Resumed rebuild for tenant '{normalizedTenant}'."
                    : $"Started a fresh rebuild for tenant '{normalizedTenant}'.",
                rebuild_started = true
            };
        }
        catch (Exception ex)
        {
            lease?.Dispose();
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

    private async Task<RebuildStartDecision> ResolveDurableStartDecisionAsync(
        mmria.common.couchdb.DBConfigurationDetail dbConfig,
        string tenant,
        string source,
        string requestedBehavior,
        bool allowResume,
        string metadataVersion,
        string requestId,
        string requestFingerprint,
        string ownerId,
        int leaseSeconds)
    {
        DateTime now = DateTime.UtcNow;
        string nowText = now.ToString("o");
        string activeDocumentId = MMRIARebuildDAL.GetActiveDocumentId(tenant);
        var existing = await _mmriaRebuildDal.GetActiveRebuildAsync(dbConfig, tenant);

        bool isForceFresh = string.Equals(requestedBehavior, "force_fresh", StringComparison.OrdinalIgnoreCase);
        bool isEnsure = string.Equals(requestedBehavior, "ensure", StringComparison.OrdinalIgnoreCase);
        bool isResumeRequested = string.Equals(requestedBehavior, "resume", StringComparison.OrdinalIgnoreCase);

        if (existing != null)
        {
            bool completed = string.Equals(existing.state, "completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(existing.state, "excluded", StringComparison.OrdinalIgnoreCase);
            bool parkedIndexingPending = IsIndexingPendingState(existing);
            bool matchingFingerprint = string.Equals(existing.request_fingerprint, requestFingerprint, StringComparison.OrdinalIgnoreCase);

            if (completed && matchingFingerprint && isEnsure)
            {
                return new RebuildStartDecision
                {
                    decision = "already_completed",
                    state = existing,
                    starts_worker = false
                };
            }

            if (!completed && !parkedIndexingPending && IsLeaseFresh(existing, now))
            {
                return new RebuildStartDecision
                {
                    decision = "already_active",
                    state = existing,
                    starts_worker = false
                };
            }

            if (!completed && !isForceFresh && (parkedIndexingPending || allowResume || isEnsure || isResumeRequested))
            {
                existing.owner_id = ownerId;
                existing.lease_acquired_utc = nowText;
                existing.heartbeat_utc = nowText;
                existing.lease_expires_utc = now.AddSeconds(leaseSeconds).ToString("o");
                existing.lease_seconds = leaseSeconds;
                existing.state = ResolveResumeState(existing);
                existing.decision = "resume";
                existing.requested_behavior = requestedBehavior;
                existing.request_id = string.IsNullOrWhiteSpace(requestId) ? existing.request_id : requestId;
                existing.resume_count++;
                existing.completed_utc = null;
                existing.last_error = null;
                existing.last_updated_utc = nowText;
                bool saved = await _mmriaRebuildDal.SaveActiveRebuildAsync(dbConfig, existing);
                if (saved)
                {
                    await SaveRunHistorySnapshotAsync(dbConfig, existing);
                    return new RebuildStartDecision
                    {
                        decision = "resume",
                        state = existing,
                        starts_worker = true,
                        resumes_existing_run = true
                    };
                }

                return new RebuildStartDecision
                {
                    decision = "already_active",
                    state = await _mmriaRebuildDal.GetActiveRebuildAsync(dbConfig, tenant) ?? existing,
                    starts_worker = false
                };
            }
        }

        if (existing != null && !IsTerminalState(existing.state) && !isForceFresh && !allowResume)
        {
            return new RebuildStartDecision
            {
                decision = "requires_force_fresh",
                state = existing,
                starts_worker = false
            };
        }

        var freshState = CreateFreshDurableState(
            activeDocumentId,
            existing?._rev,
            tenant,
            source,
            requestedBehavior,
            metadataVersion,
            requestId,
            requestFingerprint,
            ownerId,
            leaseSeconds,
            now);

        bool freshSaved = await _mmriaRebuildDal.SaveActiveRebuildAsync(dbConfig, freshState);
        if (!freshSaved)
        {
            return new RebuildStartDecision
            {
                decision = "already_active",
                state = await _mmriaRebuildDal.GetActiveRebuildAsync(dbConfig, tenant),
                starts_worker = false
            };
        }

        await SaveRunHistorySnapshotAsync(dbConfig, freshState);
        return new RebuildStartDecision
        {
            decision = "start_fresh",
            state = freshState,
            starts_worker = true,
            resumes_existing_run = false
        };
    }

    private static DurableTenantRebuildState CreateFreshDurableState(
        string activeDocumentId,
        string revision,
        string tenant,
        string source,
        string requestedBehavior,
        string metadataVersion,
        string requestId,
        string requestFingerprint,
        string ownerId,
        int leaseSeconds,
        DateTime now)
    {
        string runId = $"{now:yyyyMMddTHHmmssZ}-{NormalizeForRunId(tenant)}-{Guid.NewGuid():N}";
        string nowText = now.ToString("o");

        return new DurableTenantRebuildState
        {
            _id = activeDocumentId,
            _rev = revision,
            tenant = tenant,
            run_id = runId,
            source = source,
            mode = "legacy",
            request_id = string.IsNullOrWhiteSpace(requestId) ? Guid.NewGuid().ToString("N") : requestId.Trim(),
            request_fingerprint = requestFingerprint,
            requested_behavior = requestedBehavior,
            decision = "start_fresh",
            state = "queued",
            owner_id = ownerId,
            lease_acquired_utc = nowText,
            heartbeat_utc = nowText,
            lease_expires_utc = now.AddSeconds(leaseSeconds).ToString("o"),
            lease_seconds = leaseSeconds,
            metadata_version = metadataVersion,
            target_generation = runId,
            document_write_status = "not_started",
            index_warmup_status = "not_started",
            started_utc = nowText,
            last_updated_utc = nowText
        };
    }

    private async Task SaveRunHistorySnapshotAsync(
        mmria.common.couchdb.DBConfigurationDetail dbConfig,
        DurableTenantRebuildState state)
    {
        if (state == null)
        {
            return;
        }

        await _mmriaRebuildDal.SaveRunHistoryAsync(
            dbConfig,
            new DurableTenantRebuildRunHistory
            {
                _id = MMRIARebuildDAL.GetRunHistoryDocumentId(state.run_id),
                tenant = state.tenant,
                run_id = state.run_id,
                source = state.source,
                request_id = state.request_id,
                request_fingerprint = state.request_fingerprint,
                final_state = state.state,
                first_owner_id = state.resume_count > 0 ? null : state.owner_id,
                current_owner_id = state.owner_id,
                resume_count = state.resume_count,
                started_utc = state.started_utc,
                completed_utc = state.completed_utc,
                last_updated_utc = DateTime.UtcNow.ToString("o"),
                last_error = state.last_error
            });
    }

    private async Task MarkDurableRunTerminalAsync(
        mmria.common.couchdb.DBConfigurationDetail dbConfig,
        DurableTenantRebuildState state,
        string ownerId,
        string terminalState,
        string lastError)
    {
        if (state == null)
        {
            return;
        }

        await _mmriaRebuildDal.MutateActiveRebuildAsync(
            dbConfig,
            state.tenant,
            ownerId,
            requireCurrentOwner: true,
            active =>
            {
                active.state = terminalState;
                active.document_write_status = terminalState;
                active.completed_utc = DateTime.UtcNow.ToString("o");
                active.last_error = lastError;
            });
    }

    private static MMRIARebuildResponse BuildNonStartingResponse(
        string tenant,
        string source,
        RebuildStartDecision decision)
    {
        string decisionValue = decision?.decision ?? "already_active";
        int statusCode = string.Equals(decisionValue, "already_completed", StringComparison.OrdinalIgnoreCase)
            ? 200
            : string.Equals(decisionValue, "requires_force_fresh", StringComparison.OrdinalIgnoreCase)
                ? 409
                : 409;

        return new MMRIARebuildResponse
        {
            success = string.Equals(decisionValue, "already_completed", StringComparison.OrdinalIgnoreCase),
            status_code = statusCode,
            tenant = tenant,
            source = source,
            run_id = decision?.state?.run_id,
            decision = decisionValue,
            rebuild_started = false,
            message = decisionValue switch
            {
                "already_completed" => $"A matching rebuild has already completed for tenant '{tenant}'.",
                "requires_force_fresh" => $"Interrupted rebuild for tenant '{tenant}' cannot be resumed without an explicit fresh rebuild decision.",
                _ => $"A rebuild is already active for tenant '{tenant}'."
            },
            error = string.Equals(decisionValue, "already_completed", StringComparison.OrdinalIgnoreCase)
                ? null
                : decision?.state == null
                    ? "rebuild already active"
                    : $"Existing rebuild run_id='{decision.state.run_id}', state='{decision.state.state}', heartbeat_utc='{decision.state.heartbeat_utc}', lease_expires_utc='{decision.state.lease_expires_utc}'."
        };
    }

    private static bool IsLeaseFresh(DurableTenantRebuildState state, DateTime now)
    {
        return state != null &&
            DateTime.TryParse(state.lease_expires_utc, out DateTime expiresUtc) &&
            expiresUtc.ToUniversalTime() > now;
    }

    private static bool IsIndexingPendingState(DurableTenantRebuildState state)
    {
        return string.Equals(state?.state, "indexing_pending", StringComparison.OrdinalIgnoreCase) ||
            (
                string.Equals(state?.document_write_status, "completed", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(state?.index_warmup_status, "pending", StringComparison.OrdinalIgnoreCase)
            );
    }

    private static bool IsTerminalState(string state)
    {
        return string.Equals(state, "completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(state, "excluded", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(state, "failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(state, "requires_force_fresh", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveResumeState(DurableTenantRebuildState state)
    {
        if (string.Equals(state?.document_write_status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            return "indexing";
        }

        return "writing";
    }

    private static string NormalizeRequestedBehavior(MMRIARebuildRequest request, string normalizedSource)
    {
        string rawValue = request?.requested_behavior;
        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            string normalized = rawValue.Trim().ToLowerInvariant().Replace("-", "_", StringComparison.Ordinal);
            if (normalized is "ensure" or "resume" or "force_fresh")
            {
                return normalized;
            }
        }

        return string.Equals(normalizedSource, "startup", StringComparison.OrdinalIgnoreCase)
            ? "ensure"
            : "resume";
    }

    private static string BuildRequestFingerprint(
        string tenant,
        string metadataVersion,
        IEnumerable<string> configuredTenants,
        string summaryHostPrefix)
    {
        string configuredTenantText = string.Join(
            ",",
            DbRebuildSettings.NormalizeTenantListPreservingOrder(configuredTenants));

        return string.Join(
            "|",
            new[]
            {
                tenant ?? string.Empty,
                metadataVersion ?? string.Empty,
                configuredTenantText,
                summaryHostPrefix ?? string.Empty,
                "legacy"
            });
    }

    private static string CreateOwnerId()
    {
        return $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    }

    private static string NormalizeForRunId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "tenant";
        }

        var builder = new System.Text.StringBuilder(value.Length);
        foreach (char item in value.Trim().ToLowerInvariant())
        {
            builder.Append(char.IsLetterOrDigit(item) || item == '-' || item == '_' ? item : '-');
        }

        string result = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(result) ? "tenant" : result;
    }

    private static int GetRuntimeInteger(
        mmria.common.couchdb.OverridableConfiguration configuration,
        string hostPrefix,
        string key,
        int defaultValue,
        int minimumValue)
    {
        int value = configuration?.GetInteger(key, hostPrefix) ?? defaultValue;
        return Math.Max(minimumValue, value);
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
        MirrorBooleanIfPresent(configuration, DbRebuildSettings.StartupRebuildIndexAddBeginningKey);
        MirrorStringIfPresent(configuration, DbRebuildSettings.StartupRebuildIndexRestoreModeKey);
        MirrorIntegerIfPresent(configuration, DbRebuildSettings.StartupRebuildIndexWarmDelayMsKey);
        MirrorIntegerIfPresent(configuration, DbRebuildSettings.StartupRebuildIndexWarmPollDelayMsKey);
        MirrorIntegerIfPresent(configuration, DbRebuildSettings.StartupRebuildIndexWarmTimeoutMsKey);
        MirrorIntegerIfPresent(configuration, DbRebuildSettings.StartupRebuildIndexWarmMaxSurfacesPerRunKey);
        MirrorIntegerIfPresent(configuration, DbRebuildSettings.StartupRebuildHeartbeatIntervalSecondsKey);
        MirrorIntegerIfPresent(configuration, DbRebuildSettings.StartupRebuildLeaseSecondsKey);
        MirrorIntegerIfPresent(configuration, DbRebuildSettings.StartupRebuildStaleAfterSecondsKey);
    }

    private void MirrorIntegerIfPresent(mmria.common.couchdb.OverridableConfiguration configuration, string key)
    {
        string rawValue = _configLoader.GetConfig(key);
        if (int.TryParse(rawValue, out int parsedValue))
        {
            configuration.SetInteger("shared", key, parsedValue);
        }
    }

    private void MirrorBooleanIfPresent(mmria.common.couchdb.OverridableConfiguration configuration, string key)
    {
        string rawValue = _configLoader.GetConfig(key);
        if (bool.TryParse(rawValue, out bool parsedValue))
        {
            configuration.SetBoolean("shared", key, parsedValue);
        }
    }

    private void MirrorStringIfPresent(mmria.common.couchdb.OverridableConfiguration configuration, string key)
    {
        string rawValue = _configLoader.GetConfig(key);
        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            configuration.SetString("shared", key, rawValue.Trim());
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
