using System;
using System.Collections.Generic;
namespace mmria.common.metadata;

public sealed class app
{
    public string _id { get; set; }
    public string _rev { get; set; }
    public string name { get; set; } = "mmria";
    public string prompt { get; set; } = "mmria app (ned)";
    public string type { get; set; } = "app";

    public string version { get; set; }
    public string date_created { get; set; } 
    public string created_by { get; set; } 
    public string date_last_updated { get; set; } 
    public string last_updated_by { get; set; } 
    public node[] lookup { get; set; } 



    public System.Collections.Generic.Dictionary<string,Attachment_Item> _attachments { get; set; } 

    //public System.Dynamic.ExpandoObject validation { get; set; } 
    //public System.Dynamic.ExpandoObject global { get; set; } 

    public node[] children { get; set; } 

    Dictionary<string,mmria.common.metadata.value_node[]> look_up_dictionary;

    public app()
    {
        _attachments = new();
        look_up_dictionary = new();

    }

    public node AsNode()
    {
        return new node()
        {
            name = this.name,
            prompt = this.prompt,
            type = this.type
        };
    }


    public List<Metadata_Node> Flatten()
    {
		var result = new List<Metadata_Node>();

        if(look_up_dictionary.Count == 0)
        {
           look_up_dictionary = get_look_up(); 
        }

		foreach(var node in this.children)
		{
			var current_type = node.type.ToLowerInvariant();

            result.Add(new Metadata_Node()
				{
					is_multiform = false,
					is_grid = false,
					path = node.name,
					Node = node,
					sass_export_name = node.sass_export_name
				});
                
                
            if(current_type == "form")
			{
				if
				(
					node.cardinality == "+" ||
					node.cardinality == "*"
				)
				{
					get_metadata_node_by_type(ref result, node, true, false, node.name);
				}
				else
				{
					get_metadata_node_by_type(ref result, node, false, false, node.name);
				}
			}
		}
		return result;
    }

	private void get_metadata_node_by_type(ref List<Metadata_Node> p_result, mmria.common.metadata.node p_node, bool p_is_multiform, bool p_is_grid, string p_path)
	{
		var current_type = p_node.type.ToLowerInvariant();

        var value_to_display = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var display_to_value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if
        (
            current_type == "list"
        )
        {

            if(!string.IsNullOrWhiteSpace(p_node.path_reference))
            {
                //var key = "lookup/" + p_node.name;
                var key = p_node.path_reference;
                if(look_up_dictionary.ContainsKey(key))
                {
                    var values = look_up_dictionary[key];

                    p_node.values = values;
                }
            }

            foreach(var value_item in p_node.values)
            {
                var value = value_item.value;
                var display = value_item.display;

                if(!value_to_display.ContainsKey(value))
                {
                    value_to_display.Add(value, display);
                }

                if(!display_to_value.ContainsKey(display))
                {
                    display_to_value.Add(display, value);
                }
            }
        }

        p_result.Add(new Metadata_Node()
        {
            is_multiform = p_is_multiform,
            is_grid = p_is_grid,
            path = p_path,
            Node = p_node,
            value_to_display = value_to_display,
            display_to_value = display_to_value,
            sass_export_name = p_node.sass_export_name
        });
    
		
        if(p_node.children != null)
		{
			foreach(var node in p_node.children)
			{
				if(current_type == "grid")
				{
					get_metadata_node_by_type(ref p_result, node, p_is_multiform, true, p_path + "/" + node.name);
				}
				else
				{
					get_metadata_node_by_type(ref p_result, node, p_is_multiform, p_is_grid, p_path + "/" + node.name);
				}
			}
		}
	}

    private Dictionary<string,mmria.common.metadata.value_node[]> get_look_up()
	{
		var result = new Dictionary<string,mmria.common.metadata.value_node[]>(StringComparer.OrdinalIgnoreCase);

		foreach(var node in this.lookup)
		{
			result.Add("lookup/" + node.name, node.values);
		}
		return result;
	}
}


