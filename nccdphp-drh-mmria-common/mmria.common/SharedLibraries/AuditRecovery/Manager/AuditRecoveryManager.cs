using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.metadata;
using mmria.common.model.couchdb;
using mmria.common.model.couchdb.audit;
using mmria.common.SharedLibraries.AuditRecovery.DAL;
using mmria.common.SharedLibraries.AuditRecovery.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.AuditRecovery.Manager;

public sealed class AuditRecoveryManager
{
    private readonly AuditRecoveryDAL _dal;

    public AuditRecoveryManager(AuditRecoveryDAL dal)
    {
        _dal = dal;
    }

    public async Task<AuditRecoveryViewData> GetAuditViewDataAsync(
        string caseId,
        int page,
        string user,
        string search_text,
        bool showAll,
        DBConfigurationDetail db_config,
        CancellationToken cancellationToken)
    {
        var case_view_response = await _dal.GetCaseViewResponseAsync(caseId, db_config);
        cancellationToken.ThrowIfCancellationRequested();

        case_view_sortable_item case_view_item = case_view_response.rows.Where(i => i.id == caseId).FirstOrDefault().value;
        var (request_string, post_data) = GetFindUrl(db_config, caseId);
        var view_response = await _dal.FindChangeStacksAsync(request_string, post_data, db_config);
        cancellationToken.ThrowIfCancellationRequested();

        List<Change_Stack> result = new();
        foreach (var item in view_response.docs)
        {
            for (var i = 0; i < item.items.Count; i++)
            {
                item.items[i].temp_index = i;
            }

            item.items.Sort(new ChangeStackItemDescendingDate());

            if (showAll)
            {
                result.Add(DebounceDateTimeField(item));
            }
            else if (item.items.Count > 0 && item.case_id == caseId)
            {
                result.Add(DebounceDateTimeField(item));
            }
        }

        const int page_size = 50;
        result.Sort(new ChangeStackDescendingDate());

        return new AuditRecoveryViewData
        {
            id = caseId,
            user = user,
            search_text = search_text,
            showAll = showAll,
            cv = case_view_item,
            ls = page == -1 ? result : result.Skip((page - 1) * page_size).Take(page_size).ToList(),
            page_size = page_size,
            page = page,
            total = result.Count
        };
    }

    public async Task<AuditRecoveryDetailData> GetAuditDetailDataAsync(
        string caseId,
        string changeId,
        int changeItem,
        DBConfigurationDetail db_config,
        CancellationToken cancellationToken)
    {
        var case_view_response = await _dal.GetCaseViewResponseAsync(caseId, db_config);
        cancellationToken.ThrowIfCancellationRequested();

        case_view_sortable_item case_view_item = case_view_response.rows.Where(i => i.id == caseId).FirstOrDefault().value;
        var cs = await _dal.GetChangeStackAsync(changeId, db_config);
        cancellationToken.ThrowIfCancellationRequested();

        for (var i = 0; i < cs.items.Count; i++)
        {
            cs.items[i].temp_index = i;
        }

        var metadata = await _dal.GetMetadataAsync(cs.metadata_version, db_config);
        var lookup = GetLookUp(metadata);
        var node = GetMetadataNode(metadata, cs.items[changeItem].dictionary_path.Trim().TrimStart('/'));

        if (node == null)
        {
            return new AuditRecoveryDetailData
            {
                id = caseId,
                change_id = changeId,
                cv = case_view_item,
                cs = cs,
                change_item = changeItem,
                MetadataNode = metadata.AsNode()
            };
        }

        var converted = ConvertNode(node, lookup);
        return new AuditRecoveryDetailData
        {
            id = caseId,
            change_id = changeId,
            cv = case_view_item,
            cs = cs,
            change_item = changeItem,
            MetadataNode = node,
            value_to_display = converted.value_to_display,
            display_to_value = converted.display_to_value
        };
    }

    public async Task<Audit_Manage_User> GetAuditDocumentAsync(DBConfigurationDetail db_config)
    {
        return await _dal.GetAuditManageUserAsync(db_config);
    }

    public async Task<document_put_response> SaveAuditDocumentAsync(Audit_Manage_User auditDocument, DBConfigurationDetail db_config)
    {
        return await _dal.SaveAuditManageUserAsync(auditDocument, db_config);
    }

    public async Task<System.Dynamic.ExpandoObject> GetCaseRevisionAsync(string caseId, string revisionId, DBConfigurationDetail db_config)
    {
        return await _dal.GetCaseRevisionAsync(caseId, revisionId, db_config);
    }

