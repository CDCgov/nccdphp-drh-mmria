using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using mmria.common.couchdb;
using mmria.common.metadata;
using mmria.common.model.couchdb.audit;
using Newtonsoft.Json;

namespace mmria.server.util;

internal static class DocumentPayloadCloneHelper
{
    public static OverridableConfiguration CloneOverridableConfiguration(
        OverridableConfiguration request,
        string configurationId,
        string revision,
        DateTime? dateCreated,
        string createdBy,
        string lastUpdatedBy)
    {
        if (request == null || string.IsNullOrWhiteSpace(configurationId))
        {
            return null;
        }

        var result = new OverridableConfiguration
        {
            _id = configurationId,
            _rev = string.IsNullOrWhiteSpace(revision) ? request._rev : revision,
            date_created = dateCreated ?? request.date_created ?? DateTime.UtcNow,
            created_by = NormalizeOptionalString(createdBy) ?? NormalizeOptionalString(request.created_by) ?? "system",
            date_last_updated = DateTime.UtcNow,
            last_updated_by = NormalizeOptionalString(lastUpdatedBy) ?? NormalizeOptionalString(request.last_updated_by) ?? "system",
            boolean_keys = CloneBooleanKeys(request.boolean_keys),
            string_keys = CloneStringKeys(request.string_keys),
            integer_keys = CloneIntegerKeys(request.integer_keys)
        };

        EnsureSharedBuckets(result);
        return result;
    }

    public static app CloneMetadataApp(app request, string currentUserName)
    {
        if (request == null || string.IsNullOrWhiteSpace(request._id))
        {
            return null;
        }

        return new app
        {
            _id = NormalizeOptionalString(request._id),
            _rev = NormalizeOptionalString(request._rev),
            name = NormalizeOptionalString(request.name) ?? "mmria",
            prompt = request.prompt,
            type = "app",
            version = NormalizeOptionalString(request.version),
            date_created = string.IsNullOrWhiteSpace(request.date_created) ? DateTime.UtcNow.ToString("O") : request.date_created,
            created_by = NormalizeOptionalString(request.created_by) ?? NormalizeOptionalString(currentUserName) ?? "system",
            date_last_updated = DateTime.UtcNow.ToString("O"),
            last_updated_by = NormalizeOptionalString(currentUserName) ?? NormalizeOptionalString(request.last_updated_by) ?? "system",
            lookup = CloneNodes(request.lookup),
            _attachments = CloneAttachmentDictionary(request._attachments),
            children = CloneNodes(request.children)
        };
    }

    public static Version_Specification CloneVersionSpecification(Version_Specification request, string currentUserName)
    {
        if (request == null || string.IsNullOrWhiteSpace(request._id))
        {
            return null;
        }

        return new Version_Specification
        {
            _id = NormalizeOptionalString(request._id),
            _rev = NormalizeOptionalString(request._rev),
            data_type = "version-specification",
            date_created = string.IsNullOrWhiteSpace(request.date_created) ? DateTime.UtcNow.ToString("O") : request.date_created,
            created_by = NormalizeOptionalString(request.created_by) ?? NormalizeOptionalString(currentUserName) ?? "system",
            date_last_updated = DateTime.UtcNow.ToString("O"),
            last_updated_by = NormalizeOptionalString(currentUserName) ?? NormalizeOptionalString(request.last_updated_by) ?? "system",
            name = NormalizeOptionalString(request.name),
            publish_status = request.publish_status,
            calculations_js = request.calculations_js,
            metadata = request.metadata,
            metadata_id = NormalizeOptionalString(request.metadata_id),
            metadata_rev = NormalizeOptionalString(request.metadata_rev),
            ui_specification = request.ui_specification,
            ui_specification_id = NormalizeOptionalString(request.ui_specification_id),
            ui_specification_rev = NormalizeOptionalString(request.ui_specification_rev),
            schema = CloneStringDictionary(request.schema),
            definition_set = CloneStringDictionary(request.definition_set),
            path_to_csv_all = CloneCsvInfoDictionary(request.path_to_csv_all),
            path_to_csv_core = CloneCsvInfoDictionary(request.path_to_csv_core),
            _attachments = CloneExpandoObject(request._attachments)
        };
    }

