using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace mmria.server.model;

public sealed class Pulse_job : IJob
{
    public Pulse_job()
    {

    }

    public Task Execute(IJobExecutionContext context)
    {
        //System.Console.WriteLine($"Quartz_Pulse - {DateTime.Now:r}");

        Akka.Actor.ActorSystem actor_system = context.JobDetail.JobDataMap["ActorSystem"] as Akka.Actor.ActorSystem;
    
        var quartzSupervisor = actor_system.ActorSelection("akka://mmria-actor-system/user/QuartzSupervisor");
        quartzSupervisor.Tell("pulse");

        return Task.CompletedTask;
    }
}
