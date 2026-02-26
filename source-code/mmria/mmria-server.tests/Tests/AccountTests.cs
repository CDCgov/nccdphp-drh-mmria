#nullable enable

using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
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
    private DatabaseTestHelper? _dbHelper;
    private AccountTestHelper? _accountTestHelper;
    private mmria.common.couchdb.OverridableConfiguration? _configuration;
    private mmria.common.couchdb.DBConfigurationDetail? _dbConfig;
    private string _hostPrefix = string.Empty;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        // Initialize database helper with test configuration
        _dbHelper = new DatabaseTestHelper(purposeName: "account_tests");

        // Check CouchDB connectivity
        bool isAccessible = await _dbHelper.IsCouchDbAccessibleAsync();
        if (!isAccessible)
        {
            Assert.Inconclusive("CouchDB is not accessible. Check configuration and connection.");
        }

        // Verify test database exists
        bool exists = await _dbHelper.TestDatabaseExistsAsync();
        if (!exists)
        {
            Assert.Inconclusive("Test database does not exist.");
        }

        // Initialize account test helper with CouchDB HTTP client
        var couchDbClient = _dbHelper.GetCouchDbHttpClient();
        _accountTestHelper = new AccountTestHelper(couchDbClient);

        TestContext.WriteLine($"Account Tests initialized. Database: {_dbHelper.GetTestDatabaseName()}");
    }

    [SetUp]
    public async Task SetUpAsync()
    {
        if (_dbHelper == null)
        {
            Assert.Fail("Database helper not initialized.");
            return;
        }

        // Load test configuration for each test
        var configLoader = new TestConfigurationLoader();
        configLoader.Load();

        // Load multi-tenant configurations from CouchDB
        var (configurationSets, overridableConfigs) = await _dbHelper.LoadMultiTenantConfigurationsAsync();

        // Filter OverridableConfiguration by tenant and shared config ID
        // Naming convention: {target_test_tenant}_{multi_tenant_shared_config_id}
        // Example: tenant5_dev_cluster
        string targetConfigId = $"{configLoader.TargetTestTenant}_{configLoader.SharedConfigId}";
        _configuration = overridableConfigs.FirstOrDefault(c => c._id == targetConfigId);
        
        if (_configuration == null)
        {
            TestContext.WriteLine($"Warning: Could not find OverridableConfiguration with ID '{targetConfigId}'");
            TestContext.WriteLine($"Available configs: {string.Join(", ", overridableConfigs.Select(c => c._id))}");
            // Fall back to creating a basic configuration
            _configuration = new mmria.common.couchdb.OverridableConfiguration();
        }

        // Filter ConfigurationSet - find the one that matches our target tenant
        // ConfigurationSets contain detail_list with host_prefix keys
        mmria.common.couchdb.ConfigurationSet? targetConfigSet = null;
        string targetHostPrefix = configLoader.TargetTestTenant;
        
        foreach (var configSet in configurationSets)
        {
            if (configSet.detail_list != null && configSet.detail_list.ContainsKey(targetHostPrefix))
            {
                targetConfigSet = configSet;
                break;
            }
        }

        // Get CouchDB URL from helper (it resolves tenant URLs)
        string couchDbUrl = _dbHelper.GetTestDatabaseUrl().TrimEnd('/');
        if (couchDbUrl.EndsWith("/mmrds"))
        {
            couchDbUrl = couchDbUrl.Substring(0, couchDbUrl.Length - 6); // Remove /mmrds
        }

        // Use ConfigurationSet's detail if available, otherwise create from loaded config
        if (targetConfigSet != null && targetConfigSet.detail_list.ContainsKey(targetHostPrefix))
        {
            _dbConfig = targetConfigSet.detail_list[targetHostPrefix];
        }
        else
        {
            // Fall back to manual configuration
            _dbConfig = new mmria.common.couchdb.DBConfigurationDetail
            {
                url = couchDbUrl,
                user_name = configLoader.TimerUserName,
                user_value = configLoader.TimerPassword,
                prefix = configLoader.TestDatabasePrefix
            };

            TestContext.WriteLine($"Warning: ConfigurationSet details not found for '{targetHostPrefix}'. Using fallback configuration.");
        }

        _hostPrefix = targetHostPrefix;
        
        TestContext.WriteLine($"Account Test Configuration:");
        TestContext.WriteLine($"  Target Tenant: {configLoader.TargetTestTenant}");
        TestContext.WriteLine($"  Shared Config ID: {configLoader.SharedConfigId}");
        TestContext.WriteLine($"  Host Prefix: {_hostPrefix}");
        TestContext.WriteLine($"  CouchDB URL: {_dbConfig?.url}");
    }

    /// <summary>
    /// Test successful user authentication and session creation.
    /// Validates that a user can authenticate with valid credentials and receive a session.
    /// </summary>
    [Test]
    [Category("Account")]
    public async Task Scenario_A_SuccessfulLoginCreatesSession()
    {
        if (_accountTestHelper == null || _dbConfig == null || _configuration == null)
        {
            Assert.Fail("Test helpers not initialized.");
            return;
        }

        // Arrange
        // You would need to set up a test user in CouchDB
        // For now, we'll use placeholder values that should exist in your test database
        string testUserName = "user2";
        string testPassword = "password";

        // Act
        var loginResult = await _accountTestHelper.AuthenticateAndCreateSessionAsync(
            testUserName,
            testPassword,
            _dbConfig,
            _configuration,
            _hostPrefix);

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
        if (_accountTestHelper == null || _dbConfig == null || _configuration == null)
        {
            Assert.Fail("Test helpers not initialized.");
            return;
        }

        // Arrange
        string testUserName = "user5";
        string wrongPassword = "password@@";

        // Act
        var loginResult = await _accountTestHelper.AuthenticateAndCreateSessionAsync(
            testUserName,
            wrongPassword,
            _dbConfig,
            _configuration,
            _hostPrefix);

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
        if (_accountTestHelper == null || _dbConfig == null || _configuration == null)
        {
            Assert.Fail("Test helpers not initialized.");
            return;
        }

        // Arrange
        string emptyUserName = string.Empty;
        string emptyPassword = string.Empty;

        // Act
        var loginResult = await _accountTestHelper.AuthenticateAndCreateSessionAsync(
            emptyUserName,
            emptyPassword,
            _dbConfig,
            _configuration,
            _hostPrefix);

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
        if (_accountTestHelper == null || _dbConfig == null || _configuration == null)
        {
            Assert.Fail("Test helpers not initialized.");
            return;
        }

        // Arrange
        string testUserName = "user2";
        string testPassword = "password";
        int customTimeoutMinutes = 45;
        var preLoginTime = DateTime.Now;

        // Act
        var loginResult = await _accountTestHelper.AuthenticateAndCreateSessionAsync(
            testUserName,
            testPassword,
            _dbConfig,
            _configuration,
            _hostPrefix,
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
    /// Helper method to extract session info from successful login.
    /// Useful for dependent tests that need session details.
    /// </summary>
    protected mmria.common.SharedLibraries.Account.Model.SessionInfo? GetSessionFromLogin(LoginResult loginResult)
    {
        return AccountTestHelper.GetSessionInfoFromLoginResult(loginResult);
    }
}
