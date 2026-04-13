using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.MMRIARebuild.Model;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.MMRIARebuild.Manager;

internal sealed class MMRIARebuildWorker
{
    private const string StartupRebuildDatabaseName = "db_rebuild";
    private const string LegacyStartupRebuildCheckpointDocumentId = "startup-rebuild-status";
    private const string StartupRunSummaryDocumentId = "startup-run-summary";
    private const string StartupRebuildSecurityPayload = "{\"admins\":{\"names\":[],\"roles\":[\"form_designer\"]},\"members\":{\"names\":[],\"roles\":[\"abstractor\",\"data_analyst\",\"timer\"]}}";

    private readonly string _couchdbUrl;
    private readonly string _userName;
    private readonly string _userValue;
    private readonly string _metadataVersion;
    private readonly mmria.common.couchdb.DBConfigurationDetail _dbConfig;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly string _hostPrefix;
    private readonly TenantRebuildCoordinator.TenantRebuildLease _tenantRebuildLease;
    private readonly string _rebuildSource;
    private readonly List<string> _configuredTenants;
    private readonly string _summaryHostPrefix;

    public MMRIARebuildWorker(
        string couchdbUrl,
        string userName,
        string userValue,
        string metadataVersion,
        mmria.common.couchdb.DBConfigurationDetail dbConfig,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.couchdb.OverridableConfiguration configuration,
        string hostPrefix,
        TenantRebuildCoordinator.TenantRebuildLease tenantRebuildLease,
        string rebuildSource,
        List<string> configuredTenants,
        string summaryHostPrefix)
    {
        _couchdbUrl = couchdbUrl;
        _userName = userName;
        _userValue = userValue;
        _metadataVersion = metadataVersion;
        _dbConfig = dbConfig;
        _couchDbHttpClient = couchDbHttpClient;
        _configuration = configuration;
        _hostPrefix = hostPrefix;
        _tenantRebuildLease = tenantRebuildLease;
        _rebuildSource = rebuildSource;
        _configuredTenants = DbRebuildSettings.NormalizeTenantListPreservingOrder(configuredTenants);
        _summaryHostPrefix = string.IsNullOrWhiteSpace(summaryHostPrefix)
            ? null
            : summaryHostPrefix.Trim();
    }

    public async Task PersistQueuedSummaryAsync()
    {
        var tenantState = new StartupRebuildTenantSummary
        {
            host_prefix = GetEffectiveHostPrefix(),
            couchdb_url = _couchdbUrl,
            status = "queued",
            metadata_version = _metadataVersion,
            started_utc = DateTime.UtcNow.ToString("o"),
            last_updated_utc = DateTime.UtcNow.ToString("o")
        };

        await SyncStartupRunSummaryAsync(
            tenantState,
            forceReset: ShouldResetStartupRunSummary(),
            persistToDatabase: true);
    }

