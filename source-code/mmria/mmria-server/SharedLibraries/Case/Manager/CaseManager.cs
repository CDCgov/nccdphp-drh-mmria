using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using mmria.case_version.v260120;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.server.utils;
using Newtonsoft.Json;

namespace mmria.server.SharedLibraries.Manager;

public class SaveCaseResult
{
    public document_put_response Response { get; set; }
    public string CaseId { get; set; }
    public string SerializedCase { get; set; }
}

public class ToggleOfflineStatusResult
{
    public bool IsSuccessful { get; set; }
    public bool IsOffline { get; set; }
    public string Message { get; set; }
    public string CaseId { get; set; }
    public string SerializedCase { get; set; }
    public int StatusCode { get; set; }
    public string ErrorMessage { get; set; }
}

public class DeleteCaseResult
{
    public bool IsSuccessful { get; set; }
    public string CaseId { get; set; }
    public string DocumentJson { get; set; }
    public ExpandoObject Result { get; set; }
    public int StatusCode { get; set; }
    public string ErrorMessage { get; set; }
    public string MmriaRecordId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string UserName { get; set; }
}

public class CaseManager
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public CaseManager(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<mmria_case> GetCaseAsync(string caseId, DBConfigurationDetail dbConfig, ClaimsPrincipal user)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(caseId))
            {
                string request_string = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}");
                string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
                    "GET",
                    request_string,
                    null,
                    dbConfig.user_name,
                    dbConfig.user_value
                );

                var settings = new JsonSerializerSettings
                {
                    Converters = { 
                        new TimeOnlyJsonConverter(), 
                        new DateOnlyJsonConverter() 
                    }
                };

                var result = JsonConvert.DeserializeObject<mmria_case>(responseFromServer, settings);

                if (authorization_case.is_authorized_to_handle_jurisdiction_id(dbConfig, user, ResourceRightEnum.ReadCase, result))
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return null;
    }

    public async Task<SaveCaseResult> SaveCaseAsync(
        mmria_case caseData,
        Change_Stack changeStack,
        DBConfigurationDetail dbConfig,
        ClaimsPrincipal user,
        OverridableConfiguration configuration,
        string hostPrefix)
    {
        var response = new document_put_response();
        var result = new SaveCaseResult { Response = response };

        var write_case_folder_set = new List<string>();
        try
        {
            var mmria_record_id = "";

            var userName = "";
            if (user.Identities.Any(u => u.IsAuthenticated))
            {
                userName = user.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;

                if (string.IsNullOrWhiteSpace(caseData._rev))
                {
                    var jurisdiction_hashset = authorization.get_current_jurisdiction_id_set_for(dbConfig, user);

                    foreach (var jurisdiction_item in jurisdiction_hashset)
                    {
                        if (jurisdiction_item.ResourceRight == ResourceRightEnum.WriteCase)
                        {
                            write_case_folder_set.Add(jurisdiction_item.jurisdiction_id);
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(caseData.created_by))
            {
                caseData.created_by = userName;
            }

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            string object_string = JsonConvert.SerializeObject(caseData, settings);

            var temp_id = caseData._id;
            string id_val = null;

            id_val = temp_id.ToString();

            var is_match = System.Text.RegularExpressions.Regex.IsMatch(
                id_val,
                @"^[0-9a-fA-F][0-9a-fA-F/-]+[0-9a-fA-F]$"
            );

            if (!is_match)
            {
                response.error_description = $"No Match On Id Format: Id:{id_val}";
                result.Response = response;
                return result;
            }

            if (string.IsNullOrWhiteSpace(caseData.home_record.jurisdiction_id))
            {
                System.Console.WriteLine("missing jurisdiction api/Case POST");
                caseData.home_record.jurisdiction_id = "/";
            }

            if (!string.IsNullOrWhiteSpace(caseData.home_record.record_id))
            {
                mmria_record_id = caseData.home_record.record_id;
            }

            if (!authorization_case.is_authorized_to_handle_jurisdiction_id(dbConfig, user, ResourceRightEnum.WriteCase, caseData.home_record.jurisdiction_id))
            {
                response.error_description = $"unauthorized PUT {caseData.home_record.jurisdiction_id}: {caseData._id}";
                Console.Write($"unauthorized PUT {caseData.home_record.jurisdiction_id}: {caseData._id}");
                result.Response = response;
                return result;
            }

            // begin - check if doc exists
            try
            {
                var check_document_json = await _couchDbHttpClient.ExecuteAsync(
                    "GET",
                    dbConfig.Get_Prefix_DB_Url($"mmrds/{id_val}"),
                    null,
                    dbConfig.user_name,
                    dbConfig.user_value
                );
                var check_document_expando_object = JsonConvert.DeserializeObject<ExpandoObject>(check_document_json);
                IDictionary<string, object> result_dictionary = check_document_expando_object as IDictionary<string, object>;

                if (result_dictionary != null &&
                    !authorization_case.is_authorized_to_handle_jurisdiction_id(dbConfig, user, ResourceRightEnum.WriteCase, check_document_expando_object))
                {
                    response.error_description = $"2nd unauthorized PUT {result_dictionary["jurisdiction_id"]}: {result_dictionary["_id"]}";
                    Console.Write($"2nd unauthorized PUT {result_dictionary["jurisdiction_id"]}: {result_dictionary["_id"]}");
                    result.Response = response;
                    return result;
                }
            }
            catch (Exception ex)
            {
                // do nothing for now document doesn't exsist.
                System.Console.WriteLine($"err caseController.Post\n{ex}");
            }
            // end - check if doc exists

            string save_response_from_server = null;
            try
            {
                string metadata_url = dbConfig.Get_Prefix_DB_Url($"mmrds/{id_val}");
                save_response_from_server = await _couchDbHttpClient.ExecuteAsync(
                    "PUT",
                    metadata_url,
                    object_string,
                    dbConfig.user_name,
                    dbConfig.user_value
                );
                response = JsonConvert.DeserializeObject<document_put_response>(save_response_from_server);
                result.Response = response;
            }
            catch (Exception ex)
            {
                response.error_description = ex.ToString();
                Console.WriteLine(ex);
            }

            if (!response.ok)
            {
                Console.Write($"save failed for: {id_val}");
                if (string.IsNullOrWhiteSpace(response.error_description))
                {
                    response.error_description = save_response_from_server;
                }
                else
                {
                    response.error_description = response.error_description;
                }

                Console.Write($"save_response:\n{response.error_description}");
                result.Response = response;
                return result;
            }

            changeStack.record_id = mmria_record_id;
            changeStack.metadata_version = configuration.GetString("metadata_version", hostPrefix);

            var audit_string = JsonConvert.SerializeObject(changeStack, settings);

            string audit_url = dbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}");
            try
            {
                string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
                    "PUT",
                    audit_url,
                    audit_string,
                    dbConfig.user_name,
                    dbConfig.user_value
                );
                var audit_result = JsonConvert.DeserializeObject<document_put_response>(responseFromServer);
            }
            catch (Exception ex)
            {
                Console.Write("problem saving audit\n{0}", ex);
            }

            // Store the case ID and serialized case for the controller to dispatch sync message
            result.CaseId = id_val;
            result.SerializedCase = object_string;
            result.Response = response;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    public async Task<ToggleOfflineStatusResult> ToggleOfflineStatusAsync(
        string caseId,
        string direction,
        ClaimsPrincipal user,
        DBConfigurationDetail dbConfig)
    {
        var result = new ToggleOfflineStatusResult();

        try
        {
            // Validate direction parameter
            if (string.IsNullOrWhiteSpace(direction))
            {
                result.IsSuccessful = false;
                result.StatusCode = 400;
                result.ErrorMessage = "Direction parameter is required. Must be 'add' or 'remove'.";
                return result;
            }

            var dir = direction.ToLowerInvariant();
            if (dir != "add" && dir != "remove")
            {
                result.IsSuccessful = false;
                result.StatusCode = 400;
                result.ErrorMessage = "Invalid direction parameter. Must be 'add' or 'remove'.";
                return result;
            }

            bool targetOfflineState = dir == "add";
            Console.WriteLine($"Target offline state: {targetOfflineState}");

            // Get the current case document
            var case_response = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                dbConfig.url + $"/{dbConfig.prefix}mmrds/" + caseId,
                null,
                dbConfig.user_name,
                dbConfig.user_value
            );
            Console.WriteLine($"Case response length: {case_response?.Length ?? 0}");

            if (string.IsNullOrEmpty(case_response))
            {
                result.IsSuccessful = false;
                result.StatusCode = 404;
                result.ErrorMessage = "Case not found";
                return result;
            }

            // Check if the response indicates an error
            if (case_response.Contains("\"error\""))
            {
                Console.WriteLine($"CouchDB error in response: {case_response}");
                result.IsSuccessful = false;
                result.StatusCode = 400;
                result.ErrorMessage = "Error retrieving case from database";
                return result;
            }

            // Use Newtonsoft.Json for better compatibility with existing code
            var case_document = JsonConvert.DeserializeObject<Dictionary<string, object>>(case_response);

            if (case_document == null)
            {
                result.IsSuccessful = false;
                result.StatusCode = 400;
                result.ErrorMessage = "Invalid case document format";
                return result;
            }

            Console.WriteLine($"Case document loaded successfully, has {case_document.Count} properties");

            // Ensure we have the _id and _rev fields
            if (!case_document.ContainsKey("_id"))
            {
                case_document["_id"] = caseId;
            }

            if (!case_document.ContainsKey("_rev"))
            {
                Console.WriteLine("Warning: Document missing _rev field");
                result.IsSuccessful = false;
                result.StatusCode = 400;
                result.ErrorMessage = "Document missing revision information";
                return result;
            }

            Console.WriteLine($"Document revision: {case_document["_rev"]}");

            // Toggle offline state
            bool currentOfflineState = false;
            if (case_document.ContainsKey("is_offline") && case_document["is_offline"] != null)
            {
                if (case_document["is_offline"] is bool boolValue)
                {
                    currentOfflineState = boolValue;
                }
                else if (case_document["is_offline"] is string stringValue)
                {
                    bool.TryParse(stringValue, out currentOfflineState);
                }
                // Handle Newtonsoft.Json.Linq.JValue case
                else if (case_document["is_offline"].ToString().ToLowerInvariant() == "true")
                {
                    currentOfflineState = true;
                }
            }

            Console.WriteLine($"Current offline state: {currentOfflineState}");

            // Validate that we're not already in the target state
            if (currentOfflineState == targetOfflineState)
            {
                string message = targetOfflineState
                    ? "Case is already marked for offline use"
                    : "Case is already marked as online";
                Console.WriteLine($"State validation failed: {message}");
                result.IsSuccessful = false;
                result.StatusCode = 400;
                result.ErrorMessage = message;
                result.IsOffline = currentOfflineState;
                return result;
            }

            // Set new offline state (use targetOfflineState instead of toggling)
            bool newOfflineState = targetOfflineState;
            case_document["is_offline"] = newOfflineState;

            if (newOfflineState)
            {
                // Adding to offline list (soft lock = 1)
                case_document["offline_date"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                case_document["offline_by"] = user.Identity?.Name ?? "system";
                case_document["offline_lock_type"] = 1; // Soft lock
            }
            else
            {
                // Removing from offline list - clear all offline fields
                case_document["offline_date"] = null;
                case_document["offline_by"] = null;
                case_document["offline_lock_type"] = null;
            }

            // Update last_updated fields
            case_document["date_last_updated"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            case_document["last_updated_by"] = user.Identity?.Name ?? "system";

            Console.WriteLine($"New offline state: {newOfflineState}");

            // Save the updated document
            var json_string = JsonConvert.SerializeObject(case_document);
            Console.WriteLine($"Serialized document length: {json_string.Length}");

            var save_response = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                dbConfig.url + $"/{dbConfig.prefix}mmrds/" + caseId,
                json_string,
                dbConfig.user_name,
                dbConfig.user_value
            );
            Console.WriteLine($"Save response: {save_response}");

            if (string.IsNullOrEmpty(save_response))
            {
                result.IsSuccessful = false;
                result.StatusCode = 500;
                result.ErrorMessage = "Empty response from database";
                return result;
            }

            var save_result = JsonConvert.DeserializeObject<document_put_response>(save_response);

            if (save_result != null && save_result.ok)
            {
                result.IsSuccessful = true;
                result.IsOffline = newOfflineState;
                result.Message = $"Case {(newOfflineState ? "marked for offline use" : "removed from offline use")}";
                result.CaseId = caseId;
                result.SerializedCase = json_string;
                result.StatusCode = 200;
                Console.WriteLine($"Document updated successfully. New revision: {save_result.rev}");
            }
            else
            {
                Console.WriteLine($"Save failed - save_result.ok: {save_result?.ok}, error: {save_result?.error_description}");
                result.IsSuccessful = false;
                result.StatusCode = 400;
                result.ErrorMessage = save_result?.error_description ?? "Unknown error";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in ToggleOfflineStatus: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            result.IsSuccessful = false;
            result.StatusCode = 500;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<DeleteCaseResult> DeleteCaseAsync(string caseId, string rev, ClaimsPrincipal user, DBConfigurationDetail dbConfig)
    {
        var result = new DeleteCaseResult()
        {
            IsSuccessful = false,
            StatusCode = 400,
            CaseId = caseId
        };

        try
        {
            var mmria_record_id = "";
            var first_name = "";
            var last_name = "";

            var userName = "";
            if (user.Identities.Any(u => u.IsAuthenticated))
            {
                userName = user.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }

            string request_string = null;

            if (!string.IsNullOrWhiteSpace(caseId) && !string.IsNullOrWhiteSpace(rev))
            {
                request_string = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}?rev={rev}");
            }
            else
            {
                result.ErrorMessage = "Case ID and revision are required";
                result.StatusCode = 400;
                return result;
            }

            string document_json = null;
            try
            {
                document_json = await _couchDbHttpClient.ExecuteAsync(
                    "GET",
                    dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}"),
                    null,
                    dbConfig.user_name,
                    dbConfig.user_value
                );
                var check_docuement_curl_result = JsonConvert.DeserializeObject<ExpandoObject>(document_json);
                IDictionary<string, object> result_dictionary = check_docuement_curl_result as IDictionary<string, object>;
                
                if
                (
                    result_dictionary != null && 
                    !authorization_case.is_authorized_to_handle_jurisdiction_id(dbConfig, user, ResourceRightEnum.WriteCase, check_docuement_curl_result)
                )
                {
                    Console.Write($"unauthorized DELETE {result_dictionary["jurisdiction_id"]}: {result_dictionary["_id"]}");
                    result.ErrorMessage = "Not authorized to delete this case";
                    result.StatusCode = 403;
                    return result;
                }
                
                if (result_dictionary.ContainsKey("_rev"))
                {
                    request_string = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}?rev={result_dictionary["_rev"]}");
                }

                if 
                (
                    result_dictionary.ContainsKey("home_record") &&
                    result_dictionary["home_record"] is IDictionary<string,object> home_record
                )
                {
                    if(home_record.ContainsKey("record_id"))
                    mmria_record_id = home_record["record_id"].ToString();

                    if(home_record.ContainsKey("first_name"))
                    first_name = home_record["first_name"].ToString();

                    if(home_record.ContainsKey("last_name"))
                    last_name = home_record["last_name"].ToString();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"err DeleteCaseAsync\n{ex}");
                result.ErrorMessage = ex.Message;
                result.StatusCode = 500;
                return result;
            }

            string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
                "DELETE",
                request_string,
                null,
                dbConfig.user_name,
                dbConfig.user_value
            );
            var delete_result = JsonConvert.DeserializeObject<ExpandoObject>(responseFromServer);

            var audit_data = new Change_Stack()
            {
                _id = System.Guid.NewGuid().ToString(),
                case_id = caseId,
                case_rev = rev,

                record_id = mmria_record_id,
                is_delete = true,
                delete_rev = rev,

                user_name = userName,
                first_name = first_name,
                last_name = last_name,

                note = "deleted case",

                metadata_version = "",
                date_created = DateTime.UtcNow,
            };

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;

            var audit_string = JsonConvert.SerializeObject(audit_data, settings);

            string audit_url = dbConfig.Get_Prefix_DB_Url($"audit/{audit_data._id}");

            try
            {
                string save_delete_audit_response = await _couchDbHttpClient.ExecuteAsync(
                    "PUT",
                    audit_url,
                    audit_string,
                    dbConfig.user_name,
                    dbConfig.user_value
                );
                var audit_result = JsonConvert.DeserializeObject<document_put_response>(save_delete_audit_response);
            }
            catch(Exception ex)
            {
                Console.Write($"problem saving audit\n{ex}");
            }

            result.IsSuccessful = true;
            result.StatusCode = 200;
            result.CaseId = caseId;
            result.DocumentJson = document_json;
            result.Result = delete_result;
            result.MmriaRecordId = mmria_record_id;
            result.FirstName = first_name;
            result.LastName = last_name;
            result.UserName = userName;

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in DeleteCaseAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            result.IsSuccessful = false;
            result.StatusCode = 500;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }
}
