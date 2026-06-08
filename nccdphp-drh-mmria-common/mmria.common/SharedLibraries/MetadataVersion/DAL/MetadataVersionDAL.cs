using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace mmria.common.SharedLibraries.MetadataVersion.DAL;

public sealed class MetadataVersionDAL
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public MetadataVersionDAL(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<ExpandoObject> GetExpandoDocumentAsync(
        string requestUrl,
        string userName,
        string userValue)
    {
        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, userName, userValue);
        return JsonConvert.DeserializeObject<ExpandoObject>(response, new ExpandoObjectConverter());
    }

    public async Task<string> GetStringAsync(
        string requestUrl,
        string userName,
        string userValue)
    {
        return await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, userName, userValue);
    }

    public async Task<string> GetStringWithOptionsAsync(
        string requestUrl,
        string contentType,
        CouchDbRequestOptions requestOptions)
    {
        return await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, contentType, requestOptions);
    }

    public async Task<T> GetDocumentAsync<T>(
        string requestUrl,
        string userName,
        string userValue,
        JsonSerializerSettings settings = null)
    {
        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, userName, userValue);
        return settings == null
            ? JsonConvert.DeserializeObject<T>(response)
            : JsonConvert.DeserializeObject<T>(response, settings);
    }

    public async Task<get_response_header<T>> GetAllDocsAsync<T>(
        string requestUrl,
        string userName,
        string userValue,
        JsonSerializerSettings settings)
    {
        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, userName, userValue);
        return JsonConvert.DeserializeObject<get_response_header<T>>(response, settings);
    }

    public async Task<document_put_response> PutJsonAsync(
        string requestUrl,
        string json,
        string userName,
        string userValue)
    {
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, json, userName, userValue);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<document_put_response> PutTextAsync(
        string requestUrl,
        string content,
        string userName,
        string userValue,
        CouchDbRequestOptions requestOptions = null)
    {
        requestOptions ??= new CouchDbRequestOptions();
        if (string.IsNullOrWhiteSpace(requestOptions.UserName) && string.IsNullOrWhiteSpace(requestOptions.Password))
        {
            requestOptions = new CouchDbRequestOptions
            {
                UserName = userName,
                Password = userValue,
                BearerToken = requestOptions.BearerToken,
                AuthSessionValue = requestOptions.AuthSessionValue,
                IfMatch = requestOptions.IfMatch,
                VitalServiceKey = requestOptions.VitalServiceKey,
                SafeHeaders = requestOptions.SafeHeaders,
                TimeoutSeconds = requestOptions.TimeoutSeconds,
                ThrowOnError = requestOptions.ThrowOnError,
                ClientName = requestOptions.ClientName
            };
        }

        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, content, "text/*", requestOptions);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<ExpandoObject> DeleteDocumentAsync(
        string requestUrl,
        string userName,
        string userValue)
    {
        string response = await _couchDbHttpClient.ExecuteAsync("DELETE", requestUrl, null, userName, userValue);
        return JsonConvert.DeserializeObject<ExpandoObject>(response, new ExpandoObjectConverter());
    }

    public async Task<string> GetRevisionAsync(
        string requestUrl,
        string userName,
        string userValue)
    {
        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, userName, userValue);
        var result = JsonConvert.DeserializeObject<ExpandoObject>(response, new ExpandoObjectConverter());
        IDictionary<string, object> updater = result as IDictionary<string, object>;
        if (updater != null && updater.ContainsKey("_rev"))
        {
            return updater["_rev"]?.ToString();
        }

        return null;
    }
}
