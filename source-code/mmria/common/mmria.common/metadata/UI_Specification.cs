using System;
using System.Collections.Generic;

namespace mmria.common.metadata;

public sealed class Dimension
{
    public string style { get; set; }
    public float? x { get; set; }
    public float? y { get; set; }
    public float? width { get; set; }
    public float? height { get; set; }
}

public sealed class Dimension_Object
{
    public Dimension prompt { get; set; }
    public Dimension control { get; set; }
    
}
public sealed class UI_Specification
{
    public UI_Specification()
    {
        this.form_design = new Dictionary<string, Dimension_Object>(StringComparer.OrdinalIgnoreCase);
    }
    public string _id { get; set; }

    public string _rev { get; set; }

    public string css { get; set; }
    
    public string data_type { get; set; }
    public string date_created { get; set; } 
    public string created_by { get; set; } 
    public string date_last_updated { get; set; } 
    public string last_updated_by { get; set; }         

    public string name { get; set; }

    public string metadata_id { get; set; }
    public Dimension dimension { get; set; } 

    public Dictionary<string, Dimension_Object> form_design { get; set; } 

}
