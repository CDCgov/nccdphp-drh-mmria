#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.getset;
using mmria_case_generator.Generators;
using mmria_case_generator.Models;

namespace mmria_server.tests;

/// <summary>
/// Fluent builder for declaring and generating test data scenarios.
/// Simplifies test setup by allowing tests to declare what data they need.
/// 
/// Example:
///   var fixture = await new TestDataBuilder()
///       .WithCaseCount(25)
///       .WithStrategy("complete")
///       .WithSeed(12345)
///       .ForScenario("aggregate-basic")
///       .BuildAsync(dbHelper, configLoader);
/// </summary>
public class TestDataBuilder
{
    private string _scenarioName = "custom";
    private int _caseCount = 10;
    private string _strategy = "complete";
    private int? _randomSeed;
    private string? _tenantName;
    private string? _metadataUrl;
    private bool _shouldSaveToDatabase = true;
    private string _databaseSelectionMode = "multi-tenant";
    private bool _validateBeforeSave = false;

    /// <summary>
    /// Set number of cases to generate
    /// </summary>
    public TestDataBuilder WithCaseCount(int count)
    {
        _caseCount = count;
        return this;
    }

    /// <summary>
    /// Set generation strategy (complete, minimal, edge, sparse)
    /// </summary>
    public TestDataBuilder WithStrategy(string strategy)
    {
        _strategy = strategy;
        return this;
    }

    /// <summary>
    /// Set random seed for reproducible data
    /// </summary>
    public TestDataBuilder WithSeed(int seed)
    {
        _randomSeed = seed;
        return this;
    }

    /// <summary>
    /// Set target tenant for multi-tenant scenarios
    /// </summary>
    public TestDataBuilder ForTenant(string tenantName)
    {
        _tenantName = tenantName;
        return this;
    }

    /// <summary>
    /// Set scenario name for tracking and debugging
    /// </summary>
    public TestDataBuilder ForScenario(string scenarioName)
    {
        _scenarioName = scenarioName;
        return this;
    }

    /// <summary>
    /// Set whether to save generated cases to database
    /// </summary>
    public TestDataBuilder SaveToDatabase(bool save)
    {
        _shouldSaveToDatabase = save;
        return this;
    }

    /// <summary>
    /// Set database selection mode (configured, multi-tenant, test)
    /// </summary>
    public TestDataBuilder WithDatabaseSelectionMode(string mode)
    {
        _databaseSelectionMode = mode;
        return this;
    }

    /// <summary>
    /// Enable validation before save
    /// </summary>
    public TestDataBuilder WithValidation()
    {
        _validateBeforeSave = true;
        return this;
    }

    /// <summary>
    /// Set custom metadata URL
    /// </summary>
    public TestDataBuilder WithMetadataUrl(string url)
    {
        _metadataUrl = url;
        return this;
    }

