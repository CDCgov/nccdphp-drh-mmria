#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.MMRIAServices.DAL;
using mmria.common.SharedLibraries.MMRIAServices.Manager;
using mmria_server.tests.Helpers;
using NUnit.Framework;

namespace mmria_server.tests.Tests;

[TestFixture]
public sealed class PopulateCDCInstanceTests
{
    private PopulateCdcTestEnvironment _env = null!;

    [OneTimeSetUp]
    public async System.Threading.Tasks.Task OneTimeSetUpAsync()
    {
        _env = await PopulateCdcTestEnvironment.BootstrapAsync();
    }

    [SetUp]
    public void SetUp()
    {
        _env.ResolveConfiguration();
    }

    [Test]
    [Category("PopulateCDC")]
    public void Scenario_A_LoadsCdcInstanceConfigurationFromCentralCouchDb()
    {
        var cfg = _env.Config!;

        
        Assert.That(string.IsNullOrWhiteSpace(cfg.ConfigLoader.CentralCouchDbUrl), Is.False,
            "central_couchdb_url must be configured.");
        Assert.That(string.IsNullOrWhiteSpace(cfg.ConfigLoader.TimerUserName), Is.False,
            "timer_user_name must be configured for CDC instance bootstrap.");
        Assert.That(string.IsNullOrWhiteSpace(cfg.ConfigLoader.TimerPassword), Is.False,
            "timer_password (or timer_value fallback) must be configured for CDC instance bootstrap.");
        Assert.That(string.IsNullOrWhiteSpace(cfg.ConfigLoader.CdcInstanceConfigId), Is.False,
            "cdc_instance_config_id must be configured.");

        Assert.That(cfg.CentralConfiguration._id, Is.EqualTo(cfg.ConfigLoader.CdcInstanceConfigId),
            "Configuration should be loaded with cdc_instance_config_id.");
        Assert.That(string.Equals(cfg.CdcDbKey, "cdc", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(cfg.CdcDbKey, "cdcqa", StringComparison.OrdinalIgnoreCase), Is.True,
            "Resolved CDC db key should be 'cdc' or 'cdcqa'.");
        Assert.That(string.IsNullOrWhiteSpace(cfg.CdcDbConfiguration.user_name), Is.False,
            "CDC DB configuration should include a username.");
    }

    [Test]
    [Category("PopulateCDC")]
    public async Task Scenario_B_AllSourceDocumentIdsExistInCdcMmrdsDatabase()
    {
        var cfg = _env.Config!;
        var manager = new MMRIAServicesManager(new MMRIAServicesDAL(_env.CouchDbClient), _env.CouchDbClient);
        var message = BuildPopulateCdcMessage();

        LogStatus("Starting Populate CDC manager run...");

        var (name, _) = await manager.PopulateCDCInstanceManger(message, cfg.CentralConfiguration);
        Assert.That(name, Is.EqualTo("Finished"), "Populate CDC manager should complete before ID validation.");

        LogStatus("Populate CDC manager finished. Collecting source and CDC document IDs...");

        var sourceDocumentIds = await GetSourceDocumentIdsAsync(message, cfg.CentralConfiguration);
        var cdcDocumentIds = await GetDocumentIdsFromDatabaseAsync(cfg.CdcDbConfiguration, usePrefixedMmrds: false);

        var missingInCdc = sourceDocumentIds
            .Except(cdcDocumentIds, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int sourceTotal = sourceDocumentIds.Count;
        int cdcTotal = cdcDocumentIds.Count;
        int unmatched = missingInCdc.Count;
        int matched = sourceTotal - unmatched;

        LogStatus($"Source total documents: {sourceTotal}");
        LogStatus($"CDC total documents: {cdcTotal}");
        LogStatus($"Matched document IDs: {matched}");
        LogStatus($"Unmatched document IDs: {unmatched}");

        if (missingInCdc.Count > 0)
        {
            LogStatus($"Missing IDs (first 25): {string.Join(", ", missingInCdc.Take(25))}");
        }

        Assert.That(missingInCdc, Is.Empty,
            $"All source document IDs should exist in CDC mmrds. Missing count: {missingInCdc.Count}. Missing: {string.Join(", ", missingInCdc.Take(25))}");
    }

    private static mmria.common.metadata.Populate_CDC_Instance BuildPopulateCdcMessage()
    {
        return new mmria.common.metadata.Populate_CDC_Instance
        {
            state_list =
            [
                new mmria.common.metadata.State_List_Item { is_included = true, prefix = "tenant1", name = "tenant 1 test site" },
                new mmria.common.metadata.State_List_Item { is_included = true, prefix = "tenant2", name = "tenant 2 test site" },
                new mmria.common.metadata.State_List_Item { is_included = true, prefix = "tenant3", name = "tenant 3 test site" },
                new mmria.common.metadata.State_List_Item { is_included = true, prefix = "tenant4", name = "tenant 4 test site" },
                new mmria.common.metadata.State_List_Item { is_included = true, prefix = "tenant5", name = "tenant 5 test site" }
            ]
        };
    }

    private async Task<HashSet<string>> GetSourceDocumentIdsAsync(
        mmria.common.metadata.Populate_CDC_Instance message,
        mmria.common.couchdb.ConfigurationSet configSet)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var state in message.state_list)
        {
            if (state?.is_included != true || string.IsNullOrWhiteSpace(state.prefix))
            {
                continue;
            }

            if (!configSet.detail_list.ContainsKey(state.prefix))
            {
                continue;
            }

            var sourceDbInfo = configSet.detail_list[state.prefix];
            var sourceIds = await GetDocumentIdsFromDatabaseAsync(sourceDbInfo, usePrefixedMmrds: true);
            LogStatus($"Source prefix '{state.prefix}' document count: {sourceIds.Count}");

            foreach (var id in sourceIds)
            {
                result.Add(id);
            }
        }

        return result;
    }

    private async Task<HashSet<string>> GetDocumentIdsFromDatabaseAsync(
        mmria.common.couchdb.DBConfigurationDetail dbInfo,
        bool usePrefixedMmrds)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string dbUrl = usePrefixedMmrds && !string.IsNullOrWhiteSpace(dbInfo.prefix)
            ? $"{dbInfo.url}/{dbInfo.prefix}_mmrds"
            : $"{dbInfo.url}/mmrds";

        string allDocsUrl = $"{dbUrl}/_all_docs?include_docs=false";
        string responseFromServer = await _env.CouchDbClient.ExecuteAsync(
            "GET",
            allDocsUrl,
            null,
            dbInfo.user_name,
            dbInfo.user_value,
            timeoutSeconds: 300);

        var allDocsResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.alldocs_response<object>>(responseFromServer);
        if (allDocsResponse?.rows == null)
        {
            return result;
        }

        foreach (var row in allDocsResponse.rows)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.id))
            {
                continue;
            }

            if (row.id.StartsWith("_design", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(row.id);
        }

        return result;
    }

    private static void LogStatus(string message)
    {
        var line = $"[PopulateCDCInstanceTests] {message}";
        Console.WriteLine(line);
        TestContext.WriteLine(line);
    }
}
