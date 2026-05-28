using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using Microsoft.AspNetCore.Authorization;

using  mmria.server.extension; 
using mmria.common.steve;
using mmria.common.utils;

namespace mmria.server.Controllers;

[Authorize]
[Route("api/[controller]")]

public sealed class steveController : ControllerBase
{
    private ActorSystem _actorSystem;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public steveController
    (
        ActorSystem actorSystem, 
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime
    )
    {
        _actorSystem = actorSystem;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
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
    public async Task<mmria.common.metadata.Populate_CDC_Instance_Record> WriteMessage()
    {
        var body = await mmria.server.util.JsonRequestBodyReader.ReadAsync<mmria.common.metadata.Populate_CDC_Instance>(Request);
        mmria.common.metadata.Populate_CDC_Instance_Record result = new (); 

        var processor = _actorSystem.ActorSelection("user/populate-cdc-instance-supervisor");

        var sanitizedBody = CreateSanitizedPopulateCdcInstance(body);
        result = await processor.Ask(sanitizedBody) as mmria.common.metadata.Populate_CDC_Instance_Record;
        
        System.Console.WriteLine("here");

        return result;
    }

    private static mmria.common.metadata.Populate_CDC_Instance CreateSanitizedPopulateCdcInstance(mmria.common.metadata.Populate_CDC_Instance value)
    {
        var result = new mmria.common.metadata.Populate_CDC_Instance
        {
            _id = SanitizeSingleLineText(value?._id, 256),
            _rev = CouchDbRevisionHelper.NormalizeIncomingRevision(value?._rev),
            state_list = value?.state_list?
                .Where(item => item != null)
                .Select(item => new mmria.common.metadata.State_List_Item
                {
                    is_included = item.is_included,
                    prefix = SanitizeSingleLineText(item.prefix, 32),
                    name = SanitizeSingleLineText(item.name, 256)
                }).ToList() ?? new List<mmria.common.metadata.State_List_Item>()
        };

        return result;
    }

    private static string SanitizeSingleLineText(string value, int maxLength = 512)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(value.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return sanitized.Length > maxLength
            ? sanitized[..maxLength]
            : sanitized;
    }
}
