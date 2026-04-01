using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IHttpClientFactory _httpClientFactory;

    public zipController(
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.SharedLibraries.ExportQueue.Manager.ExportQueueManager exportQueueManager,
        IHttpClientFactory httpClientFactory)
    {
        _exportQueueManager = exportQueueManager;
        _httpClientFactory = httpClientFactory;
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
                $"The export '{id}' is missing file metadata or is no longer available.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildExportDownloadUri(id));
        var vital_service_key = configuration.GetString("vital_service_key", host_prefix);
        if (!string.IsNullOrWhiteSpace(vital_service_key))
        {
            request.Headers.TryAddWithoutValidation("vital-service-key", vital_service_key);
        }

        HttpResponseMessage service_response;
        try
        {
            var client = _httpClientFactory.CreateClient(string.Empty);
            service_response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
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
                $"Unable to reach mmria.services while retrieving export '{export_queue_item.file_name}'. {ex.Message}");
        }

        if (service_response.StatusCode == HttpStatusCode.NotFound)
        {
            service_response.Dispose();
            return CreateProblemFileResult(
                StatusCodes.Status404NotFound,
                "Export file not found",
                $"The export '{export_queue_item.file_name}' is not available on mmria.services.");
        }

        if (!service_response.IsSuccessStatusCode)
        {
            var detail = await ReadResponseDetailAsync(service_response);
            var statusCode = (int)service_response.StatusCode;
            service_response.Dispose();
            return CreateProblemFileResult(
                StatusCodes.Status502BadGateway,
                "Export download failed",
                string.IsNullOrWhiteSpace(detail)
                    ? $"mmria.services returned HTTP {statusCode} while retrieving export '{export_queue_item.file_name}'."
                    : detail);
        }

        System.IO.Stream stream;
        try
        {
            stream = await service_response.Content.ReadAsStreamAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            service_response.Dispose();
            return CreateProblemFileResult(
                StatusCodes.Status502BadGateway,
                "Export download failed",
                $"mmria.services returned an unreadable stream for export '{export_queue_item.file_name}'. {ex.Message}");
        }

        try
        {
            await _exportQueueManager.MarkDownloadedAsync(export_queue_item, db_config);
        }
        catch
        {
            service_response.Dispose();
            throw;
        }

        Response.RegisterForDispose(service_response);

        var contentType = service_response.Content.Headers.ContentType?.ToString();
        return File(
            stream,
            string.IsNullOrWhiteSpace(contentType)
                ? System.Net.Mime.MediaTypeNames.Application.Octet
                : contentType,
            export_queue_item.file_name);
    }

    private Uri BuildExportDownloadUri(string id)
    {
        string vitals_url = configuration.GetString("vitals_url", host_prefix);
        if (string.IsNullOrWhiteSpace(vitals_url))
        {
            throw new InvalidOperationException("The current tenant is missing vitals_url configuration.");
        }

        string download_url = vitals_url.Replace(
            "Message/IJESet",
            $"ExportQueue/Download/{Uri.EscapeDataString(id)}");

        if (string.Equals(download_url, vitals_url, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The current tenant vitals_url does not contain the expected Message/IJESet path.");
        }

        if (!Uri.TryCreate(download_url, UriKind.Absolute, out var download_uri))
        {
            throw new InvalidOperationException("The derived export download URL is not a valid absolute URI.");
        }

        var builder = new UriBuilder(download_uri);
        var hostPrefixQuery = $"host_prefix={Uri.EscapeDataString(host_prefix ?? string.Empty)}";
        var existingQuery = builder.Query?.TrimStart('?');
        builder.Query = string.IsNullOrWhiteSpace(existingQuery)
            ? hostPrefixQuery
            : $"{existingQuery}&{hostPrefixQuery}";

        return builder.Uri;
    }

    private async Task<string> ReadResponseDetailAsync(HttpResponseMessage service_response)
    {
        if (service_response.Content == null)
        {
            return null;
        }

        var response_text = await service_response.Content.ReadAsStringAsync(HttpContext.RequestAborted);
        if (string.IsNullOrWhiteSpace(response_text))
        {
            return null;
        }

        try
        {
            var problem = Newtonsoft.Json.JsonConvert.DeserializeObject<ProblemDetails>(response_text);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
            {
                return problem.Detail;
            }
        }
        catch
        {
        }

        return response_text;
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

        var payload = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(problem));
        return File(payload, "application/problem+json");
    }
}




