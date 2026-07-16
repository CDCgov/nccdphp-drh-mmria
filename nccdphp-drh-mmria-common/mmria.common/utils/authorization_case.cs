#if !IS_PMSS_ENHANCED
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using mmria.common.SharedLibraries.Other;
using mmria.common.SharedLibraries.Jurisdiction;
using mmria.common.SharedLibraries.Jurisdiction.DAL;


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
        var reader = new JurisdictionAuthorizationDAL(couchDbHttpClient);
        return mmria.common.utils.AuthorizationRoleCache.GetOrLoadTenantUserRoles(
            db_config?.prefix,
            reader,
            db_config);
    }

    private static mmria.common.getset.CouchDbHttpClient CreateCompatibilityCouchDbHttpClient()
    {
        return new mmria.common.getset.CouchDbHttpClient(new mmria.common.SimpleHttpClientFactory());
    }
}
#endif
