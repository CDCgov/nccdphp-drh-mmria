var $mmria = function() 
{
    return {
        dc_plc_cvs_button_click: function (p_control)
        {
            const lat = g_data.death_certificate.place_of_last_residence.latitude;
            const lon = g_data.death_certificate.place_of_last_residence.longitude;
            const year = g_data.home_record.date_of_death.year;
            const record_id = g_data.home_record.record_id;

            if(g_is_committee_member_view != null && g_is_committee_member_view == true)
            {

                $mmria.get_cvs_api_dashboard_info
                (
                    lat,
                    lon,
                    year,
                    record_id
                );

            }
            else
            {
            
                //Error: Community Vital Signs PDF
                //Validation: Address is not validated.
                // Please verify address and validate before clicking button to View Community Vital Signs PDF.


                const census_set = new Set();
                census_set.add(1);
                census_set.add(2);
                census_set.add(3);
                census_set.add(4);
                census_set.add(5);
                census_set.add(6);
    



                ///Error: Community Vital Signs PDF
                //Validation: Census tract certainty code NOT 1,2,3,4,5 or 6.
                //There might be a potential error in the address. Please verify address and validate before clicking the button to View Community Vital Signs PDF.


                let census_track_certainty_code = g_data.death_certificate.place_of_last_residence.urban_status;
                if
                (
                    census_track_certainty_code == 'Undetermined'
                )
                {

                    $mmria.info_dialog_show("Error: Community Vital Signs PDF","Census tract certainty code NOT 1,2,3,4,5 or 6.", "Please verify address and validate before clicking button to View Community Vital Signs PDF.");
                }
                else
                {

                    if
                    (
                        lat == null ||
                        lon == null ||
                        year == null ||
                        lat == "" ||
                        lon == "" ||
                        year == null
                    )
                    {
                        $mmria.info_dialog_show("Error: Community Vital Signs PDF","Address is not validated.", "Please verify address and validate before clicking button to View Community Vital Signs PDF.");
                    }
                    else
                    {
                        $mmria.get_cvs_api_dashboard_info
                        (
                            lat,
                            lon,
                            year,
                            record_id
                        );
                    }
                }
            }
        },
        cvs_view_community_vital_signs_button_click: function (p_control)
        {

            const lat = g_data.death_certificate.place_of_last_residence.latitude;
            const lon = g_data.death_certificate.place_of_last_residence.longitude;
            const year = g_data.home_record.date_of_death.year;
            const record_id = g_data.home_record.record_id;

            if(g_is_committee_member_view != null && g_is_committee_member_view == true)
            {

                $mmria.get_cvs_api_dashboard_info
                (
                    lat,
                    lon,
                    year,
                    record_id
                );

            }
            else
            {
                let census_track_certainty_code = g_data.death_certificate.address_of_injury.urban_status;
                if
                (
                    census_track_certainty_code == 'Undetermined'
                )
                {
                    $mmria.info_dialog_show("Error: Community Vital Signs PDF","Census tract certainty code NOT 1,2,3,4,5 or 6.", "Please verify address and validate before clicking button to View Community Vital Signs PDF.");
                }
                else
                {

                    if
                    (
                        lat == null ||
                        lon == null ||
                        year == null ||
                        lat == "" ||
                        lon == "" ||
                        year == null
                    )
                    {
                    
                        $mmria.info_dialog_show
                        (
                            "Error: Community Vital Signs PDF",
                            "Address is not validated.", 
                            "Please verify <strong>Place of Last Residence</strong> address on the <strong>Death Certificate</strong> form and validate before clicking button to View Community Vital Signs PDF."
                        );
                    }
                    else
                    {
                        $mmria.get_cvs_api_dashboard_info
                        (
                            lat,
                            lon,
                            year,
                            record_id
                        );
                    }
                }
            }
        },
        callback_cvs_data_success: function (p_result)
        {

            //const data = eval("(" + p_result + ")"); 
            console.log(p_result);
            //console.log(data);
            console.log(p_result);
            if
            (
                g_cvs_api_request_data.has("_id") &&
                g_cvs_api_request_data.get("_id") == g_data._id
            )
            {
                if(g_data.cvs == null)
                {
                    g_data.cvs = {
                        "cvs_used": "9999",
                        "cvs_used_how": "9999",
                        "cvs_used_other_sp": "",
                        "cvs_grid": []
                    }
                }

                if(p_result.tract == null)
                {
                    g_cvs_api_request_data.set("cvs_api_request_result_message", p_result); 
                    g_cvs_api_request_data.set
                    (
                        "cvs_api_request_result_message",
                        `status code: ${p_result.status} message: ${p_result.message}`
                    );
                    
                    
                    const new_grid_item = {

                        cvs_api_request_url: g_cvs_api_request_data.get("cvs_api_request_url"),
                        cvs_api_request_date_time: g_cvs_api_request_data.get("cvs_api_request_date_time"),
                        cvs_api_request_c_geoid: g_cvs_api_request_data.get("cvs_api_request_c_geoid"),
                        cvs_api_request_t_geoid: g_cvs_api_request_data.get("cvs_api_request_t_geoid"),
                        cvs_api_request_year: g_cvs_api_request_data.get("cvs_api_request_year"),
                        cvs_api_request_result_message: g_cvs_api_request_data.get("cvs_api_request_result_message"),
                        cvs_mdrate_county: "",
                        cvs_pctnoins_fem_county: "",
                        cvs_pctnoins_fem_tract: "",
                        cvs_pctnovehicle_county: "",                                   
                        cvs_pctnovehicle_tract: "",
                        cvs_pctmove_county: "",
                        cvs_pctmove_tract: "",
                        cvs_pctsphh_county: "",
                        cvs_pctsphh_tract: "",
                        cvs_pctovercrowdhh_county: "",
                        cvs_pctovercrowdhh_tract: "",
                        cvs_pctowner_occ_county: "",
                        cvs_pctowner_occ_tract: "",
                        cvs_pct_less_well_county: "",
                        cvs_pct_less_well_tract: "",
                        cvs_ndi_raw_county: "",
                        cvs_ndi_raw_tract: "",
                        cvs_pctpov_county: "",
                        cvs_pctpov_tract: "",
                        cvs_ice_income_all_county: "",
                        cvs_ice_income_all_tract: "",
                        cvs_medhhinc_county: "",
                        cvs_medhhinc_tract: "",
                        cvs_pctobese_county: "",
                        cvs_fi_county: "",
                        cvs_cnmrate_county: "",
                        cvs_obgynrate_county: "",
                        cvs_rtteenbirth_county: "",
                        cvs_rtstd_county: "",
                        cvs_rtmhpract_county: "",
                        cvs_rtdrugodmortality_county: "",
                        cvs_rtopioidprescript_county: "",
                        cvs_soccap_county: "",
                        cvs_rtsocassoc_county: "",
                        cvs_pcthouse_distress_county: "",
                        cvs_rtviolentcr_icpsr_county: "",
                        cvs_isolation_county: "",
                        cvs_pctrural : "",
                        cvs_mhproviderrate : "",
                        cvs_racialized_pov : ""
                        
                        };
    
                        g_data.cvs.cvs_grid = [ new_grid_item ];
                }
                else
                {
                    g_cvs_api_request_data.set("cvs_api_request_result_message", "Data request successful."); 

                    const new_grid_item = {

                    cvs_api_request_url: g_cvs_api_request_data.get("cvs_api_request_url"),
                    cvs_api_request_date_time: g_cvs_api_request_data.get("cvs_api_request_date_time"),
                    cvs_api_request_c_geoid: g_cvs_api_request_data.get("cvs_api_request_c_geoid"),
                    cvs_api_request_t_geoid: g_cvs_api_request_data.get("cvs_api_request_t_geoid"),
                    cvs_api_request_year: g_cvs_api_request_data.get("cvs_api_request_year"),
                    cvs_api_request_result_message: g_cvs_api_request_data.get("cvs_api_request_result_message"),
                    
                    cvs_pctnoins_fem_county: p_result.county.pctNOIns_Fem,
                    cvs_pctnoins_fem_tract: p_result.tract.pctNOIns_Fem,
                    cvs_pctnovehicle_county: p_result.county.pctNoVehicle,                                   
                    cvs_pctnovehicle_tract: p_result.tract.pctNoVehicle,
                    cvs_pctmove_county: p_result.county.pctMOVE,
                    cvs_pctmove_tract: p_result.tract.pctMOVE,
                    cvs_pctsphh_county: p_result.county.pctSPHH,
                    cvs_pctsphh_tract: p_result.tract.pctSPHH,
                    cvs_pctovercrowdhh_county: p_result.county.pctOVERCROWDHH,
                    cvs_pctovercrowdhh_tract: p_result.tract.pctOVERCROWDHH,
                    cvs_pctowner_occ_county: p_result.county.pctOWNER_OCC,
                    cvs_pctowner_occ_tract: p_result.tract.pctOWNER_OCC,
                    cvs_pct_less_well_county: p_result.county.pct_less_well,
                    cvs_pct_less_well_tract: p_result.tract.pct_less_well,
                    cvs_ndi_raw_county: p_result.county.ndI_raw,
                    cvs_ndi_raw_tract: p_result.tract.ndI_raw,
                    cvs_pctpov_county: p_result.county.pctPOV,
                    cvs_pctpov_tract: p_result.tract.pctPOV,
                    cvs_ice_income_all_county: p_result.county.icE_INCOME_all,
                    cvs_ice_income_all_tract: p_result.tract.icE_INCOME_all,
                    cvs_medhhinc_county: p_result.county.medhhinc,
                    cvs_medhhinc_tract: p_result.tract.medhhinc,
                    cvs_pctobese_county: p_result.county.pctOBESE,
                    cvs_fi_county: p_result.county.fi,
                    
                    cvs_obgynrate_county: p_result.county.obgyNrate,
                    cvs_rtteenbirth_county: p_result.county.rtTEENBIRTH,
                    cvs_rtstd_county: p_result.county.rtSTD,
                    cvs_rtmhpract_county: p_result.county.mhcenteRrate,
                    cvs_rtdrugodmortality_county: p_result.county.rtDRUGODMORTALITY,
                    cvs_rtopioidprescript_county: p_result.county.rtOPIOIDPRESCRIPT,
                    cvs_soccap_county: p_result.county.socCap,
                    cvs_rtsocassoc_county: p_result.county.rtSocASSOC,
                    cvs_pcthouse_distress_county: p_result.county.pctHOUSE_DISTRESS,
                    
                
                    cvs_cnmrate_county: p_result.county.midwiveSrate,
                    cvs_isolation_county: p_result.county.segregation,
                    cvs_mdrate_county: p_result.county.pcPrate,
                    cvs_rtviolentcr_icpsr_county: p_result.county.rtVIOLENTCR,

                    cvs_pctrural : p_result.county.pctRural,
                    cvs_mhproviderrate :p_result.county.mhprovideRrate,
                    cvs_racialized_pov : p_result.county.racialized_pov

                    }

                    g_data.cvs.cvs_grid = [ new_grid_item ];
                }
            }

        },
        callback_cvs_data_error: function (p_result)
        {
            console.log(p_result);
            if
            (
                g_cvs_api_request_data.has("_id") &&
                g_cvs_api_request_data.get("_id") == g_data._id
            )
            {

                if(g_data.cvs == null)
                {
                    g_data.cvs = {
                        "cvs_used": "9999",
                        "cvs_used_how": "9999",
                        "cvs_used_other_sp": "",
                        "cvs_grid": []
                    }
                }

                g_cvs_api_request_data.set
                (
                    "cvs_api_request_result_message",
                    `status code: ${p_result.status} message: ${p_result.responseText}`
                ); 

                                        
                    
                const new_grid_item = {

                    cvs_api_request_url: g_cvs_api_request_data.get("cvs_api_request_url"),
                    cvs_api_request_date_time: g_cvs_api_request_data.get("cvs_api_request_date_time"),
                    cvs_api_request_c_geoid: g_cvs_api_request_data.get("cvs_api_request_c_geoid"),
                    cvs_api_request_t_geoid: g_cvs_api_request_data.get("cvs_api_request_t_geoid"),
                    cvs_api_request_year: g_cvs_api_request_data.get("cvs_api_request_year"),
                    cvs_api_request_result_message: g_cvs_api_request_data.get("cvs_api_request_result_message"),
                    cvs_mdrate_county: "",
                    cvs_pctnoins_fem_county: "",
                    cvs_pctnoins_fem_tract: "",
                    cvs_pctnovehicle_county: "",                                   
                    cvs_pctnovehicle_tract: "",
                    cvs_pctmove_county: "",
                    cvs_pctmove_tract: "",
                    cvs_pctsphh_county: "",
                    cvs_pctsphh_tract: "",
                    cvs_pctovercrowdhh_county: "",
                    cvs_pctovercrowdhh_tract: "",
                    cvs_pctowner_occ_county: "",
                    cvs_pctowner_occ_tract: "",
                    cvs_pct_less_well_county: "",
                    cvs_pct_less_well_tract: "",
                    cvs_ndi_raw_county: "",
                    cvs_ndi_raw_tract: "",
                    cvs_pctpov_county: "",
                    cvs_pctpov_tract: "",
                    cvs_ice_income_all_county: "",
                    cvs_ice_income_all_tract: "",
                    cvs_medhhinc_county: "",
                    cvs_medhhinc_tract: "",
                    cvs_pctobese_county: "",
                    cvs_fi_county: "",
                    cvs_cnmrate_county: "",
                    cvs_obgynrate_county: "",
                    cvs_rtteenbirth_county: "",
                    cvs_rtstd_county: "",
                    cvs_rtmhpract_county: "",
                    cvs_rtdrugodmortality_county: "",
                    cvs_rtopioidprescript_county: "",
                    cvs_soccap_county: "",
                    cvs_rtsocassoc_county: "",
                    cvs_pcthouse_distress_county: "",
                    cvs_rtviolentcr_icpsr_county: "",
                    cvs_isolation_county: "",
                    cvs_pctrural : "",
                    cvs_mhproviderrate : "",
                    cvs_racialized_pov : ""
                };

                g_data.cvs.cvs_grid = [ new_grid_item ];
            }
        },
        get_cvs_api_data_info: async function
        (
            c_geoid,
            t_geoid,
            year,
            p_success_call_back,
            p_error_call_back
        )
        {
            var base_url = `${location.protocol}//${location.host}/api/cvsAPI`



            g_cvs_api_request_data.set("_id", g_data._id);
            g_cvs_api_request_data.set("cvs_api_request_url", base_url);
            g_cvs_api_request_data.set("cvs_api_request_date_time", new Date());
            g_cvs_api_request_data.set("cvs_api_request_c_geoid", c_geoid);
            g_cvs_api_request_data.set("cvs_api_request_t_geoid", t_geoid);
            g_cvs_api_request_data.set("cvs_api_request_year", year);


            //g_cvs_api_request_data.set("cvs_api_request_result_message", 
           /* 
            await fetch
            (
                base_url,
                {
                    method: "POST",
                    headers: {"Content-type": "application/json;charset=UTF-8"},
                    body: JSON.stringify({
                        action: "data",                        
                        c_geoid: c_geoid,
                        t_geoid: t_geoid,
                        year: year

                    }),
                }
            )            
            .then(response => p_success_call_back(response)) 
            .catch(err => p_error_call_back(err));
            */

            try
            {
                await $.ajax(
                    {
                        url: base_url,
                        type: 'POST',
                        contentType: 'application/json; charset=utf-8',
                        dataType: 'json',
                        success: p_success_call_back,
                        error: p_error_call_back,
                        data: JSON.stringify({
                            action: "data",                        
                            c_geoid: c_geoid,
                            t_geoid: t_geoid,
                            year: year

                        })
                    }
                );
            }
            catch(ex)
            {
                // do nothing 
            }
        },
        get_cvs_api_dashboard_info: async function
        (
            lat,
            lon, 
            year,
            id,
            p_success_call_back,
            p_error_call_back
        )
        {           
            
            http://localhost:12345/community-vital-signs?lat=33.880577&lon=-84.29106&year=2012&id=GA-2012-1234



            var base_url = `${location.protocol}//${location.host}/community-vital-signs?lat=${lat}&lon=${lon}&year=${year}&id=${id}`

            window.open(base_url, target=id)
/*
            fetch
            (
                base_url,
                {
                    method: "POST",
                    body: JSON.stringify({
                        action: "dashboard",
                        lat: lat,
                        lon: lon, 
                        year: year,
                        id: id

                    }),
                }
            )
            .then(response => p_success_call_back(response)) 
            .catch(err => p_error_call_back(err));


            try
            {
                await $.ajax(
                    {
                        url: base_url,
                        type: 'POST',
                        contentType: 'application/json; charset=utf-8',
                        dataType: 'json',
                        success: p_success_call_back,
                        error: p_error_call_back,
                        data: JSON.stringify({
                            action: "dashboard",
                            lat: lat,
                            lon: lon, 
                            year: year,
                            id: id

                        })
                    }
                );
            }
            catch(ex)
            {
                // do nothing 
            }*/

        },

        get_cvs_api_server_info: async function
        (
            p_success_call_back,
            p_error_call_back
        )
        {
            var base_url = `${location.protocol}//${location.host}/api/cvsAPI`
/*
            fetch
            (
                base_url,
                {
                    method: "POST",
                    body: JSON.stringify({
                        action: "server",

                    }),
                }
            )
            .then(response => p_success_call_back(response)) 
            .catch(err => p_error_call_back(err));
*/
            try
            {
                await $.ajax(
                    {
                        url: base_url,
                        type: 'POST',
                        contentType: 'application/json; charset=utf-8',
                        dataType: 'json',
                        success: p_success_call_back,
                        error: p_error_call_back,
                        data: JSON.stringify({
                            action: "server"
                        })
                    }
                );
            }
            catch(ex)
            {
                // do nothing 
            }



        },
        get_geocode_info: function
        (
            p_street, 
            p_city, 
            p_state, 
            p_zip, 
            p_census_year,
            p_call_back_action
        )
        {
            
            let request = [];
			let state = p_state
			
			if(state)
			{
				let check_state = state.split("-");
				state = check_state[0];
			}

	
            var base_url = `${location.protocol}//${location.host}/api/tamuGeoCode?`

            request.push(base_url);
            request.push("streetAddress=")
            request.push(p_street);
            request.push("&city=");
            request.push(p_city);
            request.push("&state=");
            request.push(state);
            request.push("&zip=")
            request.push(p_zip);
            request.push("&census_year=")
            request.push(p_census_year);
            //request.push("&format=json&allowTies=false&tieBreakingStrategy=revertToHierarchy&includeHeader=true&census=true&censusYear=2010&notStore=true&version=4.01");

            let geocode_url = request.join("");

            $.ajax(
            {
                    url: geocode_url
            }
            ).done(function(response) 
            {
                
                let geo_data = null;
                
                const data = response;
                // set the latitude and logitude
                //death_certificate/place_of_last_residence/latitude
                //death_certificate/place_of_last_residence/longitude
                if
                (
                    data &&
                    data.featureMatchingResultType &&
					!(["Unmatchable","ExceptionOccurred", "0"].indexOf(data.featureMatchingResultType) > -1) &&                 
                    data.outputGeocodes &&
                    data.outputGeocodes.length > 0 &&
                    data.outputGeocodes[0].outputGeocode &&
                    data.outputGeocodes[0].outputGeocode.featureMatchingResultType &&
                    !(["Unmatchable","ExceptionOccurred"].indexOf(data.outputGeocodes[0].outputGeocode.featureMatchingResultType) > -1)
				)
                {
                    geo_data = { 
                            FeatureMatchingResultType: data.outputGeocodes[0].outputGeocode.featureMatchingResultType,
							FeatureMatchingGeographyType: data.outputGeocodes[0].outputGeocode.featureMatchingGeographyType,
							latitude: data.outputGeocodes[0].outputGeocode.latitude,
                            longitude: data.outputGeocodes[0].outputGeocode.longitude,
							NAACCRGISCoordinateQualityCode: data.outputGeocodes[0].outputGeocode.naaccrgisCoordinateQualityCode,
                            NAACCRGISCoordinateQualityType: data.outputGeocodes[0].outputGeocode.naaccrgisCoordinateQualityType,
                            NAACCRCensusTractCertaintyCode: data.outputGeocodes[0].censusValues[0].CensusValue1.naaccrCensusTractCertaintyCode,
                            NAACCRCensusTractCertaintyType: data.outputGeocodes[0].censusValues[0].CensusValue1.naaccrCensusTractCertaintyType,
							CensusCbsaFips: data.outputGeocodes[0].censusValues[0].CensusValue1.censusCbsaFips,
                            CensusCbsaMicro: data.outputGeocodes[0].censusValues[0].CensusValue1.censusCbsaMicro,
                            CensusStateFips: data.outputGeocodes[0].censusValues[0].CensusValue1.censusStateFips,
                            CensusCountyFips: data.outputGeocodes[0].censusValues[0].CensusValue1.censusCountyFips,
                            CensusTract: data.outputGeocodes[0].censusValues[0].CensusValue1.censusTract,
                            CensusMetDivFips: data.outputGeocodes[0].censusValues[0].CensusValue1.censusMetDivFips
							
                        };
                }
                
                p_call_back_action(geo_data);

            });
        },
        compose : function() 
        {
            //http://stackoverflow.com/questions/28821765/function-composition-in-javascript
            let funcs = arguments;

            return function() {
                let args = arguments;
                for (let i = funcs.length; i > 0; i--) {
                    args = [funcs[i].apply(this, args)];
                }
                return args[0];
                // var c = compose(trim, capitalize);
            }
        },
        mapEsprimaASt: function (object, f) 
        {
            let key, child;

                if (f.call(null, object) === false) {
                    return;
                }
                for (key in object) {
                    if (object.hasOwnProperty(key)) {
                        child = object[key];
                        if (typeof child === 'object' && child !== null) {
                            $mmria.mapEsprimaASt(child, f);
                        }
                    }
                }
        },
        get_url_components: function (url)
        {
            let parse_url = /^(?:([A-Za-z]+):)?(\/{0,3})([0-9.\-A-Za-z]+)(?::(\d+))?(?:\/([^?#]*))?(?:\?([^#]*))?(?:#(.*))?$/;
            //var url = 'http://www.ora.com:80/goodparts?q#fragment';
            let result = [];
            let array = parse_url.exec(url);
            let names = ['url', 'scheme', 'slash', 'host', 'port', 'path', 'query', 'hash'];
            
            let blanks = '       ';
            let i;
            for (i = 0; i < names.length; i += 1) 
            {
               result[names[i]] = array[i];
            }
            

            return result;
        },
        get_new_guid : function () 
        {
/*            
            function s4() 
            {
                return Math.floor((1 + $mmria.getRandomCryptoValue()) * 0x10000)
                .toString(16)
                .substring(1);
            }
            return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
                s4() + '-' + s4() + s4() + s4();
*/
            let d = new Date().getTime();//Timestamp
            let d2 = (performance && performance.now && (performance.now()*1000)) || 0;//Time in microseconds since page-load or 0 if unsupported
            return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) 
            {
                let r = $mmria.getRandomCryptoValue() * 16;//random number between 0 and 16
                if(d > 0)
                {//Use timestamp until depleted
                    r = (d + r)%16 | 0;
                    d = Math.floor(d/16);
                } 
                else 
                {//Use microseconds since page-load if supported
                    r = (d2 + r)%16 | 0;
                    d2 = Math.floor(d2/16);
                }
                return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
            });
        },

        addCookie: function (cookie_name,value,days)
        {
            var expires = "";
            if (days) 
            {
                var date = new Date();
                date.setTime(date.getTime() + (days*24*60*60*1000));
                expires = "; expires=" + date.toUTCString();
            }
            else
            {
                var minutes_12 = 12;
                var current_date_time = new Date();
                var new_date_time = new Date(current_date_time.getTime() + minutes_12 * 60000);
                expires = "; expires=" + new_date_time.toGMTString();

            }
            document.cookie = cookie_name + "=" + value + expires + "; path=/";
        },
        getCookie: function (name)
        {
            var nameEQ = name + "=";
            var ca = document.cookie.split(';');
            for(var i=0; i < ca.length; i++) 
            {
                var c = ca[i];
                while (c.charAt(0)==' ') c = c.substring(1,c.length);
                if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length,c.length);
            }
            return null;
        },
        removeCookie: function (name) {
            $mmria.addCookie(name,"",-1);
        },
        set_control_value: function(p_dictionary_path, p_value, p_form_index, p_grid_index)
        {
            let form_string = "";
            let grid_string = "";

            let jq = [ '[dpath="' + p_dictionary_path + '"]' ];

            if(p_form_index != null)
            {
                jq.push('[form_index="' + p_form_index +  '"]');
            }

            if(p_grid_index != null)
            {
                jq.push('[grid_index="' + p_grid_index +  '"]');
            }
            var control = document.querySelector(jq.join(""));
            if(control != null)
            {
                control.value = p_value;
            }
            else
            {
                //console.log("control not found: " + p_dictionary_path);
            }
        },
        set_enable: function(p_dictionary_path, p_form_index, p_grid_index)
        {
            let form_string = "";
            let grid_string = "";

            let jq = [ '[dpath="' + p_dictionary_path + '"]' ];

            if(p_form_index != null)
            {
                jq.push('[form_index="' + p_form_index +  '"]');
            }

            if(p_grid_index != null)
            {
                jq.push('[grid_index="' + p_grid_index +  '"]');
            }
            var control = document.querySelector(jq.join(""));
            if(control != null)
            {
                control.disabled = false;
            }
            else
            {
                //console.log("control not found: " + p_dictionary_path);
            }
        },
        set_disable: function(p_dictionary_path, p_form_index, p_grid_index)
        {
            let form_string = "";
            let grid_string = "";

            let jq = [ '[dpath="' + p_dictionary_path + '"]' ];

            if(p_form_index != null)
            {
                jq.push('[form_index="' + p_form_index +  '"]');
            }

            if(p_grid_index != null)
            {
                jq.push('[grid_index="' + p_grid_index +  '"]');
            }
            var control = document.querySelector(jq.join(""));
            if(control != null)
            {
                control.disabled = true;
            }
            else
            {
                //console.log("control not found: " + p_dictionary_path);
            }
        },
        set_control_visibility: function(p_element_id, p_value)
        {
            switch(p_value.toLowerCase())
            {
                case "none":
                case "block":
                var control = document.getElementById(p_element_id);
                if(control != null)
                {
                    control.style.display = p_value;
                }
                break;
            }
        },
        save_current_record: function(p_call_back, p_note)
        {
            if (g_is_data_analyst_mode != null) 
            {
                return;
            }

            let save_case_request = { 
                Change_Stack:{
                    _id: $mmria.get_new_guid(),
                    case_id: g_data._id,
                    case_rev: g_data._rev,
                    date_created: new Date().toISOString(),
                    user_name: g_user_name, 
                    items: g_change_stack,
                    metadata_version: g_release_version,
                    note: (p_note != null)? p_note : ""
    
                },
                Case_Data:g_data
            };
    
        if(g_case_narrative_is_updated)
        {
            save_case_request.Change_Stack.items.push({
                _id: g_data._id,
                _rev: g_data._rev,
              object_path: "g_data.case_narrative.case_opening_overview",
              metadata_path: "/case_narrative/case_opening_overview",
              old_value: g_case_narrative_original_value,
              new_value: g_data.case_narrative.case_opening_overview,
              dictionary_path: "/case_narrative/case_opening_overview",
              metadata_type: "textarea",
              prompt: 'Case Narrative',
              date_created: g_case_narrative_is_updated_date.toISOString(),
              user_name: g_user_name
            });
        }


            $.ajax({
                url: location.protocol + '//' + location.host + '/api/case',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                data: JSON.stringify(save_case_request),
                type: "POST"
            })
            .done(function(case_response) 
            {
                console.log("$mmria.save_current_record: success");
        
                g_change_stack = [];
                g_case_narrative_is_updated = false;
                g_case_narrative_is_updated_date = null;
        
                if(g_data && g_data._id == case_response.id)
                {
                    g_data._rev = case_response.rev;
                    set_local_case(g_data);
                    //console.log('set_value save finished');
                }
        
                
                if(case_response.auth_session)
                {
                    set_session_warning_interval();
                }
        
                if(p_call_back)
                {
                    p_call_back();
                }
        
        
            }).fail(function(xhr, err) { console.log("$mmria.save_current_record: failed", err); });
          
            
        },
        get_current_multiform_index: function ()
        {
            let result = parseInt(window.location.href.substr(window.location.href.lastIndexOf("/") + 1,window.location.href.length - (window.location.href.lastIndexOf("/") + 1)));
            
            return result;
        },
        getRandomCryptoValue: function () 
        {
            let crypto = window.crypto || window.msCrypto; // handles Internet Explorer
            return crypto.getRandomValues(new Uint32Array(1))[0]  / 0xffffffff;
        },
        show_confirmation_dialog: function(p_confirm_call_back, p_cancel_call_back)
        {
            const dialog_div = $("#mmria_dialog");
            
            dialog_div.dialog
            ({
              autoOpen: false,
              closeText: '×',
              closeOnEscape: false,
              draggable: false,
              width: 600,
              modal: true,
              zIndex: 999,
              buttons : {
                'Confirm' : function() {
                  $(this).dialog('close');
                  p_confirm_call_back();
                },
                'Cancel' : function() {
                  $(this).dialog('close');
                  p_cancel_call_back();
                }
              },
              classes: {
                'ui-dialog-titlebar': 'modal-header bg-primary',
                'ui-dialog-buttonpane': 'modal-footer'
              },
              open: function(event, ui) {
                $(event.target).parent().css({
                  'position': 'fixed',
                  'left': '50%',
                  'top': '40%',
                  'transform': 'translate3d(-50%, -50%, 0)',
                  'z-index': '999',
                });

                $(event.target).find('.ui-dialog-buttonpane').hide();

                $('.ui-widget-overlay').bind('click', function () {
                  dialog_div.dialog('close');
                });
/*
                const currentHash = location.hash;
                $(window).bind('hashchange', function () {
                  if (currentHash !== location.hash) {
                    dialog_div.dialog('close');
                  }
                });*/
              }
            });

            $('.confirmLink').unbind('click');
            $('.cancelLink').unbind('click');
        
            $(".confirmLink").click
            (
                function(event) 
                {
                    event.preventDefault();
                    $("#mmria_dialog").dialog("close");
                    p_confirm_call_back();
                }
            );

            $(".cancelLink").click
            (
                function(event) 
                {
                    event.preventDefault();
                    $("#mmria_dialog").dialog("close");
                    p_cancel_call_back();
                }
            );
            

            // let dialog = document.getElementById('mmria_dialog');
            // dialog.style.top = ((window.innerHeight/2) - (dialog.offsetHeight/2))+'px';
            // dialog.style.left = ((window.innerWidth/2) - (dialog.offsetWidth/2))+'px';
            // $(".ui-dialog").css("z-index",10);
            
            $("#mmria_dialog").dialog("open");
            // $(".ui-dialog-titlebar")[0].children[0].style="background-color:silver";
        },
        info_dialog_show: function (p_title, p_header, p_inner_html, p_message_type = "default")
        {
            // var publishedAlertTypeStyling= [];
            
            // if (p_message_type == "information")
            //     publishedAlertTypeStyling = ["info-banner", "cdc-icon-alert_01 refresh-icon", "primary-button"]
            // else if (p_message_type == "warning")
            //     publishedAlertTypeStyling = ["info-banner", "cdc-icon-alert_02 warning-icon", "primary-button"]
            // else if (p_message_type == "error")
            //     publishedAlertTypeStyling = ["info-banner", "cdc-icon-close-circle error-icon", "primary-button"]
            // else
            //     publishedAlertTypeStyling = ["info-banner", "cdc-icon-close-circle refresh-icon", "primary-button"]
            function get_header()
            {
                switch(p_message_type.toLowerCase())
                {
                    case "information":
                        return `<div class="align-items-center justify-content-start modal-header ui-widget-header ui-helper-clearfix">
                        <div id="ui-id-1" class="ui-dialog-title modal-header">${p_title}</div>
                        <span id="ui-id-1-close" tabindex="0" role="button" onkeypress="info_dialog_keyboard_click(event)" onclick="$mmria.info_dialog_click()" title="close" class="ml-auto x24 cdc-icon-times-solid"></span>
                    </div>`
                    break;
                    case "warning":
                        return `<div class="align-items-center justify-content-start  modal-header ui-widget-header ui-helper-clearfix">
                        <div id="ui-id-1" class="ui-dialog-title modal-header">${p_title}</div>
                        <span id="ui-id-1-close" tabindex="0" role="button" onkeypress="info_dialog_keyboard_click(event)" onclick="$mmria.info_dialog_click()" title="close" class="ml-auto x24 cdc-icon-times-solid"></span>
                    </div>`
                    break;
                    case "error":
                        return `<div class="align-items-center justify-content-start modal-header ui-widget-header ui-helper-clearfix">
                        <div id="ui-id-1" class="ui-dialog-title">${p_title}</div>
                        <span id="ui-id-1-close" tabindex="0" role="button" onkeypress="info_dialog_keyboard_click(event)" onclick="$mmria.info_dialog_click()" title="close" class="ml-auto x24 cdc-icon-times-solid"></span>
                    </div>`
                    break;
                    default:
                        return `<div class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix">
                        <div id="ui-id-1" class="ui-dialog-title">${p_title}</div>
                        <span id="ui-id-1-close" tabindex="0" role="button" onkeypress="info_dialog_keyboard_click(event)" onclick="$mmria.info_dialog_click()" title="close" class="ml-auto x24 cdc-icon-times-solid"></span>
                    </div>`
                    break;
                }
            }
        let element = document.getElementById("case-progress-info-id");
            if(element == null)
            {
                element = document.createElement("dialog");
                element.classList.add('p-0');
                element.classList.add('set-radius');
                element.classList.add('card-container-dark');
                element.classList.add('col-md-4');
                element.setAttribute("id", "case-progress-info-id");
                element.setAttribute("aria-modal", "true");
                document.firstElementChild.appendChild(element);
            }
            let html = [];
            html.push(`
                ${get_header()}
                <div id="mmria_dialog2" style="width: auto; min-height: 101px; max-height: none; height: auto;" class="ui-dialog-content ui-widget-content">
                    <div class="modal-body">
                        <p><strong>${p_header}</strong></p>
                        ${p_inner_html}
                    </div>
                    <footer class="modal-footer">
                        <button id="mmria_dialog2_close" class="btn primary-button mr-1" onclick="$mmria.info_dialog_click()">OK</button>
                    </footer>
                </div>
            `);
            // html.push(`<h3 class="mt-0">${p_title}</h3>`);
            // html.push(`<p><strong>${p_header}</p>`);
            // html.push(`${p_inner_html}`);
            // html.push('<button class="btn btn-primary mr-1" onclick="$mmria.info_dialog_click()">OK</button>');
            element.innerHTML = html.join("");
            mmria_pre_modal("case-progress-info-id");
            element.showModal();     
            document.getElementById("ui-id-1-close").addEventListener('keyup', (e) => {
              if(e.key === "Enter")
              {
                e.preventDefault();
                info_dialog_click();
              }
            });
        },
        info_dialog_keyboard_click: function(event)
        {
          if(event.key === "Enter")
          {
            mmria_post_modal();
            let el = document.getElementById("case-progress-info-id");
            el.close();
          }
        },
        info_dialog_click: function ()
        {
            mmria_post_modal();
            let el = document.getElementById("case-progress-info-id");
            el.close();
        },
        confirm_dialog_show: function (p_title, p_header, p_inner_html, p_confirm_dialog_confirm_callback, p_confirm_dialog_cancel_callback)
        {
            let element = document.getElementById("confirm-dialog-id");
            if(element == null)
            {
                element = document.createElement("dialog");
                element.classList.add('p-0');
                element.classList.add('set-radius');
                element.setAttribute("id", "confirm-dialog-id");

                document.firstElementChild.appendChild(element);
            }
            
            let html = [];
            html.push(`
                <div class="justify-content-start modal-header bg-primary ui-widget-header ui-helper-clearfix">
                    <span id="ui-id-1" class="ui-dialog-title">${p_title}</span>
                    <button id="modal_confirm_cancel_icon"="button" class="ml-auto ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="close info dialog" onclick="$mmria.confirm_dialog_confirm_close()"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
                </div>
                <div id="mmria_dialog3" style="width: auto; min-height: 101px; max-height: none; height: auto;" class="ui-dialog-content ui-widget-content">
                    <div class="modal-body">
                        <p><strong>${p_header}</strong></p>
                        ${p_inner_html}
                    </div>
                    <footer class="modal-footer">
                        <button id="confirm-dialog-id-confirm-button" class="btn btn-primary mr-1" >Yes, change my selection</button> 
                        <button id="confirm-dialog-id-cancel-button"  class="btn modal-cancel btn-outline-secondary  mr-1" >Cancel</button>
                    </footer>
                </div>
            `);
            
            element.innerHTML = html.join("");

            let confirm_button = document.getElementById("confirm-dialog-id-confirm-button");
            let canel_button = document.getElementById("confirm-dialog-id-cancel-button");
            let modal_confirm_cancel_icon = document.getElementById("modal_confirm_cancel_icon");

            
            confirm_button.onclick =  p_confirm_dialog_confirm_callback;
            canel_button.onclick = p_confirm_dialog_cancel_callback;
            modal_confirm_cancel_icon.onclick = p_confirm_dialog_cancel_callback;

            mmria_pre_modal("confirm-dialog-id");

            element.showModal();
        },
        confirm_dialog_confirm_close: function ()
        {
            mmria_post_modal();
            let el = document.getElementById("confirm-dialog-id");
            if(el != null)
                el.close();
        },

        confirm_external_nav_dialog_show: function 
        (
            p_confirm_dialog_confirm_callback, 
            p_confirm_dialog_cancel_callback
        )
        {
            let element = document.getElementById("confirm-external-nav-dialog-id");
            if(element == null)
            {
                element = document.createElement("dialog");
                element.classList.add(...['p-0', 'set-radius', 'card-container-dark', 'col-md-4']);
                element.setAttribute("id", "confirm-external-nav-dialog-id");
                element.setAttribute("role", "dialog");

                document.firstElementChild.appendChild(element);
            }


            let html = [];
            html.push(`
                <div class="ui-dialog-titlebar modal-header ui-widget-header ui-helper-clearfix">
                    <div id="ui-id-1" class="ui-dialog-title">Exit Notification / Disclaimer Policy</div>
                    <!--span title="close exit confirm dialog" id="modal_confirm_external_cancel_icon"="button" tabindex="0" role="button" onclick="$mmria.confirm_dialog_confirm_close()" title="close" class="ml-auto x24 fill-p cdc-icon-times-solid" style="cursor:pointer;text-align:center;padding-left:0px;margin:5px">X&nbsp;&nbsp;&nbsp;&nbsp;</span-->
                </div>
                <div id="mmria_dialog4" style="width: auto; min-height: 101px; max-height: none; height: auto;" class="ui-dialog-content ui-widget-content">
                    <div class="modal-body card-content">
                        <p><strong>Links with this icon<span class="sr-only">external icon</span>
                        <span class="fi cdc-icon-external x16 fill-external" aria-hidden="true"></span> indicate that you are leaving the CDC website.</strong></p>
                        <ul>
                            <li>The Centers for Disease Control and Prevention (CDC) cannot attest to the accuracy of a non-federal website.</li>
                            <li>Linking to a non-federal website does not constitute an endorsement by CDC or any of its employees of the sponsors or the information and products presented on the website.</li>
                            <li>You will be subject to the destination website's privacy policy when you follow the link.</li>
                            <li>CDC is not responsible for Section 508 compliance (accessibility) on other federal or private website.</li>
                        </ul>
                        <p>For more information on CDC's web notification policies, see <a href="https://www.cdc.gov/Other/disclaimer.html" target="_blank">Website Disclaimers</a>.</p>
                    </div>
                    <footer class="modal-footer">
                        <button id="confirm-external-nav-dialog-id-cancel-button"  class="btn modal-cancel btn-outline-secondary  mr-1" autofocus >Cancel</button>
                        <button id="confirm-external-nav-dialog-id-confirm-button" class="btn primary-button mr-1" >Continue</button> 
                    </footer>
                </div>
            `);
            
            element.innerHTML = html.join("");

            element.style.top = ((window.innerHeight/2) - (element.offsetHeight/2))+'px';
            //element.style.left = ((window.innerWidth/2) - (element.offsetWidth/2))+'px';
            

            let confirm_button = document.getElementById("confirm-external-nav-dialog-id-confirm-button");
            let canel_button = document.getElementById("confirm-external-nav-dialog-id-cancel-button");
            //let modal_confirm_cancel_icon = document.getElementById("modal_confirm_external_cancel_icon");

            
            confirm_button.onclick =  p_confirm_dialog_confirm_callback;
            canel_button.onclick = p_confirm_dialog_cancel_callback;
            //modal_confirm_cancel_icon.onclick = p_confirm_dialog_cancel_callback;


            mmria_pre_modal("confirm-external-nav-dialog-id");

            element.showModal();
        },
        confirm_external_nav_dialog_confirm_close: function ()
        {
            mmria_post_modal();
            let el = document.getElementById("confirm-external-nav-dialog-id");
            el.close();
        },
        confirm_user_delete_dialog_show: function 
        (
            p_user_id,
            p_rev,
            p_confirm_dialog_confirm_callback, 
        )
        {
            let element = document.getElementById("confirm-user-delete-dialog-id");
            if(element == null)
            {
                element = document.createElement("dialog");
                element.classList.add(...['p-0', 'set-radius', 'card-container-dark', 'col-md-4']);
                element.setAttribute("id", "confirm-user-delete-dialog-id");
                element.setAttribute("role", "dialog");

                document.firstElementChild.appendChild(element);
            }


            let html = [];
            html.push(`
                <div class="ui-dialog-titlebar modal-header ui-widget-header ui-helper-clearfix">
                    <div id="ui-id-1" class="ui-dialog-title">Delete User Confirmation</div>
                    <button id="cancel-user-delete-button" type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="×" onclick="$mmria.confirm_user_delete_dialog_close()"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
                </div>
                <div id="mmria_dialog4" style="width: auto; min-height: 101px; max-height: none; height: auto;" class="ui-dialog-content ui-widget-content">
                    <div class="modal-body card-content">
                        <p>Are you sure want to delete: <b class="text-danger">${p_user_id.split(":")[1]}</b>?</p>
                        <span id="${p_user_id}_status" role="status" class="mr-2 spinner-container spinner-content spinner-sm">
                            <span style="background-color: transparent !important;" class="spinner-body text-primary">
                                <span class="spinner"></span>
                                <span class="sr-only">Deleting User...</span>
                            </span>
                        </span>
                    </div>
                    <footer class="modal-footer">
                        <button onclick="$mmria.confirm_user_delete_dialog_close()" id="confirm-user-delete-dialog-id-cancel-button" class="btn secondary-button mr-1" autofocus >Cancel</button>
                        <button id="confirm-user-delete-dialog-id-confirm-button" class="btn delete-button mr-1" >Delete user</button> 
                    </footer>
                </div>
            `);
            
            element.innerHTML = html.join("");

            element.style.top = ((window.innerHeight/2) - (element.offsetHeight/2))+'px';
            //element.style.left = ((window.innerWidth/2) - (element.offsetWidth/2))+'px';

            let confirm_button = document.getElementById("confirm-user-delete-dialog-id-confirm-button");
            let cancel_button = document.getElementById("confirm-user-delete-dialog-id-cancel-button");
            //let modal_confirm_cancel_icon = document.getElementById("modal_confirm_external_cancel_icon");

            confirm_button.onclick =  () => {
                p_confirm_dialog_confirm_callback(p_user_id, p_rev);
            };
            //modal_confirm_cancel_icon.onclick = p_confirm_dialog_cancel_callback;


            mmria_pre_modal("confirm-user-delete-dialog-id");

            element.showModal();
        },
        confirm_user_delete_dialog_close: function ()
        {
            mmria_post_modal();
            let el = document.getElementById("confirm-user-delete-dialog-id");
            el.close();
        },
        get_year_and_quarter: function (p_value)
        {
            let result = null;
            
            if
            (
                p_value != null &&
                p_value != ""
            )
            {
                if
                (
                    p_value instanceof Date && 
                    !isNaN(p_value.valueOf()) &&
                    p_value.getFullYear() >= 1900 &&
                    p_value.getFullYear() <= 2100
                )
                {
                    result = `Q${Math.floor((p_value.getMonth() / 3) + 1)}-${p_value.getFullYear()}`;
                }
                else 
                {
                    let date = new Date(p_value);
                 
                    if
                    (
                        date instanceof Date && 
                        !isNaN(date.valueOf())
                        && date.getFullYear() >= 1900 &&
                        date.getFullYear() <= 2100
                    )
                    {
                        result = `Q${Math.floor((date.getMonth() / 3) + 1)}-${date.getFullYear()}`;
                    }
                }
            }

            return result;
        },
        data_dictionary_dialog_show: async function (p_title, p_inner_html)
        {

            let element = document.getElementById("dictionary-lookup-id");
                if(element == null)
                {
                    element = document.createElement("dialog");
                    element.classList.add('p-0');
                    element.classList.add('set-radius');
                    element.setAttribute("id", "dictionary-lookup-id");
                    element.setAttribute("role", "dialog");
    
                    document.firstElementChild.appendChild(element);
                }

                element.style.maxWidth = "1024px";
                element.style.transform = "translateY(0%)";
                element.style.maxHeight = "600px";
                element.style.overflow = "hidden";
    
                let html = [];
                html.push(`
                    <div aria-modal="true" class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix">
                        <span id="ui-id-1" class="ui-dialog-title" style="font-family: 'Open-Sans';">${p_title.length > 100 ? p_title.slice(0,97) + "..." : p_title}</span>
                        <button type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="×" onclick="$mmria.data_dictionary_dialog_click()"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
                    </div>
                    <div id="mmria_dialog5" style="overflow-y: scroll;width: 1000; height: 500px;" class="ui-dialog-content ui-widget-content">
                        <div class="modal-body">
                            <table class="table table--standard rounded-0 mb-3" style="font-size: 14px"  >
                                <tr class="tr bg-gray-l1 font-weight-bold">
                                    <th class="th" width="140" scope="col">MMRIA Form</th>
                                    <th class="th" width="140" scope="col">Export File Name</th>
                                    <th class="th" width="120" scope="col">Export Field</th>
                                    <th class="th" width="180" scope="col">Prompt</th>
                                    <th class="th" width="380" scope="col">Description</th>
                                    <th class="th" width="260" scope="col">Path</th>
                                    <th class="th" width="110" scope="col">Data Type</th>
                                </tr>
                                ${p_inner_html}
                            </table>
                        </div>

                    </div>
                    <div>
                    <footer class="modal-footer">
                        <button id="data_dictionary_dialog_close_button" class="btn btn-primary mr-1" onclick="$mmria.data_dictionary_dialog_click()" style="font-family: 'Open-Sans';">Close</button>
                    </footer>
                    </div>
                `);
    
                element.innerHTML = html.join("");

                mmria_pre_modal("dictionary-lookup-id");
                
                window.setTimeout(()=> { const data_dictionary_dialog_close_button = document.getElementById("data_dictionary_dialog_close_button"); data_dictionary_dialog_close_button.focus(); }, 0);
                element.showModal();
        },
        data_dictionary_dialog_click: function ()
        {
            mmria_post_modal();
            let el = document.getElementById("dictionary-lookup-id");
            el.close();
        },
        committee_description_dialog_show: async function (p_title, p_inner_html)
        {
            let element = document.getElementById("dictionary-lookup-id");
                if(element == null)
                {
                    element = document.createElement("dialog");
                    element.classList.add('p-0');
                    element.classList.add('set-radius');
                    element.setAttribute("id", "dictionary-lookup-id");
                    element.setAttribute("aria-modal", "true");
                    element.setAttribute("aria-describedby", "mmria-dialog5");
                    element.setAttribute("aria-labelledby", "ui-id-1");
                    document.firstElementChild.appendChild(element);
                }

                element.style.maxWidth = "1024px";
                element.style.transform = "translateY(0%)";
                element.style.maxHeight = "600px";
                element.style.overflow = "hidden";
    
                let html = [];
                html.push(`
                    <div class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix">
                        <span id="ui-id-1" class="ui-dialog-title" style="font-family: 'Open-Sans';">${p_title.length > 100 ? p_title.slice(0,97) + "..." : p_title}</span>
                        <button id="${p_title}-button" type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="×" onclick="$mmria.data_dictionary_dialog_click()"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
                    </div>
                    <div id="mmria_dialog5" style="overflow-y: scroll;width: 1000; height: 250px;" class="ui-dialog-content ui-widget-content">
                        <div class="modal-body">
                            
                                ${p_inner_html}
                            
                        </div>

                    </div>
                    <div>
                    <footer class="modal-footer">
                        <button id="committee_description_dialog_click" class="btn btn-primary mr-1" onclick="$mmria.data_dictionary_dialog_click()" style="font-family: 'Open-Sans';">Close</button>
                    </footer>
                    </div>
                `);
    
                element.innerHTML = html.join("");

                mmria_pre_modal("dictionary-lookup-id");
                
                window.setTimeout(()=> { const committee_description_dialog_button = document.getElementById("committee_description_dialog_click"); committee_description_dialog_button.focus(); }, 0);
                element.showModal();
                document.getElementById(`${p_title}-button`).focus();
        },
        committee_description_dialog_click: function ()
        {
            mmria_post_modal();
            let el = document.getElementById("dictionary-lookup-id");
            el.close();
        },
        converter_calculater_dialog_show: async function ()
        {
            let element = document.getElementById("converter-calculater-id");
                if(element == null)
                {
                    element = document.createElement("dialog");
                    element.classList.add('p-0');
                    element.classList.add('set-radius');
                    element.setAttribute("id", "converter-calculater-id");
                    element.setAttribute("role", "dialog");
                    element.setAttribute("aria-label","conversion calculator");
    
                    document.firstElementChild.appendChild(element);
                }

                element.style.width = "400px";
                element.style.transform = "translateY(0%)";
                element.style.height = "555px";
                element.style.overflow = "hidden";
    
                const html = [];
                html.push(`
                    <div aria-modal="true" class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix">
                        <span id="ui-id-1" class="ui-dialog-title">Conversion Calculator</span>
                        <button type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="×" onclick="$mmria.converter_calculater_dialog_click()"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
                    </div>
                    <div id="mmria_dialog6" style="width: 300; height: 500px;" class="ui-dialog-content ui-widget-content">
                        <div class="modal-body" style="padding-left:12px;">
                                <div style="width: 377px;
                                height: 480px;
                                padding: 2px 2px 2px 0px;
                                border-radius: 4px;
                                border: 1px solid #bdbdbd;
                                font-family: 'Open Sans';
                                background-color: #f7f2f7;
                                box-sizing: border-box;">
                                <div id="values-limited-to-1"  style="text-align:left;">Input values are limited to 0.00 - 1000.00</div>
                                <br/>
                                    <div style="padding:2px"><b>Height</b><br/>
                                        <table style="padding:2px 2px 2px 2px">
                                            <tr style="padding:2px">
                                                <td><input id="cc_cm" type="text" value="" max="1000" min="1" onchange="cc_render_convert();" style="width:100px;" /> <label for="cc_cm">cm</label></td>
                                                <td>&lt;-- <b>convert</b> --&gt; </td>
                                                <td><input id="cc_in" type="text" value="" max="1000" min="1" onchange="cc_render_convert();" style="width:100px;" /> <label for="cc_in">in</label></td>
                                            </tr>
                                            <tr style="padding:2px">
                                                <td><input id="cc_m" type="text" value="" max="1000" min="1" onchange="cc_render_convert();" style="width:100px;" /> <label for="cc_m">m</label></td>
                                                <td>&lt;-- <b>convert</b> --&gt;</td>
                                                <td><input id="cc_ft" type="text" value="" max="1000" min="1" onchange="cc_render_convert();" style="width:100px;" /> <label for="cc_ft">ft</label></td>
                                            </tr>
                                        </table>
                                    </div>

                                    <hr/>
                                    <br/>
                                    <div style="padding:2px"><b>Weight</b><br/>
                                        <table style="padding:2px">
                                            <tr style="padding:2px">
                                            <td><input id="cc_kg" type="text" value="" max="1000" min="1" onchange="cc_render_convert();" style="width:100px;" /> <label for="cc_kg">kg</label></td>
                                                <td>&lt;-- <b>convert</b> --&gt; </td>
                                                <td><input id="cc_lbs" type="text" value="" max="1000" min="1" onchange="cc_render_convert();" style="width:100px;" /> <label for="cc_lbs">lbs</label></td>
                                                
                                            </tr>
                                            
                                            <tr style="padding:2px">
                                            <td><input id="cc_g" type="text" value="" max="1000" min="1" onchange="cc_render_convert();" style="width:100px;" /> <label for="cc_g">g</label></td>
                                                <td>&lt;-- <b>convert</b> --&gt; </td>
                                                <td><input id="cc_oz" type="text" value="" max="1000" min="1" onchange="cc_render_convert();" style="width:100px;" />&nbsp;<label for="cc_oz">oz</label></td>
                                                
                                            </tr>
                                        </table>
                                    </div>
                                    <hr/>
                                    <br/>
                                    <div style="padding:2px"><b>Temperature</b><br/>
                                        <div style="padding:2px">
                                            <div><input id="cc_f" type="text" value="" max="1000" min="1" onchange="cc_render_convert();" style="width:100px;" /><label for="cc_f"><sup>o</sup>F</label> &lt;-- <b>convert</b> --&gt; <input id="cc_c" type="text" value="" max="1000" min="1" onchange="cc_render_convert();" style="width:100px;" /> <label for="cc_c"><sup>o</sup>C</label></div>
                                        </div>
                                    </div>
                                    <br/><br/>
                                    <div style="text-align:right;padding-right: 8px;">
                                        <input id="cc_reset" class="btn btn-primary m1-3" type="button" value="Reset" onclick="cc_reset_clicked()" />
                                        <input id="cc_convert" class="btn btn-primary m1-3" type="button" value="Convert" onclick="cc_convert_clicked()" style="border: 1px solid #bdbdbd;" />
                                        <br/><br/>
                                        <div id="cc_reset_message"style="padding:2px 5px 2px 2px;text-align:left;font-size:14px">Please reset fields to perform another conversion.</div>
                                    </div>

                                </div>
                        
                        </div>

                    </div>
                    <!--div>
                        <footer class="modal-footer">
                            <button class="btn btn-primary mr-1" onclick="$mmria.converter_calculater_dialog_click()">Close</button>
                        </footer>
                    </div-->
                `);
    
                element.innerHTML = html.join("");

                mmria_pre_modal("converter-calculater-id");

                window.setTimeout(()=> { cc_main(); }, 0);
    
                element.showModal();
                
        },
        converter_calculater_dialog_click: function ()
        {
            //document.removeEventListener('keydown',cc_onKeyDown);

            mmria_post_modal();
            let el = document.getElementById("converter-calculater-id");
            el.close();
        },
        pin_un_pin_dialog_show: async function (p_case_id, p_is_pin)
        {
            const Title_Text = [];
            const Button_Text = [];
            const Description_Text = [];
            const Button_Event = [];
            const Button_style = [];

            if(p_is_pin)
            {
                Title_Text.push("Pin Case Options");
                Button_Text.push("Pin For Everyone");
                Button_Text.push("Pin For Me Only");
                Description_Text.push("Would you like to pin this case for all abstractors in this jurisdiction or pin this case only on your account?");
                
                
                Button_Event.push(`mmria_pin_case_click('${p_case_id}', true)`);
                Button_Event.push(`mmria_pin_case_click('${p_case_id}', false)`);
                Button_style.push(`style="height: 38px;
                padding-left: 12px;
                padding-right: 12px;
                border-radius: 4px;
                border: 1px solid #712177;
                background-color: #712177;
                box-sizing: border-box;
                font-family: 'Open Sans', sans-serif;
                color: rgba(255, 255, 255, 1);
                text-align: center;
                line-height: normal;
                cursor: pointer;"`);
                
            }
            else
            {
                Title_Text.push("Unpin Case for All");
                Button_Text.push("Cancel");
                Button_Text.push("Unpin For Everyone");
                Description_Text.push("Are you sure you want to unpin this case for all users in this jurisdiction?");
                
                //Description_Event.push(`mmria_un_pin_case_click(${p_case_id}, p_is_everyone)`);

                
                Button_Event.push(`$mmria.pin_un_pin_dialog_click()`);
                Button_Event.push(`mmria_un_pin_case_click('${p_case_id}', true)`);
                Button_style.push(`style="  width: 89px;
                height: 38px;
                padding-left: 12px;
                padding-right: 12px;
                border-radius: 4px;
                border: 1px solid #797979;
                background-color: #ffffff;
                color: #000000;
                box-sizing: border-box;
                font-family: 'Open Sans', sans-serif;
                color: #333333;
                text-align: center;
                line-height: normal;
                cursor: pointer;"`);
            }

            let element = document.getElementById("pin-unpin-id");
                if(element == null)
                {
                    element = document.createElement("dialog");
                    element.classList.add('p-0');
                    element.classList.add('set-radius');
                    element.setAttribute("id", "pin-unpin-id");
                    element.setAttribute("aria-modal", "true");
                    element.setAttribute("role","dialog");
    
                    document.firstElementChild.appendChild(element);
                }

                element.style.width = "500px";
                element.style.transform = "translateY(0%)";

    
                let html = [];
                html.push(`
                    <div class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix" role="dialog">
                        <span id="ui-id-1" class="ui-dialog-title">${Title_Text[0]}</span>
                        <button type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="×" onclick="$mmria.pin_un_pin_dialog_click()"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
                    </div>
                    <div id="mmria_dialog7" style="width: 300; height: 200px;" class="ui-dialog-content ui-widget-content" role="dialog">
                        <div class="modal-body">
                                <div >
                       ${Description_Text[0]}
                       <br/><br/>
                                    <div style="text-align:right;padding-right: 8px;">
                                        <input id="pin_for_everyone_button" class="btn-primary" type="button" value="${Button_Text[0]}" onclick="${Button_Event[0]}" ${Button_style[0]}/>
                                        <input id="pin_for_me" class="btn-primary" type="button" value="${Button_Text[1]}" onclick="${Button_Event[1]}" style="height: 38px;
                                        padding-left: 12px;
                                        padding-right: 12px;
                                        border-radius: 4px;
                                        border: 1px solid #712177;
                                        background-color: #712177;
                                        box-sizing: border-box;
                                        font-family: 'Open Sans', sans-serif;
                                        color: rgba(255, 255, 255, 1);
                                        text-align: center;
                                        line-height: normal;
                                        cursor: pointer;"/>
                                        
            
                                    </div>

                                </div>
                        
                        </div>

                    </div>

                `);
    
                element.innerHTML = html.join("");

                mmria_pre_modal("pin-unpin-id");

                window.setTimeout(()=> { const pin_for_me = document.getElementById("pin_for_me"); pin_for_me.focus(); }, 0);
    
                element.showModal();
                
        },
        pin_un_pin_dialog_click: function ()
        {
            mmria_post_modal();
            const el = document.getElementById("pin-unpin-id");
            if(el != null)
                el.close();
        },
        duplicate_multiform_dialog_show: async function 
        (
            p_object_path, 
            p_metadata_path, 
            p_index
        )
        {

            const dialog_id = "multiform-dialog-id";
            //g_duplicate_record_item(p_object_path, p_metadata_path, p_index) 

            const Title_Text = [];
            const Button_Text = [];
            const Description_Text = [];
            const Button_Event = [];
            const Button_style = [];

    
            Title_Text.push("Confirm Record Duplication");
            //Button_Text.push("Cancel");
            Button_Text.push("Duplicate Record");
            Description_Text.push
            (`
A duplicate of this record will be added:
<br/>
<br/>
<ul>
<li>Name and location information will be prefilled with the data from the original record.</li>
<li>Date fields will remain blank.</li>
</ul>
<br/>
Please update the duplicate record as applicable.
            `);
            
            
            
            Button_Event.push(`g_duplicate_record_item('${p_object_path}', '${p_metadata_path}', ${p_index})`);
            Button_style.push(`style="height: 38px;
            padding-left: 12px;
            padding-right: 12px;
            border-radius: 4px;
            border: 1px solid #712177;
            background-color: #712177;
            box-sizing: border-box;
            font-family: 'Open Sans', sans-serif;
            color: rgba(255, 255, 255, 1);
            text-align: center;
            line-height: normal;
            cursor: pointer;"`);
                


            let element = document.getElementById(dialog_id);
                if(element == null)
                {
                    element = document.createElement("dialog");
                    element.classList.add('p-0');
                    element.classList.add('set-radius');
                    element.setAttribute("id", dialog_id);
                    element.setAttribute("aria-modal", "true");
                    element.setAttribute("role","dialog");
    
                    document.firstElementChild.appendChild(element);
                }

                element.style.width = "520px";
                element.style.transform = "translateY(0%)";

    
                let html = [];
                html.push(`
                    <div class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix" role="dialog">
                        <span id="ui-id-1" class="ui-dialog-title">${Title_Text[0]}</span>
                        <button type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="×" onclick="$mmria.duplicate_multiform_dialog_click()"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
                    </div>
                    <div id="mmria_dialog7" style="width: 300; height: 260px;" class="ui-dialog-content ui-widget-content" role="dialog">
                        <div class="modal-body">
                                <div >
                       ${Description_Text[0]}
                       <br/><br/>
                                    <div style="text-align:right;padding-right: 8px;">
                                        <button id="confirm-dialog-id-cancel-button"  class="btn modal-cancel btn-outline-secondary  mr-1" onclick="$mmria.duplicate_multiform_dialog_click()">Cancel</button>
                                        <input id="duplicate_dialog_choice" class="btn-primary" type="button" value="${Button_Text[0]}" onclick="${Button_Event[0]}" style="height: 38px;
                                        padding-left: 12px;
                                        padding-right: 12px;
                                        border-radius: 4px;
                                        border: 1px solid #712177;
                                        background-color: #712177;
                                        box-sizing: border-box;
                                        font-family: 'Open Sans', sans-serif;
                                        color: rgba(255, 255, 255, 1);
                                        text-align: center;
                                        line-height: normal;
                                        cursor: pointer;"/>
                                        
            
                                    </div>

                                </div>
                        
                        </div>

                    </div>

                `);
    
                element.innerHTML = html.join("");

                mmria_pre_modal(dialog_id);

                window.setTimeout(()=> { const duplicate_dialog_choice = document.getElementById("duplicate_dialog_choice"); duplicate_dialog_choice.focus(); }, 0);
    
                element.showModal();
                
        },
        duplicate_multiform_dialog_click: function ()
        {
            mmria_post_modal();
            const el = document.getElementById("multiform-dialog-id");
            if(el != null)
                el.close();
        },
        unstable_network_dialog_show: async function (p_error, p_note)
        {

            let element = document.getElementById("unstable-network-id");
                if(element == null)
                {
                    element = document.createElement("dialog");
                    element.classList.add('p-0');
                    element.classList.add('set-radius');
                    element.setAttribute("id", "unstable-network-id");
                    element.setAttribute("role", "dialog");
                    element.setAttribute("aria-label","Unable To Save/Unstable Network Dialog");

                    document.firstElementChild.appendChild(element);
                }

                element.style.maxWidth = "512px";
                element.style.transform = "translateY(0%)";
                element.style.maxHeight = "600px";
                element.style.overflow = "hidden";
    
                let html = [];
                html.push(`
                    <div role='alertdialog' aria-modal="true" aria-labelledby='ui-id-1' aria-describedby='mmria-dialog5' class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix">
                        <span id="ui-id-1" class="ui-dialog-title" style="font-family: 'Open-Sans';">Unable to Save/Network Unstable</span>
                        <button type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="Close" onclick="$mmria.unstable_network_dialog_click()"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
                    </div>
                    <div id="mmria_dialog5" class="ui-dialog-content ui-widget-content">
                        <div class="modal-body">
                         <p>To prevent data loss, <b>do NOT close this MMRIA form.</b></p>
                         <p>Please wait 5 minutes, then press the <b>Save & Continue</b> button to save your work. You should receive confirmation that your data has been saved.</p>
                         
                         <p>
                         <b>If this error occurs again, please do the following:</b> 
                         <ol>
                            <li>Select <u>Show Error Detail</u> below</li>
                            <li>Select <u>Copy Details to Clipboard</u></li>
                            <li>Send an email to MMRIA Support with the following details:
                            <ul>
                            <li>Email To: <a href="mailto:mmriasupport@cdc.gov">mmriasupport@cdc.gov</a> </li>
                            <li>Subject: MMRIA Save Error</li>
                            <li>Body: Paste the contents of the Clipboard in the email (by pressing CTRL + V together on the keyboard).</li>
                            </ul>
                         </li>
                        </ol>
                         <a href="javascript:$mmria.server_response_detail_div_show()">Show Error Detail</a> | <a href="javascript:$mmria.server_response_detail_div_hide()">Hide Error Detail</a>
                         <div id="server_response_detail_div" style="display:none">
                         <br/>
                         <textarea id=server_response_textarea rows=7 cols=55 readonly>
Status: ${p_error.status === 0 ? "Unsent" : p_error.status }
Action: ${p_note}
Server Response:
${p_error.responseText== undefined ? "offline" : p_error.responseText }
                         </textarea>
                         <button class="btn btn-primary mr-1" onclick="$mmria.unstable_network_dialog_copy_click()" style="font-family: 'Open-Sans';">Copy Details to Clipboard</button>
                         </div>
                        </div>

                    </div>
                    <div style="display:block">
                    <footer class="modal-footer">
                        <button id="unstable_network_dialog_close_button" class="btn btn-primary mr-1" onclick="$mmria.unstable_network_dialog_click()" style="font-family: 'Open-Sans';">OK</button>
                    </footer>
                    </div>
                `);
    
                element.innerHTML = html.join("");

                mmria_pre_modal("unstable-network-id");
                
                window.setTimeout
                (
                    ()=> { 
                        const unstable_network_dialog_close_button = document.getElementById("unstable_network_dialog_close_button"); 
                        unstable_network_dialog_close_button.focus(); 
                    }, 
                    0
                );

                if(!element.open) element.showModal();
        },
        unstable_network_dialog_click: function ()
        {
            mmria_post_modal();
            let el = document.getElementById("unstable-network-id");
            el.close();
        },
        unstable_network_dialog_copy_click: function()
        {
            const element = document.getElementById("server_response_textarea");
            navigator.clipboard.writeText(element.value);
        },
        save_error_500_dialog_show: async function (p_error, p_note)
        {

            let element = document.getElementById("save-error-500-id");
                if(element == null)
                {
                    element = document.createElement("dialog");
                    element.classList.add('p-0');
                    element.classList.add('set-radius');
                    element.setAttribute("id", "save-error-500-id");
                    element.setAttribute("role", "dialog");
    
                    document.firstElementChild.appendChild(element);
                }

                element.style.maxWidth = "512px";
                element.style.transform = "translateY(0%)";
                element.style.maxHeight = "600px";
                element.style.overflow = "hidden";
    
                let html = [];
                html.push(`
                    <div aria-modal="true" class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix">
                        <span id="ui-id-1" class="ui-dialog-title" style="font-family: 'Open-Sans';">Error occurred when trying to save to the MMRIA server</span>
                        <button type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="Close" onclick="$mmria.save_error_500_dialog_click()"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
                    </div>
                    <div id="mmria_dialog5" class="ui-dialog-content ui-widget-content">
                        <div class="modal-body">
                         <p>An error occured while saving.</p>
                         <p>Error Summary: ${p_note}</p>
                         <p>1. Press "Save and Continue" and confirm that you see a green "Case information has been saved." message.</p>
                         <p>
                            2. If no confirmation message try navigating back to <b>MMRIA Home</b> and then to <b>View or Modify Data</b> and select your case again.<br/>
                         <br/>
                          3. If the problem persists, send an email to MMRIA Support  <a href="mailto:mmriasupport@cdc.gov">mmriasupport@cdc.gov</a>
                         </p>
                       
                         <br/>
                         <textarea id=server_response_textarea2 rows=7 cols=55 readonly>
Status: ${p_error.status === 0 ? "Unsent" : p_error.status }
Action: ${p_note}
Server Response:
${p_error.responseText== undefined ? "offline" : p_error.responseText }
                         </textarea>
                        </div>

                    </div>
                    <div style="display:block">
                    <footer class="modal-footer">
                        <button id="save_error_500_dialog_close_button" class="btn btn-primary mr-1" onclick="$mmria.save_error_500_dialog_click()" style="font-family: 'Open-Sans';">OK</button>
                    </footer>
                    </div>
                `);
    
                element.innerHTML = html.join("");

                mmria_pre_modal("save-error-500-id");
                
                window.setTimeout
                (
                    ()=> { 
                        const save_error_500_dialog_close_button = document.getElementById("save_error_500_dialog_close_button"); 
                        save_error_500_dialog_close_button.focus(); 
                    }, 
                    0
                );

                if(!element.open) element.showModal();
        },
        save_error_500_dialog_click: function ()
        {
            mmria_post_modal();
            let el = document.getElementById("save-error-500-id");
            el.close();
        },
        server_response_detail_div_show:function()
        {
            const el = document.getElementById('server_response_detail_div');
            el.style.display = 'block';
        },
        server_response_detail_div_hide:function()
        {
            const el = document.getElementById('server_response_detail_div');
            el.style.display = 'none';
        },
        field_save_error_dialog_show: async function (p_error, p_note)
        {

            let element = document.getElementById("field-save-error-id");
                if(element == null)
                {
                    element = document.createElement("dialog");
                    element.classList.add('p-0');
                    element.classList.add('set-radius');
                    element.setAttribute("id", "field-save-error-id");
                    element.setAttribute("role", "dialog");
    
                    document.firstElementChild.appendChild(element);
                }

                element.style.maxWidth = "512px";
                element.style.transform = "translateY(0%)";
                element.style.maxHeight = "600px";
                element.style.overflow = "hidden";
    
                let html = [];
                html.push(`
                    <div aria-modal="true" class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix">
                        <span id="ui-id-1" class="ui-dialog-title" style="font-family: 'Open-Sans';">Unable to save field data</span>
                        <button type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="Close" onclick="$mmria.field_save_error_dialog_click()"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
                    </div>
                    <div id="mmria_dialog6" class="ui-dialog-content ui-widget-content">
                        <div class="modal-body">
                         <p>Unable to save field data</p><br/>
                         <p>Error Summary: ${p_note}</p>
                         <br/>
                         If the problem persists, send an email to MMRIA Support  <a href="mailto:mmriasupport@cdc.gov">mmriasupport@cdc.gov</a>
                         </p>
                       
                         <br/>
                         <textarea id=server_response_textarea2 rows=7 cols=55 readonly>
Status: ${p_error.status === 0 ? "Unsent" : p_error.status }
Action: ${p_note}
Server Response:
${p_error.responseText== undefined ? "offline" : p_error.responseText }
                         </textarea>
                        </div>

                    </div>
                    <div style="display:block">
                    <footer class="modal-footer">
                        <button id="field_save_error_dialog_close_button" class="btn btn-primary mr-1" onclick="$mmria.field_save_error_dialog_click()" style="font-family: 'Open-Sans';">OK</button>
                    </footer>
                    </div>
                `);
    
                element.innerHTML = html.join("");

                mmria_pre_modal("field-save-error-id");
                
                window.setTimeout
                (
                    ()=> { 
                        const field_save_error_dialog_close_button = document.getElementById("field_save_error_dialog_close_button"); 
                        field_save_error_dialog_close_button.focus(); 
                    }, 
                    0
                );

                if(!element.open) element.showModal();
        },
        field_save_error_dialog_click: function ()
        {
            mmria_post_modal();
            let el = document.getElementById("field-save-error-id");
            el.close();
        },
        view_cdf_template_pdf: function()
        {
            window.open('./pdfs/mmria-form-v24.3-fillable-web_no_hyperlinked_footnotes.pdf', '_blank');
        },
        view_mmria_abstractor_manual_pdf: function()
        {
            window.open('./pdfs/MMRC_Abstractor_Manual_V4.pdf', '_blank');
        }
    };

}();


