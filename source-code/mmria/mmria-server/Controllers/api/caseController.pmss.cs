#if IS_PMSS_ENHANCED
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
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace mmria.pmss.server;


[Route("api/[controller]")]
public sealed class caseController: ControllerBase 
{ 
    ActorSystem _actorSystem;	

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    private readonly IAuthorizationService _authorizationService;
    //private readonly IDocumentRepository _documentRepository;

    public caseController
    ( 
        IHttpContextAccessor httpContextAccessor,
        mmria.common.couchdb.OverridableConfiguration p_configuration, 
        ActorSystem actorSystem, 
        IAuthorizationService authorizationService
    )
    {
         configuration = p_configuration;
        _actorSystem = actorSystem;
        _authorizationService = authorizationService;

        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        db_config = configuration.GetDBConfig(host_prefix);
    }
    
    [Authorize(Roles  = "abstractor, data_analyst, committee_member, vro")]
    [HttpGet]
    public async Task<mmria.case_version.pmss.v230616.mmria_case> Get(string case_id) 
    { 
        try
        {
            string request_string = db_config.Get_Prefix_DB_Url("mmrds/_all_docs?include_docs=true");

            if (!string.IsNullOrWhiteSpace (case_id)) 
            {
                request_string = db_config.Get_Prefix_DB_Url($"mmrds/{case_id}");
                var case_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
                string responseFromServer = await case_curl.executeAsync();

                //var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.case_version.pmss.v230616.mmria_case> (responseFromServer);

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    //PropertyNamingPolicy = new UpperCaseNamingPolicy(),
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                    WriteIndented = true
                };


                var json_doc = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonDocument>(responseFromServer, options);
                var result = new mmria.case_version.pmss.v230616.mmria_case();
                result.Convert(json_doc.RootElement);
                
                

                if(mmria.pmss.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.ReadCase, result))
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





    [Authorize(Roles  = "abstractor, committee_member, vro")]
    [HttpPost]
    public async Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] mmria.common.model.couchdb.Save_Case_Request<mmria.case_version.pmss.v230616.mmria_case> save_case_request
    ) 
    { 

        var case_post_request = save_case_request.Case_Data;

        string auth_session_token = null;

        string object_string = null;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {
            var pmssno = "";

            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }


            if(string.IsNullOrWhiteSpace(case_post_request.created_by))
            {
                case_post_request.created_by = userName;
            } 

            case_post_request.last_updated_by = userName;
                



            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            object_string = Newtonsoft.Json.JsonConvert.SerializeObject(case_post_request, settings);

            
            var temp_id = case_post_request._id; 
            string id_val = null;

            if(DateTime.TryParse(temp_id, out var temp_id_date_time))
            {
                id_val = string.Concat(temp_id_date_time.ToString("s"), "Z");
            }
            else
            {
                id_val = temp_id.ToString();

                var is_match = System.Text.RegularExpressions.Regex.IsMatch
                (
                    id_val, 
                    @"^[0-9a-fA-F][0-9a-fA-F/-]+[0-9a-fA-F]$"
                );	

                if(! is_match)
                {
                    return result;
                }
            }

            if(string.IsNullOrWhiteSpace(case_post_request.tracking.admin_info.jurisdiction))
            {
                case_post_request.tracking.admin_info.jurisdiction = "/";
            }

            pmssno = case_post_request.tracking.admin_info.pmssno;
            

            if(!mmria.pmss.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.WriteCase, case_post_request.tracking.admin_info.case_folder))
            {
                result.error_description = $"unauthorized PUT {case_post_request.tracking.admin_info.jurisdiction}: {case_post_request._id}";
                Console.Write($"unauthorized PUT {case_post_request.tracking.admin_info.jurisdiction}: {case_post_request._id}");
                return result;
            }


            // begin - check if doc exists
            try 
            {
                var check_document_curl = new mmria.server.cURL ("GET", null, db_config.Get_Prefix_DB_Url($"mmrds/{id_val}"), null,db_config.user_name, db_config.user_value);
                string check_document_json = await check_document_curl.executeAsync ();
                var case_object = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.case_version.pmss.v230616.mmria_case> (check_document_json);

                if
                (
                    !mmria.pmss.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.WriteCase, case_object)
                )
                {
                    result.error_description = $"unauthorized PUT: {case_object._id}";
                    Console.Write($"unauthorized PUT: {case_object._id}");
                    return result;
                }

            } 
            catch (Exception ex) 
            {
                // do nothing for now document doesn't exsist.
                System.Console.WriteLine ($"err caseController.Post\n{ex}");
            }
            // end - check if doc exists




            string metadata_url = db_config.Get_Prefix_DB_Url($"mmrds/{id_val}");
            mmria.server.cURL document_curl = new mmria.server.cURL ("PUT", null, metadata_url, object_string,db_config.user_name, db_config.user_value);
            
            string save_response_from_server = null;
            try
            {
                save_response_from_server = await document_curl.executeAsync();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(save_response_from_server);
            }
            catch(Exception ex)
            {
                result.error_description = ex.ToString();
                Console.Write("auth_session_token: {0}", auth_session_token);
                Console.WriteLine(ex);
            }

            if (!result.ok  && string.IsNullOrWhiteSpace(result.error_description))
            {
                result.error_description = save_response_from_server;
                Console.Write($"save failed for: {id_val}");
                Console.Write($"save_response:\n{save_response_from_server}");
            }


            var audit_data = save_case_request.Change_Stack;
            audit_data.record_id = pmssno;
            audit_data.metadata_version = configuration.GetString("metadata_version", host_prefix);

            var audit_string = Newtonsoft.Json.JsonConvert.SerializeObject(audit_data, settings);

            string audit_url = db_config.Get_Prefix_DB_Url($"audit/{audit_data._id}");
            mmria.server.cURL audit_curl = new mmria.server.cURL ("PUT", null, audit_url, audit_string,db_config.user_name, db_config.user_value);

            try
            {
                string responseFromServer = await audit_curl.executeAsync();
                var audit_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
            catch(Exception ex)
            {
                Console.Write("problem saving audit\n{0}", ex);

            }

            var Sync_Document_Message = new mmria.pmss.server.model.actor.Sync_Document_Message
            (
                id_val,
                object_string,
                "PUT",
                configuration.GetString("metadata_version", host_prefix)
            );

            _actorSystem.ActorOf(Props.Create<mmria.pmss.server.model.actor.Synchronize_Case>(db_config)).Tell(Sync_Document_Message);
    
            /*
            var case_sync_actor = _actorSystem.ActorSelection("akka://mmria-actor-system/user/case_sync_actor");
            case_sync_actor.Tell(Sync_Document_Message);
            */


        }
        catch(Exception ex) 
        {
            Console.Write("auth_session_token: {0}", auth_session_token);
            Console.WriteLine (ex);
        }

        return result;

    } 

    [Authorize(Roles  = "abstractor")]
    [HttpDelete]
    public async Task<mmria.case_version.pmss.v230616.mmria_case> Delete(string case_id = null, string rev = null) 
    { 
        try
        {

            var pmssno = "";
            var first_name = "";
            var last_name = "";

            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }

            string request_string = null;
            //mmria.pmss.server.utils.c_sync_document sync_document = null;

            if (!string.IsNullOrWhiteSpace (case_id) && !string.IsNullOrWhiteSpace (rev)) 
            {
                request_string = db_config.Get_Prefix_DB_Url($"mmrds/{case_id}?rev={rev}");
            }
            else 
            {
                return null;
            }

            var delete_report_curl = new mmria.server.cURL ("DELETE", null, request_string, null,db_config.user_name, db_config.user_value);
            var check_document_curl = new mmria.server.cURL ("GET", null, db_config.Get_Prefix_DB_Url($"mmrds/{case_id}"), null,db_config.user_name, db_config.user_value);

            string document_json = null;
            // check if doc exists
            try 
            {
                
                document_json = await check_document_curl.executeAsync ();
                var mmria_case = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.case_version.pmss.v230616.mmria_case> (document_json);
                
                if
                (
                    !mmria.pmss.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.WriteCase, mmria_case)
                )
                {
                    Console.Write($"unauthorized DELETE {mmria_case.tracking.admin_info.jurisdiction}: {mmria_case._id}");
                    return null;
                }
                
                
                if (!string.IsNullOrWhiteSpace(mmria_case._rev)) 
                {
                    request_string = db_config.Get_Prefix_DB_Url($"mmrds/{case_id}?rev={mmria_case._rev}");
                }

                pmssno = mmria_case.tracking.admin_info.pmssno;
/*
                if(admin_info.ContainsKey("first_name"))
                first_name = tracking["first_name"].ToString();

                if(admin_info.ContainsKey("last_name"))
                last_name = admin_info["last_name"].ToString();
                */
                
            } 
            catch (Exception ex) 
            {
                // do nothing for now document doesn't exsist.
                System.Console.WriteLine ($"err caseController.Delete\n{ex}");
            }

            string responseFromServer = await delete_report_curl.executeAsync ();;
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.case_version.pmss.v230616.mmria_case> (responseFromServer);

            var audit_data = new mmria.common.model.couchdb.Change_Stack()
            {
                _id = System.Guid.NewGuid().ToString(),
                case_id = case_id,
                case_rev = rev,

                record_id = pmssno,
                is_delete = true,
                delete_rev = rev,

                user_name = userName,
                first_name = first_name,
                last_name = last_name,

                note = "deleted case",

                metadata_version = configuration.GetString("metadata_version", host_prefix),
                date_created = DateTime.UtcNow,
            };

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
 

            var audit_string = Newtonsoft.Json.JsonConvert.SerializeObject(audit_data, settings);

            string audit_url = db_config.Get_Prefix_DB_Url($"audit/{audit_data._id}");
            mmria.server.cURL audit_curl = new mmria.server.cURL ("PUT", null, audit_url, audit_string,db_config.user_name, db_config.user_value);

            try
            {
                string save_delete_audit_response = await audit_curl.executeAsync();
                var audit_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(save_delete_audit_response);
            }
            catch(Exception ex)
            {
                Console.Write("problem saving audit\n{0}", ex);

            }



            if(! string.IsNullOrWhiteSpace(document_json))
            {
                var Sync_Document_Message = new mmria.pmss.server.model.actor.Sync_Document_Message
                (
                    case_id,
                    document_json,
                    "DELETE",
                    configuration.GetString("metadata_version", host_prefix)
                );

                _actorSystem.ActorOf(Props.Create<mmria.pmss.server.model.actor.Synchronize_Case>(db_config)).Tell(Sync_Document_Message);
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

/*
    public async Task<IActionResult> OnGetAsync(string documentId)
    {
        Document = _documentRepository.Find(documentId);

        if (Document == null)
        {
            return new NotFoundResult();
        }

        var authorizationResult = await _authorizationService
                .AuthorizeAsync(User, Document, "EditPolicy");

        if (authorizationResult.Succeeded)
        {
            return Page();
        }
        else if (User.Identity.IsAuthenticated)
        {
            return new ForbidResult();
        }
        else
        {
            return new ChallengeResult();
        }
    } 
*/

} 


#endif