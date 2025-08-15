using System;

namespace mmria.common.model.couchdb;

public sealed class alldoc_item<T>
{
    public alldoc_item(){}

    public string id { get; set; } //": "16e458537602f5ef2a710089dffd9453",
    public string key { get; set; } //": "16e458537602f5ef2a710089dffd9453",
    public struct value {  public string rev;}

    public T doc {get;set;}

}

public sealed class alldocs_response<T>
{
    public alldocs_response (){}

    public int offset { get; set; } //": 0,
    public alldoc_item<T>[] rows { get; set; }
    public int total_rows { get; set; } 
}

