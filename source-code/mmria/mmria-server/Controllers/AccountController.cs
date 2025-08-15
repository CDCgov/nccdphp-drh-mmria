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
    mmria.common.couchdb.DBConfigurationDetail db_config;

    string host_prefix = null;
    bool? use_sams = null;

    public AccountController
    (
        IHttpContextAccessor httpContextAccessor, 
        ActorSystem actorSystem, 
        mmria.common.couchdb.OverridableConfiguration configuration
    )
    {
        _accessor = httpContextAccessor;
        _actorSystem = actorSystem;
        _configuration = configuration;
        host_prefix = _accessor.HttpContext.Request.Host.GetPrefix();

        db_config = _configuration.GetDBConfig(host_prefix);
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
    public IActionResult Login(string returnUrl = null)
    {
        TempData["returnUrl"] = returnUrl;

        return View();
    }


    [AllowAnonymous] 
    [HttpPost]
    public async Task<IActionResult> Login(ApplicationUser user, string returnUrl = null) 
    {

        if(use_sams.HasValue)
        {
            if(use_sams.Value)
            {
                return RedirectToAction("SignIn");
            }
        }

        const string badUserNameOrValueMessage = "Username or password is incorrect.";
        if(
            user == null ||
            string.IsNullOrWhiteSpace(user.UserName) ||
            string.IsNullOrWhiteSpace(user.Value)
        ) 
        {
            return BadRequest(badUserNameOrValueMessage);
        }



        try
        {
            int unsuccessful_login_attempts_number_before_lockout = 5;
            
            _configuration.GetInteger("unsuccessful_login_attempts_number_before_lockout", host_prefix).SetIfIsNotNullOrWhiteSpace(ref unsuccessful_login_attempts_number_before_lockout);
            int unsuccessful_login_attempts_within_number_of_minutes = 3;
            _configuration.GetInteger("unsuccessful_login_attempts_within_number_of_minutes", host_prefix).SetIfIsNotNullOrWhiteSpace(ref unsuccessful_login_attempts_within_number_of_minutes);
            int unsuccessful_login_attempts_lockout_number_of_minutes = 3; 
            _configuration.GetInteger("unsuccessful_login_attempts_lockout_number_of_minutes", host_prefix).SetIfIsNotNullOrWhiteSpace(ref unsuccessful_login_attempts_lockout_number_of_minutes);
            var password_days_before_expires = _configuration.GetInteger("days_before_expires", host_prefix);

            var is_locked_out = false;
            var failed_login_count = 0;
            var is_app_prefix_ok = false;
            
            DateTime grace_period_date = DateTime.Now;

            try
            {
                var user_request_url = $"{db_config.url}/_users/{System.Web.HttpUtility.HtmlEncode("org.couchdb.user:" + user.UserName.ToLower())}";
                var user_request_curl = new cURL("GET", null, user_request_url, null, db_config.user_name, db_config.user_value);
                string user_response_string = await user_request_curl.executeAsync ();
                var test_user = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.user>(user_response_string);

                if(string.IsNullOrWhiteSpace(db_config.prefix))
                {
                    if(test_user.app_prefix_list == null || test_user.app_prefix_list.Count == 0)
                    {
                        is_app_prefix_ok = true;
                    }
                    else if(test_user.app_prefix_list.ContainsKey("__no_prefix__"))
                    {
                        is_app_prefix_ok = true;
                    }
                }
                else if(test_user.app_prefix_list.ContainsKey(db_config.prefix))
                {
                    is_app_prefix_ok = test_user.app_prefix_list[db_config.prefix];
                }

                var session_event_request_url = db_config.Get_Prefix_DB_Url($"session/_design/session_event_sortable/_view/by_user_id?startkey=\"{user.UserName}\"&endkey=\"{user.UserName}\"");

                var session_event_curl = new cURL("GET", null, session_event_request_url, null, db_config.user_name, db_config.user_value);
                string response_from_server = await session_event_curl.executeAsync ();

                //var session_event_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_object_key_header<mmria.common.model.couchdb.session_event>>(response_from_server);
                var session_event_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.session_event>>(response_from_server);

                DateTime first_item_date = DateTime.Now;
                DateTime last_item_date = DateTime.Now;


                var MaxRange = DateTime.Now.AddMinutes(-unsuccessful_login_attempts_within_number_of_minutes);
                session_event_response.rows.Sort(new mmria.common.model.couchdb.Compare_Session_Event_By_DateCreated<mmria.common.model.couchdb.session_event>());


                foreach(var session_event in session_event_response.rows.Where(row=> row.value.date_created >= MaxRange))
                {
                    if(session_event.value.action_result == mmria.common.model.couchdb.session_event.session_event_action_enum.failed_login)
                    {
                        
                        failed_login_count++;
                        if(failed_login_count==1)
                        {
                            first_item_date = session_event.value.date_created;
                        }
                        
                        if(failed_login_count >= unsuccessful_login_attempts_number_before_lockout)
                        {
                            last_item_date= session_event.value.date_created;
                            grace_period_date = first_item_date.AddMinutes(unsuccessful_login_attempts_lockout_number_of_minutes);
                            if(DateTime.Now < grace_period_date)
                            {
                                is_locked_out = true;
                                break;
                            }
                        }
                    }
                    else if(session_event.value.action_result == mmria.common.model.couchdb.session_event.session_event_action_enum.successful_login)
                    {
                        break;
                    }
                }

            }
            catch(Exception ex) 
            {
                System.Console.WriteLine ($"{ex}");
            }


            if(is_locked_out)
            {

                return RedirectToAction("Locked",new { user_name = user.UserName, grace_period_date = grace_period_date});
                //return View("~/Views/Account/Locked.cshtml");
            }
            

            string post_data = string.Format ("name={0}&password={1}", user.UserName, user.Value);
            byte[] post_byte_array = System.Text.Encoding.ASCII.GetBytes(post_data);

            string request_string = db_config.url + "/_session";
            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));
            //request.UseDefaultCredentials = true;

            request.PreAuthenticate = false;

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = post_byte_array.Length;

            using (System.IO.Stream stream = request.GetRequestStream())
            {
                stream.Write(post_byte_array, 0, post_byte_array.Length);
            }

            System.Net.WebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.Stream dataStream = response.GetResponseStream ();
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            string responseFromServer = await reader.ReadToEndAsync ();

            mmria.common.model.couchdb.login_response json_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.login_response>(responseFromServer);

            mmria.common.model.couchdb.login_response[] result =  new mmria.common.model.couchdb.login_response[] 
            { 
                json_result
            }; 


            //this.Response.Headers.Add("Set-Cookie", response.Headers["Set-Cookie"]);

            string[] set_cookie = response.Headers["Set-Cookie"].Split(';');
            string[] auth_array = set_cookie[0].Split('=');
            if(auth_array.Length > 1)
            {
                string auth_session_token = auth_array[1];
                result[0].auth_session = auth_session_token;
            }
            else
            {
                result[0].auth_session = "";
            }

            if(!is_app_prefix_ok)
            {
                foreach(var role in json_result.roles)
                {
                    if(role == "_admin")
                    {
                        is_app_prefix_ok = true;
                    }
                }
            }

            if (is_app_prefix_ok && json_result.ok && !string.IsNullOrWhiteSpace(json_result.name)) 
            {

                const string Issuer = "https://contoso.com";
                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.Name, json_result.name, ClaimValueTypes.String, Issuer));

                List<string> role_list = new List<string>();
                foreach(var role in json_result.roles)
                {
                    if(role == "_admin")
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "installation_admin", ClaimValueTypes.String, Issuer));
                        role_list.Add("installation_admin");
                    }
                }

                #if !IS_PMSS_ENHANCED
                foreach(var role in mmria.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, json_result.name).Select( jr => jr.role_name).Distinct())
                {
                    claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
                }
                #endif
                #if IS_PMSS_ENHANCED
                foreach(var role in mmria.pmss.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, json_result.name).Select( jr => jr.role_name).Distinct())
                {
                    claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
                }
                #endif

                //Response.Cookies.Append("uid", json_result.name);
                //Response.Cookies.Append("roles", string.Join(",",json_result.roles));
                
                //claims.Add(new Claim("EmployeeId", string.Empty, ClaimValueTypes.String, Issuer));
                //claims.Add(new Claim("EmployeeId", "123", ClaimValueTypes.String, Issuer));
                //claims.Add(new Claim(ClaimTypes.DateOfBirth, "1970-06-08", ClaimValueTypes.Date));
                //var userIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var Session_Message_id = Guid.NewGuid().ToString();
                //claims.Add(new Claim(ClaimTypes.Sid, Session_Message_id, ClaimValueTypes.String, Issuer));


                var session_idle_timeout_minutes = 30;
                
                _configuration.GetInteger("session_idle_timeout_minutes",host_prefix).SetIfIsNotNullOrWhiteSpace(ref session_idle_timeout_minutes);
                

                var userIdentity = new ClaimsIdentity("SuperSecureLogin");
                userIdentity.AddClaims(claims);
                var userPrincipal = new ClaimsPrincipal(userIdentity);

                this.HttpContext.User = userPrincipal;
                System.Threading.Thread.CurrentPrincipal = userPrincipal;
        
                #if !IS_PMSS_ENHANCED
                foreach(var role in mmria.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, user.UserName).Select( jr => jr.role_name).Distinct())
                {
                    role_list.Add(role);
                }
                #endif
                #if IS_PMSS_ENHANCED
                foreach(var role in mmria.pmss.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, user.UserName).Select( jr => jr.role_name).Distinct())
                {
                    role_list.Add(role);
                }
                #endif

                var Session_Event_Message = new mmria.server.model.actor.Session_Event_Message
                (
                    DateTime.Now,
                    user.UserName,
                    this.GetRequestIP(),
                    json_result.ok && json_result.name != null? mmria.server.model.actor.Session_Event_Message.Session_Event_Message_Action_Enum.successful_login: mmria.server.model.actor.Session_Event_Message.Session_Event_Message_Action_Enum.failed_login
                );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Record_Session_Event>(db_config)).Tell(Session_Event_Message);


                var session_data = new System.Collections.Generic.Dictionary<string,string>(StringComparer.InvariantCultureIgnoreCase);
                var session_expiration_datetime =  DateTime.Now.AddMinutes(session_idle_timeout_minutes);
                var Session_Message = new mmria.server.model.actor.Session_Message
                (
                    Session_Message_id, //_id = 
                    null, //_rev = 
                    DateTime.Now, //date_created = 
                    DateTime.Now, //date_last_updated = 
                    session_expiration_datetime, //date_expired = 

                    true, //is_active = 
                    user.UserName, //user_id = 
                    this.GetRequestIP(), //ip = 
                    Session_Event_Message._id, // session_event_id = 
                    role_list,
                    session_data
                );

                var config_couchdb_url = db_config.url;
                var config_timer_user_name = db_config.user_name;
                var config_timer_password = db_config.user_value;



                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(Session_Message, settings);

                request_string = config_couchdb_url + $"/{db_config.prefix}session/{Session_Message._id}";

                mmria.server.cURL document_curl = new mmria.server.cURL ("PUT", null, request_string, object_string, config_timer_user_name, config_timer_password);

                try
                {
                    responseFromServer = document_curl.execute();
                    var put_session_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

                    if(put_session_result.ok)
                    {
                        _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Post_Session>(db_config)).Tell(Session_Message);
                        Response.Cookies.Append("sid", Session_Message._id, new CookieOptions{ HttpOnly = true });        
                        //Response.Cookies.Append("aid", Session_Message._id, new CookieOptions{ HttpOnly = false });
                        //Response.Cookies.Append("expires_at", unix_time.ToString(), new CookieOptions{ HttpOnly = true });
                    
                    /*
                        Response.Cookies.Append("sid", Session_Message._id, new CookieOptions{ HttpOnly = true, Expires = session_expiration_datetime, SameSite = SameSiteMode.Strict });
                        Response.Cookies.Append("expires_at", unix_time.ToString(), new CookieOptions{ HttpOnly = true, Expires = session_expiration_datetime, SameSite = SameSiteMode.Strict });
                    */
                        
                        //return RedirectToAction("Index", "HOME");
                        //return RedirectToAction("Index", "HOME");
                        //return RedirectToAction("Index", "HOME");
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }




