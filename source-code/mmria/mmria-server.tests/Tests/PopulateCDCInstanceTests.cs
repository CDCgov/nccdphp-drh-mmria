#nullable enable

extern alias services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using mmria.common.SharedLibraries.MMRIAServices.DAL;
using mmria.common.SharedLibraries.MMRIAServices.Helper;
using mmria.common.SharedLibraries.MMRIAServices.Manager;
using mmria.common.SharedLibraries.MMRIAServices.Model;
using mmria_server.tests.Helpers;
using Newtonsoft.Json.Linq;
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

    [Test]
    [Category("PopulateCDC")]
    public async Task Scenario_C_EditLockFields_AreRemovedFromCdcCopy()
    {
        var cfg = _env.Config!;
        var manager = new MMRIAServicesManager(new MMRIAServicesDAL(_env.CouchDbClient), _env.CouchDbClient);
        var sourceDbInfo = cfg.CentralConfiguration.detail_list["tenant1"];
        var message = BuildPopulateCdcMessage(["tenant1"]);
        var caseId = await GetAnyCaseIdAsync(sourceDbInfo, usePrefixedMmrds: true);
        var originalSourceDoc = await GetDocumentAsync(sourceDbInfo, caseId, usePrefixedMmrds: true);
        var patchedSourceDoc = (JObject)originalSourceDoc.DeepClone();

        patchedSourceDoc["date_last_checked_out"] = DateTime.UtcNow;
        patchedSourceDoc["last_checked_out_by"] = "populate-cdc-test-user";
        patchedSourceDoc["checked_out_by_tab_id"] = "populate-cdc-test-tab";

        try
        {
            await SaveDocumentAsync(sourceDbInfo, patchedSourceDoc, usePrefixedMmrds: true);

            var (name, _) = await manager.PopulateCDCInstanceManger(message, cfg.CentralConfiguration);
            Assert.That(name, Is.EqualTo("Finished"));

            var cdcDoc = await GetDocumentAsync(cfg.CdcDbConfiguration, caseId, usePrefixedMmrds: false);
            AssertLockFieldRemoved(cdcDoc, "date_last_checked_out");
            AssertLockFieldRemoved(cdcDoc, "last_checked_out_by");
            AssertLockFieldRemoved(cdcDoc, "checked_out_by_tab_id");
        }
        finally
        {
            await SaveDocumentAsync(sourceDbInfo, originalSourceDoc, usePrefixedMmrds: true);
        }
    }

    [Test]
    [Category("PopulateCDC")]
    public async Task Scenario_D_OfflineLockFields_AreRemovedFromCdcCopy()
    {
        var cfg = _env.Config!;
        var manager = new MMRIAServicesManager(new MMRIAServicesDAL(_env.CouchDbClient), _env.CouchDbClient);
        var sourceDbInfo = cfg.CentralConfiguration.detail_list["tenant1"];
        var message = BuildPopulateCdcMessage(["tenant1"]);
        var caseId = await GetAnyCaseIdAsync(sourceDbInfo, usePrefixedMmrds: true);
        var originalSourceDoc = await GetDocumentAsync(sourceDbInfo, caseId, usePrefixedMmrds: true);
        var patchedSourceDoc = (JObject)originalSourceDoc.DeepClone();

        patchedSourceDoc["is_offline"] = true;
        patchedSourceDoc["offline_by"] = "populate-cdc-test-user";
        patchedSourceDoc["offline_lock_type"] = 2;
        patchedSourceDoc["offline_by_tab_id"] = "populate-cdc-test-tab";

        try
        {
            await SaveDocumentAsync(sourceDbInfo, patchedSourceDoc, usePrefixedMmrds: true);

            var (name, _) = await manager.PopulateCDCInstanceManger(message, cfg.CentralConfiguration);
            Assert.That(name, Is.EqualTo("Finished"));

            var cdcDoc = await GetDocumentAsync(cfg.CdcDbConfiguration, caseId, usePrefixedMmrds: false);
            AssertLockFieldRemoved(cdcDoc, "is_offline");
            AssertLockFieldRemoved(cdcDoc, "offline_by");
            AssertLockFieldRemoved(cdcDoc, "offline_lock_type");
            AssertLockFieldRemoved(cdcDoc, "offline_by_tab_id");
        }
        finally
        {
            await SaveDocumentAsync(sourceDbInfo, originalSourceDoc, usePrefixedMmrds: true);
        }
    }

    [Test]
    [Category("PopulateCDC")]
    public async Task ClearDatabaseDocumentsPreservingSystemDocsAsync_DeletesOnlyNonSystemDocs()
    {
        var cfg = _env.Config!;
        string reportDatabaseUrl = $"{cfg.CdcDbConfiguration.url.TrimEnd('/')}/report";
        string bulkDeleteBody = string.Empty;
        int bulkDeleteCallCount = 0;

        var handler = new RecordingHttpMessageHandler(async request =>
        {
            string url = request.RequestUri!.ToString();
            string method = request.Method.Method.ToUpperInvariant();
            string body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync();

            if (method == "HEAD" && url == reportDatabaseUrl)
            {
                return CreateJsonResponse(@"{ ""ok"": true }");
            }

            if (method == "GET" && url.StartsWith($"{reportDatabaseUrl}/_all_docs?", StringComparison.OrdinalIgnoreCase))
            {
                return CreateJsonResponse(@"{
                    ""total_rows"": 4,
                    ""offset"": 0,
                    ""rows"": [
                        { ""id"": ""_design/data_summary_view_report"", ""key"": ""_design/data_summary_view_report"", ""value"": { ""rev"": ""1-a"" } },
                        { ""id"": ""_local/report-cache"", ""key"": ""_local/report-cache"", ""value"": { ""rev"": ""1-b"" } },
                        { ""id"": ""case-1"", ""key"": ""case-1"", ""value"": { ""rev"": ""2-a"" } },
                        { ""id"": ""powerbi-case-1"", ""key"": ""powerbi-case-1"", ""value"": { ""rev"": ""3-a"" } }
                    ]
                }");
            }

            if (method == "POST" && url == $"{reportDatabaseUrl}/_bulk_docs")
            {
                bulkDeleteCallCount++;
                bulkDeleteBody = body;
                return CreateJsonResponse(@"[
                    { ""ok"": true, ""id"": ""case-1"", ""rev"": ""3-b"" },
                    { ""ok"": true, ""id"": ""powerbi-case-1"", ""rev"": ""4-c"" }
                ]");
            }

            throw new InvalidOperationException($"Unexpected request during report preservation test: {method} {url}");
        });

        using var httpClient = new HttpClient(handler);
        var couchDbClient = new mmria.common.getset.CouchDbHttpClient(new FixedHttpClientFactory(httpClient));

        bool databaseExisted = await MMRIAServicesHelper.ClearDatabaseDocumentsPreservingSystemDocsAsync(
            couchDbClient,
            reportDatabaseUrl,
            cfg.CdcDbConfiguration.user_name,
            cfg.CdcDbConfiguration.user_value,
            batchSize: 100);

        Assert.That(databaseExisted, Is.True);
        Assert.That(bulkDeleteCallCount, Is.EqualTo(1));

        var payload = JObject.Parse(bulkDeleteBody);
        var docs = payload["docs"] as JArray;
        Assert.That(docs, Is.Not.Null);

        var deletedIds = docs!
            .OfType<JObject>()
            .Select(doc => doc.Value<string>("_id"))
            .ToList();

        Assert.That(deletedIds, Is.EquivalentTo(new[] { "case-1", "powerbi-case-1" }));
        Assert.That(docs.All(doc => doc?["_deleted"]?.Value<bool>() == true), Is.True);
        Assert.That(deletedIds.Any(id => id != null && id.StartsWith("_design/", StringComparison.OrdinalIgnoreCase)), Is.False);
        Assert.That(deletedIds.Any(id => id != null && id.StartsWith("_local/", StringComparison.OrdinalIgnoreCase)), Is.False);
    }

    private static mmria.common.metadata.Populate_CDC_Instance BuildPopulateCdcMessage(params string[] includedPrefixes)
    {
        var includeSet = includedPrefixes?.Length > 0
            ? new HashSet<string>(includedPrefixes, StringComparer.OrdinalIgnoreCase)
            : null;

        return new mmria.common.metadata.Populate_CDC_Instance
        {
            state_list =
            [
                new mmria.common.metadata.State_List_Item { is_included = includeSet?.Contains("tenant1") ?? true, prefix = "tenant1", name = "tenant 1 test site" },
                new mmria.common.metadata.State_List_Item { is_included = includeSet?.Contains("tenant2") ?? true, prefix = "tenant2", name = "tenant 2 test site" },
                new mmria.common.metadata.State_List_Item { is_included = includeSet?.Contains("tenant3") ?? true, prefix = "tenant3", name = "tenant 3 test site" },
                new mmria.common.metadata.State_List_Item { is_included = includeSet?.Contains("tenant4") ?? true, prefix = "tenant4", name = "tenant 4 test site" },
                new mmria.common.metadata.State_List_Item { is_included = includeSet?.Contains("tenant5") ?? true, prefix = "tenant5", name = "tenant 5 test site" }
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

    private async Task<string> GetAnyCaseIdAsync(
        mmria.common.couchdb.DBConfigurationDetail dbInfo,
        bool usePrefixedMmrds)
    {
        var ids = await GetDocumentIdsFromDatabaseAsync(dbInfo, usePrefixedMmrds);
        var caseId = ids.FirstOrDefault();
        Assert.That(string.IsNullOrWhiteSpace(caseId), Is.False, "Expected at least one source case document for Populate CDC tests.");
        return caseId!;
    }

    private async Task<JObject> GetDocumentAsync(
        mmria.common.couchdb.DBConfigurationDetail dbInfo,
        string documentId,
        bool usePrefixedMmrds)
    {
        string dbUrl = usePrefixedMmrds && !string.IsNullOrWhiteSpace(dbInfo.prefix)
            ? $"{dbInfo.url}/{dbInfo.prefix}_mmrds"
            : $"{dbInfo.url}/mmrds";

        string responseFromServer = await _env.CouchDbClient.ExecuteAsync(
            "GET",
            $"{dbUrl}/{documentId}",
            null,
            dbInfo.user_name,
            dbInfo.user_value,
            timeoutSeconds: 300);

        return JObject.Parse(responseFromServer);
    }

    private async Task SaveDocumentAsync(
        mmria.common.couchdb.DBConfigurationDetail dbInfo,
        JObject document,
        bool usePrefixedMmrds)
    {
        string dbUrl = usePrefixedMmrds && !string.IsNullOrWhiteSpace(dbInfo.prefix)
            ? $"{dbInfo.url}/{dbInfo.prefix}_mmrds"
            : $"{dbInfo.url}/mmrds";

        await _env.CouchDbClient.ExecuteAsync(
            "PUT",
            $"{dbUrl}/{document.Value<string>("_id")}",
            document.ToString(),
            dbInfo.user_name,
            dbInfo.user_value,
            timeoutSeconds: 300);
    }

    private static void AssertLockFieldRemoved(JObject document, string fieldName)
    {
        Assert.That(document.ContainsKey(fieldName), Is.False, $"Expected '{fieldName}' to be removed from CDC imported document.");
    }

    private static void LogStatus(string message)
    {
        var line = $"[PopulateCDCInstanceTests] {message}";
        Console.WriteLine(line);
        TestContext.WriteLine(line);
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class FixedHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FixedHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return _client;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _responder(request);
        }
    }

}

[TestFixture]
public sealed class PopulateCDCInstanceBatchingTests
{
    private static TestCredentialSettings LoadConfiguredCredentials()
    {
        var loader = new TestConfigurationLoader();
        loader.Load();

        if (!loader.HasResolvedSensitiveSettings())
        {
            Assert.Inconclusive(loader.GetSensitiveSettingsSetupMessage());
        }

        return loader.TestCredentials;
    }

    [Test]
    [Category("PopulateCDC")]
    public async Task Scenario_E_PopulateCdc_UsesCachedExportList_And_BatchDatabaseCalls()
    {
        var credentials = LoadConfiguredCredentials();
        int exportListGetCount = 0;
        int sourceBatchReadCount = 0;
        int sourceSingleDocumentGetCount = 0;
        int targetBulkWriteCount = 0;
        int targetSingleDocumentPutCount = 0;
        string bulkWriteBody = string.Empty;

        var sourceDocuments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["case-1"] = @"{
                ""_id"": ""case-1"",
                ""first_name"": ""Alice"",
                ""last_name"": ""Smith"",
                ""created_by"": ""tester"",
                ""last_updated_by"": ""tester"",
                ""is_offline"": true,
                ""offline_by"": ""offline-user"",
                ""offline_by_tab_id"": ""tab-1"",
                ""checked_out_by_tab_id"": ""tab-1""
            }",
            ["case-2"] = @"{
                ""_id"": ""case-2"",
                ""first_name"": ""Bea"",
                ""last_name"": ""Jones"",
                ""created_by"": ""tester"",
                ""last_updated_by"": ""tester"",
                ""is_offline"": true,
                ""offline_by"": ""offline-user"",
                ""offline_by_tab_id"": ""tab-2"",
                ""checked_out_by_tab_id"": ""tab-2""
            }"
        };

        var handler = new RecordingHttpMessageHandler(async request =>
        {
            string url = request.RequestUri!.ToString();
            string method = request.Method.Method.ToUpperInvariant();
            string body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync();

            if (method == "GET" && url == "https://tenant1.example/tenant1_mmrds/_design/sortable/_view/by_date_created?skip=0&take=250000")
            {
                return CreateJsonResponse(@"{
                    ""offset"": 0,
                    ""rows"": [
                        { ""id"": ""case-1"", ""key"": ""case-1"", ""value"": {} },
                        { ""id"": ""case-2"", ""key"": ""case-2"", ""value"": {} }
                    ],
                    ""total_rows"": 2
                }");
            }

            if (method == "POST" && url == "https://tenant1.example/tenant1_mmrds/_all_docs?include_docs=true")
            {
                sourceBatchReadCount++;
                var requestJson = JObject.Parse(body);
                var requestedKeys = requestJson["keys"]?.Values<string>().ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
                Assert.That(requestedKeys.SetEquals(sourceDocuments.Keys), Is.True, "Batch read should request the current source case ids.");

                return CreateJsonResponse(@"{
                    ""offset"": null,
                    ""rows"": [
                        { ""id"": ""case-1"", ""key"": ""case-1"", ""doc"": {
                            ""_id"": ""case-1"",
                            ""first_name"": ""Alice"",
                            ""last_name"": ""Smith"",
                            ""created_by"": ""tester"",
                            ""last_updated_by"": ""tester"",
                            ""is_offline"": true,
                            ""offline_by"": ""offline-user"",
                            ""offline_by_tab_id"": ""tab-1"",
                            ""checked_out_by_tab_id"": ""tab-1""
                        }},
                        { ""id"": ""case-2"", ""key"": ""case-2"", ""doc"": {
                            ""_id"": ""case-2"",
                            ""first_name"": ""Bea"",
                            ""last_name"": ""Jones"",
                            ""created_by"": ""tester"",
                            ""last_updated_by"": ""tester"",
                            ""is_offline"": true,
                            ""offline_by"": ""offline-user"",
                            ""offline_by_tab_id"": ""tab-2"",
                            ""checked_out_by_tab_id"": ""tab-2""
                        }}
                    ],
                    ""total_rows"": 2
                }");
            }

            if (method == "GET" && url == "https://cdc.example/metadata/de-identified-export-list")
            {
                exportListGetCount++;
                return CreateJsonResponse(@"{
                    ""name_path_list"": {
                        ""global"": [""first_name"", ""last_name""],
                        ""tenant1"": [""first_name"", ""last_name""]
                    }
                }");
            }

            if (method == "POST" && url == "https://cdc.example/mmrds/_bulk_docs")
            {
                targetBulkWriteCount++;
                bulkWriteBody = body;
                return CreateJsonResponse(@"[
                    { ""ok"": true, ""id"": ""case-1"", ""rev"": ""1-a"" },
                    { ""ok"": true, ""id"": ""case-2"", ""rev"": ""1-b"" }
                ]");
            }

            if (method == "GET" && url.StartsWith("https://tenant1.example/tenant1_mmrds/", StringComparison.OrdinalIgnoreCase))
            {
                sourceSingleDocumentGetCount++;
                string caseId = url.Split('/').Last();
                return CreateJsonResponse(sourceDocuments[caseId]);
            }

            if
            (
                method == "PUT" &&
                url.StartsWith("https://cdc.example/mmrds/", StringComparison.OrdinalIgnoreCase) &&
                !url.Contains("/_security", StringComparison.OrdinalIgnoreCase) &&
                !url.Contains("/_design/", StringComparison.OrdinalIgnoreCase)
            )
            {
                targetSingleDocumentPutCount++;
                return CreateJsonResponse(@"{ ""ok"": true }");
            }

            if (url.StartsWith("https://cdc.example/", StringComparison.OrdinalIgnoreCase))
            {
                return CreateJsonResponse(@"{ ""ok"": true }");
            }

            throw new InvalidOperationException($"Unexpected request during Populate CDC test: {method} {url}");
        });

        using var httpClient = new HttpClient(handler);
        var couchDbClient = new mmria.common.getset.CouchDbHttpClient(new FixedHttpClientFactory(httpClient));
        var manager = new MMRIAServicesManager(new MMRIAServicesDAL(couchDbClient), couchDbClient);

        var configSet = new mmria.common.couchdb.ConfigurationSet();
        configSet.name_value["metadata_version"] = "26.01.20";
        configSet.detail_list["cdc"] = new mmria.common.couchdb.DBConfigurationDetail
        {
            url = "https://cdc.example",
            user_name = credentials.SampleCredentials.StubDbUserName,
            user_value = credentials.SampleCredentials.StubDbPassword
        };
        configSet.detail_list["tenant1"] = new mmria.common.couchdb.DBConfigurationDetail
        {
            url = "https://tenant1.example",
            prefix = "tenant1",
            user_name = credentials.SampleCredentials.StubDbUserName,
            user_value = credentials.SampleCredentials.StubDbPassword
        };

        var message = BuildPopulateCdcMessage("tenant1");
        var (name, _) = await manager.PopulateCDCInstanceManger(message, configSet);

        Assert.That(name, Is.EqualTo("Finished"));
        Assert.That(exportListGetCount, Is.EqualTo(1), "de-identified-export-list should be fetched once per populate run.");
        Assert.That(sourceBatchReadCount, Is.EqualTo(1), "Source case documents should be read in a single batch for this test.");
        Assert.That(sourceSingleDocumentGetCount, Is.EqualTo(0), "Populate CDC should not fall back to one GET per source case.");
        Assert.That(targetBulkWriteCount, Is.EqualTo(1), "CDC mmrds writes should use _bulk_docs.");
        Assert.That(targetSingleDocumentPutCount, Is.EqualTo(0), "Populate CDC should not PUT each CDC document individually.");

        var bulkWriteJson = JObject.Parse(bulkWriteBody);
        var docs = bulkWriteJson["docs"] as JArray;
        Assert.That(docs, Is.Not.Null);
        Assert.That(docs!.Count, Is.EqualTo(2));
        Assert.That(docs.All(doc => string.Equals(doc?["first_name"]?.ToString(), "de-identified", StringComparison.Ordinal)), Is.True);
        Assert.That(docs.All(doc => string.Equals(doc?["last_name"]?.ToString(), "de-identified", StringComparison.Ordinal)), Is.True);
        Assert.That(docs.All(doc => doc?["is_offline"] == null), Is.True);
        Assert.That(docs.All(doc => doc?["offline_by"] == null), Is.True);
        Assert.That(docs.All(doc => doc?["offline_by_tab_id"] == null), Is.True);
        Assert.That(docs.All(doc => doc?["checked_out_by_tab_id"] == null), Is.True);
    }

    [Test]
    [Category("PopulateCDC")]
    public async Task Scenario_F_PopulateCdc_AppliesConfiguredPageSize_Chunking_Retry_And_BatchDelay()
    {
        var credentials = LoadConfiguredCredentials();
        int exportListGetCount = 0;
        int targetBulkWriteCount = 0;
        int transientBulkFailureCount = 0;
        var sourceBatchSizes = new List<int>();
        var sourceDocuments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["case-1"] = @"{
                ""_id"": ""case-1"",
                ""first_name"": ""Alice"",
                ""last_name"": ""Smith"",
                ""created_by"": ""tester"",
                ""last_updated_by"": ""tester""
            }",
            ["case-2"] = @"{
                ""_id"": ""case-2"",
                ""first_name"": ""Bea"",
                ""last_name"": ""Jones"",
                ""created_by"": ""tester"",
                ""last_updated_by"": ""tester""
            }",
            ["case-3"] = @"{
                ""_id"": ""case-3"",
                ""first_name"": ""Cara"",
                ""last_name"": ""Brown"",
                ""created_by"": ""tester"",
                ""last_updated_by"": ""tester""
            }"
        };

        var handler = new RecordingHttpMessageHandler(async request =>
        {
            string url = request.RequestUri!.ToString();
            string method = request.Method.Method.ToUpperInvariant();
            string body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync();

            if (method == "GET" && url == "https://tenant1.example/tenant1_mmrds/_design/sortable/_view/by_date_created?skip=0&take=250000")
            {
                return CreateJsonResponse(@"{
                    ""offset"": 0,
                    ""rows"": [
                        { ""id"": ""case-1"", ""key"": ""case-1"", ""value"": {} },
                        { ""id"": ""case-2"", ""key"": ""case-2"", ""value"": {} },
                        { ""id"": ""case-3"", ""key"": ""case-3"", ""value"": {} }
                    ],
                    ""total_rows"": 3
                }");
            }

            if (method == "POST" && url == "https://tenant1.example/tenant1_mmrds/_all_docs?include_docs=true")
            {
                var requestJson = JObject.Parse(body);
                var requestedKeys = requestJson["keys"]?.Values<string>().ToList() ?? [];
                sourceBatchSizes.Add(requestedKeys.Count);

                var rows = new JArray();
                foreach (var key in requestedKeys)
                {
                    rows.Add(new JObject
                    {
                        ["id"] = key,
                        ["key"] = key,
                        ["doc"] = JObject.Parse(sourceDocuments[key])
                    });
                }

                return CreateJsonResponse(new JObject
                {
                    ["offset"] = null,
                    ["rows"] = rows,
                    ["total_rows"] = rows.Count
                }.ToString());
            }

            if (method == "GET" && url == "https://cdc.example/metadata/de-identified-export-list")
            {
                exportListGetCount++;
                return CreateJsonResponse(@"{
                    ""name_path_list"": {
                        ""global"": [""first_name"", ""last_name""],
                        ""tenant1"": [""first_name"", ""last_name""]
                    }
                }");
            }

            if (method == "POST" && url == "https://cdc.example/mmrds/_bulk_docs")
            {
                targetBulkWriteCount++;
                if (transientBulkFailureCount == 0)
                {
                    transientBulkFailureCount++;
                    throw new HttpRequestException("Transient bulk write failure");
                }

                var payload = JObject.Parse(body);
                var docs = payload["docs"]?.OfType<JObject>().ToList() ?? [];
                var responseDocs = new JArray();
                foreach (var doc in docs)
                {
                    responseDocs.Add(new JObject
                    {
                        ["ok"] = true,
                        ["id"] = doc.Value<string>("_id"),
                        ["rev"] = "1-a"
                    });
                }

                return CreateJsonResponse(responseDocs.ToString());
            }

            if (url.StartsWith("https://cdc.example/", StringComparison.OrdinalIgnoreCase))
            {
                return CreateJsonResponse(@"{ ""ok"": true }");
            }

            throw new InvalidOperationException($"Unexpected request during Populate CDC throttling test: {method} {url}");
        });

        using var httpClient = new HttpClient(handler);
        var couchDbClient = new mmria.common.getset.CouchDbHttpClient(new FixedHttpClientFactory(httpClient));
        var manager = new MMRIAServicesManager(new MMRIAServicesDAL(couchDbClient), couchDbClient);
        var throttleSettings = new PopulateCdcThrottleSettings
        {
            Copy = new PopulateCdcPhaseThrottleSettings
            {
                PageSize = 2,
                MaxParallelism = 1,
                BulkDocChunkSize = 1,
                BatchDelayMs = 40,
                BulkWriteRetryCount = 1,
                BulkWriteRetryDelayMs = 0
            },
            Rebuild = PopulateCdcThrottleSettings.CreateDefaults().Rebuild
        };

        var configSet = new mmria.common.couchdb.ConfigurationSet();
        configSet.name_value["metadata_version"] = "26.01.20";
        configSet.detail_list["cdc"] = new mmria.common.couchdb.DBConfigurationDetail
        {
            url = "https://cdc.example",
            user_name = credentials.SampleCredentials.StubDbUserName,
            user_value = credentials.SampleCredentials.StubDbPassword
        };
        configSet.detail_list["tenant1"] = new mmria.common.couchdb.DBConfigurationDetail
        {
            url = "https://tenant1.example",
            prefix = "tenant1",
            user_name = credentials.SampleCredentials.StubDbUserName,
            user_value = credentials.SampleCredentials.StubDbPassword
        };

        var message = BuildPopulateCdcMessage("tenant1");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var (name, _) = await manager.PopulateCDCInstanceManger(message, configSet, throttleSettings: throttleSettings);
        stopwatch.Stop();

        Assert.That(name, Is.EqualTo("Finished"));
        Assert.That(exportListGetCount, Is.EqualTo(1));
        Assert.That(sourceBatchSizes, Is.EqualTo(new[] { 2, 1 }), "Copy page size should split source reads into two batches.");
        Assert.That(targetBulkWriteCount, Is.EqualTo(4), "Chunked writes should retry the first failed chunk and then write each of the three chunks.");
        Assert.That(transientBulkFailureCount, Is.EqualTo(1), "Exactly one transient bulk write failure should be retried.");
        Assert.That(stopwatch.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(30), "Copy batch delay should add a measurable pause between source batches.");
    }

    private static mmria.common.metadata.Populate_CDC_Instance BuildPopulateCdcMessage(params string[] includedPrefixes)
    {
        var includeSet = includedPrefixes?.Length > 0
            ? new HashSet<string>(includedPrefixes, StringComparer.OrdinalIgnoreCase)
            : null;

        return new mmria.common.metadata.Populate_CDC_Instance
        {
            state_list =
            [
                new mmria.common.metadata.State_List_Item { is_included = includeSet?.Contains("tenant1") ?? true, prefix = "tenant1", name = "tenant 1 test site" },
                new mmria.common.metadata.State_List_Item { is_included = includeSet?.Contains("tenant2") ?? true, prefix = "tenant2", name = "tenant 2 test site" },
                new mmria.common.metadata.State_List_Item { is_included = includeSet?.Contains("tenant3") ?? true, prefix = "tenant3", name = "tenant 3 test site" },
                new mmria.common.metadata.State_List_Item { is_included = includeSet?.Contains("tenant4") ?? true, prefix = "tenant4", name = "tenant 4 test site" },
                new mmria.common.metadata.State_List_Item { is_included = includeSet?.Contains("tenant5") ?? true, prefix = "tenant5", name = "tenant 5 test site" }
            ]
        };
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class FixedHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FixedHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return _client;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _responder(request);
        }
    }
}

