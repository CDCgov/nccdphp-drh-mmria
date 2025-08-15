using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;


namespace mmria.server.model.actor;

public sealed class ScheduleInfoMessage
{
    public ScheduleInfoMessage
    (
        string p_cron_schedule, 
        string p_couch_db_url,
        string p_user_name,
        string p_user_value,
        string p_export_directory,
        string p_jurisdiction_user_name = null,
        string p_version_number = null
        )
    {
        cron_schedule = p_cron_schedule;
        couch_db_url = p_couch_db_url;
        user_name = p_user_name;
        user_value = p_user_value;
        export_directory = p_export_directory;
        jurisdiction_user_name = p_jurisdiction_user_name;
        version_number = p_version_number;
    }

    public string cron_schedule { get; private set; }
    public string couch_db_url { get; private set; }
    public string user_name { get; private set; }

    public string jurisdiction_user_name { get; private set; }

    public string version_number { get; private set; }

    public string user_value { get; private set; }
    public string export_directory { get; private set; }
}


public sealed class QuartzSupervisor : UntypedActor
{
    protected override void OnReceive(object message)
    {

        switch (message)
        {
            case "init":
                Console.WriteLine("Quartz Supervisor initialized");
                break;

            case "pulse":                
                bool is_perform_backup = false;

                var one_am_timespan = new TimeSpan(1, 0, 0);
                var difference = DateTime.Now - one_am_timespan;
                if(difference.Hour == 0 && difference.Minute == 0)
                {
                    is_perform_backup = true;
                }

                if(is_perform_backup)
                {
                    var  hot_backup_message = new mmria.services.backup.BackupSupervisor.PerformBackupMessage()
                    {
                        type = "hot",
                        DateStarted = DateTime.Now
                    };

                    var  cold_backup_message = new mmria.services.backup.BackupSupervisor.PerformBackupMessage()
                    {
                        type = "cold",
                        DateStarted = DateTime.Now
                    };

                    var bsr = Context.ActorSelection("akka://mmria-actor-system/user/backup-supervisor");
                    bsr.Tell(hot_backup_message); 
                    bsr.Tell(cold_backup_message); 
                }

            break;
        }
        
    }

}