async function mmria_pin_case_click(p_id, p_is_everyone)
{
    const message = {
        is_pin: true,
        case_id: p_id,
        user_id: g_user_name

    };

    var method = "POST";
    var is_admin = false;
    if(g_is_jurisdiction_admin && p_is_everyone)
    {
        is_admin = true;
        message.user_id = "everyone";
        method = "PUT";
    }

    if(g_is_jurisdiction_admin)
    {
        $mmria.pin_un_pin_dialog_click();
    }

    var url = `${location.protocol}//${location.host}/api/pinned_cases`;
    // abstractor post
    // jurisdiction put
    const pin_response = await $.ajax
    ({
        url: url,
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify(message),
        type: method,
    });

    if(pin_response.ok)
    {
        if(is_admin)
        {
            if(g_pinned_case_set.list.everyone == null)
                g_pinned_case_set.list.everyone = [];

            g_pinned_case_set.list.everyone.push(p_id);
        }
        else
        {
            if(g_pinned_case_set.list[g_user_name] == null)
                g_pinned_case_set.list[g_user_name] = []

            g_pinned_case_set.list[g_user_name].push(p_id);
        }

    }
 
    const post_html_call_back = [];
    document.getElementById('form_content_id').innerHTML = page_render
    (
        g_metadata,
        g_data,
        g_ui,
        'g_metadata',
        'g_data',
        '',
        false,
        post_html_call_back
    ).join('');

    if (post_html_call_back.length > 0) 
    {
        try
        {
        eval(post_html_call_back.join(''));
        } 
        catch (ex) 
        {
        console.log(ex);
        }
    }
}


