#nullable enable

using System.Collections.Generic;

namespace mmria.common.SharedLibraries.Account.Model;

/// <summary>
/// Authorization check result - contains user roles and permission status
/// </summary>
public class AuthorizationStatus
{
    public bool IsAuthenticated { get; set; }
    public bool IsAppPrefixOk { get; set; }
    public string? UserName { get; set; }
    public List<string> UserRoles { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public static AuthorizationStatus Success(string userName, List<string> roles, bool appPrefixOk = true) =>
        new()
        {
            IsAuthenticated = true,
            IsAppPrefixOk = appPrefixOk,
            UserName = userName,
            UserRoles = roles
        };

    public static AuthorizationStatus Failure(string errorMessage) =>
        new()
        {
            IsAuthenticated = false,
            IsAppPrefixOk = false,
            ErrorMessage = errorMessage
        };
}
