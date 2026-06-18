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
using mmria.common.SharedLibraries.Other;
using mmria.common.SharedLibraries.Case.DAL;
using mmria.common.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.Case.Manager;

public class SaveCaseResult
{
    public document_put_response Response { get; set; }
    public string CaseId { get; set; }
    public string SerializedCase { get; set; }
}

public class ReleaseCaseLockResult
{
    public bool IsSuccessful { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; }
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

public class RemoveOfflineLockResult
{
    public bool IsSuccessful { get; set; }
    public bool IsOffline { get; set; }
    public bool AlreadyInState { get; set; }
    public string Message { get; set; }
    public string CaseId { get; set; }
    public string SerializedCase { get; set; }
    public int StatusCode { get; set; }
    public string ErrorMessage { get; set; }
}

public sealed class FinalizeUnloadUpdatedDocument
{
    public string CaseId { get; set; }
    public string SerializedCase { get; set; }
}

public sealed class FinalizeUnloadResult
{
    public bool IsSuccessful { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; }

    public List<FinalizeUnloadUpdatedDocument> UpdatedDocuments { get; set; } = new();

    // Per-case failures (best-effort unload should not fail everything)
    public Dictionary<string, string> FailedCases { get; set; } = new();
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

public sealed class UpdateYearOfDeathResult
{
    public bool IsSuccessful { get; set; }
    public int StatusCode { get; set; }
    public string StatusText { get; set; }
    public string LastUpdatedBy { get; set; }
    public DateTime? DateLastUpdated { get; set; }
    public string DateOfDeath { get; set; }
}

public sealed class UpdateMaidenNameResult
{
    public bool IsSuccessful { get; set; }
    public int StatusCode { get; set; }
    public string StatusText { get; set; }
    public string LastUpdatedBy { get; set; }
    public DateTime? DateLastUpdated { get; set; }
    public string MaidenName { get; set; }
}

public class CaseManager
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public CaseManager(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<UpdateYearOfDeathResult> UpdateYearOfDeathAsync(
        string caseId,
        string role,
        string stateDatabase,
        int? yearOfDeathReplacement,
        string recordIdReplacement,
        string dateOfDeath,
        ClaimsPrincipal user,
        DBConfigurationDetail db_config,
        ConfigurationSet dbConfigSet,
        OverridableConfiguration configuration = null,
        string hostPrefix = null,
        string currentTabId = null)
    {
        var result = new UpdateYearOfDeathResult
        {
            IsSuccessful = false,
            StatusCode = 400
        };

        var userName = "";
        if (user.Identities.Any(u => u.IsAuthenticated))
        {
            userName = user.Identities.First(
                u => u.IsAuthenticated &&
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
        }

        var dal = new CaseDAL(_couchDbHttpClient);

        if (string.IsNullOrWhiteSpace(currentTabId))
        {
            currentTabId = user?.FindFirst("tab_id")?.Value;
        }

        var caseLockMinutes = 120;
        if (configuration != null && !string.IsNullOrWhiteSpace(hostPrefix))
        {
            caseLockMinutes = GetCaseLockMinutes(configuration, hostPrefix);
        }

        string responseFromServer = null;
        if (role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
        {
            var db_info = dbConfigSet.detail_list[stateDatabase];
            responseFromServer = await dal.GetCaseDocumentJsonAsync(caseId, db_info);
        }
        else
        {
            responseFromServer = await dal.GetCaseDocumentJsonAsync(caseId, db_config);
        }

        // Enforce offline + active lock ownership rules for year-of-death updates.
        // Note: We parse the JSON with JObject here to correctly handle booleans and DateTimes.
        JObject document = null;
        try
        {
            document = JObject.Parse(responseFromServer);
        }
        catch
        {
            // If parsing fails, do not block update based on lock/offline.
        }

        if (document != null)
        {
            var isOfflineToken = document["is_offline"];
            var isOffline = false;
            if (isOfflineToken != null)
            {
                if (isOfflineToken.Type == JTokenType.Boolean)
                {
                    isOffline = isOfflineToken.Value<bool>();
                }
                else
                {
                    isOffline = string.Equals(isOfflineToken.ToString(), "true", StringComparison.OrdinalIgnoreCase);
                }
            }

            if (isOffline)
            {
                result.IsSuccessful = false;
                result.StatusCode = 409;
                result.StatusText = "Case is offline and cannot be updated.";
                return result;
            }

            var lockedBy = document.Value<string>("last_checked_out_by");
            var lockedTabId = document.Value<string>("checked_out_by_tab_id");
            var checkedOutUtc = ParseUtcDateTime(document["date_last_checked_out"]);

            if (IsLockedByAnotherUser(lockedBy, checkedOutUtc, userName, caseLockMinutes))
            {
                result.IsSuccessful = false;
                result.StatusCode = 409;
                result.StatusText = "Case is locked by another user and cannot be updated.";
                return result;
            }

            // Only enforce tab ownership when caller provided a tab id.
            if (!string.IsNullOrWhiteSpace(currentTabId) &&
                IsLockedBySameUserDifferentTab(lockedBy, lockedTabId, checkedOutUtc, userName, currentTabId, caseLockMinutes))
            {
                result.IsSuccessful = false;
                result.StatusCode = 409;
                result.StatusText = "Case is locked by this user in a different tab and cannot be updated.";
                return result;
            }
        }

        var case_response = CaseJsonSerialization.DeserializeMmriaCase(responseFromServer);

        var oldYear = case_response.home_record.date_of_death.year;

        if (yearOfDeathReplacement.HasValue)
            case_response.home_record.date_of_death.year = yearOfDeathReplacement.Value;

        if (!string.IsNullOrWhiteSpace(recordIdReplacement))
            case_response.home_record.record_id = recordIdReplacement;

        case_response.last_updated_by = userName;
        case_response.date_last_updated = DateTime.Now;

        result.LastUpdatedBy = userName;
        result.DateLastUpdated = case_response.date_last_updated;

        List<string> date_of_death_sections = dateOfDeath.Length > 0
            ? new List<string>(dateOfDeath.Split("/"))
            : new List<string>();

        if (date_of_death_sections.Count == 3)
            date_of_death_sections[2] = yearOfDeathReplacement.Value.ToString();
        else if (date_of_death_sections.Count == 2)
            date_of_death_sections[1] = yearOfDeathReplacement.Value.ToString();
        else
            date_of_death_sections.Add(yearOfDeathReplacement.Value.ToString());

        result.DateOfDeath = String.Join("/", date_of_death_sections);

        var object_string = CaseJsonSerialization.SerializeMmriaCase(case_response);

        if (role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
        {
            var db_info = dbConfigSet.detail_list[stateDatabase];
            responseFromServer = await dal.PutCaseDocumentJsonAsync(caseId, object_string, db_info);
        }
        else
        {
            responseFromServer = await dal.PutCaseDocumentJsonAsync(caseId, object_string, db_config);
        }

        var document_put_response = new document_put_response();
        try
        {
            document_put_response = JsonConvert.DeserializeObject<document_put_response>(responseFromServer);
        }
        catch (Exception ex)
        {
            result.StatusText = $"Problem Setting Status to (blank)\n{ex}";
        }

        if (document_put_response.ok)
        {
            result.StatusText = "(blank)";
            result.IsSuccessful = true;
            result.StatusCode = 200;

            if (yearOfDeathReplacement.HasValue)
            {
                var auditDbConfig = role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase)
                    ? dbConfigSet.detail_list[stateDatabase]
                    : db_config;
                var changeStack = new Change_Stack
                {
                    _id = Guid.NewGuid().ToString(),
                    case_id = caseId,
                    user_name = userName,
                    note = "admin change, year of death updated",
                    date_created = DateTime.UtcNow,
                    doc_type = "Change_Stack",
                    items = new List<Change_Stack_Item>
                    {
                        new Change_Stack_Item
                        {
                            user_name = userName,
                            prompt = "Year of Death",
                            object_path = "/home_record/date_of_death/year",
                            old_value = oldYear.ToString(),
                            new_value = yearOfDeathReplacement.Value.ToString(),
                            doc_type = "Change_Stack_Item"
                        }
                    }
                };
                JsonSerializerSettings auditSettings = new JsonSerializerSettings();
                auditSettings.NullValueHandling = NullValueHandling.Ignore;
                var audit_string = JsonConvert.SerializeObject(changeStack, auditSettings);
                string audit_url = auditDbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}");
                try
                {
                    string auditResponse = await _couchDbHttpClient.ExecuteAsync(
                        "PUT", audit_url, audit_string,
                        auditDbConfig.user_name, auditDbConfig.user_value);
                    var audit_result = JsonConvert.DeserializeObject<document_put_response>(auditResponse);
                    if (audit_result == null || !audit_result.ok)
                        Console.WriteLine($"Audit save failed for case {caseId}, audit {changeStack._id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Audit save threw for case {caseId}, audit {changeStack._id}: {ex.Message}");
                }
            }
        }
        else
        {
            result.StatusText = "Problem Setting Status to (blank)";
            result.IsSuccessful = false;
            result.StatusCode = 500;
        }

        return result;
    }

    public async Task<UpdateMaidenNameResult> UpdateMaidenNameAsync(
        string caseId,
        string role,
        string stateDatabase,
        string maidenNameReplacement,
        ClaimsPrincipal user,
        DBConfigurationDetail db_config,
        ConfigurationSet dbConfigSet,
        OverridableConfiguration configuration = null,
        string hostPrefix = null,
        string currentTabId = null)
    {
        var result = new UpdateMaidenNameResult
        {
            IsSuccessful = false,
            StatusCode = 400
        };

        var userName = "";
        if (user.Identities.Any(u => u.IsAuthenticated))
        {
            userName = user.Identities.First(
                u => u.IsAuthenticated &&
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
        }

        try
        {
            var dal = new CaseDAL(_couchDbHttpClient);

            if (string.IsNullOrWhiteSpace(currentTabId))
            {
                currentTabId = user?.FindFirst("tab_id")?.Value;
            }

            var caseLockMinutes = 120;
            if (configuration != null && !string.IsNullOrWhiteSpace(hostPrefix))
            {
                caseLockMinutes = GetCaseLockMinutes(configuration, hostPrefix);
            }

            string responseFromServer = null;

            if (role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
            {
                var db_info = dbConfigSet.detail_list[stateDatabase];
                responseFromServer = await dal.GetCaseDocumentJsonAsync(caseId, db_info);
            }
            else
            {
                responseFromServer = await dal.GetCaseDocumentJsonAsync(caseId, db_config);
            }

            // Enforce offline + active lock ownership rules for maiden-name updates.
            JObject document = null;
            try
            {
                document = JObject.Parse(responseFromServer);
            }
            catch
            {
                // If parsing fails, do not block update based on lock/offline.
            }

            if (document != null)
            {
                var isOfflineToken = document["is_offline"];
                var isOffline = false;
                if (isOfflineToken != null)
                {
                    if (isOfflineToken.Type == JTokenType.Boolean)
                    {
                        isOffline = isOfflineToken.Value<bool>();
                    }
                    else
                    {
                        isOffline = string.Equals(isOfflineToken.ToString(), "true", StringComparison.OrdinalIgnoreCase);
                    }
                }

                if (isOffline)
                {
                    result.IsSuccessful = false;
                    result.StatusCode = 409;
                    result.StatusText = "Case is offline and cannot be updated.";
                    return result;
                }

                var lockedBy = document.Value<string>("last_checked_out_by");
                var lockedTabId = document.Value<string>("checked_out_by_tab_id");
                var checkedOutUtc = ParseUtcDateTime(document["date_last_checked_out"]);

                if (IsLockedByAnotherUser(lockedBy, checkedOutUtc, userName, caseLockMinutes))
                {
                    result.IsSuccessful = false;
                    result.StatusCode = 409;
                    result.StatusText = "Case is locked by another user and cannot be updated.";
                    return result;
                }

                // Only enforce tab ownership when caller provided a tab id.
                if (!string.IsNullOrWhiteSpace(currentTabId) &&
                    IsLockedBySameUserDifferentTab(lockedBy, lockedTabId, checkedOutUtc, userName, currentTabId, caseLockMinutes))
                {
                    result.IsSuccessful = false;
                    result.StatusCode = 409;
                    result.StatusText = "Case is locked by this user in a different tab and cannot be updated.";
                    return result;
                }
            }

            var case_response = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);

            // death_certificate/certificate_identification/dmaiden
            var dictionary = case_response as IDictionary<string, object>;
            if (dictionary != null)
            {
                var death_certificate = dictionary["death_certificate"] as IDictionary<string, object>;
                if (death_certificate != null)
                {
                    var certificate_identification = death_certificate["certificate_identification"] as IDictionary<string, object>;
                    if (certificate_identification != null)
                    {
                        var oldMaidenName = certificate_identification["dmaiden"]?.ToString() ?? "";
                        dictionary["last_updated_by"] = userName;
                        dictionary["date_last_updated"] = DateTime.Now;
                        certificate_identification["dmaiden"] = maidenNameReplacement;

                        result.MaidenName = maidenNameReplacement;
                        result.LastUpdatedBy = userName;
                        result.DateLastUpdated = (DateTime)dictionary["date_last_updated"];

                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        settings.NullValueHandling = NullValueHandling.Ignore;
                        var object_string = JsonConvert.SerializeObject(case_response, settings);

                        if (role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
                        {
                            var db_info = dbConfigSet.detail_list[stateDatabase];
                            responseFromServer = await dal.PutCaseDocumentJsonAsync(caseId, object_string, db_info);
                        }
                        else
                        {
                            responseFromServer = await dal.PutCaseDocumentJsonAsync(caseId, object_string, db_config);
                        }

                        var document_put_response = new document_put_response();
                        try
                        {
                            document_put_response = JsonConvert.DeserializeObject<document_put_response>(responseFromServer);
                        }
                        catch (Exception ex)
                        {
                            result.StatusText = $"Problem Setting Status to (blank)\n{ex}";
                        }

                        if (document_put_response.ok)
                        {
                            result.StatusText = "(blank)";
                            result.IsSuccessful = true;
                            result.StatusCode = 200;

                            var auditDbConfig = role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase)
                                ? dbConfigSet.detail_list[stateDatabase]
                                : db_config;
                            var changeStack = new Change_Stack
                            {
                                _id = Guid.NewGuid().ToString(),
                                case_id = caseId,
                                user_name = userName,
                                note = "admin change, maiden name updated",
                                date_created = DateTime.UtcNow,
                                doc_type = "Change_Stack",
                                items = new List<Change_Stack_Item>
                                {
                                    new Change_Stack_Item
                                    {
                                        user_name = userName,
                                        prompt = "Maiden Name",
                                        object_path = "/death_certificate/certificate_identification/dmaiden",
                                        old_value = oldMaidenName,
                                        new_value = maidenNameReplacement,
                                        doc_type = "Change_Stack_Item"
                                    }
                                }
                            };
                            JsonSerializerSettings auditSettings = new JsonSerializerSettings();
                            auditSettings.NullValueHandling = NullValueHandling.Ignore;
                            var audit_string = JsonConvert.SerializeObject(changeStack, auditSettings);
                            string audit_url = auditDbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}");
                            try
                            {
                                string auditResponse = await _couchDbHttpClient.ExecuteAsync(
                                    "PUT", audit_url, audit_string,
                                    auditDbConfig.user_name, auditDbConfig.user_value);
                                var audit_result = JsonConvert.DeserializeObject<document_put_response>(auditResponse);
                                if (audit_result == null || !audit_result.ok)
                                    Console.WriteLine($"Audit save failed for case {caseId}, audit {changeStack._id}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Audit save threw for case {caseId}, audit {changeStack._id}: {ex.Message}");
                            }
                        }
                        else
                        {
                            result.StatusText = "Problem Setting Status to (blank)";
                            result.IsSuccessful = false;
                            result.StatusCode = 500;
                        }
                    }
                    else
                    {
                        result.StatusText = "Problem Setting Status to (blank)";
                        result.IsSuccessful = false;
                        result.StatusCode = 500;
                    }
                }
                else
                {
                    result.StatusText = "Problem Setting Status to (blank)";
                    result.IsSuccessful = false;
                    result.StatusCode = 500;
                }
            }
            else
            {
                result.StatusText = "Problem Setting Status to (blank)";
                result.IsSuccessful = false;
                result.StatusCode = 500;
            }
        }
        catch (Exception ex)
        {
            result.StatusText = ex.ToString();
            result.IsSuccessful = false;
            result.StatusCode = 500;
        }

        return result;
    }

    public async Task<List<case_view_item>> FindYearOfDeathRecordsAsync(
        string recordId,
        string role,
        string stateDatabase,
        DBConfigurationDetail db_config,
        ConfigurationSet dbConfigSet)
    {
        var result = new List<case_view_item>();
        var dal = new CaseDAL(_couchDbHttpClient);
        string responseFromServer = null;

        if (role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
        {
            var db_info = dbConfigSet.detail_list[stateDatabase];
            responseFromServer = await dal.GetCasesByDateLastUpdatedViewJsonAsync(db_info);
        }
        else
        {
            responseFromServer = await dal.GetCasesByDateLastUpdatedViewJsonAsync(db_config);
        }

        case_view_response case_view_response = JsonConvert.DeserializeObject<case_view_response>(responseFromServer);

        var Locked_status_list = new List<int>() { 4, 5, 6 };
        foreach (var item in case_view_response.rows)
        {
            try
            {
                if
                (
                    item.value.record_id != null &&
                    !string.IsNullOrWhiteSpace(recordId) &&
                    (
                        item.value.record_id.IndexOf(recordId, System.StringComparison.OrdinalIgnoreCase) > -1 ||
                        recordId.IndexOf(item.value.record_id, System.StringComparison.OrdinalIgnoreCase) > -1
                    )
                    /*
                    &&
                    (
                        item.value.case_status.HasValue &&
                        Locked_status_list.IndexOf(item.value.case_status.Value) > -1
                    )*/
                )
                {
                    result.Add(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        return result;
    }

    public async Task<HashSet<string>> GetExistingRecordIdsAsync(DBConfigurationDetail dbInfo)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var dal = new CaseDAL(_couchDbHttpClient);
            string responseFromServer = await dal.GetCasesByDateCreatedViewJsonAsync(dbInfo);

            case_view_response case_view_response = JsonConvert.DeserializeObject<case_view_response>(responseFromServer);
            foreach (case_view_item cvi in case_view_response.rows)
            {
                result.Add(cvi.value.record_id);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    /// <summary>
    /// Returns true if any case document in <paramref name="dbInfo"/>'s mmrds database
    /// has the given <paramref name="recordId"/>. Uses a Mango <c>_find</c> with
    /// <c>limit:1</c> and a single-field projection so the wire transfer is constant
    /// regardless of database size.
    /// </summary>
    /// <remarks>
    /// This is intended for the record-id-uniqueness loop in
    /// <see cref="GetRecordIdReplacementForYearOfDeathAsync"/>, which previously
    /// pulled the entire by_date_created view (up to 25k rows) just to do an
    /// in-memory <see cref="HashSet{T}.Contains"/> check. With this method the loop
    /// makes at most a small number of round-trips, each returning at most one row.
    /// </remarks>
    public async Task<bool> RecordIdExistsAsync(string recordId, DBConfigurationDetail dbInfo)
    {
        if (string.IsNullOrWhiteSpace(recordId) || dbInfo == null)
        {
            return false;
        }

        try
        {
            // System.Text.Json escapes " inside the recordId via JsonEncodedText, so any
            // weird input cannot break out of the selector.
            var selectorPayload = new
            {
                selector = new
                {
                    record_id = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["$eq"] = recordId
                    }
                },
                fields = new[] { "_id" },
                limit = 1
            };

            string payload = JsonConvert.SerializeObject(selectorPayload);
            string findUrl = $"{dbInfo.url}/{dbInfo.prefix}mmrds/_find";

            string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
                "POST",
                findUrl,
                payload,
                dbInfo.user_name,
                dbInfo.user_value,
                "application/json");

            if (string.IsNullOrEmpty(responseFromServer))
            {
                return false;
            }

            using var doc = System.Text.Json.JsonDocument.Parse(responseFromServer);
            if (doc.RootElement.TryGetProperty("docs", out var docsElement) &&
                docsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                return docsElement.GetArrayLength() > 0;
            }
        }
        catch (Exception ex)
        {
            // On error, fall back to "exists" so the caller picks a different candidate id.
            // Worse case: one extra random suffix attempt — far cheaper than the original
            // 25k-row fetch fallback.
            Console.WriteLine($"RecordIdExistsAsync error for record_id={recordId}: {ex.Message}");
            return true;
        }

        return false;
    }

    public async Task<string> GetRecordIdReplacementForYearOfDeathAsync(
        string role,
        string stateDatabase,
        string recordId,
        int? yearOfDeathReplacement,
        ConfigurationSet dbConfigSet)
    {
        DBConfigurationDetail db_info = null;

        if (role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
        {
            db_info = dbConfigSet.detail_list[stateDatabase];
        }
        else if (role.Equals("jurisdiction_admin", StringComparison.OrdinalIgnoreCase))
        {
            db_info = dbConfigSet.detail_list[stateDatabase];
        }

        var array = recordId.Split('-');
        string new_record_id = $"{array[0]}-{yearOfDeathReplacement}-{array[2]}";

        // Per-candidate existence check rather than loading every record_id in the
        // database into a HashSet. The original implementation fetched up to 25,000
        // rows on every call; this loop now does one tiny Mango query per candidate.
        int my_count = -1;
        while (await RecordIdExistsAsync(new_record_id, db_info))
        {
            int _min = 1000;
            int _max = 9999;
            Random _rdm = new Random(System.DateTime.Now.Millisecond + my_count);
            my_count++;
            new_record_id = $"{array[0]}-{yearOfDeathReplacement}-{_rdm.Next(_min, _max)}";
        }

        return new_record_id;
    }

    private static int GetCaseLockMinutes(OverridableConfiguration configuration, string hostPrefix)
    {
        if (int.TryParse(configuration.GetString("case_lock_minutes", hostPrefix), out var caseLockMinutes))
        {
            return caseLockMinutes;
        }

        return 120;
    }

    private static bool IsLockedByAnotherUser(string lockedBy, DateTime? checkedOutUtc, string currentUserName, int caseLockMinutes)
    {
        if (string.IsNullOrWhiteSpace(lockedBy) || !checkedOutUtc.HasValue)
        {
            return false;
        }

        if (string.Equals(lockedBy, currentUserName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // If checked out by someone else within the window, it's locked.
        return DateTime.UtcNow <= checkedOutUtc.Value.AddMinutes(caseLockMinutes);
    }

    private static bool IsLockedBySameUserDifferentTab(
        string lockedBy,
        string lockedTabId,
        DateTime? checkedOutUtc,
        string currentUserName,
        string currentTabId,
        int caseLockMinutes)
    {
        if (string.IsNullOrWhiteSpace(lockedBy) || !checkedOutUtc.HasValue)
        {
            return false;
        }

        // Only enforce tab ownership when the stored document has a tab id.
        if (string.IsNullOrWhiteSpace(lockedTabId))
        {
            return false;
        }

        if (!string.Equals(lockedBy, currentUserName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // If lock is still active, require tab id match. Missing currentTabId counts as mismatch.
        if (DateTime.UtcNow <= checkedOutUtc.Value.AddMinutes(caseLockMinutes))
        {
            return string.IsNullOrWhiteSpace(currentTabId) || !string.Equals(lockedTabId, currentTabId, StringComparison.Ordinal);
        }

        return false;
    }

    private static DateTime? ParseUtcDateTime(JToken token)
    {
        if (token == null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type == JTokenType.Date)
        {
            var dt = token.Value<DateTime>();
            return dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
        }

        var text = token.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(
            text,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out var dto))
        {
            return dto.UtcDateTime;
        }

        return null;
    }

    private static bool IsOfflineLockedBySameUserDifferentTab(
        bool isOffline,
        string offlineBy,
        string offlineByTabId,
        string currentUserName,
        string currentTabId)
    {
        if (!isOffline)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(offlineBy) || string.IsNullOrWhiteSpace(offlineByTabId))
        {
            return false;
        }

        if (!string.Equals(offlineBy, currentUserName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(currentTabId) ||
               !string.Equals(offlineByTabId, currentTabId, StringComparison.Ordinal);
    }

    public async Task<mmria_case> GetCaseAsync(string caseId, DBConfigurationDetail dbConfig, ClaimsPrincipal user)
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

                var result = CaseJsonSerialization.DeserializeMmriaCase(responseFromServer);

                if (authorization_case.is_authorized_to_handle_jurisdiction_id(dbConfig, user, ResourceRightEnum.ReadCase, result, _couchDbHttpClient))
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }

        return null;
    }

    public async Task<SaveCaseResult> SaveCaseAsync(
        mmria_case caseData,
        Change_Stack changeStack,
        DBConfigurationDetail dbConfig,
        ClaimsPrincipal user,
        OverridableConfiguration configuration,
        string hostPrefix,
        bool bypassOfflineTabOwnershipCheck = false)
    {
        var response = new document_put_response();
        var result = new SaveCaseResult { Response = response };

        if (caseData == null || changeStack == null || string.IsNullOrWhiteSpace(caseData._id) || caseData.home_record == null)
        {
            response.ok = false;
            response.error_description = "Invalid case payload.";
            result.Response = response;
            return result;
        }

        var write_case_folder_set = new List<string>();
        var mmria_record_id = "";
        string existingCreatedBy = null;
        string existingRevision = null;
        DateTime? existingDateCreated = null;

            var userName = "";
            if (user.Identities.Any(u => u.IsAuthenticated))
            {
                userName = user.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;

                if (string.IsNullOrWhiteSpace(caseData._rev))
                {
                    var jurisdiction_hashset = authorization.get_current_jurisdiction_id_set_for(dbConfig, user, _couchDbHttpClient);

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

            if (!authorization_case.is_authorized_to_handle_jurisdiction_id(dbConfig, user, ResourceRightEnum.WriteCase, caseData.home_record.jurisdiction_id, _couchDbHttpClient))
            {
                response.error_description = $"unauthorized PUT {caseData.home_record.jurisdiction_id}: {caseData._id}";
                Console.Write($"unauthorized PUT {caseData.home_record.jurisdiction_id}: {caseData._id}");
                result.Response = response;
                return result;
            }

            // begin - check if doc exists
            string existing_locked_by = null;
            DateTime? existing_date_last_checked_out = null;
            string existing_checked_out_by_tab_id = null;
            bool existing_is_offline = false;
            string existing_offline_by = null;
            string existing_offline_by_tab_id = null;
            try
            {
                var check_document_response = await _couchDbHttpClient.ExecuteForResponseAsync(
                    "GET",
                    dbConfig.Get_Prefix_DB_Url($"mmrds/{id_val}"),
                    null,
                    dbConfig.user_name,
                    dbConfig.user_value
                );

                if (check_document_response.StatusCode == 404)
                {
                    // New case: CouchDB returns not_found for the existence probe.
                }
                else if (check_document_response.StatusCode == 200)
                {
                    var check_document_json = check_document_response.Body;
                    var check_document_jobject = JObject.Parse(check_document_json);
                    var check_document_expando_object = JsonConvert.DeserializeObject<ExpandoObject>(check_document_json);
                    IDictionary<string, object> result_dictionary = check_document_expando_object as IDictionary<string, object>;

                    // Read lock fields from the stored document json (source of truth).
                    // This avoids payload-based bypass and keeps UTC parsing consistent.
                    existingRevision = check_document_jobject.Value<string>("_rev");
                    existingCreatedBy = check_document_jobject.Value<string>("created_by");
                    existingDateCreated = ParseUtcDateTime(check_document_jobject["date_created"]);
                    existing_locked_by = check_document_jobject.Value<string>("last_checked_out_by");
                    existing_date_last_checked_out = ParseUtcDateTime(check_document_jobject["date_last_checked_out"]);
                    existing_checked_out_by_tab_id = check_document_jobject.Value<string>("checked_out_by_tab_id");
                    TryReadIsOffline(check_document_jobject, out existing_is_offline);
                    existing_offline_by = check_document_jobject.Value<string>("offline_by");
                    existing_offline_by_tab_id = check_document_jobject.Value<string>("offline_by_tab_id");

                    if (result_dictionary != null &&
                        !authorization_case.is_authorized_to_handle_jurisdiction_id(dbConfig, user, ResourceRightEnum.WriteCase, check_document_expando_object, _couchDbHttpClient))
                    {
                        var jurisdiction_id = check_document_jobject.SelectToken("home_record.jurisdiction_id")?.ToString();
                        var id = check_document_jobject.Value<string>("_id");
                        var existing_case_id = string.IsNullOrWhiteSpace(id) ? id_val : id;

                        response.error_description = $"2nd unauthorized PUT {jurisdiction_id}: {existing_case_id}";
                        Console.Write($"2nd unauthorized PUT {jurisdiction_id}: {existing_case_id}");
                        result.Response = response;
                        return result;
                    }
                }
                else
                {
                    response.ok = false;
                    response.error_description = $"Unable to verify existing case before save. CouchDB returned HTTP {check_document_response.StatusCode} for case {id_val}.";
                    result.Response = response;
                    return result;
                }

            }
            catch (JsonException ex)
            {
                response.ok = false;
                response.error_description = $"Unable to parse existing case document before save for case {id_val}.";
                Console.WriteLine($"err caseController.Post existing case parse\n{ex}");
                result.Response = response;
                return result;
            }
            catch (Exception ex)
            {
                response.ok = false;
                response.error_description = $"Unable to verify existing case before save for case {id_val}.";
                System.Console.WriteLine($"err caseController.Post existing case probe\n{ex}");
                result.Response = response;
                return result;
            }
            // end - check if doc exists

            var caseLockMinutes = GetCaseLockMinutes(configuration, hostPrefix);
            if (IsLockedByAnotherUser(existing_locked_by, existing_date_last_checked_out, userName, caseLockMinutes))
            {
                response.ok = false;
                response.error_description = $"Case is locked by {existing_locked_by}. Please try again after {caseLockMinutes} minutes.";
                result.Response = response;
                return result;
            }

            if (IsLockedBySameUserDifferentTab(
                    existing_locked_by,
                    existing_checked_out_by_tab_id,
                    existing_date_last_checked_out,
                    userName,
                    caseData.checked_out_by_tab_id,
                    caseLockMinutes))
            {
                response.ok = false;
                response.error_description = "Case is locked by another tab for this user. Please close the other tab, or wait for the lock to expire.";
                result.Response = response;
                return result;
            }

            if (!bypassOfflineTabOwnershipCheck &&
                IsOfflineLockedBySameUserDifferentTab(
                    existing_is_offline,
                    existing_offline_by,
                    existing_offline_by_tab_id,
                    userName,
                    caseData.checked_out_by_tab_id))
            {
                response.ok = false;
                response.error_description = "Case is offline in another tab for this user. Please return to the original tab used for offline mode.";
                result.Response = response;
                return result;
            }

            caseData.created_by = !string.IsNullOrWhiteSpace(existingCreatedBy)
                ? existingCreatedBy
                : (string.IsNullOrWhiteSpace(caseData.created_by) ? userName : caseData.created_by);
            caseData.date_created = existingDateCreated ?? caseData.date_created ?? DateTime.Now;
            caseData.last_updated_by = userName;
            caseData.date_last_updated = DateTime.Now;

            var caseRevisionHandling = DescribeRevisionHandling(caseData._rev, existingRevision);
            caseData._rev = CouchDbRevisionHelper.ResolveServerOwnedRevision(caseData._rev, existingRevision);
            var changeStackRevisionHandling = DescribeIncomingRevisionHandling(changeStack._rev);
            changeStack._rev = CouchDbRevisionHelper.NormalizeIncomingRevision(changeStack._rev);
            changeStack.delete_rev = CouchDbRevisionHelper.NormalizeIncomingRevision(changeStack.delete_rev);

            changeStack._id = string.IsNullOrWhiteSpace(changeStack._id) ? Guid.NewGuid().ToString() : changeStack._id;
            changeStack.case_id = id_val;
            changeStack.case_rev = caseData._rev;
            changeStack.user_name = userName;
            changeStack.date_created ??= DateTime.UtcNow;
            changeStack.doc_type = "Change_Stack";
            if (changeStack.items != null)
            {
                foreach (var item in changeStack.items.Where(i => i != null))
                {
                    item._rev = CouchDbRevisionHelper.NormalizeIncomingRevision(item._rev);
                    item.user_name = userName;
                    item.doc_type = "Change_Stack_Item";
                }
            }

            // Sliding edit lock: if the incoming payload still indicates the case is checked out,
            // refresh the checkout timestamp to extend the lock window.
            // If the client is clearing the lock, strip the tab id before persisting so the
            // saved document is fully unlocked after the owner-tab validation above succeeds.
            if (caseData.date_last_checked_out.HasValue)
            {
                caseData.date_last_checked_out = DateTime.UtcNow;
            }
            else
            {
                caseData.checked_out_by_tab_id = null;
            }

            var object_string = CaseJsonSerialization.SerializeMmriaCase(caseData);
            var casePayloadContainsRevision = object_string.Contains("\"_rev\"", StringComparison.Ordinal);

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
                    Console.WriteLine(
                        $"Case save transport failure. requestPath=/api/case; hostPrefix={hostPrefix}; caseId={id_val}; user={userName}; caseRevHandling={caseRevisionHandling}; containsRev={casePayloadContainsRevision}; exceptionType={ex.GetType().FullName}; message={ex.Message}");
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

                    Console.WriteLine(
                        $"Case save failed for {id_val}: rev={caseRevisionHandling}; contains_rev={casePayloadContainsRevision}; response={response.error_description}");
                    Console.Write($"save_response:\n{response.error_description}");
                    result.Response = response;
                    return result;
                }

                changeStack.record_id = mmria_record_id;
                changeStack.metadata_version = configuration.GetString("metadata_version", hostPrefix);

                JsonSerializerSettings auditSettings = new JsonSerializerSettings();
                auditSettings.NullValueHandling = NullValueHandling.Ignore;
                var audit_string = JsonConvert.SerializeObject(changeStack, auditSettings);

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
                    if (audit_result == null || !audit_result.ok)
                    {
                        Console.WriteLine(
                            $"Audit save failed for case {id_val}, audit {changeStack._id}: rev={changeStackRevisionHandling}; response={responseFromServer}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Audit save threw for case {id_val}, audit {changeStack._id}: {ex.Message}");
                }

                // Store the case ID and serialized case for the controller to dispatch sync message
                result.CaseId = id_val;
                result.SerializedCase = object_string;
                result.Response = response;

        return result;
    }

    public async Task<ReleaseCaseLockResult> ForceReleaseCaseLockAsync(
        string caseId,
        DBConfigurationDetail dbConfig,
        ClaimsPrincipal user)
    {
        var result = new ReleaseCaseLockResult
        {
            IsSuccessful = false,
            StatusCode = 400,
            Message = "Invalid request."
        };

        if (string.IsNullOrWhiteSpace(caseId))
        {
            result.Message = "caseId is required.";
            return result;
        }

        var userName = "";
        if (user?.Identities?.Any(u => u.IsAuthenticated) == true)
        {
            var identity = user.Identities.FirstOrDefault(u => u.IsAuthenticated && u.HasClaim(c => c.Type == ClaimTypes.Name));
            userName = identity?.FindFirst(ClaimTypes.Name)?.Value ?? "";
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            result.StatusCode = 401;
            result.Message = "User is not authenticated.";
            return result;
        }

        var is_match = System.Text.RegularExpressions.Regex.IsMatch(
            caseId,
            @"^[0-9a-fA-F][0-9a-fA-F/-]+[0-9a-fA-F]$"
        );

        if (!is_match)
        {
            result.StatusCode = 400;
            result.Message = $"No Match On Id Format: Id:{caseId}";
            return result;
        }

        string documentJson;
        try
        {
            documentJson = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}"),
                null,
                dbConfig.user_name,
                dbConfig.user_value
            );
        }
        catch (Exception ex)
        {
            result.StatusCode = 404;
            result.Message = $"Case not found or not accessible. {ex.Message}";
            return result;
        }

        JObject doc;
        try
        {
            doc = JObject.Parse(documentJson);
        }
        catch (Exception ex)
        {
            result.StatusCode = 500;
            result.Message = $"Unable to parse case document. {ex.Message}";
            return result;
        }

        // Admin operation: always clear lock fields regardless of current owner or tab.
        doc.Remove("date_last_checked_out");
        doc.Remove("last_checked_out_by");
        doc.Remove("checked_out_by_tab_id");

        var updatedJson = doc.ToString(Formatting.None);

        try
        {
            var save_response_from_server = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}"),
                updatedJson,
                dbConfig.user_name,
                dbConfig.user_value
            );

            var putResponse = JsonConvert.DeserializeObject<document_put_response>(save_response_from_server);
            if (putResponse?.ok == true)
            {
                result.IsSuccessful = true;
                result.StatusCode = 200;
                result.Message = "Lock force-released.";
                result.CaseId = caseId;
                result.SerializedCase = updatedJson;
                return result;
            }

            result.IsSuccessful = false;
            result.StatusCode = 500;
            result.Message = putResponse?.error_description ?? "Failed to force-release lock.";
            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccessful = false;
            result.StatusCode = 500;
            result.Message = ex.Message;
            return result;
        }
    }

    public async Task<ToggleOfflineStatusResult> ToggleOfflineStatusAsync(
        string caseId,
        string direction,
        ClaimsPrincipal user,
        DBConfigurationDetail dbConfig,
        string currentTabId = null,
        OverridableConfiguration configuration = null,
        string hostPrefix = null)
    {
        var result = new ToggleOfflineStatusResult();

        var userName = user?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = "system";
        }

        if (string.IsNullOrWhiteSpace(currentTabId))
        {
            currentTabId = user?.FindFirst("tab_id")?.Value;
        }

        var caseLockMinutes = 120;
        if (configuration != null && !string.IsNullOrWhiteSpace(hostPrefix))
        {
            caseLockMinutes = GetCaseLockMinutes(configuration, hostPrefix);
        }

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

            if (targetOfflineState)
            {
                if (string.IsNullOrWhiteSpace(currentTabId))
                {
                    result.IsSuccessful = false;
                    result.StatusCode = 400;
                    result.ErrorMessage = "tab_id is required to add a case to offline mode.";
                    return result;
                }

                var conflictingSoftLockCaseId = await new CaseDAL(_couchDbHttpClient)
                    .GetSoftLockedCaseIdForUserInAnotherTabAsync(userName, currentTabId, dbConfig);

                if (!string.IsNullOrWhiteSpace(conflictingSoftLockCaseId) &&
                    !string.Equals(conflictingSoftLockCaseId, caseId, StringComparison.OrdinalIgnoreCase))
                {
                    result.IsSuccessful = false;
                    result.StatusCode = 409;
                    result.ErrorMessage = "Case is offline locked by another tab for this user.";
                    return result;
                }
            }

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

            // Enforce active edit-lock ownership rules before updating the document.
            // Offline toggling updates _rev, which can cause the editing tab's Save to conflict.
            JObject lockDocument = null;
            try
            {
                lockDocument = JObject.Parse(case_response);
            }
            catch
            {
                // If parsing fails, do not block toggle based on lock.
            }

            if (lockDocument != null)
            {
                var lockedBy = lockDocument.Value<string>("last_checked_out_by");
                var lockedTabId = lockDocument.Value<string>("checked_out_by_tab_id");
                var checkedOutUtc = ParseUtcDateTime(lockDocument["date_last_checked_out"]);

                if (IsLockedByAnotherUser(lockedBy, checkedOutUtc, userName, caseLockMinutes))
                {
                    result.IsSuccessful = false;
                    result.StatusCode = 409;
                    result.ErrorMessage = $"Case is locked by {lockedBy}.";
                    return result;
                }

                if (IsLockedBySameUserDifferentTab(
                    lockedBy,
                    lockedTabId,
                    checkedOutUtc,
                    userName,
                    currentTabId,
                    caseLockMinutes))
                {
                    result.IsSuccessful = false;
                    result.StatusCode = 409;
                    result.ErrorMessage = "Case is locked by another tab for this user.";
                    return result;
                }
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

            case_document.TryGetValue("_rev", out var document_rev);
            Console.WriteLine($"Document revision: {document_rev}");

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

            // Enforce offline lock ownership:
            // - Only the user who added the offline lock may remove it.
            // - If the offline lock has a stored tab id, only that same tab may remove it.
            if (currentOfflineState && !targetOfflineState)
            {
                case_document.TryGetValue("offline_by", out var offlineByObj);
                var offlineBy = offlineByObj?.ToString();

                if (!string.IsNullOrWhiteSpace(offlineBy) &&
                    !string.Equals(offlineBy, userName, StringComparison.OrdinalIgnoreCase))
                {
                    result.IsSuccessful = false;
                    result.StatusCode = 409;
                    result.ErrorMessage = $"Case is offline locked by {offlineBy}.";
                    result.IsOffline = currentOfflineState;
                    return result;
                }

                if (case_document.TryGetValue("offline_by_tab_id", out var offlineByTabIdObj))
                {
                    var offlineByTabId = offlineByTabIdObj?.ToString();
                    if (!string.IsNullOrWhiteSpace(offlineByTabId))
                    {
                        if (string.IsNullOrWhiteSpace(currentTabId) ||
                            !string.Equals(offlineByTabId, currentTabId, StringComparison.Ordinal))
                        {
                            result.IsSuccessful = false;
                            result.StatusCode = 409;
                            result.ErrorMessage = "Case is offline locked by another tab for this user.";
                            result.IsOffline = currentOfflineState;
                            return result;
                        }
                    }
                }
            }

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
                case_document["offline_by"] = userName;
                case_document["offline_lock_type"] = 1; // Soft lock
                if (!string.IsNullOrWhiteSpace(currentTabId))
                {
                    case_document["offline_by_tab_id"] = currentTabId;
                }
            }
            else
            {
                // Removing from offline list - clear all offline fields
                case_document["offline_date"] = null;
                case_document["offline_by"] = null;
                case_document["offline_lock_type"] = null;
                case_document["offline_by_tab_id"] = null;
            }

            // Update last_updated fields
            case_document["date_last_updated"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            case_document["last_updated_by"] = userName;

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

        return result;
    }

    public async Task<RemoveOfflineLockResult> ForceRemoveOfflineLockAsync(
        string caseId,
        ClaimsPrincipal user,
        DBConfigurationDetail dbConfig)
    {
        var dal = new CaseDAL(_couchDbHttpClient);
        var result = new RemoveOfflineLockResult
        {
            IsSuccessful = false,
            StatusCode = 400,
            ErrorMessage = "Invalid request."
        };

        if (string.IsNullOrWhiteSpace(caseId))
        {
            result.ErrorMessage = "caseId is required.";
            return result;
        }

        var userName = user?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
        {
            result.StatusCode = 401;
            result.ErrorMessage = "User is not authenticated.";
            return result;
        }

        var caseResponse = await dal.GetCaseDocumentJsonAsync(caseId, dbConfig);

        if (string.IsNullOrWhiteSpace(caseResponse))
        {
            result.StatusCode = 404;
            result.ErrorMessage = "Case not found";
            return result;
        }

        var caseDocument = JsonConvert.DeserializeObject<Dictionary<string, object>>(caseResponse);
        if (caseDocument == null)
        {
            result.StatusCode = 400;
            result.ErrorMessage = "Invalid case document format";
            return result;
        }

        bool currentOfflineState = false;
        if (caseDocument.ContainsKey("is_offline") && caseDocument["is_offline"] != null)
        {
            if (caseDocument["is_offline"] is bool boolValue)
            {
                currentOfflineState = boolValue;
            }
            else if (caseDocument["is_offline"] is string stringValue)
            {
                bool.TryParse(stringValue, out currentOfflineState);
            }
            else if (string.Equals(caseDocument["is_offline"].ToString(), "true", StringComparison.OrdinalIgnoreCase))
            {
                currentOfflineState = true;
            }
        }

        var hasCaseLock =
            !string.IsNullOrWhiteSpace(caseDocument.GetValueOrDefault("last_checked_out_by")?.ToString()) ||
            caseDocument.GetValueOrDefault("date_last_checked_out") != null ||
            !string.IsNullOrWhiteSpace(caseDocument.GetValueOrDefault("checked_out_by_tab_id")?.ToString());

        if (!currentOfflineState && !hasCaseLock)
        {
            result.IsOffline = false;
            result.AlreadyInState = true;
            result.StatusCode = 200;
            result.Message = "Case is already online and not locked.";
            return result;
        }

        caseDocument["is_offline"] = false;
        caseDocument.Remove("offline_date");
        caseDocument.Remove("offline_by");
        caseDocument.Remove("offline_lock_type");
        caseDocument.Remove("offline_by_tab_id");
        caseDocument.Remove("date_last_checked_out");
        caseDocument.Remove("last_checked_out_by");
        caseDocument.Remove("checked_out_by_tab_id");
        caseDocument["date_last_updated"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        caseDocument["last_updated_by"] = userName;

        var jsonString = JsonConvert.SerializeObject(caseDocument);

        var saveResponse = await dal.PutCaseDocumentJsonAsync(caseId, jsonString, dbConfig);

        if (string.IsNullOrWhiteSpace(saveResponse))
        {
            result.StatusCode = 500;
            result.ErrorMessage = "Empty response from database";
            return result;
        }

        var saveResult = JsonConvert.DeserializeObject<document_put_response>(saveResponse);
        if (saveResult?.ok != true)
        {
            result.StatusCode = 400;
            result.ErrorMessage = saveResult?.error_description ?? "Unknown error";
            return result;
        }

        result.IsSuccessful = true;
        result.IsOffline = false;
        result.StatusCode = 200;
        result.Message = "Offline and case locks removed.";
        result.CaseId = caseId;
        result.SerializedCase = jsonString;
        return result;
    }

    public async Task<FinalizeUnloadResult> FinalizeUnloadAsync(
        string currentCaseId,
        string currentTabId,
        IEnumerable<string> offlineCaseIds,
        DBConfigurationDetail dbConfig,
        ClaimsPrincipal user)
    {
        var result = new FinalizeUnloadResult
        {
            IsSuccessful = false,
            StatusCode = 400,
            Message = "Invalid request."
        };

        var userName = user?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
        {
            result.StatusCode = 401;
            result.Message = "User is not authenticated.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(currentTabId))
        {
            result.StatusCode = 400;
            result.Message = "currentTabId is required.";
            return result;
        }

        var offlineSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (offlineCaseIds != null)
        {
            foreach (var id in offlineCaseIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    offlineSet.Add(id.Trim());
                }
            }
        }

        var caseIdsToProcess = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(currentCaseId))
        {
            caseIdsToProcess.Add(currentCaseId.Trim());
        }

        foreach (var id in offlineSet)
        {
            caseIdsToProcess.Add(id);
        }

        foreach (var caseId in caseIdsToProcess)
        {
            var doReleaseEditLock = !string.IsNullOrWhiteSpace(currentCaseId) &&
                                   string.Equals(caseId, currentCaseId, StringComparison.OrdinalIgnoreCase);
            var doRemoveOffline = offlineSet.Contains(caseId);

            var update = await FinalizeUnloadForSingleCaseAsync(
                caseId,
                doReleaseEditLock,
                currentTabId,
                doRemoveOffline,
                userName,
                dbConfig);

            if (update.IsUpdated)
            {
                result.UpdatedDocuments.Add(new FinalizeUnloadUpdatedDocument
                {
                    CaseId = caseId,
                    SerializedCase = update.UpdatedJson
                });
            }

            if (!update.IsSuccessful)
            {
                result.FailedCases[caseId] = update.ErrorMessage;
            }
        }

        result.IsSuccessful = true;
        result.StatusCode = 200;
        result.Message = "Finalize unload completed.";
        return result;
    }

    private sealed class FinalizeSingleCaseResult
    {
        public bool IsSuccessful { get; set; }
        public bool IsUpdated { get; set; }
        public string UpdatedJson { get; set; }
        public string ErrorMessage { get; set; }
    }

    private static bool TryReadIsOffline(JObject doc, out bool isOffline)
    {
        isOffline = false;
        var token = doc["is_offline"];
        if (token == null || token.Type == JTokenType.Null)
        {
            return true;
        }

        if (token.Type == JTokenType.Boolean)
        {
            isOffline = token.Value<bool>();
            return true;
        }

        var text = token.ToString();
        if (bool.TryParse(text, out var parsed))
        {
            isOffline = parsed;
            return true;
        }

        // Some data uses "true"/"false" strings; treat unknown as false.
        isOffline = string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);
        return true;
    }

    private async Task<FinalizeSingleCaseResult> FinalizeUnloadForSingleCaseAsync(
        string caseId,
        bool releaseEditLock,
        string currentTabId,
        bool removeOfflineLock,
        string userName,
        DBConfigurationDetail dbConfig)
    {
        var result = new FinalizeSingleCaseResult
        {
            IsSuccessful = true,
            IsUpdated = false
        };

        if (string.IsNullOrWhiteSpace(caseId))
        {
            result.IsSuccessful = false;
            result.ErrorMessage = "caseId is required.";
            return result;
        }

        var is_match = System.Text.RegularExpressions.Regex.IsMatch(
            caseId,
            @"^[0-9a-fA-F][0-9a-fA-F/-]+[0-9a-fA-F]$");

        if (!is_match)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = $"No Match On Id Format: Id:{caseId}";
            return result;
        }

        var requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}");

        // Best-effort retry on CouchDB 409 conflicts.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            string documentJson;
            documentJson = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                requestUrl,
                null,
                dbConfig.user_name,
                dbConfig.user_value);

            if (string.IsNullOrWhiteSpace(documentJson) || documentJson.Contains("\"error\""))
            {
                result.IsSuccessful = false;
                result.ErrorMessage = "Unable to load case.";
                return result;
            }

            var doc = JObject.Parse(documentJson);
            var changed = false;

            if (releaseEditLock)
            {
                var lockedBy = doc.Value<string>("last_checked_out_by");
                var lockedTabId = doc.Value<string>("checked_out_by_tab_id");
                var checkedOutUtc = ParseUtcDateTime(doc["date_last_checked_out"]);

                if (!string.IsNullOrWhiteSpace(lockedBy) && checkedOutUtc.HasValue)
                {
                    if (string.Equals(lockedBy, userName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Enforce tab ownership when stored document has a tab id.
                        if (string.IsNullOrWhiteSpace(lockedTabId) ||
                            (!string.IsNullOrWhiteSpace(currentTabId) && string.Equals(lockedTabId, currentTabId, StringComparison.Ordinal)))
                        {
                            doc.Remove("date_last_checked_out");
                            doc.Remove("last_checked_out_by");
                            doc.Remove("checked_out_by_tab_id");
                            changed = true;
                        }
                        else
                        {
                            result.IsSuccessful = false;
                            result.ErrorMessage = "Case is locked by another tab for this user.";
                        }
                    }
                    else
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage = $"Case is locked by {lockedBy}.";
                    }
                }
            }

            if (removeOfflineLock)
            {
                TryReadIsOffline(doc, out var isOffline);
                if (isOffline)
                {
                    var offlineBy = doc.Value<string>("offline_by");
                    var offlineLockType = doc["offline_lock_type"]?.ToString();
                    var offlineByTabId = doc.Value<string>("offline_by_tab_id");

                    // Only remove soft locks (1). Hard locks (2) are not removed during unload cleanup.
                    if (!string.IsNullOrWhiteSpace(offlineLockType) && offlineLockType != "1")
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage = "Offline hard lock cannot be removed during unload.";
                    }
                    else if (!string.IsNullOrWhiteSpace(offlineBy) &&
                             !string.Equals(offlineBy, userName, StringComparison.OrdinalIgnoreCase))
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage = $"Case is offline locked by {offlineBy}.";
                    }
                    else if (!string.IsNullOrWhiteSpace(offlineByTabId) &&
                             (string.IsNullOrWhiteSpace(currentTabId) ||
                              !string.Equals(offlineByTabId, currentTabId, StringComparison.Ordinal)))
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage = "Case is offline locked by another tab for this user.";
                    }
                    else
                    {
                        doc["is_offline"] = false;
                        doc.Remove("offline_date");
                        doc.Remove("offline_by");
                        doc.Remove("offline_lock_type");
                        doc.Remove("offline_by_tab_id");
                        changed = true;
                    }
                }
            }

