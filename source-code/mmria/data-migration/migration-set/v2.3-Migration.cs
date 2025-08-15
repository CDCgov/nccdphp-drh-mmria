using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public sealed class Case_Status
{
    public int? overall_case_status { get; set; } = 9999;//- case_status

    public string abstraction_begin_date { get; set; } // -abstrn_bgn_date

    public string abstraction_complete_date { get; set; } // - abstrn_cmp_date

    public string projected_review_date { get; set; } // - prjtd_rvw_date

    public string case_locked_date { get; set; } // - case_lck_date

//6.	committee_review/critical_factors_worksheet/recommendation_level { get; set; } // -crcfw_categ_rec
}
public sealed class v2_3_Migration
{

    public string host_db_url;
    public string db_name;
    public string config_timer_user_name;
    public string config_timer_value;

    public bool is_report_only_mode;

    public System.Text.StringBuilder output_builder;
    private Dictionary<string,mmria.common.metadata.value_node[]> lookup;

    public Dictionary<string, HashSet<string>> summary_value_dictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    public v2_3_Migration
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
        this.output_builder.AppendLine($"v2.3 Data Migration started at: {DateTime.Now.ToString("o")}");
        DateTime begin_time = System.DateTime.Now;
        
        this.output_builder.AppendLine($"v2_3_Migration started at: {begin_time.ToString("o")}");
        
