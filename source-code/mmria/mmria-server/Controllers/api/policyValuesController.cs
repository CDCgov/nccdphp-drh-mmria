using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 

namespace mmria.server;

//[AllowAnonymous] 
[Route("api/[controller]")]
public sealed class policyValuesController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public policyValuesController
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
    public IEnumerable<KeyValuePair<string,string>> Get()
    {
        var result = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);

        var minimum_length = configuration.GetInteger("pass_word_minimum_length", host_prefix);
        var days_before_expires = configuration.GetInteger("pass_word_days_before_expires", host_prefix);
        var days_before_user_is_notified_of_expiration = configuration.GetInteger("pass_word_days_before_user_is_notified_of_expiration", host_prefix);
        var default_days_in_effective_date_interval = configuration.GetInteger("default_days_in_effective_date_interval", host_prefix);
        var unsuccessful_login_attempts_number_before_lockout = configuration.GetInteger("unsuccessful_login_attempts_number_before_lockout", host_prefix);
        var unsuccessful_login_attempts_within_number_of_minutes = configuration.GetInteger("unsuccessful_login_attempts_within_number_of_minutes", host_prefix);
        var unsuccessful_login_attempts_lockout_number_of_minutes = configuration.GetInteger("unsuccessful_login_attempts_lockout_number_of_minutes", host_prefix);
        var sams_is_enabled = configuration.GetBoolean("sams:is_enabled", host_prefix);
/*
        System.Console.WriteLine($"Get Policy Values");
        System.Console.WriteLine($"host_prefix: {host_prefix}");
        System.Console.WriteLine($"minimum_length: {(minimum_length.HasValue ? minimum_length.Value.ToString() : "") }");
        System.Console.WriteLine($"days_before_expires: {(days_before_expires.HasValue ? days_before_expires.Value.ToString(): "") }");
        System.Console.WriteLine($"days_before_user_is_notified_of_expiration: {(days_before_user_is_notified_of_expiration.HasValue ? days_before_user_is_notified_of_expiration.Value.ToString(): "") }");
        System.Console.WriteLine($"default_days_in_effective_date_interval: {(default_days_in_effective_date_interval.HasValue ? default_days_in_effective_date_interval.Value.ToString(): "") }");
        System.Console.WriteLine($"unsuccessful_login_attempts_number_before_lockout: {(unsuccessful_login_attempts_number_before_lockout.HasValue ? unsuccessful_login_attempts_number_before_lockout.Value.ToString(): "") }");
        System.Console.WriteLine($"unsuccessful_login_attempts_within_number_of_minutes: {(unsuccessful_login_attempts_within_number_of_minutes.HasValue ? unsuccessful_login_attempts_within_number_of_minutes.Value.ToString(): "") }");
        System.Console.WriteLine($"unsuccessful_login_attempts_lockout_number_of_minutes: {(unsuccessful_login_attempts_lockout_number_of_minutes.HasValue ? unsuccessful_login_attempts_lockout_number_of_minutes.Value.ToString(): "") }");
        System.Console.WriteLine($"sams_is_enabled: {(sams_is_enabled.HasValue ? sams_is_enabled.Value: "") }");
    */

        result.Add("minimum_length", minimum_length.HasValue ? minimum_length.Value.ToString() : "");
        result.Add("days_before_expires", days_before_expires.HasValue ? days_before_expires.Value.ToString(): "");
        result.Add("days_before_user_is_notified_of_expiration", days_before_user_is_notified_of_expiration.HasValue ? days_before_user_is_notified_of_expiration.Value.ToString(): "");
        result.Add("default_days_in_effective_date_interval", default_days_in_effective_date_interval.HasValue ? default_days_in_effective_date_interval.Value.ToString(): "");
        result.Add("unsuccessful_login_attempts_number_before_lockout", unsuccessful_login_attempts_number_before_lockout.HasValue ? unsuccessful_login_attempts_number_before_lockout.Value.ToString(): "");
        result.Add("unsuccessful_login_attempts_within_number_of_minutes", unsuccessful_login_attempts_within_number_of_minutes.HasValue ? unsuccessful_login_attempts_within_number_of_minutes.Value.ToString(): "");
        result.Add("unsuccessful_login_attempts_lockout_number_of_minutes", unsuccessful_login_attempts_lockout_number_of_minutes.HasValue ? unsuccessful_login_attempts_lockout_number_of_minutes.Value.ToString(): "");
        result.Add("sams_is_enabled", sams_is_enabled.HasValue ? sams_is_enabled.Value.ToString(): "");

        return result;
    }



}

