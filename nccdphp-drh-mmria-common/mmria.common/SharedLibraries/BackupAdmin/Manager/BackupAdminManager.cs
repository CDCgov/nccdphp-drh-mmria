using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.BackupAdmin.DAL;

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
        var response = await _dal.GetAsync($"{configUrl}/api/backup/GetFileList", vitalServiceKey);
        return JsonSerializer.Deserialize<List<string>>(response);
    }

    public async Task<List<string>> GetRemoveFileListAsync(string configUrl, string vitalServiceKey, int over_number_of_days)
    {
        var response = await _dal.GetAsync($"{configUrl}/api/backup/GetRemoveFileList/{over_number_of_days}", vitalServiceKey);
        return JsonSerializer.Deserialize<List<string>>(response);
    }

    public async Task<List<string>> PerformFileRemovalAsync(string configUrl, string vitalServiceKey, int over_number_of_days)
    {
        var response = await _dal.GetAsync($"{configUrl}/api/backup/RemoveFiles/{over_number_of_days}", vitalServiceKey);
        return JsonSerializer.Deserialize<List<string>>(response);
    }

    public async Task<List<string>> GetSubFolderFileListAsync(string configUrl, string vitalServiceKey, string id)
    {
        var response = await _dal.GetAsync($"{configUrl}/api/backup/GetSubFolderFileList/{id}", vitalServiceKey);
        return JsonSerializer.Deserialize<List<string>>(response);
    }

    public async Task<string> PerformHotBackupAsync(string configUrl, string vitalServiceKey)
    {
        return await _dal.GetAsync($"{configUrl}/api/backup/PerformHotBackup", vitalServiceKey);
    }

    public async Task<string> PerformColdBackupAsync(string configUrl, string vitalServiceKey)
    {
        return await _dal.GetAsync($"{configUrl}/api/backup/PerformColdBackup", vitalServiceKey);
    }

    public async Task<string> PerformCompressionAsync(string configUrl, string vitalServiceKey)
    {
        return await _dal.GetAsync($"{configUrl}/api/backup/PerformCompression", vitalServiceKey);
    }
}
