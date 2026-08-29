using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.MMRIAServices.Model;
using mmria.common.SharedLibraries.MetadataVersion;
using mmria.common.SharedLibraries.VitalImport;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.MMRIAServices.DAL;

public sealed class MMRIAServicesDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.SharedLibraries.SystemConfig.IConfigurationRepository _configRepository;
    private readonly IMetadataRepository _metadataRepository;
    private readonly IVitalImportRepository _vitalImportRepository;

    public MMRIAServicesDAL(
        CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.SystemConfig.IConfigurationRepository configRepository,
        IMetadataRepository metadataRepository,
        IVitalImportRepository vitalImportRepository)
    {
        _couchDbHttpClient = couchDbHttpClient ?? throw new ArgumentNullException(nameof(couchDbHttpClient));
        _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
        _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
        _vitalImportRepository = vitalImportRepository ?? throw new ArgumentNullException(nameof(vitalImportRepository));
    }

    public async Task<case_view_response> GetCaseView(DBConfigurationDetail db_info, string search_key)
    {
        string request_string = $"{db_info.url}/{db_info.prefix}mmrds/_design/sortable/_view/by_last_name?skip=0&limit=100000&startkey=\"{search_key.ToLower()}\"&endkey=\"{search_key.ToUpper()}\"";

        try
        {
            string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, db_info.user_name, db_info.user_value, timeoutSeconds: 300);

            case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<case_view_response>(responseFromServer);


            string key_compare = search_key.ToLower().Trim(new char[] { '"' });

            case_view_response result = new case_view_response();
            result.offset = case_view_response.offset;
            result.total_rows = case_view_response.total_rows;

            foreach (case_view_item cvi in case_view_response.rows)
            {
                bool add_item = false;

                if (is_matching_search_text(cvi.value.last_name, key_compare))
                {
                    add_item = true;
                }

                if (add_item)
                {
                    result.rows.Add(cvi);
                }

            }


            result.total_rows = result.rows.Count;
            result.rows = result.rows.Skip(0).Take(100000).ToList();

            return result;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"MMRIAServicesDAL GetCaseView\nurl: {request_string}\n\nerror:\n{ex}");

        }


        return null;
    }

    public async Task<ExpandoObject> GetCaseById(DBConfigurationDetail db_info, string case_id)
    {
        try
        {
            string request_string = $"{db_info.url}/{db_info.prefix}mmrds/_all_docs?include_docs=true";

            if (!string.IsNullOrWhiteSpace(case_id))
            {
                request_string = $"{db_info.url}/{db_info.prefix}mmrds/{case_id}";
                string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, db_info.user_name, db_info.user_value);

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(responseFromServer);

                return result;

            }

        }
        catch (Exception ex)
        {

            Console.WriteLine($"MMRIAServicesDAL.GetCaseById\n{ex}");
        }

        return null;
    }

    public async Task<alldocs_response<mmria.common.ije.Batch>> GetBatchSet(
        string couchdb_url,
        string timer_user_name,
        string timer_value
    )
    {
        var result = new alldocs_response<mmria.common.ije.Batch>();

        try
        {
            var dbConfig = new DBConfigurationDetail { url = couchdb_url, user_name = timer_user_name, user_value = timer_value };
            result = await _vitalImportRepository.GetAllBatchesAsync(dbConfig);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    public async Task<mmria.common.model.couchdb.document_put_response> SaveBatchDocument(
        string couchdb_url,
        string batch_id,
        string object_string,
        string timer_user_name,
        string timer_value
    )
    {
        var dbConfig = new DBConfigurationDetail { url = couchdb_url, user_name = timer_user_name, user_value = timer_value };
        return await _vitalImportRepository.PutBatchDocumentAsync(batch_id, object_string, dbConfig);
    }

    public async Task<mmria.common.ije.Batch> Get_batch(
        string couchdb_url,
        string timer_user_name,
        string timer_value,
        string _id
    )
    {
        mmria.common.ije.Batch result = null;

        string put_url = $"{couchdb_url}/vital_import/{_id}";
        var responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", put_url, null, timer_user_name, timer_value);
        result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.ije.Batch>(responseFromServer);

        return result;
    }

    public async Task<bool> delete_batch_document(
        string couchdb_url,
        string timer_user_name,
        string timer_value,
        string _id,
        string _rev
    )
    {
        string put_url = $"{couchdb_url}/vital_import/{_id}?rev={_rev}";
        var responseFromServer = await _couchDbHttpClient.ExecuteAsync("DELETE", put_url, null, timer_user_name, timer_value);
        var delete_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);

        return true;
    }

    [Obsolete("Retired for cross-writer uniqueness by Story 29.7; SaveCaseAsync now enforces record_id uniqueness at write time. Delete after any utility callers migrate.")]
    public async Task<HashSet<string>> GetExistingRecordIds(mmria.common.couchdb.DBConfigurationDetail item_db_info)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if(item_db_info == null)
        {
            throw new ArgumentNullException(nameof(item_db_info));
        }

        if(string.IsNullOrWhiteSpace(item_db_info.url))
        {
            throw new ArgumentException("Database configuration URL is required.", nameof(item_db_info));
        }

        if(string.IsNullOrWhiteSpace(item_db_info.user_name) || string.IsNullOrWhiteSpace(item_db_info.user_value))
        {
            throw new ArgumentException("Database configuration credentials are required.", nameof(item_db_info));
        }

        string request_string = $"{item_db_info.url}/{item_db_info.prefix}mmrds/_design/sortable/_view/by_date_created?skip=0&take=25000";
        Console.WriteLine($"Fetching existing records from: {request_string}");

        string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, item_db_info.user_name, item_db_info.user_value);
        Console.WriteLine($"Response length: {responseFromServer?.Length ?? 0}");

        mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);
        Console.WriteLine($"Parsed {case_view_response?.rows?.Count ?? 0} rows");

        if(case_view_response?.rows == null)
        {
            return result;
        }

        foreach (mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
        {
            var record_id = cvi?.value?.record_id;
            if(!string.IsNullOrWhiteSpace(record_id))
            {
                result.Add(record_id);
            }
        }

        return result;
    }

    private bool is_matching_search_text(string p_val1, string p_val2)
    {
        var result = false;

        if
        (
            !string.IsNullOrWhiteSpace(p_val1) &&
            (
                p_val2.IndexOf(p_val1, StringComparison.OrdinalIgnoreCase) > -1 ||
                p_val1.IndexOf(p_val2, StringComparison.OrdinalIgnoreCase) > -1
            )
        )
        {
            result = true;
        }

        return result;
    }

    public string GetConfigurationDocumentJson(
        string couchDbUrl,
        string configId,
        string userName,
        string password
    )
    {
        var tempDbConfig = new DBConfigurationDetail { url = couchDbUrl, user_name = userName, user_value = password };
        return _configRepository.GetConfigurationJsonAsync(configId, tempDbConfig).GetAwaiter().GetResult();
    }

    public async Task<ConfigurationSet> GetConfigurationDocumentAsync(
        string couchDbUrl,
        string configId,
        string userName,
        string password,
        int timeoutSeconds = 20)
    {
        var tempDbConfig = new DBConfigurationDetail { url = couchDbUrl, user_name = userName, user_value = password };
        return await _configRepository.GetConfigurationSetAsync(configId, tempDbConfig, timeoutSeconds);
    }

    public async Task<ConfigurationSet> GetConfigurationDocumentAsync(
        DBConfigurationDetail dbConfig,
        string configId,
        int timeoutSeconds = 20)
    {
        if (dbConfig == null)
        {
            throw new ArgumentNullException(nameof(dbConfig));
        }

        return await _configRepository.GetConfigurationSetAsync(configId, dbConfig, timeoutSeconds);
    }

    public async Task<string> ExecuteDatabaseCall(
        string method,
        string url,
        string body,
        string userName,
        string userValue
    )
    {
        return await _couchDbHttpClient.ExecuteAsync(method, url, body, userName, userValue);
    }

    public async Task<HashSet<string>> GetCaseIdsByDateCreated(DBConfigurationDetail dbInfo)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string requestString = dbInfo.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_date_created?skip=0&take=250000");

        string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", requestString, null, dbInfo.user_name, dbInfo.user_value);
        var caseViewResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

        if (caseViewResponse?.rows == null)
        {
            return result;
        }

        foreach (var cvi in caseViewResponse.rows)
        {
            if (!string.IsNullOrWhiteSpace(cvi?.id))
            {
                result.Add(cvi.id);
            }
        }

        return result;
    }

    public async Task<ExpandoObject> GetCaseDocumentForPopulateCDC(DBConfigurationDetail dbInfo, string caseId)
    {
        if (string.IsNullOrWhiteSpace(caseId))
        {
            return null;
        }

        string url = dbInfo.Get_Prefix_DB_Url($"mmrds/{caseId}");

        string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbInfo.user_name, dbInfo.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(responseFromServer);
    }

    public async Task<List<ExpandoObject>> GetCaseDocumentsForPopulateCDC(DBConfigurationDetail dbInfo, IEnumerable<string> caseIds)
    {
        var result = new List<ExpandoObject>();
        if (dbInfo == null || caseIds == null)
        {
            return result;
        }

        var idList = caseIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (idList.Count == 0)
        {
            return result;
        }

        string requestString = dbInfo.Get_Prefix_DB_Url("mmrds/_all_docs?include_docs=true");
        string requestBody = Newtonsoft.Json.JsonConvert.SerializeObject(new { keys = idList });
        string responseFromServer = await _couchDbHttpClient.ExecuteAsync("POST", requestString, requestBody, dbInfo.user_name, dbInfo.user_value);
        var allDocsResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<PopulateCdcAllDocsResponse>(responseFromServer);

        if (allDocsResponse?.rows == null)
        {
            return result;
        }

        foreach (var row in allDocsResponse.rows)
        {
            if (row?.doc == null || string.IsNullOrWhiteSpace(row.id))
            {
                continue;
            }

            if (row.id.StartsWith("_design", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(row.doc);
        }

        return result;
    }

    public async Task<Dictionary<string, HashSet<string>>> GetDeIdentifiedExportListPathMapAsync(DBConfigurationDetail dbInfo)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (dbInfo == null)
        {
            return result;
        }

        var expandoObject = await _metadataRepository.GetDeIdentifiedExportListAsync(dbInfo);
        var document = expandoObject as IDictionary<string, object>;
        if
        (
            document == null ||
            !document.TryGetValue("name_path_list", out var namePathListValue) ||
            namePathListValue is not IDictionary<string, object> namePathList
        )
        {
            return result;
        }

        foreach (var kvp in namePathList)
        {
            var pathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (kvp.Value is IList<object> pathList)
            {
                foreach (var pathItem in pathList)
                {
                    var path = pathItem?.ToString();
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        pathSet.Add(path);
                    }
                }
            }

            result[kvp.Key] = pathSet;
        }

        return result;
    }

    public async Task<List<document_put_response>> BulkSavePopulateCdcDocumentsAsync(
        IEnumerable<string> documentJsonList,
        DBConfigurationDetail cdcConnection)
    {
        var result = new List<document_put_response>();
        if (cdcConnection == null || documentJsonList == null)
        {
            return result;
        }

        var docs = new JArray();
        foreach (var documentJson in documentJsonList)
        {
            if (string.IsNullOrWhiteSpace(documentJson))
            {
                continue;
            }

            docs.Add(JObject.Parse(documentJson));
        }

        if (docs.Count == 0)
        {
            return result;
        }

        string requestBody = new JObject
        {
            ["docs"] = docs
        }.ToString(Newtonsoft.Json.Formatting.None);

        string response = await _couchDbHttpClient.ExecuteAsync(
            "POST",
            $"{cdcConnection.url}/mmrds/_bulk_docs",
            requestBody,
            cdcConnection.user_name,
            cdcConnection.user_value);

        return Newtonsoft.Json.JsonConvert.DeserializeObject<List<document_put_response>>(response) ?? result;
    }

    public async Task<mmria.common.metadata.Populate_CDC_Instance> GetPopulateCDCInstanceDocumentAsync(DBConfigurationDetail db_config)
    {
        return await _metadataRepository.GetPopulateCDCInstanceDocumentAsync(db_config);
    }

    public async Task<mmria.common.model.couchdb.document_put_response> SavePopulateCDCInstanceDocumentAsync(
        string document_content,
        DBConfigurationDetail db_config)
    {
        var doc = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Populate_CDC_Instance>(document_content);
        return await _metadataRepository.SavePopulateCDCInstanceDocumentAsync(doc, db_config);
    }

    public async Task<mmria.common.metadata.Populate_CDC_Instance_Record> GetPopulateCDCInstanceFromServiceAsync(
        string service_url,
        string vital_service_key)
    {
        var response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            service_url,
            null,
            "application/json",
            new CouchDbRequestOptions
            {
                VitalServiceKey = vital_service_key
            });
        return Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Populate_CDC_Instance_Record>(response);
    }

    public async Task<TenantDatabaseCountsResponse> GetTenantDatabaseCountsFromServiceAsync(
        string service_url,
        string vital_service_key)
    {
        var response = await _couchDbHttpClient.ExecuteForResponseAsync(
            "GET",
            service_url,
            null,
            "application/json",
            new CouchDbRequestOptions
            {
                VitalServiceKey = vital_service_key,
                ThrowOnError = false,
                TimeoutSeconds = 120
            });

        if (response.StatusCode < 200 || response.StatusCode >= 300)
        {
            string detail = TryExtractErrorDetail(response.Body);
            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(detail)
                    ? $"TenantDatabaseCounts service returned HTTP {response.StatusCode}."
                    : $"TenantDatabaseCounts service returned HTTP {response.StatusCode}: {detail}");
        }

        return Newtonsoft.Json.JsonConvert.DeserializeObject<TenantDatabaseCountsResponse>(response.Body);
    }

    public async Task<mmria.common.metadata.Populate_CDC_Instance> PutPopulateCDCInstanceToServiceAsync(
        string service_url,
        string object_string,
        string vital_service_key)
    {
        var response = await _couchDbHttpClient.ExecuteAsync(
            "PUT",
            service_url,
            object_string,
            "application/json",
            new CouchDbRequestOptions
            {
                VitalServiceKey = vital_service_key
            });
        return Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Populate_CDC_Instance>(response);
    }

    public async Task<JObject> GetDatabaseMetadataAsync(
        string databaseUrl,
        string userName,
        string userValue,
        int timeoutSeconds = 20)
    {
        var response = await _couchDbHttpClient.ExecuteForResponseAsync(
            "GET",
            databaseUrl,
            null,
            userName,
            userValue,
            timeoutSeconds: timeoutSeconds,
            throwOnError: false);

        if (string.IsNullOrWhiteSpace(response.Body))
        {
            return new JObject
            {
                ["error"] = $"HTTP {response.StatusCode}"
            };
        }

        return JObject.Parse(response.Body);
    }

    public async Task<int> GetDesignDocumentCountAsync(
        string databaseUrl,
        string userName,
        string userValue,
        int timeoutSeconds = 20)
    {
        string requestUrl = $"{databaseUrl}/_all_docs?startkey=%22_design/%22&endkey=%22_design0%22";
        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            userName,
            userValue,
            timeoutSeconds: timeoutSeconds,
            throwOnError: true);

        var allDocs = Newtonsoft.Json.JsonConvert.DeserializeObject<PopulateCdcAllDocsResponse>(response);
        return allDocs?.rows?.Length ?? 0;
    }

    public async Task<List<(string id, DateTime? dateLastCheckedOut)>> GetOpenCaseStubsAsync(
        string databaseUrl,
        string userName,
        string userValue,
        int timeoutSeconds = 20)
    {
        string requestUrl = $"{databaseUrl}/_find";
        string requestBody =
            "{\"selector\":{\"checked_out_by_tab_id\":{\"$exists\":true,\"$ne\":\"\"}," +
            "\"last_checked_out_by\":{\"$exists\":true,\"$ne\":\"\"}}," +
            "\"fields\":[\"_id\",\"date_last_checked_out\"],\"limit\":1000}";

        string response = await _couchDbHttpClient.ExecuteAsync(
            "POST",
            requestUrl,
            requestBody,
            userName,
            userValue,
            timeoutSeconds: timeoutSeconds,
            throwOnError: true);

        var result = new List<(string id, DateTime? dateLastCheckedOut)>();
        var payload = Newtonsoft.Json.Linq.JObject.Parse(response);
        var docs = payload["docs"] as Newtonsoft.Json.Linq.JArray;
        if (docs == null) return result;

        foreach (var doc in docs)
        {
            var id = doc.Value<string>("_id");
            DateTime? dateLastCheckedOut = null;
            var rawDate = doc["date_last_checked_out"];
            if (rawDate != null && rawDate.Type != Newtonsoft.Json.Linq.JTokenType.Null)
            {
                if (DateTime.TryParse(rawDate.ToString(), null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                {
                    dateLastCheckedOut = parsed.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                        : parsed.ToUniversalTime();
                }
            }
            result.Add((id, dateLastCheckedOut));
        }

        return result;
    }

    private static string TryExtractErrorDetail(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            var payload = JObject.Parse(responseBody);
            string error = payload.Value<string>("error");
            string reason = payload.Value<string>("reason");
            return string.Join(
                " ",
                new[] { error, reason }
                    .Where(item => !string.IsNullOrWhiteSpace(item)));
        }
        catch
        {
            return null;
        }
    }

}

file sealed class PopulateCdcAllDocsRow
{
    public string id { get; set; }
    public ExpandoObject doc { get; set; }
}

file sealed class PopulateCdcAllDocsResponse
{
    public int? offset { get; set; }
    public PopulateCdcAllDocsRow[] rows { get; set; }
    public int total_rows { get; set; }
}