    public static UI_Specification CloneUiSpecification(UI_Specification request, string currentUserName)
    {
        if (request == null || string.IsNullOrWhiteSpace(request._id))
        {
            return null;
        }

        return new UI_Specification
        {
            _id = NormalizeOptionalString(request._id),
            _rev = NormalizeOptionalString(request._rev),
            css = request.css,
            data_type = "ui-specification",
            date_created = string.IsNullOrWhiteSpace(request.date_created) ? DateTime.UtcNow.ToString("O") : request.date_created,
            created_by = NormalizeOptionalString(request.created_by) ?? NormalizeOptionalString(currentUserName) ?? "system",
            date_last_updated = DateTime.UtcNow.ToString("O"),
            last_updated_by = NormalizeOptionalString(currentUserName) ?? NormalizeOptionalString(request.last_updated_by) ?? "system",
            name = NormalizeOptionalString(request.name),
            metadata_id = NormalizeOptionalString(request.metadata_id),
            dimension = CloneDimension(request.dimension),
            form_design = CloneFormDesign(request.form_design)
        };
    }

    public static Audit_Manage_User CloneAuditManageUser(Audit_Manage_User request, Audit_Manage_User existingDocument)
    {
        if (request == null)
        {
            return null;
        }

        var result = new Audit_Manage_User
        {
            _id = "audit-manage-user",
            _rev = NormalizeOptionalString(request._rev) ?? NormalizeOptionalString(existingDocument?._rev),
            doc_type = "Audit_Manage_User",
            is_delete = request.is_delete,
            delete_rev = NormalizeOptionalString(request.delete_rev),
            date_created = existingDocument?.date_created ?? request.date_created ?? DateTimeOffset.UtcNow,
            items = CloneAuditItems(request.items)
        };

        return result;
    }

