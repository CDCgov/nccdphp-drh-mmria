using System;

namespace mmria.common.model.couchdb.password;

public sealed class error_response
{
    public error_response ()
    {
    }

    public string error { get; set; }
    public string reason { get; set; }

}


