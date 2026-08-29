using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.metadata;
using mmria.common.model.couchdb;
using mmria.common.model.couchdb.audit;
using mmria.common.SharedLibraries.Audit;
using mmria.common.SharedLibraries.AuditRecovery.Model;
using mmria.common.SharedLibraries.Case;
using mmria.common.SharedLibraries.MetadataVersion;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.AuditRecovery.DAL;

public sealed class AuditRecoveryDAL
{
    private readonly IAuditRepository _auditRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IMetadataRepository _metadataRepository;

    public AuditRecoveryDAL(IAuditRepository auditRepository, ICaseRepository caseRepository, IMetadataRepository metadataRepository)
    {
        _auditRepository = auditRepository;
        _caseRepository = caseRepository;
        _metadataRepository = metadataRepository;
    }

    public async Task<case_view_response> GetCaseViewResponseAsync(string caseId, DBConfigurationDetail db_config)
    {
        string response = await _caseRepository.GetCasesByIdViewJsonAsync(caseId, db_config);
        return JsonConvert.DeserializeObject<case_view_response>(response);
    }

    public async Task<Change_Stack> GetChangeStackAsync(string changeId, DBConfigurationDetail db_config)
    {
        return await _auditRepository.GetAuditEntryAsync(changeId, db_config);
    }

    public async Task<app> GetMetadataAsync(string metadataVersion, DBConfigurationDetail db_config)
    {
        return await _metadataRepository.GetAppDocumentAsync(metadataVersion, db_config);
    }

    public async Task<Audit_Manage_User?> GetAuditManageUserAsync(DBConfigurationDetail db_config)
    {
        return await _auditRepository.GetAuditManageUserAsync(db_config);
    }

    public async Task SaveAuditManageUserAsync(Audit_Manage_User auditDocument, DBConfigurationDetail db_config)
    {
        await _auditRepository.SaveAuditManageUserAsync(auditDocument, db_config);
    }

    public async Task<ExpandoObject> GetCaseRevisionAsync(string caseId, string revisionId, DBConfigurationDetail db_config)
    {
        string response = await _caseRepository.GetCaseAtRevisionAsync(caseId, revisionId, db_config);
        return JsonConvert.DeserializeObject<ExpandoObject>(response);
    }
}
