#if !IS_PMSS_ENHANCED
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using mmria.getset;

namespace mmria.services;

public enum ResourceRightEnum
{
    ReadDeidentifiedCase,
    ReadCase,
    WriteCase,
    ReadMetadata,
    WriteMetadata,
    ReadUser,
    WriteUser,
    ReadJurisdiction,
    WriteJurisdiction
}

public sealed class authorization
{


    public static HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)> get_current_jurisdiction_id_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal
    )
    {
        var result = new HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)>();

        if (!p_claims_principal.HasClaim(c => c.Type == ClaimTypes.Name && 
                                        c.Issuer == "https://contoso.com"))
        {
            return result;
        }

        if (p_claims_principal.HasClaim(c => c.Type == ClaimTypes.Role && 
                                        c.Value == "installation_admin"))
        {

            result.Add(("/", ResourceRightEnum.ReadUser));
            result.Add(("/", ResourceRightEnum.WriteUser));
            result.Add(("/", ResourceRightEnum.ReadJurisdiction));
            result.Add(("/", ResourceRightEnum.WriteJurisdiction));

        }

        var user_name = p_claims_principal.Claims.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault().Value; 

        string jurisdicion_view_url = $"{db_config.url}/{db_config.prefix}jurisdiction/_design/sortable/_view/by_user_id";
        var jurisdicion_curl = new cURL("GET", null, jurisdicion_view_url, null, db_config.user_name, db_config.user_value);
        string jurisdicion_result_string = null;
        try
        {
            jurisdicion_result_string = jurisdicion_curl.execute();
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex);
            return result;
        }
        
        var jurisdiction_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.user_role_jurisdiction>>(jurisdicion_result_string);
        foreach(mmria.common.model.couchdb.get_sortable_view_response_item<mmria.common.model.couchdb.user_role_jurisdiction> jvi in jurisdiction_view_response.rows)
        {
            if(jvi.key!=null && jvi.value.user_id == user_name)
            {
                if
                (
                    jvi.value.is_active == null ||
                    jvi.value.effective_start_date == null 
                )
                {
                    continue;
                }

                if
                (
                    !
                    (jvi.value.is_active.HasValue ||
                    jvi.value.effective_start_date.HasValue)
                )
                {
                    continue;
                }

                
                    
                var now_date = DateTime.Now;

                if
                (
                    jvi.value.effective_end_date == null ||
                    jvi.value.effective_end_date.HasValue == false
                )
                {
                    jvi.value.effective_end_date = now_date;
                }

                if
                (
                    !jvi.value.is_active.Value ||
                    !(jvi.value.effective_start_date.Value <= now_date) ||
                    !(now_date <= jvi.value.effective_end_date.Value)
                )
                {
                    continue;
                }

                switch(jvi.value.role_name)
                {
                    case "abstractor":
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadCase));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.WriteCase));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                        break;
                    case "data_analyst":
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadCase));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                        break;
                    case "committee_member":
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadDeidentifiedCase));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                        break;
                    case "form_designer":
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.WriteMetadata));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                        break;
                    case "jurisdiction_admin":
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadUser));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.WriteUser));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadJurisdiction));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.WriteJurisdiction));
                        break;
                    case "installation_admin":
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadUser));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.WriteUser));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadJurisdiction));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.WriteJurisdiction));
                        break;
                    
                }
                
            }
            
        }

        return result;
    }


    public static HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)> get_current_jurisdiction_id_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        string p_user_name
    )
    {

        var result = new HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)>();

        string jurisdicion_view_url = $"{db_config.url}/{db_config.prefix}jurisdiction/_design/sortable/_view/by_user_id";
        var jurisdicion_curl = new cURL("GET", null, jurisdicion_view_url, null, db_config.user_name, db_config.user_value);
        string jurisdicion_result_string = null;
        try
        {
            jurisdicion_result_string = jurisdicion_curl.execute();
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex);
            return result;
        }            

        var jurisdiction_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.user_role_jurisdiction>>(jurisdicion_result_string);
        foreach(mmria.common.model.couchdb.get_sortable_view_response_item<mmria.common.model.couchdb.user_role_jurisdiction> jvi in jurisdiction_view_response.rows)
        {
            if(jvi.key!=null && jvi.value.user_id == p_user_name)
            {
                if
                (
                    jvi.value.is_active == null ||
                    jvi.value.effective_start_date == null 
                )
                {
                    continue;
                }

                if
                (
                    !
                    (jvi.value.is_active.HasValue ||
                    jvi.value.effective_start_date.HasValue)
                )
                {
                    continue;
                }

                
                    
                var now_date = DateTime.Now;

                if
                (
                    jvi.value.effective_end_date == null ||
                    jvi.value.effective_end_date.HasValue == false
                )
                {
                    jvi.value.effective_end_date = now_date;
                }

                if
                (
                    !jvi.value.is_active.Value ||
                    !(jvi.value.effective_start_date.Value <= now_date) ||
                    !(now_date <= jvi.value.effective_end_date.Value)
                )
                {
                    continue;
                }

                switch(jvi.value.role_name)
                {
                    case "abstractor":
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadCase));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.WriteCase));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                        break;
                    case "data_analyst":
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadCase));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                        break;
                    case "committee_member":
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadDeidentifiedCase));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                        break;
                    case "form_designer":
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.WriteMetadata));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                        break;
                    case "jurisdiction_admin":
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadUser));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.WriteUser));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadJurisdiction));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.WriteJurisdiction));
                        break;
                    case "installation_admin":
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadUser));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.WriteUser));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.ReadJurisdiction));
                        result.Add((jvi.value.jurisdiction_id, ResourceRightEnum.WriteJurisdiction));
                        break;
                    
                }
                
            }
            
        }

        return result;
    }

    public static HashSet<(string jurisdiction_id, string user_id, string role_name)> get_current_user_role_jurisdiction_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        string p_user_name
    )
    {
        var result = new HashSet<(string jurisdiction_id, string user_id, string role_name)>();


        string jurisdicion_view_url = $"{db_config.url}/{db_config.prefix}jurisdiction/_design/sortable/_view/by_user_id";
        var jurisdicion_curl = new cURL("GET", null, jurisdicion_view_url, null, db_config.user_name, db_config.user_value);
        string jurisdicion_result_string = null;
        try
        {
            jurisdicion_result_string = jurisdicion_curl.execute();
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex);
            return result;
        }
        
        var jurisdiction_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.user_role_jurisdiction>>(jurisdicion_result_string);
        foreach(mmria.common.model.couchdb.get_sortable_view_response_item<mmria.common.model.couchdb.user_role_jurisdiction> jvi in jurisdiction_view_response.rows)
        {
            if(jvi.key!=null && jvi.value.user_id == p_user_name)
            {

                if
                (
                    jvi.value.is_active == null ||
                    jvi.value.effective_start_date == null
                )
                {
                    continue;
                }

                if
                (
                    !
                    (jvi.value.is_active.HasValue ||
                    jvi.value.effective_start_date.HasValue)
                )
                {
                    continue;
                }

                var now_date = DateTime.Now;

                if
                (
                    jvi.value.effective_end_date == null ||
                    jvi.value.effective_end_date.HasValue == false
                )
                {
                    jvi.value.effective_end_date = now_date;
                }

                if
                (
                    !jvi.value.is_active.Value ||
                    !(jvi.value.effective_start_date.Value <= now_date )||
                    !(now_date <= jvi.value.effective_end_date.Value)
                )
                {
                    continue;
                }


                result.Add((jvi.value.jurisdiction_id, jvi.value.user_id, jvi.value.role_name));
            }
            
        }

        return result;
    }

}
#endif
