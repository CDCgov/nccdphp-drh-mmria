#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Dynamic;
using mmria.common;
using Microsoft.Extensions.Configuration;
using Akka.Actor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server;
	
[Route("api/[controller]")]
public sealed class vital_importController: ControllerBase 
{ 


    private ActorSystem _actorSystem;


    private readonly IAuthorizationService _authorizationService;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public vital_importController
    (
        ActorSystem actorSystem, 
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        _actorSystem = actorSystem;
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    private bool is_authorized()
    {
        var result = false;

        var vital_service_key = configuration.GetString("vital_service_key", host_prefix);
        
        if
        (
            (
                !this.Request.Headers.ContainsKey("vitals_service_key") ||
                string.IsNullOrWhiteSpace(vital_service_key)
            ) &&
            this.Request.Headers["vitals_service_key"] != vital_service_key
        )
        {
            result = false;
        }
        else
        {
            result = true;
        }

        return result;
    }
    
    [AllowAnonymous]
    [HttpGet("view")]
    public async Task<mmria.common.model.couchdb.case_view_response> GetCaseView
    (

        string search_key

    )
    {
        if ( !is_authorized() )
        {
            return null;
        }

        var couchdb_max_take_value = 268_435_456;
        int skip = 0;
        int take = couchdb_max_take_value;
        string sort = "by_last_name";
        bool descending = false;


        string sort_view = sort.ToLower ();
        switch (sort_view)
        {
            case "by_date_created":
            case "by_date_last_updated":
            case "by_last_name":
            case "by_first_name":
            case "by_middle_name":
            case "by_year_of_death":
            case "by_month_of_death":
            case "by_committee_review_date":
            case "by_created_by":
            case "by_last_updated_by":
            case "by_state_of_death":
            case "by_date_last_checked_out":
            case "by_last_checked_out_by":
            
            case "by_case_status":
                break;

            default:
                sort_view = "by_date_created";
            break;
        }



        try
        {
            System.Text.StringBuilder request_builder = new System.Text.StringBuilder ();
            request_builder.Append ($"{db_config.url}/{db_config.prefix}mmrds/_design/sortable/_view/{sort_view}?");

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


            string request_string = request_builder.ToString();
            var case_view_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = case_view_curl.execute();

            mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

            
            string key_compare = search_key.ToLower ().Trim (new char [] { '"' });

            mmria.common.model.couchdb.case_view_response result = new mmria.common.model.couchdb.case_view_response();
            result.offset = case_view_response.offset;
            result.total_rows = case_view_response.total_rows;

            foreach(mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
            {
                bool add_item = false;

                if(is_matching_search_text(cvi.value.last_name, key_compare))
                {
                    add_item = true;
                }

                if(add_item)
                {
                    result.rows.Add (cvi);
                }
            
            }


            result.total_rows = result.rows.Count;
            result.rows =  result.rows.Skip (skip).Take (take).ToList ();

            return result;
            
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        }


        return null;
    }

    private bool is_matching_search_text(string p_val1, string p_val2)
    {
        var result = false;

        if 
        (
            !string.IsNullOrWhiteSpace(p_val1) && 
            p_val1.Length > 3 &&
            (
                p_val2.IndexOf (p_val1, StringComparison.OrdinalIgnoreCase) > -1 ||
                p_val1.IndexOf (p_val2, StringComparison.OrdinalIgnoreCase) > -1
            )
        )
        {
            result = true;
        }

        return result;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<System.Dynamic.ExpandoObject> Get(string case_id) 
    { 
        if ( !is_authorized() )
        {
            return null;
        }

        try
        {
            string request_string = $"{db_config.url}/{db_config.prefix}mmrds/_all_docs?include_docs=true";

            if (!string.IsNullOrWhiteSpace (case_id)) 
            {
                request_string = $"{db_config.url}/{db_config.prefix}mmrds/{case_id}";
                var case_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
                string responseFromServer = await case_curl.executeAsync();

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (responseFromServer);

                if(mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.server.utils.ResourceRightEnum.ReadCase, result))
                {
                    return result;
                }
                else
                {
                    return null;
                }

            } 

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    } 




    [AllowAnonymous]
    [HttpPost]
    public async Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] System.Dynamic.ExpandoObject case_post_request
    ) 
    { 

        if ( !is_authorized() )
        {
            return null;
        }

        string auth_session_token = null;

        string object_string = null;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();


        try
        {

            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }

            var byName = (IDictionary<string,object>)case_post_request;
            var created_by = byName["created_by"] as string;
            if(string.IsNullOrWhiteSpace(created_by))
            {
                byName["created_by"] = userName;
            } 

            if(byName.ContainsKey("last_updated_by"))
            {
                byName["last_updated_by"] = userName;
            }
            else
            {
                byName.Add("last_updated_by", userName);
                
            }


            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            object_string = Newtonsoft.Json.JsonConvert.SerializeObject(case_post_request, settings);

            
            var temp_id = byName["_id"]; 
            string id_val = null;

            if(temp_id is DateTime)
            {
                id_val = string.Concat(((DateTime)temp_id).ToString("s"), "Z");
            }
            else
            {
                id_val = temp_id.ToString();
            }



            var home_record = (IDictionary<string,object>)byName["home_record"];
            if(!home_record.ContainsKey("jurisdiction_id"))
            {
                home_record.Add("jurisdiction_id", "/");
            }

            if(!mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.server.utils.ResourceRightEnum.WriteCase, home_record["jurisdiction_id"].ToString()))
            {
                Console.Write($"unauthorized PUT {home_record["jurisdiction_id"]}: {byName["_id"]}");
                return result;
            }


            // begin - check if doc exists
            try 
            {
                var check_document_curl = new cURL ("GET", null, $"{db_config.url}/{db_config.prefix}mmrds/{id_val}", null, db_config.user_name, db_config.user_value);
                string check_document_json = await check_document_curl.executeAsync ();
                var check_document_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (check_document_json);
                IDictionary<string, object> result_dictionary = check_document_expando_object as IDictionary<string, object>;

                if
                (
                    result_dictionary != null && 
                    !mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.server.utils.ResourceRightEnum.WriteCase, check_document_expando_object)
                )
                {
                    Console.Write($"unauthorized PUT {result_dictionary["jurisdiction_id"]}: {result_dictionary["_id"]}");
                    return result;
                }

            } 
            catch (Exception ex) 
            {
                // do nothing for now document doesn't exsist.
                System.Console.WriteLine ($"err caseController.Post\n{ex}");
            }
            // end - check if doc exists




