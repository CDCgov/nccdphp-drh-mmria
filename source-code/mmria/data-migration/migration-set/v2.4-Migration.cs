using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public sealed class v2_4_Migration
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


    public v2_4_Migration
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
        this.output_builder.AppendLine($"v2.4 Data Migration started at: {DateTime.Now.ToString("o")}");
        DateTime begin_time = System.DateTime.Now;
        
        this.output_builder.AppendLine($"v2_4_Migration started at: {begin_time.ToString("o")}");
        
        var gs = new C_Get_Set_Value(this.output_builder);
        try
        {
            //string metadata_url = host_db_url + "/metadata/2016-06-12T13:49:24.759Z";
            string metadata_url = $"https://couchdb-test-mmria.apps.ecpaas-dev.cdc.gov/metadata/version_specification-20.12.01/metadata";

            //string metadata_url = $"{host_db_url}/metadata/version_specification-20.12.01/metadata";
            
            cURL metadata_curl = new cURL("GET", null, metadata_url, null, null, null);
            mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(await metadata_curl.executeAsync());
        
            this.lookup = get_look_up(metadata);

            var pmss_list = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "cr_p_mm",
                "cr_pm_secon"
            };

            var miscellaneous_list = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bcifsmod_framo_deliv",
                "ar_coa_infor",
                "pppcf_pp_type",
                "posopc_p_type",
                "omovmcf_provicer_type",
                "omovdiaot_t_type"
            };


            var eight_to_7_list = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {

            "dcd_eiua_force",
            "dcd_ioh_origi",
            "dcd_e_level",
            "dciai_wia_work",
            "dciai_wsbi_use",
            "dcdi_doi_hospi",
            "dcdi_doo_hospi",
            "dcdi_mo_death",
            "dcdi_wa_perfo",
            "dcdi_waufd_codin",
            //"dcdi_p_statu",
            "dcdi_dtct_death",
            "bfdcpfodd_whd_plann",
            "bfdcpfodd_a_type",
            "bfdcpfodd_wm_trans",
            "bfdcpdof_e_level",
            "bfdcpdof_ifoh_origi",
            //"bfdcpdofr_ro_fathe",
            "bfdcpdom_m_marri",
            "bfdcpdom_Imnmhpabsit_hospi",
            "bfdcpdom_eiua_force",
            "bfdcpdom_ioh_origi",
            "bfdcpdom_e_level",
            "bfdcppc_plura",
            "bfdcppc_ww_used",
            "bfdcpcs_non_speci",
            "bfdcprf_rfit_pregn",
            "bfdcp_ipotd_pregn",
            "bfdcp_oo_labor",
            "bfdcp_o_proce",
            "bfdcp_cola_deliv",
            "bfdcp_m_morbi",
            "bcifs_im_gesta",
            "bcifsbad_gende",
            "bcifsbad_iilato_repor",
            "bcifsbad_iibba_disch",
            "bcifsbad_witw2_hours",
            "bcifsmod_wdwfab_unsuc",
            "bcifsmod_wdwveab_unsuc",
            "bcifsmod_f_deliv",
            "bcifsmod_framo_deliv",
            "bcifsmod_icwtol_attem",
            "bcifs_aco_newbo",
            "arrc_r_type",
            "art_level",
            "pppcf_p_type",
            "pppcf_iu_wic",
            "p_hpe_condi",
            "p_wtdmh_condi",
            "pfmh_i_livin",
            "p_eos_use",
            "psug_scree",
            "psug_c_educa",
            "pphdg_in_livin",
            "pi_wp_plann",
            "pi_wpub_contr",
            "pit_wproi_treat",
            "pit_fe_drugs",
            "pit_ar_techn",
            "pcp_whd_plann",
            "pcp_apv_alone",
            "p_wtp_ident",
            "p_wta_react",
            "pmaddp_ia_react",
            "p_wtpd_hospi",
            "p_wmrt_other",
            "pmr_wa_kept",
            "posopc_place",
            "evahmrbaadi_a_condi",
            "evahmrbaadi_wrfa_hospi",
            "evahmrbaadi_wtta_hospi",
            "evahmrbaadi_dp_statu",
            "evahmrbaadi_da_disch",
            "evahmrnalf_mott_facil",
            "evahmrnalf_oo_trave",
            "evahmrlt_d_level",
            "evahmrool_fd_route",
            "evahmrool_m_gesta",
            "evahmrba_title",
            "evahmr_wtco_anest",
            "evahmr_aa_react",
            "evahmr_as_proce",
            "evahmr_ab_trans",
            "omovv_v_type",
            "omovmcf_p_type",
            "omovmcf_wtphppc_provi",
            "omovlt_d_level",
            "saepsec_so_incom",
            "saepsec_e_statu",
            "saepsec_cl_arran",
            "saepsec_homel",
            "saepmoh_relat",
            "saepmoh_gende",
            "saepsamr_compi",
            "saep_ds_use",
            "mhp_wtdpmh_cond",
            "mhpwtdmhc_rf_treat"
            };


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

            var sf = single_form_value_set.Where
            ( 
                x=> eight_to_7_list.Contains(x.sass_export_name) ||
                miscellaneous_list.Contains(x.sass_export_name) ||
                pmss_list.Contains(x.sass_export_name)
                
            ).ToList();

            foreach(var item in sf)
            {
                Console.WriteLine($"{item.sass_export_name} - {item.path}");
            }


            var pmss_set  = single_form_value_set.Where
            ( 
                x=> pmss_list.Contains(x.sass_export_name)
                
            ).ToList();


            
            var eight_to_7_sf = single_form_value_set.Where
            ( 
                x=> eight_to_7_list.Contains(x.sass_export_name) 
                
            ).ToList();


            var eight_to_7_sfmv = single_form_multi_value_set.Where
            ( 
                x=> eight_to_7_list.Contains(x.sass_export_name) 
                
            ).ToList();

            var eight_to_7_sfgv = single_form_grid_value_set.Where
            ( 
                x=> eight_to_7_list.Contains(x.sass_export_name) 
                
            ).ToList();
            var eight_to_7_sfgmv = single_form_grid_multi_value_list_set.Where
            ( 
                x=> eight_to_7_list.Contains(x.sass_export_name) 
                
            ).ToList();

            var eight_to_7_mv = multiform_value_set.Where
            ( 
                x=> eight_to_7_list.Contains(x.sass_export_name) 
                
            ).ToList();
            var eight_to_7_mmv = multiform_multi_value_set.Where
            ( 
                x=> eight_to_7_list.Contains(x.sass_export_name) 
                
            ).ToList();

            var eight_to_7_mgv = multiform_grid_value_set.Where
            ( 
                x=> eight_to_7_list.Contains(x.sass_export_name) 
                
            ).ToList();
            var eight_to_7_mgmv = multiform_grid_multi_value_set.Where
            ( 
                x=> eight_to_7_list.Contains(x.sass_export_name) 
                
            ).ToList();

            var eight_to_7_count = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            eight_to_7_sf.Select(x=>eight_to_7_count.Add(x.Node.sass_export_name)).ToList();

            eight_to_7_sfmv.Select(x=>eight_to_7_count.Add(x.Node.sass_export_name)).ToList();

            eight_to_7_sfgv.Select(x=>eight_to_7_count.Add(x.Node.sass_export_name)).ToList();
            eight_to_7_sfgmv.Select(x=>eight_to_7_count.Add(x.Node.sass_export_name)).ToList();

            eight_to_7_mv.Select(x=>eight_to_7_count.Add(x.Node.sass_export_name)).ToList();
            eight_to_7_mmv.Select(x=>eight_to_7_count.Add(x.Node.sass_export_name)).ToList();

            eight_to_7_mgv.Select(x=>eight_to_7_count.Add(x.Node.sass_export_name)).ToList();
            eight_to_7_mgmv.Select(x=>eight_to_7_count.Add(x.Node.sass_export_name)).ToList();


            foreach (var firstItem in eight_to_7_list)
            {

                if (!eight_to_7_count.Contains(firstItem))
                {

                    Console.WriteLine(firstItem);
                }
            }

            var pmss_map = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
            {
                { "10", "10.9"},
                { "20", "20.9"},
                { "30", "30.1"},
                { "31", "31.1"},
                { "40", "40.1"},
                { "50", "50.1"},
                { "60", "60.1"},
                { "70", "70.1"},
                { "80", "80.9"},
                { "82", "82.9"},
                { "83", "83.9"},
                { "85", "85.1"},
                { "89", "89.9"},
                { "90", "90.9"},
                { "91", "91.9"},
                { "92", "92.9"},
                { "93", "93.9"},
                { "95", "95.1"},
                { "96", "96.9"},
                { "97", "97.9"},
                { "100", "100.9"},
                { "999", "999.1"}
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
                    var mmria_id = value_result.result?.ToString();
                    if(mmria_id.IndexOf("_design") > -1)
                    {
                        continue;
                    }

                    var host_state = "TT";
                    // host_state  *** begin
                    try
                    {
                        
                        value_result = gs.get_value(doc, "host_state");
                        var test_host_state_object = value_result.result;
                        if
                        (
                            test_host_state_object == null || 
                            string.IsNullOrWhiteSpace(test_host_state_object.ToString()) ||
                            test_host_state_object.ToString().ToLower() == "central"
                        )
                        {
                            if(test_host_state_object.ToString().ToLower() == "central")
                            {
                                if(db_name.IndexOf("_") > -1)
                                {
                                    host_state = db_name.Split("_")[0];
                                }
                                else
                                {
                                    host_state = test_host_state_object.ToString();
                                }
                            }
                            else
                            {
                                host_state = test_host_state_object.ToString();
                            }

                            
                        }
                        else
                        {
                            host_state = test_host_state_object.ToString();
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    // host_state  *** end


                // record_id - begin
                try
                {
                    value_result = gs.get_value(doc, "home_record/record_id");
                    string year_of_death = "1900";
                    var year_of_death_value_result = gs.get_value(doc, "home_record/date_of_death/year");
                    if
                    (
                        !year_of_death_value_result.is_error &&
                        year_of_death_value_result.result != null &&
                        !string.IsNullOrWhiteSpace(year_of_death_value_result.result.ToString()) &&
                        year_of_death_value_result.result.ToString() != "9999"
                    )
                    {
                        year_of_death = year_of_death_value_result.result.ToString();
                    }
                    
                    if(!value_result.is_error)
                    {
                        if 
                        (
                            value_result.result == null ||
                            value_result.result.ToString() == ""
                        )
                        {
                            string record_id = null;
                            do
                            {
                                record_id = $"{host_state.ToUpper()}-{year_of_death}-{GenerateRandomFourDigits().ToString()}";
                            }
                            while(ExistingRecordIds.Contains(record_id));
                            ExistingRecordIds.Add(record_id);
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }
                            
                            case_has_changed = case_has_changed && gs.set_value("home_record/record_id", record_id, doc);
                            var output_text = $"item record_id: {mmria_id} Generated new record_id {record_id}";
                            this.output_builder.AppendLine(output_text);
                            Console.WriteLine(output_text);
                        } 
                    } 
                    else
                    {
                        string record_id = null;
                        do
                        {
                            record_id = $"{host_state.ToUpper()}-{year_of_death}-{GenerateRandomFourDigits().ToString()}";
                        }
                        while(ExistingRecordIds.Contains(record_id));
                        ExistingRecordIds.Add(record_id);

                        if(case_change_count == 0)
                        {
                            case_change_count += 1;
                            case_has_changed = true;
                        }
                        
                        case_has_changed = case_has_changed && gs.set_value("home_record/record_id", record_id, doc);
                        var output_text = $"item record_id: {mmria_id} Generated new record_id {record_id}";
                        this.output_builder.AppendLine(output_text);
                        Console.WriteLine(output_text);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
                // record_id - end




                // converted from single value to multivalue - begin
                try
                {
                    //saepsec_homel social_and_environmental_profile/socio_economic_characteristics/homelessness
                    var saepsec_homel_path = "social_and_environmental_profile/socio_economic_characteristics/homelessness";
                    value_result = gs.get_value(doc, saepsec_homel_path);
                    if (!value_result.is_error)
                    {
                        if(value_result.result == null)
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }
                            object new_value_list = new List<object>(){ 9999 };

                            case_has_changed = case_has_changed && gs.set_multi_value(saepsec_homel_path, new_value_list, doc);
                            var output_text = $"item record_id: {mmria_id} path:{saepsec_homel_path} Converted from single value to multivalue {value_result.result} => [{string.Join(',', new_value_list)}]";
                            this.output_builder.AppendLine(output_text);
                            Console.WriteLine(output_text);
                        }
                        else if
                        (
                            value_result.result is string ||
                            value_result.result is int
                        )
                        {

                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }

                            object new_value = value_result.result;
                            if(new_value.ToString() == "8888")
                            {
                                new_value = "7777";
                            }


                            object new_value_list = new List<object>(){
                                new_value
                            };

                            case_has_changed = case_has_changed && gs.set_multi_value(saepsec_homel_path, new_value_list, doc);
                            var output_text = $"item record_id: {mmria_id} path:{saepsec_homel_path} Converted from single value to multivalue {value_result.result} => [{string.Join(',', new_value_list)}]";
                            this.output_builder.AppendLine(output_text);
                            Console.WriteLine(output_text);
                        }
                        else
                        {
                            System.Console.WriteLine("here");

                        }


                    }
                    else
                    {
                        // do nothing
                        //System.Console.WriteLine("Do nothing missing form");

                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
                try
                {
                    //saep_poc_incar social_and_environmental_profile/previous_or_current_incarcerations
                    var saep_poc_incar_path = "social_and_environmental_profile/previous_or_current_incarcerations";
                    value_result = gs.get_value(doc, saep_poc_incar_path);
                    if (!value_result.is_error)
                    {
                        if (value_result.result == null)
                        {	
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }
                            object new_value_list = new List<object>(){ 9999 };

                            case_has_changed = case_has_changed && gs.set_multi_value(saep_poc_incar_path, new_value_list, doc);
                            var output_text = $"item record_id: {mmria_id} path:{saep_poc_incar_path} Converted from single value to multivalue {value_result.result} => [{string.Join(',', new_value_list)}]";
                            this.output_builder.AppendLine(output_text);
                            Console.WriteLine(output_text);
                        }
                        else if
                        (
                                value_result.result is string ||
                                value_result.result is int
                        )
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }
                            object new_value_list = new List<object>(){
                                value_result.result
                            };

                            case_has_changed = case_has_changed && gs.set_multi_value(saep_poc_incar_path, new_value_list, doc);
                            var output_text = $"item record_id: {mmria_id} path:{saep_poc_incar_path} Converted from single value to multivalue {value_result.result} => [{string.Join(',', new_value_list)}]";
                            this.output_builder.AppendLine(output_text);
                            Console.WriteLine(output_text);
                        }
                        else
                        {
                            System.Console.WriteLine("here");

                        }

                    }
                    else
                    {
                        // Do nothing
                        //System.Console.WriteLine("Do nothing missing form");

                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
                // converted from single value to multivalue - end

                foreach(var node in  pmss_set)
                {
                    value_result = gs.get_value(doc, node.path);
                    try
                    {
                        if(!value_result.is_error)
                        {
                            var value = value_result.result;
                            if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            {
                                continue;	
                            }
                            var value_string = value.ToString();
                            if(pmss_map.ContainsKey(value_string))
                            {
                                if(case_change_count == 0)
                                {
                                    case_change_count += 1;
                                    case_has_changed = true;
                                }
                                
                                string new_value = pmss_map[value_string];

                                case_has_changed = case_has_changed && gs.set_value(node.path, new_value, doc);
                                var output_text = $"item record_id: {mmria_id} path:{node.path} Converted {value_string} => {new_value}";
                                this.output_builder.AppendLine(output_text);
                                Console.WriteLine(output_text);
                            }
                        }	
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }	
                }



                foreach(var node in  eight_to_7_sf)
                {
                    value_result = gs.get_value(doc, node.path);
                    try
                    {
                        if(!value_result.is_error)
                        {
                            var value = value_result.result;
                            if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            {
                                continue;	
                            }

                            if(value.ToString() == "8888")
                            {
                                if(case_change_count == 0)
                                {
                                    case_change_count += 1;
                                    case_has_changed = true;
                                }
                                
                                string new_value = "7777";

                                case_has_changed = case_has_changed && gs.set_value(node.path, new_value, doc);
                                var output_text = $"item record_id: {mmria_id} path:{node.path} Converted 8888 => 7777";
                                this.output_builder.AppendLine(output_text);
                                Console.WriteLine(output_text);
                            }
                        }	
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }		
                }


                foreach(var node in  eight_to_7_sfmv)
                {
                    var multivalue_result = gs.get_value(doc, node.path);

                    try
                    {
                        if(!multivalue_result.is_error)
                        {
                            var list =  multivalue_result.result as IList<object>;

                            if(list != null)
                            {
                                List<int> new_list = new List<int>();
                                var is_list_changed = false;
                                
                                for(int i = 0; i < list.Count; i++)
                                {
                                    object value = list[i];

                                    if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                    {
                                        new_list.Add(9999);

                                        continue;	
                                    }

                                    if(value.ToString() == "8888")
                                    {
                                        //if(case_change_count == 0)
                                        //{
                                        //	case_change_count += 1;
                                        //	case_has_changed = true;
                                        //}
                                        is_list_changed = true;
                                        int new_value = 7777;

                                        new_list.Add(new_value);

                                        //case_has_changed = case_has_changed && gs.set_value(node.path, new_value, doc);
                                        //var output_text = $"item record_id: {mmria_id} path:{node.path} Converted 8888 => 7777";
                                        //this.output_builder.AppendLine(output_text);
                                        //Console.WriteLine(output_text);
                                    }
                                    else if(value.ToString()=="American Indian\\/Alaska Native")
                                    {
                                        new_list.Add(2);
                                    }
                                    else
                                    {
                                        new_list.Add(int.Parse(value.ToString()));
                                    }
                                }

                                if(is_list_changed)
                                {
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        case_has_changed = true;
                                    }
                                    

                                    case_has_changed = case_has_changed && gs.set_multi_value(node.path, new_list, doc);
                                    var output_text = $"item record_id: {mmria_id} path:{node.path} Converted as list item 8888 => 7777";
                                    this.output_builder.AppendLine(output_text);
                                    Console.WriteLine(output_text);
                                }
                            }
                        }	
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }		
                }


                foreach(var node in  eight_to_7_sfgv)
                {
                    var grid_value_result = gs.get_grid_value(doc, node.path);
                    try
                    {	
                        if(!grid_value_result.is_error)
                        {
                            var list =  grid_value_result.result as IList<(int, object)>;

                            if(list != null)
                            {
                                var new_list = new List<(int, object)>();
                                var is_list_changed = false;

                                var output_text = new System.Text.StringBuilder();
                                for(int i = 0; i < list.Count; i++)
                                {
                                    (int, object) tuple_value = list[i];
                                    var value = tuple_value.Item2;

                                    if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                    {
                                        //is_list_changed = true;
                                        new_list.Add((tuple_value.Item1, 9999));
                                        //output_text.AppendLine($"item record_id: {mmria_id} path:{node.path} grid_index:{tuple_value.Item1} Converted  null => 9999");
                                        continue;	
                                    }

                                    if(value.ToString() == "8888")
                                    {
                                        //if(case_change_count == 0)
                                        //{
                                        //	case_change_count += 1;
                                        //	case_has_changed = true;
                                        //}
                                        is_list_changed = true;
                                        object new_value = 7777;

                                        new_list.Add((tuple_value.Item1, new_value));
                                        output_text.AppendLine($"item record_id: {mmria_id} path:{node.path} grid_index:{tuple_value.Item1} Converted  8888 => 7777");

                                        //case_has_changed = case_has_changed && gs.set_value(node.path, new_value, doc);
                                        //var output_text = $"item record_id: {mmria_id} path:{node.path} Converted 8888 => 7777";
                                        //this.output_builder.AppendLine(output_text);
                                        //Console.WriteLine(output_text);
                                    }
                                    else
                                    {
                                        new_list.Add((tuple_value.Item1, value));
                                    }
                                }

                                if(is_list_changed)
                                {
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        case_has_changed = true;
                                    }
                                    

                                    case_has_changed = case_has_changed && gs.set_grid_value(doc, node.path, new_list);
                                    
                                    this.output_builder.AppendLine(output_text.ToString());
                                    Console.WriteLine(output_text);
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }			
                }

                foreach(var node in  eight_to_7_mv)
                {
                    var multiform_value_result = gs.get_multiform_value(doc, node.path);
                    
                    try
                    {
                        if(!multiform_value_result.is_error)
                        {
                            var list = multiform_value_result.result as IList<(int, object)>;
                            var new_list = new List<(int, object)>();
                            var is_list_changed = false;

                            var output_text = new System.Text.StringBuilder();
                            for(var i = 0; i < list.Count; i++)
                            {
                                var index = list[i].Item1;
                                var value = list[i].Item2;
                                
                                if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                {
                                    //is_list_changed = true;
                                    new_list.Add((index, 9999));
                                    //output_text.AppendLine($"item record_id: {mmria_id} path:{node.path} grid_index:{index} Converted null => 9999");
                                    continue;	
                                }

                                if(value.ToString() == "8888")
                                {
                                    
                                    
                                    object new_value = 7777;
                                    new_list.Add((index, new_value));
                                    is_list_changed = true;
                                    output_text.AppendLine($"item record_id: {mmria_id} path:{node.path} grid_index:{index} Converted 8888 => 7777");
                                }
                                else
                                {
                                    new_list.Add((index, value));
                                }
                            }


                            if(is_list_changed)
                            {
                                if(case_change_count == 0)
                                {
                                    case_change_count += 1;
                                    case_has_changed = true;
                                }

                                case_has_changed = case_has_changed && gs.set_multiform_value(doc, node.path, new_list);
                                
                                this.output_builder.AppendLine(output_text.ToString());
                                Console.WriteLine(output_text);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }	
                }

                foreach(var node in  eight_to_7_mmv)
                {
                    var multiform_value_result = gs.get_multiform_value(doc, node.path);

                    try
                    {	
                        if(!multiform_value_result.is_error)
                        {
                            var list = multiform_value_result.result as IList<(int, object)>;
                            var new_list = new List<(int, object)>();
                            var is_list_changed = false;

                            var output_text = new System.Text.StringBuilder();
                            foreach(var (form_index, original_value) in list)
                            {
                                var value_list = original_value as IList<object>;
                                if(value_list == null)
                                {
                                    value_list = new List<object>();
                                }
                                var new_value_list = new List<object>();
                                foreach(var value in value_list)
                                {
                                    if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                    {
                                        //is_list_changed = true;
                                        new_value_list.Add(9999);
                                        //output_text.AppendLine($"item record_id: {mmria_id} path:{node.path} form_index:{form_index} list_index:{new_value_list.Count -1} null => 9999");
                                        continue;	
                                    }

                                    if(value.ToString() == "8888")
                                    {
                                        
                                        
                                        object new_value = 7777;
                                        new_value_list.Add(new_value);
                                        is_list_changed = true;
                                        output_text.AppendLine($"item record_id: {mmria_id} path:{node.path} form_index:{form_index} list_index:{new_value_list.Count -1} 8888 => 7777");
                                    }
                                    else
                                    {
                                        new_value_list.Add(value);
                                    }
                                }
                                new_list.Add((form_index, new_value_list));
                            }


                            if(is_list_changed)
                            {
                                if(case_change_count == 0)
                                {
                                    case_change_count += 1;
                                    case_has_changed = true;
                                }

                                case_has_changed = case_has_changed && gs.set_multiform_value(doc, node.path, new_list);
                                
                                this.output_builder.AppendLine(output_text.ToString());
                                Console.WriteLine(output_text);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }		
                }

                foreach(var node in  eight_to_7_mgv)
                {
                    var multiform_grid_value_result = gs.get_multiform_grid_value(doc, node.path);
                    
                    try
                    {
                        if(!multiform_grid_value_result.is_error)
                        {
                            var list = multiform_grid_value_result.result as IList<(int, int, object)>;
                            var new_list = new List<(int, int, object)>();
                            var is_list_changed = false;

                            var output_text = new System.Text.StringBuilder();

                            for(var i = 0; i < list.Count; i++)
                            {
                                var (form_index, grid_index, value) = list[i];
                                
                                if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                {
                                    //is_list_changed = true;
                                    new_list.Add((form_index, grid_index, 9999));
                                    //output_text.AppendLine($"item record_id: {mmria_id} path:{node.path} form_index:{form_index} grid_index:{grid_index} null => 9999");
                                    continue;	
                                }

                                if(value.ToString() == "8888")
                                {
                                    
                                    
                                    object new_value = 7777;
                                    new_list.Add((form_index, grid_index, new_value));
                                    is_list_changed = true;
                                    output_text.AppendLine($"item record_id: {mmria_id} path:{node.path} form_index:{form_index} grid_index:{grid_index} 8888 => 7777");
                                }
                                else
                                {
                                    new_list.Add((form_index, grid_index, value));
                                }
                            }


                            if(is_list_changed)
                            {
                                if(case_change_count == 0)
                                {
                                    case_change_count += 1;
                                    case_has_changed = true;
                                }

                                case_has_changed = case_has_changed && gs.set_multiform_grid_value(doc, node.path, new_list);
                                
                                this.output_builder.AppendLine(output_text.ToString());
                                Console.WriteLine(output_text);
                            }
                            
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }		
                }

                try
                {
                    var mhp_wtdpmh_condi_path = "mental_health_profile/were_there_documented_preexisting_mental_health_conditions";
                    value_result = gs.get_value(doc, mhp_wtdpmh_condi_path);
                    if(!value_result.is_error)
                    {
                        var value = value_result.result;
                        if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            //do nothing;	
                        }
                        else if(value.ToString() == "8888")
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }
                            
                            string new_value = "7777";

                            case_has_changed = case_has_changed && gs.set_value(mhp_wtdpmh_condi_path, new_value, doc);
                            var output_text = $"item record_id: {mmria_id} path:{mhp_wtdpmh_condi_path} Converted 8888 => 7777";
                            this.output_builder.AppendLine(output_text);
                            Console.WriteLine(output_text);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }

                try
                {
                    //bcifsmod_framo_deliv +1 4 -> 7777 	/birth_certificate_infant_fetal_section/method_of_delivery/final_route_and_method_of_delivery
                    var bcifsmod_framo_deliv_path = "birth_certificate_infant_fetal_section/method_of_delivery/final_route_and_method_of_delivery";
                    var multiform_value_result = gs.get_multiform_value(doc, bcifsmod_framo_deliv_path);
                    if(!multiform_value_result.is_error)
                    {
                        var list = multiform_value_result.result;
                        var new_list = new List<(int, object)>();
                        var is_list_changed = false;

                        var output_text = new System.Text.StringBuilder();
                        foreach(var (form_index, value) in list)
                        {
        
                            if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            {
                                //is_list_changed = true;
                                new_list.Add((form_index, 9999));
                                //output_text.AppendLine($"item record_id: {mmria_id} path:{bcifsmod_framo_deliv_path} Converted null => 9999");
                                continue;	
                            }
                            else if(value.ToString() == "8888")
                            {
                                is_list_changed = true;
                                new_list.Add((form_index, 7777));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{bcifsmod_framo_deliv_path} Converted 8888 => 7777");
                            }
                            else if(value.ToString() == "4")
                            {
                                is_list_changed = true;
                                new_list.Add((form_index, 7777));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{bcifsmod_framo_deliv_path} Converted 4 => 7777");
                            }
                            else
                            {
                                new_list.Add((form_index, value));
                            }
                        
                        }

                        if(is_list_changed)
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }

                            case_has_changed = case_has_changed && gs.set_multiform_value(doc, bcifsmod_framo_deliv_path, new_list);
                            
                            this.output_builder.AppendLine(output_text.ToString());
                            Console.WriteLine(output_text);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }

                try
                {
                    //ar_coa_infor -1 + 4 -> 2 /autopsy_report/completeness_of_autopsy_information
                    var ar_coa_infor_path = "autopsy_report/completeness_of_autopsy_information";
                    value_result = gs.get_value(doc, ar_coa_infor_path);
                    if(!value_result.is_error)
                    {
                        var value = value_result.result;
                        if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            // do nothing
                        }
                        else if(value.ToString() == "3")
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }
                            
                            string new_value = "2";

                            case_has_changed = case_has_changed && gs.set_value(ar_coa_infor_path, new_value, doc);
                            var output_text = $"item record_id: {mmria_id} path:{ar_coa_infor_path} Converted 4 => 2";
                            this.output_builder.AppendLine(output_text);
                            Console.WriteLine(output_text);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }

                try
                {
                    //pppcf_pp_type -1 +1 3 -> 4 	/prenatal/primary_prenatal_care_facility/primary_provider_type
                    var pppcf_pp_type_path = "prenatal/primary_prenatal_care_facility/primary_provider_type";
                    value_result = gs.get_value(doc, pppcf_pp_type_path);
                    if(!value_result.is_error)
                    {
                        var value = value_result.result;
                        if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            // do nothing
                        }
                        else if(value.ToString() == "3")
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }
                            
                            string new_value = "4";

                            case_has_changed = case_has_changed && gs.set_value(pppcf_pp_type_path, new_value, doc);
                            var output_text = $"item record_id: {mmria_id} path:{pppcf_pp_type_path} Converted 3 => 4";
                            this.output_builder.AppendLine(output_text);
                            Console.WriteLine(output_text);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }




                try
                {
                    //posopc_p_type -1 +1 3->4 /prenatal/other_sources_of_prenatal_care/provider_type
                    var posopc_p_type_path = "prenatal/other_sources_of_prenatal_care/provider_type";
                    var grid_value_result = gs.get_grid_value(doc, posopc_p_type_path);
                    if(!grid_value_result.is_error)
                    {
                        var new_list = new List<(int, object)>();
                        var is_list_changed = false;
                        var output_text = new System.Text.StringBuilder();
                        foreach(var (grid_index, value) in grid_value_result.result)
                        {
                        
                            if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            {
                                new_list.Add((grid_index, 9999));
                                //is_list_changed = true;
                                //output_text.AppendLine($"item record_id: {mmria_id} path:{posopc_p_type_path} grid_index:{grid_index} Converted null => 9999");
                            }
                            else if(value.ToString() == "3")
                            {
                                object new_value = 4;
                                new_list.Add((grid_index, new_value));
                                is_list_changed = true;
                                output_text.AppendLine($"item record_id: {mmria_id} path:{posopc_p_type_path} grid_index:{grid_index} Converted 3 => 4");
                            }
                            else
                            {
                                new_list.Add((grid_index, value));
                            }
                        }

                        if(is_list_changed)
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }
                            
                            case_has_changed = case_has_changed && gs.set_grid_value(doc, posopc_p_type_path, new_list);
                            
                            this.output_builder.AppendLine(output_text.ToString());
                            Console.WriteLine(output_text);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }

                    
                try
                {
                    //omovmcf_provider_type -1 +1 6->7 Not found 	/other_medical_office_visits/medical_care_facility/provider_type
                    var omovmcf_provider_type_path = "other_medical_office_visits/medical_care_facility/provider_type";
                    var multiform_value_result = gs.get_multiform_value(doc, omovmcf_provider_type_path);
                    if(!multiform_value_result.is_error)
                    {
                        var list = multiform_value_result.result;
                        var new_list = new List<(int, object)>();
                        var is_list_changed = false;

                        var output_text = new System.Text.StringBuilder();
                        foreach(var (form_index, value) in list)
                        {
        
                            if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            {
                                //is_list_changed = true;
                                new_list.Add((form_index, 9999));
                                //output_text.AppendLine($"item record_id: {mmria_id} path:{omovmcf_provider_type_path} form_index:{form_index} Converted null => 9999");
                                continue;	
                            }

                            if(value.ToString() == "6")
                            {
                                is_list_changed = true;
                                new_list.Add((form_index, 7));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{omovmcf_provider_type_path} form_index:{form_index} Converted 6 => 7");
                            }
                            else
                            {
                                new_list.Add((form_index, value));
                            }
                        
                        }

                        if(is_list_changed)
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }

                            case_has_changed = case_has_changed && gs.set_multiform_value(doc, omovmcf_provider_type_path, new_list);
                            
                            this.output_builder.AppendLine(output_text.ToString());
                            Console.WriteLine(output_text);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }


                try
                {
                    var omovlt_d_level_path = "other_medical_office_visits/laboratory_tests/diagnostic_level";
                    var multiform_grid = gs.get_multiform_grid(doc, omovlt_d_level_path, true);
                    if(multiform_grid!= null)
                    {
                        var is_list_changed = false;
                        var new_list = new List<(int,int,dynamic)>();
                        var output_text = new System.Text.StringBuilder();
                        var multiform_list = multiform_grid as List<object>;
                        if(multiform_list!=null)
                        for(var multiform_index = 0; multiform_index < multiform_list.Count; multiform_index++)
                        {

                            var grid_list = multiform_list[multiform_index] as List<object>;
                            if(grid_list!=null)
                            for(var grid_item_index = 0; grid_item_index < grid_list.Count; grid_item_index++)
                            {
                                var item = grid_list[grid_item_index] as IDictionary<string,object>;
                                
                                
                                if(item!=null && item.ContainsKey("diagnostic_level"))
                                {
                                    System.Console.WriteLine($"Diagnostic Level: {item["diagnostic_level"]}");

                                    var value = item["diagnostic_level"];

                                    if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                    {
                                        //is_list_changed = true;
                                        new_list.Add((multiform_index, grid_item_index, "9999"));
                                        //output_text.AppendLine($"item record_id: {mmria_id} path:{omovlt_d_level_path} form_index:{grid_item_index} Converted null => 9999");
                                        //continue;	
                                    }
                                    else if(value.ToString() == "8888")
                                    {
                                        is_list_changed = true;
                                        new_list.Add((multiform_index, grid_item_index, "7777"));
                                        
                                        output_text.AppendLine($"item record_id: {mmria_id} path:{omovlt_d_level_path} form_index:{grid_item_index} Converted 6 => 7");
                                    }
                                    else if(value.ToString() == "1")
                                    {
                                        is_list_changed = true;
                                        new_list.Add((multiform_index, grid_item_index, "2"));
                                        output_text.AppendLine($"item record_id: {mmria_id} path:{omovlt_d_level_path} form_index:{grid_item_index} Converted 6 => 7");
                                    }
                                    else if(value.ToString() == "3")
                                    {
                                        is_list_changed = true;
                                        new_list.Add((multiform_index, grid_item_index, "2"));
                                        output_text.AppendLine($"item record_id: {mmria_id} path:{omovlt_d_level_path} form_index:{grid_item_index} Converted 6 => 7");
                                    }
                                    else if(value.ToString() == "4")
                                    {
                                        is_list_changed = true;
                                        new_list.Add((multiform_index, grid_item_index, "5"));
                                        output_text.AppendLine($"item record_id: {mmria_id} path:{omovlt_d_level_path} form_index:{grid_item_index} Converted 6 => 7");
                                    }
                                    else if(value.ToString() == "6")
                                    {
                                        is_list_changed = true;
                                        new_list.Add((multiform_index, grid_item_index, "5"));
                                        output_text.AppendLine($"item record_id: {mmria_id} path:{omovlt_d_level_path} form_index:{grid_item_index} Converted 6 => 7");
                                    }
                                    else
                                    {
                                        new_list.Add((multiform_index, grid_item_index, value));
                                    }
                                }
                                
                            }
                        }


                        if(is_list_changed)
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }

                            case_has_changed = case_has_changed && gs.set_multiform_grid_value(doc, omovlt_d_level_path, new_list);
                            
                            this.output_builder.AppendLine(output_text.ToString());
                            Console.WriteLine(output_text);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }


                try
                {
                    /*
                    omovdiaot_t_type -1 +8 	/other_medical_office_visits/diagnostic_imaging_and_other_technology/Procedure
                        0 ->CT
                        1 ->CVS
                        2 -> ECG
                        3 -> EEG
                        4 ->MRI
                        5 -> PET
                        6 -> US
                        7 ->Xray
                        8 ->Other
                        */
                    var omovdiaot_t_type_path = "other_medical_office_visits/diagnostic_imaging_and_other_technology/technology_type";
                    var new_omovdiaot_t_type_path = "other_medical_office_visits/diagnostic_imaging_and_other_technology/procedure";
                    var multiform_value_result = gs.get_multiform_grid_value(doc, omovdiaot_t_type_path);
                    if(!multiform_value_result.is_error)
                    {
                        var list = multiform_value_result.result;
                        var new_list = new List<(int, int, object)>();
                        var is_list_changed = false;

                        var output_text = new System.Text.StringBuilder();

                        foreach(var (form_index, grid_index, value) in list)
                        {
        
                            if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            {
                                //is_list_changed = true;
                                new_list.Add((form_index, grid_index, "9999"));
                                //output_text.AppendLine($"item record_id: {mmria_id} path:{omovdiaot_t_type_path} form_index:{form_index} grid_index:{grid_index} Converted null => 9999");
                                continue;	
                            }

                            switch(value.ToString())
                            {
                                case "0":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, "CT"));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{omovdiaot_t_type_path} form_index:{form_index} grid_index:{grid_index} Converted 0 => CT");
                                break;
                                case "1":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, "CVS"));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{omovdiaot_t_type_path} form_index:{form_index} grid_index:{grid_index} Converted 1 => CVS");
                                break;
                                case "2":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, "ECG"));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{omovdiaot_t_type_path} form_index:{form_index} grid_index:{grid_index} Converted 2 => ECG");
                                break;
                                case "3":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, "EEG"));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{omovdiaot_t_type_path} form_index:{form_index} grid_index:{grid_index} Converted 3 => EEG");
                                break;
                                case "4":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, "MRI"));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{omovdiaot_t_type_path} form_index:{form_index} grid_index:{grid_index} Converted 4 => MRI");
                                break;
                                case "5":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, "PET"));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{omovdiaot_t_type_path} form_index:{form_index} grid_index:{grid_index} Converted 5 => PET");
                                break;
                                case "6":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, "US"));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{omovdiaot_t_type_path} form_index:{form_index} grid_index:{grid_index} Converted 6 => US");
                                break;
                                case "7":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, "Xray"));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{omovdiaot_t_type_path} form_index:{form_index} grid_index:{grid_index} Converted 7 => Xray");
                                break;
                                case "8":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, "Other"));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{omovdiaot_t_type_path} form_index:{form_index} grid_index:{grid_index} Converted 8 => Other");
                                break;
                                default:
                                    new_list.Add((form_index, grid_index, value));
                                    break;
                            }
                        
                        }

                        if(is_list_changed)
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }

                            case_has_changed = case_has_changed && gs.set_multiform_grid_value(doc, new_omovdiaot_t_type_path, new_list);
                            
                            this.output_builder.AppendLine(output_text.ToString());
                            Console.WriteLine(output_text);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }



                try
                {
                    //evahmrlt_d_level +4 /er_visit_and_hospital_medical_records/labratory_tests/diagnostic_level
                    var evahmrlt_d_level_path = "er_visit_and_hospital_medical_records/labratory_tests/diagnostic_level";
                    var multiform_value_result = gs.get_multiform_grid_value(doc, evahmrlt_d_level_path);
                    if(!multiform_value_result.is_error)
                    {
                        var list = multiform_value_result.result;
                        var new_list = new List<(int, int, object)>();
                        var is_list_changed = false;

                        var output_text = new System.Text.StringBuilder();
                        foreach(var (form_index, grid_index, value) in list)
                        {
        
                            if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            {
                                //is_list_changed = true;
                                new_list.Add((form_index, grid_index, 9999));
                                //output_text.AppendLine($"item record_id: {mmria_id} path:{evahmrlt_d_level_path} form_index:{form_index} grid_index:{grid_index} Converted null => 9999");
                                continue;	
                            }

                            switch(value.ToString())
                            {
                                case "8888":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, 7777));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{evahmrlt_d_level_path} form_index:{form_index} grid_index:{grid_index} Converted 8888 => 7777");
                                break;
                                case "1":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, 2));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{evahmrlt_d_level_path} form_index:{form_index} grid_index:{grid_index} Converted 1 => 2");
                                break;
                                case "3":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, 2));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{evahmrlt_d_level_path} form_index:{form_index} grid_index:{grid_index} Converted 3 => 2");
                                break;
                                case "4":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, 5));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{evahmrlt_d_level_path} form_index:{form_index} grid_index:{grid_index} Converted 4 => 5");
                                break;
                                case "6":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, 5));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{evahmrlt_d_level_path} form_index:{form_index} grid_index:{grid_index} Converted 6 => 5");
                                break;
                                default:
                                    new_list.Add((form_index, grid_index, value));
                                    break;
                            }
                        
                        }

                        if(is_list_changed)
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }

                            case_has_changed = case_has_changed && gs.set_multiform_grid_value(doc, evahmrlt_d_level_path, new_list);
                            this.output_builder.AppendLine(output_text.ToString());
                            Console.WriteLine(output_text);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }


                    
                try
                {
                    //evahmrba_title +3 /er_visit_and_hospital_medical_records/birth_attendant/title
                    var evahmrba_title_path = "er_visit_and_hospital_medical_records/birth_attendant/title";
                    var multiform_value_result = gs.get_multiform_grid_value(doc, evahmrba_title_path);
                    if(!multiform_value_result.is_error)
                    {
                        var list = multiform_value_result.result;
                        var new_list = new List<(int, int, object)>();
                        var is_list_changed = false;

                        var output_text = new System.Text.StringBuilder();
                        foreach(var (form_index, grid_index, value) in list)
                        {
        
                            if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            {
                                //is_list_changed = true;
                                new_list.Add((form_index, grid_index, 9999));
                                //output_text.AppendLine($"item record_id: {mmria_id} path:{evahmrba_title_path} form_index:{form_index} grid_index:{grid_index} Converted null => 9999");
                                continue;	
                            }

                            switch(value.ToString())
                            {
                                case "8888":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, 7777));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{evahmrba_title_path} form_index:{form_index} grid_index:{grid_index} Converted 8888 => 7777");
                                break;
                                case "3":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, 6));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{evahmrba_title_path} form_index:{form_index} grid_index:{grid_index} Converted 3 => 6");
                                break;
                                case "5":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, 6));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{evahmrba_title_path} form_index:{form_index} grid_index:{grid_index} Converted 5 => 6");
                                break;
                                case "4":
                                is_list_changed = true;
                                new_list.Add((form_index, grid_index, 7));
                                output_text.AppendLine($"item record_id: {mmria_id} path:{evahmrba_title_path} form_index:{form_index} grid_index:{grid_index} Converted 4 => 7");
                                break;
                                default:
                                    new_list.Add((form_index, grid_index, value));
                                    break;
                            }
                        
                        }

                        if(is_list_changed)
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }

                            case_has_changed = case_has_changed && gs.set_multiform_grid_value(doc, evahmrba_title_path, new_list);
                            
                            this.output_builder.AppendLine(output_text.ToString());
                            Console.WriteLine(output_text);
                        }
                    }
                    

                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }


                try
                {
                    //	dcdi_p_statu – 7777 has been removed.
                    // 7.	Map existing: Not Specified (#8888)  Unknown if pregnant in last year (#88)
                    var dcdi_p_statu_path = "death_certificate/death_information/pregnancy_status";

                    value_result = gs.get_value(doc, dcdi_p_statu_path);
                    if(!value_result.is_error)
                    {
                        var value = value_result.result;
                        if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            // do nothing for now
                        }
                        else if(value.ToString() == "8888")
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }
                            
                            string new_value = "88";

                            case_has_changed = case_has_changed && gs.set_value(dcdi_p_statu_path, new_value, doc);
                            var output_text = $"item record_id: {mmria_id} path:{dcdi_p_statu_path} Converted 8888 => 88";
                            this.output_builder.AppendLine(output_text);
                            Console.WriteLine(output_text);
                        }
                    }	
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }	


