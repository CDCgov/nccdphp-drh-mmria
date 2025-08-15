using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public sealed class v2_7_Migration
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


    public v2_7_Migration
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
        this.output_builder.AppendLine($"v2.7 Data Migration started at: {DateTime.Now.ToString("o")}");
        DateTime begin_time = System.DateTime.Now;
        
        this.output_builder.AppendLine($"v2_7_Migration started at: {begin_time.ToString("o")}");
        
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
/*
            foreach(var item in sf)
            {
                Console.WriteLine($"{item.sass_export_name} - {item.path}");
            }
*/

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

/*
            foreach (var firstItem in eight_to_7_list)
            {

                if (!eight_to_7_count.Contains(firstItem))
                {

                    Console.WriteLine(firstItem);
                }
            }*/

            var pmss_map = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
            {
                { "88", "88.9"},
                /*
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
                { "999", "999.1"}*/
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
/*
1a) When there is a value in existing data field: Fetal Weight (grams) [arbf_f_weigh] then set the Fetal Weight UOM field [arbf_f_weigh_uom] to the Value [0] (equivalent to Grams).

1b) If [arbf_f_weigh] is empty/blank/null, then set [arbf_f_weigh_uom] to 9999 (equivalent to Missing)

2a) When there is a value in existing data field: Fetal Length (inches) [arbf_f_lengt] then set the Fetal Length UOM field [arbf_f_lengt_uom] to the Value [0] (equivalent to Inches)

2b) If [arbf_f_lengt] is empty/blank/null, then set [arbf_f_lengt_uom] to 9999 (equivalent to Missing)


arbf_f_weigh = autopsy_report/biometrics/fetus/fetal_weight
arbf_f_weigh_uom = autopsy_report/biometrics/fetus/fetal_weight_uom
arbf_f_lengt = autopsy_report/biometrics/fetus/fetal_length
arbf_f_lengt_uom = autopsy_report/biometrics/fetus/fetal_length_uom


arbf_f_weigh_oz = autopsy_report/biometrics/fetus/fetal_weight_ounce_value


*/


int? arbf_f_weight = null;
int? arbf_f_weight_uom = null;
int? arbf_f_length = null;
int? arbf_f_length_uom = null;

bool arbf_f_weight_error = true;
bool arbf_f_length_error = true;

value_result = gs.get_value(doc, "autopsy_report/biometrics/fetus/fetal_weight");
arbf_f_weight_error = value_result.is_error;
if(value_result.result is not null)
{

if
(
    value_result.result is string arbf_f_weight_string
)
{
    if(int.TryParse(arbf_f_weight_string, out int test_int))
    arbf_f_weight = test_int;
}
else if(value_result.result is Int64)
{
    arbf_f_weight = (int) value_result.result;
}


}


var arbf_f_weight_uom_path = "autopsy_report/biometrics/fetus/fetal_weight_uom";
value_result = gs.get_value(doc, arbf_f_weight_uom_path);
if(value_result.result is not null)
{
if
(
    value_result.result is string fetal_weight_uom_string
)
{
    if(int.TryParse(fetal_weight_uom_string, out int test_int))
    arbf_f_weight_uom = test_int;
}
else if(value_result.result is Int64)
{
    arbf_f_weight_uom = (int) value_result.result;
}
}


value_result = gs.get_value(doc, "autopsy_report/biometrics/fetus/fetal_length");
arbf_f_length_error = value_result.is_error;
if(value_result.result is not null)
{
if
(
    value_result.result is string fetal_length_string 
)
{
    if(int.TryParse(fetal_length_string, out int test_int))
    arbf_f_length = test_int;
}
else if(value_result.result is Int64)
{
    arbf_f_length = (int) value_result.result;
}
}

var fetal_length_uom_path = "autopsy_report/biometrics/fetus/fetal_length_uom";
value_result = gs.get_value(doc, fetal_length_uom_path);
if(value_result.result is not null)
{
if
(
    value_result.result is string fetal_length_uom_string 
)
{
    if(int.TryParse(fetal_length_uom_string, out int test_int))
    arbf_f_length_uom = test_int;
}
else if(value_result.result is Int64)
{
    arbf_f_length_uom = (int) value_result.result;
    
}
}

if(!arbf_f_weight_error)
{
if(arbf_f_weight.HasValue)
{
    //1a) When there is a value in existing data field: Fetal Weight (grams) [arbf_f_weigh] then set the Fetal Weight UOM field [arbf_f_weigh_uom] to the Value [0] (equivalent to Grams).
    if(case_change_count == 0)
    {
        case_change_count += 1;
        case_has_changed = true;
    }
    
    case_has_changed = case_has_changed && gs.set_value(arbf_f_weight_uom_path, "0", doc);
    var output_text = $"item record_id: {mmria_id} path:{arbf_f_weight_uom_path} set to => {0} Gram";
    this.output_builder.AppendLine(output_text);
    Console.WriteLine(output_text);
}
else if (! arbf_f_weight_uom.HasValue)
{
    //1b) If [arbf_f_weigh] is empty/blank/null, then set [arbf_f_weigh_uom] to 9999 (equivalent to Missing)
    if(case_change_count == 0)
    {
        case_change_count += 1;
        case_has_changed = true;
    }
    
    case_has_changed = case_has_changed && gs.set_value(arbf_f_weight_uom_path, "9999", doc);
    var output_text = $"item record_id: {mmria_id} path:{arbf_f_weight_uom_path} set to => {9999} (blank)";
    this.output_builder.AppendLine(output_text);
    Console.WriteLine(output_text);
}
}

if(!arbf_f_length_error)
{
if(arbf_f_length.HasValue)
{
    //2a) When there is a value in existing data field: Fetal Length (inches) [arbf_f_lengt] then set the Fetal Length UOM field [arbf_f_lengt_uom] to the Value [0] (equivalent to Inches)
    if(case_change_count == 0)
    {
        case_change_count += 1;
        case_has_changed = true;
    }
    
    case_has_changed = case_has_changed && gs.set_value(fetal_length_uom_path, "0", doc);
    var output_text = $"item record_id: {mmria_id} path:{fetal_length_uom_path} set to => {0} Inches";
    this.output_builder.AppendLine(output_text);
    Console.WriteLine(output_text);
}
else if (! arbf_f_length_uom.HasValue)
{
    //2b) If [arbf_f_lengt] is empty/blank/null, then set [arbf_f_lengt_uom] to 9999 (equivalent to Missing)
    if(case_change_count == 0)
    {
        case_change_count += 1;
        case_has_changed = true;
    }
    
    case_has_changed = case_has_changed && gs.set_value(fetal_length_uom_path, "9999", doc);
    var output_text = $"item record_id: {mmria_id} path:{fetal_length_uom_path} set to => {9999} (blank)";
    this.output_builder.AppendLine(output_text);
    Console.WriteLine(output_text);
}
}




                if(!is_report_only_mode && case_has_changed)
                {
                    var save_result = await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(doc as IDictionary<string, object>,"v2.7 part1");
                }

            }

        
        }
    }
    catch(Exception ex)
    {
        Console.WriteLine(ex);
    }

    Console.WriteLine($"v2_7_Migration Finished {DateTime.Now}");
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