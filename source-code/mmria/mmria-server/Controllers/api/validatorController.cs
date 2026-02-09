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
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    public validatorController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
    }


    [AllowAnonymous] 
    [HttpGet]
    public async Task<FileResult> Get()
    {
        FileResult result = null;

        try
        {
            string request_string = db_config.url + $"/metadata/2016-06-12T13:49:24.759Z/validator.js";

            string responseString = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                request_string,
                null,
                null,
                null
            );
            
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

            string metadata_url = db_config.url + "/metadata/2016-06-12T13:49:24.759Z/validator.js";

            var revision = await get_revision(db_config.url + "/metadata/2016-06-12T13:49:24.759Z");

            var headerDict = new Dictionary<string, string>();


/*
            System.Net.WebRequest request = System.Net.WebRequest.Create(new System.Uri(metadata_url));
            request.Method = "PUT";
            request.ContentType = "text/*";
            request.ContentLength = validator_js_text.Length;
            request.PreAuthenticate = false;

            if (!string.IsNullOrWhiteSpace(this.Request.Cookies["AuthSession"]))
            {
                string auth_session_value = this.Request.Cookies["AuthSession"];
                request.Headers.Add("Cookie", "AuthSession=" + auth_session_value);
                request.Headers.Add("X-CouchDB-WWW-Authenticate", auth_session_value);
                request.Headers.Add("X-CouchDB-WWW-Authenticate", auth_session_value);
            }
*/

            

            if (!string.IsNullOrWhiteSpace(revision))
            {
                headerDict.Add("If-Match", revision);
                //System.Text.RegularExpressions.Regex rgx = new System.Text.RegularExpressions.Regex("[^a-zA-Z0-9 -]");
                //string If_Match = rgx.Replace(this.Request.Headers["If-Match"], "");
                
            }

            try
            {

                /*
                streamWriter.Write(validator_js_text);
                streamWriter.Flush();
                streamWriter.Close();

                System.Net.WebResponse response = (System.Net.HttpWebResponse) await request.GetResponseAsync();
                System.IO.Stream dataStream = response.GetResponseStream ();
                System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);

                                    if(response.Headers["Set-Cookie"] != null)
                {
                    this.Response.Headers.Add("Set-Cookie", response.Headers["Set-Cookie"]);
                }
                    */
                string responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", metadata_url, validator_js_text, db_config.user_name, db_config.user_value, "text/*", headerDict);

                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);



            //System.Threading.Tasks.Task.Run( new Action(()=> { var f = new GenerateSwaggerFile(); System.IO.File.WriteAllText(Program.config_file_root_folder + "/api-docs/api.json", f.generate(metadata)); }));
                
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

    private async System.Threading.Tasks.Task<string> get_revision(string p_document_url)
    {

        string result = null;

        string temp_document_json = null;

        try
        {
            
            temp_document_json = await _couchDbHttpClient.ExecuteAsync("GET", p_document_url, null, db_config.user_name, db_config.user_value);
            var request_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(temp_document_json);
            IDictionary<string, object> updater = request_result as IDictionary<string, object>;
            if(updater != null && updater.ContainsKey("_rev"))
            {
                result = updater ["_rev"].ToString ();
            }
        }
        catch(Exception ex) 
        {
            if (!(ex.Message.IndexOf ("(404) Object Not Found") > -1)) 
            {
                //System.Console.WriteLine ("c_sync_document.get_revision");
                //System.Console.WriteLine (ex);
            }
        }

        return result;
    }
} 