// bfdcpdofr_ro_fathe – there is no “7777” response option
// var bfdcpdofr_ro_fathe_path = "birth_fetal_death_certificate_parent/demographic_of_father/race/race_of_father";

// ar_coa_infor – there is no “3” response option
// 3.	Map existing Minimal (#3)  Minor Gaps (#2)
//var ar_coa_infor_path = "autopsy_report/completeness_of_autopsy_information";



// evahmr_pathology
//er_visit_and_hospital_medical_records/pathology/date_and_time



/*

ar_wa_perfo
autopsy_report/was_autopsy_performed

9999	(blank)	
0	Referred/Performed/Available	
1	Referred/Performed/Not available	
2	Referred/Not performed	
3	Not Referred




ar_autopsy_referral
0,1,2 -> 1 (Yes)
3 -> 0 (No)

ar_autopsy_type
0,1 -> 1 (Yes)
2 -> 0 (No)

ar_autopsy_report
0 -> 1 (Yes)
1 -> 0 (No)


*/

/*
var  omovmcf_p_type = "";
For the Field: "omovmcf_p_type" Option #4 - No Prenatal Care was requested to be removed. I forgot to provide a data migration for existing data. They all need to go to #9999 - Blank.   
So for variable: omovmcf_p_type the data migration rule is: { "4", "9999"}

*/


                try
                {
                    var ar_wa_perfo_path = "autopsy_report/was_autopsy_performed";
                    value_result = gs.get_value(doc, ar_wa_perfo_path);

                    if(!value_result.is_error)
                    {
                        var value = value_result.result;

                        if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            // do nothing for now
                        }
                        else 
                        {

                            var output_text = "";
                            var yes_value = "1";
                            var no_value = "0";

                            var ar_autopsy_referral_path = "autopsy_report/was_there_an_autopsy_referral";



                                /*
                                
                                ar_autopsy_referral
                                0,1,2 -> 1 (Yes)
                                3 -> 0 (No)
                                */
                            switch(value.ToString())
                            {

                                case "0":
                                
                                break;
                                case "1":
                                break;
                                case "2":
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        case_has_changed = true;
                                    }
                                    
                                    case_has_changed = case_has_changed && gs.set_value(ar_autopsy_referral_path, yes_value, doc);
                                    output_text = $"item record_id: {mmria_id} path:{ar_autopsy_referral_path} Converted  {value} => {yes_value}";
                                    this.output_builder.AppendLine(output_text);
                                    Console.WriteLine(output_text);
                                break;
                                case "3":
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        case_has_changed = true;
                                    }
                                    case_has_changed = case_has_changed && gs.set_value(ar_autopsy_referral_path, no_value, doc);
                                    output_text = $"item record_id: {mmria_id} path:{ar_autopsy_referral_path} Converted  {value} => {no_value}";
                                    this.output_builder.AppendLine(output_text);
                                    Console.WriteLine(output_text);
                                break;
                                case "7777":
                                case "9999":
                                break;
                            }

                            
                            var ar_autopsy_type_path = "autopsy_report/type_of_autopsy_or_examination";

                                /*
                                ar_autopsy_type
                                0,1 -> 1 (Yes)
                                2 -> 0 (No)
                                */
                            switch(value.ToString())
                            {

                                case "0":
                                case "1":
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        case_has_changed = true;
                                    }
                                    case_has_changed = case_has_changed && gs.set_value(ar_autopsy_type_path, yes_value, doc);
                                    output_text = $"item record_id: {mmria_id} path:{ar_autopsy_type_path} Converted  {value} => {yes_value}";
                                    this.output_builder.AppendLine(output_text);
                                    Console.WriteLine(output_text);
                                break;
                                case "2":
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        case_has_changed = true;
                                    }
                                    case_has_changed = case_has_changed && gs.set_value(ar_autopsy_type_path, no_value, doc);
                                    output_text = $"item record_id: {mmria_id} path:{ar_autopsy_type_path} Converted  {value} => {no_value}";
                                    this.output_builder.AppendLine(output_text);
                                    Console.WriteLine(output_text);
                                break;
                                case "3":
                                case "7777":
                                case "9999":
                                break;
                            }
                        

                            var ar_autopsy_report_path = "autopsy_report/is_autopsy_or_exam_report_available";


                                /*
                                ar_autopsy_report
                                0 -> 1 (Yes)
                                1 -> 0 (No)
                                */

                            switch(value.ToString())
                            {
                                case "0":
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        case_has_changed = true;
                                    }
                                    case_has_changed = case_has_changed && gs.set_value(ar_autopsy_report_path, yes_value, doc);
                                    output_text = $"item record_id: {mmria_id} path:{ar_autopsy_report_path} Converted {value} => {yes_value}";
                                    this.output_builder.AppendLine(output_text);
                                    Console.WriteLine(output_text);
                                break;
                                case "1":
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        case_has_changed = true;
                                    }
                                    case_has_changed = case_has_changed && gs.set_value(ar_autopsy_report_path, no_value, doc);
                                    output_text = $"item record_id: {mmria_id} path:{ar_autopsy_report_path} Converted  {value} => {no_value}";
                                    this.output_builder.AppendLine(output_text);
                                    Console.WriteLine(output_text);
                                break;
                                case "2":
                                case "3":
                                case "7777":
                                case "9999":
                                break;
                            }
                        }
                    }	
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }	


                try
                {
                    var dcdi_mo_death_path = "death_certificate/death_information/manner_of_death";
                    value_result = gs.get_value(doc, dcdi_mo_death_path);
                    if(!value_result.is_error)
                    {
                        var value = value_result.result;
                        if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            //do nothing;	
                        }
                        else if(value.ToString() == "8888")
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }
                            
                            string new_value = "7777";

                            case_has_changed = case_has_changed && gs.set_value(dcdi_mo_death_path, new_value, doc);
                            var output_text = $"item record_id: {mmria_id} path:{dcdi_mo_death_path} Converted 8888 => 7777";
                            this.output_builder.AppendLine(output_text);
                            Console.WriteLine(output_text);
                        }
                        else if(value.ToString() == "4")
                        {
                            if(case_change_count == 0)
                            {
                                case_change_count += 1;
                                case_has_changed = true;
                            }
                            
                            string new_value = "3";

                            case_has_changed = case_has_changed && gs.set_value(dcdi_mo_death_path, new_value, doc);
                            var output_text = $"item record_id: {mmria_id} path:{dcdi_mo_death_path} Converted 4 => 3";
                            this.output_builder.AppendLine(output_text);
                            Console.WriteLine(output_text);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }


                if(!is_report_only_mode && case_has_changed)
                {
                    var save_result = await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(doc as IDictionary<string, object>,"v2.4 part1");
                }

            }

        
        }
    }
    catch(Exception ex)
    {
        Console.WriteLine(ex);
    }

    Console.WriteLine($"v2_4_Migration Finished {DateTime.Now}");
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
    }*/

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

