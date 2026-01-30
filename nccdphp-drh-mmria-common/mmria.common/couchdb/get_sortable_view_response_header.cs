using System;

namespace mmria.common.model.couchdb;

public sealed class get_sortable_view_reponse_header<T>
{
    public int offset { get; set; } //": 0,
    public System.Collections.Generic.List<get_sortable_view_response_item<T>> rows { get; set; }
    public int total_rows { get; set; } 
    
    public get_sortable_view_reponse_header () 
    {
        this.rows = new System.Collections.Generic.List<get_sortable_view_response_item<T>> ();
    }

    public get_sortable_view_reponse_header
    (
        int p_offset,
        System.Collections.Generic.List<get_sortable_view_response_item<T>> p_rows,
        int p_total_rows 
    ) 
    {
        this.offset = p_offset;
        this.rows = p_rows;
        this.total_rows = p_total_rows;
    }

}


public sealed class get_sortable_view_response_item<T>
{
    public get_sortable_view_response_item(){}

    public string id { get; set; } //": "16e458537602f5ef2a710089dffd9453",
    public string key { get; set; } //": "16e458537602f5ef2a710089dffd9453",

    //public T value {  get; set; }
    public T value {  get; set; }


}

/*
public sealed class get_sortable_view_response_item_value
{
    public get_sortable_view_response_item_value(){}

    public string rev { get; set; } //": "16e458537602f5ef2a710089dffd9453",

}
*/
