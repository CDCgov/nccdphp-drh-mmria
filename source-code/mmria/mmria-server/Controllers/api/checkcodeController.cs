using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;  
namespace mmria.pmss.server;

[Route("api/[controller]")]
public sealed class checkcodeController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager _metadataVersionManager;
    public checkcodeController
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
    public async System.Threading.Tasks.Task<string> Get()
    {
        System.Console.WriteLine ("Recieved message.");
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


    public class PutCheckCodeRequest
    {
        public PutCheckCodeRequest(){}

        public string data { get; set; }
    }

    [Authorize(Roles  = "form_designer")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Put
    (
        [FromBody] PutCheckCodeRequest CheckCodeRequest
    ) 
    { 
        //string check_code_json;
        string check_code_json = GetSanitizedCheckCodeJson(CheckCodeRequest);
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

            if (string.IsNullOrWhiteSpace(check_code_json))
            {
                return result;
            }

            try
            {
                /*

                System.IO.Stream dataStream0 = this.Request.Body;
                // Open the stream using a StreamReader for easy access.
                //dataStream0.Seek(0, System.IO.SeekOrigin.Begin);
                System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);
                // Read the content.
                check_code_json = await reader0.ReadToEndAsync ();
                */

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

    private static string GetSanitizedCheckCodeJson(PutCheckCodeRequest request)
    {
        return request?.data;
    }
} 


