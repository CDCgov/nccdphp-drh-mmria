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
    public version_attachController
	(
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    [Authorize(Roles  = "form_designer")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        
        //mmria.common.metadata.Add_Attachement add_attachement
    ) 
    { 
        string document_content;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

            try
            {


                mmria.common.metadata.Add_Attachement add_attachement = new common.metadata.Add_Attachement();

                System.IO.Stream dataStream0 = this.Request.Body;

                //dataStream0.Seek(0, System.IO.SeekOrigin.Begin);
                System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);

                document_content = await reader0.ReadToEndAsync ();

                var split_one = document_content.Split("&");

                foreach(var key_pair in split_one)
                {

                    var split_two = key_pair.Split("=");

                    switch(split_two[0].ToString())
                    {
                        case "_id":
                            add_attachement._id = split_two[1].ToString();
                            break;
                        case "_rev":
                            add_attachement._rev = split_two[1].ToString();
                        break;
                        case "doc_name":
                            add_attachement.doc_name = split_two[1].ToString();
                        break;
                        case "document_content":
                            //add_attachement.document_content  = Base64Decode(split_two[1]);

                            add_attachement.document_content  = System.Net.WebUtility.UrlDecode(split_two[1]);

                            //add_attachement.document_content  = System.Text.Encoding.UTF8.DecodeBase64(split_two[1]);
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
                cURL check_document_curl = new cURL ("Get", null, check_url, null, db_config.user_name, db_config.user_value);

                bool save_document = false;

                try
                {
                    string responseFromServer = await check_document_curl.executeAsync();
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

                    var put_curl = new cURL("PUT", null, metadata_url, add_attachement.document_content, db_config.user_name, db_config.user_value, "text/*");
                    put_curl.AddHeader("If-Match",  add_attachement._rev);

                    string responseFromServer = await put_curl.executeAsync();

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

