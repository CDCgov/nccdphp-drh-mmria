using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Serilog;
using Serilog.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server;

[Authorize(Policy = "form_designer")]
[Route("api/[controller]")]
public sealed class ui_specificationController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager _metadataVersionManager;
    public ui_specificationController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager metadataVersionManager
    )
    {
        _metadataVersionManager = metadataVersionManager;
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
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
        string ui_specification_json;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {


            if
            (
                ui_specification.data_type == null ||
                ui_specification.data_type != "ui-specification" || 
                ui_specification._id == "2016-06-12T13:49:24.759Z" ||
                ui_specification._id == "de-identified-list"

            )
            {
                return null;
            }

            result = await _metadataVersionManager.SaveUiSpecificationAsync(ui_specification, db_config);


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

