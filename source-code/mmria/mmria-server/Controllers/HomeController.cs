using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Akka.Actor;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;
using System.Collections.Generic;

namespace mmria.server.Controllers;

public sealed class HomeController : Controller
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    
    public HomeController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    public async Task<IActionResult> Index()
    {


        var userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;


        var days_til_expiration = -1;

        var password_days_before_expires = configuration.GetInteger("pass_word_days_before_expires",host_prefix);

        if(password_days_before_expires.HasValue && password_days_before_expires.Value > 0)
        {
            try
            {

                
                var session_event_request_url = $"{db_config.url}/{db_config.prefix}session/_design/session_event_sortable/_view/by_user_id?startkey=\"{userName}\"&endkey=\"{userName}\"";

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
                    days_til_expiration = password_days_before_expires.Value - (int)(DateTime.Now - date_of_last_password_change).TotalDays;
                }
                    
                
            }
            catch(Exception ex) 
            {
                System.Console.WriteLine ($"{ex}");
            }
        }




        try
        {
            ViewBag.is_power_bi_user = false;

            string my_user_url = $"{db_config.url}/_users/org.couchdb.user:{userName}";

            var user_curl = new cURL("GET",null,my_user_url,null, db_config.user_name, db_config.user_value);
            string responseFromServer = await user_curl.executeAsync();

            var user  = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.user>(responseFromServer);
            if
            (
                !string.IsNullOrEmpty(user.alternate_email)
            )
            {
                ViewBag.is_power_bi_user = true;
            }
        }
        catch(Exception ex) 
        {
            System.Console.WriteLine ($"{ex}");
        }


        ViewBag.sams_is_enabled = configuration.GetBoolean("sams:is_enabled", host_prefix).Value;
        ViewBag.days_til_password_expires = days_til_expiration;
        ViewBag.config_password_days_before_expires = password_days_before_expires;

        var LinkList = configuration.GetExternalHomePageLinks();

        

        return View(LinkList);
    }

}
