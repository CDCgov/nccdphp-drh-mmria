using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

//20.12.01
//21.04.19
public sealed class VitalsMigration01
{
    class CsvItem
    {
        public CsvItem(){}
        public string value {get;set;}
        public string _id {get;set;}
        public string date_created {get;set;}
        public string created_by {get;set;}
        public string date_last_updated {get;set;}
        public string last_updated_by {get;set;}
    }

    private string data_migration_name = "VitalsMigration01";
    public string host_db_url;
    public string db_name;
    public string config_timer_user_name;
    public string config_timer_value;

    public bool is_report_only_mode;

    public System.Text.StringBuilder output_builder;
    private Dictionary<string,mmria.common.metadata.value_node[]> lookup;

    List<Metadata_Node> all_list_set;

    List<Metadata_Node> single_form_value_set;
    List<Metadata_Node> single_form_multi_value_set;
    List<Metadata_Node> single_form_grid_value_set;
    List<Metadata_Node> single_form_grid_multi_value_list_set;
    List<Metadata_Node> multiform_value_set;
    List<Metadata_Node> multiform_multi_value_set;
    List<Metadata_Node> multiform_grid_value_set;

    List<Metadata_Node> multiform_grid_multi_value_set;

    public Dictionary<string, HashSet<string>> summary_value_dictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    public bool is_data_correction = false;

    private string state_prefix;

    mmria.common.couchdb.ConfigurationSet ConfigurationSet = null;

    public VitalsMigration01
    (
        string p_host_db_url, 
        string p_db_name, 
        string p_config_timer_user_name, 
        string p_config_timer_value,
        System.Text.StringBuilder p_output_builder,
        Dictionary<string, HashSet<string>> p_summary_value_dictionary,
        bool p_is_report_only_mode,
        string p_state_prefix,
        mmria.common.couchdb.ConfigurationSet p_ConfigurationSet
    ) 
    {

        host_db_url = p_host_db_url;
        db_name = p_db_name;
        config_timer_user_name = p_config_timer_user_name;
        config_timer_value = p_config_timer_value;
        output_builder = p_output_builder;
        summary_value_dictionary = p_summary_value_dictionary;
        is_report_only_mode = p_is_report_only_mode;
        state_prefix = p_state_prefix;
        ConfigurationSet = p_ConfigurationSet;
    }


