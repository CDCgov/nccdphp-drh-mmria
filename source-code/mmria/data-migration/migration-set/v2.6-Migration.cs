using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public sealed class v2_6_Migration
{

    private string data_migration_name = "v2.6";
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

    private string state_prefix;

    public v2_6_Migration
    (
        string p_host_db_url, 
        string p_db_name, 
        string p_config_timer_user_name, 
        string p_config_timer_value,
        System.Text.StringBuilder p_output_builder,
        Dictionary<string, HashSet<string>> p_summary_value_dictionary,
        bool p_is_report_only_mode,
        string p_state_prefix
    ) 
    {

        host_db_url = p_host_db_url;
        db_name = p_db_name;
        config_timer_user_name = p_config_timer_user_name;
        config_timer_value = p_config_timer_value;
        output_builder = p_output_builder;
        summary_value_dictionary = p_summary_value_dictionary;
        is_report_only_mode = p_is_report_only_mode;
        state_prefix = p_state_prefix;
    }


    public async Task execute()
    {
        this.output_builder.AppendLine($"v2.6 Data Migration started at: {DateTime.Now.ToString("o")}");
        DateTime begin_time = System.DateTime.Now;
        
        this.output_builder.AppendLine($"{data_migration_name} started at: {begin_time.ToString("o")}");
        
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

            string url = $"{host_db_url}/{db_name}/_all_docs?include_docs=true";
            var case_curl = new cURL("GET", null, url, null, config_timer_user_name, config_timer_value);
            string responseFromServer = await case_curl.executeAsync();
            
            var race_value_list = this.lookup["lookup/race"];
            var omb_race_recode_list = this.lookup["lookup/omb_race_recode"];
            var value_to_display = new Dictionary<int,string>();
            var display_to_value = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach(var item in race_value_list)
            {
                value_to_display.Add(int.Parse(item.value), item.display);
                
            }

            foreach(var item in omb_race_recode_list)
            {
                display_to_value.Add(item.display, int.Parse(item.value));
            }

            display_to_value.Add("Black or African American", 1);
            display_to_value.Add("American Indian or Alaska Native", 3);

            HashSet<string> prefix_list = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ny",
                "pa",
                "fl_dev"
            };


            if(prefix_list.Contains(state_prefix))
            {
                //await update_case_folder_tree(state_prefix);
                //await update_jurisdiction_roles(state_prefix);

            }




            var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<System.Dynamic.ExpandoObject>>(responseFromServer);
            var test_lower_case_regex = new System.Text.RegularExpressions.Regex("[a-z]");
            
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

                    try
                    {
                        var record_id_path = "home_record/record_id";
                        value_result = gs.get_value(doc, record_id_path);
                        var test_record_id_object = value_result.result;
                        if
                        (
                            !string.IsNullOrWhiteSpace(test_record_id_object.ToString())
                        )
                        {
                            if(test_lower_case_regex.IsMatch(test_record_id_object.ToString()))
                            {
                                if(case_change_count == 0)
                                {
                                    case_change_count += 1;
                                    case_has_changed = true;
                                }
                                var record_id = test_record_id_object.ToString().ToUpper();
                                case_has_changed = case_has_changed && gs.set_value(record_id_path, record_id, doc);
                                var output_text = $"item _id: {mmria_id} Converted {record_id_path} to uppercase.  {value_result.result} => {record_id}";
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
                        var addquarter_path = "addquarter";
                        var date_created_path = "date_created";
                        value_result = gs.get_value(doc, addquarter_path);
                        var test_addquarter_object = value_result.result;
                        if
                        (
                            test_addquarter_object == null ||
                            string.IsNullOrWhiteSpace(test_addquarter_object.ToString())
                        )
                        {
                            value_result = gs.get_value(doc, date_created_path);
                            var new_value = get_year_and_quarter(value_result.result);
                            if(!string.IsNullOrWhiteSpace(new_value))
                            {
                                if(case_change_count == 0)
                                {
                                    case_change_count += 1;
                                    case_has_changed = true;
                                }
                                
                                case_has_changed = case_has_changed && gs.set_value(addquarter_path, new_value, doc);
                                var output_text = $"item _id: {mmria_id} updated {addquarter_path}: {test_addquarter_object} => {new_value}";
                                this.output_builder.AppendLine(output_text);
                                Console.WriteLine(output_text);
                            }
                            else
                            {

                                value_result = gs.get_value(doc, "home_record/record_id");
                                test_addquarter_object = value_result.result;
                                if
                                (
                                    test_addquarter_object != null ||
                                    !string.IsNullOrWhiteSpace(test_addquarter_object.ToString())
                                )
                                {
                                    var arr = test_addquarter_object.ToString().Split("-");
                                    int test_year = 0;
                                    if
                                    (
                                        arr.Length > 1 &&
                                        !string.IsNullOrWhiteSpace(arr[1]) &&
                                        int.TryParse(arr[1], out test_year) &&
                                        test_year >= 1900 &&
                                        test_year <=2100

                                    )
                                    {
                                        new_value = $"Q1-{test_year}";											
                                        
                                        if(case_change_count == 0)
                                        {
                                            case_change_count += 1;
                                            case_has_changed = true;
                                        }
                                        
                                        case_has_changed = case_has_changed && gs.set_value(addquarter_path, new_value, doc);
                                        var output_text = $"item _id: {mmria_id} updated {addquarter_path}: {test_addquarter_object} => {new_value}";
                                        this.output_builder.AppendLine(output_text);
                                        Console.WriteLine(output_text);
                                    }
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
                        var cmpquarter_path = "cmpquarter";
                        var committee_review_date_of_review_path = "committee_review/date_of_review";
                        value_result = gs.get_value(doc, cmpquarter_path);
                        var test_cmpquarter_object = value_result.result;
                        if
                        (
                            test_cmpquarter_object == null ||
                            string.IsNullOrWhiteSpace(test_cmpquarter_object.ToString())
                        )
                        {
                            value_result = gs.get_value(doc, committee_review_date_of_review_path);
                            var new_value = get_year_and_quarter(value_result.result);
                            if(!string.IsNullOrWhiteSpace(new_value))
                            {
                                if(case_change_count == 0)
                                {
                                    case_change_count += 1;
                                    case_has_changed = true;
                                }
                                
                                case_has_changed = case_has_changed && gs.set_value(cmpquarter_path, new_value, doc);
                                var output_text = $"item _id: {mmria_id} updated {cmpquarter_path} : {test_cmpquarter_object} => {new_value}";
                                this.output_builder.AppendLine(output_text);
                                Console.WriteLine(output_text);
                            }
                            
                        }

                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

/*
                    if(prefix_list.Contains(state_prefix))
                    {
                        // update jurisdiction_id on case 

                        try
                        {
                            var home_record_jurisdiction_id_path = "home_record/jurisdiction_id";
                            value_result = gs.get_value(doc, home_record_jurisdiction_id_path);
                            var test_dynamic = value_result.result;
                            if
                            (
                                test_dynamic != null ||
                                !string.IsNullOrWhiteSpace(test_dynamic.ToString())
                            )
                            {
                                var current_value = test_dynamic.ToString();

                                if(current_value == "/Philadelphia" || current_value == "/nyc")
                                {
                                    if(case_change_count == 0)
                                    {
                                        case_change_count += 1;
                                        case_has_changed = true;
                                    }
                                    
                                    case_has_changed = case_has_changed && gs.set_value(home_record_jurisdiction_id_path, "/Shared", doc);
                                    var output_text = $"item _id: {mmria_id} updated {home_record_jurisdiction_id_path} : {current_value} => /Shared";
                                    this.output_builder.AppendLine(output_text);
                                    Console.WriteLine(output_text);
                                
                                }
                                
                            }

                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        
                    }*/



string primary_occupation = null;
string business_industry = null;

//OCCUP
//mor_field_set["OCCUP"]
var item_result = gs.get_value(doc, "death_certificate/demographics/primary_occupation");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    primary_occupation = item_result.result.ToString();
}

//INDUST
item_result = gs.get_value(doc, "death_certificate/demographics/occupation_business_industry");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    business_industry = item_result.result.ToString();
}
var niosh_result = get_niosh_codes
(
    primary_occupation,
    business_industry
);

if
(
    !niosh_result.is_error && 
    (
        niosh_result.Industry.Count > 0 ||
        niosh_result.Occupation.Count > 0 
    )
)
{   
                
    if(case_change_count == 0)
    {
        case_change_count += 1;
        case_has_changed = true;
    }
    
    case_has_changed = case_has_changed && gs.set_value("death_certificate/demographics/dc_m_industry_code_1", niosh_result.Industry[0].Code, doc);
    
    if(niosh_result.Industry.Count > 1)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("death_certificate/demographics/dc_m_industry_code_2",  niosh_result.Industry[1].Code, doc);
    }

    if(niosh_result.Industry.Count > 2)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("death_certificate/demographics/dc_m_industry_code_3",  niosh_result.Industry[2].Code, doc);
    }

    if(niosh_result.Occupation.Count > 0)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("death_certificate/demographics/dc_m_occupation_code_1",  niosh_result.Occupation[0].Code, doc);
    }

    if(niosh_result.Occupation.Count > 1)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("death_certificate/demographics/dc_m_occupation_code_2", niosh_result.Occupation[1].Code, doc);
    }

    if(niosh_result.Occupation.Count > 2)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("death_certificate/demographics/dc_m_occupation_code_3", niosh_result.Occupation[2].Code, doc);
    }
}



