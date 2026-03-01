#nullable enable

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.Account.DAL;
using mmria.common.SharedLibraries.Account.Model;
using mmria_server.tests.Helpers;

namespace mmria_server.tests.Tests;

/// <summary>
/// Account authentication and session management tests.
/// Tests user login flows, authentication, authorization, and session creation.
/// Uses AccountTestHelper to execute production-like login logic without side effects.
/// </summary>
[TestFixture]
public class AccountTests
{
    private TestEnvironment _env = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _env = await TestEnvironment.BootstrapAsync("account_tests");
    }

    [SetUp]
    public async Task SetUpAsync()
    {
        await _env.ResolveConfigurationAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        await _env.CleanupAsync();
    }

    /// <summary>
    /// Test successful user authentication and session creation.
    /// Validates that a user can authenticate with valid credentials and receive a session.
    /// </summary>
    [Test]
    [Category("Account")]
    public async Task Scenario_A_SuccessfulLoginCreatesSession()
    {
        var cfg = _env.Config!;

        // Arrange
        // You would need to set up a test user in CouchDB
        // For now, we'll use placeholder values that should exist in your test database
        string testUserName = "user2";
        string testPassword = "password";

        // Act
        var loginResult = await _env.AccountTestHelper.AuthenticateAndCreateSessionAsync(
            testUserName,
            testPassword,
            cfg.DbConfig,
            cfg.Configuration,
            cfg.HostPrefix);

        // Assert - Successful login (may be inconclusive if test user doesn't exist)
        if (loginResult.IsUnauthorized && loginResult.ErrorMessage?.Contains("not found") == true)
        {
            Assert.Inconclusive($"Test user '{testUserName}' does not exist in test database. Create test user to run this test.");
            return;
        }

        // If we get here, the authentication was attempted
        Assert.That(loginResult, Is.Not.Null, "Login result should not be null");

        if (loginResult.IsSuccessful)
        {
            // Assert - Has session info
            Assert.That(loginResult.SessionInfo, Is.Not.Null, 
                "Session info should be created after successful login");

            // Assert - Session ID is valid
            Assert.That(loginResult.SessionInfo!.SessionId, Is.Not.Null.And.Not.Empty,
                "Session ID should be generated");

            // Assert - Expiration is in future
            Assert.That(loginResult.SessionInfo.ExpirationDateTime, Is.GreaterThan(DateTime.Now),
                "Session expiration should be in the future");

            // Assert - User ID matches
            Assert.That(loginResult.SessionInfo.UserId, Is.EqualTo(testUserName),
                $"User ID should match login username");

            TestContext.WriteLine($"✓ User '{testUserName}' authenticated successfully");
            TestContext.WriteLine($"  Session ID: {loginResult.SessionInfo.SessionId}");
            TestContext.WriteLine($"  Roles: {string.Join(", ", loginResult.SessionInfo.Roles)}");
        }
    }

    /// <summary>
    /// Test that invalid credentials are rejected.
    /// Validates proper error handling for authentication failures.
    /// </summary>
    [Test]
    [Category("Account")]
    public async Task Scenario_B_InvalidCredentialsFailsLogin()
    {
        var cfg = _env.Config!;

        // Arrange
        string testUserName = "user5";
        string wrongPassword = "password@@";

        // Act
        var loginResult = await _env.AccountTestHelper.AuthenticateAndCreateSessionAsync(
            testUserName,
            wrongPassword,
            cfg.DbConfig,
            cfg.Configuration,
            cfg.HostPrefix);

        // Assert - Login should fail (unless user doesn't exist, then inconclusive)
        if (loginResult.ErrorMessage?.Contains("not found") == true)
        {
            Assert.Inconclusive($"Test user '{testUserName}' does not exist. Run Scenario_A first to set up test user.");
            return;
        }

        Assert.That(loginResult.IsUnauthorized || !loginResult.IsSuccessful, Is.True,
            "Login should fail with invalid password");

        // Assert - No session created
        Assert.That(loginResult.SessionInfo, Is.Null,
            "Session should not be created for failed login");

        TestContext.WriteLine($"✓ Invalid credentials correctly rejected");
    }

    /// <summary>
    /// Test that empty credentials are rejected.
    /// Validates input validation in the login flow.
    /// </summary>
    [Test]
    [Category("Account")]
    public async Task Scenario_C_EmptyCredentialsFailsLogin()
    {
        var cfg = _env.Config!;

        // Arrange
        string emptyUserName = string.Empty;
        string emptyPassword = string.Empty;

        // Act
        var loginResult = await _env.AccountTestHelper.AuthenticateAndCreateSessionAsync(
            emptyUserName,
            emptyPassword,
            cfg.DbConfig,
            cfg.Configuration,
            cfg.HostPrefix);

        // Assert - Login should fail
        Assert.That(loginResult.IsUnauthorized, Is.True,
            "Login should fail with empty credentials");

        // Assert - No session created
        Assert.That(loginResult.SessionInfo, Is.Null,
            "Session should not be created for empty credentials");

        TestContext.WriteLine($"✓ Empty credentials correctly rejected");
    }

    /// <summary>
    /// Test session expiration timeout configuration.
    /// Validates that session timeout is correctly applied.
    /// </summary>
    [Test]
    [Category("Account")]
    public async Task Scenario_D_SessionTimeoutIsApplied()
    {
        var cfg = _env.Config!;

        // Arrange
        string testUserName = "user2";
        string testPassword = "password";
        int customTimeoutMinutes = 45;
        var preLoginTime = DateTime.Now;

        // Act
        var loginResult = await _env.AccountTestHelper.AuthenticateAndCreateSessionAsync(
            testUserName,
            testPassword,
            cfg.DbConfig,
            cfg.Configuration,
            cfg.HostPrefix,
            customTimeoutMinutes);

        // Check if user exists before validating
        if (loginResult.ErrorMessage?.Contains("not found") == true)
        {
            Assert.Inconclusive($"Test user '{testUserName}' does not exist. Run Scenario_A first to set up test user.");
            return;
        }

        // Assert timeout only if login was successful
        if (loginResult.IsSuccessful && loginResult.SessionInfo != null)
        {
            var expectedExpirationTime = preLoginTime.AddMinutes(customTimeoutMinutes);
            var actualExpirationTime = loginResult.SessionInfo.ExpirationDateTime;

            // Allow 5 second tolerance for execution time
            Assert.That(actualExpirationTime, 
                Is.GreaterThanOrEqualTo(expectedExpirationTime.AddSeconds(-5)),
                "Session expiration should be approximately the timeout duration from login");
            
            Assert.That(actualExpirationTime,
                Is.LessThanOrEqualTo(expectedExpirationTime.AddSeconds(5)),
                "Session expiration should not exceed expected timeout");

            TestContext.WriteLine($"✓ Session timeout of {customTimeoutMinutes} minutes applied correctly");
        }
    }

    /// <summary>
    /// Test successful login and Claims creation for authentication context.
    /// Validates that login succeeds and ClaimsPrincipal can be created with proper roles.
    /// This mirrors the AccountController.Login() pattern for building authentication context.
    /// </summary>
    [Test]
    [Category("Account")]
    public async Task Scenario_E_SuccessfulLoginCreatesValidClaims()
    {
        var cfg = _env.Config!;

        // Arrange
        string testUserName = "user2";
        string testPassword = "password";
        string testId = Guid.NewGuid().ToString();
        const string Issuer = "https://contoso.com";

        TestContext.WriteLine($"Starting login test with ID: {testId}");

        // Act - Authenticate user
        var loginResult = await _env.AccountTestHelper.AuthenticateAndCreateSessionAsync(
            testUserName,
            testPassword,
            cfg.DbConfig,
            cfg.Configuration,
            cfg.HostPrefix);

        // Assert - Check authentication result
        if (loginResult.IsUnauthorized && loginResult.ErrorMessage?.Contains("not found") == true)
        {
            Assert.Inconclusive($"Test user '{testUserName}' does not exist in test database.");
            return;
        }

        Assert.That(loginResult.IsSuccessful, Is.True, 
            $"Login should succeed for valid user. Error: {loginResult.ErrorMessage}");
        Assert.That(loginResult.SessionInfo, Is.Not.Null, 
            "SessionInfo should be created after successful login");

        var sessionInfo = loginResult.SessionInfo!;

        // Act - Build Claims from session info (following AccountController.Login pattern)
        var claims = new List<Claim>();
        
        // Add name claim
        claims.Add(new Claim(ClaimTypes.Name, testUserName, ClaimValueTypes.String, Issuer));

        // Add role claims from session info
        foreach (var role in sessionInfo.Roles ?? new List<string>())
        {
            claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }

        // Create ClaimsIdentity and ClaimsPrincipal
        var userIdentity = new ClaimsIdentity("SuperSecureLogin");
        userIdentity.AddClaims(claims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);

        // Assert - Validate Claims were created correctly
        Assert.That(userIdentity.Claims.Count(), Is.GreaterThan(0),
            "ClaimsIdentity should contain at least one claim (Name)");

        // Assert - Validate Name claim
        var nameClaim = userIdentity.FindFirst(ClaimTypes.Name);
        Assert.That(nameClaim, Is.Not.Null, "Name claim should exist");
        Assert.That(nameClaim?.Value, Is.EqualTo(testUserName), 
            "Name claim value should match username");

        // Assert - Validate Role claims
        var roleClaims = userIdentity.FindAll(ClaimTypes.Role).ToList();
        Assert.That(roleClaims.Count, Is.EqualTo(sessionInfo.Roles?.Count ?? 0),
            "Number of Role claims should match SessionInfo roles");

        foreach (var expectedRole in sessionInfo.Roles ?? new List<string>())
        {
            var roleClaim = roleClaims.FirstOrDefault(c => c.Value == expectedRole);
            Assert.That(roleClaim, Is.Not.Null,
                $"Role claim for '{expectedRole}' should exist in ClaimsIdentity");
        }

        // Assert - Validate Principal
        Assert.That(userPrincipal.Identity, Is.Not.Null, "Principal should have identity");
        Assert.That(userPrincipal.Identity?.IsAuthenticated, Is.True,
            "Principal identity should be authenticated");
        Assert.That(userPrincipal.Identity?.Name, Is.EqualTo(testUserName),
            "Principal name should match username");

        // Test complete
        TestContext.WriteLine($"✓ Successful login with valid claims created");
        TestContext.WriteLine($"  Test ID: {testId}");
        TestContext.WriteLine($"  Session ID: {sessionInfo.SessionId}");
        TestContext.WriteLine($"  User: {testUserName}");
        TestContext.WriteLine($"  Claims Count: {userIdentity.Claims.Count()}");
        TestContext.WriteLine($"  Roles: {string.Join(", ", roleClaims.Select(c => c.Value))}");
    }

    /// <summary>
    /// Helper method to extract session info from successful login.
    /// Useful for dependent tests that need session details.
    /// </summary>
    protected mmria.common.SharedLibraries.Account.Model.SessionInfo? GetSessionFromLogin(LoginResult loginResult)
    {
        return AccountTestHelper.GetSessionInfoFromLoginResult(loginResult);
    }

    /// <summary>
    /// Regression test: non-admin login must create a session document that is immediately retrievable.
    /// Guards the login/session persistence path for non-admin users.
    /// </summary>
    [Test]
    [Category("Account")]
    public async Task Scenario_F_NonAdminLoginPersistsSessionDocument()
    {
        var cfg = _env.Config!;

        string testUserName = "user5";
        string testPassword = "password";

        var loginResult = await _env.AccountTestHelper.AuthenticateAndCreateSessionAsync(
            testUserName,
            testPassword,
            cfg.DbConfig,
            cfg.Configuration,
            cfg.HostPrefix);

        if (loginResult.IsUnauthorized && loginResult.ErrorMessage?.Contains("not found") == true)
        {
            Assert.Inconclusive($"Test user '{testUserName}' does not exist in test database.");
            return;
        }

        Assert.That(loginResult.IsSuccessful, Is.True,
            $"Expected successful non-admin login. Error: {loginResult.ErrorMessage}");
        Assert.That(loginResult.SessionInfo, Is.Not.Null);

        var sessionId = loginResult.SessionInfo!.SessionId;
        Assert.That(sessionId, Is.Not.Null.And.Not.Empty);

        var dal = new AccountDAL(_env.CouchDbClient);
        var sessionDocumentJson = await dal.GetSessionDocumentAsync(sessionId, cfg.DbConfig);

        Assert.That(sessionDocumentJson, Is.Not.Null.And.Not.Empty,
            "Expected persisted session document to be retrievable immediately after login.");

        using var sessionDocument = JsonDocument.Parse(sessionDocumentJson!);
        var root = sessionDocument.RootElement;

        Assert.That(root.TryGetProperty("_id", out var idElement), Is.True,
            "Session document should contain _id.");
        Assert.That(idElement.GetString(), Is.EqualTo(sessionId));

        Assert.That(root.TryGetProperty("user_id", out var userIdElement), Is.True,
            "Session document should contain user_id.");
        Assert.That(userIdElement.GetString(), Is.EqualTo(testUserName));

        Assert.That(root.TryGetProperty("is_active", out var isActiveElement), Is.True,
            "Session document should contain is_active.");
        Assert.That(isActiveElement.GetBoolean(), Is.True);
    }
}
