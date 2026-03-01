using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using mmria.common.SharedLibraries.Session.Model;

namespace mmria.server.model.actor;

public sealed class Post_Session : ReceiveActor
{
    mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public Post_Session
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        db_config = _db_config;
        _couchDbHttpClient = couchDbHttpClient;

        ReceiveAsync<Session_Message>(async session_message =>
        {
            try
            {
                mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();
                string request_string = db_config.url + $"/{db_config.prefix}session/{session_message._id}";

                try 
                {
                    string check_document_json = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
                    var check_document_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.session> (check_document_json);

                    if(!string.IsNullOrWhiteSpace(check_document_expando_object.user_id) && !session_message.user_id.Equals(check_document_expando_object.user_id, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.Write($"unauthorized PUT {session_message._id} by: {session_message.user_id}");
                        return;
                    }
                } 
                catch (Exception) 
                {
                    // do nothing for now document doesn't exsist.
                }

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(session_message, settings);

                try
                {
                    string responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", request_string, object_string, db_config.user_name, db_config.user_value);
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine (ex);
            } 

            Context.Stop(this.Self);
        });
    }

}
