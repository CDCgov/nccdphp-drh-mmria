using System;
using System.IO;
using System.Collections.Generic;
using Akka.Actor;

namespace mmria.server.model.actor;

/*
public sealed class ListDirectoryMessage
{
    public ListDirectoryMessage(project p_project, string p_comment)
    {
        project = p_project;
        comment = p_comment;
    }

    public project project { get; }
    public string comment { get; }
}


public sealed class ListDirectorySupervisor : UntypedActor
{
    private IActorRef child = Context.ActorOf(Props.Create<ListDirectory>(), "ListDirectory");

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case ListDirectoryMessage ldm:
                child.Tell(ldm);
                break;
        }
    }
}

public sealed class ListDirectory : UntypedActor
{
    protected override void PreStart() => Console.WriteLine("ListDirectory started");
    protected override void PostStop() => Console.WriteLine("ListDirectory stopped");

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case ListDirectoryMessage ListDirectoryMessage:
                project project = ListDirectoryMessage.project;
                //file-data/file-name-directory/hash-name.file
                //string new_directory = System.IO.Path.Combine(file.workingdirectory, "file-data", file.filename.Replace(file.monitoreddirectory, ""));
                


                var ListDirectoryCommand = new ListDirectoryCommand()
                {
                    comment = ListDirectoryMessage.comment,
                    Path = project.monitoredDirectory,
                    DateTime = System.DateTime.Now
                };

                List<FileDetail> FileDetailList = new List<FileDetail>();

                var FileDataWriterSupervisor = Context.ActorOf(Props.Create<file_server.model.actor.FileDataWriterSupervisor>(), "FileDataWriterSupervisor");

                var stack = new Stack<string>();
                stack.Push(project.monitoredDirectory);

                while (stack.Count > 0)
                {
                    var folder = stack.Pop();

                    //result.Append(spacing);
                    //result.AppendLine(folder);
                    foreach (var file in Directory.GetFiles(folder, "*.*"))
                    {
                        bool ignore = false;

                        foreach (string ignore_item in project.ignoredList)
                        {
                            string check_file = System.IO.Path.Combine(project.monitoredDirectory, ignore_item);
                            if (file.StartsWith(check_file, StringComparison.CurrentCulture))
                            {
                                ignore = true;
                            }
                        }

                        if (ignore) continue;


                        FileDataWriterSupervisor.Tell(new file_server.model.actor.WriteFile(file, project.workingDirectory, project.monitoredDirectory));

                        var fileInfo = new FileInfo(file);
                        FileDetailList.Add(new FileDetail()
                        {
                            Name = fileInfo.Name,
                            Attributes = fileInfo.Attributes,
                            CreationTime = fileInfo.CreationTime,
                            CreationTimeUtc = fileInfo.CreationTimeUtc,
                            Exists = fileInfo.Exists,
                            Extension = fileInfo.Extension,
                            FullName = fileInfo.FullName,
                            LastAccessTime = fileInfo.LastAccessTime,
                            LastAccessTimeUtc = fileInfo.LastAccessTimeUtc,
                            LastWriteTime = fileInfo.LastWriteTime,
                            LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
                            IsReadOnly = fileInfo.IsReadOnly,
                            Length = fileInfo.Length,
                            Hash = GetHash(fileInfo.FullName)
                        });

                    }

                    foreach (var directory in Directory.GetDirectories(folder))
                    {
                        stack.Push(directory);
                    }
                }

                ListDirectoryCommand.FileDetailList = FileDetailList;

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(ListDirectoryCommand, settings);


                string json_path = System.IO.Path.Combine(project.workingDirectory, "file-json", ListDirectoryCommand.DateTime.ToString("o").Replace(":", "_") + ".json");
                System.IO.File.WriteAllText(json_path, object_string);

                Console.WriteLine($"{object_string}");


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

