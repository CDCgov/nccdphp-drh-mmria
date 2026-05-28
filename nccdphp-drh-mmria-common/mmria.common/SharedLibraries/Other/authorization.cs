#if !IS_PMSS_ENHANCED
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;

namespace mmria.common.SharedLibraries.Other;

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
        return get_current_jurisdiction_id_set_for(
            db_config,
            p_claims_principal,
            CreateCompatibilityCouchDbHttpClient());
    }


    public static HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)> get_current_jurisdiction_id_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
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
        foreach (var role in GetActiveUserRoleJurisdictions(db_config, user_name, couchDbHttpClient))
        {
            switch(role.role_name)
            {
                case "abstractor":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadCase));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteCase));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                    break;
                case "data_analyst":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadCase));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                    break;
                case "committee_member":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadDeidentifiedCase));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                    break;
                case "form_designer":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteMetadata));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                    break;
                case "jurisdiction_admin":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadUser));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteUser));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadJurisdiction));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteJurisdiction));
                    break;
                case "installation_admin":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadUser));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteUser));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadJurisdiction));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteJurisdiction));
                    break;
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
        return get_current_jurisdiction_id_set_for(
            db_config,
            p_user_name,
            CreateCompatibilityCouchDbHttpClient());
    }


    public static HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)> get_current_jurisdiction_id_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        string p_user_name,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        var result = new HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)>();

        foreach (var role in GetActiveUserRoleJurisdictions(db_config, p_user_name, couchDbHttpClient))
        {
            switch(role.role_name)
            {
                case "abstractor":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadCase));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteCase));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                    break;
                case "data_analyst":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadCase));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                    break;
                case "committee_member":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadDeidentifiedCase));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                    break;
                case "form_designer":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteMetadata));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                    break;
                case "jurisdiction_admin":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadUser));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteUser));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadJurisdiction));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteJurisdiction));
                    break;
                case "installation_admin":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadUser));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteUser));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadJurisdiction));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteJurisdiction));
                    break;

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
        return get_current_user_role_jurisdiction_set_for(
            db_config,
            p_user_name,
            CreateCompatibilityCouchDbHttpClient());
    }

    public static HashSet<(string jurisdiction_id, string user_id, string role_name)> get_current_user_role_jurisdiction_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        string p_user_name,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        var result = new HashSet<(string jurisdiction_id, string user_id, string role_name)>();

        foreach (var role in GetActiveUserRoleJurisdictions(db_config, p_user_name, couchDbHttpClient))
        {
            result.Add((role.jurisdiction_id, role.user_id, role.role_name));
        }

        return result;
    }

    /// <summary>
    /// Checks if the given jurisdiction hashset authorizes the specified resource action
    /// against the target user_role_jurisdiction. This overload accepts a pre-computed
    /// jurisdiction hashset so it can be called without a ClaimsPrincipal.
    /// </summary>
    public static bool is_authorized_to_handle_jurisdiction_id
    (
        HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)> jurisdiction_hashset,
        ResourceRightEnum p_resource_action, 
        mmria.common.model.couchdb.user_role_jurisdiction p_user_role_jurisdiction
    )
    {

        bool result = false;

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

    private static mmria.common.getset.CouchDbHttpClient CreateCompatibilityCouchDbHttpClient()
    {
        return new mmria.common.getset.CouchDbHttpClient(new mmria.common.SimpleHttpClientFactory());
    }

    private static List<mmria.common.model.couchdb.user_role_jurisdiction> GetActiveUserRoleJurisdictions(
        mmria.common.couchdb.DBConfigurationDetail db_config,
        string userName,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        return mmria.common.utils.AuthorizationRoleCache.GetOrLoadActiveUserRoles(
            db_config?.prefix,
            userName,
            () => LoadActiveUserRoleJurisdictions(db_config, userName, couchDbHttpClient));
    }

    private static List<mmria.common.model.couchdb.user_role_jurisdiction> LoadActiveUserRoleJurisdictions(
        mmria.common.couchdb.DBConfigurationDetail db_config,
        string userName,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        var result = new List<mmria.common.model.couchdb.user_role_jurisdiction>();
        string quotedUserName = $"\"{userName}\"";
        string encodedUserName = Uri.EscapeDataString(quotedUserName);
        string jurisdicion_view_url =
            $"{db_config.url}/{db_config.prefix}jurisdiction/_design/sortable/_view/by_user_id?startkey={encodedUserName}&endkey={encodedUserName}";
        string jurisdicion_result_string = null;

        try
        {
            jurisdicion_result_string = couchDbHttpClient.ExecuteAsync(
                "GET",
                jurisdicion_view_url,
                null,
                db_config.user_name,
                db_config.user_value,
                "application/json").GetAwaiter().GetResult();
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(
                $"Current-user role lookup failed. user={userName}; prefix={db_config.prefix}; view={jurisdicion_view_url}; exceptionType={ex.GetType().FullName}; message={ex.Message}");
            return result;
        }

        var jurisdiction_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<
            mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.user_role_jurisdiction>>(
            jurisdicion_result_string);

        if (jurisdiction_view_response?.rows == null)
        {
            return result;
        }

        var now_date = DateTime.Now;
        foreach (var jvi in jurisdiction_view_response.rows)
        {
            if (jvi?.key == null || jvi.value?.user_id != userName)
            {
                continue;
            }

            if (!IsActiveRole(jvi.value, now_date))
            {
                continue;
            }

            // Guard against jurisdiction documents with a missing role_name. Such rows
            // would later be passed to new Claim(ClaimTypes.Role, role, ...), which
            // throws ArgumentNullException and 500s the entire sign-in flow.
            // Skip + log so the offending document can be located and corrected.
            if (string.IsNullOrWhiteSpace(jvi.value.role_name))
            {
                System.Console.WriteLine(
                    $"Skipping jurisdiction role with null/empty role_name. user={userName}; prefix={db_config.prefix}; jurisdiction_id={jvi.value.jurisdiction_id}; doc_id={jvi.value._id}");
                continue;
            }

            result.Add(jvi.value);
        }

        return result;
    }

    private static bool IsActiveRole(mmria.common.model.couchdb.user_role_jurisdiction value, DateTime nowDate)
    {
        if (value == null ||
            value.is_active == null ||
            value.effective_start_date == null ||
            !value.is_active.HasValue ||
            !value.effective_start_date.HasValue)
        {
            return false;
        }

        var effectiveEndDate = value.effective_end_date.HasValue
            ? value.effective_end_date.Value
            : nowDate;

        return value.is_active.Value &&
            value.effective_start_date.Value <= nowDate &&
            nowDate <= effectiveEndDate;
    }

}
#endif
