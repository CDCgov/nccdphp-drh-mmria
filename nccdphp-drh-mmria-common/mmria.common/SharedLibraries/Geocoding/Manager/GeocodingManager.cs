using System;
using System.Net.Http;

namespace mmria.common.SharedLibraries.Geocoding.Manager;

public sealed class GeocodingManager
{
    // Reused across calls to avoid socket exhaustion (see TAMUGeoCode legacy pattern).
    private static readonly HttpClient _httpClient = new HttpClient();

    public GeocodeResult FetchGeocode(
        string geocodeApiKey,
        string street,
        string city,
        string state,
        string zip,
        string censusYear)
    {
        var result = new GeocodeResult();

        // Split state on '-' — send only the code portion (e.g. "GA-Georgia" -> "GA").
        var stateCode = state ?? "";
        if (!string.IsNullOrEmpty(stateCode) && stateCode.Contains('-'))
        {
            stateCode = stateCode.Split('-')[0];
        }

        try
        {
            var requestUrl = BuildRequestString(
                geocodeApiKey ?? "",
                street ?? "",
                city ?? "",
                stateCode,
                zip ?? "",
                censusYear ?? "");

            var responseBody = _httpClient.GetStringAsync(requestUrl).Result;
            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.texas_am.geocode_response>(responseBody);

            if (response == null || response.OutputGeocodes == null || response.OutputGeocodes.Length == 0)
            {
                return EmptyResult();
            }

            var outputGeocode = response.OutputGeocodes[0].OutputGeocode;
            mmria.common.texas_am.CensusValue censusValue = null;

            var censusList = response.OutputGeocodes[0].CensusValues;
            if (censusList != null && censusList.Count > 0 && censusList[0] != null && censusList[0].ContainsKey("CensusValue1"))
            {
                censusValue = censusList[0]["CensusValue1"];
            }

            if (outputGeocode == null ||
                (outputGeocode.FeatureMatchingResultType != null &&
                 outputGeocode.FeatureMatchingResultType.Equals("Unmatchable", StringComparison.OrdinalIgnoreCase)))
            {
                return EmptyResult();
            }

            result.Latitude = outputGeocode.Latitude ?? "";
            result.Longitude = outputGeocode.Longitude ?? "";
            result.FeatureMatchingGeographyType = outputGeocode.FeatureMatchingGeographyType ?? "";
            result.NAACCRGISCoordinateQualityCode = outputGeocode.NAACCRGISCoordinateQualityCode ?? "";
            result.NAACCRGISCoordinateQualityType = outputGeocode.NAACCRGISCoordinateQualityType ?? "";

            if (censusValue != null)
            {
                result.NAACCRCensusTractCertaintyCode = censusValue.NAACCRCensusTractCertaintyCode ?? "";
                result.NAACCRCensusTractCertaintyType = censusValue.NAACCRCensusTractCertaintyType ?? "";
                result.CensusStateFips = censusValue.CensusStateFips ?? "";
                result.CensusCountyFips = censusValue.CensusCountyFips ?? "";
                result.CensusTractFips = censusValue.CensusTract ?? "";
                result.CensusCbsaFips = censusValue.CensusCbsaFips ?? "";
                result.CensusCbsaMicro = censusValue.CensusCbsaMicro ?? "";
                result.CensusMetDivFips = censusValue.CensusMetDivFips ?? "";
            }

            result.UrbanStatus = DeriveUrbanStatus(result);
            result.StateCountyFips = result.CensusStateFips + result.CensusCountyFips;

            return result;
        }
        catch (Exception)
        {
            return EmptyResult();
        }
    }

    private static GeocodeResult EmptyResult()
    {
        return new GeocodeResult { UrbanStatus = "Undetermined" };
    }

    private static string DeriveUrbanStatus(GeocodeResult r)
    {
        int certaintyCode = int.TryParse(r.NAACCRCensusTractCertaintyCode, out var c) ? c : 0;
        int cbsaFips = int.TryParse(r.CensusCbsaFips, out var f) ? f : 0;
        bool inRange = certaintyCode >= 1 && certaintyCode <= 6;

        if (inRange && cbsaFips > 0 && !string.IsNullOrEmpty(r.CensusMetDivFips))
            return "Metropolitan Division";
        if (inRange && cbsaFips > 0 && r.CensusCbsaMicro == "0")
            return "Metropolitan";
        if (inRange && cbsaFips > 0 && r.CensusCbsaMicro == "1")
            return "Micropolitan";
        if (inRange && r.CensusCbsaFips == "")
            return "Rural";
        return "Undetermined";
    }

    private static string NormalizeCensusYear(string censusYear)
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

    private static string BuildRequestString(
        string geocodeApiKey,
        string streetAddress,
        string city,
        string state,
        string zip,
        string censusYear)
    {
        var year = NormalizeCensusYear(censusYear);

        return string.Format(
            "https://geoservices.tamu.edu/Services/Geocode/WebService/GeocoderWebServiceHttpNonParsed_V04_01.aspx?streetAddress={0}&city={1}&state={2}&zip={3}&apikey={4}&format=json&allowTies=false&tieBreakingStrategy=flipACoin&includeHeader=true&census=true&censusYear={5}&notStore=false&version=4.01",
            Uri.EscapeDataString(streetAddress),
            Uri.EscapeDataString(city),
            Uri.EscapeDataString(state),
            Uri.EscapeDataString(zip),
            Uri.EscapeDataString(geocodeApiKey),
            Uri.EscapeDataString(year));
    }
}
