#if !IS_PMSS_ENHANCED
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using mmria.common.SharedLibraries.Other;


namespace mmria.common.utils;

public sealed class authorization_case
{
public static bool is_authorized_to_handle_jurisdiction_id
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal, 
        mmria.common.SharedLibraries.Other.ResourceRightEnum p_resoure_right_enum,
        mmria.case_version.v260615.mmria_case p_mmria_case
    )
    {
        return is_authorized_to_handle_jurisdiction_id(
            db_config,
            p_claims_principal,
            p_resoure_right_enum,
            p_mmria_case,
            CreateCompatibilityCouchDbHttpClient());
    }

public static bool is_authorized_to_handle_jurisdiction_id
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal, 
        mmria.common.SharedLibraries.Other.ResourceRightEnum p_resoure_right_enum,
        mmria.case_version.v260615.mmria_case p_mmria_case,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {

        bool result = false;

        var jurisdiction_hashset = mmria.common.SharedLibraries.Other.authorization.get_current_jurisdiction_id_set_for(db_config, p_claims_principal, couchDbHttpClient);
        
        if
        (
            p_mmria_case.home_record.jurisdiction_id == null
        )
        {
            p_mmria_case.home_record.jurisdiction_id = "/";
        }
        
        foreach(var jurisdiction_item in  jurisdiction_hashset)
        {
            var regex = new System.Text.RegularExpressions.Regex("^" + jurisdiction_item.jurisdiction_id);
            if
            (
                regex.IsMatch(p_mmria_case.home_record.jurisdiction_id) && 
                p_resoure_right_enum ==  jurisdiction_item.ResourceRight
            )
            {
                
                result = true;
                break;
            }
        }
        
        

        return result;
    }

    public static bool is_authorized_to_handle_jurisdiction_id
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal, 
        mmria.common.SharedLibraries.Other.ResourceRightEnum p_resoure_right_enum,
        System.Dynamic.ExpandoObject p_case_expando_object
    )
    {
        return is_authorized_to_handle_jurisdiction_id(
            db_config,
            p_claims_principal,
            p_resoure_right_enum,
            p_case_expando_object,
            CreateCompatibilityCouchDbHttpClient());
    }

    public static bool is_authorized_to_handle_jurisdiction_id
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal, 
        mmria.common.SharedLibraries.Other.ResourceRightEnum p_resoure_right_enum,
        System.Dynamic.ExpandoObject p_case_expando_object,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {

        bool result = false;

        var jurisdiction_hashset = mmria.common.SharedLibraries.Other.authorization.get_current_jurisdiction_id_set_for(db_config, p_claims_principal, couchDbHttpClient);
        
        IDictionary<string,object> byName = (IDictionary<string,object>)p_case_expando_object;

        if(byName != null)
        {
            if
            (
                !byName.ContainsKey("home_record") || 
                byName["home_record"] == null
            )
            {
                byName["home_record"] = new Dictionary<string,object>();
            }

            var home_record = byName["home_record"] as IDictionary<string,object>;

            if(home_record != null)
            {
                if
                (
                    !home_record.ContainsKey("jurisdiction_id") || 
                    home_record["jurisdiction_id"] == null
                )
                {
                    home_record["jurisdiction_id"] = "/";
                }
                
                foreach(var jurisdiction_item in  jurisdiction_hashset)
                {
                    var regex = new System.Text.RegularExpressions.Regex("^" + jurisdiction_item.jurisdiction_id);
                    if
                    (
                        regex.IsMatch(home_record["jurisdiction_id"].ToString()) && 
                        p_resoure_right_enum ==  jurisdiction_item.ResourceRight
                    )
                    {
                        
                        result = true;
                        break;
                    }
                }
            }
        }

        return result;
    }

    public static bool is_authorized_to_handle_jurisdiction_id
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal, 
        mmria.common.SharedLibraries.Other.ResourceRightEnum p_resoure_right_enum,
        string jurisdiction_id
    )
    {
        return is_authorized_to_handle_jurisdiction_id(
            db_config,
            p_claims_principal,
            p_resoure_right_enum,
            jurisdiction_id,
            CreateCompatibilityCouchDbHttpClient());
    }

    public static bool is_authorized_to_handle_jurisdiction_id
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal, 
        mmria.common.SharedLibraries.Other.ResourceRightEnum p_resoure_right_enum,
        string jurisdiction_id,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {

        bool result = false;

        var jurisdiction_hashset = mmria.common.SharedLibraries.Other.authorization.get_current_jurisdiction_id_set_for(db_config, p_claims_principal, couchDbHttpClient);

        
        foreach(var jurisdiction_item in jurisdiction_hashset)
        {
            var regex = new System.Text.RegularExpressions.Regex("^" + @jurisdiction_item.jurisdiction_id);
            if
            (
                regex.IsMatch(jurisdiction_id) &&
                p_resoure_right_enum == jurisdiction_item.ResourceRight
            )
            {
                result = true;
                break;
            }
        }

        return result;
    }


    public static HashSet<(string jurisdiction_id, string user_id, string role_name)> get_user_jurisdiction_set(mmria.common.couchdb.DBConfigurationDetail db_config)
    {
        return get_user_jurisdiction_set(db_config, CreateCompatibilityCouchDbHttpClient());
    }

    public static HashSet<(string jurisdiction_id, string user_id, string role_name)> get_user_jurisdiction_set(
        mmria.common.couchdb.DBConfigurationDetail db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        return mmria.common.utils.AuthorizationRoleCache.GetOrLoadTenantUserRoles(
            db_config?.prefix,
            () => LoadUserJurisdictionSet(db_config, couchDbHttpClient));
    }

    private static HashSet<(string jurisdiction_id, string user_id, string role_name)> LoadUserJurisdictionSet(
        mmria.common.couchdb.DBConfigurationDetail db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        HashSet<(string,string,string)> result = new HashSet<(string,string,string)>();

        string jurisdicion_view_url = $"{db_config.url}/{db_config.prefix}jurisdiction/_design/sortable/_view/by_user_id";
        string jurisdicion_result_string = null;
        try
        {
            jurisdicion_result_string = couchDbHttpClient.ExecuteAsync("GET", jurisdicion_view_url, null, db_config.user_name, db_config.user_value, "application/json").Result;
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex);
            return result;
        }
        
        var jurisdiction_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.user_role_jurisdiction>>(jurisdicion_result_string);
        foreach(mmria.common.model.couchdb.get_sortable_view_response_item<mmria.common.model.couchdb.user_role_jurisdiction> jvi in jurisdiction_view_response.rows)
        {
            if(jvi.key!=null)
            {
                result.Add((jvi.value.jurisdiction_id,jvi.value.user_id, jvi.value.role_name));
            }
            
        }

        return result;
    }

    private static mmria.common.getset.CouchDbHttpClient CreateCompatibilityCouchDbHttpClient()
    {
        return new mmria.common.getset.CouchDbHttpClient(new mmria.common.SimpleHttpClientFactory());
    }
}
#endif
