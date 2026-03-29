using System;
using System.Collections.Generic;
using Microsoft.CSharp;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Linq;
using mmria.common.model.couchdb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;

using  mmria.server.extension;    

namespace mmria.server;

//[Authorize(Roles  = "jurisdiction_admin")]
//[AllowAnonymous] 
[Route("api/[controller]")]
public sealed class sessionDBController: ControllerBase 
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.Session.Manager.SessionManager _sessionManager;
    public sessionDBController 
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.SharedLibraries.Session.Manager.SessionManager sessionManager
    )
    {
        _sessionManager = sessionManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }


    [HttpGet]
    public async System.Threading.Tasks.Task<IEnumerable<session_response>> Get() 
    { 
        try
        {
            string auth_session_value = this.Request.Cookies["AuthSession"];
            return await _sessionManager.GetCouchDbSessionAsync(auth_session_value, db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    }


    //[Authorize(Roles  = "abstractor")]
    [HttpPut]
    [HttpPost]
    public async System.Threading.Tasks.Task<IEnumerable<login_response>> Post
    (
        [FromBody] Post_Request_Struct post_request_struct 
    ) 
    {
        

        /*
        post_request_struct.userid = null;
        //post_request_struct.password = null;

        try 
        {

            System.IO.Stream dataStream0 = await this.Request.Content.ReadAsStreamAsync ();
            // Open the stream using a StreamReader for easy access.
            //dataStream0.Seek(0, System.IO.SeekOrigin.Begin);
            System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);
            // Read the content.
            string temp = reader0.ReadToEnd ();
            //System.Console.Write ($"temp {temp}");
            post_request_struct = Newtonsoft.Json.JsonConvert.DeserializeObject<Post_Request_Struct> (temp);

            //mmria.server.utilsLuceneSearchIndexer.RunIndex(new List<mmria.common.model.home_record> { mmria.common.model.home_record.convert(queue_request)});
            //System.Dynamic.ExpandoObject json_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(result, new  Newtonsoft.Json.Converters.ExpandoObjectConverter());



            //string metadata = DecodeUrlString(temp);
        } catch (Exception ex) {
            Console.WriteLine (ex);
        }
*/

        /*
HOST="http://127.0.0.1:5984"
> curl -vX POST $HOST/_session -H 'Content-Type: application/x-www-form-urlencoded' -d 'name=anna&password=secret'
        */
        try
        {
            return await _sessionManager.LoginToCouchDbSessionAsync(db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    }
}



