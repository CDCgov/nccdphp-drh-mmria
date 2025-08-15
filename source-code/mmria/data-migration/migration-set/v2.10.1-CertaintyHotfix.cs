using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public sealed class v2_10_1_CertaintyHotfix
{


	private Microsoft.Extensions.Configuration.IConfiguration configuration;

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

	mmria.common.couchdb.ConfigurationSet configDB;

	migrate.common.CVS_API CVS_API = null;


	public v2_10_1_CertaintyHotfix
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
		this.output_builder.AppendLine($"v2.10.1 Data Migration started at: {DateTime.Now.ToString("o")}");
		DateTime begin_time = System.DateTime.Now;
		
		this.output_builder.AppendLine($"v2_10_1_CertaintyHotfix started at: {begin_time.ToString("o")}");
		
		var gs = new C_Get_Set_Value(this.output_builder);

		

		try
		{





			//string metadata_url = host_db_url + "/metadata/2016-06-12T13:49:24.759Z";
			string metadata_url = $"https://couchdb-test-mmria.apps.ecpaas-dev.cdc.gov/metadata/version_specification-22.09.13/metadata";

			//string metadata_url = $"{host_db_url}/metadata/version_specification-20.12.01/metadata";
			
			cURL metadata_curl = new cURL("GET", null, metadata_url, null, null, null);
			mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(await metadata_curl.executeAsync());
		
			CVS_API = new common.CVS_API
			(
				configDB,
				this.output_builder,
				metadata
			);



			string ping_result = await CVS_API.PingCVSServer();
			int ping_count = 1;

			System.Console.WriteLine($"CVS for: {host_db_url}");
			
			while
			(
				(
					ping_result == null ||
					ping_result != "Server is up!"
				) && 
				ping_count < 2
			)
			{


				var output_text = $"{DateTime.Now.ToString("o")} CVS Server Not running: Waiting 40 seconds to try again: {ping_result}";
				this.output_builder.AppendLine(output_text);
				Console.WriteLine(output_text);
				

				const int Milliseconds_In_Second = 1000;
				var next_date = DateTime.Now.AddMilliseconds(40 * Milliseconds_In_Second);
				while(DateTime.Now < next_date)
				{
					// do nothing
				}
				
				ping_result = await CVS_API.PingCVSServer();
				ping_count +=1;

			}


			//var Valid_CVS_Years = await CVS_Get_Valid_Years();


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



			const string certainty_code_field = "naaccr_census_tract_certainty_code";
			const string year_of_death_path = "home_record/date_of_death/year";

			var SingleFormSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"death_certificate/place_of_last_residence/",
				"death_certificate/address_of_injury/",
				"death_certificate/address_of_death/",
				"birth_fetal_death_certificate_parent/facility_of_delivery_location/",
				"birth_fetal_death_certificate_parent/location_of_residence/",
				"prenatal/location_of_primary_prenatal_care_facility/"
			};

			var MultiFormSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"er_visit_and_hospital_medical_records/name_and_location_facility/",
				"other_medical_office_visits/location_of_medical_care_facility/",
				"medical_transport/origin_information/",
				"medical_transport/destination_information/"
			};

			var GeoCodeResultSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"street",
				"apartment",
				"city",
				"state",
				"country",
				"zip_code",
				"county",
				"feature_matching_geography_type",
				"latitude",
				"longitude",
				"naaccr_gis_coordinate_quality_code",
				"naaccr_gis_coordinate_quality_type",
				"naaccr_census_tract_certainty_code",
				"naaccr_census_tract_certainty_type",
				"state_county_fips",
				"census_state_fips",
				"census_county_fips",
				"census_tract_fips",
				"urban_status",
				"census_met_div_fips",
				"census_cbsa_fips",
				"census_cbsa_micro"
			};



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
					var mmria_id = value_result.result.ToString();
					
					
					value_result = gs.get_value(doc, "home_record/record_id");
					var mmria_record_id = "";
					
					if(value_result.result != null)
						mmria_record_id = value_result.result.ToString();

					if(mmria_id.IndexOf("_design") > -1)
					{
						continue;
					}


