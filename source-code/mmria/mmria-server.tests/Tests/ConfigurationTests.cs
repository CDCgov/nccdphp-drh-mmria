#nullable enable

using NUnit.Framework;
using System;
using System.Collections.Generic;
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

    
}
