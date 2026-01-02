using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace mmria.common.ije;

public sealed class Batch
{
    public enum StatusEnum
    {
        Validating,
        InProcess,
        Finished,
        FinishedSynchronized,
        Deleted,
        DeletedSynchronized,

        BatchRejected
    }
    public StatusEnum Status { get; init;}
    public string id { get; init;}
    public string _rev { get; init; }
    public DateTime? date_created { get; init; } 
    public string created_by { get; init; } 
    public DateTime? date_last_updated { get; init; } 
    public string last_updated_by { get; init; } 

    public string reporting_state { get; init;}
    public string mor_file_name { get; init;}
    public string fet_file_name { get; init;}
    public string nat_file_name { get; init;}
    public DateTime? ImportDate { get; init;}
    public List<BatchItem> record_result { get; init;}

    public string StatusInfo { get; init; }
}

public sealed class BatchStatusMessage
{
    public string id {get;init;}
    public Batch.StatusEnum status {get;init;}
}

public sealed class BatchRemoveDataMessage
{
    public string id {get;init;}
    public DateTime date_of_removal {get;init;}
}


