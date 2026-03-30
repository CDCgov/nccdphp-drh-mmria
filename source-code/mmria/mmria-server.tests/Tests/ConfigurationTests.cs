#nullable enable

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
/* local configuration now loads from appsettings.local.example.json plus appsettings.local.json:
    "multi_tenant_jurisdictions": "tenant1,tenant2,tenant3,tenant4,tenant5,cdc",
    "multi_tenant_shared_config_id": "dev_cluster",
    "multi_tenant_template_couchdb_url": "http://{replace}-couchdb.local:6984"
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
        if (_env != null)
        {
            await _env.CleanupAsync();
        }
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
    /// Scenario B: Program must keep the shared CouchDB handler cookie-free and avoid
    /// reintroducing the old actor-specific service provider path.
    /// </summary>
    [Test]
    [Category("Configuration")]
    public void Scenario_B_ServerStartupUsesSingleProviderAndCookieFreeCouchDbHandler()
    {
        var programPath = FindProgramCsPath();
        Assert.That(programPath, Is.Not.Null.And.Not.Empty, "Could not locate Program.cs for verification.");

        var programContent = File.ReadAllText(programPath!);

        var mainHandlerBlock = "AddHttpClient(\"CouchDb\"";
        var rebuildHandlerBlock = "AddHttpClient(\"CouchDbRebuild\"";

        Assert.That(programContent.Contains(mainHandlerBlock), Is.True,
            "Expected main CouchDb HttpClient registration in Program.cs.");
        Assert.That(programContent.Contains(rebuildHandlerBlock), Is.True,
            "Expected rebuild CouchDb HttpClient registration in Program.cs.");
        Assert.That(programContent.Contains("BuildServiceProvider()"), Is.False,
            "Program.cs should not create an ad hoc startup service provider.");

        var mainStart = programContent.IndexOf(mainHandlerBlock, StringComparison.Ordinal);
        var rebuildStart = programContent.IndexOf(rebuildHandlerBlock, StringComparison.Ordinal);

        Assert.That(mainStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(rebuildStart, Is.GreaterThanOrEqualTo(0));

        var mainSegment = programContent.Substring(mainStart, Math.Max(0, rebuildStart - mainStart));
        var rebuildSegment = programContent.Substring(rebuildStart);

        Assert.That(mainSegment.Contains("UseCookies = false"), Is.True,
            "Main CouchDb SocketsHttpHandler must set UseCookies = false.");
        Assert.That(rebuildSegment.Contains("UseCookies = false"), Is.True,
            "Rebuild CouchDb SocketsHttpHandler must set UseCookies = false.");
    }

    [Test]
    [Category("Configuration")]
    public void Scenario_C_ServicesShipsCurrentCaseTemplateAsset()
    {
        var serverTemplatePath = FindRepoRelativePath(
            "source-code",
            "mmria",
            "mmria-server",
            "database-scripts",
            "case-version-26.01.20.json");
        var servicesTemplatePath = FindRepoRelativePath(
            "nccdphp-drh-mmria-services",
            "mmria.services",
            "database-scripts",
            "case-version-26.01.20.json");

        Assert.That(File.Exists(serverTemplatePath), Is.True, "Expected server case template source file.");
        Assert.That(File.Exists(servicesTemplatePath), Is.True, "Expected mmria.services case template asset file.");

        var serverTemplate = File.ReadAllText(serverTemplatePath);
        var servicesTemplate = File.ReadAllText(servicesTemplatePath);

        Assert.That(servicesTemplate, Is.Not.Empty, "mmria.services case template asset should not be empty.");
        Assert.That(
            NormalizeTemplateText(servicesTemplate),
            Is.EqualTo(NormalizeTemplateText(serverTemplate)),
            "mmria.services should ship the current server case template content.");
    }

    [Test]
    [Category("Configuration")]
    public async Task Scenario_D_RebuildResolverFindsExactServicesCaseTemplateWithoutFallback()
    {
        var servicesTemplatePath = FindRepoRelativePath(
            "nccdphp-drh-mmria-services",
            "mmria.services",
            "database-scripts",
            "case-version-26.01.20.json");
        var servicesProjectDirectory = Directory.GetParent(Path.GetDirectoryName(servicesTemplatePath)!)!.FullName;
        var expectedTemplate = File.ReadAllText(servicesTemplatePath);
        var logMessages = new List<string>();
        var originalCurrentDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(servicesProjectDirectory);

            var resolvedTemplate = await mmria.common.SharedLibraries.MMRIARebuild.Manager.c_case_template_resolver
                .ReadBestAvailableCaseTemplateAsync("26.01.20", logMessages.Add);

            Assert.That(resolvedTemplate, Is.EqualTo(expectedTemplate));
            Assert.That(
                logMessages.Any(message => message.Contains("Falling back", StringComparison.OrdinalIgnoreCase)),
                Is.False,
                "Resolver should not fall back when the exact services case template is present.");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
        }
    }

    private static string? FindProgramCsPath()
    {
        return FindRepoRelativePath("source-code", "mmria", "mmria-server", "Program.cs");
    }

    private static string FindRepoRelativePath(params string[] segments)
    {
        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 10 && !string.IsNullOrWhiteSpace(current); i++)
        {
            var candidate = Path.GetFullPath(Path.Combine(new[] { current }.Concat(segments).ToArray()));
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

        throw new FileNotFoundException($"Could not locate path: {Path.Combine(segments)}");
    }

    private static string NormalizeTemplateText(string content)
    {
        return content.Replace("\r\n", "\n").TrimEnd('\n');
    }

    
}

[TestFixture]
public class TestConfigurationLoaderTests
{
    [Test]
    [Category("Configuration")]
    public void Scenario_C_ExampleOnlyConfigRequiresLocalSecrets()
    {
        using var tempDir = new TemporarySettingsDirectory();
        tempDir.WriteSettingsFile("appsettings.local.example.json", BuildExampleSettingsJson("__set_in_appsettings.local.json:test__"));

        var loader = new TestConfigurationLoader(tempDir.Path);
        loader.Load();

        Assert.That(loader.IsExampleSettingsLoaded, Is.True);
        Assert.That(loader.IsLocalSettingsLoaded, Is.False);
        Assert.That(loader.HasResolvedSensitiveSettings(), Is.False);
        Assert.That(loader.GetUnsetSensitiveSettings(), Does.Contain("mmria_settings:timer_user_name"));
        Assert.That(loader.GetUnsetSensitiveSettings(), Does.Contain("test_credentials:shared_users:primary_user_name"));
    }

    [Test]
    [Category("Configuration")]
    public void Scenario_D_LocalConfigOverridesExampleValues()
    {
        using var tempDir = new TemporarySettingsDirectory();
        tempDir.WriteSettingsFile("appsettings.local.example.json", BuildExampleSettingsJson("__set_in_appsettings.local.json:test__"));
        tempDir.WriteSettingsFile(
            "appsettings.local.json",
            @"{
  ""mmria_settings"": {
    ""timer_user_name"": ""override-timer-user"",
    ""timer_password"": ""override-timer-secret"",
    ""timer_value"": ""override-timer-secret""
  },
  ""test_credentials"": {
    ""shared_users"": {
      ""primary_user_name"": ""override-primary"",
      ""secondary_user_name"": ""override-secondary"",
      ""password"": ""override-shared-secret"",
      ""invalid_password_for_primary_user"": ""override-invalid-secret""
    },
    ""sample_credentials"": {
      ""test_harness_user_name"": ""override-harness"",
      ""test_harness_password"": ""override-harness-secret"",
      ""stub_db_user_name"": ""override-stub-user"",
      ""stub_db_password"": ""override-stub-secret"",
      ""form_url_encoded_password"": ""override-form-secret"",
      ""user_creation_password"": ""override-create-secret"",
      ""alternate_user_creation_password"": ""override-alt-secret""
    }
  }
}");

        var loader = new TestConfigurationLoader(tempDir.Path);
        loader.Load();

        Assert.That(loader.IsExampleSettingsLoaded, Is.True);
        Assert.That(loader.IsLocalSettingsLoaded, Is.True);
        Assert.That(loader.TimerUserName, Is.EqualTo("override-timer-user"));
        Assert.That(loader.TimerPassword, Is.EqualTo("override-timer-secret"));
        Assert.That(loader.TargetTestTenant, Is.EqualTo("tenant4"));
        Assert.That(loader.TestCredentials.SharedUsers.PrimaryUserName, Is.EqualTo("override-primary"));
        Assert.That(loader.TestCredentials.SampleCredentials.FormUrlEncodedPassword, Is.EqualTo("override-form-secret"));
        Assert.That(loader.HasResolvedSensitiveSettings(), Is.True);
    }

    [Test]
    [Category("Configuration")]
    public void Scenario_E_EnvironmentModeOverridesMmriaSettings()
    {
        using var tempDir = new TemporarySettingsDirectory();
        tempDir.WriteSettingsFile("appsettings.local.example.json", BuildExampleSettingsJson("__set_in_appsettings.local.json:test__"));
        tempDir.WriteSettingsFile(
            "appsettings.local.json",
            @"{
  ""test_credentials"": {
    ""shared_users"": {
      ""primary_user_name"": ""local-primary"",
      ""secondary_user_name"": ""local-secondary"",
      ""password"": ""local-shared-secret"",
      ""invalid_password_for_primary_user"": ""local-invalid-secret""
    },
    ""sample_credentials"": {
      ""test_harness_user_name"": ""local-harness"",
      ""test_harness_password"": ""local-harness-secret"",
      ""stub_db_user_name"": ""local-stub-user"",
      ""stub_db_password"": ""local-stub-secret"",
      ""form_url_encoded_password"": ""local-form-secret"",
      ""user_creation_password"": ""local-create-secret"",
      ""alternate_user_creation_password"": ""local-alt-secret""
    }
  }
}");

        var priorEnvironment = new Dictionary<string, string?>
        {
            ["is_environment_based"] = Environment.GetEnvironmentVariable("is_environment_based"),
            ["timer_user_name"] = Environment.GetEnvironmentVariable("timer_user_name"),
            ["timer_password"] = Environment.GetEnvironmentVariable("timer_password"),
            ["target_test_tenant"] = Environment.GetEnvironmentVariable("target_test_tenant"),
            ["multi_tenant_jurisdictions"] = Environment.GetEnvironmentVariable("multi_tenant_jurisdictions"),
            ["multi_tenant_template_couchdb_url"] = Environment.GetEnvironmentVariable("multi_tenant_template_couchdb_url")
        };

        try
        {
            Environment.SetEnvironmentVariable("is_environment_based", "true");
            Environment.SetEnvironmentVariable("timer_user_name", "env-timer-user");
            Environment.SetEnvironmentVariable("timer_password", "env-timer-secret");
            Environment.SetEnvironmentVariable("target_test_tenant", "env-tenant");
            Environment.SetEnvironmentVariable("multi_tenant_jurisdictions", "tenantA,tenantB");
            Environment.SetEnvironmentVariable("multi_tenant_template_couchdb_url", "http://{replace}-env-couchdb.local:6984");

            var loader = new TestConfigurationLoader(tempDir.Path);
            loader.Load();

            Assert.That(loader.TimerUserName, Is.EqualTo("env-timer-user"));
            Assert.That(loader.TimerPassword, Is.EqualTo("env-timer-secret"));
            Assert.That(loader.TargetTestTenant, Is.EqualTo("env-tenant"));
            Assert.That(loader.Tenants, Is.EqualTo(new[] { "tenantA", "tenantB" }));
            Assert.That(loader.CouchDbTemplateUrl, Is.EqualTo("http://{replace}-env-couchdb.local:6984"));
            Assert.That(loader.TestCredentials.SharedUsers.PrimaryUserName, Is.EqualTo("local-primary"));
        }
        finally
        {
            foreach (var entry in priorEnvironment)
            {
                Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            }
        }
    }

    private static string BuildExampleSettingsJson(string placeholder)
    {
        return $@"{{
  ""mmria_settings"": {{
    ""is_environment_based"": ""false"",
    ""multi_tenant_jurisdictions"": ""tenant1,tenant2,tenant3,tenant4,tenant5,cdc"",
    ""multi_tenant_shared_config_id"": ""dev_cluster"",
    ""multi_tenant_template_couchdb_url"": ""http://{{replace}}-couchdb.local:6984"",
    ""target_test_tenant"": ""tenant4"",
    ""case_lock_minutes"": ""111"",
    ""ije_number_to_generate"": ""5"",
    ""ije_jurisdication_sampling"": ""MI,AL,GA,FL"",
    ""ije_year_of_death_sampling"": ""2019,2020,2022,2023"",
    ""timer_user_name"": ""{placeholder}"",
    ""timer_password"": ""{placeholder}"",
    ""timer_value"": ""{placeholder}""
  }},
  ""test_credentials"": {{
    ""shared_users"": {{
      ""primary_user_name"": ""{placeholder}"",
      ""secondary_user_name"": ""{placeholder}"",
      ""password"": ""{placeholder}"",
      ""invalid_password_for_primary_user"": ""{placeholder}""
    }},
    ""sample_credentials"": {{
      ""test_harness_user_name"": ""{placeholder}"",
      ""test_harness_password"": ""{placeholder}"",
      ""stub_db_user_name"": ""{placeholder}"",
      ""stub_db_password"": ""{placeholder}"",
      ""form_url_encoded_password"": ""{placeholder}"",
      ""user_creation_password"": ""{placeholder}"",
      ""alternate_user_creation_password"": ""{placeholder}""
    }}
  }}
}}";
    }

    private sealed class TemporarySettingsDirectory : IDisposable
    {
        public TemporarySettingsDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mmria-test-config-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void WriteSettingsFile(string fileName, string contents)
        {
            File.WriteAllText(System.IO.Path.Combine(Path, fileName), contents);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
