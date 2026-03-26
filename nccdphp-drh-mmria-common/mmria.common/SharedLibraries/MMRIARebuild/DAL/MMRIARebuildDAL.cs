using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.MMRIARebuild.Model;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.MMRIARebuild.DAL;

public sealed class MMRIARebuildDAL
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public MMRIARebuildDAL(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient ?? throw new ArgumentNullException(nameof(couchDbHttpClient));
    }

    public async Task<MMRIARebuildResponse> PostRebuildToServiceAsync(
        string serviceUrl,
        string objectString,
        string vitalServiceKey)
    {
        var customHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "vital-service-key", vitalServiceKey }
        };

        var response = await _couchDbHttpClient.ExecuteAsync(
            "POST",
            serviceUrl,
            objectString,
            null,
            null,
            "application/json",
            customHeaders);

        return Newtonsoft.Json.JsonConvert.DeserializeObject<MMRIARebuildResponse>(response)
            ?? new MMRIARebuildResponse
            {
                success = false,
                status_code = 500,
                error = "The rebuild service returned an empty response."
            };
    }

    public async Task<JObject> TryGetStartupRunSummaryDocumentAsync(mmria.common.couchdb.DBConfigurationDetail dbConfig)
    {
        if (dbConfig == null)
        {
            return null;
        }

        string url = $"{dbConfig.url}/{dbConfig.prefix}db_rebuild/startup-run-summary";
        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            url,
            null,
            dbConfig.user_name,
            dbConfig.user_value);

        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var payload = JObject.Parse(response);
        if (string.Equals(payload.Value<string>("error"), "not_found", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return payload;
    }
}
