using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Akka.Actor;

namespace RecordsProcessor_Worker.Actors;

public sealed class Message_Type_2 : UntypedActor
{
    protected override void PreStart() => Console.WriteLine("Message_Type_2 started");
    protected override void PostStop() => Console.WriteLine("Message_Type_2 stopped");

    protected override void OnReceive(object message)
    {
        Console.WriteLine($"Message_Type_2 : {message}");
        Thread.Sleep(5000);
        Console.WriteLine("Message_Type_2 Part 1 complete");
        Thread.Sleep(1000);
        Console.WriteLine("Message_Type_2 Part 2 complete");
        Thread.Sleep(20000);
        Console.WriteLine("Message_Type_2 Part 3 and Final complete");
    }
}

