using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;  

namespace mmria.server.Controllers;
    
[Route("api/[controller]")]
[AllowAnonymous] 
public sealed class healthzController : Controller
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    
    public healthzController
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
    public async Task<IActionResult> Index()
    {
        if (!await url_endpoint_exists (db_config.Get_Prefix_DB_Url($"mmrds"), db_config.user_name, db_config.user_value)) 
        {
            return StatusCode(500); 
        }
        else
        {
            return Ok(); 
        }
    }

    async Task<bool> url_endpoint_exists (string p_target_server, string p_user_name, string p_value, string p_method = "HEAD")
    {
        System.Net.HttpStatusCode response_result;

        try
        {
            System.Net.HttpWebRequest request = System.Net.WebRequest.Create(p_target_server) as System.Net.HttpWebRequest;
     
            if(request != null)
            {
                request.Method = p_method;

                if (!string.IsNullOrWhiteSpace(p_user_name) && !string.IsNullOrWhiteSpace(p_value))
                {
                    string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(p_user_name + ":" + p_value));
                    request.Headers.Add("Authorization", "Basic " + encoded);
                }

                System.Net.HttpWebResponse response = await request.GetResponseAsync() as System.Net.HttpWebResponse;

                if(response != null)
                {
                    response_result = response.StatusCode;
                    response.Close();
                    return (response_result == System.Net.HttpStatusCode.OK);
                }
                else
                {
                    return false;
                }
            }
            return  false;
        }
        catch (Exception) 
        {
            //Log.Information ($"failed end_point exists check: {p_target_server}\n{ex}");
            //Log.Information ($"failed end_point exists check: {p_target_server}");
            return false;
        }            
    }
}
