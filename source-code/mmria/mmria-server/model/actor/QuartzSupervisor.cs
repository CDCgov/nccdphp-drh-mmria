using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.DependencyInjection;
using mmria.server.model.actor.quartz;

namespace mmria.server.model.actor;

public sealed class ScheduleInfoMessage
{
    public ScheduleInfoMessage
    (
        string p_cron_schedule, 
        string p_couch_db_url,
        string p_db_prefix,
        string p_user_name,
        string p_user_value,
        string p_export_directory,
        string p_jurisdiction_user_name,
        string p_version_number,
        string p_cdc_instance_pull_list
        )
    {
        cron_schedule = p_cron_schedule;
        couch_db_url = p_couch_db_url;
        user_name = p_user_name;
        user_value = p_user_value;
        db_prefix = p_db_prefix;
        export_directory = p_export_directory;
        jurisdiction_user_name = p_jurisdiction_user_name;
        version_number = p_version_number;
        cdc_instance_pull_list  = p_cdc_instance_pull_list;
    }

    public string cron_schedule { get; private set; }
    public string couch_db_url { get; private set; }
    public string db_prefix { get; private set; }
    public string user_name { get; private set; }

    public string jurisdiction_user_name { get; private set; }

    public string version_number { get; private set; }

    public string user_value { get; private set; }
    public string export_directory { get; private set; }

    public string cdc_instance_pull_list { get; private set; }
}


public sealed class QuartzSupervisor : UntypedActor
{
    //private IActorRef checkForChanges = Context.ActorOf(Props.Create<CheckForChanges>(), "CheckForChanges");

    //private ScheduleInfoMessage scheduleInfo = null;
    readonly IServiceScope _scope;

    mmria.common.couchdb.OverridableConfiguration configuration = null;
    mmria.common.couchdb.ConfigurationSet configuration_set;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    string host_prefix;

    public QuartzSupervisor
    (
        mmria.common.couchdb.OverridableConfiguration _configuration,
        string _host_prefix,
        mmria.common.couchdb.ConfigurationSet _configuration_set,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
 

        configuration = _configuration;
        host_prefix = _host_prefix;
        configuration_set = _configuration_set;
        _couchDbHttpClient = couchDbHttpClient;
    }

    protected override void PostStop()
    {
        //_scope.Dispose();
    }
/*
    public static Props Props(ScheduleInfoMessage p_scheduleInfo) => Akka.Actor.Props.Create(() => new QuartzSupervisor(p_scheduleInfo));
*/