/*
Single Forms
/death_certificate/place_of_last_residence/
/death_certificate/address_of_injury/
/death_certificate/address_of_death/
/birth_fetal_death_certificate_parent/facility_of_delivery_location/
/birth_fetal_death_certificate_parent/location_of_residence/
/prenatal/location_of_primary_prenatal_care_facility/

Multi forms

/er_visit_and_hospital_medical_records/name_and_location_facility/
/other_medical_office_visits/location_of_medical_care_facility/
/medical_transport/origin_information/
/medical_transport/destination_information/


result

/street
/apartment
/city
/state
/country
/zip_code
/county
/feature_matching_geography_type
/latitude
/longitude
/naaccr_gis_coordinate_quality_code
/naaccr_gis_coordinate_quality_type
/naaccr_census_tract_certainty_code
/naaccr_census_tract_certainty_type
/state_county_fips
/census_state_fips
/census_county_fips
/census_tract_fips
/urban_status
/census_met_div_fips
/census_cbsa_fips
/census_cbsa_micro


*/


					string get_single_form_value(string p_base_path, string p_field)
					{
						string result = null;

						value_result = gs.get_value(doc, p_base_path + p_field);
						if
						(
							value_result.result is not null &&
							value_result.result is string value_string &&
							!string.IsNullOrWhiteSpace(value_string)
						)
						{
							result = value_string;
						}


						return result;
					}


					List<(int, string)> get_multi_form_value(string p_base_path, string p_field)
					{
						var result = new List<(int, string)>();

						C_Get_Set_Value.get_multiform_value_result multiform_value_result = gs.get_multiform_value(doc, p_base_path + p_field);
if
						(
							multiform_value_result.result is not null &&
							multiform_value_result.result is List<(int, object)> result_list && 
							result_list.Count > 0
						)
						{

							foreach(var (index, value) in result_list)
							{
								if(value != null)
								{
									result.Add((index, value.ToString()));
								}
								else
								{
									result.Add((index, null));
								}
								
							}
						}


						return result;
					}


					string census_year = get_single_form_value(year_of_death_path, "");

					int over_count = 0;

					foreach(var base_path in SingleFormSet)
					{
						var certainty_field_path = base_path + certainty_code_field;


						value_result = gs.get_value(doc, certainty_field_path);

						if
						(
							value_result.result is not null &&
							value_result.result is string certainty_value_string &&
							!string.IsNullOrWhiteSpace(certainty_value_string)
						)
						{
							if( !isInNeedOfConversion(certainty_value_string))
							{

								string streetAddress = get_single_form_value(base_path, "street");
								string city = get_single_form_value(base_path, "city");
								string state = get_single_form_value(base_path, "state");
								string zip = get_single_form_value(base_path, "zip_code");
								
								if
								(
									(
										string.IsNullOrEmpty(streetAddress) &&
										string.IsNullOrEmpty(city) &&
										string.IsNullOrEmpty(state) &&
										string.IsNullOrEmpty(zip) 
									) ||
									(
										//string.IsNullOrEmpty(streetAddress) ||
										string.IsNullOrEmpty(city) &&
										string.IsNullOrEmpty(state) &&
										string.IsNullOrEmpty(zip) 
									)
									||
									(
										string.IsNullOrEmpty(streetAddress) &&
										string.IsNullOrEmpty(city) &&
										!string.IsNullOrEmpty(state) &&
										string.IsNullOrEmpty(zip) 
									)
								)
								continue;


								var geocode_result = Convert(await GetGeocodeInfo
								(
									streetAddress,
									city,
									state,
									zip,
									census_year
								));


								
								if
								(
									geocode_result.Census_Value.NAACCRCensusTractCertaintyCode.Trim() == "9"
								)
								{
									over_count += 1;

									if(over_count == 2)
									{
										System.Console.WriteLine("here");
									}
								}


								var old_certainy_code = -1;
								var new_certainty_code = -1;
								

								int.TryParse(certainty_value_string, out old_certainy_code);		
								int.TryParse(geocode_result.Census_Value.NAACCRCensusTractCertaintyCode, out new_certainty_code);
								
								if
								(
									old_certainy_code != -1 && 
									old_certainy_code < new_certainty_code 
								)
								{
									var possible_problem_text = $"possible problem: item id: {mmria_id} record_id:{mmria_record_id} path:{base_path} Certainty from :{certainty_value_string} => {geocode_result.Census_Value.NAACCRCensusTractCertaintyCode}  street: {streetAddress} city:{city} state:{state} zip:{zip} yod:{census_year}";
									this.output_builder.AppendLine(possible_problem_text);
									Console.WriteLine(possible_problem_text);
									continue;
								}
								else if
								(
									old_certainy_code != -1 && 
									old_certainy_code == new_certainty_code 
								)
								continue;

								if(case_change_count == 0)
								{
									case_change_count += 1;
									case_has_changed = true;
								}

								case_has_changed = case_has_changed && Set_SingleForm_Location_Gecocode
								(
									gs,
									geocode_result,
									doc,
									base_path
								);

								if(base_path == "death_certificate/place_of_last_residence/")
								{
									case_has_changed = case_has_changed && await CVS_API.Execute(doc, base_path, case_has_changed);
								}
								
								//case_has_changed = case_has_changed && gs.set_value(dciai_to_injur_path, new_time, doc);
								var output_text = $"item id: {mmria_id} case_has_changed: {case_has_changed} record_id:{mmria_record_id} path:{base_path} Certainty from :{certainty_value_string} => {geocode_result.Census_Value.NAACCRCensusTractCertaintyCode}  street: {streetAddress} city:{city} state:{state} zip:{zip} yod:{census_year}";
								this.output_builder.AppendLine(output_text);
								Console.WriteLine(output_text);

							}
						}


					}


					foreach(var base_path in MultiFormSet)
					{
						var certainty_field_path = base_path + certainty_code_field;
						
						C_Get_Set_Value.get_multiform_value_result multiform_value_result = null;

						var certainty_list = get_multi_form_value(certainty_field_path, "");

						var new_list = new List<(int, object)>();
						var has_changed = false;


						if(certainty_list.Count < 1) 
						{
							continue;
						}

						List<(int index, string value)> streetAddress = get_multi_form_value(base_path, "street");
						List<(int index, string value)> city = get_multi_form_value(base_path, "city");
						List<(int index, string value)> state = get_multi_form_value(base_path, "state");
						List<(int index, string value)> zip = get_multi_form_value(base_path, "zip_code");
						
						foreach(var (index, value) in certainty_list)
						{
							if
							(
								value is not null &&
								value is string certainty_value_string &&
								!string.IsNullOrWhiteSpace(certainty_value_string)
							)
							{
								if( !isInNeedOfConversion(certainty_value_string))
								{
									if
									(
										(
											string.IsNullOrEmpty(streetAddress[index].value) &&
											string.IsNullOrEmpty(city[index].value) &&
											string.IsNullOrEmpty(state[index].value) &&
											string.IsNullOrEmpty(zip[index].value) 
										) ||
										(
											//string.IsNullOrEmpty(streetAddress[index].value) &&
											string.IsNullOrEmpty(city[index].value) &&
											string.IsNullOrEmpty(state[index].value) &&
											string.IsNullOrEmpty(zip[index].value) 
										)
										||
										(
											string.IsNullOrEmpty(streetAddress[index].value) &&
											string.IsNullOrEmpty(city[index].value) &&
											!string.IsNullOrEmpty(state[index].value) &&
											string.IsNullOrEmpty(zip[index].value) 
										)
									)
									continue;

									var geocode_result = Convert(await GetGeocodeInfo
									(
										streetAddress[index].value,
										city[index].value,
										state[index].value,
										zip[index].value,
										census_year
									));


									if(geocode_result.Census_Value.NAACCRCensusTractCertaintyCode.Trim() == "9")
									{
										System.Console.WriteLine("here");
									}

									var old_certainy_code = -1;
									var new_certainty_code = -1;
									

									int.TryParse(certainty_value_string, out old_certainy_code);		
									int.TryParse(geocode_result.Census_Value.NAACCRCensusTractCertaintyCode, out new_certainty_code);
									
									if
									(
										old_certainy_code != -1 && 
										old_certainy_code < new_certainty_code 
									)
									{
										var possible_problem_text = $"possible problem: item id: {mmria_id} record_id:{mmria_record_id} path:{base_path} Certainty from :{certainty_value_string} => {geocode_result.Census_Value.NAACCRCensusTractCertaintyCode}  street: {streetAddress[index].value} city:{city[index].value} state:{state[index].value} zip:{zip[index].value} yod:{census_year}";
										this.output_builder.AppendLine(possible_problem_text);
										Console.WriteLine(possible_problem_text);
										continue;
									}
									else if
									(
										old_certainy_code != -1 && 
										old_certainy_code == new_certainty_code 
									)
									continue;

									if(case_change_count == 0)
									{
										case_change_count += 1;
										case_has_changed = true;
									}

									case_has_changed = case_has_changed && Set_MultiForm_Location_Gecocode
									(
										gs,
										geocode_result,
										doc,
										index,
										base_path
									);
									
									//case_has_changed = case_has_changed && gs.set_value(dciai_to_injur_path, new_time, doc);
									var output_text = $"item id: {mmria_id} record_id:{mmria_record_id} path:{base_path} Form Index: {index} Certainty from :{certainty_value_string} => {geocode_result.Census_Value.NAACCRCensusTractCertaintyCode}  street: {streetAddress[index].value} city:{city[index].value} state:{state[index].value} zip:{zip[index].value} yod:{census_year}";
									this.output_builder.AppendLine(output_text);
									Console.WriteLine(output_text);

								}
							}
						}
						

					}

				/*

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
				*/

