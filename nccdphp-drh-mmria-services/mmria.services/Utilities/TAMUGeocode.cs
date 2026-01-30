using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using mmria.common.model;

namespace mmria.services.vitalsimport.Utilities;

public sealed class TAMUGeoCode
{

	public mmria.common.texas_am.geocode_response execute
	(
		string geocode_api_key,
		string street_address,
		string city,
		string state,
		string zip
	) 
	{ 

		var result = new common.texas_am.geocode_response();

		string request_string = string.Format ("https://geoservices.tamu.edu/Services/Geocode/WebService/GeocoderWebServiceHttpNonParsed_V04_01.aspx?streetAddress={0}&city={1}&state={2}&zip={3}&apikey={4}&format=json&allowTies=false&tieBreakingStrategy=flipACoin&includeHeader=true&census=true&censusYear=2000|2010&notStore=false&version=4.01", street_address, city, state, zip, geocode_api_key);

		var curl = new mmria.getset.cURL("GET", null, request_string, null);
		try
		{
			string responseFromServer = curl.execute();

			result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.texas_am.geocode_response>(responseFromServer);
		
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
		string zip
	) 
	{ 
		
		string request_string = string.Format ("https://geoservices.tamu.edu/Services/Geocode/WebService/GeocoderWebServiceHttpNonParsed_V04_01.aspx?streetAddress={0}&city={1}&state={2}&zip={3}&apikey={4}&format=json&allowTies=false&tieBreakingStrategy=flipACoin&includeHeader=true&census=true&censusYear=2000|2010&notStore=false&version=4.01", street_address, city, state, zip, geocode_api_key);

		var curl = new mmria.getset.cURL("GET", null, request_string, null);

		string responseFromServer = await curl.executeAsync();

		var json_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.texas_am.geocode_response>(responseFromServer);



		var result =  new mmria.common.texas_am.geocode_response[] 
		{ 
			json_result

		}; 

		return result;
	} 
}
