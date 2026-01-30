using System;

namespace mmria.common.model.couchdb;

public sealed class session_response
{
    public session_response ()
    {
    }

    public string auth_session {get; set;}
    public bool ok { get; set; }
    public UserCTX userCTX  { get; set; }
    public Info info { get; set; }

    //{"ok":true,"userCtx":{"name":null,"roles":[]},"info":{"authentication_db":"_users","authentication_handlers":["oauth","cookie","default"]}}
    /*
        {
        "ok":true,
        "userCtx":
        {
            "name":"mmrds",
            "roles":["_admin"]
        },
        "info":
        {
            "authentication_db":"_users",
            "authentication_handlers":
            [
                "oauth",
                "cookie",
                "default"
            ],
            "authenticated":"cookie"
        }
        }
    */


}


