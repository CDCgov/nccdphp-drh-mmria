using System.Collections.Generic;

namespace mmria.common.SharedLibraries.OfflineCase.Model;

public class OfflineCaseRequest
{
    public List<string> offline_ids { get; set; } = new List<string>();
    public string offline_key { get; set; } = string.Empty;
    public string device_id { get; set; } = string.Empty;
    public string browser_id { get; set; } = string.Empty;
    public string tab_id { get; set; } = string.Empty;
}
