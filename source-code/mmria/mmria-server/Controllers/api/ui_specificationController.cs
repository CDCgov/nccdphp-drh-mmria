using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Serilog;
using Serilog.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server;

[Authorize(Policy = "form_designer")]
[Route("api/[controller]")]
public sealed class ui_specificationController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public ui_specificationController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }


    [Route("list")]
    [AllowAnonymous] 
    [HttpGet]
    public async System.Threading.Tasks.Task<List<mmria.common.metadata.UI_Specification>> List()
    {
        Log.Information  ("Recieved message.");
        var result = new List<mmria.common.metadata.UI_Specification>();

        try
        {
            string ui_specification_url = db_config.url + $"/metadata/_all_docs?include_docs=true";

            var curl = new cURL("GET", null, ui_specification_url, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await curl.executeAsync();

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings{
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    MissingMemberHandling =  Newtonsoft.Json.MissingMemberHandling.Ignore
            };
            var ui_specification_list = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<mmria.common.metadata.UI_Specification>> (responseFromServer, settings);

            foreach(var row in ui_specification_list.rows)
            {
                var ui_specification = row.doc;
                if
                (
                    ui_specification.data_type == null || 
                    ui_specification.data_type != "ui-specification"|| 
                    ui_specification._id == "2016-06-12T13:49:24.759Z" ||
                    ui_specification._id == "de-identified-list"
                )
                {
                    continue;
                }
                result.Add(row.doc);
                    
            }

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }



    [Route("{id?}")]
    [AllowAnonymous] 
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.metadata.UI_Specification> Get(string id = "default-ui-specification")
    {
        Log.Information  ("Recieved message.");
        var result = new mmria.common.metadata.UI_Specification();

        try
        {
            string ui_specification_url = db_config.url + $"/metadata/" + id;
            
            var ui_specification_curl = new cURL("GET", null, ui_specification_url, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await ui_specification_curl.executeAsync();

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings{
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    MissingMemberHandling =  Newtonsoft.Json.MissingMemberHandling.Ignore
            };
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.UI_Specification> (responseFromServer, settings);

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }


    [Route("{id?}")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] mmria.common.metadata.UI_Specification ui_specification
    ) 
    { 
        string ui_specification_json;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {


            if
            (
                ui_specification.data_type == null ||
                ui_specification.data_type != "ui-specification" || 
                ui_specification._id == "2016-06-12T13:49:24.759Z" ||
                ui_specification._id == "de-identified-list"

            )
            {
                return null;
            }

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings{
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    MissingMemberHandling =  Newtonsoft.Json.MissingMemberHandling.Ignore
            };
            ui_specification_json = Newtonsoft.Json.JsonConvert.SerializeObject(ui_specification, settings);

            string ui_specification_url = db_config.url + "/metadata/" + ui_specification._id;


            cURL document_curl = new cURL ("PUT", null, ui_specification_url, ui_specification_json, db_config.user_name, db_config.user_value);


            try
            {
                string responseFromServer = await document_curl.executeAsync();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
            catch(Exception ex)
            {
                Log.Information ($"jurisdiction_treeController:{ex}");
            }


            if (!result.ok) 
            {

            }

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }
            
        return result;
    } 


    [Route("{_id?}")]
    [HttpDelete]
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Delete(string _id = null, string rev = null) 
    { 
        try
        {
            string request_string = null;

            if (
                    !string.IsNullOrWhiteSpace (_id) &&
                    !string.IsNullOrWhiteSpace (rev)
                    && _id != "default-ui-specification"
            ) 
            {
                request_string = db_config.url + "/metadata/" + _id + "?rev=" + rev;
            }
            else 
            {
                return null;
            }


            if
            (
                _id == "2016-06-12T13:49:24.759Z" ||
                _id == "de-identified-list"
            )
            {
                return null;
            }


            var delete_report_curl = new cURL ("DELETE", null, request_string, null, db_config.user_name, db_config.user_value);


            string responseFromServer = await delete_report_curl.executeAsync ();;
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (responseFromServer);

            return result;

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }


} 

