using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Akka.Actor;
using  mmria.server.extension;
using mmria.common.SharedLibraries.Account.Manager;
using mmria.common.SharedLibraries.Account.Model;
//https://github.com/blowdart/AspNetAuthorizationWorkshop
//https://digitalmccullough.com/posts/aspnetcore-auth-system-demystified.html
//https://gitlab.com/free-time-programmer/tutorials/demystify-aspnetcore-auth/tree/master
//https://docs.microsoft.com/en-us/aspnet/core/mvc/views/layout?view=aspnetcore-2.1

namespace mmria.server.Controllers;



public sealed partial class AccountController : Controller
{

    IHttpContextAccessor _accessor;
    ActorSystem _actorSystem;

    mmria.common.couchdb.OverridableConfiguration _configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly AccountManager _accountManager;

    string host_prefix = null;
    bool? use_sams = null;

public AccountController
(
    IHttpContextAccessor httpContextAccessor, 
    ActorSystem actorSystem, 
    mmria.common.couchdb.OverridableConfiguration configuration,
    List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
    List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
    mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
    AccountManager accountManager
)
{
    _accessor = httpContextAccessor;
    _actorSystem = actorSystem;
    _configuration = configuration;
    _overridableConfigSets = overridableConfigSets;
    _dbConfigSets = dbConfigSets;
    _couchDbHttpClient = couchDbHttpClient;
    _accountManager = accountManager;
    
    host_prefix = _accessor.HttpContext.Request.Host.GetPrefix();
    Console.WriteLine(host_prefix);
    
    // Use the helper method
    _configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(
        _overridableConfigSets,
        configuration,
        host_prefix
    );
    
    db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(
        _dbConfigSets,
        configuration,
        host_prefix
    );

    //db_config = _configuration.GetDBConfig(host_prefix);
    use_sams = _configuration.GetBoolean("sams:is_enabled", host_prefix);
}
/*
    public List<ApplicationUser> Users => new List<ApplicationUser>() 
    {
        new ApplicationUser { UserName = "user1", Value = "password" },
        new ApplicationUser{ UserName = "user2", Value = "password" }
    };
*/

    [AllowAnonymous] 
    public IActionResult Locked(string user_name, DateTime grace_period_date)
    {
        ViewBag.user_name = user_name;
        ViewBag.grace_period_date = grace_period_date;
        ViewBag.unsuccessful_login_attempts_lockout_number_of_minutes = _configuration.GetInteger("unsuccessful_login_attempts_lockout_number_of_minutes", host_prefix);

        return View();
    }

    [AllowAnonymous]
    public IActionResult AutoLogin(string returnUrl = null)
    {
        // Smart login endpoint that detects SAMS configuration
        // Used by offline mode transitions to ensure correct authentication flow
        if (use_sams.HasValue && use_sams.Value)
        {
            return RedirectToAction("SignIn", new { returnUrl });
        }
        
        return RedirectToAction("Login", new { returnUrl });
    }

