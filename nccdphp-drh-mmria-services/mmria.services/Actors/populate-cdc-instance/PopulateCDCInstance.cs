using System;
using System.Threading.Tasks;
using Akka.Actor;
using mmria.common.SharedLibraries.MMRIAServices.DAL;
using mmria.common.SharedLibraries.MMRIAServices.Manager;
using mmria.common.SharedLibraries.MMRIAServices.Model;
using mmria.common.SharedLibraries.MetadataVersion.DAL;

namespace mmria.services.populate_cdc_instance;

public sealed class PopulateCDCInstance : ReceiveActor
{

    public record class Status(string Name, string Description);


    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly MMRIAServicesManager _mmriaServicesManager;
    private readonly PopulateCdcThrottleSettings _populateCdcThrottleSettings;

    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
    public PopulateCDCInstance(
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        PopulateCdcThrottleSettings populateCdcThrottleSettings)
    {
        _couchDbHttpClient = couchDbHttpClient;
        _mmriaServicesManager = new MMRIAServicesManager(new MMRIAServicesDAL(_couchDbHttpClient, new mmria.common.SharedLibraries.SystemConfig.DAL.SystemConfigDAL(_couchDbHttpClient), new MetadataVersionDAL(_couchDbHttpClient), new mmria.common.SharedLibraries.VitalImport.DAL.VitalImportDAL(_couchDbHttpClient, new mmria.common.SharedLibraries.Case.DAL.CaseDAL(_couchDbHttpClient))), _couchDbHttpClient);
        _populateCdcThrottleSettings = populateCdcThrottleSettings ?? PopulateCdcThrottleSettings.CreateDefaults();
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
        var reply_to = Sender;

        try
        {
            var db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;
            Action<string> report_progress = description => reply_to.Tell(new Status("Progress", description));
            var (name, description) = await _mmriaServicesManager.PopulateCDCInstanceManger(
                message,
                db_config_set,
                report_progress,
                _populateCdcThrottleSettings);

            var cdc_connection = db_config_set.detail_list.ContainsKey("cdc")
                ? db_config_set.detail_list["cdc"]
                : db_config_set.detail_list["cdcqa"];
            var metadata_release_version_name = db_config_set.name_value["metadata_version"];

            Console.WriteLine($"[PopulateCDC] Starting CDC de_id/report rebuild at {cdc_connection.url}.");
            report_progress("Phase 2 of 2: starting CDC de-identified case database/report database rebuild.");

            var rebuild = new mmria.server.utils.c_document_sync_all(
                cdc_connection,
                metadata_release_version_name,
                _couchDbHttpClient,
                progressCallback: report_progress,
                throttleSettings: _populateCdcThrottleSettings);

            await rebuild.executeAsync();

            Console.WriteLine($"[PopulateCDC] CDC de_id/report rebuild complete at {cdc_connection.url}.");

            reply_to.Tell(new Status(name, $"{description} CDC de-identified case database/report database rebuild complete."));
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: Fatal exception in Process_Message");
            Console.WriteLine(ex);
            reply_to.Tell(new Status("Error", ex.Message));
        }
        


         Context.Stop(this.Self);

    }

}


