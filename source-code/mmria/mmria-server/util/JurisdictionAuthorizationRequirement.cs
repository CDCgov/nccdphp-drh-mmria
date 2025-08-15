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
    mmria.common.couchdb.DBConfigurationDetail db_config;

    public HasJurisdictionAuthorizationHandler(mmria.common.couchdb.DBConfigurationDetail _db_config)
    {
        db_config = _db_config;
    }

    protected override Task HandleRequirementAsync
    (
        AuthorizationHandlerContext context, 
        JurisdictionAuthorizationRequirement requirement,
        System.Dynamic.ExpandoObject caseExpandoObject
        
    )
    {
        if (!context.User.HasClaim(c => c.Type == "JurisdictionList" && 
                                        c.Issuer == "https://contoso.com"))
        {
            return Task.CompletedTask;
        }



        string jurisdicion_view_url = $"{db_config.url}/{db_config.prefix}jurisdiction/_design/sortable/_view/by_user_id?";
        var jurisdicion_curl = new cURL("POST", null, jurisdicion_view_url, null, db_config.user_name, db_config.user_value);
        string jurisdicion_result_string = null;
        try
        {
            jurisdicion_result_string = jurisdicion_curl.execute();
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex);
        }
        
        var jurisdiction_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<mmria.common.model.couchdb.jurisdiction_view_sortable_item>>(jurisdicion_result_string);
        //IDictionary<string, object> jurisdicion_result_dictionary = jurisdicion_result_data[0] as IDictionary<string, object>;
        //var juridiction_curl = new cURL();
        foreach(mmria.common.model.couchdb.get_response_item<mmria.common.model.couchdb.jurisdiction_view_sortable_item> jvi in jurisdiction_view_response.rows)
        {
            
            
        }

        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
