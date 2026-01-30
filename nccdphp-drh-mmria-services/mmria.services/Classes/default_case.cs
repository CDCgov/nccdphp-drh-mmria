using System;
using System.Collections.Generic;

namespace mmria.services.vitalsimport;

public static class default_case
{
    static public void create(mmria.common.metadata.app p_metadata, IDictionary<string,object> p_parent, bool p_create_grid = false)
    {
        p_parent["_id"] = System.Guid.NewGuid().ToString();
        for(var i = 0; i < p_metadata.children.Length; i++)
        {
            var child = p_metadata.children[i];
            create(child, p_parent, p_create_grid);
        }
    }
    static public void create(mmria.common.metadata.node p_metadata, IDictionary<string,object> p_parent, bool p_create_grid = false)
    {
        switch(p_metadata.type.ToLower())
        {
            case "grid":
            var grid = new List<object>();
            
            if(p_create_grid)
            {
                if(p_metadata.name != "recommendations_of_committee")
                {
                    var sample_grid_item = new Dictionary<string,object>();
                    for(var i = 0; i < p_metadata.children.Length; i++)
                    {
                        var child = p_metadata.children[i];
                        create(child, sample_grid_item);
                    }
                    grid.Add(sample_grid_item);
                }
            }
            p_parent.Add(p_metadata.name, grid);
            break;
            case "form":
            var temp_object = new Dictionary<string,object>();
            for(var i = 0; i < p_metadata.children.Length; i++)
            {
                var child = p_metadata.children[i];
                create(child, temp_object, p_create_grid);
            }

            if(!string.IsNullOrWhiteSpace(p_metadata.cardinality))
            {
                switch(p_metadata.cardinality)
                {
                    case "+":
                    case "*":
                        if(p_create_grid)
                        {
                            var list = new List<object>();
                            list.Add(temp_object);
                            p_parent[p_metadata.name] = list;
                        }
                        else
                        {
                            p_parent[p_metadata.name] = new List<object>();
                        }
                        break;
                    case "?":
                    case "1":
                    default:
                        p_parent[p_metadata.name] = temp_object;
                        break;
                }
            }
            else
            {
                p_parent[p_metadata.name] = temp_object;
            }

            break;

            case "group":
            var group = new Dictionary<string,object>();
            for(var i = 0; i < p_metadata.children.Length; i++)
            {
                var child = p_metadata.children[i];
                create(child, group, p_create_grid);
                
            }
            p_parent[p_metadata.name] = group;
            break;
            case "app":
                p_parent["_id"] = System.Guid.NewGuid().ToString();
                for(var i = 0; i < p_metadata.children.Length; i++)
                {
                    var child = p_metadata.children[i];
                    create(child, p_parent, p_create_grid);
                }
            //p_parent["host_state"] = sanitize_encodeHTML(window.location.host.split("-")[0]);
            break;
            case "string":
            case "textarea":
            case "address":
                if(!string.IsNullOrWhiteSpace(p_metadata.default_value))
                {
                    p_parent[p_metadata.name] = p_metadata.default_value;
                }
                else if(!string.IsNullOrWhiteSpace(p_metadata.pre_fill))
                {
                    p_parent[p_metadata.name] = new String(p_metadata.pre_fill);
                }
                else
                {
                    p_parent[p_metadata.name] = "";
                }
                break;
            case "number":
                    if(!string.IsNullOrWhiteSpace(p_metadata.default_value))
                    {
                        p_parent[p_metadata.name] = double.Parse(p_metadata.default_value);
                    }
                    else
                    {
                        p_parent[p_metadata.name] = "";
                    }
                break;
            case "boolean":
                    if(!string.IsNullOrWhiteSpace(p_metadata.default_value))
                    {
                    p_parent[p_metadata.name] = bool.Parse(p_metadata.default_value);
                    }
                    else
                    {
                    p_parent[p_metadata.name] = false;
                    }
                    break;
            case "list":
            case "yes_no":
                if(p_metadata.is_multiselect?? false)
                {
                    p_parent[p_metadata.name] = new List<object>();
                }
                else
                {
                    p_parent[p_metadata.name] = "9999";
                }
                    
                break;
            case "date":
            case "datetime":
                    if(!string.IsNullOrWhiteSpace(p_metadata.default_value))
                    {
                        p_parent[p_metadata.name] = DateTime.Parse(p_metadata.default_value);
                    }
                    else
                    {
                    p_parent[p_metadata.name] = "";
                    }
                    break;
            case "time":
                //p_parent[p_metadata.name] = new Date("2016-01-01T00:00:00.000Z");
                p_parent[p_metadata.name] = "";
                break;
            case "hidden":
                p_parent[p_metadata.name] = "";
                break;
            case "jurisdiction":
                p_parent[p_metadata.name] = "/";
                break;           
            case "label":
            case "button":
            case "always_enabled_button":
            case "chart":
                    break;
            default:
                Console.WriteLine("create_default_object not processed", p_metadata);
            break;
        }
    }

}
