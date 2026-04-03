using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using mmria.common.utils;

using  mmria.server.extension;
using mmria.server.util;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class metadataController: ControllerBase 
{ 
    private readonly mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager _metadataVersionManager;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public metadataController
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
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();
        var sanitizedMetadata = DocumentPayloadCloneHelper.CloneMetadataApp(metadata, GetCurrentUserName());

        if (sanitizedMetadata == null)
        {
            return result;
        }

        try
        {
            result = await _metadataVersionManager.SaveMetadataAsync(sanitizedMetadata, db_config);
            if (result == null || !result.ok)
            {
                var revisionHandling = CouchDbRevisionHelper.DescribeRevisionHandling(metadata?._rev, null);
                Console.WriteLine(
                    $"Metadata save failed for {sanitizedMetadata._id}: rev={revisionHandling}; response={result?.error_description}");
            }

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
        var sanitizedVersionSpecification = DocumentPayloadCloneHelper.CloneVersionSpecification(p_version_specification, GetCurrentUserName());

        if
        (
            sanitizedVersionSpecification == null ||
            sanitizedVersionSpecification.data_type == null ||
            sanitizedVersionSpecification.data_type != "version-specification" || 
            sanitizedVersionSpecification._id == "2016-06-12T13:49:24.759Z" ||
            sanitizedVersionSpecification._id == "de-identified-list"

        )
        {
            return null;
        }


        try
        {

            result = await _metadataVersionManager.SaveMetadataVersionSpecificationAsync(sanitizedVersionSpecification, db_config);
            if (result == null || !result.ok)
            {
                var revisionHandling = CouchDbRevisionHelper.DescribeRevisionHandling(p_version_specification?._rev, null);
                Console.WriteLine(
                    $"Metadata version specification save failed for {sanitizedVersionSpecification._id}: rev={revisionHandling}; response={result?.error_description}");
            }

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

} 


