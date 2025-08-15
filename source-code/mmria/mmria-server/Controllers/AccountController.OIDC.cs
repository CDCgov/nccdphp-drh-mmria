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


using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 


using Newtonsoft.Json.Linq;
//using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Akka.Actor;

using mmria.server.Controllers;


/*
https://github.com/18F/identity-oidc-aspnet

*/

namespace mmria.common.Controllers;

public sealed partial class AccountController : Controller
{
    public const string ClientId = "urn:gov:gsa:openidconnect.profiles:sp:sso:logingov:aspnet_example";
    public const string ClientUrl = "http://localhost:50764";
    public const string IdpUrl = "https://idp.int.identitysandbox.gov";
    public const string AcrValues = "http://idmanagement.gov/ns/assurance/loa/1";


    // private IConfiguration _configuration;
    private IHttpContextAccessor _accessor;
    private ActorSystem _actorSystem;

    private bool user_principal_created = false;

    mmria.common.couchdb.OverridableConfiguration configuration;

    mmria.common.couchdb.SAMSConfigurationDetail sams_config;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public AccountController
    (
        IHttpContextAccessor httpContextAccessor, 
        ActorSystem actorSystem, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        _accessor = httpContextAccessor;
        _actorSystem = actorSystem;
        configuration = _configuration;

        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);

        sams_config = configuration.GetSAMSConfigurationDetail(host_prefix);
    }


    [AllowAnonymous] 
    public ActionResult SignIn()
    {

        //Response.Cookies.Delete("sid");
        //Response.Cookies.Delete("expires_at");

        

        var sams_endpoint_authorization = configuration.GetString("sams:endpoint_authorization",host_prefix);
        var sams_endpoint_token = configuration.GetString("sams:endpoint_token",host_prefix);
        var sams_endpoint_user_info = configuration.GetString("sams:endpoint_user_info",host_prefix);
        var sams_endpoint_token_validation = configuration.GetString("sams:endpoint_token_validation",host_prefix);
        var sams_endpoint_user_info_sys = configuration.GetString("sams:endpoint_user_info_sys",host_prefix);
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

        /*
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "client_id", sams_client_id },
            { "client_secret", sams_client_secret },
            { "grant_type", "client_credentials" },
            { "code", code },
        });
            */

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

        var user_info_sys_request = new HttpRequestMessage(HttpMethod.Post, sams_endpoint_user_info + "?token=" + id_token);


        user_info_sys_request.Headers.Add("Authorization","Bearer " + access_token); 
        user_info_sys_request.Headers.Add("client_id", sams_client_id); 
        user_info_sys_request.Headers.Add("client_secret", sams_client_secret); 

        /* 
        user_info_sys_request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "client_id", sams_client_id },
            { "client_secret", sams_client_secret },
            { "grant_type", "client_credentials" },
            { "scope", scope },
        });
        */



        response = await client.SendAsync(user_info_sys_request);
        response.EnsureSuccessStatusCode();

        var temp_string = await response.Content.ReadAsStringAsync();
        payload = JObject.Parse(temp_string);

        
        var email = payload.Value<string>("email");


        //check if user exists
        var config_couchdb_url = db_config.url;
        var config_timer_user_name =db_config.user_name;
        var config_timer_value = db_config.user_value;

        var config_session_idle_timeout_minutes = configuration.GetInteger("session_idle_timeout_minutes", host_prefix);
        mmria.common.model.couchdb.user user = null;
        try
        {
            string request_string = config_couchdb_url + "/_users/" + System.Web.HttpUtility.HtmlEncode("org.couchdb.user:" + email.ToLower());
            var user_curl = new mmria.server.cURL("GET", null, request_string, null, config_timer_user_name, config_timer_value);
            var responseFromServer = await user_curl.executeAsync();

            user = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.user>(responseFromServer);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

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

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(user, settings);

                string user_db_url = config_couchdb_url + "/_users/"  + user._id;

                var user_curl = new mmria.server.cURL("PUT", null, user_db_url, object_string, config_timer_user_name, config_timer_value);
                var responseFromServer = await user_curl.executeAsync();
                user_save_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

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


            var Session_Event_Message = new mmria.server.model.actor.Session_Event_Message
            (
                DateTime.Now,
                user.name,
                this.GetRequestIP(),
                mmria.server.model.actor.Session_Event_Message.Session_Event_Message_Action_Enum.successful_login
            );

            _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Record_Session_Event>(db_config)).Tell(Session_Event_Message);


            List<string> role_list = new List<string>();
            foreach(var role in user.roles)
            {
                if(role == "_admin")
                {
                    role_list.Add("installation_admin");
                }
            }

            #if !IS_PMSS_ENHANCED
            foreach(var role in mmria.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, user.name).Select( jr => jr.role_name).Distinct())
            {
                role_list.Add(role);
            }
            #endif
            #if IS_PMSS_ENHANCED
            foreach(var role in mmria.pmss.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, user.name).Select( jr => jr.role_name).Distinct())
            {
                role_list.Add(role);
            }
            #endif

            var session_expiration_datetime =  DateTime.Now.AddMinutes(config_session_idle_timeout_minutes.Value);
            var Session_Message = new mmria.server.model.actor.Session_Message
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

            string request_string = config_couchdb_url + $"/{db_config.prefix}session/{Session_Message._id}";

            mmria.server.cURL document_curl = new mmria.server.cURL ("PUT", null, request_string, object_string, config_timer_user_name, config_timer_value);

            try
            {
                string responseFromServer = document_curl.execute();
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

                if(result.ok)
                {
                    _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Post_Session>(db_config)).Tell(Session_Message);
                    Response.Cookies.Append("sid", Session_Message._id, new CookieOptions{ HttpOnly = true });
                    Response.Cookies.Append("expires_at", unix_time.ToString(), new CookieOptions{ HttpOnly = true });
                    
                    /*
                    Response.Cookies.Append("sid", Session_Message._id, new CookieOptions{ HttpOnly = true, Expires = session_expiration_datetime, SameSite = SameSiteMode.Strict });
                    Response.Cookies.Append("expires_at", unix_time.ToString(), new CookieOptions{ HttpOnly = true, Expires = session_expiration_datetime, SameSite = SameSiteMode.Strict });
                    */
                    
                    //return RedirectToAction("Index", "HOME");
                    //return RedirectToAction("Index", "HOME");
                    return RedirectToAction("Index", "HOME");
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

        //Response.Cookies.Append("uid", p_user_name);
        //Response.Cookies.Append("roles", string.Join(",",p_role_list));
        
        var userIdentity = new ClaimsIdentity("SuperSecureLogin");
        userIdentity.AddClaims(claims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);

        var session_idle_timeout_minutes = 30;
        configuration.GetInteger("session_idle_timeout_minutes",host_prefix).SetIfIsNotNullOrWhiteSpace(ref session_idle_timeout_minutes);



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

