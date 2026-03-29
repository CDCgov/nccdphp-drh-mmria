using System;
using System.Collections.Generic;
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
    private readonly System.Net.Http.HttpClient _httpClient;
    
    public healthzController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        System.Net.Http.IHttpClientFactory httpClientFactory
    )
    {
        _httpClient = httpClientFactory.CreateClient(string.Empty);
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
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
        try
        {
            using var request = new System.Net.Http.HttpRequestMessage(
                p_method == "HEAD" ? System.Net.Http.HttpMethod.Head : System.Net.Http.HttpMethod.Get,
                p_target_server
            );

            if (!string.IsNullOrWhiteSpace(p_user_name) && !string.IsNullOrWhiteSpace(p_value))
            {
                string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(p_user_name + ":" + p_value));
                request.Headers.Add("Authorization", "Basic " + encoded);
            }

            using var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception) 
        {
            //Log.Information ($"failed end_point exists check: {p_target_server}\n{ex}");
            //Log.Information ($"failed end_point exists check: {p_target_server}");
            return false;
        }            
    }
}
