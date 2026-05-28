namespace mmria.common.SharedLibraries.OfflineCase.Model;

public class LightweightDocumentChange
{
    public string DocumentId { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
    public int SyncState { get; set; } = 0; // 0 = not synced, 1 = synced, 2 = abandoned, 3 = error
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}
