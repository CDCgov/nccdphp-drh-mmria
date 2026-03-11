using System.Collections.Generic;
using mmria.common.metadata;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.AuditRecovery.Model;

public sealed class AuditRecoveryViewData
{
    public string id { get; set; }
    public string user { get; set; } = "all";
    public string search_text { get; set; } = "all";
    public bool showAll { get; set; }
    public case_view_sortable_item cv { get; set; }
    public List<Change_Stack> ls { get; set; } = new();
    public int page_size { get; set; }
    public int page { get; set; }
    public int total { get; set; }
}

public sealed class AuditRecoveryDetailData
{
    public string id { get; set; }
    public string change_id { get; set; }
    public int change_item { get; set; }
    public bool showAll { get; set; }
    public case_view_sortable_item cv { get; set; }
    public Change_Stack cs { get; set; }
    public node MetadataNode { get; set; }
    public Dictionary<string, string> value_to_display { get; set; }
    public Dictionary<string, string> display_to_value { get; set; }
}

public struct ChangeStackResult
{
    public Change_Stack[] docs;
}

public struct AuditSelector
{
    public Dictionary<string, Dictionary<string, string>> selector;
    public string[] fields;
    public string use_index;
    public int limit;
}
