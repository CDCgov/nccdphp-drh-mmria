using System;

namespace mmria.common.model.couchdb;

public sealed class document_put_response
{
    public string auth_session {get; set;}

    public Boolean ok { get; set; }
    public string id { get; set; }
    public string rev { get; set; }

    public string error_description { get; set; }

    // Machine-readable rejection code — null on success and on legacy rejections
    // that predate structured codes. See mmria.common.model.couchdb.SaveErrorCodes.
    public string error_code { get; set; }

    public document_put_response ()
    {
        
    }
}


