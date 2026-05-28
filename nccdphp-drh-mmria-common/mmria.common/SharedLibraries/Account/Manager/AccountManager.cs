#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using mmria.common.couchdb;
using mmria.common.SharedLibraries.Account.Model;
using mmria.common.SharedLibraries.Account.DAL;

namespace mmria.common.SharedLibraries.Account.Manager;

/// <summary>
/// Account Manager - orchestrates login, authorization, and session management.
/// Contains all business logic for account operations.
/// Calls DAL for all data access.
/// NO CouchDB calls in this class - all are delegated to AccountDAL.
/// </summary>
public class AccountManager
{
    private readonly AccountDAL _dal;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public AccountManager(AccountDAL dal, mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _dal = dal;
        _couchDbHttpClient = couchDbHttpClient;
    }

    /// <summary>
    /// Process complete login workflow:
    /// 1. Check if account is locked
    /// 2. Authenticate with CouchDB
    /// 3. Validate app prefix access
    /// 4. Create session
    /// Called by controller for the main login action
    /// </summary>
    public async Task<LoginResult> ProcessLoginAsync(
        LoginRequest loginRequest,
        DBConfigurationDetail dbConfig,
        OverridableConfiguration configuration,
        string hostPrefix,
        int sessionIdleTimeoutMinutes = 30)
    {
        // Validate input
        if (!loginRequest.IsValid())
        {
            return LoginResult.Unauthorized("Username and password are required.");
        }

        var canonicalUserName = NormalizeUserName(loginRequest.UserName);
        if (string.IsNullOrWhiteSpace(canonicalUserName))
        {
            return LoginResult.Unauthorized("Username and password are required.");
        }

        // Step 1: Check lockout status
        var lockoutStatus = await CheckLockoutStatusAsync(
            canonicalUserName,
            dbConfig,
            configuration,
            hostPrefix);

        if (lockoutStatus.IsLockedOut)
        {
            return LoginResult.LockedOut(lockoutStatus.GracePeriodDate);
        }

        // Step 2: Authenticate with CouchDB
        var authResult = await AuthenticateUserAsync(
            canonicalUserName,
            loginRequest.Password!,
            dbConfig,
            configuration,
            hostPrefix);

        if (!authResult.IsAuthenticated)
        {
            return LoginResult.Unauthorized(authResult.ErrorMessage);
        }

        if (!authResult.IsAppPrefixOk)
        {
            return LoginResult.Unauthorized("User does not have access to this application.");
        }

        // Step 3: Create session
        var sessionInfo = await CreateSessionAsync(
            authResult.UserName ?? canonicalUserName,
            authResult.UserRoles,
            dbConfig,
            sessionIdleTimeoutMinutes);

        if (!sessionInfo.IsSuccessful)
        {
            return LoginResult.Failure(sessionInfo.ErrorMessage ?? "Failed to create session.");
        }

        return LoginResult.Success(sessionInfo);
    }

    /// <summary>
    /// Check if account is locked due to failed login attempts
    /// Business logic: Count failures within time window, calculate grace period
    /// </summary>
    public async Task<LockoutStatus> CheckLockoutStatusAsync(
        string userName,
        DBConfigurationDetail dbConfig,
        OverridableConfiguration configuration,
        string hostPrefix)
    {
        userName = NormalizeUserName(userName);
        if (string.IsNullOrWhiteSpace(userName))
        {
            return LockoutStatus.NotLockedOut();
        }

        // Load lockout policy from configuration
        int thresholdBeforeLockout = 5;
        var thresholdConfig = configuration.GetInteger("unsuccessful_login_attempts_number_before_lockout", hostPrefix);
        if (thresholdConfig.HasValue)
        {
            thresholdBeforeLockout = thresholdConfig.Value;
        }

        int withinMinutes = 3;
        var withinConfig = configuration.GetInteger("unsuccessful_login_attempts_within_number_of_minutes", hostPrefix);
        if (withinConfig.HasValue)
        {
            withinMinutes = withinConfig.Value;
        }

        int lockoutMinutes = 3;
        var lockoutConfig = configuration.GetInteger("unsuccessful_login_attempts_lockout_number_of_minutes", hostPrefix);
        if (lockoutConfig.HasValue)
        {
            lockoutMinutes = lockoutConfig.Value;
        }

        // Get session events for this user
        var events = await _dal.GetSessionEventsAsync(userName, dbConfig);
        if (events.Count == 0)
        {
            return LockoutStatus.NotLockedOut();
        }

        // Count failed attempts within the time window
        var maxRange = DateTime.Now.AddMinutes(-withinMinutes);
        var failedAttempts = events
            .Where(e => e.date_created >= maxRange &&
                   e.action_result == mmria.common.model.couchdb.session_event.session_event_action_enum.failed_login)
            .ToList();

        if (failedAttempts.Count < thresholdBeforeLockout)
        {
            return LockoutStatus.NotLockedOut();
        }

        // Calculate grace period from first attempt
        var firstAttempt = failedAttempts.First();
        var gracePeriodDate = firstAttempt.date_created.AddMinutes(lockoutMinutes);

        if (DateTime.Now < gracePeriodDate)
        {
            return LockoutStatus.LockedOut(gracePeriodDate, failedAttempts.Count, thresholdBeforeLockout);
        }

        return LockoutStatus.NotLockedOut();
    }