    public static Add_Attachement CloneAddAttachment(Add_Attachement request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request._id))
        {
            return null;
        }

        return new Add_Attachement
        {
            _id = NormalizeOptionalString(request._id),
            _rev = NormalizeOptionalString(request._rev),
            doc_name = NormalizeOptionalString(request.doc_name),
            document_content = request.document_content
        };
    }

    private static void EnsureSharedBuckets(OverridableConfiguration request)
    {
        if (!request.boolean_keys.ContainsKey("shared"))
        {
            request.boolean_keys["shared"] = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }

        if (!request.string_keys.ContainsKey("shared"))
        {
            request.string_keys["shared"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        if (!request.integer_keys.ContainsKey("shared"))
        {
            request.integer_keys["shared"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static Dictionary<string, Dictionary<string, bool>> CloneBooleanKeys(Dictionary<string, Dictionary<string, bool>> source)
    {
        var result = new Dictionary<string, Dictionary<string, bool>>(StringComparer.OrdinalIgnoreCase);
        if (source == null)
        {
            return result;
        }

        foreach (var outer in source)
        {
            var outerKey = NormalizeOptionalString(outer.Key);
            if (string.IsNullOrWhiteSpace(outerKey))
            {
                continue;
            }

            var inner = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (outer.Value != null)
            {
                foreach (var innerItem in outer.Value)
                {
                    var innerKey = NormalizeOptionalString(innerItem.Key);
                    if (!string.IsNullOrWhiteSpace(innerKey))
                    {
                        inner[innerKey] = innerItem.Value;
                    }
                }
            }

            result[outerKey] = inner;
        }

        return result;
    }

    private static Dictionary<string, Dictionary<string, string>> CloneStringKeys(Dictionary<string, Dictionary<string, string>> source)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        if (source == null)
        {
            return result;
        }

        foreach (var outer in source)
        {
            var outerKey = NormalizeOptionalString(outer.Key);
            if (string.IsNullOrWhiteSpace(outerKey))
            {
                continue;
            }

            var inner = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (outer.Value != null)
            {
                foreach (var innerItem in outer.Value)
                {
                    var innerKey = NormalizeOptionalString(innerItem.Key);
                    if (!string.IsNullOrWhiteSpace(innerKey))
                    {
                        inner[innerKey] = NormalizeOptionalString(innerItem.Value);
                    }
                }
            }

            result[outerKey] = inner;
        }

        return result;
    }

    private static Dictionary<string, Dictionary<string, int>> CloneIntegerKeys(Dictionary<string, Dictionary<string, int>> source)
    {
        var result = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        if (source == null)
        {
            return result;
        }

        foreach (var outer in source)
        {
            var outerKey = NormalizeOptionalString(outer.Key);
            if (string.IsNullOrWhiteSpace(outerKey))
            {
                continue;
            }

            var inner = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (outer.Value != null)
            {
                foreach (var innerItem in outer.Value)
                {
                    var innerKey = NormalizeOptionalString(innerItem.Key);
                    if (!string.IsNullOrWhiteSpace(innerKey))
                    {
                        inner[innerKey] = innerItem.Value;
                    }
                }
            }

            result[outerKey] = inner;
        }

        return result;
    }

    private static Dictionary<string, Attachment_Item> CloneAttachmentDictionary(Dictionary<string, Attachment_Item> source)
    {
        var result = new Dictionary<string, Attachment_Item>(StringComparer.OrdinalIgnoreCase);
        if (source == null)
        {
            return result;
        }

        foreach (var kvp in source)
        {
            var key = NormalizeOptionalString(kvp.Key);
            if (string.IsNullOrWhiteSpace(key) || kvp.Value == null)
            {
                continue;
            }

            result[key] = new Attachment_Item
            {
                content_type = NormalizeOptionalString(kvp.Value.content_type),
                revpos = kvp.Value.revpos,
                digest = NormalizeOptionalString(kvp.Value.digest),
                length = kvp.Value.length,
                stub = kvp.Value.stub
            };
        }

        return result;
    }

    private static node[] CloneNodes(node[] source)
    {
        if (source == null)
        {
            return Array.Empty<node>();
        }

        return source
            .Where(item => item != null)
            .Select(CloneNode)
            .ToArray();
    }

    private static node CloneNode(node source)
    {
        return new node
        {
            prompt = source.prompt,
            name = NormalizeOptionalString(source.name),
            type = NormalizeOptionalString(source.type),
            list_item_data_type = NormalizeOptionalString(source.list_item_data_type),
            data_type = NormalizeOptionalString(source.data_type),
            cardinality = NormalizeOptionalString(source.cardinality),
            values = CloneValueNodes(source.values),
            children = CloneNodes(source.children),
            is_core_summary = source.is_core_summary,
            is_required = source.is_required,
            is_read_only = source.is_read_only,
            is_hidden = source.is_hidden,
            is_multiselect = source.is_multiselect,
            is_save_value_display_description = source.is_save_value_display_description,
            list_display_size = source.list_display_size,
            control_style = NormalizeOptionalString(source.control_style),
            default_value = source.default_value,
            pre_fill = NormalizeOptionalString(source.pre_fill),
            regex_pattern = source.regex_pattern,
            decimal_precision = NormalizeOptionalString(source.decimal_precision),
            x_axis = NormalizeOptionalString(source.x_axis),
            x_label = source.x_label,
            x_type = NormalizeOptionalString(source.x_type),
            y_axis = NormalizeOptionalString(source.y_axis),
            y_label = source.y_label,
            y_type = NormalizeOptionalString(source.y_type),
            x_start = NormalizeOptionalString(source.x_start),
            path_reference = NormalizeOptionalString(source.path_reference),
            mirror_reference = NormalizeOptionalString(source.mirror_reference),
            pre_populate_reference = NormalizeOptionalString(source.pre_populate_reference),
            description = source.description,
            data_summary_report_description = source.data_summary_report_description,
            validation_description = source.validation_description,
            validation = CloneExpandoObject(source.validation),
            onfocus = CloneExpandoObject(source.onfocus),
            onchange = CloneExpandoObject(source.onchange),
            onblur = CloneExpandoObject(source.onblur),
            onclick = CloneExpandoObject(source.onclick),
            tags = CloneStringArray(source.tags),
            sass_export_name = NormalizeOptionalString(source.sass_export_name),
            max_value = NormalizeOptionalString(source.max_value),
            min_value = NormalizeOptionalString(source.min_value),
            max_length = NormalizeOptionalString(source.max_length),
            grid_template = source.grid_template,
            grid_template_areas = source.grid_template_areas,
            grid_gap = NormalizeOptionalString(source.grid_gap),
            grid_auto_flow = NormalizeOptionalString(source.grid_auto_flow),
            grid_row = NormalizeOptionalString(source.grid_row),
            grid_column = NormalizeOptionalString(source.grid_column),
            grid_area = NormalizeOptionalString(source.grid_area),
            other_specify_list = NormalizeOptionalString(source.other_specify_list),
            disable_on_selected_item_list = NormalizeOptionalString(source.disable_on_selected_item_list),
            parent_list = NormalizeOptionalString(source.parent_list),
            top = NormalizeOptionalString(source.top),
            left = NormalizeOptionalString(source.left),
            width = NormalizeOptionalString(source.width),
            height = NormalizeOptionalString(source.height),
            padding = NormalizeOptionalString(source.padding),
            text_align = NormalizeOptionalString(source.text_align),
            is_not_selectable = source.is_not_selectable,
            sort_order = NormalizeOptionalString(source.sort_order),
            committee_description = source.committee_description,
            is_display_field_length = source.is_display_field_length
        };
    }

    private static value_node[] CloneValueNodes(value_node[] source)
    {
        if (source == null)
        {
            return Array.Empty<value_node>();
        }

        return source
            .Where(item => item != null)
            .Select(item => new value_node
            {
                display = item.display,
                description = item.description,
                value = NormalizeOptionalString(item.value),
                parent_value = NormalizeOptionalString(item.parent_value),
                is_not_selectable = item.is_not_selectable,
                is_mutually_exclusive = item.is_mutually_exclusive
            })
            .ToArray();
    }

    private static ExpandoObject CloneExpandoObject(ExpandoObject source)
    {
        if (source == null)
        {
            return null;
        }

        return JsonConvert.DeserializeObject<ExpandoObject>(
            JsonConvert.SerializeObject(source));
    }

    private static string[] CloneStringArray(string[] source)
    {
        return source?
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToArray() ?? Array.Empty<string>();
    }

    private static Dictionary<string, string> CloneStringDictionary(Dictionary<string, string> source)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (source == null)
        {
            return result;
        }

        foreach (var kvp in source)
        {
            var key = NormalizeOptionalString(kvp.Key);
            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = kvp.Value;
            }
        }

        return result;
    }

    private static Dictionary<string, csv_info> CloneCsvInfoDictionary(Dictionary<string, csv_info> source)
    {
        var result = new Dictionary<string, csv_info>(StringComparer.OrdinalIgnoreCase);
        if (source == null)
        {
            return result;
        }

        foreach (var kvp in source)
        {
            var key = NormalizeOptionalString(kvp.Key);
            if (string.IsNullOrWhiteSpace(key) || kvp.Value == null)
            {
                continue;
            }

            result[key] = new csv_info
            {
                file_name = NormalizeOptionalString(kvp.Value.file_name),
                field_name = NormalizeOptionalString(kvp.Value.field_name)
            };
        }

        return result;
    }

    private static Dimension CloneDimension(Dimension source)
    {
        if (source == null)
        {
            return null;
        }

        return new Dimension
        {
            style = source.style,
            x = source.x,
            y = source.y,
            width = source.width,
            height = source.height
        };
    }

    private static Dimension_Object CloneDimensionObject(Dimension_Object source)
    {
        if (source == null)
        {
            return null;
        }

        return new Dimension_Object
        {
            prompt = CloneDimension(source.prompt),
            control = CloneDimension(source.control)
        };
    }

    private static Dictionary<string, Dimension_Object> CloneFormDesign(Dictionary<string, Dimension_Object> source)
    {
        var result = new Dictionary<string, Dimension_Object>(StringComparer.OrdinalIgnoreCase);
        if (source == null)
        {
            return result;
        }

        foreach (var kvp in source)
        {
            var key = NormalizeOptionalString(kvp.Key);
            if (string.IsNullOrWhiteSpace(key) || kvp.Value == null)
            {
                continue;
            }

            result[key] = CloneDimensionObject(kvp.Value);
        }

        return result;
    }

    private static List<Audit_Manage_User.Audit_Manage_User_Item> CloneAuditItems(List<Audit_Manage_User.Audit_Manage_User_Item> source)
    {
        var result = new List<Audit_Manage_User.Audit_Manage_User_Item>();
        if (source == null)
        {
            return result;
        }

        foreach (var item in source.Where(item => item != null))
        {
            result.Add(new Audit_Manage_User.Audit_Manage_User_Item
            {
                date_created = item.date_created ?? DateTimeOffset.UtcNow,
                created_by = NormalizeOptionalString(item.created_by),
                action = NormalizeOptionalString(item.action),
                element_id = item.element_id,
                user_id = NormalizeOptionalString(item.user_id),
                field = NormalizeOptionalString(item.field),
                field_path = NormalizeOptionalString(item.field_path),
                old_value = item.old_value,
                new_value = item.new_value,
                note = item.note
            });
        }

        return result;
    }

    private static string NormalizeOptionalString(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
