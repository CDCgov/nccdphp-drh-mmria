using System;
namespace mmria.common.metadata;
	
public sealed class value_node
{
    public string display { get; set; }
    public string description { get; set; }
    public string value { get; set; }

    public string parent_value { get; set; }

    public bool? is_not_selectable { get; set; }

    public bool? is_mutually_exclusive { get; set; }

    public value_node()
    {


    }
}



