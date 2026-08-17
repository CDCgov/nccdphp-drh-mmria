using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.model.couchdb.audit;
using mmria.common.SharedLibraries.AuditRecovery.Model;

namespace mmria.common.SharedLibraries.Audit;

public interface IAuditRepository
{
    Task WriteAuditEntryAsync(Change_Stack entry, DBConfigurationDetail dbConfig);
    Task<Change_Stack> GetAuditEntryAsync(string auditId, DBConfigurationDetail dbConfig);
    Task DeleteAuditEntryAsync(string auditId, string rev, DBConfigurationDetail dbConfig);
    Task<get_sortable_view_reponse_header<Audit_Detail_View>> GetDeletedCasesViewAsync(DBConfigurationDetail dbConfig);
    Task<Audit_Manage_User?> GetAuditManageUserAsync(DBConfigurationDetail dbConfig);
    Task SaveAuditManageUserAsync(Audit_Manage_User doc, DBConfigurationDetail dbConfig);
    Task<ChangeStackResult> FindAuditsByCaseAsync(string caseId, DBConfigurationDetail dbConfig);
}
