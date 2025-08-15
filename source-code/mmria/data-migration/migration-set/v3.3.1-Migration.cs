using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public sealed class v3_3_1_Migration
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

	//mmria.common.couchdb.ConfigurationSet db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;

	mmria.common.couchdb.ConfigurationSet db_config_set;

	public bool is_data_correction = false;


	public v3_3_1_Migration
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
		db_config_set = p_configuration_set;
	}


	public async Task execute()
	{
		this.output_builder.AppendLine($"v3.3.1Data Migration started at: {DateTime.Now.ToString("o")}");
		DateTime begin_time = System.DateTime.Now;
		
		this.output_builder.AppendLine($"v3_3_1_Migration started at: {begin_time.ToString("o")}");
		
		var gs = new C_Get_Set_Value(this.output_builder);


		string ping_result = PingCVSServer(db_config_set);
		int ping_count = 1;
		
		while
		(
			(
				ping_result == null ||
				ping_result.ToLower() != "Server is up!".ToLower()
			) && 
			ping_count < 2
		)   
		{

			Console.WriteLine($"{DateTime.Now.ToString("o")} CVS Server Not running: Waiting 40 seconds to try again: {ping_result}");

			const int Milliseconds_In_Second = 1000;
			var next_date = DateTime.Now.AddMilliseconds(40 * Milliseconds_In_Second);
			while(DateTime.Now < next_date)
			{
				// do nothing
			}
			
			ping_result = PingCVSServer(db_config_set);
			ping_count +=1;

			

		}
		


		const string base_folder = "C:/Users/isu7/OneDrive - CDC/PMSS/ije/zCVS";

		var file_name_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase){

	"AK.csv",
	"AL.csv",
	"AZ.csv",
	"HI.csv",
	"MD.csv",
	"ME.csv",
	"MD.csv"

	};


		try
		{
			string FromMetadataVersion = "23.07.25";
			string ToMetadataVersion = "23.11.08";

			string metadata_url = $"{host_db_url}/metadata/version_specification-{ToMetadataVersion}/metadata";
			
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

			System.Console.WriteLine(host_db_url);
			
			var id_list = GetIdList();

			var prefix = host_db_url.Split(".")[0].Split("-")[2].ToUpper();

			//mmria.mmrds.util.csv_Data csv_data = new mmria.mmrds.util.csv_Data();
			//System.Data.DataTable cvs_data_table = csv_data.get_datatable($"{base_folder}/{prefix}.csv");
	

            var Valid_CVS_Years = CVS_Get_Valid_Years(db_config_set);


			foreach(var existing_id in id_list)
			{

				if(existing_id.IndexOf("_design") > -1)
				{
					continue;
				}

				//if(!id_set.Contains(existing_id)) continue;

				string url = $"{host_db_url}/{db_name}/{existing_id}";
				var case_curl = new cURL("GET", null, url, null, config_timer_user_name, config_timer_value);
				string responseFromServer = await case_curl.executeAsync();
				
				var doc = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);
			

				//var case_item in case_response.rows
				var case_has_changed = false;
				var case_change_count = 0;

				//var doc = case_item.doc;
				
				if(doc != null)
				{

					C_Get_Set_Value.get_value_result value_result = gs.get_value(doc, "_id");
					string mmria_id = value_result.result.ToString();
					
					string get_value(string p_path)
					{
						var result = String.Empty;


						migrate.C_Get_Set_Value.get_value_result temp_result = gs.get_value(doc, p_path);
						if
						(
							! temp_result.is_error &&
							temp_result.result != null
						)
						{
							result = temp_result.result.ToString();
						}

						return result;
					}

					
					bool set_value(string p_path, string p_value)

					{
						if(case_change_count == 0)
						{
							case_change_count += 1;
							case_has_changed = true;
						}

						case_has_changed = case_has_changed &&  gs.set_value(p_path, p_value, doc);

						return case_has_changed;
					}


					//bool set_grid_value(string p_path, List<(int, object)> p_value_list)
					bool set_grid_value(string p_path, object p_value_list)

					{
						if(case_change_count == 0)
						{
							case_change_count += 1;
							case_has_changed = true;
						}

						case_has_changed = case_has_changed  &&  gs.set_grid_value(doc, p_path, new List<(int, object)>() { ( 0, p_value_list) });

						return case_has_changed;
					}


				//var current_result = cvs_data_table.Select($"_id='{mmria_id}'");

		
				
				//if(current_result.Length < 1) continue;

				//var current_row = current_result[0];

				//var tract_county_result = convert_to_tract_county(current_row);


                var state_county_fips = get_value("death_certificate/place_of_last_residence/state_county_fips");
                var  census_tract_fips = get_value("death_certificate/place_of_last_residence/census_tract_fips");
                var  year = get_value("home_record/date_of_death/year");

                var cvs_form_metadata = new mmria.common.metadata.node();

                foreach(var child in metadata.children)
                {
                    if(child.name.Equals("cvs", StringComparison.OrdinalIgnoreCase))
                    {
                        cvs_form_metadata = child;
                    }
                }




				var new_case_dictionary = doc as IDictionary<string, object>;

                if
				(
					new_case_dictionary != null &&
					!new_case_dictionary.ContainsKey("cvs")
				)
                {

					var new_cvs_form = new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);
					mmria.services.vitalsimport.default_case.create(cvs_form_metadata, new_cvs_form, true);
					var list = new_cvs_form["cvs"] as  IDictionary<string,object>;
                    new_case_dictionary.Add("cvs", list);               
                }


/*

				set_grid_value("cvs/cvs_grid/cvs_api_request_url", current_row["cvs_api_request_url"]);
				set_grid_value("cvs/cvs_grid/cvs_api_request_date_time", current_row["cvs_api_request_date_time"]);
				set_grid_value("cvs/cvs_grid/cvs_api_request_c_geoid", current_row["cvs_api_request_c_geoid"]);
				set_grid_value("cvs/cvs_grid/cvs_api_request_t_geoid", current_row["cvs_api_request_t_geoid"]);
				set_grid_value("cvs/cvs_grid/cvs_api_request_year", current_row["cvs_api_request_year"]);


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
				set_grid_value("cvs/cvs_grid/cvs_rtmhpract_county", tract_county_result.county.MHCENTERrate);
				set_grid_value("cvs/cvs_grid/cvs_rtdrugodmortality_county", tract_county_result.county.rtDRUGODMORTALITY);
				set_grid_value("cvs/cvs_grid/cvs_rtopioidprescript_county", tract_county_result.county.rtOPIOIDPRESCRIPT);
				set_grid_value("cvs/cvs_grid/cvs_soccap_county", tract_county_result.county.SocCap);
				set_grid_value("cvs/cvs_grid/cvs_rtsocassoc_county", tract_county_result.county.rtSocASSOC);
				set_grid_value("cvs/cvs_grid/cvs_pcthouse_distress_county", tract_county_result.county.pctHOUSE_DISTRESS);
				set_grid_value("cvs/cvs_grid/cvs_rtviolentcr_icpsr_county", tract_county_result.county.rtVIOLENTCR_ICPSR);
				set_grid_value("cvs/cvs_grid/cvs_isolation_county", tract_county_result.county.isolation);

				set_grid_value("cvs/cvs_grid/cvs_cnmrate_county", tract_county_result.county.MIDWIVESrate);
				set_grid_value("cvs/cvs_grid/cvs_isolation_county", tract_county_result.county.segregation);
				set_grid_value("cvs/cvs_grid/cvs_mdrate_county", tract_county_result.county.PCPrate);
				set_grid_value("cvs/cvs_grid/cvs_rtviolentcr_icpsr_county", tract_county_result.county.rtVIOLENTCR);

				set_grid_value("cvs/cvs_grid/cvs_pctrural", tract_county_result.county.pctRural);
				set_grid_value("cvs/cvs_grid/cvs_racialized_pov",  tract_county_result.county.Racialized_pov);
				set_grid_value("cvs/cvs_grid/cvs_mhproviderrate",  tract_county_result.county.MHPROVIDERrate);

				set_grid_value("cvs/cvs_grid/cvs_api_request_result_message", current_row["cvs_api_request_result_message"]);









goto cvs_early_exit_label;*/


                var int_year_of_death = -1;
                int test_int_year = -1;

                const int year_difference_limit = 9;

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
                        if(lower_diff <= year_difference_limit)
                        {
                            calculated_year_of_death = Valid_CVS_Years[0];
                        }
                    }
                    else
                    {
                        if(upper_diff <= year_difference_limit)
                        {
                            calculated_year_of_death = Valid_CVS_Years[Valid_CVS_Years.Count -1];
                        }
                    }
                }

				const int cut_off_lower_bound_year = 2005;
                if
                (
                    !string.IsNullOrEmpty(state_county_fips) &&
                    !string.IsNullOrEmpty(census_tract_fips) &&
                    !string.IsNullOrEmpty(year) &&
					! (test_int_year < cut_off_lower_bound_year)
					
                )
                {
                    var t_geoid = $"{state_county_fips}{census_tract_fips.Replace(".","").PadRight(6, '0')}";


                    var (cvs_response_status, tract_county_result) = GetCVSData
                    (
                        state_county_fips,
                        t_geoid,
                        calculated_year_of_death.ToString(),
                       db_config_set
                    );

                    if(cvs_response_status == "success")
                    {

                        if(calculated_year_of_death != int_year_of_death)
                        {
                            cvs_response_status += " year_of_death adjusted";
                        }

                        if
                        (
                          is_result_quality_in_need_of_checking(tract_county_result)
                        )
                        {
                            cvs_response_status += " check quality";
							//goto cvs_early_exit_label;

                        }

						set_grid_value("cvs/cvs_grid/cvs_api_request_url", db_config_set.name_value["cvs_api_url"]);
						set_grid_value("cvs/cvs_grid/cvs_api_request_date_time", DateTime.Now.ToString("o"));
						set_grid_value("cvs/cvs_grid/cvs_api_request_c_geoid", state_county_fips);
						set_grid_value("cvs/cvs_grid/cvs_api_request_t_geoid", t_geoid);
						set_grid_value("cvs/cvs_grid/cvs_api_request_year", calculated_year_of_death.ToString());


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
                        set_grid_value("cvs/cvs_grid/cvs_rtmhpract_county", tract_county_result.county.MHCENTERrate);
                        set_grid_value("cvs/cvs_grid/cvs_rtdrugodmortality_county", tract_county_result.county.rtDRUGODMORTALITY);
                        set_grid_value("cvs/cvs_grid/cvs_rtopioidprescript_county", tract_county_result.county.rtOPIOIDPRESCRIPT);
                        set_grid_value("cvs/cvs_grid/cvs_soccap_county", tract_county_result.county.SocCap);
                        set_grid_value("cvs/cvs_grid/cvs_rtsocassoc_county", tract_county_result.county.rtSocASSOC);
                        set_grid_value("cvs/cvs_grid/cvs_pcthouse_distress_county", tract_county_result.county.pctHOUSE_DISTRESS);
                        set_grid_value("cvs/cvs_grid/cvs_rtviolentcr_icpsr_county", tract_county_result.county.rtVIOLENTCR_ICPSR);
                        set_grid_value("cvs/cvs_grid/cvs_isolation_county", tract_county_result.county.isolation);

                        set_grid_value("cvs/cvs_grid/cvs_cnmrate_county", tract_county_result.county.MIDWIVESrate);
                        set_grid_value("cvs/cvs_grid/cvs_isolation_county", tract_county_result.county.segregation);
                        set_grid_value("cvs/cvs_grid/cvs_mdrate_county", tract_county_result.county.PCPrate);
                        set_grid_value("cvs/cvs_grid/cvs_rtviolentcr_icpsr_county", tract_county_result.county.rtVIOLENTCR);

                        set_grid_value("cvs/cvs_grid/cvs_pctrural", tract_county_result.county.pctRural);
                        set_grid_value("cvs/cvs_grid/cvs_racialized_pov",  tract_county_result.county.Racialized_pov);
                        set_grid_value("cvs/cvs_grid/cvs_mhproviderrate",  tract_county_result.county.MHPROVIDERrate);

						set_grid_value("cvs/cvs_grid/cvs_api_request_result_message", cvs_response_status);

                    }

                    
                }
                else
                {
                    
                }