    public async Task execute()
    {
        this.output_builder.AppendLine($"{data_migration_name} Data Migration started at: {DateTime.Now.ToString("o")}");
        DateTime begin_time = System.DateTime.Now;
        
        this.output_builder.AppendLine($"{data_migration_name} started at: {begin_time.ToString("o")}");
        
        var batch_list = await GetVitalBatch();

        var mmria_id_set = new Dictionary<string,HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        HashSet<string> ImportStateSet = new(StringComparer.OrdinalIgnoreCase);

        foreach(var b in batch_list.rows)
        {
            if(b.doc.record_result.Count > 0)
            {
                var state = b.doc.record_result[0].ReportingState;
                ImportStateSet.Add(state);
                mmria_id_set[state] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach(var r in b.doc.record_result)
                {
                    mmria_id_set[state].Add(r.mmria_id);
                }
            }
        }

        var reporting_state = host_db_url.Replace("https://couchdb-mmria-", "").Replace(".apps.ecpaas.cdc.gov","");
        if(!ImportStateSet.Contains(reporting_state))
        {
            Console.WriteLine($"State {reporting_state} has no vital imports");
            Console.WriteLine($"{data_migration_name} Finished {DateTime.Now}");
            return;
        }




        var gs = new C_Get_Set_Value(this.output_builder);
        try
        {
            //string metadata_url = host_db_url + "/metadata/2016-06-12T13:49:24.759Z";
            string metadata_url = $"https://couchdb-test-mmria.apps.ecpaas-dev.cdc.gov/metadata/version_specification-20.12.01/metadata";

            //string metadata_url = $"{host_db_url}/metadata/version_specification-20.12.01/metadata";
            
            cURL metadata_curl = new cURL("GET", null, metadata_url, null, null, null);
            mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(await metadata_curl.executeAsync());
        
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

            string url = $"{host_db_url}/{db_name}/_all_docs?include_docs=true";
            var case_curl = new cURL("GET", null, url, null, config_timer_user_name, config_timer_value);
            string responseFromServer = await case_curl.executeAsync();
            
            var race_value_list = this.lookup["lookup/race"];
            var omb_race_recode_list = this.lookup["lookup/omb_race_recode"];
            var value_to_display = new Dictionary<int,string>();
            var display_to_value = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach(var item in race_value_list)
            {
                value_to_display.Add(int.Parse(item.value), item.display);
                
            }

            foreach(var item in omb_race_recode_list)
            {
                display_to_value.Add(item.display, int.Parse(item.value));
            }

            display_to_value.Add("Black or African American", 1);
            display_to_value.Add("American Indian or Alaska Native", 3);

            var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<System.Dynamic.ExpandoObject>>(responseFromServer);

            foreach(var case_item in case_response.rows)
            {
                var case_has_changed = false;
                var case_change_count = 0;

                var doc = case_item.doc;
                
                if(doc != null)
                {

                    C_Get_Set_Value.get_value_result value_result = gs.get_value(doc, "_id");
                    var mmria_id = value_result.result?.ToString();
                    if(mmria_id.IndexOf("_design") > -1)
                    {
                        continue;
                    }

                    if(!mmria_id_set[reporting_state].Contains(mmria_id))
                    {
                        continue;
                    }

// change single select into multiselect
// home_record/how_was_this_death_identified

                    try
                    {

                        var how_was_this_death_identified_path = "home_record/how_was_this_death_identified";
                        value_result = gs.get_value(doc, how_was_this_death_identified_path);
                        var current_record_id = value_result.result;

                        int new_int_value  = 9999;

                        if (!value_result.is_error)
                        {
                            if(value_result.result is IList<object> value_list)
                            {
                                Console.WriteLine("item record_id: {mmria_id} Already converted skipping");
                            }
                            else if(value_result.result != null)
                            {
                                Console.WriteLine("Not IList");

                                if(!int.TryParse(value_result.result.ToString(), out new_int_value))
                                {
                                    new_int_value = 9999;
                                    //Console.WriteLine($"Value is NOT int {value_result.result}");
                                }
                                else
                                {
                                    Console.WriteLine($"Value {value_result.result}");
                                }

                                if(case_change_count == 0)
                                {
                                    case_change_count += 1;
                                    case_has_changed = true;
                                }

                                var new_value = new List<object>() { new_int_value };
                                case_has_changed = case_has_changed && gs.set_objectvalue(how_was_this_death_identified_path, new_value, doc);
                                var output_text = $"item record_id: {mmria_id} Converted single item to list.  {value_result.result} => [ {new_int_value} ]";
                                this.output_builder.AppendLine(output_text);
                                Console.WriteLine(output_text);
                            }
                            
                            
                        }
                    
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    

                    if(!is_report_only_mode && case_has_changed)
                    {
                        var save_result = await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(doc as IDictionary<string, object>, data_migration_name);
                    }

                }

        
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }

        Console.WriteLine($"{data_migration_name} Finished {DateTime.Now}");
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
                    Node = node,
                    sass_export_name = node.sass_export_name
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
                display_to_value = display_to_value,
                sass_export_name = p_node.sass_export_name
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


    private string calculate_omb_recode(IList<string> p_value_list)
    {
        string result = "(blank)";
        var asian_list = new string[7]{ 
                "Asian Indian",
                "Chinese",
                "Filipino",
                "Japanese",
                "Korean",
                "Vietnamese",
                "Other Asian"
            };
        var islander_list = new string[4]{
                "Native Hawaiian",
                "Guamanian or Chamorro",
                "Samoan",
                "Other Pacific Islander"
            };
        if (p_value_list.Count == 0)
        {
        }
        else if (p_value_list.Count == 1)
        {
            if (get_intersection(p_value_list, asian_list)?.Count > 0) {
                result = "Asian";
            } else if (get_intersection(p_value_list, islander_list)?.Count > 0) {
                result = "Pacific Islander";
            } else
            {
                result = p_value_list[0];
            }
        }
        else
        {
            if (p_value_list.Contains("Race Not Specified"))
            {
                result = "Race Not Specified";
            }
            else
            {
                var asian_intersection_count = get_intersection(p_value_list, asian_list)?.Count;
                var is_asian = 0;
                var islander_intersection_count = get_intersection(p_value_list, islander_list)?.Count;
                var is_islander = 0;
                if (asian_intersection_count > 0)
                    is_asian = 1;
                if (islander_intersection_count > 0)
                    is_islander = 1;
                var number_not_in_asian_or_islander_categories = p_value_list.Count - asian_intersection_count - islander_intersection_count;
                var total_unique_items = number_not_in_asian_or_islander_categories + is_asian + is_islander;
                switch (total_unique_items)
                {
                    case 1:
                        if (is_asian == 1)
                        {
                            result = "Asian";
                        }
                        else if (is_islander == 1)
                        {
                            result = "Pacific Islander";
                        }
                        else
                        {
                            Console.WriteLine("This should never happen bug");
                        }
                        break;
                    case 2:
                        result = "Bi-Racial";
                        break;
                    default:
                        result = "Multi-Racial";
                        break;
                }
            }
        }
        return result;
    }

    public IList<string> get_intersection(IList<string> p_list_1, IList<string> p_list_2)
    {
        var result = p_list_1.Intersect(p_list_2)?.ToArray();

        //var a = p_list_1;
        //var b = p_list_2;
        //a.sort();
        //b.sort();
        //var ai = 0, bi = 0;
        //var result = [];
        //while (ai < a.length && bi < b.length)
        //{
        //    if (a[ai] < b[bi])
        //    {
        //        ai++;
        //    }
        //    else if (a[ai] > b[bi])
        //    {
        //        bi++;
        //    }
        //    else
        //    {
        //        result.push(a[ai]);
        //        ai++;
        //        bi++;
        //    }
        //}
        return result;
    }

    public async Task<mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch>> GetVitalBatch() 
    { 
        mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch> result = null;

        try
        {
            var config = ConfigurationSet.detail_list["vital_import"];
            
            string url = $"{config.url}/vital_import/_all_docs?include_docs=true";


            var user_curl = new cURL("GET", null, url, null, config.user_name, config.user_value);

            var responseFromServer = await user_curl.executeAsync();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch>>(responseFromServer);

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
            
        }


        return result;
    }


}


