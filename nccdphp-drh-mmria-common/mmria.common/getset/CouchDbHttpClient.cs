using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mmria.common.getset;

public sealed class CouchDbHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CouchDbHttpClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<string> ExecuteAsync
    (
        string method,
        string url,
        string payload = null,
        string userName = null,
        string password = null,
        string contentType = "application/json",
        System.Collections.Generic.Dictionary<string, string> customHeaders = null,
        bool allowRedirect = true,
        int? timeoutSeconds = null,
        bool throwOnError = false
    )
    {
        ValidateUrl(url);

        var httpClient = _httpClientFactory.CreateClient("CouchDb");
        
        // Set timeout if specified (default is 100 seconds)
        if (timeoutSeconds.HasValue)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds.Value);
        }

        // Validate JSON payload before sending (only for JSON content types)
        if (!string.IsNullOrEmpty(payload) && 
            (method.ToUpper() == "PUT" || method.ToUpper() == "POST") &&
            contentType != null && 
            contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            ValidateJsonPayload(payload);
        }

        var request = new HttpRequestMessage(GetHttpMethod(method), url);
        
        // Set content type
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        // Add Basic Authentication if credentials provided
        if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
        {
            request.Headers.Authorization = CreateBasicAuthHeader(userName, password);
        }

        // Add custom headers with sanitization
        if (customHeaders != null)
        {
            var rgx = new Regex("[^a-zA-Z0-9 -]");
            foreach (var kvp in customHeaders)
            {
                var key = rgx.Replace(kvp.Key, "");
                var val = rgx.Replace(kvp.Value, "");
                if (!string.IsNullOrWhiteSpace(key))
                {
                    request.Headers.TryAddWithoutValidation(key, SanitizeHeader(val));
                }
            }
        }

        // Add payload if provided
        if (!string.IsNullOrEmpty(payload))
        {
            request.Content = new StringContent(payload, Encoding.UTF8, contentType);
        }

        var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        
        // Log and optionally throw on HTTP errors
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseCouchDbError(responseBody, (int)response.StatusCode);
            Console.WriteLine($"CouchDB Error [{method}]: HTTP {(int)response.StatusCode}");
            
            if (throwOnError)
            {
                throw new HttpRequestException(errorMessage);
            }
        }
        
        return responseBody;
    }

    public async Task<string> ExecuteBytesAsync
    (
        string method,
        string url,
        byte[] payloadBytes,
        string userName = null,
        string password = null,
        string contentType = "application/octet-stream",
        System.Collections.Generic.Dictionary<string, string> customHeaders = null,
        bool allowRedirect = true,
        int? timeoutSeconds = null,
        bool throwOnError = false
    )
    {
        ValidateUrl(url);

        var httpClient = _httpClientFactory.CreateClient("CouchDb");

        if (timeoutSeconds.HasValue)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds.Value);
        }

        var request = new HttpRequestMessage(GetHttpMethod(method), url);

        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
        {
            request.Headers.Authorization = CreateBasicAuthHeader(userName, password);
        }

        if (customHeaders != null)
        {
            var rgx = new Regex("[^a-zA-Z0-9 -]");
            foreach (var kvp in customHeaders)
            {
                var key = rgx.Replace(kvp.Key, "");
                var val = rgx.Replace(kvp.Value, "");
                if (!string.IsNullOrWhiteSpace(key))
                {
                    request.Headers.TryAddWithoutValidation(key, SanitizeHeader(val));
                }
            }
        }

        if (payloadBytes != null && payloadBytes.Length > 0)
        {
            request.Content = new ByteArrayContent(payloadBytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseCouchDbError(responseBody, (int)response.StatusCode);
            Console.WriteLine($"CouchDB Error [{method}]: HTTP {(int)response.StatusCode}");

            if (throwOnError)
            {
                throw new HttpRequestException(errorMessage);
            }
        }

        return responseBody;
    }

    public string Execute
    (
        string method,
        string url,
        string payload = null,
        string userName = null,
        string password = null,
        string contentType = "application/json",
        System.Collections.Generic.Dictionary<string, string> customHeaders = null,
        bool allowRedirect = true,
        int? timeoutSeconds = null,
        bool throwOnError = false
    )
    {
        return ExecuteAsync(method, url, payload, userName, password, contentType, customHeaders, allowRedirect, timeoutSeconds, throwOnError).GetAwaiter().GetResult();
    }

    private static HttpMethod GetHttpMethod(string method)
    {
        return method.ToUpper() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            "HEAD" => HttpMethod.Head,
            _ => HttpMethod.Get
        };
    }

    public static string SanitizeHeader(string headerString)
    {
        if (string.IsNullOrEmpty(headerString))
        {
            return headerString;
        }

        var sb = new StringBuilder();
        foreach (var ch in headerString)
        {
            if ((ch == 9 || ch >= 32) && ch != 127)
            {
                sb.Append(ch);
            }
        }
        
        return sb.ToString();
    }

    private static void ValidateJsonPayload(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Invalid JSON payload: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates Basic Authentication header securely by processing credentials inline
    /// and zeroing out sensitive data from memory after use.
    /// Follows AI_CONTEXT.md security guideline: avoid storing credentials in string variables.
    /// </summary>
    private static AuthenticationHeaderValue CreateBasicAuthHeader(string userName, string password)
    {
        byte[] credentialBytes = null;
        char[] encodedChars = null;
        try
        {
            // Build credential bytes without creating an intermediate plaintext "user:password" string
            var iso88591 = Encoding.GetEncoding("ISO-8859-1");
            var userByteCount = iso88591.GetByteCount(userName);
            var passwordByteCount = iso88591.GetByteCount(password);

            credentialBytes = GC.AllocateUninitializedArray<byte>(userByteCount + 1 + passwordByteCount);

            var offset = iso88591.GetBytes(userName.AsSpan(), credentialBytes);
            credentialBytes[offset++] = (byte)':';
            offset += iso88591.GetBytes(password.AsSpan(), credentialBytes.AsSpan(offset));

            var base64Length = ((offset + 2) / 3) * 4;
            encodedChars = GC.AllocateUninitializedArray<char>(base64Length);

            if (!Convert.TryToBase64Chars(credentialBytes.AsSpan(0, offset), encodedChars, out var charsWritten))
            {
                throw new InvalidOperationException("Failed to encode Basic authentication credentials.");
            }

            return new AuthenticationHeaderValue("Basic", new string(encodedChars, 0, charsWritten));
        }
        finally
        {
            // Zero out sensitive data from memory to minimize exposure window
            if (credentialBytes != null)
            {
                CryptographicOperations.ZeroMemory(credentialBytes);
            }

            if (encodedChars != null)
            {
                Array.Clear(encodedChars, 0, encodedChars.Length);
            }
        }
    }void ValidateUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("URL cannot be null or empty");
        }

        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch (UriFormatException)
        {
            throw new ArgumentException("Invalid URL format");
        }

        // Only allow HTTP/HTTPS
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Only HTTP and HTTPS URLs are allowed");
        }

        var host = uri.Host.ToLowerInvariant();

        // Block localhost and private IP ranges to prevent SSRF
        //44331 is the port for running mmria.services on local machine for development and testing purposes. Allowing access to this port is necessary for local development scenarios
        if (uri.Port != 44331 && (host == "localhost" || host == "127.0.0.1" || host == "::1" ||
            host.StartsWith("127.") ||
            host.StartsWith("10.") ||
            host.StartsWith("192.168.") ||
            (host.StartsWith("172.") && IsPrivate172(host))))
        {
            throw new ArgumentException("Internal URLs are not allowed");
        }
    }

    private static bool IsPrivate172(string host)
    {
        // Check if 172.x.x.x is in private range 172.16-31
        var parts = host.Split('.');
        if (parts.Length == 4 && int.TryParse(parts[1], out var secondOctet))
        {
            return secondOctet >= 16 && secondOctet <= 31;
        }
        return false;
    }

    private static string ParseCouchDbError(string responseBody, int statusCode)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("error", out var errorElement) && 
                root.TryGetProperty("reason", out var reasonElement))
            {
                var error = errorElement.GetString();
                var reason = reasonElement.GetString();
                return $"HTTP {statusCode} - CouchDB Error: {error}, Reason: {reason}";
            }
        }
        catch
        {
            // If parsing fails, return generic error
        }
        
        return $"HTTP {statusCode} - {responseBody}";
    }
}
