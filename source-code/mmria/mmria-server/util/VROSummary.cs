#if IS_PMSS_ENHANCED
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using mmria.getset;
using migrate;
using System.ComponentModel.DataAnnotations.Schema;


namespace mmria.server.utils;

/*
public sealed class ItemCount
{
    public ItemCount(){}

    public string host_name {get; set; }

    public string folder_name {get; set; } = "/";
    
    public int total{get; set; }
}*/
public sealed class VROSummaryItem
{
    public VROSummaryItem(){}
    public string host_name {get; set; }

    public string folder_name {get; set; } = "/";
    public string rpt_date{get; set; }
    public int num_recs{get; set; }
    public int num_users_unq{get; set; }
    public int num_users_ja{get; set; }
    public int num_users_abs{get; set; }
    public int num_user_anl{get; set; }

    public int num_user_cm { get; set; }


    public string Death_Year { get; set; }
    public string Jurisdiction_Abrev { get; set; }
    public string Jurisdiction_Name { get; set; }
    public string DC_AuxNo { get; set; }
    public string DC_FileNo { get; set; }
    public string DC_DOD { get; set; }
    public string DC_TimingOfDeath { get; set; }
    public string DC_Cod33A { get; set; }
    public string DC_Cod33B { get; set; }
    public string DC_Cod33C { get; set; }
    public string DC_Cod33D { get; set; }
    public string DC_Other_Factors { get; set; }
    public string ACME_UC { get; set; }
    public string MAN_UC { get; set; }
    public string EAC { get; set; }
    public string CDC_CheckBox { get; set; }
    public string CDC_ICD { get; set; }
    public string CDC_LiteralCOD { get; set; }
    public string CDC_Match_Det_BC { get; set; }
    public string CDC_Match_Det_FDC { get; set; }
    public string CDC_Match_Prob_BC { get; set; }
    public string CDC_Match_Prob_FDC { get; set; }
    public string VRO_Resolution_Status { get; set; }
    public string VRO_Confirmation_Method_and_Additional_Notes { get; set; }


}

sealed class VROIComparer : IComparer<VROSummaryItem>
{
    public int Compare(VROSummaryItem x, VROSummaryItem y)
    {
        
        if (x == null || y == null)
        {
            return 0;
        }

        if (x.host_name == null || y.host_name == null)
        {
            return 0;
        }
        
        // "CompareTo()" method

        if(x.host_name != y.host_name)
        {
            return x.host_name.CompareTo(y.host_name);
        }
        else
        {
            return x.folder_name.CompareTo(y.folder_name);
        }
        
    }
}

public sealed class VROSummary
{

    mmria.common.couchdb.OverridableConfiguration configuration;

    mmria.common.couchdb.DBConfigurationDetail db_config;

    string host_prefix;


    public VROSummary
    (

        mmria.common.couchdb.OverridableConfiguration _configuration,
        string _host_prefix 
    )
    {
        configuration = _configuration;
        host_prefix = _host_prefix;

        db_config = configuration.GetDBConfig(host_prefix);

    }


