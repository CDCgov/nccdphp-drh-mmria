using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using mmria.server.extension;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 

namespace mmria.server.Controllers;


[Authorize(Roles = "installation_admin")]
public sealed class _configController : Controller
{

    IConfiguration configuration;
    mmria.common.couchdb.ConfigurationSet config_set;

    mmria.common.couchdb.OverridableConfiguration overridable_configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    string shared_config_id = null;
    public _configController
    (
        IConfiguration p_configuration, 
        mmria.common.couchdb.ConfigurationSet p_config_db,
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = p_configuration;
        config_set = p_config_db;

        overridable_configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = overridable_configuration.GetDBConfig(host_prefix);

        shared_config_id = configuration["mmria_settings:shared_config_id"];
        if(string.IsNullOrWhiteSpace(shared_config_id))
        {
            shared_config_id = configuration["shared_config_id"];
        }
    }

    public IActionResult Index()
    {
        var app_config = new mmria.server.model.app_config();

        app_config.is_environment_based = bool.Parse(configuration["mmria_settings:is_environment_based"]);
        app_config.web_site_url = configuration["mmria_settings:web_site_url"];
        app_config.log_directory = configuration["mmria_settings:log_directory"];
        app_config.export_directory = configuration["mmria_settings:export_directory"];
        app_config.couchdb_url = db_config.url;
        app_config.timer_user_name = db_config.user_name;
        app_config.timer_value = db_config.user_value;
        app_config.cron_schedule = configuration["mmria_settings:cron_schedule"];
        app_config.pass_word_minimum_length = int.Parse(configuration["password_settings:minimum_length"]);
        app_config.pass_word_days_before_expires = int.Parse(configuration["password_settings:days_before_expires"]);
        app_config.pass_word_days_before_user_is_notified_of_expiration = int.Parse(configuration["password_settings:days_before_user_is_notified_of_expiration"]);
        app_config.EMAIL_USE_AUTHENTICATION = bool.Parse(configuration["smtp:use_authentication"]);
        app_config.EMAIL_USE_SSL = bool.Parse(configuration["smtp:use_ssl"]);
        app_config.SMTP_HOST = configuration["smtp:host"];
        app_config.SMTP_PORT = int.Parse(configuration["smtp:port"]);
        app_config.EMAIL_FROM = configuration["smtp:email_from"];
        app_config.EMAIL_PASSWORD = configuration["smtp:email_password"];
        app_config.shared_config_id = configuration["mmria_settings:shared_config_id"];
        app_config.host_site = HttpContext.Request.Host.GetPrefix();
        return View(app_config);
    }



    [HttpGet]
    public async Task<IActionResult> GetConfiguration()
    {
        var app_config = new mmria.common.couchdb.Configuration();

        try
        {

            
            string request_string = $"{db_config.url}/configuration/{shared_config_id}";


            System.Console.WriteLine($"GetConfiguration: request_string {request_string}");

            var case_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_curl.executeAsync();

            app_config = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.couchdb.Configuration> (responseFromServer);

        }
        catch(System.Exception ex)
        {
            System.Console.WriteLine (ex);
        } 
        return Json(app_config);
    }


    [HttpGet]
    public async Task<IActionResult> GetConfigurationMaster()
    {
        var app_config = new mmria.common.couchdb.OverridableConfiguration();

        try
        {
            string request_string = $"{db_config.url}/configuration/{shared_config_id}";

            System.Console.WriteLine($"GetConfigurationMaster: request_string {request_string}");

            var case_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_curl.executeAsync();

            app_config = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.couchdb.OverridableConfiguration> (responseFromServer);
        }
        catch(System.Exception ex)
        {
            System.Console.WriteLine (ex);
        } 

        return Json(app_config);
    }


