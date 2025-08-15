using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using mmria.common.model.couchdb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;
namespace mmria.server;

[Route("api/[controller]")]
public sealed class sessionController: ControllerBase
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public sessionController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    [Route("list")]
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.session>> Get
    (
        int skip = 0,
        int take = 25,
        string sort = "by_date_created",
        string search_key = null,
        bool descending = false
    ) 
    {
        string sort_view = sort.ToLower ();
        switch (sort_view)
        {
                case "by_date_created":
                case "by_date_created_user_id":
                case "by_session_event_id":
                case "by_user_id":
                case "by_ip":
                break;

            default:
                sort_view = "by_date_created";
            break;
        }



        try
        {
            System.Text.StringBuilder request_builder = new System.Text.StringBuilder ();
            request_builder.Append (db_config.url);
            request_builder.Append ($"/{db_config.prefix}jurisdiction/_design/sortable/_view/{sort_view}?");


            if (string.IsNullOrWhiteSpace (search_key))
            {
                if (skip > -1) 
                {
                    request_builder.Append ($"skip={skip}");
                } 
                else 
                {

                    request_builder.Append ("skip=0");
                }


                if (take > -1) 
                {
                    request_builder.Append ($"&limit={take}");
                }

                if (descending) 
                {
                    request_builder.Append ("&descending=true");
                }
            } 
            else 
            {
                request_builder.Append ("skip=0");

                if (descending) 
                {
                    request_builder.Append ("&descending=true");
                }
            }

            var user_role_jurisdiction_curl = new cURL("GET", null, request_builder.ToString(), null, db_config.user_name, db_config.user_value);
            string response_from_server = await user_role_jurisdiction_curl.executeAsync ();

            var session_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.session>>(response_from_server);

            if (string.IsNullOrWhiteSpace (search_key)) 
            {
                return session_view_response;
            } 
            else 
            {
                string key_compare = search_key.ToLower ().Trim (new char [] { '"' });

                var result = new mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.session>();
                result.offset = session_view_response.offset;
                result.total_rows = session_view_response.total_rows;

                //foreach(mmria.common.model.couchdb.user_role_jurisdiction cvi in case_view_response.rows)
                foreach(mmria.common.model.couchdb.get_sortable_view_response_item<mmria.common.model.couchdb.session> cvi in session_view_response.rows)
                {
                    bool add_item = false;
                    if (cvi.value.ip != null && cvi.value.ip.Equals(key_compare, StringComparison.OrdinalIgnoreCase))
                    {
                        add_item = true;
                    }

                    if(bool.TryParse(key_compare, out bool is_active))
                    {
                        if(cvi.value.is_active == is_active)
                        {
                            add_item = true;
                        }
                    }


                    if(cvi.value.user_id != null && cvi.value.user_id.Equals (key_compare, StringComparison.OrdinalIgnoreCase))
                    {
                        add_item = true;
                    }

                    DateTime is_date;
                    if(DateTime.TryParse(key_compare, out is_date))
                    {
                        if(cvi.value.date_created == is_date)
                        {
                            add_item = true;
                        }
                    }
                

                    if(DateTime.TryParse(key_compare, out is_date))
                    {
                        if(cvi.value.date_last_updated == is_date)
                        {
                            add_item = true;
                        }
                    }
                    

                    if(cvi.value.session_event_id != null && cvi.value.session_event_id.Equals (key_compare, StringComparison.OrdinalIgnoreCase))
                    {
                        add_item = true;
                    }

                    if(add_item) result.rows.Add (cvi);
                    
                }

                result.total_rows = result.rows.Count;
                result.rows =  result.rows.Skip (skip).Take (take).ToList ();

                return result;
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    } 


    [HttpGet]
    public  async System.Threading.Tasks.Task<IEnumerable<session_response>> Get() 
    { 
        try
        {
            string request_string = db_config.url + $"/{db_config.prefix}session";
            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));

            request.PreAuthenticate = false;

            System.Net.WebResponse response = await request.GetResponseAsync();
            System.IO.Stream dataStream = response.GetResponseStream ();
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            string responseFromServer = reader.ReadToEnd ();
            session_response json_result = Newtonsoft.Json.JsonConvert.DeserializeObject<session_response>(responseFromServer);
/*
            if(response.Headers["Set-Cookie"] != null)
            {
                this.Response.Headers.Add("Set-Cookie", response.Headers["Set-Cookie"]);
                string[] set_cookie = response.Headers["Set-Cookie"].Split(';');
                string[] auth_array = set_cookie[0].Split('=');
                if(auth_array.Length > 1)
                {
                    string auth_session_token = auth_array[1];
                    json_result.auth_session = auth_session_token;
                }
                else
                {
                    json_result.auth_session = "";
                }
            }*/

            session_response[] result =  new session_response[] 
            { 
                json_result
            }; 

            return result;

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    }

    [HttpPut]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] session Post_Request

    ) 
    { 

        try
        {

            mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();
            string request_string = db_config.url + $"/{db_config.prefix}session/{Post_Request._id}";

            try 
            {
                

                var check_document_curl = new cURL ("GET", null, request_string, null, db_config.user_name, db_config.user_value);
                string check_document_json = await check_document_curl.executeAsync ();
                var check_document_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<session> (check_document_json);

                var userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;


                if(!userName.Equals(check_document_expando_object.user_id, StringComparison.OrdinalIgnoreCase))
                {
                    Console.Write($"unauthorized PUT {Post_Request._id} by: {userName}");
                    return result;
                }

            } 
            catch (Exception ex) 
            {
                // do nothing for now document doesn't exsist.
                System.Console.WriteLine ($"err caseController.Post\n{ex}");
            }

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(Post_Request, settings);

            cURL document_curl = new cURL ("PUT", null, request_string, object_string, db_config.user_name, db_config.user_value);

            try
            {
                string responseFromServer = await document_curl.executeAsync();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }

    //https://wiki.apache.org/couchdb/Session_API
    // DELETE api/_sevalues/5 
    public logout_response Delete() 
    { 
        try
        {
            string request_string = db_config.url + "/_session";


            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));
            request.Method = "DELETE";
            request.PreAuthenticate = false;

            if (!string.IsNullOrWhiteSpace(this.Request.Cookies["AuthSession"]))
            {
                string auth_session_value = this.Request.Cookies["AuthSession"];
                request.Headers.Add("Cookie", "AuthSession=" + auth_session_value);
                request.Headers.Add("X-CouchDB-WWW-Authenticate", auth_session_value);
            }



            System.Net.WebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.Stream dataStream = response.GetResponseStream ();
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            string responseFromServer = reader.ReadToEnd ();
            logout_response json_result = Newtonsoft.Json.JsonConvert.DeserializeObject<logout_response>(responseFromServer);

            return json_result;
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    }

}

public struct Post_Request_Struct
{
    public string name;
    public string value;
}


