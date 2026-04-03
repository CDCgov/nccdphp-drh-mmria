using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Serilog;
using Serilog.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.server.util;
namespace mmria.server;

[Authorize(Policy = "form_designer")]
[Route("api/[controller]")]
public sealed class ui_specificationController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager _metadataVersionManager;
    public ui_specificationController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager metadataVersionManager
    )
    {
        _metadataVersionManager = metadataVersionManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }


    [Route("list")]
    [AllowAnonymous] 
    [HttpGet]
    public async System.Threading.Tasks.Task<List<mmria.common.metadata.UI_Specification>> List()
    {
        Log.Information  ("Recieved message.");
        var result = new List<mmria.common.metadata.UI_Specification>();

        try
        {
            result = await _metadataVersionManager.ListUiSpecificationsAsync(db_config);
        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }



    [Route("{id?}")]
    [AllowAnonymous] 
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.metadata.UI_Specification> Get(string id = "default-ui-specification")
    {
        Log.Information  ("Recieved message.");
        var result = new mmria.common.metadata.UI_Specification();

        try
        {
            result = await _metadataVersionManager.GetUiSpecificationAsync(id, db_config);
        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }


    [Route("{id?}")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] mmria.common.metadata.UI_Specification ui_specification
    ) 
    { 
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();
        var sanitizedUiSpecification = DocumentPayloadCloneHelper.CloneUiSpecification(ui_specification, GetCurrentUserName());

        try
        {


            if
            (
                sanitizedUiSpecification == null ||
                sanitizedUiSpecification.data_type == null ||
                sanitizedUiSpecification.data_type != "ui-specification" || 
                sanitizedUiSpecification._id == "2016-06-12T13:49:24.759Z" ||
                sanitizedUiSpecification._id == "de-identified-list"

            )
            {
                return null;
            }

            result = await _metadataVersionManager.SaveUiSpecificationAsync(sanitizedUiSpecification, db_config);


            if (!result.ok) 
            {

            }

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }
            
        return result;
    } 

    private string GetCurrentUserName()
    {
        if (User?.Identities?.Any(u => u.IsAuthenticated) == true)
        {
            return User.Identities.First(
                u => u.IsAuthenticated &&
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                .FindFirst(System.Security.Claims.ClaimTypes.Name)
                .Value;
        }

        return null;
    }


    [Route("{_id?}")]
    [HttpDelete]
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Delete(string _id = null, string rev = null) 
    { 
        try
        {
            return await _metadataVersionManager.DeleteUiSpecificationAsync(_id, rev, db_config);

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }


} 

