using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public struct id_eq_struct
{
    public string _id;
}



public struct selector_id_struct
{
    public id_eq_struct selector;
    public int limit;
}
public sealed class editable_list
{

/*
    "autopsy_report/toxicology/substance":
    "prenatal/substance_use_grid/substance":
    "medical_transport/origin_information/place_of_origin":
    "social_and_environmental_profile/if_yes_specify_substances/substance":
*/



    public string db_server_url;

    public string db_name;
    public string config_timer_user_name;
    public string config_timer_value;
    public bool is_report_only_mode;

    public System.Text.StringBuilder output_builder;

    public Dictionary<string, HashSet<string>> summary_value_dictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string,mmria.common.metadata.value_node[]> lookup;

    public editable_list
    (
        string p_db_server_url, 
        string p_db_name, 
        string p_config_timer_user_name, 
        string p_config_timer_value,
        System.Text.StringBuilder p_output_builder,
        Dictionary<string, HashSet<string>> p_summary_value_dictionary,
        bool p_is_report_only_mode
    ) 
    {

        db_server_url = p_db_server_url;
        db_name = p_db_name;
        config_timer_user_name = p_config_timer_user_name;
        config_timer_value = p_config_timer_value;
        output_builder = p_output_builder;
        summary_value_dictionary = p_summary_value_dictionary;
        is_report_only_mode = p_is_report_only_mode;
    }
    


    public async Task execute()
    {
        DateTime begin_time = System.DateTime.Now;
        
        this.output_builder.AppendLine($"editable list migration started at: {begin_time.ToString("o")}");

        try
        {
            //string migration_plan_id = message.ToString();

            Console.WriteLine($"Process_Editable_List Begin {begin_time}");


            //string metadata_url = db_server_url + "/metadata/2016-06-12T13:49:24.759Z";
            string metadata_url = $"{db_server_url}/metadata/version_specification-20.04.21/metadata";
            
            cURL metadata_curl = new cURL("GET", null, metadata_url, null, config_timer_user_name, config_timer_value);
            mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(await metadata_curl.executeAsync());

            this.lookup = get_look_up(metadata);


            var substance_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var place_of_origin_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach(var item in this.lookup["lookup/substance"])
            {
                substance_set.Add(item.value);
            }

            var place_of_origin_node = get_metadata_node(metadata, "medical_transport/origin_information/place_of_origin");
            foreach(var item in place_of_origin_node.values)
            {
                place_of_origin_set.Add(item.value);
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
                var case_item = case_response_item.doc;
                // get_grid_value "autopsy_report/toxicology/substance":
                // get_grid_value"prenatal/substance_use_grid/substance":
                // get_multiform_value "medical_transport/origin_information/place_of_origin":
                // get_grid_value"social_and_environmental_profile/if_yes_specify_substances/substance":
                
                var change_history_list = new List<(bool, bool)>();
                
                change_history_list.Add(execute_grid_changes_for(case_item,"autopsy_report/toxicology/substance", substance_set));
                change_history_list.Add(execute_grid_changes_for(case_item,"prenatal/substance_use_grid/substance", substance_set));
                change_history_list.Add(execute_grid_changes_for(case_item,"social_and_environmental_profile/if_yes_specify_substances/substance", substance_set));
                change_history_list.Add(execute_multiform_changes_for(case_item,"medical_transport/origin_information/place_of_origin", place_of_origin_set));

                /*
                var grid_value = new C_Get_Set_Value().get_multiform_value(case_item,"medical_transport/origin_information/place_of_origin");
                foreach(var child in grid_value)
                {
                    System.Console.WriteLine($"{child.Item1}: {child.Item2}");
                }
                */

                var save_record = false;
                foreach(var item in change_history_list)
                {
                    if(item.Item1 == true)	
                    {
                        save_record = true;
                        break;
                    }
                }

                foreach(var item in change_history_list)
                {
                    if(item.Item1 == true)	
                    {
                        save_record = save_record && item.Item2;
                        break;
                    }
                }

                if(!is_report_only_mode && save_record)
                {
                    var gsv = new C_Get_Set_Value(this.output_builder);
                    gsv.set_value("date_last_updated", DateTime.UtcNow.ToString("o"), case_item);
                    gsv.set_value("last_updated_by", "migration_plan", case_item);

                    settings = new Newtonsoft.Json.JsonSerializerSettings ();
                    settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    var changed_object_string = Newtonsoft.Json.JsonConvert.SerializeObject(case_item, settings);

                    IDictionary<string, object> doc = case_item as IDictionary<string, object>;

                    string put_url = $"{db_server_url}/{db_name}/{doc["_id"]}";
                    cURL document_curl = new cURL ("PUT", null, put_url, changed_object_string, config_timer_user_name, config_timer_value);

                    try
                    {
                        responseFromServer = document_curl.execute();
                        var	result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
                    }
                    catch(Exception ex)
                    {
                        //Console.Write("auth_session_token: {0}", auth_session_token);
                        this.output_builder.AppendLine("editable list migration exception:{ex}");
                        Console.WriteLine(ex);
                    }
                }

            }

            Console.WriteLine($"EditableList End {System.DateTime.Now}");
        
        }
        catch(Exception ex)
        {
            this.output_builder.AppendLine($"editable list migration exception:{ex}");
            Console.WriteLine($"EditableList exception {System.DateTime.Now}\n{ex}");
        }

        TimeSpan time_span = System.DateTime.Now - begin_time;
        Console.WriteLine($"EditableList duration total seconds: {time_span.TotalSeconds} total_minutes{time_span.TotalMinutes}\n");
        
    }

