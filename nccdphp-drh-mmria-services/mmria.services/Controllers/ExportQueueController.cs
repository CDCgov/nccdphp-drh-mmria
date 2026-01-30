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

[Route("api/[controller]")]
[ApiController]
public sealed class ExportQueueController : ControllerBase
{
    private ActorSystem _actorSystem;
    private mmria.common.couchdb.ConfigurationSet _configurationSet;

    public ExportQueueController(ActorSystem actorSystem, mmria.common.couchdb.ConfigurationSet configurationSet)
    {
        _actorSystem = actorSystem;
        _configurationSet = configurationSet;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<IActionResult> ProcessExportQueue([FromBody] ExportQueueRequest request)
    {
        try
        {
            mmria.common.couchdb.DBConfigurationDetail item_db_info;

            string host_prefix = request.host_prefix;
            string jurisdiction_user_name = request.jurisdiction_user_name;
            string queue_item_id = request.queue_item_id;

            mmria.common.couchdb.ConfigurationSet db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;
            item_db_info = db_config_set.detail_list[host_prefix];

            // Get configuration values
            var db_config = new mmria.common.couchdb.DBConfigurationDetail
            {
                url = item_db_info.url,
                prefix = "",
                user_name = item_db_info.user_name,
                user_value = item_db_info.user_value
            };

            // Create schedule info message
            var scheduleInfo = new mmria.server.model.actor.ScheduleInfoMessage
            (
                _configurationSet.name_value["cron_schedule"],
                item_db_info.url,
                "",
                item_db_info.user_name,
                item_db_info.user_value,
                _configurationSet.name_value.ContainsKey("export_directory") ? _configurationSet.name_value["export_directory"] : "/workspace/export",
                request.jurisdiction_user_name,
                _configurationSet.name_value.ContainsKey("metadata_version") ? _configurationSet.name_value["metadata_version"] : "",
                _configurationSet.name_value.ContainsKey("cdc_instance_pull_list") ? _configurationSet.name_value["cdc_instance_pull_list"] : ""
            );

            // Create and tell the actor to process
            var actor = _actorSystem.ActorOf(Akka.Actor.Props.Create<mmria.services.ExportQueue.Process_Export_Queue>(db_config));
            actor.Tell(scheduleInfo);

            return Ok(new { success = true, message = "Export queue processing initiated" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ExportQueueController error: {ex}");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

public sealed class ExportQueueRequest
{
    public string queue_item_id { get; set; }
    public string jurisdiction_user_name { get; set; }
    public string host_prefix { get; set; }
}
