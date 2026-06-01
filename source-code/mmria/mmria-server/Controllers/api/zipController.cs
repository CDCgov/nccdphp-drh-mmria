using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mmria.common.SharedLibraries.ExportQueue.Model;
using mmria.server.extension;

namespace mmria.server;

[Authorize(Roles = "abstractor,data_analyst")]
[Route("api/[controller]")]
public sealed class zipController : ControllerBase
{
    private readonly mmria.common.couchdb.OverridableConfiguration configuration;
    private readonly mmria.common.couchdb.DBConfigurationDetail db_config;
    private readonly string host_prefix;
    private readonly mmria.common.SharedLibraries.ExportQueue.Manager.ExportQueueManager _exportQueueManager;

    public zipController(
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.SharedLibraries.ExportQueue.Manager.ExportQueueManager exportQueueManager)
    {
        _exportQueueManager = exportQueueManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();
        db_config = tenantRuntime.RequireDbConfig();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var export_queue_item = await _exportQueueManager.GetQueueItemAsync(id, db_config);
        if (export_queue_item == null || string.IsNullOrWhiteSpace(export_queue_item.file_name))
        {
            return CreateProblemFileResult(
                StatusCodes.Status404NotFound,
                "Export file not found",
                "The requested export is missing file metadata or is no longer available.");
        }

        ExportQueueDownloadResult downloadResult;
        try
        {
            downloadResult = await _exportQueueManager.DownloadExportFileAsync(
                id,
                host_prefix,
                configuration.GetString("vitals_url", host_prefix),
                configuration.GetString("vital_service_key", host_prefix),
                HttpContext.RequestAborted);
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return CreateProblemFileResult(
                StatusCodes.Status502BadGateway,
                "Export download failed",
                "Unable to reach mmria.services while retrieving the requested export.");
        }

        if (downloadResult.Status == ExportQueueDownloadStatus.NotFound)
        {
            return CreateProblemFileResult(
                StatusCodes.Status404NotFound,
                "Export file not found",
                "The requested export is not available on mmria.services.");
        }

        if (downloadResult.Status == ExportQueueDownloadStatus.ServiceError)
        {
            return CreateProblemFileResult(
                StatusCodes.Status502BadGateway,
                "Export download failed",
                "mmria.services returned an error while retrieving the requested export.");
        }

        if (downloadResult.Status == ExportQueueDownloadStatus.Unreadable)
        {
            return CreateProblemFileResult(
                StatusCodes.Status502BadGateway,
                "Export download failed",
                "mmria.services returned an unreadable stream for the requested export.");
        }

        if (!downloadResult.IsSuccess)
        {
            return CreateProblemFileResult(
                StatusCodes.Status502BadGateway,
                "Export download failed",
                "mmria.services returned an unexpected response while retrieving the requested export.");
        }

        try
        {
            await _exportQueueManager.MarkDownloadedAsync(export_queue_item, db_config);
        }
        catch
        {
            downloadResult.Dispose();
            throw;
        }

        Response.RegisterForDispose(downloadResult);

        var safeDownloadFileName = GetSafeDownloadFileName(export_queue_item.file_name);
        return mmria.server.util.SafeFileDownloadResultFactory.Create(
            downloadResult.ContentStream,
            downloadResult.ContentType,
            safeDownloadFileName,
            "export.zip");
    }

    private FileContentResult CreateProblemFileResult(int statusCode, string title, string detail)
    {
        Response.StatusCode = statusCode;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

        var payload = Encoding.UTF8.GetBytes(mmria.server.util.EscapedJsonResultFactory.Serialize(problem));
        return File(payload, "application/problem+json");
    }

    private static string GetSafeDownloadFileName(string fileName) =>
        mmria.server.util.ContainedPathHelper.CreateSafeDownloadFileName(fileName, "export.zip");
}




