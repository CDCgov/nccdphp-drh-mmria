using System;
using System.Collections.Generic;
namespace mmria.common.metadata;

public sealed class Substance_Mapping_Item
{
    public string source_value { get; set; }
    public string target_value { get; set; }
}


public sealed class Substance_Mapping
{
    public string _id { get; set; }
    public string _rev { get; set; }

    public Dictionary<string, List<Substance_Mapping_Item>> substance_lists { get; set; }
}
