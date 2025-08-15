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
using mmria.common.model.couchdb.recover_doc;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;  

namespace mmria.server;
	
[Route("api/[controller]")]
public sealed class caseRevisionController: ControllerBase 
{ 
    private ActorSystem _actorSystem;

	//IHttpContextAccessor _accessor;


    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;

    string host_prefix = null;

    private readonly IAuthorizationService _authorizationService;
    //private readonly IDocumentRepository _documentRepository;

    public caseRevisionController
    (
        IHttpContextAccessor httpContextAccessor,
        ActorSystem actorSystem, 
        IAuthorizationService authorizationService, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        _actorSystem = actorSystem;
        _authorizationService = authorizationService;
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        db_config = configuration.GetDBConfig(host_prefix);
    }
    
    [Authorize(Roles  = "installation_admin")]
    [HttpGet]
    public async Task<System.Dynamic.ExpandoObject> Get(string jurisdiction_id, string case_id, string revision_id) 
    { 
        try
        {
            var config = configuration.GetDBConfig(jurisdiction_id);

            string all_revs_url = $"{config.url}/{config.prefix}mmrds/{case_id}?rev={revision_id}";

            if (!string.IsNullOrWhiteSpace (case_id)) 
            {

                var case_curl = new cURL("GET", null, all_revs_url, null, config.user_name, config.user_value);
                string responseFromServer = case_curl.execute();

               
                
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);

                return result;

            } 

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    } 


    [Authorize(Roles  = "installation_admin")]
    [HttpPost]
    public async Task<mmria.common.model.couchdb.document_put_response> Post
    (
        string jurisdiction_id, 
        string case_id, 
        string revision_id
    ) 
    { 

        return null;

        /*

        var case_post_request = save_case_request.Case_Data;

        string auth_session_token = null;

        string object_string = null;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();


        try
        {

            var config = ConfigDB.detail_list[jurisdiction_id];

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

            if(!mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(User, mmria.server.utils.ResourceRightEnum.WriteCase, home_record["jurisdiction_id"].ToString()))
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
                    !mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(User, mmria.server.utils.ResourceRightEnum.WriteCase, check_document_expando_object)
                )
                {
                    Console.Write($"unauthorized PUT {result_dictionary["jurisdiction_id"]}: {result_dictionary["_id"]}");
                    return result;
                }

            } 
            catch (Exception ex) 
            {
                // do nothing for now document doesn't exsist.
                System.Console.WriteLine ($"err caseRevisionController.Post\n{ex}");
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


            var audit_data = save_case_request.Change_Stack;

            var audit_string = Newtonsoft.Json.JsonConvert.SerializeObject(audit_data, settings);

            string audit_url = $"{db_config.url}/{db_config.prefix}audit/{audit_data._id}";
            cURL audit_curl = new cURL ("PUT", null, audit_url, audit_string, db_config.user_name, db_config.user_value);

            try
            {
                string responseFromServer = await audit_curl.executeAsync();
                var audit_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
            catch(Exception ex)
            {
                Console.Write("problem saving audit\n{0}", ex);

            }

            var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message
            (
                id_val,
                    object_string
            );

            _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>()).Tell(Sync_Document_Message);
    
            /*
            var case_sync_actor = _actorSystem.ActorSelection("akka://mmria-actor-system/user/case_sync_actor");
            case_sync_actor.Tell(Sync_Document_Message);
            * /
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
        */

    } 

    
} 