    private (bool, bool)  execute_multiform_changes_for(System.Dynamic.ExpandoObject p_object, string p_path, HashSet<string> p_value_set)

    {
        var all_changes_good = false;
        var result = false;

        var gs = new C_Get_Set_Value(this.output_builder);
        C_Get_Set_Value.get_multiform_value_result multiform_value_result = gs.get_multiform_value(p_object, p_path); 

        if(!multiform_value_result.is_error)
        {
            var grid_value = multiform_value_result.result;

            var list_change_set  = new List<(int, object)>();
            var list_other_change_set  = new List<(int, object)>();

            var mmria_id = gs.get_value(p_object, "_id");


            var path_key = $"{p_path}_other";
            if(!this.summary_value_dictionary.ContainsKey(path_key))
            {
                this.summary_value_dictionary.Add(path_key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }

            foreach(var child in grid_value)
            {

                var list_value_object = child.Item2;
                string list_value = "";
                if(list_value_object != null)
                {
                    list_value = list_value_object.ToString();
                }

                if(string.IsNullOrWhiteSpace(list_value) && p_value_set.Contains("9999"))
                {
                    list_change_set.Add((child.Item1, "9999"));
                    //list_other_change_set.Add((child.Item1, list_value));
                }
                else if(!p_value_set.Contains(list_value))
                {
                    System.Console.WriteLine($"{child.Item1}: {list_value}");
                    list_change_set.Add((child.Item1, "Other"));
                    list_other_change_set.Add((child.Item1, list_value));

                    if(!this.summary_value_dictionary[path_key].Contains(list_value))
                    {
                        this.summary_value_dictionary[path_key].Add(list_value);
                    }

                    this.output_builder.AppendLine($"editable list item added to other record_id: {mmria_id} path:{p_path} item: {list_value}");
                }
                else if(list_value == "Other")
                {
                    multiform_value_result = gs.get_multiform_value(p_object, path_key);
                    var other_value_list = multiform_value_result.result;
                    foreach(var other_value_item in other_value_list)
                    {
                        if(other_value_item.Item1 == child.Item1)
                        {
                            var other_value = other_value_item.Item2;

                            if(other_value != null && !string.IsNullOrWhiteSpace(other_value.ToString()) && !this.summary_value_dictionary[path_key].Contains(other_value.ToString()))
                            {
                                this.summary_value_dictionary[path_key].Add(other_value.ToString());
                            }

                            break;
                        }
                    }
                }
            }

            
            if(list_change_set.Count > 0 || list_other_change_set.Count > 0)
            {
                result = true;
            
                all_changes_good = gs.set_multiform_value(p_object,p_path, list_change_set);
                all_changes_good = all_changes_good && gs.set_multiform_value(p_object,$"{p_path}_other", list_other_change_set);
            }
        }
        return (result, all_changes_good);

    }


    private (bool, bool) execute_grid_changes_for(System.Dynamic.ExpandoObject p_object, string p_path, HashSet<string> p_value_set)

    {
        var all_changes_good = false;
        var result = false;



        var gs = new C_Get_Set_Value(this.output_builder);

        C_Get_Set_Value.get_grid_value_result grid_value_result = gs.get_grid_value(p_object, p_path);

        if(!grid_value_result.is_error)
        {
            var grid_value = grid_value_result.result;

            
            var list_change_set  = new List<(int, object)>();
            var list_other_change_set  = new List<(int, object)>();
            var mmria_id = gs.get_value(p_object, "_id");

            var path_key = $"{p_path}_other";
            if(!this.summary_value_dictionary.ContainsKey(path_key))
            {
                this.summary_value_dictionary.Add(path_key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }

            foreach(var child in grid_value)
            {

                var list_value = child.Item2?.ToString();

                if(string.IsNullOrWhiteSpace(list_value) && p_value_set.Contains("9999"))
                {
                    list_change_set.Add((child.Item1, "9999"));
                    //list_other_change_set.Add((child.Item1, list_value));
                }
                else if(!p_value_set.Contains(list_value))
                {
                    System.Console.WriteLine($"{child.Item1}: {list_value}");
                    list_change_set.Add((child.Item1, "Other"));
                    list_other_change_set.Add((child.Item1, list_value));

                    if(!this.summary_value_dictionary[path_key].Contains(list_value))
                    {
                        this.summary_value_dictionary[path_key].Add(list_value.ToString());
                    }
                    this.output_builder.AppendLine($"editable list item added to other record_id: {mmria_id} path:{p_path} item: {list_value}");
                }
                else if(list_value == "Other")
                {

                    //C_Get_Set_Value.get_grid_value_result
                    grid_value_result = gs.get_grid_value(p_object, path_key);
                    var other_value_list = grid_value_result.result;
                    
                    foreach(var other_value_item in other_value_list)
                    {
                        if(other_value_item.Item1 == child.Item1)
                        {
                            var other_value = other_value_item.Item2;

                            if(other_value != null && !string.IsNullOrWhiteSpace(other_value.ToString()) && !this.summary_value_dictionary[path_key].Contains(other_value.ToString()))
                            {
                                this.summary_value_dictionary[path_key].Add(other_value.ToString());
                            }

                            break;
                        }
                    }
                }

                
            }

            
            if(list_change_set.Count > 0 || list_other_change_set.Count > 0)
            {
                result = true;
            

                all_changes_good = gs.set_grid_value(p_object,p_path, list_change_set);
                all_changes_good = all_changes_good && gs.set_grid_value(p_object,$"{p_path}_other", list_other_change_set);
            }
        }

        return (result, all_changes_good);

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
