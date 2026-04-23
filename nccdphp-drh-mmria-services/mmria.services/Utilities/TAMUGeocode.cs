using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using mmria.common.model;

namespace mmria.services.vitalsimport.Utilities;

public sealed class TAMUGeoCode
{
    private static string GetCensusYear(string census_year)
    {
        if (int.TryParse(census_year, out var test_year))
        {
            return test_year switch
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
        string geocode_api_key,
        string street_address,
        string city,
        string state,
        string zip,
        string census_year)
    {
        var censusYear = GetCensusYear(census_year);

        return string.Format(
            "https://geoservices.tamu.edu/Services/Geocode/WebService/GeocoderWebServiceHttpNonParsed_V04_01.aspx?streetAddress={0}&city={1}&state={2}&zip={3}&apikey={4}&format=json&allowTies=false&tieBreakingStrategy=flipACoin&includeHeader=true&census=true&censusYear={5}&notStore=false&version=4.01",
            Uri.EscapeDataString(street_address),
            Uri.EscapeDataString(city),
            Uri.EscapeDataString(state),
            Uri.EscapeDataString(zip),
            Uri.EscapeDataString(geocode_api_key),
            Uri.EscapeDataString(censusYear));
    }

	public mmria.common.texas_am.geocode_response execute
	(
		string geocode_api_key,
		string street_address,
		string city,
		string state,
		string zip,
        string census_year = "2020"
	) 
	{ 

		var result = new common.texas_am.geocode_response();

		string request_string = BuildRequestString(geocode_api_key, street_address, city, state, zip, census_year);

		try
		{
			using var httpClient = new System.Net.Http.HttpClient();
			string responseFromServer = httpClient.GetStringAsync(request_string).Result;
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.texas_am.geocode_response>(responseFromServer) ?? result;
		
		}
		catch(Exception)
		{
			// do nothing for now
		}

		return result;

	} 


	public async Task<IEnumerable<mmria.common.texas_am.geocode_response>> executeAsync
	(
		string geocode_api_key,
		string street_address,
		string city,
		string state,
		string zip,
        string census_year = "2020"
	) 
	{ 
		
		string request_string = BuildRequestString(geocode_api_key, street_address, city, state, zip, census_year);

	using var httpClient = new System.Net.Http.HttpClient();
	string responseFromServer = await httpClient.GetStringAsync(request_string);

	var json_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.texas_am.geocode_response>(responseFromServer) ?? new mmria.common.texas_am.geocode_response();

		var result =  new mmria.common.texas_am.geocode_response[] 
		{ 
			json_result

		}; 

		return result;
	} 
}
