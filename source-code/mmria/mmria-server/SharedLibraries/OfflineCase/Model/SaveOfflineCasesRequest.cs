using System.Collections.Generic;

namespace mmria.server.SharedLibraries.Model.OfflineCase;

public class SaveOfflineCasesRequest
{
    public string OfflineSessionId { get; set; } = string.Empty;
    public List<DocumentChange> CaseDocuments { get; set; } = new List<DocumentChange>();
}