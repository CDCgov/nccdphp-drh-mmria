using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 

namespace mmria.server.Controllers;

[Authorize(Roles  = "abstractor,data_analyst")]
[Route("interactive-aggregate-report")]
public sealed class interactive_aggregate_reportController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;


    public interactive_aggregate_reportController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets
    )
    {
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.is_power_bi_user = false;

        var userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;

        try
        {
            string my_user_url = $"{db_config.url}/_users/org.couchdb.user:{userName}";

            var user_curl = new cURL("GET",null,my_user_url,null, db_config.user_name, db_config.user_value);
            string responseFromServer = await user_curl.executeAsync();

            var user  = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.user>(responseFromServer);
            if(!string.IsNullOrEmpty(user.alternate_email))
            {
                ViewBag.is_power_bi_user = true;

                var temp_string = configuration.GetString("power_bi_aggregate", host_prefix);
                
                if(!string.IsNullOrWhiteSpace(temp_string))
                {
                    var app_instance_name = configuration.GetString("mmria_settings:app_instance_name", host_prefix);
                    if(!string.IsNullOrWhiteSpace(app_instance_name))
                    {
                        ViewBag.power_bi_link = temp_string.Replace("{prefix}",app_instance_name);
                    }
                    else
                    {
                        ViewBag.power_bi_link = temp_string.Replace("{prefix}","");
                    }
                }
                else
                {
                    ViewBag.power_bi_link = "";
                }
            }
        }
        catch(Exception ex) 
        {
            System.Console.WriteLine ($"{ex}");
        }

        return View();
    }
}
