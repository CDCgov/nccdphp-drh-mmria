using System;
using System.Collections.Generic;

namespace mmria.common.model.couchdb;

public sealed class get_sortable_view_reponse_object_key_header<T>
{
    public int offset { get; set; } //": 0,
    public System.Collections.Generic.List<get_sortable_view_response_object_key_item<T>> rows { get; set; }
    public int total_rows { get; set; } 
    
    public get_sortable_view_reponse_object_key_header () 
    {
        this.rows = new System.Collections.Generic.List<get_sortable_view_response_object_key_item<T>> ();
    }

    public get_sortable_view_reponse_object_key_header
    (
        int p_offset,
        System.Collections.Generic.List<get_sortable_view_response_object_key_item<T>> p_rows,
        int p_total_rows 
    ) 
    {
        this.offset = p_offset;
        this.rows = p_rows;
        this.total_rows = p_total_rows;
    }

}


public sealed class get_sortable_view_response_object_key_item<T>
{
    public get_sortable_view_response_object_key_item(){}

    public string id { get; set; } //": "16e458537602f5ef2a710089dffd9453",
    public object[] key { get; set; } //": "16e458537602f5ef2a710089dffd9453",

    //public T value {  get; set; }
    public T value {  get; set; }


}


public sealed class Compare_Session_Event_By_DateCreated<T> : IComparer<get_sortable_view_response_item<session_event>>
{
    public int Compare(get_sortable_view_response_item<session_event> a, get_sortable_view_response_item<session_event> b)
    {
        if (a.value.date_created > b.value.date_created)
        {
            return -1;
        }
            
        if (a.value.date_created < b.value.date_created)
        {
            return 1;
        }
        else
        {
            return 0;
        }
            
    }
}

/*
public sealed class Compare_Session_Event_By_DateCreated<T> : IComparer<get_sortable_view_response_object_key_item<T>>
{
    public int Compare(get_sortable_view_response_object_key_item<T> a, get_sortable_view_response_object_key_item<T> b)
    {
        var a_date = a.key[0] as DateTime?;
        var b_date = b.key[0] as DateTime?;


        if (a_date.Value > b_date.Value)
        {
            return -1;
        }
            
        if (a_date.Value < b_date.Value)
        {
            return 1;
        }
        else
        {
            return 0;
        }
            
    }
}

    */

