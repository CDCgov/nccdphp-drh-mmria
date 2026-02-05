using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using mmria.server.model.actor;

namespace mmria.server.model.actor.quartz;

public sealed class Rebuild_Export_Queue : ReceiveActor
{
    //protected override void PreStart() => Console.WriteLine("Rebuild_Export_Queue started");
    //protected override void PostStop() => Console.WriteLine("Rebuild_Export_Queue stopped");
    private readonly mmria.common.couchdb.DBConfigurationDetail _dbConfig;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public Rebuild_Export_Queue
    (
        mmria.common.couchdb.DBConfigurationDetail dbConfig,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _dbConfig = dbConfig;
        _couchDbHttpClient = couchDbHttpClient;
        
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


            if (await url_endpoint_exists (_dbConfig.url + $"/{_dbConfig.prefix}export_queue", scheduleInfo.user_name, scheduleInfo.user_value)) 
            {
                System.Console.WriteLine (await _couchDbHttpClient.ExecuteAsync("DELETE", _dbConfig.url + $"/{_dbConfig.prefix}export_queue", null, scheduleInfo.user_name, scheduleInfo.user_value, "application/json"));
            }


            try 
            {
                System.Console.WriteLine ("Creating export_queue db.");
                System.Console.WriteLine (await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + $"/{_dbConfig.prefix}export_queue", null, scheduleInfo.user_name, scheduleInfo.user_value, "application/json"));
                await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + $"/{_dbConfig.prefix}export_queue/_security", "{\"admins\":{\"names\":[],\"roles\":[\"abstractor\"]},\"members\":{\"names\":[],\"roles\":[\"abstractor\"]}}", scheduleInfo.user_name, scheduleInfo.user_value, "application/json");

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

    private async System.Threading.Tasks.Task<bool> url_endpoint_exists (string p_target_server, string p_user_name, string p_value, string p_method = "HEAD")
    {
        bool result = false;

        try 
        {
            await _couchDbHttpClient.ExecuteAsync(p_method, p_target_server, null, p_user_name, p_value, "application/json");
            /*
            HTTP/1.1 200 OK
            Cache-Control: must-revalidate
            Content-Type: application/json
            Date: Mon, 12 Aug 2013 01:27:41 GMT
            Server: CouchDB (Erlang/OTP)*/
            result = true;
        } 
        catch (Exception) 
        {
            // do nothing for now
        }


        return result;
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