            if (!changed)
            {
                result.IsUpdated = false;
                return result;
            }

            doc["date_last_updated"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            doc["last_updated_by"] = userName;

            var updatedJson = doc.ToString(Formatting.None);
            var putResponseJson = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                requestUrl,
                updatedJson,
                dbConfig.user_name,
                dbConfig.user_value);

            document_put_response putResponse = null;
            if (!string.IsNullOrWhiteSpace(putResponseJson) && putResponseJson.TrimStart().StartsWith("{"))
            {
                putResponse = JsonConvert.DeserializeObject<document_put_response>(putResponseJson);
            }

            if (putResponse?.ok == true)
            {
                result.IsUpdated = true;
                result.UpdatedJson = updatedJson;
                return result;
            }

            // Retry on conflict; otherwise stop.
            var looksLikeConflict =
                (!string.IsNullOrWhiteSpace(putResponse?.error_description) &&
                 putResponse.error_description.Contains("conflict", StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(putResponseJson) &&
                 (putResponseJson.Contains("(409)") ||
                  putResponseJson.Contains("\"error\":\"conflict\"") ||
                  putResponseJson.Contains("Document update conflict", StringComparison.OrdinalIgnoreCase)));

            if (looksLikeConflict)
            {
                continue;
            }

            result.IsSuccessful = false;
            result.ErrorMessage = putResponse?.error_description ?? putResponseJson ?? "Failed to update case.";
            return result;
        }