async function mmria_un_pin_case_click(p_id, p_is_everyone)
{
    const message = {
        is_pin: false,
        case_id: p_id,
        user_id: g_user_name

    };

    var method = "POST";
    var is_admin = false;
    if(g_is_jurisdiction_admin && p_is_everyone)
    {
        is_admin = true;
        message.user_id = "everyone";
        method = "PUT";
    }

    if(g_is_jurisdiction_admin)
    {
        $mmria.pin_un_pin_dialog_click();
    }

    var url = `${location.protocol}//${location.host}/api/pinned_cases`;

    const pin_response = await $.ajax
    ({
        url: url,
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify(message),
        type: method,
    });

    if(pin_response.ok)
    {
        if(is_admin)
        {
           const index = g_pinned_case_set.list.everyone.indexOf(p_id);

           g_pinned_case_set.list.everyone.splice(index,1);
        }
        else
        {

            const index = g_pinned_case_set.list[g_user_name].indexOf(p_id);
            g_pinned_case_set.list[g_user_name].splice(index,1);
        }

    }

    const post_html_call_back = [];
    document.getElementById('form_content_id').innerHTML = page_render
    (
        g_metadata,
        g_data,
        g_ui,
        'g_metadata',
        'g_data',
        '',
        false,
        post_html_call_back
    ).join('');


    if (post_html_call_back.length > 0) 
    {
        try
        {
        eval(post_html_call_back.join(''));
        } 
        catch (ex) 
        {
        console.log(ex);
        }
    }
}

