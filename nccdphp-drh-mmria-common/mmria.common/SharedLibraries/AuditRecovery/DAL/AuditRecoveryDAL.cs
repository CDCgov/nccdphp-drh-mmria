using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.metadata;
using mmria.common.model.couchdb;
using mmria.common.model.couchdb.audit;
using mmria.common.SharedLibraries.AuditRecovery.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.AuditRecovery.DAL;

public sealed class AuditRecoveryDAL
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public AuditRecoveryDAL(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<case_view_response> GetCaseViewResponseAsync(string caseId, DBConfigurationDetail db_config)
    {
        string request = $"{db_config.url}/{db_config.prefix}mmrds/_design/sortable/_view/by_id?key=\"{caseId}\"";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", request, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<case_view_response>(response);
    }

    public async Task<ChangeStackResult> FindChangeStacksAsync(string requestUrl, string postData, DBConfigurationDetail db_config)
    {
        string response = await _couchDbHttpClient.ExecuteAsync("POST", requestUrl, postData, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<ChangeStackResult>(response);
    }

    public async Task<Change_Stack> GetChangeStackAsync(string changeId, DBConfigurationDetail db_config)
    {
        string request = $"{db_config.url}/{db_config.prefix}audit/{changeId}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", request, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<Change_Stack>(response);
    }

    public async Task<get_sortable_view_reponse_header<Audit_Detail_View>> GetDeletedAuditDetailViewAsync(DBConfigurationDetail db_config)
    {
        string request = $"{db_config.url}/{db_config.prefix}audit/_design/sortable/_view/by_deleted?skip=0&limit=25000&descending=true";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", request, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<get_sortable_view_reponse_header<Audit_Detail_View>>(response);
    }

    public async Task<string> GetOpenRevisionsJsonAsync(string caseId, DBConfigurationDetail db_config)
    {
        string request = $"{db_config.url}/{db_config.prefix}mmrds/{caseId}?revs=true&open_revs=all";
        return await _couchDbHttpClient.ExecuteAsync("GET", request, null, db_config.user_name, db_config.user_value);
    }

    public async Task<document_put_response> RestoreCaseDocumentJsonAsync(string caseId, string caseDocumentJson, DBConfigurationDetail db_config)
    {
        string request = $"{db_config.url}/{db_config.prefix}mmrds/{caseId}";
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", request, caseDocumentJson, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<document_put_response> DeleteAuditDocumentAsync(string auditId, string rev, DBConfigurationDetail db_config)
    {
        string request = $"{db_config.url}/{db_config.prefix}audit/{auditId}?rev={rev}";
        string response = await _couchDbHttpClient.ExecuteAsync("DELETE", request, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<app> GetMetadataAsync(string metadataVersion, DBConfigurationDetail db_config)
    {
        string request = $"{db_config.url}/metadata/version_specification-{metadataVersion}/metadata";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", request, null, null, null);
        return JsonConvert.DeserializeObject<app>(response);
    }

    public async Task<Audit_Manage_User> GetAuditManageUserAsync(DBConfigurationDetail db_config)
    {
        string request = $"{db_config.url}/{db_config.prefix}audit/audit-manage-user";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", request, null, db_config.user_name, db_config.user_value);
        if (response.Contains("\"error\":\"not_found\""))
        {
            return null;
        }

        return JsonConvert.DeserializeObject<Audit_Manage_User>(response);
    }

    public async Task<document_put_response> SaveAuditManageUserAsync(Audit_Manage_User auditDocument, DBConfigurationDetail db_config)
    {
        string body = JsonConvert.SerializeObject(auditDocument, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        string request = $"{db_config.url}/{db_config.prefix}audit/{auditDocument._id}";
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", request, body, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<ExpandoObject> GetCaseRevisionAsync(string caseId, string revisionId, DBConfigurationDetail db_config)
    {
        string request = $"{db_config.url}/{db_config.prefix}mmrds/{caseId}?rev={revisionId}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", request, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<ExpandoObject>(response);
    }
}
