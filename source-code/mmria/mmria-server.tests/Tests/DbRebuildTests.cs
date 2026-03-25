#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.server.util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace mmria_server.tests.Tests;

[TestFixture]
public sealed class DbRebuildTests
{
    private TestConfigurationLoader _configLoader = null!;
    private ActorSystem _actorSystem = null!;
    private CouchDbHttpClient _couchDbHttpClient = null!;

    [OneTimeSetUp]
    public Task OneTimeSetUpAsync()
    {
        _configLoader = new TestConfigurationLoader();
        _configLoader.Load();
        _actorSystem = ActorSystem.Create("db-rebuild-tests");
        _couchDbHttpClient = new CouchDbHttpClient(new mmria.common.SimpleHttpClientFactory());
        return Task.CompletedTask;
    }

    [SetUp]
    public void SetUp()
    {
        StartupRebuildTenantGate.ResetForTests();
        TenantRebuildCoordinator.ResetForTests();
        StartupRunSummaryCache.ClearForTests();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        if (_actorSystem != null)
        {
            await _actorSystem.Terminate();
        }
    }

    [Test]
    public void Scenario_A_TestProjectConfig_ExposesStartupRebuildSubset()
    {
        Assert.That(_configLoader.StartupRebuildTenants, Is.EqualTo(new[] { "tenant1", "tenant2", "cdc" }));
        Assert.That(_configLoader.StartupRebuildMaxConcurrentTenants, Is.EqualTo(1));
    }

    [Test]
    public void Scenario_B_BlankStartupRebuildSubset_FallsBackToConfiguredTenants()
    {
        var resolvedTenants = DbRebuildSettings.ResolveStartupRebuildTenants(
            string.Empty,
            "tenant1,tenant2,tenant3,tenant4,tenant5,cdc");

        Assert.That(
            resolvedTenants,
            Is.EqualTo(new[] { "tenant1", "tenant2", "tenant3", "tenant4", "tenant5", "cdc" }));
    }

    [Test]
    public void Scenario_C_StartupRebuildMaxConcurrentTenants_DefaultsAndClamps()
    {
        Assert.That(DbRebuildSettings.ResolveMaxConcurrentTenants(null), Is.EqualTo(1));
        Assert.That(DbRebuildSettings.ResolveMaxConcurrentTenants("0"), Is.EqualTo(1));
        Assert.That(DbRebuildSettings.ResolveMaxConcurrentTenants("-4"), Is.EqualTo(1));
        Assert.That(DbRebuildSettings.ResolveMaxConcurrentTenants("2"), Is.EqualTo(2));
    }

    [Test]
    public async Task Scenario_D_TenantGate_SerializesWhenCapacityIsOne()
    {
        StartupRebuildTenantGate.ResetForTests(1);

        using var firstLease = await StartupRebuildTenantGate.AcquireAsync(1);
        var secondLeaseTask = StartupRebuildTenantGate.AcquireAsync(1);

        await Task.Delay(150);
        Assert.That(secondLeaseTask.IsCompleted, Is.False);

        firstLease.Dispose();

        var completedTask = await Task.WhenAny(secondLeaseTask, Task.Delay(1000));
        Assert.That(completedTask, Is.SameAs(secondLeaseTask));

        using var secondLease = await secondLeaseTask;
    }

    [Test]
    public async Task Scenario_E_TenantGate_AllowsTwoConcurrentTenantsAndBlocksThird()
    {
        StartupRebuildTenantGate.ResetForTests(2);

        using var firstLease = await StartupRebuildTenantGate.AcquireAsync(2);
        using var secondLease = await StartupRebuildTenantGate.AcquireAsync(2);
        var thirdLeaseTask = StartupRebuildTenantGate.AcquireAsync(2);

        await Task.Delay(150);
        Assert.That(thirdLeaseTask.IsCompleted, Is.False);

        firstLease.Dispose();

        var completedTask = await Task.WhenAny(thirdLeaseTask, Task.Delay(1000));
        Assert.That(completedTask, Is.SameAs(thirdLeaseTask));

        using var thirdLease = await thirdLeaseTask;
    }

    [Test]
    public void Scenario_F_TenantReservation_PreventsDuplicateTenantReservations()
    {
        bool firstAcquired = TenantRebuildCoordinator.TryAcquire(
            "tenant1",
            "startup",
            "legacy",
            "queued",
            out var firstLease,
            out _);

        bool secondAcquired = TenantRebuildCoordinator.TryAcquire(
            "tenant1",
            "manual",
            "legacy",
            "queued",
            out _,
            out var existingReservation);

        Assert.That(firstAcquired, Is.True);
        Assert.That(secondAcquired, Is.False);
        Assert.That(existingReservation, Is.Not.Null);
        Assert.That(existingReservation.tenant, Is.EqualTo("tenant1"));

        firstLease.Dispose();
    }

