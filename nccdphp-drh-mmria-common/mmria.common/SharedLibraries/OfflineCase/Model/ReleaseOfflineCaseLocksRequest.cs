using System.Collections.Generic;

namespace mmria.common.SharedLibraries.OfflineCase.Model;

public class ReleaseOfflineCaseLocksRequest
{
    public string OfflineSessionId { get; set; } = string.Empty;
    public List<string> CaseIds { get; set; } = new List<string>();
}
