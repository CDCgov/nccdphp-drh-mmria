using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;

namespace strongcase;

public class Template_Writer_M_Get
{

    Dictionary<string, mmria.common.metadata.Metadata_Node> dictionary_set;
    string name_space = string.Empty;
    public Template_Writer_M_Get(Dictionary<string, mmria.common.metadata.Metadata_Node> _dictionary_set, string p_namespace)
    {
        dictionary_set = _dictionary_set;
        name_space = p_namespace;
    }


    public async Task Execute()
    {
        var get_set_template = System.IO.File.ReadAllText("mmria_case.get.m.template.cs.text").Replace(".v241001;",$".{name_space};");
        var template_keys = new Dictionary<string, System.Text.StringBuilder>()
        {
            {"//{get_string}", new System.Text.StringBuilder()},
            {"//{get_double}", new System.Text.StringBuilder()},
            {"//{get_boolean}", new System.Text.StringBuilder()},
            {"//{get_datetime}", new System.Text.StringBuilder()},
            {"//{get_date_only}", new System.Text.StringBuilder()},
            {"//{get_time_only}", new System.Text.StringBuilder()},
            {"//{get_list_of_double}", new System.Text.StringBuilder()},
            {"//{get_list_of_string}", new System.Text.StringBuilder()},
 
        };

        foreach(var kvp in dictionary_set.Where( kv => kv.Value.is_multiform == true && kv.Value.is_grid == false))
        {
            var node = kvp.Value.Node;
            var meta_node = kvp.Value;

            var new_name = kvp.Key.Replace("/",".");
            var first_index = new_name.IndexOf(".");
            if(first_index > -1)
            {
                var pre_name = new_name[..first_index];
                var post_name = new_name[first_index..];

                if(post_name.EndsWith(".class"))
                {
                    post_name = ".@class";
                }
                
                new_name = pre_name + "[index]" + post_name;

            }


            switch(node.type.ToLower())
            {
                case "jurisdiction":
                case "textarea":
                case "html_area":
                case "string":
                    template_keys["//{get_string}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                break;
                case "number":
                    template_keys["//{get_double}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                break;

                case "datetime":
                    template_keys["//{get_datetime}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                    
                break;
                case "date":
                    template_keys["//{get_date_only}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                break;
                case "time":
                    template_keys["//{get_time_only}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                break;
                case "boolean":
                    template_keys["//{get_boolean}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                break;
                case "hidden":
                    if(string.IsNullOrWhiteSpace(node.data_type))
                    {
                        template_keys["//{get_string}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                    }
                    else switch(node.data_type.ToLower())
                    {
                        case "number":
                            template_keys["//{get_double}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                        break;
                        case "boolean":
                            template_keys["//{get_boolean}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                        break;
                        case "datetime":
                            template_keys["//{get_datetime}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                            
                        break;
                        case "date":
                            template_keys["//{get_date_only}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                        break;
                        case "time":
                            template_keys["//{get_time_only}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                        break;
                        default:
                            template_keys["//{get_string}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                        break;
                    }
                break;
                case "list":
                    if
                    (

                        node.is_multiselect.HasValue &&
                        node.is_multiselect.Value
                    )
                    {
                        if(string.IsNullOrWhiteSpace(node.data_type))
                        {
                            template_keys["//{get_list_of_string}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                        }
                        else if(node.data_type.ToLower() == "number")
                        {
                            template_keys["//{get_list_of_double}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                        }
                        else
                        {
                            template_keys["//{get_list_of_string}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                        }
                    }
                    else
                    {
                        if(string.IsNullOrWhiteSpace(node.data_type))
                        {
                            template_keys["//{get_string}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                        }
                        else if(node.data_type.ToLower() == "number")
                        {
                            template_keys["//{get_double}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                        }
                        else
                        {
                            template_keys["//{get_string}"].AppendLine($"""         "{kvp.Key}" => {new_name},""");
                        }
                    }
                break;
                
            }
        }


        foreach(var kvp in template_keys)
        {
            get_set_template = get_set_template.Replace(kvp.Key, kvp.Value.ToString());
        }

        System.IO.File.WriteAllText("output/mmria_case.get.m.cs", get_set_template);

    }

}