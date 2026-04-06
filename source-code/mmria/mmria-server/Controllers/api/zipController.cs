using System;
using System.IO;
using System.Linq;
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
                "The requested export is missing file metadata or is no longer available.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildExportDownloadUri(id));
        var vital_service_key = configuration.GetString("vital_service_key", host_prefix);
        if (!string.IsNullOrWhiteSpace(vital_service_key))
        {
            var sanitizedVitalServiceKey = mmria.common.getset.CouchDbHttpClient.SanitizeHeader(vital_service_key)?.Trim();
            if (!string.IsNullOrWhiteSpace(sanitizedVitalServiceKey))
            {
                request.Headers.Add("vital-service-key", sanitizedVitalServiceKey);
            }
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
                "Unable to reach mmria.services while retrieving the requested export.");
        }

        if (service_response.StatusCode == HttpStatusCode.NotFound)
        {
            service_response.Dispose();
            return CreateProblemFileResult(
                StatusCodes.Status404NotFound,
                "Export file not found",
                "The requested export is not available on mmria.services.");
        }

        if (!service_response.IsSuccessStatusCode)
        {
            service_response.Dispose();
            return CreateProblemFileResult(
                StatusCodes.Status502BadGateway,
                "Export download failed",
                "mmria.services returned an error while retrieving the requested export.");
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
                "mmria.services returned an unreadable stream for the requested export.");
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
        var safeDownloadFileName = GetSafeDownloadFileName(export_queue_item.file_name);
        return File(
            stream,
            string.IsNullOrWhiteSpace(contentType)
                ? System.Net.Mime.MediaTypeNames.Application.Octet
                : contentType,
            safeDownloadFileName);
    }

    private Uri BuildExportDownloadUri(string id)
    {
        var servicesBaseUri = GetServicesBaseUri();
        var downloadUri = new Uri(servicesBaseUri, $"api/ExportQueue/Download/{Uri.EscapeDataString(id)}");

        var builder = new UriBuilder(downloadUri);
        var hostPrefixQuery = $"host_prefix={Uri.EscapeDataString(host_prefix ?? string.Empty)}";
        var existingQuery = builder.Query?.TrimStart('?');
        builder.Query = string.IsNullOrWhiteSpace(existingQuery)
            ? hostPrefixQuery
            : $"{existingQuery}&{hostPrefixQuery}";

        return builder.Uri;
    }

    private Uri GetServicesBaseUri()
    {
        string vitals_url = configuration.GetString("vitals_url", host_prefix);
        if (string.IsNullOrWhiteSpace(vitals_url))
        {
            throw new InvalidOperationException("The current tenant is missing vitals_url configuration.");
        }

        var servicesBaseUrl = vitals_url.Replace("/api/Message/IJESet", string.Empty);

        if (string.Equals(servicesBaseUrl, vitals_url, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The current tenant vitals_url does not contain the expected Message/IJESet path.");
        }

        if (!Uri.TryCreate(servicesBaseUrl, UriKind.Absolute, out var servicesUri))
        {
            throw new InvalidOperationException("The derived export services URL is not a valid absolute URI.");
        }

        if (servicesUri.Scheme != Uri.UriSchemeHttp && servicesUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("The derived export services URL must use HTTP or HTTPS.");
        }

        if (!string.IsNullOrWhiteSpace(servicesUri.UserInfo) || !string.IsNullOrWhiteSpace(servicesUri.Fragment))
        {
            throw new InvalidOperationException("The derived export services URL must not contain user info or fragments.");
        }

        return new UriBuilder(servicesUri)
        {
            Query = string.Empty,
            Fragment = string.Empty,
            Path = servicesUri.AbsolutePath.TrimEnd('/') + "/"
        }.Uri;
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




