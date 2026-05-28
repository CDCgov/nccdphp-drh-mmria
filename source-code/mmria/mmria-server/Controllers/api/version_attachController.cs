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
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager _metadataVersionManager;
    public version_attachController
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
                result = await _metadataVersionManager.SaveVersionAttachmentAsync(add_attachement, db_config, true);
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

