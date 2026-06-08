using System.Collections.Generic;
using System.Dynamic;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.ExportQueue.Model;

namespace mmria.common.SharedLibraries.ExportQueue.DAL;

public sealed class ExportQueueDAL
{
    private readonly CouchDbHttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExportQueueDAL(CouchDbHttpClient httpClient, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClient;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ExpandoObject> GetAllQueueDocumentsAsync(DBConfigurationDetail db_config)
    {
        string request_string = db_config.Get_Prefix_DB_Url("export_queue/_all_docs?include_docs=true");
        string response = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(response);
    }

    public async Task<T> GetQueueDocumentAsync<T>(string id, DBConfigurationDetail db_config)
    {
        string request_string = db_config.Get_Prefix_DB_Url("export_queue/" + id);
        string response = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(response);
    }

    public async Task<document_put_response> SaveQueueDocumentAsync(string id, string document_content, DBConfigurationDetail db_config)
    {
        string request_string = db_config.Get_Prefix_DB_Url("export_queue/" + id);
        string response = await _httpClient.ExecuteAsync("PUT", request_string, document_content, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<string> TriggerExportQueueServiceAsync(
        string service_url,
        string request_json,
        string vitalServiceKey)
    {
        return await _httpClient.ExecuteAsync(
            "POST",
            service_url,
            request_json,
            "application/json",
            new CouchDbRequestOptions
            {
                VitalServiceKey = vitalServiceKey
            });
    }

    public async Task<ExportQueueDownloadResult> DownloadExportFileAsync(
        Uri requestUri,
        string vitalServiceKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        var sanitizedVitalServiceKey = CouchDbHttpClient.SanitizeHeader(vitalServiceKey)?.Trim();
        if (!string.IsNullOrWhiteSpace(sanitizedVitalServiceKey))
        {
            request.Headers.Add("vital-service-key", sanitizedVitalServiceKey);
        }

        HttpResponseMessage serviceResponse = null;
        try
        {
            var client = _httpClientFactory.CreateClient(string.Empty);
            serviceResponse = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (serviceResponse.StatusCode == HttpStatusCode.NotFound)
            {
                serviceResponse.Dispose();
                return ExportQueueDownloadResult.NotFound();
            }

            if (!serviceResponse.IsSuccessStatusCode)
            {
                serviceResponse.Dispose();
                return ExportQueueDownloadResult.ServiceError();
            }

            try
            {
                var stream = await serviceResponse.Content.ReadAsStreamAsync(cancellationToken);
                var contentType = serviceResponse.Content.Headers.ContentType?.ToString();
                return ExportQueueDownloadResult.Success(stream, contentType, serviceResponse);
            }
            catch
            {
                serviceResponse.Dispose();
                return ExportQueueDownloadResult.Unreadable();
            }
        }
        catch
        {
            serviceResponse?.Dispose();
            throw;
        }
    }
}
