#if IS_PMSS_ENHANCED
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using mmria.common.getset;
using mmria.common.SharedLibraries.Jurisdiction;
using mmria.common.SharedLibraries.Jurisdiction.DAL;


namespace mmria.pmss.server.utils;

public sealed class authorization_case
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public authorization_case(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public static bool is_authorized_to_handle_jurisdiction_id
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal, 
        ResourceRightEnum p_resoure_right_enum,
        mmria.case_version.pmss.v230616.mmria_case p_case_expando_object
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
        ResourceRightEnum p_resoure_right_enum,
        mmria.case_version.pmss.v230616.mmria_case p_case_expando_object,
        CouchDbHttpClient couchDbHttpClient
    )
    {

        bool result = false;

        var jurisdiction_hashset = mmria.pmss.server.utils.authorization.get_current_jurisdiction_id_set_for(db_config, p_claims_principal, couchDbHttpClient);
        

        //IDictionary<string,object> pre_tracking = (IDictionary<string,object>)p_case_expando_object;
        //IDictionary<string,object> tracking = (IDictionary<string,object>)pre_tracking["tracking"];
        

        if(p_case_expando_object.tracking != null)
        {
            if
            ( 
                p_case_expando_object.tracking.admin_info == null
            )
            {
               p_case_expando_object. tracking.admin_info = new ();
            }


            if
            (
                string.IsNullOrWhiteSpace(p_case_expando_object.tracking.admin_info.case_folder)
            )
            {
                p_case_expando_object.tracking.admin_info.case_folder= "/";
            }
            
            foreach(var jurisdiction_item in  jurisdiction_hashset)
            {
                var regex = new System.Text.RegularExpressions.Regex("^" + jurisdiction_item.jurisdiction_id);
                if
                (
                    regex.IsMatch(p_case_expando_object.tracking.admin_info.case_folder) && 
                    p_resoure_right_enum ==  jurisdiction_item.ResourceRight
                )
                {
                    
                    result = true;
                    break;
                }
            }
            
        }

        return result;
    }

    public static bool is_authorized_to_handle_jurisdiction_id
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal, 
        ResourceRightEnum p_resoure_right_enum,
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
        ResourceRightEnum p_resoure_right_enum,
        string jurisdiction_id,
        CouchDbHttpClient couchDbHttpClient
    )
    {

        bool result = false;

        var jurisdiction_hashset = mmria.pmss.server.utils.authorization.get_current_jurisdiction_id_set_for(db_config, p_claims_principal, couchDbHttpClient);

        
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

    private static CouchDbHttpClient CreateCompatibilityCouchDbHttpClient()
    {
        return new CouchDbHttpClient(new mmria.common.SimpleHttpClientFactory());
    }


    public async Task<HashSet<(string jurisdiction_id, string user_id, string role_name)>> get_user_jurisdiction_set(mmria.common.couchdb.DBConfigurationDetail db_config)
    {
        var reader = new JurisdictionAuthorizationDAL(_couchDbHttpClient);
        return mmria.common.utils.AuthorizationRoleCache.GetOrLoadTenantUserRoles(
            db_config?.prefix,
            reader,
            db_config);
    }
}
#endif
