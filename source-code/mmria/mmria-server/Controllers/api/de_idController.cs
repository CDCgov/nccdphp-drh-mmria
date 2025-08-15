using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server;

[Authorize(Roles  = "committee_member")]
[Route("api/[controller]")]
public sealed class de_idController: ControllerBase 
{     
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public de_idController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    public async Task<System.Dynamic.ExpandoObject> Get(string case_id = null) 
    { 
        try
        {
            string request_string = db_config.Get_Prefix_DB_Url($"de_id/_all_docs?include_docs=true");

            if (!string.IsNullOrWhiteSpace (case_id)) 
            {
                request_string = db_config.Get_Prefix_DB_Url($"de_id/{case_id}");
            } 

            var request_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await request_curl.executeAsync();

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


