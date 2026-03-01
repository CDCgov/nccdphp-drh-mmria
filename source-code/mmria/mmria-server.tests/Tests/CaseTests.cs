#nullable enable

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using mmria_server.tests;
using mmria_server.tests.Helpers;
using mmria.common.SharedLibraries.CaseView;
using mmria.common.Testing.CaseGeneration.Services;
using mmria.common.Testing.CaseGeneration.Models;

namespace mmria_server.tests.Tests;

/// <summary>
/// Case Tests validate the case management system's ability to:
/// - Create and retrieve case documents
/// - Update and delete cases
/// - Enforce authorization and jurisdiction scoping
/// - Maintain data integrity across operations
/// - Handle edge cases and error conditions
/// 
/// Uses test data fixtures to validate case lifecycle operations.
/// Each scenario tests different aspects of case management.
/// </summary>
[TestFixture]
public class CaseTests
{
    private TestEnvironment _env = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _env = await TestEnvironment.BootstrapAsync("cases");
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
    /// Scenario A: Create Cases Using Case Generator
    /// Validates case generation with complete data and saves to CouchDB
    /// </summary>
    [Test]
    [Category("Case")]
    public async Task Scenario_A_CaseGenerator()
    {
        var cfg = _env.Config!;

        // Initialize case generator service with the test CouchDB client
        var caseGeneratorService = new CaseGeneratorService(_env.CouchDbClient);

        // Create generation configuration for edge strategy
        var metadataUrl = MiscHelpers.BuildMetadataUrl(cfg.MultiTenantMetadataUrl, cfg.ConfigLoader.TargetTestTenant, cfg.MetadataVersion);
        var generationConfig = new GenerationConfig
        {
            Jurisdiction = cfg.ConfigLoader.TargetTestTenant,
            JurisdictionId = "/",
            CaseCount = 200,
            MetadataVersion = cfg.MetadataVersion,
            OutputDirectory = "c:\\temp\\edge-cases",
            MetadataUrl = metadataUrl,
            Strategy = GenerationStrategy.FromName("edge"),
            SaveToCouchDb = true,
            CouchDbUrl = cfg.DbConfig.url,
            CouchDbUsername = cfg.DbConfig.user_name,
            CouchDbPassword = cfg.DbConfig.user_value,
            DatabaseName = "mmrds",
            ValidateBeforeSave = true,
            RandomSeed = 99999,
            DemographicWeights = new DemographicWeights
            {
                RaceEthnicity = new Dictionary<string, double>
                {
                    { "White", 0.60 },
                    { "Black", 0.15 },
                    { "Hispanic", 0.20 },
                    { "Asian", 0.04 },
                    { "Other", 0.01 }
                },
                Education = new Dictionary<string, double>
                {
                    { "High School or Less", 0.40 },
                    { "Some College", 0.25 },
                    { "Bachelor's Degree", 0.25 },
                    { "Advanced Degree", 0.10 }
                },
                Insurance = new Dictionary<string, double>
                {
                    { "Medicaid", 0.35 },
                    { "Private", 0.40 },
                    { "Uninsured", 0.15 },
                    { "Medicare", 0.08 },
                    { "Other", 0.02 }
                },
                AgeRange = new Dictionary<string, double>
                {
                    { "18-25", 0.25 },
                    { "26-35", 0.50 },
                    { "36-45", 0.20 },
                    { "46+", 0.05 }
                },
                MaritalStatus = new Dictionary<string, double>
                {
                    { "Single", 0.35 },
                    { "Married", 0.45 },
                    { "Divorced", 0.15 },
                    { "Widowed", 0.05 }
                },
                EmploymentStatus = new Dictionary<string, double>
                {
                    { "Employed", 0.65 },
                    { "Unemployed", 0.25 },
                    { "Other", 0.10 }
                },
                HousingStatus = new Dictionary<string, double>
                {
                    { "Stable", 0.75 },
                    { "Unstable", 0.15 },
                    { "Homeless", 0.10 }
                }
            }
        };

        // Generate and save cases
        TestContext.WriteLine($"Generating {generationConfig.CaseCount} test cases using edge strategy...");
        TestContext.WriteLine($"Target: {generationConfig.CouchDbUrl}/{generationConfig.DatabaseName}");
        var result = await caseGeneratorService.GenerateCasesAsync(generationConfig);

        // Verify generation succeeded
        Assert.That(result, Is.Not.Null, "Generation results should not be null");
        Assert.That(result.Success, Is.True, $"Generation should succeed: {result.ErrorMessage}");
        Assert.That(result.GeneratedCases, Is.Not.Null, "Generated cases should not be null");
        Assert.That(result.GeneratedCases.Count, Is.EqualTo(200), "Should generate exactly 200 cases");

        // Verify CouchDB save results
        Assert.That(result.CouchDbResult, Is.Not.Null, "CouchDB result should not be null when SaveToCouchDb is true");
        Assert.That(result.CouchDbResult!.SuccessCount, Is.EqualTo(200), "Should save all 200 cases to CouchDB");
        Assert.That(result.CouchDbResult.FailureCount, Is.EqualTo(0), "Should have no save failures");

        TestContext.WriteLine($"✓ Generated and saved {result.CouchDbResult.SuccessCount} cases successfully");
        TestContext.WriteLine($"✓ Success rate: {result.CouchDbResult.SuccessRate:F1}%");
        TestContext.WriteLine($"✓ Metadata version used: {cfg.MetadataVersion}");
        TestContext.WriteLine($"✓ Metadata URL used: {metadataUrl}");
        TestContext.WriteLine($"✓ Scenario A complete");
    }

