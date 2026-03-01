#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using mmria.common.getset;
using mmria.common.couchdb;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.Account.DAL;

/// <summary>
/// Data Access Layer for Account operations.
/// Contains ALL CouchDB calls for authentication, session events, and session management.
/// No business logic - only data operations.
/// </summary>
public class AccountDAL
{
    private readonly CouchDbHttpClient _httpClient;

    public AccountDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Get CouchDB user document to validate user exists and get their roles
    /// </summary>
    public async Task<user?> GetCouchDbUserAsync(
        string userName,
        DBConfigurationDetail dbConfig)
    {
        try
        {
            var userDocId = $"org.couchdb.user:{userName.ToLower()}";
            var url = $"{dbConfig.url}/_users/{System.Web.HttpUtility.HtmlEncode(userDocId)}";

            var response = await _httpClient.ExecuteAsync(
                "GET",
                url,
                null,
                dbConfig.user_name,
                dbConfig.user_value);

            var couchUser = JsonConvert.DeserializeObject<user>(response);
            return couchUser;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get CouchDB user {userName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Authenticate with CouchDB session endpoint - validates username and password.
    /// Uses CouchDbHttpClient with x-www-form-urlencoded payload.
    /// </summary>
    public async Task<login_response?> AuthenticateWithSessionAsync(
        string userName,
        string password,
        string couchDbUrl)
    {
        byte[]? payloadBytes = null;
        try
        {
            userName = (userName ?? string.Empty).Trim();
            var requestUrl = couchDbUrl.TrimEnd('/') + "/_session";

            payloadBytes = BuildSessionAuthFormPayload(userName, password);

            var responseFromServer = await _httpClient.ExecuteBytesAsync(
                "POST",
                requestUrl,
                payloadBytes,
                null,
                null,
                "application/x-www-form-urlencoded");

            var loginResponse = JsonConvert.DeserializeObject<login_response>(responseFromServer);
            if (loginResponse != null && loginResponse.ok)
            {
                if (string.IsNullOrWhiteSpace(loginResponse.name))
                {
                    loginResponse.name = userName;
                }
                return loginResponse;
            }

            return loginResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to authenticate user {userName}: {ex.Message}");
            return null;
        }
        finally
        {
            if (payloadBytes != null)
            {
                CryptographicOperations.ZeroMemory(payloadBytes);
            }
        }
    }

    private static byte[] BuildSessionAuthFormPayload(string userName, string? password)
    {
        password = password ?? string.Empty;
        byte[]? userBytes = null;
        byte[]? passwordBytes = null;

        try
        {
            userBytes = Encoding.UTF8.GetBytes(userName);
            passwordBytes = Encoding.UTF8.GetBytes(password);

            using var stream = new MemoryStream(userBytes.Length + passwordBytes.Length + 32);

            WriteAscii(stream, "name=");
            WriteFormUrlEncoded(stream, userBytes!);
            WriteAscii(stream, "&password=");
            WriteFormUrlEncoded(stream, passwordBytes!);

            return stream.ToArray();
        }
        finally
        {
            if (userBytes != null)
            {
                CryptographicOperations.ZeroMemory(userBytes);
            }

            if (passwordBytes != null)
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }
    }

    private static void WriteAscii(Stream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteFormUrlEncoded(Stream stream, byte[] bytes)
    {
        foreach (var b in bytes)
        {
            if (IsUnreservedFormByte(b))
            {
                stream.WriteByte(b);
            }
            else if (b == (byte)' ')
            {
                stream.WriteByte((byte)'+');
            }
            else
            {
                stream.WriteByte((byte)'%');
                stream.WriteByte(ToUpperHexByte((b >> 4) & 0xF));
                stream.WriteByte(ToUpperHexByte(b & 0xF));
            }
        }
        // Zero sensitive data immediately after use
        CryptographicOperations.ZeroMemory(bytes);
    }

    private static bool IsUnreservedFormByte(byte b)
    {
        return
            (b >= (byte)'a' && b <= (byte)'z') ||
            (b >= (byte)'A' && b <= (byte)'Z') ||
            (b >= (byte)'0' && b <= (byte)'9') ||
            b == (byte)'-' ||
            b == (byte)'_' ||
            b == (byte)'.' ||
            b == (byte)'*';
    }

    private static byte ToUpperHexByte(int value)
    {
        return (byte)(value < 10 ? value + '0' : value - 10 + 'A');
    }

    /// <summary>
    /// Get session events for a user within a time range to check for failed login attempts
    /// </summary>
    public async Task<List<session_event>> GetSessionEventsAsync(
        string userName,
        DBConfigurationDetail dbConfig)
    {
        try
        {
            var url = dbConfig.Get_Prefix_DB_Url(
                $"session/_design/session_event_sortable/_view/by_user_id?startkey=\"{userName}\"&endkey=\"{userName}\"");

            var response = await _httpClient.ExecuteAsync(
                "GET",
                url,
                null,
                dbConfig.user_name,
                dbConfig.user_value);

            var viewResponse = JsonConvert.DeserializeObject<
                get_sortable_view_reponse_header<session_event>>(response);

            if (viewResponse?.rows != null)
            {
                viewResponse.rows.Sort(
                    new Compare_Session_Event_By_DateCreated<session_event>());
                return viewResponse.rows.ConvertAll(r => r.value);
            }

            return new List<session_event>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get session events for {userName}: {ex.Message}");
            return new List<session_event>();
        }
    }

    /// <summary>
    /// Create session event document in database (failed/successful login audit trail)
    /// </summary>
    public async Task<bool> CreateSessionEventAsync(
        string sessionEventId,
        string userId,
        string actionResult,
        string ipAddress,
        DBConfigurationDetail dbConfig)
    {
        try
        {
            var sessionEvent = new session_event
            {
                _id = sessionEventId,
                user_id = userId,
                action_result = (session_event.session_event_action_enum)
                    Enum.Parse(typeof(session_event.session_event_action_enum), actionResult),
                date_created = DateTime.Now,
                ip = ipAddress
            };

            var json = JsonConvert.SerializeObject(sessionEvent);
            var url = dbConfig.Get_Prefix_DB_Url($"session/{sessionEventId}");

            var response = await _httpClient.ExecuteAsync(
                "PUT",
                url,
                json,
                dbConfig.user_name,
                dbConfig.user_value);

            var result = JsonConvert.DeserializeObject<document_put_response>(response);
            return result?.ok ?? false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create session event: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Create or update session document in database
    /// </summary>
    public async Task<document_put_response?> CreateSessionDocumentAsync(
        string sessionJson,
        string sessionId,
        DBConfigurationDetail dbConfig)
    {
        try
        {
            var url = dbConfig.Get_Prefix_DB_Url($"session/{sessionId}");

            var response = await _httpClient.ExecuteAsync(
                "PUT",
                url,
                sessionJson,
                dbConfig.user_name,
                dbConfig.user_value);

            var result = JsonConvert.DeserializeObject<document_put_response>(response);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create session document: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get existing session document from database (returns raw JSON)
    /// </summary>
    public async Task<string?> GetSessionDocumentAsync(
        string sessionId,
        DBConfigurationDetail dbConfig)
    {
        try
        {
            var url = dbConfig.Get_Prefix_DB_Url($"session/{sessionId}");

            var response = await _httpClient.ExecuteAsync(
                "GET",
                url,
                null,
                dbConfig.user_name,
                dbConfig.user_value);

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get session document {sessionId}: {ex.Message}");
            return null;
        }
    }
}
