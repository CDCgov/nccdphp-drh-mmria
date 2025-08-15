using System;
namespace mmria.common.metadata;

public sealed class node
{
    public node() {} 

    public string prompt { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string list_item_data_type { get; set; }
    public string data_type { get; set; }
    public string cardinality { get; set; } 
    public value_node[] values  { get; set; } 
    public node[] children { get; set; } 

    public bool? is_core_summary { get; set; } 
    public bool? is_required { get; set; } 
    public bool? is_read_only { get; set; } 
    public bool? is_hidden { get; set; } 

    public bool? is_multiselect{ get; set; } 
    
    public bool? is_save_value_display_description { get; set; } 
    public int? list_display_size { get; set; }

    public string control_style { get; set; } 
    public string default_value { get; set; } 
    public string pre_fill { get; set; } 

    public string regex_pattern { get; set; } 

    public string decimal_precision { get; set; } 

    public string x_axis { get; set; }
    public string x_label { get; set; } 
    public string x_type { get; set; } 


    public string y_axis { get; set; } 
    public string y_label { get; set; } 
    public string y_type { get; set; } 

    public string x_start { get; set; } 

    public string path_reference { get; set; } 
    public string mirror_reference { get; set; }

    public string pre_populate_reference { get; set; }


    public string description { get; set; }

    public string data_summary_report_description { get; set; }

    public string validation_description { get; set; }

    public System.Dynamic.ExpandoObject validation { get; set; } 
    public System.Dynamic.ExpandoObject  onfocus { get; set; } 
    public System.Dynamic.ExpandoObject  onchange { get; set; } 
    public System.Dynamic.ExpandoObject  onblur { get; set; } 
    public System.Dynamic.ExpandoObject  onclick { get; set; } 


    public string[] tags { get; set; }

    public string sass_export_name { get; set; }
    public string max_value { get; set; } 
    public string min_value { get; set; } 

    public string max_length { get; set; } 


    public string grid_template { get; set; } 
    public string grid_template_areas { get; set; } 
    public string grid_gap { get; set; } 
    public string grid_auto_flow { get; set; } 
    public string grid_row { get; set; } 
    public string grid_column { get; set; } 
    public string grid_area { get; set; }


    public string other_specify_list { get;set;}

    public string disable_on_selected_item_list { get; set; }

    public string parent_list { get;set;}
    public string top { get; set; }
    public string left { get; set; }
    public string width { get; set; }
    public string height { get; set; }
    public string padding { get; set; }
    public string text_align { get; set; }

    public bool? is_not_selectable { get; set; } 
    public string sort_order { get; set; }

    public string committee_description { get; set; }

    public bool? is_display_field_length { get; set; }



}


