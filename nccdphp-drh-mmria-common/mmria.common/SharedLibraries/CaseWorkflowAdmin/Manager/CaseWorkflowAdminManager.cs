using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.model.couchdb.audit;
using mmria.common.SharedLibraries.CaseWorkflowAdmin.DAL;

namespace mmria.common.SharedLibraries.CaseWorkflowAdmin.Manager;

/// <summary>
/// Manager for case workflow admin operations (clear status, recover deleted).
/// Contains business logic moved from clear_case_status.cs and recover_deleted_case.cs.
/// Audit writes (Story 7.2) live here — not in controllers.
/// NO outer try/catch — controllers own error surfacing.
/// </summary>
public sealed class CaseWorkflowAdminManager
{
    private readonly CaseWorkflowAdminDAL _dal;

    public CaseWorkflowAdminManager(CaseWorkflowAdminDAL dal)
    {
        _dal = dal;
    }

    // ── clear_case_status ────────────────────────────────────────────────

    public async Task<case_view_response> GetCasesByDateAsync(DBConfigurationDetail dbConfig)
        => await _dal.GetCasesByDateAsync(dbConfig);

    /// <summary>
    /// Fetches the case document, clears case_status.overall_case_status to 9999,
    /// updates last_updated_by / date_last_updated, PUTs the case back, and on success
    /// writes the Change_Stack audit entry. Returns (ok, oldCaseStatus, errorMessage).
    /// </summary>
    public async Task<(bool ok, string oldCaseStatus, string errorMessage)> ClearCaseStatusAsync(
        DBConfigurationDetail dbConfig,
        string caseId,
        string userName)
    {
        var caseDoc = await _dal.GetCaseDocumentAsync(dbConfig, caseId);
        var dictionary = caseDoc as IDictionary<string, object>;
        if (dictionary == null)
            return (false, "", "Case document could not be parsed.");

        if (!dictionary.TryGetValue("home_record", out var homeRecordObj) || homeRecordObj is not IDictionary<string, object> home_record)
            return (false, "", "home_record not found in case document.");

        if (!home_record.TryGetValue("case_status", out var caseStatusObj) || caseStatusObj is not IDictionary<string, object> case_status)
            return (false, "", "case_status not found in home_record.");

        var oldCaseStatus = case_status.TryGetValue("overall_case_status", out var existing) ? existing?.ToString() ?? "" : "";
        case_status["overall_case_status"] = 9999;
        case_status["case_locked_date"] = "";
        dictionary["last_updated_by"] = userName;
        dictionary["date_last_updated"] = DateTime.Now;

        var settings = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
        var caseJson = Newtonsoft.Json.JsonConvert.SerializeObject(caseDoc, settings);

        var putResult = await _dal.UpdateCaseDocumentAsync(dbConfig, caseId, caseJson);
        if (!putResult.ok)
            return (false, oldCaseStatus, "PUT case document returned ok=false.");

        var auditEntry = new Change_Stack
        {
            _id = Guid.NewGuid().ToString(),
            case_id = caseId,
            user_name = userName,
            note = "admin change, case unlocked, case status cleared",
            old_value = oldCaseStatus,
            new_value = "",
            date_created = DateTime.UtcNow,
            doc_type = "Change_Stack",
            items = new List<Change_Stack_Item>
            {
                new Change_Stack_Item
                {
                    user_name = userName,
                    prompt = "Case Status",
                    object_path = "/home_record/case_status/overall_case_status",
                    dictionary_path = "/home_record/case_status/overall_case_status",
                    old_value = oldCaseStatus,
                    new_value = "9999",
                    doc_type = "Change_Stack_Item"
                }
            }
        };
        try
        {
            await _dal.WriteAuditEntryAsync(dbConfig, auditEntry);
        }
        catch (Exception auditEx)
        {
            Console.WriteLine($"CaseWorkflowAdminManager.ClearCaseStatusAsync audit write failed: {auditEx.Message}");
        }

        return (true, oldCaseStatus, null);
    }

    // ── recover_deleted_case ─────────────────────────────────────────────

    public async Task<get_sortable_view_reponse_header<Audit_Detail_View>> GetDeletedCasesViewAsync(DBConfigurationDetail dbConfig)
        => await _dal.GetDeletedCasesViewAsync(dbConfig);

    /// <summary>
    /// Fetches audit doc, resolves current _rev via open_revs, fetches the deleted
    /// revision, PUTs the case back, DELETEs the tombstone, and writes audit entry.
    /// Returns (ok, errorMessage). Controller retains its outer try/catch.
    /// NOTE: The revision-parsing logic is preserved verbatim from the original controller.
    /// </summary>
    public async Task<(bool ok, string errorMessage)> RecoverDeletedCaseAsync(
        DBConfigurationDetail dbConfig,
        string auditId,
        string userName)
    {
        var auditDoc = await _dal.GetAuditDocumentAsync(dbConfig, auditId);

        var revisionsRaw = await _dal.GetCaseRevisionsRawAsync(dbConfig, auditDoc.case_id);
        var startIndex = revisionsRaw.IndexOf("_rev");
        var endIndex = revisionsRaw.IndexOf(",", startIndex);
        var currentRev = revisionsRaw.Substring(startIndex, endIndex - startIndex)
            .Replace("\"", "").Replace("_rev:", "");

        var caseDoc = await _dal.GetCaseAtRevisionAsync(dbConfig, auditDoc.case_id, auditDoc.delete_rev);
        var resultDictionary = caseDoc as IDictionary<string, object>;
        if (resultDictionary == null)
            return (false, "Case document at deleted revision could not be parsed.");

        if (resultDictionary.ContainsKey("_rev"))
            resultDictionary.Remove("_rev");

        resultDictionary["date_last_updated"] = DateTime.Now;
        resultDictionary["last_updated_by"] = userName;

        var settings = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
        var caseJson = Newtonsoft.Json.JsonConvert.SerializeObject(caseDoc, settings);

        var putResult = await _dal.RestoreCaseDocumentAsync(dbConfig, auditDoc.case_id, caseJson);
        if (!putResult.ok)
            return (false, "PUT restore case returned ok=false.");

        await _dal.DeleteAuditDocumentAsync(dbConfig, auditId, auditDoc._rev);

        var auditEntry = new Change_Stack
        {
            _id = Guid.NewGuid().ToString(),
            case_id = auditDoc.case_id,
            user_name = userName,
            note = "admin change, case recovered",
            date_created = DateTime.UtcNow,
            doc_type = "Change_Stack",
            items = new List<Change_Stack_Item>
            {
                new Change_Stack_Item
                {
                    user_name = userName,
                    prompt = "Case Recovered",
                    object_path = "/case_id",
                    dictionary_path = "/case_id",
                    old_value = "deleted",
                    new_value = "recovered",
                    doc_type = "Change_Stack_Item"
                }
            }
        };
        try
        {
            await _dal.WriteAuditEntryAsync(dbConfig, auditEntry);
        }
        catch (Exception auditEx)
        {
            Console.WriteLine($"CaseWorkflowAdminManager.RecoverDeletedCaseAsync audit write failed: {auditEx.Message}");
        }

        return (true, null);
    }
}
