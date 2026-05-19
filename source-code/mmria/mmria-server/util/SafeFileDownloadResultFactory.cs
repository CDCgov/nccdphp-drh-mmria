using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using HttpMediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace mmria.server.util;

public static class SafeFileDownloadResultFactory
{
    private const int MaxContentTypeLength = 256;

    public static ActionResult Create(byte[] fileBytes, string contentType, string downloadFileName, string fallbackName = "download.bin")
    {
        ArgumentNullException.ThrowIfNull(fileBytes);
        var safeDownloadFileName = CreateSafeDownloadFileName(downloadFileName, fallbackName);

        return new SafeFileContentResult(fileBytes, NormalizeContentType(contentType), safeDownloadFileName);
    }

    public static ActionResult Create(Stream fileStream, string contentType, string downloadFileName, string fallbackName = "download.bin")
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        var safeDownloadFileName = CreateSafeDownloadFileName(downloadFileName, fallbackName);

        return new SafeFileStreamResult(fileStream, NormalizeContentType(contentType), safeDownloadFileName);
    }

    private static string CreateSafeDownloadFileName(string downloadFileName, string fallbackName) =>
        ContainedPathHelper.CreateSafeDownloadFileName(downloadFileName, fallbackName);

    private static string NormalizeContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return System.Net.Mime.MediaTypeNames.Application.Octet;
        }

        var trimmedContentType = contentType.Trim();
        if (trimmedContentType.Length > MaxContentTypeLength ||
            trimmedContentType.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
        {
            return System.Net.Mime.MediaTypeNames.Application.Octet;
        }

        if (!HttpMediaTypeHeaderValue.TryParse(trimmedContentType, out var parsedContentType) ||
            string.IsNullOrWhiteSpace(parsedContentType.MediaType))
        {
            return System.Net.Mime.MediaTypeNames.Application.Octet;
        }

        return parsedContentType.ToString();
    }

    private static string BuildAttachmentHeaderValue(string safeDownloadFileName)
    {
        var quotedFallback = new string(
            safeDownloadFileName
                .Select(static character => character >= 0x20 && character <= 0x7E && character != '"' && character != '\\'
                    ? character
                    : '_')
                .ToArray());

        return $"attachment; filename=\"{quotedFallback}\"; filename*=UTF-8''{Uri.EscapeDataString(safeDownloadFileName)}";
    }

    public sealed class SafeFileContentResult : ActionResult
    {
        public SafeFileContentResult(byte[] fileContents, string contentType, string downloadFileName)
        {
            FileContents = fileContents ?? throw new ArgumentNullException(nameof(fileContents));
            ContentType = contentType;
            DownloadFileName = downloadFileName;
        }

        public byte[] FileContents { get; }

        public string ContentType { get; }

        public string DownloadFileName { get; }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var response = context.HttpContext.Response;
            response.ContentType = ContentType;
            response.ContentLength = FileContents.LongLength;
            response.Headers[HeaderNames.ContentDisposition] = BuildAttachmentHeaderValue(DownloadFileName);
            await response.Body.WriteAsync(FileContents, context.HttpContext.RequestAborted);
        }
    }

    public sealed class SafeFileStreamResult : ActionResult
    {
        public SafeFileStreamResult(Stream fileStream, string contentType, string downloadFileName)
        {
            FileStream = fileStream ?? throw new ArgumentNullException(nameof(fileStream));
            ContentType = contentType;
            DownloadFileName = downloadFileName;
        }

        public Stream FileStream { get; }

        public string ContentType { get; }

        public string DownloadFileName { get; }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var response = context.HttpContext.Response;
            response.ContentType = ContentType;
            response.Headers[HeaderNames.ContentDisposition] = BuildAttachmentHeaderValue(DownloadFileName);

            await using var fileStream = FileStream;
            await fileStream.CopyToAsync(response.Body, context.HttpContext.RequestAborted);
        }
    }
}
