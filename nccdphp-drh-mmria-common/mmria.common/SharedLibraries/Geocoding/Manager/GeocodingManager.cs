using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.Geocoding.DAL;
using mmria.common.texas_am;

namespace mmria.common.SharedLibraries.Geocoding.Manager;

public sealed class GeocodingManager
{
    private static readonly Regex InvalidStreetAddressCharacters = new(@"[^A-Za-z0-9\s\.,#'&/\-]", RegexOptions.Compiled);
    private static readonly Regex InvalidCityCharacters = new(@"[^A-Za-z0-9\s\.'\-]", RegexOptions.Compiled);
    private static readonly Regex NonLetterCharacters = new(@"[^A-Za-z]", RegexOptions.Compiled);
    private static readonly Regex InvalidZipCharacters = new(@"[^0-9\-]", RegexOptions.Compiled);
    private static readonly Regex MultiWhitespacePattern = new(@"\s+", RegexOptions.Compiled);

    private readonly GeocodingDAL _dal;

    public GeocodingManager(GeocodingDAL dal)
    {
        _dal = dal;
    }

    public static GeocodingManager CreateDefault()
    {
        return new GeocodingManager(new GeocodingDAL(new mmria.common.SimpleHttpClientFactory()));
    }

    public async Task<geocode_response> GetGeocodeAsync(
        string geocodeApiKey,
        string streetAddress,
        string city,
        string state,
        string zip,
        string censusYear = "2020")
    {
        var result = new geocode_response();
        var sanitizedStreetAddress = SanitizeStreetAddress(streetAddress);
        var sanitizedCity = SanitizeCity(city);
        var sanitizedState = SanitizeState(state);
        var sanitizedZip = SanitizeZip(zip);

        if (string.IsNullOrWhiteSpace(sanitizedStreetAddress) &&
            string.IsNullOrWhiteSpace(sanitizedCity) &&
            string.IsNullOrWhiteSpace(sanitizedState) &&
            string.IsNullOrWhiteSpace(sanitizedZip))
        {
            return result;
        }

        var geocodeQueryParameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["streetAddress"] = sanitizedStreetAddress,
            ["city"] = sanitizedCity,
            ["state"] = sanitizedState,
            ["zip"] = sanitizedZip,
            ["apikey"] = SanitizeOptionalQueryValue(geocodeApiKey, 256),
            ["format"] = "json",
            ["allowTies"] = "false",
            ["tieBreakingStrategy"] = "flipACoin",
            ["includeHeader"] = "true",
            ["census"] = "true",
            ["censusYear"] = GetCensusYear(censusYear),
            ["notStore"] = "false",
            ["version"] = "4.01"
        };

        try
        {
            return await _dal.GetGeocodeAsync(geocodeQueryParameters);
        }
        catch (Exception)
        {
            return result;
        }
    }

    private static string GetCensusYear(string censusYear)
    {
        if (int.TryParse(censusYear, out var testYear))
        {
            return testYear switch
            {
                < 2000 => "1990",
                < 2010 => "2000",
                < 2020 => "2010",
                _ => "2020"
            };
        }

        return "2020";
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

    private static string RemoveControlCharacters(string value) =>
        new string((value ?? string.Empty).Where(character => !char.IsControl(character)).ToArray());
}
