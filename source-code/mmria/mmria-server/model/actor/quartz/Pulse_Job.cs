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
        Console.WriteLine($"[CDC-DEBUG] Pulse_job fired at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        Akka.Actor.ActorSystem actor_system = context.JobDetail.JobDataMap["ActorSystem"] as Akka.Actor.ActorSystem;
    
        // Send pulse to all tenant QuartzSupervisors
        Console.WriteLine("[CDC-DEBUG] Sending pulse to akka://mmria-actor-system/user/QuartzSupervisor-*");
        var quartzSupervisors = actor_system.ActorSelection("akka://mmria-actor-system/user/QuartzSupervisor-*");
        quartzSupervisors.Tell("pulse");

        return Task.CompletedTask;
    }
}
