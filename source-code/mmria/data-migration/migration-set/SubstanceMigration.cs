using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public sealed class SubstanceMigration
{

/*
    "autopsy_report/toxicology/substance":
    "prenatal/substance_use_grid/substance":
    "medical_transport/origin_information/place_of_origin":
    "social_and_environmental_profile/if_yes_specify_substances/substance":
*/


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

    public string db_name;
    public string config_timer_user_name;
    public string config_timer_value;

    private string config_metadata_user_name;
    private string config_metadata_value;
    public bool is_report_only_mode;


    List<Metadata_Node> all_list_set;
    public System.Text.StringBuilder output_builder;

    public Dictionary<string, HashSet<string>> summary_value_dictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string,mmria.common.metadata.value_node[]> lookup;

    public SubstanceMigration
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
        
        this.output_builder.AppendLine($"Substance Migration migration started at: {begin_time.ToString("o")}");

        try
        {
            //string migration_plan_id = message.ToString();

            Console.WriteLine($"Process_SubstanceMigration Begin {begin_time}");


            //string metadata_url = db_server_url + "/metadata/2016-06-12T13:49:24.759Z";
            string metadata_url = $"{db_server_url}/metadata/version_specification-20.04.21/metadata";
            
            cURL metadata_curl = new cURL("GET", null, metadata_url, null, config_timer_user_name, config_timer_value);
            mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(await metadata_curl.executeAsync());

            this.lookup = get_look_up(metadata);

            all_list_set = get_metadata_node_by_type(metadata, "list");

            var substance_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var place_of_origin_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach(var item in this.lookup["lookup/substance"])
            {
                substance_set.Add(item.value);
            }


            string url = $"{db_server_url}/{db_name}/_all_docs?include_docs=true";
            var case_curl = new cURL("GET", null, url, null, config_timer_user_name, config_timer_value);
            string responseFromServer = case_curl.execute();
            var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<System.Dynamic.ExpandoObject>>(responseFromServer);

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;


            /*
            string url = $"{db_server_url}/{db_name}/_find";
            selector_id_struct selector;
            selector.selector._id = "385ff271-071d-4a27-a500-000000000000";
            selector.limit = 1000;
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(selector, settings);
            var case_curl = new cURL("POST", null, url, object_string, config_timer_user_name, config_timer_value);
            string responseFromServer = case_curl.execute();
            var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<Result_Struct>(responseFromServer);
            */

            //foreach(var case_item in case_response.docs)
            foreach(var case_response_item in case_response.rows)
            {

                var case_has_changed = false;
                var case_change_count = 0;
                //IDictionary<string, object> doc = case_item.doc as IDictionary<string, object>;
                var doc = case_response_item.doc;
                
                var gs = new C_Get_Set_Value(this.output_builder);

                if(doc != null)
                {

                    C_Get_Set_Value.get_value_result value_result = gs.get_value(doc, "_id");
                    var mmria_id = value_result.result?.ToString();
                    if(mmria_id.IndexOf("_design") > -1)
                    {
                        continue;
                    }
            /*
                    if(case_change_count == 0)
                    {
                        case_change_count += 1;
                        case_has_changed = true;
                    }

                    case_has_changed = case_has_changed && gs.set_value("host_state", "co", doc);
*/

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


                        get_grid_value_result = gs.get_grid_value(doc, substance_path);
                        if(!get_grid_value_result.is_error)
                        {
                            var list = get_grid_value_result.result;
                            var new_list = new List<(int, object)>();

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
                                
                                if
                                (
                                    value_string != "9999" &&
                                    lookup_node.value_to_display.ContainsKey(value_string) &&
                                    lookup_node.value_to_display[value_string] != value_string
                                )
                                {
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        case_has_changed = true;
                                    }

                                    new_list.Add((index, lookup_node.value_to_display[value_string]));
                                }
                                /*
                                else
                                {
                                    new_list.Add((index, value));
                                }*/
                            }

                            if(new_list.Count > 0)
                            {
                                case_has_changed = case_has_changed && gs.set_grid_value(doc, substance_path, new_list);

                                get_grid_value_result = gs.get_grid_value(doc, substance_path);
                                System.Console.WriteLine($"list {list.Count} {get_grid_value_result.result.Count} { list.Count == get_grid_value_result.result.Count}");

                            }

                            

                        }



                    }
                



                    if(!is_report_only_mode && case_has_changed)
                    {
                        await new SaveRecord(this.db_server_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(doc as IDictionary<string, object>, "SubstanceMigration", true);
                    }
                }

            }

            Console.WriteLine($"SubstanceMigration End {System.DateTime.Now}");
        
        }
        catch(Exception ex)
        {
            this.output_builder.AppendLine($"Substance Migration migration exception:{ex}");
            Console.WriteLine($"SubstanceMigration exception {System.DateTime.Now}\n{ex}");
        }

        TimeSpan time_span = System.DateTime.Now - begin_time;
        Console.WriteLine($"SubstanceMigration duration total seconds: {time_span.TotalSeconds} total_minutes{time_span.TotalMinutes}\n");
        
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

    
    private async Task<Result_Struct> execute_path(string p_find_value)
    {

        Result_Struct result = new Result_Struct();

        try
        {

            var selector_struc = new Selector_Struc();
            selector_struc.selector = new System.Collections.Generic.Dictionary<string,System.Collections.Generic.Dictionary<string,string>>(StringComparer.OrdinalIgnoreCase);
            selector_struc.limit = 10000;
            selector_struc.selector.Add("committee_review.pregnancy_relatedness", new System.Collections.Generic.Dictionary<string,string>(StringComparer.OrdinalIgnoreCase));
            selector_struc.selector["committee_review.pregnancy_relatedness"].Add("$eq", p_find_value);

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string selector_struc_string = Newtonsoft.Json.JsonConvert.SerializeObject (selector_struc, settings);

            //System.Console.WriteLine(selector_struc_string);


            string find_url = $"{db_server_url}/{db_name}/_all_docs?include_docs=true";

            var case_curl = new cURL("POST", null, find_url, selector_struc_string, config_timer_user_name, config_timer_value);
            string responseFromServer = await case_curl.executeAsync();
            
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result_Struct>(responseFromServer);

            //System.Console.WriteLine($"case_response.docs.length {result.docs.Length}");
        }
        catch(Exception)
        {

        }

        return result;
    }

    
    private bool migrate_old_to_new_value(System.Dynamic.ExpandoObject case_item, string p_new_value)
    {
        bool result = false;
        var gsv = new C_Get_Set_Value(this.output_builder);

        C_Get_Set_Value.get_value_result get_value_result = gsv.get_value(case_item, "committee_review/pregnancy_relatedness");
        if(!get_value_result.is_error)
        {
            var current_value = get_value_result.result;

            System.Console.WriteLine($"current_value: {current_value}");

            
            result = gsv.set_value("committee_review/pregnancy_relatedness", p_new_value, case_item);

            get_value_result = gsv.get_value(case_item, "committee_review/pregnancy_relatedness");
            
            if(!get_value_result.is_error)
            {
                var next_value = get_value_result.result;
                System.Console.WriteLine($"is_changed: {result} next_value: {next_value}");
            }
        }


        return result;
    }
