#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Dynamic;
using mmria.common;
using mmria.common.utils;
using Microsoft.Extensions.Configuration;
using Akka.Actor;
using Microsoft.AspNetCore.Authorization;

using  mmria.server.extension;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace mmria.server;


[Route("api/[controller]")]
public sealed class caseController: ControllerBase 
{ 
    ActorSystem _actorSystem;	

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.SharedLibraries.Case.Manager.CaseManager _caseManager;

    private readonly IAuthorizationService _authorizationService;
    //private readonly IDocumentRepository _documentRepository;

    public caseController
    ( 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        ActorSystem actorSystem, 
        IAuthorizationService authorizationService,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.Case.Manager.CaseManager caseManager
    )
    {
        configuration = tenantRuntime.RequireConfiguration();
        db_config = tenantRuntime.RequireDbConfig();
        _actorSystem = actorSystem;
        _authorizationService = authorizationService;
        _couchDbHttpClient = couchDbHttpClient;
        _caseManager = caseManager;

        host_prefix = tenantRuntime.EffectiveHostPrefix;
    }
    

    [Authorize(Roles  = "abstractor, data_analyst")]
    [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    //public async Task<System.Dynamic.ExpandoObject> Get(string case_id) 
    public async Task<mmria.case_version.v260120.mmria_case> Get(string case_id) 
    { 
        try
        {
                Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
            return await _caseManager.GetCaseAsync(case_id, db_config, User);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    } 


    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpGet("{case_id}/rev")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetRev(string case_id)
    {
        try
        {
            var sanitizedId = SanitizeSingleLineText(case_id, 256);
            if (string.IsNullOrWhiteSpace(sanitizedId))
                return BadRequest();

            string url = $"{db_config.url}/{db_config.prefix}mmrds/{Uri.EscapeDataString(sanitizedId)}";
            string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
                "GET", url, null, db_config.user_name, db_config.user_value);

            if (string.IsNullOrWhiteSpace(responseFromServer) || responseFromServer.Contains("\"not_found\""))
                return NotFound();

            var doc = Newtonsoft.Json.Linq.JObject.Parse(responseFromServer);
            var id = doc["_id"]?.ToString();
            var rev = doc["_rev"]?.ToString();

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(rev))
                return NotFound();

            var result = new { _id = id, _rev = rev };

            var offlineDate = await GetOfflineDateAsync();
            if (!string.IsNullOrWhiteSpace(offlineDate))
                Response.Headers["X-Offline-Date"] = offlineDate;

            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";

            return mmria.server.util.EscapedJsonResultFactory.Create(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500);
        }
    }

    public sealed class Save_Case_Request
    {
        public mmria.common.model.couchdb.Change_Stack Change_Stack {get;set;} = new();

        public mmria.case_version.v260120.mmria_case Case_Data {get;set;}
        public Save_Case_Request()
        {

        }
    }


