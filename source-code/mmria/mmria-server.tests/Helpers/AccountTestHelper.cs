#nullable enable

using System;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.SharedLibraries.Account.DAL;
using mmria.common.SharedLibraries.Account.Manager;
using mmria.common.SharedLibraries.Account.Model;

namespace mmria_server.tests.Helpers;

/// <summary>
/// Test helper for Account/Login operations.
/// Provides a reusable function to create user sessions in tests and test authentication flows.
/// This helper extracts the core login business logic from AccountController,
/// excluding redirects, actor operations, cookie creation, and SAMS checks.
/// </summary>
public class AccountTestHelper
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    /// <summary>
    /// Initialize AccountTestHelper with a CouchDbHttpClient instance.
    /// </summary>
    public AccountTestHelper(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient ?? throw new ArgumentNullException(nameof(couchDbHttpClient));
    }

    /// <summary>
    /// Authenticate a user and create a session following production login flow.
    /// This executes the same business logic as AccountController.Login() but returns
    /// the LoginResult without side effects (no redirects, actors, or cookies).
    /// 
    /// Useful for:
    /// - Creating user sessions in test setup
    /// - Testing authentication and authorization
    /// - Testing dependent functionality that requires authenticated sessions
    /// </summary>
    public async Task<LoginResult> AuthenticateAndCreateSessionAsync(
        string userName,
        string password,
        DBConfigurationDetail dbConfig,
        OverridableConfiguration configuration,
        string hostPrefix,
        int sessionIdleTimeoutMinutes = 30)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return LoginResult.Unauthorized("Username and password are required.");
        }

        try
        {
            // Initialize AccountDAL and Manager using provided CouchDbHttpClient
            var accountDAL = new AccountDAL(_couchDbHttpClient);
            var accountManager = new AccountManager(accountDAL);

            // Create login request
            var loginRequest = new LoginRequest
            {
                UserName = userName,
                Password = password
            };

            // Delegate to AccountManager for complete login workflow
            // This covers:
            // 1. Lockout status check
            // 2. User authentication with CouchDB
            // 3. App prefix access validation
            // 4. Session creation
            var loginResult = await accountManager.ProcessLoginAsync(
                loginRequest,
                dbConfig,
                configuration,
                hostPrefix,
                sessionIdleTimeoutMinutes);

            return loginResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error for user {userName}: {ex}");
            return LoginResult.Failure($"Login failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Extract session information from a successful login result.
    /// Useful for accessing session details after authentication in tests.
    /// </summary>
    public static SessionInfo? GetSessionInfoFromLoginResult(LoginResult loginResult)
    {
        if (loginResult == null || !loginResult.IsSuccessful)
        {
            return null;
        }

        return loginResult.SessionInfo;
    }
}