    [AllowAnonymous] 
    public IActionResult Login(string returnUrl = null)
    {
        TempData["returnUrl"] = returnUrl;
        ViewBag.is_offline_mode_enabled = _configuration.GetBoolean("is_offline_mode_enabled", host_prefix) ?? false;
        ViewBag.is_offline_logging_enabled = _configuration.GetBoolean("is_offline_logging_enabled", host_prefix) ?? false;
        ViewBag.offline_logging_max_logs = _configuration.GetInteger("offline_logging_max_logs", host_prefix) ?? 10000;

        return View();
    }


    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(ApplicationUser user, string returnUrl = null)
    {
        const string badUserNameOrValueMessage = "Username or password is incorrect.";
        
        // Check for SAMS configuration
        if (use_sams.HasValue && use_sams.Value)
        {
            return RedirectToAction("SignIn");
        }

        // Validate basic input
        if (user == null || string.IsNullOrWhiteSpace(user.UserName) || string.IsNullOrWhiteSpace(user.Value))
        {
            ViewBag.LoginError = badUserNameOrValueMessage;
            return View();
        }

        // Track prior user for offline mode detection
        string priorUserName = "";
        string priorRole = "";
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            priorUserName = User.Identities.First(
                u => u.IsAuthenticated &&
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                .FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            priorRole = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Role))
                .FindFirst(System.Security.Claims.ClaimTypes.Role).Value;
        }

        try
        {
            // Create login request
            var loginRequest = new LoginRequest { UserName = user.UserName, Password = user.Value };

            // Get session idle timeout from configuration
            var session_idle_timeout_minutes = 30;
            _configuration.GetInteger("session_idle_timeout_minutes", host_prefix)
                .SetIfIsNotNullOrWhiteSpace(ref session_idle_timeout_minutes);

            // Delegate ALL business logic to AccountManager
            var loginResult = await _accountManager.ProcessLoginAsync(
                loginRequest,
                db_config,
                _configuration,
                host_prefix,
                session_idle_timeout_minutes);

            // Handle lockout response
            if (loginResult.IsLockedOut)
            {
                return RedirectToAction("Locked", new 
                { 
                    user_name = user.UserName, 
                    grace_period_date = loginResult.LockoutGracePeriodDate 
                });
            }

            // Handle authentication failure
            if (!loginResult.IsSuccessful || loginResult.IsUnauthorized)
            {
                ViewBag.LoginError = badUserNameOrValueMessage;
                return View();
            }

            // Success - set up authentication context using data from manager
            var sessionInfo = loginResult.SessionInfo;
            
            // Build claims from manager-provided roles
            const string Issuer = "https://contoso.com";
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, user.UserName, ClaimValueTypes.String, Issuer));

            foreach (var role in sessionInfo.Roles ?? new List<string>())
            {
                claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
            }

            // Set current user context with claims
            var userIdentity = new ClaimsIdentity("SuperSecureLogin");
            userIdentity.AddClaims(claims);
            var userPrincipal = new ClaimsPrincipal(userIdentity);
            this.HttpContext.User = userPrincipal;
            System.Threading.Thread.CurrentPrincipal = userPrincipal;

            // Log successful login event via Akka actor
            var Session_Event_Message = new mmria.server.model.actor.Session_Event_Message
            (
                DateTime.Now,
                user.UserName,
                this.GetRequestIP(),
                mmria.server.model.actor.Session_Event_Message.Session_Event_Message_Action_Enum.successful_login
            );
            _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Record_Session_Event>(db_config, _couchDbHttpClient))
                .Tell(Session_Event_Message);

            // Set session/authentication cookie
            var session_expiration_datetime = sessionInfo.ExpirationDateTime;
            Response.Cookies.Append("sid", sessionInfo.SessionId, new CookieOptions 
            { 
                HttpOnly = true, 
                Expires = session_expiration_datetime, 
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Secure = Request.IsHttps
            });

            // Post session via Akka actor (notification pattern)
            var session_data = new System.Collections.Generic.Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            var Session_Message = new mmria.server.model.actor.Session_Message
            (
                sessionInfo.SessionId,
                null,
                DateTime.Now,
                DateTime.Now,
                session_expiration_datetime,
                true,
                user.UserName,
                this.GetRequestIP(),
                sessionInfo.SessionEventId,
                sessionInfo.Roles,
                session_data
            );
            _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Post_Session>(db_config, _couchDbHttpClient))
                .Tell(Session_Message);

            // Handle offline mode redirect detection
            if ((_configuration.GetBoolean("is_offline_mode_enabled", host_prefix) ?? false) == true)
            {
                if (priorUserName == user.UserName && priorRole == "offline_mode")
                {
                    // Force a full logout to clear offline_mode role if user is switching from offline to online login
                    return Redirect("/case");
                }

                // Check for active offline sessions and redirect if found         
                try
                {
                    var offlineCaseManager = (mmria.server.SharedLibraries.Manager.IOfflineCaseManager)
                        HttpContext.RequestServices.GetService(typeof(mmria.server.SharedLibraries.Manager.IOfflineCaseManager));
                    if (offlineCaseManager != null)
                    {
                        var shouldRedirect = await offlineCaseManager.ShouldRedirectToCaseSummaryAsync(user.UserName, db_config);
                        if (shouldRedirect)
                        {
                            Console.WriteLine($"User {user.UserName} has active offline session, redirecting to /Case#/summary");
                            return Redirect("/Case#/summary");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking offline session for user {user.UserName}: {ex}");
                    // Continue with normal login flow if check fails
                }
            }

            // Determine return URL and redirect
            if (returnUrl == null)
            {
                returnUrl = TempData["returnUrl"]?.ToString();
            }

            if (returnUrl != null)
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error for user {user?.UserName}: {ex}");

            // Log failed login event via Akka actor
            var Session_Event_Message = new mmria.server.model.actor.Session_Event_Message
            (
                DateTime.Now,
                user?.UserName ?? "unknown",
                this.GetRequestIP(),
                mmria.server.model.actor.Session_Event_Message.Session_Event_Message_Action_Enum.failed_login
            );
            _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Record_Session_Event>(db_config))
                .Tell(Session_Event_Message);

            ViewBag.LoginError = badUserNameOrValueMessage;
            return View();
        }
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Logout() 
    {
            //var db_config = _configuration.GetDBConfig(host_prefix);

            var config_couchdb_url = db_config.url;
            var config_timer_user_name = db_config.user_name;
            var config_timer_password = db_config.user_value;
            var config_db_prefix = db_config.prefix;

            mmria.server.model.actor.Session_MessageDTO session_message = null;
            try
            {
                string request_string = $"{config_couchdb_url}/{config_db_prefix}session/{Request.Cookies["sid"]}";
                System.Console.WriteLine($"Connection Refused on method: Get url: {request_string}");
            
                
                var responseFromServer = await _couchDbHttpClient.ExecuteAsync(
                    "GET",
                    request_string,
                    null,
                    config_timer_user_name,
                    config_timer_password
                );

                session_message = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.server.model.actor.Session_MessageDTO>(responseFromServer);

            }
            catch(System.Exception ex)
            {
                System.Console.WriteLine (ex);

            } 

            session_message.date_expired = DateTime.Now;

            var Session_Message = new mmria.server.model.actor.Session_Message
            (
                session_message._id,
                session_message._rev, //_rev = 
                session_message.date_created, //date_created = 
                session_message.date_last_updated, //date_last_updated = 
                session_message.date_expired, //date_expired = 

                session_message.is_active, //is_active = 
                session_message.user_id, //user_id = 
                session_message.ip, //ip = 
                session_message.session_event_id, // session_event_id = 
                session_message.role_list,
                session_message.data
            );


            Response.Cookies.Append("sid", "", new CookieOptions{ HttpOnly = true, Expires = DateTime.Now });
            Response.Cookies.Append("expires_at", "", new CookieOptions{ HttpOnly = true, Expires = DateTime.Now });

            System.Threading.Thread.CurrentPrincipal = null;

            _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Post_Session>(db_config, _couchDbHttpClient)).Tell(Session_Message);

        if
        (
            use_sams.HasValue  &&
            use_sams.Value 
        )
        {

            return Redirect(_configuration.GetSharedString("sams:logout_url"));
        }
        else
        {

/*
            await HttpContext.SignOutAsync
            (
                CookieAuthenticationDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(-5),
                    IsPersistent = false,
                    AllowRefresh = true,
                }
            );*/


            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
        
        //Response.Cookies.Delete("uid");
        //Response.Cookies.Delete("roles");
        
    }

    [AllowAnonymous] 
    public IActionResult OfflineLogin(string returnUrl = null)
    {
        TempData["returnUrl"] = returnUrl;
        ViewBag.is_offline_logging_enabled = _configuration.GetBoolean("is_offline_logging_enabled", host_prefix) ?? false;
        ViewBag.offline_logging_max_logs = _configuration.GetInteger("offline_logging_max_logs", host_prefix) ?? 10000;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public IActionResult OfflineLogin(OfflineApplicationUser user, string returnUrl = null)
    {
        // For offline mode, we don't validate server-side
        // The client-side JavaScript will handle validation against cached service worker data
        // This action is just a fallback in case JavaScript validation fails
        
        if (user == null || string.IsNullOrWhiteSpace(user.OfflineKey))
        {
            ViewBag.LoginError = "Offline access key is required.";
            return View();
        }

        // If we reach here, it means JavaScript validation passed but we still need server processing
        // In offline mode, we'll redirect to the application since the real validation happened client-side
        
        if (returnUrl == null)
        {
            returnUrl = TempData["returnUrl"]?.ToString();
        }

        if (returnUrl != null)
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }



    private IActionResult RedirectToLocal(string returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction("Index", "Home");
        }
    }

    [AllowAnonymous] 
    public IActionResult Forbidden()
    {
        return View();
    }


    public async Task<IActionResult> Profile()
    {
        //var db_config = _configuration.GetDBConfig(host_prefix);

        var days_til_value_expires = -1;

        int pass_value_days_before_expires = 0;
        
        _configuration.GetInteger("password_settings:days_before_expires", host_prefix).SetIfIsNotNullOrWhiteSpace(ref pass_value_days_before_expires);

        if(pass_value_days_before_expires > 0)
        {
            try
            {
                var userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;

                
                var session_event_request_url = db_config.Get_Prefix_DB_Url($"session/_design/session_event_sortable/_view/by_user_id?startkey=\"{userName}\"&endkey=\"{userName}\"");

                string response_from_server = await _couchDbHttpClient.ExecuteAsync(
                    "GET",
                    session_event_request_url,
                    null,
                    db_config.user_name,
                    db_config.user_value
                );

                //var session_event_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_object_key_header<mmria.common.model.couchdb.session_event>>(response_from_server);
                var session_event_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.session_event>>(response_from_server);

                DateTime first_item_date = DateTime.Now;
                DateTime last_item_date = DateTime.Now;

                session_event_response.rows.Sort(new mmria.common.model.couchdb.Compare_Session_Event_By_DateCreated<mmria.common.model.couchdb.session_event>());

                var date_of_last_password_change = DateTime.MinValue;
        
                foreach(var session_event in session_event_response.rows)
                {
                    if(session_event.value.action_result == mmria.common.model.couchdb.session_event.session_event_action_enum.password_changed)
                    {
                        date_of_last_password_change = session_event.value.date_created;
                        break;
                    }
                }

                if(date_of_last_password_change != DateTime.MinValue)
                {
                    days_til_value_expires = pass_value_days_before_expires - (int)(DateTime.Now - date_of_last_password_change).TotalDays;
                }
                else if(session_event_response.rows.Count > 0)
                {
                    days_til_value_expires = pass_value_days_before_expires - (int)(DateTime.Now - session_event_response.rows[session_event_response.rows.Count-1].value.date_created).TotalDays;
                }

                    
                
            }
            catch(Exception ex) 
            {
                System.Console.WriteLine ($"{ex}");
            }
        }
        
        ViewBag.days_til_password_expires = days_til_value_expires;
        ViewBag.config_password_days_before_expires = pass_value_days_before_expires;


        if(use_sams.HasValue)
        {
            ViewBag.sams_is_enabled = use_sams.Value;
        }
        else ViewBag.sams_is_enabled = false;

        return View();
    }

    public string GetRequestIP(bool tryUseXForwardHeader = true)
    {
        string ip = null;

        // todo support new "Forwarded" header (2014) https://en.wikipedia.org/wiki/X-Forwarded-For

        // X-Forwarded-For (csv list):  Using the First entry in the list seems to work
        // for 99% of cases however it has been suggested that a better (although tedious)
        // approach might be to read each IP from right to left and use the first public IP.
        // http://stackoverflow.com/a/43554000/538763
        //
        if (tryUseXForwardHeader)
            ip = GetHeaderValueAs<string>("X-Forwarded-For").SplitCsv().FirstOrDefault();

        // RemoteIpAddress is always null in DNX RC1 Update1 (bug).
        if (ip.IsNullOrWhitespace() && _accessor.HttpContext?.Connection?.RemoteIpAddress != null)
            ip = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();

        if (ip.IsNullOrWhitespace())
            ip = GetHeaderValueAs<string>("REMOTE_ADDR");

        // _httpContextAccessor.HttpContext?.Request?.Host this is the local host.

        if (ip.IsNullOrWhitespace())
            throw new Exception("Unable to determine caller's IP.");

        return ip;
    }

    public T GetHeaderValueAs<T>(string headerName)
    {
        Microsoft.Extensions.Primitives.StringValues values = new Microsoft.Extensions.Primitives.StringValues();

        if (_accessor.HttpContext?.Request?.Headers?.TryGetValue(headerName, out values) ?? false)
        {
            string rawValues = values.ToString();   // writes out as Csv when there are multiple.

            if (!rawValues.IsNullOrWhitespace())
                return (T)Convert.ChangeType(values.ToString(), typeof(T));
        }
        return default(T);
    }


    public async Task create_user_principal(string p_user_name, List<string> p_role_list)
    {
        const string Issuer = "https://contoso.com";
        var claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.Name, p_user_name, ClaimValueTypes.String, Issuer));


        foreach(var role in p_role_list)
        {
            if(role == "_admin")
            {
                claims.Add(new Claim(ClaimTypes.Role, "installation_admin", ClaimValueTypes.String, Issuer));
            }
        }

        #if !IS_PMSS_ENHANCED
        foreach(var role in mmria.common.SharedLibraries.Other.authorization.get_current_user_role_jurisdiction_set_for(db_config, p_user_name).Select( jr => jr.role_name).Distinct())
        {

            claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }
        #endif
        #if IS_PMSS_ENHANCED
        foreach(var role in mmria.pmss.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, p_user_name).Select( jr => jr.role_name).Distinct())
        {

            claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }
        #endif
