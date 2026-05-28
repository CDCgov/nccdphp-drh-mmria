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
namespace mmria.server;

[Route("api/[controller]")]
public sealed class version_code_genController: ControllerBase
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager _metadataVersionManager;

    public version_code_genController
	(
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager metadataVersionManager
    )
    {
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
        _metadataVersionManager = metadataVersionManager;
    }

    [AllowAnonymous] 
    [HttpGet]
    public async Task<string> Get()
    {
        string result = null;

        try
        {
            result = await _metadataVersionManager.GetValidatorAsync(db_config);
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    } 

    // POST api/values 
    [AllowAnonymous] 
    [HttpPost]
    [HttpPut]
    public async System.Threading.Tasks.Task<ContentResult> Post
    (
        [FromBody] System.Dynamic.ExpandoObject code_gen_request
    ) 
    { 
        var generatedFile = "";

        //if(!string.IsNullOrWhiteSpace(json))
        try
        {

            var byName = (IDictionary<string,object>)code_gen_request;
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var payload_string = Newtonsoft.Json.JsonConvert.SerializeObject(byName["payload"], settings);
            generatedFile = await GenerateFileAsync(payload_string);

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        }
/*
        this.Response.Clear();
        this.Response.ClearHeaders();
        this.Response.AddHeader("Content-Type", "text/plain");
*/
        return Content(generatedFile, "text/plain");
    }

    async Task<string> GenerateFileAsync(string schemaJson)
    {
        string result = null;

        var schema = await NJsonSchema.JsonSchema.FromJsonAsync(schemaJson);
        var settings = new NJsonSchema.CodeGeneration.CSharp.CSharpGeneratorSettings()
        {
            Namespace = "AwesomeSauce.v1",
            ClassStyle = NJsonSchema.CodeGeneration.CSharp.CSharpClassStyle.Poco,
            GenerateJsonMethods = true,
            GenerateDataAnnotations = true
        };

        var generator = new NJsonSchema.CodeGeneration.CSharp.CSharpGenerator(schema, settings);
        result = generator.GenerateFile();
        return result;
    }

} 


