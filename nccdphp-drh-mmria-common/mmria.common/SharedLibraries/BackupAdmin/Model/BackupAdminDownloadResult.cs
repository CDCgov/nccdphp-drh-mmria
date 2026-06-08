namespace mmria.common.SharedLibraries.BackupAdmin.Model;

public enum BackupAdminDownloadStatus
{
    Success,
    NotFound,
    ServiceError
}

public sealed class BackupAdminDownloadResult
{
    private BackupAdminDownloadResult(BackupAdminDownloadStatus status, int statusCode, byte[] content)
    {
        Status = status;
        StatusCode = statusCode;
        Content = content;
    }

    public BackupAdminDownloadStatus Status { get; }

    public int StatusCode { get; }

    public byte[] Content { get; }

    public bool IsSuccess => Status == BackupAdminDownloadStatus.Success;

    internal static BackupAdminDownloadResult Success(byte[] content)
    {
        return new BackupAdminDownloadResult(BackupAdminDownloadStatus.Success, 200, content);
    }

    internal static BackupAdminDownloadResult NotFound()
    {
        return new BackupAdminDownloadResult(BackupAdminDownloadStatus.NotFound, 404, null);
    }

    internal static BackupAdminDownloadResult ServiceError(int statusCode)
    {
        return new BackupAdminDownloadResult(BackupAdminDownloadStatus.ServiceError, statusCode, null);
    }
}
