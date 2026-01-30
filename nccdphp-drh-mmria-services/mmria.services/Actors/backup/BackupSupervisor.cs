using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using Akka.Actor;
using mmria.common.ije;

namespace mmria.services.backup;

public sealed class BackupSupervisor : ReceiveActor
{

    public sealed class PerformBackupMessage
    {
        public PerformBackupMessage(){}

        public string type  { get; set; }
        public DateTime DateStarted {get; set; } = DateTime.Now;

        public bool ReturnToSender { get; set; } = true;
    }

    public sealed class BackupFinishedMessage
    {
        public BackupFinishedMessage(){}
        public string type  { get; set; }
        public DateTime DateEnded {get; set; }
    }

    DateTime? HotBackupStarted = null;
    DateTime? ColdBackupStarted = null;

    DateTime? CompressionStarted = null;

    IConfiguration configuration;
    ILogger logger;
    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
    public BackupSupervisor()
    {
        //IConfiguration p_configuration
        //configuration = p_configuration;
        //logger = p_logger;


        Receive<PerformBackupMessage>(message =>
        {   

            switch(message.type.ToLower())
            {

                case "cold":
                    ColdBackupStarted = DateTime.Now;
                    var cold_backup_processor = Context.ActorOf<mmria.services.backup.BackupColdProcessor>();
                    cold_backup_processor.Tell(message);
                    break;

                case "hot":
                    HotBackupStarted = DateTime.Now;
                    var hot_backup_processor = Context.ActorOf<mmria.services.backup.BackupHotProcessor>();
                    hot_backup_processor.Tell(message);
                    break;

                case "compress":
                    CompressionStarted = DateTime.Now;
                    var file_compressor = Context.ActorOf<mmria.services.backup.FileCompressor>();
                    file_compressor.Tell(message);
                    break;
            }

            //Console.WriteLine(JsonConvert.SerializeObject(message));
            //Sender.Tell("Message Recieved");
            
        });

        Receive<BackupFinishedMessage>(message =>
        {   

            switch(message.type.ToLower())
            {
                case "cold":
                    ColdBackupStarted = null;
                    break;

                case "hot":
                    HotBackupStarted = null;
                    break;

                case "compress":
                    CompressionStarted = null;
                    break;
            }

            //Console.WriteLine(JsonConvert.SerializeObject(message));
            //Sender.Tell("Message Recieved");
            
        });

    }




    
}

