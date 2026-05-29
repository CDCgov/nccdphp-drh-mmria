using System;
using System.Net.Http;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;

namespace mmria.common.SharedLibraries.HealthDiagnostics.DAL;

public sealed class HealthDiagnosticsDAL
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HealthDiagnosticsDAL(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> UrlEndpointExistsAsync(
        DBConfigurationDetail dbConfig,
        string databaseName,
        string method = "HEAD")
    {
        try
        {
            using var request = new HttpRequestMessage(
                method == "HEAD" ? HttpMethod.Head : HttpMethod.Get,
                dbConfig.Get_Prefix_DB_Url(databaseName));

            if (!string.IsNullOrWhiteSpace(dbConfig.user_name) && !string.IsNullOrWhiteSpace(dbConfig.user_value))
            {
                request.Headers.Authorization = CouchDbHttpClient.CreateBasicAuthHeaderValue(dbConfig.user_name, dbConfig.user_value);
            }

            var client = _httpClientFactory.CreateClient(string.Empty);
            using var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