        var gs = new C_Get_Set_Value(this.output_builder);
        try
        {
            //string metadata_url = host_db_url + "/metadata/2016-06-12T13:49:24.759Z";
            //string metadata_url = $"https://couchdb-test-mmria.apps.ecpaas-dev.cdc.gov/metadata/version_specification-20.07.13/metadata";

            string metadata_url = $"{host_db_url}/metadata/version_specification-20.07.13/metadata";
            
            cURL metadata_curl = new cURL("GET", null, metadata_url, null, config_timer_user_name, config_timer_value);
            mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(await metadata_curl.executeAsync());

/*
            this.lookup = get_look_up(metadata);

            var estimate_based_on_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var onset_of_labor_was_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


            var estimate_based_on_node = get_metadata_node(metadata, "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on");
            foreach(var item in estimate_based_on_node.values)
            {
                estimate_based_on_set.Add(item.value);
            }


            var onset_of_labor_was_node = get_metadata_node(metadata, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was");
            foreach(var item in onset_of_labor_was_node.values)
            {
                onset_of_labor_was_set.Add(item.value);
            }
*/
            Console.WriteLine($"v2_3_Migration Begin {begin_time}");



            //this.lookup = get_look_up(metadata);
        
        
        //1.	/home_record/case_status/overall_case_status - case_status

        // 	committee_review/critical_factors_worksheet/recommendation_level -crcfw_categ_rec
            string url = $"{host_db_url}/{db_name}/_all_docs?include_docs=true";

            var case_curl = new cURL("GET", null, url, null, config_timer_user_name, config_timer_value);
            string responseFromServer = case_curl.execute();
            
            var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<System.Dynamic.ExpandoObject>>(responseFromServer);

            foreach(var row in case_response.rows)
            {
                var case_item = row.doc;

                var case_has_changed = false;
                var change_count = 0;


                C_Get_Set_Value.get_value_result value_result = gs.get_value(case_item, "_id");
                var mmria_id = value_result.result?.ToString();

                if(mmria_id.IndexOf("_design") > -1)
                {
                    continue;
                }

                DateTime? date_created = null;
                value_result = gs.get_value(case_item, "date_created");
                if(!value_result.is_error)
                {
                    if(value_result.result != null)
                    {
                        if(value_result.result is DateTime)
                        {
                            date_created = value_result.result as DateTime?;
                        }
                        else
                        {

                        }
                    }
                }

                List<(int, object)> change_list = new System.Collections.Generic.List<(int, object)>();	

                string mmria_path = "home_record/case_status";
                value_result = gs.get_value(case_item, mmria_path);

                string abstraction_begin_date = null;
                if(date_created.HasValue)
                {
                    abstraction_begin_date = $"{date_created.Value.Year}-{date_created.Value.Month.ToString().PadLeft(2, '0')}-{date_created.Value.Day.ToString().PadLeft(2, '0')}";
                }
                
                if
                (
                    !value_result.is_error &&
                    value_result.result == null
                )
                {

                    if(change_count == 0)
                    {   
                        case_has_changed = gs.set_objectvalue("home_record/case_status", new System.Dynamic.ExpandoObject(), case_item);

                        case_has_changed =  case_has_changed && gs.set_value("home_record/case_status/overall_case_status", "9999", case_item);

                        case_has_changed = case_has_changed && gs.set_value("home_record/case_status/abstraction_begin_date", abstraction_begin_date, case_item);

                        case_has_changed = case_has_changed && gs.set_value("home_record/case_status/abstraction_complete_date", "", case_item);

                        case_has_changed = case_has_changed && gs.set_value("home_record/case_status/projected_review_date", "", case_item);

                        case_has_changed = case_has_changed && gs.set_value("home_record/case_status/case_locked_date", "", case_item);

                        change_count+= 1;
                    }
                    else
                    {
                        case_has_changed = case_has_changed && gs.set_objectvalue("home_record/case_status", new System.Dynamic.ExpandoObject(), case_item);

                        case_has_changed = case_has_changed && gs.set_value("home_record/case_status/overall_case_status", "9999", case_item);

                        case_has_changed = case_has_changed && gs.set_value("home_record/case_status/abstraction_begin_date", abstraction_begin_date, case_item);

                        case_has_changed = case_has_changed && gs.set_value("home_record/case_status/abstraction_complete_date", "", case_item);

                        case_has_changed = case_has_changed && gs.set_value("home_record/case_status/projected_review_date", "", case_item);

                        case_has_changed = case_has_changed && gs.set_value("home_record/case_status/case_locked_date", "", case_item);

                    }

                }
                /*
                else if(!value_result.is_error)
                {
                    value_result = gs.get_value(case_item, "home_record/case_status/overall_case_status");

                    if
                    (
                        ! value_result.is_error &&
                        (
                            value_result.result == null ||
                            string.IsNullOrWhiteSpace(value_result.result.ToString())
                        )
                    )
                    {
                        if(change_count == 0)
                        {   

                            case_has_changed =  gs.set_value("home_record/case_status/overall_case_status", "9999", case_item);
                            change_count+= 1;
                        }
                        else
                        {
                            case_has_changed =  case_has_changed && gs.set_value("home_record/case_status/overall_case_status", "9999", case_item);
                        }
                    

                        value_result = gs.get_value(case_item, "home_record/case_status/abstraction_begin_date");
                        if
                        (
                            ! value_result.is_error &&
                            (
                                value_result.result == null ||
                                string.IsNullOrWhiteSpace(value_result.result.ToString())
                            )
                        )
                        {
                            if(change_count == 0)
                            {   
                                case_has_changed = gs.set_value("home_record/case_status/abstraction_begin_date", abstraction_begin_date, case_item);
                                change_count+= 1;
                            }
                            else
                            {
                                case_has_changed = case_has_changed && gs.set_value("home_record/case_status/abstraction_begin_date", abstraction_begin_date, case_item);
                            }
                        }
                    }
                }*/
            
        

                // 	committee_review/critical_factors_worksheet/recommendation_level -crcfw_categ_rec
                
                mmria_path = "committee_review/critical_factors_worksheet/recommendation_level";
                var grid_value_result = gs.get_grid_value(case_item, mmria_path);
                if(!grid_value_result.is_error)
                {
                    var grid = grid_value_result.result;

                    List<(int, object)> new_list = new List<(int, object)>();
                    var is_changed = false;
                    for(var i = 0; i < grid.Count; i++)
                    {
                        var tuple = grid[i];

                        if(tuple.Item2 == null)
                        {
                            new_list.Add((tuple.Item1, "9999"));
                            is_changed = true;
                        }
                        else
                        {
                            new_list.Add(tuple);
                        }
                    }


                    if(is_changed)
                    {

                        if(change_count == 0)
                        {
                            case_has_changed = gs.set_grid_value(case_item, mmria_path, new_list);
                            change_count+= 1;
                        }
                        else
                        {
                            case_has_changed = case_has_changed && gs.set_grid_value(case_item, mmria_path, new_list);
                        }
                    }
                }

/*
bcifsri_do_deliv
/birth_certificate_infant_fetal_section/record_identification/date_of_delivery

                mmria_path = "birth_certificate_infant_fetal_section/record_identification/date_of_delivery";
                var multiform_value_result = gs.get_multiform_value(case_item, mmria_path);
                if(!multiform_value_result.is_error)
                {
                    var tuple_list = multiform_value_result.result;
                    foreach(var tuple in tuple_list)
                    {
                        var index = tuple.Item1;
                        var value_dynamic = tuple.Item2;
                        if(value_dynamic != null)
                        {
                            var value_string = value_dynamic.ToString();

                            if(!string.IsNullOrWhiteSpace(value_string))
                            {
                                System.Console.WriteLine($"{mmria_id}\t{value_string}");
                                if(change_count == 0)
                                {
                                    //case_has_changed = gs.set_value(mmria_path, "9999", case_item);
                                    //change_count+= 1;
                                }
                                else
                                {
                                    //case_has_changed = case_has_changed && gs.set_value(mmria_path, "9999", case_item);
                                }
                            }
                        }
                    }
                }
*/

                if(!is_report_only_mode && case_has_changed)
                {
                    var save_result = await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(case_item as IDictionary<string, object>,"v2.3");
                }
            
            }
        
        }
        catch(Exception)
        {

        }

        Console.WriteLine($"v2_3_Migration Finished {DateTime.Now}");
    
/*
new fields

1.	/home_record/case_status/overall_case_status - case_status

2.	home_record/case_status/abstraction_begin_date -abstrn_bgn_date

3.	home_record/case_status/abstraction_complete_date - abstrn_cmp_date

4.	home_record/case_status/projected_review_date - prjtd_rvw_date

5.	home_record/case_status/case_locked_date - case_lck_date

6.	committee_review/critical_factors_worksheet/recommendation_level -crcfw_categ_rec




*/




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



