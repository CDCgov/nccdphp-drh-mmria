#if !IS_PMSS_ENHANCED
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using mmria.common.SharedLibraries.Other;

namespace mmria.server.utils;

public sealed class authorization_user
{


    public static bool is_authorized_to_handle_jurisdiction_id
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal, 
        mmria.common.model.couchdb.user p_user,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {

        bool result = false;

        var jurisdiction_hashset = mmria.common.SharedLibraries.Other.authorization.get_current_jurisdiction_id_set_for(
            db_config,
            p_claims_principal,
            couchDbHttpClient);


        var target_user_jurisdictions = mmria.common.SharedLibraries.Other.authorization.get_current_jurisdiction_id_set_for(
            db_config,
            p_user.name,
            couchDbHttpClient);

        foreach(var target_jurisdiction_item in target_user_jurisdictions)
        {

            //bool is_jurisdiction_ok = false;
            foreach((string, mmria.common.SharedLibraries.Other.ResourceRightEnum) jurisdiction_item in jurisdiction_hashset)
            {
                var regex = new System.Text.RegularExpressions.Regex("^" + @jurisdiction_item.Item1);
                var target_jurisdiction_id = target_jurisdiction_item.jurisdiction_id;
                if(target_jurisdiction_id == null)
                {
                    target_jurisdiction_id = "/";
                }

                if(regex.IsMatch(target_jurisdiction_id))
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
        mmria.common.SharedLibraries.Other.ResourceRightEnum p_resource_action, 
        mmria.common.model.couchdb.user_role_jurisdiction p_user_role_jurisdiction
    )
    {
        return is_authorized_to_handle_jurisdiction_id(
            db_config,
            p_claims_principal,
            p_resource_action,
            p_user_role_jurisdiction,
            CreateCompatibilityCouchDbHttpClient());
    }

    public static bool is_authorized_to_handle_jurisdiction_id
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal,
        mmria.common.SharedLibraries.Other.ResourceRightEnum p_resource_action, 
        mmria.common.model.couchdb.user_role_jurisdiction p_user_role_jurisdiction,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        var jurisdiction_hashset = mmria.common.SharedLibraries.Other.authorization.get_current_jurisdiction_id_set_for(
            db_config,
            p_claims_principal,
            couchDbHttpClient);
        return mmria.common.SharedLibraries.Other.authorization.is_authorized_to_handle_jurisdiction_id(jurisdiction_hashset, p_resource_action, p_user_role_jurisdiction);
    }



    public static HashSet<string> get_current_jurisdiction_id_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
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

        var jurisdiction_view_response = mmria.common.SharedLibraries.Other.authorization.get_current_jurisdiction_id_set_for(
            db_config,
            p_claims_principal,
            couchDbHttpClient);

        foreach(var jvi in jurisdiction_view_response)
        {
            result.Add(jvi.jurisdiction_id);
        }

        return result;
    }

    private static mmria.common.getset.CouchDbHttpClient CreateCompatibilityCouchDbHttpClient()
    {
        return new mmria.common.getset.CouchDbHttpClient(new mmria.common.SimpleHttpClientFactory());
    }

}
#endif
