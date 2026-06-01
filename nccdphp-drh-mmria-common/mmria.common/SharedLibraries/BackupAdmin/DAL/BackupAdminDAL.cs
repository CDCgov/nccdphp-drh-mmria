using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.BackupAdmin.Model;
using mmria.common.getset;

namespace mmria.common.SharedLibraries.BackupAdmin.DAL;

public sealed class BackupAdminDAL
{
    public const string HttpClientName = "BackupAdmin";

    private readonly CouchDbHttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;

    public BackupAdminDAL(CouchDbHttpClient httpClient, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClient;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetAsync(string url, string vitalServiceKey)
    {
        return await _httpClient.ExecuteAsync(
            "GET",
            url,
            null,
            "application/json",
            new CouchDbRequestOptions
            {
                VitalServiceKey = vitalServiceKey
            });
    }

    public async Task<BackupAdminDownloadResult> DownloadFileAsync(Uri requestUri, string vitalServiceKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("vital-service-key", vitalServiceKey);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return BackupAdminDownloadResult.NotFound();
        }

        if (!response.IsSuccessStatusCode)
        {
            return BackupAdminDownloadResult.ServiceError((int)response.StatusCode);
        }

        using var content = response.Content;
        return BackupAdminDownloadResult.Success(await content.ReadAsByteArrayAsync());
    }
}
