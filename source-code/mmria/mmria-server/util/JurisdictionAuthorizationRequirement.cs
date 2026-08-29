using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;


namespace mmria.server.utils;

public sealed class JurisdictionAuthorizationRequirement : IAuthorizationRequirement
{
}


public sealed class HasJurisdictionAuthorizationHandler : AuthorizationHandler<JurisdictionAuthorizationRequirement, System.Dynamic.ExpandoObject>
{
    private readonly mmria.server.util.RequestTenantRuntime _tenantRuntime;
    private readonly mmria.common.SharedLibraries.Jurisdiction.IJurisdictionAuthorizationReader _jurisdictionAuthorizationReader;

    public HasJurisdictionAuthorizationHandler(
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.SharedLibraries.Jurisdiction.IJurisdictionAuthorizationReader jurisdictionAuthorizationReader)
    {
        _tenantRuntime = tenantRuntime;
        _jurisdictionAuthorizationReader = jurisdictionAuthorizationReader;
    }

    protected override async Task HandleRequirementAsync
    (
        AuthorizationHandlerContext context, 
        JurisdictionAuthorizationRequirement requirement,
        System.Dynamic.ExpandoObject caseExpandoObject
        
    )
    {
        if (!context.User.HasClaim(c => c.Type == "JurisdictionList" && 
                                        c.Issuer == "https://contoso.com"))
        {
            return;
        }

        var db_config = _tenantRuntime.DbConfig;
        System.Collections.Generic.IReadOnlyList<mmria.common.SharedLibraries.Jurisdiction.Model.JurisdictionRoleEntry> jurisdiction_roles = null;
        try
        {
            jurisdiction_roles = await _jurisdictionAuthorizationReader.GetRolesByUserIdAsync(null, db_config);
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex);
        }
        
        //IDictionary<string, object> jurisdicion_result_dictionary = jurisdicion_result_data[0] as IDictionary<string, object>;
        foreach(var jvi in jurisdiction_roles ?? System.Array.Empty<mmria.common.SharedLibraries.Jurisdiction.Model.JurisdictionRoleEntry>())
        {
            
            
        }

        context.Succeed(requirement);

        return;
    }
}
