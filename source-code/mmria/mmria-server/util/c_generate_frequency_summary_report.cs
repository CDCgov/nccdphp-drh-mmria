using System;
using System.Collections.Generic;
using System.Linq;
using Metadata_Node = mmria.common.metadata.Metadata_Node;

using mmria.getset;

namespace mmria.server.utils;

public sealed class c_generate_frequency_summary_report
{

/*
	public sealed class Metadata_Node
	{
		public Metadata_Node(){}
		public bool is_multiform { get; set; }
		public bool is_grid { get; set; }

		public string path {get;set;}

		public string sass_export_name {get;set;}
		public mmria.common.metadata.node Node { get; set; }

        public string[] tags {get;set;}

		public Dictionary<string,string> display_to_value { get; set; }
		public Dictionary<string,string> value_to_display { get; set; }
	}
    */

    class SetDetailContext
    {
        public SetDetailContext() {}

        public System.Dynamic.ExpandoObject source_object { get; set; }
        public mmria.server.model.SummaryReport.FrequencySummaryDocument FrequencySummaryDocument { get; set; }
    }

/*


FREQ (N)

STAT_N (N,MIN,MEAN,MEDIAN,SD)

STAT_D (N,MIN,MAX)


FREQ STAT_N STAT_D

FREQ

List Values

value   display     N (Counts)
9999    (blank)     1
1       Yes         2
0       No          3
N (Total Count)


2	FREQ	N (Total Count)
3	STAT_N	N (Total Count), Missing Count, Min, Max, Mean, Standard Deviation, Median, Mode
4	STAT_D	N (Total Count), Missing Count, Min, Max




FREQ
app/home_record/how_was_this_death_identified
app/home_record/case_status/overall_case_status
app/home_record/overall_assessment_of_timing_of_death/abstrator_assigned_status
app/home_record/overall_assessment_of_timing_of_death/hr_prg_outcome
er_visit_and_hospital_medical_records/physical_exam_and_evaluations/body_system
er_visit_and_hospital_medical_records/onset_of_labor/final_delivery_route
er_visit_and_hospital_medical_records/onset_of_labor/pregnancy_outcome
er_visit_and_hospital_medical_records/were_there_complications_of_anesthesia
er_visit_and_hospital_medical_records/any_adverse_reactions
er_visit_and_hospital_medical_records/any_surgical_procedures
er_visit_and_hospital_medical_records/any_blood_transfusions
prenatal/pregnancy_history/details_grid/outcome
prenatal/pregnancy_history/details_grid/is_now_living
prenatal/routine_monitoring/urine_protein
prenatal/routine_monitoring/urine_ketones
prenatal/routine_monitoring/urine_glucose

STAT_N
app/home_record/overall_assessment_of_timing_of_death/number_of_days_after_end_of_pregnancey
er_visit_and_hospital_medical_records/highest_bp/systolic_bp
er_visit_and_hospital_medical_records/highest_bp/diastolic_bp
er_visit_and_hospital_medical_records/vital_signs/pulse
er_visit_and_hospital_medical_records/vital_signs/bp_systolic
er_visit_and_hospital_medical_records/vital_signs/bp_diastolic
er_visit_and_hospital_medical_records/vital_signs/respiration
er_visit_and_hospital_medical_records/vital_signs/temperature
er_visit_and_hospital_medical_records/vital_signs/oxygen_saturation
er_visit_and_hospital_medical_records/maternal_biometrics/height/bmi
er_visit_and_hospital_medical_records/maternal_biometrics/admission_weight
prenatal/pregnancy_history/details_grid/gestational_age
prenatal/routine_monitoring/gestational_age_weeks
prenatal/routine_monitoring/systolic_bp
prenatal/routine_monitoring/diastolic
prenatal/routine_monitoring/heart_rate
prenatal/routine_monitoring/oxygen_saturation
prenatal/routine_monitoring/blood_hematocrit
prenatal/routine_monitoring/weight

STAT_D
birth_certificate_infant_fetal_section/record_identification/date_of_delivery
app/home_record/case_status/abstraction_begin_date
app/home_record/case_status/abstraction_complete_date
app/home_record/case_status/committee_review_date
app/home_record/case_status/case_locked_date
er_visit_and_hospital_medical_records/vital_signs/date_and_time
prenatal/pregnancy_history/details_grid/date_ended
prenatal/routine_monitoring/date_and_time







*/

