using System;

namespace mmria.common.SharedLibraries.Session.Model;

public sealed class Session_Event_Message
{
    public enum Session_Event_Message_Action_Enum
    {
        failed_login,
        successful_login,
        password_changed
    }

    public Session_Event_Message
    (
        DateTime p_date_created,
        string p_user_id,
        string p_ip,
        Session_Event_Message_Action_Enum p_action_result
    )
    {
        date_created  = p_date_created;
        user_id  = p_user_id;
        ip  = p_ip;
        action_result  = p_action_result;
        
        _id = Guid.NewGuid().ToString();
    }

    public string _id {get; private set; }
    public DateTime date_created {get; private set;}
    public string user_id {get; private set;}
    public string ip {get; private set;}
    public Session_Event_Message_Action_Enum action_result {get; private set;}

}
