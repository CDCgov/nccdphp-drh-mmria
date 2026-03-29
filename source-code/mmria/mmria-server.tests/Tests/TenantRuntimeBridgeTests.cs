#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using mmria.common.couchdb;
using mmria.server.util;
using NUnit.Framework;

namespace mmria_server.tests.Tests;

[TestFixture]
public sealed class TenantRuntimeBridgeTests
{
    [Test]
    public void TenantCatalog_ResolvesSingleTenantConfigurationAndDbConfig()
    {
        var rootRuntimeSettings = new RootRuntimeSettings
        {
            IsMultiTenantMode = false,
            SingleTenantName = "single"
        };

        var configuration = CreateConfiguration("single_shared", "single", "http://single.test");
        var configurationSet = CreateConfigurationSet("single", "http://single.test");
        var tenantCatalog = new TenantCatalog(
            rootRuntimeSettings,
            new List<OverridableConfiguration> { configuration },
            new List<ConfigurationSet> { configurationSet });

        var resolvedConfiguration = tenantCatalog.TryResolveConfiguration("anything");
        var resolvedDbConfig = tenantCatalog.TryResolveDbConfig("anything");

        Assert.That(resolvedConfiguration?._id, Is.EqualTo("single_shared"));
        Assert.That(resolvedDbConfig?.url, Is.EqualTo("http://single.test"));
        Assert.That(tenantCatalog.IsTenantAvailable("anything"), Is.True);
    }

    [Test]
    public void TenantCatalog_ResolvesKnownMultiTenantConfigurationAndDbConfig()
    {
        var tenantCatalog = CreateMultiTenantCatalog();

        var resolvedConfiguration = tenantCatalog.TryResolveConfiguration("tenant5");
        var resolvedDbConfig = tenantCatalog.TryResolveDbConfig("tenant5");

        Assert.That(resolvedConfiguration?._id, Is.EqualTo("tenant5_shared"));
        Assert.That(resolvedDbConfig?.url, Is.EqualTo("http://tenant5.test"));
        Assert.That(tenantCatalog.IsTenantAvailable("tenant5"), Is.True);
    }

    [Test]
    public void TenantCatalog_RejectsUnknownTenantInMultiTenantMode()
    {
        var tenantCatalog = CreateMultiTenantCatalog();

        Assert.That(tenantCatalog.TryResolveConfiguration("unknown"), Is.Null);
        Assert.That(tenantCatalog.TryResolveDbConfig("unknown"), Is.Null);
        Assert.That(tenantCatalog.IsTenantAvailable("unknown"), Is.False);
    }

    [Test]
    public void RequestTenantRuntime_UsesCatalogResolution()
    {
        var tenantCatalog = CreateMultiTenantCatalog();
        var runtime = new RequestTenantRuntime(
            "tenant4",
            tenantCatalog.TryResolveConfiguration("tenant4"),
            tenantCatalog.TryResolveConfigurationSet("tenant4"),
            tenantCatalog.TryResolveDbConfig("tenant4"),
            tenantCatalog.IsTenantAvailable("tenant4"));

        Assert.That(runtime.HostPrefix, Is.EqualTo("tenant4"));
        Assert.That(runtime.Configuration?._id, Is.EqualTo("tenant4_shared"));
        Assert.That(runtime.ConfigurationSet?._id, Is.EqualTo("tenant4"));
        Assert.That(runtime.DbConfig?.url, Is.EqualTo("http://tenant4.test"));
        Assert.That(runtime.IsTenantAvailable, Is.True);
    }

    [Test]
    public void ProgramSource_RemovesFallbackSingletonRegistrationsAndSecondServiceProvider()
    {
        var programSource = File.ReadAllText(FindRepoRelativePath("source-code", "mmria", "mmria-server", "Program.cs"));

        Assert.That(programSource, Does.Not.Contain("AddSingleton<mmria.common.couchdb.OverridableConfiguration>(overridableConfigSets[0])"));
        Assert.That(programSource, Does.Not.Contain("AddSingleton<mmria.common.couchdb.ConfigurationSet>(dbConfigSets[0])"));
        Assert.That(programSource, Does.Not.Contain("BuildServiceProvider()"));
        Assert.That(programSource, Does.Contain("AddScoped<mmria.server.util.RequestTenantRuntime>"));
        Assert.That(programSource, Does.Contain("AddScoped<mmria.common.couchdb.ConfigurationSet>"));
        Assert.That(programSource, Does.Contain("AddSingleton<mmria.server.util.TenantCatalog>"));
        Assert.That(programSource, Does.Contain("LoadRequiredOverridableConfigurationsAsync("));
        Assert.That(programSource, Does.Contain("LoadRequiredConfigurationSetsAsync("));
        Assert.That(programSource, Does.Contain("AddSingleton<mmria.common.SharedLibraries.MMRIARebuild.Manager.MMRIARebuildManager>(serviceProvider =>"));
    }

    [Test]
    public void ServicesProgramSource_UsesSingleProviderAndExplicitRebuildManagerFactory()
    {
        var programSource = File.ReadAllText(FindRepoRelativePath("nccdphp-drh-mmria-services", "mmria.services", "Program.cs"));

        Assert.That(programSource, Does.Not.Contain("BuildServiceProvider()"));
        Assert.That(programSource, Does.Not.Contain("new mmria.common.couchdb.ConfigurationSet()"));
        Assert.That(programSource, Does.Contain("LoadRequiredConfigurationSetsAsync("));
        Assert.That(programSource, Does.Contain("AddSingleton<ActorSystem>(serviceProvider =>"));
        Assert.That(programSource, Does.Contain("GetRequiredService<ActorSystem>()"));
        Assert.That(programSource, Does.Contain("new MMRIARebuildManager("));
    }

    private static TenantCatalog CreateMultiTenantCatalog()
    {
        var rootRuntimeSettings = new RootRuntimeSettings
        {
            IsMultiTenantMode = true,
            ConfiguredTenants = ["tenant4", "tenant5"],
            SharedConfigId = "shared"
        };

        return new TenantCatalog(
            rootRuntimeSettings,
            new List<OverridableConfiguration>
            {
                CreateConfiguration("tenant4_shared", "tenant4", "http://tenant4.test"),
                CreateConfiguration("tenant5_shared", "tenant5", "http://tenant5.test")
            },
            new List<ConfigurationSet>
            {
                CreateConfigurationSet("tenant4", "http://tenant4.test"),
                CreateConfigurationSet("tenant5", "http://tenant5.test")
            });
    }

    private static OverridableConfiguration CreateConfiguration(string documentId, string tenant, string url)
    {
        var configuration = new OverridableConfiguration
        {
            _id = documentId
        };

        configuration.SetString(tenant, "couchdb_url", url);
        configuration.SetString(tenant, "db_prefix", string.Empty);
        configuration.SetString(tenant, "timer_user_name", "tester");
        configuration.SetString(tenant, "timer_value", "secret");
        return configuration;
    }

    private static ConfigurationSet CreateConfigurationSet(string tenant, string url)
    {
        var configurationSet = new ConfigurationSet
        {
            _id = tenant
        };

        configurationSet.detail_list[tenant] = new DBConfigurationDetail
        {
            url = url,
            prefix = string.Empty,
            user_name = "tester",
            user_value = "secret"
        };

        return configurationSet;
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
}