primary_occupation = null;
business_industry = null;

//DAD_OC_T
item_result = gs.get_value(doc, "birth_fetal_death_certificate_parent/demographic_of_father/primary_occupation");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    primary_occupation = item_result.result.ToString();
}

//DAD_IN_T
item_result = gs.get_value(doc, "birth_fetal_death_certificate_parent/demographic_of_father/occupation_business_industry");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    business_industry = item_result.result.ToString();
}
niosh_result = get_niosh_codes
(
    primary_occupation,
    business_industry
);

if
(
    !niosh_result.is_error && 
    (
        niosh_result.Industry.Count > 0 ||
        niosh_result.Occupation.Count > 0 
    )
)
{   
            
    if(case_change_count == 0)
    {
        case_change_count += 1;
        case_has_changed = true;
    }
    
    case_has_changed = case_has_changed && gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_1", niosh_result.Industry[0].Code, doc);


    if(niosh_result.Industry.Count > 1)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_2",  niosh_result.Industry[1].Code, doc);
    }

    if(niosh_result.Industry.Count > 2)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_3",  niosh_result.Industry[2].Code, doc);
    }

    if(niosh_result.Occupation.Count > 0)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_1",  niosh_result.Occupation[0].Code, doc);
    }

    if(niosh_result.Occupation.Count > 1)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_2", niosh_result.Occupation[1].Code, doc);
    }

    if(niosh_result.Occupation.Count > 2)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_3", niosh_result.Occupation[2].Code, doc);
    }

}


