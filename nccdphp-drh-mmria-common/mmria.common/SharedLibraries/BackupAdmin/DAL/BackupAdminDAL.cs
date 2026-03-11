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
        var headers = new Dictionary<string, string> { { "vital-service-key", vitalServiceKey } };
        return await _httpClient.ExecuteAsync("GET", url, null, null, null, "application/json", headers);
    }
}
