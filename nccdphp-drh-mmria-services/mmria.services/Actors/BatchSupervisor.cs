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
using mmria.common.SharedLibraries.MetadataVersion.DAL;

namespace RecordsProcessor_Worker.Actors;

public sealed class BatchSupervisor : ReceiveActor, IWithStash
{
    private sealed class InitializeBatchList { public static readonly InitializeBatchList Instance = new(); }

    private const int CvsServerRetryDelayMs = 40 * 1000;

    Dictionary<string, mmria.common.ije.Batch.StatusEnum> batch_id_list;
    IConfiguration configuration;
    ILogger logger;
    mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    MMRIAServicesManager _mmriaServicesManager;

    public IStash Stash { get; set; }

    protected override void PreStart()
    {
        Console.WriteLine("Process_Message started");
        // Defer the initial batch-list load until after the actor is started so we
        // don't block the construction thread on a CouchDB round-trip.
        Self.Tell(InitializeBatchList.Instance);
    }
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
    public BatchSupervisor(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
        _mmriaServicesManager = new MMRIAServicesManager(new MMRIAServicesDAL(_couchDbHttpClient, new mmria.common.SharedLibraries.SystemConfig.DAL.SystemConfigDAL(_couchDbHttpClient), new MetadataVersionDAL(_couchDbHttpClient)), _couchDbHttpClient);
        //IConfiguration p_configuration
        //configuration = p_configuration;
        //logger = p_logger;
        batch_id_list = new Dictionary<string, mmria.common.ije.Batch.StatusEnum>();

        Become(Initializing);
    }

    private void Initializing()
    {
        ReceiveAsync<InitializeBatchList>(async _ =>
        {
            try
            {
                var alldocs = await _mmriaServicesManager.GetBatchSet(
                    mmria.services.vitalsimport.Program.couchdb_url,
                    mmria.services.vitalsimport.Program.timer_user_name,
                    mmria.services.vitalsimport.Program.timer_value
                );
                foreach (var row in alldocs.rows)
                {
                    batch_id_list[row.id] = row.doc.Status;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:o} BatchSupervisor failed to load initial batch list: {ex}");
            }
            finally
            {
                Become(Ready);
                Stash.UnstashAll();
            }
        });

        // Hold any incoming work until the initial batch list has been loaded,
        // preserving the original synchronous-construction ordering guarantee.
        ReceiveAny(_ => Stash.Stash());
    }

    private void Ready()
    {
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

                    Console.WriteLine($"{DateTime.Now.ToString("o")} CVS Server Not running: Waiting {CvsServerRetryDelayMs / 1000} seconds to try again: {ping_result}");

                    Console.WriteLine($"{DateTime.Now:o} BatchSupervisor: waiting {CvsServerRetryDelayMs / 1000}s before retry attempt {ping_count + 1}...");
                    await Task.Delay(CvsServerRetryDelayMs);
                    Console.WriteLine($"{DateTime.Now:o} BatchSupervisor: wait complete, retrying CVS ping (attempt {ping_count + 1}).");

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
