using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.model.couchdb.audit;
using mmria.common.SharedLibraries.Audit;
using mmria.common.SharedLibraries.Case;

namespace mmria.common.SharedLibraries.CaseWorkflowAdmin.DAL;

public sealed class CaseWorkflowAdminDAL
{
    private readonly ICaseRepository _caseRepository;
    private readonly IAuditRepository _auditRepository;

    public CaseWorkflowAdminDAL(ICaseRepository caseRepository, IAuditRepository auditRepository)
    {
        _caseRepository = caseRepository;
        _auditRepository = auditRepository;
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
        await _auditRepository.WriteAuditEntryAsync(auditEntry, dbConfig);
    }

    // ── recover_deleted_case: FindRecord ─────────────────────────────────

    public async Task<get_sortable_view_reponse_header<Audit_Detail_View>> GetDeletedCasesViewAsync(DBConfigurationDetail dbConfig)
    {
        return await _auditRepository.GetDeletedCasesViewAsync(dbConfig);
    }

    // ── recover_deleted_case: UpdateDeletedCase ───────────────────────────

    public async Task<Change_Stack> GetAuditDocumentAsync(DBConfigurationDetail dbConfig, string auditId)
    {
        return await _auditRepository.GetAuditEntryAsync(auditId, dbConfig);
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
        await _auditRepository.DeleteAuditEntryAsync(auditId, rev, dbConfig);
    }
}