    public async Task ExecuteAsync()
    {
        int pageSize = GetRebuildSetting("startup_rebuild_page_size", 100, 1);
        int batchDelayMs = GetRebuildSetting("startup_rebuild_batch_delay_ms", 0, 0);
        int writeRetryCount = GetRebuildSetting("startup_rebuild_bulk_write_retry_count", 2, 0);
        int writeRetryDelayMs = GetRebuildSetting("startup_rebuild_bulk_write_retry_delay_ms", 1000, 0);
        int progressPersistEveryBatches = GetRebuildSetting("startup_rebuild_progress_persist_every_batches", 10, 1);
        int maxConcurrentTenants = DbRebuildSettings.ResolveMaxConcurrentTenants(_configuration, GetEffectiveHostPrefix());

        int processedCaseCount = 0;
        int skippedCaseCount = 0;
        int documentErrorCount = 0;
        int deIdBulkErrorCount = 0;
        int reportBulkErrorCount = 0;
        int totalDeIdDocCount = 0;
        int totalReportDocCount = 0;
        int completedBatchCount = 0;
        string lastProcessedId = null;
        bool rebuildCompletedSuccessfully = false;

        var tenantRebuildState = new StartupRebuildTenantSummary
        {
            host_prefix = GetEffectiveHostPrefix(),
            couchdb_url = _couchdbUrl,
            status = "running",
            metadata_version = _metadataVersion,
            started_utc = DateTime.UtcNow.ToString("o"),
            completed_utc = null,
            last_error = null
        };

        void UpdateRebuildState(string status, string lastError, bool isCompleted)
        {
            tenantRebuildState.status = status;
            tenantRebuildState.last_processed_id = lastProcessedId;
            tenantRebuildState.completed_batch_count = completedBatchCount;
            tenantRebuildState.processed_case_count = processedCaseCount;
            tenantRebuildState.skipped_case_count = skippedCaseCount;
            tenantRebuildState.document_error_count = documentErrorCount;
            tenantRebuildState.de_id_bulk_error_count = deIdBulkErrorCount;
            tenantRebuildState.report_bulk_error_count = reportBulkErrorCount;
            tenantRebuildState.total_de_id_doc_count = totalDeIdDocCount;
            tenantRebuildState.total_report_doc_count = totalReportDocCount;
            tenantRebuildState.last_updated_utc = DateTime.UtcNow.ToString("o");
            tenantRebuildState.completed_utc = isCompleted ? DateTime.UtcNow.ToString("o") : null;
            tenantRebuildState.last_error = lastError;
        }

        async Task PersistStartupRunSummaryAsync(bool forceReset, string context, bool persistToDatabase)
        {
            try
            {
                await SyncStartupRunSummaryAsync(tenantRebuildState, forceReset, persistToDatabase);
            }
            catch (Exception summaryEx)
            {
                System.Console.WriteLine($"Failed to persist {context} startup run summary: {summaryEx}");
            }
        }

        bool resetStartupRunSummary = ShouldResetStartupRunSummary();

        System.Console.WriteLine();
        System.Console.WriteLine("========== MMRIARebuildWorker.ExecuteAsync() ==========");
        System.Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        System.Console.WriteLine($"Tenant prefix: '{_dbConfig.prefix}'");
        System.Console.WriteLine($"CouchDB URL: {_couchdbUrl}");
        System.Console.WriteLine("Startup rebuild implementation: legacy");
        System.Console.WriteLine($"Page size: {pageSize}");
        System.Console.WriteLine($"Max concurrent tenants: {maxConcurrentTenants}");
        System.Console.WriteLine($"Batch delay: {batchDelayMs} ms");
        System.Console.WriteLine($"Bulk write retries: {writeRetryCount}");
        System.Console.WriteLine($"Bulk write retry delay: {writeRetryDelayMs} ms");
        System.Console.WriteLine($"Progress persistence cadence: every {progressPersistEveryBatches} batch(es)");
        System.Console.WriteLine("=======================================================");
        System.Console.WriteLine();

        using var tenantGateLease = await StartupRebuildTenantGate.AcquireAsync(maxConcurrentTenants);
        _tenantRebuildLease.UpdateStatus("running");

        try
        {
            try
            {
                await DeleteLegacyStartupRebuildCheckpointAsync();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Unable to delete legacy startup rebuild checkpoint before rebuild execution: {ex.Message}");
            }

            UpdateRebuildState("running", null, false);
            await PersistStartupRunSummaryAsync(resetStartupRunSummary, "initial", persistToDatabase: true);

            var legacySyncAll = new c_document_sync_all_legacy(
                _couchdbUrl,
                _userName,
                _userValue,
                _metadataVersion,
                _dbConfig,
                _couchDbHttpClient,
                _configuration,
                _hostPrefix,
                pageSize,
                batchDelayMs,
                writeRetryCount,
                writeRetryDelayMs,
                async progress =>
                {
                    processedCaseCount = progress.processed_case_count;
                    skippedCaseCount = progress.skipped_case_count;
                    documentErrorCount = progress.document_error_count;
                    deIdBulkErrorCount = progress.de_id_bulk_error_count;
                    reportBulkErrorCount = progress.report_bulk_error_count;
                    totalDeIdDocCount = progress.total_de_id_doc_count;
                    totalReportDocCount = progress.total_report_doc_count;
                    completedBatchCount = progress.completed_batch_count;
                    lastProcessedId = progress.last_processed_id;

                    UpdateRebuildState("running", null, false);

                    bool shouldPersistProgress = completedBatchCount % progressPersistEveryBatches == 0;
                    await PersistStartupRunSummaryAsync(
                        forceReset: false,
                        context: shouldPersistProgress ? $"legacy post-batch {progress.batch_number}" : $"legacy cached post-batch {progress.batch_number}",
                        persistToDatabase: shouldPersistProgress);
                });

            var legacyResult = await legacySyncAll.executeAsync();
            processedCaseCount = legacyResult.processed_case_count;
            skippedCaseCount = legacyResult.skipped_case_count;
            documentErrorCount = legacyResult.document_error_count;
            deIdBulkErrorCount = legacyResult.de_id_bulk_error_count;
            reportBulkErrorCount = legacyResult.report_bulk_error_count;
            totalDeIdDocCount = legacyResult.total_de_id_doc_count;
            totalReportDocCount = legacyResult.total_report_doc_count;
            completedBatchCount = legacyResult.completed_batch_count;
            lastProcessedId = legacyResult.last_processed_id;
            rebuildCompletedSuccessfully = legacyResult.rebuild_completed_successfully;
            tenantRebuildState.last_error = legacyResult.last_error;

            UpdateRebuildState(
                rebuildCompletedSuccessfully ? "completed" : "paused",
                rebuildCompletedSuccessfully ? null : tenantRebuildState.last_error,
                rebuildCompletedSuccessfully);

            await PersistStartupRunSummaryAsync(forceReset: false, context: "final", persistToDatabase: true);

            System.Console.WriteLine();
            System.Console.WriteLine(
                $"Startup rebuild {(rebuildCompletedSuccessfully ? "complete" : "paused")}. " +
                $"Processed {processedCaseCount} cases, generated {totalDeIdDocCount} de_id docs and {totalReportDocCount} report docs. " +
                $"Document build errors: {documentErrorCount}. de_id bulk errors: {deIdBulkErrorCount}. report bulk errors: {reportBulkErrorCount}. Skipped cases: {skippedCaseCount}.");
            System.Console.WriteLine();
        }
        finally
        {
            _tenantRebuildLease.Dispose();
        }
    }

