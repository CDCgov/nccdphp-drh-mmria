using System;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mmria.common.SharedLibraries.ExportQueue.Manager;
using mmria.common.SharedLibraries.Security.FileSystem;
using mmria.services.Models;
namespace mmria.services.vitalsimport.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class ExportQueueController : ControllerBase
{
    private readonly ActorSystem _actorSystem;
    private readonly mmria.common.couchdb.ConfigurationSet _configurationSet;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly ExportQueueManager _exportQueueManager;

    public ExportQueueController(
        ActorSystem actorSystem, 
        mmria.common.couchdb.ConfigurationSet configurationSet,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        ExportQueueManager exportQueueManager)
    {
        _actorSystem = actorSystem;
        _configurationSet = configurationSet;
        _couchDbHttpClient = couchDbHttpClient;
        _exportQueueManager = exportQueueManager;
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

            System.Console.WriteLine($"[EXPORT-QUEUE] services received host_prefix='{host_prefix}' id='{queue_item_id}'");

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
            var scheduleInfo = new ScheduleInfoMessage
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
            var actor = _actorSystem.ActorOf(Akka.Actor.Props.Create<mmria.services.ExportQueue.Process_Export_Queue>(db_config, _couchDbHttpClient));
            actor.Tell(scheduleInfo);

            return Ok(new { success = true, message = "Export queue processing initiated" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ExportQueueController error: {ex}");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("Download/{id}")]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<IActionResult> Download(string id, [FromQuery] string host_prefix)
    {
        try
        {
            host_prefix ??= string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { success = false, message = "id is required." });
            }

            mmria.common.couchdb.ConfigurationSet db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;
            if (!db_config_set.detail_list.TryGetValue(host_prefix, out var item_db_info) || item_db_info == null)
            {
                return NotFound(new { success = false, message = $"Tenant '{host_prefix}' was not found." });
            }

            var db_config = new mmria.common.couchdb.DBConfigurationDetail
            {
                url = item_db_info.url,
                prefix = "",
                user_name = item_db_info.user_name,
                user_value = item_db_info.user_value
            };

            var queue_item = await _exportQueueManager.GetQueueItemAsync(id, db_config);
            if (queue_item == null || string.IsNullOrWhiteSpace(queue_item.file_name))
            {
                return NotFound(new { success = false, message = $"The export '{id}' is missing file metadata or is no longer available." });
            }

            string export_directory = _configurationSet.name_value.ContainsKey("export_directory")
                ? _configurationSet.name_value["export_directory"]
                : "/workspace/export";

            string publicFileName;
            string physicalFileName;
            try
            {
                publicFileName = ContainedFileStore.ValidateContainedName(queue_item.file_name, nameof(queue_item.file_name));
                physicalFileName = ContainedFileStore.ValidateContainedName(
                    string.IsNullOrWhiteSpace(queue_item.storage_file_name)
                        ? queue_item.file_name
                        : queue_item.storage_file_name,
                    nameof(queue_item.storage_file_name));
            }
            catch (ArgumentException)
            {
                return NotFound(new { success = false, message = $"The export '{queue_item.file_name}' is not available on this service." });
            }

            if (!ContainedFileStore.TryFindExistingFile(export_directory, physicalFileName, out var fileInfo) &&
                !string.Equals(physicalFileName, publicFileName, StringComparison.OrdinalIgnoreCase) &&
                !ContainedFileStore.TryFindExistingFile(export_directory, publicFileName, out fileInfo))
            {
                return NotFound(new { success = false, message = $"The export '{queue_item.file_name}' is not available on this service." });
            }

            return new PhysicalFileResult(fileInfo.FullName, "application/octet-stream")
            {
                FileDownloadName = publicFileName
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ExportQueueController download error: {ex}");
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
