namespace mmria.common.SharedLibraries.OfflineCase.Model;

public class UpdateOfflineStateRequest
{
    public string OfflineSessionId { get; set; } = string.Empty;
    public int OfflineState { get; set; } = 0; // 0 = initial/not started, 1 = in progress, 2 = completed, 3 = error/failed
}
