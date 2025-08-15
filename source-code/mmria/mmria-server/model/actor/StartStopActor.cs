using System;
using Akka.Actor;

namespace mmria.server.model.actor;

public sealed class StartStopActor1 : UntypedActor
{
    protected override void PreStart()
    {
        //Console.WriteLine("StartStopActor1 started");
        Context.ActorOf(Props.Create<StartStopActor2>(), "second");
    }

    //protected override void PostStop() => Console.WriteLine("StartStopActor1 stopped");

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case "stop":
                Context.Stop(Self);
                break;
        }
    }
}

public sealed class StartStopActor2 : UntypedActor
{
    protected override void PreStart() => Console.WriteLine("StartStopActor2 started");
    protected override void PostStop() => Console.WriteLine("StartStopActor2 stopped");

    protected override void OnReceive(object message)
    {
    }
}

