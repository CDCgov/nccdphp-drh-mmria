using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;

namespace strongcase;

public class Template_Writer_S_Set
{

    Dictionary<string, mmria.common.metadata.Metadata_Node> dictionary_set;
    string name_space = string.Empty;
    public Template_Writer_S_Set(Dictionary<string, mmria.common.metadata.Metadata_Node> _dictionary_set, string p_namespace)
    {
        dictionary_set = _dictionary_set;
        name_space = p_namespace;
    }


    public async Task Execute()
    {
        var get_set_template = System.IO.File.ReadAllText("mmria_case.set.s.template.cs.text").Replace(".v241001;",$".{name_space};");
        var template_keys = new Dictionary<string, System.Text.StringBuilder>()
        {
            {"//{set_string}", new System.Text.StringBuilder()},
            
            {"//{set_double}", new System.Text.StringBuilder()},
            {"//{set_boolean}", new System.Text.StringBuilder()},
            {"//{set_datetime}", new System.Text.StringBuilder()},
            {"//{set_date_only}", new System.Text.StringBuilder()},
            {"//{set_time_only}", new System.Text.StringBuilder()},
            {"//{set_list_of_double}", new System.Text.StringBuilder()},
            {"//{set_list_of_string}", new System.Text.StringBuilder()},
 
        };

        foreach(var kvp in dictionary_set.Where( kv => kv.Value.is_multiform == false && kv.Value.is_grid == false))
        {
            var node = kvp.Value.Node;
            var meta_node = kvp.Value;

            var new_name = kvp.Key.Replace("/",".");
            var last_index = new_name.LastIndexOf(".");

            if(last_index > -1)
            {
                var pre_name = new_name[..last_index];
                var post_name = new_name[last_index..];

                if(post_name.EndsWith(".class")) 
                {
                    post_name = ".@class";
                }
                
                new_name = pre_name + post_name;

            }

            var value_string = 
                        $"""
                                    case "{kvp.Key}":
                                        {new_name} = value;
                                        result = true;
                                    break;
                        """;

            switch(node.type.ToLower())
            {
                case "jurisdiction":
                case "textarea":
                case "html_area":
                case "string":
                    template_keys["//{set_string}"].AppendLine(value_string);
                
                /*
            case "_id":
                this._id = value;
            break;
                */
                
                break;
                case "number":
                    template_keys["//{set_double}"].AppendLine(value_string);
                break;

                case "datetime":
                    template_keys["//{set_datetime}"].AppendLine(value_string);
                    
                break;
                case "date":
                    template_keys["//{set_date_only}"].AppendLine(value_string);
                break;
                case "time":
                    template_keys["//{set_time_only}"].AppendLine(value_string);
                break;
                case "boolean":
                    template_keys["//{set_boolean}"].AppendLine(value_string);
                break;
                case "hidden":
                    if(string.IsNullOrWhiteSpace(node.data_type))
                    {
                        template_keys["//{set_string}"].AppendLine(value_string);
                    }
                    else switch(node.data_type.ToLower())
                    {
                        case "number":
                            template_keys["//{set_double}"].AppendLine(value_string);
                        break;
                        case "boolean":
                            template_keys["//{set_boolean}"].AppendLine(value_string);
                        break;
                        case "datetime":
                            template_keys["//{set_datetime}"].AppendLine(value_string);
                            
                        break;
                        case "date":
                            template_keys["//{set_date_only}"].AppendLine(value_string);
                        break;
                        case "time":
                            template_keys["//{set_time_only}"].AppendLine(value_string);
                        break;
                        default:
                            template_keys["//{set_string}"].AppendLine(value_string);
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
                            template_keys["//{set_list_of_string}"].AppendLine(value_string);
                        }
                        else if(node.data_type.ToLower() == "number")
                        {
                            template_keys["//{set_list_of_double}"].AppendLine(value_string);
                        }
                        else
                        {
                            template_keys["//{set_list_of_string}"].AppendLine(value_string);
                        }
                    }
                    else
                    {
                        if(string.IsNullOrWhiteSpace(node.data_type))
                        {
                            template_keys["//{set_string}"].AppendLine(value_string);
                        }
                        else if(node.data_type.ToLower() == "number")
                        {
                            template_keys["//{set_double}"].AppendLine(value_string);
                        }
                        else
                        {
                            template_keys["//{set_string}"].AppendLine(value_string);
                        }
                    }
                break;
                
            }
        }


        foreach(var kvp in template_keys)
        {
            get_set_template = get_set_template.Replace(kvp.Key, kvp.Value.ToString());
        }

        System.IO.File.WriteAllText("output/mmria_case.set.s.cs", get_set_template);

    }

}