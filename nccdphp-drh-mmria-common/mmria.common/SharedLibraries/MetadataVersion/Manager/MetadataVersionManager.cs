using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.metadata;
using mmria.common.model.couchdb;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.MetadataVersion.Manager;

public sealed class MetadataVersionManager
{
    private const string DefaultMetadataId = "2016-06-12T13:49:24.759Z";
    private const string DeIdentifiedListId = "de-identified-list";
    private const string DefaultUiSpecificationId = "default-ui-specification";
    private const string DefaultVersionSpecificationId = "default_version_specification";

    private readonly IMetadataRepository _dal;

    public MetadataVersionManager(IMetadataRepository dal)
    {
        _dal = dal;
    }

    public async Task<ExpandoObject> GetMetadataAsync(DBConfigurationDetail db_config)
    {
        return await _dal.GetDefaultMetadataDocumentAsync(db_config);
    }

    public async Task<ExpandoObject> GetMetadataAsync(string id, DBConfigurationDetail db_config)
    {
        return await _dal.GetMetadataDocumentByIdAsync(id, db_config);
    }

    public async Task<mmria.common.metadata.app> GetAppMetadataAsync(string id, DBConfigurationDetail db_config)
    {
        return await _dal.GetAppDocumentAsync(id, db_config);
    }

    public async Task<document_put_response> SaveMetadataAsync(app metadata, DBConfigurationDetail db_config)
    {
        return await _dal.SaveMetadataDocumentAsync(metadata, db_config);
    }

    public async Task<string> GetCheckCodeAsync(DBConfigurationDetail db_config)
    {
        return await _dal.GetCheckCodeAttachmentAsync(db_config);
    }

    public async Task<document_put_response> SaveCheckCodeAsync(string check_code_json, DBConfigurationDetail db_config)
    {
        return await _dal.SaveCheckCodeAttachmentAsync(check_code_json, db_config);
    }

    public async Task<document_put_response> SaveMetadataVersionSpecificationAsync(Version_Specification versionSpecification, DBConfigurationDetail db_config)
    {
        if (!IsValidMetadataVersionSpecification(versionSpecification))
        {
            return null;
        }

        string json_string = JsonConvert.SerializeObject(versionSpecification, CreateSerializerSettings());
        return await _dal.SaveVersionSpecificationDocumentAsync(versionSpecification._id, json_string, db_config);
    }

    public async Task<List<Version_Specification>> ListVersionSpecificationsAsync(DBConfigurationDetail db_config)
    {
        var response = await _dal.GetAllVersionSpecificationHeadersAsync(db_config);

        var result = new List<Version_Specification>();
        foreach (var row in response.rows)
        {
            var version_specification = row.doc;
            if
            (
                version_specification.data_type == null ||
                version_specification.data_type != "version-specification" ||
                version_specification._id == DefaultMetadataId ||
                version_specification._id == DeIdentifiedListId
            )
            {
                continue;
            }

            result.Add(row.doc);
        }

        return result;
    }

    public async Task<Version_Specification> GetVersionSpecificationMetadataAsync(string version_specification_id, DBConfigurationDetail db_config)
    {
        return await _dal.GetVersionSpecificationEnvelopeAsync(version_specification_id, db_config);
    }

    public async Task<string> GetValidatorAsync(DBConfigurationDetail db_config)
    {
        return await _dal.GetValidatorAttachmentAsync(db_config);
    }

    public async Task<string> GetVersionDocumentAsync(string version_specification_id, string document_name, DBConfigurationDetail db_config)
    {
        return await _dal.GetVersionDocumentAttachmentAsync(version_specification_id, document_name, db_config);
    }

