using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using Akka.Actor;

namespace mmria.services.backup;

public sealed class BackupHotProcessor : ReceiveActor
{
    protected override void PreStart() => Console.WriteLine("BackupHotProcessor Process_Message started");
    protected override void PostStop() => Console.WriteLine("BackupHotProcessor Process_Message stopped");

    public BackupHotProcessor()
    {
        Become(Waiting);
    }

    void Processing()
    {
        Receive<mmria.services.backup.BackupSupervisor.PerformBackupMessage>(message =>
        {
            // discard message;
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


    private async Task Process_Message(mmria.services.backup.BackupSupervisor.PerformBackupMessage message)
    {   
        mmria.common.couchdb.ConfigurationSet db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;

        var backup_db_url = db_config_set.name_value["backup_db_url"];
        var backup_db_user = db_config_set.name_value["backup_db_user"];
        var backup_db_user_value = db_config_set.name_value["backup_db_user_value"];

        int Second = 1000;
        int Sleep_Time_In_Miliseonds = 5 * Second;
    
        var db_replication_list = new List<string>()
        {
            "audit",
            "mmrds",
            "jurisdiction",
            "session",
            "_users",
            "configuration"
        };


            Console.WriteLine("Replication: Start");

            {
                Console.WriteLine("Backup vital_import");
                var replication_url = $"{backup_db_url}/_replicator";
                Console.WriteLine(replication_url);

                var replicate_struct = new Replicate_Struct();

                replicate_struct.source.url = $"{mmria.services.vitalsimport.Program.couchdb_url}/vital_import";
                replicate_struct.source.headers.Authorization = "Basic " + Base64Encode($"{mmria.services.vitalsimport.Program.timer_user_name}:{mmria.services.vitalsimport.Program.timer_value}");
                replicate_struct.create_target = false;
                replicate_struct.continuous = false;


                replicate_struct.target.url = $"{backup_db_url}/vital_import";
                
                
                replicate_struct.target.headers.Authorization = "Basic " + Base64Encode($"{backup_db_user}:{backup_db_user_value}");

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string replicate_struct_string = Newtonsoft.Json.JsonConvert.SerializeObject (replicate_struct, settings);

                try
                {
                    var replication_curl = new mmria.getset.cURL("POST", null, replication_url, replicate_struct_string, backup_db_user, backup_db_user_value);
                    var replication_curl_result = await replication_curl.executeAsync();

                    Console.WriteLine(replication_curl_result);
                    
                    await Task.Delay(Sleep_Time_In_Miliseonds);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Replication error \n{ex}");
                }
            }

            foreach(var kvp in db_config_set.detail_list)
            {
                var prefix = kvp.Key.ToLower();

                var data_connection = kvp.Value;

                if(kvp.Key.Equals("vital_import", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach(var replication_db in db_replication_list)
                {
                    var replication_url = $"{backup_db_url}/_replicator";
                    Console.WriteLine(replication_url);

                    var replicate_struct = new Replicate_Struct();

                    replicate_struct.source.url = $"{data_connection.url}/{replication_db}";
                    replicate_struct.source.headers.Authorization = "Basic " + Base64Encode($"{data_connection.user_name}:{data_connection.user_value}");
                    replicate_struct.create_target = true;
                    replicate_struct.continuous = false;

                    if(replication_db.IndexOf("_") == 0)
                    {
                        replicate_struct.target.url = $"{backup_db_url}/{prefix}{replication_db}";
                    }
                    else
                    {
                        replicate_struct.target.url = $"{backup_db_url}/{prefix}_{replication_db}";
                    }
                    
                    replicate_struct.target.headers.Authorization = "Basic " + Base64Encode($"{backup_db_user}:{backup_db_user_value}");

                    Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                    settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    string replicate_struct_string = Newtonsoft.Json.JsonConvert.SerializeObject (replicate_struct, settings);

                    try
                    {
                        var replication_curl = new mmria.getset.cURL("POST", null, replication_url, replicate_struct_string, backup_db_user, backup_db_user_value);
                        var replication_curl_result = await replication_curl.executeAsync();

                        Console.WriteLine(replication_curl_result);
                        
                        await Task.Delay(Sleep_Time_In_Miliseonds);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"{prefix} \n{ex}");
                    }
                }
            }
            Console.WriteLine("Replication: End");
            
        if(message.ReturnToSender)
        {
            this.Sender.Tell(new mmria.services.backup.BackupSupervisor.BackupFinishedMessage()
            {
                type = "hot",
                DateEnded = DateTime.Now

            });
        }

        
        Console.WriteLine($"Processing Message : {message}");

        Context.Stop(this.Self);

    }

    static string Base64Encode(string plainText) 
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }
}


     

   

