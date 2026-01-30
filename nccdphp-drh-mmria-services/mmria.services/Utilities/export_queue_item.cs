using System;
using System.Collections.Generic;

namespace mmria.services;

public sealed class export_queue_item
{
    public enum ExportTypeEnum
    {
        csv,
        excel
    }
    public export_queue_item ()
    {
        //de_identified_field_set= new HashSet<string>();
        //case_set = new List<string>();
    }

    public string _id {get; set;}

    
    public string _rev {get; set;}

    public string data_type { get; set; } = "export";
    public bool? _deleted { get; set;}
    public DateTime? date_created { get; set;}
    public string created_by { get; set;}
    public DateTime? date_last_updated { get; set;}
    public string last_updated_by { get; set;}
    public string file_name { get; set;}
    public string export_type { get; set;}
    public string status { get; set;}

    public string all_or_core {get; set;}
    public string grantee_name {get; set;}
    public string is_encrypted {get; set;}
    public string zip_key {get; set;}
    
    public string de_identified_selection_type {get; set;}
    public string[] de_identified_field_set {get; set;}

    public string case_filter_type {get; set;}

    public string case_file_type {get; set;}
    public string[] case_set {get; set;}

    public ExportTypeEnum ExportType { get; set; } = ExportTypeEnum.csv;
    public string[] field_set {get; set;}

    public int[] pregnancy_relatedness {get; set;}

    public bool? include_blank_date_of_reviews {get; set;}

    public bool? include_blank_date_of_deaths {get; set;}
    public DateTime? date_of_review_begin {get; set;}
    public DateTime? date_of_review_end {get; set;}
    public DateTime?  date_of_death_begin {get; set;}
    public DateTime?  date_of_death_end {get; set;}

}
