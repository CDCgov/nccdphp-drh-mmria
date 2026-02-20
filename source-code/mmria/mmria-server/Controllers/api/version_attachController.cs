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
public sealed class version_attachController: ControllerBase
{ 

    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    public version_attachController
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

    [Authorize(Roles  = "form_designer")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        
        //mmria.common.metadata.Add_Attachement add_attachement
    ) 
    { 
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

            try
            {
                mmria.common.metadata.Add_Attachement add_attachement = new common.metadata.Add_Attachement();

                // Read raw body to handle large form data properly
                Request.EnableBuffering(); // Allow multiple reads
                string bodyContent;
                using (var reader = new System.IO.StreamReader(Request.Body, System.Text.Encoding.UTF8, leaveOpen: true))
                {
                    bodyContent = await reader.ReadToEndAsync();
                    Request.Body.Position = 0; // Reset for potential re-reads
                }
                
                Console.WriteLine($"Raw body length: {bodyContent.Length}");
                
                // Parse URL-encoded form data manually
                var keyValuePairs = bodyContent.Split('&');
                
                foreach (var pair in keyValuePairs)
                {
                    var firstEquals = pair.IndexOf('=');
                    if (firstEquals == -1) continue;
                    
                    var key = pair.Substring(0, firstEquals);
                    var value = pair.Substring(firstEquals + 1);
                    
                    // URL decode the value
                    var decodedValue = System.Net.WebUtility.UrlDecode(value);
                    
                    switch (key)
                    {
                        case "_id":
                            add_attachement._id = decodedValue;
                            Console.WriteLine($"_id: {add_attachement._id}");
                            break;
                        case "_rev":
                            add_attachement._rev = decodedValue;
                            Console.WriteLine($"_rev: {add_attachement._rev}");
                            break;
                        case "doc_name":
                            add_attachement.doc_name = decodedValue;
                            Console.WriteLine($"doc_name: {add_attachement.doc_name}");
                            break;
                        case "document_content":
                            add_attachement.document_content = decodedValue;
                            Console.WriteLine($"document_content length: {add_attachement.document_content?.Length ?? 0}");
                            Console.WriteLine($"document_content starts with: {add_attachement.document_content?.Substring(0, Math.Min(100, add_attachement.document_content.Length))}");
                            break;
                    }
                }

                
                if
                (
                    //p_version_specification.data_type == null ||
                    //p_version_specification.data_type != "version-specification" || 
                    add_attachement._id =="default_ui_specification" ||
                    add_attachement._id == "2016-06-12T13:49:24.759Z" ||
                    add_attachement._id == "de-identified-list"

                )
                {
                    return null;
                }

                string check_url = db_config.url + "/metadata/"  + add_attachement._id;

                bool save_document = false;

                try
                {
                    string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", check_url, null, db_config.user_name, db_config.user_value);
                    var check_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Version_Specification>(responseFromServer);

                    if
                    (
                        !string.IsNullOrWhiteSpace(check_result.data_type) &&
                        check_result.data_type == "version-specification" 
                    )
                    {
                        if(string.IsNullOrWhiteSpace(check_result.data_type))
                        {
                            save_document = true;
                        }
                        else if(check_result.publish_status != common.metadata.publish_status_enum.final)
                        {
                            save_document = true;
                        }
                        
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
                
                if(save_document)
                {

                    string metadata_url = db_config.url + $"/metadata/{add_attachement._id}/{add_attachement.doc_name}";

                    var headerDict = new Dictionary<string, string>();
                    headerDict.Add("If-Match", add_attachement._rev);

                    string responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", metadata_url, add_attachement.document_content, db_config.user_name, db_config.user_value, "text/*", headerDict);

                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

                    if (!result.ok) 
                    {

                    }
                }
            }
            catch(Exception ex) 
            {
                Console.WriteLine (ex);
            }
            
        return result;
    } 

    public static string Base64Decode(string base64EncodedData) 
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

} 

