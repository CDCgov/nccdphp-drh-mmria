using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace mmria.server.model.actor.quartz;

public sealed class Remove_Deleted : UntypedActor
{
    //protected override void PreStart() => Console.WriteLine("Remove_Deleted started");
    //protected override void PostStop() => Console.WriteLine("Remove_Deleted stopped");

    protected override void OnReceive(object message)
    {
            Console.WriteLine($"Remove_Deleted Baby {System.DateTime.Now}");

        /*
        switch (message)
        {
            case WriteFile file:
                //file-data/file-name-directory/hash-name.file
                string new_directory = System.IO.Path.Combine(file.workingdirectory, "file-data", file.filename.Replace(file.monitoreddirectory, ""));
                

                Console.WriteLine($"QuartzWriter.OnRecieve {file.filename} >> {new_directory}");
                if(!System.IO.Directory.Exists(new_directory))
                {
                    System.IO.Directory.CreateDirectory(new_directory);
                }

                string new_path = System.IO.Path.Combine(new_directory, GetHash(file.filename));
                if(!System.IO.File.Exists(new_path))
                {
                    System.IO.File.Copy(file.filename, new_path);
                }
                
                break;

                case RecordFileMessage rfm:
                    Console.WriteLine(rfm.filename);
                    break;
        }*/

    }

}
