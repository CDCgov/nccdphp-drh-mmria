using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using mmria.common.texas_am;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.Geocoding.DAL;

public sealed class GeocodingDAL
{
    public const string HttpClientName = "Geocoding";

    private static readonly Uri GeocodeServiceBaseUri = new("https://geoservices.tamu.edu/Services/Geocode/WebService/GeocoderWebServiceHttpNonParsed_V04_01.aspx");
    private readonly IHttpClientFactory _httpClientFactory;

    public GeocodingDAL(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<geocode_response> GetGeocodeAsync(IReadOnlyDictionary<string, string> queryParameters)
    {
        var requestUri = ValidateTrustedGeocodeUri(BuildGeocodeRequestUri(queryParameters));
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        string responseFromServer = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<geocode_response>(responseFromServer) ?? new geocode_response();
    }

    private static Uri BuildGeocodeRequestUri(IReadOnlyDictionary<string, string> queryParameters)
    {
        var queryString = string.Join(
            "&",
            queryParameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value ?? string.Empty)}"));

        return new UriBuilder(GeocodeServiceBaseUri)
        {
            Query = queryString
        }.Uri;
    }

    private static Uri ValidateTrustedGeocodeUri(Uri requestUri)
    {
        if (requestUri == null || !requestUri.IsAbsoluteUri)
        {
            throw new ArgumentException("Geocode request URI must be an absolute URI.", nameof(requestUri));
        }

        if (!Uri.Compare(
                GeocodeServiceBaseUri,
                requestUri,
                UriComponents.SchemeAndServer,
                UriFormat.SafeUnescaped,
                StringComparison.OrdinalIgnoreCase).Equals(0))
        {
            throw new ArgumentException("Geocode request URI escaped the trusted TAMU host.", nameof(requestUri));
        }

        if (!string.Equals(requestUri.AbsolutePath, GeocodeServiceBaseUri.AbsolutePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Geocode request URI escaped the trusted TAMU path.", nameof(requestUri));
        }

        if (!string.IsNullOrWhiteSpace(requestUri.UserInfo) || !string.IsNullOrWhiteSpace(requestUri.Fragment))
        {
            throw new ArgumentException("Geocode request URI must not contain user info or fragments.", nameof(requestUri));
        }

        return requestUri;
    }
}