    string source_json;
    string metadata_version;

    mmria.common.couchdb.DBConfigurationDetail db_config = null;

    string data_type = "frequency_summary";

    Dictionary<string,mmria.common.metadata.value_node[]> lookup;

    private System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> List_Look_Up;
	
	List<Metadata_Node> all_list_set;

	List<Metadata_Node> single_form_value_set;
	List<Metadata_Node> single_form_multi_value_set;
	List<Metadata_Node> single_form_grid_value_set;
	List<Metadata_Node> single_form_grid_multi_value_list_set;
	List<Metadata_Node> multiform_value_set;
	List<Metadata_Node> multiform_multi_value_set;
	List<Metadata_Node> multiform_grid_value_set;

	List<Metadata_Node> multiform_grid_multi_value_set;

    private int blank_value = 9999;

    

    public c_generate_frequency_summary_report 
    (
        string p_source_json, 
        string p_type,
        string p_metadata_version,
        mmria.common.couchdb.DBConfigurationDetail _db_config
    )
    {

        source_json = p_source_json;
        this.data_type = p_type;
        metadata_version = p_metadata_version;
        db_config = _db_config;
    }

    public string execute ()
    {
        string result = null;

        var gs = new migrate.C_Get_Set_Value(new ());
        
        string metadata_url = db_config.url + $"/metadata/version_specification-{metadata_version}/metadata";
        cURL metadata_curl = new cURL("GET", null, metadata_url, null, db_config.user_name, db_config.user_value);
        mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_curl.execute());

		System.Dynamic.ExpandoObject source_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (source_json);




        List_Look_Up = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach(var child in metadata.children)
        {
            Get_List_Look_Up(List_Look_Up, metadata.lookup, child, "/" + child.name);
        }

        this.lookup = get_look_up(metadata);

		all_list_set = get_metadata_node_by_type(metadata, "list");


        var with_tags = all_list_set.Where
        (
            o=> o.tags != null && 
            o.tags.Length > 0 && 
            (
                o.tags.Contains("FREQ", StringComparer.OrdinalIgnoreCase) || 
                o.tags.Contains("STAT_N", StringComparer.OrdinalIgnoreCase) ||
                o.tags.Contains("STAT_D", StringComparer.OrdinalIgnoreCase) ||
                o.tags.Contains("CALC_DATE", StringComparer.OrdinalIgnoreCase) 
                
            )
        ).ToList();

