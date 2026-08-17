using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.model.couchdb.audit;
using mmria.common.SharedLibraries.AuditRecovery.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.Audit.DAL;

public sealed class AuditDAL : IAuditRepository
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public AuditDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task WriteAuditEntryAsync(Change_Stack entry, DBConfigurationDetail dbConfig)
    {
        var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        var json = JsonConvert.SerializeObject(entry, settings);
        var url = dbConfig.Get_Prefix_DB_Url($"audit/{entry._id}");
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        if (result == null || !result.ok)
            System.Console.WriteLine($"AuditDAL.WriteAuditEntryAsync: save failed for audit {entry._id}");
    }

    public async Task<Change_Stack> GetAuditEntryAsync(string auditId, DBConfigurationDetail dbConfig)
    {
        var url = dbConfig.Get_Prefix_DB_Url($"audit/{auditId}");
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<Change_Stack>(response);
    }

    public async Task DeleteAuditEntryAsync(string auditId, string rev, DBConfigurationDetail dbConfig)
    {
        var url = dbConfig.Get_Prefix_DB_Url($"audit/{auditId}?rev={rev}");
        await _couchDbHttpClient.ExecuteAsync("DELETE", url, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<get_sortable_view_reponse_header<Audit_Detail_View>> GetDeletedCasesViewAsync(DBConfigurationDetail dbConfig)
    {
        var url = dbConfig.Get_Prefix_DB_Url("audit/_design/sortable/_view/by_deleted?skip=0&limit=25000&descending=true");
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<get_sortable_view_reponse_header<Audit_Detail_View>>(response)
            ?? new get_sortable_view_reponse_header<Audit_Detail_View>();
    }

    public async Task<Audit_Manage_User?> GetAuditManageUserAsync(DBConfigurationDetail dbConfig)
    {
        var url = dbConfig.Get_Prefix_DB_Url("audit/audit-manage-user");
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        if (response.Contains("\"error\":\"not_found\""))
            return null;
        return JsonConvert.DeserializeObject<Audit_Manage_User>(response);
    }

    public async Task SaveAuditManageUserAsync(Audit_Manage_User doc, DBConfigurationDetail dbConfig)
    {
        var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        var json = JsonConvert.SerializeObject(doc, settings);
        var url = dbConfig.Get_Prefix_DB_Url($"audit/{doc._id}");
        await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<ChangeStackResult> FindAuditsByCaseAsync(string caseId, DBConfigurationDetail dbConfig)
    {
        var selector = new AuditSelector
        {
            selector = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["case_id"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["$eq"] = caseId }
            },
            limit = 10_000,
            use_index = "case-id-date-last-updated-index"
        };
        var json = JsonConvert.SerializeObject(selector, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        var url = dbConfig.Get_Prefix_DB_Url("audit/_find");
        string response = await _couchDbHttpClient.ExecuteAsync("POST", url, json, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<ChangeStackResult>(response);
    }
}
