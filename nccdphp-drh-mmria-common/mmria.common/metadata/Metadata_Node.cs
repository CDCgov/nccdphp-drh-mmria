using System.Collections.Generic;

namespace mmria.common.metadata;

public sealed class Metadata_Node
{
    public Metadata_Node(){}
    public bool is_multiform { get; set; }
    public bool is_grid { get; set; }

    public string path {get;set;}

    public string hash_value {get;set;}

    public string[] tags {get;set;}

    public string sass_export_name {get;set;}
    public mmria.common.metadata.node Node { get; set; }

    public Dictionary<string,string> display_to_value { get; set; }
    public Dictionary<string,string> value_to_display { get; set; }
}