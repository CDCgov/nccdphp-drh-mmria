using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace mmria.common.SharedLibraries.Session.Model;

public sealed class Session_Message
{

    public Session_Message 
    (
    string p_id,
    string p_rev,
    DateTime p_date_created,
    DateTime p_date_last_updated,

    DateTime? p_date_expired,

    bool p_is_active,
    string p_user_id,
    string p_ip,
    string p_session_event_id,
    System.Collections.Generic.List<string> p_role_list,
    System.Collections.Generic.Dictionary<string,string> p_data
    )
    {
        _id = p_id;
        _rev = p_rev;
        date_created = p_date_created;
        date_last_updated = p_date_last_updated;

        date_expired = p_date_expired;

        is_active = p_is_active;
        user_id = p_user_id;
        ip = p_ip;
        session_event_id = p_session_event_id;
        role_list = p_role_list;
        data = p_data;
        
    }

    public string _id {get; private set;}
    public string _rev {get; private set;}
    public string data_type { get; private set; } = "session";
    public DateTime date_created {get; private set;}
    public DateTime date_last_updated {get; private set;}

    public DateTime? date_expired {get; private set;}

    public bool is_active {get; private set;}
    public string user_id {get; private set;}
    public string ip {get; private set;}
    public string session_event_id {get; private set;}

    public System.Collections.Generic.List<string> role_list {get; private set;}

    public System.Collections.Generic.Dictionary<string,string> data { get; private set; }

}

public sealed class Session_MessageDTO
{
    public Session_MessageDTO()
    {
        data = new System.Collections.Generic.Dictionary<string,string>();
        role_list = new System.Collections.Generic.List<string> ();
    }

    public string _id {get;  set;}
    public string _rev {get;  set;}
    public string data_type { get;  set; } = "session";
    public DateTime date_created {get;  set;}
    public DateTime date_last_updated {get;  set;}

    public DateTime? date_expired {get;  set;}

    public bool is_active {get;  set;}
    public string user_id {get;  set;}
    public string ip {get;  set;}
    public string session_event_id {get;  set;}
    public System.Collections.Generic.List<string> role_list {get; set;}


    public System.Collections.Generic.Dictionary<string,string> data { get;  set; }

}