        result.IsSuccessful = false;
        result.ErrorMessage = "Conflict updating case.";
        return result;
    }

    public async Task<DeleteCaseResult> DeleteCaseAsync(
        string caseId,
        string rev,
        ClaimsPrincipal user,
        DBConfigurationDetail dbConfig,
        OverridableConfiguration configuration = null,
        string hostPrefix = null,
        string currentTabId = null)
    {
        var result = new DeleteCaseResult()
        {
            IsSuccessful = false,
            StatusCode = 400,
            CaseId = caseId
        };

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
                    !authorization_case.is_authorized_to_handle_jurisdiction_id(dbConfig, user, ResourceRightEnum.WriteCase, check_docuement_curl_result, _couchDbHttpClient)
                )
                {
                    result_dictionary.TryGetValue("jurisdiction_id", out var jurisdiction_id_obj);
                    result_dictionary.TryGetValue("_id", out var id_obj);
                    var jurisdiction_id = jurisdiction_id_obj?.ToString();
                    var id = id_obj?.ToString();

                    Console.Write($"unauthorized DELETE {jurisdiction_id}: {id}");
                    result.ErrorMessage = "Not authorized to delete this case";
                    result.StatusCode = 403;
                    return result;
                }

                if (string.IsNullOrWhiteSpace(currentTabId))
                {
                    currentTabId = user?.FindFirst("tab_id")?.Value;
                }

