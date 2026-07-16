#nullable enable

using System.Dynamic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.CaseValidation.Model;

namespace mmria.common.SharedLibraries.MetadataVersion;

/// <summary>
/// Repository interface for all in-scope <c>metadata</c> CouchDB operations.
/// <see cref="DAL.MetadataVersionDAL"/> is the sole implementation.
/// A SQL migration requires only a new implementation of this interface — no caller changes needed.
/// </summary>
public interface IMetadataRepository
{
    // ── Version Specification — App Document ──────────────────────────────────

    /// <summary>GET metadata/version_specification-{version}/metadata (app field-tree document).</summary>
    Task<mmria.common.metadata.app> GetAppDocumentAsync(string version, DBConfigurationDetail dbConfig);

    // ── Default Metadata Document (legacy root schema) ────────────────────────

    /// <summary>GET metadata/2016-06-12T13:49:24.759Z (legacy default form document).</summary>
    Task<ExpandoObject> GetDefaultMetadataDocumentAsync(DBConfigurationDetail dbConfig);

    /// <summary>GET metadata/{id} — returns the raw document as an ExpandoObject.</summary>
    Task<ExpandoObject> GetMetadataDocumentByIdAsync(string id, DBConfigurationDetail dbConfig);

    /// <summary>PUT metadata/{metadata._id} (save the app document).</summary>
    Task<document_put_response> SaveMetadataDocumentAsync(mmria.common.metadata.app metadata, DBConfigurationDetail dbConfig);

    // ── Version Specification Envelope ────────────────────────────────────────

    /// <summary>GET metadata/version_specification-{version} (envelope document — prepends the prefix internally).</summary>
    Task<mmria.common.metadata.Version_Specification> GetVersionSpecificationEnvelopeAsync(string version, DBConfigurationDetail dbConfig);

    /// <summary>
    /// GET metadata/{id} — returns the document as <see cref="mmria.common.metadata.Version_Specification"/>
    /// using the raw document <c>_id</c> (already includes the <c>version_specification-</c> prefix).
    /// </summary>
    Task<mmria.common.metadata.Version_Specification> GetVersionSpecificationByRawIdAsync(string id, DBConfigurationDetail dbConfig);

    /// <summary>PUT metadata/{id} — saves a serialized version specification; <paramref name="id"/> is the raw document _id.</summary>
    Task<document_put_response> SaveVersionSpecificationDocumentAsync(string id, string json, DBConfigurationDetail dbConfig);

    /// <summary>GET metadata/_all_docs?include_docs=true typed as <see cref="mmria.common.metadata.Version_Specification"/>.</summary>
    Task<get_response_header<mmria.common.metadata.Version_Specification>> GetAllVersionSpecificationHeadersAsync(DBConfigurationDetail dbConfig);

    // ── Attachments ───────────────────────────────────────────────────────────

    /// <summary>GET metadata/{DefaultMetadataId}/mmria-check-code.js.</summary>
    Task<string> GetCheckCodeAttachmentAsync(DBConfigurationDetail dbConfig);

    /// <summary>Fetch current revision, then PUT metadata/{DefaultMetadataId}/mmria-check-code.js.</summary>
    Task<document_put_response> SaveCheckCodeAttachmentAsync(string content, DBConfigurationDetail dbConfig);

    /// <summary>GET metadata/{DefaultMetadataId}/validator.js.</summary>
    Task<string> GetValidatorAttachmentAsync(DBConfigurationDetail dbConfig);

    /// <summary>Fetch current revision, then PUT metadata/{DefaultMetadataId}/validator.js.</summary>
    Task<document_put_response> SaveValidatorAttachmentAsync(string content, DBConfigurationDetail dbConfig);

    /// <summary>GET metadata/version_specification-{version}/{documentName}.</summary>
    Task<string> GetVersionDocumentAttachmentAsync(string version, string documentName, DBConfigurationDetail dbConfig);

    /// <summary>PUT metadata/{id}/{documentName} with If-Match:{rev}.</summary>
    Task<document_put_response> SaveVersionDocumentAttachmentAsync(string id, string documentName, string content, string? rev, DBConfigurationDetail dbConfig);

    // ── UI Specification ──────────────────────────────────────────────────────

