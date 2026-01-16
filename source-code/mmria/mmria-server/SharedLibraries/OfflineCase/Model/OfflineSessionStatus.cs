namespace mmria.server.SharedLibraries.Model.OfflineCase;

/// <summary>
/// Result object containing offline session status information.
/// </summary>
public class OfflineSessionStatus
{
    /// <summary>
    /// Indicates whether the user has an active offline session requiring attention.
    /// </summary>
    public bool HasActiveSession { get; set; }

    /// <summary>
    /// The offline_state value from the session (0 = active, 1 = partially synced, 2 = completed).
    /// Null if no active session exists.
    /// </summary>
    public int? OfflineState { get; set; }

    /// <summary>
    /// Full session data including offline_ids, offline_key, and case_documents.
    /// Null if no active session exists.
    /// </summary>
    public OfflineCaseResponse SessionData { get; set; }
}

/// <summary>
/// Result object containing offline session status information (lightweight version).
/// </summary>
public class OfflineSessionStatusLight
{
    /// <summary>
    /// Indicates whether the user has an active offline session requiring attention.
    /// </summary>
    public bool HasActiveSession { get; set; }

    /// <summary>
    /// The offline_state value from the session (0 = active, 1 = partially synced, 2 = completed).
    /// Null if no active session exists.
    /// </summary>
    public int? OfflineState { get; set; }

    /// <summary>
    /// Full session data including offline_ids, offline_key, and case_documents.
    /// Null if no active session exists.
    /// </summary>
    public LightweightOfflineCaseResponse SessionData { get; set; }
}
