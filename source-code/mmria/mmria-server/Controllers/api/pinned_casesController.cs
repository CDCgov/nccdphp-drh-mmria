using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mmria.common.model;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;  

namespace mmria.server.Controllers;

[Route("api/[controller]")]
public sealed class pinned_casesController : ControllerBase
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public pinned_casesController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    [Authorize(Roles = "abstractor")]
    [HttpGet]
    public async Task<mmria.common.model.couchdb.pinned_case_set> Get()
    {
        mmria.common.model.couchdb.pinned_case_set result = await GetPinnedCaseSet();
        return result;
    }

    [Authorize(Roles = "abstractor")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (


    )
    {
        string document_content;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response();

        try
        {
            System.IO.Stream dataStream0 = this.Request.Body;

            System.IO.StreamReader reader0 = new System.IO.StreamReader(dataStream0);

            document_content = await reader0.ReadToEndAsync();

            var pin_case_message = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.pin_case_message>(document_content);

            var pinned_case_set = await GetPinnedCaseSet();

            if(pin_case_message.is_pin)
            {
                pinned_case_set.pin_case(pin_case_message);
            }
            else
            {
                pinned_case_set.unpin_case(pin_case_message);
            }

            result = await SetPinnedCaseSet(pinned_case_set);

            if (!result.ok)
            {

            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    [Authorize(Roles = "jurisdiction_admin")]
    [HttpPut]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Put
    (

    )
    {
        string document_content;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response();

        try
        {
            System.IO.Stream dataStream0 = this.Request.Body;

            System.IO.StreamReader reader0 = new System.IO.StreamReader(dataStream0);

            document_content = await reader0.ReadToEndAsync();

            var pin_case_message = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.pin_case_message>(document_content);

            if(pin_case_message.user_id == "everyone")
            {

                var pinned_case_set = await GetPinnedCaseSet();

                if(pin_case_message.is_pin)
                {
                    pinned_case_set.pin_case(pin_case_message);
                }
                else
                {
                    pinned_case_set.unpin_case(pin_case_message);
                }

                result = await SetPinnedCaseSet(pinned_case_set);

                if (!result.ok)
                {

                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    public static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }


    async Task<mmria.common.model.couchdb.pinned_case_set> GetPinnedCaseSet()
    {

        mmria.common.model.couchdb.pinned_case_set result = null;

        try
        {
            string request_string = db_config.Get_Prefix_DB_Url("jurisdiction/pinned-case-set");
            var case_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_curl.executeAsync();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.pinned_case_set>(responseFromServer);
        }
        catch (System.Net.WebException wex)
        {
            if(wex.Message.Contains("(404) Object Not Found"))
            {
                result = new();

                result.date_created = DateTime.Now;
                result.created_by = "system";
                result.date_last_updated = DateTime.Now;
                result.last_updated_by = "system";


                SetPinnedCaseSet(result);
            }
            else
            {
                Console.WriteLine(wex);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
        
    }

    async Task<mmria.common.model.couchdb.document_put_response> SetPinnedCaseSet
    (
        mmria.common.model.couchdb.pinned_case_set value 
    )
    {
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response();

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;


        var document_content = Newtonsoft.Json.JsonConvert.SerializeObject(value, settings);

        if(value._id == "pinned-case-set")
        {
            string url = db_config.Get_Prefix_DB_Url("jurisdiction/pinned-case-set");
            //System.Console.WriteLine ("json\n{0}", object_string);

            cURL put_document_curl = new cURL("PUT", null, url, document_content, db_config.user_name, db_config.user_value);

            try
            {
                string responseFromServer = await put_document_curl.executeAsync();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
            catch (Exception ex)
            {
            Console.WriteLine(ex);
            }
        }

        return result;
        
    }

}

