using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;

namespace migrate.set;

public sealed class Process_Migrate_Charactor_to_Numeric
{
    public sealed class Substance_Mapping_Item
    {
        public string source_value { get; set; }
        public string target_value { get; set; }
    }
/*
                            "autopsy_report/toxicology/substance",
                            "prenatal/substance_use_grid/substance",
                            "social_and_environmental_profile/if_yes_specify_substances/substance"
*/
    public sealed class Substance_Mapping
    {
        public string _id { get; set; }
        public string _rev { get; set; }

        public Dictionary<string, List<Substance_Mapping_Item>> substance_lists { get; set; }
    }

    public string db_server_url;

    private string db_name;
    private string config_timer_user_name;
    private string config_timer_value;

    private string config_metadata_user_name;
    private string config_metadata_value;
    List<Metadata_Node> all_list_set;

    List<Metadata_Node> single_form_value_set;
    List<Metadata_Node> single_form_multi_value_set;
    List<Metadata_Node> single_form_grid_value_set;
    List<Metadata_Node> single_form_grid_multi_value_list_set;
    List<Metadata_Node> multiform_value_set;
    List<Metadata_Node> multiform_multi_value_set;
    List<Metadata_Node> multiform_grid_value_set;

    List<Metadata_Node> multiform_grid_multi_value_set;

