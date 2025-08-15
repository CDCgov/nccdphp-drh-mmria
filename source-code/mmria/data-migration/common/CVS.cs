using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace migrate.common;

public sealed class CVS_API
{
    mmria.common.couchdb.ConfigurationSet ConfigDB;
    public System.Text.StringBuilder output_builder;

    mmria.common.metadata.app metadata;

    
    public CVS_API
    (
        mmria.common.couchdb.ConfigurationSet p_config_db,
        System.Text.StringBuilder p_output_builder,
        mmria.common.metadata.app p_metadata
    )
    {
        ConfigDB = p_config_db;
        output_builder = p_output_builder;
        metadata = p_metadata;

    }

    public async Task<bool> Execute
    (
        System.Dynamic.ExpandoObject doc,
        string p_path,
        bool case_has_changed
    )
    {

        var begin_time = DateTime.Now;

        this.output_builder.AppendLine($"Update CSV at: {begin_time.ToString("o")}");
		
		var gs = new C_Get_Set_Value(this.output_builder);


        C_Get_Set_Value.get_value_result value_result = gs.get_value(doc, "_id");
        var mmria_id = value_result.result.ToString();

        var Valid_CVS_Years = await CVS_Get_Valid_Years();

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


            // ensure cvs node exists in case
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


            /*
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
                    return case_has_changed;
                }
            }
            */


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

        return case_has_changed;
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


	async Task<List<int>> CVS_Get_Valid_Years() 
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

	async Task<(string, mmria.common.cvs.tract_county_result)> GetCVSData
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