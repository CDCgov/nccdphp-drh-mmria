using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.server.utils;

public sealed class c_generate_frequency_summary_report
{

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

/*


FREQ (N)

STAT_N (N,MIN,MEAN,MEDIAN,SD)

STAT_D (N,MIN,MAX)


FREQ STAT_N STAT_D


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

    common.couchdb.DBConfigurationDetail connection;
    string metadata_release_version_name;
    

    public c_generate_frequency_summary_report (common.couchdb.DBConfigurationDetail p_connection, string p_metadata_release_version_name, string p_source_json, string p_type = "dqr-detail")
    {

        connection = p_connection;
        metadata_release_version_name = p_metadata_release_version_name;
        source_json = p_source_json;
        this.data_type = p_type;
    }

    public string execute ()
    {
        string result = null;

        var gs = new migrate.C_Get_Set_Value(new ());
        
        string metadata_url = connection.url + $"/metadata/version_specification-{metadata_release_version_name}/metadata";
        var metadata_curl = new mmria.getset.cURL("GET", null, metadata_url, null, connection.user_name, connection.user_value);
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
                o.tags.Contains("STAT_D", StringComparer.OrdinalIgnoreCase) 
                
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

        value_result = gs.get_value(source_object, "home_record/jurisdiction_id");
        FrequencySummaryDocument.case_folder = ((object)value_result.result).ToString();

        value_result = gs.get_value(source_object, "home_record/record_id");
        FrequencySummaryDocument.record_id = value_result.result != null? ((object)value_result.result).ToString() : ""; //'OR-2019-4806',
    
        FrequencySummaryDocument._id  = value_result.result != null ? ((object)value_result.result).ToString(): "/";


        FrequencySummaryDocument.date_created = DateTime.Now;


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


