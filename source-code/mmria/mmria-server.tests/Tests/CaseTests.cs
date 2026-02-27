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
        string testUserName = "user2";
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
}