    /// <summary>GET metadata/_all_docs?include_docs=true typed as <see cref="mmria.common.metadata.UI_Specification"/>.</summary>
    Task<get_response_header<mmria.common.metadata.UI_Specification>> GetAllUiSpecificationHeadersAsync(DBConfigurationDetail dbConfig);

    /// <summary>GET metadata/{id} — returns the document as <see cref="mmria.common.metadata.UI_Specification"/>.</summary>
    Task<mmria.common.metadata.UI_Specification> GetUiSpecificationByIdAsync(string id, DBConfigurationDetail dbConfig);

    /// <summary>PUT metadata/{id} — saves a serialized UI specification document.</summary>
    Task<document_put_response> SaveUiSpecificationDocumentAsync(string id, string json, DBConfigurationDetail dbConfig);

    /// <summary>DELETE metadata/{id}?rev={rev}.</summary>
    Task<ExpandoObject> DeleteMetadataDocumentAsync(string id, string rev, DBConfigurationDetail dbConfig);

    // ── De-Identification Lists ───────────────────────────────────────────────

    /// <summary>GET metadata/de-identified-list.</summary>
    Task<ExpandoObject> GetDeIdentifiedListAsync(DBConfigurationDetail dbConfig);

    /// <summary>PUT metadata/de-identified-list.</summary>
    Task<document_put_response> SaveDeIdentifiedListAsync(string json, DBConfigurationDetail dbConfig);

    /// <summary>GET metadata/de-identified-export-list.</summary>
    Task<ExpandoObject> GetDeIdentifiedExportListAsync(DBConfigurationDetail dbConfig);

    /// <summary>PUT metadata/de-identified-export-list.</summary>
    Task<document_put_response> SaveDeIdentifiedExportListAsync(string json, DBConfigurationDetail dbConfig);

    // ── Populate CDC Instance ─────────────────────────────────────────────────

    /// <summary>GET metadata/populate-cdc-instance.</summary>
    Task<mmria.common.metadata.Populate_CDC_Instance> GetPopulateCDCInstanceDocumentAsync(DBConfigurationDetail dbConfig);

    /// <summary>PUT metadata/populate-cdc-instance.</summary>
    Task<document_put_response> SavePopulateCDCInstanceDocumentAsync(mmria.common.metadata.Populate_CDC_Instance doc, DBConfigurationDetail dbConfig);

    // ── Export Standard List ──────────────────────────────────────────────────

    /// <summary>GET metadata/export-standard-list — returns raw ExpandoObject (type is local to services).</summary>
    Task<ExpandoObject> GetExportStandardListAsync(DBConfigurationDetail dbConfig);

    /// <summary>PUT metadata/export-standard-list.</summary>
    Task<document_put_response> SaveExportStandardListAsync(string json, DBConfigurationDetail dbConfig);

    // ── Substance Mapping ─────────────────────────────────────────────────────

    /// <summary>GET metadata/substance-mapping.</summary>
    Task<mmria.common.metadata.Substance_Mapping> GetSubstanceMappingAsync(DBConfigurationDetail dbConfig);

    /// <summary>PUT metadata/substance-mapping.</summary>
    Task<document_put_response> SaveSubstanceMappingAsync(string json, DBConfigurationDetail dbConfig);

    // ── Duplicate Multiform List ──────────────────────────────────────────────

    /// <summary>
    /// GET metadata/duplicate-multiform-list — returns raw JSON string.
    /// Callers deserialize to their local <c>DuplicateMultiformResult</c> type (not in mmria.common).
    /// </summary>
    Task<string> GetDuplicateMultiFormListAsync(DBConfigurationDetail dbConfig);

    // ── Case Validation Rules ─────────────────────────────────────────────────

    /// <summary>GET metadata/case-validation-rules. Returns null if the document does not exist (404).</summary>
    Task<CaseValidationRuleDocument?> GetCaseValidationRulesAsync(string metadataVersion, DBConfigurationDetail dbConfig);

    /// <summary>PUT metadata/{doc._id} for the case-validation-rules document.</summary>
    Task<document_put_response> SaveCaseValidationRulesAsync(CaseValidationRuleDocument doc, DBConfigurationDetail dbConfig);
}