    public async Task<document_put_response> SaveVersionSpecificationAsync(Version_Specification versionSpecification, DBConfigurationDetail db_config)
    {
        string id_val = versionSpecification._id;
        bool save_document = false;

        if (!string.IsNullOrWhiteSpace(versionSpecification._rev))
        {
            try
            {
                var check_result = await _dal.GetVersionSpecificationByRawIdAsync(id_val, db_config);

                if
                (
                    !string.IsNullOrWhiteSpace(check_result.data_type) &&
                    check_result.data_type == "version-specification"
                )
                {
                    if (string.IsNullOrWhiteSpace(check_result.data_type))
                    {
                        save_document = true;
                    }
                    else if (check_result.publish_status != publish_status_enum.final)
                    {
                        save_document = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        if (!save_document)
        {
            return new document_put_response();
        }

        string object_string = JsonConvert.SerializeObject(versionSpecification, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        return await _dal.SaveVersionSpecificationDocumentAsync(id_val, object_string, db_config);
    }

    public async Task<document_put_response> SaveVersionAttachmentAsync(Add_Attachement add_attachement, DBConfigurationDetail db_config, bool requireEditableVersion)
    {
        if (add_attachement == null || IsAlwaysProtectedVersionAttachmentId(add_attachement._id))
        {
            return null;
        }

        if (requireEditableVersion)
        {
            if (add_attachement._id == "default_ui_specification")
            {
                return null;
            }

            bool save_document = false;

            try
            {
                var check_result = await _dal.GetVersionSpecificationByRawIdAsync(add_attachement._id, db_config);

                if
                (
                    !string.IsNullOrWhiteSpace(check_result.data_type) &&
                    check_result.data_type == "version-specification"
                )
                {
                    if (string.IsNullOrWhiteSpace(check_result.data_type))
                    {
                        save_document = true;
                    }
                    else if (check_result.publish_status != publish_status_enum.final)
                    {
                        save_document = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            if (!save_document)
            {
                return new document_put_response();
            }
        }

        return await _dal.SaveVersionDocumentAttachmentAsync(
            add_attachement._id,
            add_attachement.doc_name,
            add_attachement.document_content,
            add_attachement._rev,
            db_config);
    }

    public async Task<List<UI_Specification>> ListUiSpecificationsAsync(DBConfigurationDetail db_config)
    {
        var response = await _dal.GetAllUiSpecificationHeadersAsync(db_config);

        var result = new List<UI_Specification>();
        foreach (var row in response.rows)
        {
            var ui_specification = row.doc;
            if
            (
                ui_specification.data_type == null ||
                ui_specification.data_type != "ui-specification" ||
                ui_specification._id == DefaultMetadataId ||
                ui_specification._id == DeIdentifiedListId
            )
            {
                continue;
            }

            result.Add(row.doc);
        }

        return result;
    }

    public async Task<UI_Specification> GetUiSpecificationAsync(string id, DBConfigurationDetail db_config)
    {
        return await _dal.GetUiSpecificationByIdAsync(id, db_config);
    }

    public async Task<document_put_response> SaveUiSpecificationAsync(UI_Specification ui_specification, DBConfigurationDetail db_config)
    {
        if (!IsValidUiSpecification(ui_specification))
        {
            return null;
        }

        string ui_specification_json = JsonConvert.SerializeObject(ui_specification, CreateSerializerSettings());
        return await _dal.SaveUiSpecificationDocumentAsync(ui_specification._id, ui_specification_json, db_config);
    }

    public async Task<ExpandoObject> DeleteUiSpecificationAsync(string id, string rev, DBConfigurationDetail db_config)
    {
        if
        (
            string.IsNullOrWhiteSpace(id) ||
            string.IsNullOrWhiteSpace(rev) ||
            id == DefaultUiSpecificationId ||
            id == DefaultMetadataId ||
            id == DeIdentifiedListId
        )
        {
            return null;
        }

        return await _dal.DeleteMetadataDocumentAsync(id, rev, db_config);
    }

    public async Task<document_put_response> SaveValidatorAsync(string validator_js_text, DBConfigurationDetail db_config)
    {
        return await _dal.SaveValidatorAttachmentAsync(validator_js_text, db_config);
    }

    private static JsonSerializerSettings CreateSerializerSettings()
    {
        return new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };
    }

    private static bool IsValidMetadataVersionSpecification(Version_Specification versionSpecification)
    {
        return versionSpecification.data_type != null &&
            versionSpecification.data_type == "version-specification" &&
            versionSpecification._id != DefaultMetadataId &&
            versionSpecification._id != DeIdentifiedListId;
    }

    private static bool IsValidUiSpecification(UI_Specification ui_specification)
    {
        return ui_specification.data_type != null &&
            ui_specification.data_type == "ui-specification" &&
            ui_specification._id != DefaultMetadataId &&
            ui_specification._id != DeIdentifiedListId;
    }

    private static bool IsAlwaysProtectedVersionAttachmentId(string id)
    {
        return id == DefaultVersionSpecificationId ||
            id == DefaultMetadataId ||
            id == DeIdentifiedListId;
    }
}