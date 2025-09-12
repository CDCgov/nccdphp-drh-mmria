using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Serilog;
using Serilog.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server;

[Route("api/[controller]")]
public sealed class user_role_jurisdictionController: ControllerBase 
{ 
     IHttpContextAccessor httpContextAccessor;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public user_role_jurisdictionController
    (
        IHttpContextAccessor p_httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        httpContextAccessor = p_httpContextAccessor;
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<IList<mmria.common.model.couchdb.user_role_jurisdiction>> Get(string p_urj_id)
    {
        //Log.Information  ("Recieved message.");
        var result = new List<mmria.common.model.couchdb.user_role_jurisdiction>();

        try
        {
            var User = httpContextAccessor.HttpContext.User;
            
            string jurisdiction_url = db_config.url + $"/{db_config.prefix}jurisdiction/" + p_urj_id;
            if(string.IsNullOrWhiteSpace(p_urj_id))
            {
                jurisdiction_url = db_config.url + $"/{db_config.prefix}jurisdiction/_all_docs?include_docs=true";

                var case_curl = new cURL("GET", null, jurisdiction_url, null, db_config.user_name, db_config.user_value);
                string responseFromServer = await case_curl.executeAsync();

                var user_role_list = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<mmria.common.model.couchdb.user_role_jurisdiction>> (responseFromServer);

                #if !IS_PMSS_ENHANCED
                foreach(var row in user_role_list.rows)
                {
                    var user_role_jurisdiction = row.doc;

                    if
                    (
                        user_role_jurisdiction.data_type != null &&
                        user_role_jurisdiction.data_type == mmria.common.model.couchdb.user_role_jurisdiction.user_role_jursidiction_const &&
                        mmria.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.server.utils.ResourceRightEnum.ReadUser, user_role_jurisdiction))
                    {
                        result.Add(user_role_jurisdiction);
                    }						
                }
                #endif
                #if IS_PMSS_ENHANCED
                foreach(var row in user_role_list.rows)
                {
                    var user_role_jurisdiction = row.doc;

                    if
                    (
                        user_role_jurisdiction.data_type != null &&
                        user_role_jurisdiction.data_type == mmria.common.model.couchdb.user_role_jurisdiction.user_role_jursidiction_const &&
                        mmria.pmss.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.ReadUser, user_role_jurisdiction))
                    {
                        result.Add(user_role_jurisdiction);
                    }						
                }
                #endif
                    
            }
            else
            {
                jurisdiction_url = db_config.url + $"/{db_config.prefix}jurisdiction/" + p_urj_id;	
                var case_curl = new cURL("GET", null, jurisdiction_url, null, db_config.user_name, db_config.user_value);
                string responseFromServer = await case_curl.executeAsync();

                var user_role_jurisdiction = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.user_role_jurisdiction> (responseFromServer);

                #if !IS_PMSS_ENHANCED
                if
                (
                    user_role_jurisdiction.data_type != null &&
                    user_role_jurisdiction.data_type == mmria.common.model.couchdb.user_role_jurisdiction.user_role_jursidiction_const &&
                    mmria.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.server.utils.ResourceRightEnum.ReadUser, user_role_jurisdiction)
                )
                {
                    result.Add(user_role_jurisdiction);
                }
                #endif
                #if IS_PMSS_ENHANCED
                if
                (
                    user_role_jurisdiction.data_type != null &&
                    user_role_jurisdiction.data_type == mmria.common.model.couchdb.user_role_jurisdiction.user_role_jursidiction_const &&
                    mmria.pmss.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.ReadUser, user_role_jurisdiction)
                )
                {
                    result.Add(user_role_jurisdiction);
                }
                #endif

            }
            

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }


    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] mmria.common.model.couchdb.user_role_jurisdiction user_role_jurisdiction
    ) 
    { 
        string user_role_jurisdiction_json;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {
            #if !IS_PMSS_ENHANCED
            if(!mmria.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.server.utils.ResourceRightEnum.WriteUser, user_role_jurisdiction))
            {
                return null;
            }
            #endif
            #if IS_PMSS_ENHANCED
            if(!mmria.pmss.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.WriteUser, user_role_jurisdiction))
            {
                return null;
            }
            #endif

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            user_role_jurisdiction_json = Newtonsoft.Json.JsonConvert.SerializeObject(user_role_jurisdiction, settings);

            string jurisdiction_tree_url = db_config.url + $"/{db_config.prefix}jurisdiction/" + user_role_jurisdiction._id;

            cURL document_curl = new cURL ("PUT", null, jurisdiction_tree_url, user_role_jurisdiction_json, db_config.user_name, db_config.user_value);

            try
            {
                string responseFromServer = await document_curl.executeAsync();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
            catch(Exception ex)
            {
                Log.Information ($"jurisdiction_treeController:{ex}");
            }


            if (!result.ok) 
            {

            }

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }
            
        return result;
    }

    [HttpPost("bulk")]
    public async System.Threading.Tasks.Task<List<mmria.common.model.couchdb.document_put_response>> PostBulk
    (
        [FromBody] List<mmria.common.model.couchdb.user_role_jurisdiction> user_role_jurisdictions
    ) 
    { 
        List<mmria.common.model.couchdb.document_put_response> results = new List<mmria.common.model.couchdb.document_put_response>();

        try
        {
            #if !IS_PMSS_ENHANCED
            foreach (var user_role_jurisdiction in user_role_jurisdictions)
            {
                if(!mmria.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.server.utils.ResourceRightEnum.WriteUser, user_role_jurisdiction))
                {
                    return null;
                }
            }
            #endif
            #if IS_PMSS_ENHANCED
            foreach (var user_role_jurisdiction in user_role_jurisdictions)
            {
                if(!mmria.pmss.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.WriteUser, user_role_jurisdiction))
                {
                    return null;
                }
            }
            #endif

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string user_role_jurisdictions_json = Newtonsoft.Json.JsonConvert.SerializeObject(new { docs = user_role_jurisdictions }, settings);

            string bulk_docs_url = db_config.url + $"/{db_config.prefix}jurisdiction/_bulk_docs";

            cURL document_curl = new cURL ("POST", null, bulk_docs_url, user_role_jurisdictions_json, db_config.user_name, db_config.user_value);

            try
            {
                string responseFromServer = await document_curl.executeAsync();
                results = Newtonsoft.Json.JsonConvert.DeserializeObject<List<mmria.common.model.couchdb.document_put_response>>(responseFromServer);
            }
            catch(Exception ex)
            {
                Log.Information ($"jurisdiction_treeController:{ex}");
            }
        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }
            
        return results;
    }


    [HttpDelete]
    public async System.Threading.Tasks.Task<IActionResult> Delete(string _id = null, string rev = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                return BadRequest(new { error = "missing_id" });
            }

            // Prefer authoritative rev from the DB; fall back to client-provided rev when necessary.
            string delete_rev = rev;
            string jurisdiction_get_url = db_config.url + $"/{db_config.prefix}jurisdiction/" + _id;

            var check_document_curl = new cURL("GET", null, jurisdiction_get_url, null, db_config.user_name, db_config.user_value);
            try
            {
                string document_json = await check_document_curl.executeAsync();
                var check_document_curl_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.user_role_jurisdiction>(document_json);

                #if !IS_PMSS_ENHANCED
                if (!mmria.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.server.utils.ResourceRightEnum.WriteUser, check_document_curl_result))
                {
                    return Forbid();
                }
                #endif
                #if IS_PMSS_ENHANCED
                if (!mmria.pmss.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.WriteUser, check_document_curl_result))
                {
                    return Forbid();
                }
                #endif

                if (!string.IsNullOrWhiteSpace(check_document_curl_result._rev))
                {
                    delete_rev = check_document_curl_result._rev; // prefer DB rev
                }
            }
            catch (Exception ex)
            {
                // If GET failed, surface a 502 so caller knows the backing DB couldn't be read.
                Log.Information($"user_role_jurisdictionController.Delete: error fetching doc {_id}: {ex}");
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "failed_to_fetch_document" });
            }

            if (string.IsNullOrWhiteSpace(delete_rev))
            {
                return BadRequest(new { error = "missing_rev" });
            }

            string request_string = db_config.url + $"/{db_config.prefix}jurisdiction/" + _id + "?rev=" + delete_rev;

            var delete_report_curl = new cURL("DELETE", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer;
            try
            {
                responseFromServer = await delete_report_curl.executeAsync();
            }
            catch (Exception ex)
            {
                Log.Information($"user_role_jurisdictionController.Delete: error deleting doc {_id}: {ex}");
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "failed_to_delete_document" });
            }

            // Return the raw couch response as JSON so client `response.ok` checks continue to work.
            try
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Information($"user_role_jurisdictionController.Delete: failed to deserialize delete response for {_id}: {ex}");
                // fallback: return raw string
                return Ok(new { ok = true, raw = responseFromServer });
            }
        }
        catch (Exception ex)
        {
            Log.Information($"user_role_jurisdictionController.Delete: unexpected error: {ex}");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "internal_error" });
        }
    }


} 