    private string GetEffectiveHostPrefix()
    {
        if (!string.IsNullOrWhiteSpace(_hostPrefix))
        {
            return _hostPrefix.Trim();
        }

        if (!string.IsNullOrWhiteSpace(_dbConfig?.prefix))
        {
            return _dbConfig.prefix.Trim();
        }

        return "shared";
    }

    private int GetRebuildSetting(string key, int defaultValue, int minimumValue, int? maximumValue = null)
    {
        int configuredValue = _configuration?.GetInteger(key, GetEffectiveHostPrefix()) ?? defaultValue;
        configuredValue = Math.Max(minimumValue, configuredValue);

        if (maximumValue.HasValue)
        {
            configuredValue = Math.Min(maximumValue.Value, configuredValue);
        }

        return configuredValue;
    }

    private List<string> GetConfiguredTenants()
    {
        if (_configuredTenants.Count > 0)
        {
            return _configuredTenants.ToList();
        }

        return new List<string> { GetEffectiveHostPrefix() };
    }

    private string GetSummaryHostPrefix()
    {
        if (!string.IsNullOrWhiteSpace(_summaryHostPrefix))
        {
            return _summaryHostPrefix;
        }

        return GetEffectiveHostPrefix();
    }

    private string GetRebuildDatabaseUrl(string baseCouchdbUrl)
    {
        return baseCouchdbUrl + $"/{_dbConfig.prefix}{StartupRebuildDatabaseName}";
    }

    private string GetLegacyStartupRebuildCheckpointUrl()
    {
        return GetRebuildDatabaseUrl(_couchdbUrl) + $"/{LegacyStartupRebuildCheckpointDocumentId}";
    }