[TestFixture]
public sealed class PopulateCdcThrottleSettingsLoaderTests
{
    [Test]
    [Category("PopulateCDC")]
    public void Load_UsesDefaults_WhenValuesAreMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var settings = mmria.services.populate_cdc_instance.PopulateCdcThrottleSettingsLoader.Load(configuration);
        var defaults = PopulateCdcThrottleSettings.CreateDefaults();

        Assert.That(settings.Copy.PageSize, Is.EqualTo(defaults.Copy.PageSize));
        Assert.That(settings.Copy.MaxParallelism, Is.EqualTo(defaults.Copy.MaxParallelism));
        Assert.That(settings.Copy.BulkDocChunkSize, Is.EqualTo(defaults.Copy.BulkDocChunkSize));
        Assert.That(settings.Copy.BatchDelayMs, Is.EqualTo(defaults.Copy.BatchDelayMs));
        Assert.That(settings.Copy.BulkWriteRetryCount, Is.EqualTo(defaults.Copy.BulkWriteRetryCount));
        Assert.That(settings.Copy.BulkWriteRetryDelayMs, Is.EqualTo(defaults.Copy.BulkWriteRetryDelayMs));
        Assert.That(settings.Rebuild.PageSize, Is.EqualTo(defaults.Rebuild.PageSize));
        Assert.That(settings.Rebuild.MaxParallelism, Is.EqualTo(defaults.Rebuild.MaxParallelism));
        Assert.That(settings.Rebuild.BulkDocChunkSize, Is.EqualTo(defaults.Rebuild.BulkDocChunkSize));
        Assert.That(settings.Rebuild.BatchDelayMs, Is.EqualTo(defaults.Rebuild.BatchDelayMs));
        Assert.That(settings.Rebuild.BulkWriteRetryCount, Is.EqualTo(defaults.Rebuild.BulkWriteRetryCount));
        Assert.That(settings.Rebuild.BulkWriteRetryDelayMs, Is.EqualTo(defaults.Rebuild.BulkWriteRetryDelayMs));
    }

    [Test]
    [Category("PopulateCDC")]
    public void Load_ClampsInvalidValues_ToSafeMinimums()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["mmria_settings:populate_cdc_copy_page_size"] = "0",
                ["mmria_settings:populate_cdc_copy_max_parallelism"] = "-1",
                ["mmria_settings:populate_cdc_copy_bulk_doc_chunk_size"] = "-3",
                ["mmria_settings:populate_cdc_copy_batch_delay_ms"] = "-10",
                ["mmria_settings:populate_cdc_copy_bulk_write_retry_count"] = "-5",
                ["mmria_settings:populate_cdc_copy_bulk_write_retry_delay_ms"] = "-15",
                ["mmria_settings:populate_cdc_rebuild_page_size"] = "abc",
                ["mmria_settings:populate_cdc_rebuild_max_parallelism"] = "0",
                ["mmria_settings:populate_cdc_rebuild_bulk_doc_chunk_size"] = "-8",
                ["mmria_settings:populate_cdc_rebuild_batch_delay_ms"] = "-30",
                ["mmria_settings:populate_cdc_rebuild_bulk_write_retry_count"] = "-2",
                ["mmria_settings:populate_cdc_rebuild_bulk_write_retry_delay_ms"] = "-40"
            })
            .Build();

        var settings = mmria.services.populate_cdc_instance.PopulateCdcThrottleSettingsLoader.Load(configuration);
        var defaults = PopulateCdcThrottleSettings.CreateDefaults();

        Assert.That(settings.Copy.PageSize, Is.EqualTo(1));
        Assert.That(settings.Copy.MaxParallelism, Is.EqualTo(1));
        Assert.That(settings.Copy.BulkDocChunkSize, Is.EqualTo(0));
        Assert.That(settings.Copy.BatchDelayMs, Is.EqualTo(0));
        Assert.That(settings.Copy.BulkWriteRetryCount, Is.EqualTo(0));
        Assert.That(settings.Copy.BulkWriteRetryDelayMs, Is.EqualTo(0));
        Assert.That(settings.Rebuild.PageSize, Is.EqualTo(defaults.Rebuild.PageSize));
        Assert.That(settings.Rebuild.MaxParallelism, Is.EqualTo(1));
        Assert.That(settings.Rebuild.BulkDocChunkSize, Is.EqualTo(0));
        Assert.That(settings.Rebuild.BatchDelayMs, Is.EqualTo(0));
        Assert.That(settings.Rebuild.BulkWriteRetryCount, Is.EqualTo(0));
        Assert.That(settings.Rebuild.BulkWriteRetryDelayMs, Is.EqualTo(0));
    }
}

