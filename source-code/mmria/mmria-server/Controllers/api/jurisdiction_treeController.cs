using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Serilog;
using Serilog.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;
namespace mmria.server;

[Route("api/[controller]")]
public sealed class jurisdiction_treeController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public class case_folder_metadata
    {
      public string Name { get; set; }
      public string ParentName { get; set; }
      public int NestedLevel { get; set; }
    }
    public jurisdiction_treeController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.jurisdiction_tree> Get()
    {
        Log.Information  ("Recieved message.");
        mmria.common.model.couchdb.jurisdiction_tree result = null;

        try
        {
            string jurisdiction_tree_url = db_config.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree");

            var jurisdiction_curl = new cURL("GET", null, jurisdiction_tree_url, null, db_config.user_name, db_config.user_value);
            string response_from_server = await jurisdiction_curl.executeAsync ();

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.jurisdiction_tree>(response_from_server);

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }

    [Route("new_case_folder")]
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.jurisdiction_tree> GetJurisdictionTree()
    {
        Log.Information  ("Recieved message.");
        mmria.common.model.couchdb.jurisdiction_tree result = null;

        try
        {
            string jurisdiction_tree_url = db_config.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree");

            var jurisdiction_curl = new cURL("GET", null, jurisdiction_tree_url, null, db_config.user_name, db_config.user_value);
            string response_from_server = await jurisdiction_curl.executeAsync ();

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.jurisdiction_tree>(response_from_server);

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }

    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] mmria.common.model.couchdb.jurisdiction_tree jurisdiction_tree
    ) 
    { 
        string jurisdiction_json;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {

            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;
            }

            if(string.IsNullOrWhiteSpace(jurisdiction_tree.created_by))
            {
                jurisdiction_tree.created_by = userName;
            } 
            jurisdiction_tree.last_updated_by = userName;

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            jurisdiction_json = Newtonsoft.Json.JsonConvert.SerializeObject(jurisdiction_tree, settings);

            string jurisdiction_tree_url = db_config.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree");

            cURL document_curl = new cURL ("PUT", null, jurisdiction_tree_url, jurisdiction_json, db_config.user_name, db_config.user_value);

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

} 


