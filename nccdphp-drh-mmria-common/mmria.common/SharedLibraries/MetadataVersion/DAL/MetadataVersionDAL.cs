using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.CaseValidation.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace mmria.common.SharedLibraries.MetadataVersion.DAL;

/// <summary>
/// Data Access Layer for all in-scope <c>metadata</c> CouchDB operations.
/// Implements <see cref="IMetadataRepository"/> — the SQL migration seam for the metadata database.
/// No business logic — only data operations.
/// </summary>
public sealed class MetadataVersionDAL : IMetadataRepository
{
    private const string DefaultMetadataId = "2016-06-12T13:49:24.759Z";
    private const string CheckCodeAttachmentName = "mmria-check-code.js";
    private const string ValidatorAttachmentName = "validator.js";
    private const string DeIdentifiedListId = "de-identified-list";
    private const string DeIdentifiedExportListId = "de-identified-export-list";
    private const string PopulateCDCInstanceId = "populate-cdc-instance";
    private const string ExportStandardListId = "export-standard-list";
    private const string SubstanceMappingId = "substance-mapping";
    private const string DuplicateMultiFormListId = "duplicate-multiform-list";
    private const string CaseValidationRulesId = "case-validation-rules";
    private const string BroadcastMessageListId = "broadcast-message-list";
    private const string SystemOfflineConfigId = "system-offline-config";

    private static readonly JsonSerializerSettings IgnoreNullSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    private readonly CouchDbHttpClient _couchDbHttpClient;

    public MetadataVersionDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    // ── Version Specification — App Document ──────────────────────────────────

    /// <inheritdoc />
    public async Task<mmria.common.metadata.app> GetAppDocumentAsync(string version, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/version_specification-{version}/metadata";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<mmria.common.metadata.app>(response, IgnoreNullSettings);
    }

    // ── Default Metadata Document (legacy root schema) ────────────────────────

