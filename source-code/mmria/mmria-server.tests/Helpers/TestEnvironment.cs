#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using NUnit.Framework;

namespace mmria_server.tests.Helpers;

/// <summary>
/// Resolved configuration DTO returned by <see cref="TestEnvironment.ResolveConfigurationAsync"/>.
/// Contains every value the SetUp block used to compute manually.
/// </summary>
public sealed class TestEnvironmentConfig
{
    public required OverridableConfiguration Configuration { get; init; }
    public required DBConfigurationDetail DbConfig { get; init; }
    public required string HostPrefix { get; init; }
    public required string MetadataVersion { get; init; }
    public required string MultiTenantMetadataUrl { get; init; }
    public required TestConfigurationLoader ConfigLoader { get; init; }
}

/// <summary>
/// One-stop helper that replaces the repeated OneTimeSetUp / SetUp / OneTimeTearDown
/// blocks across every test class.
///
/// Usage in a test class:
/// <code>
/// private TestEnvironment _env = null!;
///
/// [OneTimeSetUp]
/// public async Task OneTimeSetUpAsync()
/// {
///     _env = await TestEnvironment.BootstrapAsync("cases");
/// }
///
/// [SetUp]
/// public async Task SetUpAsync()
/// {
///     await _env.ResolveConfigurationAsync();
/// }
///
/// [OneTimeTearDown]
/// public async Task OneTimeTearDownAsync()
/// {
///     await _env.CleanupAsync();
/// }
/// </code>
///
/// After bootstrap + resolve you can access:
///   _env.DbHelper, _env.CouchDbClient, _env.AccountTestHelper, _env.Config
/// </summary>
public sealed class TestEnvironment
{
    // --- Bootstrapped (OneTimeSetUp) ---
    public DatabaseTestHelper DbHelper { get; }
    public CouchDbHttpClient CouchDbClient { get; }
    public AccountTestHelper AccountTestHelper { get; }

    // --- Resolved (SetUp) ---
    public TestEnvironmentConfig? Config { get; private set; }

    private TestEnvironment(DatabaseTestHelper dbHelper, CouchDbHttpClient couchDbClient)
    {
        DbHelper = dbHelper;
        CouchDbClient = couchDbClient;
        AccountTestHelper = new AccountTestHelper(couchDbClient);
    }

    /// <summary>
    /// Database bootstrap — replaces the OneTimeSetUp block.
    /// Creates the DatabaseTestHelper, verifies CouchDB connectivity,
    /// verifies the test database exists, and returns a ready-to-use environment.
    /// Marks the test Inconclusive when infrastructure is unavailable.
    /// </summary>
    /// <param name="purposeName">Label used in the generated database name (e.g. "cases", "aggregate_report").</param>
    public static async Task<TestEnvironment> BootstrapAsync(string purposeName)
    {
        var dbHelper = new DatabaseTestHelper(purposeName: purposeName);

        if (!dbHelper.ConfigurationLoader.HasResolvedSensitiveSettings())
        {
            Assert.Inconclusive(dbHelper.ConfigurationLoader.GetSensitiveSettingsSetupMessage());
        }

        bool isAccessible = await dbHelper.IsCouchDbAccessibleAsync();
        if (!isAccessible)
        {
            Assert.Inconclusive("CouchDB is not accessible. Check configuration and connection.");
        }

        bool exists = await dbHelper.TestDatabaseExistsAsync();
        if (!exists)
        {
            Assert.Inconclusive("Test database does not exist.");
        }

        var couchDbClient = dbHelper.GetCouchDbHttpClient();

        TestContext.WriteLine($"TestEnvironment bootstrapped. Database: {dbHelper.GetTestDatabaseName()}");

        return new TestEnvironment(dbHelper, couchDbClient);
    }

