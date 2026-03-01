using System.Collections.Generic;
using System;

namespace mmria.common.SharedLibraries.OfflineCase.Model;

public class OfflineCaseResponse
{
    public string _id { get; set; } = string.Empty;
    public string _rev { get; set; } = string.Empty;
    public List<string> offline_ids { get; set; } = new List<string>();
    public string offline_key { get; set; } = string.Empty;    
    public int offline_state { get; set; } = 0;
    public List<DocumentChange> case_documents { get; set; } = new List<DocumentChange>();
    public string created_by { get; set; } = string.Empty;
    public DateTime date_created { get; set; }
    public string last_updated_by { get; set; } = string.Empty;
    public DateTime date_last_updated { get; set; }
}
