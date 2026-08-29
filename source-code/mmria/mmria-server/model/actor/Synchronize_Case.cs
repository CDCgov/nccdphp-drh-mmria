#if !IS_PMSS_ENHANCED
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using mmria.server.model.actor;
using mmria.common.SharedLibraries.DeIdentified;
using mmria.common.SharedLibraries.Report;

namespace mmria.server.model.actor;
public sealed class Sync_Document_Message
{
    public Sync_Document_Message 
    (
        string p_document_id, 
        string p_document_json, 
        string p_method,
        string p_metadata_version
    )
    {
        
        document_id = p_document_id;
        document_json = p_document_json;
        method = p_method.ToUpper();
        metadata_version = p_metadata_version;
    }
    public string document_json { get; private set; }
    public string document_id { get; private set;}
    public string method { get; private set;}

    public string metadata_version { get; private set; }
}



public sealed class Sync_All_Documents_Message
{
    public Sync_All_Documents_Message 
    (
        DateTime p_time_sent,
        string p_metadata_version
    )
    {
        time_sent = p_time_sent;
        metadata_version = p_metadata_version;
    }
    public DateTime time_sent { get; private set; }
    public string metadata_version { get; private set; }
}
public sealed class Synchronize_Case : UntypedActor
{
    //protected override void PreStart() => Console.WriteLine("Synchronize_Case started");
    //protected override void PostStop() => Console.WriteLine("Synchronize_Case stopped");
	mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly string _host_prefix;
    private readonly IDeIdentifiedRepository _deIdentifiedRepository;
    private readonly IReportRepository _reportRepository;

    public Synchronize_Case
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.couchdb.OverridableConfiguration configuration = null,
        string host_prefix = null,
        IDeIdentifiedRepository deIdentifiedRepository = null,
        IReportRepository reportRepository = null
    )
    {
        db_config = _db_config;
        _couchDbHttpClient = couchDbHttpClient;
        _configuration = configuration;
        _host_prefix = host_prefix;
        _deIdentifiedRepository = deIdentifiedRepository;
        _reportRepository = reportRepository;
    }
    protected override void OnReceive(object message)
    {
        
        switch (message)
        {
            case Sync_Document_Message sync_document_message:


            var sync_document = new mmria.server.utils.c_sync_document 
            (
                sync_document_message.document_id, 
                sync_document_message.document_json, 
                sync_document_message.method,
                sync_document_message.metadata_version,
                db_config,
                _couchDbHttpClient,
                deIdentifiedRepository: _deIdentifiedRepository,
                reportRepository: _reportRepository,
                configuration: _configuration,
                host_prefix: _host_prefix
            );

            try
            {
                _ = sync_document.executeAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Synchronize_Case exception: {ex}");
            }
            
            break;

            case Sync_All_Documents_Message sync_all_documents_message:
                if(_configuration == null || string.IsNullOrWhiteSpace(_host_prefix))
                {
                    Console.WriteLine("Synchronize_Case received Sync_All_Documents_Message without rebuild service configuration. Skipping local full rebuild.");
                    break;
                }

                string rebuildServiceUrl = mmria.common.SharedLibraries.MMRIARebuild.Manager.MMRIARebuildManager.BuildServiceUrl(
                    _configuration.GetString("vitals_url", _host_prefix));
                string vitalServiceKey = _configuration.GetString("vital_service_key", _host_prefix);

                if(string.IsNullOrWhiteSpace(rebuildServiceUrl) || string.IsNullOrWhiteSpace(vitalServiceKey))
                {
                    Console.WriteLine($"Synchronize_Case could not resolve rebuild service configuration for tenant '{_host_prefix}'.");
                    break;
                }

                var rebuildManager = new mmria.common.SharedLibraries.MMRIARebuild.Manager.MMRIARebuildManager(
                    new mmria.common.SharedLibraries.MMRIARebuild.DAL.MMRIARebuildDAL(_couchDbHttpClient),
                    _couchDbHttpClient,
                    mmria.server.Program.configuration,
                    new System.Collections.Generic.List<mmria.common.couchdb.ConfigurationSet>());

                _ = rebuildManager.QueueRebuildOnServiceAsync(
                    new mmria.common.SharedLibraries.MMRIARebuild.Model.MMRIARebuildRequest
                    {
                        tenant = _host_prefix,
                        source = "manual"
                    },
                    rebuildServiceUrl,
                    vitalServiceKey);

            break;
        }

        Context.Stop(this.Self);
    }

}
#endif
