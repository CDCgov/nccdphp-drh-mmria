using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Akka.Actor;

using  mmria.server.extension;
using System.Collections.Generic;

namespace mmria.server.Controllers;

public sealed class HomeController : Controller
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.Session.Manager.SessionManager _sessionManager;
    private readonly mmria.common.SharedLibraries.ManageUsers.Manager.ManageUsersManager _manageUsersManager;
    
    public HomeController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.SharedLibraries.Session.Manager.SessionManager sessionManager,
        mmria.common.SharedLibraries.ManageUsers.Manager.ManageUsersManager manageUsersManager
    )
    {
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        _sessionManager = sessionManager;
        _manageUsersManager = manageUsersManager;
        
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(
            _overridableConfigSets,
            _configuration,
            host_prefix
        );
        
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(
            _dbConfigSets,
            _configuration,
            host_prefix
        );
    }

    public async Task<IActionResult> Index()
    {


        var userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;


        var days_til_expiration = -1;

        var password_days_before_expires = configuration.GetInteger("pass_word_days_before_expires", host_prefix);

        if (password_days_before_expires.HasValue && password_days_before_expires.Value > 0)
        {
            try
            {
                days_til_expiration = await _sessionManager.GetDaysUntilPasswordExpirationAsync(userName, password_days_before_expires, db_config);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"{ex}");
            }
        }




        try
        {
            ViewBag.is_power_bi_user = false;
            var user = await _manageUsersManager.GetMyUserAsync(User, db_config);
            if
            (
                user != null &&
                !string.IsNullOrEmpty(user.alternate_email)
            )
            {
                ViewBag.is_power_bi_user = true;
            }
        }
        catch(Exception ex) 
        {
            System.Console.WriteLine ($"{ex}");
        }


        ViewBag.sams_is_enabled = configuration.GetBoolean("sams:is_enabled", host_prefix).Value;
        ViewBag.days_til_password_expires = days_til_expiration;
        ViewBag.config_password_days_before_expires = password_days_before_expires;
        ViewBag.is_offline_mode_enabled = configuration.GetBoolean("is_offline_mode_enabled", host_prefix) ?? false;
        ViewBag.is_offline_logging_enabled = configuration.GetBoolean("is_offline_logging_enabled", host_prefix) ?? false;
        ViewBag.offline_logging_max_logs = configuration.GetInteger("offline_logging_max_logs", host_prefix) ?? 10000;
        var LinkList = configuration.GetExternalHomePageLinks();

        

        return View(LinkList);
    }

}
