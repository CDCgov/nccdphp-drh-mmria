using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.metadata;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.MetadataVersion.DAL;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.MetadataVersion.Manager;

public sealed class MetadataVersionManager
{
    private const string DefaultMetadataId = "2016-06-12T13:49:24.759Z";
    private const string DeIdentifiedListId = "de-identified-list";
    private const string DefaultUiSpecificationId = "default-ui-specification";
    private const string DefaultVersionSpecificationId = "default_version_specification";
    private const string CheckCodeAttachmentName = "mmria-check-code.js";
    private const string ValidatorAttachmentName = "validator.js";

    private readonly MetadataVersionDAL _dal;

    public MetadataVersionManager(MetadataVersionDAL dal)
    {
        _dal = dal;
    }

    public async Task<ExpandoObject> GetMetadataAsync(DBConfigurationDetail db_config)
    {
        return await _dal.GetExpandoDocumentAsync(
            $"{db_config.url}/metadata/{DefaultMetadataId}",
            null,
            null);
    }

    public async Task<ExpandoObject> GetMetadataAsync(string id, DBConfigurationDetail db_config)
    {
        return await _dal.GetExpandoDocumentAsync(
            $"{db_config.url}/metadata/{id}",
            null,
            null);
    }

    public async Task<mmria.common.metadata.app> GetAppMetadataAsync(string id, DBConfigurationDetail db_config)
    {
        return await _dal.GetDocumentAsync<mmria.common.metadata.app>(
            $"{db_config.url}/metadata/version_specification-{id}/metadata",
            null,
            null);
    }

    public async Task<document_put_response> SaveMetadataAsync(app metadata, DBConfigurationDetail db_config)
    {
        string object_string = JsonConvert.SerializeObject(metadata, CreateSerializerSettings());
        return await _dal.PutJsonAsync($"{db_config.url}/metadata/{metadata._id}", object_string, db_config.user_name, db_config.user_value);
    }

    public async Task<string> GetCheckCodeAsync(DBConfigurationDetail db_config)
    {
        return await _dal.GetStringAsync(
            $"{db_config.url}/metadata/{DefaultMetadataId}/{CheckCodeAttachmentName}",
            null,
            null);
    }

    public async Task<document_put_response> SaveCheckCodeAsync(string check_code_json, DBConfigurationDetail db_config)
    {
        string revision = null;
        try
        {
            revision = await _dal.GetRevisionAsync($"{db_config.url}/metadata/{DefaultMetadataId}", db_config.user_name, db_config.user_value);
        }
        catch (Exception ex)
        {
            if (!(ex.Message.IndexOf("(404) Object Not Found") > -1))
            {
                throw;
            }
        }

        mmria.common.getset.CouchDbRequestOptions requestOptions = null;
        if (!string.IsNullOrWhiteSpace(revision))
        {
            requestOptions = new mmria.common.getset.CouchDbRequestOptions
            {
                IfMatch = revision
            };
        }

        return await _dal.PutTextAsync(
            $"{db_config.url}/metadata/{DefaultMetadataId}/{CheckCodeAttachmentName}",
            check_code_json,
            db_config.user_name,
            db_config.user_value,
            requestOptions);
    }

    public async Task<document_put_response> SaveMetadataVersionSpecificationAsync(Version_Specification versionSpecification, DBConfigurationDetail db_config)
    {
        if (!IsValidMetadataVersionSpecification(versionSpecification))
        {
            return null;
        }

        string json_string = JsonConvert.SerializeObject(versionSpecification, CreateSerializerSettings());
        return await _dal.PutJsonAsync($"{db_config.url}/metadata/{versionSpecification._id}", json_string, db_config.user_name, db_config.user_value);
    }

    public async Task<List<Version_Specification>> ListVersionSpecificationsAsync(DBConfigurationDetail db_config)
    {
        var response = await _dal.GetAllDocsAsync<Version_Specification>(
            $"{db_config.url}/metadata/_all_docs?include_docs=true",
            db_config.user_name,
            db_config.user_value,
            CreateSerializerSettings());

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
        return await _dal.GetDocumentAsync<Version_Specification>(
            $"{db_config.url}/metadata/version_specification-{version_specification_id}",
            db_config.user_name,
            db_config.user_value);
    }

