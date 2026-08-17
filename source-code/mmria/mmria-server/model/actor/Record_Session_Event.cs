using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using mmria.common.SharedLibraries.Session;
using mmria.common.SharedLibraries.Session.Model;

namespace mmria.server.model.actor;

public sealed class Record_Session_Event : UntypedActor
{
    //protected override void PreStart() => Console.WriteLine("Session_Event_Message started");
    //protected override void PostStop() => Console.WriteLine("Session_Event_Message stopped");

	mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly ISessionRepository _sessionRepository;

    public Record_Session_Event
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        ISessionRepository sessionRepository
    )
    {
        db_config = _db_config;
        _sessionRepository = sessionRepository;
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

                _ = _sessionRepository.SaveSessionEventAsync(se, db_config);

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
