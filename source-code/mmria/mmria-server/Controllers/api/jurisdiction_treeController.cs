using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Serilog;
using Serilog.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using mmria.common.utils;
using mmria.common.SharedLibraries.Jurisdiction.Manager;

using  mmria.server.extension;
namespace mmria.server;

[Route("api/[controller]")]
public sealed class jurisdiction_treeController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly JurisdictionManager _jurisdictionManager;
    public class case_folder_metadata
    {
      public string Name { get; set; }
      public string ParentName { get; set; }
      public int NestedLevel { get; set; }
    }
    public jurisdiction_treeController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        JurisdictionManager jurisdictionManager
    )
    {
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
        _jurisdictionManager = jurisdictionManager;
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.jurisdiction_tree> Get()
    {
        Log.Information  ("Recieved message.");
        mmria.common.model.couchdb.jurisdiction_tree result = null;

        try
        {
            result = await _jurisdictionManager.GetJurisdictionTreeAsync(db_config);

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }

    [Route("new_case_folder")]
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.jurisdiction_tree> GetJurisdictionTree()
    {
        Log.Information  ("Recieved message.");
        mmria.common.model.couchdb.jurisdiction_tree result = null;

        try
        {
            result = await _jurisdictionManager.GetJurisdictionTreeAsync(db_config);

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }

    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post()
    {
        var jurisdiction_tree = await mmria.server.util.JsonRequestBodyReader.ReadAsync<mmria.common.model.couchdb.jurisdiction_tree>(Request);
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {

            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;
            }

            try
            {
                result = await _jurisdictionManager.SaveJurisdictionTreeAsync(jurisdiction_tree, userName, db_config);

                if (result == null || !result.ok)
                {
                    Log.Warning(
                        "jurisdiction_tree save failed for {DocumentId}; response={Response}",
                        "jurisdiction/jurisdiction_tree",
                        result?.error_description);
                }
            }
            catch(Exception ex)
            {
                Log.Information ($"jurisdiction_treeController:{ex}");
            }

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }
            
        return result;
    } 

} 


