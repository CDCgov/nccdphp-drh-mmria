using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.getset;

namespace mmria.common.SharedLibraries.BackupAdmin.DAL;

public sealed class BackupAdminDAL
{
    private readonly CouchDbHttpClient _httpClient;

    public BackupAdminDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetAsync(string url, string vitalServiceKey)
    {
        return await _httpClient.ExecuteAsync(
            "GET",
            url,
            null,
            "application/json",
            new CouchDbRequestOptions
            {
                VitalServiceKey = vitalServiceKey
            });
    }
}
