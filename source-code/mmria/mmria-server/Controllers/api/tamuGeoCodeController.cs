using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

using mmria.common.model;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server;

[Authorize]
[Route("api/[controller]")]
public sealed class tamuGeoCodeController: ControllerBase 
{ 
    private static readonly Uri GeocodeServiceBaseUri = new("https://geoservices.tamu.edu/Services/Geocode/WebService/GeocoderWebServiceHttpNonParsed_V04_01.aspx");
    private static readonly Regex InvalidStreetAddressCharacters = new(@"[^A-Za-z0-9\s\.,#'&/\-]", RegexOptions.Compiled);
    private static readonly Regex InvalidCityCharacters = new(@"[^A-Za-z0-9\s\.'\-]", RegexOptions.Compiled);
    private static readonly Regex NonLetterCharacters = new(@"[^A-Za-z]", RegexOptions.Compiled);
    private static readonly Regex InvalidZipCharacters = new(@"[^0-9\-]", RegexOptions.Compiled);
    private static readonly Regex MultiWhitespacePattern = new(@"\s+", RegexOptions.Compiled);
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly HttpClient _httpClient;
    public tamuGeoCodeController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime
    )
    {
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
        
        _httpClient = mmria.server.util.OutboundRequestSecurityHelper.CreateNoRedirectClient();
    }
    
    [Authorize(Roles  = "abstractor")]
    [HttpGet]
    public async Task<mmria.common.texas_am.geocode_response> Get
    (
        string streetAddress,
        string city,
        string state,
        string zip,
        string census_year = "2020"
    ) 
    { 
            var result = new mmria.common.texas_am.geocode_response();

            int test_year = -1; 
            
            var censusYear = "2020";

            //"2000|2010"
            if(int.TryParse(census_year, out test_year ))
            {
                censusYear = test_year switch
                {
                    < 2000 => "1990",
                    < 2010 => "2000",
                    < 2020 => "2010",
                    _ => "2020"
                };
            }

            string geocode_api_key = configuration.GetSharedString("geocode_api_key");
            var sanitizedApiKey = SanitizeOptionalQueryValue(geocode_api_key, 256);
            var sanitizedStreetAddress = SanitizeStreetAddress(streetAddress);
            var sanitizedCity = SanitizeCity(city);
            var sanitizedState = SanitizeState(state);
            var sanitizedZip = SanitizeZip(zip);

            if
            (
                string.IsNullOrWhiteSpace(sanitizedStreetAddress) &&
                string.IsNullOrWhiteSpace(sanitizedCity) &&
                string.IsNullOrWhiteSpace(sanitizedState) &&
                string.IsNullOrWhiteSpace(sanitizedZip)
            )
            {
                return result;
            }

            var geocodeQueryParameters = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["streetAddress"] = sanitizedStreetAddress,
                ["city"] = sanitizedCity,
                ["state"] = sanitizedState,
                ["zip"] = sanitizedZip,
                ["apikey"] = sanitizedApiKey,
                ["format"] = "json",
                ["allowTies"] = "false",
                ["tieBreakingStrategy"] = "flipACoin",
                ["includeHeader"] = "true",
                ["census"] = "true",
                ["censusYear"] = censusYear,
                ["notStore"] = "false",
                ["version"] = "4.01"
            };
            var requestUri = ValidateTrustedGeocodeUri(BuildGeocodeRequestUri(geocodeQueryParameters));

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string responseFromServer = await response.Content.ReadAsStringAsync();

                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.texas_am.geocode_response>(responseFromServer);
            
            }
            catch(Exception)// ex)
            {
                // do nothing for now
            }

            return result;
    }

    private static string SanitizeStreetAddress(string value) =>
        SanitizeAllowlistedText(value, InvalidStreetAddressCharacters, 200);

    private static string SanitizeCity(string value) =>
        SanitizeAllowlistedText(value, InvalidCityCharacters, 100);

    private static string SanitizeState(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var letterOnlyState = NonLetterCharacters.Replace(RemoveControlCharacters(value), string.Empty).Trim().ToUpperInvariant();
        return letterOnlyState.Length switch
        {
            >= 2 => letterOnlyState[..2],
            1 => letterOnlyState,
            _ => string.Empty
        };
    }

    private static string SanitizeZip(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var zipValue = InvalidZipCharacters.Replace(RemoveControlCharacters(value), string.Empty).Trim();
        return zipValue.Length > 10 ? zipValue[..10] : zipValue;
    }

    private static string SanitizeAllowlistedText(string value, Regex invalidCharacters, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitizedValue = invalidCharacters.Replace(RemoveControlCharacters(value), " ").Trim();
        sanitizedValue = MultiWhitespacePattern.Replace(sanitizedValue, " ");

        return sanitizedValue.Length > maxLength ? sanitizedValue[..maxLength] : sanitizedValue;
    }

    private static string SanitizeOptionalQueryValue(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitizedValue = RemoveControlCharacters(value).Trim();
        return sanitizedValue.Length > maxLength ? sanitizedValue[..maxLength] : sanitizedValue;
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

    private static string RemoveControlCharacters(string value) =>
        new string((value ?? string.Empty).Where(character => !char.IsControl(character)).ToArray());


} 