    /// <summary>
    /// Scenario B: Get Case
    /// Validates case retrieval and deserialization
    /// </summary>
    [Test]
    [Category("Case")]
    public async Task Scenario_B_GetCase()
    {
        
    }

    /// <summary>
    /// Scenario C: Update Case
    /// Validates case updates and revision management
    /// </summary>
    [Test]
    [Category("Case")]
    public async Task Scenario_C_UpdateCase()
    {
   
    }

    /// <summary>
    /// Scenario D: Delete Case
    /// Validates case deletion and audit trail
    /// </summary>
    [Test]
    [Category("Case")]
    public async Task Scenario_D_DeleteCase()
    {

    }

    /// <summary>
    /// Scenario E: Authorization Enforcement
    /// Validates jurisdiction-scoped access control
    /// </summary>
    [Test]
    [Category("Case")]
    public async Task Scenario_E_AuthorizationEnforcement()
    {

    }

    /// <summary>
    /// Scenario F: Data Integrity
    /// Validates complex field types and conversions
    /// </summary>
    [Test]
    [Category("Case")]
    public async Task Scenario_F_DataIntegrity()
    {

    }

    /// <summary>
    /// Scenario G: Load Case List
    /// Validates case view search and filtering with pagination
    /// Tests: GET /api/case_view with sort, filtering, and pinned cases
    /// </summary>
    [Test]
    [Category("Case")]
    public async Task Scenario_G_LoadCaseList()
    {
        var cfg = _env.Config!;

        // Arrange - Authenticate user to get ClaimsPrincipal
        string testUserName = "user5";
        string testPassword = "password";
        const string Issuer = "https://contoso.com";

        TestContext.WriteLine("Authenticating user for case list retrieval...");

        var loginResult = await _env.AccountTestHelper.AuthenticateAndCreateSessionAsync(
            testUserName,
            testPassword,
            cfg.DbConfig,
            cfg.Configuration,
            cfg.HostPrefix);

        // Check if user exists
        if (loginResult.IsUnauthorized && loginResult.ErrorMessage?.Contains("not found") == true)
        {
            Assert.Inconclusive($"Test user '{testUserName}' does not exist in test database.");
            return;
        }

        Assert.That(loginResult.IsSuccessful, Is.True,
            $"User authentication failed: {loginResult.ErrorMessage}");
        Assert.That(loginResult.SessionInfo, Is.Not.Null, "SessionInfo required for case list query");

        var sessionInfo = loginResult.SessionInfo!;

        // Build ClaimsPrincipal from session (mirroring AccountController.Login pattern)
        var claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.Name, testUserName, ClaimValueTypes.String, Issuer));
        
        foreach (var role in sessionInfo.Roles ?? new List<string>())
        {
            claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }

        var userIdentity = new ClaimsIdentity("SuperSecureLogin");
        userIdentity.AddClaims(claims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);

        TestContext.WriteLine($"User authenticated: {testUserName}");
        TestContext.WriteLine($"User roles: {string.Join(", ", sessionInfo.Roles ?? new List<string>())}");

        // Act - Create CaseViewManager and execute query
        var caseViewManager = new mmria.common.SharedLibraries.CaseView.CaseViewManager(
            cfg.DbConfig,
            userPrincipal,
            true,  // isIdentifiedCase
            false, // includePinnedCases
            _env.CouchDbClient
        );

        // Execute case view query with default parameters
        var result = await caseViewManager.execute(
            System.Threading.CancellationToken.None,
            skip: 0,
            take: 25,
            sort: "by_date_created",
            search_key: null,
            descending: false,
            case_status: "all",
            field_selection: "all",
            pregnancy_relatedness: "all",
            date_of_death_range: "all",
            date_of_review_range: "all"
        );

        // Assert - Verify results
        Assert.That(result, Is.Not.Null, "Case view result should not be null");
        Assert.That(result.total_rows, Is.GreaterThan(0),
            "Case count should be greater than 0. Ensure cases exist in database.");
        Assert.That(result.rows, Is.Not.Null, "Rows should not be null");
        Assert.That(result.rows.Count, Is.GreaterThan(0),
            "At least one case should be returned in this batch");

        // Log results
        TestContext.WriteLine($"✓ Case list retrieved successfully");
        TestContext.WriteLine($"  Total cases: {result.total_rows}");
        TestContext.WriteLine($"  Cases in this batch: {result.rows.Count}");
        TestContext.WriteLine($"  First case record ID: {result.rows.FirstOrDefault()?.value?.record_id}");
        TestContext.WriteLine($"✓ Scenario G complete");
    }

    /// <summary>
    /// Scenario H: _rev Conflict Returns Failure
    /// Validates that saving a case with a stale _rev is rejected and signals failure.
    /// 
    /// Issue: CouchDB returns HTTP 409 on _rev conflict, but the server currently wraps it
    /// as HTTP 200 with ok:false in the body. This test documents that gap.
    /// 
    /// Steps:
    /// 1. Create a case, capture _rev (rev1)
    /// 2. Save the case again to advance to rev2
    /// 3. Attempt a third save using rev1 (stale)
    /// 
    /// Assert: response body contains "ok":false
    /// Assert: response HTTP status is 409 (currently will be 200 — test documents the bug)
    /// </summary>
    [Test]
    [Category("SaveConflict")]
    public async Task Scenario_H_StaleRev_Returns_Failure()
    {
        // TODO: Implement
        // 1. Create a case, capture _rev (rev1)
        // 2. Save the case again → new _rev (rev2)
        // 3. Save the case a third time using rev1 (stale)
        // Assert: response body contains "ok":false  (and ideally "error":"conflict")
        // Assert: response HTTP status is 409 (currently will be 200 — this test should FAIL today, documenting the bug)
        Assert.Inconclusive("Scenario H not yet implemented.");
    }

    /// <summary>
    /// Scenario I: Repeated Saves With Stale _rev All Fail
    /// Validates that once a _rev conflict occurs, subsequent saves with the same stale
    /// _rev also fail — confirming the client cannot recover without fetching the latest _rev.
    /// 
    /// Steps:
    /// 1. Create a case, capture _rev (rev1)
    /// 2. Save → advance to rev2
    /// 3. Attempt save with rev1 → expect ok:false
    /// 4. Attempt save with rev1 again → expect ok:false
    /// 
    /// Assert: neither attempt succeeded; document version is unchanged
    /// </summary>
    [Test]
    [Category("SaveConflict")]
    public async Task Scenario_I_Repeated_Stale_Rev_Saves_All_Fail()
    {
        // TODO: Implement
        // 1. Create a case, capture _rev (rev1)
        // 2. Save → advance to rev2
        // 3. Attempt save with rev1 → expect ok:false
        // 4. Attempt save with rev1 again → expect ok:false
        // Assert: neither attempt succeeded; document version unchanged
        Assert.Inconclusive("Scenario I not yet implemented.");
    }

    /// <summary>
    /// Scenario J: Server Accepts Save Without Lock
    /// Validates that the server does NOT enforce case locks — any authenticated user can
    /// save a case regardless of who holds the lock.
    /// 
    /// This documents the absence of server-side lock enforcement (Issue 4).
    /// Locks are currently client-side only.
    /// 
    /// Steps:
    /// 1. User A locks case (sets date_last_checked_out, last_checked_out_by = "userA")
    /// 2. User B POSTs a save to /api/case with a different identity, no lock fields
    /// 
    /// Assert: save succeeds (ok:true) — server did not reject the unlocked save
    /// </summary>
    [Test]
    [Category("LockEnforcement")]
    public async Task Scenario_J_Save_Without_Lock_Succeeds()
    {
        // TODO: Implement
        // 1. User A locks case (sets date_last_checked_out, last_checked_out_by = "userA")
        // 2. User B POSTs a save to /api/case with a different identity, no lock fields
        // Assert: save succeeds (ok:true)
        // This documents the absence of server-side lock enforcement
        Assert.Inconclusive("Scenario J not yet implemented.");
    }

    /// <summary>
    /// Scenario K: Concurrent Edits Cause Data Loss
    /// Validates that when two clients read the same case simultaneously and both attempt
    /// to save, one succeeds and the other's changes are silently lost.
    /// 
    /// Steps:
    /// 1. Both clients read the same case → both hold rev1
    /// 2. Client A saves a change to field_A → ok:true, document is now rev2
    /// 3. Client B saves a change to field_B using rev1 → expect conflict
    /// 
    /// Assert: Client B's field_B change is lost
    /// Assert: The conflict response signals failure (currently may be silent — documents the bug)
    /// </summary>
    [Test]
    [Category("SaveConflict")]
    public async Task Scenario_K_Concurrent_Edits_Cause_Data_Loss()
    {
        // TODO: Implement
        // 1. Read case → both clients hold rev1
        // 2. Client A saves field_A change → ok:true, now rev2
        // 3. Client B saves field_B change using rev1 → expect conflict
        // Assert: Client B's field_B change is lost
        // Assert: response signals failure (currently may be silent)
        Assert.Inconclusive("Scenario K not yet implemented.");
    }

    /// <summary>
    /// Scenario L: Migration Save Failure Not Propagated
    /// Validates that migration save methods return false on _rev conflict but do NOT
    /// throw — meaning callers that ignore the return value silently lose data.
    /// 
    /// Steps:
    /// 1. Insert a document, then advance its _rev externally (simulating an intervening save)
    /// 2. Call the migration save method with a stale payload
    /// 
    /// Assert: method returns false (failure detected internally)
    /// Assert: no exception is thrown (failure is silent to callers that ignore the return value)
    /// </summary>
    [Test]
    [Category("Migration")]
    public async Task Scenario_L_Migration_Save_Failure_Detected()
    {
        // TODO: Implement
        // 1. Insert a document, advance its _rev externally
        // 2. Call migration save method with stale payload
        // Assert: method returns false
        // Assert: no exception thrown (failure is silent to the caller)
        // Documents that callers must check the bool return value
        Assert.Inconclusive("Scenario L not yet implemented.");
    }

    /// <summary>
    /// Scenario M: Sync Conflict Is Silent
    /// Validates that document sync operations do not surface _rev conflicts to callers —
    /// the sync completes without error even when the target document is not updated.
    /// 
    /// Steps:
    /// 1. Write the same document to both source and target databases
    /// 2. Advance the target document's _rev independently (simulating a diverged edit)
    /// 3. Run the sync operation
    /// 
    /// Assert: no exception is thrown
    /// Assert: the target document was NOT updated (data diverged silently)
    /// </summary>
    [Test]
    [Category("Sync")]
    public async Task Scenario_M_Sync_Conflict_Silent()
    {
        // TODO: Implement
        // 1. Write doc to source and target
        // 2. Advance target's _rev independently
        // 3. Run sync operation
        // Assert: no exception thrown
        // Assert: target document was NOT updated (data diverged silently)
        Assert.Inconclusive("Scenario M not yet implemented.");
    }
    /// <summary>
    /// Scenario N: Loop Case Get
    /// Validates that every case in the database can be retrieved individually without errors.
    /// Collects all exceptions during the loop and outputs a distinct error summary at the end.
    /// 
    /// Steps:
    /// 1. Authenticate user and retrieve all case IDs via CaseViewManager
    /// 2. Loop through each case and call GetCaseAsync
    /// 3. Collect any exceptions thrown per case
    /// 4. Output a distinct list of unique error messages
    /// 
    /// Assert: all cases retrieved successfully with no exceptions
    /// </summary>
    [Test]
    [Category("Case")]
    public async Task Scenario_N_LoopCaseGet()
    {
        var cfg = _env.Config!;

        // Arrange - Authenticate user
        string testUserName = "user5";
        string testPassword = "password";
        const string Issuer = "https://contoso.com";

        TestContext.WriteLine("Authenticating user for loop case get...");

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
            $"User authentication failed: {loginResult.ErrorMessage}");
        Assert.That(loginResult.SessionInfo, Is.Not.Null, "SessionInfo required for case list query");

        var sessionInfo = loginResult.SessionInfo!;

        // Build ClaimsPrincipal from session
        var claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.Name, testUserName, ClaimValueTypes.String, Issuer));
        foreach (var role in sessionInfo.Roles ?? new List<string>())
        {
            claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }
        var userIdentity = new ClaimsIdentity("SuperSecureLogin");
        userIdentity.AddClaims(claims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);

        // Act - Step 1: Get all cases via CaseViewManager
        var caseViewManager = new mmria.common.SharedLibraries.CaseView.CaseViewManager(
            cfg.DbConfig,
            userPrincipal,
            true,  // isIdentifiedCase
            false, // includePinnedCases
            _env.CouchDbClient
        );

        // First pass to get total_rows, then retrieve all
        var firstPage = await caseViewManager.execute(
            System.Threading.CancellationToken.None,
            skip: 0,
            take: 1,
            sort: "by_date_created",
            search_key: null,
            descending: false,
            case_status: "all",
            field_selection: "all",
            pregnancy_relatedness: "all",
            date_of_death_range: "all",
            date_of_review_range: "all"
        );

        Assert.That(firstPage, Is.Not.Null, "Case view result should not be null");

        if (firstPage.total_rows == 0)
        {
            Assert.Inconclusive("No cases in database; cannot run loop test.");
            return;
        }

        TestContext.WriteLine($"Total cases found: {firstPage.total_rows}");

        var allCases = await caseViewManager.execute(
            System.Threading.CancellationToken.None,
            skip: 0,
            take: firstPage.total_rows,
            sort: "by_date_created",
            search_key: null,
            descending: false,
            case_status: "all",
            field_selection: "all",
            pregnancy_relatedness: "all",
            date_of_death_range: "all",
            date_of_review_range: "all"
        );

        Assert.That(allCases, Is.Not.Null, "Full case view result should not be null");
        Assert.That(allCases.rows, Is.Not.Null, "Rows should not be null");

        // Act - Step 2: Loop through each case and retrieve details, collecting exceptions
        var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_env.CouchDbClient);

        int successCount = 0;
        int nullCount = 0;
        var failedIds = new List<string>();
        // Collect (docId, exception) pairs for every failure
        var caseExceptions = new List<(string DocId, Exception Ex)>();

        foreach (var row in allCases.rows)
        {
            var docId = row.id;
            if (string.IsNullOrWhiteSpace(docId))
                continue;

            try
            {
                var caseDetail = await caseManager.GetCaseAsync(docId, cfg.DbConfig, userPrincipal);

                if (caseDetail != null)
                    successCount++;
                else
                {
                    nullCount++;
                    failedIds.Add(docId);
                }
            }
            catch (Exception ex)
            {
                caseExceptions.Add((docId, ex));
                failedIds.Add(docId);
            }
        }

        // Output summary
        TestContext.WriteLine($"Successfully retrieved: {successCount}");
        TestContext.WriteLine($"Null results (unauthorized or missing): {nullCount}");
        TestContext.WriteLine($"Exceptions thrown: {caseExceptions.Count}");

        if (failedIds.Count > 0)
            TestContext.WriteLine($"Failed IDs: {string.Join(", ", failedIds)}");

        // Output distinct error messages with counts
        if (caseExceptions.Count > 0)
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("=== Distinct Errors ===");
            var distinctErrors = caseExceptions
                .GroupBy(e => $"[{e.Ex.GetType().Name}] {e.Ex.Message}")
                .OrderByDescending(g => g.Count())
                .ToList();

            foreach (var group in distinctErrors)
            {
                TestContext.WriteLine($"  ({group.Count()}x) {group.Key}");
                // Show one representative case ID per distinct error
                TestContext.WriteLine($"    Example doc ID: {group.First().DocId}");
            }
            TestContext.WriteLine("=======================");
        }

        // Assert: all cases were retrieved without exception and without null
        Assert.That(caseExceptions.Count, Is.EqualTo(0),
            $"{caseExceptions.Count} exception(s) thrown during case retrieval. " +
            $"Distinct errors: {string.Join("; ", caseExceptions.GroupBy(e => $"[{e.Ex.GetType().Name}] {e.Ex.Message}").Select(g => $"{g.Key} ({g.Count()}x)"))}");

        Assert.That(successCount, Is.EqualTo(allCases.rows.Count),
            $"Expected all {allCases.rows.Count} cases to be retrieved, but {nullCount} returned null. " +
            $"Failed IDs: {string.Join(", ", failedIds)}");

        TestContext.WriteLine($"✓ Scenario N complete — all {successCount} cases retrieved without exception");
    }
    [Test]
    [Category("Case")]
    public async Task Scenario_O_EditCase()
    {
        var cfg = _env.Config!;

        // Arrange - Authenticate user
        string testUserName = "user5";
        string testPassword = "password";
        const string Issuer = "https://contoso.com";

        TestContext.WriteLine("Authenticating user for loop case get...");

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
            $"User authentication failed: {loginResult.ErrorMessage}");
        Assert.That(loginResult.SessionInfo, Is.Not.Null, "SessionInfo required for case list query");

        var sessionInfo = loginResult.SessionInfo!;

        // Build ClaimsPrincipal from session
        var claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.Name, testUserName, ClaimValueTypes.String, Issuer));
        foreach (var role in sessionInfo.Roles ?? new List<string>())
        {
            claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }
        var userIdentity = new ClaimsIdentity("SuperSecureLogin");
        userIdentity.AddClaims(claims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);

        // Act - Step 1: Get all cases via CaseViewManager
        var caseViewManager = new mmria.common.SharedLibraries.CaseView.CaseViewManager(
            cfg.DbConfig,
            userPrincipal,
            true,  // isIdentifiedCase
            false, // includePinnedCases
            _env.CouchDbClient
        );

        // First pass to get total_rows, then retrieve all
        var firstPage = await caseViewManager.execute(
            System.Threading.CancellationToken.None,
            skip: 0,
            take: 1,
            sort: "by_date_created",
            search_key: null,
            descending: false,
            case_status: "all",
            field_selection: "all",
            pregnancy_relatedness: "all",
            date_of_death_range: "all",
            date_of_review_range: "all"
        );

        Assert.That(firstPage, Is.Not.Null, "Case view result should not be null");

        if (firstPage.total_rows == 0)
        {
            Assert.Inconclusive("No cases in database; cannot run loop test.");
            return;
        }

        TestContext.WriteLine($"Total cases found: {firstPage.total_rows}");

        var allCases = await caseViewManager.execute(
            System.Threading.CancellationToken.None,
            skip: 0,
            take: firstPage.total_rows,
            sort: "by_date_created",
            search_key: null,
            descending: false,
            case_status: "all",
            field_selection: "all",
            pregnancy_relatedness: "all",
            date_of_death_range: "all",
            date_of_review_range: "all"
        );

        Assert.That(allCases, Is.Not.Null, "Full case view result should not be null");
        Assert.That(allCases.rows, Is.Not.Null, "Rows should not be null");

        try
        {
            // Act - Step 2: Get the first case and edit it
            var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_env.CouchDbClient);

            var docId = allCases.rows.FirstOrDefault()?.id;
            Assert.That(string.IsNullOrWhiteSpace(docId), Is.False,
                "Unable to find a case ID to edit.");

            var caseDetail = await caseManager.GetCaseAsync(docId!, cfg.DbConfig, userPrincipal);
            Assert.That(caseDetail, Is.Not.Null,
                $"Case with ID {docId} could not be retrieved for editing.");
            Assert.That(string.IsNullOrWhiteSpace(caseDetail!._rev), Is.False,
                $"Case {docId} is missing _rev before save.");

            TestContext.WriteLine($"Case {docId} retrieved successfully. Attempting to edit...");

            var originalUpdatedAt = caseDetail!.date_last_updated;
            var originalRev = caseDetail._rev;
            var expectedUpdatedAt = DateTime.UtcNow;
            var expectedUpdatedBy = $"{testUserName}-scenario-o-{Guid.NewGuid():N}";

            TestContext.WriteLine($"Original _rev: {originalRev}");

            caseDetail.date_last_updated = expectedUpdatedAt;
            caseDetail.last_updated_by = expectedUpdatedBy;

            var changeStack = new mmria.common.model.couchdb.Change_Stack
            {
                _id = Guid.NewGuid().ToString(),
                date_created = DateTime.UtcNow,
                user_name = testUserName,
                case_id = caseDetail._id,
                case_rev = caseDetail._rev
            };
            var saveResult = await caseManager.SaveCaseAsync(
                caseDetail,
                changeStack,
                cfg.DbConfig,
                userPrincipal,
                cfg.Configuration,
                cfg.HostPrefix);

            Assert.That(saveResult.Response.ok, Is.True,
                $"Failed to save updated case {docId}: {saveResult.Response.error_description}");

            var updatedCase = await caseManager.GetCaseAsync(docId!, cfg.DbConfig, userPrincipal);
            Assert.That(updatedCase, Is.Not.Null,
                $"Case with ID {docId} could not be retrieved after editing.");
            Assert.That(string.IsNullOrWhiteSpace(updatedCase!._rev), Is.False,
                $"Updated case {docId} is missing _rev after save.");

            Assert.That(updatedCase!.last_updated_by, Is.EqualTo(expectedUpdatedBy),
                "last_updated_by did not persist after save.");
            Assert.That(updatedCase.date_last_updated.HasValue, Is.True,
                "date_last_updated should be set after save.");
            Assert.That(updatedCase.date_last_updated!.Value.ToUniversalTime(),
                Is.GreaterThanOrEqualTo(expectedUpdatedAt.AddSeconds(-2)),
                "date_last_updated did not persist as expected.");

            if (originalUpdatedAt.HasValue)
            {
                Assert.That(updatedCase.date_last_updated!.Value.ToUniversalTime(),
                    Is.GreaterThanOrEqualTo(originalUpdatedAt.Value.ToUniversalTime()),
                    "date_last_updated did not advance from its original value.");
            }

            Assert.That(updatedCase._rev, Is.Not.EqualTo(originalRev),
                $"_rev did not advance for case {docId} after save.");

            TestContext.WriteLine($"✓ Case {docId} edited and verified successfully");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Scenario_O_EditCase threw an exception: {ex}");
        }
    }

    [Test]
    [Category("LockEnforcement")]
    public async Task Scenario_P_LockCaseForEditing_SecondUser_Within2Hours_Blocked()
    {
        var cfg = _env.Config!;

        string userA = "user5";
        string userB = "user2";
        string password = "password";
        const string Issuer = "https://contoso.com";

        TestContext.WriteLine("Authenticating two users for lock enforcement test (within 2 hours)...");

        var loginA = await _env.AccountTestHelper.AuthenticateAndCreateSessionAsync(
            userA,
            password,
            cfg.DbConfig,
            cfg.Configuration,
            cfg.HostPrefix);

        if (loginA.IsUnauthorized && loginA.ErrorMessage?.Contains("not found") == true)
        {
            Assert.Inconclusive($"Test user '{userA}' does not exist in test database.");
            return;
        }

        var loginB = await _env.AccountTestHelper.AuthenticateAndCreateSessionAsync(
            userB,
            password,
            cfg.DbConfig,
            cfg.Configuration,
            cfg.HostPrefix);

        if (loginB.IsUnauthorized && loginB.ErrorMessage?.Contains("not found") == true)
        {
            Assert.Inconclusive($"Test user '{userB}' does not exist in test database.");
            return;
        }

        Assert.That(loginA.IsSuccessful, Is.True, $"User A authentication failed: {loginA.ErrorMessage}");
        Assert.That(loginB.IsSuccessful, Is.True, $"User B authentication failed: {loginB.ErrorMessage}");
        Assert.That(loginA.SessionInfo, Is.Not.Null, "User A SessionInfo required");
        Assert.That(loginB.SessionInfo, Is.Not.Null, "User B SessionInfo required");

        var claimsA = new List<Claim> { new Claim(ClaimTypes.Name, userA, ClaimValueTypes.String, Issuer) };
        foreach (var role in loginA.SessionInfo!.Roles ?? new List<string>())
        {
            claimsA.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }

        var claimsB = new List<Claim> { new Claim(ClaimTypes.Name, userB, ClaimValueTypes.String, Issuer) };
        foreach (var role in loginB.SessionInfo!.Roles ?? new List<string>())
        {
            claimsB.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }

        var principalA = new ClaimsPrincipal(new ClaimsIdentity(claimsA, "SuperSecureLogin"));
        var principalB = new ClaimsPrincipal(new ClaimsIdentity(claimsB, "SuperSecureLogin"));

        try
        {
            var caseViewManager = new mmria.common.SharedLibraries.CaseView.CaseViewManager(
                cfg.DbConfig,
                principalA,
                true,
                false,
                _env.CouchDbClient
            );

            var firstPage = await caseViewManager.execute(
                System.Threading.CancellationToken.None,
                skip: 0,
                take: 1,
                sort: "by_date_created",
                search_key: null,
                descending: false,
                case_status: "all",
                field_selection: "all",
                pregnancy_relatedness: "all",
                date_of_death_range: "all",
                date_of_review_range: "all"
            );

            Assert.That(firstPage.total_rows, Is.GreaterThan(0), "No cases in database for lock test.");

            var allCases = await caseViewManager.execute(
                System.Threading.CancellationToken.None,
                skip: 0,
                take: firstPage.total_rows,
                sort: "by_date_created",
                search_key: null,
                descending: false,
                case_status: "all",
                field_selection: "all",
                pregnancy_relatedness: "all",
                date_of_death_range: "all",
                date_of_review_range: "all"
            );

            var docId = allCases.rows.FirstOrDefault()?.id;
            Assert.That(string.IsNullOrWhiteSpace(docId), Is.False, "Unable to find a case ID for lock test.");

            var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_env.CouchDbClient);

            var caseA = await caseManager.GetCaseAsync(docId!, cfg.DbConfig, principalA);
            Assert.That(caseA, Is.Not.Null, $"User A could not load case {docId}");

            var lockStart = DateTime.UtcNow;
            caseA!.date_last_updated = lockStart;
            caseA.date_last_checked_out = lockStart;
            caseA.last_checked_out_by = userA;

            var lockStackA = new mmria.common.model.couchdb.Change_Stack
            {
                _id = Guid.NewGuid().ToString(),
                date_created = DateTime.UtcNow,
                user_name = userA,
                case_id = caseA._id,
                case_rev = caseA._rev,
                note = "Scenario_P user A lock"
            };

            var saveA = await caseManager.SaveCaseAsync(
                caseA,
                lockStackA,
                cfg.DbConfig,
                principalA,
                cfg.Configuration,
                cfg.HostPrefix);

            Assert.That(saveA.Response.ok, Is.True,
                $"User A failed to lock case {docId}: {saveA.Response.error_description}");

            var caseB = await caseManager.GetCaseAsync(docId!, cfg.DbConfig, principalB);
            if (caseB == null)
            {
                Assert.Inconclusive($"Second user '{userB}' could not access case {docId} in this environment.");
                return;
            }

            var secondAttempt = DateTime.UtcNow;
            caseB!.date_last_updated = secondAttempt;
            caseB.date_last_checked_out = secondAttempt;
            caseB.last_checked_out_by = userB;

            var lockStackB = new mmria.common.model.couchdb.Change_Stack
            {
                _id = Guid.NewGuid().ToString(),
                date_created = DateTime.UtcNow,
                user_name = userB,
                case_id = caseB._id,
                case_rev = caseB._rev,
                note = "Scenario_P user B lock within 2h"
            };

            var saveB = await caseManager.SaveCaseAsync(
                caseB,
                lockStackB,
                cfg.DbConfig,
                principalB,
                cfg.Configuration,
                cfg.HostPrefix);

            var afterAttempt = await caseManager.GetCaseAsync(docId!, cfg.DbConfig, principalA);
            Assert.That(afterAttempt, Is.Not.Null, $"Unable to reload case {docId} after user B attempt.");

            // Expected behavior (currently known bug): second user should NOT be able to take lock within 2 hours.
            Assert.That(saveB.Response.ok, Is.False,
                "Expected second save to be blocked within lock window, but save succeeded.");
            Assert.That(afterAttempt!.last_checked_out_by, Is.EqualTo(userA),
                "Expected lock owner to remain user A within 2-hour window.");
        }
        catch (InconclusiveException)
        {
            throw;
        }
        catch (AssertionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Assert.Fail($"Scenario_P_LockCaseForEditing_SecondUser_Within2Hours_Blocked threw an exception: {ex}");
        }
    }

    [Test]
    [Category("LockEnforcement")]
    public async Task Scenario_Q_LockCaseForEditing_SecondUser_After2Hours_Allowed()
    {
        var cfg = _env.Config!;

        string userA = "user5";
        string userB = "user2";
        string password = "password";
        const string Issuer = "https://contoso.com";

        TestContext.WriteLine("Authenticating two users for lock expiry test (after 2+ hours)...");

        var loginA = await _env.AccountTestHelper.AuthenticateAndCreateSessionAsync(
            userA,
            password,
            cfg.DbConfig,
            cfg.Configuration,
            cfg.HostPrefix);

        if (loginA.IsUnauthorized && loginA.ErrorMessage?.Contains("not found") == true)
        {
            Assert.Inconclusive($"Test user '{userA}' does not exist in test database.");
            return;
        }

        var loginB = await _env.AccountTestHelper.AuthenticateAndCreateSessionAsync(
            userB,
            password,
            cfg.DbConfig,
            cfg.Configuration,
            cfg.HostPrefix);

        if (loginB.IsUnauthorized && loginB.ErrorMessage?.Contains("not found") == true)
        {
            Assert.Inconclusive($"Test user '{userB}' does not exist in test database.");
            return;
        }

        Assert.That(loginA.IsSuccessful, Is.True, $"User A authentication failed: {loginA.ErrorMessage}");
        Assert.That(loginB.IsSuccessful, Is.True, $"User B authentication failed: {loginB.ErrorMessage}");
        Assert.That(loginA.SessionInfo, Is.Not.Null, "User A SessionInfo required");
        Assert.That(loginB.SessionInfo, Is.Not.Null, "User B SessionInfo required");

        var claimsA = new List<Claim> { new Claim(ClaimTypes.Name, userA, ClaimValueTypes.String, Issuer) };
        foreach (var role in loginA.SessionInfo!.Roles ?? new List<string>())
        {
            claimsA.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }

        var claimsB = new List<Claim> { new Claim(ClaimTypes.Name, userB, ClaimValueTypes.String, Issuer) };
        foreach (var role in loginB.SessionInfo!.Roles ?? new List<string>())
        {
            claimsB.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }

        var principalA = new ClaimsPrincipal(new ClaimsIdentity(claimsA, "SuperSecureLogin"));
        var principalB = new ClaimsPrincipal(new ClaimsIdentity(claimsB, "SuperSecureLogin"));

        try
        {
            var caseViewManager = new mmria.common.SharedLibraries.CaseView.CaseViewManager(
                cfg.DbConfig,
                principalA,
                true,
                false,
                _env.CouchDbClient
            );

            var firstPage = await caseViewManager.execute(
                System.Threading.CancellationToken.None,
                skip: 0,
                take: 1,
                sort: "by_date_created",
                search_key: null,
                descending: false,
                case_status: "all",
                field_selection: "all",
                pregnancy_relatedness: "all",
                date_of_death_range: "all",
                date_of_review_range: "all"
            );

            Assert.That(firstPage.total_rows, Is.GreaterThan(0), "No cases in database for lock expiry test.");

            var allCases = await caseViewManager.execute(
                System.Threading.CancellationToken.None,
                skip: 0,
                take: firstPage.total_rows,
                sort: "by_date_created",
                search_key: null,
                descending: false,
                case_status: "all",
                field_selection: "all",
                pregnancy_relatedness: "all",
                date_of_death_range: "all",
                date_of_review_range: "all"
            );

            var docId = allCases.rows.FirstOrDefault()?.id;
            Assert.That(string.IsNullOrWhiteSpace(docId), Is.False, "Unable to find a case ID for lock expiry test.");

            var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_env.CouchDbClient);

            var caseA = await caseManager.GetCaseAsync(docId!, cfg.DbConfig, principalA);
            Assert.That(caseA, Is.Not.Null, $"User A could not load case {docId}");

            var expiredLockDate = DateTime.UtcNow.AddHours(-2).AddMinutes(-5);
            caseA!.date_last_updated = expiredLockDate;
            caseA.date_last_checked_out = expiredLockDate;
            caseA.last_checked_out_by = userA;

            var lockStackA = new mmria.common.model.couchdb.Change_Stack
            {
                _id = Guid.NewGuid().ToString(),
                date_created = DateTime.UtcNow,
                user_name = userA,
                case_id = caseA._id,
                case_rev = caseA._rev,
                note = "Scenario_Q user A expired lock"
            };

            var saveA = await caseManager.SaveCaseAsync(
                caseA,
                lockStackA,
                cfg.DbConfig,
                principalA,
                cfg.Configuration,
                cfg.HostPrefix);

            Assert.That(saveA.Response.ok, Is.True,
                $"User A failed to set initial expired lock on case {docId}: {saveA.Response.error_description}");

            var caseB = await caseManager.GetCaseAsync(docId!, cfg.DbConfig, principalB);
            if (caseB == null)
            {
                Assert.Inconclusive($"Second user '{userB}' could not access case {docId} in this environment.");
                return;
            }

            var lockByBDate = DateTime.UtcNow;
            caseB!.date_last_updated = lockByBDate;
            caseB.date_last_checked_out = lockByBDate;
            caseB.last_checked_out_by = userB;

            var lockStackB = new mmria.common.model.couchdb.Change_Stack
            {
                _id = Guid.NewGuid().ToString(),
                date_created = DateTime.UtcNow,
                user_name = userB,
                case_id = caseB._id,
                case_rev = caseB._rev,
                note = "Scenario_Q user B lock after expiry"
            };

            var saveB = await caseManager.SaveCaseAsync(
                caseB,
                lockStackB,
                cfg.DbConfig,
                principalB,
                cfg.Configuration,
                cfg.HostPrefix);

            Assert.That(saveB.Response.ok, Is.True,
                $"Expected user B to lock after expiry, but save failed: {saveB.Response.error_description}");

            var afterAttempt = await caseManager.GetCaseAsync(docId!, cfg.DbConfig, principalA);
            Assert.That(afterAttempt, Is.Not.Null, $"Unable to reload case {docId} after user B lock.");
            Assert.That(afterAttempt!.last_checked_out_by, Is.EqualTo(userB),
                "Expected lock owner to transfer to user B after 2-hour expiry.");
            Assert.That(afterAttempt.date_last_checked_out.HasValue, Is.True,
                "Expected date_last_checked_out to be set by user B.");
            Assert.That(afterAttempt.date_last_checked_out!.Value.ToUniversalTime(),
                Is.GreaterThanOrEqualTo(lockByBDate.AddSeconds(-2)),
                "Expected user B lock timestamp to be persisted.");
        }
        catch (InconclusiveException)
        {
            throw;
        }
        catch (AssertionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Assert.Fail($"Scenario_Q_LockCaseForEditing_SecondUser_After2Hours_Allowed threw an exception: {ex}");
        }
    }
}
