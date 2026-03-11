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

        string request_string = $"{item_db_info.url}/{item_db_info.prefix}mmrds/_design/sortable/_view/by_date_created?skip=0&take=25000";
        Console.WriteLine($"Fetching existing records from: {request_string}");

        string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, item_db_info.user_name, item_db_info.user_value);
        Console.WriteLine($"Response length: {responseFromServer?.Length ?? 0}");

        mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);
        Console.WriteLine($"Parsed {case_view_response?.rows?.Count ?? 0} rows");

        foreach (mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
        {
            result.Add(cvi.value.record_id);

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
        string auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{userName}:{password}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

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

        string requestString = $"{dbInfo.url}/mmrds/_design/sortable/_view/by_date_created?skip=0&take=250000";
        if (!string.IsNullOrWhiteSpace(dbInfo.prefix))
        {
            requestString = $"{dbInfo.url}/{dbInfo.prefix}_mmrds/_design/sortable/_view/by_date_created?skip=0&take=250000";
        }

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

        string url = $"{dbInfo.url}/mmrds/{caseId}";
        if (!string.IsNullOrWhiteSpace(dbInfo.prefix))
        {
            url = $"{dbInfo.url}/{dbInfo.prefix}_mmrds/{caseId}";
        }

        string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbInfo.user_name, dbInfo.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(responseFromServer);
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
        var customHeaders = new Dictionary<string, string>
        {
            { "vital-service-key", vital_service_key }
        };

        var response = await _couchDbHttpClient.ExecuteAsync("GET", service_url, null, null, null, "application/json", customHeaders);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Populate_CDC_Instance_Record>(response);
    }

    public async Task<mmria.common.metadata.Populate_CDC_Instance> PutPopulateCDCInstanceToServiceAsync(
        string service_url,
        string object_string,
        string vital_service_key)
    {
        var customHeaders = new Dictionary<string, string>
        {
            { "vital-service-key", vital_service_key }
        };

        var response = await _couchDbHttpClient.ExecuteAsync("PUT", service_url, object_string, null, null, "application/json", customHeaders);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Populate_CDC_Instance>(response);
    }

}
