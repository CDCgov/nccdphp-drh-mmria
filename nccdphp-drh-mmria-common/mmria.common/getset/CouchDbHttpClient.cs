using System;
using System.Linq;
using System.Net.Http.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mmria.common.getset;

public sealed class CouchDbRequestOptions
{
    public string UserName { get; init; }
    public string Password { get; init; }
    public string BearerToken { get; init; }
    public string AuthSessionValue { get; init; }
    public string IfMatch { get; init; }
    public string VitalServiceKey { get; init; }
    public System.Collections.Generic.Dictionary<string, string> SafeHeaders { get; init; }
    public int? TimeoutSeconds { get; init; }
    public bool ThrowOnError { get; init; }
    public bool SuppressErrorLogging { get; init; }
    public string ClientName { get; init; }
}

public sealed class CouchDbHttpResponse
{
    public string Body { get; init; }
    public int StatusCode { get; init; }
    public System.Collections.Generic.IReadOnlyDictionary<string, string[]> Headers { get; init; }

    public string GetFirstHeaderValue(string headerName)
    {
        if (string.IsNullOrWhiteSpace(headerName) ||
            Headers == null ||
            !Headers.TryGetValue(headerName, out var values) ||
            values == null ||
            values.Length == 0)
        {
            return null;
        }

        return values[0];
    }

    public System.Collections.Generic.IReadOnlyList<string> GetHeaderValues(string headerName)
    {
        if (string.IsNullOrWhiteSpace(headerName) ||
            Headers == null ||
            !Headers.TryGetValue(headerName, out var values) ||
            values == null)
        {
            return Array.Empty<string>();
        }

        return values;
    }
}

