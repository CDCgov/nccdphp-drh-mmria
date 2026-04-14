using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Akka.Actor;

namespace mmria.services.backup;

public sealed class BackupHotProcessor : ReceiveActor
{
    protected override void PreStart() => Console.WriteLine("BackupHotProcessor Process_Message started");
    protected override void PostStop() => Console.WriteLine("BackupHotProcessor Process_Message stopped");

    private mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public BackupHotProcessor(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
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
        ReceiveAsync<mmria.services.backup.BackupSupervisor.PerformBackupMessage>(async message =>
        {
            Become(Processing);
            await Process_Message(message);
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
                replicate_struct.source.headers.Authorization = BuildBasicAuthValue(mmria.services.vitalsimport.Program.timer_user_name, mmria.services.vitalsimport.Program.timer_value);
                replicate_struct.create_target = false;
                replicate_struct.continuous = false;


                replicate_struct.target.url = $"{backup_db_url}/vital_import";
                
                
                replicate_struct.target.headers.Authorization = BuildBasicAuthValue(backup_db_user, backup_db_user_value);

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string replicate_struct_string = Newtonsoft.Json.JsonConvert.SerializeObject (replicate_struct, settings);

                try
                {
                    var replication_curl_result = await _couchDbHttpClient.ExecuteAsync("POST", replication_url, replicate_struct_string, backup_db_user, backup_db_user_value);

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
                    replicate_struct.source.headers.Authorization = BuildBasicAuthValue(data_connection.user_name, data_connection.user_value);
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
                    
                    replicate_struct.target.headers.Authorization = BuildBasicAuthValue(backup_db_user, backup_db_user_value);

                    Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                    settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    string replicate_struct_string = Newtonsoft.Json.JsonConvert.SerializeObject (replicate_struct, settings);

                    try
                    {
                        var replication_curl_result = await _couchDbHttpClient.ExecuteAsync("POST", replication_url, replicate_struct_string, backup_db_user, backup_db_user_value);

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

    static string BuildBasicAuthValue(string userName, string password)
    {
        byte[] credentialBytes = null;
        char[] encodedChars = null;
        try
        {
            var userBytes = Encoding.UTF8.GetByteCount(userName);
            var passBytes = Encoding.UTF8.GetByteCount(password);
            credentialBytes = new byte[userBytes + 1 + passBytes];

            var offset = Encoding.UTF8.GetBytes(userName.AsSpan(), credentialBytes);
            credentialBytes[offset++] = (byte)':';
            Encoding.UTF8.GetBytes(password.AsSpan(), credentialBytes.AsSpan(offset));

            var totalLen = userBytes + 1 + passBytes;
            var base64Len = ((totalLen + 2) / 3) * 4;
            encodedChars = new char[base64Len];

            if (!Convert.TryToBase64Chars(credentialBytes.AsSpan(0, totalLen), encodedChars, out var charsWritten))
            {
                throw new InvalidOperationException("Failed to encode credentials.");
            }

            return "Basic " + new string(encodedChars, 0, charsWritten);
        }
        finally
        {
            if (credentialBytes != null)
            {
                CryptographicOperations.ZeroMemory(credentialBytes);
            }
            if (encodedChars != null)
            {
                Array.Clear(encodedChars, 0, encodedChars.Length);
            }
        }
    }
}


     

   