function mmria_count_number_pinned()
{
    const case_set = new Set();

    if(Object.hasOwn(g_pinned_case_set, 'list'))
    {
        if(Object.hasOwn(g_pinned_case_set.list, 'everyone'))
        {
            for(const i in g_pinned_case_set.list.everyone)
            {
                case_set.add(g_pinned_case_set.list.everyone[i]);
            }
        }

        
        if(Object.hasOwn(g_pinned_case_set.list, g_user_name))
        {
            for(const i in g_pinned_case_set.list[g_user_name])
            {
                case_set.add(g_pinned_case_set.list[g_user_name][i]);
            }
        }
        
    }

    return case_set.size;
}


function mmria_pre_modal(p_id)
{

    const body = document.getElementsByTagName("body")[0];
    //body.setAttribute("aria-hidden", "true");
    for(var i = 0; i < body.children.length; i++)
    {
        const item = body.children[i];
        if
        (
            p_id != null &&
            item.id != null &&
            item.id != p_id 
        )
        {
            item.setAttribute("aria-hidden", "true");
        }
        else
        {
            item.setAttribute("aria-hidden", "true");
        }
        
        
    }
}

function mmria_post_modal()
{
    const body = document.getElementsByTagName("body")[0];
    //body.removeAttribute("aria-hidden");
    for(var i = 0; i < body.children.length; i++)
    {
        const item = body.children[i];
        item.removeAttribute("aria-hidden");
    }
}