public sealed class CouchDbHttpClient
{
    private const string DefaultClientName = "CouchDb";
    private static readonly System.Collections.Generic.HashSet<string> ReservedForwardedHeaderNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "authorization",
            "cookie",
            "x-couchdb-www-authenticate",
            "if-match",
            "vital-service-key",
            "host",
            "content-length",
            "content-type",
            "transfer-encoding",
            "connection"
        };
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
        bool throwOnError = false,
        string clientName = null
    )
    {
        var response = await ExecuteForResponseAsync(
            method,
            url,
            payload,
            contentType,
            CreateRequestOptions(userName, password, customHeaders, timeoutSeconds, throwOnError, clientName));

        return response.Body;
    }

    public async Task<string> ExecuteAsync(
        string method,
        string url,
        string payload,
        string contentType,
        CouchDbRequestOptions requestOptions)
    {
        var response = await ExecuteForResponseAsync(method, url, payload, contentType, requestOptions);
        return response.Body;
    }

    public async Task<CouchDbHttpResponse> ExecuteForResponseAsync
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
        bool throwOnError = false,
        string clientName = null
    )
    {
        return await ExecuteForResponseAsync(
            method,
            url,
            payload,
            contentType,
            CreateRequestOptions(userName, password, customHeaders, timeoutSeconds, throwOnError, clientName));
    }

    public async Task<CouchDbHttpResponse> ExecuteForResponseAsync(
        string method,
        string url,
        string payload,
        string contentType,
        CouchDbRequestOptions requestOptions)
    {
        requestOptions ??= new CouchDbRequestOptions();

        if (!string.IsNullOrEmpty(payload) &&
            (method.ToUpper() == "PUT" || method.ToUpper() == "POST") &&
            contentType != null &&
            contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            ValidateJsonPayload(payload);
        }

        using var request = CreateRequestMessage(method, url, requestOptions);
        if (!string.IsNullOrEmpty(payload))
        {
            request.Content = new StringContent(payload, Encoding.UTF8, contentType);
        }

        return await SendForResponseAsync(method, request, requestOptions);
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
        bool throwOnError = false,
        string clientName = null
    )
    {
        var response = await ExecuteBytesForResponseAsync(
            method,
            url,
            payloadBytes,
            contentType,
            CreateRequestOptions(userName, password, customHeaders, timeoutSeconds, throwOnError, clientName));

        return response.Body;
    }

    public async Task<string> ExecuteBytesAsync(
        string method,
        string url,
        byte[] payloadBytes,
        string contentType,
        CouchDbRequestOptions requestOptions)
    {
        var response = await ExecuteBytesForResponseAsync(method, url, payloadBytes, contentType, requestOptions);
        return response.Body;
    }

    public async Task<string> ExecuteJsonAsync<TPayload>(
        string method,
        string url,
        TPayload payload,
        JsonSerializerOptions serializerOptions,
        string userName = null,
        string password = null,
        string contentType = "application/json",
        System.Collections.Generic.Dictionary<string, string> customHeaders = null,
        bool allowRedirect = true,
        int? timeoutSeconds = null,
        bool throwOnError = false,
        string clientName = null)
    {
        var response = await ExecuteJsonForResponseAsync(
            method,
            url,
            payload,
            serializerOptions,
            contentType,
            CreateRequestOptions(userName, password, customHeaders, timeoutSeconds, throwOnError, clientName));

        return response.Body;
    }

    public async Task<string> ExecuteJsonAsync<TPayload>(
        string method,
        string url,
        TPayload payload,
        JsonSerializerOptions serializerOptions,
        string contentType,
        CouchDbRequestOptions requestOptions)
    {
        var response = await ExecuteJsonForResponseAsync(
            method,
            url,
            payload,
            serializerOptions,
            contentType,
            requestOptions);

        return response.Body;
    }

    public async Task<CouchDbHttpResponse> ExecuteBytesForResponseAsync
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
        bool throwOnError = false,
        string clientName = null
    )
    {
        return await ExecuteBytesForResponseAsync(
            method,
            url,
            payloadBytes,
            contentType,
            CreateRequestOptions(userName, password, customHeaders, timeoutSeconds, throwOnError, clientName));
    }

    public async Task<CouchDbHttpResponse> ExecuteJsonForResponseAsync<TPayload>
    (
        string method,
        string url,
        TPayload payload,
        JsonSerializerOptions serializerOptions,
        string userName = null,
        string password = null,
        string contentType = "application/json",
        System.Collections.Generic.Dictionary<string, string> customHeaders = null,
        bool allowRedirect = true,
        int? timeoutSeconds = null,
        bool throwOnError = false,
        string clientName = null
    )
    {
        return await ExecuteJsonForResponseAsync(
            method,
            url,
            payload,
            serializerOptions,
            contentType,
            CreateRequestOptions(userName, password, customHeaders, timeoutSeconds, throwOnError, clientName));
    }

    public async Task<CouchDbHttpResponse> ExecuteJsonForResponseAsync<TPayload>(
        string method,
        string url,
        TPayload payload,
        JsonSerializerOptions serializerOptions,
        string contentType,
        CouchDbRequestOptions requestOptions)
    {
        requestOptions ??= new CouchDbRequestOptions();

        using var request = CreateRequestMessage(method, url, requestOptions);

        if (payload != null)
        {
            request.Content = JsonContent.Create(
                payload,
                mediaType: new MediaTypeHeaderValue(contentType),
                options: serializerOptions);
        }

        return await SendForResponseAsync(method, request, requestOptions);
    }

    public async Task<CouchDbHttpResponse> ExecuteBytesForResponseAsync(
        string method,
        string url,
        byte[] payloadBytes,
        string contentType,
        CouchDbRequestOptions requestOptions)
    {
        requestOptions ??= new CouchDbRequestOptions();

        using var request = CreateRequestMessage(method, url, requestOptions);

        if (payloadBytes != null && payloadBytes.Length > 0)
        {
            request.Content = new ByteArrayContent(payloadBytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        return await SendForResponseAsync(method, request, requestOptions);
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
        bool throwOnError = false,
        string clientName = null
    )
    {
        return ExecuteAsync(
            method,
            url,
            payload,
            userName,
            password,
            contentType,
            customHeaders,
            allowRedirect,
            timeoutSeconds,
            throwOnError,
            clientName).GetAwaiter().GetResult();
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

    private static CouchDbRequestOptions CreateRequestOptions(
        string userName,
        string password,
        System.Collections.Generic.Dictionary<string, string> customHeaders,
        int? timeoutSeconds,
        bool throwOnError,
        string clientName)
    {
        string authSessionValue = null;
        string ifMatch = null;
        string vitalServiceKey = null;
        string bearerToken = null;
        System.Collections.Generic.Dictionary<string, string> safeHeaders = null;

        if (customHeaders != null)
        {
            foreach (var kvp in customHeaders)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    continue;
                }

                var headerName = kvp.Key.Trim();
                var headerValue = kvp.Value ?? string.Empty;
                switch (headerName.ToLowerInvariant())
                {
                    case "cookie":
                        authSessionValue ??= ExtractAuthSessionValue(headerValue);
                        break;
                    case "x-couchdb-www-authenticate":
                        authSessionValue ??= SanitizeHeader(headerValue)?.Trim();
                        break;
                    case "if-match":
                        ifMatch = SanitizeHeader(headerValue)?.Trim();
                        break;
                    case "vital-service-key":
                        vitalServiceKey = SanitizeHeader(headerValue)?.Trim();
                        break;
                    case "authorization":
                        bearerToken = ExtractBearerToken(headerValue);
                        break;
                    default:
                        var sanitizedName = SanitizeHeaderName(headerName);
                        var sanitizedHeaderValue = SanitizeHeader(headerValue)?.Trim();
                        if (!string.IsNullOrWhiteSpace(sanitizedName) &&
                            !IsReservedForwardedHeaderName(sanitizedName) &&
                            !string.IsNullOrWhiteSpace(sanitizedHeaderValue))
                        {
                            safeHeaders ??= new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            safeHeaders[sanitizedName] = sanitizedHeaderValue;
                        }
                        break;
                }
            }
        }

        return new CouchDbRequestOptions
        {
            UserName = userName,
            Password = password,
            BearerToken = bearerToken,
            AuthSessionValue = authSessionValue,
            IfMatch = ifMatch,
            VitalServiceKey = vitalServiceKey,
            SafeHeaders = safeHeaders,
            TimeoutSeconds = timeoutSeconds,
            ThrowOnError = throwOnError,
            SuppressErrorLogging = false,
            ClientName = clientName
        };
    }

    private HttpRequestMessage CreateRequestMessage(string method, string url, CouchDbRequestOptions requestOptions)
    {
        var uri = ValidateAndCreateUri(url);
        var request = new HttpRequestMessage(GetHttpMethod(method), uri);

        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        ApplyRequestOptions(request, requestOptions);

        return request;
    }

    private async Task<CouchDbHttpResponse> SendForResponseAsync(string method, HttpRequestMessage request, CouchDbRequestOptions requestOptions)
    {
        var httpClient = _httpClientFactory.CreateClient(
            string.IsNullOrWhiteSpace(requestOptions?.ClientName) ? DefaultClientName : requestOptions.ClientName);

        if (requestOptions?.TimeoutSeconds.HasValue == true)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(requestOptions.TimeoutSeconds.Value);
        }

        using var response = await httpClient.SendAsync(request);
        var responseBody = response.Content == null
            ? string.Empty
            : await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseCouchDbError(responseBody, (int)response.StatusCode);
            if (requestOptions?.SuppressErrorLogging != true)
            {
                Console.WriteLine($"CouchDB Error [{method}]: HTTP {(int)response.StatusCode}");
            }

            if (requestOptions?.ThrowOnError == true)
            {
                throw new HttpRequestException(errorMessage);
            }
        }

        return new CouchDbHttpResponse
        {
            Body = responseBody,
            StatusCode = (int)response.StatusCode,
            Headers = CaptureHeaders(response)
        };
    }

    private static void ApplyRequestOptions(HttpRequestMessage request, CouchDbRequestOptions requestOptions)
    {
        if (requestOptions == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(requestOptions.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                GetValidatedHeaderValue(requestOptions.BearerToken, nameof(requestOptions.BearerToken)));
        }
        else if (!string.IsNullOrWhiteSpace(requestOptions.UserName) && !string.IsNullOrWhiteSpace(requestOptions.Password))
        {
            request.Headers.Authorization = CreateBasicAuthHeaderValue(requestOptions.UserName, requestOptions.Password);
        }

        if (!string.IsNullOrWhiteSpace(requestOptions.AuthSessionValue))
        {
            var sanitizedAuthSessionValue = GetValidatedHeaderValue(
                requestOptions.AuthSessionValue,
                nameof(requestOptions.AuthSessionValue));
            request.Headers.Add("Cookie", $"AuthSession={Uri.EscapeDataString(sanitizedAuthSessionValue)}");
        }

        if (!string.IsNullOrWhiteSpace(requestOptions.IfMatch))
        {
            request.Headers.IfMatch.Add(CreateIfMatchHeaderValue(requestOptions.IfMatch));
        }

        if (!string.IsNullOrWhiteSpace(requestOptions.VitalServiceKey))
        {
            request.Headers.Add(
                "vital-service-key",
                GetValidatedHeaderValue(requestOptions.VitalServiceKey, nameof(requestOptions.VitalServiceKey)));
        }

        if (requestOptions.SafeHeaders != null)
        {
            foreach (var kvp in requestOptions.SafeHeaders)
            {
                var sanitizedName = SanitizeHeaderName(kvp.Key);
                if (!string.IsNullOrWhiteSpace(sanitizedName) &&
                    !IsReservedForwardedHeaderName(sanitizedName) &&
                    !string.IsNullOrWhiteSpace(kvp.Value))
                {
                    request.Headers.Add(
                        sanitizedName,
                        GetValidatedHeaderValue(kvp.Value, sanitizedName));
                }
            }
        }
    }

    private static EntityTagHeaderValue CreateIfMatchHeaderValue(string ifMatchValue)
    {
        var sanitizedIfMatchValue = GetValidatedHeaderValue(ifMatchValue, nameof(ifMatchValue));

        if (sanitizedIfMatchValue == "*")
        {
            return EntityTagHeaderValue.Any;
        }

        sanitizedIfMatchValue = sanitizedIfMatchValue.Trim('"');
        if (string.IsNullOrWhiteSpace(sanitizedIfMatchValue))
        {
            throw new ArgumentException("If-Match value is required.", nameof(ifMatchValue));
        }

        return new EntityTagHeaderValue($"\"{sanitizedIfMatchValue}\"");
    }

    private static string GetValidatedHeaderValue(string headerValue, string paramName)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            throw new ArgumentException("Header value is required.", paramName);
        }

        var trimmedValue = headerValue.Trim();
        var sanitizedValue = SanitizeHeader(trimmedValue)?.Trim();
        if (!string.Equals(trimmedValue, sanitizedValue, StringComparison.Ordinal))
        {
            throw new ArgumentException("Header value contains invalid control characters.", paramName);
        }

        return sanitizedValue;
    }

    private static System.Collections.Generic.IReadOnlyDictionary<string, string[]> CaptureHeaders(HttpResponseMessage response)
    {
        var headers = new System.Collections.Generic.Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        if (response.Content != null)
        {
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = header.Value.ToArray();
            }
        }

        return headers;
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
    public static AuthenticationHeaderValue CreateBasicAuthHeaderValue(string userName, string password)
    {
        byte[] credentialBytes = null;
        char[] encodedChars = null;
        try
        {
            // Build credential bytes without creating an intermediate plaintext "user:credential" string
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
    }

    private static string ExtractAuthSessionValue(string cookieHeader)
    {
        if (string.IsNullOrWhiteSpace(cookieHeader))
        {
            return null;
        }

        var match = Regex.Match(cookieHeader, @"AuthSession=([^;]+)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    private static string ExtractBearerToken(string authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return null;
        }

        const string bearerPrefix = "Bearer ";
        if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authorizationHeader.Substring(bearerPrefix.Length).Trim();
    }

    private static string SanitizeHeaderName(string headerName)
    {
        if (string.IsNullOrWhiteSpace(headerName))
        {
            return null;
        }

        return Regex.Replace(headerName, "[^a-zA-Z0-9-]", string.Empty);
    }

    private static bool IsReservedForwardedHeaderName(string headerName)
    {
        if (string.IsNullOrWhiteSpace(headerName))
        {
            return false;
        }

        return ReservedForwardedHeaderNames.Contains(headerName);
    }

    private static Uri ValidateAndCreateUri(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("URL cannot be null or empty");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid URL format");
        }

        // Only allow HTTP/HTTPS
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Only HTTP and HTTPS URLs are allowed");
        }

        if (!string.IsNullOrWhiteSpace(uri.UserInfo) || !string.IsNullOrWhiteSpace(uri.Fragment))
        {
            throw new ArgumentException("URLs must not include user info or fragments");
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new ArgumentException("URL host is required");
        }

        var host = uri.Host.ToLowerInvariant();

        bool isLoopbackHost =
            host == "localhost" ||
            host == "127.0.0.1" ||
            host == "0.0.0.0" ||
            host == "::1" ||
            host.StartsWith("127.");

        // Allow the local development services that commonly run on loopback.
        bool isAllowedDevelopmentLoopbackEndpoint =
            isLoopbackHost &&
            (uri.Port == 44331 || uri.Port == 5984 || uri.Port == 12345);

        // Block localhost and private IP ranges to prevent SSRF.
        if (!isAllowedDevelopmentLoopbackEndpoint && (isLoopbackHost ||
            host.StartsWith("10.") ||
            host.StartsWith("192.168.") ||
            (host.StartsWith("172.") && IsPrivate172(host))))
        {
            throw new ArgumentException("Internal URLs are not allowed");
        }

        return uri;
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
