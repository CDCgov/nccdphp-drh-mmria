using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.model.couchdb.audit;
using mmria.common.SharedLibraries.Case;

namespace mmria.common.SharedLibraries.CaseWorkflowAdmin.DAL;

public sealed class CaseWorkflowAdminDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;
    private readonly ICaseRepository _caseRepository;

    public CaseWorkflowAdminDAL(CouchDbHttpClient couchDbHttpClient, ICaseRepository caseRepository)
    {
        _couchDbHttpClient = couchDbHttpClient;
        _caseRepository = caseRepository;
    }

    // ── clear_case_status: FindRecord ────────────────────────────────────

    public async Task<case_view_response> GetCasesByDateAsync(DBConfigurationDetail dbConfig)
    {
        var json = await _caseRepository.GetCasesByDateLastUpdatedViewJsonAsync(dbConfig);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<case_view_response>(json)
            ?? new case_view_response();
    }

    // ── clear_case_status: ClearCaseStatus ───────────────────────────────

    public async Task<System.Dynamic.ExpandoObject> GetCaseDocumentAsync(DBConfigurationDetail dbConfig, string caseId)
    {
        var json = await _caseRepository.GetCaseDocumentJsonAsync(caseId, dbConfig);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(json);
    }

    public async Task<document_put_response> UpdateCaseDocumentAsync(DBConfigurationDetail dbConfig, string caseId, string caseJson)
    {
        var json = await _caseRepository.PutCaseDocumentJsonAsync(caseId, caseJson, dbConfig);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<document_put_response>(json)
            ?? new document_put_response { ok = false };
    }

    public async Task WriteAuditEntryAsync(DBConfigurationDetail dbConfig, Change_Stack auditEntry)
    {
        var settings = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(auditEntry, settings);
        var url = dbConfig.Get_Prefix_DB_Url($"audit/{auditEntry._id}");
        await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
    }

    // ── recover_deleted_case: FindRecord ─────────────────────────────────

    public async Task<get_sortable_view_reponse_header<Audit_Detail_View>> GetDeletedCasesViewAsync(DBConfigurationDetail dbConfig)
    {
        var url = dbConfig.Get_Prefix_DB_Url("audit/_design/sortable/_view/by_deleted?skip=0&limit=25000&descending=true");
        var response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<get_sortable_view_reponse_header<Audit_Detail_View>>(response)
            ?? new get_sortable_view_reponse_header<Audit_Detail_View>();
    }

    // ── recover_deleted_case: UpdateDeletedCase ───────────────────────────

    public async Task<Change_Stack> GetAuditDocumentAsync(DBConfigurationDetail dbConfig, string auditId)
    {
        var url = dbConfig.Get_Prefix_DB_Url($"audit/{auditId}");
        var response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Change_Stack>(response);
    }

    public async Task<string> GetCaseRevisionsRawAsync(DBConfigurationDetail dbConfig, string caseId)
    {
        return await _caseRepository.GetCaseRevisionsRawAsync(caseId, dbConfig);
    }

    public async Task<System.Dynamic.ExpandoObject> GetCaseAtRevisionAsync(DBConfigurationDetail dbConfig, string caseId, string revision)
    {
        var json = await _caseRepository.GetCaseAtRevisionAsync(caseId, revision, dbConfig);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(json);
    }

    public async Task<document_put_response> RestoreCaseDocumentAsync(DBConfigurationDetail dbConfig, string caseId, string caseJson)
    {
        var json = await _caseRepository.PutCaseDocumentJsonAsync(caseId, caseJson, dbConfig);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<document_put_response>(json)
            ?? new document_put_response { ok = false };
    }

    public async Task DeleteAuditDocumentAsync(DBConfigurationDetail dbConfig, string auditId, string rev)
    {
        var url = dbConfig.Get_Prefix_DB_Url($"audit/{auditId}?rev={rev}");
        await _couchDbHttpClient.ExecuteAsync("DELETE", url, null, dbConfig.user_name, dbConfig.user_value);
    }
}

