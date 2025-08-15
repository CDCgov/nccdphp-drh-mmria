using System;

namespace mmria.common.model.couchdb;

public sealed class document_put_error
{
    public string error { get; set;}
    public string reason { get; set; }

    public document_put_error ()
    {
    }
}


