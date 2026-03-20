#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mmria.common.Testing.CaseGeneration.Models;
using mmria.common.Testing.CaseGeneration.Writers;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace mmria_server.tests.Tests;

[TestFixture]
public sealed class CaseGeneratorWriterTests
{
    [Test]
    public async Task SaveCasesBatchAsync_UsesBulkDocsBatches()
    {
        int singleDocumentPutCount = 0;
        var batchSizes = new List<int>();
        var progressValues = new List<int>();

        var handler = new RecordingHttpMessageHandler(async request =>
        {
            string url = request.RequestUri!.ToString();
            string method = request.Method.Method.ToUpperInvariant();
            string body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync();

            if (method == "POST" && url == "https://couch.example/mmrds/_bulk_docs")
            {
                var requestJson = JObject.Parse(body);
                var docs = requestJson["docs"] as JArray;
                Assert.That(docs, Is.Not.Null);

                batchSizes.Add(docs!.Count);
                Assert.That(docs.All(doc => !string.IsNullOrWhiteSpace(doc?["_id"]?.ToString())), Is.True,
                    "Each bulk document should include an _id before save.");

                var responseItems = new JArray();
                foreach (var doc in docs)
                {
                    responseItems.Add(new JObject
                    {
                        ["ok"] = true,
                        ["id"] = doc?["_id"]?.ToString(),
                        ["rev"] = "1-test"
                    });
                }

                return CreateJsonResponse(responseItems.ToString());
            }

            if (method == "PUT" && url.StartsWith("https://couch.example/mmrds/", StringComparison.OrdinalIgnoreCase))
            {
                singleDocumentPutCount++;
                return CreateJsonResponse(@"{ ""ok"": true }");
            }

            throw new InvalidOperationException($"Unexpected request during case generator writer test: {method} {url}");
        });

        using var httpClient = new HttpClient(handler);
        var couchDbClient = new mmria.common.getset.CouchDbHttpClient(new FixedHttpClientFactory(httpClient));
        var config = new GenerationConfig
        {
            Jurisdiction = "tenant1",
            CouchDbUrl = "https://couch.example",
            DatabaseName = "mmrds"
        };
        var writer = new CouchDbWriter(config, couchDbClient);

        var cases = Enumerable.Range(1, 105)
            .Select(index => new Dictionary<string, object?>
            {
                ["created_by"] = "test-user",
                ["last_updated_by"] = "test-user",
                ["case_status"] = "in-progress",
                ["jurisdiction_id"] = "tenant1",
                ["version"] = "26.01.20"
            })
            .ToList();

        var progress = new ListProgress(progressValues);
        var result = await writer.SaveCasesBatchAsync(cases, progress);

        Assert.That(result.TotalCases, Is.EqualTo(105));
        Assert.That(result.SuccessCount, Is.EqualTo(105));
        Assert.That(result.FailureCount, Is.EqualTo(0));
        Assert.That(result.SavedDocumentIds.Count, Is.EqualTo(105));
        Assert.That(result.Errors, Is.Empty);
        Assert.That(batchSizes, Is.EqualTo(new[] { 100, 5 }));
        Assert.That(singleDocumentPutCount, Is.EqualTo(0), "Batch saves should not use one PUT per document.");
        Assert.That(progressValues.Count, Is.EqualTo(105));
        Assert.That(progressValues.First(), Is.EqualTo(1));
        Assert.That(progressValues.Last(), Is.EqualTo(105));
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

    private sealed class ListProgress : IProgress<int>
    {
        private readonly List<int> _values;

        public ListProgress(List<int> values)
        {
            _values = values;
        }

        public void Report(int value)
        {
            _values.Add(value);
        }
    }
}
