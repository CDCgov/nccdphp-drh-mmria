#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using DbRebuildSettings = mmria.server.util.DbRebuildSettings;
using StartupRebuildTenantGate = mmria.server.util.StartupRebuildTenantGate;
using StartupRunSummaryCache = mmria.server.util.StartupRunSummaryCache;
using TenantRebuildCoordinator = mmria.server.util.TenantRebuildCoordinator;
using mmria.common.couchdb;
using mmria.common.SharedLibraries.MMRIARebuild.DAL;
using mmria.common.SharedLibraries.MMRIARebuild.Manager;
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

    [OneTimeSetUp]
    public Task OneTimeSetUpAsync()
    {
        _configLoader = new TestConfigurationLoader();
        _configLoader.Load();
        _actorSystem = ActorSystem.Create("db-rebuild-tests");
        return Task.CompletedTask;
    }

    [SetUp]
    public void SetUp()
    {
        StartupRebuildTenantGate.ResetForTests();
        TenantRebuildCoordinator.ResetForTests();
        StartupRunSummaryCache.ClearForTests();
        StartupRunSummaryUpdateGate.ResetForTests();
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
    public async Task Scenario_E1_SummaryUpdateGate_SerializesWritesPerSummaryHost()
    {
        using var firstLease = await StartupRunSummaryUpdateGate.AcquireAsync("cdc");
        var secondLeaseTask = StartupRunSummaryUpdateGate.AcquireAsync("cdc");

        await Task.Delay(150);
        Assert.That(secondLeaseTask.IsCompleted, Is.False);

        firstLease.Dispose();

        var completedTask = await Task.WhenAny(secondLeaseTask, Task.Delay(1000));
        Assert.That(completedTask, Is.SameAs(secondLeaseTask));

        using var secondLease = await secondLeaseTask;
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
        var summaryDocument = CreateServiceOwnedSummary(
            configuredTenants: ["tenant1", "tenant2", "cdc"],
            tenantStatuses:
            [
                ("tenant1", "completed"),
                ("tenant2", "running"),
                ("cdc", "queued")
            ]);

        var service = CreateMultiTenantSetupService(
            loadedTenants: ["tenant1", "tenant2", "tenant3", "tenant4", "tenant5", "cdc"],
            httpClientFactory: CreateSummaryHttpClientFactory(summaryDocument));

        JObject summary = await service.GetStartupRunSummaryAsync("tenant1");

        Assert.That(
            summary["configured_tenants"]?.Values<string>().ToArray(),
            Is.EqualTo(new[] { "tenant1", "tenant2", "cdc" }));
        Assert.That(
            summary["loaded_tenants"]?.Values<string>().ToArray(),
            Is.EqualTo(new[] { "cdc", "tenant1", "tenant2", "tenant3", "tenant4", "tenant5" }));
    }

    [Test]
    public async Task Scenario_H_Summary_IncludesManualTenantWhenServiceSummaryContainsIt()
    {
        var summaryDocument = CreateServiceOwnedSummary(
            configuredTenants: ["tenant1", "tenant2", "cdc"],
            tenantStatuses:
            [
                ("tenant1", "completed"),
                ("tenant2", "completed"),
                ("cdc", "queued"),
                ("tenant5", "running")
            ]);

        var service = CreateMultiTenantSetupService(
            loadedTenants: ["tenant1", "tenant2", "tenant3", "tenant4", "tenant5", "cdc"],
            httpClientFactory: CreateSummaryHttpClientFactory(summaryDocument));

        JObject summary = await service.GetStartupRunSummaryAsync("tenant1");

        Assert.That(
            summary["configured_tenants"]?.Values<string>().ToArray(),
            Is.EqualTo(new[] { "tenant1", "tenant2", "cdc", "tenant5" }));
        Assert.That(
            summary["active_rebuilds"]?.Values<JObject>().Select(item => item.Value<string>("tenant")).ToArray(),
            Does.Contain("tenant5"));
    }

    [Test]
    public async Task Scenario_I_ManualRebuild_PostsToServiceEndpoint()
    {
        string observedUrl = null;
        string observedHeader = null;
        string observedBody = null;

        var httpClientFactory = new StubHttpClientFactory(request =>
        {
            observedUrl = request.RequestUri?.ToString();
            observedHeader = request.Headers.TryGetValues("vital-service-key", out var values)
                ? values.SingleOrDefault()
                : null;
            observedBody = request.Content == null
                ? null
                : request.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == "/api/MMRIARebuild")
            {
                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "status_code": 202,
                      "tenant": "tenant5",
                      "source": "manual",
                      "message": "Started a fresh rebuild for tenant 'tenant5'.",
                      "rebuild_started": true
                    }
                    """);
            }

            return CreateJsonResponse(HttpStatusCode.OK, "{\"error\":\"not_found\"}");
        });

        var service = CreateMultiTenantSetupService(
            loadedTenants: ["tenant5", "cdc"],
            httpClientFactory: httpClientFactory);

        MultiTenantSetupResult result = await service.RebuildTenantAsync("tenant5");

        Assert.That(result.success, Is.True);
        Assert.That(result.status_code, Is.EqualTo(202));
        Assert.That(result.rebuild_started, Is.True);
        Assert.That(observedUrl, Is.EqualTo("http://tenant5.services.test/api/MMRIARebuild"));
        Assert.That(observedHeader, Is.EqualTo("service-key-tenant5"));
        Assert.That(observedBody, Does.Contain("\"tenant\":\"tenant5\""));
        Assert.That(observedBody, Does.Contain("\"source\":\"manual\""));
    }

    private MultiTenantSetupService CreateMultiTenantSetupService(
        IEnumerable<string> loadedTenants,
        IHttpClientFactory httpClientFactory = null)
    {
        string[] tenants = loadedTenants.ToArray();
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["mmria_settings:is_environment_based"] = "false",
            ["mmria_settings:multi_tenant_jurisdictions"] = string.Join(",", tenants),
            ["mmria_settings:multi_tenant_jurisdictions_rebuild"] = "tenant1,tenant2,cdc",
            ["mmria_settings:multi_tenant_shared_config_id"] = _configLoader.SharedConfigId ?? "dev_cluster",
            ["mmria_settings:multi_tenant_shared_config_id_template_couchdb_url"] = "http://{replace}.test",
            ["mmria_settings:multi_tenant_re_build_src"] = "cdc",
            ["mmria_settings:timer_user_name"] = _configLoader.TimerUserName ?? "mmrds",
            ["mmria_settings:timer_value"] = _configLoader.TimerPassword ?? "mmrds",
            ["mmria_settings:startup_rebuild_max_concurrent_tenants"] = "1",
            ["mmria_settings:metadata_version"] = "26.01.20"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        httpClientFactory ??= new StubHttpClientFactory(_ => CreateJsonResponse(HttpStatusCode.OK, "{\"error\":\"not_found\"}"));
        var couchDbHttpClient = new CouchDbHttpClient(httpClientFactory);
        var overridableConfigurations = new List<OverridableConfiguration>();
        var configurationSets = new List<ConfigurationSet>();
        var combinedConfigurationSet = new ConfigurationSet
        {
            _id = "shared"
        };

        foreach (string tenant in tenants)
        {
            string tenantUrl = $"http://{tenant}.test";
            var overridableConfiguration = new OverridableConfiguration
            {
                _id = $"{tenant}_{_configLoader.SharedConfigId ?? "dev_cluster"}"
            };

            overridableConfiguration.SetString("shared", "multi_tenant_jurisdictions", string.Join(",", tenants));
            overridableConfiguration.SetString("shared", DbRebuildSettings.StartupRebuildTenantsKey, "tenant1,tenant2,cdc");
            overridableConfiguration.SetString("shared", "multi_tenant_re_build_src", "cdc");
            overridableConfiguration.SetString("shared", "multi_tenant_shared_config_id_template_couchdb_url", "http://{replace}.test");
            overridableConfiguration.SetString(tenant, "couchdb_url", tenantUrl);
            overridableConfiguration.SetString(tenant, "db_prefix", string.Empty);
            overridableConfiguration.SetString(tenant, "timer_user_name", _configLoader.TimerUserName ?? "mmrds");
            overridableConfiguration.SetString(tenant, "timer_value", _configLoader.TimerPassword ?? "mmrds");
            overridableConfiguration.SetString(tenant, "vitals_url", $"http://{tenant}.services.test/api/Message/IJESet");
            overridableConfiguration.SetString(tenant, "vital_service_key", $"service-key-{tenant}");

            overridableConfigurations.Add(overridableConfiguration);

            var configurationSet = new ConfigurationSet
            {
                _id = tenant
            };

            var detail = new DBConfigurationDetail
            {
                url = tenantUrl,
                prefix = string.Empty,
                user_name = _configLoader.TimerUserName ?? "mmrds",
                user_value = _configLoader.TimerPassword ?? "mmrds"
            };

            configurationSet.detail_list[tenant] = detail;
            configurationSets.Add(configurationSet);
            combinedConfigurationSet.detail_list[tenant] = detail;
        }

        combinedConfigurationSet.name_value["metadata_version"] = "26.01.20";

        var rebuildManager = new MMRIARebuildManager(
            new MMRIARebuildDAL(couchDbHttpClient),
            couchDbHttpClient,
            configuration,
            combinedConfigurationSet);

        return new MultiTenantSetupService(
            configuration,
            overridableConfigurations,
            configurationSets,
            overridableConfigurations.First(),
            couchDbHttpClient,
            _actorSystem,
            rebuildManager,
            NullLogger<MultiTenantSetupService>.Instance);
    }

    private static StubHttpClientFactory CreateSummaryHttpClientFactory(JObject summaryDocument)
    {
        return new StubHttpClientFactory(request =>
        {
            if (request.Method == HttpMethod.Get &&
                request.RequestUri?.AbsolutePath.EndsWith("/db_rebuild/startup-run-summary", StringComparison.OrdinalIgnoreCase) == true)
            {
                return CreateJsonResponse(HttpStatusCode.OK, summaryDocument.ToString());
            }

            return CreateJsonResponse(HttpStatusCode.OK, "{\"error\":\"not_found\"}");
        });
    }

    private static JObject CreateServiceOwnedSummary(
        IEnumerable<string> configuredTenants,
        IEnumerable<(string Tenant, string Status)> tenantStatuses)
    {
        var tenantStatusObject = new JObject();

        foreach (var (tenant, status) in tenantStatuses)
        {
            tenantStatusObject[tenant] = new JObject
            {
                ["host_prefix"] = tenant,
                ["status"] = status,
                ["started_utc"] = "2026-03-25T00:00:00.0000000Z",
                ["last_updated_utc"] = "2026-03-25T00:00:00.0000000Z"
            };
        }

        return new JObject
        {
            ["_id"] = "startup-run-summary",
            ["status"] = "running",
            ["metadata_version"] = "26.01.20",
            ["summary_host_prefix"] = "cdc",
            ["configured_tenants"] = new JArray(configuredTenants),
            ["tenant_statuses"] = tenantStatusObject,
            ["started_utc"] = "2026-03-25T00:00:00.0000000Z",
            ["last_updated_utc"] = "2026-03-25T00:00:00.0000000Z"
        };
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new StubHttpMessageHandler(_responseFactory));
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
