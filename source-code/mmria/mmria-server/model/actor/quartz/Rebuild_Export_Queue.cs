using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using mmria.server.model.actor;
using mmria.common.SharedLibraries.ExportQueue;

namespace mmria.server.model.actor.quartz;

public sealed class Rebuild_Export_Queue : ReceiveActor
{
    //protected override void PreStart() => Console.WriteLine("Rebuild_Export_Queue started");
    //protected override void PostStop() => Console.WriteLine("Rebuild_Export_Queue stopped");
    private readonly mmria.common.couchdb.DBConfigurationDetail _dbConfig;
    private readonly IExportQueueRepository _exportQueueRepository;

    public Rebuild_Export_Queue
    (
        mmria.common.couchdb.DBConfigurationDetail dbConfig,
        IExportQueueRepository exportQueueRepository
    )
    {
        _dbConfig = dbConfig;
        _exportQueueRepository = exportQueueRepository;
        
        ReceiveAsync<ScheduleInfoMessage>(async scheduleInfo =>
        {
            //Console.WriteLine($"Rebuild_Export_Queue Baby {System.DateTime.Now}");
 
            var midnight_timespan = new TimeSpan(0, 0, 0);
            var difference = DateTime.Now - midnight_timespan;
            if(difference.Hour != 0 && difference.Minute != 0)
            {
                Context.Stop(Self);
                return;
            }
            /*
            try 
            {
                Program.PauseSchedule (); 
            }
            catch (Exception ex) 
            {
                System.Console.WriteLine ($"rebuild_queue_job. error pausing schedule\n{ex}");
            }
            */

            try 
            {
                string export_directory = scheduleInfo.export_directory;

                if (System.IO.Directory.Exists (export_directory))
                {
                    RecursiveDirectoryDelete(new System.IO.DirectoryInfo(export_directory));
                }

                System.IO.Directory.CreateDirectory(export_directory);


            }
            catch (Exception ex) 
            {
                System.Console.WriteLine ($"rebuild_queue_job. error deleting directory queue\n{ex}");
            }

            try 
            {
                await _exportQueueRepository.PurgeAndReinitializeAsync(_dbConfig);
            }
            catch (Exception ex) 
            {
                System.Console.WriteLine ($"rebuild_queue_job. error creating queue\n{ex}");
            }

/*

            try 
            {
                Program.ResumeSchedule (); 
            }
            catch (Exception ex) 
            {
                System.Console.WriteLine ($"rebuild_queue_job. error resuming schedule\n{ex}");
            }
*/

            Context.Stop(Self);
        });
    }

    private void RecursiveDirectoryDelete(System.IO.DirectoryInfo baseDir)
    {
        if (!baseDir.Exists)
            return;

        foreach (var dir in baseDir.EnumerateDirectories())
        {
            RecursiveDirectoryDelete(dir);
        }
        baseDir.Delete(true);
    }

}
