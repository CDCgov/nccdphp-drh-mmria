using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using mmria.services.vitalsimport.Actors.VitalsImport;
using mmria.services.vitalsimport.Messages;
using System;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Net;

namespace mmria.services.vitalsimport.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public sealed class PopulateCDCInstanceController : ControllerBase
{
    private ActorSystem _actorSystem;
    private IConfiguration _configurationSet;

    public PopulateCDCInstanceController(ActorSystem actorSystem, IConfiguration configurationSet)
    {
        _actorSystem = actorSystem;
        _configurationSet = configurationSet;
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
