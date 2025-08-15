#if IS_PMSS_ENHANCED
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;


using mmria.server.utils;
namespace mmria.pmss.server.utils;

public sealed class authorization_user
{


    public static bool is_authorized_to_handle_jurisdiction_id
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal, 
        mmria.common.model.couchdb.user p_user
    )
    {

        bool result = false;

        var jurisdiction_hashset = mmria.pmss.server.utils.authorization.get_current_jurisdiction_id_set_for(db_config, p_claims_principal);


        string jurisdicion_view_url = $"{db_config.url}/{db_config.prefix}jurisdiction/_design/sortable/_view/by_user_id?{p_user.name}";
        var jurisdicion_curl = new mmria.server.cURL("GET", null, jurisdicion_view_url, null, db_config.user_name, db_config.user_value);
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

        var user_role_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.user_role_jurisdiction>>(jurisdicion_result_string);

        foreach(mmria.common.model.couchdb.get_sortable_view_response_item<mmria.common.model.couchdb.user_role_jurisdiction> cvi in user_role_response.rows)
        {

            //bool is_jurisdiction_ok = false;
            foreach((string, ResourceRightEnum) jurisdiction_item in jurisdiction_hashset)
            {
                var regex = new System.Text.RegularExpressions.Regex("^" + @jurisdiction_item.Item1);
                if(cvi.value.jurisdiction_id == null)
                {
                    cvi.value.jurisdiction_id = "/";
                }

                if(regex.IsMatch(cvi.value.jurisdiction_id))
                {
                    return true;
                }
            }

/*
            foreach(string jurisdiction_id in  jurisdiction_hashset)
            {
                var regex = new System.Text.RegularExpressions.Regex("^" + jurisdiction_id);
                if(p_user._role_jurisdiction.jurisdiction_id != null && regex.IsMatch(p_user_role_jurisdiction.jurisdiction_id))
                {
                    result = true;
                    break;
                }
            }
*/
        }


        return result;
    }

    public static bool is_authorized_to_handle_jurisdiction_id
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal,
        ResourceRightEnum p_resource_action, 
        mmria.common.model.couchdb.user_role_jurisdiction p_user_role_jurisdiction
    )
    {

        bool result = false;

        var jurisdiction_hashset = mmria.pmss.server.utils.authorization.get_current_jurisdiction_id_set_for(db_config, p_claims_principal);
            
        foreach(var jurisdiction_item in  jurisdiction_hashset)
        {
            var regex = new System.Text.RegularExpressions.Regex("^" + jurisdiction_item.jurisdiction_id);
            if
            (   p_user_role_jurisdiction.jurisdiction_id != null && 
                regex.IsMatch(p_user_role_jurisdiction.jurisdiction_id) &&
                p_resource_action == jurisdiction_item.ResourceRight

            )
            {
                result = true;
                break;
            }
        }


        return result;
    }



    public static HashSet<string> get_current_jurisdiction_id_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal
    )
    {
        HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!p_claims_principal.HasClaim(c => c.Type == ClaimTypes.Name && 
                                        c.Issuer == "https://contoso.com"))
        {
            return result;
        }

        if (p_claims_principal.HasClaim(c => c.Type == ClaimTypes.Role && 
                                        c.Value == "installation_admin"))
        {
            result.Add("/");
        }

        var user_name = p_claims_principal.Claims.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault().Value; 

        string jurisdicion_view_url = $"{db_config.url}/{db_config.prefix}jurisdiction/_design/sortable/_view/by_user_id?{user_name}";
        var jurisdicion_curl = new mmria.server.cURL("GET", null, jurisdicion_view_url, null, db_config.user_name, db_config.user_value);
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
        
        var now = DateTime.Now;
        foreach(mmria.common.model.couchdb.get_sortable_view_response_item<mmria.common.model.couchdb.user_role_jurisdiction> jvi in jurisdiction_view_response.rows)
        {
            if(jvi.key!=null && jvi.key == user_name)
            {
                if(jvi.value.is_active != null && jvi.value.is_active.HasValue && jvi.value.is_active.Value)
                {

                    bool add_item = true;
                    
                    if(jvi.value.effective_start_date != null && jvi.value.effective_start_date.HasValue)
                    {
                        if(jvi.value.effective_start_date > now)
                        {
                            add_item = false;
                        }
                        
                    }

                    if(jvi.value.effective_end_date != null && jvi.value.effective_end_date.HasValue)
                    {
                        
                        if(jvi.value.effective_end_date.Value < now)
                        {
                            add_item = false;
                        }
                        
                    }

                    if(add_item)
                    {
                        result.Add(jvi.value.jurisdiction_id);
                    }
                    

                }
            }
            
        }

        return result;
    }

}
#endif