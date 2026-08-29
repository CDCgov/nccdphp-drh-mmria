#if IS_PMSS_ENHANCED
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using mmria.common.SharedLibraries.Jurisdiction;
using mmria.common.SharedLibraries.Jurisdiction.DAL;
using mmria.common.SharedLibraries.Jurisdiction.Model;

namespace mmria.pmss.server.utils;

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
    WriteJurisdiction,
    ReadVro
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
            CreateCompatibilityReader());
    }


    public static HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)> get_current_jurisdiction_id_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        return get_current_jurisdiction_id_set_for(
            db_config,
            p_claims_principal,
            new JurisdictionAuthorizationDAL(couchDbHttpClient));
    }

    public static HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)> get_current_jurisdiction_id_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        System.Security.Claims.ClaimsPrincipal p_claims_principal,
        IJurisdictionAuthorizationReader reader
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
        foreach (var role in GetActiveUserRoleJurisdictions(db_config, user_name, reader))
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
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadCase));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteCase));
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
                case "vro":
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadCase));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.WriteCase));
                    result.Add((role.jurisdiction_id, ResourceRightEnum.ReadMetadata));
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
            CreateCompatibilityReader());
    }


    public static HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)> get_current_jurisdiction_id_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        string p_user_name,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        return get_current_jurisdiction_id_set_for(
            db_config,
            p_user_name,
            new JurisdictionAuthorizationDAL(couchDbHttpClient));
    }

    public static HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)> get_current_jurisdiction_id_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        string p_user_name,
        IJurisdictionAuthorizationReader reader
    )
    {

        var result = new HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)>();

        foreach (var role in GetActiveUserRoleJurisdictions(db_config, p_user_name, reader))
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
            CreateCompatibilityReader());
    }

    public static HashSet<(string jurisdiction_id, string user_id, string role_name)> get_current_user_role_jurisdiction_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        string p_user_name,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        return get_current_user_role_jurisdiction_set_for(
            db_config,
            p_user_name,
            new JurisdictionAuthorizationDAL(couchDbHttpClient));
    }

    public static HashSet<(string jurisdiction_id, string user_id, string role_name)> get_current_user_role_jurisdiction_set_for
    (
        mmria.common.couchdb.DBConfigurationDetail db_config,
        string p_user_name,
        IJurisdictionAuthorizationReader reader
    )
    {
        var result = new HashSet<(string jurisdiction_id, string user_id, string role_name)>();

        foreach (var role in GetActiveUserRoleJurisdictions(db_config, p_user_name, reader))
        {
            result.Add((role.jurisdiction_id!, role.user_id!, role.role_name!));
        }

        return result;
    }

    private static IJurisdictionAuthorizationReader CreateCompatibilityReader()
    {
        return new JurisdictionAuthorizationDAL(
            new mmria.common.getset.CouchDbHttpClient(new mmria.common.SimpleHttpClientFactory()));
    }

    private static IReadOnlyList<JurisdictionRoleEntry> GetActiveUserRoleJurisdictions(
        mmria.common.couchdb.DBConfigurationDetail db_config,
        string userName,
        IJurisdictionAuthorizationReader reader)
    {
        return mmria.common.utils.AuthorizationRoleCache.GetOrLoadActiveUserRoles(
            db_config?.prefix,
            userName,
            reader,
            db_config);
    }

}
#endif

