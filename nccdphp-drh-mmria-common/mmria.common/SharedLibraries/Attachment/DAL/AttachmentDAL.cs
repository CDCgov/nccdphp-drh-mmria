using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.Attachment.DAL;

public sealed class AttachmentDAL
{
    private readonly CouchDbHttpClient _httpClient;

    public AttachmentDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<pmss_case_view_response> GetPmssCaseViewByNumberAsync(string pmssno, DBConfigurationDetail db_config)
    {
        string request_string = $"{db_config.url}/{db_config.prefix}mmrds/_design/sortable/_view/by_pmss_number?skip=0&take=250000";
        string response = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        var case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<pmss_case_view_response>(response);
        var result = new pmss_case_view_response
        {
            offset = case_view_response.offset,
            total_rows = case_view_response.total_rows
        };

        result.rows = case_view_response.rows
            .Where(r => r.value != null && r.value.pmssno != null && r.value.pmssno.Equals(pmssno, System.StringComparison.OrdinalIgnoreCase))
            .ToList();
        result.total_rows = result.rows.Count;
        return result;
    }
}
