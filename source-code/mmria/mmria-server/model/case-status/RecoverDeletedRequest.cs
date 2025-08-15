using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace mmria.server.model.recover_deleted;

public sealed class Request
{
    public string StateDatabase { get; set; }
    public string RecordId { get; set; }
    public string Role { get; set; } = "jurisdiction_admin";
}


public sealed class RequestResponse
{
    public RequestResponse()
    {
        Detail = new List<mmria.common.model.couchdb.audit.Audit_Detail_View>();
    }
    public List<mmria.common.model.couchdb.audit.Audit_Detail_View> Detail { get; set; }

    public string SearchText { get; set; }

    public bool is_cdc_admin { get; set; }

}

    public sealed class Result
{
    public string _id { get; set; }
    public bool Ok { get; set; }
}
