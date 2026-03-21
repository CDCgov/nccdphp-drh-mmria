using System.Collections.Generic;

namespace mmria.server.utils;

public sealed class c_document_sync_rebuild_context
{
    public mmria.common.metadata.app metadata { get; init; }

    public HashSet<string> de_identified_set { get; init; } = new(System.StringComparer.OrdinalIgnoreCase);

    public string case_template_json { get; init; }
}

public sealed class c_document_sync_build_result
{
    public string de_identified_json { get; set; }

    public List<string> report_document_json_list { get; set; } = new();
}
