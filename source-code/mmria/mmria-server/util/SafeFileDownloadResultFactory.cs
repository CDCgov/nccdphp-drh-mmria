using System;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace mmria.server.util;

public static class SafeFileDownloadResultFactory
{
    private const int MaxContentTypeLength = 256;

    public static FileContentResult Create(byte[] fileBytes, string contentType, string downloadFileName, string fallbackName = "download.bin")
    {
        ArgumentNullException.ThrowIfNull(fileBytes);
        var safeDownloadFileName = CreateSafeDownloadFileName(downloadFileName, fallbackName);

        return new FileContentResult(fileBytes, NormalizeContentType(contentType))
        {
            FileDownloadName = safeDownloadFileName
        };
    }

    public static FileStreamResult Create(Stream fileStream, string contentType, string downloadFileName, string fallbackName = "download.bin")
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        var safeDownloadFileName = CreateSafeDownloadFileName(downloadFileName, fallbackName);

        return new FileStreamResult(fileStream, NormalizeContentType(contentType))
        {
            FileDownloadName = safeDownloadFileName
        };
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

        if (!MediaTypeHeaderValue.TryParse(trimmedContentType, out var parsedContentType) ||
            string.IsNullOrWhiteSpace(parsedContentType.MediaType))
        {
            return System.Net.Mime.MediaTypeNames.Application.Octet;
        }

        return parsedContentType.ToString();
    }
}
