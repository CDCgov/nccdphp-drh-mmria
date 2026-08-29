using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;  
namespace mmria.server;

[Route("api/[controller]/{rev?}")]
public sealed class validatorController: ControllerBase
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager _metadataVersionManager;
    public validatorController
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
    public async Task<FileResult> Get()
    {
        FileResult result = null;

        try
        {
            string responseString = await _metadataVersionManager.GetValidatorAsync(db_config);
            
            byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(responseString);
            result = File(responseBytes, "application/javascript", "validator");

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    } 
    public static byte[] ReadFully(System.IO.Stream input)
    {
        byte[] buffer = new byte[16*1024];
        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        {
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }
    }


    [Authorize(Roles  = "form_designer")]
    [HttpPost]
    [HttpPut]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post() 
    { 
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        //if(!string.IsNullOrWhiteSpace(json))
        try
        {

            System.IO.Stream dataStream0 = this.Request.Body;
            // Open the stream using a StreamReader for easy access.
            //dataStream0.Seek(0, System.IO.SeekOrigin.Begin);
            System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);
            // Read the content.
            string validator_js_text = await reader0.ReadToEndAsync ();

            try
            {
                result = await _metadataVersionManager.SaveValidatorAsync(validator_js_text, db_config);
            }
            catch(Exception ex)
            {
                Console.WriteLine (ex);
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
} 


