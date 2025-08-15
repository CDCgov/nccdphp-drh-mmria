using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public sealed class vSEP_DataFix
{
    sealed class DataTupleComparer : IComparer<DataTuple>
    {
        public int Compare(DataTuple x, DataTuple y)
        {
            if (!x.DateLastUpdated.HasValue || !y.DateLastUpdated.HasValue)
            {
                if(x.DateLastUpdated.HasValue  == y.DateLastUpdated.HasValue)
                return 0;

                if(!x.DateLastUpdated.HasValue)
                return -1;

                if(!y.DateLastUpdated.HasValue)
                return 1;
            }
            
            // CompareTo() method
            return x.DateLastUpdated.Value.CompareTo(y.DateLastUpdated.Value);
            
        }
    }

    sealed class DataTuple
    {
        public string _id {get;set;}
        public string ReportingState{get;set;}
        public string SourceFilePath{get;set;}

        public DateTime? date_created{get;set;}
        public string  created_by{get;set;}
        public DateTime? DateLastUpdated{get;set;}
        public string LastUpdateByWho{get;set;}
        public string SEPValue {get;set;}  

    }

    sealed class CsvItem
    {
        public CsvItem(){}
        public string value {get;set;}
        public string _id {get;set;}
        public string date_created {get;set;}
        public string created_by {get;set;}
        public string date_last_updated {get;set;}
        public string last_updated_by {get;set;}
    }

    private string data_migration_name = "vSEP_DataFix";
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

    public vSEP_DataFix
    (
        string p_host_db_url, 
        string p_db_name, 
        string p_config_timer_user_name, 
        string p_config_timer_value,
        System.Text.StringBuilder p_output_builder,
        Dictionary<string, HashSet<string>> p_summary_value_dictionary,
        bool p_is_report_only_mode,
        string p_state_prefix
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
    }


    public async Task execute()
    {
        this.output_builder.AppendLine($"{this.data_migration_name} Data Migration started at: {DateTime.Now.ToString("o")}");
        DateTime begin_time = System.DateTime.Now;
        
        this.output_builder.AppendLine($"{data_migration_name} started at: {begin_time.ToString("o")}");
        
        var gs = new C_Get_Set_Value(this.output_builder);
        try
        {

            HashSet<string> state_list = new(StringComparer.OrdinalIgnoreCase)
            {
                "ma",
                "nc",
                "oh",
                "tn",
                "ut",
                "wi"
            };
            

            //C:\work-space\bk-file-set
            //C:\work-space\bk-file-set\migration
            //03/19/2020


            Dictionary<string, CsvItem> csv = new(StringComparer.OrdinalIgnoreCase);

            if(state_list.Contains(state_prefix) )	
            {
                var csv_data = ParseCsv(System.IO.File.ReadAllText($"C:/Users/isu7/Downloads/RMOR_Backup/{state_prefix}/0/0.csv"));


                var value_column_index = csv_data[0].IndexOf("saepsoes_eosoe_stres");
                var _id_column_index = csv_data[0].IndexOf("_id");
                var date_created_column_index = csv_data[0].IndexOf("d_creat");
                var created_by_column_index = csv_data[0].IndexOf("c_by");
                var date_last_updated_column_index = csv_data[0].IndexOf("dl_updat");
                var last_updated_by_column_index = csv_data[0].IndexOf("lu_by");

                foreach(var item in csv_data.Skip(1))
                {
                    var id =item[_id_column_index];
                    var value = item[value_column_index];

                    if
                    (
                        string.IsNullOrWhiteSpace(value) || 
                        value.IndexOf("|") <1 && 
                        value.IndexOf("|") == value.LastIndexOf("|") 
                        //||
                        //value.IndexOf("|") == 0

                    )
                    continue;

                    {
                        csv.Add(id, new CsvItem()
                        {
                            value = value,
                            _id = id,
                            date_created = item[date_created_column_index],
                            created_by = item[created_by_column_index],
                            date_last_updated = item[date_last_updated_column_index],
                            last_updated_by = item[last_updated_by_column_index]
                        });
                    }
                }
            }

            List<DataTuple> DataTupleList = new();
            Dictionary<string, string> state_id_map = new();
            
            DataTupleList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DataTuple>>(System.IO.File.ReadAllText("c:/temp/SEPData.json"));

            state_id_map = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText("c:/temp/SEPState_ID_Map.json"));


            var backup_list = DataTupleList.Where
            ( 
                i=> i.ReportingState.Equals(state_prefix, StringComparison.OrdinalIgnoreCase) && 
                !string.IsNullOrWhiteSpace(i.SEPValue) &&
                i.SEPValue.Split("|").Length > 1
            ).ToList();


            backup_list.Sort(new DataTupleComparer());

            if(csv.Count == 0)
            {

                foreach(var item in backup_list)
                {
                    var is_add = false;

                    if(csv.ContainsKey(item._id))
                    {
                        var current_count = item.SEPValue.Split("|").Length;
                        var existing_count = csv[item._id].value.Split("|").Length;

                        if(current_count > existing_count)
                        {
                            csv[item._id] = new CsvItem()
                            {
                                value = item.SEPValue,
                                _id = item._id,
                                date_created = item.date_created.HasValue ? item.date_created.Value.ToString("o") : null,
                                created_by = item.created_by,
                                date_last_updated = item.DateLastUpdated.HasValue ? item.DateLastUpdated.Value.ToString("o") : null,
                                last_updated_by = item.LastUpdateByWho
                            };
                        }
                    }
                    else
                    {
                        is_add = true;
                    }

                    if(is_add)
                    {
                        csv.Add(item._id, new CsvItem()
                        {
                            value = item.SEPValue,
                            _id = item._id,
                            date_created = item.date_created.HasValue ? item.date_created.Value.ToString("o") : null,
                            created_by = item.created_by,
                            date_last_updated = item.DateLastUpdated.HasValue ? item.DateLastUpdated.Value.ToString("o") : null,
                            last_updated_by = item.LastUpdateByWho
                        });
                    }
                }
            
                if(csv.Count == 0)
                {
                    this.output_builder.AppendLine($"{state_prefix}: csv.Count:{csv.Count} Nothing to Process. Exiting");
                    return;
                }
            }

            //var csv_data = ConvertCSVtoDataTable($"C:/Users/isu7/Downloads/RMOR_Backup/{state_prefix}/0/0.csv");

            System.Console.WriteLine($"{state_prefix}: {csv.Count}");

            //var sub = csv.Where( i=> i.value.IndexOf("|") < 0)).To


            //string metadata_url = host_db_url + "/metadata/2016-06-12T13:49:24.759Z";
            string metadata_url = $"https://couchdb-test-mmria.apps.ecpaas-dev.cdc.gov/metadata/version_specification-{Program.config_metadata_version}/metadata";
            
            //return;

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
            
            Metadata_Node sep_node = single_form_multi_value_set.Where(i=>i.path == "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress" ).First();

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

                    if(!csv.ContainsKey(mmria_id)) continue;

                    var old_item = csv[mmria_id.ToString()];

// change single select into multiselect
// home_record/how_was_this_death_identified

                    List<string> old_string_list = new();
                    List<long> old_list = new();

                    foreach(var item in old_item.value.Split("|"))
                    {
                        old_string_list.Add(item);
                        var key = item;
                        long key_check = -1;

                        if(!sep_node.display_to_value.ContainsKey(item))
                        {
                            if(string.IsNullOrEmpty(key))
                            {
                                continue;
                            }

                            //System.Console.WriteLine($"*** missing value:{key}");
                            //this.output_builder.AppendLine($"missing value:{key}");
                            if(item == "History of Substance Use Treatment")
                            {
                                key = "History of Treatment for Substance Use";
                            }
                            else
                            {
                                if(!long.TryParse(key, out key_check))
                                {
                                    System.Console.WriteLine(key);
                                }
                            }
                        }
                        
                        
                        if(long.TryParse(key, out key_check))
                        {
                            old_list.Add(key_check);
                        }
                        else
                        {
                            var item_value =  long.Parse(sep_node.display_to_value[key]);
                            old_list.Add(item_value);
                        }
                    }

                    try
                    {

                        var sep_path = "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress";
                        value_result = gs.get_value(doc, sep_path);
                        var current_record_id = value_result.result;

                        int new_int_value  = 9999;

                        if (!value_result.is_error)
                        {
                            if(value_result.result is IList<object> value_list)
                            {
                                if(value_list.Count < 2)
                                {
                                    Console.WriteLine($"_id: {mmria_id}");
                                    Console.WriteLine($"old_list: {string.Join('|', old_list)}");
                                    Console.WriteLine($"current_list: {string.Join('|', value_list)}");

                                    this.output_builder.AppendLine($"-id: {mmria_id}");
                                    this.output_builder.AppendLine($"old_list: {string.Join('|', old_list)}");
                                    this.output_builder.AppendLine($"current_list: {string.Join('|', value_list)}");

                                if(case_change_count == 0)
                                {
                                    case_change_count += 1;
                                    case_has_changed = true;
                                }

                                
                                case_has_changed = case_has_changed && gs.set_objectvalue(sep_path, old_list, doc);
                                var output_text = $"item record_id: {mmria_id} Applied SEP List Fix.  {string.Join('|',value_result.result)} => [ {string.Join('|',old_list)} ]";
                                this.output_builder.AppendLine(output_text);
                                Console.WriteLine(output_text);

                                }
                            }
                            /*
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
                                case_has_changed = case_has_changed && gs.set_objectvalue(sep_path, new_value, doc);
                                var output_text = $"item record_id: {mmria_id} Converted single item to list.  {value_result.result} => [ {new_int_value} ]";
                                this.output_builder.AppendLine(output_text);
                                Console.WriteLine(output_text);
                            }*/
                            
                            
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

/*
    private async Task<bool> sav_e_case_del(IDictionary<string, object> case_item)
    {
        bool result = false;
        var gsv = new C_Get_Set_Value(this.output_builder);

        //var case_item  = p_case_item as System.Collections.Generic.Dictionary<string, object>;

        gsv.set_value("date_last_updated", DateTime.UtcNow.ToString("o"), case_item);
        gsv.set_value("last_updated_by", "migration_plan", case_item);


        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(case_item, settings);

        string put_url = $"{host_db_url}/{db_name}/{case_item["_id"]}";
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

    public static System.Data.DataTable ConvertCSVtoDataTable(string strFilePath)
    {
        System.Data.DataTable dt = new ();
        using (System.IO.StreamReader sr = new (strFilePath))
        {
            string[] headers = sr.ReadLine().Split(',');
            foreach (string header in headers)
            {
                dt.Columns.Add(header);
            }

            while (!sr.EndOfStream)
            {
                string[] rows = sr.ReadLine().Split(',');
                System.Data.DataRow dr = dt.NewRow();
                for (int i = 0; i < headers.Length; i++)
                {
                    dr[i] = rows[i];
                }
                dt.Rows.Add(dr);
            }

        }


        return dt;
    }

    static List<List<string>> ParseCsv(string csv) {
        var parsedCsv = new List<List<string>>();
        var row = new List<string>();
        string field = "";
        bool inQuotedField = false;

        for (int i = 0; i < csv.Length; i++) {
            char current = csv[i];
            char next = i == csv.Length - 1 ? ' ' : csv[i + 1];

            // if current character is not a quote or comma or carriage return or newline (or not a quote and currently in an a quoted field), just add the character to the current field text
            if ((current != '"' && current != ',' && current != '\r' && current != '\n') || (current != '"' && inQuotedField)) {
                field += current;
            } else if (current == ' ' || current == '\t') {
                continue; // ignore whitespace outside a quoted field
            } else if (current == '"') {
                if (inQuotedField && next == '"') { // quote is escaping a quote within a quoted field
                    i++; // skip escaping quote
                    field += current;
                } else if (inQuotedField) { // quote signifies the end of a quoted field
                    row.Add(field);
                    if (next == ',') {
                        i++; // skip the comma separator since we've already found the end of the field
                    }
                    field = "";
                    inQuotedField = false;
                } else { // quote signifies the beginning of a quoted field
                    inQuotedField = true; 
                }
            } else if (current == ',') { //
                row.Add(field);
                field = "";
            } else if (current == '\n') {
                row.Add(field);
                parsedCsv.Add(new List<string>(row));
                field = "";
                row.Clear();
            }
        }

        return parsedCsv;
    }



}