    public async Task<List<VROSummaryItem>> execute
    (
        System.Threading.CancellationToken cancellationToken
    )
    {


        var field_dictionary = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
        {
            { "_id", "_id"},
            { "Death_Year", "tracking/admin_info/track_year"},
            { "Jurisdiction_Abrev", "tracking/admin_info/jurisdiction"},
            { "Jurisdiction_Name", "tracking/admin_info/jurisdiction"},
            { "DC_AuxNo", "ije_dc/file_info/auxno_dc"},
            { "DC_FileNo", "ije_dc/file_info/fileno_dc"},
            { "DC_DOD_month", "ije_dc/death_info/dod_mo_dc"},
            { "DC_DOD_day", "ije_dc/death_info/dod_dy_dc"},
            { "DC_DOD_year", "ije_dc/death_info/dod_yr_dc"},
            { "DC_TimingOfDeath", "ije_dc/death_info/tod_dc"},
            { "DC_Cod33A", "ije_dc/cause_details/cod1a_dc"},
            { "DC_Cod33B", "ije_dc/cause_details/cod1b_dc"},
            { "DC_Cod33C", "ije_dc/cause_details/cod1c_dc"},
            { "DC_Cod33D", "ije_dc/cause_details/cod1d_dc"},
            { "DC_Other_Factors", "ije_dc/cause_details/othercondition_dc"},
            { "ACME_UC", "ije_dc/cause_details/acme_uc_dc"},
            { "MAN_UC", "ije_dc/cause_details/man_uc_dc"},
            { "EAC", "ije_dc/cause_details/eac_dc"},
            { "CDC_CheckBox", "vro_case_determination/cdc_case_matching_results/pregcb_match"},
            { "CDC_ICD", "vro_case_determination/cdc_case_matching_results/icd10_match"},
            { "CDC_LiteralCOD", "vro_case_determination/cdc_case_matching_results/literalcod_match"},
            { "CDC_Match_Det_BC", "vro_case_determination/cdc_case_matching_results/bc_det_match"},
            { "CDC_Match_Det_FDC", "vro_case_determination/cdc_case_matching_results/fdc_det_match"},
            { "CDC_Match_Prob_BC", "vro_case_determination/cdc_case_matching_results/bc_prob_match"},
            { "CDC_Match_Prob_FDC", "vro_case_determination/cdc_case_matching_results/fdc_prob_match"},
            { "VRO_Resolution_Status", "vro_case_determination/vro_update/vro_resolution_status"},
            { "VRO_Confirmation_Method_and_Additional_Notes", "vro_case_determination/vro_update/vro_resolution_remarks"},
        };

        var result = new List<VROSummaryItem>();

        var id_list = GetIdList();
            
        var gs = new C_Get_Set_Value(new ());
        C_Get_Set_Value.get_value_result value_result = null;
        
        
        foreach(var id in id_list)
        {
            var item = new VROSummaryItem();
			try
			{
				string URL = $"{db_config.url}/{db_config.prefix}mmrds/{id}";
				var document_curl = new mmria.getset.cURL ("GET", null, URL, null, db_config.user_name, db_config.user_value);
				var curl_result = document_curl.execute();

				System.Dynamic.ExpandoObject case_row = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(curl_result);

                string get_value(string column_name)
                {
                    string result = null;

                    if(field_dictionary.ContainsKey(column_name))
                    {
                        var mmria_path = field_dictionary[column_name];

                        value_result = gs.get_value(case_row, mmria_path);
                        if(!value_result.is_error)
                        {
                            if(value_result.result != null)
                            {
                                result = value_result.result.ToString();
                            }
                        }
                    }

                    return result;
                }

                var _id = get_value("_id");

                if(_id.IndexOf("_design") > -1) continue;

                var case_folder = get_value("Jurisdiction_Name");

                item.Death_Year = get_value("Death_Year");
                item.Jurisdiction_Abrev = get_value("Jurisdiction_Abrev");
                item.Jurisdiction_Name = get_value("Jurisdiction_Name");
                item.DC_AuxNo = get_value("DC_AuxNo");
                item.DC_FileNo = get_value("DC_FileNo");
                item.DC_DOD = get_value("DC_DOD");
                item.DC_TimingOfDeath = get_value("DC_TimingOfDeath");
                item.DC_Cod33A = get_value("DC_Cod33A");
                item.DC_Cod33B = get_value("DC_Cod33B");
                item.DC_Cod33C = get_value("DC_Cod33C");
                item.DC_Cod33D = get_value("DC_Cod33D");
                item.DC_Other_Factors = get_value("DC_Other_Factors");
                item.ACME_UC = get_value("ACME_UC");
                item.MAN_UC = get_value("MAN_UC");
                item.EAC = get_value("EAC");
                item.CDC_CheckBox = get_value("CDC_CheckBox");
                item.CDC_ICD = get_value("CDC_ICD");
                item.CDC_LiteralCOD = get_value("CDC_LiteralCOD");
                item.CDC_Match_Det_BC = get_value("CDC_Match_Det_BC");
                item.CDC_Match_Det_FDC = get_value("CDC_Match_Det_FDC");
                item.CDC_Match_Prob_BC = get_value("CDC_Match_Prob_BC");
                item.CDC_Match_Prob_FDC = get_value("CDC_Match_Prob_FDC");
                item.VRO_Resolution_Status = get_value("VRO_Resolution_Status");
                item.VRO_Confirmation_Method_and_Additional_Notes = get_value("VRO_Confirmation_Method_and_Additional_Notes");


                result.Add(item);

        
            }
            catch(Exception)
            {

            }
        }

        return result;
    }