    /// <inheritdoc />
    public async Task<ExpandoObject> GetDefaultMetadataDocumentAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{DefaultMetadataId}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<ExpandoObject>(response, new ExpandoObjectConverter());
    }

    /// <inheritdoc />
    public async Task<ExpandoObject> GetMetadataDocumentByIdAsync(string id, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{id}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<ExpandoObject>(response, new ExpandoObjectConverter());
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveMetadataDocumentAsync(mmria.common.metadata.app metadata, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{metadata._id}";
        string json = JsonConvert.SerializeObject(metadata, IgnoreNullSettings);
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    // ── Version Specification Envelope ────────────────────────────────────────

    /// <inheritdoc />
    public async Task<mmria.common.metadata.Version_Specification> GetVersionSpecificationEnvelopeAsync(string version, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/version_specification-{version}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<mmria.common.metadata.Version_Specification>(response, IgnoreNullSettings);
    }

    /// <inheritdoc />
    public async Task<mmria.common.metadata.Version_Specification> GetVersionSpecificationByRawIdAsync(string id, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{id}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<mmria.common.metadata.Version_Specification>(response, IgnoreNullSettings);
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveVersionSpecificationDocumentAsync(string id, string json, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{id}";
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    /// <inheritdoc />
    public async Task<get_response_header<mmria.common.metadata.Version_Specification>> GetAllVersionSpecificationHeadersAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/_all_docs?include_docs=true";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<get_response_header<mmria.common.metadata.Version_Specification>>(response, IgnoreNullSettings);
    }

    // ── Attachments ───────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<string> GetCheckCodeAttachmentAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{DefaultMetadataId}/{CheckCodeAttachmentName}";
        return await _couchDbHttpClient.ExecuteAsync("GET", url, null, null, null);
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveCheckCodeAttachmentAsync(string content, DBConfigurationDetail dbConfig)
    {
        string? revision = await FetchRevisionOrNullAsync($"{dbConfig.url}/metadata/{DefaultMetadataId}", dbConfig.user_name, dbConfig.user_value);
        var requestOptions = revision != null ? new CouchDbRequestOptions { IfMatch = revision } : null;
        return await PutTextAttachmentAsync(
            $"{dbConfig.url}/metadata/{DefaultMetadataId}/{CheckCodeAttachmentName}",
            content,
            dbConfig.user_name,
            dbConfig.user_value,
            requestOptions);
    }

    /// <inheritdoc />
    public async Task<string> GetValidatorAttachmentAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{DefaultMetadataId}/{ValidatorAttachmentName}";
        return await _couchDbHttpClient.ExecuteAsync("GET", url, null, null, null);
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveValidatorAttachmentAsync(string content, DBConfigurationDetail dbConfig)
    {
        string? revision = await FetchRevisionOrNullAsync($"{dbConfig.url}/metadata/{DefaultMetadataId}", dbConfig.user_name, dbConfig.user_value);
        var requestOptions = revision != null ? new CouchDbRequestOptions { IfMatch = revision } : null;
        return await PutTextAttachmentAsync(
            $"{dbConfig.url}/metadata/{DefaultMetadataId}/{ValidatorAttachmentName}",
            content,
            dbConfig.user_name,
            dbConfig.user_value,
            requestOptions);
    }

    /// <inheritdoc />
    public async Task<string> GetVersionDocumentAttachmentAsync(string version, string documentName, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/version_specification-{version}/{documentName}";
        return await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveVersionDocumentAttachmentAsync(string id, string documentName, string content, string? rev, DBConfigurationDetail dbConfig)
    {
        var requestOptions = rev != null ? new CouchDbRequestOptions { IfMatch = rev } : null;
        return await PutTextAttachmentAsync(
            $"{dbConfig.url}/metadata/{id}/{documentName}",
            content,
            dbConfig.user_name,
            dbConfig.user_value,
            requestOptions);
    }

    // ── UI Specification ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<get_response_header<mmria.common.metadata.UI_Specification>> GetAllUiSpecificationHeadersAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/_all_docs?include_docs=true";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<get_response_header<mmria.common.metadata.UI_Specification>>(response, IgnoreNullSettings);
    }

    /// <inheritdoc />
    public async Task<mmria.common.metadata.UI_Specification> GetUiSpecificationByIdAsync(string id, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{id}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<mmria.common.metadata.UI_Specification>(response, IgnoreNullSettings);
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveUiSpecificationDocumentAsync(string id, string json, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{id}";
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    /// <inheritdoc />
    public async Task<ExpandoObject> DeleteMetadataDocumentAsync(string id, string rev, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{id}?rev={rev}";
        string response = await _couchDbHttpClient.ExecuteAsync("DELETE", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<ExpandoObject>(response, new ExpandoObjectConverter());
    }

    // ── De-Identification Lists ───────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ExpandoObject> GetDeIdentifiedListAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{DeIdentifiedListId}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<ExpandoObject>(response, new ExpandoObjectConverter());
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveDeIdentifiedListAsync(string json, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{DeIdentifiedListId}";
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    /// <inheritdoc />
    public async Task<ExpandoObject> GetDeIdentifiedExportListAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{DeIdentifiedExportListId}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<ExpandoObject>(response, new ExpandoObjectConverter());
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveDeIdentifiedExportListAsync(string json, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{DeIdentifiedExportListId}";
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    // ── Populate CDC Instance ─────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<mmria.common.metadata.Populate_CDC_Instance> GetPopulateCDCInstanceDocumentAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{PopulateCDCInstanceId}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<mmria.common.metadata.Populate_CDC_Instance>(response);
    }

    /// <inheritdoc />
    public async Task<document_put_response> SavePopulateCDCInstanceDocumentAsync(mmria.common.metadata.Populate_CDC_Instance doc, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{PopulateCDCInstanceId}";
        string json = JsonConvert.SerializeObject(doc, IgnoreNullSettings);
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    // ── Export Standard List ──────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ExpandoObject> GetExportStandardListAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{ExportStandardListId}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<ExpandoObject>(response, new ExpandoObjectConverter());
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveExportStandardListAsync(string json, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{ExportStandardListId}";
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    // ── Substance Mapping ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<mmria.common.metadata.Substance_Mapping> GetSubstanceMappingAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{SubstanceMappingId}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<mmria.common.metadata.Substance_Mapping>(response, IgnoreNullSettings);
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveSubstanceMappingAsync(string json, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{SubstanceMappingId}";
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    // ── Duplicate Multiform List ──────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<string> GetDuplicateMultiFormListAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{DuplicateMultiFormListId}";
        return await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
    }

    // ── Broadcast Message List ────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<mmria.common.metadata.BroadcastMessageList> GetBroadcastMessageListAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{BroadcastMessageListId}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, null, null);
        return JsonConvert.DeserializeObject<mmria.common.metadata.BroadcastMessageList>(response, IgnoreNullSettings)
            ?? new mmria.common.metadata.BroadcastMessageList();
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveBroadcastMessageListAsync(string json, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{BroadcastMessageListId}";
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, json, null, null);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    // ── System Offline Config ─────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<mmria.common.metadata.SystemOfflineConfig?> GetSystemOfflineConfigAsync(DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.Get_Prefix_DB_Url("metadata")}/{SystemOfflineConfigId}";
        try
        {
            string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
            return JsonConvert.DeserializeObject<mmria.common.metadata.SystemOfflineConfig>(response, IgnoreNullSettings);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("(404) Object Not Found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("not_found", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveSystemOfflineConfigAsync(string json, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.Get_Prefix_DB_Url("metadata")}/{SystemOfflineConfigId}";
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    // ── Case Validation Rules ─────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<CaseValidationRuleDocument?> GetCaseValidationRulesAsync(string metadataVersion, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{CaseValidationRulesId}";
        try
        {
            string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
            return JsonConvert.DeserializeObject<CaseValidationRuleDocument>(response, IgnoreNullSettings);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("(404) Object Not Found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("not_found", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveCaseValidationRulesAsync(CaseValidationRuleDocument doc, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{doc._id}";
        string json = JsonConvert.SerializeObject(doc, IgnoreNullSettings);
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    // ── App Document by Raw ID ────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<mmria.common.metadata.app> GetAppDocumentByRawIdAsync(string documentId, DBConfigurationDetail dbConfig)
    {
        string url = $"{dbConfig.url}/metadata/{documentId}/metadata";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<mmria.common.metadata.app>(response, IgnoreNullSettings);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task<string?> FetchRevisionOrNullAsync(string documentUrl, string userName, string userValue)
    {
        try
        {
            string response = await _couchDbHttpClient.ExecuteAsync("GET", documentUrl, null, userName, userValue);
            var result = JsonConvert.DeserializeObject<ExpandoObject>(response, new ExpandoObjectConverter());
            IDictionary<string, object> dict = result;
            if (dict != null && dict.ContainsKey("_rev"))
            {
                return dict["_rev"]?.ToString();
            }

            return null;
        }
        catch (Exception ex)
        {
            if (ex.Message.IndexOf("(404) Object Not Found") > -1)
            {
                return null;
            }

            throw;
        }
    }

    private async Task<document_put_response> PutTextAttachmentAsync(
        string url,
        string content,
        string userName,
        string userValue,
        CouchDbRequestOptions? requestOptions)
    {
        requestOptions ??= new CouchDbRequestOptions();
        if (string.IsNullOrWhiteSpace(requestOptions.UserName) && string.IsNullOrWhiteSpace(requestOptions.Password))
        {
            requestOptions = new CouchDbRequestOptions
            {
                UserName = userName,
                Password = userValue,
                BearerToken = requestOptions.BearerToken,
                AuthSessionValue = requestOptions.AuthSessionValue,
                IfMatch = requestOptions.IfMatch,
                VitalServiceKey = requestOptions.VitalServiceKey,
                SafeHeaders = requestOptions.SafeHeaders,
                TimeoutSeconds = requestOptions.TimeoutSeconds,
                ThrowOnError = requestOptions.ThrowOnError,
                SuppressErrorLogging = requestOptions.SuppressErrorLogging,
                ClientName = requestOptions.ClientName
            };
        }

        string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, content, "text/*", requestOptions);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }
}
