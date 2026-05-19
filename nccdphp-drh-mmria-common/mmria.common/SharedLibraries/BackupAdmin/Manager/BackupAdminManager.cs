using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.BackupAdmin.DAL;
using System;
using System.Linq;

namespace mmria.common.SharedLibraries.BackupAdmin.Manager;

public sealed class BackupAdminManager
{
    private readonly BackupAdminDAL _dal;

    public BackupAdminManager(BackupAdminDAL dal)
    {
        _dal = dal;
    }

    public async Task<List<string>> GetFileListAsync(string configUrl, string vitalServiceKey)
    {
        var response = await _dal.GetAsync(BuildBackupServiceUri(configUrl, "GetFileList").AbsoluteUri, vitalServiceKey);
        return JsonSerializer.Deserialize<List<string>>(response);
    }

    public async Task<List<string>> GetRemoveFileListAsync(string configUrl, string vitalServiceKey, int over_number_of_days)
    {
        var response = await _dal.GetAsync(BuildBackupServiceUri(configUrl, "GetRemoveFileList", over_number_of_days.ToString()).AbsoluteUri, vitalServiceKey);
        return JsonSerializer.Deserialize<List<string>>(response);
    }

    public async Task<List<string>> PerformFileRemovalAsync(string configUrl, string vitalServiceKey, int over_number_of_days)
    {
        var response = await _dal.GetAsync(BuildBackupServiceUri(configUrl, "RemoveFiles", over_number_of_days.ToString()).AbsoluteUri, vitalServiceKey);
        return JsonSerializer.Deserialize<List<string>>(response);
    }

    public async Task<List<string>> GetSubFolderFileListAsync(string configUrl, string vitalServiceKey, string id)
    {
        var response = await _dal.GetAsync(BuildBackupServiceUri(configUrl, "GetSubFolderFileList", id).AbsoluteUri, vitalServiceKey);
        return JsonSerializer.Deserialize<List<string>>(response);
    }

    public async Task<string> PerformHotBackupAsync(string configUrl, string vitalServiceKey)
    {
        return await _dal.GetAsync(BuildBackupServiceUri(configUrl, "PerformHotBackup").AbsoluteUri, vitalServiceKey);
    }

    public async Task<string> PerformColdBackupAsync(string configUrl, string vitalServiceKey)
    {
        return await _dal.GetAsync(BuildBackupServiceUri(configUrl, "PerformColdBackup").AbsoluteUri, vitalServiceKey);
    }

    public async Task<string> PerformCompressionAsync(string configUrl, string vitalServiceKey)
    {
        return await _dal.GetAsync(BuildBackupServiceUri(configUrl, "PerformCompression").AbsoluteUri, vitalServiceKey);
    }

    public async Task<BackupAdminDownloadResult> DownloadFileAsync(string configUrl, string vitalServiceKey, string fileName)
    {
        var response = await _dal.GetBytesAsync(
            BuildBackupServiceUri(configUrl, "GetFile", fileName).AbsoluteUri,
            vitalServiceKey);

        return BackupAdminDownloadResult.FromResponse(response);
    }

    public async Task<BackupAdminDownloadResult> DownloadSubFolderFileAsync(string configUrl, string vitalServiceKey, string folderName, string fileName)
    {
        var response = await _dal.GetBytesAsync(
            BuildBackupServiceUri(configUrl, "GetSubFolderFile", folderName, fileName).AbsoluteUri,
            vitalServiceKey);

        return BackupAdminDownloadResult.FromResponse(response);
    }

    private static Uri BuildBackupServiceUri(string configUrl, params string[] pathSegments)
    {
        if (string.IsNullOrWhiteSpace(configUrl))
        {
            throw new ArgumentException("Backup service configuration URL is required.", nameof(configUrl));
        }

        if (!Uri.TryCreate(configUrl, UriKind.Absolute, out var parsedBaseUri))
        {
            throw new ArgumentException("Backup service configuration URL must be an absolute URI.", nameof(configUrl));
        }

        if (parsedBaseUri.Scheme != Uri.UriSchemeHttp && parsedBaseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Backup service configuration URL must use HTTP or HTTPS.", nameof(configUrl));
        }

        if (!string.IsNullOrWhiteSpace(parsedBaseUri.UserInfo) || !string.IsNullOrWhiteSpace(parsedBaseUri.Fragment))
        {
            throw new ArgumentException("Backup service configuration URL must not contain user info or fragments.", nameof(configUrl));
        }

        var normalizedBaseUri = new UriBuilder(parsedBaseUri)
        {
            Query = string.Empty,
            Fragment = string.Empty,
            Path = parsedBaseUri.AbsolutePath.TrimEnd('/') + "/"
        }.Uri;

        var encodedSegments = string.Join(
            "/",
            pathSegments
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .Select(segment => Uri.EscapeDataString(segment.Trim())));

        return new Uri(normalizedBaseUri, $"api/backup/{encodedSegments}");
    }
}

public sealed class BackupAdminDownloadResult
{
    public byte[] Body { get; init; } = Array.Empty<byte>();
    public int StatusCode { get; init; }
    public string ContentType { get; init; }

    public bool IsNotFound => StatusCode == 404;
    public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode <= 299;

    public static BackupAdminDownloadResult FromResponse(mmria.common.getset.CouchDbByteArrayResponse response)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        return new BackupAdminDownloadResult
        {
            Body = response.Body ?? Array.Empty<byte>(),
            StatusCode = response.StatusCode,
            ContentType = response.GetFirstHeaderValue("Content-Type")
        };
    }
}
