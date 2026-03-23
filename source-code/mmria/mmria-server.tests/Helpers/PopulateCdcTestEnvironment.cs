#nullable enable

using System;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.MMRIAServices.DAL;
using mmria.common.SharedLibraries.MMRIAServices.Manager;
using mmria.common.couchdb;
using mmria.common.getset;
using NUnit.Framework;

namespace mmria_server.tests.Helpers;

public sealed class PopulateCdcTestEnvironmentConfig
{
    public required ConfigurationSet CentralConfiguration { get; init; }
    public required DBConfigurationDetail CdcDbConfiguration { get; init; }
    public required string CdcDbKey { get; init; }
    public required TestConfigurationLoader ConfigLoader { get; init; }
}

public sealed class PopulateCdcTestEnvironment
{
    public CouchDbHttpClient CouchDbClient { get; }

    public PopulateCdcTestEnvironmentConfig? Config { get; private set; }

    private PopulateCdcTestEnvironment(CouchDbHttpClient couchDbClient)
    {
        CouchDbClient = couchDbClient;
    }

    public static async Task<PopulateCdcTestEnvironment> BootstrapAsync()
    {
        var configLoader = new TestConfigurationLoader();
        configLoader.Load();

        if (!configLoader.HasResolvedSensitiveSettings())
        {
            Assert.Inconclusive(configLoader.GetSensitiveSettingsSetupMessage());
        }

        var couchDbClient = new CouchDbHttpClient(new mmria.common.SimpleHttpClientFactory());
        string centralUrl = configLoader.CentralCouchDbUrl.TrimEnd('/');

        try
        {
            var response = await couchDbClient.ExecuteAsync(
                "GET",
                $"{centralUrl}/",
                null,
                configLoader.TimerUserName,
                configLoader.TimerPassword);

            if (string.IsNullOrWhiteSpace(response))
            {
                Assert.Inconclusive("Central CouchDB ping returned an empty response.");
            }
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"Central CouchDB is not accessible at '{centralUrl}'. Error: {ex.Message}");
        }

        TestContext.WriteLine($"PopulateCdcTestEnvironment bootstrapped. Central CouchDB: {centralUrl}");
        return new PopulateCdcTestEnvironment(couchDbClient);
    }

    public PopulateCdcTestEnvironmentConfig ResolveConfiguration()
    {
        var configLoader = new TestConfigurationLoader();
        configLoader.Load();

        if (!configLoader.HasResolvedSensitiveSettings())
        {
            Assert.Inconclusive(configLoader.GetSensitiveSettingsSetupMessage());
        }

        var manager = new MMRIAServicesManager(new MMRIAServicesDAL(CouchDbClient), CouchDbClient);
        var configSet = manager.GetConfiguration(
            configLoader.CentralCouchDbUrl,
            configLoader.CdcInstanceConfigId,
            configLoader.TimerUserName ?? string.Empty,
            configLoader.TimerPassword ?? string.Empty);

        Assert.That(configSet, Is.Not.Null, "CDC instance ConfigurationSet should load from central CouchDB.");
        Assert.That(configSet.detail_list, Is.Not.Null, "ConfigurationSet.detail_list should not be null.");

        string cdcKey = configSet.detail_list.ContainsKey("cdc") ? "cdc" : "cdcqa";
        Assert.That(configSet.detail_list.ContainsKey(cdcKey), Is.True,
            "ConfigurationSet.detail_list should contain either 'cdc' or 'cdcqa'.");

        var cdcDbConfig = configSet.detail_list[cdcKey];
        Assert.That(string.IsNullOrWhiteSpace(cdcDbConfig.url), Is.False,
            $"Resolved CDC connection '{cdcKey}' must include a CouchDB url.");

        Config = new PopulateCdcTestEnvironmentConfig
        {
            CentralConfiguration = configSet,
            CdcDbConfiguration = cdcDbConfig,
            CdcDbKey = cdcKey,
            ConfigLoader = configLoader
        };

        return Config;
    }
}
