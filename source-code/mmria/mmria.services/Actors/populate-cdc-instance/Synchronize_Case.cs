using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using mmria.server.model.actor;

namespace mmria.server.model.actor;
public sealed class Sync_Document_Message
{
    public Sync_Document_Message 
    (
        string p_document_id, 
        string p_document_json, common.couchdb.DBConfigurationDetail p_connection,
        string p_metadata_release_version_name,
        string p_method = "PUT")
    {
        
        document_id = p_document_id;
        document_json = p_document_json;
        method = p_method;
        connection = p_connection;
        metadata_release_version_name = p_metadata_release_version_name;
    }
    public string document_json { get; private set; }
    public string document_id { get; private set;}
    public string method { get; private set;}

    public common.couchdb.DBConfigurationDetail connection { get; private set;}

    public string metadata_release_version_name { get; private set;}
}



public sealed class Sync_All_Documents_Message
{
    public Sync_All_Documents_Message 
    (
        DateTime p_time_sent, 
        common.couchdb.DBConfigurationDetail p_connection,
        string p_metadata_release_version_name
    )
    {
        time_sent = p_time_sent;
        connection = p_connection;
        metadata_release_version_name = p_metadata_release_version_name;
        
    }
    public DateTime time_sent { get; private set; }
    public common.couchdb.DBConfigurationDetail connection { get; private set;}
    public string metadata_release_version_name { get; private set;}
}
public sealed class Synchronize_Case : ReceiveActor
{
    
    //protected override void PreStart() => Console.WriteLine("Synchronize_Case started");
    //protected override void PostStop() => Console.WriteLine("Synchronize_Case stopped");

    public Synchronize_Case()
    {
        
        Receive<Sync_Document_Message>(message =>
        {
            var sync_document = new mmria.server.utils.c_sync_document 
            (
                message.document_id, 
                message.document_json, 
                message.connection,
                message.metadata_release_version_name,
                message.method
            );

            try
            {
                sync_document.executeAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Synchronize_Case exception: {ex}");
            }

            Context.Stop(this.Self);
            
        });

        Receive<Sync_All_Documents_Message>(message =>
        {


            mmria.server.utils.c_document_sync_all sync_all = new mmria.server.utils.c_document_sync_all 
            (
                message.connection,
                message.metadata_release_version_name
            );

            sync_all.executeAsync ();

            Context.Stop(this.Self);
        });

        
    }

}
