using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public tamuGeoCodeController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
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

            string request_string = string.Format ($"https://geoservices.tamu.edu/Services/Geocode/WebService/GeocoderWebServiceHttpNonParsed_V04_01.aspx?streetAddress={streetAddress}&city={city}&state={state}&zip={zip}&apikey={geocode_api_key}&format=json&allowTies=false&tieBreakingStrategy=flipACoin&includeHeader=true&census=true&censusYear={censusYear}&notStore=false&version=4.01");

            var curl = new mmria.getset.cURL("GET", null, request_string, null);
            try
            {
                string responseFromServer = await curl.executeAsync();

                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.texas_am.geocode_response>(responseFromServer);
            
            }
            catch(Exception)// ex)
            {
                // do nothing for now
            }

            return result;
    }


} 

