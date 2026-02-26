#nullable enable

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using mmria_server.tests;
using mmria.common.couchdb;


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
    private DatabaseTestHelper? _dbHelper;
    private mmria.common.getset.CouchDbHttpClient? _couchDbClient;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        // Initialize database helper with test configuration
        _dbHelper = new DatabaseTestHelper(purposeName: "aggregate_report");

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

        TestContext.WriteLine($"Aggregate Report Tests initialized. Database: {_dbHelper.GetTestDatabaseName()}");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {

    }

    /// <summary>
    /// Scenario A: Load Multi-Tenant Configuration
    /// Validates configuration loading for multi-tenant deployments
    /// </summary>
    [Test]
    [Category("Configuration")]
    public async Task Scenario_A_LoadMultiTenantConfiguration()
    {
        if (_couchDbClient == null)
        {
            Assert.Fail("CouchDB HTTP client not initialized.");
            return;
        }

        // Load test configuration using TestConfigurationLoader
        var configLoader = new TestConfigurationLoader();
        configLoader.Load();

        // Initialize MultiTenantConfigurationLoader
        var multiTenantLoader = new MultiTenantConfigurationLoader(null);

        TestContext.WriteLine($"Configuration loaded:");
        TestContext.WriteLine($"  Timer User: {configLoader.TimerUserName}");
        TestContext.WriteLine($"  Shared Config ID: {configLoader.SharedConfigId}");
        TestContext.WriteLine($"  CouchDB Template URL: {configLoader.CouchDbTemplateUrl}");
        TestContext.WriteLine($"  Tenants: {string.Join(", ", configLoader.Tenants)}");

        Assert.That(configLoader.Tenants, Is.Not.Empty, "Should have at least one tenant");

        // Load ConfigurationSets for all tenants
        TestContext.WriteLine($"\nLoading ConfigurationSets...");
        var configurationSets = await multiTenantLoader.LoadConfigurationSetsAsync(
            configLoader.Tenants,
            configLoader.CouchDbTemplateUrl,
            configLoader.TimerUserName,
            configLoader.TimerPassword,
            configLoader.ConfigId,
            _couchDbClient);

        Assert.That(configurationSets, Is.Not.Null, "ConfigurationSets should not be null");
        Assert.That(configurationSets.Count, Is.GreaterThan(0), "Should load at least one ConfigurationSet");
        TestContext.WriteLine($"  ✓ Loaded {configurationSets.Count} ConfigurationSets");

        foreach (var configSet in configurationSets)
        {
            TestContext.WriteLine($"    - {configSet._id}");
        }

        // Load OverridableConfigurations for all tenants
        TestContext.WriteLine($"\nLoading OverridableConfigurations...");
        var overridableConfigs = await multiTenantLoader.LoadOverridableConfigurationsAsync(
            configLoader.Tenants,
            configLoader.CouchDbTemplateUrl,
            configLoader.TimerUserName,
            configLoader.TimerPassword,
            configLoader.SharedConfigId,
            configLoader.ConfigId,
            _couchDbClient);

        Assert.That(overridableConfigs, Is.Not.Null, "OverridableConfigurations should not be null");
        Assert.That(overridableConfigs.Count, Is.GreaterThan(0), "Should load at least one OverridableConfiguration");
        TestContext.WriteLine($"  ✓ Loaded {overridableConfigs.Count} OverridableConfigurations");

        foreach (var config in overridableConfigs)
        {
            TestContext.WriteLine($"    - {config._id}");
        }

        TestContext.WriteLine($"\n✓ Scenario A complete - Multi-tenant configuration loaded successfully");
    }

    
}
