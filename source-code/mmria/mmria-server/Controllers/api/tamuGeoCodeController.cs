using System;
using System.Threading.Tasks;

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
using mmria.common.SharedLibraries.Geocoding.Manager;
namespace mmria.server;

[Authorize]
[Route("api/[controller]")]
public sealed class tamuGeoCodeController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly GeocodingManager _geocodingManager;
    public tamuGeoCodeController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        GeocodingManager geocodingManager
    )
    {
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
        _geocodingManager = geocodingManager;
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
        return await _geocodingManager.GetGeocodeAsync(
            configuration.GetSharedString("geocode_api_key"),
            streetAddress,
            city,
            state,
            zip,
            census_year);
    }
} 

