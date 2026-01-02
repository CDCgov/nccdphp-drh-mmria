using System;

namespace mmria.common.model.couchdb;

public sealed class UserCTX
{
    public UserCTX ()
    {
    }

    public string name { get; set;}
    public string[] roles { get; set;}
}


