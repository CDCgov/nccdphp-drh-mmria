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
    private readonly mmria.common.SharedLibraries.Account.IUserRepository _userRepository;
    private readonly mmria.common.SharedLibraries.Session.ISessionRepository _sessionRepository;
    mmria.common.SharedLibraries.Session.Manager.SessionManager _sessionManager;
    IHttpContextAccessor accessor;
    

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public passwordChangeController
    (
        mmria.common.SharedLibraries.Session.Manager.SessionManager sessionManager,
        IHttpContextAccessor _accessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.Account.IUserRepository userRepository,
        mmria.common.SharedLibraries.Session.ISessionRepository sessionRepository
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;

        _sessionManager = sessionManager;
        accessor = _accessor;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
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

                var session_event_response = await _sessionRepository.GetSessionEventsByUserIdAsync(userName, db_config);

                //var session_event_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_object_key_header<mmria.common.model.couchdb.session_event>>(response_from_server);

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
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post()
    {
        //bool valid_login = false;
        var user = await mmria.server.util.JsonRequestBodyReader.ReadAsync<ApplicationUser>(Request);

        var safeRequest = CreateSanitizedPasswordChangeRequest(user);
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        var userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;

        try
        {
            var user_object = await _userRepository.CheckUserAsync("org.couchdb.user:" + userName, db_config);

            if
            (
                string.IsNullOrWhiteSpace(user_object._id) ||
                safeRequest == null ||
                !safeRequest.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)
            )
            {
                return null;
            }

            user_object.password = safeRequest.Value;

            result = await _userRepository.PutUserAsync(user_object, db_config);

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

    private static ApplicationUser CreateSanitizedPasswordChangeRequest(ApplicationUser request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserName))
        {
            return null;
        }

        return new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Value = request.Value
        };
    }

} 