    private string GetStartupRunSummaryBaseUrl()
    {
        string currentHostPrefix = GetEffectiveHostPrefix();
        string summaryHostPrefix = GetSummaryHostPrefix();

        if (string.Equals(summaryHostPrefix, currentHostPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return _couchdbUrl;
        }

        string tenantUrlTemplate = _configuration?.GetString("multi_tenant_shared_config_id_template_couchdb_url", currentHostPrefix);
        if (string.IsNullOrWhiteSpace(tenantUrlTemplate))
        {
            return null;
        }

        return tenantUrlTemplate.Replace("{replace}", summaryHostPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private string GetStartupRunSummaryUrl()
    {
        string summaryBaseUrl = GetStartupRunSummaryBaseUrl();
        if (string.IsNullOrWhiteSpace(summaryBaseUrl))
        {
            return null;
        }

        return GetRebuildDatabaseUrl(summaryBaseUrl) + $"/{StartupRunSummaryDocumentId}";
    }

    private async Task EnsureRebuildDatabaseExistsAsync(string baseCouchdbUrl)
    {
        if (string.IsNullOrWhiteSpace(baseCouchdbUrl))
        {
            return;
        }

        string rebuildDatabaseUrl = GetRebuildDatabaseUrl(baseCouchdbUrl);
        if (await UrlEndpointExistsAsync(rebuildDatabaseUrl))
        {
            return;
        }

        try
        {
            await _couchDbHttpClient.ExecuteAsync("PUT", rebuildDatabaseUrl, null, _userName, _userValue);
        }
        catch (Exception)
        {
        }

        try
        {
            await _couchDbHttpClient.ExecuteAsync("PUT", rebuildDatabaseUrl + "/_security", StartupRebuildSecurityPayload, _userName, _userValue);
        }
        catch (Exception securityEx)
        {
            System.Console.WriteLine($"Failed to configure {_dbConfig.prefix}{StartupRebuildDatabaseName}/_security at '{baseCouchdbUrl}': {securityEx.Message}");
        }
    }

    private async Task DeleteLegacyStartupRebuildCheckpointAsync()
    {
        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            GetLegacyStartupRebuildCheckpointUrl(),
            null,
            _userName,
            _userValue);

        if (string.IsNullOrWhiteSpace(response))
        {
            return;
        }

        var payload = JObject.Parse(response);
        if (string.Equals(payload.Value<string>("error"), "not_found", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string rev = payload.Value<string>("_rev");
        if (string.IsNullOrWhiteSpace(rev))
        {
            return;
        }

        await _couchDbHttpClient.ExecuteAsync(
            "DELETE",
            GetLegacyStartupRebuildCheckpointUrl() + $"?rev={Uri.EscapeDataString(rev)}",
            null,
            _userName,
            _userValue);
    }

    private async Task<StartupRunSummary> TryGetStartupRunSummaryAsync()
    {
        string summaryUrl = GetStartupRunSummaryUrl();
        if (string.IsNullOrWhiteSpace(summaryUrl))
        {
            return null;
        }

        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            summaryUrl,
            null,
            _userName,
            _userValue);

        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var payload = JObject.Parse(response);
        if (string.Equals(payload.Value<string>("error"), "not_found", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var summary = payload.ToObject<StartupRunSummary>();
        if (summary != null && string.IsNullOrWhiteSpace(summary._id))
        {
            summary._id = StartupRunSummaryDocumentId;
        }

        return NormalizeStartupRunSummary(summary);
    }

    private static StartupRunSummary NormalizeStartupRunSummary(StartupRunSummary summary)
    {
        summary ??= new StartupRunSummary();
        summary.configured_tenants ??= new List<string>();
        summary.tenant_statuses ??= new Dictionary<string, StartupRebuildTenantSummary>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(summary._id))
        {
            summary._id = StartupRunSummaryDocumentId;
        }

        return summary;
    }

    private StartupRunSummary GetCachedStartupRunSummary(string summaryHostPrefix)
    {
        if (!StartupRunSummaryCache.TryGet(summaryHostPrefix, out var cachedSummaryPayload))
        {
            return null;
        }

        return NormalizeStartupRunSummary(cachedSummaryPayload.ToObject<StartupRunSummary>());
    }

    private void SetCachedStartupRunSummary(string summaryHostPrefix, StartupRunSummary summary)
    {
        if (string.IsNullOrWhiteSpace(summaryHostPrefix) || summary == null)
        {
            return;
        }

        StartupRunSummaryCache.Set(summaryHostPrefix, JObject.FromObject(NormalizeStartupRunSummary(summary)));
    }

    private StartupRunSummary CreateStartupRunSummary(List<string> configuredTenants, string summaryHostPrefix)
    {
        var summary = new StartupRunSummary
        {
            status = "running",
            metadata_version = _metadataVersion,
            summary_host_prefix = summaryHostPrefix,
            configured_tenants = configuredTenants.ToList(),
            tenant_statuses = new Dictionary<string, StartupRebuildTenantSummary>(StringComparer.OrdinalIgnoreCase),
            started_utc = DateTime.UtcNow.ToString("o"),
            completed_utc = null,
            last_error = null
        };

        foreach (string tenant in configuredTenants)
        {
            summary.tenant_statuses[tenant] = new StartupRebuildTenantSummary
            {
                host_prefix = tenant,
                status = "pending"
            };
        }

        return summary;
    }

    private void UpdateRunSummaryTotals(StartupRunSummary summary, List<string> configuredTenants)
    {
        configuredTenants ??= new List<string>();
        summary.configured_tenants = configuredTenants.ToList();
        summary.total_tenant_count = configuredTenants.Count;
        summary.completed_tenant_count = 0;
        summary.paused_tenant_count = 0;
        summary.running_tenant_count = 0;
        summary.pending_tenant_count = 0;
        summary.total_processed_case_count = 0;
        summary.total_skipped_case_count = 0;
        summary.total_document_error_count = 0;
        summary.total_de_id_bulk_error_count = 0;
        summary.total_report_bulk_error_count = 0;
        summary.total_de_id_doc_count = 0;
        summary.total_report_doc_count = 0;

        foreach (string tenant in configuredTenants)
        {
            if (!summary.tenant_statuses.TryGetValue(tenant, out var tenantSummary) || tenantSummary == null)
            {
                summary.pending_tenant_count++;
                continue;
            }

            summary.total_processed_case_count += tenantSummary.processed_case_count;
            summary.total_skipped_case_count += tenantSummary.skipped_case_count;
            summary.total_document_error_count += tenantSummary.document_error_count;
            summary.total_de_id_bulk_error_count += tenantSummary.de_id_bulk_error_count;
            summary.total_report_bulk_error_count += tenantSummary.report_bulk_error_count;
            summary.total_de_id_doc_count += tenantSummary.total_de_id_doc_count;
            summary.total_report_doc_count += tenantSummary.total_report_doc_count;

            switch (tenantSummary.status?.ToLowerInvariant())
            {
                case "completed":
                    summary.completed_tenant_count++;
                    break;
                case "paused":
                    summary.paused_tenant_count++;
                    break;
                case "running":
                case "queued":
                    summary.running_tenant_count++;
                    break;
                default:
                    summary.pending_tenant_count++;
                    break;
            }
        }

        summary.last_updated_utc = DateTime.UtcNow.ToString("o");
        summary.last_error = summary.tenant_statuses.Values
            .Where(item => item != null && !string.IsNullOrWhiteSpace(item.last_error))
            .Select(item => item.last_error)
            .FirstOrDefault();

        if (summary.total_tenant_count > 0 && summary.completed_tenant_count == summary.total_tenant_count)
        {
            summary.status = "completed";
            summary.completed_utc ??= DateTime.UtcNow.ToString("o");
        }
        else if (summary.running_tenant_count > 0)
        {
            summary.status = "running";
            summary.completed_utc = null;
        }
        else if (summary.paused_tenant_count > 0)
        {
            summary.status = "incomplete";
            summary.completed_utc = null;
        }
        else
        {
            summary.status = "running";
            summary.completed_utc = null;
        }
    }

    private static List<string> BuildEffectiveSummaryTenants(
        StartupRunSummary summary,
        IEnumerable<string> requestConfiguredTenants,
        string currentHostPrefix)
    {
        IEnumerable<string> summaryConfiguredTenants = summary?.configured_tenants ?? Enumerable.Empty<string>();
        IEnumerable<string> summaryTenantKeys = summary?.tenant_statuses?.Keys ?? Enumerable.Empty<string>();
        IEnumerable<string> currentTenant = string.IsNullOrWhiteSpace(currentHostPrefix)
            ? Enumerable.Empty<string>()
            : new[] { currentHostPrefix };

        return DbRebuildSettings.NormalizeTenantListPreservingOrder(
            (requestConfiguredTenants ?? Enumerable.Empty<string>())
                .Concat(summaryConfiguredTenants)
                .Concat(summaryTenantKeys)
                .Concat(currentTenant));
    }

    private void ApplyTenantStateToSummary(
        StartupRunSummary summary,
        List<string> requestConfiguredTenants,
        string currentHostPrefix,
        string summaryHostPrefix,
        StartupRebuildTenantSummary tenantState)
    {
        List<string> effectiveSummaryTenants = BuildEffectiveSummaryTenants(
            summary,
            requestConfiguredTenants,
            currentHostPrefix);

        summary.summary_host_prefix = summaryHostPrefix;
        summary.metadata_version = _metadataVersion;

        var configuredTenantSet = new HashSet<string>(effectiveSummaryTenants, StringComparer.OrdinalIgnoreCase);
        foreach (string staleTenant in summary.tenant_statuses.Keys
            .Where(item => !configuredTenantSet.Contains(item))
            .ToList())
        {
            if (!string.Equals(staleTenant, currentHostPrefix, StringComparison.OrdinalIgnoreCase))
            {
                summary.tenant_statuses.Remove(staleTenant);
            }
        }

        foreach (string tenant in effectiveSummaryTenants)
        {
            if (!summary.tenant_statuses.ContainsKey(tenant))
            {
                summary.tenant_statuses[tenant] = new StartupRebuildTenantSummary
                {
                    host_prefix = tenant,
                    status = "pending"
                };
            }
        }

        if (!summary.tenant_statuses.TryGetValue(currentHostPrefix, out var tenantSummary) || tenantSummary == null)
        {
            tenantSummary = new StartupRebuildTenantSummary
            {
                host_prefix = currentHostPrefix
            };
            summary.tenant_statuses[currentHostPrefix] = tenantSummary;
        }

        tenantSummary.host_prefix = currentHostPrefix;
        tenantSummary.couchdb_url = _couchdbUrl;
        tenantSummary.status = tenantState.status;
        tenantSummary.metadata_version = tenantState.metadata_version;
        tenantSummary.last_processed_id = tenantState.last_processed_id;
        tenantSummary.completed_batch_count = tenantState.completed_batch_count;
        tenantSummary.processed_case_count = tenantState.processed_case_count;
        tenantSummary.skipped_case_count = tenantState.skipped_case_count;
        tenantSummary.document_error_count = tenantState.document_error_count;
        tenantSummary.de_id_bulk_error_count = tenantState.de_id_bulk_error_count;
        tenantSummary.report_bulk_error_count = tenantState.report_bulk_error_count;
        tenantSummary.total_de_id_doc_count = tenantState.total_de_id_doc_count;
        tenantSummary.total_report_doc_count = tenantState.total_report_doc_count;
        tenantSummary.started_utc = tenantState.started_utc;
        tenantSummary.last_updated_utc = tenantState.last_updated_utc;
        tenantSummary.completed_utc = tenantState.completed_utc;
        tenantSummary.last_error = tenantState.last_error;

        UpdateRunSummaryTotals(summary, effectiveSummaryTenants);
    }

    private async Task SaveStartupRunSummaryAsync(
        StartupRunSummary summary,
        List<string> configuredTenants,
        string currentHostPrefix,
        string summaryHostPrefix,
        StartupRebuildTenantSummary tenantState)
    {
        if (summary == null)
        {
            return;
        }

        string summaryBaseUrl = GetStartupRunSummaryBaseUrl();
        string summaryUrl = GetStartupRunSummaryUrl();
        if (string.IsNullOrWhiteSpace(summaryBaseUrl) || string.IsNullOrWhiteSpace(summaryUrl))
        {
            return;
        }

        await EnsureRebuildDatabaseExistsAsync(summaryBaseUrl);

        summary._id = StartupRunSummaryDocumentId;
        summary.last_updated_utc = DateTime.UtcNow.ToString("o");

        for (int attempt = 0; attempt < 3; attempt++)
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
                _userName,
                _userValue);

            if (!string.IsNullOrWhiteSpace(response))
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(response);
                if (result?.ok == true)
                {
                    summary._rev = result.rev;
                    SetCachedStartupRunSummary(summary.summary_host_prefix, summary);
                    return;
                }

                var responsePayload = JObject.Parse(response);
                if (attempt < 2 &&
                    string.Equals(responsePayload.Value<string>("error"), "conflict", StringComparison.OrdinalIgnoreCase))
                {
                    var latestSummary = await TryGetStartupRunSummaryAsync();
                    summary = latestSummary ?? CreateStartupRunSummary(configuredTenants, summaryHostPrefix);
                    ApplyTenantStateToSummary(summary, configuredTenants, currentHostPrefix, summaryHostPrefix, tenantState);
                    summary._rev = latestSummary?._rev;
                    continue;
                }
            }

            System.Console.WriteLine($"Failed to save startup run summary for '{summaryBaseUrl}'. Response: {response ?? "<null>"}");
            break;
        }
    }

    private async Task SyncStartupRunSummaryAsync(
        StartupRebuildTenantSummary tenantState,
        bool forceReset,
        bool persistToDatabase)
    {
        if (tenantState == null)
        {
            return;
        }

        string summaryBaseUrl = GetStartupRunSummaryBaseUrl();
        if (string.IsNullOrWhiteSpace(summaryBaseUrl))
        {
            return;
        }

        List<string> configuredTenants = DbRebuildSettings.NormalizeTenantListPreservingOrder(GetConfiguredTenants());
        string currentHostPrefix = GetEffectiveHostPrefix();
        if (!configuredTenants.Contains(currentHostPrefix, StringComparer.OrdinalIgnoreCase))
        {
            configuredTenants.Add(currentHostPrefix);
        }

        configuredTenants = DbRebuildSettings.NormalizeTenantListPreservingOrder(configuredTenants);

        string summaryHostPrefix = GetSummaryHostPrefix();
        using var summaryUpdateLease = await StartupRunSummaryUpdateGate.AcquireAsync(summaryHostPrefix);

        await EnsureRebuildDatabaseExistsAsync(summaryBaseUrl);

        var summary = forceReset ? null : GetCachedStartupRunSummary(summaryHostPrefix);
        summary ??= await TryGetStartupRunSummaryAsync();

        if (forceReset ||
            summary == null ||
            !string.Equals(summary.metadata_version, _metadataVersion, StringComparison.OrdinalIgnoreCase))
        {
            summary = CreateStartupRunSummary(configuredTenants, summaryHostPrefix);
        }

        ApplyTenantStateToSummary(summary, configuredTenants, currentHostPrefix, summaryHostPrefix, tenantState);
        SetCachedStartupRunSummary(summaryHostPrefix, summary);

        if (persistToDatabase)
        {
            await SaveStartupRunSummaryAsync(summary, configuredTenants, currentHostPrefix, summaryHostPrefix, tenantState);
        }
    }

    private async Task<bool> UrlEndpointExistsAsync(string url)
    {
        try
        {
            await _couchDbHttpClient.ExecuteAsync("HEAD", url, null, _userName, _userValue, throwOnError: true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool ShouldResetStartupRunSummary()
    {
        if (!string.Equals(_rebuildSource, "startup", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string currentHostPrefix = GetEffectiveHostPrefix();
        if (string.Equals(currentHostPrefix, GetSummaryHostPrefix(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(currentHostPrefix, GetConfiguredTenants().FirstOrDefault(), StringComparison.OrdinalIgnoreCase);
    }
}
