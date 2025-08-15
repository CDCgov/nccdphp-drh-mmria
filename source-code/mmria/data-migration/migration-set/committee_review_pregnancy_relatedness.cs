using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public sealed class committee_review_pregnancy_relatedness
{
    public string host_db_url;
    public string db_name;
    public string config_timer_user_name;
    public string config_timer_value;

    public bool is_report_only_mode;

    public System.Text.StringBuilder output_builder;

    public Dictionary<string, HashSet<string>> summary_value_dictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    public committee_review_pregnancy_relatedness
    (
        string p_host_db_url, 
        string p_db_name, 
        string p_config_timer_user_name, 
        string p_config_timer_value,
        System.Text.StringBuilder p_output_builder,
        Dictionary<string, HashSet<string>> p_summary_value_dictionary,
        bool p_is_report_only_mode
    ) 
    {

        host_db_url = p_host_db_url;
        db_name = p_db_name;
        config_timer_user_name = p_config_timer_user_name;
        config_timer_value = p_config_timer_value;
        output_builder = p_output_builder;
        summary_value_dictionary = p_summary_value_dictionary;
        is_report_only_mode = p_is_report_only_mode;
    }
    


    public async Task execute()
    {
        this.output_builder.AppendLine($"committee review/pregnancy related migration started at: {DateTime.Now.ToString("o")}");
        try
        {
            var result = await get_matching_cases_for("Pregnancy-Associated but NOT Related");
            foreach(var item in result.docs)
            {
                if(migrate_old_to_new_value(item, "0"))
                {
                    if(!is_report_only_mode)
                    {
                        await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(item as IDictionary<string, object>, "Pregnancy-Associated but NOT Related");
                    }
                }
            }

            result = await get_matching_cases_for("Not Pregnancy Related or Associated (i.e. False Positive)");
            foreach(var item in result.docs)
            {
                if(migrate_old_to_new_value(item, "99"))
                {
                    if(!is_report_only_mode)
                    {
                        await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(item as IDictionary<string, object>, "Not Pregnancy Related or Associated (i.e. False Positive)");
                    }
                }
            }


            result = await get_matching_cases_for("Unable to Determine if Pregnancy Related or Associated");
            foreach(var item in result.docs)
            {
                if(migrate_old_to_new_value(item, "2"))
                {
                    if(!is_report_only_mode)
                    {
                        await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(item as IDictionary<string, object>,"Unable to Determine if Pregnancy Related or Associated");
                    }
                }
            }

            /*
                Update pregnancy-related values Jan 2019

                Update values in the database for the pregnancy-relatedness field 
                that were created with older versions of MMRIA and don't match the current values.
                    This will allow them to display properly on the data input form; 
                    and keep them from creating alternate values in the exported data.

                    completed by above
            */
                
        }
        catch(Exception)
        {

        }
    }


    private async Task<Result_Struct> get_matching_cases_for(string p_find_value)
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

            System.Console.WriteLine(selector_struc_string);


            string find_url = $"{host_db_url}/{db_name}/_find";

            var case_curl = new cURL("POST", null, find_url, selector_struc_string, config_timer_user_name, config_timer_value);
            string responseFromServer = await case_curl.executeAsync();
            
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result_Struct>(responseFromServer);

            System.Console.WriteLine($"case_response.docs.length {result.docs.Length}");
        }
        catch(Exception)
        {

        }

        return result;
    }

    
    private bool migrate_old_to_new_value(System.Dynamic.ExpandoObject case_item, string p_new_value)
    {
        bool result = false;

        var current_value = get_value(case_item, "committee_review/pregnancy_relatedness");

        System.Console.WriteLine($"current_value: {current_value}");

        result = set_value("committee_review/pregnancy_relatedness", p_new_value, case_item);

        var next_value = get_value(case_item, "committee_review/pregnancy_relatedness");
        System.Console.WriteLine($"is_changed: {result} next_value: {next_value}");


        return result;
    }
