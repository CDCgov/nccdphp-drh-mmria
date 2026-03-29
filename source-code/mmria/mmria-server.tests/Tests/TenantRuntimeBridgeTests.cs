#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using mmria.common.couchdb;
using mmria.server;
using mmria.server.Controllers;
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
    public void ProgramSource_RemovesFallbackSingletonRegistrationsCompatibilityBridgeAndSecondServiceProvider()
    {
        var programSource = File.ReadAllText(FindRepoRelativePath("source-code", "mmria", "mmria-server", "Program.cs"));

        Assert.That(programSource, Does.Not.Contain("AddSingleton<mmria.common.couchdb.OverridableConfiguration>(overridableConfigSets[0])"));
        Assert.That(programSource, Does.Not.Contain("AddSingleton<mmria.common.couchdb.ConfigurationSet>(dbConfigSets[0])"));
        Assert.That(programSource, Does.Not.Contain("BuildServiceProvider()"));
        Assert.That(programSource, Does.Contain("AddScoped<mmria.server.util.RequestTenantRuntime>"));
        Assert.That(programSource, Does.Not.Contain("AddScoped<mmria.common.couchdb.ConfigurationSet>"));
        Assert.That(programSource, Does.Not.Contain("AddScoped<mmria.common.couchdb.OverridableConfiguration>"));
        Assert.That(programSource, Does.Not.Contain("AddScoped<mmria.common.couchdb.DBConfigurationDetail>"));
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

    [Test]
    public void CoreRequestPathSources_UseRequestTenantRuntimeInsteadOfLegacyTenantLookup()
    {
        var requestSources = new[]
        {
            FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "AccountController.cs"),
            FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "AccountController.OIDC.cs"),
            FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "api", "caseController.cs"),
            FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "api", "case_viewController.cs"),
            FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "api", "caseRevisionController.cs"),
            FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "backup_managerController.cs")
        };

        foreach (var requestSource in requestSources)
        {
            var source = File.ReadAllText(requestSource);

            Assert.That(source, Does.Contain("RequestTenantRuntime"), $"Expected RequestTenantRuntime in {requestSource}");
            Assert.That(source, Does.Not.Contain("MultiTenantConfigHelper"), $"Expected no MultiTenantConfigHelper usage in {requestSource}");
            Assert.That(source, Does.Not.Contain("List<mmria.common.couchdb.OverridableConfiguration>"), $"Expected no raw overridable-config list injection in {requestSource}");
            Assert.That(source, Does.Not.Contain("List<mmria.common.couchdb.ConfigurationSet>"), $"Expected no raw configuration-set list injection in {requestSource}");
        }

        var layoutSource = File.ReadAllText(FindRepoRelativePath("source-code", "mmria", "mmria-server", "Views", "Shared", "_Layout.cshtml"));
        Assert.That(layoutSource, Does.Contain("@inject mmria.server.util.RequestTenantRuntime"));
        Assert.That(layoutSource, Does.Not.Contain("@inject mmria.common.couchdb.OverridableConfiguration"));
    }

    [Test]
    public void RequestLayerSources_RemoveLegacyTenantResolutionPatterns()
    {
        var serverRoot = Path.GetDirectoryName(FindRepoRelativePath("source-code", "mmria", "mmria-server", "Program.cs"))!;
        var controllerFiles = Directory.GetFiles(Path.Combine(serverRoot, "Controllers"), "*.cs", SearchOption.AllDirectories);

        foreach (var controllerFile in controllerFiles)
        {
            var source = File.ReadAllText(controllerFile);
            Assert.That(source, Does.Not.Contain("MultiTenantConfigHelper"), $"Expected no legacy helper usage in {controllerFile}");
            Assert.That(source, Does.Not.Contain("List<mmria.common.couchdb.OverridableConfiguration>"), $"Expected no raw overridable-config list injection in {controllerFile}");
            Assert.That(source, Does.Not.Contain("List<mmria.common.couchdb.ConfigurationSet>"), $"Expected no raw configuration-set list injection in {controllerFile}");
        }

        var viewFiles = Directory.GetFiles(Path.Combine(serverRoot, "Views"), "*.cshtml", SearchOption.AllDirectories);
        foreach (var viewFile in viewFiles)
        {
            var source = File.ReadAllText(viewFile);
            Assert.That(source, Does.Not.Contain("@inject mmria.common.couchdb."), $"Expected no direct config injection in {viewFile}");
        }

        var helperPath = Path.Combine(serverRoot, "util", "MultiTenantConfigHelper.cs");
        if (File.Exists(helperPath))
        {
            var helperSource = File.ReadAllText(helperPath);
            Assert.That(helperSource, Does.Not.Contain("public static class MultiTenantConfigHelper"));
        }
    }

    [Test]
    public void RequestAuthAuthorizationSources_DoNotInstantiateSimpleHttpClientFactory()
    {
        var requestSources = new[]
        {
            FindRepoRelativePath("source-code", "mmria", "mmria-server", "CustomAuthHandler.cs"),
            FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "AccountController.cs"),
            FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "AccountController.OIDC.cs"),
            FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "api", "data_summary_viewController.cs"),
            FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "api", "interactive_report_viewController.cs"),
            FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "api", "user_role_jurisdictionController.cs"),
            FindRepoRelativePath("nccdphp-drh-mmria-common", "mmria.common", "SharedLibraries", "Account", "Manager", "AccountManager.cs"),
            FindRepoRelativePath("nccdphp-drh-mmria-common", "mmria.common", "SharedLibraries", "Case", "Manager", "CaseManager.cs"),
            FindRepoRelativePath("nccdphp-drh-mmria-common", "mmria.common", "SharedLibraries", "ManageUsers", "Manager", "ManageUsersManager.cs"),
            FindRepoRelativePath("nccdphp-drh-mmria-common", "mmria.common", "SharedLibraries", "OfflineCase", "Manager", "OfflineCaseManager.cs"),
            FindRepoRelativePath("nccdphp-drh-mmria-common", "mmria.common", "SharedLibraries", "VitalImport", "Manager", "VitalImportManager.cs")
        };

        foreach (var requestSource in requestSources)
        {
            var source = File.ReadAllText(requestSource);
            Assert.That(source, Does.Not.Contain("SimpleHttpClientFactory"), $"Expected no request-path SimpleHttpClientFactory usage in {requestSource}");
        }
    }

    [Test]
    public void RequestLayerControllers_ConstructWithRequestTenantRuntimeAndTenantCatalog()
    {
        var tenantRuntime = CreateRequestTenantRuntime("tenant4", "http://tenant4.test");
        var tenantCatalog = CreateMultiTenantCatalog();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        Assert.DoesNotThrow(() =>
        {
            _ = new mmria.server.Controllers.AccountController(
                httpContextAccessor,
                null!,
                tenantRuntime,
                null!,
                null!);
        });

        Assert.DoesNotThrow(() =>
        {
            _ = new mmria.common.Controllers.AccountController(
                httpContextAccessor,
                null!,
                tenantRuntime,
                null!);
        });

        Assert.DoesNotThrow(() =>
        {
            _ = new caseController(
                tenantRuntime,
                null!,
                null!,
                null!,
                null!);
        });

        Assert.DoesNotThrow(() =>
        {
            _ = new case_viewController(
                tenantRuntime,
                null!);
        });

        Assert.DoesNotThrow(() =>
        {
            _ = new caseRevisionController(
                tenantRuntime,
                tenantCatalog,
                null!,
                null!,
                null!);
        });

        Assert.DoesNotThrow(() =>
        {
            _ = new backupManagerController(
                NullLogger<backupManagerController>.Instance,
                tenantRuntime,
                null!,
                null!);
        });

        Assert.DoesNotThrow(() =>
        {
            _ = new caseRevisionListController(
                tenantRuntime,
                tenantCatalog,
                null!);
        });

        Assert.DoesNotThrow(() =>
        {
            _ = new caseRevisionList_case_viewController(
                tenantRuntime,
                tenantCatalog,
                null!);
        });

        Assert.DoesNotThrow(() =>
        {
            _ = new VitalsImport_FileUpload.Controllers.vitalsController(
                NullLogger<VitalsImport_FileUpload.Controllers.vitalsController>.Instance,
                httpContextAccessor,
                tenantRuntime,
                tenantCatalog,
                null!);
        });

        Assert.DoesNotThrow(() =>
        {
            _ = new OfflineCaseController(
                httpContextAccessor,
                null!,
                null!,
                null!,
                tenantRuntime);
        });
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

    private static RequestTenantRuntime CreateRequestTenantRuntime(string hostPrefix, string url)
    {
        var configuration = CreateConfiguration($"{hostPrefix}_shared", hostPrefix, url);
        var configurationSet = CreateConfigurationSet(hostPrefix, url);
        return new RequestTenantRuntime(
            hostPrefix,
            configuration,
            configurationSet,
            configurationSet.detail_list[hostPrefix],
            isTenantAvailable: true);
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
