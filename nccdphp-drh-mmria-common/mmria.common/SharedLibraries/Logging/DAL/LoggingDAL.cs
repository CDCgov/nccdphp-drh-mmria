using System;
using System.Net;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Logging.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.Logging.DAL;

public sealed class LoggingDAL
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public LoggingDAL(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<string> GetLoggingByOfflineSessionViewJsonAsync(DBConfigurationDetail dbConfig)
    {
        string dbUrl = $"{dbConfig.url}/{dbConfig.prefix}logging";
        string requestUrl = $"{dbUrl}/_design/sortable/_view/by-offline-session";
        return await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<string> GetLogsViewJsonAsync(
        LoggingLogQuery query,
        bool restrictToCurrentUser,
        int limit,
        DBConfigurationDetail dbConfig)
    {
        query ??= new LoggingLogQuery();

        string dbUrl = $"{dbConfig.url}/{dbConfig.prefix}logging";
        string viewUrl;

        if ((!string.IsNullOrWhiteSpace(query.startDate) && query.startDate.ToLower() != "all") ||
            (!string.IsNullOrWhiteSpace(query.endDate) && query.endDate.ToLower() != "all"))
        {
            DateTime startDt = DateTime.MinValue;
            DateTime endDt = DateTime.MaxValue;
            bool hasStart = !string.IsNullOrWhiteSpace(query.startDate) && DateTime.TryParse(query.startDate, out startDt);
            bool hasEnd = !string.IsNullOrWhiteSpace(query.endDate) && DateTime.TryParse(query.endDate, out endDt);

            string startKeyIso;
            string endKeyIso;

            if (hasStart && hasEnd)
            {
                var later = startDt > endDt ? startDt : endDt;
                var earlier = startDt > endDt ? endDt : startDt;
                startKeyIso = later.ToString("o");
                endKeyIso = earlier.ToString("o");
            }
            else if (hasStart)
            {
                startKeyIso = DateTime.MaxValue.ToString("o");
                endKeyIso = startDt.ToString("o");
            }
            else if (hasEnd)
            {
                startKeyIso = endDt.ToString("o");
                endKeyIso = DateTime.MinValue.ToString("o");
            }
            else
            {
                startKeyIso = !string.IsNullOrWhiteSpace(query.endDate) ? query.endDate : DateTime.MaxValue.ToString("o");
                endKeyIso = !string.IsNullOrWhiteSpace(query.startDate) ? query.startDate : DateTime.MinValue.ToString("o");
            }

            var encodedStart = WebUtility.UrlEncode($"\"{startKeyIso}\"");
            var encodedEnd = WebUtility.UrlEncode($"\"{endKeyIso}\"");

            viewUrl = $"{dbUrl}/_design/sortable/_view/by-timestamp?include_docs=true&startkey={encodedStart}&endkey={encodedEnd}&descending=true&limit={limit}";
        }
        else if (!string.IsNullOrWhiteSpace(query.sessionId) && query.sessionId.ToLower() != "all")
        {
            var encodedKey = WebUtility.UrlEncode($"\"{query.sessionId}\"");
            viewUrl = $"{dbUrl}/_design/sortable/_view/by-offline-session?key={encodedKey}&include_docs=true&descending=true";
        }
        else if (!restrictToCurrentUser && !string.IsNullOrWhiteSpace(query.userName) && query.userName.ToLower() != "all")
        {
            var encodedKey = WebUtility.UrlEncode($"\"{query.userName}\"");
            viewUrl = $"{dbUrl}/_design/sortable/_view/by-user?key={encodedKey}&include_docs=true&descending=true";
        }
        else if (!string.IsNullOrWhiteSpace(query.context) && query.context.ToLower() != "all")
        {
            var encodedKey = WebUtility.UrlEncode($"\"{query.context}\"");
            viewUrl = $"{dbUrl}/_design/sortable/_view/by-context?key={encodedKey}&include_docs=true&descending=true";
        }
        else if (!string.IsNullOrWhiteSpace(query.level) && query.level.ToLower() != "all")
        {
            var encodedKey = WebUtility.UrlEncode($"\"{query.level.ToLower()}\"");
            viewUrl = $"{dbUrl}/_design/sortable/_view/by-level?key={encodedKey}&include_docs=true&descending=true";
        }
        else
        {
            viewUrl = $"{dbUrl}/_design/sortable/_view/all-fields?include_docs=true&limit={limit}&skip={query.skip}&descending=true";
        }

        return await _couchDbHttpClient.ExecuteAsync("GET", viewUrl, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<document_put_response> SaveLogEntryAsync(object logEntry, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}logging";
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        string requestBody = JsonConvert.SerializeObject(logEntry, settings);
        string response = await _couchDbHttpClient.ExecuteAsync("POST", requestUrl, requestBody, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }
}