[TestFixture]
public sealed class PopulateCdcRebuildBulkWriteTests
{
    [Test]
    [Category("PopulateCDC")]
    public async Task BulkWriteAsync_UsesChunking_And_RetriesTransientFailures()
    {
        int bulkWriteCallCount = 0;
        int transientFailureCount = 0;
        var handler = new RecordingHttpMessageHandler(async request =>
        {
            string url = request.RequestUri!.ToString();
            string method = request.Method.Method.ToUpperInvariant();
            string body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync();

            if (method == "POST" && url == "https://cdc.example/report/_bulk_docs")
            {
                bulkWriteCallCount++;
                if (transientFailureCount == 0)
                {
                    transientFailureCount++;
                    throw new HttpRequestException("Transient report bulk write failure");
                }

                var payload = JObject.Parse(body);
                var docs = payload["docs"]?.OfType<JObject>().ToList() ?? [];
                var responseDocs = new JArray();
                foreach (var doc in docs)
                {
                    responseDocs.Add(new JObject
                    {
                        ["ok"] = true,
                        ["id"] = doc.Value<string>("_id"),
                        ["rev"] = "1-a"
                    });
                }

                return CreateJsonResponse(responseDocs.ToString());
            }

            throw new InvalidOperationException($"Unexpected request during Populate CDC rebuild bulk write test: {method} {url}");
        });

        using var httpClient = new HttpClient(handler);
        var couchDbClient = new mmria.common.getset.CouchDbHttpClient(new FixedHttpClientFactory(httpClient));
        var rebuild = new services::mmria.server.utils.c_document_sync_all(
            new mmria.common.couchdb.DBConfigurationDetail
            {
                url = "https://cdc.example",
                prefix = string.Empty,
                user_name = "stub-user",
                user_value = "stub-password"
            },
            "26.01.20",
            couchDbClient);

        var method = typeof(services::mmria.server.utils.c_document_sync_all).GetMethod(
            "bulk_write_async",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string), typeof(List<string>), typeof(int), typeof(int), typeof(int) },
            modifiers: null);

        Assert.That(method, Is.Not.Null, "Expected private bulk_write_async overload to exist for phase 2 throttling.");

        var task = (Task<(int success_count, int error_count)>)method!.Invoke(
            rebuild,
            new object[]
            {
                "report",
                new List<string>
                {
                    @"{ ""_id"": ""doc-1"", ""value"": 1 }",
                    @"{ ""_id"": ""doc-2"", ""value"": 2 }",
                    @"{ ""_id"": ""doc-3"", ""value"": 3 }"
                },
                2,
                1,
                0
            })!;

        var result = await task;

        Assert.That(result.success_count, Is.EqualTo(3));
        Assert.That(result.error_count, Is.EqualTo(0));
        Assert.That(transientFailureCount, Is.EqualTo(1));
        Assert.That(bulkWriteCallCount, Is.EqualTo(3), "One transient retry plus two successful chunks should result in three bulk write attempts.");
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class FixedHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FixedHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return _client;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _responder(request);
        }
    }
}
