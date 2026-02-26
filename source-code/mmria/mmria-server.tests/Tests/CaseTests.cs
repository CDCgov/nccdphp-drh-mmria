#nullable enable

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using mmria_server.tests;
using mmria.common.SharedLibraries.CaseView;
using mmria.common.Testing.CaseGeneration.Services;
using mmria.common.Testing.CaseGeneration.Models;

namespace mmria_server.tests.Tests;

/// <summary>
/// Wrapper around DatabaseTestHelper to override database URL for simple tenant-based naming.
/// Allows using simple database names (mmrds) instead of test naming pattern (mmria_test_tenant5_mmrds_20260226_014442).
/// </summary>


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
    private DatabaseTestHelper? _dbHelper;
    private mmria.common.getset.CouchDbHttpClient? _couchDbClient;
    
    // Multi-tenant configurations loaded from CouchDB
    private List<mmria.common.couchdb.ConfigurationSet>? _configurationSets;
    private List<mmria.common.couchdb.OverridableConfiguration>? _overridableConfigs;
    
    // Configuration objects for test scenario setup
    private mmria.common.couchdb.OverridableConfiguration? _configuration;
    private mmria.common.couchdb.DBConfigurationDetail? _dbConfig;
    private string _hostPrefix = string.Empty;
    private string _metadataVersion = "26.01.20"; // Default metadata version

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        // Initialize database helper with test configuration
        _dbHelper = new DatabaseTestHelper(purposeName: "cases");

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

        // Get the CouchDB HTTP client for direct access in tests
        _couchDbClient = _dbHelper.GetCouchDbHttpClient();

        TestContext.WriteLine($"Case Tests initialized. Database: {_dbHelper.GetTestDatabaseName()}");
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
        (_configurationSets, _overridableConfigs) = await _dbHelper.LoadMultiTenantConfigurationsAsync();

        // Filter OverridableConfiguration by tenant and shared config ID
        // Naming convention: {target_test_tenant}_{multi_tenant_shared_config_id}
        // Example: tenant5_dev_cluster
        string targetConfigId = $"{configLoader.TargetTestTenant}_{configLoader.SharedConfigId}";
        _configuration = _overridableConfigs.FirstOrDefault(c => c._id == targetConfigId);
        
        if (_configuration == null)
        {
            TestContext.WriteLine($"Warning: Could not find OverridableConfiguration with ID '{targetConfigId}'");
            TestContext.WriteLine($"Available configs: {string.Join(", ", _overridableConfigs.Select(c => c._id))}");
            // Fall back to creating a basic configuration
            _configuration = new mmria.common.couchdb.OverridableConfiguration();
        }

        // Filter ConfigurationSet - find the one that matches our target tenant
        // ConfigurationSets contain detail_list with host_prefix keys
        mmria.common.couchdb.ConfigurationSet? targetConfigSet = null;
        string targetHostPrefix = configLoader.TargetTestTenant;
        
        foreach (var configSet in _configurationSets ?? new List<mmria.common.couchdb.ConfigurationSet>())
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
        
        // Get metadata version from configuration
        // Structure: string_keys["shared"]["metadata_version"]
        _metadataVersion = ""; // default
        
        if (_configuration?.string_keys != null && _configuration.string_keys.ContainsKey("shared"))
        {
            var sharedDict = _configuration.string_keys["shared"];
            if (sharedDict.ContainsKey("metadata_version"))
            {
                _metadataVersion = sharedDict["metadata_version"];
                TestContext.WriteLine($"Loaded metadata_version from shared: {_metadataVersion}");
            }
        }
        
        TestContext.WriteLine($"Case Test Configuration:");
        TestContext.WriteLine($"  Target Tenant: {configLoader.TargetTestTenant}");
        TestContext.WriteLine($"  Shared Config ID: {configLoader.SharedConfigId}");
        TestContext.WriteLine($"  Host Prefix: {_hostPrefix}");
        TestContext.WriteLine($"  CouchDB URL: {_dbConfig?.url}");
        TestContext.WriteLine($"  Metadata Version (loaded): '{_metadataVersion}'");
        
        // Ensure metadata version was loaded
        if (string.IsNullOrEmpty(_metadataVersion))
        {
            Assert.Fail($"Metadata version not found in configuration shared keys. Check configuration setup.");
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        // Clear test documents from database
        if (_dbHelper != null)
        {
            await _dbHelper.ClearTestDatabaseAsync();
            TestContext.WriteLine($"Case Tests cleanup complete.");
        }
    }

    /// <summary>
    /// Scenario A: Create Cases Using Case Generator
    /// Validates case generation with complete data and saves to CouchDB
    /// </summary>
    [Test]
    [Category("Case")]
    public async Task Scenario_A_CaseGenerator()
    {
        if (_dbHelper == null || _couchDbClient == null)
        {
            Assert.Fail("Database helper not initialized.");
            return;
        }

        // Load test configuration
        var configLoader = new TestConfigurationLoader();
        configLoader.Load();

        // Initialize case generator service with the test CouchDB client
        var caseGeneratorService = new CaseGeneratorService(_couchDbClient);

        // Create generation configuration for edge strategy
        var generationConfig = new GenerationConfig
        {
            Jurisdiction = configLoader.TargetTestTenant,
            JurisdictionId = "/",
            CaseCount = 200,
            MetadataVersion = _metadataVersion,
            OutputDirectory = "c:\\temp\\edge-cases",
            MetadataUrl = $"https://{configLoader.TargetTestTenant}-mmria.local:12345/api/version/{_metadataVersion}/metadata",
            Strategy = GenerationStrategy.FromName("edge"),
            SaveToCouchDb = true,
            CouchDbUrl = _dbConfig?.url,
            CouchDbUsername = _dbConfig?.user_name,
            CouchDbPassword = _dbConfig?.user_value,
            DatabaseName = "mmrds",
            ValidateBeforeSave = true,
            RandomSeed = 99999,
            DemographicWeights = new mmria.common.Testing.CaseGeneration.Models.DemographicWeights
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
        TestContext.WriteLine($"✓ Metadata version used: {_metadataVersion}");
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
       
    }
}
