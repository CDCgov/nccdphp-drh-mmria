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
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.MMRIAServices.DAL;

public sealed class MMRIAServicesDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public MMRIAServicesDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient ?? throw new ArgumentNullException(nameof(couchDbHttpClient));
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

        string url = $"{couchdb_url}/vital_import/_all_docs?include_docs=true";
        try
        {
            var responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", url, null, timer_user_name, timer_value);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<alldocs_response<mmria.common.ije.Batch>>(responseFromServer);

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
        string put_url = $"{couchdb_url}/vital_import/{batch_id}";
        var responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", put_url, object_string, timer_user_name, timer_value);
        var put_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
        return put_result;
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
        string requestUrl = $"{couchDbUrl}/configuration/{configId}";

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = CouchDbHttpClient.CreateBasicAuthHeaderValue(userName, password);

        return httpClient.GetStringAsync(requestUrl).GetAwaiter().GetResult();
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

        string requestString = $"{GetMmrdsDatabaseUrl(dbInfo)}/_design/sortable/_view/by_date_created?skip=0&take=250000";

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

        string url = $"{GetMmrdsDatabaseUrl(dbInfo)}/{caseId}";

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

        string requestString = $"{GetMmrdsDatabaseUrl(dbInfo)}/_all_docs?include_docs=true";
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

        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            $"{dbInfo.url}/metadata/de-identified-export-list",
            null,
            dbInfo.user_name,
            dbInfo.user_value);

        var expandoObject = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(response);
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
        string request_string = $"{db_config.url}/metadata/populate-cdc-instance";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Populate_CDC_Instance>(response);
    }

    public async Task<mmria.common.model.couchdb.document_put_response> SavePopulateCDCInstanceDocumentAsync(
        string document_content,
        DBConfigurationDetail db_config)
    {
        string request_string = $"{db_config.url}/metadata/populate-cdc-instance";
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", request_string, document_content, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(response);
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

    private static string GetMmrdsDatabaseUrl(DBConfigurationDetail dbInfo)
    {
        return string.IsNullOrWhiteSpace(dbInfo?.prefix)
            ? $"{dbInfo?.url}/mmrds"
            : $"{dbInfo.url}/{dbInfo.prefix}_mmrds";
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