primary_occupation = null;
business_industry = null;
//MOM_OC_T
item_result = gs.get_value(doc, "birth_fetal_death_certificate_parent/demographic_of_mother/primary_occupation");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    primary_occupation = item_result.result.ToString();
}

//MOM_IN_T
item_result = gs.get_value(doc, "birth_fetal_death_certificate_parent/demographic_of_mother/occupation_business_industry");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    business_industry = item_result.result.ToString();
}
niosh_result = get_niosh_codes
(
    primary_occupation,
    business_industry
);

if
(
    !niosh_result.is_error && 
    (
        niosh_result.Industry.Count > 0 ||
        niosh_result.Occupation.Count > 0 
    )
)
{   
                
    if(case_change_count == 0)
    {
        case_change_count += 1;
        case_has_changed = true;
    }
    
    case_has_changed = case_has_changed && gs.set_value("birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_1", niosh_result.Industry[0].Code, doc);


    if(niosh_result.Industry.Count > 1)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_2",  niosh_result.Industry[1].Code, doc);
    }

    if(niosh_result.Industry.Count > 2)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_3",  niosh_result.Industry[2].Code, doc);
    }

    if(niosh_result.Occupation.Count > 0)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_1",  niosh_result.Occupation[0].Code, doc);
    }

    if(niosh_result.Occupation.Count > 1)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_2", niosh_result.Occupation[1].Code, doc);
    }

    if(niosh_result.Occupation.Count > 2)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_3", niosh_result.Occupation[2].Code, doc);
    }
}



primary_occupation = null;
business_industry = null;

item_result = gs.get_value(doc, "social_and_environmental_profile/socio_economic_characteristics/occupation");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    primary_occupation = item_result.result.ToString();
}

niosh_result = get_niosh_codes
(
    primary_occupation,
    business_industry
);

