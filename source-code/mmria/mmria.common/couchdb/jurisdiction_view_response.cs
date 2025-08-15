using System;

namespace mmria.common.model.couchdb;


/*
{
    "total_rows":2,
    "offset":0,
    "rows":[
            {"id":"02279162-6be3-49e4-930f-42eed7cd4706","key":["hubbard","silvia","2016"],"value":null},
            {"id":"cb9f90c5-fc9b-1530-7d20-4891f9a40027","key":["sumo","maiko","2017"],"value":null}
    ]
}

home_record/first_name
home_record/middle_name
home_record/last_name
home_record/date_of_death/year
home_record/date_of_death/month

date_created
created_by
date_last_updated
last_updated_by

home_record/record_id
home_record/agency_jurisdiction_id
committee_review/date_of_review

http://localhost:5984/mmrds/_design/sortable/_view/all

find one
    by_date?key="2009/01/30 18:04:11"

find many
    by_date?startkey="2010/01/01 00:00:00"&endkey="2010/02/00 00:00:00"


reverse sort
    descending=true
    also endkey=1&descending=true: 


doc.date_created, 
date_created:doc.date_created, 
created_by:doc.created_by,
date_last_updated:doc.date_last_updated,
last_updated_by:doc.last_updated_by,
role_name:doc.role_name,
user_id:doc.user_id,
parent_id:doc.parent_id,
jurisdiction_id:doc.jurisdiction_id,
is_active:doc.is_active,
effective_start_date:doc.effective_start_date,
effective_end_date:doc.effective_end_date

*/





public sealed class jurisdiction_view_sortable_item
{
    public jurisdiction_view_sortable_item () { }

    public DateTime? date_created{ get; set; }
    public string created_by{ get; set; }
    public DateTime? date_last_updated{ get; set; }
    public string last_updated_by{ get; set; }
    public string role_name{ get; set; }
    public string user_id{ get; set; }
    public string parent_id{ get; set; }
    public string jurisdiction_id{ get; set; }
    public bool? is_active{ get; set; }
    public DateTime? effective_start_date{ get; set; }
    public DateTime? effective_end_date{ get; set; }


}

/*

public sealed class jurisdiction_view_item
{
    public jurisdiction_view_item(){}

    public string id { get; set; } //": "16e458537602f5ef2a710089dffd9453",
    public string key { get; set; } //": "16e458537602f5ef2a710089dffd9453",
    public jurisdiction_view_sortable_item value {  get; set; }

}

public sealed class jurisdiction_view_response
{
    public jurisdiction_view_response () 
    {
        this.rows = new System.Collections.Generic.List<jurisdiction_view_item> ();
    }

    public jurisdiction_view_response 
    (
        int p_offset,
        System.Collections.Generic.List<jurisdiction_view_item> p_rows,
        int p_total_rows 
    ) 
    {
        this.offset = p_offset;
        this.rows = p_rows;
        this.total_rows = p_total_rows;
    }


    public int offset { get; set; } //": 0,
    public System.Collections.Generic.List<jurisdiction_view_item> rows { get; set; }
    public int total_rows { get; set; } 
}

 */


