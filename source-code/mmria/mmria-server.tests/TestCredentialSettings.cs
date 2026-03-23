#nullable enable

namespace mmria_server.tests;

public sealed class SharedTestUsers
{
    public string PrimaryUserName { get; init; } = string.Empty;
    public string SecondaryUserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string InvalidPasswordForPrimaryUser { get; init; } = string.Empty;
}

public sealed class SampleCredentialSettings
{
    public string TestHarnessUserName { get; init; } = string.Empty;
    public string TestHarnessPassword { get; init; } = string.Empty;
    public string StubDbUserName { get; init; } = string.Empty;
    public string StubDbPassword { get; init; } = string.Empty;
    public string FormUrlEncodedPassword { get; init; } = string.Empty;
    public string UserCreationPassword { get; init; } = string.Empty;
    public string AlternateUserCreationPassword { get; init; } = string.Empty;
}

public sealed class TestCredentialSettings
{
    public SharedTestUsers SharedUsers { get; init; } = new();
    public SampleCredentialSettings SampleCredentials { get; init; } = new();
}
