using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using mmria.common.SharedLibraries.Session;
using mmria.common.SharedLibraries.Session.Model;

namespace mmria.server.model.actor;

public sealed class Post_Session : ReceiveActor
{
    mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly ISessionRepository _sessionRepository;

    public Post_Session
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        ISessionRepository sessionRepository
    )
    {
        db_config = _db_config;
        _sessionRepository = sessionRepository;

        ReceiveAsync<Session_Message>(async session_message =>
        {
            try
            {
                mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

                try 
                {
                    var check_document_expando_object = await _sessionRepository.GetSessionDocumentAsync(session_message._id, db_config);

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
                    result = await _sessionRepository.SaveSessionRawAsync(session_message._id, object_string, db_config);
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
