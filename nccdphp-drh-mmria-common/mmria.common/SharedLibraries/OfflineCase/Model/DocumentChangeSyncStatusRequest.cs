namespace mmria.common.SharedLibraries.OfflineCase.Model;

public class DocumentChangeSyncStatusRequest
{
    public string OfflineSessionId { get; set; } = string.Empty;
    public string _id { get; set; } = string.Empty;//case document ID
    public int SyncState { get; set; } = 0; // 0 = not synced, 1 = synced, 2 = processed, 3 = abandoned, 4= released by admin, 5 = no change, 6 = error
}
