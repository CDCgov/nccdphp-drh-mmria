using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 

namespace mmria.server;

[Route("api/powerbi-measures/{indicator_id?}")]
public sealed class powerbi_measureController: ControllerBase
{ 

    public struct Result_Struct
    {
        public mmria.server.model.c_opioid_report_object[] docs;
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
    public powerbi_measureController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }


    [AllowAnonymous] 
    [HttpGet]
    public async Task<Result_Struct> Get(string indicator_id)
    {
        Result_Struct result = new Result_Struct();
        result.docs = new List<mmria.server.model.c_opioid_report_object>().ToArray();
        
        var config_couchdb_url = db_config.url;
        var config_timer_user_name = db_config.user_name;
        var config_timer_value = db_config.user_value;
        var config_db_prefix = db_config.prefix;

        try
        {
            var selector_struc = new Selector_Struc();
            selector_struc.selector = new System.Collections.Generic.Dictionary<string,System.Collections.Generic.Dictionary<string,string>>(StringComparer.OrdinalIgnoreCase);
            selector_struc.limit = 10000;
            selector_struc.selector.Add("_id", new System.Collections.Generic.Dictionary<string,string>(StringComparer.OrdinalIgnoreCase));
            selector_struc.selector["_id"].Add("$regex", "^powerbi");
            selector_struc.use_index = "powerbi-report-index";
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string selector_struc_string = Newtonsoft.Json.JsonConvert.SerializeObject (selector_struc, settings);

            //System.Console.WriteLine(selector_struc_string);

            string find_url = $"{config_couchdb_url}/{config_db_prefix}report/_find";

            var case_curl = new cURL("POST", null, find_url, selector_struc_string, config_timer_user_name, config_timer_value);
            string responseFromServer = await case_curl.executeAsync();
            
            if(!string.IsNullOrWhiteSpace(indicator_id))
            {

                List<mmria.server.model.c_opioid_report_object> new_list = new();
                var response_result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result_Struct>(responseFromServer);

                var regex = new System.Text.RegularExpressions.Regex("^" + indicator_id);
                foreach(var doc in response_result.docs)
                {
                    var new_data = new System.Collections.Generic.List<mmria.server.model.opioid_report_value_struct>();
                    foreach(var item in doc.data)
                    {
                        if
                        (
                            item.indicator_id == indicator_id &&
                            item.value > 0
                        )
                        {
                            new_data.Add(item);
                        }
                    }

                    doc.data = new_data;
                    
                }

                result.docs = response_result.docs;
            }
            else
            {
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result_Struct>(responseFromServer);
            }

            //System.Console.WriteLine($"case_response.docs.length {result.docs.Length}");
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }
} 