/*

/death_certificate/certificate_identification/time_of_death
/death_certificate/injury_associated_information/time_of_injury
/birth_certificate_infant_fetal_section/record_identification/time_of_delivery





				if(mmria_id == "")
				{
					var save_result = await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(doc as IDictionary<string, object>,"v2.10.1");
				}
				else*/ 
				
				if(!is_report_only_mode && case_has_changed)
				{
					var save_result = await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(doc as IDictionary<string, object>,"v2.10.1");
				}

			}

		
		}
	}
	catch(Exception ex)
	{
		Console.WriteLine(ex);
	}

	Console.WriteLine($"v2_10_1_CertaintyHotfix Finished {DateTime.Now}");
}

	bool isInNeedOfConversion(string p_value)
	{
		var result = true;

		if(p_value != null)
		{
			if
			(
				p_value.Trim() != "1"
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
			var upper = p_value.Trim().ToUpper();
			if
			(
				upper.Contains("AM")
			)
			{
				var data = upper.Replace(" AM","").Split(":");
				if(data[0].Contains("12"))
				{
					data[0] = "0";
				}
				result =string.Join(':',data);
			}
			else if(upper.Contains("PM"))
			{
				var data = upper.Replace(" PM","").Split(":");
				if(int.TryParse(data[0], out var hour))
				{
					var new_hour = hour + 12;
					if(hour == 12)
					{
						new_hour = 12;
					}

					if(new_hour < 24)
					{
						data[0] = new_hour.ToString();
						result =string.Join(':',data);
					}
					else
					{
						System.Console.WriteLine("Hour greater than 24...I should never happen");
					}
				}
				else
				{
					System.Console.WriteLine("unable to parse data[0] should never happen");
				}
		
			}
			else
			{
				System.Console.WriteLine("I should never happen");
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



    public async Task<mmria.common.texas_am.geocode_response> GetGeocodeInfo
    (
        string streetAddress,
        string city,
        string state,
        string zip,
        string census_year = "2020"
    ) 
    { 

            var result = new mmria.common.texas_am.geocode_response();

            int test_year = -1; 
            
            var censusYear = "2020";

            //"2000|2010"
            if(int.TryParse(census_year, out test_year ))
            {
                censusYear = test_year switch
                {
                    < 2000 => "1990",
                    < 2010 => "2000",
                    < 2020 => "2010",
                    _ => "2020"
                };
            }

            string geocode_api_key = configuration["data_migration:geocode_api_key"];

            string request_string = string.Format ($"https://geoservices.tamu.edu/Services/Geocode/WebService/GeocoderWebServiceHttpNonParsed_V04_01.aspx?streetAddress={streetAddress}&city={city}&state={state}&zip={zip}&apikey={geocode_api_key}&format=json&allowTies=false&tieBreakingStrategy=flipACoin&includeHeader=true&census=true&censusYear={censusYear}&notStore=false&version=4.01");

            var curl = new cURL("GET", null, request_string, null);
            try
            {
                string responseFromServer = await curl.executeAsync();

                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.texas_am.geocode_response>(responseFromServer);
            
            }
            catch(Exception)// ex)
            {
                // do nothing for now
            }

            return result;
    }

	sealed class GeocodeTuple
    {
        public GeocodeTuple(){}

        public mmria.common.texas_am.OutputGeocode OutputGeocode {get;set;}
        public mmria.common.texas_am.CensusValue Census_Value {get;set;}

    }

    private GeocodeTuple Convert(mmria.common.texas_am.geocode_response value)
    {

        var result = new GeocodeTuple();

        
        if(value!= null && value.OutputGeocodes?.Length > 0)
        {
            result.OutputGeocode = value.OutputGeocodes[0].OutputGeocode;

            if(value.OutputGeocodes[0].CensusValues.Count > 0)
            {
                if(value.OutputGeocodes[0].CensusValues[0].ContainsKey("CensusValue1"))
                {
                    result.Census_Value = value.OutputGeocodes[0].CensusValues[0]["CensusValue1"];
                }
                
            }
        }

        return result;
    }

	public void SetConfiguration(Microsoft.Extensions.Configuration.IConfiguration value)
	{
		configuration = value;

	}


	public void SetConfigDB(mmria.common.couchdb.ConfigurationSet value)
	{
		configDB = value;
		
	}

    private bool Set_SingleForm_Location_Gecocode
	(
		migrate.C_Get_Set_Value gs, 
		GeocodeTuple geocode_data, 
		System.Dynamic.ExpandoObject new_case,
		string p_base_path
	)
    {

		var result = true;

        string urban_status = null;
        string state_county_fips = null;

        string feature_matching_geography_type = "Unmatchable";
        string latitude = "";
        string longitude = "";
        string naaccr_gis_coordinate_quality_code = "";
        string naaccr_gis_coordinate_quality_type = "";
        string naaccr_census_tract_certainty_code = "";
        string naaccr_census_tract_certainty_type = "";
        string census_state_fips = "";
        string census_county_fips = "";
        string census_tract_fips = "";
        string census_cbsa_fips = "";
        string census_cbsa_micro = "";
        string census_met_div_fips = "";
        urban_status = "";
        state_county_fips = "";

        var outputGeocode_data = geocode_data.OutputGeocode;
        var censusValues_data = geocode_data.Census_Value;
        
        if
        (
            outputGeocode_data != null && 
            outputGeocode_data.FeatureMatchingResultType != null &&
            !outputGeocode_data.FeatureMatchingResultType.Equals("Unmatchable", StringComparison.OrdinalIgnoreCase)
        )
        {
            latitude = outputGeocode_data.Latitude;
            longitude = outputGeocode_data.Longitude;
            feature_matching_geography_type = outputGeocode_data.FeatureMatchingGeographyType;
            naaccr_gis_coordinate_quality_code = outputGeocode_data.NAACCRGISCoordinateQualityCode;
            naaccr_gis_coordinate_quality_type = outputGeocode_data.NAACCRGISCoordinateQualityType;
            naaccr_census_tract_certainty_code = censusValues_data?.NAACCRCensusTractCertaintyCode;
            naaccr_census_tract_certainty_type = censusValues_data?.NAACCRCensusTractCertaintyType;
            census_state_fips = censusValues_data?.CensusStateFips;
            census_county_fips = censusValues_data?.CensusCountyFips;
            census_tract_fips = censusValues_data?.CensusTract;
            census_cbsa_fips = censusValues_data?.CensusCbsaFips;
            census_cbsa_micro = censusValues_data?.CensusCbsaMicro;
            census_met_div_fips = censusValues_data?.CensusMetDivFips;
            // calculate urban_status
            if (censusValues_data != null)
            {
                if
                        (
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                            censusValues_data?.CensusCbsaFips == ""
                        )
                {
                    urban_status = "Rural";
                }
                else if
                (
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                    int.Parse(censusValues_data?.CensusCbsaFips) > 0
                )
                {
                    if (!string.IsNullOrEmpty(censusValues_data?.CensusMetDivFips))
                    {
                        urban_status = "Metropolitan Division";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 0)
                    {
                        urban_status = "Metropolitan";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 1)
                    {
                        urban_status = "Micropolitan";
                    }
                }
                else
                {
                    urban_status = "Undetermined";
                } 
            }

            // calculate state_county_fips
            if (!String.IsNullOrEmpty(censusValues_data?.CensusStateFips) && !String.IsNullOrEmpty(censusValues_data?.CensusCountyFips))
            {
                state_county_fips = censusValues_data?.CensusStateFips + censusValues_data?.CensusCountyFips;
            }

            result = result && gs.set_value($"{p_base_path}latitude", latitude, new_case);
            result = result && gs.set_value($"{p_base_path}longitude",longitude, new_case);
        }

        result = result && gs.set_value($"{p_base_path}feature_matching_geography_type", feature_matching_geography_type, new_case);
        result = result && gs.set_value($"{p_base_path}latitude", latitude, new_case);
        result = result && gs.set_value($"{p_base_path}longitude", longitude, new_case);
        result = result && gs.set_value($"{p_base_path}naaccr_gis_coordinate_quality_code", naaccr_gis_coordinate_quality_code, new_case);
        result = result && gs.set_value($"{p_base_path}naaccr_gis_coordinate_quality_type", naaccr_gis_coordinate_quality_type, new_case);
        result = result && gs.set_value($"{p_base_path}naaccr_census_tract_certainty_code", naaccr_census_tract_certainty_code, new_case);
        result = result && gs.set_value($"{p_base_path}naaccr_census_tract_certainty_type", naaccr_census_tract_certainty_type, new_case);
        result = result && gs.set_value($"{p_base_path}census_state_fips", census_state_fips, new_case);
        result = result && gs.set_value($"{p_base_path}census_county_fips", census_county_fips, new_case);
        result = result && gs.set_value($"{p_base_path}census_tract_fips", census_tract_fips, new_case);
        result = result && gs.set_value($"{p_base_path}census_cbsa_fips", census_cbsa_fips, new_case);
        result = result && gs.set_value($"{p_base_path}census_cbsa_micro", census_cbsa_micro, new_case);
        result = result && gs.set_value($"{p_base_path}census_met_div_fips", census_met_div_fips, new_case);
        result = result && gs.set_value($"{p_base_path}urban_status", urban_status, new_case);
        result = result && gs.set_value($"{p_base_path}state_county_fips", state_county_fips, new_case);

		return result;
    }


	private bool Set_MultiForm_Location_Gecocode
	(
		migrate.C_Get_Set_Value gs, 
		GeocodeTuple geocode_data, 
		System.Dynamic.ExpandoObject new_case,
		int p_multiform_index,
		string p_base_path
	)
    {

		var result = true;

        string urban_status = null;
        string state_county_fips = null;

        string feature_matching_geography_type = "Unmatchable";
        string latitude = "";
        string longitude = "";
        string naaccr_gis_coordinate_quality_code = "";
        string naaccr_gis_coordinate_quality_type = "";
        string naaccr_census_tract_certainty_code = "";
        string naaccr_census_tract_certainty_type = "";
        string census_state_fips = "";
        string census_county_fips = "";
        string census_tract_fips = "";
        string census_cbsa_fips = "";
        string census_cbsa_micro = "";
        string census_met_div_fips = "";
        urban_status = "";
        state_county_fips = "";

        var outputGeocode_data = geocode_data.OutputGeocode;
        var censusValues_data = geocode_data.Census_Value;
        
        if
        (
            outputGeocode_data != null && 
            outputGeocode_data.FeatureMatchingResultType != null &&
            !outputGeocode_data.FeatureMatchingResultType.Equals("Unmatchable", StringComparison.OrdinalIgnoreCase)
        )
        {
            latitude = outputGeocode_data.Latitude;
            longitude = outputGeocode_data.Longitude;
            feature_matching_geography_type = outputGeocode_data.FeatureMatchingGeographyType;
            naaccr_gis_coordinate_quality_code = outputGeocode_data.NAACCRGISCoordinateQualityCode;
            naaccr_gis_coordinate_quality_type = outputGeocode_data.NAACCRGISCoordinateQualityType;
            naaccr_census_tract_certainty_code = censusValues_data?.NAACCRCensusTractCertaintyCode;
            naaccr_census_tract_certainty_type = censusValues_data?.NAACCRCensusTractCertaintyType;
            census_state_fips = censusValues_data?.CensusStateFips;
            census_county_fips = censusValues_data?.CensusCountyFips;
            census_tract_fips = censusValues_data?.CensusTract;
            census_cbsa_fips = censusValues_data?.CensusCbsaFips;
            census_cbsa_micro = censusValues_data?.CensusCbsaMicro;
            census_met_div_fips = censusValues_data?.CensusMetDivFips;
            // calculate urban_status
            if (censusValues_data != null)
            {
                if
                        (
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                            censusValues_data?.CensusCbsaFips == ""
                        )
                {
                    urban_status = "Rural";
                }
                else if
                (
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                    int.Parse(censusValues_data?.CensusCbsaFips) > 0
                )
                {
                    if (!string.IsNullOrEmpty(censusValues_data?.CensusMetDivFips))
                    {
                        urban_status = "Metropolitan Division";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 0)
                    {
                        urban_status = "Metropolitan";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 1)
                    {
                        urban_status = "Micropolitan";
                    }
                }
                else
                {
                    urban_status = "Undetermined";
                } 
            }

            // calculate state_county_fips
            if (!String.IsNullOrEmpty(censusValues_data?.CensusStateFips) && !String.IsNullOrEmpty(censusValues_data?.CensusCountyFips))
            {
                state_county_fips = censusValues_data?.CensusStateFips + censusValues_data?.CensusCountyFips;
            }

            result = result && gs.set_multiform_value(new_case, $"{p_base_path}latitude", new () { ( p_multiform_index, latitude ) });
            result = result && gs.set_multiform_value(new_case, $"{p_base_path}longitude", new () { ( p_multiform_index, longitude) } );
        }

        result = result && gs.set_multiform_value(new_case, $"{p_base_path}feature_matching_geography_type", new () { ( p_multiform_index, feature_matching_geography_type) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}latitude", new () { ( p_multiform_index, latitude) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}longitude", new () { ( p_multiform_index, longitude) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}naaccr_gis_coordinate_quality_code", new () { ( p_multiform_index, naaccr_gis_coordinate_quality_code) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}naaccr_gis_coordinate_quality_type", new () { ( p_multiform_index, naaccr_gis_coordinate_quality_type) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}naaccr_census_tract_certainty_code", new () { ( p_multiform_index, naaccr_census_tract_certainty_code) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}naaccr_census_tract_certainty_type", new () { ( p_multiform_index, naaccr_census_tract_certainty_type) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}census_state_fips", new () { ( p_multiform_index, census_state_fips) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}census_county_fips", new () { ( p_multiform_index, census_county_fips) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}census_tract_fips", new () { ( p_multiform_index, census_tract_fips) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}census_cbsa_fips", new () { ( p_multiform_index, census_cbsa_fips) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}census_cbsa_micro", new () { ( p_multiform_index, census_cbsa_micro) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}census_met_div_fips", new () { ( p_multiform_index, census_met_div_fips) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}urban_status", new () { ( p_multiform_index, urban_status) } );
        result = result && gs.set_multiform_value(new_case, $"{p_base_path}state_county_fips", new () { ( p_multiform_index, state_county_fips) } );

		return result;
    }

}
