using System.Collections.Generic;

namespace mmria.common.SharedLibraries.OfflineCase.Model;

public sealed class RecoverSoftLocksRequest
{
    public string OfflineSessionId { get; set; } = string.Empty;
    public string tab_id { get; set; } = string.Empty;
    public List<string> CaseIds { get; set; } = new List<string>();
}