function handle_print()
{
  window.print();
}

function set_title(title)
{
  document.title = title;
}

function info_dialog_keyboard_click(event)
{
  if (event.key === "Enter")
  {
    mmria_post_modal();
    let el = document.getElementById("case-progress-info-id");
    el.close();
  }
}

function info_dialog_click()
{
  mmria_post_modal();
  let el = document.getElementById("case-progress-info-id");
  el.close();
}

function info_dialog_show(p_title, p_header, p_inner_html, p_message_type = "default")
{
    // var publishedAlertTypeStyling= [];
    
    // if (p_message_type == "information")
    //     publishedAlertTypeStyling = ["info-banner", "cdc-icon-alert_01 refresh-icon", "primary-button"]
    // else if (p_message_type == "warning")
    //     publishedAlertTypeStyling = ["info-banner", "cdc-icon-alert_02 warning-icon", "primary-button"]
    // else if (p_message_type == "error")
    //     publishedAlertTypeStyling = ["info-banner", "cdc-icon-close-circle error-icon", "primary-button"]
    // else
    //     publishedAlertTypeStyling = ["info-banner", "cdc-icon-close-circle refresh-icon", "primary-button"]
    function get_header()
    {
        switch(p_message_type.toLowerCase())
        {
            case "information":
                return `<div class="align-items-center justify-content-start modal-header ui-widget-header ui-helper-clearfix">
                <div id="ui-id-1" class="ui-dialog-title">${p_title}</div>
                <span id="ui-id-1-close" tabindex="0" role="button" onkeypress="info_dialog_keyboard_click(event)" onclick="$mmria.info_dialog_click()" title="close" class="ml-auto x24 fill-p cdc-icon-times-solid"></span>
            </div>`
            break;
            case "warning":
                return `<div class="align-items-center justify-content-start  modal-header ui-widget-header ui-helper-clearfix">
                <div id="ui-id-1" class="ui-dialog-title">${p_title}</div>
                <span id="ui-id-1-close" tabindex="0" role="button" onkeypress="info_dialog_keyboard_click(event)" onclick="$mmria.info_dialog_click()" title="close" class="ml-auto x24 fill-p cdc-icon-times-solid"></span>
            </div>`
            break;
            case "error":
                return `<div class="align-items-center justify-content-start modal-header ui-widget-header ui-helper-clearfix">
                <div id="ui-id-1" class="ui-dialog-title">${p_title}</div>
                <span id="ui-id-1-close" tabindex="0" role="button" onkeypress="info_dialog_keyboard_click(event)" onclick="$mmria.info_dialog_click()" title="close" class="ml-auto x24 fill-p cdc-icon-times-solid"></span>
            </div>`
            break;
            default:
                return `<div class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix">
                <div id="ui-id-1" class="ui-dialog-title">${p_title}</div>
                <span id="ui-id-1-close" tabindex="0" role="button" onkeypress="info_dialog_keyboard_click(event)" onclick="$mmria.info_dialog_click()" title="close" class="ml-auto x24 fill-p cdc-icon-times-solid"></span>
            </div>`
            break;
        }
    }
    let element = document.getElementById("case-progress-info-id");
        if(element == null)
        {
            element = document.createElement("dialog");
            element.classList.add('p-0');
            element.classList.add('set-radius');
            element.classList.add('card-container-dark');
            element.classList.add('col-md-4');
            element.setAttribute("id", "case-progress-info-id");
            element.setAttribute("aria-modal", "true");
            document.firstElementChild.appendChild(element);
        }
        let html = [];
        html.push(`
            ${get_header()}
            <div id="mmria_dialog2" style="width: auto; min-height: 101px; max-height: none; height: auto;" class="ui-dialog-content ui-widget-content">
                <div class="modal-body">
                    <p><strong>${p_header}</strong></p>
                    ${p_inner_html}
                </div>
                <footer class="modal-footer">
                    <button id="mmria_dialog2_close" class="btn primary-button mr-1" onclick="$mmria.info_dialog_click()">OK</button>
                </footer>
            </div>
        `);
        // html.push(`<h3 class="mt-0">${p_title}</h3>`);
        // html.push(`<p><strong>${p_header}</p>`);
        // html.push(`${p_inner_html}`);
        // html.push('<button class="btn btn-primary mr-1" onclick="$mmria.info_dialog_click()">OK</button>');
        element.innerHTML = html.join("");
        mmria_pre_modal("case-progress-info-id");
        element.showModal();
    }