/*
        Response.Cookies.Append("uid", p_user_name, new CookieOptions{ HttpOnly = true });
        Response.Cookies.Append("roles", string.Join(",",p_role_list), new CookieOptions{ HttpOnly = true });
*/          
        var userIdentity = new ClaimsIdentity("SuperSecureLogin");
        userIdentity.AddClaims(claims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);


        int session_idle_timeout_minutes = 10;
        
        _configuration.GetInteger("session_idle_timeout_minutes", host_prefix).SetIfIsNotNullOrWhiteSpace(ref session_idle_timeout_minutes);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            userPrincipal,
            new AuthenticationProperties
            {
                ExpiresUtc = DateTime.UtcNow.AddMinutes(session_idle_timeout_minutes),
                IsPersistent = false,
                AllowRefresh = true,
            });

    }

}

public static class IsLocalExtension
{
    public static List<string> SplitCsv(this string csvList, bool nullOrWhitespaceInputReturnsNull = false)
    {
        if (string.IsNullOrWhiteSpace(csvList))
            return nullOrWhitespaceInputReturnsNull ? null : new List<string>();

        return csvList
            .TrimEnd(',')
            .Split(',')
            .AsEnumerable<string>()
            .Select(s => s.Trim())
            .ToList();
    }

    public static bool IsNullOrWhitespace(this string s)
    {
        return String.IsNullOrWhiteSpace(s);
    }


    private const string NullIpAddress = "::1";
//_accessor.HttpContext.Connection.RemoteIpAddress.ToString()
    public static bool IsLocal(this HttpRequest req, IHttpContextAccessor _accessor)
    {
        var connection = req.HttpContext.Connection;
        if (_accessor.HttpContext.Connection.RemoteIpAddress.IsSet())
        {
            //We have a remote address set up
            return _accessor.HttpContext.Connection.LocalIpAddress.IsSet() 
                //Is local is same as remote, then we are local
                ? _accessor.HttpContext.Connection.RemoteIpAddress.Equals(_accessor.HttpContext.Connection.LocalIpAddress) 
                //else we are remote if the remote IP address is not a loopback address
                : System.Net.IPAddress.IsLoopback(_accessor.HttpContext.Connection.RemoteIpAddress);
        }

        return true;
    }

    private static bool IsSet(this System.Net.IPAddress address)
    {
        return address != null && address.ToString() != NullIpAddress;
    }



}
