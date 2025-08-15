using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Akka.Actor;

namespace RecordsProcessor_Worker.Actors;

public sealed class Message_Type_3 : UntypedActor
{
    protected override void PreStart() => Console.WriteLine("Message_Type_3 started");
    protected override void PostStop() => Console.WriteLine("Message_Type_3 stopped");

    protected override void OnReceive(object message)
    {
        Console.WriteLine($"Message_Type_3 : {message}");
        Thread.Sleep(1000);
        Console.WriteLine("Message_Type_3 Part 1 complete");
        Thread.Sleep(3000);
        Console.WriteLine("Message_Type_3 Part 2 complete");
        Thread.Sleep(5000);
        Console.WriteLine("Message_Type_3 Part 3 and Final complete");
    }
}

