using System;
using System.Collections.Generic;
using Microsoft.CSharp;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Linq;
using mmria.common.model.couchdb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;

using  mmria.server.extension;    

namespace mmria.server;

//[Authorize(Roles  = "jurisdiction_admin")]
//[AllowAnonymous] 
[Route("api/[controller]")]
public sealed class sessionDBController: ControllerBase 
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public sessionDBController 
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }


    [HttpGet]
    public async System.Threading.Tasks.Task<IEnumerable<session_response>> Get() 
    { 
        try
        {
            string request_string = db_config.url + "/_session";
            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));

            request.PreAuthenticate = false;



            if (!string.IsNullOrWhiteSpace(this.Request.Cookies["AuthSession"]))
            {
                string auth_session_value = this.Request.Cookies["AuthSession"];
                request.Headers.Add("Cookie", "AuthSession=" + auth_session_value);
                request.Headers.Add("X-CouchDB-WWW-Authenticate", auth_session_value);
            }
/*
            if(this.Request.Headers.Contains("Cookie") && this.Request.Headers.GetValues("Cookie").Count() > 0)
            {
                string[] cookie_set = this.Request.Headers.GetValues("Cookie").First().Split(';');
                for(int i = 0; i < cookie_set.Length; i++)
                {
                    string[] auth_session_token = cookie_set[i].Split('=');
                    if(auth_session_token[0].Trim() == "AuthSession")
                    {
                        request.Headers.Add("Cookie", "AuthSession=" + auth_session_token[1]);
                        request.Headers.Add("X-CouchDB-WWW-Authenticate", auth_session_token[1]);
                        break;
                    }
                }
            }
*/

            System.Net.WebResponse response = await request.GetResponseAsync();
            System.IO.Stream dataStream = response.GetResponseStream ();
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            string responseFromServer = reader.ReadToEnd ();
            session_response json_result = Newtonsoft.Json.JsonConvert.DeserializeObject<session_response>(responseFromServer);

            if(response.Headers["Set-Cookie"] != null)
            {
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
            }

            /*
    < HTTP/1.1 200 OK
    < Set-Cookie: AuthSession=YW5uYTo0QUIzOTdFQjrC4ipN-D-53hw1sJepVzcVxnriEw;
    < Version=1; Path=/; HttpOnly
    > ...
    <
    {"ok":true}*/

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


    //[Authorize(Roles  = "abstractor")]
    [HttpPut]
    [HttpPost]
    public async System.Threading.Tasks.Task<IEnumerable<login_response>> Post
    (
        [FromBody] Post_Request_Struct post_request_struct 
    ) 
    {
        

        /*
        post_request_struct.userid = null;
        //post_request_struct.password = null;

        try 
        {

            System.IO.Stream dataStream0 = await this.Request.Content.ReadAsStreamAsync ();
            // Open the stream using a StreamReader for easy access.
            //dataStream0.Seek(0, System.IO.SeekOrigin.Begin);
            System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);
            // Read the content.
            string temp = reader0.ReadToEnd ();
            //System.Console.Write ($"temp {temp}");
            post_request_struct = Newtonsoft.Json.JsonConvert.DeserializeObject<Post_Request_Struct> (temp);

            //mmria.server.utilsLuceneSearchIndexer.RunIndex(new List<mmria.common.model.home_record> { mmria.common.model.home_record.convert(queue_request)});
            //System.Dynamic.ExpandoObject json_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(result, new  Newtonsoft.Json.Converters.ExpandoObjectConverter());



            //string metadata = DecodeUrlString(temp);
        } catch (Exception ex) {
            Console.WriteLine (ex);
        }
*/

        /*
HOST="http://127.0.0.1:5984"
> curl -vX POST $HOST/_session -H 'Content-Type: application/x-www-form-urlencoded' -d 'name=anna&password=secret'
*/
        try
        {
            string post_data = string.Format ("name={0}&password={1}", db_config.user_name, db_config.user_value);
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
            }/**/

            System.Net.WebResponse response = await request.GetResponseAsync();

            System.IO.Stream dataStream = response.GetResponseStream ();

            // Open the stream using a StreamReader for easy access.
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd ();

            login_response json_result = Newtonsoft.Json.JsonConvert.DeserializeObject<login_response>(responseFromServer);

            login_response[] result =  new login_response[] 
            { 
                json_result
            }; 


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

            //this.ActionContext.Response.Headers.Add("Set-Cookie", auth_session_token);

            return result;

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    }

    //https://wiki.apache.org/couchdb/Session_API
    // DELETE api/_sevalues/5 

    /*
    public async System.Threading.Tasks.Task<logout_response> Delete() 
    { 
        try
        {
            string request_string = db_config.url + "/_session";
            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));
            request.Method = "DELETE";
            request.PreAuthenticate = false;

            if(this.Request.Headers.Contains("Cookie") && this.Request.Headers.GetValues("Cookie").Count() > 0)
            {
                string[] cookie_set = this.Request.Headers.GetValues("Cookie").First().Split(';');
                for(int i = 0; i < cookie_set.Length; i++)
                {
                    string[] auth_session_token = cookie_set[i].Split('=');
                    if(auth_session_token[0].Trim() == "AuthSession")
                    {
                        request.Headers.Add("Cookie", "AuthSession=" + auth_session_token[1]);
                        request.Headers.Add("X-CouchDB-WWW-Authenticate", auth_session_token[1]);
                        break;
                    }
                }
            }


            System.Net.WebResponse response = await request.GetResponseAsync();
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
        */

}



