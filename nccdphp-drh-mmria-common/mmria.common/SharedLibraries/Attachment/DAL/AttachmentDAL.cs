using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Case;

namespace mmria.common.SharedLibraries.Attachment.DAL;

public sealed class AttachmentDAL
{
    private readonly CouchDbHttpClient _httpClient;
    private readonly ICaseRepository _caseRepository;

    public AttachmentDAL(CouchDbHttpClient httpClient, ICaseRepository caseRepository)
    {
        _httpClient = httpClient;
        _caseRepository = caseRepository;
    }

    public async Task<pmss_case_view_response> GetPmssCaseViewByNumberAsync(string pmssno, DBConfigurationDetail db_config)
    {
        string response = await _caseRepository.GetCasesByPmssNumberViewJsonAsync(db_config);
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
