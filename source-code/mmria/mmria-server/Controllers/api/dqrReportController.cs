using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common.functional;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server;

[Authorize(Roles  = "abstractor, data_analyst")]
[Route("api/dqr-detail/{quarter_string}")]
public sealed class dqrReportController: ControllerBase 
{  
    public struct Result_Struct
    {
        public mmria.server.model.dqr.DQRDetail[] docs;
    }

    struct Selector_Struc
    {
        //public System.Dynamic.ExpandoObject selector;
        public System.Collections.Generic.Dictionary<string,System.Collections.Generic.Dictionary<string,string>> selector;
        public string[] fields;

        public string use_index;

        public int limit;
    }

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public dqrReportController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }
    public async Task<Result_Struct> Get(string quarter_string)
    {
        var result = new Result_Struct();
        
        var config_couchdb_url = db_config.url;
        var config_timer_user_name = db_config.user_name;
        var config_timer_value = db_config.user_value;
        var config_db_prefix = db_config.prefix;
        
        try
        {

            var selector_struc = new Selector_Struc();
            selector_struc.selector = new System.Collections.Generic.Dictionary<string,System.Collections.Generic.Dictionary<string,string>>(StringComparer.OrdinalIgnoreCase);
            selector_struc.limit = 10000;
            selector_struc.selector.Add("data_type", new System.Collections.Generic.Dictionary<string,string>(StringComparer.OrdinalIgnoreCase));
            selector_struc.selector["data_type"].Add("$eq", "dqr-detail");
            
            /*
            selector_struc.selector = new System.Collections.Generic.Dictionary<string,System.Collections.Generic.Dictionary<string,string>>(StringComparer.OrdinalIgnoreCase);
            selector_struc.limit = 10000;
            selector_struc.selector.Add("committee_review.pregnancy_relatedness", new System.Collections.Generic.Dictionary<string,string>(StringComparer.OrdinalIgnoreCase));
            selector_struc.selector["committee_review.pregnancy_relatedness"].Add("$eq", p_find_value);
            */


            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string selector_struc_string = Newtonsoft.Json.JsonConvert.SerializeObject (selector_struc, settings);

            string find_url = $"{config_couchdb_url}/{config_db_prefix}report/_find";
            var case_curl = new cURL("POST", null, find_url, selector_struc_string, config_timer_user_name, config_timer_value);
            string responseFromServer = await case_curl.executeAsync();

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result_Struct>(responseFromServer);
            
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }
} 


