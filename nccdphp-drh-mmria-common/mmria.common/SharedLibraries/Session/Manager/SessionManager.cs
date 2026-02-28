using System;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.Session.Model;

namespace mmria.common.SharedLibraries.Session.Manager;

public sealed class SessionManager
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public SessionManager
    (
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public void RecordSessionEvent(Session_Event_Message sem, mmria.common.couchdb.DBConfigurationDetail db_config)
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

    }

    public async Task PostSessionAsync(Session_Message session_message, mmria.common.couchdb.DBConfigurationDetail db_config)
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

}
