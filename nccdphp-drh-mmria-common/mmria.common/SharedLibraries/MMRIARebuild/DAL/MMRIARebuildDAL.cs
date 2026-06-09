using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.getset;
using mmria.common.SharedLibraries.MMRIARebuild.Model;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.MMRIARebuild.DAL;

public sealed class MMRIARebuildDAL
{
    private const string StartupRebuildDatabaseName = "db_rebuild";
    private const string StartupRebuildSecurityPayload = "{\"admins\":{\"names\":[],\"roles\":[\"form_designer\"]},\"members\":{\"names\":[],\"roles\":[\"abstractor\",\"data_analyst\",\"timer\"]}}";

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
        var response = await _couchDbHttpClient.ExecuteAsync(
            "POST",
            serviceUrl,
            objectString,
            "application/json",
            new CouchDbRequestOptions
            {
                VitalServiceKey = vitalServiceKey
            });

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

    public async Task EnsureRebuildDatabaseExistsAsync(mmria.common.couchdb.DBConfigurationDetail dbConfig)
    {
        if (dbConfig == null || string.IsNullOrWhiteSpace(dbConfig.url))
        {
            return;
        }

        string databaseUrl = GetRebuildDatabaseUrl(dbConfig);

        try
        {
            await _couchDbHttpClient.ExecuteAsync(
                "HEAD",
                databaseUrl,
                null,
                dbConfig.user_name,
                dbConfig.user_value,
                throwOnError: true);
        }
        catch (Exception)
        {
            try
            {
                await _couchDbHttpClient.ExecuteAsync("PUT", databaseUrl, null, dbConfig.user_name, dbConfig.user_value);
            }
            catch (Exception)
            {
            }
        }

        try
        {
            await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                databaseUrl + "/_security",
                StartupRebuildSecurityPayload,
                dbConfig.user_name,
                dbConfig.user_value);
        }
        catch (Exception securityEx)
        {
            System.Console.WriteLine($"Failed to configure {dbConfig.prefix}{StartupRebuildDatabaseName}/_security: {securityEx.Message}");
        }
    }

    public async Task<DurableTenantRebuildState> GetActiveRebuildAsync(
        mmria.common.couchdb.DBConfigurationDetail dbConfig,
        string tenant)
    {
        if (dbConfig == null || string.IsNullOrWhiteSpace(tenant))
        {
            return null;
        }

        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            $"{GetRebuildDatabaseUrl(dbConfig)}/{GetActiveDocumentId(tenant)}",
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

        return payload.ToObject<DurableTenantRebuildState>();
    }

    public async Task<bool> SaveActiveRebuildAsync(
        mmria.common.couchdb.DBConfigurationDetail dbConfig,
        DurableTenantRebuildState state)
    {
        if (dbConfig == null || state == null || string.IsNullOrWhiteSpace(state._id))
        {
            return false;
        }

        string payload = Newtonsoft.Json.JsonConvert.SerializeObject(
            state,
            new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            });

        string response = await _couchDbHttpClient.ExecuteAsync(
            "PUT",
            $"{GetRebuildDatabaseUrl(dbConfig)}/{Uri.EscapeDataString(state._id)}",
            payload,
            dbConfig.user_name,
            dbConfig.user_value);

        return IsOkPutResponse(response, out string rev, out _);
    }

    public async Task<bool> MutateActiveRebuildAsync(
        mmria.common.couchdb.DBConfigurationDetail dbConfig,
        string tenant,
        string ownerId,
        bool requireCurrentOwner,
        Action<DurableTenantRebuildState> mutate)
    {
        if (dbConfig == null || string.IsNullOrWhiteSpace(tenant) || mutate == null)
        {
            return false;
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            var state = await GetActiveRebuildAsync(dbConfig, tenant);
            if (state == null)
            {
                return false;
            }

            if (requireCurrentOwner &&
                !string.Equals(state.owner_id, ownerId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            mutate(state);
            state._id = GetActiveDocumentId(tenant);
            state.tenant = tenant;
            state.last_updated_utc = DateTime.UtcNow.ToString("o");

            string payload = Newtonsoft.Json.JsonConvert.SerializeObject(
                state,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                });

            string response = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                $"{GetRebuildDatabaseUrl(dbConfig)}/{Uri.EscapeDataString(state._id)}",
                payload,
                dbConfig.user_name,
                dbConfig.user_value);

            if (IsOkPutResponse(response, out _, out string error))
            {
                return true;
            }

            if (!string.Equals(error, "conflict", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return false;
    }

    public async Task SaveRunHistoryAsync(
        mmria.common.couchdb.DBConfigurationDetail dbConfig,
        DurableTenantRebuildRunHistory history)
    {
        if (dbConfig == null || history == null || string.IsNullOrWhiteSpace(history._id))
        {
            return;
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (string.IsNullOrWhiteSpace(history._rev))
            {
                history._rev = await TryGetDocumentRevisionAsync(dbConfig, history._id);
            }

            string payload = Newtonsoft.Json.JsonConvert.SerializeObject(
                history,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                });

            string response = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                $"{GetRebuildDatabaseUrl(dbConfig)}/{Uri.EscapeDataString(history._id)}",
                payload,
                dbConfig.user_name,
                dbConfig.user_value);

            if (IsOkPutResponse(response, out string rev, out string error))
            {
                history._rev = rev;
                return;
            }

            if (!string.Equals(error, "conflict", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            history._rev = null;
        }
    }

    public static string GetActiveDocumentId(string tenant)
    {
        string normalized = string.IsNullOrWhiteSpace(tenant) ? "unknown" : tenant.Trim().ToLowerInvariant();
        var builder = new System.Text.StringBuilder(normalized.Length);
        foreach (char value in normalized)
        {
            builder.Append(char.IsLetterOrDigit(value) || value == '-' || value == '_' ? value : '-');
        }

        return "tenant-rebuild-active-" + builder.ToString().Trim('-');
    }

    public static string GetRunHistoryDocumentId(string runId)
    {
        return "tenant-rebuild-run-" + (string.IsNullOrWhiteSpace(runId) ? "unknown" : runId.Trim());
    }

    private static string GetRebuildDatabaseUrl(mmria.common.couchdb.DBConfigurationDetail dbConfig)
    {
        return $"{dbConfig.url}/{dbConfig.prefix}{StartupRebuildDatabaseName}";
    }

    private async Task<string> TryGetDocumentRevisionAsync(
        mmria.common.couchdb.DBConfigurationDetail dbConfig,
        string documentId)
    {
        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            $"{GetRebuildDatabaseUrl(dbConfig)}/{Uri.EscapeDataString(documentId)}",
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

        return payload.Value<string>("_rev");
    }

    private static bool IsOkPutResponse(string response, out string rev, out string error)
    {
        rev = null;
        error = null;

        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        var payload = JObject.Parse(response);
        rev = payload.Value<string>("rev");
        error = payload.Value<string>("error");
        return payload.Value<bool?>("ok") == true;
    }
}
