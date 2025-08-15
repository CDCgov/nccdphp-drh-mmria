using System;

namespace mmria.common.model.couchdb;
	
public sealed class session_event
{
    public enum session_event_action_enum
    {
        failed_login,
        successful_login,
        password_changed
        
    }
    public session_event ()
    {
    }

    public string _id {get; set;}
    public string data_type {get; set;} = "session-event";
    public DateTime date_created {get; set;}
    public string user_id {get; set;}
    public string ip {get; set;}
    public session_event_action_enum action_result {get; set;}

}