    /// <summary>
    /// Build and generate test data fixture asynchronously
    /// </summary>
    public async Task<TestDataFixture> BuildAsync(
        DatabaseTestHelper dbHelper,
        TestConfigurationLoader configLoader)
    {
        // Initialize fixture
        var fixture = new TestDataFixture
        {
            ScenarioName = _scenarioName,
            CaseCount = _caseCount,
            Seed = _randomSeed,
            CreatedAt = DateTime.UtcNow,
            CaseIds = new List<string>()
        };

        try
        {
            // Create generation configuration
            var tenant = _tenantName ?? (configLoader.Tenants.Length > 0 ? configLoader.Tenants[0] : "TESTJURISDICTION");
            var generationConfig = configLoader.CreateGenerationConfig(tenant, _metadataUrl);

            // Override with builder settings
            generationConfig.CaseCount = _caseCount;
            generationConfig.Strategy = GenerationStrategy.FromName(_strategy);
            generationConfig.RandomSeed = _randomSeed;

            generationConfig.OutputConfig!.SaveToDatabase = _shouldSaveToDatabase;
            generationConfig.OutputConfig!.DatabaseSelectionMode = _databaseSelectionMode;
            generationConfig.OutputConfig!.ValidateBeforeSave = _validateBeforeSave;

            Console.WriteLine($"[TestDataBuilder] Building scenario '{_scenarioName}'");
            Console.WriteLine($"  Cases: {_caseCount}");
            Console.WriteLine($"  Strategy: {_strategy}");
            Console.WriteLine($"  Tenant: {tenant}");
            Console.WriteLine($"  Seed: {_randomSeed?.ToString() ?? "(random)"}");

            // Create test database if needed
            if (_shouldSaveToDatabase)
            {
                var testDbUrl = dbHelper.GetTestDatabaseUrl();
                var caseHelper = new CaseDataHelper(
                    new CouchDbHttpClient(new mmria.common.SimpleHttpClientFactory()),
                    testDbUrl,
                    configLoader.TimerUserName,
                    configLoader.TimerPassword
                );

                // Initialize metadata
                var metadataUrl = generationConfig.GetResolvedMetadataUrl();
                var metadataManager = new MetadataManager();
                
                try
                {
                    await metadataManager.FetchMetadataAsync(metadataUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ⚠ Warning: Could not fetch metadata from {metadataUrl}: {ex.Message}");
                    Console.WriteLine($"  Continuing with empty metadata...");
                }

                // Generate and save cases
                fixture.CaseIds = await caseHelper.GenerateAndSaveRealisticCasesAsync(
                    generationConfig,
                    metadataManager,
                    _caseCount
                );

                Console.WriteLine($"  ✓ Generated {fixture.CaseIds.Count} cases");
            }
            else
            {
                // Generate in-memory only
                var metadataUrl = generationConfig.GetResolvedMetadataUrl();
                var metadataManager = new MetadataManager();
                
                try
                {
                    await metadataManager.FetchMetadataAsync(metadataUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ⚠ Warning: Could not fetch metadata from {metadataUrl}: {ex.Message}");
                    Console.WriteLine($"  Continuing with empty metadata...");
                }

                var generator = new CaseDataGenerator(metadataManager, generationConfig);
                for (int i = 1; i <= _caseCount; i++)
                {
                    var generatedCase = generator.GenerateCase(i);
                    fixture.CaseIds.Add(generatedCase["_id"].ToString() ?? $"case-{i}");
                }

                Console.WriteLine($"  ✓ Generated {_caseCount} cases (in-memory)");
            }

            // Compute distribution summary
            fixture.Summary = new TestDataFixture.DistributionSummary();

            Console.WriteLine($"[TestDataBuilder] Scenario '{_scenarioName}' complete");

            return fixture;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TestDataBuilder] ERROR building scenario: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// Result of building a test data scenario.
/// Contains generated case IDs and distribution summary for assertions.
/// </summary>
public class TestDataFixture
{
    /// <summary>
    /// Scenario name for tracking
    /// </summary>
    public string ScenarioName { get; set; } = string.Empty;

    /// <summary>
    /// List of generated case IDs (for cleanup and querying)
    /// </summary>
    public List<string> CaseIds { get; set; } = new();

    /// <summary>
    /// Total number of cases generated
    /// </summary>
    public int CaseCount { get; set; }

    /// <summary>
    /// Random seed used (if any) for reproducibility
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// When fixture was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Distribution summary extracted from generated data
    /// Used for validating report outputs against expected distributions
    /// </summary>
    public DistributionSummary Summary { get; set; } = new();

    /// <summary>
    /// Extracted distribution data from generated cases
    /// Used for assertions against report endpoints
    /// </summary>
    public class DistributionSummary
    {
        public int PregnancyRelated { get; set; }
        public int PregnancyAssociatedNotRelated { get; set; }
        public Dictionary<string, int> EthnicityBreakdown { get; set; } = new();
        public Dictionary<string, int> AgeDistribution { get; set; } = new();
        public Dictionary<string, int> ToxicologyBreakdown { get; set; } = new();
        public Dictionary<string, int> EducationBreakdown { get; set; } = new();
        public Dictionary<string, int> ContributingFactors { get; set; } = new();
        public Dictionary<string, int> PregnancyRelatednessBreakdown { get; set; } = new();
    }
}
