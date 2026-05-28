using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.services.Utilities.Exporter;

public sealed partial class exporter
{
    private Dictionary<string,mmria.common.metadata.value_node[]> lookup;

    Dictionary<string, Metadata_Node> MetaDataNode_Dictionary;

    public enum TableTypeEnum
    {
        none,
        flat,
        grid,

        multiform,
        multiform_grid
    }

    TableTypeEnum GetTableType(string p_mmria_path)
    {
        var result = TableTypeEnum.none;

        if(MetaDataNode_Dictionary.ContainsKey(p_mmria_path))
        {
            var check = MetaDataNode_Dictionary[p_mmria_path];

            if(check.is_multiform && check.is_grid)
            {
                result = TableTypeEnum.multiform_grid;
            }
            else if(check.is_multiform)
            {
                result = TableTypeEnum.multiform;
            }
            else if(check.is_grid)
            {
                result = TableTypeEnum.grid;
            }
            else
            {
                result = TableTypeEnum.flat;
            }
        }
        

        return result;
    }

    public sealed class Metadata_Node
    {
        public Metadata_Node(){}
        public bool is_multiform { get; set; }
        public bool is_grid { get; set; }

        public string path {get;set;}

        public string sass_export_name {get;set;}
        public mmria.common.metadata.node Node { get; set; }

        public Dictionary<string,string> display_to_value { get; set; }
        public Dictionary<string,string> value_to_display { get; set; }
    }
    

    private Dictionary<string,mmria.common.metadata.value_node[]> get_look_up(mmria.common.metadata.app p_metadata)
    {
        var result = new Dictionary<string,mmria.common.metadata.value_node[]>(StringComparer.OrdinalIgnoreCase);

        foreach(var node in p_metadata.lookup)
        {
            result.Add("lookup/" + node.name, node.values);
        }
        return result;
    }	

    private Dictionary<string, Metadata_Node> get_metadata_node(mmria.common.metadata.app p_metadata)
    {
        var result = new Dictionary<string, Metadata_Node>(StringComparer.OrdinalIgnoreCase);
        foreach(var node in p_metadata.children)
        {
            result.Add
            (
                node.name,
                new Metadata_Node()
                {
                    is_multiform = false,
                    is_grid = false,
                    path = node.name,
                    Node = node,
                    sass_export_name = node.sass_export_name
                }
            );
            

            if(node.type.ToLowerInvariant() == "form")
            {
                if
                (
                    node.cardinality == "+" ||
                    node.cardinality == "*"
                )
                {
                    get_metadata_node(ref result, node, true, false, node.name);
                }
                else
                {
                    get_metadata_node(ref result, node, false, false, node.name);
                }
            }
        }
        return result;
    }

    private void get_metadata_node(ref Dictionary<string, Metadata_Node> p_result, mmria.common.metadata.node p_node, bool p_is_multiform, bool p_is_grid, string p_path)
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
                if(this.lookup.ContainsKey(key))
                {
                    var values = this.lookup[key];

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

        if(!p_result.ContainsKey(p_path))
        {
            p_result.Add
            (
                p_path,
                new Metadata_Node()
                {
                    is_multiform = p_is_multiform,
                    is_grid = p_is_grid,
                    path = p_path,
                    Node = p_node,
                    value_to_display = value_to_display,
                    display_to_value = display_to_value,
                    sass_export_name = p_node.sass_export_name
                }
            );
        }
        
        
        if(p_node.children != null)
        {
            foreach(var node in p_node.children)
            {
                if(current_type == "grid")
                {
                    get_metadata_node(ref p_result, node, p_is_multiform, true, p_path + "/" + node.name);
                }
                else
                {
                    get_metadata_node(ref p_result, node, p_is_multiform, p_is_grid, p_path + "/" + node.name);
                }
            }
        }
    }
}