/*

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    userPrincipal,
                    new AuthenticationProperties
                    {
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(session_idle_timeout_minutes),
                        IsPersistent = false,
                        AllowRefresh = true,
                    });*/
            }




            //this.ActionContext.Response.Headers.Add("Set-Cookie", auth_session_token);

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

            var Session_Event_Message = new mmria.server.model.actor.Session_Event_Message
            (
                DateTime.Now,
                user.UserName,
                this.GetRequestIP(),
                mmria.server.model.actor.Session_Event_Message.Session_Event_Message_Action_Enum.failed_login
            );

            _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Record_Session_Event>(db_config)).Tell(Session_Event_Message);

        } 
/*
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.Name, lookupUser.UserName));

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
*/
        if(returnUrl == null) 
        {
            returnUrl = TempData["returnUrl"]?.ToString();
        }

        if(returnUrl != null) 
        {
            return Redirect(returnUrl);
        }
        
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet]
    [HttpPost]
    public IActionResult Logout() 
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
            
                
                var session_message_curl = new mmria.server.cURL("GET", null, request_string, null, config_timer_user_name, config_timer_password);
                var responseFromServer =  session_message_curl.execute();

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

            _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Post_Session>(db_config)).Tell(Session_Message);

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

                var session_event_curl = new cURL("GET", null, session_event_request_url, null, db_config.user_name, db_config.user_value);
                string response_from_server = await session_event_curl.executeAsync ();

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
        foreach(var role in mmria.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, p_user_name).Select( jr => jr.role_name).Distinct())
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
