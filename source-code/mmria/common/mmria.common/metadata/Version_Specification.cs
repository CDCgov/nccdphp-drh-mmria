using System;
using System.Collections.Generic;

namespace mmria.common.metadata;

public sealed class csv_info
{
    public csv_info() {}

    public string file_name { get; set; }
    public string field_name { get; set; } 
}

public enum publish_status_enum
{
    draft,
    final
}
public sealed class Version_Specification
{
    public Version_Specification()
    {
        this.schema = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        this.definition_set = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        this.path_to_csv_all = new Dictionary<string, csv_info>(StringComparer.OrdinalIgnoreCase);
        //this.path_to_csv_all_field = new Dictionary<string, csv_info>(StringComparer.OrdinalIgnoreCase);
        //this.path_to_csv_core_file = new Dictionary<string, csv_info>(StringComparer.OrdinalIgnoreCase);
        this.path_to_csv_core = new Dictionary<string, csv_info>(StringComparer.OrdinalIgnoreCase);
        this.data_type = "version-specification";
    }
    public string _id { get; set; }

    public string _rev { get; set; }

    public string data_type { get; set; }
    public string date_created { get; set; } 
    public string created_by { get; set; } 
    public string date_last_updated { get; set; } 
    public string last_updated_by { get; set; }         

    public string name { get; set; }

    public publish_status_enum publish_status { get; set; }


    public string calculations_js { get; set; }
    public string metadata { get; set; }

    public string metadata_id { get; set; }
    public string metadata_rev { get; set; }
    public string ui_specification { get; set; }
    public string ui_specification_id { get; set; }
    public string ui_specification_rev { get; set; }

    public Dictionary<string, string> schema { get; set; }
    public Dictionary<string, string> definition_set { get; set; } 
    public Dictionary<string, csv_info> path_to_csv_all { get; set; }
    //public Dictionary<string, csv_info> path_to_csv_all_field { get; set; }
    //public Dictionary<string, csv_info> path_to_csv_core_file { get; set; }
    public Dictionary<string, csv_info> path_to_csv_core { get; set; }

    public System.Dynamic.ExpandoObject _attachments { get; set; } 

}