    public async System.Threading.Tasks.Task GetUserCount
    (
        System.Threading.CancellationToken cancellationToken, 
        string p_id, 
        mmria.common.couchdb.DBConfigurationDetail p_config_detail, 
        ItemCount p_result, 
        VROSummaryItem p_SummaryItem,
        string exclude_jurisdiction
    ) 
    { 
        try
        {
            string request_string = $"{p_config_detail.url}/_users/_all_docs?include_docs=true&skip=1";

            var user_curl = new cURL("GET",null,request_string,null, p_config_detail.user_name, p_config_detail.user_value);
            string responseFromServer = await user_curl.executeAsync();

            var user_alldocs_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<mmria.common.model.couchdb.user>>(responseFromServer);

            HashSet<string> user_id_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<mmria.common.model.couchdb.get_response_item<mmria.common.model.couchdb.user>> temp_list = new List<mmria.common.model.couchdb.get_response_item<mmria.common.model.couchdb.user>>();
            foreach(mmria.common.model.couchdb.get_response_item<mmria.common.model.couchdb.user> uai in user_alldocs_response.rows)
            {
                cancellationToken.ThrowIfCancellationRequested();


                if(string.IsNullOrWhiteSpace(p_config_detail.prefix))
                {
                    if(uai.doc.app_prefix_list == null)
                    {
                        p_result.total +=1; 
                        user_id_set.Add(uai.doc.name);
                    }
                    else if(uai.doc.app_prefix_list.Count == 0 || uai.doc.app_prefix_list.ContainsKey("__no_prefix__"))
                    {
                        p_result.total +=1;
                        user_id_set.Add(uai.doc.name);
                    }
                }
                else if(uai.doc.app_prefix_list.ContainsKey(p_config_detail.prefix.ToLower()))
                {
                    p_result.total +=1;
                    user_id_set.Add(uai.doc.name);
                }
                else
                {

                }
            }

            await GetJurisdictions
            (
                cancellationToken,  
                p_id, 
                p_config_detail, 
                p_SummaryItem, 
                user_id_set,
                exclude_jurisdiction
            );

            p_result.total = user_id_set.Count;

        }
        catch(System.Exception)
        {
            p_result.total = -1;
        }
    }