/*
example usage
            var pregnancy_relatedness_set_node = get_metadata_node(metadata, "committee_review/pregnancy_relatedness");
            foreach(var item in pregnancy_relatedness_set_node.values)
            {
                pregnancy_relatedness_set.Add(item.value);
            }
*/

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



/*
bcifsmod_framo_deliv +1 4 -> 7777
ar_coa_infor -1 + 3 -> 2
pppcf_pp_type -1 +1 3 -> 4
posopc_p_type -1 +1 3->4
omovmcf_provicer_type -1 +1 6->7
omovdiaot_t_type -1 +8

evahmrlt_d_level +4
evahmrba_title +3
*/

            // pmss migration - start
            // 24
/*
            Migrate 10 -> 10.9
            Migrate Existing 20->20.9)
            Migrate Existing 30->30.1)
            Migrate Existing 31->31.1)
            Migrate Existing 60->60.1)
            Migrate Existing 40->40.1)
            Migrate Existing 50->50.1)
            Migrate Existing 70->70.1)
            Migrate Existing 80->80.9)
            Migrate Existing 82->82.9)
            Migrate Existing 83->83.9)
            Migrate Existing 85->85.1)
Migrate Existing 90->90.9)
Migrate Existing 89->89.9)
Migrate Existing 90->90.9)
Migrate Existing 91->91.9)
 Migrate Existing 92->92.9)
Migrate Existing 93->93.9)
Migrate Existing 95->95.1)
Migrate Existing 96->96.9)
Migrate Existing 97->97.9)
Migrate Existing 100->100.9)
Migrate Existing 999->999.1)
*/

            // pmss migration - end