                var caseLockMinutes = 120;
                if (configuration != null && !string.IsNullOrWhiteSpace(hostPrefix))
                {
                    caseLockMinutes = GetCaseLockMinutes(configuration, hostPrefix);
                }

                // Enforce offline + active lock ownership rules for deletes.
                // Note: We parse the JSON with JObject here to correctly handle booleans and DateTimes.
                JObject document = null;
                try
                {
                    document = JObject.Parse(document_json);
                }
                catch
                {
                    // If parsing fails, do not block deletion based on lock/offline.
                }

                if (document != null)
                {
                    var isOfflineToken = document["is_offline"];
                    var isOffline = false;
                    if (isOfflineToken != null)
                    {
                        if (isOfflineToken.Type == JTokenType.Boolean)
                        {
                            isOffline = isOfflineToken.Value<bool>();
                        }
                        else
                        {
                            isOffline = string.Equals(isOfflineToken.ToString(), "true", StringComparison.OrdinalIgnoreCase);
                        }
                    }

                    if (isOffline)
                    {
                        result.IsSuccessful = false;
                        result.StatusCode = 409;
                        result.ErrorMessage = "Case is offline and cannot be deleted.";
                        return result;
                    }

                    var lockedBy = document.Value<string>("last_checked_out_by");
                    var lockedTabId = document.Value<string>("checked_out_by_tab_id");
                    var checkedOutUtc = ParseUtcDateTime(document["date_last_checked_out"]);

                    if (IsLockedByAnotherUser(lockedBy, checkedOutUtc, userName, caseLockMinutes))
                    {
                        result.IsSuccessful = false;
                        result.StatusCode = 409;
                        result.ErrorMessage = "Case is locked by another user and cannot be deleted.";
                        return result;
                    }

                    if (IsLockedBySameUserDifferentTab(lockedBy, lockedTabId, checkedOutUtc, userName, currentTabId, caseLockMinutes))
                    {
                        result.IsSuccessful = false;
                        result.StatusCode = 409;
                        result.ErrorMessage = "Case is locked by this user in a different tab and cannot be deleted.";
                        return result;
                    }
                }
                
