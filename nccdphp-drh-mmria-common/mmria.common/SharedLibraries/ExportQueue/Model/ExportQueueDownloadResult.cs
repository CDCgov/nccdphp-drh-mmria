using System;
using System.IO;

namespace mmria.common.SharedLibraries.ExportQueue.Model;

public enum ExportQueueDownloadStatus
{
    Success,
    NotFound,
    ServiceError,
    Unreadable
}

public sealed class ExportQueueDownloadResult : IDisposable
{
    private readonly IDisposable _response;
    private bool _disposed;

    private ExportQueueDownloadResult(
        ExportQueueDownloadStatus status,
        Stream contentStream,
        string contentType,
        IDisposable response)
    {
        Status = status;
        ContentStream = contentStream;
        ContentType = contentType;
        _response = response;
    }

    public ExportQueueDownloadStatus Status { get; }

    public Stream ContentStream { get; }

    public string ContentType { get; }

    public bool IsSuccess => Status == ExportQueueDownloadStatus.Success;

    internal static ExportQueueDownloadResult Success(Stream contentStream, string contentType, IDisposable response)
    {
        return new ExportQueueDownloadResult(
            ExportQueueDownloadStatus.Success,
            contentStream,
            contentType,
            response);
    }

    internal static ExportQueueDownloadResult NotFound()
    {
        return new ExportQueueDownloadResult(
            ExportQueueDownloadStatus.NotFound,
            null,
            null,
            null);
    }

    internal static ExportQueueDownloadResult ServiceError()
    {
        return new ExportQueueDownloadResult(
            ExportQueueDownloadStatus.ServiceError,
            null,
            null,
            null);
    }

    internal static ExportQueueDownloadResult Unreadable()
    {
        return new ExportQueueDownloadResult(
            ExportQueueDownloadStatus.Unreadable,
            null,
            null,
            null);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ContentStream?.Dispose();
        _response?.Dispose();
        _disposed = true;
    }
}
