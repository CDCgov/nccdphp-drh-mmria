using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class metadataController: ControllerBase 
{ 
    private readonly mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager _metadataVersionManager;
    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public metadataController
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
        
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(
            _overridableConfigSets,
            _configuration,
            host_prefix
        );
        
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(
            _dbConfigSets,
            _configuration,
            host_prefix
        );
    }
    
    [AllowAnonymous] 
    [HttpGet]
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Get()
    {
        //System.Console.WriteLine ("Recieved message.");
        string result = null;
        System.Dynamic.ExpandoObject json_result = null;
        try
        {
            json_result = await _metadataVersionManager.GetMetadataAsync(db_config);
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return json_result;
    }


    [AllowAnonymous] 
    [Route("{id}")]
    [HttpGet]
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Get(string id)
    {
        //System.Console.WriteLine ("Recieved message.");
        string result = null;
        System.Dynamic.ExpandoObject json_result = null;
        try
        {
            json_result = await _metadataVersionManager.GetMetadataAsync(id, db_config);
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return json_result;
    }


    [Authorize(Policy = "form_designer")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] mmria.common.metadata.app metadata
    ) 
    { 
        string object_string = null;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {
            result = await _metadataVersionManager.SaveMetadataAsync(metadata, db_config);

            if (!result.ok) 
            {

            }

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }
            
        return result;
    } 



    [HttpGet("GetCheckCode")]
    public async System.Threading.Tasks.Task<string> GetCheckCode()
    {
        //System.Console.WriteLine ("Recieved message.");
        string result = null;

        try
        {
            result = await _metadataVersionManager.GetCheckCodeAsync(db_config);
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }


    [Authorize(Roles  = "form_designer")]
    [HttpPost("PutCheckCode")]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> PutCheckCode
    (
        
    ) 
    { 
        string check_code_json;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {

            System.IO.Stream dataStream0 = this.Request.Body;
            System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);

            check_code_json = await reader0.ReadToEndAsync ();

            result = await _metadataVersionManager.SaveCheckCodeAsync(check_code_json, db_config);

            if (!result.ok) 
            {

            }

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }
            
        return result;
    } 

    [Authorize(Roles  = "form_designer")]
    [Route("{id}")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] mmria.common.metadata.Version_Specification p_version_specification
    ) 
    { 
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        if
        (
            p_version_specification.data_type == null ||
            p_version_specification.data_type != "version-specification" || 
            p_version_specification._id == "2016-06-12T13:49:24.759Z" ||
            p_version_specification._id == "de-identified-list"

        )
        {
            return null;
        }


        try
        {

            result = await _metadataVersionManager.SaveMetadataVersionSpecificationAsync(p_version_specification, db_config);

            if (!result.ok) 
            {

            }

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }
        
        return result;
    }

} 