		single_form_value_set = with_tags.Where(o=> o.is_multiform == false && o.is_grid == false && o.Node.is_multiselect == null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();
		single_form_multi_value_set = with_tags.Where(o=> o.is_multiform == false && o.is_grid == false && o.Node.is_multiselect != null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();

		single_form_grid_value_set = with_tags.Where(o=> o.is_multiform == false && o.is_grid == true && o.Node.is_multiselect == null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();
		single_form_grid_multi_value_list_set = with_tags.Where(o=> o.is_multiform == false && o.is_grid == true && o.Node.is_multiselect != null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();

		multiform_value_set = with_tags.Where(o=> o.is_multiform == true && o.is_grid == false && o.Node.is_multiselect == null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();
		multiform_multi_value_set = with_tags.Where(o=> o.is_multiform == true && o.is_grid == false && o.Node.is_multiselect != null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();

		multiform_grid_value_set = with_tags.Where(o=> o.is_multiform == true && o.is_grid == true && o.Node.is_multiselect == null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();
		multiform_grid_multi_value_set = with_tags.Where(o=> o.is_multiform == true && o.is_grid == true && o.Node.is_multiselect != null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();


		var total_count = single_form_value_set.Count + single_form_grid_value_set.Count + multiform_value_set.Count + multiform_grid_value_set.Count + single_form_multi_value_set.Count + single_form_grid_multi_value_list_set.Count + multiform_multi_value_set.Count + multiform_grid_multi_value_set.Count;
		//System.Console.WriteLine($"all_list_set.Count: {all_list_set.Count} total_count: {total_count}");
		//System.Console.WriteLine($"is count the same: {all_list_set.Count == single_form_value_set.Count + single_form_grid_value_set.Count + multiform_value_set.Count + multiform_grid_value_set.Count + single_form_multi_value_set.Count + single_form_grid_multi_value_list_set.Count + multiform_multi_value_set.Count + multiform_grid_multi_value_set.Count}");


        //migrate.C_Get_Set_Value.get_grid_value_result grid_value_result = null;
        migrate.C_Get_Set_Value.get_value_result value_result = null;

        var FrequencySummaryDocument = new mmria.server.model.SummaryReport.FrequencySummaryDocument();


        value_result = gs.get_value(source_object, "_id");
    
        FrequencySummaryDocument._id  = ((object)value_result.result).ToString();

        #if !IS_PMSS_ENHANCED
        value_result = gs.get_value(source_object, "home_record/jurisdiction_id");
        FrequencySummaryDocument.case_folder = ((object)value_result.result).ToString();

        value_result = gs.get_value(source_object, "home_record/record_id");
        FrequencySummaryDocument.record_id = value_result.result != null? ((object)value_result.result).ToString() : ""; //'OR-2019-4806',
        #endif

        #if IS_PMSS_ENHANCED
        value_result = gs.get_value(source_object, "tracking/admin_info/jurisdiction");
        FrequencySummaryDocument.case_folder = ((object)value_result.result).ToString();

        value_result = gs.get_value(source_object, "tracking/admin_info/pmssno");
        FrequencySummaryDocument.record_id = value_result.result != null? ((object)value_result.result).ToString() : ""; //'OR-2019-4806',
        #endif
        //FrequencySummaryDocument._id  = value_result.result != null ? ((object)value_result.result).ToString(): "/";

        FrequencySummaryDocument.date_created = DateTime.Now;

        object val = null;

/*

        var id_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "348ffd22-fe32-46d7-977e-92502456a936"
        };


        if(id_set.Contains(FrequencySummaryDocument._id))
        {
            System.Console.WriteLine("here");
        }*/
        
        // host_state  *** begin
        try
        {
            var host_state = "";
            value_result = gs.get_value(source_object, "host_state");
            var test_host_state_object = value_result.result;
            if
            (
                test_host_state_object != null
            )
            {
                host_state = test_host_state_object.ToString();
            }
            FrequencySummaryDocument.host_state = host_state;
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }
        
        // host_state  *** end
        
        #if !IS_PMSS_ENHANCED
        try
        {
         
            val = gs.get_value(source_object,  "home_record/case_status/overall_case_status").result;
            if(val != null && val.ToString() != "")
            {
                FrequencySummaryDocument.case_status  = System.Convert.ToInt32(val);
            }

            val = gs.get_value(source_object, "home_record/date_of_death/year").result;
            if(val != null && val.ToString() != "")
            {
                FrequencySummaryDocument.year_of_death = System.Convert.ToInt32(val);
            }

            val = gs.get_value(source_object, "home_record/date_of_death/month").result;
            if(val != null && val.ToString() != "")
            {
                FrequencySummaryDocument.month_of_death = System.Convert.ToInt32(val);
            }

            val = gs.get_value(source_object, "home_record/date_of_death/day").result;
            if(val != null && val.ToString() != "")
            {
                FrequencySummaryDocument.day_of_death = System.Convert.ToInt32(val);
            }
            
        
        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }

                try
        {
            val = gs.get_value(source_object, "committee_review/date_of_review").result;
            if
            (
                val != null && 
                val.ToString() != ""
            )
            {
                FrequencySummaryDocument.day_of_case_review = System.Convert.ToDateTime(val).Day;
                FrequencySummaryDocument.year_of_case_review = System.Convert.ToDateTime(val).Year;
                FrequencySummaryDocument.month_of_case_review = System.Convert.ToDateTime(val).Month;
            }
        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }

        try
        {

            value_result = gs.get_value(source_object, "committee_review/pregnancy_relatedness");
            if
            (
                ! value_result.is_error &&
                value_result.result != null
            )
            {
                val = value_result.result.ToString();

                if(int.TryParse(val.ToString(), out var test_int))
                {
                    FrequencySummaryDocument.pregnancy_relatedness = test_int;
                }


            }

        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }
        #endif

        #if IS_PMSS_ENHANCED
        try
        {
         

            val = gs.get_value(source_object,  "tracking/admin_info/status").result;
            if(val != null && val.ToString() != "")
            {
                FrequencySummaryDocument.case_status  = System.Convert.ToInt32(val);
            }

            val = gs.get_value(source_object, "tracking/admin_info/track_year").result;
            if(val != null && val.ToString() != "")
            {
                FrequencySummaryDocument.year_of_death = System.Convert.ToInt32(val);
            }

            val = gs.get_value(source_object, "tracking/date_of_death/month").result;
            if(val != null && val.ToString() != "")
            {
                FrequencySummaryDocument.month_of_death = System.Convert.ToInt32(val);
            }

            val = gs.get_value(source_object, "tracking/date_of_death/day").result;
            if(val != null && val.ToString() != "")
            {
                FrequencySummaryDocument.day_of_death = System.Convert.ToInt32(val);
            }
        
        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }

        try
        {
            val = gs.get_value(source_object, "preparer_remarks/preparer_grp/review_1_on").result;
            if
            (
                val != null && 
                val.ToString() != ""
            )
            {
                FrequencySummaryDocument.day_of_case_review = System.Convert.ToDateTime(val).Day;
                FrequencySummaryDocument.year_of_case_review = System.Convert.ToDateTime(val).Year;
                FrequencySummaryDocument.month_of_case_review = System.Convert.ToDateTime(val).Month;
            }
        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }

        try
        {

            value_result = gs.get_value(source_object, "cause_of_death/class");
            if
            (
                ! value_result.is_error &&
                value_result.result != null
            )
            {
                val = value_result.result.ToString();

                if(int.TryParse(val.ToString(), out var test_int))
                {
                    FrequencySummaryDocument.pregnancy_relatedness = test_int;
                }


            }

        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }
        #endif


        var SetDetailContext = new SetDetailContext()
        {
            source_object = source_object,
            FrequencySummaryDocument = FrequencySummaryDocument
        };

        process(metadata, SetDetailContext);

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        //settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        result = Newtonsoft.Json.JsonConvert.SerializeObject(FrequencySummaryDocument, settings);

        return result;
    }

    private void Get_List_Look_Up
    (
        System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> p_result,
        mmria.common.metadata.node[] p_lookup,
        mmria.common.metadata.node p_metadata,
        string p_path
    )
    {
        switch (p_metadata.type.ToLower())
        {
            case "form":
            case "group":
            case "grid":
            foreach (mmria.common.metadata.node node in p_metadata.children)
            {
                Get_List_Look_Up(p_result, p_lookup, node, p_path + "/" + node.name.ToLower());
            }
            break;
            case "list":
            if
            (
                p_metadata.control_style != null &&
                p_metadata.control_style.ToLower() == "editable"
            )
            {
                break;
            }

            p_result.Add(p_path, new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

            var value_node_list = p_metadata.values;
            if
            (
                !string.IsNullOrWhiteSpace(p_metadata.path_reference)
            )
            {
                var name = p_metadata.path_reference.Replace("lookup/", "");
                foreach (var item in p_lookup)
                {
                if (item.name.ToLower() == name.ToLower())
                {
                    value_node_list = item.values;
                    break;
                }
                }
            }

            foreach (var value in value_node_list)
            {
                p_result[p_path].Add(value.value, value.display);
            }

            break;
            default:
            break;
        }
    }
	private List<Metadata_Node> process(mmria.common.metadata.app p_metadata, SetDetailContext Context)
	{
		var result = new List<Metadata_Node>();
		foreach(var node in p_metadata.children)
		{

            if(node.type.ToLower() == "form")
			{
				if
				(
					node.cardinality == "+" ||
					node.cardinality == "*"
				)
				{
					process(node, true, false, node.name, Context);
				}
				else
				{
					process(node, false, false, node.name, Context);
				}
			}
		}
		return result;
	}

    private void process(mmria.common.metadata.node p_node, bool p_is_multiform, bool p_is_grid, string p_path, SetDetailContext Context)
	{
        var path = $"{p_path}/{p_node.name}";

        var gs = new migrate.C_Get_Set_Value(new ());

        int? Get_List_Number_Value(string _path)
        {
            int? result = null;
            var value_result = gs.get_value(Context.source_object, _path);
            if(!value_result.is_error)
            {
                if(value_result.result != null)
                { 
                    if(int.TryParse(value_result.result.ToString(), out var temp))
                    {
                        result = temp;
                    }
                    
                }
            }

            return result;
        }

        switch(p_node.type.ToLower())
        {
            /*case "group":
                if
                (
                    p_node.tags != null &&
                    p_node.tags.Contains("CALC_DATE") 
                )
                {

                }
                else for(var i = 0; i < p_node.children.Count(); i++)
                {
                    var item = p_node.children[i];
                    process(item,  p_is_multiform, p_is_grid, path, Context);
                    
                }
                break;*/
            case "grid":
                for(var i = 0; i < p_node.children.Count(); i++)
                {
                    var item = p_node.children[i];
                    process(item,  p_is_multiform, true, path, Context);
                    
                }
                break;

            case "form":
                for(var i = 0; i < p_node.children.Count(); i++)
                {
                    var item = p_node.children[i];
                    process(item,  p_is_multiform, p_is_grid, p_path, Context);
                    
                }
                break;

            case "chart":
            case "label":
            case "button":
            case "always_enabled_button":
                break;


            case "group":


                try
                {
                    if(p_node.tags.Contains("CALC_DATE"))
                    {



                        var year = Get_List_Number_Value($"{path}/year");
                        var month = Get_List_Number_Value($"{path}/month");
                        var day = Get_List_Number_Value($"{path}/day");

                        var item = new mmria.server.model.SummaryReport.Detail();

                        if
                        (
                            !year.HasValue ||
                            !month.HasValue ||
                            !day.HasValue ||
                            
                            year.Value == 9999 ||
                            month.Value == 9999 ||
                            day.Value == 9999 
                                    
                                
                        )
                        {
                            item.value = "(-)";
                            item.count = 1;
                        }
                        else
                        {
                            try
                            {
                                var date_only = new DateOnly(year.Value, month.Value, day.Value);
                                item.value = $"{date_only}";
                                item.count = 1;
                            }
                            catch(Exception ex)
                            {
                                System.Console.WriteLine($"Calc_Date setting DateOnly error \n{ex}");
                            }

                        }


                        
                        if(!Context.FrequencySummaryDocument.path_to_detail.ContainsKey(path))
                        {
                            Context.FrequencySummaryDocument.path_to_detail.Add(path, new());
                        }
                        Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                    }

                
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine($"Calc_Date on Group Field error \n{ex}");
                }

                for(var i = 0; i < p_node.children.Count(); i++)
                {
                    var item = p_node.children[i];
                    process(item,  p_is_multiform, p_is_grid, path, Context);
                    
                }
                break;
            default:
                
                
                if(p_node.tags.Length == 0) break;
                
                if
                (
                    !p_node.tags.Contains("FREQ") &&

                    !p_node.tags.Contains("STAT_N") &&
 
                    !p_node.tags.Contains("STAT_D") &&

                    !p_node.tags.Contains("CALC_DATE")
                )
                {
                    break;
                }


                var BLANK_OR_MISSING = "(-)";

                if(p_node.type.Trim().Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    BLANK_OR_MISSING = "9999";
                }
               
/*
                if
                (
                    //path == "home_record/how_was_this_death_identified" ||
                    //path == "birth_fetal_death_certificate_parent/race/race_of_mother" ||
                    //path == "birth_certificate_infant_fetal_section/record_type" ||
                    path == "home_record/overall_assessment_of_timing_of_death/abstrator_assigned_status"
                )
                {
                    Console.WriteLine("here");
                }
*/

                List<mmria.server.model.SummaryReport.Detail> set_single_value_detail()
                {

                    var result = new List<mmria.server.model.SummaryReport.Detail>();
                    Context.FrequencySummaryDocument.path_to_detail.Add(path, result);

                    var value_result = gs.get_value(Context.source_object, path);
                    if(value_result.is_error)
                    {
                        var item = new mmria.server.model.SummaryReport.Detail();
                        item.value = BLANK_OR_MISSING;
                        item.count = 1;
                        Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                    }
                    else 
                    {
                        if(value_result.result != null)
                        { 
                            if(value_result.result is IList<object> object_list)
                            {
                                if(object_list.Count == 0)
                                {
                                    var item = new mmria.server.model.SummaryReport.Detail();
                                    item.value = BLANK_OR_MISSING;
                                    item.count = 1;

                                    Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                }
                                else foreach(var i in object_list)
                                {
                                    if
                                    (
                                        i != null &&
                                        i is not IList<object> && 
                                        !string.IsNullOrWhiteSpace(i.ToString())
                                    )
                                    {
                                        var item = new mmria.server.model.SummaryReport.Detail();
                                        item.value = i.ToString();
                                        item.count = 1;

                                        Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                    }
                                    else
                                    {
                                        var item = new mmria.server.model.SummaryReport.Detail();
                                        item.value = BLANK_OR_MISSING;
                                        item.count = 1;

                                        Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                    }
                                }
                                
                                
                            }
                            else
                            {
                                var item = new mmria.server.model.SummaryReport.Detail();
                                if
                                (
                                    string.IsNullOrWhiteSpace(value_result.result.ToString()))
                                {
                                    item.value = BLANK_OR_MISSING;
                                }
                                else
                                {
                                    item.value = value_result.result.ToString();
                                }
                                
                                
                                item.count = 1;
                                
                                Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                            }
                        }
                        else
                        {
                            var item = new mmria.server.model.SummaryReport.Detail();
                            item.value = BLANK_OR_MISSING;
                            item.count = 1;
                            Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                        }
                    }

                    return result;
                }


                List<mmria.server.model.SummaryReport.Detail> set_single_grid_value_detail()
                {

                    var result = new List<mmria.server.model.SummaryReport.Detail>();
                    Context.FrequencySummaryDocument.path_to_detail.Add(path, result);

                    var is_grid_exist_result = gs.get_value(Context.source_object, p_path);
                    if(is_grid_exist_result.is_error)
                    {
                        return result;
                    }

                    if(is_grid_exist_result.result == null)
                    {
                        return result;
                    }

                    if(is_grid_exist_result.result is IList<object> is_grid_exist_object_list)
                    {
                        if(is_grid_exist_object_list.Count == 0)
                        {
                            return result;
                        }
                    }






                    




                    var value_result = gs.get_grid_value(Context.source_object, path);


                    if(value_result.is_error)
                    {

                    }
                    else 
                    {
                        if(value_result.result != null)
                        { 
                            if(value_result.result is IList<(int, object)> object_list)
                            {

                                if(object_list.Count == 0)
                                {
                                    /*
                                    var item = new mmria.server.model.SummaryReport.Detail();
                                    item.value = BLANK_OR_MISSING;
                                    item.count = 1;
                                    Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                    */

                                }
                                else foreach(var (index, i) in object_list)
                                {
                                    if
                                    (
                                        i != null &&
                                        !string.IsNullOrWhiteSpace(i.ToString())
                                    )
                                    {
                                        
                                        var item = new mmria.server.model.SummaryReport.Detail();
                                        item.value = i.ToString();
                                        item.count = 1;

                                        Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                    }
                                    else
                                    {
                                        var item = new mmria.server.model.SummaryReport.Detail();
                                        item.value = BLANK_OR_MISSING;
                                        item.count = 1;
                                        Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                    }
                                }
                                
                                
                            }
                            else
                            {
                                var item = new mmria.server.model.SummaryReport.Detail();
                                if(string.IsNullOrWhiteSpace(value_result.result.ToString()))
                                {
                                    item.value = BLANK_OR_MISSING;
                                }
                                else
                                {
                                    item.value = value_result.result.ToString();
                                }
                                item.count = 1;
                                
                                Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                            }
                        }
                        else
                        {
                            var item = new mmria.server.model.SummaryReport.Detail();
                            item.value = BLANK_OR_MISSING;
                            item.count = 1;
                            Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                        }
                    }

                    return result;
                }


                List<mmria.server.model.SummaryReport.Detail> set_multi_form_value_detail()
                {
                    var result = new List<mmria.server.model.SummaryReport.Detail>();
                    Context.FrequencySummaryDocument.path_to_detail.Add(path, result);

                    var form_path = path.Split("/")[0];
                    var is_form_exist_result = gs.get_value(Context.source_object, form_path);
                    if(is_form_exist_result.is_error)
                    {
                        return result;
                    }

                    if(is_form_exist_result.result == null)
                    {
                        return result;
                    }

                    if(is_form_exist_result.result is IList<object> is_form_exist_object_list)
                    {
                        if(is_form_exist_object_list.Count == 0)
                        {
                            return result;
                        }
                    }

                    
                    

                    var value_result = gs.get_multiform_value(Context.source_object, path);
                    if(value_result.is_error)
                    {

                    }
                    else 
                    {
                        if(value_result.result != null)
                        { 
                            if(value_result.result is IList<(int, object)> object_list)
                            {

                                if(object_list.Count == 0)
                                {
                                    var item = new mmria.server.model.SummaryReport.Detail();
                                    item.value = "9999";
                                    item.count = 1;
                                    Context.FrequencySummaryDocument.path_to_detail[path].Add(item);

                                }
                                else foreach(var (index, i) in object_list)
                                {
                                    if(i is List<object>)
                                    {
                                        var i_list = (List<object>) i;
                                        foreach(var answer_item in i_list)
                                        {
                                            if
                                            (
                                                answer_item != null &&
                                                !string.IsNullOrWhiteSpace(answer_item.ToString())
                                            )
                                            {
                                                
                                                var item = new mmria.server.model.SummaryReport.Detail();
                                                item.value = answer_item.ToString();
                                                item.count = 1;

                                                Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                            }
                                            else
                                            {
                                                var item = new mmria.server.model.SummaryReport.Detail();
                                                item.value = BLANK_OR_MISSING;
                                                item.count = 1;
                                                Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                            }
                                        }
                                    }
                                    else  if
                                    (
                                        i != null &&
                                        !string.IsNullOrWhiteSpace(i.ToString())
                                    )
                                    {
                                        
                                        var item = new mmria.server.model.SummaryReport.Detail();
                                        item.value = i.ToString();
                                        item.count = 1;

                                        Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                    }
                                    else
                                    {
                                        var item = new mmria.server.model.SummaryReport.Detail();
                                        item.value = BLANK_OR_MISSING;
                                        item.count = 1;
                                        Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                    }
                                }
                                
                                
                            }
                            else
                            {
                                var item = new mmria.server.model.SummaryReport.Detail();
                                if(string.IsNullOrWhiteSpace(value_result.result.ToString()))
                                {
                                    item.value = BLANK_OR_MISSING;
                                }
                                else
                                {
                                    item.value = value_result.result.ToString();
                                }
                                
                                item.count = 1;
                                
                                Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                            }
                        }
                        else
                        {
                            var item = new mmria.server.model.SummaryReport.Detail();
                            item.value = BLANK_OR_MISSING;
                            item.count = 1;
                            Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                        }
                    }

                    return result;
                }


                List<mmria.server.model.SummaryReport.Detail> set_multi_form_grid_value_detail()
                {

                    var result = new List<mmria.server.model.SummaryReport.Detail>();
                    Context.FrequencySummaryDocument.path_to_detail.Add(path, result);

                    var path_array = path.Split("/");
                    var form_path = path_array[0];
                    var is_form_exist_result = gs.get_value(Context.source_object, form_path);
                    if(is_form_exist_result.is_error)
                    {
                        return result;
                    }

                    if(is_form_exist_result.result == null)
                    {
                        return result;
                    }

                    if(is_form_exist_result.result is IList<object> is_form_exist_object_list)
                    {
                        if(is_form_exist_object_list.Count == 0)
                        {
                            return result;
                        }
                    }

                    var grid_path = "";

                    for(var i = 0; i < path_array.Length-1; i++)
                    {
                        if(i != 0)
                        {
                            grid_path += "/";
                        }
                        
                        grid_path += path_array[i];
                        
                    }

                    var is_grid_exist_result = gs.get_multiform_value(Context.source_object, grid_path);
                    if(is_grid_exist_result.is_error)
                    {
                        return result;
                    }

                    if(is_grid_exist_result.result == null)
                    {
                        return result;
                    }

                    if(is_grid_exist_result.result is List<(int, object)> is_grid_exist_object_list)
                    {
                        if(is_grid_exist_object_list.Count == 0)
                        {
                            return result;
                        }
                    }


                    

                    var value_result = gs.get_multiform_grid_value(Context.source_object, path);
                    if(value_result.is_error)
                    {

                    }
                    else 
                    {
                        if(value_result.result != null)
                        { 
                            if(value_result.result is IList<(int, int, object)> object_list)
                            {

                                if(object_list.Count == 0)
                                {
                                    /*
                                    var item = new mmria.server.model.SummaryReport.Detail();
                                    item.value = "9999";
                                    item.count = 1;
                                    Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                    */
                                }
                                else foreach(var (x, y, i) in object_list)
                                {
                                    if
                                    (
                                        i != null &&
                                        !string.IsNullOrWhiteSpace(i.ToString())
                                    )
                                    {
                                        
                                        var item = new mmria.server.model.SummaryReport.Detail();
                                        item.value = i.ToString();
                                        item.count = 1;

                                        Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                    }
                                    else
                                    {
                                        var item = new mmria.server.model.SummaryReport.Detail();
                                        item.value = BLANK_OR_MISSING;
                                        item.count = 1;
                                        Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                                    }
                                }
                                
                                
                            }
                            else
                            {
                                var item = new mmria.server.model.SummaryReport.Detail();
                                if(string.IsNullOrWhiteSpace(value_result.result.ToString()))
                                {
                                    item.value = BLANK_OR_MISSING;
                                }
                                else
                                {
                                    item.value = value_result.result.ToString();
                                }
                                
                                item.count = 1;
                                
                                Context.FrequencySummaryDocument.path_to_detail[path].Add(item);
                            }
                        }
                        else
                        {

                        }
                    }

                    return result;
                }


                if
                (
                    p_node.type == "group" && 
                    p_node.tags != null &&
                    p_node.tags.Contains("CALC_DATE")
                )
                {
                    /*var item = new mmria.server.model.SummaryReport.Detail();
                    //Console.WriteLine("Generate Frequency <todo> CALC_DATE Group");
                    var year_value_result = gs.get_multiform_grid_value(Context.source_object, path + "/year");
                    var month_value_result = gs.get_multiform_grid_value(Context.source_object, path + "/month");
                    var day_value_result = gs.get_multiform_grid_value(Context.source_object, path + "/day");


                    if
                    (
                        !year_value_result.is_error &&
                        !year_value_result.result
                    )
                    
                    for(var i = 0; i < p_node.children.Count(); i++)
                    {
                        var item = p_node.children[i];
                        process(item,  p_is_multiform, p_is_grid, p_path, Context);
                        
                    }
                    item.value = "<todo>";
                    item.count = 1;
                                
                    Context.FrequencySummaryDocument.path_to_detail[path].Add(item);*/
                }

                if(p_is_multiform)
                {
                    if(p_is_grid)
                    {
                        set_multi_form_grid_value_detail();
                    }
                    else
                    {
                        set_multi_form_value_detail();
                    }

                }
                else if(p_is_grid)
                {
                    set_single_grid_value_detail();
                }
                else
                {
                    set_single_value_detail();
                }





            break;
        }
	}




	private List<Metadata_Node> get_metadata_node_by_type(mmria.common.metadata.app p_metadata, string p_type)
	{
		var result = new List<Metadata_Node>();
		foreach(var node in p_metadata.children)
		{
			var current_type = node.type.ToLowerInvariant();
			//if(current_type == p_type)
			//{
				result.Add(new Metadata_Node()
				{
					is_multiform = false,
					is_grid = false,
					path = node.name,
					Node = node,
					sass_export_name = node.sass_export_name,
                    tags = node.tags
				});
			//}
			//else 
            if(current_type == "form")
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
            sass_export_name = p_node.sass_export_name,
            tags = p_node.tags
        });
    
        
        if(p_node.children != null)
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

    private Dictionary<string,mmria.common.metadata.value_node[]> get_look_up(mmria.common.metadata.app p_metadata)
	{
		var result = new Dictionary<string,mmria.common.metadata.value_node[]>(StringComparer.OrdinalIgnoreCase);

		foreach(var node in p_metadata.lookup)
		{
			result.Add("lookup/" + node.name, node.values);
		}
		return result;
	}




}


