using System.Collections.Generic;

namespace mmria.common.SharedLibraries.OfflineCase.Model;

public class DocumentChange
{
    public string DocumentId { get; set; } = string.Empty;
    public mmria.case_version.v260120.mmria_case OriginalDocument { get; set; }
    public mmria.case_version.v260120.mmria_case ModifiedDocument { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
    public int SyncState { get; set; } = 0; // 0 = not synced, 1 = synced, 2 = abandoned, 3 = error
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public List<mmria.common.model.couchdb.Change_Stack_Item> ChangeStackItems { get; set; } = new List<mmria.common.model.couchdb.Change_Stack_Item>();
}
