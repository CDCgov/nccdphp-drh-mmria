using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Akka.Actor;

namespace RecordsProcessor_Worker.Actors;

public sealed class Process_Message : UntypedActor
{
    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");

    protected override void OnReceive(object message)
    {
        Console.WriteLine($"Processing Message : {message}");

        string messageContent = message.ToString();
        string[] switchStrings = { "1", "2", "3" };
        string result = switchStrings.FirstOrDefault<string>(s => messageContent.Contains(s));

        switch (result)
        {
            case "1":
                Context.ActorOf(Props.Create<Message_Type_1>()).Tell(messageContent);
                break;
            case "2":
                Context.ActorOf(Props.Create<Message_Type_2>()).Tell(messageContent);
                break;
            case "3":
                Context.ActorOf(Props.Create<Message_Type_3>()).Tell(messageContent);
                break;
            default:
                Console.WriteLine($"Message contains no routing info, discarded");
                break;
        }
    }
}

