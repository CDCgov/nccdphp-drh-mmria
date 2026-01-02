using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mmria.common.ije;

public sealed class BatchItem
{
    public enum StatusEnum
    {
        Validating,
        InProcess,
        NewCaseAdded,
        ExistingCaseSkipped,
        ImportFailed
    }
    public StatusEnum Status { get; init;}

    public string CDCUniqueID { get; init;}
    public DateTime? ImportDate { get; init;}
    public string ImportFileName { get; init;}
    public string ReportingState { get; init;}
    public string StateOfDeathRecord { get; init;}
    public string DateOfDeath { get; init;}
    public string DateOfBirth { get; init;}
    public string LastName { get; init;}
    public string FirstName { get; init;}

    public string mmria_record_id { get; init;}
    public string mmria_id { get; init;}

    public string case_folder { get; init;}

    
    public string StatusDetail { get; init;}

}

