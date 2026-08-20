using System;
using System.Threading.Tasks;
using mmria.case_version.v260615;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Audit;
using mmria.common.SharedLibraries.Case;
using mmria.common.SharedLibraries.Case.Manager;
using mmria.common.utils;

namespace mmria.services.SharedLibraries.VitalImport.Manager;

// Story 29.8: dedicated writer for vital-import batch case creation.
// Bypasses SaveCaseAsync's user-request authorization check (the batch runs
// under a synthetic service identity that intentionally holds no
// role/jurisdiction assignments), while still enforcing Story 29.1's
// record_id format/uniqueness guards and running the Story 29.4 collision-
// retry loop. Registered only in the vital-import service DI graph and kept
// internal so mmria-server controllers cannot resolve or bind to it.
internal sealed class VitalImportCaseWriter
{
    internal const string VitalImportUserName = "vital-import";
    internal const int MaxRecordIdRetries = 5;

    private readonly ICaseRepository _caseRepository;
    private readonly CaseManager _caseManager;
    private readonly IAuditRepository _auditRepository;

    public VitalImportCaseWriter(
        ICaseRepository caseRepository,
        CaseManager caseManager,
        IAuditRepository auditRepository)
    {
        _caseRepository = caseRepository;
        _caseManager = caseManager;
        _auditRepository = auditRepository;
    }

    // Persists a brand-new case for a vitals-import batch item. Runs Story 29.1
    // record_id format/uniqueness guards and Story 29.4 collision retry inside
    // a 5-attempt cap. Writes the audit change_stack with user_name = "vital-import"
    // on success. Takes no ClaimsPrincipal - a controller reaching for this
    // method has no legitimate signature to bind to.
    public async Task<SaveCaseResult> SaveNewVitalImportCaseAsync(
        mmria_case caseData,
        Change_Stack changeStack,
        DBConfigurationDetail dbConfig,
        OverridableConfiguration configuration,
        string hostPrefix)
    {
        var result = new SaveCaseResult { Response = new document_put_response() };

        if (caseData == null || changeStack == null ||
            string.IsNullOrWhiteSpace(caseData._id) || caseData.home_record == null)
        {
            result.Response.ok = false;
            result.Response.error_description = "Invalid case payload.";
            return result;
        }

        var now = DateTime.UtcNow;
        caseData.created_by = VitalImportUserName;
        caseData.last_updated_by = VitalImportUserName;
        caseData.date_created ??= now;
        caseData.date_last_updated = now;

        StampChangeStackAttribution(changeStack, caseData, now);

        var mmria_record_id = caseData.home_record.record_id;
        document_put_response response = null;

        for (int attempt = 1; attempt <= MaxRecordIdRetries; attempt++)
        {
            var object_string = CaseJsonSerialization.SerializeMmriaCase(caseData);

            response = await _caseManager.ValidateRecordIdAndPersistAsync(
                caseData._id,
                object_string,
                caseData.home_record.record_id,
                enforceRecordIdGuards: true,
                dbConfig);

            if (response != null && response.ok)
            {
                changeStack.record_id = caseData.home_record.record_id;
                changeStack.metadata_version = configuration?.GetString("metadata_version", hostPrefix);
                await _auditRepository.WriteAuditEntryAsync(changeStack, dbConfig);

                result.CaseId = caseData._id;
                result.SerializedCase = object_string;
                result.Response = response;
                return result;
            }

            if (response != null &&
                string.Equals(response.error_code, SaveErrorCodes.RecordIdConflict, StringComparison.Ordinal) &&
                attempt < MaxRecordIdRetries)
            {
                if (!TryExtractStatePrefixAndYear(caseData.home_record.record_id, out var statePrefix, out var year))
                {
                    result.Response = response;
                    return result;
                }

                try
                {
                    var new_record_id = await _caseManager.GenerateUniqueRecordIdAsync(statePrefix, year, dbConfig);
                    caseData.home_record.record_id = new_record_id;
                    mmria_record_id = new_record_id;
                    Console.WriteLine($"VitalImportCaseWriter record_id collision retry attempt={attempt} case_id={caseData._id} new_record_id={new_record_id}");
                    continue;
                }
                catch (Exception genEx)
                {
                    response.error_description = $"unable to generate unique record id after {attempt} attempts: {genEx.Message}";
                    result.Response = response;
                    return result;
                }
            }

            // Non-conflict failure, or final attempt.
            result.Response = response ?? new document_put_response
            {
                ok = false,
                error_description = "unknown save failure"
            };
            return result;
        }

        result.Response = new document_put_response
        {
            ok = false,
            error_code = SaveErrorCodes.RecordIdConflict,
            error_description = "unable to generate unique record id after 5 attempts"
        };
        return result;
    }

    private static void StampChangeStackAttribution(Change_Stack changeStack, mmria_case caseData, DateTime now)
    {
        changeStack._id = string.IsNullOrWhiteSpace(changeStack._id) ? Guid.NewGuid().ToString() : changeStack._id;
        changeStack.case_id = caseData._id;
        changeStack.case_rev = caseData._rev;
        changeStack.user_name = VitalImportUserName;
        changeStack.date_created ??= now;
        changeStack.doc_type = "Change_Stack";

        if (changeStack.items != null)
        {
            foreach (var item in changeStack.items)
            {
                if (item == null) continue;
                item.user_name = VitalImportUserName;
                item.doc_type = "Change_Stack_Item";
            }
        }
    }

    private static bool TryExtractStatePrefixAndYear(string recordId, out string statePrefix, out string year)
    {
        statePrefix = null;
        year = null;

        if (string.IsNullOrWhiteSpace(recordId))
        {
            return false;
        }

        var segments = recordId.Split('-');
        if (segments.Length < 3 ||
            !System.Text.RegularExpressions.Regex.IsMatch(segments[^2], @"^\d{4}$"))
        {
            return false;
        }

        var prefix = string.Join('-', segments[..^2]);
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return false;
        }

        statePrefix = prefix;
        year = segments[^2];
        return true;
    }
}
