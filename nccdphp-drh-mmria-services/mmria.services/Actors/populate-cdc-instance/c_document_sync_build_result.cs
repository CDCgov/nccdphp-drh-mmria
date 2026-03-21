using System.Collections.Generic;

namespace mmria.server.utils;

public sealed class c_document_sync_build_result
{
    public string de_identified_json { get; set; }

    public List<string> report_document_json_list { get; set; } = new();
}