    /// <summary>
    /// Configuration resolution — replaces the SetUp block.
    /// Loads multi-tenant configs from CouchDB, filters by tenant,
    /// extracts metadata version / URL, validates, and stores the result in <see cref="Config"/>.
    /// </summary>
    public async Task<TestEnvironmentConfig> ResolveConfigurationAsync()
    {
        // 1. Load TestConfigurationLoader
        var configLoader = new TestConfigurationLoader();
        configLoader.Load();

        if (!configLoader.HasResolvedSensitiveSettings())
        {
            Assert.Inconclusive(configLoader.GetSensitiveSettingsSetupMessage());
        }

        // 2. Load multi-tenant configurations from CouchDB
        var (configurationSets, overridableConfigs) = await DbHelper.LoadMultiTenantConfigurationsAsync();

        // 3. Filter OverridableConfiguration by tenant + shared config ID
        string targetConfigId = $"{configLoader.TargetTestTenant}_{configLoader.SharedConfigId}";
        var configuration = overridableConfigs.FirstOrDefault(c => c._id == targetConfigId);

        if (configuration == null)
        {
            TestContext.WriteLine($"Warning: Could not find OverridableConfiguration with ID '{targetConfigId}'");
            TestContext.WriteLine($"Available configs: {string.Join(", ", overridableConfigs.Select(c => c._id))}");
            configuration = new OverridableConfiguration();
        }

        // 4. Filter ConfigurationSet for target tenant
        string targetHostPrefix = configLoader.TargetTestTenant;
        DBConfigurationDetail? dbConfig = null;

        foreach (var configSet in configurationSets ?? new List<ConfigurationSet>())
        {
            if (configSet.detail_list != null && configSet.detail_list.ContainsKey(targetHostPrefix))
            {
                dbConfig = configSet.detail_list[targetHostPrefix];
                break;
            }
        }

        if (dbConfig == null)
        {
            // Fallback: build from loaded config
            string couchDbUrl = DbHelper.GetTestDatabaseUrl().TrimEnd('/');
            if (couchDbUrl.EndsWith("/mmrds"))
            {
                couchDbUrl = couchDbUrl.Substring(0, couchDbUrl.Length - 6);
            }

            dbConfig = new DBConfigurationDetail
            {
                url = couchDbUrl,
                user_name = configLoader.TimerUserName,
                user_value = configLoader.TimerPassword,
                prefix = configLoader.TestDatabasePrefix
            };

            TestContext.WriteLine($"Warning: ConfigurationSet details not found for '{targetHostPrefix}'. Using fallback configuration.");
        }

        // 5. Extract metadata version and URL from shared keys
        string metadataVersion = "";
        string multiTenantMetadataUrl = "";

        if (configuration?.string_keys != null && configuration.string_keys.ContainsKey("shared"))
        {
            var sharedDict = configuration.string_keys["shared"];
            if (sharedDict.ContainsKey("metadata_version"))
            {
                metadataVersion = sharedDict["metadata_version"];
                TestContext.WriteLine($"Loaded metadata_version from shared: {metadataVersion}");
            }
            if (sharedDict.ContainsKey("multi_tenant_metadata_url"))
            {
                multiTenantMetadataUrl = sharedDict["multi_tenant_metadata_url"];
                TestContext.WriteLine($"Loaded multi_tenant_metadata_url from shared: {multiTenantMetadataUrl}");
            }
        }

        // 6. Log resolved state
        TestContext.WriteLine($"Test Configuration: Target Tenant: {configLoader.TargetTestTenant}, Shared Config ID: {configLoader.SharedConfigId}, Host Prefix: {targetHostPrefix}, CouchDB URL: {dbConfig?.url}, Metadata Version (loaded): '{metadataVersion}', Metadata URL Template (loaded): '{multiTenantMetadataUrl}'");

        // 7. Validate
        if (string.IsNullOrEmpty(metadataVersion))
        {
            Assert.Fail("Metadata version not found in configuration shared keys. Check configuration setup.");
        }

        Config = new TestEnvironmentConfig
        {
            Configuration = configuration!,
            DbConfig = dbConfig!,
            HostPrefix = targetHostPrefix,
            MetadataVersion = metadataVersion,
            MultiTenantMetadataUrl = multiTenantMetadataUrl,
            ConfigLoader = configLoader
        };

        return Config;
    }

    /// <summary>
    /// Teardown — replaces the OneTimeTearDown block.
    /// Clears test documents from the database.
    /// </summary>
    public async Task CleanupAsync()
    {
        await DbHelper.ClearTestDatabaseAsync();
        TestContext.WriteLine("TestEnvironment cleanup complete.");
    }
}
