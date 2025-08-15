using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public sealed class v2_8_1_Migration
{

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


	public v2_8_1_Migration
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
		this.output_builder.AppendLine($"v2.8.1 Data Migration started at: {DateTime.Now.ToString("o")}");
		DateTime begin_time = System.DateTime.Now;
		
		this.output_builder.AppendLine($"v2_8_1_Migration started at: {begin_time.ToString("o")}");
		
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


			var ExistingRecordIds = await GetExistingRecordIds();


			string url = $"{host_db_url}/{db_name}/_all_docs?include_docs=true";
			var case_curl = new cURL("GET", null, url, null, config_timer_user_name, config_timer_value);
			string responseFromServer = await case_curl.executeAsync();
			


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


					void check_and_update_muilti_value(string p_path)
					{

						C_Get_Set_Value.get_multiform_value_result multiform_value_result = null;

						multiform_value_result = gs.get_multiform_value(doc, p_path);

						if
						(
							multiform_value_result.result is not null &&
							multiform_value_result.result is List<(int, object)> result_list && 
							result_list.Count > 0
						)
						{

							var new_list = new List<(int, object)>();
							var has_changed = false;

							foreach(var (index, value) in result_list)
							{
								if(value != null)
								{
									var time_value_string = value.ToString();

									if( !isInNeedOfConversion(time_value_string))
									{

										var new_time = ConvertToStandardTime(time_value_string);


										if(case_change_count == 0)
										{
											case_change_count += 1;
											case_has_changed = true;
										}
										
										new_list.Add((index, new_time));

									}
								}
							}

							if(new_list.Count > 0)
							{

								case_has_changed = case_has_changed && gs.set_multiform_value(doc, p_path, new_list);
								var output_text = $"item record_id: {mmria_id} path:{p_path} set from {string.Join(",",result_list)} => {string.Join(",",new_list)}";
								this.output_builder.AppendLine(output_text);
								Console.WriteLine(output_text);
							}
						}
					}

/*

DC

Time of Death
dcci_to_death
/death_certificate/certificate_identification/time_of_death 

2

DC

Time of Injury
dciai_to_injur
/death_certificate/injury_associated_information/time_of_injury 

3

BCDC-Infant

Time of Delivery
bcifsri_to_deliv
/birth_certificate_infant_fetal_section/record_identification/time_of_delivery

4

ER

Time of Arrival
evahmrbaadidoa_to_arriv
/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/time_of_arrival

5

ER

Time of Admission
evahmrbaadidoha_to_admis
/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/time_of_admission

6

ER

Time of Discharge
evahmrbaadidohd_to_disch
/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/time_of_discharge

7

ER

Time of Onset of Labor
evahmrooldoool_tooo_labor
/er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/time_of_onset_of_labor 

8

ER

Time of Rupture
evahmrooldor_to_ruptu
/er_visit_and_hospital_medical_records/onset_of_labor/date_of_rupture/time_of_rupture

9

OMOV

Arrival Time
omovvdomov_a_time
/other_medical_office_visits/visit/date_of_medical_office_visit/arrival_time

*/


{
var dcci_to_death_path = "death_certificate/certificate_identification/time_of_death";
value_result = gs.get_value(doc, dcci_to_death_path);

	if
	(
		value_result.result is not null &&
		value_result.result is string time_value_string &&
		!string.IsNullOrWhiteSpace(time_value_string)
	)
	{
		if( !isInNeedOfConversion(time_value_string))
		{

			var new_time = ConvertToStandardTime(time_value_string);


			if(case_change_count == 0)
			{
				case_change_count += 1;
				case_has_changed = true;
			}
			
			case_has_changed = case_has_changed && gs.set_value(dcci_to_death_path, new_time, doc);
			var output_text = $"item record_id: {mmria_id} path:{dcci_to_death_path} set from {time_value_string} => {new_time}";
			this.output_builder.AppendLine(output_text);
			Console.WriteLine(output_text);


		}
	}
}



{
	var dciai_to_injur_path = "death_certificate/injury_associated_information/time_of_injury";
	value_result = gs.get_value(doc, dciai_to_injur_path);

	if
	(
		value_result.result is not null &&
		value_result.result is string time_value_string &&
		!string.IsNullOrWhiteSpace(time_value_string)
	)
	{
		if( !isInNeedOfConversion(time_value_string))
		{

			var new_time = ConvertToStandardTime(time_value_string);


			if(case_change_count == 0)
			{
				case_change_count += 1;
				case_has_changed = true;
			}
			
			case_has_changed = case_has_changed && gs.set_value(dciai_to_injur_path, new_time, doc);
			var output_text = $"item record_id: {mmria_id} path:{dciai_to_injur_path} set from {time_value_string} => {new_time}";
			this.output_builder.AppendLine(output_text);
			Console.WriteLine(output_text);


		}
	}
}





			check_and_update_muilti_value("birth_certificate_infant_fetal_section/record_identification/time_of_delivery");
			check_and_update_muilti_value("er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/time_of_arrival");
			check_and_update_muilti_value("er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/time_of_admission");
			check_and_update_muilti_value("er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/time_of_discharge");
			check_and_update_muilti_value("er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/time_of_onset_of_labor ");
			check_and_update_muilti_value("er_visit_and_hospital_medical_records/onset_of_labor/date_of_rupture/time_of_rupture");
			check_and_update_muilti_value("other_medical_office_visits/visit/date_of_medical_office_visit/arrival_time");




				if(!is_report_only_mode && case_has_changed)
				{
					var save_result = await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(doc as IDictionary<string, object>,"v2.8.1 part1");
				}

			}

		
		}
	}
	catch(Exception ex)
	{
		Console.WriteLine(ex);
	}

	Console.WriteLine($"v2_8_1_Migration Finished {DateTime.Now}");
}

	bool isInNeedOfConversion(string p_value)
	{
		var result = true;

		if(p_value != null)
		{
			if
			(
				p_value.Trim().StartsWith("12:") ||
				p_value.Trim().StartsWith("24:")
			)
			{
				result = false;
			}
		}


		return result;
	}



	string ConvertToStandardTime(string p_value)
	{
		var result = p_value;

		if(p_value != null)
		{
			if
			(
				p_value.Trim().StartsWith("12:")
			)
			{
				var data = p_value.Split(":");
				data[0] = "00";
				result =string.Join(':',data);
			}
			else if
			(
				p_value.Trim().StartsWith("24:")
			)
			{
				var data = p_value.Split(":");
				data[0] = "12";
				result =string.Join(':',data);
			}
		}


		return result;
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


	public async Task<HashSet<string>> GetExistingRecordIds()
	{
		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


		try
		{        
			string request_string = $"{host_db_url}/{db_name}/_design/sortable/_view/by_date_created?skip=0&take=25000";

			var case_view_curl = new cURL("GET", null, request_string, null, config_timer_user_name, config_timer_value);
			string responseFromServer = await case_view_curl.executeAsync();

			mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

			foreach(mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
			{
				result.Add(cvi.value.record_id);

			}
		}
		catch(Exception ex) 
		{
			Console.WriteLine (ex);
		}

		return result;
	} 

	private int GenerateRandomFourDigits()
	{
		int _min = 1000;
		int _max = 9999;
		Random _rdm = new Random();
		return _rdm.Next(_min, _max);
	}


    }