                if (result_dictionary.ContainsKey("_rev"))
                {
                    var storedRev = result_dictionary["_rev"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(storedRev))
                    {
                        request_string = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}?rev={storedRev}");
                        rev = storedRev;
                    }
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

    private static string DescribeRevisionHandling(string incoming, string existing)
    {
        var normalizedIncoming = CouchDbRevisionHelper.NormalizeOptionalRevision(incoming);
        var normalizedExisting = CouchDbRevisionHelper.NormalizeOptionalRevision(existing);
        var resolved = CouchDbRevisionHelper.ResolveServerOwnedRevision(incoming, existing);

        if (string.IsNullOrWhiteSpace(resolved))
        {
            if (!string.IsNullOrWhiteSpace(normalizedIncoming) &&
                !CouchDbRevisionHelper.IsValidRevision(normalizedIncoming))
            {
                return "rejected_invalid";
            }

            return "omitted";
        }

        if (!string.IsNullOrWhiteSpace(normalizedExisting) &&
            string.Equals(resolved, normalizedExisting, StringComparison.Ordinal))
        {
            return "resolved_existing";
        }

        return "preserved_incoming";
    }

    private static string DescribeIncomingRevisionHandling(string incoming)
    {
        var normalizedIncoming = CouchDbRevisionHelper.NormalizeOptionalRevision(incoming);
        if (string.IsNullOrWhiteSpace(normalizedIncoming))
        {
            return "omitted";
        }

        return CouchDbRevisionHelper.IsValidRevision(normalizedIncoming)
            ? "preserved_incoming"
            : "rejected_invalid";
    }
}