    private static (string url, string post) GetFindUrl(DBConfigurationDetail db_config, string caseId)
    {
        var selector_struc = new AuditSelector();
        selector_struc.selector = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        selector_struc.limit = 1_000_000;
        selector_struc.selector.Add("case_id", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        selector_struc.selector["case_id"].Add("$eq", caseId);
        selector_struc.use_index = "case-id-date-last-updated-index";

        string selector_struc_string = JsonConvert.SerializeObject(selector_struc, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        string result = $"{db_config.url}/{db_config.prefix}audit/_find";
        return (result, selector_struc_string);
    }

    private static Dictionary<string, value_node[]> GetLookUp(app metadata)
    {
        var result = new Dictionary<string, value_node[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in metadata.lookup)
        {
            result.Add("lookup/" + node.name, node.values);
        }

        return result;
    }

    private static node GetMetadataNode(app metadata, string path)
    {
        node result = null;
        node current = null;
        string[] splitPath = path.Split("/");

        for (int i = 0; i < splitPath.Length; i++)
        {
            string current_name = splitPath[i];
            if (i == 0)
            {
                foreach (var child in metadata.children)
                {
                    if (child.name.Equals(current_name, StringComparison.OrdinalIgnoreCase))
                    {
                        current = child;
                        break;
                    }
                }
            }
            else
            {
                if (current.children != null)
                {
                    foreach (var child2 in current.children)
                    {
                        if (child2.name.Equals(current_name, StringComparison.OrdinalIgnoreCase))
                        {
                            current = child2;
                            break;
                        }
                    }
                }
                else
                {
                    return result;
                }

                if (i == splitPath.Length - 1)
                {
                    result = current;
                }
            }
        }

        return result;
    }

    private static (Dictionary<string, string> display_to_value, Dictionary<string, string> value_to_display) ConvertNode(node value, Dictionary<string, value_node[]> lookup)
    {
        Dictionary<string, string> display_to_value = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> value_to_display = new(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(value.path_reference))
        {
            var key = value.path_reference;
            if (lookup.ContainsKey(key))
            {
                value.values = lookup[key];
            }
        }

        if (value.values != null)
        {
            foreach (var value_item in value.values)
            {
                var v = value_item.value;
                var display = value_item.display;

                if (!value_to_display.ContainsKey(v))
                {
                    value_to_display.Add(v, display);
                }

                if (!display_to_value.ContainsKey(display))
                {
                    display_to_value.Add(display, v);
                }
            }
        }

        return (display_to_value, value_to_display);
    }

    private static Change_Stack DebounceDateTimeField(Change_Stack value)
    {
        var result = new Change_Stack()
        {
            _id = value._id,
            _rev = value._rev,
            case_id = value.case_id,
            case_rev = value.case_rev,
            user_name = value.user_name,
            note = value.note,
            metadata_version = value.metadata_version,
            date_created = value.date_created
        };

        string found_path = "";
        int found_index = -1;
        int last_index = -1;
        int target_index = -1;
        for (var subitem_index = 0; subitem_index < value.items.Count; subitem_index++)
        {
            var subitem = value.items[subitem_index];

            if (string.Equals(subitem.metadata_type, "DATETIME", StringComparison.OrdinalIgnoreCase))
            {
                if (subitem.dictionary_path == found_path)
                {
                    last_index = subitem_index;
                    continue;
                }
                else
                {
                    found_path = subitem.dictionary_path;
                    found_index = subitem_index;
                    target_index = result.items.Count;
                    result.items.Add(subitem);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(found_path))
                {
                    if (last_index > -1)
                    {
                        result.items[target_index].old_value = value.items[last_index].old_value;
                    }

                    result.items[target_index].new_value = value.items[found_index].new_value;
                    found_path = "";
                    found_index = -1;
                    last_index = -1;
                    target_index = -1;
                }

                result.items.Add(subitem);
            }
        }

        if (!string.IsNullOrWhiteSpace(found_path) && last_index > -1)
        {
            result.items[target_index].old_value = value.items[last_index].old_value;
            result.items[target_index].new_value = value.items[found_index].new_value;
        }

        return result;
    }

    public sealed class ChangeStackDescendingDate : IComparer<Change_Stack>
    {
        public int Compare(Change_Stack x, Change_Stack y)
        {
            return y.date_created.Value.CompareTo(x.date_created.Value);
        }
    }

    public sealed class ChangeStackItemDescendingDate : IComparer<Change_Stack_Item>
    {
        public int Compare(Change_Stack_Item x, Change_Stack_Item y)
        {
            return y.date_created.Value.CompareTo(x.date_created.Value);
        }
    }
}