cvs_early_exit_label:


				if(!is_report_only_mode && case_has_changed)
				{
					var save_result = await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(doc as IDictionary<string, object>,"v3.3.2");
				}

			}

		
		}
	}
	catch(Exception ex)
	{
		Console.WriteLine(ex);
	}

	Console.WriteLine($"v3_3_1_Migration Finished {DateTime.Now}");
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


	private int GenerateRandomFourDigits()
	{
		int _min = 1000;
		int _max = 9999;
		Random _rdm = new Random();
		return _rdm.Next(_min, _max);
	}


	public (string, mmria.common.cvs.tract_county_result) GetCVSData
    (
        string c_geoid,
		string t_geoid,
		string year,
        mmria.common.couchdb.ConfigurationSet ConfigDB
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

			

            var body_text =  System.Text.Json.JsonSerializer.Serialize(get_all_data_body);
            var get_all_data_curl = new cURL("POST", null, base_url, body_text);

            response_string = get_all_data_curl.execute();
            System.Console.WriteLine(response_string);

			var options = new System.Text.Json.JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
            result = System.Text.Json.JsonSerializer.Deserialize<mmria.common.cvs.tract_county_result>(response_string, options);
        
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

            return ($"Status: {ex.Status} {ex.Message} {response_string}", null);
        }

        return ("success", result);
    }


    public List<int> CVS_Get_Valid_Years(mmria.common.couchdb.ConfigurationSet ConfigDB) 
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

			var body_text =  System.Text.Json.JsonSerializer.Serialize(get_year_body);
			var get_year_curl = new cURL("POST", null, base_url, body_text);
			string get_year_response = get_year_curl.execute();
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

		var five_required_fields_are_all_zero = false;
		var tract_zero_count_more_than_30_percent = false;
		var county_zero_count_more_than_90_percent = false;

		const float tract_total = 11F;
		const float county_total = 33F;

		float tract_zero_count = 0F;
		float county_zero_count = 0F;

		const float _30_percent_are_zeros = .3F;

		const float _90_percent_are_zeros = .9F;

		if
		(
			val.tract.pctMOVE == 0  && //cvs_pctmove_tract
			val.tract.pctNOIns_Fem == 0 && //cvs_pctnoins_fem_tract		
			val.county.pctNoVehicle == 0 && //cvs_pctnovehicle_county
			val.tract.pctNoVehicle == 0 && //cvs_pctnovehicle_tract
			val.tract.pctOWNER_OCC == 0 //cvs_pctowner_occ_tract
		)
		{
			five_required_fields_are_all_zero = true;
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



		if(val.county.MIDWIVESrate == 0) county_zero_count += 1;
		if(val.county.segregation == 0) county_zero_count += 1;
		if(val.county.PCPrate == 0) county_zero_count += 1;
		if(val.county.rtVIOLENTCR == 0) county_zero_count += 1;

		if(val.county.pctRural == 0) county_zero_count += 1;
		if(val.county.Racialized_pov == 0) county_zero_count += 1;
		if(val.county.MHPROVIDERrate == 0) county_zero_count += 1;


		if(tract_zero_count / tract_total > _30_percent_are_zeros) tract_zero_count_more_than_30_percent = true;

		if(county_zero_count / county_total > _90_percent_are_zeros) county_zero_count_more_than_90_percent = true;


		return five_required_fields_are_all_zero || 
			tract_zero_count_more_than_30_percent || 
			county_zero_count_more_than_90_percent;
	}

	mmria.common.niosh.NioshResult get_niosh_codes(string p_occupation, string p_industry)
    {
        var result = new mmria.common.niosh.NioshResult();
        var builder = new System.Text.StringBuilder();
        builder.Append("https://wwwn.cdc.gov/nioccs/IOCode?n=3");
        var has_occupation = false;
        var has_industry = false;

        if(!string.IsNullOrWhiteSpace(p_occupation))
        {
            has_occupation = true;
            builder.Append($"&o={p_occupation}");
        }

        if(!string.IsNullOrWhiteSpace(p_industry))
        {
            has_industry = true;
            builder.Append($"&i={p_industry}");
        }

        
        if(has_occupation || has_industry)
        {
            var niosh_url = builder.ToString();

            var niosh_curl = new cURL("GET", null, niosh_url, null);

            try
            {
                string responseFromServer = niosh_curl.execute();

                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.niosh.NioshResult>(responseFromServer);
            }
            catch
            {
                result.is_error = true;
            }
            
        }
        //{"Industry": [{"Code": "611110","Title": "Elementary and Secondary Schools","Probability": "9.999934E-001"},{"Code": "611310","Title": "Colleges, Universities, and Professional Schools","Probability": "2.598214E-006"},{"Code": "009990","Title": "Insufficient information","Probability": "2.312557E-006"}],"Occupation": [{"Code": "00-9900","Title": "Insufficient Information","Probability": "9.999897E-001"},{"Code": "11-9032","Title": "Education Administrators, Elementary and Secondary School","Probability": "6.550550E-006"},{"Code": "53-3022","Title": "Bus Drivers, School or Special Client","Probability": "4.932875E-007"}],"Scheme": "NAICS 2012 and SOC 2010"}
        return result;

    }



	public string PingCVSServer
    (
        mmria.common.couchdb.ConfigurationSet ConfigDB
    ) 
    { 
        var response_string = "";
        try
        {
            var base_url = ConfigDB.name_value["cvs_api_url"];

            var sever_status_body = new mmria.common.cvs.server_status_post_body()
            {
                id = ConfigDB.name_value["cvs_api_id"],
                secret = ConfigDB.name_value["cvs_api_key"],

            };

            var body_text =  System.Text.Json.JsonSerializer.Serialize(sever_status_body);
            var server_statu_curl = new cURL("POST", null, base_url, body_text);

            response_string = server_statu_curl.execute();
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
//"Server is up!"


        return response_string.Trim('"');
    }

	private HashSet<string> GetIdList ()
	{
/*
		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"0354d819-f445-cb98-bb8c-1eed8932e2f4",
			"0305b731-9112-46d4-8537-43195b15b0f2",
			"05a2395f-4493-41e5-9d45-a5f7ee68346e"
		};

		return result;*/


		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			string URL = $"{host_db_url}/{db_name}/_all_docs";
			var document_curl = new cURL ("GET", null, URL, null, config_timer_user_name, config_timer_value);
			var curl_result = document_curl.execute();

			var all_cases = System.Text.Json.JsonSerializer.Deserialize<mmria.common.model.couchdb.alldocs_response<System.Dynamic.ExpandoObject>> (curl_result);
			var all_cases_rows = all_cases.rows;

			foreach (var row in all_cases_rows) 
			{
				result.Add(row.id);
			}
		}
		catch(Exception)
		{

		}
		return result;
	}



	mmria.common.cvs.tract_county_result convert_to_tract_county(System.Data.DataRow row)
	{
		var result = new mmria.common.cvs.tract_county_result();
		result.tract = new();
		result.county = new();


		double convert_db_object(string column_name)
		{
			if(!row.Table.Columns.Contains(column_name))
			{
				//System.Console.WriteLine("missing Column: " + column_name);
				return 0;
			} 

			object value = row[column_name];

			if(value is string string_value)
			{
				double out_value = -1;
				if(double.TryParse(string_value, out out_value))
				{
					return out_value;
				}
			}
			else if(value is double) return (double) value;
			else
			{
				System.Console.WriteLine("here");
			}

			return 0;
		}


		//set_grid_value("cvs/cvs_grid/cvs_api_request_url", db_config_set.name_value["cvs_api_url"]);
		//set_grid_value("cvs/cvs_grid/cvs_api_request_date_time", DateTime.Now.ToString("o"));
		//set_grid_value("cvs/cvs_grid/cvs_api_request_c_geoid", state_county_fips);
		//set_grid_value("cvs/cvs_grid/cvs_api_request_t_geoid", t_geoid);
		//set_grid_value("cvs/cvs_grid/cvs_api_request_year", calculated_year_of_death.ToString());


		result.county.MDrate = convert_db_object("cvs_mdrate_county");//, result.county.MDrate);
		result.county.pctNOIns_Fem = convert_db_object("cvs_pctnoins_fem_county");//, result.county.pctNOIns_Fem);
		result.tract.pctNOIns_Fem = convert_db_object("cvs_pctnoins_fem_tract");//, result.tract.pctNOIns_Fem
		result.county.pctNoVehicle = convert_db_object("cvs_pctnovehicle_county");//, result.county.pctNoVehicle
		result.tract.pctNoVehicle = convert_db_object("cvs_pctnovehicle_tract");//, result.tract.pctNoVehicle
		result.county.pctMOVE = convert_db_object("cvs_pctmove_county");//, result.county.pctMOVE
		result.tract.pctMOVE = convert_db_object("cvs_pctmove_tract");//, result.tract.pctMOVE
		result.county.pctSPHH = convert_db_object("cvs_pctsphh_county");//, result.county.pctSPHH
		result.tract.pctSPHH = convert_db_object("cvs_pctsphh_tract");//, result.tract.pctSPHH
		result.county.pctOVERCROWDHH = convert_db_object("cvs_pctovercrowdhh_county");//, result.county.pctOVERCROWDHH
		result.tract.pctOVERCROWDHH = convert_db_object("cvs_pctovercrowdhh_tract");//, result.tract.pctOVERCROWDHH
		result.county.pctOWNER_OCC = convert_db_object("cvs_pctowner_occ_county");//, result.county.pctOWNER_OCC
		result.tract.pctOWNER_OCC = convert_db_object("cvs_pctowner_occ_tract");//, result.tract.pctOWNER_OCC
		result.county.pct_less_well = convert_db_object("cvs_pct_less_well_county");//, result.county.pct_less_well
		result.tract.pct_less_well = convert_db_object("cvs_pct_less_well_tract");//, result.tract.pct_less_well
		result.county.NDI_raw = convert_db_object("cvs_ndi_raw_county");//, result.county.NDI_raw
		result.tract.NDI_raw = convert_db_object("cvs_ndi_raw_tract");//, result.tract.NDI_raw
		result.county.pctPOV = convert_db_object("cvs_pctpov_county");//, result.county.pctPOV
		result.tract.pctPOV = convert_db_object("cvs_pctpov_tract");//, result.tract.pctPOV
		result.county.ICE_INCOME_all = convert_db_object("cvs_ice_income_all_county");//, result.county.ICE_INCOME_all
		result.tract.ICE_INCOME_all = convert_db_object("cvs_ice_income_all_tract");//, result.tract.ICE_INCOME_all
		result.county.MEDHHINC = convert_db_object("cvs_medhhinc_county");//, result.county.MEDHHINC
		result.tract.MEDHHINC = convert_db_object("cvs_medhhinc_tract");//, result.tract.MEDHHINC
		result.county.pctOBESE = convert_db_object("cvs_pctobese_county");//, result.county.pctOBESE
		result.county.FI = convert_db_object("cvs_fi_county");//, result.county.FI
		result.county.CNMrate = convert_db_object("cvs_cnmrate_county");//, result.county.CNMrate
		result.county.OBGYNrate = convert_db_object("cvs_obgynrate_county");//, result.county.OBGYNrate
		result.county.rtTEENBIRTH = convert_db_object("cvs_rtteenbirth_county");//, result.county.rtTEENBIRTH
		result.county.rtSTD = convert_db_object("cvs_rtstd_county");//, result.county.rtSTD
		result.county.MHCENTERrate = convert_db_object("cvs_rtmhpract_county");//, result.county.MHCENTERrate
		result.county.rtDRUGODMORTALITY = convert_db_object("cvs_rtdrugodmortality_county");//, result.county.rtDRUGODMORTALITY
		result.county.rtOPIOIDPRESCRIPT = convert_db_object("cvs_rtopioidprescript_county");//, result.county.rtOPIOIDPRESCRIPT
		result.county.SocCap = convert_db_object("cvs_soccap_county");//, result.county.SocCap
		result.county.rtSocASSOC = convert_db_object("cvs_rtsocassoc_county");//, result.county.rtSocASSOC
		result.county.pctHOUSE_DISTRESS = convert_db_object("cvs_pcthouse_distress_county");//, result.county.pctHOUSE_DISTRESS
		result.county.rtVIOLENTCR_ICPSR = convert_db_object("cvs_rtviolentcr_icpsr_county");//, result.county.rtVIOLENTCR_ICPSR
		result.county.isolation = convert_db_object("cvs_isolation_county");//, result.county.isolation

		result.county.MIDWIVESrate = convert_db_object("cvs_cnmrate_county");//, result.county.MIDWIVESrate);
		result.county.segregation = convert_db_object("cvs_isolation_county");//, result.county.segregation);
		result.county.PCPrate = convert_db_object("cvs_mdrate_county");//, result.county.PCPrate);
		result.county.rtVIOLENTCR = convert_db_object("cvs_rtviolentcr_icpsr_county");//, result.county.rtVIOLENTCR);

		result.county.pctRural = convert_db_object("cvs_pctrural");//, result.county.pctRural);
		result.county.Racialized_pov = convert_db_object("cvs_racialized_pov");//,  result.county.Racialized_pov);
		result.county.MHPROVIDERrate = convert_db_object("cvs_mhproviderrate");//,  result.county.MHPROVIDERrate);

		return result;
	}

}
