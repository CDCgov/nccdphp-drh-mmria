using System;

namespace mmria.common.model.couchdb;

public sealed class pmss_case_view_sortable_item
{
    public pmss_case_view_sortable_item () { }

    public int? date_of_death_year{ get; set; }
    public int? date_of_death_month{ get; set; }

    public DateTime? date_created { get; set; }
    public string created_by{ get; set; }
    public DateTime? date_last_updated{ get; set; }
    public string last_updated_by{ get; set; }

    public DateTime? date_last_checked_out{ get; set; }
    public string last_checked_out_by{ get; set; }

    public string case_folder {get; set;}

    public DateTime? case_locked_date { get; set; }

    public string host_state{ get; set; }
  
    public string jurisdiction{ get; set; }
    public int? track_year{ get; set; }
    public string pmssno{ get; set; }
    public string med_coder_check{ get; set; }
    public string med_dir_check{ get; set; }
    public string death_certificate_number{ get; set; }
    public string status{ get; set; }
    public DateTime? dod{ get; set; }
    public string month_of_death{ get; set; }
    public string day_of_death{ get; set; }
    public string year_of_death{ get; set; }
    public DateTime? dob{ get; set; }
    public string month_of_birth{ get; set; }
    public string day_of_birth{ get; set; }
    public string year_of_birth{ get; set; }
    public string agreement_status{ get; set; }
    public string version{ get; set; }

    public string residence_zip {get;set;}

    public string type {get;set;} = "pmss";

    public string mage {get;set;}
    public string manner {get;set;}
    public string cod1a {get;set;}
    public string cod1b {get;set;}
    public string cod1c {get;set;}
    public string cod1d {get;set;}
    public string cod_other_condition {get;set;}
    public string classification {get;set;}

}


public sealed class pmss_case_view_item
{
    public pmss_case_view_item(){}

    public string id { get; set; } //": "16e458537602f5ef2a710089dffd9453",
    public string key { get; set; } //": "16e458537602f5ef2a710089dffd9453",
    public pmss_case_view_sortable_item value {  get; set; }

}

public sealed class pmss_case_view_response
{
    public pmss_case_view_response () 
    {
        this.rows = new System.Collections.Generic.List<pmss_case_view_item> ();
    }

    public pmss_case_view_response 
    (
        int p_offset,
        System.Collections.Generic.List<pmss_case_view_item> p_rows,
        int p_total_rows 
    ) 
    {
        this.offset = p_offset;
        this.rows = p_rows;
        this.total_rows = p_total_rows;
    }


    public int offset { get; set; } //": 0,
    public System.Collections.Generic.List<pmss_case_view_item> rows { get; set; }
    public int total_rows { get; set; } 
}


