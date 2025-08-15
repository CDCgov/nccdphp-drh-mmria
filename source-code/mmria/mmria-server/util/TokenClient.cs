using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace mmria.server.utils;

public sealed class TokenClient
{
    private IConfiguration _configuration;
        string sams_endpoint_authorization;
        string sams_endpoint_token;
        string sams_endpoint_user_info;
        string sams_endpoint_token_validation;
        string sams_endpoint_user_info_sys;
        string sams_client_id;
        string sams_callback_url;  

        string sams_client_secret;

    public TokenClient(IConfiguration configuration)
    {
        _configuration = configuration;

        sams_endpoint_authorization = _configuration["sams:endpoint_authorization"];
        sams_endpoint_token = _configuration["sams:endpoint_token"];
        sams_endpoint_user_info = _configuration["sams:endpoint_user_info"];
        sams_endpoint_token_validation = _configuration["sams:token_validation"];
        sams_endpoint_user_info_sys = _configuration["sams:user_info_sys"];
        sams_client_id = _configuration["sams:client_id"];
        sams_callback_url = _configuration["sams:callback_url"];   
        sams_client_secret = _configuration["sams:client_secret"];
    }
    public async Task<(string id_token, string access_token)> Get_Id_And_Access_Token
    (
        string code,
        string state
    )
    {

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
        var scope = payload.Value<string>("scope");



        var id_token = payload.Value<string>("id_token");

        return (id_token, access_token);

    }

    public async Task<string> get_user_info_sys_request
    (
        string id_token,
        string access_token
    )
    {
        HttpClient client = new HttpClient();
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



        var response = await client.SendAsync(user_info_sys_request);

        //return response;

        response.EnsureSuccessStatusCode();

        var temp_string = await response.Content.ReadAsStringAsync();

        return temp_string;
    }



    public struct refresh_token_struct
    {
        public string access_token;
        public int expires_in;
        public string refresh_token;

        public bool is_error;
    }

    public async Task<refresh_token_struct> get_refresh_token
    (
        string p_access_token,
        string p_refresh_token   
    )
    {
/*
https://auth0.com/learn/refresh-tokens/


*/
        refresh_token_struct result = new refresh_token_struct();

        HttpClient client = new HttpClient();
        var user_refresh_request = new HttpRequestMessage(HttpMethod.Post, sams_endpoint_authorization + $"?refresh_token={p_refresh_token}&grant_type=refresh_token");
        user_refresh_request.Headers.Add("Authorization","Bearer " + p_access_token); 
        user_refresh_request.Headers.Add("client_id", sams_client_id); 
        user_refresh_request.Headers.Add("client_secret", sams_client_secret); 

        var response = await client.SendAsync(user_refresh_request);
        if(response.IsSuccessStatusCode)
        {
            //response.EnsureSuccessStatusCode();

            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            result.access_token = payload.Value<string>("access_token");
            result.expires_in = payload.Value<int>("expires_in");
            result.refresh_token = payload.Value<string>("refresh_token");
        }
        else
        {
            result.is_error = true;
        }

        return result;


/*
https://auth0.com/learn/refresh-tokens/

*/

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

}
