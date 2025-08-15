using System;
using Akka.Actor;

namespace mmria.server.model.actor;

public sealed class SupervisingActor : UntypedActor
{
    private IActorRef child = Context.ActorOf(Props.Create<SupervisedActor>(), "supervised-actor");

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case "failChild":
                child.Tell("fail");
                break;
        }
    }
}

public sealed class SupervisedActor : UntypedActor
{
    protected override void PreStart() => Console.WriteLine("supervised actor started");
    protected override void PostStop() => Console.WriteLine("supervised actor stopped");

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case "fail":
                Console.WriteLine("supervised actor fails now");
                throw new Exception("I failed!");
        }
    }
}

