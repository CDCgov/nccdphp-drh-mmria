using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using mmria.server.extension;

namespace mmria.server.authentication;

public sealed class CustomAuthHandler : AuthenticationHandler<CustomAuthOptions>
{
    mmria.common.couchdb.OverridableConfiguration _configuration;

    public CustomAuthHandler
    (
        mmria.common.couchdb.OverridableConfiguration configuration, 
        IOptionsMonitor<CustomAuthOptions> options, 
        ILoggerFactory logger, 
        UrlEncoder encoder, 
        ISystemClock clock
    ) 
        : base(options, logger, encoder, clock)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string host_prefix = Request.Host.GetPrefix();

        var db_config = _configuration.GetDBConfig(host_prefix);

        if(db_config == null)
        {
            string couchdb_url =  null;
            string timer_user_name = null;
            string timer_value = null;
            string db_prefix = null;

            System.Environment.GetEnvironmentVariable("couchdb_url").SetIfIsNotNullOrWhiteSpace(ref couchdb_url);
            System.Environment.GetEnvironmentVariable("db_prefix").SetIfIsNotNullOrWhiteSpace(ref db_prefix);
            System.Environment.GetEnvironmentVariable("timer_user_name").SetIfIsNotNullOrWhiteSpace(ref timer_user_name);
            System.Environment.GetEnvironmentVariable("timer_password").SetIfIsNotNullOrWhiteSpace(ref timer_value);
    
            db_config = new()
            {
                url =  couchdb_url,
                user_name = timer_user_name,
                user_value = timer_value
            };
            

        }
        

        if
        (
            Request.Cookies.ContainsKey("sid") && 
            !string.IsNullOrWhiteSpace(Request.Cookies["sid"])
        )
        {

/*
            var config_couchdb_url = _configuration["mmria_settings:couchdb_url"];
            var config_timer_user_name = _configuration["mmria_settings:timer_user_name"];
            var config_timer_password = _configuration["mmria_settings:timer_value"];
            var config_db_prefix = _configuration["mmria_settings:db_prefix"];
*/

            mmria.server.model.actor.Session_MessageDTO session_message = null;
            try
            {
                string request_string = db_config.Get_Prefix_DB_Url($"session/{Request.Cookies["sid"]}");
                var session_message_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
                var responseFromServer =  session_message_curl.execute();

                session_message = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.server.model.actor.Session_MessageDTO>(responseFromServer);

            }
            catch(System.Exception ex)
            {
                System.Console.WriteLine (ex);

            } 

            if(session_message == null)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid session."));
            }

            if(session_message.date_expired == null || session_message.date_expired.HasValue == false)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid session. Expired"));
            }
            else if(session_message.date_expired.Value < System.DateTime.Now)
            {
                if(Request.Path.Value.StartsWith("/api/"))
                {
                    return Task.FromResult(AuthenticateResult.Fail("Invalid session. Expired"));
                }
                else
                {
                    return Task.FromResult(AuthenticateResult.Fail("Invalid session. Expired"));
                }
            }
            else if
            (
                !string.IsNullOrWhiteSpace(session_message.user_id)
            )
            {

                var date_diff = session_message.date_expired.Value - System.DateTime.Now;
                var time_out_value = _configuration.GetInteger("session_idle_timeout_minutes", "shared");
                double time_out = 15;
                
                if(time_out_value.HasValue)
                {
                    time_out = time_out_value.Value;
                }

                var session_url = false;

                if(Request.Path.Value.StartsWith("/api/"))
                {

                }

                if
                (
                    date_diff.TotalMinutes < time_out &&
                    time_out - date_diff.TotalMinutes  > 1
                )
                {   

                    session_message.date_expired = System.DateTime.Now.AddMinutes(time_out);
                    string session_message_json = Newtonsoft.Json.JsonConvert.SerializeObject(session_message);
                    try
                    {
                        string request_string = db_config.Get_Prefix_DB_Url($"session/{Request.Cookies["sid"]}");
                        
                        var session_put_curl = new mmria.server.cURL("PUT", null, request_string, session_message_json, db_config.user_name, db_config.user_value);
                        var responseFromServer =  session_put_curl.execute();

                        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer); 
                        if(!response.ok)
                        {
                            System.Console.WriteLine ("problem saving session update.");
                        }

                    }
                    catch(System.Exception ex)
                    {
                        System.Console.WriteLine (ex);
                    } 
                }

                //mmria.common.model.couchdb.user user = null;



                const string Issuer = "https://contoso.com";
                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.Name, session_message.user_id, ClaimValueTypes.String, Issuer));

                foreach(var role in session_message.role_list)
                {
                    if(role == "installation_admin")
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "installation_admin", ClaimValueTypes.String, Issuer));
                    }
                }

                #if !IS_PMSS_ENHANCED
                foreach(var role in mmria.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, session_message.user_id).Select( jr => jr.role_name).Distinct())
                {
                    claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
                }
                #endif
                #if IS_PMSS_ENHANCED
                foreach(var role in mmria.pmss.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, session_message.user_id).Select( jr => jr.role_name).Distinct())
                {
                    claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
                }
                #endif

                var userIdentity = new ClaimsIdentity("SuperSecureLogin");
                userIdentity.AddClaims(claims);
                var userPrincipal = new ClaimsPrincipal(userIdentity);

                /*
                var session_idle_timeout_minutes = 15;
                
                var temp_int = _configuration.GetInteger("session_idle_timeout_minutes", host_prefix);
                if(temp_int.HasValue)
                {
                    session_idle_timeout_minutes = temp_int.Value;
                }
                */

                var ticket = new AuthenticationTicket(userPrincipal,"custom");

                this.Context.User=userPrincipal;
                System.Threading.Thread.CurrentPrincipal = userPrincipal;


                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            else
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid session."));
            }
        }
        else 
        {
            if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var authorization))
            {
                return Task.FromResult(AuthenticateResult.Fail("Cannot read authorization header."));
            }

            // The auth key from Authorization header check against the configured ones
            if (authorization.Any(key => Options.AuthKey.All(ak => ak != key)))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid auth key."));
            }

            return Task.FromResult(AuthenticateResult.Fail("Invalid auth key."));
        }
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if(this.Options.Is_SAMS)
        {
            Response.Redirect("/Account/SignIn");
        }
        else
        {
            Response.Redirect("/Account/Login");

        }
    }

}
