#nullable enable

using System;
using System.Collections.Generic;

namespace mmria.common.SharedLibraries.Account.Model;

/// <summary>
/// Session creation result - contains session ID, expiration, and metadata
/// </summary>
public class SessionInfo
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime ExpirationDateTime { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SessionEventId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }

    public static SessionInfo Success(string sessionId, DateTime expiration, string userId, string eventId, List<string> roles) =>
        new()
        {
            SessionId = sessionId,
            ExpirationDateTime = expiration,
            UserId = userId,
            SessionEventId = eventId,
            Roles = roles,
            IsSuccessful = true
        };

    public static SessionInfo Failure(string errorMessage) =>
        new()
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage
        };
}
