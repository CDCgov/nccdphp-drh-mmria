using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;


using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 


using Newtonsoft.Json.Linq;
//using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Microsoft.AspNetCore.HttpOverrides;
using Akka.Actor;
using mmria.common.SharedLibraries.Session.Model;
using mmria.common.SharedLibraries.Session.Manager;
using mmria.common.SharedLibraries.Session;

using mmria.server.Controllers;


/*
https://github.com/18F/identity-oidc-aspnet

*/

namespace mmria.common.Controllers;

public sealed partial class AccountController : Controller
{
    private const string OfflineExitPendingCookieName = "mmria_offline_exit_pending";

    private bool HasPendingOfflineExitCleanup()
    {
        return string.Equals(
            Request.Cookies[OfflineExitPendingCookieName],
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static readonly System.Text.Json.JsonSerializerOptions SensitiveJsonPayloadOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public const string ClientId = "urn:gov:gsa:openidconnect.profiles:sp:sso:logingov:aspnet_example";
    public const string ClientUrl = "http://localhost:50764";
    public const string IdpUrl = "https://idp.int.identitysandbox.gov";
    public const string AcrValues = "http://idmanagement.gov/ns/assurance/loa/1";


    // private IConfiguration _configuration;
    private IHttpContextAccessor _accessor;
    private mmria.common.SharedLibraries.Session.Manager.SessionManager _sessionManager;

    private bool user_principal_created = false;

    mmria.common.couchdb.OverridableConfiguration configuration;

    mmria.common.couchdb.SAMSConfigurationDetail sams_config;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.SharedLibraries.Account.IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepository;

    public AccountController
    (
        IHttpContextAccessor httpContextAccessor,
        mmria.common.SharedLibraries.Session.Manager.SessionManager sessionManager,
        ISessionRepository sessionRepository,
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.Account.IUserRepository userRepository
    )
    {
        _accessor = httpContextAccessor;
        _sessionManager = sessionManager;
        _sessionRepository = sessionRepository;
        configuration = tenantRuntime.RequireConfiguration();
        db_config = tenantRuntime.RequireDbConfig();
        _couchDbHttpClient = couchDbHttpClient;
        _userRepository = userRepository;

        host_prefix = tenantRuntime.EffectiveHostPrefix;

        sams_config = configuration.GetSAMSConfigurationDetail(host_prefix);
    }


    [AllowAnonymous] 
    public async Task<ActionResult> SignIn()
    {
        // Guard: redirect to app-offline page if the system is currently offline for this tenant.
        // This prevents an unnecessary SAMS round-trip when the app is unavailable.
        try
        {
            var vitalsUrl = configuration.GetString("vitals_url", host_prefix)
                ?.Replace("/api/Message/IJESet", string.Empty);
            if (!string.IsNullOrWhiteSpace(vitalsUrl))
            {
                var requestOptions = new mmria.common.getset.CouchDbRequestOptions
                {
                    VitalServiceKey = configuration.GetString("vital_service_key", host_prefix)
                };
                var json = await _couchDbHttpClient.ExecuteAsync(
                    "GET", $"{vitalsUrl}/api/systemOffline/GetSystemOfflineConfig",
                    null, "application/json", requestOptions);
                var cfg = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.SystemOfflineConfig>(json);
                if (cfg != null)
                {
                    bool affectsThisTenant = cfg.apply_to_all_jurisdictions ||
                        (cfg.selected_jurisdictions ?? new List<string>())
                            .Contains(host_prefix, StringComparer.OrdinalIgnoreCase);
                    bool isOffline = !string.IsNullOrWhiteSpace(cfg.offline_date) &&
                        DateTime.TryParse(cfg.offline_date, null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out var offlineDate) &&
                        DateTime.UtcNow >= offlineDate.ToUniversalTime();
                    if (affectsThisTenant && isOffline)
                        return RedirectToAction("AppOffline");
                }
            }
        }
        catch { /* if the offline check fails, proceed with the normal SAMS redirect */ }

        var sams_endpoint_authorization = configuration.GetString("sams:endpoint_authorization",host_prefix);
        var sams_client_id = sams_config.client_id;
        var sams_callback_url = sams_config.callback_url;        

        var state = Guid.NewGuid().ToString("N");
        var nonce = Guid.NewGuid().ToString("N");

        var sams_url = $"{sams_endpoint_authorization}?" +
            "&client_id=" + sams_client_id +
            //"&prompt=select_account" +
            "&redirect_uri=" + $"{sams_callback_url}" +
            "&response_type=code" +
            "&scope=" + System.Web.HttpUtility.HtmlEncode("openid profile email") +
            "&state=" + state +
            "&nonce=" + nonce;
        System.Diagnostics.Debug.WriteLine($"url: {sams_url}");
        return Redirect(sams_url);
    }

    [AllowAnonymous] 
    public async Task<ActionResult> SignInCallback()
    {

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

        var sams_endpoint_authorization = configuration.GetString("sams:endpoint_authorization",host_prefix);
        var sams_endpoint_token = configuration.GetString("sams:endpoint_token",host_prefix);
        var sams_endpoint_user_info = configuration.GetString("sams:endpoint_user_info",host_prefix);
        var sams_endpoint_token_validation = configuration.GetString("sams:endpoint_token_validation",host_prefix);
        var sams_endpoint_user_info_sys = configuration.GetString("sams:endpoint_user_info_sys",host_prefix);
        var sams_client_id =sams_config.client_id;
        var sams_client_secret = sams_config.client_secret;
        
        var sams_callback_url = sams_config.callback_url;

        //?code=6c17b2a3-d65a-44fd-a28c-9aee982f80be&state=a4c8326ca5574999aa13ca02e9384c3d
        // Retrieve code and state from query string, pring for debugging
        var querystring = Request.QueryString.Value;
        var querystring_skip = querystring.Substring(1, querystring.Length -1);
        var querystring_array = querystring_skip.Split("&");

        var querystring_dictionary = new Dictionary<string,string>();
        foreach(string item in querystring_array)
        {
            var pair = item.Split("=");
            querystring_dictionary.Add(pair[0], pair[1]);
        }

        var code = querystring_dictionary["code"];
        var state = querystring_dictionary["state"];
        System.Diagnostics.Debug.WriteLine($"code: {code}");
        System.Diagnostics.Debug.WriteLine($"state: {state}");

        HttpClient client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, sams_endpoint_token);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "client_id", sams_client_id },
            { "client_secret", sams_client_secret },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "scope", "openid profile email"},
            {"redirect_uri", sams_callback_url }
        });


        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
        var access_token = payload.Value<string>("access_token");
        var refresh_token = payload.Value<string>("refresh_token");
        var expires_in = payload.Value<int>("expires_in");


        var scope = payload.Value<string>("scope");

        //HttpContext.Session.SetString("access_token", access_token);
        //HttpContext.Session.SetString("refresh_token", refresh_token);

        var unix_time = DateTimeOffset.UtcNow.AddSeconds(expires_in);
        //HttpContext.Session.SetString("expires_at", unix_time.ToString());



        var id_token = payload.Value<string>("id_token");;
        var id_array = id_token.Split('.');


        var replaced_value = id_array[1].Replace('-', '+').Replace('_', '/');
        var base64 = replaced_value.PadRight(replaced_value.Length + (4 - replaced_value.Length % 4) % 4, '=');


        var id_0 = DecodeToken(id_array[0]);
        var id_1 = DecodeToken(id_array[1]);

        var id_body = Base64Decode(base64);

        var userInfoUriBuilder = new UriBuilder(sams_endpoint_user_info);
        var userInfoQuery = HttpUtility.ParseQueryString(userInfoUriBuilder.Query);
        userInfoQuery["token"] = id_token;
        userInfoUriBuilder.Query = userInfoQuery.ToString();
        var user_info_sys_request = new HttpRequestMessage(HttpMethod.Post, userInfoUriBuilder.Uri);

        user_info_sys_request.Headers.Authorization = mmria.server.util.OutboundRequestSecurityHelper.CreateBearerAuthenticationHeaderValue(access_token, nameof(access_token));
        user_info_sys_request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", sams_client_id },
            { "client_secret", sams_client_secret }
        });

        response = await client.SendAsync(user_info_sys_request);
        response.EnsureSuccessStatusCode();

        var temp_string = await response.Content.ReadAsStringAsync();
        payload = JObject.Parse(temp_string);

        
        var email = payload.Value<string>("email");


        //check if user exists
        var config_couchdb_url = db_config.url;
        var config_timer_user_name = db_config.user_name;
        var config_timer_value = db_config.user_value;

        var session_idle_timeout_minutes = mmria.server.util.SessionTimeoutHelper.GetSessionIdleTimeoutMinutes(
            configuration,
            configuration,
            host_prefix);
        mmria.common.model.couchdb.user user = null;
        try
        {
            user = await _userRepository.GetCouchDbUserAsync(email.ToLower(), db_config);

            // GetCouchDbUserAsync returns null on exception.
            // A 404 body with no name deserializes to a non-null user with null name; treat as not-found.
            if (user != null && string.IsNullOrWhiteSpace(user.name))
            {
                Console.WriteLine($"_users GET for {email?.ToLower()} returned a payload with no name field; treating as not-found.");
                user = null;
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);

        } 

        mmria.common.model.couchdb.document_put_response user_save_result = null;
        var is_app_prefix_ok = false;

        if(user == null)// if user does NOT exists create user with email
        {
            user = add_new_user(email.ToLower(), Guid.NewGuid().ToString());

            try
            {
                //test_user.app_prefix_list.ContainsKey("__no_prefix__")
                if(string.IsNullOrWhiteSpace(db_config.prefix))
                {
                    user.app_prefix_list.Add("__no_prefix__", true);
                    is_app_prefix_ok = true;
                }
                else if(user.app_prefix_list.ContainsKey(db_config.prefix))
                {
                    user.app_prefix_list[db_config.prefix] = true;
                    is_app_prefix_ok = true;
                }

                user_save_result = await _userRepository.PutUserAsync(user, db_config);

            }
            catch(Exception ex) 
            {
                Console.WriteLine (ex);
            }
        }
        else
        {
            if(string.IsNullOrWhiteSpace(db_config.prefix))
            {
                if(user.app_prefix_list == null || user.app_prefix_list.Count == 0)
                {
                    is_app_prefix_ok = true;
                }
                else if(user.app_prefix_list.ContainsKey("__no_prefix__"))
                {
                    is_app_prefix_ok = true;
                }
            }
            if(user.app_prefix_list.ContainsKey(db_config.prefix))
            {
                is_app_prefix_ok = user.app_prefix_list[db_config.prefix];
            }
        }

        if(!is_app_prefix_ok)
        {
            foreach(var role in user.roles)
            {
                if(role == "_admin")
                {
                    is_app_prefix_ok = true;
                }
            }
        }

        //create login session
        if(is_app_prefix_ok && (user_save_result == null || user_save_result.ok))
        {
            var session_data = new System.Collections.Generic.Dictionary<string,string>(StringComparer.InvariantCultureIgnoreCase);
            session_data["access_token"] = access_token;
            session_data["refresh_token"] = refresh_token;
            session_data["expires_at"] = unix_time.ToString();

            create_user_principal(this.HttpContext, user.name, new List<string>(), unix_time.DateTime);


            var Session_Event_Message = new Session_Event_Message
            (
                DateTime.Now,
                user.name,
                this.GetRequestIP(),
                mmria.common.SharedLibraries.Session.Model.Session_Event_Message.Session_Event_Message_Action_Enum.successful_login
            );

            _sessionManager.RecordSessionEvent(Session_Event_Message, db_config);


            List<string> role_list = new List<string>();
            foreach(var role in user.roles)
            {
                if(role == "_admin")
                {
                    role_list.Add("installation_admin");
                }
            }

            #if !IS_PMSS_ENHANCED
            foreach(var role in mmria.common.SharedLibraries.Other.authorization.get_current_user_role_jurisdiction_set_for(db_config, user.name, _couchDbHttpClient).Select( jr => jr.role_name).Distinct())
            {
                role_list.Add(role);
            }
            #endif
            #if IS_PMSS_ENHANCED
            foreach(var role in mmria.pmss.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, user.name, _couchDbHttpClient).Select( jr => jr.role_name).Distinct())
            {
                role_list.Add(role);
            }
            #endif

            var session_expiration_datetime =  DateTime.Now.AddMinutes(session_idle_timeout_minutes);
            var Session_Message = new Session_Message
            (
                Guid.NewGuid().ToString(), //_id = 
                null, //_rev = 
                DateTime.Now, //date_created = 
                DateTime.Now, //date_last_updated = 
                session_expiration_datetime, //date_expired = 

                true, //is_active = 
                user.name, //user_id = 
                this.GetRequestIP(), //ip = 
                Session_Event_Message._id, // session_event_id = 
                role_list,
                session_data
            );




            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(Session_Message, settings);

            try
            {
                var result = await _sessionRepository.SaveSessionRawAsync(Session_Message._id, object_string, db_config);

                if(result.ok)
                {
                    _ = _sessionManager.PostSessionAsync(Session_Message, db_config);
                    mmria.server.util.AppSessionCookieHelper.AppendAppSessionCookies(
                        Response,
                        Session_Message._id,
                        session_expiration_datetime,
                        Request.IsHttps,
                        unix_time.ToString(),
                        mmria.server.util.AppSessionCookieHelper.StandardSessionScopeValue);
                    

                    if((configuration.GetBoolean("is_offline_mode_enabled", host_prefix) ?? false) == true){
                        var hasPendingOfflineExitCleanup = HasPendingOfflineExitCleanup();

                        if(!hasPendingOfflineExitCleanup && priorUserName == user.name && priorRole == "offline_mode")
                        {
                            // Force a full logout to clear offline_mode role if user is switching from offline to online login
                            return Redirect("/case");
                        }

                        // Check for active offline sessions and redirect if found
                         try
                        {
                            if(!hasPendingOfflineExitCleanup)
                            {
                                var offlineCaseManager = (mmria.common.SharedLibraries.OfflineCase.Manager.IOfflineCaseManager)HttpContext.RequestServices.GetService(typeof(mmria.common.SharedLibraries.OfflineCase.Manager.IOfflineCaseManager));
                                if (offlineCaseManager != null)
                                {
                                    var shouldRedirect = await offlineCaseManager.ShouldRedirectToCaseSummaryAsync(user.name, db_config);
                                    if (shouldRedirect)
                                    {
                                        Console.WriteLine($"User {user.name} has active offline session, redirecting to /Case#/summary");
                                        return Redirect("/Case#/summary");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error checking offline session for user {user.name}: {ex}");
                            // Continue with normal login flow if check fails
                        }
                  
                    }
                    //return RedirectToAction("Index", "HOME");
                    return Redirect("/");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }


        }


        System.Console.WriteLine($"http_async_signin_called: {user_principal_created}");
        TempData["user_name"] = user.name;
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

    public void create_user_principal(HttpContext p_context, string p_user_name, List<string> p_role_list, DateTime p_session_expire_date_time)
    {
        const string Issuer = "https://contoso.com";

        if (string.IsNullOrWhiteSpace(p_user_name))
        {
            Console.WriteLine("create_user_principal: refusing to create principal with null/empty user name.");
            return;
        }

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
        foreach(var role in mmria.common.SharedLibraries.Other.authorization.get_current_user_role_jurisdiction_set_for(db_config, p_user_name, _couchDbHttpClient).Select( jr => jr.role_name).Distinct())
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                Console.WriteLine($"create_user_principal: skipping null/empty role for user={p_user_name}");
                continue;
            }
            claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }
        #endif
        #if IS_PMSS_ENHANCED
        foreach(var role in mmria.pmss.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, p_user_name, _couchDbHttpClient).Select( jr => jr.role_name).Distinct())
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                Console.WriteLine($"create_user_principal: skipping null/empty role for user={p_user_name}");
                continue;
            }
            claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }
        #endif

        //Response.Cookies.Append("uid", p_user_name);
        //Response.Cookies.Append("roles", string.Join(",",p_role_list));
        
        var userIdentity = new ClaimsIdentity("SuperSecureLogin");
        userIdentity.AddClaims(claims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);

        var ticket = new AuthenticationTicket(userPrincipal,"custom");

        p_context.User = userPrincipal;
        System.Threading.Thread.CurrentPrincipal = userPrincipal;
        user_principal_created = true;

    }

    private string DecodeToken(string p_value)
    {
        var replaced_value = p_value.Replace('-', '+').Replace('_', '/');
        var base64 = replaced_value.PadRight(replaced_value.Length + (4 - replaced_value.Length % 4) % 4, '=');
        return Base64Decode(base64);
    }

    private string Base64Decode(string base64EncodedData) 
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }


/*
    private bool checkID(string idBody, string issuer, string clientID)
    {
        object o = JObject.Parse(idBody);
        
        if (o.iss != issuer) return false;
        if (o.aud != clientID) return false;
        if (o.exp < DateTime.UtcNow) return false;

        return true;
    }
    */

    private mmria.common.model.couchdb.user add_new_user(string p_name, string p_password)
    {
        return new mmria.common.model.couchdb.user(){
            _id = $"org.couchdb.user:{p_name}",
            password =  p_password,
            password_scheme = "pbkdf2",
            iterations = 10,
            name = p_name,
            roles = new List<string>().ToArray(),
            type = "user",
            derived_key =  "a1bb5c132df5b7df7654bbfa0e93f9e304e40cfe",
            salt = "510427706d0deb511649021277b2c05d",
            is_active = true,
            is_enabled = true
            };
    }

}

