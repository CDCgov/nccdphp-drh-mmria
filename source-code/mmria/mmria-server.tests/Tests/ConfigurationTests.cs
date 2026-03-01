#nullable enable

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using mmria_server.tests;
using mmria_server.tests.Helpers;


namespace mmria_server.tests.Tests;

/// <summary>
/// Configuration Tests validate the application's ability to:
/// - Load and apply configuration settings correctly
/// - Handle multi-tenant configuration overrides based on host prefix
/// var configLoader = new mmria.common.couchdb.MultiTenantConfigurationLoader(configuration);
/// mmria.common.couchdb.OverridableConfiguration
/// mmria.common.couchdb.ConfigurationSet
/// configLoader.LoadConfigurationSetsAsync
/// configLoader.LoadOverridableConfigurationsAsync
/* test with the following settings from appsettings.test.json:
    "timer_user_name":"mmrds",
    "timer_value":"mmrds",    
    "multi_tenant_jurisdictions": "tenant1,tenant2,tenant3,tenant4,tenant5,cdc",
    "multi_tenant_shared_config_id": "dev_cluster",
    "multi_tenant_template_couchdb_url": "http://{replace}-couchdb.local:6984",
*/
/// For guidence on how to implement the test, look at the program.cs within the mmria-server project, and see how the configuration is loaded and applied in the actual application. 
/// Create a test case within Scenario_A_LoadMultiTenantConfiguration() that do the above. For now just load the configurations with configLoader.LoadConfigurationSetsAsync and configLoader.LoadOverridableConfigurationsAsync
/// I will provide additional guidance later.
/// </summary>
[TestFixture]
public class ConfigurationTests
{
    private TestEnvironment _env = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _env = await TestEnvironment.BootstrapAsync("configuration");
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
    /// Scenario A: Load Multi-Tenant Configuration
    /// Validates configuration loading for multi-tenant deployments
    /// </summary>
    [Test]
    [Category("Configuration")]
    public async Task Scenario_A_LoadMultiTenantConfiguration()
    {
        // Load multi-tenant configurations using helper method
        var (configurationSets, overridableConfigs) = await _env.DbHelper.LoadMultiTenantConfigurationsAsync();

        // Verify ConfigurationSets were loaded
        Assert.That(configurationSets, Is.Not.Null, "ConfigurationSets should not be null");
        Assert.That(configurationSets.Count, Is.GreaterThan(0), "Should load at least one ConfigurationSet");

        TestContext.WriteLine($"Loaded {configurationSets.Count} ConfigurationSets:");
        foreach (var configSet in configurationSets)
        {
            TestContext.WriteLine($"  - {configSet._id}");
        }

        // Verify OverridableConfigurations were loaded
        Assert.That(overridableConfigs, Is.Not.Null, "OverridableConfigurations should not be null");
        Assert.That(overridableConfigs.Count, Is.GreaterThan(0), "Should load at least one OverridableConfiguration");

        TestContext.WriteLine($"\nLoaded {overridableConfigs.Count} OverridableConfigurations:");
        foreach (var config in overridableConfigs)
        {
            TestContext.WriteLine($"  - {config._id}");
        }

        TestContext.WriteLine($"\n✓ Scenario A complete - Multi-tenant configuration loaded successfully");
    }

    /// <summary>
    /// Scenario B: Program must disable cookies on shared CouchDB HTTP handlers.
    /// Guards against auth cookie bleed between user-auth and admin session writes.
    /// </summary>
    [Test]
    [Category("Configuration")]
    public void Scenario_B_CouchDbHttpHandlersDisableCookies()
    {
        var programPath = FindProgramCsPath();
        Assert.That(programPath, Is.Not.Null.And.Not.Empty, "Could not locate Program.cs for verification.");

        var programContent = File.ReadAllText(programPath!);

        var mainHandlerBlock = "AddHttpClient(\"CouchDb\"";
        var actorHandlerBlock = "actorServiceCollection.AddHttpClient(string.Empty";

        Assert.That(programContent.Contains(mainHandlerBlock), Is.True,
            "Expected main CouchDb HttpClient registration in Program.cs.");
        Assert.That(programContent.Contains(actorHandlerBlock), Is.True,
            "Expected actor CouchDb HttpClient registration in Program.cs.");

        var mainStart = programContent.IndexOf(mainHandlerBlock, StringComparison.Ordinal);
        var actorStart = programContent.IndexOf(actorHandlerBlock, StringComparison.Ordinal);

        Assert.That(mainStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(actorStart, Is.GreaterThanOrEqualTo(0));

        var mainSegment = programContent.Substring(mainStart, Math.Max(0, actorStart - mainStart));
        var actorSegment = programContent.Substring(actorStart);

        Assert.That(mainSegment.Contains("UseCookies = false"), Is.True,
            "Main CouchDb SocketsHttpHandler must set UseCookies = false.");
        Assert.That(actorSegment.Contains("UseCookies = false"), Is.True,
            "Actor CouchDb SocketsHttpHandler must set UseCookies = false.");
    }

    private static string? FindProgramCsPath()
    {
        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 10 && !string.IsNullOrWhiteSpace(current); i++)
        {
            var candidate = Path.GetFullPath(Path.Combine(current, "source-code", "mmria", "mmria-server", "Program.cs"));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            var parent = Directory.GetParent(current);
            if (parent == null)
            {
                break;
            }

            current = parent.FullName;
        }

        return null;
    }

    
}