async function erase_mm_link_click()
{
    function cancel()
    {
        $mmria.confirm_external_nav_dialog_confirm_close();
    }

    function confirm()
    {
        $mmria.confirm_external_nav_dialog_confirm_close();
        window.open
        (
            "https://www.erase-mm-cvsproject.org", 
            "_blank"
        );
    }

    $mmria.confirm_external_nav_dialog_show
    (
        confirm, 
        cancel
    )
}

async function external_link_click(p_url)
{
    function cancel()
    {
        $mmria.confirm_external_nav_dialog_confirm_close();
    }

    function confirm()
    {
        $mmria.confirm_external_nav_dialog_confirm_close();
        window.open
        (
            p_url, 
            "_blank"
        );
    }

    $mmria.confirm_external_nav_dialog_show
    (
        confirm, 
        cancel
    )
}


async function mmria_check_session()
{
    /*
    const response = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/${p_url}`,
    });

    return response*/


    let response = {};

    try
    {
        const response_promise = await fetch
        (
            `${location.protocol}//${location.host}/api/session`, {
            method: "GET",
            headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json; charset=utf-8',
            'dataType': 'json',
            },
        
            //make sure to serialize your JSON body
            //body: JSON.stringify(save_case_request)
        });

        if (!response_promise.ok) {
            throw new Error(`Response status: ${response.status}`);
          }

        response = await response_promise.json();
    }  
    catch(xhr) 
    {
        /*
        //alert(`server save_case: failed\n${err}\n${xhr.responseText}`);

        $mmria.unstable_network_dialog_show(xhr, p_note);
        if (xhr.status == 401) 
        {
            let redirect_url = location.protocol + '//' + location.host;
            window.location = redirect_url;
        }
        else if (xhr.status == 200 && xhr.responseText.length >= 49000) 
        {
            let redirect_url = location.protocol + '//' + location.host;
            window.location = redirect_url;
        }
            */
    }

    return response;
}


function mmria_check_if_need_to_redirect(p_input)
{
    if 
    (
        p_input.ok &&
        p_input.redirected &&
        p_input.url.indexOf("/Account/") > -1
    )
    {
        window.location = p_input.url;
    }
}

function getSSCookie() 
{
    const value = `; ${document.cookie}`;
    const parts = value.split(`; sid`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}

function set_character_limit(p_string_to_limit, p_limit)
{
    if (p_string_to_limit.length > p_limit) {
        return p_string_to_limit.substring(0, p_limit) + "...";
    }
    return p_string_to_limit;
}