    public async Task<string> GetValidatorAsync(DBConfigurationDetail db_config)
    {
        return await _dal.GetStringAsync(
            $"{db_config.url}/metadata/{DefaultMetadataId}/{ValidatorAttachmentName}",
            null,
            null);
    }

    public async Task<string> GetVersionDocumentAsync(string version_specification_id, string document_name, DBConfigurationDetail db_config)
    {
        return await _dal.GetStringAsync(
            $"{db_config.url}/metadata/version_specification-{version_specification_id}/{document_name}",
            null,
            null);
    }

    public async Task<document_put_response> SaveVersionSpecificationAsync(Version_Specification versionSpecification, DBConfigurationDetail db_config)
    {
        string id_val = versionSpecification._id;
        bool save_document = false;

        if (!string.IsNullOrWhiteSpace(versionSpecification._rev))
        {
            try
            {
                var check_result = await _dal.GetDocumentAsync<Version_Specification>(
                    $"{db_config.url}/metadata/{id_val}",
                    db_config.user_name,
                    db_config.user_value);

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

        return await _dal.PutJsonAsync($"{db_config.url}/metadata/{id_val}", object_string, db_config.user_name, db_config.user_value);
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
                var check_result = await _dal.GetDocumentAsync<Version_Specification>(
                    $"{db_config.url}/metadata/{add_attachement._id}",
                    db_config.user_name,
                    db_config.user_value);

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

        return await _dal.PutTextAsync(
            $"{db_config.url}/metadata/{add_attachement._id}/{add_attachement.doc_name}",
            add_attachement.document_content,
            db_config.user_name,
            db_config.user_value,
            new mmria.common.getset.CouchDbRequestOptions
            {
                IfMatch = add_attachement._rev
            });
    }

    public async Task<List<UI_Specification>> ListUiSpecificationsAsync(DBConfigurationDetail db_config)
    {
        var response = await _dal.GetAllDocsAsync<UI_Specification>(
            $"{db_config.url}/metadata/_all_docs?include_docs=true",
            db_config.user_name,
            db_config.user_value,
            CreateSerializerSettings());

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
        return await _dal.GetDocumentAsync<UI_Specification>(
            $"{db_config.url}/metadata/{id}",
            db_config.user_name,
            db_config.user_value,
            CreateSerializerSettings());
    }

    public async Task<document_put_response> SaveUiSpecificationAsync(UI_Specification ui_specification, DBConfigurationDetail db_config)
    {
        if (!IsValidUiSpecification(ui_specification))
        {
            return null;
        }

        string ui_specification_json = JsonConvert.SerializeObject(ui_specification, CreateSerializerSettings());
        return await _dal.PutJsonAsync($"{db_config.url}/metadata/{ui_specification._id}", ui_specification_json, db_config.user_name, db_config.user_value);
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

        return await _dal.DeleteDocumentAsync($"{db_config.url}/metadata/{id}?rev={rev}", db_config.user_name, db_config.user_value);
    }

    public async Task<document_put_response> SaveValidatorAsync(string validator_js_text, DBConfigurationDetail db_config)
    {
        string revision = null;
        try
        {
            revision = await _dal.GetRevisionAsync($"{db_config.url}/metadata/{DefaultMetadataId}", db_config.user_name, db_config.user_value);
        }
        catch (Exception ex)
        {
            if (!(ex.Message.IndexOf("(404) Object Not Found") > -1))
            {
                throw;
            }
        }

        mmria.common.getset.CouchDbRequestOptions requestOptions = null;
        if (!string.IsNullOrWhiteSpace(revision))
        {
            requestOptions = new mmria.common.getset.CouchDbRequestOptions
            {
                IfMatch = revision
            };
        }

        return await _dal.PutTextAsync(
            $"{db_config.url}/metadata/{DefaultMetadataId}/{ValidatorAttachmentName}",
            validator_js_text,
            db_config.user_name,
            db_config.user_value,
            requestOptions);
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