// migration list
// 4.	Map existing Not Specified (#8888) -> Unknown (#7777)
/*

dcd_eiua_force 
dcd_ioh_origi
dcd_e_level
dciai_wia_work
dciai_wsbi_use
dcdi_doi_hospi
dcdi_doo_hospi
dcdi_mo_death
dcdi_wa_perfo
dcdi_waufd_codin
dcdi_p_statu
dcdi_dtct_death
bfdcpfodd_whd_plann
bfdcpfodd_a_type
bfdcpfodd_wm_trans
bfdcpdof_e_level
bfdcpdof_ifoh_origi
bfdcpdofr_ro_fathe
bfdcpdom_m_marri
bfdcpdom_Imnmhpabsit_hospi
bfdcpdom_eiua_force
bfdcpdom_ioh_origi
bfdcpdom_e_level
bfdcppc_plura
bfdcppc_ww_used
bfdcpcs_non_speci
bfdcprf_rfit_pregn
bfdcp_ipotd_pregn
bfdcp_oo_labor
bfdcp_o_proce
bfdcp_cola_deliv
bfdcp_m_morbi
bcifs_im_gesta
bcifsbad_gende
bcifsbad_iilato_repor
bcifsbad_iibba_disch
bcifsbad_witw2_hours
bcifsmod_wdwfab_unsuc
bcifsmod_wdwveab_unsuc
bcifsmod_f_deliv
bcifsmod_framo_deliv +1 4 -> 7777
bcifsmod_icwtol_attem
bcifs_aco_newbo

ar_coa_infor -1 + 3 -> 2
arrc_r_type
art_level
pppcf_p_type

pppcf_pp_type -1 +1 3 -> 4
pppcf_iu_wic
p_hpe_condi
p_wtdmh_condi
pfmh_i_livin
p_eos_use
psug_scree
psug_c_educa
pphdg_in_livin
pi_wp_plann
pi_wpub_contr
pit_wproi_treat
pit_fe_drugs
pit_ar_techn
pcp_whd_plann
pcp_apv_alone
p_wtp_ident
p_wta_react
pmaddp_ia_react
p_wtpd_hospi
p_wmrt_other
pmr_wa_kept
posopc_place
posopc_p_type -1 +1 3->4

evahmrbaadi_a_condi
evahmrbaadi_wrfa_hospi
evahmrbaadi_wtta_hospi
evahmrbaadi_dp_statu
evahmrbaadi_da_disch
evahmrnalf_mott_facil
evahmrnalf_oo_trave

evahmrlt_d_level +4
    1->2
    3->2
    4->5
    6->5

evahmrool_fd_route
evahmrool_m_gesta

evahmrba_title +3
    3->6
    5->6
    4->7

evahmr_wtco_anest
evahmr_aa_react
evahmr_as_proce
evahmr_ab_trans
omovv_v_type
omovmcf_p_type

omovmcf_provicer_type -1 +1 6->7
omovmcf_wtphppc_provi

omovlt_d_level +4
    1->2
    3->2
    4->5
    6->5



omovdiaot_t_type -1 +8
0 ->CT
1 ->CVS
2 -> ECG
3 -> EEG
4 ->MRI
5 -> PET
6 -> US
7 ->Xray
8 ->Other

saepsec_so_incom
saepsec_e_statu
saepsec_cl_arran
saepsec_homel
saepmoh_relat
saepmoh_gende
saepsamr_compi
saep_ds_use
mhp_wtdpmh_cond
mhpwtdmhc_rf_treat

            */