#if !IS_PMSS_ENHANCED
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using mmria.common.SharedLibraries.Other;
using mmria.common.SharedLibraries.Jurisdiction.DAL;

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

        var reader = new JurisdictionAuthorizationDAL(couchDbHttpClient);
        var userEntries = reader.GetRolesByUserIdAsync(p_user.name, db_config).GetAwaiter().GetResult();

        foreach (var entry in userEntries)
        {
            foreach ((string, mmria.common.SharedLibraries.Other.ResourceRightEnum) jurisdiction_item in jurisdiction_hashset)
            {
                var regex = new System.Text.RegularExpressions.Regex("^" + @jurisdiction_item.Item1);
                string jurisdictionId = string.IsNullOrEmpty(entry.jurisdiction_id) ? "/" : entry.jurisdiction_id;

                if (regex.IsMatch(jurisdictionId))
                {
                    return true;
                }
            }
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

        var user_name = p_claims_principal.Claims.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault().Value;

        var reader = new JurisdictionAuthorizationDAL(couchDbHttpClient);
        var rawEntries = reader.GetRolesByUserIdAsync(user_name, db_config).GetAwaiter().GetResult();

        var now = DateTime.Now;
        foreach (var entry in rawEntries)
        {
            if (entry?.user_id != user_name)
                continue;

            if (entry.is_active == null || !entry.is_active.HasValue || !entry.is_active.Value)
                continue;

            bool add_item = true;

            if (entry.effective_start_date.HasValue && entry.effective_start_date.Value > now)
                add_item = false;

            if (entry.effective_end_date.HasValue && entry.effective_end_date.Value < now)
                add_item = false;

            if (add_item)
                result.Add(entry.jurisdiction_id!);
        }

        return result;
    }

    private static mmria.common.getset.CouchDbHttpClient CreateCompatibilityCouchDbHttpClient()
    {
        return new mmria.common.getset.CouchDbHttpClient(new mmria.common.SimpleHttpClientFactory());
    }

}
#endif
