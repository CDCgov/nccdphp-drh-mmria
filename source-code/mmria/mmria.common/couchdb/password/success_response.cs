using System;

namespace mmria.common.model.couchdb.password;

public sealed class success_response
{
    public success_response ()
    {
    }
    //{"ok":true,"id":"org.couchdb.user:jan","rev":"2-ed293d3a0ae09f0c624f10538ef33c6f"}

    public bool ok {get;set;}
    public string id { get;set;}
    public string rev { get; set;}

}


