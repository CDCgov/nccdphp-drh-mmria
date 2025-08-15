using System;

namespace mmria.common.model.couchdb;

public sealed class login_response
{
    public login_response ()
    {
    }

    public bool ok { get; set; }
    public string name { get; set; }
    public string[] roles { get; set; }
    public string auth_session {get; set;}

    //{"ok":true,"name":null,"roles":["_admin"]}



}
