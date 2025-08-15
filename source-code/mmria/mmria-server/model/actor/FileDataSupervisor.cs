using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
//using System.Reactive;
//using System.Reactive.Linq;

namespace mmria.server.model.actor;

/*
public sealed class WriteFile
{
    public WriteFile(string fileName, string workingDirectory, string monitoredDirectory)
    {
        filename = fileName;
        workingdirectory = workingDirectory;
        monitoreddirectory = monitoredDirectory;
    }

    public string filename { get; }
    public string workingdirectory { get; }
    public string monitoreddirectory { get; }
}

public sealed class MonitorProjectMessage
{
    public MonitorProjectMessage(string p_projectFilePath)
    {
        projectFilePath = p_projectFilePath;
    }

    public string projectFilePath { get; }
}


public sealed class RecordFileMessage
{
    public RecordFileMessage(string p_fileName, MonitorProjectMessage p_MonitorProjectMessage)
    {
        filename = p_fileName;
        MonitorProjectMessage = p_MonitorProjectMessage;
    }

    public string filename { get; }
    public MonitorProjectMessage MonitorProjectMessage { get; }
}


public sealed class FileDataWriterSupervisor : UntypedActor
{
    private IActorRef child = Context.ActorOf(Props.Create<FileDataWriter>(), "FileDataWriterSupervisor");

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case WriteFile file:
                child.Tell(file);
                break;

            case MonitorProjectMessage mpm:
                project project = project.FromJson(System.IO.File.OpenText(mpm.projectFilePath).ReadToEnd());

                IDisposable writer = new file_server.util.FileSystemObservable(mpm.projectFilePath, "*.*", false)
                .CreatedFiles
                .Where(x => {
                    foreach (string ignore_item in project.ignoredList)
                    {
                        string check_file = System.IO.Path.Combine(project.monitoredDirectory, ignore_item);
                        if (x.FullPath.StartsWith(check_file, StringComparison.CurrentCulture)) 
                        {
                            return false;
                        }
                    }
                
                    return true;
                })
                .Select(x => x.FullPath)
                .Subscribe(Console.WriteLine);
                break;
        }
    }
}

public sealed class FileDataWriter : UntypedActor
{
    protected override void PreStart() => Console.WriteLine("FileDataWriter started");
    protected override void PostStop() => Console.WriteLine("FileDataWriter stopped");

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case WriteFile file:
                //file-data/file-name-directory/hash-name.file
                string new_directory = System.IO.Path.Combine(file.workingdirectory, "file-data", file.filename.Replace(file.monitoreddirectory, ""));
                

                Console.WriteLine($"FileDataWriter.OnRecieve {file.filename} >> {new_directory}");
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
        }

    }

    public static string GetHash(string file_path)
    {
        string result;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        System.Security.Cryptography.MD5 md5Hasher = System.Security.Cryptography.MD5.Create();

        using (System.IO.FileStream fs = new System.IO.FileStream(file_path, System.IO.FileMode.Open,
                            System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
        {
            foreach (Byte b in md5Hasher.ComputeHash(fs))
                sb.Append(b.ToString("X2").ToLowerInvariant());
        }

        result = sb.ToString();

        return result;
    }
    

}
*/

