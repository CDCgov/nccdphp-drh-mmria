using System;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mmria.common.SharedLibraries.ExportQueue;
using mmria.services.Models;
namespace mmria.services.vitalsimport.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class ExportQueueController : ControllerBase
{
    private ActorSystem _actorSystem;
    private mmria.common.couchdb.ConfigurationSet _configurationSet;
    private mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly IExportQueueRepository _exportQueueRepository;

    public ExportQueueController(
        ActorSystem actorSystem, 
        mmria.common.couchdb.ConfigurationSet configurationSet,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        IExportQueueRepository exportQueueRepository)
    {
        _actorSystem = actorSystem;
        _configurationSet = configurationSet;
        _couchDbHttpClient = couchDbHttpClient;
        _exportQueueRepository = exportQueueRepository;
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
            var actor = _actorSystem.ActorOf(Akka.Actor.Props.Create<mmria.services.ExportQueue.Process_Export_Queue>(db_config, _couchDbHttpClient, _exportQueueRepository));
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

            string request_string = db_config.Get_Prefix_DB_Url("export_queue/" + id);
            string response_from_server = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                request_string,
                null,
                db_config.user_name,
                db_config.user_value);

            var queue_item = Newtonsoft.Json.JsonConvert.DeserializeObject<export_queue_item>(response_from_server);
            if (queue_item == null || string.IsNullOrWhiteSpace(queue_item.file_name))
            {
                return NotFound(new { success = false, message = $"The export '{id}' is missing file metadata or is no longer available." });
            }

            string export_directory = _configurationSet.name_value.ContainsKey("export_directory")
                ? _configurationSet.name_value["export_directory"]
                : "/workspace/export";

            string file_path;
            try
            {
                file_path = ResolveContainedFilePath(export_directory, queue_item.file_name);
            }
            catch (ArgumentException)
            {
                return NotFound(new { success = false, message = $"The export '{queue_item.file_name}' is not available on this service." });
            }

            if (!System.IO.File.Exists(file_path))
            {
                return NotFound(new { success = false, message = $"The export '{queue_item.file_name}' is not available on this service." });
            }

            return new PhysicalFileResult(file_path, "application/octet-stream")
            {
                FileDownloadName = queue_item.file_name
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ExportQueueController download error: {ex}");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    private static string NormalizeTrustedDirectoryRoot(string baseDirectory, string paramName)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new ArgumentException("Base directory is required.", paramName);
        }

        var rootPath = System.IO.Path.GetFullPath(baseDirectory);
        if (!System.IO.Path.IsPathFullyQualified(rootPath))
        {
            throw new ArgumentException("Base directory must be fully qualified.", paramName);
        }

        return System.IO.Path.EndsInDirectorySeparator(rootPath)
            ? rootPath
            : rootPath + System.IO.Path.DirectorySeparatorChar;
    }

    private static string ResolveContainedFilePath(string trustedBaseDirectory, string fileName)
    {
        var normalizedRoot = NormalizeTrustedDirectoryRoot(trustedBaseDirectory, nameof(trustedBaseDirectory));
        var safeFileName = ValidateContainedName(fileName, nameof(fileName));
        var combinedPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(normalizedRoot, safeFileName));
        EnsureContainedPath(normalizedRoot, combinedPath, nameof(fileName));
        return combinedPath;
    }

    private static string ValidateContainedName(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty path segment is required.", paramName);
        }

        var trimmedValue = value.Trim();
        if (trimmedValue is "." or "..")
        {
            throw new ArgumentException("Relative path operators are not allowed.", paramName);
        }

        if (System.IO.Path.IsPathRooted(trimmedValue) ||
            trimmedValue.Contains(System.IO.Path.DirectorySeparatorChar) ||
            trimmedValue.Contains(System.IO.Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("Only a single file name is allowed.", paramName);
        }

        if (trimmedValue.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Path segment contains invalid filename characters.", paramName);
        }

        return trimmedValue;
    }

    private static void EnsureContainedPath(string trustedBaseDirectory, string resolvedPath, string paramName)
    {
        if (!resolvedPath.StartsWith(trustedBaseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Resolved path escaped the configured base directory.", paramName);
        }
    }
}

public sealed class ExportQueueRequest
{
    public string queue_item_id { get; set; }
    public string jurisdiction_user_name { get; set; }
    public string host_prefix { get; set; }
}
