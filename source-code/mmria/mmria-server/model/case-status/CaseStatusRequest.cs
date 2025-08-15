using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace mmria.server.model.casestatus;

public sealed class CaseStatusRequest
{
    public string StateDatabase { get; set; }
    public string RecordId { get; set; }
    public string Role { get; set; } = "jurisdiction_admin";
}

public sealed class CaseStatusDetail
{
    public string _id { get; set; }
    public string RecordId { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MiddleName { get; set; }

    public string DateOfDeath { get; set; }
    public string StateOfDeath { get; set; }
    public string LastUpdatedBy { get; set; }
    public DateTime? DateLastUpdated { get; set; }

    public int? CaseStatus { get; set; }
    public string CaseStatusDisplay { get; set; }
    public string AgencyCaseId { get; set; }
    public string LocalFileNumber { get; set; }
    public string StateFileNumber { get; set; }

    public string StateDatabase {get; set; }

    public bool is_cdc_admin { get; set; }

    public string Role { get; set; }  = "jurisdiction_admin";
}

public sealed class CaseStatusRequestResponse
{
    public CaseStatusRequestResponse()
    {
        CaseStatusDetail = new List<CaseStatusDetail>();
    }
    public List<CaseStatusDetail> CaseStatusDetail { get; set; }

    public string SearchText { get; set; }

    public bool is_cdc_admin { get; set; }

}

    public sealed class CaseStatusResult
{
    public string _id { get; set; }
    public bool Ok { get; set; }
}
