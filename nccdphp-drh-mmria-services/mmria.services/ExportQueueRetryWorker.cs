using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mmria.common.SharedLibraries.ExportQueue.Manager;
using mmria.services.Models;

namespace mmria.services.vitalsimport;

public sealed class ExportQueueRetryWorker : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan MinimumQueuedAge = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FreshCreatingWindow = TimeSpan.FromMinutes(10);

    private readonly ILogger<ExportQueueRetryWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ActorSystem _actorSystem;
    private readonly mmria.common.couchdb.ConfigurationSet _configurationSet;
    private readonly SemaphoreSlim _scanLock = new SemaphoreSlim(1, 1);

    public ExportQueueRetryWorker(
        ILogger<ExportQueueRetryWorker> logger,
        IServiceScopeFactory serviceScopeFactory,
        ActorSystem actorSystem,
        mmria.common.couchdb.ConfigurationSet configurationSet)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _actorSystem = actorSystem;
        _configurationSet = configurationSet;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Export queue retry worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunScanAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXPORT-QUEUE] retry worker tick failed error='{ex.Message}' terminal_status='retry_tick_failed'");
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }
    }

    private async Task RunScanAsync(CancellationToken cancellationToken)
    {
        if (!_scanLock.Wait(0))
        {
            Console.WriteLine("[EXPORT-QUEUE] retry worker skipped overlapping tick terminal_status='retry_tick_skipped'");
            return;
        }

        try
        {
            if (_configurationSet?.detail_list == null)
            {
                return;
            }

            foreach (var tenantConfig in _configurationSet.detail_list)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessTenantAsync(tenantConfig.Key, tenantConfig.Value, cancellationToken);
            }
        }
        finally
        {
            _scanLock.Release();
        }
    }

    private async Task ProcessTenantAsync(
        string tenant,
        mmria.common.couchdb.DBConfigurationDetail tenantDbInfo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenant) || tenantDbInfo == null)
        {
            return;
        }

        var dbConfig = CreateQueueDbConfig(tenantDbInfo);
        var nowUtc = DateTime.UtcNow;
        var freshCreatingCutoffUtc = nowUtc.Subtract(FreshCreatingWindow);
        var queuedCutoffUtc = nowUtc.Subtract(MinimumQueuedAge);

        try
        {
            using var serviceScope = _serviceScopeFactory.CreateScope();
            var exportQueueManager = serviceScope.ServiceProvider.GetRequiredService<ExportQueueManager>();

            var freshCreating = await exportQueueManager.GetFreshCreatingExportAsync(dbConfig, freshCreatingCutoffUtc);
            if (freshCreating != null)
            {
                Console.WriteLine($"[EXPORT-QUEUE] retry skipped fresh creating request_id='' tenant='{tenant}' queue_id='{freshCreating._id}' requested_queue_id='' status='{freshCreating.status}' terminal_status='retry_skipped_fresh_creating'");
                return;
            }

            var staleCreating = await exportQueueManager.GetOldestStaleCreatingExportAsync(dbConfig, freshCreatingCutoffUtc);
            if (staleCreating != null)
            {
                await exportQueueManager.MarkStaleCreatingExportErrorAsync(staleCreating._id, dbConfig);
                Console.WriteLine($"[EXPORT-QUEUE] stale creating marked error request_id='' tenant='{tenant}' queue_id='{staleCreating._id}' requested_queue_id='' status='{staleCreating.status}' terminal_status='stale_creating_error'");
            }

            var queuedItem = await exportQueueManager.GetNextQueuedServiceItemOlderThanAsync(dbConfig, queuedCutoffUtc);
            if (queuedItem == null)
            {
                return;
            }

            var requestId = Guid.NewGuid().ToString("N");
            var scheduleInfo = CreateScheduleInfo(
                tenant,
                tenantDbInfo,
                queuedItem.created_by,
                requestId,
                queuedItem._id);

            var actor = _actorSystem.ActorOf(
                Props.Create<mmria.services.ExportQueue.Process_Export_Queue>(dbConfig, _serviceScopeFactory));
            actor.Tell(scheduleInfo);

            Console.WriteLine($"[EXPORT-QUEUE] retry triggered request_id='{requestId}' tenant='{tenant}' queue_id='{queuedItem._id}' requested_queue_id='{queuedItem._id}' status='{queuedItem.status}' terminal_status='retry_triggered'");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EXPORT-QUEUE] retry tenant failed request_id='' tenant='{tenant}' queue_id='' requested_queue_id='' error='{ex.Message}' terminal_status='retry_tenant_failed'");
        }
    }

    private ScheduleInfoMessage CreateScheduleInfo(
        string tenant,
        mmria.common.couchdb.DBConfigurationDetail tenantDbInfo,
        string jurisdictionUserName,
        string requestId,
        string requestedQueueItemId)
    {
        return new ScheduleInfoMessage
        (
            GetSetting("cron_schedule"),
            tenantDbInfo.url,
            "",
            tenantDbInfo.user_name,
            tenantDbInfo.user_value,
            GetSetting("export_directory", "/workspace/export"),
            string.IsNullOrWhiteSpace(jurisdictionUserName) ? "mmria-services" : jurisdictionUserName,
            GetSetting("metadata_version"),
            GetSetting("cdc_instance_pull_list"),
            requestId,
            requestedQueueItemId,
            tenant
        );
    }

    private string GetSetting(string key, string defaultValue = "")
    {
        if (_configurationSet?.name_value != null &&
            _configurationSet.name_value.TryGetValue(key, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return defaultValue;
    }

    private static mmria.common.couchdb.DBConfigurationDetail CreateQueueDbConfig(
        mmria.common.couchdb.DBConfigurationDetail tenantDbInfo)
    {
        return new mmria.common.couchdb.DBConfigurationDetail
        {
            url = tenantDbInfo.url,
            prefix = "",
            user_name = tenantDbInfo.user_name,
            user_value = tenantDbInfo.user_value
        };
    }
}
