using System.Collections.Generic;

namespace mmria.common.SharedLibraries.OfflineCase.Model;

public class SaveOfflineCasesRequest
{
    public string OfflineSessionId { get; set; } = string.Empty;
    public List<DocumentChange> CaseDocuments { get; set; } = new List<DocumentChange>();
}
