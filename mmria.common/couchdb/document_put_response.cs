using System;

namespace mmria.common.model.couchdb;

public sealed class document_put_response
{
    public string auth_session {get; set;}

    public Boolean ok { get; set; }
    public string id { get; set; }
    public string rev { get; set; }

    public string error_description { get; set; }

    public document_put_response ()
    {
        
    }
}


