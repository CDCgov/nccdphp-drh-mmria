using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace mmria.server.model.maiden_name;

public sealed class MaidenNameRequest
{
    public string StateDatabase { get; set; }
    public string RecordId { get; set; }
    public string Role { get; set; } = "cdc_admin";
}

public sealed class MaidenNameDetail
{
    public string _id { get; set; }
    public string RecordId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MiddleName { get; set; }

    // public string DateOfDeath { get; set; }
    // public string StateOfDeath { get; set; }
    public string LastUpdatedBy { get; set; }
    public DateTime? DateLastUpdated { get; set; }
    public string AgencyCaseId { get; set; }
    public string LocalFileNumber { get; set; }
    public string StateFileNumber { get; set; }

    public string MaidenName { get; set; }
    public string MaidenNameReplacement { get; set; }

    public int? CaseStatus { get; set; }
    public string CaseStatusDisplay { get; set; }
    

    public string StatusText { get; set; }
    public string StateDatabase {get; set; }

    public string Role { get; set; }  = "cdc_admin";
}

public sealed class MaidenNameRequestResponse
{
    public MaidenNameRequestResponse()
    {
        MaidenNameDetail = new List<MaidenNameDetail>();
    }
    public List<MaidenNameDetail> MaidenNameDetail { get; set; }

    public string SearchText { get; set; }   

}

    public sealed class MaidenNameResult
{
    public string _id { get; set; }
    public bool Ok { get; set; }
}
