using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace migrate.set;

public sealed class CVS_Migration
{
	public string host_db_url;
	public string db_name;
	public string config_timer_user_name;
	public string config_timer_value;

	public bool is_report_only_mode;

	mmria.common.couchdb.ConfigurationSet ConfigDB;

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

	public CVS_Migration
	(
		string p_host_db_url, 
		string p_db_name, 
		string p_config_timer_user_name, 
		string p_config_timer_value,
		System.Text.StringBuilder p_output_builder,
		Dictionary<string, HashSet<string>> p_summary_value_dictionary,
		bool p_is_report_only_mode,
		mmria.common.couchdb.ConfigurationSet p_configuration_set
	) 
	{

		host_db_url = p_host_db_url;
		db_name = p_db_name;
		config_timer_user_name = p_config_timer_user_name;
		config_timer_value = p_config_timer_value;
		output_builder = p_output_builder;
		summary_value_dictionary = p_summary_value_dictionary;
		is_report_only_mode = p_is_report_only_mode;
		ConfigDB = p_configuration_set;
	}


	public async Task execute()
	{
		this.output_builder.AppendLine($"CVS_Migration Data Migration started at: {DateTime.Now.ToString("o")}");
		DateTime begin_time = System.DateTime.Now;
		
		this.output_builder.AppendLine($"CVS_Migration started at: {begin_time.ToString("o")}");
		
		var gs = new C_Get_Set_Value(this.output_builder);

		

		try
		{

			string ping_result = await PingCVSServer();
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
				
				ping_result = await PingCVSServer();
				ping_count +=1;

			}


			var Valid_CVS_Years = await CVS_Get_Valid_Years();


			string MetadataVersion = "22.06.08";

			string metadata_url = $"{host_db_url}/metadata/version_specification-{MetadataVersion}/metadata";
			
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
					var mmria_id = value_result.result.ToString();
					if(mmria_id.IndexOf("_design") > -1)
					{
						continue;
					}

					string get_value(string p_path)
					{
						var result = String.Empty;

						value_result = gs.get_value(doc, p_path);
						if
						(
							! value_result.is_error &&
							value_result.result != null
						)
						{
							result = value_result.result.ToString();
						}

						return result;
					}

					List<(int, string)> get_grid_value(string p_path)
					{
						var result = new List<(int, string)>();

						var value_result = gs.get_grid_value(doc, p_path);
						if
						(
							! value_result.is_error &&
							value_result.result != null
						)
						{
							foreach(var (index, value) in value_result.result )
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


					
					void set_grid_value(string p_path, object p_value_list)
					{
						case_has_changed = case_has_changed &&  gs.set_grid_value(doc, p_path, new List<(int, object)>() { ( 0, p_value_list) });
					}


					var state_county_fips = get_value("death_certificate/place_of_last_residence/state_county_fips");
					var  census_tract_fips = get_value("death_certificate/place_of_last_residence/census_tract_fips");
					var  year = get_value("home_record/date_of_death/year");


					if
					(
						!string.IsNullOrEmpty(state_county_fips) &&
						!string.IsNullOrEmpty(census_tract_fips) &&
						!string.IsNullOrEmpty(year)
					)
					{
						var t_geoid = $"{state_county_fips}{census_tract_fips.Replace(".","").PadRight(6, '0')}";


						// check if record already populated
						var new_case_dictionary = doc as IDictionary<string, object>;
						if(!new_case_dictionary.ContainsKey("cvs"))
						{

							var cvs_form_metadata = new mmria.common.metadata.node();

							foreach(var child in metadata.children)
							{
								if(child.name.Equals("cvs", StringComparison.OrdinalIgnoreCase))
								{
									cvs_form_metadata = child;
								}
							}

							var new_cvs_form = new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);
							mmria.services.vitalsimport.default_case.create(cvs_form_metadata, new_cvs_form, true);
							var list = new_cvs_form["cvs"] as IDictionary<string,object>;

							if(new_case_dictionary != null)
							{
								new_case_dictionary["cvs"] = list;
							}

						}

						///cvs/cvs_grid/cvs_api_request_result_message

                        var api_result_message = get_grid_value("cvs/cvs_grid/cvs_api_request_result_message");
                        var cvs_pctmove_tract = get_grid_value("cvs/cvs_grid/cvs_pctmove_tract");
                        var cvs_pctnoins_fem_tract	= get_grid_value("cvs/cvs_grid/cvs_pctnoins_fem_tract");
                        var cvs_pctnovehicle_county = get_grid_value("cvs/cvs_grid/cvs_pctnovehicle_county");
                        var cvs_pctnovehicle_tract = get_grid_value("cvs/cvs_grid/cvs_pctnovehicle_tract");
                        var cvs_pctowner_occ_tract	 = get_grid_value("cvs/cvs_grid/cvs_pctowner_occ_tract");



						if
                        (
                            api_result_message.Count > 0 &&
                            cvs_pctmove_tract.Count > 0 &&
                            cvs_pctnoins_fem_tract.Count > 0 &&
                            cvs_pctnovehicle_county.Count > 0 &&
                            cvs_pctnovehicle_tract.Count > 0 &&
                            cvs_pctowner_occ_tract.Count > 0
                        )
						{
							var api_result_text = api_result_message[0].Item2;

/*
							if(mmria_id.Equals("", StringComparison.OrdinalIgnoreCase))
							{
								System.Console.WriteLine("here");
							}
*/
							if(api_result_text.IndexOf("check quality") >  -1)
							{
								// do nothing continue
							}
							else if
                            (
                                api_result_text.StartsWith("success") &&
                                (
									!string.IsNullOrWhiteSpace(cvs_pctmove_tract[0].Item2) &&
									!string.IsNullOrWhiteSpace(cvs_pctnoins_fem_tract[0].Item2) &&
									!string.IsNullOrWhiteSpace(cvs_pctnovehicle_county[0].Item2) &&
									!string.IsNullOrWhiteSpace(cvs_pctnovehicle_tract[0].Item2) &&
									!string.IsNullOrWhiteSpace(cvs_pctowner_occ_tract[0].Item2)
								) 
	
                            )
							{
								if

								(
                                	cvs_pctmove_tract[0].Item2 != "0" &&
                                	cvs_pctnoins_fem_tract[0].Item2 != "0" &&
                                	cvs_pctnovehicle_county[0].Item2 != "0" &&
                                	cvs_pctnovehicle_tract[0].Item2 != "0" &&
                                	cvs_pctowner_occ_tract[0].Item2 != "0"
								)
								continue;
							}
						}

						if(case_change_count == 0)
						{
							case_change_count += 1;
							case_has_changed = true;
						}
						


						var int_year_of_death = -1;
						int test_int_year = -1;

						if(int.TryParse(year, out test_int_year))
						{
							int_year_of_death = test_int_year;
						}

						var calculated_year_of_death = int_year_of_death;

						if
						(
							Valid_CVS_Years != null &&
							Valid_CVS_Years.Count > 0 &&
							! Valid_CVS_Years.Contains(int_year_of_death)
						)
						{

							var lower_diff = System.Math.Abs(Valid_CVS_Years[0] - int_year_of_death);
							var upper_diff = System.Math.Abs(Valid_CVS_Years[Valid_CVS_Years.Count -1] - int_year_of_death);

							if(lower_diff < upper_diff)
							{
								if(lower_diff <= 3)
								{
									calculated_year_of_death = Valid_CVS_Years[0];
								}
							}
							else
							{
								if(upper_diff <= 3)
								{
									calculated_year_of_death = Valid_CVS_Years[Valid_CVS_Years.Count -1];
								}
							}
						}
					

						var (cvs_response_status, tract_county_result) = await GetCVSData
						(
							state_county_fips,
							t_geoid,
							calculated_year_of_death.ToString()
						);

						set_grid_value("cvs/cvs_grid/cvs_api_request_url", ConfigDB.name_value["cvs_api_url"]);
						set_grid_value("cvs/cvs_grid/cvs_api_request_date_time", DateTime.Now.ToString("o"));
						set_grid_value("cvs/cvs_grid/cvs_api_request_c_geoid", state_county_fips);
						set_grid_value("cvs/cvs_grid/cvs_api_request_t_geoid", t_geoid);
						set_grid_value("cvs/cvs_grid/cvs_api_request_year", calculated_year_of_death.ToString());
						set_grid_value("cvs/cvs_grid/cvs_api_request_result_message", cvs_response_status);



						if(cvs_response_status == "success")
						{

							if
							(
								calculated_year_of_death != int_year_of_death ||
								(
									Valid_CVS_Years != null &&
									!Valid_CVS_Years.Contains(int_year_of_death)
								)
							)
							{
								cvs_response_status += " year_of_death adjusted";
							}

							if
							(
								is_result_quality_in_need_of_checking(tract_county_result)
							)
							{
								cvs_response_status += " check quality";
								set_grid_value("cvs/cvs_grid/cvs_api_request_result_message", cvs_response_status);
							}

							set_grid_value("cvs/cvs_grid/cvs_mdrate_county", tract_county_result.county.MDrate);
							set_grid_value("cvs/cvs_grid/cvs_pctnoins_fem_county", tract_county_result.county.pctNOIns_Fem);
							set_grid_value("cvs/cvs_grid/cvs_pctnoins_fem_tract", tract_county_result.tract.pctNOIns_Fem);
							set_grid_value("cvs/cvs_grid/cvs_pctnovehicle_county", tract_county_result.county.pctNoVehicle);
							set_grid_value("cvs/cvs_grid/cvs_pctnovehicle_tract", tract_county_result.tract.pctNoVehicle);
							set_grid_value("cvs/cvs_grid/cvs_pctmove_county", tract_county_result.county.pctMOVE);
							set_grid_value("cvs/cvs_grid/cvs_pctmove_tract", tract_county_result.tract.pctMOVE);
							set_grid_value("cvs/cvs_grid/cvs_pctsphh_county", tract_county_result.county.pctSPHH);
							set_grid_value("cvs/cvs_grid/cvs_pctsphh_tract", tract_county_result.tract.pctSPHH);
							set_grid_value("cvs/cvs_grid/cvs_pctovercrowdhh_county", tract_county_result.county.pctOVERCROWDHH);
							set_grid_value("cvs/cvs_grid/cvs_pctovercrowdhh_tract", tract_county_result.tract.pctOVERCROWDHH);
							set_grid_value("cvs/cvs_grid/cvs_pctowner_occ_county", tract_county_result.county.pctOWNER_OCC);
							set_grid_value("cvs/cvs_grid/cvs_pctowner_occ_tract", tract_county_result.tract.pctOWNER_OCC);
							set_grid_value("cvs/cvs_grid/cvs_pct_less_well_county", tract_county_result.county.pct_less_well);
							set_grid_value("cvs/cvs_grid/cvs_pct_less_well_tract", tract_county_result.tract.pct_less_well);
							set_grid_value("cvs/cvs_grid/cvs_ndi_raw_county", tract_county_result.county.NDI_raw);
							set_grid_value("cvs/cvs_grid/cvs_ndi_raw_tract", tract_county_result.tract.NDI_raw);
							set_grid_value("cvs/cvs_grid/cvs_pctpov_county", tract_county_result.county.pctPOV);
							set_grid_value("cvs/cvs_grid/cvs_pctpov_tract", tract_county_result.tract.pctPOV);
							set_grid_value("cvs/cvs_grid/cvs_ice_income_all_county", tract_county_result.county.ICE_INCOME_all);
							set_grid_value("cvs/cvs_grid/cvs_ice_income_all_tract", tract_county_result.tract.ICE_INCOME_all);
							set_grid_value("cvs/cvs_grid/cvs_medhhinc_county", tract_county_result.county.MEDHHINC);
							set_grid_value("cvs/cvs_grid/cvs_medhhinc_tract", tract_county_result.tract.MEDHHINC);
							set_grid_value("cvs/cvs_grid/cvs_pctobese_county", tract_county_result.county.pctOBESE);
							set_grid_value("cvs/cvs_grid/cvs_fi_county", tract_county_result.county.FI);
							set_grid_value("cvs/cvs_grid/cvs_cnmrate_county", tract_county_result.county.CNMrate);
							set_grid_value("cvs/cvs_grid/cvs_obgynrate_county", tract_county_result.county.OBGYNrate);
							set_grid_value("cvs/cvs_grid/cvs_rtteenbirth_county", tract_county_result.county.rtTEENBIRTH);
							set_grid_value("cvs/cvs_grid/cvs_rtstd_county", tract_county_result.county.rtSTD);
							set_grid_value("cvs/cvs_grid/cvs_rtmhpract_county", tract_county_result.county.rtMHPRACT);
							set_grid_value("cvs/cvs_grid/cvs_rtdrugodmortality_county", tract_county_result.county.rtDRUGODMORTALITY);
							set_grid_value("cvs/cvs_grid/cvs_rtopioidprescript_county", tract_county_result.county.rtOPIOIDPRESCRIPT);
							set_grid_value("cvs/cvs_grid/cvs_soccap_county", tract_county_result.county.SocCap);
							set_grid_value("cvs/cvs_grid/cvs_rtsocassoc_county", tract_county_result.county.rtSocASSOC);
							set_grid_value("cvs/cvs_grid/cvs_pcthouse_distress_county", tract_county_result.county.pctHOUSE_DISTRESS);
							set_grid_value("cvs/cvs_grid/cvs_rtviolentcr_icpsr_county", tract_county_result.county.rtVIOLENTCR_ICPSR);
							set_grid_value("cvs/cvs_grid/cvs_isolation_county", tract_county_result.county.isolation);

							var output_text = $"item record_id: {mmria_id} path: cvs/cvs_grid {cvs_response_status}";
							this.output_builder.AppendLine(output_text);
							Console.WriteLine(output_text);

						}
						else
						{
							set_grid_value("cvs/cvs_grid/cvs_mdrate_county", "");
							set_grid_value("cvs/cvs_grid/cvs_pctnoins_fem_county", "");
							set_grid_value("cvs/cvs_grid/cvs_pctnoins_fem_tract", "");
							set_grid_value("cvs/cvs_grid/cvs_pctnovehicle_county", "");
							set_grid_value("cvs/cvs_grid/cvs_pctnovehicle_tract", "");
							set_grid_value("cvs/cvs_grid/cvs_pctmove_county", "");
							set_grid_value("cvs/cvs_grid/cvs_pctmove_tract", "");
							set_grid_value("cvs/cvs_grid/cvs_pctsphh_county", "");
							set_grid_value("cvs/cvs_grid/cvs_pctsphh_tract", "");
							set_grid_value("cvs/cvs_grid/cvs_pctovercrowdhh_county", "");
							set_grid_value("cvs/cvs_grid/cvs_pctovercrowdhh_tract", "");
							set_grid_value("cvs/cvs_grid/cvs_pctowner_occ_county", "");
							set_grid_value("cvs/cvs_grid/cvs_pctowner_occ_tract", "");
							set_grid_value("cvs/cvs_grid/cvs_pct_less_well_county", "");
							set_grid_value("cvs/cvs_grid/cvs_pct_less_well_tract", "");
							set_grid_value("cvs/cvs_grid/cvs_ndi_raw_county", "");
							set_grid_value("cvs/cvs_grid/cvs_ndi_raw_tract", "");
							set_grid_value("cvs/cvs_grid/cvs_pctpov_county", "");
							set_grid_value("cvs/cvs_grid/cvs_pctpov_tract", "");
							set_grid_value("cvs/cvs_grid/cvs_ice_income_all_county", "");
							set_grid_value("cvs/cvs_grid/cvs_ice_income_all_tract", "");
							set_grid_value("cvs/cvs_grid/cvs_medhhinc_county", "");
							set_grid_value("cvs/cvs_grid/cvs_medhhinc_tract", "");
							set_grid_value("cvs/cvs_grid/cvs_pctobese_county", "");
							set_grid_value("cvs/cvs_grid/cvs_fi_county", "");
							set_grid_value("cvs/cvs_grid/cvs_cnmrate_county", "");
							set_grid_value("cvs/cvs_grid/cvs_obgynrate_county", "");
							set_grid_value("cvs/cvs_grid/cvs_rtteenbirth_county", "");
							set_grid_value("cvs/cvs_grid/cvs_rtstd_county", "");
							set_grid_value("cvs/cvs_grid/cvs_rtmhpract_county", "");
							set_grid_value("cvs/cvs_grid/cvs_rtdrugodmortality_county", "");
							set_grid_value("cvs/cvs_grid/cvs_rtopioidprescript_county", "");
							set_grid_value("cvs/cvs_grid/cvs_soccap_county", "");
							set_grid_value("cvs/cvs_grid/cvs_rtsocassoc_county", "");
							set_grid_value("cvs/cvs_grid/cvs_pcthouse_distress_county", "");
							set_grid_value("cvs/cvs_grid/cvs_rtviolentcr_icpsr_county", "");
							set_grid_value("cvs/cvs_grid/cvs_isolation_county", "");

							var response_status = "";
							if(cvs_response_status != null)
							{
								if(cvs_response_status.Length > 40)
								{
									response_status = cvs_response_status.Replace("\n"," ").Substring(0, 40);
								}
								else
								{
									response_status = cvs_response_status;
								}
							}

							var output_text = $"item record_id: {mmria_id} path: cvs/cvs_grid fail: {response_status}";
							this.output_builder.AppendLine(output_text);
							Console.WriteLine(output_text);

						}

					}


/*


g_data.death_certificate.place_of_last_residence.state_county_fips
g_data.death_certificate.place_of_last_residence.census_tract_fips

            const t_geoid = state_county_fips + g_data.death_certificate.place_of_last_residence.census_tract_fips.replace(".","").padStart(6, "0");
            //$mmria.get_cvs_api_data_info
            //(
                g_data.death_certificate.place_of_last_residence.state_county_fips,  //c_geoid, // = "13089",
                t_geoid, // = "13089021204",
                g_data.home_record.date_of_death.year, //year = "2012"


					var new_grid_item = new mmria.common.cvs.CVS_Grid_Item()
					{

						//cvs_api_request_data._ =("_id", g_data._id),
						cvs_api_request_url = base_url,
						cvs_api_request_date_time =("cvs_api_request_date_time", new Date()),
						cvs_api_request_c_geoid =("cvs_api_request_c_geoid", c_geoid),
						cvs_api_request_t_geoid =("cvs_api_request_t_geoid", t_geoid),
						cvs_api_request_year =("cvs_api_request_year", year),
					}
*/


/*

cvs_api_request_url
cvs_api_request_date_time
cvs_api_request_c_geoid
cvs_api_request_t_geoid
cvs_api_request_year
cvs_api_request_result_message

*/
				if(!is_report_only_mode && case_has_changed)
				{
					var save_result = await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(doc as IDictionary<string, object>,"CVS_Migration", true);
				}

			}

		
		}
	}
	catch(Exception ex)
	{
		Console.WriteLine(ex);
	}

	Console.WriteLine($"CVS_Migration Finished {DateTime.Now}");
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


	public async Task<mmria.common.texas_am.geocode_response> Get
        (
			string street_address,
			string city,
			string state,
			string zip
        ) 
		{ 

                var result = new mmria.common.texas_am.geocode_response();

                string geocode_api_key = ConfigDB.name_value["geocode_api_key"];
                //string geocode_api_url = configuration["mmria_settings:geocode_api_url"];

                string request_string = string.Format ($"https://geoservices.tamu.edu/Services/Geocode/WebService/GeocoderWebServiceHttpNonParsed_V04_01.aspx?streetAddress={street_address}&city={city}&state={state}&zip={zip}&apikey={geocode_api_key}&format=json&allowTies=false&tieBreakingStrategy=flipACoin&includeHeader=true&census=true&censusYear=2000|2010&notStore=false&version=4.01");

                var curl = new cURL("GET", null, request_string, null);
                try
                {
                    string responseFromServer = await curl.executeAsync();

                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.texas_am.geocode_response>(responseFromServer);
                
                }
                catch(Exception ex)
                {
                    // do nothing for now
                }

                return result;
		}


	public async Task<string> PingCVSServer() 
    { 
        var response_string = "";

        var base_url = ConfigDB.name_value["cvs_api_url"];

        try
        {
			var sever_status_body = new mmria.common.cvs.server_status_post_body()
			{
				id = ConfigDB.name_value["cvs_api_id"],
				secret = ConfigDB.name_value["cvs_api_key"],

			};

			var body_text = JsonSerializer.Serialize(sever_status_body);
			var server_statu_curl = new cURL("POST", null, base_url, body_text);

			response_string = await server_statu_curl.executeAsync();
			System.Console.WriteLine(response_string);

    
        }
        catch(System.Net.WebException ex)
        {
            System.Console.WriteLine($"cvsAPIController  POST\n{ex}");
            
            /*return Problem(
                type: "/docs/errors/forbidden",
                title: "CVS API Error",
                detail: ex.Message,
                statusCode: (int) ex.Status,
                instance: HttpContext.Request.Path
            );*/
        }


        return response_string.Trim('"');
    }	


	public async Task<List<int>> CVS_Get_Valid_Years() 
    { 
        var result = new List<int>()
		{
			2010,
			2011,
			2012,
			2013,
			2014,
			2015,
			2016,
			2017,
			2018,
			2019,
			2020
		};

        var base_url = ConfigDB.name_value["cvs_api_url"];

        try
        {


			var get_year_body = new mmria.common.cvs.get_year_post_body()
			{
				id = ConfigDB.name_value["cvs_api_id"],
				secret = ConfigDB.name_value["cvs_api_key"],
				payload = new()
			};

			var body_text = JsonSerializer.Serialize(get_year_body);
			var get_year_curl = new cURL("POST", null, base_url, body_text);
			string get_year_response = await get_year_curl.executeAsync();
			result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>> (get_year_response);

			System.Console.WriteLine(get_year_response);

    
        }
        catch(System.Net.WebException ex)
        {
            System.Console.WriteLine($"cvsAPIController Get Year POST\n{ex}");
            
            /*return Problem(
                type: "/docs/errors/forbidden",
                title: "CVS API Error",
                detail: ex.Message,
                statusCode: (int) ex.Status,
                instance: HttpContext.Request.Path
            );*/
        }


        return result;
    }	



	bool is_result_quality_in_need_of_checking(mmria.common.cvs.tract_county_result val)
	{

		var over_all_result = false;
		var tract_result = false;
		var county_result = false;

		const float tract_total = 11F;
		const float county_total = 26F;

		float tract_zero_count = 0F;
		float county_zero_count = 0F;

		const float _30_percent_correct = .3F;

		if
		(
			val.tract.pctMOVE == 0  && //cvs_pctmove_tract
			val.tract.pctNOIns_Fem == 0 && //cvs_pctnoins_fem_tract		
			val.county.pctNoVehicle == 0 && //cvs_pctnovehicle_county
			val.tract.pctNoVehicle == 0 && //cvs_pctnovehicle_tract
			val.tract.pctOWNER_OCC == 0 //cvs_pctowner_occ_tract
		)
		{
			over_all_result = true;
		}


		if(val.tract.pctNOIns_Fem == 0) tract_zero_count += 1;
		if(val.tract.MEDHHINC == 0) tract_zero_count += 1;
		if(val.tract.pctNoVehicle == 0) tract_zero_count += 1;
		if(val.tract.pctMOVE == 0) tract_zero_count += 1;
		if(val.tract.pctSPHH == 0) tract_zero_count += 1;
		if(val.tract.pctOVERCROWDHH == 0) tract_zero_count += 1;
		if(val.tract.pctOWNER_OCC == 0) tract_zero_count += 1;
		if(val.tract.pct_less_well == 0) tract_zero_count += 1;
		if(val.tract.NDI_raw == 0) tract_zero_count += 1;
		if(val.tract.pctPOV == 0) tract_zero_count += 1;
		if(val.tract.ICE_INCOME_all == 0) tract_zero_count += 1;



		if(val.county.MDrate == 0) county_zero_count += 1;
		if(val.county.pctNOIns_Fem == 0) county_zero_count += 1;
		if(val.county.pctNoVehicle == 0) county_zero_count += 1;
		if(val.county.pctMOVE == 0) county_zero_count += 1;
		if(val.county.pctSPHH == 0) county_zero_count += 1;
		if(val.county.pctOVERCROWDHH == 0) county_zero_count += 1;
		if(val.county.pctOWNER_OCC == 0) county_zero_count += 1;
		if(val.county.pct_less_well == 0) county_zero_count += 1;
		if(val.county.NDI_raw == 0) county_zero_count += 1;
		if(val.county.pctPOV == 0) county_zero_count += 1;
		if(val.county.ICE_INCOME_all == 0) county_zero_count += 1;
		if(val.county.MEDHHINC == 0) county_zero_count += 1;
		if(val.county.pctOBESE == 0) county_zero_count += 1;
		if(val.county.FI == 0) county_zero_count += 1;
		if(val.county.CNMrate == 0) county_zero_count += 1;
		if(val.county.OBGYNrate == 0) county_zero_count += 1;
		if(val.county.rtTEENBIRTH == 0) county_zero_count += 1;
		if(val.county.rtSTD == 0) county_zero_count += 1;
		if(val.county.rtMHPRACT == 0) county_zero_count += 1;
		if(val.county.rtDRUGODMORTALITY == 0) county_zero_count += 1;
		if(val.county.rtOPIOIDPRESCRIPT == 0) county_zero_count += 1;
		if(val.county.SocCap == 0) county_zero_count += 1;
		if(val.county.rtSocASSOC == 0) county_zero_count += 1;
		if(val.county.pctHOUSE_DISTRESS == 0) county_zero_count += 1;
		if(val.county.rtVIOLENTCR_ICPSR == 0) county_zero_count += 1;
		if(val.county.isolation == 0) county_zero_count += 1;


		if(tract_zero_count / tract_total < _30_percent_correct) tract_result = true;

		if(county_zero_count / county_total < _30_percent_correct) county_result = true;


		return over_all_result || tract_result || county_result;
	}
	public async Task<(string, mmria.common.cvs.tract_county_result)> GetCVSData
    (
		string c_geoid,
		string t_geoid,
		string year
    ) 
    { 

        mmria.common.cvs.tract_county_result result = null;
        var response_string = string.Empty;

        var base_url = ConfigDB.name_value["cvs_api_url"];

        try
        {
            
			var get_all_data_body = new mmria.common.cvs.get_all_data_post_body()
			{
				id = ConfigDB.name_value["cvs_api_id"],
				secret = ConfigDB.name_value["cvs_api_key"],
				payload = new()
				{
					
					c_geoid = c_geoid,
					t_geoid = t_geoid,
					year = year
				}
			};

			var body_text = JsonSerializer.Serialize(get_all_data_body);
			var get_all_data_curl = new cURL("POST", null, base_url, body_text);

			response_string = await get_all_data_curl.executeAsync();
			System.Console.WriteLine(response_string);

			result = JsonSerializer.Deserialize<mmria.common.cvs.tract_county_result>(response_string);

		

             
        }
        catch(System.Net.WebException ex)
        {
            System.Console.WriteLine($"CVS MIGRATION GetCVSDATA\n{ex}");
            
            /*return Problem(
                type: "/docs/errors/forbidden",
                title: "CVS API Error",
                detail: ex.Message,
                statusCode: (int) ex.Status,
                instance: HttpContext.Request.Path
            );*/

			return ($"Status: {ex.Status} {ex.Message} {response_string}", null);
        }

        return ("success", result);
    }
	


}
