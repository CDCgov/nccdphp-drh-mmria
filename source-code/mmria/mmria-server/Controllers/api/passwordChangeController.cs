using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Akka.Actor;
using mmria.common.SharedLibraries.Session.Model;
using mmria.common.SharedLibraries.Session.Manager;

using mmria.server.extension;

using mmria.common.model;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class passwordChangeController: ControllerBase 
{ 
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    mmria.common.SharedLibraries.Session.Manager.SessionManager _sessionManager;
    IHttpContextAccessor accessor;
    

    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public passwordChangeController
    (
        mmria.common.SharedLibraries.Session.Manager.SessionManager sessionManager,
        IHttpContextAccessor _accessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _couchDbHttpClient = couchDbHttpClient;

        _sessionManager = sessionManager;
        accessor = _accessor;
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        host_prefix = accessor.HttpContext.Request.Host.GetPrefix();
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
    }


    [HttpGet]
    public async System.Threading.Tasks.Task<int> Get() 
    { 
        var days_til_expires = -1;

        int days_before_expires = 3;

        configuration.GetInteger("password_days_before_expires", host_prefix).SetIfIsNotNullOrWhiteSpace(ref days_before_expires);

        DateTime grace_period_date = DateTime.Now;


        if(days_before_expires > 0)
        {
            try
            {
                var userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;

                var session_event_request_url = db_config.Get_Prefix_DB_Url($"session/_design/session_event_sortable/_view/by_user_id?startkey=\"{userName}\"&endkey=\"{userName}\"");

                string response_from_server = await _couchDbHttpClient.ExecuteAsync("GET", session_event_request_url, null, db_config.user_name, db_config.user_value);

                //var session_event_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_object_key_header<mmria.common.model.couchdb.session_event>>(response_from_server);
                var session_event_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.session_event>>(response_from_server);

                DateTime first_item_date = DateTime.Now;
                DateTime last_item_date = DateTime.Now;

                session_event_response.rows.Sort(new mmria.common.model.couchdb.Compare_Session_Event_By_DateCreated<mmria.common.model.couchdb.session_event>());

                var date_of_last_change = DateTime.MinValue;
        
                foreach(var session_event in session_event_response.rows)
                {
                    if(session_event.value.action_result == mmria.common.model.couchdb.session_event.session_event_action_enum.password_changed)
                    {
                        date_of_last_change = session_event.value.date_created;
                        break;
                    }
                }

                if(date_of_last_change != DateTime.MinValue)
                {
                    days_til_expires = days_before_expires - (int)(DateTime.Now - date_of_last_change).TotalDays;
                }
                else if(session_event_response.rows.Count > 0)
                {
                    days_til_expires = days_before_expires - (int)(DateTime.Now - session_event_response.rows[session_event_response.rows.Count-1].value.date_created).TotalDays;
                }
                    
                
            }
            catch(Exception ex) 
            {
                System.Console.WriteLine ($"{ex}");
            }
        }

        

        return days_til_expires;
    }


    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post([FromBody] ApplicationUser user) 
    { 
        //bool valid_login = false;

        string object_string = null;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        var userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;

        try
        {
            string user_db_url = db_config.url + "/_users/org.couchdb.user:" + userName;
            var responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", user_db_url, object_string, db_config.user_name, db_config.user_value);
            var user_object = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.user>(responseFromServer);

            if
            (
                user_object == null ||
                !user.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)
            )
            {
                return null;
            }

            user_object.password = user.Value;

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            object_string = Newtonsoft.Json.JsonConvert.SerializeObject(user_object, settings);

            responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", user_db_url, object_string, db_config.user_name, db_config.user_value);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

            if (result.ok) 
            {
                var Session_Event_Message = new mmria.common.SharedLibraries.Session.Model.Session_Event_Message
                (
                    DateTime.Now,
                    userName,
                    accessor.HttpContext.Connection.RemoteIpAddress.ToString(),
                    mmria.common.SharedLibraries.Session.Model.Session_Event_Message.Session_Event_Message_Action_Enum.password_changed
                );

                _sessionManager.RecordSessionEvent(Session_Event_Message, db_config);

            }

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    } 

} 


