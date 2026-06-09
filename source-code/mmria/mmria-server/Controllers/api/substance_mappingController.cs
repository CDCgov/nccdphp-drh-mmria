using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mmria.common.model;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.common.SharedLibraries.MetadataVersion.Manager;
namespace mmria.server.Controllers;

[Route("api/[controller]")]
public sealed class substance_mappingController : ControllerBase
{
        mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly MetadataVersionManager _metadataVersionManager;

    public substance_mappingController
	(
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        MetadataVersionManager metadataVersionManager
    )
    {
        _metadataVersionManager = metadataVersionManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [AllowAnonymous]
    //[Route("list")]
    [HttpGet]
    public async Task<mmria.common.metadata.Substance_Mapping> Get()
    {
        mmria.common.metadata.Substance_Mapping result = null;
        try
        {
        result = await _metadataVersionManager.GetSubstanceMappingAsync(db_config);
        }
        catch (Exception ex)
        {
        Console.WriteLine(ex);
        }

        return result;
    }

    [Authorize(Roles = "form_designer,cdc_analyst")]
    //[Route("{id}")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (

    //mmria.common.metadata.Add_Attachement add_attachement
    )
    {
        string document_content;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response();

        try
        {
        System.IO.Stream dataStream0 = this.Request.Body;
        //dataStream0.Seek(0, System.IO.SeekOrigin.Begin);
        System.IO.StreamReader reader0 = new System.IO.StreamReader(dataStream0);

        document_content = await reader0.ReadToEndAsync();

        mmria.common.metadata.Substance_Mapping substance_mapping = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Substance_Mapping>(document_content);

        if(substance_mapping._id == "substance-mapping")
        {
            try
            {
            result = await _metadataVersionManager.SaveSubstanceMappingAsync(document_content, db_config);
            }
            catch (Exception ex)
            {
            Console.WriteLine(ex);
            }
        }

        if (!result.ok)
        {

        }

        }
        catch (Exception ex)
        {
        Console.WriteLine(ex);
        }

        return result;
    }

    public static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

}

