using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Akka.Actor;
using mmria.common.ije;
using mmria.common.SharedLibraries.MMRIAServices.DAL;
using mmria.common.SharedLibraries.MMRIAServices.Manager;

namespace RecordsProcessor_Worker.Actors;

public sealed class BatchSupervisor : ReceiveActor
{

    Dictionary<string, mmria.common.ije.Batch.StatusEnum> batch_id_list;
    IConfiguration configuration;
    ILogger logger;
    mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    MMRIAServicesManager _mmriaServicesManager;
    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
    public BatchSupervisor(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
        _mmriaServicesManager = new MMRIAServicesManager(new MMRIAServicesDAL(_couchDbHttpClient), _couchDbHttpClient);
        //IConfiguration p_configuration
        //configuration = p_configuration;
        //logger = p_logger;
        batch_id_list = new Dictionary<string, mmria.common.ije.Batch.StatusEnum>();

        var alldocs = _mmriaServicesManager.GetBatchSet(
            mmria.services.vitalsimport.Program.couchdb_url,
            mmria.services.vitalsimport.Program.timer_user_name,
            mmria.services.vitalsimport.Program.timer_value
        ).Result;
        foreach(var row in alldocs.rows)
        {
            batch_id_list.Add(row.id, row.doc.Status);
        }

        ReceiveAsync<mmria.common.ije.NewIJESet_Message>(async message =>
        {

                string ping_result = await _mmriaServicesManager.PingCVSServer(mmria.services.vitalsimport.Program.DbConfigSet);
                int ping_count = 1;
                
                while
                (
                    (
                        ping_result == null ||
                        ping_result.ToLower() != "Server is up!".ToLower()
                    ) && 
                    ping_count < 2
                )   
                {

                    Console.WriteLine($"{DateTime.Now.ToString("o")} CVS Server Not running: Waiting 40 seconds to try again: {ping_result}");

					const int Milliseconds_In_Second = 1000;
					var next_date = DateTime.Now.AddMilliseconds(40 * Milliseconds_In_Second);
                    while(DateTime.Now < next_date)
					{
						// do nothing
					}
                    
                    ping_result = await _mmriaServicesManager.PingCVSServer(mmria.services.vitalsimport.Program.DbConfigSet);
                    ping_count +=1;

                    

                }


            batch_id_list.Add(message.batch_id, mmria.common.ije.Batch.StatusEnum.InProcess);
            var batch_processor = Context.ActorOf(Props.Create<RecordsProcessor_Worker.Actors.BatchProcessor>(_couchDbHttpClient), message.batch_id);
            batch_processor.Tell(message);
            //Console.WriteLine(JsonConvert.SerializeObject(message));
            //Sender.Tell("Message Recieved");
            
        });

        Receive<mmria.common.ije.BatchStatusMessage>(message =>
        {
            batch_id_list[message.id] = message.status;
            
        });



        Receive<mmria.common.ije.BatchRemoveDataMessage>(message =>
        {
            if(batch_id_list.ContainsKey(message.id))
            {
                if
                (
                    batch_id_list[message.id] == mmria.common.ije.Batch.StatusEnum.Finished ||
                    batch_id_list[message.id] == mmria.common.ije.Batch.StatusEnum.BatchRejected
                )
                {
                    var batch_processor = Context.ActorOf(Props.Create<RecordsProcessor_Worker.Actors.BatchProcessor>(_couchDbHttpClient), message.id);
                    batch_processor.Tell(message);
                }
            }
            
        });


        
    }
}
