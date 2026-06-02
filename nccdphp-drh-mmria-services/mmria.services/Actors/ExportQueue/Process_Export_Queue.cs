using System;
using System.Collections.Generic;
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
            System.Console.WriteLine($"[EXPORT-QUEUE] actor start url='{db_config.url}' prefix='{db_config.prefix}'");
            var __export_queue_sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using var serviceScope = _serviceScopeFactory.CreateScope();
                var exportQueueManager = serviceScope.ServiceProvider.GetRequiredService<ExportQueueManager>();
                var couchDbHttpClient = serviceScope.ServiceProvider.GetRequiredService<CouchDbHttpClient>();

                await Process_Export_Queue_Item (scheduleInfoMessage, exportQueueManager, couchDbHttpClient);
            }
            catch(Exception ex)
            {
                // to nothing for now
                System.Console.WriteLine ("[EXPORT-QUEUE] error url='{0}' prefix='{1}' Process_Export_Queue_Item: {2}", db_config.url, db_config.prefix, ex);

            }

            try
            {
                using var serviceScope = _serviceScopeFactory.CreateScope();
                var exportQueueManager = serviceScope.ServiceProvider.GetRequiredService<ExportQueueManager>();

                await Process_Export_Queue_Delete (scheduleInfoMessage, exportQueueManager);
            }
            catch(Exception ex)
            {
                // to nothing for now
                System.Console.WriteLine ("[EXPORT-QUEUE] error url='{0}' prefix='{1}' Process_Export_Queue_Delete: {2}", db_config.url, db_config.prefix, ex);

            }

            System.Console.WriteLine($"[EXPORT-QUEUE] tick complete url='{db_config.url}' prefix='{db_config.prefix}' elapsed_ms={__export_queue_sw.ElapsedMilliseconds}");

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


    public async System.Threading.Tasks.Task Process_Export_Queue_Item (
        ScheduleInfoMessage scheduleInfoMessage,
        ExportQueueManager exportQueueManager,
        CouchDbHttpClient couchDbHttpClient)
    {
        //System.Console.WriteLine ("{0} check_for_changes_job.Process_Export_Queue_Item: started", System.DateTime.Now);

        ExportQueueItem item_to_process = await exportQueueManager.GetNextQueuedServiceItemAsync(db_config);

        if (item_to_process != null)
        {
            System.Console.WriteLine($"[EXPORT-QUEUE] processing item url='{db_config.url}' prefix='{db_config.prefix}' id='{item_to_process._id}'");
            EnsureQueueItemStorageNames(item_to_process);

            async System.Threading.Tasks.Task write_error(ExportQueueItem i, Exception e)
            {
                try
                {
                    await exportQueueManager.MarkExportErrorAsync(i._id, e, db_config);
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine (ex);
                }
            }

            item_to_process.date_last_updated = new DateTime?();
            //item_to_process.last_updated_by = g_uid;


            List<string> args = new List<string>();
            args.Add("exporter:exporter");
            args.Add("user_name:" + scheduleInfoMessage.user_name);
            args.Add("password:" + scheduleInfoMessage.user_value);
            args.Add("database_url:" + scheduleInfoMessage.couch_db_url);
            args.Add ("item_file_name:" + item_to_process.file_name);
            args.Add ("item_id:" + item_to_process._id);
            args.Add ("juris_user_name:" + scheduleInfoMessage.jurisdiction_user_name);


            if 
            (
                item_to_process.export_type.StartsWith ("core csv", StringComparison.OrdinalIgnoreCase) ||
                item_to_process.export_type.StartsWith ("core xlsx", StringComparison.OrdinalIgnoreCase)
            )
            {
                await exportQueueManager.MarkCreatingAsync(item_to_process, db_config);

                try
                {
                
                    mmria.services.Utilities.CoreElementExport.core_element_exporter core_element_exporter = new mmria.services.Utilities.CoreElementExport.core_element_exporter(scheduleInfoMessage, couchDbHttpClient, exportQueueManager);
                    await core_element_exporter.Execute(item_to_process);
                }
                catch(Exception ex)
                {

                    await write_error(item_to_process, ex);
                    System.Console.WriteLine (ex);
                }

            
            }
            else if
            (
                item_to_process.export_type.StartsWith ("all csv", StringComparison.OrdinalIgnoreCase) ||
                item_to_process.export_type.StartsWith ("all xlsx", StringComparison.OrdinalIgnoreCase)
            )
            {
                await exportQueueManager.MarkCreatingAsync(item_to_process, db_config);

                try
                {
                    mmria.services.Utilities.Exporter.mmrds_exporter mmrds_exporter = new mmria.services.Utilities.Exporter.mmrds_exporter(scheduleInfoMessage, couchDbHttpClient, exportQueueManager);
                    if(!await mmrds_exporter.Execute(item_to_process))
                    {
                        System.Console.WriteLine ("exporter failed to finish");
                    }
                }
                catch(Exception ex)
                {
                    await write_error(item_to_process, ex);
                    System.Console.WriteLine (ex);
                }

            }
            else if (item_to_process.export_type.StartsWith ("cdc csv", StringComparison.OrdinalIgnoreCase)) 
            {
                await exportQueueManager.MarkCreatingAsync(item_to_process, db_config);
                args.Add ("is_cdc_de_identified:true");

                try
                {
                    mmria.services.Utilities.Exporter.mmrds_exporter mmrds_exporter = new mmria.services.Utilities.Exporter.mmrds_exporter (scheduleInfoMessage, couchDbHttpClient, exportQueueManager);
                    //mmrds_exporter.Execute (item_to_process);
                    if(!await mmrds_exporter.Execute(item_to_process))
                    {
                        System.Console.WriteLine ("exporter failed to finish");
                    }
                }
                catch(Exception ex)
                {
                    await write_error(item_to_process, ex);
                    System.Console.WriteLine (ex);
                }


            }
            else 
            {
                await exportQueueManager.MarkCreatingAsync(item_to_process, db_config);
                args.Add ("is_cdc_de_identified:true");

                try
                {
                    mmria.services.Utilities.Exporter.exporter custom_exporter = new mmria.services.Utilities.Exporter.exporter (scheduleInfoMessage, couchDbHttpClient, exportQueueManager);
                    //mmrds_exporter.Execute (item_to_process);
                    if(!await custom_exporter.Execute(item_to_process))
                    {
                        await write_error(item_to_process, new Exception("exporter failed to finish"));
                        System.Console.WriteLine ("exporter failed to finish");
                    }
                }
                catch(Exception ex)
                {
                    await write_error(item_to_process, ex);
                    System.Console.WriteLine (ex);
                }
            }

        }

    }


    public async System.Threading.Tasks.Task Process_Export_Queue_Delete (
        ScheduleInfoMessage scheduleInfoMessage,
        ExportQueueManager exportQueueManager)
    {
        //System.Console.WriteLine ("{0} check_for_changes_job.Process_Export_Queue_Delete: started", System.DateTime.Now);

        ExportQueueItem item_to_process = await exportQueueManager.GetNextDeletedServiceItemAsync(db_config);
        if (item_to_process != null)
        {
            try
            {
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
                    System.Console.WriteLine ("check_for_changes_job.Process_Export_Queue_Delete: Unable to Delete Directory {0}", item_directory_name);
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
                    System.Console.WriteLine ("Program.Process_Export_Queue_Delete: Unable to Delete File {0}", storage_file_name);
                }

                await exportQueueManager.MarkExpungedAsync(item_to_process, db_config);
            }
            catch(Exception)
            {
                // do nothing for now
            }

        }

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
