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

    public version_code_genController
	(
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    [AllowAnonymous] 
    [HttpGet]
    public async Task<string> Get()
    {
        string result = null;

        try
        {

            string request_string = db_config.url + $"/metadata/2016-06-12T13:49:24.759Z/validator.js";

            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));
            request.Method = "GET";
            request.PreAuthenticate = false;

            System.Net.WebResponse response = (System.Net.HttpWebResponse) await request.GetResponseAsync();
            System.IO.Stream dataStream = response.GetResponseStream();
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            result = await reader.ReadToEndAsync ();

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
            var payload = byName["payload"].ToString(); 
            //string id_val = null;


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
                //ClassStyle = NJsonSchema.CodeGeneration.CSharp.CSharpClassStyle.Inpc 
                ClassStyle = NJsonSchema.CodeGeneration.CSharp.CSharpClassStyle.Poco,
                GenerateJsonMethods = true,
                GenerateDataAnnotations = true
            };

            var generator = new NJsonSchema.CodeGeneration.CSharp.CSharpGenerator(schema, settings);
            result = generator.GenerateFile();

//NJsonSchema.CodeGeneration.CSharp.CSharpClassStyle.
            return result;
    }

} 