    public async System.Threading.Tasks.Task GetCaseCount
    (
        System.Threading.CancellationToken cancellationToken, 
        string p_id, 
        mmria.common.couchdb.DBConfigurationDetail p_config_detail, 
        ItemCount p_result,
        string exclude_jurisdiction
    ) 
    { 
        try
        {
            string request_string = $"{p_config_detail.url}/{p_config_detail.prefix}mmrds/_design/sortable/_view/by_jurisdiction_id?skip=0&take=100000";


            cancellationToken.ThrowIfCancellationRequested();
            var user_curl = new cURL("GET",null,request_string,null, p_config_detail.user_name, p_config_detail.user_value);
            string responseFromServer = await user_curl.executeAsync();

            cancellationToken.ThrowIfCancellationRequested();

            var case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

            if
            (
                p_result.host_name.ToUpper() == "NY" ||
                p_result.host_name.ToUpper() == "NYC" ||
                p_result.host_name.ToUpper() == "PA" ||
                p_result.host_name.ToUpper() == "PHILADELPHIA"

            )
            {

                var count = 0;
                foreach(var cvr in case_view_response.rows)
                {

                    if(p_result.folder_name == "/")
                    {
                        if
                        (
                            !cvr.value.jurisdiction_id.StartsWith("/PHILADELPHIA", StringComparison.OrdinalIgnoreCase) &&
                            !cvr.value.jurisdiction_id.StartsWith("/NYC", StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            count += 1;
                        }
                    }
                    else if(p_result.folder_name == "/NYC")
                    {
                        if(cvr.value.jurisdiction_id.StartsWith("/NYC", StringComparison.OrdinalIgnoreCase))
                        {
                            count += 1;
                        }
                    }
                    else if(p_result.folder_name == "/PHILADELPHIA")
                    {
                        if(cvr.value.jurisdiction_id.StartsWith("/PHILADELPHIA", StringComparison.OrdinalIgnoreCase))
                        {
                            count += 1;
                        }
                        
                    }
                }

                p_result.total = count;
            }
            else
            {
                p_result.total = case_view_response.total_rows;
            }


            

        }
        catch(System.Exception)
        {
            p_result.total = -1;
        }


    }

    
    public async Task GetJurisdictions
    (
        System.Threading.CancellationToken cancellationToken,  
        string p_id, 
        mmria.common.couchdb.DBConfigurationDetail p_config_detail, 
        VROSummaryItem p_result, 
        HashSet<string> p_user_id_set,
        string exclude_jurisdiction
    ) 
    {
        //string sort = "by_date_created";
        string search_key = null;
        bool descending = false;
        int skip = 0;
        int take = 20000;
        search_key = "";
        string sort_view = "by_date_created";

        try
        {
            System.Text.StringBuilder request_builder = new System.Text.StringBuilder ();
            request_builder.Append (p_config_detail.url);
            request_builder.Append ($"/{p_config_detail.prefix}jurisdiction/_design/sortable/_view/{sort_view}?");


            if (string.IsNullOrWhiteSpace (search_key))
            {
                if (skip > -1) 
                {
                    request_builder.Append ($"skip={skip}");
                } 
                else 
                {

                    request_builder.Append ("skip=0");
                }


                if (take > -1) 
                {
                    request_builder.Append ($"&limit={take}");
                }

                if (descending) 
                {
                    request_builder.Append ("&descending=true");
                }
            } 
            else 
            {
                request_builder.Append ("skip=0");

                if (descending) 
                {
                    request_builder.Append ("&descending=true");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var user_role_jurisdiction_curl = new cURL("GET", null, request_builder.ToString(), null, p_config_detail.user_name, p_config_detail.user_value);
            string response_from_server = await user_role_jurisdiction_curl.executeAsync ();

            cancellationToken.ThrowIfCancellationRequested();

            var case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.user_role_jurisdiction>>(response_from_server);

            HashSet<string> Jurisdictin_User_Set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var Jurisdictin_Role_Dictionary = new Dictionary<string,HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                {  "jurisdiction_admin", new HashSet<string>(StringComparer.OrdinalIgnoreCase)},
                { "abstractor", new HashSet<string>(StringComparer.OrdinalIgnoreCase)},
                { "data_analyst", new HashSet<string>(StringComparer.OrdinalIgnoreCase)},
                { "committee_member", new HashSet<string>(StringComparer.OrdinalIgnoreCase)},
            };



            foreach(mmria.common.model.couchdb.get_sortable_view_response_item<mmria.common.model.couchdb.user_role_jurisdiction> cvi in case_view_response.rows)
            {
                if(string.IsNullOrWhiteSpace(cvi.value.role_name)) continue;

                if(string.IsNullOrWhiteSpace(cvi.value.jurisdiction_id)) continue;

                if(string.IsNullOrWhiteSpace(cvi.value.user_id)) continue;

                if(cvi.value.is_active.HasValue && cvi.value.is_active.Value == false) continue;


                var user_id = cvi.value.user_id;

                if(!p_user_id_set.Contains(user_id)) continue;

                if(!string.IsNullOrWhiteSpace(exclude_jurisdiction))
                {
                    if
                    (
                        exclude_jurisdiction ==
                        cvi.value.jurisdiction_id.ToUpper()
                    )
                    {
                        continue;
                    }
                }

                if(Jurisdictin_Role_Dictionary.ContainsKey(cvi.value.role_name))
                {
                    Jurisdictin_Role_Dictionary[cvi.value.role_name].Add(cvi.value.user_id);
                    Jurisdictin_User_Set.Add(cvi.value.user_id);
                }
            }

            p_result.num_users_ja = Jurisdictin_Role_Dictionary["jurisdiction_admin"].Count;
            p_result.num_users_abs = Jurisdictin_Role_Dictionary["abstractor"].Count;
            p_result.num_user_anl = Jurisdictin_Role_Dictionary["data_analyst"].Count;
            p_result.num_user_cm = Jurisdictin_Role_Dictionary["committee_member"].Count;

            p_user_id_set.RemoveWhere( x=> !Jurisdictin_User_Set.Contains(x));
            
        }
        catch(System.Exception)
        {
            //System.Console.WriteLine (ex);
            p_result.num_users_ja = -1;
            p_result.num_users_abs = -1;
            p_result.num_user_anl = -1;
            p_result.num_user_cm = -1;

        } 
    }


    HashSet<string> GetIdList ()
	{

		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		try
		{
			string URL = $"{db_config.url}/{db_config.prefix}mmrds/_all_docs";
			var document_curl = new mmria.getset.cURL ("GET", null, URL, null, db_config.user_name, db_config.user_value);
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
}
#endif