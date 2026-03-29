using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace mmria.server.model.actor.quartz;

public sealed class Check_DB_Install : ReceiveActor
{
    //protected override void PreStart() => Console.WriteLine("Check_DB_Install started");
    //protected override void PostStop() => Console.WriteLine("Check_DB_Install stopped");

	private readonly mmria.common.couchdb.DBConfigurationDetail _dbConfig;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public Check_DB_Install
    (
        mmria.common.couchdb.DBConfigurationDetail dbConfig,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _dbConfig = dbConfig;
        _couchDbHttpClient = couchDbHttpClient;
        
        ReceiveAsync<ScheduleInfoMessage>(async scheduleInfoMessage =>
        {
            //Console.WriteLine($"Starup/Install Check - start {System.DateTime.Now}");
            if 
            (
                url_endpoint_exists (_dbConfig.url, null, null, "GET") &&
                !_dbConfig.user_name.Equals("couchdb_admin_user_name", StringComparison.OrdinalIgnoreCase) &&
                !_dbConfig.user_value.Equals ("couchdb_admin_password", StringComparison.OrdinalIgnoreCase) &&
                !url_endpoint_exists (_dbConfig.url, _dbConfig.user_name, _dbConfig.user_value, "GET")
            )
            {

                try
                {
                        await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + $"/_node/nonode@nohost/_config/admins/{_dbConfig.user_name}", $"\"{_dbConfig.user_value}\"", null, null, "application/json");

                    //await new cURL ("PUT", null, db_config.url + "/_node/nonode@nohost/_config/mmria_section/app_version", $"\"{Program.config_app_version}\"", db_config.user_name, Program.config_timer_password).executeAsync();


                        await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + "/_node/nonode@nohost/_config/couch_httpd_auth/allow_persistent_cookies", $"\"true\"", _dbConfig.user_name, _dbConfig.user_value, "application/json");


                        await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + "/_node/nonode@nohost/_config/chttpd/bind_address", $"\"0.0.0.0\"", _dbConfig.user_name, _dbConfig.user_value, "application/json");
                        await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + "/_node/nonode@nohost/_config/chttpd/port", $"\"5984\"", _dbConfig.user_name, _dbConfig.user_value, "application/json");


                        await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + "/_node/nonode@nohost/_config/httpd/enable_cors", $"\"true\"", _dbConfig.user_name, _dbConfig.user_value, "application/json");


                        await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + "/_node/nonode@nohost/_config/cors/origins", $"\"*\"", _dbConfig.user_name, _dbConfig.user_value, "application/json");

                        await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + "/_node/nonode@nohost/_config/cors/credentials", $"\"true\"", _dbConfig.user_name, _dbConfig.user_value, "application/json");

                        await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + "/_node/nonode@nohost/_config/cors/headers", $"\"accept, authorization, content-type, origin, referer, cache-control, x-requested-with\"", _dbConfig.user_name, _dbConfig.user_value, "application/json");

                        await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + "/_node/nonode@nohost/_config/cors/methods", $"\"GET, PUT, POST, HEAD, DELETE\"", _dbConfig.user_name, _dbConfig.user_value, "application/json");

                        await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + "/_users", null, _dbConfig.user_name, _dbConfig.user_value, "application/json");
                        await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + "/_replicator", null, _dbConfig.user_name, _dbConfig.user_value, "application/json");
                        await _couchDbHttpClient.ExecuteAsync("PUT", _dbConfig.url + "/_global_changes", null, _dbConfig.user_name, _dbConfig.user_value, "application/json");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Check_DB_Install Failed configuration \n{ex}");
                }
            }
            //Console.WriteLine($"Starup/Install Check - end {System.DateTime.Now}");
            
            Context.Stop(Self);
        });
    }


    bool url_endpoint_exists (string p_target_server, string p_user_name, string p_value, string p_method = "HEAD")
    {
        try
        {
            var httpClientFactory = new mmria.common.SimpleHttpClientFactory();
            using var httpClient = httpClientFactory.CreateClient(string.Empty);
            using var request = new System.Net.Http.HttpRequestMessage(
                p_method == "HEAD" ? System.Net.Http.HttpMethod.Head : System.Net.Http.HttpMethod.Get,
                p_target_server
            );

            if (!string.IsNullOrWhiteSpace(p_user_name) && !string.IsNullOrWhiteSpace(p_value))
            {
                request.Headers.Authorization = mmria.common.getset.CouchDbHttpClient.CreateBasicAuthHeaderValue(p_user_name, p_value);
            }

            using var response = httpClient.Send(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception) 
        {
            //Log.Information ($"failed end_point exists check: {p_target_server}\n{ex}");
            Console.WriteLine($"failed end_point exists check: {p_target_server}");
            return false;
        }            
    }

}
