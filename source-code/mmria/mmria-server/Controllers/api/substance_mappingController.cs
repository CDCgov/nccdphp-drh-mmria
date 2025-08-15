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
namespace mmria.server.Controllers;

[Route("api/[controller]")]
public sealed class substance_mappingController : ControllerBase
{
        mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public substance_mappingController
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
    //[Route("list")]
    [HttpGet]
    public async Task<mmria.common.metadata.Substance_Mapping> Get()
    {
        mmria.common.metadata.Substance_Mapping result = null;
        try
        {
        string request_string = $"{db_config.url}/metadata/substance-mapping";
        var case_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
        string responseFromServer = await case_curl.executeAsync();
        result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Substance_Mapping>(responseFromServer);
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
            string url = $"{db_config.url}/metadata/substance-mapping";
            //System.Console.WriteLine ("json\n{0}", object_string);

            cURL put_document_curl = new cURL("PUT", null, url, document_content, db_config.user_name, db_config.user_value);

            //bool save_document = false;


            try
            {
            string responseFromServer = await put_document_curl.executeAsync();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);


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

