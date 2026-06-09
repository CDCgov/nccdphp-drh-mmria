using System;
using Akka.Actor;

namespace mmria.server.model.actor;

public sealed class QuartzSupervisor : UntypedActor
{
    protected override void OnReceive(object message)
    {

        switch (message)
        {
            case "init":
                Console.WriteLine("Quartz Supervisor initialized. Automatic backups are scheduled by dedicated backup Quartz jobs.");
                break;

            case "pulse":                
                break;
        }
        
    }

}


