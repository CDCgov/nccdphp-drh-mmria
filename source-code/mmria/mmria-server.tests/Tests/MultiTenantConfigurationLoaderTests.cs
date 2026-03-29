#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using mmria.common.couchdb;
using mmria.common.getset;
using NUnit.Framework;

namespace mmria_server.tests.Tests;

[TestFixture]
public sealed class MultiTenantConfigurationLoaderTests
{
    [Test]
    public async Task LoadRequiredConfigurationSetsAsync_LoadsSingleTenantConfigurationSet()
    {
        var loader = new MultiTenantConfigurationLoader();
        var expectedConfigurationSet = CreateConfigurationSet("single", "http://single.test");
        var couchDbHttpClient = new CouchDbHttpClient(new StubHttpClientFactory(request =>
        {
            Assert.That(request.RequestUri?.ToString(), Is.EqualTo("http://single.test/configuration/single"));
            return CreateJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedConfigurationSet));
        }));

        var result = await loader.LoadRequiredConfigurationSetsAsync(
            Array.Empty<string>(),
            "http://single.test",
            "tester",
            "secret",
            "single",
            couchDbHttpClient);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0]._id, Is.EqualTo("single"));
        Assert.That(result[0].detail_list["single"].url, Is.EqualTo("http://single.test"));
    }

    [Test]
    public async Task LoadRequiredOverridableConfigurationsAsync_LoadsSingleTenantConfiguration()
    {
        var loader = new MultiTenantConfigurationLoader();
        var expectedConfiguration = CreateOverridableConfiguration("shared", "http://single.test");
        var couchDbHttpClient = new CouchDbHttpClient(new StubHttpClientFactory(request =>
        {
            Assert.That(request.RequestUri?.ToString(), Is.EqualTo("http://single.test/configuration/shared"));
            return CreateJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedConfiguration));
        }));

        var result = await loader.LoadRequiredOverridableConfigurationsAsync(
            Array.Empty<string>(),
            "http://single.test",
            "tester",
            "secret",
            "shared",
            "single",
            couchDbHttpClient);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0]._id, Is.EqualTo("single_shared"));
        Assert.That(result[0].GetString("shared", "couchdb_url"), Is.EqualTo("http://single.test"));
    }

    [Test]
    public void LoadRequiredConfigurationSetsAsync_ThrowsWhenConfigurationSetIsMissing()
    {
        var loader = new MultiTenantConfigurationLoader();
        var couchDbHttpClient = new CouchDbHttpClient(new StubHttpClientFactory(_ =>
            CreateJsonResponse(HttpStatusCode.OK, "{\"error\":\"not_found\",\"reason\":\"missing\"}")));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await loader.LoadRequiredConfigurationSetsAsync(
                ["tenant1"],
                "http://{replace}-couchdb.local:5984",
                "tester",
                "secret",
                "configuration",
                couchDbHttpClient));

        Assert.That(exception!.Message, Does.Contain("Required ConfigurationSet 'tenant1' was not found"));
    }

    [Test]
    public void LoadRequiredOverridableConfigurationsAsync_ThrowsWhenConfigurationIsMissing()
    {
        var loader = new MultiTenantConfigurationLoader();
        var couchDbHttpClient = new CouchDbHttpClient(new StubHttpClientFactory(_ =>
            CreateJsonResponse(HttpStatusCode.OK, "{\"error\":\"not_found\",\"reason\":\"missing\"}")));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await loader.LoadRequiredOverridableConfigurationsAsync(
                ["tenant1"],
                "http://{replace}-couchdb.local:5984",
                "tester",
                "secret",
                "shared",
                "configuration",
                couchDbHttpClient));

        Assert.That(exception!.Message, Does.Contain("Required OverridableConfiguration 'shared' was not found"));
    }

    private static OverridableConfiguration CreateOverridableConfiguration(string sharedBucket, string couchDbUrl)
    {
        var configuration = new OverridableConfiguration
        {
            _id = sharedBucket
        };

        configuration.SetString("shared", "couchdb_url", couchDbUrl);
        configuration.SetString("shared", "timer_user_name", "tester");
        configuration.SetString("shared", "timer_value", "secret");
        return configuration;
    }

    private static ConfigurationSet CreateConfigurationSet(string tenant, string couchDbUrl)
    {
        var configurationSet = new ConfigurationSet
        {
            _id = tenant
        };

        configurationSet.detail_list[tenant] = new DBConfigurationDetail
        {
            url = couchDbUrl,
            prefix = string.Empty,
            user_name = "tester",
            user_value = "secret"
        };

        return configurationSet;
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
