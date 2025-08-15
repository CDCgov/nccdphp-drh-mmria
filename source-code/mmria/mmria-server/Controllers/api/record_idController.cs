using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class record_idController: ControllerBase
{ 
    public record Record_Id_Response
    {
        public bool ok { get; init;}
        public bool is_unique { get; init;}
    }
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public record_idController
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
    public async Task<Record_Id_Response> Get(string record_id)
    {
        var result = new Record_Id_Response(){ ok = true, is_unique = false };
        try
        {        
            //"2016-06-12T13:49:24.759Z"
            //string request_string = configuration["mmria_settings:couchdb_url + $"/metadata/version_specification-{Configuration["mmria_settings:metadata_version"]}/validator";
            string request_string = db_config.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_date_created?skip=0&take=25000");

            var case_view_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_view_curl.executeAsync();

            mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

            var temp = new List<mmria.common.model.couchdb.case_view_item>();

            var is_found = false;
            foreach(mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
            {
                if(!string.IsNullOrWhiteSpace(cvi.value.record_id))
                {
                    if(cvi.value.record_id.Trim().Equals(record_id.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        is_found = true;
                        break;
                    }
                }
            }

            result = new Record_Id_Response(){ ok = true, is_unique = !is_found };

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    } 
    
} 


