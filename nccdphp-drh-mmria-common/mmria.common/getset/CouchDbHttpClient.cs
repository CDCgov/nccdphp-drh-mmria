using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
        int? timeoutSeconds = null
    )
    {
        var httpClient = _httpClientFactory.CreateClient();
        
        // Set timeout if specified (default is 100 seconds)
        if (timeoutSeconds.HasValue)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds.Value);
        }

        var request = new HttpRequestMessage(GetHttpMethod(method), url);
        
        // Set content type
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        // Add Basic Authentication if credentials provided
        if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
        {
            var credentials = $"{userName}:{password}";
            var encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(credentials));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
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
        //response.EnsureSuccessStatusCode();//needed to match cURL legacy behavior
        
        return await response.Content.ReadAsStringAsync();
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
        int? timeoutSeconds = null
    )
    {
        return ExecuteAsync(method, url, payload, userName, password, contentType, customHeaders, allowRedirect, timeoutSeconds).GetAwaiter().GetResult();
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
}
