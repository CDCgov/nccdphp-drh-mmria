using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.Geocoding.Manager;

namespace mmria.services.vitalsimport.Utilities;

public sealed class TAMUGeoCode
{
    private readonly GeocodingManager _geocodingManager;

    public TAMUGeoCode()
        : this(GeocodingManager.CreateDefault())
    {
    }

    internal TAMUGeoCode(GeocodingManager geocodingManager)
    {
        _geocodingManager = geocodingManager;
    }

    public mmria.common.texas_am.geocode_response execute(
        string geocode_api_key,
        string street_address,
        string city,
        string state,
        string zip,
        string census_year = "2020")
    {
        return _geocodingManager
            .GetGeocodeAsync(geocode_api_key, street_address, city, state, zip, census_year)
            .GetAwaiter()
            .GetResult();
    }

    public async Task<IEnumerable<mmria.common.texas_am.geocode_response>> executeAsync(
        string geocode_api_key,
        string street_address,
        string city,
        string state,
        string zip,
        string census_year = "2020")
    {
        var geocodeResponse = await _geocodingManager.GetGeocodeAsync(
            geocode_api_key,
            street_address,
            city,
            state,
            zip,
            census_year);

        return new[]
        {
            geocodeResponse
        };
    }
}