    /// <summary>
    /// Authenticate user with CouchDB and validate permissions
    /// Business logic: Check app_prefix access, extract roles, validate authorization
    /// </summary>
    public async Task<AuthorizationStatus> AuthenticateUserAsync(
        string userName,
        string password,
        DBConfigurationDetail dbConfig,
        OverridableConfiguration configuration,
        string hostPrefix)
    {
        try
        {
            userName = NormalizeUserName(userName);
            if (string.IsNullOrWhiteSpace(userName))
            {
                return AuthorizationStatus.Failure("Username or password is incorrect.");
            }

            // Step 1: Get user from CouchDB /_users to check app_prefix access
            var couchUser = await _dal.GetCouchDbUserAsync(userName, dbConfig);
            if (couchUser == null)
            {
                return AuthorizationStatus.Failure("User not found.");
            }

            // Step 2: Check app_prefix access
            bool isAppPrefixOk = ValidateAppPrefixAccess(couchUser.app_prefix_list, dbConfig.prefix);

            // Step 3: Authenticate against CouchDB session endpoint
            var loginResponse = await _dal.AuthenticateWithSessionAsync(
                userName,
                password,
                dbConfig.url);

            if (loginResponse == null || !loginResponse.ok || string.IsNullOrWhiteSpace(loginResponse.name))
            {
                return AuthorizationStatus.Failure("Username or password is incorrect.");
            }

            var canonicalUserName = NormalizeUserName(loginResponse.name);
            if (string.IsNullOrWhiteSpace(canonicalUserName))
            {
                return AuthorizationStatus.Failure("Username or password is incorrect.");
            }

            // Step 4: If not app_prefix_ok from user doc, check if user is _admin
            if (!isAppPrefixOk && loginResponse.roles != null)
            {
                if (loginResponse.roles.Contains("_admin"))
                {
                    isAppPrefixOk = true;
                }
            }

            // Step 5: Build role list
            var roleList = new List<string>();
            if (loginResponse.roles != null)
            {
                foreach (var role in loginResponse.roles)
                {
                    if (role == "_admin")
                    {
                        roleList.Add("installation_admin");
                    }
                }
            }

            // Step 6: Get user's jurisdiction roles (from authorization library)
            // Note: This call to authorization helper stays here as it's business logic
            try
            {
#if !IS_PMSS_ENHANCED
                var jurisdictionRoles = mmria.common.SharedLibraries.Other.authorization
                    .get_current_user_role_jurisdiction_set_for(dbConfig, canonicalUserName, _couchDbHttpClient)
                    .Select(jr => jr.role_name)
                    .Distinct()
                    .ToList();
                roleList.AddRange(jurisdictionRoles);
#endif
#if IS_PMSS_ENHANCED
                var jurisdictionRoles = mmria.pmss.server.utils.authorization
                    .get_current_user_role_jurisdiction_set_for(dbConfig, canonicalUserName, _couchDbHttpClient)
                    .Select(jr => jr.role_name)
                    .Distinct()
                    .ToList();
                roleList.AddRange(jurisdictionRoles);
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load jurisdiction roles for {canonicalUserName}: {ex.Message}");
            }

            return AuthorizationStatus.Success(canonicalUserName, roleList, isAppPrefixOk);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication error for {userName}: {ex.Message}");
            return AuthorizationStatus.Failure($"Authentication error: {ex.Message}");
        }
    }

    /// <summary>
    /// Create session after successful authentication
    /// Business logic: Generate session ID, persist session to DB
    /// Note: Controller is responsible for building Session_Message actor message
    /// </summary>
    public async Task<SessionInfo> CreateSessionAsync(
        string userName,
        List<string> roles,
        DBConfigurationDetail dbConfig,
        int sessionIdleTimeoutMinutes = 30)
    {
        try
        {
            userName = NormalizeUserName(userName);
            if (string.IsNullOrWhiteSpace(userName))
            {
                return SessionInfo.Failure("Session creation failed: Username is required.");
            }

            // Generate session ID and event ID
            var sessionId = Guid.NewGuid().ToString();
            var sessionEventId = Guid.NewGuid().ToString();
            var expirationDateTime = DateTime.Now.AddMinutes(sessionIdleTimeoutMinutes);

            // Persist session document to CouchDB synchronously BEFORE returning.
            // CustomAuthHandler reads the sid cookie on the very next request (after redirect).
            // The Post_Session Akka actor fires after the redirect is issued, so it cannot
            // be relied on to write the document in time — this write must complete first.
            var sessionDoc = new
            {
                _id = sessionId,
                data_type = "session",
                date_created = DateTime.Now,
                date_last_updated = DateTime.Now,
                date_expired = expirationDateTime,
                is_active = true,
                user_id = userName,
                ip = string.Empty,
                session_event_id = sessionEventId,
                role_list = roles ?? new List<string>(),
                data = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            };
            var sessionJson = JsonConvert.SerializeObject(sessionDoc, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            await _dal.CreateSessionDocumentAsync(sessionJson, sessionId, dbConfig);

            return SessionInfo.Success(sessionId, expirationDateTime, userName, sessionEventId, roles);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Session creation error for {userName}: {ex.Message}");
            return SessionInfo.Failure($"Session creation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Business logic: Validate app_prefix access
    /// </summary>
    private bool ValidateAppPrefixAccess(Dictionary<string, bool>? appPrefixList, string? configPrefix)
    {
        if (string.IsNullOrWhiteSpace(configPrefix))
        {
            // No prefix configured - check if user allows __no_prefix__
            if (appPrefixList == null || appPrefixList.Count == 0)
            {
                return true;
            }
            return appPrefixList.ContainsKey("__no_prefix__");
        }

        // Prefix configured - check if user has access to this specific prefix
        return appPrefixList != null && appPrefixList.ContainsKey(configPrefix) && appPrefixList[configPrefix];
    }

    private static string NormalizeUserName(string? userName)
    {
        return (userName ?? string.Empty).Trim().ToLowerInvariant();
    }
}