/*
    private async Task<bool> sav_e_case_del(IDictionary<string, object> case_item)
    {
        bool result = false;

        //var case_item  = p_case_item as System.Collections.Generic.Dictionary<string, object>;

        set_value("date_last_updated", DateTime.UtcNow.ToString("o"), case_item);
        set_value("last_updated_by", "migration_plan", case_item);


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


    public object get_value(System.Dynamic.ExpandoObject p_object, string p_path, string p_data_type = "string")
    {
        object result = null;

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            object index = p_object;

            /*
            if (path[1] == "abnormal_conditions_of_newborn")
            {
                System.Console.WriteLine("break");
            }*/


            for (int i = 0; i < path.Length; i++)
            {
                
                if(i == 0)
                {

                    if (i == path.Length - 1 && index is IDictionary<string, object>)
                    {
                        
                        IDictionary<string, object> dictionary_object = index as IDictionary<string, object>;

                        if(dictionary_object == null)
                        {
                            result = null;
                            return result;
                        }

                        object val = null;

                        if(dictionary_object.ContainsKey(path[i]))
                        {
                            val = dictionary_object[path[i]]; 
                        }

                        if(val != null)
                        {
                            if(val.GetType() == typeof(System.DateTime))
                            {
                                System.DateTime? temp_date_time = val as System.DateTime?;
                                result = temp_date_time.Value;	
                            }
                            else if(val.GetType() == typeof(string))
                            {
                                result = val.ToString();	
                            }

                            else if(val.GetType() == typeof(System.Collections.Generic.List<string>))
                            {
                                
                                result = val as System.Collections.Generic.List<string>;	
                            }
                            else if(val.GetType() == typeof(System.Collections.Generic.List<object>))
                            {

                                result = val as System.Collections.Generic.List<object>;	
                            }
                            else
                            {
                                result = val;	
                            }
                        }
                        else
                        {
                            result = null;	
                        }
                    }
                    else
                    {
                        index = ((IDictionary<string, object>)p_object)[path[i]];
                    }
                }
                else if (i == path.Length - 1)
                {
                    if (index is IDictionary<string, object>)
                    {

                        IDictionary<string, object> dictionary_object = index as IDictionary<string, object>;

                        if(dictionary_object == null)
                        {
                            result = null;
                            return result;
                        }

                        object val = null;

                        if(dictionary_object.ContainsKey(path[i]))
                        {
                            val = dictionary_object[path[i]]; 
                        }


                        if(val != null)
                        {
                            if(val.GetType() == typeof(System.DateTime))
                            {
                                System.DateTime? temp_date_time = val as System.DateTime?;
                                result = temp_date_time.Value;	
                            }
                            else if(val.GetType() == typeof(string))
                            {
                                result = val.ToString();	
                            }

                            else if(val.GetType() == typeof(System.Collections.Generic.List<string>))
                            {

                                result = val as System.Collections.Generic.List<string>;	
                            }
                            else if(val.GetType() == typeof(System.Collections.Generic.List<object>))
                            {

                                result = val as System.Collections.Generic.List<object>;	
                            }
                            else
                            {
                                result = val;	
                            }

                        }
                        else
                        {
                            result = null;	
                        }
                    }
                    else
                    {
                        //System.Console.WriteLine("break");
                    }
                }
                else if(index is IDictionary<string, object>)
                {
                    if(index != null && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        index = ((IDictionary<string, object>)index)[path[i]];
                    }
                
                }
                else if (index != null)
                {
                    System.Console.WriteLine(index.GetType());
                    /*
                    else if (index != null && index[path[i]].GetType() == typeof(IList<object>))
                    {
                        index = index[path[i]] as IList<object>;
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>) && !((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        //System.Console.WriteLine("Index not found. This should not happen. {0}", p_path);
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>))
                    {
                        index = index[path[i]] as IDictionary<string, object>;
                    }
                    */
                }
                else
                {
                    //System.Console.WriteLine("This should not happen. {0}", p_path);
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }

        return result;

    }
}
