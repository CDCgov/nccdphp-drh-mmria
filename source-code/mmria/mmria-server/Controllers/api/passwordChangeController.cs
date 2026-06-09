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
using mmria.common.SharedLibraries.Account.Manager;

using mmria.server.extension;

using mmria.common.model;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class passwordChangeController: ControllerBase 
{ 
    mmria.common.SharedLibraries.Session.Manager.SessionManager _sessionManager;
    private readonly AccountManager _accountManager;
    IHttpContextAccessor accessor;
    

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public passwordChangeController
    (
        mmria.common.SharedLibraries.Session.Manager.SessionManager sessionManager,
        IHttpContextAccessor _accessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        AccountManager accountManager
    )
    {
        _sessionManager = sessionManager;
        _accountManager = accountManager;
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

                days_til_expires = await _sessionManager.GetDaysUntilPasswordExpirationAsync(
                    userName,
                    days_before_expires,
                    db_config);
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
            if
            (
                safeRequest == null ||
                !safeRequest.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)
            )
            {
                return null;
            }

            result = await _accountManager.ChangePasswordAsync(
                safeRequest.UserName,
                safeRequest.Value,
                userName,
                db_config);

            if (result?.ok == true) 
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


