#nullable enable

namespace mmria.common.SharedLibraries.Account.Model;

/// <summary>
/// User login request data
/// </summary>
public class LoginRequest
{
    public string? UserName { get; set; }
    public string? Password { get; set; }
    
    public bool IsValid() => !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password);
}