    protected override void OnReceive(object message)
    {

        switch (message)
        {
            case "init":

                Console.WriteLine("Quartz Supervisor initialized");
                Console.WriteLine($"[CDC-DEBUG] QuartzSupervisor init for host_prefix='{host_prefix}'");
                break;

            case "pulse":
                Console.WriteLine($"[CDC-DEBUG] QuartzSupervisor pulse received for host_prefix='{host_prefix}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                var db_config = configuration.GetDBConfig(host_prefix);
                var cdcInstancePullList = configuration.GetString("cdc_instance_pull_list", host_prefix);
                var isDbCheckEnabled = configuration.GetBoolean("is_db_check_enabled", host_prefix);

                Console.WriteLine($"[CDC-DEBUG] host_prefix='{host_prefix}', db_config null? {db_config == null}");
                if (db_config != null)
                {
                    Console.WriteLine($"[CDC-DEBUG] db_config.url='{db_config.url}', db_config.prefix='{db_config.prefix}'");
                }
                Console.WriteLine($"[CDC-DEBUG] cdc_instance_pull_list='{cdcInstancePullList}'");
                Console.WriteLine($"[CDC-DEBUG] is_db_check_enabled='{isDbCheckEnabled}'");
                
                if (db_config == null)
                {
                    Console.WriteLine($"[CDC-DEBUG] Breaking pulse processing because db_config is null for host_prefix='{host_prefix}'");
                    break;
                }

                mmria.server.model.actor.ScheduleInfoMessage new_scheduleInfo = new actor.ScheduleInfoMessage
                    (
                        configuration.GetString("cron_schedule", host_prefix),
                        db_config.url,
                        db_config.prefix,
                        db_config.user_name,
                        db_config.user_value,
                        configuration.GetString("export_directory", host_prefix),
                        null, //jurisdiction_user_name,
                        configuration.GetString("metadata_version", host_prefix),
                        cdcInstancePullList
                    );
            

                if
                (
                    isDbCheckEnabled.HasValue && 
                    isDbCheckEnabled.Value
                )
                {
                    Console.WriteLine($"[CDC-DEBUG] Launching Check_DB_Install for host_prefix='{host_prefix}'");
                    Context.ActorOf(Props.Create<Check_DB_Install>(db_config, _couchDbHttpClient)).Tell(new_scheduleInfo);
                }
                
                bool is_rebuild_queue = false;

                var midnight_timespan = new TimeSpan(0, 0, 0);
                var difference = DateTime.Now - midnight_timespan;
                if(difference.Hour == 0 && difference.Minute == 0)
                {
                    is_rebuild_queue = true;
                }

                if(is_rebuild_queue)
                {
                    Console.WriteLine($"[CDC-DEBUG] Launching Rebuild_Export_Queue for host_prefix='{host_prefix}'");
                    Context.ActorOf(Props.Create<Rebuild_Export_Queue>(db_config, _couchDbHttpClient)).Tell(new_scheduleInfo);
                }
                else if(!string.IsNullOrWhiteSpace(cdcInstancePullList))
                {
                    Console.WriteLine($"[CDC-DEBUG] Launching Process_Central_Pull_list for host_prefix='{host_prefix}'");
                    Context.ActorOf(Props.Create<Process_Central_Pull_list>(configuration_set, db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(new_scheduleInfo);
                }
                else
                {
                    Console.WriteLine($"[CDC-DEBUG] Skipping Process_Central_Pull_list for host_prefix='{host_prefix}' because cdc_instance_pull_list is blank.");
                }


                

                

            break;
        }
        
    }

}
 /*
public sealed class CheckForChanges : UntypedActor
{
    //protected override void PreStart() => Console.WriteLine("CheckForChanges started");
    //protected override void PostStop() => Console.WriteLine("CheckForChanges stopped");

    protected override void OnReceive(object message)
    {
            Console.WriteLine($"CheckForChanges {System.DateTime.Now}");

       
        switch (message)
        {
            case WriteFile file:
                //file-data/file-name-directory/hash-name.file
                string new_directory = System.IO.Path.Combine(file.workingdirectory, "file-data", file.filename.Replace(file.monitoreddirectory, ""));
                

                Console.WriteLine($"QuartzWriter.OnRecieve {file.filename} >> {new_directory}");
                if(!System.IO.Directory.Exists(new_directory))
                {
                    System.IO.Directory.CreateDirectory(new_directory);
                }

                string new_path = System.IO.Path.Combine(new_directory, GetHash(file.filename));
                if(!System.IO.File.Exists(new_path))
                {
                    System.IO.File.Copy(file.filename, new_path);
                }
                
                break;

                case RecordFileMessage rfm:
                    Console.WriteLine(rfm.filename);
                    break;
        }

    }

}
*/








                        /*
                    Program.DateOfLastChange_Sequence_Call = new List<DateTime> ();
                    Program.Change_Sequence_Call_Count++;
                    Program.DateOfLastChange_Sequence_Call.Add (DateTime.Now);


                    //StdSchedulerFactory sf = new StdSchedulerFactory ();
                    //Program.sched = sf.GetScheduler ();
                    DateTimeOffset startTime = DateBuilder.NextGivenSecondDate (null, 15);

                    IJobDetail check_for_changes_job = JobBuilder.Create<mmria.server.model.check_for_changes_job> ()
                                                                        .WithIdentity ("check_for_changes_job", "group1")
                                                                        .Build ();

                    string cron_schedule = Program.config_cron_schedule;


                    Program.check_for_changes_job_trigger = (ITrigger)TriggerBuilder.Create ()
                                    .WithIdentity ("check_for_changes_job_trigger", "group1")
                                    .StartAt (startTime)
                                    .WithCronSchedule (cron_schedule)
                                    .Build ();


                    DateTimeOffset? check_for_changes_job_ft = sched.ScheduleJob (check_for_changes_job, Program.check_for_changes_job_trigger);



                    IJobDetail rebuild_queue_job = JobBuilder.Create<mmria.server.model.rebuild_queue_job> ()
                                                                    .WithIdentity ("rebuild_queue_job", "group2")
                                                                    .Build ();

                    string rebuild_queue_job_cron_schedule = "0 0 0 * * ?";// at midnight every 24 hours


                    Program.rebuild_queue_job_trigger = (ITrigger)TriggerBuilder.Create ()
                                    .WithIdentity ("rebuild_queue_job_trigger", "group2")
                                    .StartAt (startTime)
                                    .WithCronSchedule (rebuild_queue_job_cron_schedule)
                                    .Build ();


                    DateTimeOffset? rebuild_queue_job_ft = sched.ScheduleJob (rebuild_queue_job, Program.rebuild_queue_job_trigger);
                     */
