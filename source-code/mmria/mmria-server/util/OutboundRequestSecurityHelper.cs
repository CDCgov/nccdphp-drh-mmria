using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace mmria.server.util;

public static class OutboundRequestSecurityHelper
{
    private static readonly Regex BearerTokenPattern = new("^[A-Za-z0-9._~+/=-]{1,4096}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static HttpClient CreateNoRedirectClient(TimeSpan? timeout = null)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        var client = new HttpClient(handler, disposeHandler: true);
        if (timeout.HasValue)
        {
            client.Timeout = timeout.Value;
        }

        return client;
    }

    public static AuthenticationHeaderValue CreateBearerAuthenticationHeaderValue(string bearerToken, string paramName = "bearerToken")
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            throw new ArgumentException("Bearer token is required.", paramName);
        }

        var sanitizedToken = ValidateHeaderValue(bearerToken, paramName, 4096);
        if (!BearerTokenPattern.IsMatch(sanitizedToken))
        {
            throw new ArgumentException("Bearer token contains unexpected characters.", paramName);
        }

        return new AuthenticationHeaderValue("Bearer", sanitizedToken);
    }

    public static string ValidateHeaderValue(string value, string paramName, int maxLength = 4096)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Header value is required.", paramName);
        }

        var trimmedValue = value.Trim();
        if (trimmedValue.Length > maxLength)
        {
            throw new ArgumentException("Header value exceeds the maximum allowed length.", paramName);
        }

        if (trimmedValue.Any(character => !IsVisibleAsciiHeaderCharacter(character)))
        {
            throw new ArgumentException("Header value contains unsupported characters.", paramName);
        }

        var sanitizedValue = mmria.common.getset.CouchDbHttpClient.SanitizeHeader(trimmedValue)?.Trim();
        if (string.IsNullOrWhiteSpace(sanitizedValue) || !string.Equals(trimmedValue, sanitizedValue, StringComparison.Ordinal))
        {
            throw new ArgumentException("Header value contains invalid characters.", paramName);
        }

        return sanitizedValue;
    }

    private static bool IsVisibleAsciiHeaderCharacter(char character) =>
        character >= 0x20 && character <= 0x7E;
}
