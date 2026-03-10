using System;
using System.Threading.Tasks;
using Akka.Actor;
using mmria.common.SharedLibraries.MMRIAServices.DAL;
using mmria.common.SharedLibraries.MMRIAServices.Manager;

namespace mmria.services.populate_cdc_instance;

public sealed class PopulateCDCInstance : ReceiveActor
{

    public record class Status(string Name, string Description);


    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly MMRIAServicesManager _mmriaServicesManager;

    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
    public PopulateCDCInstance(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
        _mmriaServicesManager = new MMRIAServicesManager(new MMRIAServicesDAL(_couchDbHttpClient));
        Become(Waiting);
    }

    void Processing()
    {
        Receive<mmria.common.metadata.Populate_CDC_Instance>(message =>
        {
            // discard message;
        });
    }

    void Waiting()
    {
        ReceiveAsync<mmria.common.metadata.Populate_CDC_Instance>(async message =>
        {
            Become(Processing);
            await Process_Message(message);
        });
    }
    
    private async Task Process_Message(mmria.common.metadata.Populate_CDC_Instance message)
    {
        try
        {
            var db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;
            var (name, description) = await _mmriaServicesManager.PopulateCDCInstanceManger(message, db_config_set, DeIdentifyCase);
            Sender.Tell(new Status(name, description));
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: Fatal exception in Process_Message");
            Console.WriteLine(ex);
            Sender.Tell(new Status("Error", ex.Message));
        }
        


         Context.Stop(this.Self);

    }

    private async Task<string> DeIdentifyCase(
        string documentJson,
        string instanceName,
        mmria.common.couchdb.DBConfigurationDetail cdcConnection,
        string metadataReleaseVersionName
    )
    {
        return await new mmria.server.utils.c_cdc_de_identifier(
            documentJson,
            instanceName,
            cdcConnection,
            metadataReleaseVersionName,
            _couchDbHttpClient
        ).executeAsync();
    }

}