if
(
    !niosh_result.is_error && 
    (
        niosh_result.Industry.Count > 0 ||
        niosh_result.Occupation.Count > 0 
    )
)
{   
                            
    if(case_change_count == 0)
    {
        case_change_count += 1;
        case_has_changed = true;
    }
    
    case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_1", niosh_result.Industry[0].Code, doc);


    if(niosh_result.Industry.Count > 1)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_2",  niosh_result.Industry[1].Code, doc);
    }

    if(niosh_result.Industry.Count > 2)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_3",  niosh_result.Industry[2].Code, doc);
    }

    if(niosh_result.Occupation.Count > 0)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_1",  niosh_result.Occupation[0].Code, doc);
    }

    if(niosh_result.Occupation.Count > 1)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_2", niosh_result.Occupation[1].Code, doc);
    }


    if(niosh_result.Occupation.Count > 2)
    {                      
        if(case_change_count == 0)
        {
            case_change_count += 1;
            case_has_changed = true;
        }
        
        case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_3", niosh_result.Occupation[2].Code, doc);
    }
}








                }

                if(!is_report_only_mode && case_has_changed)
                {
                    var save_result = await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(doc as IDictionary<string, object>, data_migration_name);
                }
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }

        Console.WriteLine($"{data_migration_name} Finished {DateTime.Now}");
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
    string get_year_and_quarter(object p_value)
    {
        var result = string.Empty;
        
        if(p_value != null && !string.IsNullOrWhiteSpace(p_value.ToString()))
        try
        {
    
            if(p_value is DateTime)
            {
                var date_time = (DateTime) p_value;
                result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
            }
            else
            {
                var date_string = p_value.ToString();
                if(date_string.IndexOf("-") > -1)
                {
                    var int_array = date_string.Split("-");
                    if(int_array.Length == 3)
                    {
                        DateTime date_time = new DateTime(int.Parse(int_array[0]), int.Parse(int_array[1]), int.Parse(int_array[2]));
                        result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
                    }
                    else
                    {
                        DateTime date_time = DateTime.ParseExact
                        (
                            date_string,
                            "yyyy-MM-dd", //"MM/dd/yyyy", 
                            System.Globalization.CultureInfo.InvariantCulture
                        );
                        result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
                    }
                }
                else if(date_string.IndexOf("/") > -1)
                {
                    DateTime date_time = DateTime.ParseExact
                    (
                        date_string,
                        "MM/dd/yyyy", 
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                    result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
                }
                else
                {
                    DateTime date_time = DateTime.ParseExact
                    (
                        date_string,
                        "yyyy-MM-dd", //"MM/dd/yyyy", 
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                    result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
                }
            }
        
        }
        catch
        {
            // do nothing
        }

        return result;
    }

    async Task  update_jurisdiction_roles(string prefix)
    {
        var RoleList =  await GetUserRoleJurisdictionSet();

        var case_has_changed = false;

        var new_role_list = new cBulk_user_role_jurisdiction();

        foreach
        (
            var role in RoleList.Where
            (
                r => r.role_name != "installation_admin" &&
                r.role_name != "form_designer" &&
                (
                    r.user_id != "isu7@cdc.gov" ||
                    r.user_id != "cuv5@cdc.gov" ||
                    r.user_id != "ylr2@cdc.gov" 
                )
            )
        )
        {
            switch (role.jurisdiction_id)
            {
                case "/":
                    if(prefix == "pa")
                    {
                        role.jurisdiction_id = $"pa/{role.jurisdiction_id}";
                    }
                    else if(prefix == "ny")
                    {
                        role.jurisdiction_id = $"ny/{role.jurisdiction_id}";
                    }
                    new_role_list.docs.Add(role);

                    new_role_list.docs.Add
                    (
                        new mmria.common.model.couchdb.user_role_jurisdiction()
                        {
                            _id = Guid.NewGuid().ToString(),
                            role_name = role.role_name,
                            user_id = role.user_id,
                            jurisdiction_id = "/Shared",
                            effective_start_date = DateTime.Now,
                            is_active = true,
                            date_created = DateTime.Now,
                            created_by = "isu7@cdc.gov",
                            date_last_updated = DateTime.Now,
                            last_updated_by = "isu7@cdc.gov",
                            data_type = "user_role_jursidiction"
                        }
                    );

                break;
                case "/nyc":
                //case "/nyc":

                new_role_list.docs.Add
                    (
                        new mmria.common.model.couchdb.user_role_jurisdiction()
                        {
                            _id = Guid.NewGuid().ToString(),
                            role_name = role.role_name,
                            user_id = role.user_id,
                            jurisdiction_id = "/Shared",
                            effective_start_date = DateTime.Now,
                            is_active = true,
                            date_created = DateTime.Now,
                            created_by = "isu7@cdc.gov",
                            date_last_updated = DateTime.Now,
                            last_updated_by = "isu7@cdc.gov",
                            data_type = "user_role_jursidiction"
                        }
                    );
                break;
                case "/Philadelphia":

                new_role_list.docs.Add
                    (
                        new mmria.common.model.couchdb.user_role_jurisdiction()
                        {
                            _id = Guid.NewGuid().ToString(),
                            role_name = role.role_name,
                            user_id = role.user_id,
                            jurisdiction_id = "/Shared",
                            effective_start_date = DateTime.Now,
                            is_active = true,
                            date_created = DateTime.Now,
                            created_by = "isu7@cdc.gov",
                            date_last_updated = DateTime.Now,
                            last_updated_by = "isu7@cdc.gov",
                            data_type = "user_role_jursidiction"
                        }
                    );

                break;
            }
        }

        if(!is_report_only_mode && case_has_changed)
        {
            var save_result = await Put_JurisdictionDocument(new_role_list);
        }

    }

    async Task update_case_folder_tree(string prefix)
    {
        var jurisiction_tree = await GetJurisdictionTree();

        var case_has_changed = false;

        var new_list = new List<mmria.common.model.couchdb.jurisdiction>();

        //"id": "jurisdiction_tree//north_ga",
        //"name": "/north_ga",

        //"id": "jurisdiction_tree//Georgia//Georgia/area1",
        //"name": "/Georgia/area1",

        //"id": "jurisdiction_tree//Georgia//Georgia/area1//Georgia/area1/sub//Georgia/area1/sub/sub",
        //"name": "/Georgia/area1/sub/sub",

        if(prefix == "pa")
        {
            var pa_node = jurisiction_tree.children.Where(c => c.id == "jurisdiction_tree//pa").FirstOrDefault();
            
            if(pa_node == null)
            {
                pa_node = new mmria.common.model.couchdb.jurisdiction();

                pa_node.id = $"jurisdiction_tree//pa";
                pa_node.name = "/pa";
                pa_node.date_created = DateTime.Now;
                pa_node.created_by = "isu7";
                pa_node.date_last_updated = DateTime.Now;
                pa_node.last_updated_by = "isu7";
                pa_node.is_active = true;
                pa_node.is_enabled = true;
                pa_node.parent_id = "jurisdiction_tree";

                new_list.Add(pa_node);
                
            }


            var phila_node = jurisiction_tree.children.Where(c => c.id == "jurisdiction_tree//Philadelphia").FirstOrDefault();
            var shared_node = jurisiction_tree.children.Where(c => c.id == "jurisdiction_tree//Shared").FirstOrDefault();
            if(shared_node == null)
            {
                shared_node = new mmria.common.model.couchdb.jurisdiction();
                shared_node.id = $"jurisdiction_tree//shared";
                shared_node.name = "/shared";
                shared_node.date_created = DateTime.Now;
                shared_node.created_by = "isu7";
                shared_node.date_last_updated = DateTime.Now;
                shared_node.last_updated_by = "isu7";
                shared_node.is_active = true;
                shared_node.is_enabled = true;
                shared_node.parent_id = "jurisdiction_tree";
                new_list.Add(shared_node);

                phila_node.children = new List<mmria.common.model.couchdb.jurisdiction>();
                new_list.Add(phila_node);
            }

        }


            
        else if( prefix == "ny" || prefix == "localhost" || prefix == "fl_dev")
        {

            var ny_node = jurisiction_tree.children.Where(c => c.id == "jurisdiction_tree//ny").FirstOrDefault();
            if(ny_node == null)
            {
                ny_node = new mmria.common.model.couchdb.jurisdiction();
                ny_node.id = $"jurisdiction_tree//ny";
                ny_node.name = "/ny";
                ny_node.date_created = DateTime.Now;
                ny_node.created_by = "isu7";
                ny_node.date_last_updated = DateTime.Now;
                ny_node.last_updated_by = "isu7";
                ny_node.is_active = true;
                ny_node.is_enabled = true;
                ny_node.parent_id = "jurisdiction_tree";

                new_list.Add(ny_node);
            }

            var nyc_node = jurisiction_tree.children.Where(c => c.id == "jurisdiction_tree//nyc").FirstOrDefault();
            var shared_node = jurisiction_tree.children.Where(c => c.id == "jurisdiction_tree//shared").FirstOrDefault();
            if(shared_node == null)
            {
                shared_node = new mmria.common.model.couchdb.jurisdiction();
                shared_node.id = $"jurisdiction_tree//shared";
                shared_node.name = "/shared";
                shared_node.date_created = DateTime.Now;
                shared_node.created_by = "isu7";
                shared_node.date_last_updated = DateTime.Now;
                shared_node.last_updated_by = "isu7";
                shared_node.is_active = true;
                shared_node.is_enabled = true;
                shared_node.parent_id = "jurisdiction_tree";
                new_list.Add(shared_node);


                nyc_node.children = new List<mmria.common.model.couchdb.jurisdiction>();
                new_list.Add(nyc_node);
            }
        }

        if(!is_report_only_mode && case_has_changed)
        {
            jurisiction_tree.children = new_list.ToArray();
            var save_result = await SetJurisdictionTree(jurisiction_tree);
        }
    }
    async System.Threading.Tasks.Task<mmria.common.model.couchdb.jurisdiction_tree> GetJurisdictionTree()
    {

        mmria.common.model.couchdb.jurisdiction_tree result = null;

        try
        {
            string jurisdiction_tree_url = $"{host_db_url}/jurisdiction/jurisdiction_tree";

            var jurisdiction_curl = new cURL("GET", null, jurisdiction_tree_url, null, config_timer_user_name, config_timer_value);
            string response_from_server = await jurisdiction_curl.executeAsync ();

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.jurisdiction_tree>(response_from_server);

        }
        catch(Exception ex) 
        {
            Console.WriteLine($"{ex}");
        }

        return result;
    }

    async System.Threading.Tasks.Task<IList<mmria.common.model.couchdb.user_role_jurisdiction>> GetUserRoleJurisdictionSet()
    {

        IList<mmria.common.model.couchdb.user_role_jurisdiction> result = null;

        try
        {
            string jurisdiction_tree_url = $"{host_db_url}/jurisdiction/_all_docs?include_docs=true";

            var jurisdiction_curl = new cURL("GET", null, jurisdiction_tree_url, null, config_timer_user_name, config_timer_value);
            string response_from_server = await jurisdiction_curl.executeAsync ();

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<IList<mmria.common.model.couchdb.user_role_jurisdiction>>(response_from_server);

        }
        catch(Exception ex) 
        {
            Console.WriteLine($"{ex}");
        }

        return result;
    }


    async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> SetJurisdictionTree
    (
        mmria.common.model.couchdb.jurisdiction_tree jurisdiction_tree
    ) 
    { 
        string jurisdiction_json;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {
            jurisdiction_tree.last_updated_by = "v2.6-data-migration";

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            jurisdiction_json = Newtonsoft.Json.JsonConvert.SerializeObject(jurisdiction_tree, settings);

            string jurisdiction_tree_url = $"{host_db_url}/jurisdiction/jurisdiction_tree";

            cURL document_curl = new cURL ("PUT", null, jurisdiction_tree_url, jurisdiction_json, config_timer_user_name, config_timer_value);



            try
            {
                string responseFromServer = await document_curl.executeAsync();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"jurisdiction_treeController:{ex}");
            }

            if (!result.ok) 
            {

            }

        }
        catch(Exception ex) 
        {
            Console.WriteLine ($"{ex}");
        }
            
        return result;
    } 

    public sealed class cBulk_user_role_jurisdiction
    {
        public cBulk_user_role_jurisdiction ()
        {
            docs = new List<mmria.common.model.couchdb.user_role_jurisdiction> ();
        }

        public List<mmria.common.model.couchdb.user_role_jurisdiction> docs { get; set; }

    }

    public sealed class cBulkDocumentResponseItem
    {
        public cBulkDocumentResponseItem ()
        {

        }

        public string id { get; set; }
        public bool ok { get; set; }
        public string rev { get; set; }

    }

    private async Task<string> Put_JurisdictionDocument(cBulk_user_role_jurisdiction p_bulk_document)
    {

        string result = null;
        string bulk_document_string = Newtonsoft.Json.JsonConvert.SerializeObject(p_bulk_document);
        string URL = $"{host_db_url}/jurisdiction/_bulk_docs";
        cURL document_curl = new cURL ("POST", null, URL, bulk_document_string, config_timer_user_name, config_timer_value);
        try
        {
            result = await document_curl.executeAsync ();
        }
        catch (Exception ex)
        {
            result = ex.ToString ();
        }
        return result;
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


}