    [Test]
    public async Task Scenario_G_Summary_UsesStartupSubsetForConfiguredTenants()
    {
        var service = CreateMultiTenantSetupService(
            loadedTenants: new[] { "tenant1", "tenant2", "tenant3", "tenant4", "tenant5", "cdc" });

        StartupRunSummaryCache.Set("cdc", CreateCachedSummary(["tenant1", "tenant2", "cdc"]));
        Assert.That(
            TenantRebuildCoordinator.TryAcquire("tenant2", "startup", "legacy", "running", out var lease, out _),
            Is.True);

        try
        {
            JObject summary = await service.GetStartupRunSummaryAsync("tenant1");

            Assert.That(
                summary["configured_tenants"]?.Values<string>().ToArray(),
                Is.EqualTo(new[] { "tenant1", "tenant2", "cdc" }));
            Assert.That(
                summary["loaded_tenants"]?.Values<string>().ToArray(),
                Is.EqualTo(new[] { "cdc", "tenant1", "tenant2", "tenant3", "tenant4", "tenant5" }));
        }
        finally
        {
            lease.Dispose();
        }
    }

    [Test]
    public async Task Scenario_H_Summary_IncludesManualReservationOutsideStartupSubset()
    {
        var service = CreateMultiTenantSetupService(
            loadedTenants: new[] { "tenant1", "tenant2", "tenant3", "tenant4", "tenant5", "cdc" });

        StartupRunSummaryCache.Set("cdc", CreateCachedSummary(["tenant1", "tenant2", "cdc"]));
        Assert.That(
            TenantRebuildCoordinator.TryAcquire("tenant5", "manual", "legacy", "running", out var lease, out _),
            Is.True);

        try
        {
            JObject summary = await service.GetStartupRunSummaryAsync("tenant1");

            Assert.That(
                summary["configured_tenants"]?.Values<string>().ToArray(),
                Is.EqualTo(new[] { "tenant1", "tenant2", "cdc", "tenant5" }));
            Assert.That(
                summary["active_rebuilds"]?.Values<JObject>().Select(item => item.Value<string>("tenant")).ToArray(),
                Does.Contain("tenant5"));
        }
        finally
        {
            lease.Dispose();
        }
    }

    private MultiTenantSetupService CreateMultiTenantSetupService(IEnumerable<string> loadedTenants)
    {
        string[] tenants = loadedTenants.ToArray();
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["mmria_settings:is_environment_based"] = "false",
            ["mmria_settings:multi_tenant_jurisdictions"] = string.Join(",", tenants),
            ["mmria_settings:multi_tenant_jurisdictions_rebuild"] = "tenant1,tenant2,cdc",
            ["mmria_settings:multi_tenant_shared_config_id"] = _configLoader.SharedConfigId ?? "dev_cluster",
            ["mmria_settings:multi_tenant_shared_config_id_template_couchdb_url"] = "http://{replace}-couchdb.local:6984",
            ["mmria_settings:multi_tenant_re_build_src"] = "cdc",
            ["mmria_settings:timer_user_name"] = _configLoader.TimerUserName ?? "mmrds",
            ["mmria_settings:timer_value"] = _configLoader.TimerPassword ?? "mmrds",
            ["mmria_settings:startup_rebuild_max_concurrent_tenants"] = "1"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var overridableConfigurations = new List<OverridableConfiguration>();
        var configurationSets = new List<ConfigurationSet>();

        foreach (string tenant in tenants)
        {
            var overridableConfiguration = new OverridableConfiguration
            {
                _id = $"{tenant}_{_configLoader.SharedConfigId ?? "dev_cluster"}"
            };

            overridableConfiguration.SetString("shared", "multi_tenant_jurisdictions", string.Join(",", tenants));
            overridableConfiguration.SetString("shared", DbRebuildSettings.StartupRebuildTenantsKey, "tenant1,tenant2,cdc");
            overridableConfiguration.SetString("shared", "multi_tenant_re_build_src", "cdc");
            overridableConfiguration.SetString("shared", "multi_tenant_shared_config_id_template_couchdb_url", "http://{replace}-couchdb.local:6984");
            overridableConfiguration.SetString(tenant, "couchdb_url", _configLoader.ResolveTenantUrl(tenant));
            overridableConfiguration.SetString(tenant, "db_prefix", string.Empty);
            overridableConfiguration.SetString(tenant, "timer_user_name", _configLoader.TimerUserName ?? "mmrds");
            overridableConfiguration.SetString(tenant, "timer_value", _configLoader.TimerPassword ?? "mmrds");

            overridableConfigurations.Add(overridableConfiguration);

            var configurationSet = new ConfigurationSet
            {
                _id = tenant
            };
            configurationSet.detail_list[tenant] = new DBConfigurationDetail
            {
                url = _configLoader.ResolveTenantUrl(tenant),
                prefix = string.Empty,
                user_name = _configLoader.TimerUserName ?? "mmrds",
                user_value = _configLoader.TimerPassword ?? "mmrds"
            };

            configurationSets.Add(configurationSet);
        }

        return new MultiTenantSetupService(
            configuration,
            overridableConfigurations,
            configurationSets,
            overridableConfigurations.First(),
            _couchDbHttpClient,
            _actorSystem,
            NullLogger<MultiTenantSetupService>.Instance);
    }

    private static JObject CreateCachedSummary(IEnumerable<string> configuredTenants)
    {
        var summary = new JObject
        {
            ["_id"] = "startup-run-summary",
            ["status"] = "running",
            ["metadata_version"] = "26.01.20",
            ["summary_host_prefix"] = "cdc",
            ["configured_tenants"] = new JArray(configuredTenants),
            ["tenant_statuses"] = new JObject(),
            ["started_utc"] = "2026-03-25T00:00:00.0000000Z",
            ["last_updated_utc"] = "2026-03-25T00:00:00.0000000Z"
        };

        return summary;
    }
}