    [HttpPost]
    public async Task<IActionResult> SetConfigurationMaster
    (
        [FromBody] mmria.common.couchdb.OverridableConfiguration app_config
    )
    {
        mmria.common.model.couchdb.document_put_response result = new();
        try
        {
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(app_config, settings);

            string request_string = $"{db_config.url}/configuration/{shared_config_id}";
            
            var case_curl = new cURL("PUT", null, request_string, object_string, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_curl.executeAsync();

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response> (responseFromServer);
        }
        catch(System.Exception ex)
        {
            System.Console.WriteLine (ex);
        } 

        return Json(result);
    }


    [HttpGet]
    public async Task<IActionResult> GetAppliedConfiguration()
    {
        var result = new mmria.common.couchdb.OverridableConfiguration();

        try
        {
            result._id = configuration["mmria_settings:shared_config_id"];

            result.boolean_keys.Add("shared", new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase));
            result.string_keys.Add("shared", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            result.integer_keys.Add("shared", new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
        
            result.string_keys["shared"].Add("geocode_api_url", configuration["mmria_settings:geocode_api_url"]);
            result.string_keys["shared"].Add("couchdb_url", db_config.url);
            result.string_keys["shared"].Add("db_prefix", configuration["mmria_settings:db_prefix"]);
            result.string_keys["shared"].Add("web_site_url", configuration["mmria_settings:web_site_url"]);
            result.string_keys["shared"].Add("timer_user_name", db_config.user_name);
            result.string_keys["shared"].Add("timer_value", db_config.user_value);
            result.string_keys["shared"].Add("cron_schedule", configuration["mmria_settings:cron_schedule"]);

            result.string_keys["shared"].Add("log_directory", configuration["mmria_settings:log_directory"]);
            result.string_keys["shared"].Add("export_directory", configuration["mmria_settings:export_directory"]);
            result.string_keys["shared"].Add("metadata_version", configuration["mmria_settings:metadata_version"]);
            result.string_keys["shared"].Add("vitals_url", configuration["mmria_settings:vitals_url"]);
            result.string_keys["shared"].Add("app_instance_name", configuration["mmria_settings:app_instance_name"]);
            
            foreach(var kvp in config_set.name_value)
            {
                result.string_keys["shared"].Add(kvp.Key, kvp.Value);
            }

            result.string_keys["shared"].Add("sams:direct_login_url", configuration["sams:direct_login_url"]);
            result.string_keys["shared"].Add("sams:endpoint_authorization",configuration["sams:endpoint_authorization"]);
            result.string_keys["shared"].Add("sams:endpoint_token",configuration["sams:endpoint_token"]);
            result.string_keys["shared"].Add("sams:endpoint_user_info",configuration["sams:endpoint_user_info"]);
            result.string_keys["shared"].Add("sams:endpoint_token_validation",configuration["sams:endpoint_token_validation"]);
            result.string_keys["shared"].Add("sams:endpoint_user_info_sys",configuration["sams:endpoint_user_info_sys"]);
            result.string_keys["shared"].Add("sams:client_id",configuration["sams:client_id"]);
            result.string_keys["shared"].Add("sams:client_secret",configuration["sams:client_secret"]);
            result.string_keys["shared"].Add("sams:callback_url",configuration["sams:callback_url"]);
            result.string_keys["shared"].Add("sams:logout_url", configuration["sams:logout_url"]);
            result.string_keys["shared"].Add("sams:activity_name", configuration["sams:activity_name"]);

            bool is_schedule_enabled = true;
            configuration["mmria_settings:is_schedule_enabled"].SetIfIsNotNullOrWhiteSpace(ref is_schedule_enabled);
            bool is_db_check_enabled = false;
            configuration["mmria_settings:is_db_check_enabled"].SetIfIsNotNullOrWhiteSpace(ref is_db_check_enabled);
            bool is_environment_based = true;
            configuration["mmria_settings:is_environment_based"].SetIfIsNotNullOrWhiteSpace(ref is_environment_based);
            bool is_development = false;
            configuration["mmria_settings:is_development"].SetIfIsNotNullOrWhiteSpace(ref is_development);
            bool use_development_settings = false;
            configuration["mmria_settings:use_development_settings"].SetIfIsNotNullOrWhiteSpace(ref use_development_settings);
            bool sams_is_enabled = false;
            configuration["mmria_settings:sams:is_enabled"].SetIfIsNotNullOrWhiteSpace(ref sams_is_enabled);

            result.boolean_keys["shared"].Add("is_schedule_enabled ", is_schedule_enabled);
            result.boolean_keys["shared"].Add("is_db_check_enabled", is_db_check_enabled);
            result.boolean_keys["shared"].Add("is_environment_based", is_environment_based);
            result.boolean_keys["shared"].Add("is_development", is_development);
            result.boolean_keys["shared"].Add("use_development_settings", use_development_settings);
            result.boolean_keys["shared"].Add("sams:is_enabled", sams_is_enabled);


            int session_idle_timeout_minutes = 70;
            configuration["mmria_settings:session_idle_timeout_minutes"].SetIfIsNotNullOrWhiteSpace(ref session_idle_timeout_minutes);
            int pass_word_minimum_length = 8;
            configuration["password_settings:minimum_length"].SetIfIsNotNullOrWhiteSpace(ref pass_word_minimum_length);
            int pass_word_days_before_expires = 0;
            configuration["password_settings:days_before_expires"].SetIfIsNotNullOrWhiteSpace(ref pass_word_days_before_expires);
            int pass_word_days_before_user_is_notified_of_expiration = 0;
            configuration["password_settings:days_before_user_is_notified_of_expiration"].SetIfIsNotNullOrWhiteSpace(ref pass_word_days_before_user_is_notified_of_expiration);
            int default_days_in_effective_date_interval = 120;
            configuration["authentication_settings:default_days_in_effective_date_interval"].SetIfIsNotNullOrWhiteSpace(ref default_days_in_effective_date_interval);
            int unsuccessful_login_attempts_number_before_lockout = 5;
            configuration["authentication_settings:unsuccessful_login_attempts_number_before_lockout"].SetIfIsNotNullOrWhiteSpace(ref unsuccessful_login_attempts_number_before_lockout);
            int unsuccessful_login_attempts_within_number_of_minutes = 5;
            configuration["authentication_settings:unsuccessful_login_attempts_within_number_of_minutes"].SetIfIsNotNullOrWhiteSpace(ref unsuccessful_login_attempts_within_number_of_minutes);
            int unsuccessful_login_attempts_lockout_number_of_minutes = 120;
            configuration["authentication_settings:unsuccessful_login_attempts_lockout_number_of_minutes"].SetIfIsNotNullOrWhiteSpace(ref unsuccessful_login_attempts_lockout_number_of_minutes);



            result.integer_keys["shared"].Add("session_idle_timeout_minutes", session_idle_timeout_minutes);
            result.integer_keys["shared"].Add("pass_word_minimum_length", pass_word_minimum_length);
            result.integer_keys["shared"].Add("pass_word_days_before_expires", pass_word_days_before_expires);
            result.integer_keys["shared"].Add("pass_word_days_before_user_is_notified_of_expiration", pass_word_days_before_user_is_notified_of_expiration);
            result.integer_keys["shared"].Add("default_days_in_effective_date_interval", default_days_in_effective_date_interval);
            result.integer_keys["shared"].Add("unsuccessful_login_attempts_number_before_lockout", unsuccessful_login_attempts_number_before_lockout);
            result.integer_keys["shared"].Add("unsuccessful_login_attempts_within_number_of_minutes", unsuccessful_login_attempts_within_number_of_minutes);
            result.integer_keys["shared"].Add("unsuccessful_login_attempts_lockout_number_of_minutes", unsuccessful_login_attempts_lockout_number_of_minutes);

        }
        catch(System.Exception ex)
        {
            System.Console.WriteLine (ex);
        } 
        return Json(result);
    }

}
