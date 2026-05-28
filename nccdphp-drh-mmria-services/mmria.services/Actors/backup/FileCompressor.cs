using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using Akka.Actor;

namespace mmria.services.backup;

public sealed class FileCompressor : ReceiveActor
{
    protected override void PreStart() => Console.WriteLine("FileCompressor started");
    protected override void PostStop() => Console.WriteLine("FileCompressor stopped");

    public FileCompressor()
    {
        Become(Waiting);
    }

    void Processing()
    {
        Receive<mmria.services.backup.BackupSupervisor.PerformBackupMessage>(message =>
        {
            // discard message;
            Console.WriteLine("File Compressor discarded message.");
        });
    }

    void Waiting()
    {
        Receive<mmria.services.backup.BackupSupervisor.PerformBackupMessage>(message =>
        {
            Become(Processing);
            Process_Message(message);
        });
    }

    void Process_Message(mmria.services.backup.BackupSupervisor.PerformBackupMessage message)
    {
        Console.WriteLine("Beginning File Compressor.");

        try
        {
            mmria.common.couchdb.ConfigurationSet db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;
            mmria.server.utils.cFolderCompressor folder_compressor = new mmria.server.utils.cFolderCompressor();
            string encryption_key = null;


            string root_folder = db_config_set.name_value["backup_storage_root_folder"];

            var suffix = "-ready-for-compression.txt";

            var directory_info_list = new List<System.IO.DirectoryInfo>();
            foreach(var directory_path in System.IO.Directory.GetDirectories(root_folder))
            {

                var directory = new System.IO.DirectoryInfo(directory_path);
                var target_folder = directory.FullName;
                var date_string = directory.Name;

                foreach(var f in directory.GetFiles().OrderBy(x=> x.LastWriteTime))
                {
                    if (f.Name.EndsWith(suffix))
                    {
                        directory_info_list.Add(directory);
                        break;
                    }
                }

            }

            foreach(var directory in directory_info_list.OrderBy(x=> x.LastWriteTime))
            {
                var target_folder = directory.FullName;
                var date_string = directory.Name;

                foreach(var f in directory.GetFiles().OrderBy(x=> x.LastWriteTime))
                {
                    if (f.Name.EndsWith(suffix))
                    {
                        try
                        {
                            var prefix = f.Name.Replace(suffix, "");
                            var zip_file_name = $"{date_string}-{prefix}.zip";
                            var db_folder = System.IO.Path.Combine(target_folder, prefix);
                             

                            var target_zip_file = System.IO.Path.Combine(target_folder, zip_file_name);

                            if(System.IO.File.Exists(target_zip_file))
                            {
                                System.IO.File.Delete(target_zip_file);
                            }

                            if(System.IO.Directory.Exists(db_folder))
                            {
                                folder_compressor.Compress
                                (
                                    target_zip_file,
                                    encryption_key,
                                    db_folder
                                );

                                System.IO.Directory.Delete(db_folder, true);
                            }

                            f.Delete();
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine($"File Compressor \n{ex}");
                        }
                    }
                }


            }

        }
        catch(Exception ex)
        {
            Console.WriteLine($"File Compressor\n{ex}");
        }

        if(message.ReturnToSender)
        {
            this.Sender.Tell(new mmria.services.backup.BackupSupervisor.BackupFinishedMessage()
            {
                type = "compress",
                DateEnded = DateTime.Now

            });
        }

        Console.WriteLine("FileCompressor fin.");

        Context.Stop(this.Self);
    }

}
   

