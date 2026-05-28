using System;

namespace mmria.common.model.couchdb;

public sealed class view_item<T>
{
    public view_item(){}

    public string id { get; set; } //": "16e458537602f5ef2a710089dffd9453",
    public string key { get; set; } //": "16e458537602f5ef2a710089dffd9453",
    public T value {  get; set; }

}

public sealed class view_response<T>
{
    public view_response() 
    {
        this.rows = new System.Collections.Generic.List<view_item<T>> ();
    }

    public view_response 
    (
        int p_offset,
        System.Collections.Generic.List<view_item<T>> p_rows,
        int p_total_rows 
    ) 
    {
        this.offset = p_offset;
        this.rows = p_rows;
        this.total_rows = p_total_rows;
    }

    public int offset { get; set; }
    public System.Collections.Generic.List<view_item<T>> rows { get; set; }
    public int total_rows { get; set; } 
}


