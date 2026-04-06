using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace mmria.server.util;

public static class SafeFileDownloadResultFactory
{
    public static FileContentResult Create(byte[] fileBytes, string contentType, string downloadFileName, string fallbackName = "download.bin")
    {
        ArgumentNullException.ThrowIfNull(fileBytes);

        return new FileContentResult(fileBytes, NormalizeContentType(contentType))
        {
            FileDownloadName = ContainedPathHelper.CreateSafeDownloadFileName(downloadFileName, fallbackName)
        };
    }

    public static FileStreamResult Create(Stream fileStream, string contentType, string downloadFileName, string fallbackName = "download.bin")
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        return new FileStreamResult(fileStream, NormalizeContentType(contentType))
        {
            FileDownloadName = ContainedPathHelper.CreateSafeDownloadFileName(downloadFileName, fallbackName)
        };
    }

    private static string NormalizeContentType(string contentType) =>
        string.IsNullOrWhiteSpace(contentType)
            ? System.Net.Mime.MediaTypeNames.Application.Octet
            : contentType;
}
