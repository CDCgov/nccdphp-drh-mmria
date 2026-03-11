using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Session.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.Session.DAL;

public class SessionDAL
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public SessionDAL(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<get_sortable_view_reponse_header<session>> GetSessionSortableViewAsync(
        int skip,
        int take,
        string sortView,
        bool hasSearchKey,
        bool descending,
        DBConfigurationDetail dbConfig)
    {
        var requestBuilder = new StringBuilder();
        requestBuilder.Append(dbConfig.url);
        requestBuilder.Append($"/{dbConfig.prefix}jurisdiction/_design/sortable/_view/{sortView}?");

        if (!hasSearchKey)
        {
            if (skip > -1)
            {
                requestBuilder.Append($"skip={skip}");
            }
            else
            {
                requestBuilder.Append("skip=0");
            }

            if (take > -1)
            {
                requestBuilder.Append($"&limit={take}");
            }

            if (descending)
            {
                requestBuilder.Append("&descending=true");
            }
        }
        else
        {
            requestBuilder.Append("skip=0");

            if (descending)
            {
                requestBuilder.Append("&descending=true");
            }
        }

        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestBuilder.ToString(), null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<get_sortable_view_reponse_header<session>>(response);
    }

    public async Task<document_put_response> CreateSessionAsync(Session_Message session, DBConfigurationDetail dbConfig)
    {
        string objectString = JsonConvert.SerializeObject(session, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}session/{session._id}";

        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, objectString, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }

    public async Task SaveSessionEventAsync(session_event sessionEvent, DBConfigurationDetail dbConfig)
    {
        string objectString = JsonConvert.SerializeObject(sessionEvent, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}session/{sessionEvent._id}";
        await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, objectString, dbConfig.user_name, dbConfig.user_value, "application/json");
    }

    public async Task<session_response> GetSessionDatabaseAsync(DBConfigurationDetail dbConfig)
    {
        string requestString = $"{dbConfig.url}/{dbConfig.prefix}session";
        WebRequest request = WebRequest.Create(new Uri(requestString));
        request.PreAuthenticate = false;

        using WebResponse response = await request.GetResponseAsync();
        string responseFromServer = await ReadResponseAsync(response);
        return JsonConvert.DeserializeObject<session_response>(responseFromServer);
    }

    public async Task<session> GetSessionDocumentAsync(string id, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}session/{id}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<session>(response);
    }

    public async Task<document_put_response> SaveSessionAsync(session session, DBConfigurationDetail dbConfig)
    {
        string objectString = JsonConvert.SerializeObject(session, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}session/{session._id}";
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, objectString, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<session_response> GetCouchDbSessionAsync(string authSessionValue, DBConfigurationDetail dbConfig)
    {
        string requestString = $"{dbConfig.url}/_session";
        WebRequest request = WebRequest.Create(new Uri(requestString));
        request.PreAuthenticate = false;

        if (!string.IsNullOrWhiteSpace(authSessionValue))
        {
            request.Headers.Add("Cookie", "AuthSession=" + authSessionValue);
            request.Headers.Add("X-CouchDB-WWW-Authenticate", authSessionValue);
        }

        using WebResponse response = await request.GetResponseAsync();
        string responseFromServer = await ReadResponseAsync(response);
        session_response result = JsonConvert.DeserializeObject<session_response>(responseFromServer);

        if (response.Headers["Set-Cookie"] != null)
        {
            result.auth_session = ExtractAuthSession(response.Headers["Set-Cookie"]);
        }

        return result;
    }

    public async Task<login_response> LoginToCouchDbSessionAsync(DBConfigurationDetail dbConfig)
    {
        string postData = $"name={dbConfig.user_name}&password={dbConfig.user_value}";
        byte[] postByteArray = Encoding.ASCII.GetBytes(postData);

        string requestString = $"{dbConfig.url}/_session";
        WebRequest request = WebRequest.Create(new Uri(requestString));
        request.PreAuthenticate = false;
        request.Method = "POST";
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = postByteArray.Length;

        using (Stream stream = request.GetRequestStream())
        {
            stream.Write(postByteArray, 0, postByteArray.Length);
        }

        using WebResponse response = await request.GetResponseAsync();
        string responseFromServer = await ReadResponseAsync(response);
        login_response result = JsonConvert.DeserializeObject<login_response>(responseFromServer);
        result.auth_session = ExtractAuthSession(response.Headers["Set-Cookie"]);
        return result;
    }

    public async Task<get_sortable_view_reponse_header<session_event>> GetSessionEventsByUserIdAsync(string userName, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}session/_design/session_event_sortable/_view/by_user_id?startkey=\"{userName}\"&endkey=\"{userName}\"";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<get_sortable_view_reponse_header<session_event>>(response);
    }

    private static async Task<string> ReadResponseAsync(WebResponse response)
    {
        using Stream dataStream = response.GetResponseStream();
        using StreamReader reader = new StreamReader(dataStream);
        return await reader.ReadToEndAsync();
    }

    private static string ExtractAuthSession(string setCookieHeader)
    {
        if (string.IsNullOrWhiteSpace(setCookieHeader))
        {
            return string.Empty;
        }

        string[] setCookie = setCookieHeader.Split(';');
        string[] authArray = setCookie[0].Split('=');
        if (authArray.Length > 1)
        {
            return authArray[1];
        }

        return string.Empty;
    }
}
