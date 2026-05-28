#nullable enable

using System;

namespace mmria.common.SharedLibraries.Account.Model;

/// <summary>
/// Final login operation result - returned to controller for routing decisions
/// </summary>
public class LoginResult
{
    public bool IsSuccessful { get; set; }
    public bool IsLockedOut { get; set; }
    public bool IsUnauthorized { get; set; }
    public DateTime? LockoutGracePeriodDate { get; set; }
    public SessionInfo? SessionInfo { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Successful login result
    /// </summary>
    public static LoginResult Success(SessionInfo sessionInfo) =>
        new()
        {
            IsSuccessful = true,
            SessionInfo = sessionInfo
        };

    /// <summary>
    /// Account is locked due to failed login attempts
    /// </summary>
    public static LoginResult LockedOut(DateTime gracePeriodDate) =>
        new()
        {
            IsLockedOut = true,
            LockoutGracePeriodDate = gracePeriodDate,
            ErrorMessage = "Account is locked due to multiple failed login attempts."
        };

    /// <summary>
    /// Invalid credentials or insufficient permissions
    /// </summary>
    public static LoginResult Unauthorized(string? errorMessage = null) =>
        new()
        {
            IsUnauthorized = true,
            ErrorMessage = errorMessage ?? "Username or password is incorrect."
        };

    /// <summary>
    /// Generic failure
    /// </summary>
    public static LoginResult Failure(string errorMessage) =>
        new()
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage
        };
}