    [Authorize(Roles  = "abstractor")]
    [HttpPost]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<mmria.common.model.couchdb.document_put_response> Post() 
    { 
        try
        {
            var save_case_request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<Save_Case_Request>(Request);
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            var sanitizedRequest = CreateSanitizedSaveCaseRequest(save_case_request, GetCurrentUserName());
            if (sanitizedRequest?.Case_Data == null)
            {
                return new mmria.common.model.couchdb.document_put_response
                {
                    ok = false,
                    error_description = "Invalid case payload."
                };
            }

            var saveResult = await _caseManager.SaveCaseAsync(
                sanitizedRequest.Case_Data,
                sanitizedRequest.Change_Stack,
                db_config,
                User,
                configuration,
                host_prefix
            );

            // Dispatch sync message if save was successful
            if (saveResult.Response.ok && !string.IsNullOrWhiteSpace(saveResult.CaseId))
            {
                var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message(
                    saveResult.CaseId,
                    saveResult.SerializedCase,
                    "PUT",
                    configuration.GetString("metadata_version", host_prefix)
                );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);
            }

            return saveResult.Response;
        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex);
            return new mmria.common.model.couchdb.document_put_response 
            { 
                error_description = ex.Message 
            };
        }
    }


    public sealed class Force_Release_Lock_Request
    {
        public string case_id { get; set; }
    }

    //THIS IS FOR JURISDICTION ADMINS TO FORCE-RELEASE LOCKS IN CASES WHERE ABSTRACTORS FORGOT TO UNCHECKOUT, ETC. NOT INTENDED FOR REGULAR USE.
    [Authorize(Roles = "jurisdiction_admin")]
    [HttpPost("manage-case-checkout/force-release-lock")]
    public async Task<IActionResult> ForceReleaseLock()
    {
        var request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<Force_Release_Lock_Request>(Request);
        var sanitizedRequest = CreateSanitizedForceReleaseLockRequest(request);
        if (sanitizedRequest == null || string.IsNullOrWhiteSpace(sanitizedRequest.case_id))
        {
            return BadRequest(new { message = "case_id is required" });
        }

        var releaseResult = await _caseManager.ForceReleaseCaseLockAsync(sanitizedRequest.case_id, db_config, User);

        if (!releaseResult.IsSuccessful)
        {
            return StatusCode(releaseResult.StatusCode, new { message = releaseResult.Message });
        }

        if (!string.IsNullOrWhiteSpace(releaseResult.CaseId) && !string.IsNullOrWhiteSpace(releaseResult.SerializedCase))
        {
            var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message(
                releaseResult.CaseId,
                releaseResult.SerializedCase,
                "PUT",
                configuration.GetString("metadata_version", host_prefix)
            );

            _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);
        }

        return Ok(new { ok = true });
    }


    public sealed class Finalize_Unload_Request
    {
        public string current_case_id { get; set; }
        public string tab_id { get; set; }
        public List<string> offline_case_ids { get; set; }
    }


    
    [Authorize(Roles = "abstractor")]
    [HttpPost("finalize-unload")]
    public async Task<IActionResult> FinalizeUnload(System.Threading.CancellationToken cancellationToken)
    {
        var request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<Finalize_Unload_Request>(Request);
        var sanitizedRequest = CreateSanitizedFinalizeUnloadRequest(request);
        if (sanitizedRequest == null)
        {
            return BadRequest(new { ok = false, message = "Request body is required" });
        }

        try
        {
            var finalizeResult = await _caseManager.FinalizeUnloadAsync(
                sanitizedRequest.current_case_id,
                sanitizedRequest.tab_id,
                sanitizedRequest.offline_case_ids,
                db_config,
                User
            );

            if (!finalizeResult.IsSuccessful)
            {
                return StatusCode(finalizeResult.StatusCode, new { ok = false, message = finalizeResult.Message, failed = finalizeResult.FailedCases });
            }

            if (finalizeResult.UpdatedDocuments != null)
            {
                foreach (var updated in finalizeResult.UpdatedDocuments)
                {
                    if (updated == null || string.IsNullOrWhiteSpace(updated.CaseId) || string.IsNullOrWhiteSpace(updated.SerializedCase))
                    {
                        continue;
                    }

                    var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message(
                        updated.CaseId,
                        updated.SerializedCase,
                        "PUT",
                        configuration.GetString("metadata_version", host_prefix)
                    );

                    _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);
                }
            }

            return Ok(new { ok = true, updated_count = finalizeResult.UpdatedDocuments?.Count ?? 0, failed = finalizeResult.FailedCases });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { ok = false, message = ex.Message });
        }
    }

    public sealed class SetOfflineStatusRequest
    {
        public string direction { get; set; } // "add" or "remove"
    }

    //THIS FUNCTION IS FOR ABSTRACTORS TO SOFT LOCK A CASE FOR OFFLINE MODE
    [Authorize(Roles = "abstractor, jurisdiction_admin")]
    [HttpPost("toggle-offline/{caseId}")]
    public async Task<IActionResult> ToggleOfflineStatus(string caseId, System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            var request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<SetOfflineStatusRequest>(Request);
            var sanitizedRequest = CreateSanitizedSetOfflineStatusRequest(request);
            Console.WriteLine($"ToggleOfflineStatus called for caseId: {caseId}, direction: {sanitizedRequest?.direction}");

            var tabId = Request?.Query["tab_id"].FirstOrDefault();

            var toggleResult = await _caseManager.ToggleOfflineStatusAsync(
                caseId,
                sanitizedRequest?.direction,
                User,
                db_config,
                tabId,
                configuration,
                host_prefix
            );

            if (toggleResult.IsSuccessful)
            {
                // Dispatch sync message
                var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message(
                    toggleResult.CaseId,
                    toggleResult.SerializedCase,
                    "PUT",
                    configuration.GetString("metadata_version", host_prefix)
                );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);

                return Ok(new { success = true, is_offline = toggleResult.IsOffline, message = toggleResult.Message });
            }
            else
            {
                return toggleResult.StatusCode switch
                {
                    400 => BadRequest(new { success = false, message = toggleResult.ErrorMessage }),
                    409 => StatusCode(409, new { success = false, message = toggleResult.ErrorMessage }),
                    404 => NotFound(new { success = false, message = toggleResult.ErrorMessage }),
                    500 => StatusCode(500, new { success = false, message = toggleResult.ErrorMessage }),
                    _ => BadRequest(new { success = false, message = toggleResult.ErrorMessage })
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in ToggleOfflineStatus: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { success = false, message = "Internal server error while toggling offline status", error = ex.Message });
        }
    }

    //THIS FUNCTION IS SPECIFICALLY FOR JURISDICTION ADMINS TO REMOVE OFFLINE LOCKS IN CASES WHERE ABSTRACTORS FORGOT TO UNCHECKOUT OR UNSET OFFLINE STATUS, ETC. NOT INTENDED FOR REGULAR USE.
    [Authorize(Roles = "jurisdiction_admin")]
    [HttpPost("manage-case-checkout/remove-offline-lock/{caseId}")]
    public async Task<IActionResult> RemoveOfflineLock(string caseId, System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            var removeResult = await _caseManager.ForceRemoveOfflineLockAsync(
                caseId,
                User,
                db_config
            );

            if (removeResult.AlreadyInState)
            {
                return Ok(new
                {
                    success = false,
                    already_in_state = true,
                    is_offline = removeResult.IsOffline,
                    message = removeResult.Message
                });
            }

            if (removeResult.IsSuccessful)
            {
                var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message(
                    removeResult.CaseId,
                    removeResult.SerializedCase,
                    "PUT",
                    configuration.GetString("metadata_version", host_prefix)
                );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);

                return Ok(new
                {
                    success = true,
                    is_offline = removeResult.IsOffline,
                    message = removeResult.Message
                });
            }

            return removeResult.StatusCode switch
            {
                400 => BadRequest(new { success = false, message = removeResult.ErrorMessage }),
                401 => Unauthorized(new { success = false, message = removeResult.ErrorMessage }),
                404 => NotFound(new { success = false, message = removeResult.ErrorMessage }),
                _ => StatusCode(500, new { success = false, message = removeResult.ErrorMessage })
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in RemoveOfflineLock: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { success = false, message = "Internal server error while removing offline lock", error = ex.Message });
        }
    }

    [Authorize(Roles = "abstractor")]
    [HttpDelete]
    public async Task<System.Dynamic.ExpandoObject> Delete(string case_id = null, string rev = null) 
    { 
        try
        {
            var tabId = Request?.Query["tab_id"].FirstOrDefault();
            var deleteResult = await _caseManager.DeleteCaseAsync(case_id, rev, User, db_config, configuration, host_prefix, tabId);

            if (deleteResult.IsSuccessful)
            {
                // Dispatch sync message
                if (!string.IsNullOrWhiteSpace(deleteResult.DocumentJson))
                {
                    var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message
                    (
                        deleteResult.CaseId,
                        deleteResult.DocumentJson,
                        "DELETE",
                        configuration.GetString("metadata_version", host_prefix)
                    );

                    _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);
                }

                return deleteResult.Result;
            }
            else
            {
                Response.StatusCode = deleteResult.StatusCode;
                return null;
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }

    private Save_Case_Request CreateSanitizedSaveCaseRequest(Save_Case_Request request, string currentUserName)
    {
        var sanitizedCase = CreateSanitizedCase(request?.Case_Data, currentUserName);
        if (sanitizedCase == null)
        {
            return null;
        }

        return new Save_Case_Request
        {
            Case_Data = sanitizedCase,
            Change_Stack = CreateSanitizedChangeStack(request?.Change_Stack, sanitizedCase._id, sanitizedCase._rev, currentUserName)
        };
    }

    private static mmria.case_version.v260120.mmria_case CreateSanitizedCase(
        mmria.case_version.v260120.mmria_case request,
        string currentUserName)
    {
        if (request == null || string.IsNullOrWhiteSpace(request._id))
        {
            return null;
        }

        mmria.case_version.v260120.mmria_case sanitizedCase;
        try
        {
            sanitizedCase = CaseJsonSerialization.DeserializeMmriaCase(CaseJsonSerialization.SerializeMmriaCase(request));
        }
        catch
        {
            return null;
        }

        sanitizedCase._id = SanitizeSingleLineText(request._id, 256);
        sanitizedCase._rev = CouchDbRevisionHelper.NormalizeIncomingRevision(request._rev);
        sanitizedCase.created_by = SanitizeSingleLineText(request.created_by, 256);
        sanitizedCase.last_updated_by = SanitizeSingleLineText(currentUserName, 256);
        sanitizedCase.last_checked_out_by = SanitizeSingleLineText(request.last_checked_out_by, 256);
        sanitizedCase.checked_out_by_tab_id = SanitizeSingleLineText(request.checked_out_by_tab_id, 256);
        sanitizedCase.is_offline = NormalizeBooleanLikeValue(request.is_offline);
        sanitizedCase.offline_date = SanitizeSingleLineText(request.offline_date, 128);
        sanitizedCase.offline_by = SanitizeSingleLineText(request.offline_by, 256);
        sanitizedCase.offline_lock_type = SanitizeSingleLineText(request.offline_lock_type, 64);

        if (sanitizedCase.home_record != null)
        {
            sanitizedCase.home_record.jurisdiction_id = SanitizeSingleLineText(request.home_record?.jurisdiction_id, 512);
            sanitizedCase.home_record.record_id = SanitizeSingleLineText(request.home_record?.record_id, 256);
        }

        return sanitizedCase;
    }

    private static mmria.common.model.couchdb.Change_Stack CreateSanitizedChangeStack(
        mmria.common.model.couchdb.Change_Stack request,
        string caseId,
        string caseRevision,
        string currentUserName)
    {
        var sanitizedItems = request?.items?
            .Where(item => item != null)
            .Select(item => CreateSanitizedChangeStackItem(item, currentUserName))
            .ToList() ?? new List<mmria.common.model.couchdb.Change_Stack_Item>();

        return new mmria.common.model.couchdb.Change_Stack
        {
            _id = SanitizeSingleLineText(request?._id, 256),
            _rev = CouchDbRevisionHelper.NormalizeIncomingRevision(request?._rev),
            case_id = SanitizeSingleLineText(caseId, 256),
            case_rev = SanitizeSingleLineText(caseRevision, 256),
            record_id = SanitizeSingleLineText(request?.record_id, 256),
            is_delete = request?.is_delete,
            delete_rev = CouchDbRevisionHelper.NormalizeIncomingRevision(request?.delete_rev),
            first_name = SanitizeSingleLineText(request?.first_name, 256),
            last_name = SanitizeSingleLineText(request?.last_name, 256),
            user_name = SanitizeSingleLineText(currentUserName, 256),
            note = SanitizeMultilineText(request?.note, 2048),
            metadata_version = SanitizeSingleLineText(request?.metadata_version, 128),
            date_created = request?.date_created,
            items = sanitizedItems,
            doc_type = "Change_Stack"
        };
    }

    private static mmria.common.model.couchdb.Change_Stack_Item CreateSanitizedChangeStackItem(
        mmria.common.model.couchdb.Change_Stack_Item request,
        string currentUserName)
    {
        return new mmria.common.model.couchdb.Change_Stack_Item
        {
            _id = SanitizeSingleLineText(request?._id, 256),
            _rev = CouchDbRevisionHelper.NormalizeIncomingRevision(request?._rev),
            user_name = SanitizeSingleLineText(currentUserName, 256),
            temp_index = request?.temp_index,
            date_created = request?.date_created,
            object_path = SanitizeSingleLineText(request?.object_path, 512),
            metadata_path = SanitizeSingleLineText(request?.metadata_path, 512),
            old_value = request?.old_value,
            new_value = request?.new_value,
            dictionary_path = SanitizeSingleLineText(request?.dictionary_path, 512),
            form_index = request?.form_index,
            grid_index = request?.grid_index,
            prompt = SanitizeMultilineText(request?.prompt, 512),
            metadata_type = SanitizeSingleLineText(request?.metadata_type, 128),
            doc_type = "Change_Stack_Item"
        };
    }

    private static Force_Release_Lock_Request CreateSanitizedForceReleaseLockRequest(Force_Release_Lock_Request request)
    {
        if (request == null)
        {
            return null;
        }

        return new Force_Release_Lock_Request
        {
            case_id = SanitizeSingleLineText(request.case_id, 256)
        };
    }

    private static Finalize_Unload_Request CreateSanitizedFinalizeUnloadRequest(Finalize_Unload_Request request)
    {
        if (request == null)
        {
            return null;
        }

        return new Finalize_Unload_Request
        {
            current_case_id = SanitizeSingleLineText(request.current_case_id, 256),
            tab_id = SanitizeSingleLineText(request.tab_id, 256),
            offline_case_ids = SanitizeIdentifierList(request.offline_case_ids)
        };
    }

    private static SetOfflineStatusRequest CreateSanitizedSetOfflineStatusRequest(SetOfflineStatusRequest request)
    {
        if (request == null)
        {
            return new SetOfflineStatusRequest();
        }

        return new SetOfflineStatusRequest
        {
            direction = NormalizeOfflineDirection(request.direction)
        };
    }

    private string GetCurrentUserName()
    {
        if (User?.Identities?.Any(u => u.IsAuthenticated) == true)
        {
            return User.Identities.First(
                u => u.IsAuthenticated &&
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                .FindFirst(System.Security.Claims.ClaimTypes.Name)
                .Value;
        }

        return null;
    }

    private static List<string> SanitizeIdentifierList(IEnumerable<string> values)
    {
        if (values == null)
        {
            return new List<string>();
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            var sanitized = SanitizeSingleLineText(value, 256);
            if (string.IsNullOrWhiteSpace(sanitized) || !seen.Add(sanitized))
            {
                continue;
            }

            result.Add(sanitized);
        }

        return result;
    }

    private static string NormalizeOfflineDirection(string value)
    {
        var sanitized = SanitizeSingleLineText(value, 16).ToLowerInvariant();
        return sanitized == "add" || sanitized == "remove"
            ? sanitized
            : string.Empty;
    }

    private static string NormalizeBooleanLikeValue(string value)
    {
        var sanitized = SanitizeSingleLineText(value, 16);
        if (string.Equals(sanitized, "true", StringComparison.OrdinalIgnoreCase))
        {
            return "true";
        }

        if (string.Equals(sanitized, "false", StringComparison.OrdinalIgnoreCase))
        {
            return "false";
        }

        return string.Empty;
    }

    private async Task<string> GetOfflineDateAsync()
    {
        try
        {
            var vitalsUrl = configuration.GetString("vitals_url", host_prefix)
                ?.Replace("/api/Message/IJESet", string.Empty);
            if (string.IsNullOrWhiteSpace(vitalsUrl))
                return null;

            var getUrl = $"{vitalsUrl}/api/systemOffline/GetSystemOfflineConfig";
            var requestOptions = new mmria.common.getset.CouchDbRequestOptions
            {
                VitalServiceKey = configuration.GetString("vital_service_key", host_prefix)
            };
            var responseBody = await _couchDbHttpClient.ExecuteAsync(
                "GET", getUrl, null, "application/json", requestOptions);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.SystemOfflineConfig>(responseBody);
            return config?.offline_date;
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizeSingleLineText(string value, int maxLength = 512)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(value.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return sanitized.Length > maxLength
            ? sanitized[..maxLength]
            : sanitized;
    }

    private static string SanitizeMultilineText(string value, int maxLength = 2048)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(value.Where(character => character == '\r' || character == '\n' || !char.IsControl(character)).ToArray()).Trim();
        return sanitized.Length > maxLength
            ? sanitized[..maxLength]
            : sanitized;
    }



} 


#endif
