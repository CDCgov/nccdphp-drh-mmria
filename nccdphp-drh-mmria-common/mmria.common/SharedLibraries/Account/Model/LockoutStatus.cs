#nullable enable

using System;

namespace mmria.common.SharedLibraries.Account.Model;

/// <summary>
/// Account lockout status after failed login attempts
/// </summary>
public class LockoutStatus
{
    public bool IsLockedOut { get; set; }
    public DateTime GracePeriodDate { get; set; }
    public int FailedAttemptCount { get; set; }
    public int ThresholdBeforeLockout { get; set; }

    public static LockoutStatus NotLockedOut() => new() { IsLockedOut = false };
    
    public static LockoutStatus LockedOut(DateTime gracePeriodDate, int attempts, int threshold) =>
        new()
        {
            IsLockedOut = true,
            GracePeriodDate = gracePeriodDate,
            FailedAttemptCount = attempts,
            ThresholdBeforeLockout = threshold
        };
}
