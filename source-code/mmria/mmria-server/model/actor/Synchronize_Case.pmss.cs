#if IS_PMSS_ENHANCED
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using mmria.pmss.server.model.actor;

namespace mmria.pmss.server.model.actor;
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

    public Synchronize_Case
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config
    )
    {
        db_config = _db_config;
    }
    protected override void OnReceive(object message)
    {
        
        switch (message)
        {
            case Sync_Document_Message sync_document_message:


            var sync_document = new mmria.pmss.server.utils.c_sync_document 
            (
                sync_document_message.document_id, 
                sync_document_message.document_json, 
                sync_document_message.method,
                sync_document_message.metadata_version,
                db_config
            );

            try
            {
                sync_document.executeAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Synchronize_Case exception: {ex}");
            }
            
            break;

            case Sync_All_Documents_Message sync_all_documents_message:

                mmria.pmss.server.utils.c_document_sync_all sync_all = new mmria.pmss.server.utils.c_document_sync_all 
                (
                    db_config.url,
                    db_config.user_name,
                    db_config.user_value,
                    sync_all_documents_message.metadata_version,
                    db_config
                );

                sync_all.executeAsync ();

            break;
        }

        Context.Stop(this.Self);
    }

}
#endif