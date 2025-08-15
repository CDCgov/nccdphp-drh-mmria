using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Akka.Actor;

namespace RecordsProcessor_Worker.Actors;

public sealed class Message_Type_1 : UntypedActor
{
    protected override void PreStart() => Console.WriteLine("Message_Type_1 started");
    protected override void PostStop() => Console.WriteLine("Message_Type_1 stopped");

    protected override void OnReceive(object message)
    {
        Console.WriteLine($"Message_Type_1 : {message}");
        Thread.Sleep(10000);
        Console.WriteLine("Message_Type_1 Part 1 complete");
        Thread.Sleep(5000);
        Console.WriteLine("Message_Type_1 Part 2 complete");
        Thread.Sleep(15000);
        Console.WriteLine("Message_Type_1 Part 3 and Final complete");
    }
}