            string metadata_url = $"{db_config.url}/{db_config.prefix}mmrds/{id_val}";
            cURL document_curl = new cURL ("PUT", null, metadata_url, object_string, db_config.user_name, db_config.user_value);

            try
            {
                string responseFromServer = await document_curl.executeAsync();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
            catch(Exception ex)
            {
                Console.Write("auth_session_token: {0}", auth_session_token);
                Console.WriteLine(ex);
            }

            var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message
            (
                id_val,
                object_string,
                "PUT",
                configuration.GetString("metadata_version", host_prefix)
            );

            _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config)).Tell(Sync_Document_Message);
    
            /*
            var case_sync_actor = _actorSystem.ActorSelection("akka://mmria-actor-system/user/case_sync_actor");
            case_sync_actor.Tell(Sync_Document_Message);
            */
            if (!result.ok)
            {

            }

        }
        catch(Exception ex) 
        {
            Console.Write("auth_session_token: {0}", auth_session_token);
            Console.WriteLine (ex);
        }

        return result;

    } 

    [AllowAnonymous]
    [HttpDelete]
    public async Task<System.Dynamic.ExpandoObject> Delete(string case_id = null, string rev = null) 
    { 

        if ( !is_authorized() )
        {
            return null;
        }

        try
        {

            
            string request_string = null;
            //mmria.server.utils.c_sync_document sync_document = null;

            if (!string.IsNullOrWhiteSpace (case_id) && !string.IsNullOrWhiteSpace (rev)) 
            {
                request_string = db_config.url + $"/{db_config.prefix}mmrds/" + case_id + "?rev=" + rev;
            }
            else 
            {
                return null;
            }

            var delete_report_curl = new cURL ("DELETE", null, request_string, null, db_config.user_name, db_config.user_value);
            var check_document_curl = new cURL ("GET", null, db_config.url + $"/{db_config.prefix}mmrds/" + case_id, null, db_config.user_name, db_config.user_value);

            string document_json = null;
            // check if doc exists
            try 
            {
                
                document_json = await check_document_curl.executeAsync ();
                var check_docuement_curl_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (document_json);
                IDictionary<string, object> result_dictionary = check_docuement_curl_result as IDictionary<string, object>;
                
                if
                (
                    result_dictionary != null && 
                    !mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.server.utils.ResourceRightEnum.WriteCase, check_docuement_curl_result)
                )
                {
                    Console.Write($"unauthorized DELETE {result_dictionary["jurisdiction_id"]}: {result_dictionary["_id"]}");
                    return null;
                }
                
                
                if (result_dictionary.ContainsKey ("_rev")) 
                {
                    request_string = db_config.url + $"/{db_config.prefix}mmrds/" + case_id + "?rev=" + result_dictionary ["_rev"];
                    //System.Console.WriteLine ("json\n{0}", object_string);
                }
            } 
            catch (Exception ex) 
            {
                // do nothing for now document doesn't exsist.
                System.Console.WriteLine ($"err caseController.Delete\n{ex}");
            }

            string responseFromServer = await delete_report_curl.executeAsync ();;
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (responseFromServer);


            if(! string.IsNullOrWhiteSpace(document_json))
            {
                var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message
                (
                    case_id,
                    document_json,
                    "DELETE",
                    configuration.GetString("metadata_version", host_prefix)
                );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config)).Tell(Sync_Document_Message);
                /*
                var case_sync_actor = _actorSystem.ActorSelection("akka://mmria-actor-system/user/case_sync_actor");
                case_sync_actor.Tell(Sync_Document_Message);
                */

            }
            return result;

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }

} 


#endif