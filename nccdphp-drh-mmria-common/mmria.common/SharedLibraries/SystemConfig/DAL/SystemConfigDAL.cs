#nullable enable

using System;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;

namespace mmria.common.SharedLibraries.SystemConfig.DAL;

/// <summary>
/// Data Access Layer for configuration CouchDB operations.
/// Contains ALL CouchDB calls against the configuration database.
/// The configuration database is shared (non-tenant-prefixed):
///   URL pattern: {dbConfig.url}/configuration/{configId}
/// </summary>
public sealed class SystemConfigDAL : IConfigurationRepository
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public SystemConfigDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient ?? throw new ArgumentNullException(nameof(couchDbHttpClient));
    }

    /// <inheritdoc />
    public async Task<string?> GetConfigurationJsonAsync(string configId, DBConfigurationDetail dbConfig)
    {
        if (dbConfig == null) throw new ArgumentNullException(nameof(dbConfig));

        string requestUrl = $"{dbConfig.url}/configuration/{configId}";
        try
        {
            return await _couchDbHttpClient.ExecuteAsync(
                "GET",
                requestUrl,
                null,
                dbConfig.user_name,
                dbConfig.user_value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SystemConfigDAL.GetConfigurationJsonAsync failed for '{configId}': {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ConfigurationSet?> GetConfigurationSetAsync(string configId, DBConfigurationDetail dbConfig, int timeoutSeconds = 20)
    {
        if (dbConfig == null) throw new ArgumentNullException(nameof(dbConfig));

        string requestUrl = $"{dbConfig.url.TrimEnd('/')}/configuration/{Uri.EscapeDataString(configId)}";
        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value,
            timeoutSeconds: timeoutSeconds,
            throwOnError: true);

        return Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigurationSet>(response);
    }

    /// <inheritdoc />
    public async Task<string?> PutConfigurationAsync(string configId, string configJson, DBConfigurationDetail dbConfig)
    {
        if (dbConfig == null) throw new ArgumentNullException(nameof(dbConfig));

        string requestUrl = $"{dbConfig.url}/configuration/{configId}";
        try
        {
            return await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                requestUrl,
                configJson,
                dbConfig.user_name,
                dbConfig.user_value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SystemConfigDAL.PutConfigurationAsync failed for '{configId}': {ex.Message}");
            return null;
        }
    }
}
