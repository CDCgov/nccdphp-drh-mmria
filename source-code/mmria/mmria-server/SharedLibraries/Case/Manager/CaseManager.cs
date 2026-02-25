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
}