/*
    private async Task<bool> savsav_e_case_dele_case(IDictionary<string, object> case_item)
    {
        bool result = false;
        var gsv = new C_Get_Set_Value(this.output_builder);

        //var case_item  = p_case_item as System.Collections.Generic.Dictionary<string, object>;

        gsv.set_value("date_last_updated", DateTime.UtcNow.ToString("o"), case_item);
        gsv.set_value("last_updated_by", "migration_plan", case_item);


        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(case_item, settings);

        string put_url = $"{db_server_url}/{db_name}"  + case_item["_id"];
        cURL document_curl = new cURL ("PUT", null, put_url, object_string, config_timer_user_name, config_timer_value);

        try
        {
            var responseFromServer = await document_curl.executeAsync();
            var	put_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

            if(put_result.ok)
            {
                result = true;
            }
            
        }
        catch(Exception ex)
        {
            //Console.Write("auth_session_token: {0}", auth_session_token);
            Console.WriteLine(ex);
        }

        return result;
    }
    */
    private void process_node(ref bool is_changed, mmria.common.metadata.node p_metadata, object p_value)
    {
        IDictionary<string,object> object_dictionary = null;
        switch(p_metadata.type.ToLowerInvariant())
        {
            case "group":
                object_dictionary = p_value as IDictionary<string,object>;
                
                if(object_dictionary != null)
                foreach(var child in p_metadata.children)
                {
                    if(object_dictionary.ContainsKey(child.name))
                    {
                        try
                        {
                            process_node(ref is_changed, child, object_dictionary[child.name]);
                        }
                        catch(Exception)
                        {
                            Console.WriteLine("unable to process" + child.name + " : " + child.type);
                        }
                    }

                }
            break;				
            case "grid":
                var object_array = p_value as IList<object>;

                if(object_array != null)
                foreach(var object_item in object_array)
                {
                    object_dictionary = object_item as IDictionary<string,object>;
                    if(object_dictionary != null)
                    foreach(var child in p_metadata.children)
                    {
                        if(object_dictionary.ContainsKey(child.name))
                        {
                            try
                            {
                                process_node(ref is_changed, child, object_dictionary[child.name]);
                            }
                            catch(Exception)
                            {
                                Console.WriteLine("unable to process" + child.name + " : " + child.type);
                            }
                        }

                    }
                }
            break;
            case "form":
                if
                (
                    p_metadata.cardinality == "+" ||
                    p_metadata.cardinality == "*"
                )
                {
                    var form_array = p_value as IList<object>;
                    if(form_array != null)
                    foreach(var form in form_array)
                    {
                        object_dictionary = form as IDictionary<string,object>;
                        
                        if(object_dictionary != null)
                        foreach(var child in p_metadata.children)
                        {
                            if(object_dictionary.ContainsKey(child.name))
                            {
                                try
                                {
                                    process_node(ref is_changed, child, object_dictionary[child.name]);
                                }
                                catch(Exception)
                                {
                                    Console.WriteLine("unable to process" + child.name + " : " + child.type);
                                }
                            }
                            

                        }		
                    }
                }
                else
                {
                    object_dictionary = p_value as IDictionary<string,object>;

                    if(object_dictionary != null)
                    foreach(var child in p_metadata.children)
                    {
                        if(object_dictionary.ContainsKey(child.name))
                        {
                            try
                            {
                                process_node(ref is_changed, child, object_dictionary[child.name]);
                            }
                            catch(Exception)
                            {
                                Console.WriteLine("unable to process" + child.name + " : " + child.type);
                            }
                        }
                    }	
                }

            break;
            case "list":
                try
                {
                    //process_list(ref is_changed, this.lookup,  p_metadata, p_value);
                }
                catch(Exception)
                {
                    Console.WriteLine("unable to process" + p_metadata.name + " : " + p_metadata.type);
                }
            break;
            default:
                break;
        }
    }



    private mmria.common.metadata.node get_metadata_node(mmria.common.metadata.app p_metadata, string p_path)
    {

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
    private static Dictionary<string,mmria.common.metadata.value_node[]> get_look_up(mmria.common.metadata.app p_metadata)
    {
        var result = new Dictionary<string,mmria.common.metadata.value_node[]>(StringComparer.OrdinalIgnoreCase);

        foreach(var node in p_metadata.lookup)
        {
            result.Add("lookup/" + node.name, node.values);
        }
        return result;
    }


}
