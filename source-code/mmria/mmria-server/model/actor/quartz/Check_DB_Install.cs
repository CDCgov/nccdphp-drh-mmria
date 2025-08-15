using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace mmria.server.model.actor.quartz;

public sealed class Check_DB_Install : UntypedActor
{
    //protected override void PreStart() => Console.WriteLine("Check_DB_Install started");
    //protected override void PostStop() => Console.WriteLine("Check_DB_Install stopped");

	mmria.common.couchdb.DBConfigurationDetail db_config = null;

    public Check_DB_Install
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config
    )
    {
        db_config = _db_config;
    }

    protected override void OnReceive(object message)
    {
        switch(message)
        {
            case ScheduleInfoMessage scheduleInfoMessage:

            //Console.WriteLine($"Starup/Install Check - start {System.DateTime.Now}");
            if 
            (
                url_endpoint_exists (db_config.url, null, null, "GET") &&
                !db_config.user_name.Equals("couchdb_admin_user_name", StringComparison.OrdinalIgnoreCase) &&
                !db_config.user_value.Equals ("couchdb_admin_password", StringComparison.OrdinalIgnoreCase) &&
                !url_endpoint_exists (db_config.url, db_config.user_name, db_config.user_value, "GET")
            )
            {

                try
                {
                        new cURL ("PUT", null, db_config.url + $"/_node/nonode@nohost/_config/admins/{db_config.user_name}", $"\"{db_config.user_value}\"", null, null).execute();

                    //await new cURL ("PUT", null, db_config.url + "/_node/nonode@nohost/_config/mmria_section/app_version", $"\"{Program.config_app_version}\"", db_config.user_name, Program.config_timer_password).executeAsync();


                        new cURL ("PUT", null, db_config.url + "/_node/nonode@nohost/_config/couch_httpd_auth/allow_persistent_cookies", $"\"true\"", db_config.user_name, db_config.user_value).execute();


                        new cURL ("PUT", null, db_config.url + "/_node/nonode@nohost/_config/chttpd/bind_address", $"\"0.0.0.0\"", db_config.user_name, db_config.user_value).execute();
                        new cURL ("PUT", null, db_config.url + "/_node/nonode@nohost/_config/chttpd/port", $"\"5984\"", db_config.user_name, db_config.user_value).execute();


                        new cURL ("PUT", null, db_config.url + "/_node/nonode@nohost/_config/httpd/enable_cors", $"\"true\"", db_config.user_name, db_config.user_value).execute();


                        new cURL ("PUT", null, db_config.url + "/_node/nonode@nohost/_config/cors/origins", $"\"*\"", db_config.user_name, db_config.user_value).execute();

                        new cURL ("PUT", null, db_config.url + "/_node/nonode@nohost/_config/cors/credentials", $"\"true\"", db_config.user_name, db_config.user_value).execute();

                        new cURL ("PUT", null, db_config.url + "/_node/nonode@nohost/_config/cors/headers", $"\"accept, authorization, content-type, origin, referer, cache-control, x-requested-with\"", db_config.user_name, db_config.user_value).execute();

                        new cURL ("PUT", null, db_config.url + "/_node/nonode@nohost/_config/cors/methods", $"\"GET, PUT, POST, HEAD, DELETE\"", db_config.user_name, db_config.user_value).execute();

                        new cURL ("PUT", null, db_config.url + "/_users", null, db_config.user_name, db_config.user_value).execute();
                        new cURL ("PUT", null, db_config.url + "/_replicator", null, db_config.user_name, db_config.user_value).execute();
                        new cURL ("PUT", null, db_config.url + "/_global_changes", null, db_config.user_name, db_config.user_value).execute();
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Check_DB_Install Failed configuration \n{ex}");
                }
            }
            //Console.WriteLine($"Starup/Install Check - end {System.DateTime.Now}");
            break;
        }

        Context.Stop(this.Self);
    }


    bool url_endpoint_exists (string p_target_server, string p_user_name, string p_value, string p_method = "HEAD")
    {
        System.Net.HttpStatusCode response_result;

        try
        {
            //Creating the HttpWebRequest
            System.Net.HttpWebRequest request = System.Net.WebRequest.Create(p_target_server) as System.Net.HttpWebRequest;
            //Setting the Request method HEAD, you can also use GET too.

            if(request == null)
            {
                return false;
            }
            
            request.Method = p_method;

            if (!string.IsNullOrWhiteSpace(p_user_name) && !string.IsNullOrWhiteSpace(p_value))
            {
                string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(p_user_name + ":" + p_value));
                request.Headers.Add("Authorization", "Basic " + encoded);
            }

            //Getting the Web Response.
            System.Net.HttpWebResponse response = request.GetResponse() as System.Net.HttpWebResponse;
            //Returns TRUE if the Status code == 200
            if(response != null)
            {
                response_result = response.StatusCode;
                response.Close();
                return (response_result == System.Net.HttpStatusCode.OK);
            }
            else
            {
                return false;
            }
            
        }
        catch (Exception) 
        {
            //Log.Information ($"failed end_point exists check: {p_target_server}\n{ex}");
            Console.WriteLine($"failed end_point exists check: {p_target_server}");
            return false;
        }            
    }

}
