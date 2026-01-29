using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using Microsoft.AspNetCore.Authorization;

using  mmria.server.extension; 
using mmria.common.steve;

namespace mmria.server.Controllers;

[Authorize]
[Route("api/[controller]")]

public sealed class steveController : ControllerBase
{
    private ActorSystem _actorSystem;
    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public steveController
    (
        ActorSystem actorSystem, 
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets
    )
    {
        _actorSystem = actorSystem;
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
    }


    [HttpGet]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<mmria.common.metadata.Populate_CDC_Instance_Record> ReadMessage()
    {

        mmria.common.metadata.Populate_CDC_Instance_Record result = new ();
        var processor = _actorSystem.ActorSelection("user/populate-cdc-instance-supervisor");

        result = await processor.Ask(DateTime.Now) as mmria.common.metadata.Populate_CDC_Instance_Record;

        System.Console.WriteLine("here");

        return result;

    }


    [HttpPut]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<mmria.common.metadata.Populate_CDC_Instance_Record> ReadMessage([FromBody] mmria.common.metadata.Populate_CDC_Instance body)
    {
        mmria.common.metadata.Populate_CDC_Instance_Record result = new (); 

        var processor = _actorSystem.ActorSelection("user/populate-cdc-instance-supervisor");

        result = await processor.Ask(body) as mmria.common.metadata.Populate_CDC_Instance_Record;
        
        System.Console.WriteLine("here");

        return result;
    }
}
