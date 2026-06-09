using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using mmria.common.getset;
using Microsoft.Extensions.DependencyInjection;
using mmria.common.SharedLibraries.ExportQueue.Manager;
using mmria.common.SharedLibraries.ExportQueue.Model;
using mmria.common.SharedLibraries.Security.FileSystem;
using mmria.services.Models;

namespace mmria.services.ExportQueue;

public sealed class Process_Export_Queue : ReceiveActor
{
    private static readonly TimeSpan ExportHeartbeatInterval = TimeSpan.FromSeconds(60);
    //protected override void PreStart() => Console.WriteLine("Process_Export_Queue started");
    //protected override void PostStop() => Console.WriteLine("Process_Export_Queue stopped");

	mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public Process_Export_Queue
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        IServiceScopeFactory serviceScopeFactory
    )
    {
        db_config = _db_config;
        _serviceScopeFactory = serviceScopeFactory;

        ReceiveAsync<ScheduleInfoMessage>(async scheduleInfoMessage =>
        {
            //Console.WriteLine($"Process_Export_Queue {System.DateTime.Now}");

            //System.Console.WriteLine ("{0} Beginning Export Queue Item Processing", System.DateTime.Now);
            System.Console.WriteLine($"[EXPORT-QUEUE] actor start request_id='{scheduleInfoMessage.request_id}' tenant='{scheduleInfoMessage.tenant}' queue_id='' requested_queue_id='{scheduleInfoMessage.requested_queue_item_id}' url='{db_config.url}' prefix='{db_config.prefix}' terminal_status='started'");
            var __export_queue_sw = System.Diagnostics.Stopwatch.StartNew();
            var itemTerminalStatus = "not_started";
            var deleteTerminalStatus = "not_started";

            try
            {
                using var serviceScope = _serviceScopeFactory.CreateScope();
                var exportQueueManager = serviceScope.ServiceProvider.GetRequiredService<ExportQueueManager>();
                var couchDbHttpClient = serviceScope.ServiceProvider.GetRequiredService<CouchDbHttpClient>();

                itemTerminalStatus = await Process_Export_Queue_Item (scheduleInfoMessage, exportQueueManager, couchDbHttpClient);
            }
            catch(Exception ex)
            {
                // to nothing for now
                itemTerminalStatus = "item_error";
                System.Console.WriteLine ("[EXPORT-QUEUE] error request_id='{0}' tenant='{1}' queue_id='' requested_queue_id='{2}' url='{3}' prefix='{4}' terminal_status='item_error' Process_Export_Queue_Item: {5}", scheduleInfoMessage.request_id, scheduleInfoMessage.tenant, scheduleInfoMessage.requested_queue_item_id, db_config.url, db_config.prefix, ex);

            }

            try
            {
                using var serviceScope = _serviceScopeFactory.CreateScope();
                var exportQueueManager = serviceScope.ServiceProvider.GetRequiredService<ExportQueueManager>();

                deleteTerminalStatus = await Process_Export_Queue_Delete (scheduleInfoMessage, exportQueueManager);
            }
            catch(Exception ex)
            {
                // to nothing for now
                deleteTerminalStatus = "delete_error";
                System.Console.WriteLine ("[EXPORT-QUEUE] error request_id='{0}' tenant='{1}' queue_id='' requested_queue_id='{2}' url='{3}' prefix='{4}' terminal_status='delete_error' Process_Export_Queue_Delete: {5}", scheduleInfoMessage.request_id, scheduleInfoMessage.tenant, scheduleInfoMessage.requested_queue_item_id, db_config.url, db_config.prefix, ex);

            }

            System.Console.WriteLine($"[EXPORT-QUEUE] tick complete request_id='{scheduleInfoMessage.request_id}' tenant='{scheduleInfoMessage.tenant}' queue_id='' requested_queue_id='{scheduleInfoMessage.requested_queue_item_id}' item_terminal_status='{itemTerminalStatus}' delete_terminal_status='{deleteTerminalStatus}' url='{db_config.url}' prefix='{db_config.prefix}' elapsed_ms={__export_queue_sw.ElapsedMilliseconds} terminal_status='tick_complete'");

            Context.Stop(this.Self);
        });
    }

    private static void EnsureQueueItemStorageNames(ExportQueueItem queueItem)
    {
        var publicFileName = ContainedFileStore.ValidateContainedName(queueItem.file_name, nameof(queueItem.file_name));

        if (string.IsNullOrWhiteSpace(queueItem.storage_file_name))
        {
            queueItem.storage_file_name = ContainedFileStore.CreateGeneratedArtifactName(
                "export",
                System.IO.Path.GetExtension(publicFileName));
        }
        else
        {
            queueItem.storage_file_name = ContainedFileStore.ValidateContainedName(
                queueItem.storage_file_name,
                nameof(queueItem.storage_file_name));
        }

        if (string.IsNullOrWhiteSpace(queueItem.storage_directory_name))
        {
            queueItem.storage_directory_name = ContainedFileStore.CreateSafeContainedName(
                System.IO.Path.GetFileNameWithoutExtension(queueItem.storage_file_name),
                "export-work");
        }
        else
        {
            queueItem.storage_directory_name = ContainedFileStore.ValidateContainedName(
                queueItem.storage_directory_name,
                nameof(queueItem.storage_directory_name));
        }
    }

    private async Task RunWithHeartbeatAsync(
        ScheduleInfoMessage scheduleInfoMessage,
        ExportQueueItem item,
        ExportQueueManager exportQueueManager,
        Func<Task> exportWork)
    {
        using var heartbeatCancellation = new CancellationTokenSource();
        var heartbeatTask = RunHeartbeatAsync(
            scheduleInfoMessage,
            item._id,
            exportQueueManager,
            heartbeatCancellation.Token);

        try
        {
            await exportWork();
        }
        finally
        {
            heartbeatCancellation.Cancel();
            try
            {
                await heartbeatTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task RunHeartbeatAsync(
        ScheduleInfoMessage scheduleInfoMessage,
        string queueItemId,
        ExportQueueManager exportQueueManager,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(ExportHeartbeatInterval, cancellationToken);

            try
            {
                var touched = await exportQueueManager.TouchCreatingHeartbeatAsync(queueItemId, db_config);
                if (!touched)
                {
                    return;
                }

                System.Console.WriteLine($"[EXPORT-QUEUE] heartbeat request_id='{scheduleInfoMessage.request_id}' tenant='{scheduleInfoMessage.tenant}' queue_id='{queueItemId}' requested_queue_id='{scheduleInfoMessage.requested_queue_item_id}' terminal_status='heartbeat'");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[EXPORT-QUEUE] heartbeat failed request_id='{scheduleInfoMessage.request_id}' tenant='{scheduleInfoMessage.tenant}' queue_id='{queueItemId}' requested_queue_id='{scheduleInfoMessage.requested_queue_item_id}' error='{ex.Message}' terminal_status='heartbeat_failed'");
            }
        }
    }

    private async Task<string> EnsureTerminalStatusAsync(
        ExportQueueItem item,
        ExportQueueManager exportQueueManager)
    {
        var latestItem = await exportQueueManager.GetQueueItemAsync(item._id, db_config);
        var latestStatus = latestItem?.status;

        if (!string.IsNullOrWhiteSpace(latestStatus) &&
            latestStatus.StartsWith("Creating Export...", StringComparison.OrdinalIgnoreCase))
        {
            await exportQueueManager.MarkExportErrorAsync(
                item._id,
                new InvalidOperationException("exporter finished without completing queue item"),
                db_config);

            return "Export error... exporter finished without completing queue item";
        }

        return string.IsNullOrWhiteSpace(latestStatus) ? "unknown" : latestStatus;
    }


    public async Task<string> Process_Export_Queue_Item (
        ScheduleInfoMessage scheduleInfoMessage,
        ExportQueueManager exportQueueManager,
        CouchDbHttpClient couchDbHttpClient)
    {
        //System.Console.WriteLine ("{0} check_for_changes_job.Process_Export_Queue_Item: started", System.DateTime.Now);

        ExportQueueItem item_to_process = await exportQueueManager.GetNextQueuedServiceItemAsync(db_config);

        if (item_to_process == null)
        {
            System.Console.WriteLine($"[EXPORT-QUEUE] no queued item request_id='{scheduleInfoMessage.request_id}' tenant='{scheduleInfoMessage.tenant}' queue_id='' requested_queue_id='{scheduleInfoMessage.requested_queue_item_id}' terminal_status='no_queued_item'");
            return "no_queued_item";
        }

        System.Console.WriteLine($"[EXPORT-QUEUE] processing item request_id='{scheduleInfoMessage.request_id}' tenant='{scheduleInfoMessage.tenant}' queue_id='{item_to_process._id}' requested_queue_id='{scheduleInfoMessage.requested_queue_item_id}' url='{db_config.url}' prefix='{db_config.prefix}' terminal_status='processing'");
        EnsureQueueItemStorageNames(item_to_process);

        async Task write_error(ExportQueueItem i, Exception e)
        {
            try
            {
                await exportQueueManager.MarkExportErrorAsync(i._id, e, db_config);
            }
            catch(Exception ex)
            {
                System.Console.WriteLine($"[EXPORT-QUEUE] export error status write failed request_id='{scheduleInfoMessage.request_id}' tenant='{scheduleInfoMessage.tenant}' queue_id='{i?._id}' requested_queue_id='{scheduleInfoMessage.requested_queue_item_id}' error='{ex.Message}' terminal_status='error_status_write_failed'");
            }
        }

        var exportType = item_to_process.export_type ?? string.Empty;

        try
        {
            await exportQueueManager.MarkCreatingAsync(item_to_process, db_config);

            if 
            (
                exportType.StartsWith ("core csv", StringComparison.OrdinalIgnoreCase) ||
                exportType.StartsWith ("core xlsx", StringComparison.OrdinalIgnoreCase)
            )
            {
                await RunWithHeartbeatAsync(
                    scheduleInfoMessage,
                    item_to_process,
                    exportQueueManager,
                    async () =>
                    {
                        var core_element_exporter = new mmria.services.Utilities.CoreElementExport.core_element_exporter(scheduleInfoMessage, couchDbHttpClient, exportQueueManager);
                        await core_element_exporter.Execute(item_to_process);
                    });
            }
            else if
            (
                exportType.StartsWith ("all csv", StringComparison.OrdinalIgnoreCase) ||
                exportType.StartsWith ("all xlsx", StringComparison.OrdinalIgnoreCase)
            )
            {
                await RunWithHeartbeatAsync(
                    scheduleInfoMessage,
                    item_to_process,
                    exportQueueManager,
                    async () =>
                    {
                        var mmrds_exporter = new mmria.services.Utilities.Exporter.mmrds_exporter(scheduleInfoMessage, couchDbHttpClient, exportQueueManager);
                        if(!await mmrds_exporter.Execute(item_to_process))
                        {
                            throw new InvalidOperationException("exporter failed to finish");
                        }
                    });
            }
            else if (exportType.StartsWith ("cdc csv", StringComparison.OrdinalIgnoreCase))
            {
                await RunWithHeartbeatAsync(
                    scheduleInfoMessage,
                    item_to_process,
                    exportQueueManager,
                    async () =>
                    {
                        var mmrds_exporter = new mmria.services.Utilities.Exporter.mmrds_exporter (scheduleInfoMessage, couchDbHttpClient, exportQueueManager);
                        if(!await mmrds_exporter.Execute(item_to_process))
                        {
                            throw new InvalidOperationException("exporter failed to finish");
                        }
                    });
            }
            else 
            {
                await RunWithHeartbeatAsync(
                    scheduleInfoMessage,
                    item_to_process,
                    exportQueueManager,
                    async () =>
                    {
                        var custom_exporter = new mmria.services.Utilities.Exporter.exporter (scheduleInfoMessage, couchDbHttpClient, exportQueueManager);
                        if(!await custom_exporter.Execute(item_to_process))
                        {
                            throw new InvalidOperationException("exporter failed to finish");
                        }
                    });
            }

            var terminalStatus = await EnsureTerminalStatusAsync(item_to_process, exportQueueManager);
            System.Console.WriteLine($"[EXPORT-QUEUE] processing complete request_id='{scheduleInfoMessage.request_id}' tenant='{scheduleInfoMessage.tenant}' queue_id='{item_to_process._id}' requested_queue_id='{scheduleInfoMessage.requested_queue_item_id}' status='{terminalStatus}' terminal_status='{terminalStatus}'");
            return terminalStatus;
        }
        catch(Exception ex)
        {
            await write_error(item_to_process, ex);
            System.Console.WriteLine($"[EXPORT-QUEUE] processing error request_id='{scheduleInfoMessage.request_id}' tenant='{scheduleInfoMessage.tenant}' queue_id='{item_to_process._id}' requested_queue_id='{scheduleInfoMessage.requested_queue_item_id}' error='{ex.Message}' terminal_status='export_error'");
            return "export_error";
        }

    }


    public async Task<string> Process_Export_Queue_Delete (
        ScheduleInfoMessage scheduleInfoMessage,
        ExportQueueManager exportQueueManager)
    {
        //System.Console.WriteLine ("{0} check_for_changes_job.Process_Export_Queue_Delete: started", System.DateTime.Now);

        ExportQueueItem item_to_process = await exportQueueManager.GetNextDeletedServiceItemAsync(db_config);
        if (item_to_process == null)
        {
            return "no_deleted_item";
        }

        if (item_to_process != null)
        {
            try
            {
                System.Console.WriteLine($"[EXPORT-QUEUE] delete processing request_id='{scheduleInfoMessage.request_id}' tenant='{scheduleInfoMessage.tenant}' queue_id='{item_to_process._id}' requested_queue_id='{scheduleInfoMessage.requested_queue_item_id}' terminal_status='delete_processing'");

                var item_directory_name = !string.IsNullOrWhiteSpace(item_to_process.storage_directory_name)
                    ? item_to_process.storage_directory_name
                    : ContainedFileStore.CreateSafeContainedName(
                        System.IO.Path.GetFileNameWithoutExtension(ContainedFileStore.ValidateContainedName(item_to_process.file_name, nameof(item_to_process.file_name))),
                        "export-work");

                try
                {
                    ContainedFileStore.DeleteExistingDirectoryByName(scheduleInfoMessage.export_directory, item_directory_name, true);
                }
                catch(Exception)
                {
                    // do nothing for now
                    System.Console.WriteLine ("[EXPORT-QUEUE] delete directory failed request_id='{0}' tenant='{1}' queue_id='{2}' requested_queue_id='{3}' directory='{4}' terminal_status='delete_directory_failed'", scheduleInfoMessage.request_id, scheduleInfoMessage.tenant, item_to_process._id, scheduleInfoMessage.requested_queue_item_id, item_directory_name);
                }

                var storage_file_name = !string.IsNullOrWhiteSpace(item_to_process.storage_file_name)
                    ? item_to_process.storage_file_name
                    : ContainedFileStore.ValidateContainedName(item_to_process.file_name, nameof(item_to_process.file_name));

                try
                {
                    if (!ContainedFileStore.DeleteExistingFileByName(scheduleInfoMessage.export_directory, storage_file_name) &&
                        !string.Equals(storage_file_name, item_to_process.file_name, StringComparison.OrdinalIgnoreCase))
                    {
                        ContainedFileStore.DeleteExistingFileByName(scheduleInfoMessage.export_directory, item_to_process.file_name);
                    }
                }
                catch(Exception)
                {
                    // do nothing for now
                    System.Console.WriteLine ("[EXPORT-QUEUE] delete file failed request_id='{0}' tenant='{1}' queue_id='{2}' requested_queue_id='{3}' file='{4}' terminal_status='delete_file_failed'", scheduleInfoMessage.request_id, scheduleInfoMessage.tenant, item_to_process._id, scheduleInfoMessage.requested_queue_item_id, storage_file_name);
                }

                await exportQueueManager.MarkExpungedAsync(item_to_process, db_config);
                System.Console.WriteLine($"[EXPORT-QUEUE] delete complete request_id='{scheduleInfoMessage.request_id}' tenant='{scheduleInfoMessage.tenant}' queue_id='{item_to_process._id}' requested_queue_id='{scheduleInfoMessage.requested_queue_item_id}' terminal_status='expunged'");
                return "expunged";
            }
            catch(Exception ex)
            {
                // do nothing for now
                System.Console.WriteLine($"[EXPORT-QUEUE] delete error request_id='{scheduleInfoMessage.request_id}' tenant='{scheduleInfoMessage.tenant}' queue_id='{item_to_process._id}' requested_queue_id='{scheduleInfoMessage.requested_queue_item_id}' error='{ex.Message}' terminal_status='delete_error'");
                return "delete_error";
            }

        }

        return "no_deleted_item";
    }

    /*
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                maxNrOfRetries: 0,
                withinTimeRange: TimeSpan.FromMinutes(0),
                localOnlyDecider: OnError
                );
        }

        Directive OnError(Exception ex)
        {
            var result = ex switch
            {
                ArgumentException ae => Directive.Resume,
                NullReferenceException ne => Directive.Restart,
                _ => Directive.Stop
            };
            
            return result;
        }
    */
}
