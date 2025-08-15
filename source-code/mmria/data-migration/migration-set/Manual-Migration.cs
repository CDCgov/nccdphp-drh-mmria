using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public sealed class Manual_Migration
{
    public string db_server_url;

    private string db_name;
    private string config_timer_user_name;
    private string config_timer_value;
    List<mmria.common.model.migration_plan_item> migration_plan_item_list;


    public System.Text.StringBuilder output_builder;
    
    public Dictionary<string, HashSet<string>> summary_value_dictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    public bool is_report_only_mode;

    private Dictionary<string,mmria.common.metadata.value_node[]> lookup;

    public Manual_Migration(
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
    
        migration_plan_item_list = new List<mmria.common.model.migration_plan_item>()
        {
            /*
                New fields created for Boolean fields

                Created 1 new list field to receive data currently captured in 2 Boolean fields.
                2 MMRIA indicators involved, 
                ER/hospital form onset of labor and 
                Prenatal care form estimated date of confinement based on.
            */
            new mmria.common.model.migration_plan_item(){
                old_mmria_path = "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on_ultrasound",
                new_mmria_path = "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on",
                old_value = "true",
                new_value = "0" // "Ultrasound" // 0
            },
            new mmria.common.model.migration_plan_item(){
                old_mmria_path = "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on_lmp",
                new_mmria_path = "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on",
                old_value = "true",
                new_value = "1" // "Last menstrual period" // 1
            },
            new mmria.common.model.migration_plan_item(){
                old_mmria_path = "er_visit_and_hospital_medical_records/onset_of_labor/is_artificial",
                new_mmria_path = "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was",
                old_value = "true",
                new_value = "1" // "Artificial" // 1
            },
            new mmria.common.model.migration_plan_item(){
                old_mmria_path = "er_visit_and_hospital_medical_records/onset_of_labor/is_spontaneous",
                new_mmria_path = "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was",
                old_value = "true",
                new_value = "0" // "Spontaneous" // 0
            },
            /*
                Update pregnancy-related values Jan 2019

                Update values in the database for the pregnancy-relatedness field 
                that were created with older versions of MMRIA and don't match the current values.
                    This will allow them to display properly on the data input form; 
                    and keep them from creating alternate values in the exported data.
            */
                new mmria.common.model.migration_plan_item(){
                old_mmria_path = "committee_review/pregnancy_relatedness",
                new_mmria_path = "committee_review/pregnancy_relatedness",
                old_value = "Pregnancy-Associated but NOT Related",
                new_value = "0"//"Pregnancy-Associated, but NOT -Related"
            },
                new mmria.common.model.migration_plan_item(){
                old_mmria_path = "committee_review/pregnancy_relatedness",
                new_mmria_path = "committee_review/pregnancy_relatedness",
                old_value = "Not Pregnancy Related or Associated (i.e. False Positive)",
                new_value = "99" //"Not Pregnancy-Related or -Associated (i.e. False Positive)"
            },
                new mmria.common.model.migration_plan_item(){
                old_mmria_path = "committee_review/pregnancy_relatedness",
                new_mmria_path = "committee_review/pregnancy_relatedness",
                old_value = "Unable to Determine if Pregnancy Related or Associated",
                new_value = "2" // "Pregnancy-Associated but Unable to Determine Pregnancy-Relatedness"
            }
        };
    }

    public async Task execute()
    {
        DateTime begin_time = System.DateTime.Now;
        
        try
        {
            //string migration_plan_id = message.ToString();

            Console.WriteLine($"Process Manual Migration Begin {begin_time}");

            this.output_builder.AppendLine($"Process Manual Migration started at: {begin_time.ToString("o")}");


            //string metadata_url = db_server_url + "/metadata/2016-06-12T13:49:24.759Z";
            string metadata_url = $"{db_server_url}/metadata/version_specification-20.04.21/metadata";
            
            cURL metadata_curl = new cURL("GET", null, metadata_url, null, config_timer_user_name, config_timer_value);
            mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(await metadata_curl.executeAsync());


            // **** lookup start
            this.lookup = get_look_up(metadata);


            var estimate_based_on_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var onset_of_labor_was_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            //var onset_of_labor_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pregnancy_relatedness_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


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

/*
            var onset_of_labor_node = get_metadata_node(metadata, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor");
            foreach(var item in onset_of_labor_node.values)
            {
                onset_of_labor_set.Add(item.value);
            }*/


            var pregnancy_relatedness_set_node = get_metadata_node(metadata, "committee_review/pregnancy_relatedness");
            foreach(var item in pregnancy_relatedness_set_node.values)
            {
                pregnancy_relatedness_set.Add(item.value);
            }

            // **** lookup end

            var gs = new C_Get_Set_Value(this.output_builder);


            var dummy_ids_to_removed = new System.Collections.Generic.List<string>(){
                "02279162-6be3-49e4-930f-42eed7cd4706",
                "cb9f90c5-fc9b-1530-7d20-4891f9a40027"
            };

            foreach(var id in dummy_ids_to_removed)
            {
                
                var case_result = await get_matching_cases_for("_id", id);
                foreach(var item in case_result.docs)
                {
                    if(!is_report_only_mode)
                    {
                        var rev = gs.get_value(item, "_rev");

                        var delete_url = $"{db_server_url}/{db_name}/{id}?rev={rev}";
                        try
                        {
                            

                            var delete_report_curl = new cURL ("DELETE", null, delete_url, null, config_timer_user_name, config_timer_value);

                            var delete_report_response = await delete_report_curl.executeAsync ();;
                            var result_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (delete_report_response);
                            this.output_builder.AppendLine($"removed case {id} from db: {delete_url}");
                        }
                        catch(Exception ex)
                        {
                            System.Console.WriteLine(ex);
                            this.output_builder.AppendLine($"error tyring to remove case {id} from db: {delete_url}");
                            this.output_builder.AppendLine(ex.ToString());

                        }
                    }

                }
            }



            /*

0 "Ultrasound"
1 "Last menstrual period"
*/

            
            var result = await get_matching_cases_for("prenatal.current_pregnancy.estimated_date_of_confinement.estimate_based_on_ultrasound","true");
            foreach(var item in result.docs)
            {
                bool try_bool = false;
                
                C_Get_Set_Value.get_value_result value_result = gs.get_value(item, "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on");

                if(!value_result.is_error)
                {
                    var test_value_object = value_result.result;
                    
                    var test_value_string = "";
                    if(test_value_object != null)
                    {
                        test_value_string = test_value_object.ToString();
                    }

                    var is_valid_target_value = estimate_based_on_set.Contains(test_value_string);
                    if(!is_valid_target_value)
                    {
                        gs.set_value("prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on", "0", item);
                        try_bool = true;
                    }

                    if(!is_report_only_mode && try_bool)
                    {
                        new SaveRecord(this.db_server_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(item, "Manual-Migration.prenatal.current_pregnancy.estimated_date_of_confinement.estimate_based_on_ultrasound");
                    }
                }

            }



            result = await get_matching_cases_for("prenatal.current_pregnancy.estimated_date_of_confinement.estimate_based_on","Ultrasound");
            foreach(var item in result.docs)
            {
                bool try_bool = false;

                C_Get_Set_Value.get_value_result value_result = gs.get_value(item, "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on");
                if(!value_result.is_error)
                {
                    var test_value_object = value_result.result;
                    var test_value_string = "";
                    if(test_value_object != null)
                    {
                        test_value_string = test_value_object.ToString();
                    }

                    var is_valid_target_value = estimate_based_on_set.Contains(test_value_string);

                    if(!is_valid_target_value)
                    {
                        gs.set_value("prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on", "0", item);
                        try_bool = true;
                    }

                    if(!is_report_only_mode && try_bool)
                    {
                        new SaveRecord(this.db_server_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(item, "Manual-Migration.prenatal.prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on.ultrasound");
                    }
                }

            }
// ***************** 


            
            result = await get_matching_cases_for("prenatal.current_pregnancy.estimated_date_of_confinement.estimate_based_on_lmp","true");
            foreach(var item in result.docs)
            {
                bool try_bool = false;
                //var value = gs.get_value(item, "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on");
                
                C_Get_Set_Value.get_value_result value_result = gs.get_value(item, "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on");

                if(!value_result.is_error)
                {
                    var test_value_object = value_result.result;
                    var test_value_string = "";
                    if(test_value_object != null)
                    {
                        test_value_string = test_value_object.ToString();
                    }

                    var is_valid_target_value = estimate_based_on_set.Contains(test_value_string);
                    if(!is_valid_target_value)
                    {
                        gs.set_value("prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on", "1", item);
                        try_bool = true;
                    }

                    if(!is_report_only_mode && try_bool)
                    {
                        new SaveRecord(this.db_server_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(item, "Manual-Migration.prenatal.current_pregnancy.estimated_date_of_confinement.estimate_based_on_lmp.true");
                    }
                }
            }

            result = await get_matching_cases_for("prenatal.current_pregnancy.estimated_date_of_confinement.estimate_based_on","Last menstrual period");
            foreach(var item in result.docs)
            {
                bool try_bool = false;

                C_Get_Set_Value.get_value_result value_result = gs.get_value(item, "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on");

                if(!value_result.is_error)
                {
                    var value = value_result.result;

                    value_result = gs.get_value(item, "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on");
                    if(!value_result.is_error)
                    {
                        var test_value_object = value_result.result;
                        var test_value_string = "";
                        if(test_value_object != null)
                        {
                            test_value_string = test_value_object.ToString();
                        }

                        var is_valid_target_value = estimate_based_on_set.Contains(test_value_string);
                        if(!is_valid_target_value)
                        {
                            gs.set_value("prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on", "1", item);
                            try_bool = true;
                        }

                        if(!is_report_only_mode && try_bool)
                        {
                            new SaveRecord(this.db_server_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(item, "Manual-Migration.prenatal.current_pregnancy.estimated_date_of_confinement.estimate_based_on.Last menstrual period");
                        }
                    }
                }
            }


// ***************
// ***************
// ***************

            // Map based - start
/*
1 "Artificial"
0 "Spontaneous"



            */


            string url = $"{db_server_url}/{db_name}/_all_docs?include_docs=true";

            var case_curl = new cURL("GET", null, url, null, config_timer_user_name, config_timer_value);
            string responseFromServer = case_curl.execute();
            
            var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<System.Dynamic.ExpandoObject>>(responseFromServer);

/*
            var host_state_array = this.db_server_url.Split("-");
            var host_state = host_state_array[1];

            if(host_state == "central")
            {
                if(db_name.IndexOf("_") > -1)
                {
                    host_state = db_name.Split("_")[0];
                }
            }*/

            var host_state = "mo";

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

                // host_state  *** begin

                /*

                value_result = gs.get_value(case_item, "host_state");
                var test_host_state_object = value_result.result;
                if
                (
                    test_host_state_object == null || 
                    string.IsNullOrWhiteSpace(test_host_state_object.ToString()) ||
                    test_host_state_object.ToString().ToLower() == "central"
                )
                {
                    if(test_host_state_object == null)
                    {
                        if(db_name.IndexOf("_") > -1)
                        {
                            host_state = db_name.Split("_")[0];
                        }
                    }
                    else if(test_host_state_object != null && test_host_state_object.ToString().ToLower() == "central")
                    {
                        if(db_name.IndexOf("_") > -1)
                        {
                            host_state = db_name.Split("_")[0];
                        }
                    }

                    if(change_count == 0)
                    {
                        case_has_changed = gs.set_value("host_state", host_state, case_item);
                        change_count+= 1;
                    }
                    else
                    {
                        case_has_changed = case_has_changed && gs.set_value("host_state", host_state, case_item);
                    }
                }
                */
                // host_state  *** end

                List<(int, object)> change_list = new System.Collections.Generic.List<(int, object)>();	

                C_Get_Set_Value.get_multiform_value_result multiform_value_result = gs.get_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/is_artificial");
                
                if(!multiform_value_result.is_error)
                {
                    var multiform_value_list = multiform_value_result.result;
                    multiform_value_result = gs.get_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was");
                    var target_value_list = multiform_value_result.result;
                    
                    for(var i = 0; i < multiform_value_list.Count; i++)
                    {
                        var value = multiform_value_list[i];
                        var test_value_object = target_value_list[i].Item2;

                        var test_value_string = "";
                        if(test_value_object != null)
                        {
                            test_value_string = test_value_object.ToString();
                        }

                        var is_valid_target_value = onset_of_labor_was_set.Contains(test_value_string);

                        //bool try_bool;
                        if(!is_valid_target_value && value.Item2 != null && !string.IsNullOrWhiteSpace(value.Item2.ToString()) && value.Item2.ToString() == "true")
                        {
                            change_list.Add((value.Item1, "1"));
                        }
                        else
                        {
                            //System.Console.WriteLine($"value.item2: {value.Item2}");
                        }

                    }
                }

                if(change_list.Count > 0)
                {
                    if(change_count == 0)
                    {
                        case_has_changed = gs.set_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was", change_list);
                        change_count+= 1;
                    }
                    else
                    {
                        case_has_changed = case_has_changed && gs.set_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was", change_list);
                    }
                    //System.Console.WriteLine("here");
                }
                change_list.Clear();

                
                multiform_value_result  = gs.get_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/is_spontaneous");
                if(!multiform_value_result.is_error)
                {
                    var multiform_value_list = multiform_value_result.result;

                    multiform_value_result = gs.get_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was");
                    var target_value_list = multiform_value_result.result;
                    for(var i = 0; i < multiform_value_list.Count; i++)
                    {
                        var value = multiform_value_list[i];
                        var test_value_object = target_value_list[i].Item2;

                        var test_value_string = "";
                        if(test_value_object != null)
                        {
                            test_value_string = test_value_object.ToString();
                        }

                        var is_valid_target_value = onset_of_labor_was_set.Contains(test_value_string);

                        //bool try_bool;
                        if(!is_valid_target_value && value.Item2 != null && !string.IsNullOrWhiteSpace(value.Item2.ToString()) && value.Item2.ToString() == "true")
                        {
                            change_list.Add((value.Item1, "0"));
                        }
                        else
                        {
                            //System.Console.WriteLine($"value.item2: {value.Item2}");
                        }

                    }
                }

                if(change_list.Count > 0)
                {
                    if(change_count == 0)
                    {
                        case_has_changed = gs.set_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was", change_list);
                        change_count+= 1;
                    }
                    else
                    {
                        case_has_changed = case_has_changed && gs.set_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was", change_list);
                    }
                }
                change_list.Clear();


                multiform_value_result = gs.get_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/is_artificial");

                if(!multiform_value_result.is_error)
                {
                    var grid_value_list = multiform_value_result.result;
                    multiform_value_result = gs.get_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was");
                    if(!multiform_value_result.is_error)
                    {
                        var target_value_list = multiform_value_result.result;

                        //foreach(var grid_value_list_row in grid_value_list)
                        for(var i = 0; i < grid_value_list.Count; i++)
                        {
                            //grid_value_list_row in grid_value_list
                            var grid_value_list_row = grid_value_list[i];
                            var test_value_object = target_value_list[i].Item2;

                            var test_value_string = "";
                            if(test_value_object != null)
                            {
                                test_value_string = test_value_object.ToString();
                            }

                            var is_valid_target_value = onset_of_labor_was_set.Contains(test_value_string);

                            bool try_bool = false;
                            if(!is_valid_target_value && grid_value_list_row.Item2 != null && bool.TryParse(grid_value_list_row.Item2.ToString(), out try_bool))
                            {
                                if(try_bool)
                                {
                                    change_list.Add((grid_value_list_row.Item1, "1"));
                                }
                                else if(test_value_string.Trim().ToLower() == "Artificial".ToLower() || test_value_string.Trim().ToLower() == "spontaneous".ToLower())
                                {
                                    change_list.Add((grid_value_list_row.Item1, "9999"));	
                                }
                            }
                        }
                    }
                }

                if(change_list.Count > 0)
                {
                    if(change_count == 0)
                    {
                        case_has_changed = gs.set_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was", change_list);
                        change_count+= 1;
                    }
                    else
                    {
                        case_has_changed = case_has_changed && gs.set_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was", change_list);
                    }
                }
                change_list.Clear();

                
                multiform_value_result = gs.get_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/is_spontaneous");
                if(!multiform_value_result.is_error)
                {
                    var multiform_value_list = multiform_value_result.result;

                    multiform_value_result = gs.get_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was");
                    if(!multiform_value_result.is_error)
                    {
                        var target_value_list = multiform_value_result.result;
                        foreach(var item in multiform_value_list)
                        {
                            var test_value_object = target_value_list[item.Item1].Item2;

                            var test_value_string = "";
                            if(test_value_object != null)
                            {
                                test_value_string = test_value_object.ToString();
                            }

                            var is_valid_target_value = onset_of_labor_was_set.Contains(test_value_string);

                            bool try_bool = false;
                            if(!is_valid_target_value && item.Item2 != null && bool.TryParse(item.Item2.ToString(), out try_bool))
                            {
                                if(try_bool)
                                {
                                    change_list.Add((item.Item1, "0"));
                                }
                                else if(test_value_string.Trim().ToLower() == "Artificial".ToLower() || test_value_string.Trim().ToLower() == "spontaneous".ToLower())
                                {
                                    change_list.Add((item.Item1, "9999"));	
                                }
                                
                            }
                            
                        }
                    }
                }

                if(change_list.Count > 0)
                {
                    if(change_count == 0)
                    {
                        case_has_changed = gs.set_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was", change_list);
                        change_count+= 1;
                    }
                    else
                    {
                        case_has_changed = case_has_changed && gs.set_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was", change_list);
                    }
                }
                change_list.Clear();
// **** informant_interview - start

            var informant_interview_race_display_to_value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var informant_interview_race_value_to_display = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var informant_interview_race_node = get_metadata_node(metadata, "informant_interviews/race");
            if(informant_interview_race_node.path_reference != null)
            {
                informant_interview_race_node.values = this.lookup[informant_interview_race_node.path_reference];
            }
            foreach(var item in informant_interview_race_node.values)
            {
                informant_interview_race_display_to_value.Add(item.display, item.value);
                informant_interview_race_value_to_display.Add(item.value, item.display);
            }

            multiform_value_result = gs.get_multiform_value(case_item, "informant_interviews/race");
            if(!multiform_value_result.is_error)
            {
                var multiform_value_list = multiform_value_result.result;
                foreach(var form_item in multiform_value_list)
                {
                    var test_value_object = form_item.Item2;
                    if(test_value_object == null)
                    {
                        change_list.Add((form_item.Item1, "9999"));	
                        //change_count += 1;
                    }
                    else if(test_value_object is IList<object>)
                    {
                        var test_value_list = test_value_object as IList<object>;
                        if(test_value_list.Count > 0)
                        {
                            foreach(var list_item in test_value_list)
                            {
                                var test_value_string = "";
                                if(list_item != null)
                                {
                                    test_value_string = list_item.ToString().Trim();
                                }

                                if(string.IsNullOrWhiteSpace(test_value_string))
                                {
                                    change_list.Add((form_item.Item1, "9999"));	
                                    break;
                                }
                                if(informant_interview_race_display_to_value.ContainsKey(test_value_string))
                                {
                                    change_list.Add((form_item.Item1, informant_interview_race_display_to_value[test_value_string]));
                                    //change_count += 1;
                                    break;
                                }
                                else if(informant_interview_race_value_to_display.ContainsKey(test_value_string))
                                {
                                    change_list.Add((form_item.Item1, test_value_string));
                                    //change_count += 1;
                                    break;
                                }
                                else if(test_value_string.Trim().ToLower() == "Hawaiian / Pacific Islander".ToLower())
                                {
                                    var pacific_islander_item_value = "3";
                                    change_list.Add((form_item.Item1, pacific_islander_item_value));
                                    //change_count += 1;
                                    break;
                                }
                                else
                                {
                                    System.Console.WriteLine("Manual Migration.cs informant interview bad mapping:"  + test_value_string);
                                
                                    var summary_key = "informant_interviews/race";
                                    if(!this.summary_value_dictionary.ContainsKey(summary_key))
                                    {
                                        this.summary_value_dictionary.Add(summary_key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                                    }
                                    if(!this.summary_value_dictionary[summary_key].Contains(test_value_string))
                                    {
                                        this.summary_value_dictionary[summary_key].Add(test_value_string);
                                    }
                                    this.output_builder.AppendLine($"item not found record_id: {mmria_id} path:{summary_key} item: {test_value_string}");
                                }
                            }
                        }
                        else
                        {
                            change_list.Add((form_item.Item1, "9999"));	
                            //change_count += 1;
                            
                        }
                    }
                    else if(string.IsNullOrWhiteSpace(test_value_object.ToString()))
                    {
                        change_list.Add((form_item.Item1, "9999"));	
                        //change_count += 1;
                    }
                    else if(informant_interview_race_display_to_value.ContainsKey(test_value_object.ToString()))
                    {
                        change_list.Add((form_item.Item1, informant_interview_race_display_to_value[test_value_object.ToString()]));	
                    }
                    else if(!informant_interview_race_value_to_display.ContainsKey(test_value_object.ToString()))
                    {
                        var value_string = test_value_object.ToString();

                        var summary_key = "informant_interviews/race";
                        if(!this.summary_value_dictionary.ContainsKey(summary_key))
                        {
                            this.summary_value_dictionary.Add(summary_key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                        }
                        if(!this.summary_value_dictionary[summary_key].Contains(value_string))
                        {
                            this.summary_value_dictionary[summary_key].Add(value_string);
                        }
                        this.output_builder.AppendLine($"item not found record_id: {mmria_id} path:{summary_key} item: {value_string}");
                    
                
                    }
                    
                }
            }
            else
            {
                System.Console.WriteLine("here");
            }

            if(change_list.Count > 0)
            {
                if(change_count == 0)
                {
                    case_has_changed = gs.set_multiform_value(case_item, "informant_interviews/race", change_list);
                    change_count+= 1;
                }
                else
                {
                    case_has_changed = case_has_changed && gs.set_multiform_value(case_item, "informant_interviews/race", change_list);
                }
            }
            change_list.Clear();


// **** informant_interview - end

// ***** evidence_of_social_or_emotional_stress start
            var evidence_of_social_or_emotional_stress_display_to_value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var evidence_of_social_or_emotional_stress_value_to_display = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var evidence_of_social_or_emotional_stress_node = get_metadata_node(metadata, "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress");
            if(evidence_of_social_or_emotional_stress_node.path_reference != null)
            {
                evidence_of_social_or_emotional_stress_node.values = this.lookup[evidence_of_social_or_emotional_stress_node.path_reference];
            }
            foreach(var item in evidence_of_social_or_emotional_stress_node.values)
            {
                evidence_of_social_or_emotional_stress_display_to_value.Add(item.display, item.value);
                evidence_of_social_or_emotional_stress_value_to_display.Add(item.value, item.display);
            }

            value_result = gs.get_value(case_item, "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress");
            var value_dynamic = value_result.result;
            if(value_result.is_error)
            {
                // do nothing
            }
            else if(value_dynamic == null)
            {
                var new_data = new List<int>() { 9999 };

                if(change_count == 0)
                {
                    case_has_changed = gs.set_multi_value( "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress", new_data, case_item);
                    change_count+= 1;
                }
                else
                {
                    case_has_changed = case_has_changed && gs.set_multi_value( "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress", new_data, case_item);
                }

                    //gs.get_value(case_item, "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress");
            }
            else if(value_dynamic is IList<object>)
            {
                var value_list = value_dynamic as IList<object>;

                if(value_list.Count == 0)
                {
                    var new_data = new List<int>() { 9999 };

                    if(change_count == 0)
                    {
                        case_has_changed = gs.set_multi_value( "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress", new_data, case_item);
                        change_count+= 1;
                    }
                    else
                    {
                        case_has_changed = case_has_changed && gs.set_multi_value( "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress", new_data, case_item);
                    }
                }
            }
            else
            {
                var new_data = new List<int>();
                var value_string = value_dynamic.ToString();

                if(evidence_of_social_or_emotional_stress_value_to_display.ContainsKey(value_string))
                {
                    new_data.Add(int.Parse(value_string));
                    if(change_count == 0)
                    {
                        case_has_changed = gs.set_multi_value( "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress", new_data, case_item);
                        change_count+= 1;
                    }
                    else
                    {
                        case_has_changed = case_has_changed && gs.set_multi_value( "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress", new_data, case_item);
                    }
                }
                else
                {

                }

            }

// ***** evidence_of_social_or_emotional_stress end

/*
                multiform_value_list = gs.get_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was");
                foreach(var value in multiform_value_list)
                {
                    //bool try_bool;
                    if(value.Item2 != null && !string.IsNullOrWhiteSpace(value.Item2.ToString()) && value.Item2.ToString().Trim().ToLower() == "Artificial".ToLower())
                    {
                        change_list.Add((value.Item1, "1"));
                    }
                    else
                    {
                        //System.Console.WriteLine($"value.item2: {value.Item2}");
                    }

                }

                if(change_list.Count > 0)
                {
                    if(change_count == 0)
                    {
                        case_has_changed = gs.set_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was", change_list);
                        change_count+= 1;
                    }
                    else
                    {
                        case_has_changed = case_has_changed && gs.set_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was", change_list);
                    }
                }
                change_list.Clear();
                */
/*
                multiform_value_list = gs.get_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was");
                foreach(var value in multiform_value_list)
                {
                    //bool try_bool;
                    if(value.Item2 != null && !string.IsNullOrWhiteSpace(value.Item2.ToString()) && value.Item2.ToString().Trim().ToLower() == "spontaneous".ToLower())
                    {
                        change_list.Add((value.Item1, "0"));
                    }
                    else
                    {
                        //System.Console.WriteLine($"value.item2: {value.Item2}");
                    }

                }

                if(change_list.Count > 0)
                {
                    if(change_count == 0)
                    {
                        case_has_changed = gs.set_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was", change_list);
                        change_count+= 1;
                    }
                    else
                    {
                        case_has_changed = case_has_changed && gs.set_multiform_value(case_item, "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was", change_list);
                    }
                }
                change_list.Clear();*/


                if(!is_report_only_mode && case_has_changed)
                {
                    new SaveRecord(this.db_server_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(case_item, "Manual-Migration");
                }

/**/


            }

            // Map based - end


            Console.WriteLine($"Process Manual Migration End {System.DateTime.Now}");
        
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Process Manual Migration exception {System.DateTime.Now}\n{ex}");
        }

        TimeSpan time_span = System.DateTime.Now - begin_time;
        Console.WriteLine($"Process Manual Migration duration total seconds: {time_span.TotalSeconds} total_minutes{time_span.TotalMinutes}\n");
        
    }
/*
    private bool sav_e_case_del(System.Dynamic.ExpandoObject item)
    {
        bool result = false;

        var gsv = new C_Get_Set_Value(this.output_builder);
        gsv.set_value("date_last_updated", DateTime.UtcNow.ToString("o"), item);
        gsv.set_value("last_updated_by", "migration_plan", item);

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;

        var changed_object_string = Newtonsoft.Json.JsonConvert.SerializeObject(item, settings);

        IDictionary<string, object> doc = item as IDictionary<string, object>;

        string put_url = $"{db_server_url}/{db_name}/{doc["_id"]}";
        cURL document_curl = new cURL ("PUT", null, put_url, changed_object_string, config_timer_user_name, config_timer_value);

        try
        {
            string responseFromServer = document_curl.execute();
            var	put_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
        }
        catch(Exception ex)
        {
            //Console.Write("auth_session_token: {0}", auth_session_token);
            Console.WriteLine(ex);
        }
    

        return result;
    }
    */

    private (bool, bool)  execute_multiform_changes_for(System.Dynamic.ExpandoObject p_object, string p_path, HashSet<string> p_value_set)

    {
        var result = false;
        var all_changes_good = false;

        var gs = new C_Get_Set_Value(this.output_builder);
        C_Get_Set_Value.get_multiform_value_result multiform_value_result = gs.get_multiform_value(p_object, p_path);
        var grid_value = multiform_value_result.result;

        var list_change_set  = new List<(int, object)>();
        var list_other_change_set  = new List<(int, object)>();

        foreach(var child in grid_value)
        {

            var list_value = child.Item2;

            if(!p_value_set.Contains(list_value))
            {
                System.Console.WriteLine($"{child.Item1}: {list_value}");
                list_change_set.Add((child.Item1, "Other"));
                list_other_change_set.Add((child.Item1, list_value));
            }
            
        }

        
        if(list_change_set.Count > 0 || list_other_change_set.Count > 0)
        {
            result = true;
        
            all_changes_good = gs.set_multiform_value(p_object,p_path, list_change_set);
            all_changes_good = all_changes_good && gs.set_multiform_value(p_object,$"{p_path}_other", list_other_change_set);
        }
        return (result, all_changes_good);

    }


    private (bool, bool) execute_grid_changes_for(System.Dynamic.ExpandoObject p_object, string p_path, HashSet<string> p_value_set)

    {
        var result = false;
        var all_changes_good = false;

        var gs = new C_Get_Set_Value(this.output_builder);
        C_Get_Set_Value.get_grid_value_result grid_value_result = gs.get_grid_value(p_object, p_path);
        var grid_value = grid_value_result.result;

        var list_change_set  = new List<(int, object)>();
        var list_other_change_set  = new List<(int, object)>();

        foreach(var child in grid_value)
        {

            var list_value = child.Item2;

            if(!p_value_set.Contains(list_value))
            {
                System.Console.WriteLine($"{child.Item1}: {list_value}");
                list_change_set.Add((child.Item1, "Other"));
                list_other_change_set.Add((child.Item1, list_value));
            }
            
        }

        
        if(list_change_set.Count > 0 || list_other_change_set.Count > 0)
        {
            result = true;
        

            all_changes_good = gs.set_grid_value(p_object,p_path, list_change_set);
            all_changes_good = all_changes_good && gs.set_grid_value(p_object,$"{p_path}_other", list_other_change_set);
        }

        return (result, all_changes_good);

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
    }*/
    
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


    private async Task<Result_Struct> get_matching_cases_for(string p_selector, string p_find_value)
    {

        Result_Struct result = new Result_Struct();

        try
        {

            var selector_struc = new Selector_Struc();
            selector_struc.selector = new System.Collections.Generic.Dictionary<string,System.Collections.Generic.Dictionary<string,string>>(StringComparer.OrdinalIgnoreCase);
            selector_struc.limit = 10000;
            selector_struc.selector.Add(p_selector, new System.Collections.Generic.Dictionary<string,string>(StringComparer.OrdinalIgnoreCase));
            selector_struc.selector[p_selector].Add("$eq", p_find_value);

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string selector_struc_string = Newtonsoft.Json.JsonConvert.SerializeObject (selector_struc, settings);

            System.Console.WriteLine(selector_struc_string);


            string find_url = $"{db_server_url}/{db_name}/_find";

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

}

