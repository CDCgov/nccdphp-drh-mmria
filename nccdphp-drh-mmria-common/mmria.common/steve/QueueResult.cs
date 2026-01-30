using System;
using System.Collections.Generic;
using System.IO;

namespace mmria.common.steve;

public sealed class QueueItem
{
    public DateTime DateCreated { get;set;}
    public string CreatedBy	 { get;set;}
    public DateTime DateLastUpdated	 { get;set;}
    public string LastUpdatedBy	 { get;set;}
    public string FileName	 { get;set;}
    public string ExportType	 { get;set;}
    public string Status { get;set;}
}
public sealed class QueueResult
{
    public QueueResult() 
    {
        this.Items = new();
    }
    public List<QueueItem> Items { get;set;}
}