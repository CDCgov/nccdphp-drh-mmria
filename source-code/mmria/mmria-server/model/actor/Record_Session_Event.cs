using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using mmria.common.SharedLibraries.Session.Model;

namespace mmria.server.model.actor;

public sealed class Record_Session_Event : UntypedActor
{
    //protected override void PreStart() => Console.WriteLine("Session_Event_Message started");
    //protected override void PostStop() => Console.WriteLine("Session_Event_Message stopped");

	mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public Record_Session_Event
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        db_config = _db_config;
        _couchDbHttpClient = couchDbHttpClient;
    }

    protected override void OnReceive(object message)
    {
        
        switch (message)
        {
            case Session_Event_Message sem:


            try
            {
                


                var se = new mmria.common.model.couchdb.session_event();
                se.data_type = "session-event";
                se._id =sem._id;
                se.date_created  = sem.date_created;
                se.user_id  = sem.user_id;
                se.ip  = sem.ip;

                switch(sem.action_result)
                {
                    case Session_Event_Message.Session_Event_Message_Action_Enum.successful_login:
                        se.action_result = mmria.common.model.couchdb.session_event.session_event_action_enum.successful_login;
                        break;
                    case Session_Event_Message.Session_Event_Message_Action_Enum.password_changed:
                        se.action_result = mmria.common.model.couchdb.session_event.session_event_action_enum.password_changed;
                        break;                            
                    case Session_Event_Message.Session_Event_Message_Action_Enum.failed_login:
                    default:
                        se.action_result = mmria.common.model.couchdb.session_event.session_event_action_enum.failed_login;
                        break;

                }


                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                var session_event_json = Newtonsoft.Json.JsonConvert.SerializeObject(se, settings);

                var request_url = $"{db_config.url}/{db_config.prefix}session/{se._id}";
                _ = _couchDbHttpClient.ExecuteAsync("PUT", request_url, session_event_json, db_config.user_name, db_config.user_value, "application/json");

                //var session_event_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_object_key_header<mmria.common.model.couchdb.session_event>>(response_from_server);

            }
            catch(Exception ex)
            {
                Console.WriteLine($"Session_Event_Message exception: {ex}");
            }
            
            break;
        }

    }

}