    public System.Text.StringBuilder output_builder;
    public Dictionary<string, HashSet<string>> summary_value_dictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);


    public bool is_data_correction = false;
    public bool is_report_only_mode;
    private Dictionary<string,mmria.common.metadata.value_node[]> lookup;

    public Process_Migrate_Charactor_to_Numeric
    (
        string p_db_server_url, 
        string p_db_name, 
        string p_config_timer_user_name, 
        string p_config_timer_value,
        string p_config_metadata_user_name,
        string p_config_metadata_value,
        System.Text.StringBuilder p_output_builder,
        Dictionary<string, HashSet<string>> p_summary_value_dictionary,
        bool p_is_report_only_mode
    ) 
    {

        db_server_url = p_db_server_url;
        db_name = p_db_name;
        config_timer_user_name = p_config_timer_user_name;
        config_timer_value = p_config_timer_value;
        
        config_metadata_user_name = p_config_metadata_user_name;
        config_metadata_value = p_config_metadata_value;
        
        output_builder = p_output_builder;
        summary_value_dictionary = p_summary_value_dictionary;
        is_report_only_mode = p_is_report_only_mode;
    }

    public async Task execute()
    {

        DateTime begin_time = System.DateTime.Now;

        this.output_builder.AppendLine($"Character to numeric list migration started at: {begin_time.ToString("o")}");


        try
        {
            //string migration_plan_id = message.ToString();

            Console.WriteLine($"Process_Migrate_Charactor_to_Numeric Begin {begin_time}");


            string metadata_url = db_server_url + "/metadata/version_specification-20.05.07/metadata";
            cURL metadata_curl = new cURL("GET", null, metadata_url, null, config_timer_user_name, config_timer_value);
            mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_curl.execute());

            this.lookup = get_look_up(metadata);

            all_list_set = get_metadata_node_by_type(metadata, "list");

            single_form_value_set = all_list_set.Where(o=> o.is_multiform == false && o.is_grid == false && o.Node.is_multiselect == null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();
            single_form_multi_value_set = all_list_set.Where(o=> o.is_multiform == false && o.is_grid == false && o.Node.is_multiselect != null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();

            single_form_grid_value_set = all_list_set.Where(o=> o.is_multiform == false && o.is_grid == true && o.Node.is_multiselect == null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();
            single_form_grid_multi_value_list_set = all_list_set.Where(o=> o.is_multiform == false && o.is_grid == true && o.Node.is_multiselect != null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();

            multiform_value_set = all_list_set.Where(o=> o.is_multiform == true && o.is_grid == false && o.Node.is_multiselect == null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();
            multiform_multi_value_set = all_list_set.Where(o=> o.is_multiform == true && o.is_grid == false && o.Node.is_multiselect != null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();

            multiform_grid_value_set = all_list_set.Where(o=> o.is_multiform == true && o.is_grid == true && o.Node.is_multiselect == null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();
            multiform_grid_multi_value_set = all_list_set.Where(o=> o.is_multiform == true && o.is_grid == true && o.Node.is_multiselect != null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();


            var total_count = single_form_value_set.Count + single_form_grid_value_set.Count + multiform_value_set.Count + multiform_grid_value_set.Count + single_form_multi_value_set.Count + single_form_grid_multi_value_list_set.Count + multiform_multi_value_set.Count + multiform_grid_multi_value_set.Count;
            System.Console.WriteLine($"all_list_set.Count: {all_list_set.Count} total_count: {total_count}");
            System.Console.WriteLine($"is count the same: {all_list_set.Count == single_form_value_set.Count + single_form_grid_value_set.Count + multiform_value_set.Count + multiform_grid_value_set.Count + single_form_multi_value_set.Count + single_form_grid_multi_value_list_set.Count + multiform_multi_value_set.Count + multiform_grid_multi_value_set.Count}");

            string url = $"{db_server_url}/{db_name}/_all_docs?include_docs=true";


            //string url = $"{db_server_url}/{db_name}/12EDA495-D276-4B05-8D6D-CB4739211443";
            var case_curl = new cURL("GET", null, url, null, config_timer_user_name, config_timer_value);
            string responseFromServer = await case_curl.executeAsync();
            
            var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<System.Dynamic.ExpandoObject>>(responseFromServer);

            var gs = new C_Get_Set_Value(this.output_builder);

            foreach(var case_item in case_response.rows)
            {
                var case_has_changed = false;
                var case_change_count = 0;
                //IDictionary<string, object> doc = case_item.doc as IDictionary<string, object>;
                var doc = case_item.doc;
                
                if(doc != null)
                {

                    C_Get_Set_Value.get_value_result value_result = gs.get_value(doc, "_id");
                    var mmria_id = value_result.result?.ToString();
                    if(mmria_id.IndexOf("_design") > -1)
                    {
                        continue;
                    }




                    
                    if(true)
                    {
                        
                        string substance_mapping_url = "https://couchdb-test-mmria.apps.ecpaas-dev.cdc.gov/metadata/substance-mapping";
                        cURL substance_mapping_curl = new cURL("GET", null, substance_mapping_url, null, config_metadata_user_name, config_metadata_value);
                        Substance_Mapping substance_mapping = Newtonsoft.Json.JsonConvert.DeserializeObject<Substance_Mapping>(substance_mapping_curl.execute());
                        

                        var substance_path_list = new List<string>()
                        {
                            "autopsy_report/toxicology/substance",//single_form_grid
                            "prenatal/substance_use_grid/substance",//single_form_grid
                            "social_and_environmental_profile/if_yes_specify_substances/substance"//single_form_grid
                        };

                        foreach(var substance_path in  substance_path_list)
                        {
                            var source_target_dictionary = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);

                            foreach(var st in substance_mapping.substance_lists[substance_path])
                            {
                                source_target_dictionary[st.source_value.Trim()] = st.target_value.Trim();
                            }
                        

                            var lookup_node = all_list_set.Where( x => x.path == substance_path).FirstOrDefault();

                            bool is_blank = false;

/*
                            if(mmria_id == "07135EF4-B158-49BA-ADCA-F9E2C29C2B3D")
                            //if(mmria_id == "A627C0D5-BA78-47A9-93A3-A704EB466E24")
                            {
                                if(substance_path == "autopsy_report/toxicology/substance")
                                {

                                }
                            }
*/
                            C_Get_Set_Value.get_grid_value_result get_grid_value_result = gs.get_grid_value(doc, substance_path);
                            C_Get_Set_Value.get_grid_value_result get_grid_value_other_result = gs.get_grid_value(doc, $"{substance_path}_other");

                            if(!get_grid_value_result.is_error)
                            {
                                var list = get_grid_value_result.result;
                                var new_list = new List<(int, object)>();
                                var new_other_list = new List<(int, object)>();

                                var other_list = get_grid_value_other_result.result;

                                for(var i = 0; i < list.Count; i++)
                                {
                                    var (index, value) = (list[i].Item1, list[i].Item2);
                                    string value_string = "9999";
                                    if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                    {
                                        is_blank = true;
                                    }
                                    else
                                    {
                                        value_string = value.ToString();
                                    }


                                    if(value_string.Trim().ToLower() == "Other".ToLower())
                                    {
                                        if(other_list[i].Item2 == null)
                                        {
                                            continue;
                                        }

                                        var (other_index, other_value) = (other_list[i].Item1, other_list[i].Item2.ToString().Trim());
                                        if
                                        (
                                            other_value != null &&
                                            (
                                                (
                                                    source_target_dictionary.ContainsKey(other_value) && 
                                                    source_target_dictionary[other_value].ToLower() != "other" &&
                                                    source_target_dictionary[other_value].ToLower() != "9999" 
                                                )||
                                                lookup_node.value_to_display.ContainsKey(other_value)
                                            )
                                        )
                                        {

                                            if(case_change_count == 0)
                                            {
                                                case_change_count += 1;
                                                case_has_changed = true;
                                            }


                                            if(lookup_node.value_to_display.ContainsKey(other_value))
                                            {
                                                var key = lookup_node.value_to_display[other_value];
                                                var other_list_value = lookup_node.display_to_value[key];
                                                new_list.Add((index, other_list_value));
                                                new_other_list.Add((other_index, ""));

                                                this.output_builder.AppendLine($"applied substance mapping {substance_path}: {mmria_id} mapped {other_value} => {other_list_value} EXISTING");

                                            }
                                            else
                                            {
                                                new_list.Add((index, source_target_dictionary[other_value]));
                                                new_other_list.Add((other_index, ""));

                                                this.output_builder.AppendLine($"applied substance mapping {substance_path}: {mmria_id} mapped {other_value} => {source_target_dictionary[other_value]}");
                                                if
                                                (
                                                    this.summary_value_dictionary.ContainsKey($"{substance_path}_other") &&
                                                    this.summary_value_dictionary[$"{substance_path}_other"].Contains(other_value)
                                                )
                                                {
                                                    this.summary_value_dictionary[$"{substance_path}_other"].Remove(other_value);
                                                }
                                            }



                                            

                                            
                                            //System.Console.WriteLine("here");
                                        }
                                        
                                    }
                                }

                                if(new_list.Count > 0)
                                {
                                    case_has_changed = case_has_changed && gs.set_grid_value(doc, substance_path, new_list);
                                    case_has_changed = case_has_changed && gs.set_grid_value(doc, $"{substance_path}_other", new_other_list);
                                    
                                }
                            }
                        }
                    }




                    foreach(var node in  single_form_value_set)
                    {

                        bool is_blank = false;

                        value_result = gs.get_value(doc, node.path);
                        
                        if(!value_result.is_error)
                        {
                            var value = value_result.result?.ToString();
                            string value_string = "9999";
                            if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            {
                                is_blank = true;
                                //value_string = "(blank)";
                            }
                            else
                            {
                                value_string = value.ToString();
                            }

                            if(is_blank || !node.value_to_display.ContainsKey(value_string))
                            {
                                if(node.display_to_value.ContainsKey(value_string) || is_blank && node.display_to_value.ContainsKey("(blank)") )
                                {
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        case_has_changed = true;
                                    }

                                    string new_value = null;

                                    if(node.display_to_value.ContainsKey(value_string))
                                    {
                                        new_value = node.display_to_value[value];
                                    }
                                    else //if(is_blank)
                                    {
                                        new_value = "9999";
                                    }
                                    case_has_changed = case_has_changed && gs.set_value(node.path, new_value, doc);
                                }
                                else
                                {
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        case_has_changed = true;
                                    }


                                    var result_tuple = get_mapped_value(node.path, value);
                                    if(result_tuple.is_mapped)
                                    {
                                        case_has_changed = case_has_changed && gs.set_value(node.path, result_tuple.value.ToString(), doc);
                                    }
                                    else
                                    {
                                        var summary_key = node.path;
                                        if(!this.summary_value_dictionary.ContainsKey(summary_key))
                                        {
                                            this.summary_value_dictionary.Add(summary_key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                                        }
                                        if(!this.summary_value_dictionary[summary_key].Contains(value_string))
                                        {
                                            this.summary_value_dictionary[summary_key].Add(value_string);
                                        }
                                        this.output_builder.AppendLine($"item not found record_id: {mmria_id} path:{node.path} item: {value}");
                                    }
                                }
                            }

                            //process_list(ref case_has_changed, lookup, node.Node, doc);
                        }
                    }

// ***** Correction Begin
if(is_data_correction)
{
                    var path_list = new List<string>();
                    foreach(var node in  single_form_multi_value_set)
                    {
                        path_list.Add(node.path);	
                    }

                    Dictionary<string, List<Rev_Record_Value>> rev_record_value_dictionary = Get_Rev_Record_Value_List(mmria_id, path_list);

                    foreach(var kvp in rev_record_value_dictionary)
                    {
                        var rev_record_value_list = kvp.Value;

                        var list_index_begin = -1;

                        for(var i = 0; i < rev_record_value_list.Count; i++)
                        {
                            if(rev_record_value_list[i].value is List<object>)
                            {
                                list_index_begin = i;
                                break;
                            }
                        }

                        if(list_index_begin > 0)
                        {
                            var value_object = rev_record_value_list[list_index_begin].value;
                            gs.set_multi_value(kvp.Key, value_object, doc);
                        }
                    }
}
// ***** Correction End

                    foreach(var node in  single_form_multi_value_set)
                    {
                        value_result =  gs.get_value(doc, node.path);
                        if(!value_result.is_error)
                        {
                            var value_list_dynamic = value_result.result;
                            
                            if(value_list_dynamic is IList<object>)
                            {
                                var value_list = value_list_dynamic as IList<object>;
                                var is_list_modified = false;

                                if(value_list != null)
                                for(int value_index = 0; value_index < value_list.Count; value_index++)
                                {
                                    bool is_blank = false;

                                    var value_object = value_list[value_index];

                                    string value_string = "9999";
                                    

                                    if(value_object == null|| string.IsNullOrWhiteSpace(value_object.ToString()))
                                    {
                                        is_blank = true;
                                    }
                                    else
                                    {
                                            value_string = value_object.ToString();
                                    }

                                    if(is_blank || !node.value_to_display.ContainsKey(value_string))
                                    {
                                        
                                        if(node.display_to_value.ContainsKey(value_string) || is_blank && node.display_to_value.ContainsKey("(blank)") )
                                        {
                                            object new_value = null;

                                            if(node.display_to_value.ContainsKey(value_string))
                                            {
                                                new_value = node.display_to_value[value_string];
                                            }
                                            else //if(is_blank)
                                            {
                                                new_value = "9999";
                                            }

                                            value_list[value_index] = new_value;
                                            is_list_modified = true;
                                            //case_has_changed = case_has_changed && gs.set_value(node.path, new_value, doc, value_index);
                                        }
                                        else
                                        {
                                            var result_tuple = get_mapped_value(node.path, value_object);
                                            if(result_tuple.is_mapped)
                                            {
                                                value_list[value_index] = result_tuple.value.ToString();
                                                is_list_modified = true;
                                                //case_has_changed = case_has_changed && gs.set_value(node.path, result_tuple.value.ToString(), doc);
                                            }
                                            else
                                            {
                                                var summary_key = node.path;
                                                if(!this.summary_value_dictionary.ContainsKey(summary_key))
                                                {
                                                    this.summary_value_dictionary.Add(summary_key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                                                }
                                                if(!this.summary_value_dictionary[summary_key].Contains(value_string))
                                                {
                                                    this.summary_value_dictionary[summary_key].Add(value_string);
                                                }
                                                this.output_builder.AppendLine($"item not found record_id: {mmria_id} path:{node.path} item: {value_object}");
                                            }
                                        }
                                    }
                                }

                                if(is_list_modified)
                                {
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        
                                        case_has_changed = true;
                                    }
                                    case_has_changed = case_has_changed && gs.set_multi_value(node.path, value_list, doc);
                                }
                            }
                            

                            //process_list(ref case_has_changed, lookup, node.Node, doc);
                        }
                    }


                    foreach(var node in  single_form_grid_value_set)
                    {

                        bool is_blank = false;
                        C_Get_Set_Value.get_grid_value_result grid_value_result = gs.get_grid_value(doc, node.path);

                        if(!grid_value_result.is_error)
                        {
                            var value_list =grid_value_result.result;

                            var change_list = new List<(int, object)>();

                            var result_list = new List<object>();

                            var list_has_change = false;

                            foreach(var value_tuple in value_list)
                            {
                                var value = value_tuple.Item2;

                                var value_string = "9999";

                                if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                {
                                    is_blank = true;
                                    
                                }
                                else
                                {
                                    value_string = value.ToString();
                                }

                                if(is_blank || !node.value_to_display.ContainsKey(value_string))
                                {
                                    if(node.display_to_value.ContainsKey(value_string) || is_blank && node.display_to_value.ContainsKey("(blank)") )
                                    {
                                        if(case_change_count == 0)
                                        {
                                            case_change_count += 1;
                                            case_has_changed = true;
                                        }

                                        object new_value = null;

                                        if(node.display_to_value.ContainsKey(value_string))
                                        {
                                            new_value = node.display_to_value[value_string];
                                        }
                                        else //if(is_blank)
                                        {
                                            if(node.Node.data_type == "number")
                                            {
                                                new_value = 9999;
                                            }
                                            else
                                            {
                                                new_value = "9999";
                                            }
                                        }
                                        
                                        change_list.Add(( value_tuple.Item1, new_value));

                                        //result_list.Add(new_value);
                                        //list_has_change = true;
                                        
                                    }
                                    else
                                    {
                                        if(case_change_count == 0)
                                        {
                                            case_change_count += 1;
                                            case_has_changed = true;
                                        }

                                        var result_tuple = get_mapped_value(node.path, value);
                                        if(result_tuple.is_mapped)
                                        {
                                            change_list.Add(( value_tuple.Item1, result_tuple.value));

                                            //case_has_changed = case_has_changed && gs.set_value(node.path, result_tuple.value.ToString(), doc);
                                        }
                                        else
                                        {
                                            var summary_key = node.path;
                                            if(!this.summary_value_dictionary.ContainsKey(summary_key))
                                            {
                                                this.summary_value_dictionary.Add(summary_key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                                            }
                                            if(!this.summary_value_dictionary[summary_key].Contains(value_string))
                                            {
                                                this.summary_value_dictionary[summary_key].Add(value_string);
                                            }
                                            this.output_builder.AppendLine($"item not found record_id: {mmria_id} path:{node.path} item: {value}");
                                        }
                                    }
                                }


                            }


                            if(change_list.Count > 0)
                            {
                                case_has_changed = case_has_changed && gs.set_grid_value(doc, node.path, change_list);
                            }
                        }
                    }


                    // **** currently this situation is NOT in metadata so count is always 0
                    foreach(var node in single_form_grid_multi_value_list_set)
                    {
                        bool is_blank = false;

                        C_Get_Set_Value.get_grid_value_result grid_value_result = gs.get_grid_value(doc, node.path);

                        if(!grid_value_result.is_error)
                        {
                            var grid_value_list = grid_value_result.result;

                            var change_list = new List<(int, object)>();

                            //var result_list = new List<object>();

                            var list_has_change = false;

                            foreach(var value_tuple in grid_value_list)
                            {

                                var value_list = value_tuple.Item2 as List<object>;
                                var result_list = new List<object>();
                                foreach(var value in value_list)
                                {
                                    var value_string = "9999";

                                    if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                    {
                                        is_blank = true;
                                        
                                    }
                                    else
                                    {
                                        value_string = value.ToString();
                                    }

                                    if(is_blank || !node.value_to_display.ContainsKey(value_string))
                                    {
                                        if(node.display_to_value.ContainsKey(value_string) || is_blank && node.display_to_value.ContainsKey("(blank)") )
                                        {
                                            if(case_change_count == 0)
                                            {
                                                case_change_count += 1;
                                                case_has_changed = true;
                                            }

                                            object new_value = null;

                                            if(node.display_to_value.ContainsKey(value_string))
                                            {
                                                new_value = node.display_to_value[value_string];
                                            }
                                            else //if(is_blank)
                                            {
                                                if(node.Node.data_type == "number")
                                                {
                                                    new_value = 9999;
                                                }
                                                else
                                                {
                                                    new_value = "9999";
                                                }

                                                //change_list.Add(( value_tuple.Item1, new_value));
                                                
                                            }

                                            result_list.Add(new_value);
                                            list_has_change = true;
                                            
                                        }
                                        else
                                        {
                                            if(case_change_count == 0)
                                            {
                                                case_change_count += 1;
                                                case_has_changed = true;
                                            }

                                            var result_tuple = get_mapped_value(node.path, value);
                                            if(result_tuple.is_mapped)
                                            {
                                                //change_list.Add(( value_tuple.Item1, result_tuple.value));

                                                result_list.Add(value);
                                                list_has_change = true;

                                                //case_has_changed = case_has_changed && gs.set_value(node.path, result_tuple.value.ToString(), doc);
                                            }
                                            else
                                            {

                                                result_list.Add(value);

                                                var summary_key = node.path;
                                                if(!this.summary_value_dictionary.ContainsKey(summary_key))
                                                {
                                                    this.summary_value_dictionary.Add(summary_key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                                                }
                                                if(!this.summary_value_dictionary[summary_key].Contains(value_string))
                                                {
                                                    this.summary_value_dictionary[summary_key].Add(value_string);
                                                }
                                                this.output_builder.AppendLine($"item not found record_id: {mmria_id} path:{node.path} item: {value}");
                                            }
                                        }
                                    }
                                }
                                if(list_has_change)
                                {
                                    change_list.Add(( value_tuple.Item1, result_list));
                                }
                            }


                            if(change_list.Count > 0)
                            {
                                case_has_changed = case_has_changed && gs.set_grid_value(doc, node.path, change_list);
                            }
                        }
                    }


                    foreach(var question in  multiform_value_set)
                    {
                        bool is_blank = false;

                        C_Get_Set_Value.get_multiform_value_result multiform_value_result = gs.get_multiform_value(doc, question.path);
                        if(!multiform_value_result.is_error)
                        {
                            var multiform_value_list_set = multiform_value_result.result;

                            var change_list = new List<(int, object)>();

                            foreach(var form in multiform_value_list_set)
                            {
                                
                                var value = form.Item2;
                                
                                string value_string = "9999";
                                if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                {
                                    is_blank = true;
                                }
                                else
                                {
                                    value_string = value.ToString();
                                }


                                if(!question.value_to_display.ContainsKey(value_string))
                                {
                                    if(question.display_to_value.ContainsKey(value_string) || is_blank && question.display_to_value.ContainsKey("(blank)") )
                                    {
                                        if(case_change_count == 0)
                                        {
                                            case_change_count += 1;
                                            case_has_changed = true;
                                        }

                                        object new_value = null;

                                        if(question.display_to_value.ContainsKey(value_string))
                                        {
                                            new_value = question.display_to_value[value_string];
                                        }
                                        else //if(is_blank)
                                        {
                                            new_value = "9999";
                                        }
                                        change_list.Add(( form.Item1, new_value));
                                    }
                                    else
                                    {
                                        if(case_change_count == 0)
                                        {
                                            case_change_count += 1;
                                            case_has_changed = true;
                                        }

                                        var result_tuple = get_mapped_value(question.path, value);
                                        if(result_tuple.is_mapped)
                                        {
                                            change_list.Add(( form.Item1, result_tuple.value));
                                            //case_has_changed = case_has_changed && gs.set_value(question.path, result_tuple.value.ToString(), doc);
                                        }
                                        else
                                        {
                                            var summary_key = question.path;
                                            if(!this.summary_value_dictionary.ContainsKey(summary_key))
                                            {
                                                this.summary_value_dictionary.Add(summary_key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                                            }
                                            if(!this.summary_value_dictionary[summary_key].Contains(value_string))
                                            {
                                                this.summary_value_dictionary[summary_key].Add(value_string);
                                            }
                                            this.output_builder.AppendLine($"item not found record_id: {mmria_id} path:{question.path} item: {value}");
                                        }
                                    }
                                }
                                
                            }

                            if(change_list.Count > 0)
                            {
                                case_has_changed = case_has_changed && gs.set_multiform_value(doc, question.path, change_list);
                            }
                        }
                        
                    }

                    foreach(var question in  multiform_multi_value_set)
                    {
                        bool is_blank = false;

                        C_Get_Set_Value.get_multiform_value_result multiform_value_result = gs.get_multiform_value(doc, question.path);
                        if(!multiform_value_result.is_error)
                        {
                            var multiform_value_list_set = multiform_value_result.result;

                            var change_list = new List<(int, object)>();

                            foreach(var form in multiform_value_list_set)
                            {
                                
                                var value = form.Item2;

                                var new_result_list = new List<object>();
                                var value_list = value as List<object>;


                                if(value_list == null)
                                {
                                    value_list = new List<object>();
                                }
                                var question_answer_is_modified = false;
                                foreach(var value_item in value_list)
                                {
                                    string value_string = "9999";
                                    if(value_item == null || string.IsNullOrWhiteSpace(value_item.ToString()))
                                    {
                                        is_blank = true;
                                    }
                                    else
                                    {
                                        value_string = value_item.ToString();
                                    }

                                    if(is_blank || !question.value_to_display.ContainsKey(value_string))
                                    {
                                        if(question.display_to_value.ContainsKey(value_string) || is_blank && question.display_to_value.ContainsKey("(blank)") )
                                        {
                                            if(case_change_count == 0)
                                            {
                                                case_change_count += 1;
                                                case_has_changed = true;
                                            }

                                            object new_value = null;

                                            if(question.display_to_value.ContainsKey(value_string))
                                            {
                                                new_value = question.display_to_value[value_string];
                                            }
                                            else //if(is_blank)
                                            {
                                                new_value = "9999";
                                            }

                                            new_result_list.Add(new_value);
                                            question_answer_is_modified = true;
                                            
                                        }
                                        else
                                        {
                                            if(case_change_count == 0)
                                            {
                                                case_change_count += 1;
                                                case_has_changed = true;
                                            }

                                            var result_tuple = get_mapped_value(question.path, value_item);
                                            if(result_tuple.is_mapped)
                                            {
                                                new_result_list.Add(result_tuple.value);
                                                question_answer_is_modified = true;
                                                //case_has_changed = case_has_changed && gs.set_value(question.path, result_tuple.value.ToString(), doc);
                                            }
                                            else
                                            {
                                                var summary_key = question.path;
                                                if(!this.summary_value_dictionary.ContainsKey(summary_key))
                                                {
                                                    this.summary_value_dictionary.Add(summary_key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                                                }
                                                if(!this.summary_value_dictionary[summary_key].Contains(value_string))
                                                {
                                                    this.summary_value_dictionary[summary_key].Add(value_string);
                                                }
                                                this.output_builder.AppendLine($"item not found record_id: {mmria_id} path:{question.path} item: {value}");
                                            }
                                        }
                                    }

                                    new_result_list.Add(value_item);
                                }

                                if(question_answer_is_modified && new_result_list.Count > 0)
                                {
                                    change_list.Add(( form.Item1, new_result_list));
                                }
                                    
                                    
                                

                            }

                            if(change_list.Count > 0)
                            {
                                case_has_changed = case_has_changed && gs.set_multiform_value(doc, question.path, change_list);
                            }
                        }
                        
                    }

                    foreach(var node in  multiform_grid_value_set)
                    {
                        
                        bool is_blank = false;
                        C_Get_Set_Value.get_multiform_grid_value_result multiform_grid_value_result = gs.get_multiform_grid_value(doc, node.path);
                        if(!multiform_grid_value_result.is_error)
                        {
                            var value_list = multiform_grid_value_result.result;

                            var change_list = new List<(int, int, object)>();

                            foreach(var value_tuple in value_list)
                            {
                                var value = value_tuple.Item3;

                                var value_string = "9999";

                                if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                {
                                    is_blank = true;
                                }
                                else
                                {
                                    value_string = value.ToString();
                                }

                                if(is_blank || !node.value_to_display.ContainsKey(value_string))
                                {
                                    if(node.display_to_value.ContainsKey(value_string) || is_blank && node.display_to_value.ContainsKey("(blank)") )
                                    {
                                        if(case_change_count == 0)
                                        {
                                            case_change_count += 1;
                                            case_has_changed = true;
                                        }

                                        object new_value = null;

                                        if(node.display_to_value.ContainsKey(value_string))
                                        {
                                            new_value = node.display_to_value[value_string];
                                        }
                                        else if(is_blank)
                                        {
                                            new_value = "9999";
                                        }
                                        else
                                        {
                                            
                                        }
                                        change_list.Add(( value_tuple.Item1, value_tuple.Item2, new_value));
                                    }
                                    else
                                    {
                                        if(case_change_count == 0)
                                        {
                                            case_change_count += 1;
                                            case_has_changed = true;
                                        }

                                        var result_tuple = get_mapped_value(node.path, value);
                                        if(result_tuple.is_mapped)
                                        {
                                            change_list.Add(( value_tuple.Item1, value_tuple.Item2, result_tuple.value));
                                            //case_has_changed = case_has_changed && gs.set_value(node.path, result_tuple.value.ToString(), doc);
                                        }
                                        else
                                        {
                                            var summary_key = node.path;
                                            if(!this.summary_value_dictionary.ContainsKey(summary_key))
                                            {
                                                this.summary_value_dictionary.Add(summary_key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                                            }
                                            if(!this.summary_value_dictionary[summary_key].Contains(value_string))
                                            {
                                                this.summary_value_dictionary[summary_key].Add(value_string);
                                            }
                                            this.output_builder.AppendLine($"item not found record_id: {mmria_id} path:{node.path} item: {value}");
                                        }
                                    }
                                }
                            }

                            if(change_list.Count > 0)
                            {
                                case_has_changed = case_has_changed && gs.set_multiform_grid_value(doc, node.path, change_list);
                            }
                        }
                        
                    }


                    foreach(var node in  multiform_grid_multi_value_set)
                    {
                        
                        bool is_blank = false;

                        C_Get_Set_Value.get_multiform_grid_value_result multiform_grid_value_result = gs.get_multiform_grid_value(doc, node.path);
                        if(!multiform_grid_value_result.is_error)
                        {
                            var multi_value_list = multiform_grid_value_result.result;
    
                            var change_list = new List<(int, int, object)>();

                            foreach(var value_tuple in multi_value_list)
                            {
                                var value_list = value_tuple.Item3 as List<object>;
                                var result_list = new List<object>();

                                var is_list_changed = false;

                                foreach(var value in value_list)
                                {
                                    var value_string = "9999";

                                    

                                    if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                    {
                                        is_blank = true;
                                    }
                                    else
                                    {
                                        value_string = value.ToString();
                                    }

                                    if(is_blank || !node.value_to_display.ContainsKey(value_string))
                                    {
                                        if(node.display_to_value.ContainsKey(value_string) || is_blank && node.display_to_value.ContainsKey("(blank)") )
                                        {
                                            if(case_change_count == 0)
                                            {
                                                case_change_count += 1;
                                                case_has_changed = true;
                                            }

                                            object new_value = null;

                                            if(node.display_to_value.ContainsKey(value_string))
                                            {
                                                new_value = node.display_to_value[value_string];
                                            }
                                            else //if(is_blank)
                                            {
                                                new_value = "9999";
                                            }

                                            is_list_changed = true;
                                            result_list.Add(new_value);
                                            
                                        }
                                        else
                                        {
                                            if(case_change_count == 0)
                                            {
                                                case_change_count += 1;
                                                case_has_changed = true;
                                            }

                                            var result_tuple = get_mapped_value(node.path, value);
                                            if(result_tuple.is_mapped)
                                            {
                                                is_list_changed = true;
                                                result_list.Add(result_tuple.value);
                                                //case_has_changed = case_has_changed && gs.set_value(node.path, result_tuple.value.ToString(), doc);
                                            }
                                            else
                                            {
                                                result_list.Add(value);
                                                var summary_key = node.path;
                                                if(!this.summary_value_dictionary.ContainsKey(summary_key))
                                                {
                                                    this.summary_value_dictionary.Add(summary_key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                                                }
                                                if(!this.summary_value_dictionary[summary_key].Contains(value_string))
                                                {
                                                    this.summary_value_dictionary[summary_key].Add(value_string);
                                                }
                                                this.output_builder.AppendLine($"item not found record_id: {mmria_id} path:{node.path} item: {value}");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        result_list.Add(value);
                                    }
                                    
                                }
                                if(is_list_changed)
                                {
                                    change_list.Add(( value_tuple.Item1, value_tuple.Item2, result_list));
                                }
                            }

                            if(change_list.Count > 0)
                            {
                                case_has_changed = case_has_changed && gs.set_multiform_grid_value(doc, node.path, change_list);
                            }
                        }
                        
                    }

                    {
                        var path = "social_and_environmental_profile/sources_of_social_services_information_for_this_record/element";
                        Metadata_Node node = null;

                        foreach(var n in all_list_set)
                        {
                            if(n.path.Equals(path, StringComparison.OrdinalIgnoreCase))
                            {
                                node = n;
                            }
                        }


                        C_Get_Set_Value.get_grid_value_result grid_value_result = gs.get_grid_value(doc, path);
                        if(!grid_value_result.is_error)
                        {
                            var value_list = grid_value_result.result;
                            var change_list = new List<(int, object)>();
                            var change_other_list = new List<(int, object)>();

                            foreach(var value in value_list)
                            {
                                var other_list_option_value = 8;
                                var object_test = value.Item2;
                                if(object_test != null)
                                if(!node.value_to_display.ContainsKey(object_test.ToString()))
                                {
                                    change_list.Add(( value.Item1, other_list_option_value));
                                    change_other_list.Add(( value.Item1, value.Item2));
                                    
                                }
                            }

                            if(change_list.Count > 0)
                            {
                                this.output_builder.AppendLine($"sent to other {mmria_id} - {path}");
                                if(case_change_count == 0)
                                {
                                    case_change_count += 1;
                                    case_has_changed = true;
                                }

                                case_has_changed = case_has_changed && gs.set_grid_value(doc, path,change_list);
                            }

                            if(change_other_list.Count > 0)
                            {
                                this.output_builder.AppendLine($"sent to other {mmria_id} - {path}");
                                if(case_change_count == 0)
                                {
                                    case_change_count += 1;
                                    case_has_changed = true;
                                }

                                case_has_changed = case_has_changed && gs.set_grid_value(doc, path + "_other",change_other_list);
                            }

                        }

                    }


                    if(!is_report_only_mode && case_has_changed)
                    {
                        set_value("date_last_updated", DateTime.UtcNow.ToString("o"), case_item.doc);
                        set_value("last_updated_by", "migration_plan", case_item.doc);

                        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(doc, settings);

                        string put_url = $"{db_server_url}/{db_name}/{case_item.id}";
                        cURL document_curl = new cURL ("PUT", null, put_url, object_string, config_timer_user_name, config_timer_value);

                        try
                        {
                            responseFromServer = document_curl.execute();
                            var	result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
                        }
                        catch(Exception ex)
                        {
                            //Console.Write("auth_session_token: {0}", auth_session_token);
                            Console.WriteLine(ex);
                        }
                    }
                }
            }

            Console.WriteLine($"Process_Migrate_Charactor_to_Numeric End {System.DateTime.Now}");
        
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Process_Migrate_Charactor_to_Numeric exception {System.DateTime.Now}\n{ex}");
        }

        TimeSpan time_span = System.DateTime.Now - begin_time;
        Console.WriteLine($"Process_Migrate_Charactor_to_Numeric duration total seconds: {time_span.TotalSeconds} total_minutes{time_span.TotalMinutes}\n");
        
    }


    public sealed class Metadata_Node
    {
        public Metadata_Node(){}
        public bool is_multiform { get; set; }
        public bool is_grid { get; set; }

        public string path {get;set;}
        public mmria.common.metadata.node Node { get; set; }

        public Dictionary<string,string> display_to_value { get; set; }
        public Dictionary<string,string> value_to_display { get; set; }
    }
    

    private List<Metadata_Node> get_metadata_node_by_type(mmria.common.metadata.app p_metadata, string p_type)
    {
        var result = new List<Metadata_Node>();
        foreach(var node in p_metadata.children)
        {
            var current_type = node.type.ToLowerInvariant();
            if(current_type == p_type)
            {
                result.Add(new Metadata_Node()
                {
                    is_multiform = false,
                    is_grid = false,
                    path = node.name,
                    Node = node
                });
            }
            else if(current_type == "form")
            {
                if
                (
                    node.cardinality == "+" ||
                    node.cardinality == "*"
                )
                {
                    get_metadata_node_by_type(ref result, node, p_type, true, false, node.name);
                }
                else
                {
                    get_metadata_node_by_type(ref result, node, p_type, false, false, node.name);
                }
            }
        }
        return result;
    }

    private void get_metadata_node_by_type(ref List<Metadata_Node> p_result, mmria.common.metadata.node p_node, string p_type, bool p_is_multiform, bool p_is_grid, string p_path)
    {
        var current_type = p_node.type.ToLowerInvariant();
        if(current_type == p_type)
        {
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

            p_result.Add(new Metadata_Node()
            {
                is_multiform = p_is_multiform,
                is_grid = p_is_grid,
                path = p_path,
                Node = p_node,
                value_to_display = value_to_display,
                display_to_value = display_to_value
            });
        }
        else if(p_node.children != null)
        {
            foreach(var node in p_node.children)
            {
                if(current_type == "grid")
                {
                    get_metadata_node_by_type(ref p_result, node, p_type, p_is_multiform, true, p_path + "/" + node.name);
                }
                else
                {
                    get_metadata_node_by_type(ref p_result, node, p_type, p_is_multiform, p_is_grid, p_path + "/" + node.name);
                }
            }
        }
    }

    private mmria.common.metadata.node get_metadata_node(mmria.common.metadata.app p_metadata, string p_path)
    {

/*
example usage
            var pregnancy_relatedness_set_node = get_metadata_node(metadata, "committee_review/pregnancy_relatedness");
            foreach(var item in pregnancy_relatedness_set_node.values)
            {
                pregnancy_relatedness_set.Add(item.value);
            }
*/

        mmria.common.metadata.node result = null;

        mmria.common.metadata.node current = null;
        
        string[] path = p_path.Split("/");

        for(int i = 0; i < path.Length; i++)
        {
            string current_name = path[i];
            if(i == 0)
            {
                foreach(var child in p_metadata.children)
                {
                    if(child.name.Equals(current_name, StringComparison.OrdinalIgnoreCase))
                    {
                        current = child;
                        break;
                    }
                }
            }

            else
            {

                if(current.children != null)
                {
                    foreach(var child2 in current.children)
                    {
                        if(child2.name.Equals(current_name, StringComparison.OrdinalIgnoreCase))
                        {
                            current = child2;
                            break;
                        }
                    }	
                }
                else
                {
                    return result;
                }

                if(i == path.Length -1)
                {
                    result = current;
                }
            }

        }

        return result;
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
    private bool set_value(string p_metadata_path, string p_value, object p_case, int p_index = -1)
    {
        bool result = false;

        var metadata_path_array = p_metadata_path.Split("/");
        var item_key = metadata_path_array[0];

        if(metadata_path_array.Length == 1)
        {
            if(p_index < 0)
            {
                switch(p_case)
                {
                    case IDictionary<string,object> val:
                        if(val.ContainsKey(item_key))
                        {
                            val[item_key] = p_value;
                        }
                        else
                        {
                            val.Add(item_key, p_value);
                        }
                        result = true;
                        break;

                }
            }
            else
            {
                switch(p_case)
                {
                    case IList<object> val:
                        switch(val[p_index])
                        {
                            case  IDictionary<string,object> list_val:
                            if(list_val.ContainsKey(item_key))
                            {
                                list_val[item_key] = p_value;
                            }
                            else
                            {
                                list_val.Add(item_key, p_value);
                            }
                            result = true;
                            break;
                        }
                        break;

                }
            }
        }
        else
        {
            var  builder = new System.Text.StringBuilder();
            for(int i = 1; i < metadata_path_array.Length; i++)
            {
                builder.Append(metadata_path_array[i]);
                builder.Append("/");
            }
            builder.Length = builder.Length - 1;

            var metadata_path = builder.ToString();
            object new_item = null;

            switch(p_case)
            {
                case IDictionary<string,object> val:
                    if(val.ContainsKey(item_key))
                    {
                        result = set_value(metadata_path, p_value, val[item_key]);
                    }
                    else
                    {
                        // error
                    }

                    break;
                case IList<object> val:
                    foreach(var item in val)
                    {
                        switch(item)
                        {
                            case IDictionary<string,object> item_val:
                                if(item_val.ContainsKey(item_key))
                                {
                                    result &= set_value(metadata_path, p_value, item_val[item_key]);
                                }
                                else
                                {
                                    // error
                                }

                                break;
                        }
                    }

                    break;

            }
            //result = set_value(metadata_path, p_value, new_item);
        }

        return result;
    }
    

    public Dictionary<string, List<Rev_Record_Value>> Get_Rev_Record_Value_List(string p_case_id, List<string> p_mmria_path_list)
    {
        
        var rev_record_list = new Dictionary<string, List<Rev_Record_Value>>(StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, List<Rev_Record_Value>>(StringComparer.OrdinalIgnoreCase);
        var output_builder = new System.Text.StringBuilder();
        var gs = new C_Get_Set_Value(output_builder);


        var revision_list_url = $"{db_server_url}/{db_name}/{p_case_id}?revs_info=true";
        //System.Console.WriteLine($"\trev_list: {revision_list_url}");

        var case_rev_info_curl = new cURL("GET", null, revision_list_url, null, config_timer_user_name, config_timer_value);
        string responseFromServer = case_rev_info_curl.execute();
    
        var case_rev_info_response = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);
        var case_rev_info_dictionary = case_rev_info_response as IDictionary<string,object>;

        var revs_info_list = case_rev_info_dictionary["_revs_info"] as List<object>;

        foreach(var rev_info in revs_info_list)
        {
            //4 obtain version before deletion, e.g.:
            //curl http://example.iriscouch.com/test/$id?rev=$prev_rev
            
            System.Dynamic.ExpandoObject expando_object = rev_info as System.Dynamic.ExpandoObject;
            IDictionary<string,object> dictionary = expando_object as IDictionary<string,object>;

            var status = dictionary["status"].ToString();
            var _rev = dictionary["rev"].ToString();

            if(status.ToLower() == "available")
            {
                var revision_url = $"{db_server_url}/{db_name}/{p_case_id}?rev={_rev}";
                //System.Console.WriteLine($"\tprevious_revision_url: {revision_url}");

                try
                {
                    var _rev_curl = new cURL("GET", null, revision_url, null, config_timer_user_name, config_timer_value);
                    responseFromServer = _rev_curl.execute();

                    var revision_response = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);
                    dictionary = revision_response as IDictionary<string,object>;
                    
                    C_Get_Set_Value.get_value_result value_result = gs.get_value(revision_response,"date_created");
                    DateTime? date_created = value_result.result as DateTime?;
                    value_result = gs.get_value(revision_response,"date_last_updated");
                    DateTime? date_last_updated = value_result.result as DateTime?;

                    foreach(var p_mmria_path in p_mmria_path_list)
                    {

                        if(!rev_record_list.ContainsKey(p_mmria_path))
                        {
                            rev_record_list.Add(p_mmria_path, new List<Rev_Record_Value>());
                            result.Add(p_mmria_path, new List<Rev_Record_Value>());
                        }
                        var val = gs.get_value(revision_response, p_mmria_path);

                        rev_record_list[p_mmria_path].Add(new Rev_Record_Value(){
                            _id = p_case_id,
                            _rev = _rev,

                            date_created = date_created,
                            created_by = dictionary["created_by"].ToString(),
                            date_last_updated = date_last_updated,
                            last_updated_by = dictionary["last_updated_by"].ToString(),
                            avilability_status = status.ToLower(),
                            mmria_path = p_mmria_path,
                            value = val
                        });
                    }
                    
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine(ex);
                    return new Dictionary<string, List<Rev_Record_Value>>(StringComparer.OrdinalIgnoreCase);
                }
            }

        }

        foreach(var kvp in rev_record_list)
        {
            result[kvp.Key] = kvp.Value.OrderByDescending( r=> r.date_last_updated).ToList();
            /*
            var descending_order_list = kvp.Value.OrderByDescending( r=> r.date_last_updated).ToList();
            Rev_Record_Value current = null;
            Rev_Record_Value next = null;
            for(var i = 0; i < descending_order_list.Count; i++)
            {
                current = descending_order_list[i];
                if( i + 1 < descending_order_list.Count)
                {
                    next = descending_order_list[i + 1];
                    if(current._id == next._id)
                    {
                        var value_1_dynamic = current.value;
                        var value_2_dynamic = next.value;

                        var value_1 = "";
                        var value_2 = "";

                        if(value_1_dynamic != null)
                        {
                            object value_check = (object) value_1_dynamic;
                            if(value_check is List<object> value_list)
                            {
                                value_1 = "[" + string.Join(",", value_list) + "]";
                            }
                            else
                            {
                                value_1 = value_check.ToString();
                            }
                            
                        }
                        
                        if(value_2_dynamic != null)
                        {
                            object value_check = (object) value_2_dynamic;
                            if(value_check is List<object> value_list)
                            {
                                value_2 = "[" + string.Join(",", value_list) + "]";
                            }
                            else
                            {
                                value_2 = value_check.ToString();
                            }
                        }
                    }

                }
                
            }*/
        }


        return result;
    }
    public string print(Rev_Record_Value _)
    {
        return $"{_._id},\t{_._rev },\t{_.date_created},\t{_.created_by},\t{_.date_last_updated},\t{_.last_updated_by},\t{_.avilability_status},\t{_.mmria_path},\t{_.value}";
    }

    public sealed class Bool_Result_Object
    {
        public Bool_Result_Object() {}

        public bool is_mapped { get; set; }
        public object value { get; set; }
    }
    private Bool_Result_Object get_mapped_value(string p_path, object p_data)
    {
        var metadata_node  = all_list_set.Where(o=> o.path.ToLower() == p_path.ToLower()).SingleOrDefault();
        var data_value_list = metadata_node.display_to_value;

        var name_to_value_lookup = metadata_node.display_to_value;
        object result_object = null;
        var result = new Bool_Result_Object() {is_mapped=false, value=result_object};// result = p_data;

        var is_number_regex = new System.Text.RegularExpressions.Regex(@"^\-?\d+\.?\d*$");

        /*
        if(p_path == "/autopsy_report/causes_of_death/type")
        {
            console.log("break");
        }*/

        try
        {

            if(p_path == "informant_interviews/race")
            {
                if(p_data is List<object>)
                {
                    var p_data_list = p_data as List<object>;
                    result.is_mapped = true;
                    result.value = 9999;

                    for(var i = 0; i < p_data_list.Count; i++)
                    {
                        var val = p_data_list[i];

                        if
                        (
                            val != null && 
                            val.ToString() != ""
                        )
                        {
                            var key = val.ToString();

                            if(data_value_list.ContainsKey(key))
                            {
                                //p_data[i] = data_value_list[val];
                                result.is_mapped = true;
                                result.value = data_value_list[key];
                            }
                            else if
                            (   
                                name_to_value_lookup[key] == null
                            )
                            {
                                this.output_builder.AppendLine($"{p_path} - {key}");
                            }
                        }
                    }
                }
                else if(data_value_list.ContainsKey(p_data.ToString().ToLower()))
                {
                    result.is_mapped = true;
                    result.value = data_value_list[p_data.ToString().ToLower()];
                }
                else if
                (   
                    name_to_value_lookup[p_data.ToString().ToLower()] == null
                )
                {
                    this.output_builder.AppendLine($"{p_path} - {p_data}");
                }
            }
            else if(p_data is List<object>)
            {
                switch(p_path.ToLower())
                {
                    case "informant_interviews/race":
                        System.Console.WriteLine("get_mapped_value: informant_interviews/race");
                        /*
                        if(p_data is List<object> race_list)
                        {
                            
                        }
                        else if(p_data_string.Trim().ToLower() == "white")
                        {
                            result.is_mapped = true;
                            result.value = 0;
                        }
                        else
                        {
                            //g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                        }*/
                        break;
                    default:
                        this.output_builder.AppendLine($"Bad list {p_path} - {p_data}");
                        break;
                }
                /*
                var p_data_list = p_data as List<object>;

                var result_list = new List<object>();
                bool list_is_changed = false;
                for(var i = 0; i < p_data_list.Count; i++)
                {
                    var item_object = p_data_list[i];

                    result_list.Add(item_object);

                    if(item_object == null || item_object.ToString() =="")
                    {
                        list_is_changed = true;
                        result_list[i] = data_value_list["(blank)"];
                    }
                    else
                    {
                        var item = item_object.ToString();

                        if(data_value_list.ContainsKey(item) && is_number_regex.Match(data_value_list[item]).Success)
                        {
                            list_is_changed = true;
                            result_list[i] = data_value_list[item];
                        }
                        else if (item_object is bool)
                        {
                            this.output_builder.AppendLine($"{p_path} - {item}");
                        }
                        else if(data_value_list.ContainsKey(item) && !is_number_regex.Match(data_value_list[item]).Success)
                        {
                            list_is_changed = true;
                            result_list[i] = data_value_list[item.ToLower()];
                        }                   
                        else if(p_data_list[i].ToString().ToLower() == "No, not Spanish/ Hispanic/ Latino".ToLower())
                        {
                            list_is_changed = true;
                            result_list[i] = "0";
                        }
                        else if(p_data_list[i].ToString().ToLower() == "Yes, Other Spanish/ Hispanic/ Latino".ToLower())
                        {
                            list_is_changed = true;
                            result_list[i] = "4";
                        }
                        else if
                        (
                            p_data_list[i].ToString().Length > 3 && 
                            (
                                p_data_list[i].ToString().Substring(2,1) == "-" ||
                                p_data_list[i].ToString().Substring(1,1) == "-"
                            )
                        )
                        {
                            var val = p_data_list[i].ToString().Split("-")[1].Trim().ToLower();
                            if(data_value_list.ContainsKey(val))
                            {
                                list_is_changed = true;
                                result_list[i] = data_value_list[val];
                            }
                        }
                        else if(p_data_list[i].ToString() == "-9")
                        {
                            list_is_changed = true;
                            result_list[i] = "9999";
                        }
                        else if(p_data_list[i].ToString() == "-8")
                        {
                            list_is_changed = true;
                            result_list[i] = "8888";
                        }
                        else if(p_data_list[i].ToString() == "-7")
                        {
                            list_is_changed = true;
                            result_list[i] = "7777";
                        }
                        else if
                        (   
                            p_data_list[i] != null  && name_to_value_lookup.ContainsKey(p_data_list[i].ToString())
                        )
                        {
                            this.output_builder.AppendLine($"{p_path} - {p_data_list[i]}");
                        }
                    }
                }

                if(list_is_changed)
                {
                    result.is_mapped = true;
                    result.value = result_list;
                }
                */
            }
            else
            {
                if(p_data == null)
                {
                    result.is_mapped = true;
                    result.value = data_value_list["(blank)"];
                }
                else 
                {
                    var p_data_string = p_data.ToString();

                
                    if(p_data_string =="")
                    {
                        result.is_mapped = true;
                        result.value = data_value_list["(blank)"];
                    }
                    else if(data_value_list.ContainsKey(p_data_string) && is_number_regex.Match(p_data_string).Success)
                    {
                        result.is_mapped = true;
                        result.value = data_value_list[p_data_string];
                    }
                    /*
                    else if(p_data is bool)
                    {
                        if
                        (
                            p_path == "social_and_environmental_profile/health_care_system/no_prenatal_care"
                        )
                        {
                            if((bool)p_data)
                            {
                                result.is_mapped = true;
                                result.value = 0;
                            }
                            else
                            {
                                result.is_mapped = true;
                                result.value = 1;
                            }
                            
                        }
                        else if(data_value_list.ContainsKey("yes") && !string.IsNullOrWhiteSpace(p_data_string))
                        {
                            result.is_mapped = true;
                            result.value = data_value_list["yes"];
                        }
                        else if(data_value_list.ContainsKey("no"))
                        {
                            result.is_mapped = true;
                            result.value = data_value_list["no"];
                        }
                        else
                        {
                            this.output_builder.AppendLine($"{p_path} - {p_data_string}");
                        }
                    }*/
                    else if(!is_number_regex.Match(p_data_string).Success && data_value_list.ContainsKey(p_data_string))
                    {
                        result.is_mapped = true;
                        result.value = data_value_list[p_data_string.ToLower()];
                    }
                    else if(p_data_string.ToLower() == "No, not Spanish/ Hispanic/ Latino".ToLower())
                    {
                        result.is_mapped = true;
                        result.value = "0";
                    }
                    else if(p_data_string.ToLower() == "Yes, Other Spanish/ Hispanic/ Latino".ToLower())
                    {
                        result.is_mapped = true;
                        result.value = "4";
                    }
                    else if
                    (
                        p_data_string.Length > 3 && 
                        (
                            p_data_string.Substring(2,1) == "-" ||
                            p_data_string.Substring(1,1) == "-"
                        )
                        
                    )
                    {
                        var val = p_data_string.Split("-")[1].Trim().ToLower();
                        if(data_value_list.ContainsKey(val))
                        {
                            result.is_mapped = true;
                            result.value = data_value_list[val];
                        }
                        else
                        {
                            this.output_builder.AppendLine($"{p_path} - {p_data_string}");
                        }
                    }
                    else if(p_data_string == "-9")
                    {
                        result.is_mapped = true;
                        result.value = "9999";
                    }
                    else if(p_data_string == "-8")
                    {
                        result.is_mapped = true;
                        result.value = "8888";
                    }
                    else if(p_data_string == "-7")
                    {
                        result.is_mapped = true;
                        result.value = "7777";
                    }
                    else if(p_path == "birth_certificate_infant_fetal_section/is_multiple_gestation" && p_data_string.ToLower() == "Multiple gestation".ToLower())
                    {
                        result.is_mapped = true;
                        result.value = 1;
                    }
                    else if(p_path == "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/deceased_at_discharge" && p_data_string.ToLower() == "Deceased at the time of discharge?".ToLower())
                    {
                        result.is_mapped = true;
                        result.value = 1;
                    }
                    else if(p_path == "committee_review/critical_factors_worksheet/prevention")
                    {
                        if(p_data_string.ToLower() == "prevent incidence")
                        {
                            result.is_mapped = true;
                            result.value = 1;
                        }
                        else if(p_data_string.ToLower() == "prevent progression")
                        {
                            result.is_mapped = true;
                            result.value = 2;
                        }
                        else if(p_data_string.ToLower() == "prevent complications")
                        {
                            result.is_mapped = true;
                            result.value = 3;
                        }
                        else if(p_data_string.ToLower() == "(Blanks)".ToLower())
                        {
                            result.is_mapped = true;
                            result.value = 9999;
                        }
                        else if
                        (   
                            !name_to_value_lookup.ContainsKey(p_data_string)
                        )
                        {
                            this.output_builder.AppendLine($"{p_path} - {p_data_string}");
                        }
                        else
                        {
                            this.output_builder.AppendLine($"{p_path} - {p_data_string}");
                        }
                    }
                    else if(p_path == "birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin" && p_data_string.ToLower() == "Yes, other Spanish/ Hispanic/ Latino".ToLower())
                    {
                        result.is_mapped = true;
                        result.value = 4;
                    }/*
                    else if
                    (   
                        g_name_to_value_lookup[p_path][p_data] == null 
                    )
                    {
                        this.output_builder.AppendLine(`${p_path} - ${p_data}`);
                    }*/
                    else
                    {
                        switch(p_path.ToLower())
                        {
                            case "birth_fetal_death_certificate_parent/demographic_of_mother/country_of_birth":
                            case "social_and_environmental_profile/socio_economic_characteristics/country_of_birth":
                            case "death_certificate/demographics/country_of_birth":

                                switch(p_data_string.Trim().ToLower())
                                {
                                    case "(see geography)":
                                    case "unknown":
                                
                                        result.is_mapped = true;
                                        result.value = "9999";
                                    break;
                                    case "dominican rep"://  DR
                                    case "bani, dominican republic"://DR
                                    case "santo domingo, dominican republic"://DR
                                        result.is_mapped = true;
                                        result.value = "DR";

                                    break;
                                    case "unites states"://US
                                    case "united stataes"://US
                                    case "puerto rican"://US
                                        result.is_mapped = true;
                                        result.value = "US";
                                    break;
                                    case "taiwan"://CH
                                    case "cape verdean"://CV
                                    case "care verdean"://CV
                                        result.is_mapped = true;
                                        result.value = "CV";
                                    break;
                                    case "salvadoran"://ES
                                        result.is_mapped = true;
                                        result.value = "ES";
                                    break;
                                    case "colombian"://CO
                                        result.is_mapped = true;
                                        result.value = "CO";
                                    break;
                                    
                                    


                                    default:
                                    break;
                                }
                            
                                break;



                            case "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/year":
                                if(p_data_string == "14")
                                {
                                    result.is_mapped = true;
                                    result.value = 9999;
                                }
                                else if(p_data_string == "9")
                                {
                                    result.is_mapped = true;
                                    result.value = 9999;
                                }
                                else
                                {
                                    //g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                                }
                                break;
                            case "informant_interviews/race":
                                if(p_data is List<object> race_list)
                                {
                                    System.Console.WriteLine("get_mapped_value: informant_interviews/race");
                                }
                                else if(p_data_string.Trim().ToLower() == "white")
                                {
                                    result.is_mapped = true;
                                    result.value = 0;
                                }
                                else
                                {
                                    //g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                                }
                                break;
                            case "other_medical_office_visits/physical_exam/body_system":
                                if(p_data_string.Trim().ToLower() == "endoncrine")
                                {
                                    result.is_mapped = true;
                                    result.value = 7;
                                }
                                else
                                {
                                    //g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                                }
                                break;
                            case "prenatal/current_pregnancy/attended_prenatal_visits_alone":
                                if(p_data_string.Trim().ToLower() == "unknown")
                                {
                                    result.is_mapped = true;
                                    result.value = 7777;
                                }/*
                                else if(p_data_string.Trim().ToLower() == "Not Specified".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 7777;
                                }
                                else if(p_data_string.Trim().ToLower() == "8888")
                                {
                                    result.is_mapped = true;
                                    result.value = 7777;
                                }*/
                                else
                                {
                                    //g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                                }
                                break;
                            case "prenatal/primary_prenatal_care_facility/principal_source_of_payment":
                                if(p_data_string.Trim().ToLower() == "private")
                                {
                                    result.is_mapped = true;
                                    result.value = 0;
                                }
                                else if(p_data_string.Trim().ToLower() == "public")
                                {
                                    result.is_mapped = true;
                                    result.value = 1;
                                }
                                else if(p_data_string.Trim().ToLower() == "self")
                                {
                                    result.is_mapped = true;
                                    result.value = 2;
                                }
                                else if(p_data_string.Trim().ToLower() == "Not specified".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 7777;
                                }
                                else if(p_data_string.Trim().ToLower() == "Unknown".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 7777;
                                }
                                /*
                                else if(p_data_string.Trim().ToLower() == "Not specified".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 2;
                                }
                                
                                else if(p_data_string.Trim().ToLower() == "Unknown".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 2;
                                }*/
                                else
                                {
                                    //g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                                }
                                break;
                            case "social_and_environmental_profile/health_care_system/no_prenatal_care":
                                if
                                (
                                    p_data is bool bool_value &&
                                    !string.IsNullOrWhiteSpace(p_data_string)
                                )
                                {
                                    if(bool_value)
                                    {
                                        result.is_mapped = true;
                                        result.value = 0;
                                    }
                                    else
                                    {
                                        result.is_mapped = true;
                                        result.value = 1;
                                    }
                                }
                                else
                                {
                                    //g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                                }
                                break;
                            

                            case "committee_review/critical_factors_worksheet/class":
                                if(p_data_string.Trim().ToLower() == "enforcement")
                                {
                                    result.is_mapped = true;
                                    result.value = 20;
                                }
                                else if(p_data_string.Trim().ToLower() == "access/finacial")
                                {
                                    result.is_mapped = true;
                                    result.value = 11;
                                }
                                else
                                {
                                    //g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                                }
                                break;
                            case "committee_review/critical_factors_worksheet/category":
                                if(p_data_string.Trim().ToLower() == "Medical Care- Provider".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 3;
                                }
                                else if(p_data_string.Trim().ToLower() == "Medical Care- Facility".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 4;
                                }
                                break;
                            case "committee_review/pregnancy_relatedness":
                                if(p_data_string.Trim().ToLower() == "pregnancy-associated but not related")
                                {
                                    result.is_mapped = true;
                                    result.value = 0;
                                }
                                else if(p_data_string.Trim().ToLower() == "Not Pregnancy Related or Associated (i.e. False Positive)".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 99;
                                }
                                
                                break;
                            case "committee_review/pmss_mm":
                            case "committee_review/pmss_mm_secondary":
                                if(p_data_string.Trim().ToLower().IndexOf("91 pulmonary conditions") > -1)
                                {
                                    result.is_mapped = true;
                                    result.value = 91;
                                }
                                else if(p_data_string.Trim().ToLower().IndexOf("83 collagen vascular") > -1)
                                {
                                    result.is_mapped = true;
                                    result.value = 83;
                                }
                                else if(p_data_string.Trim().ToLower().ToLower() == "10.1 Hemorrhage - Rupture/Laceration/Intraabdominal Bleeding".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 10.1;
                                }
                                else if(p_data_string.Trim().ToLower() == "90.9 Other Cardiovascular Disease, including CHF, Cardiomegaly, Cardiac Hypertrophy, Cardiac Fibrosis, Non-Acute Myocarditis/NOS".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 90.9;
                                }
                                else if(p_data_string.Trim().ToLower() == "80.1 Post-Partum/Peripartum Cardiomyopathy".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 80.1;
                                }
                                else if(p_data_string.Trim().ToLower() == "10.5 Hemorrhage - Uterine Atony/Post-Partum Hemorrhage".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 10.5;
                                }

                                else if(p_data_string.Trim().ToLower() == "20.1 Post-Partum Genital Tract (e.g. of the Uterus/Pelvis/Perineum/Necrotizing Fasciitis)".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 20.1;
                                }
                                //95 CerebrovascularAccident (Hemorrhage/Thrombosis/Aneurysm/Malformation) not secondary to Hypertension
                                else if(p_data_string.Trim().ToLower() == "95 CerebrovascularAccident (Hemorrhage/Thrombosis/Aneurysm/Malformation) not secondary to Hypertension".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 95;
                                }
                                else
                                {
                                    //g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                                }
                                break;
                            case "death_certificate/race/omb_race_recode":
                            case "birth_fetal_death_certificate_parent/demographic_of_father/race/omb_race_recode":
                            case "birth_fetal_death_certificate_parent/race/omb_race_recode":
                                switch(p_data_string.Trim().ToLower())
                                {
                                    case "7":
                                    case "8":
                                    case "9":
                                    case "10":
                                    case "11":
                                    case "12":
                                        result.is_mapped = true;
                                        result.value = 4;
                                        break;
                                    case "white":
                                        result.is_mapped = true;
                                        result.value = 0;
                                        break;
                                    case "black":
                                        result.is_mapped = true;
                                        result.value = 1;
                                        break;											
                                    default:
                                        System.Console.WriteLine("death_certificate/race/omb_race_recode: missing: " + p_data_string);
                                        break;
                                }

                                break;
                            case "death_certificate/injury_associated_information/date_of_injury/year":
                                if(p_data_string == "203")
                                {
                                    result.is_mapped = true;
                                    result.value = 9999;
                                }
                                else
                                {
                                    //g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                                }
                                break;
                            case "death_certificate/injury_associated_information/transportation_related_injury":
                                if(p_data_string.Trim().ToLower() == "Other (specify)".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 3;
                                }
                                else
                                {
                                    //g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                                }
                                break;
                            case "autopsy_report/was_autopsy_performed":
                                if(p_data_string.Trim().ToLower() == "Referred / Performed / Available".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 0;
                                }
                                break;

                            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/maternal_level_of_care":
                                if(p_data_string.Trim().ToLower() == "Other (Describe)".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 5;
                                }
                                break;
                                

                            case "mental_health_profile/were_there_documented_preexisting_mental_health_conditions":

                                System.Console.WriteLine("here");
                                break;
                            case "mental_health_profile/were_there_documented_preexisting_mental_health_conditions/referral_for_treatment":
                                if(p_data_string.Trim().ToLower() == "Unknown".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 7777;
                                }
                                else
                                {
                                    System.Console.WriteLine("here");
                                }
                                
                                break;
                            case "committee_review/was_this_death_a_sucide":
                            case "committee_review/was_this_death_a_homicide":
                            case "committee_review/did_obesity_contribute_to_the_death":
                            case "committee_review/did_mental_health_conditions_contribute_to_the_death":
                                if(p_data_string.Trim().ToLower() == "8888".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 9999;
                                }
                                else
                                {
                                    System.Console.WriteLine("here");
                                }


                                break;
                            case "committee_review/critical_factors_worksheet/prevention":
                                if(p_data_string.Trim().ToLower() == "(Blanks)".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 9999;
                                }

                                break;
                            case "mental_health_profile/were_there_documented_mental_health_conditions/referral_for_treatment":
                                if(p_data_string.Trim().ToLower() == "Unknown".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 7777;
                                }
                                else
                                {
                                    System.Console.WriteLine("here");
                                }
                                break;
                            case "birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy":
                                if(p_data_string.Trim().ToLower() == "Prepregancy Hypertension".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 2;
                                }
                                else
                                {
                                    System.Console.WriteLine("here");
                                }
                                break;		
                            case "home_record/case_progress_report/birth_certificate_infant_or_fetal_death_section": 
                            case "home_record/case_progress_report/prenatal_care_record":
                            case "home_record/case_progress_report/er_visits_and_hospitalizations":
                            case "home_record/case_progress_report/other_medical_visits":
                            case "home_record/case_progress_report/social_and_psychological_profile": 
                            case "home_record/case_progress_report/informant_interviews":
                                if(p_data_string.Trim().ToLower() == "1")
                                {
                                    result.is_mapped = true;
                                    result.value = 1;
                                }
                                else if(data_value_list.ContainsKey(p_data_string))
                                {
                                    System.Console.WriteLine("here");
                                }
                                else
                                {
                                    System.Console.WriteLine("here");
                                }
                                break;
                            case "death_certificate/death_information/pregnancy_status":
                                System.Console.WriteLine("here");
                                break;
                            case "other_medical_office_visits/medical_care_facility/payment_source":
                                if(p_data_string.Trim().ToLower() == "Public".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 1;
                                }
                                else if(p_data_string.Trim().ToLower() == "Private".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 0;
                                }
                                else if(p_data_string.Trim().ToLower() == "Not Specified".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 9999;
                                }
                                else if(p_data_string.Trim().ToLower() == "Self".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 2;
                                }
                                break;
                            case "birth_certificate_infant_fetal_section/is_multiple_gestation":
                            case "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/deceased_at_discharge":
                                if(p_data_string.Trim().ToLower() == "True".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 1;
                                }
                                else if(p_data_string.Trim().ToLower() == "False".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 0;
                                }
                                else
                                {	
                                    System.Console.WriteLine(p_path);
                                }
                                break;
                            case "autopsy_report/toxicology/level":
                                if(p_data_string.Trim().ToLower() == "Theraputic".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 2;
                                }
                                break;
                            case "er_visit_and_hospital_medical_records/labratory_tests/diagnostic_level":
                                System.Console.WriteLine("er_visit_and_hospital_medical_records/labratory_tests/diagnostic_level");
                                break;
                            case "er_visit_and_hospital_medical_records/labratory_tests/flag":
                                System.Console.WriteLine("er_visit_and_hospital_medical_records/labratory_tests/flag");
                                break;
                            case "other_medical_office_visits/diagnostic_imaging_and_other_technology/technology_type":
                                System.Console.WriteLine("other_medical_office_visits/diagnostic_imaging_and_other_technology/technology_type");
                                break;
                            case "birth_fetal_death_certificate_parent/demographic_of_father/education_level":
                            case "birth_fetal_death_certificate_parent/demographic_of_mother/education_level":
                                if(p_data_string.Trim().ToLower() == "Associate's Degree".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 4;
                                }
                                break;
                            case "death_certificate/death_information/manner_of_death":
                                if(p_data_string.Trim().ToLower() == "Homocide".ToLower())
                                {
                                    result.is_mapped = true;
                                    result.value = 1;
                                }
                                break;
                            default:
                                //g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                                break;
                        }
                    }
                }
                
            }


            if(!result.is_mapped)
            {
                if(!string.IsNullOrWhiteSpace(metadata_node.Node.path_reference) && metadata_node.Node.path_reference.ToLower() == "lookup/state")
                {
                    if(p_data != null && !(p_data is List<object>))
                    {
                        var p_data_string = p_data.ToString();
                        if
                        (
                            p_data_string.Length == 2 && 
                            (
                                p_data_string.ToLower() == "xx" ||
                                p_data_string.ToLower() == "mx"
                            )
                        )
                        {
                            result.is_mapped = true;
                            result.value = 9999;
                        }
                    }
                    
                }

                if(!string.IsNullOrWhiteSpace(metadata_node.Node.path_reference) && metadata_node.Node.path_reference.ToLower() == "lookup/year")
                {
                    if(p_data != null && !(p_data is List<object>))
                    {
                        var p_data_string = p_data.ToString();

                        if(p_data_string.Length == 2)
                        {
                            var test_int = -1;
                            if(int.TryParse("20" + p_data_string, out test_int) && metadata_node.value_to_display.ContainsKey(test_int.ToString()))
                            {
                                result.is_mapped = true;
                                result.value = test_int;
                            }
                        }
                        else
                        {
                            switch(p_data_string.ToLower())
                            {
                                case "1024":
                                case "2106":
                                case "207":
                                    result.is_mapped = true;
                                    result.value = 9999;
                                break;
                            }
                        }
                    }
                }
            }
                
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex);
            this.output_builder.AppendLine($"{p_path} - {p_data}");
        }

        return result;
    }

}


