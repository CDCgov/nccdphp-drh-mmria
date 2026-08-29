#nullable enable

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using mmria.common.getset;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Session;

namespace mmria.common.SharedLibraries.Account.DAL;

/// <summary>
/// Data Access Layer for Account operations.
/// Contains ALL CouchDB calls for authentication, session events, and session management.
/// No business logic - only data operations.
/// </summary>
public class AccountDAL : mmria.common.SharedLibraries.Account.IUserRepository
{
    private static readonly System.Text.Json.JsonSerializerOptions SensitiveJsonPayloadOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly CouchDbHttpClient _httpClient;
    private readonly ISessionRepository _sessionRepository;

    public AccountDAL(CouchDbHttpClient httpClient, ISessionRepository sessionRepository)
    {
        _httpClient = httpClient;
        _sessionRepository = sessionRepository;
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
            userName = NormalizeUserName(userName);
            if (string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            var userDocId = $"org.couchdb.user:{userName}";
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
    /// Authenticate with the CouchDB session endpoint using the supplied credentials.
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
            userName = NormalizeUserName(userName);
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
        byte[]? userBytes = null;
        byte[]? passwordBytes = null;

        try
        {
            userBytes = Encoding.UTF8.GetBytes(userName);
            passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
            var payloadBytes = GC.AllocateUninitializedArray<byte>(
                "name=".Length +
                GetFormUrlEncodedLength(userBytes) +
                "&password=".Length +
                GetFormUrlEncodedLength(passwordBytes));

            var offset = 0;
            WriteAscii(payloadBytes, ref offset, "name=");
            WriteFormUrlEncoded(payloadBytes, userBytes, ref offset);
            WriteAscii(payloadBytes, ref offset, "&password=");
            WriteFormUrlEncoded(payloadBytes, passwordBytes, ref offset);

            return payloadBytes;
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

    private static int GetFormUrlEncodedLength(ReadOnlySpan<byte> bytes)
    {
        var length = 0;
        foreach (var b in bytes)
        {
            length += IsUnreservedFormByte(b) || b == (byte)' ' ? 1 : 3;
        }

        return length;
    }

    private static void WriteAscii(Span<byte> destination, ref int offset, string text)
    {
        offset += Encoding.ASCII.GetBytes(text, destination.Slice(offset));
    }

    private static void WriteFormUrlEncoded(Span<byte> destination, ReadOnlySpan<byte> bytes, ref int offset)
    {
        foreach (var b in bytes)
        {
            if (IsUnreservedFormByte(b))
            {
                destination[offset++] = b;
            }
            else if (b == (byte)' ')
            {
                destination[offset++] = (byte)'+';
            }
            else
            {
                destination[offset++] = (byte)'%';
                destination[offset++] = ToUpperHexByte((b >> 4) & 0xF);
                destination[offset++] = ToUpperHexByte(b & 0xF);
            }
        }
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

    private static string NormalizeUserName(string? userName)
    {
        return (userName ?? string.Empty).Trim().ToLowerInvariant();
    }

    // -----------------------------------------------------------------------
    // IUserRepository — canonical _users CRUD methods
    // -----------------------------------------------------------------------

    /// <summary>
    /// Get a CouchDB user document by full user_id (e.g. "org.couchdb.user:someone").
    /// </summary>
    public async Task<user> GetUserAsync(
        string userId,
        DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/_users/{userId}";
        string responseFromServer = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<user>(responseFromServer);
    }

    /// <summary>
    /// Check if a CouchDB user document exists by full user_id.
    /// Returns an empty user object if not found or on error — never returns null.
    /// </summary>
    public async Task<user> CheckUserAsync(
        string userId,
        DBConfigurationDetail dbConfig)
    {
        try
        {
            string requestUrl = $"{dbConfig.url}/_users/{userId}";
            string responseFromServer = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);

            if (string.IsNullOrWhiteSpace(responseFromServer))
            {
                return new user();
            }

            if (responseFromServer.Contains("\"error\"") && responseFromServer.Contains("not_found"))
            {
                return new user();
            }

            return JsonConvert.DeserializeObject<user>(responseFromServer) ?? new user();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new user();
        }
    }

    /// <summary>
    /// Create or update a CouchDB user document via PUT.
    /// </summary>
    public async Task<document_put_response> PutUserAsync(
        user user,
        DBConfigurationDetail dbConfig)
    {
        string userDbUrl = $"{dbConfig.url}/_users/{user._id}";
        string responseFromServer = await _httpClient.ExecuteJsonAsync(
            "PUT",
            userDbUrl,
            user,
            SensitiveJsonPayloadOptions,
            dbConfig.user_name,
            dbConfig.user_value,
            "application/json");
        return JsonConvert.DeserializeObject<document_put_response>(responseFromServer);
    }

    /// <summary>
    /// Delete a CouchDB user document via DELETE.
    /// </summary>
    public async Task<System.Dynamic.ExpandoObject> DeleteUserAsync(
        string userId,
        string rev,
        DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/_users/{userId}?rev={rev}";
        string responseFromServer = await _httpClient.ExecuteAsync("DELETE", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);
    }

    /// <summary>
    /// Get all users from _all_docs with pagination.
    /// </summary>
    public async Task<get_response_header<user>> GetAllUsersAsync(
        int skip,
        int take,
        DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/_users/_all_docs?include_docs=true&skip={skip}&limit={take}";
        string responseFromServer = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<get_response_header<user>>(responseFromServer);
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
            var viewResponse = await _sessionRepository.GetSessionEventsByUserIdAsync(userName, dbConfig);

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

            await _sessionRepository.SaveSessionEventAsync(sessionEvent, dbConfig);
            return true;
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
            return await _sessionRepository.SaveSessionRawAsync(sessionId, sessionJson, dbConfig);
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
            return await _sessionRepository.GetSessionDocumentRawAsync(sessionId, dbConfig);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get session document {sessionId}: {ex.Message}");
            return null;
        }
    }
}
