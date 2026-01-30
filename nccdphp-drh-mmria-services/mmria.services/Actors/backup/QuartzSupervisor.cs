using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace mmria.server.model.actor;

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


