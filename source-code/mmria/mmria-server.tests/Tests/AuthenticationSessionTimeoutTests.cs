#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.server.authentication;
using mmria.server.util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace mmria_server.tests.Tests;

[TestFixture]
[NonParallelizable]
public sealed class AuthenticationSessionTimeoutTests
{
    private const string HostPrefix = "tenant4";
    private const string HostName = "tenant4-mmria.local";
    private const string LocalCouchDbUrl = "http://localhost:12345";

    [Test]
    public void GetSessionIdleTimeoutMinutes_PrefersTenantSpecificValue()
    {
        var tenantConfiguration = CreateTenantConfiguration(45);
        var fallbackConfiguration = CreateFallbackConfiguration(sharedTimeoutMinutes: 20);

        var result = SessionTimeoutHelper.GetSessionIdleTimeoutMinutes(
            tenantConfiguration,
            fallbackConfiguration,
            HostPrefix);

        Assert.That(result, Is.EqualTo(45));
    }

    [Test]
    public void GetSessionIdleTimeoutMinutes_UsesFallbackSharedValueWhenTenantMissing()
    {
        var tenantConfiguration = CreateTenantConfiguration(null);
        var fallbackConfiguration = CreateFallbackConfiguration(sharedTimeoutMinutes: 20);

        var result = SessionTimeoutHelper.GetSessionIdleTimeoutMinutes(
            tenantConfiguration,
            fallbackConfiguration,
            HostPrefix);

        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void GetSessionIdleTimeoutMinutes_UsesThirtyMinuteDefaultWhenMissing()
    {
        var fallbackConfiguration = CreateFallbackConfiguration(sharedTimeoutMinutes: null);

        var result = SessionTimeoutHelper.GetSessionIdleTimeoutMinutes(
            tenantConfiguration: null,
            fallbackConfiguration,
            HostPrefix);

        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public async Task CustomAuthHandler_RefreshesUsingTenantSpecificTimeout()
    {
        HttpRequestMessage? putRequest = null;
        string? putBody = null;
        var configSets = CreateConfigSetList();
        var tenantConfiguration = CreateTenantConfiguration(45, configId: $"{HostPrefix}_shared");

        using var httpClient = new HttpClient(new RecordingHttpMessageHandler(async request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return CreateJsonResponse(CreateActiveSessionJson());
            }

            putRequest = request;
            putBody = request.Content == null ? null : await request.Content.ReadAsStringAsync();
            return CreateJsonResponse(@"{ ""ok"": true, ""id"": ""session-1"", ""rev"": ""2-test"" }");
        }));

        var result = await AuthenticateWithHandlerAsync(
            CreateRequestTenantRuntime(
                CreateRootRuntimeSettings(isMultiTenantMode: true, configuredTenants: [HostPrefix]),
                new List<OverridableConfiguration> { tenantConfiguration },
                configSets),
            httpClient);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(putRequest, Is.Not.Null);
        Assert.That(putRequest!.Method, Is.EqualTo(HttpMethod.Put));
        Assert.That(putBody, Is.Not.Null.And.Not.Empty);

        var refreshedExpiration = DateTime.Parse(JObject.Parse(putBody!)["date_expired"]!.ToString());
        Assert.That(refreshedExpiration, Is.EqualTo(DateTime.Now.AddMinutes(45)).Within(TimeSpan.FromSeconds(15)));
    }

    [Test]
    public async Task CustomAuthHandler_UsesSharedFallbackTimeoutWhenTenantOverrideIsMissing()
    {
        string? putBody = null;
        var configSets = CreateConfigSetList();
        var fallbackConfiguration = CreateFallbackConfiguration(sharedTimeoutMinutes: 20, isMultiTenantMode: false);

        using var httpClient = new HttpClient(new RecordingHttpMessageHandler(async request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return CreateJsonResponse(CreateActiveSessionJson());
            }

            putBody = request.Content == null ? null : await request.Content.ReadAsStringAsync();
            return CreateJsonResponse(@"{ ""ok"": true, ""id"": ""session-1"", ""rev"": ""2-test"" }");
        }));

        var result = await AuthenticateWithHandlerAsync(
            CreateRequestTenantRuntime(
                CreateRootRuntimeSettings(isMultiTenantMode: false),
                new List<OverridableConfiguration> { fallbackConfiguration },
                configSets),
            httpClient);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(putBody, Is.Not.Null.And.Not.Empty);

        var refreshedExpiration = DateTime.Parse(JObject.Parse(putBody!)["date_expired"]!.ToString());
        Assert.That(refreshedExpiration, Is.EqualTo(DateTime.Now.AddMinutes(20)).Within(TimeSpan.FromSeconds(15)));
    }

    [Test]
    public async Task CustomAuthHandler_UsesThirtyMinuteDefaultWhenConfigValueIsMissing()
    {
        string? putBody = null;
        var configSets = CreateConfigSetList();
        var fallbackConfiguration = CreateFallbackConfiguration(sharedTimeoutMinutes: null, isMultiTenantMode: false);

        using var httpClient = new HttpClient(new RecordingHttpMessageHandler(async request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return CreateJsonResponse(CreateActiveSessionJson());
            }

            putBody = request.Content == null ? null : await request.Content.ReadAsStringAsync();
            return CreateJsonResponse(@"{ ""ok"": true, ""id"": ""session-1"", ""rev"": ""2-test"" }");
        }));

        var result = await AuthenticateWithHandlerAsync(
            CreateRequestTenantRuntime(
                CreateRootRuntimeSettings(isMultiTenantMode: false),
                new List<OverridableConfiguration> { fallbackConfiguration },
                configSets),
            httpClient);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(putBody, Is.Not.Null.And.Not.Empty);

        var refreshedExpiration = DateTime.Parse(JObject.Parse(putBody!)["date_expired"]!.ToString());
        Assert.That(refreshedExpiration, Is.EqualTo(DateTime.Now.AddMinutes(30)).Within(TimeSpan.FromSeconds(15)));
    }

    [Test]
    public async Task CustomAuthHandler_UsesInjectedCouchDbClientForRoleLookup()
    {
        var observedRequestPaths = new List<string>();
        var configSets = CreateConfigSetList();
        var tenantConfiguration = CreateTenantConfiguration(45, configId: $"{HostPrefix}_shared");

        using var httpClient = new HttpClient(new RecordingHttpMessageHandler(async request =>
        {
            observedRequestPaths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);

            if (request.Method == HttpMethod.Get &&
                request.RequestUri?.AbsolutePath.Contains("/session/", StringComparison.OrdinalIgnoreCase) == true)
            {
                return CreateJsonResponse(CreateActiveSessionJson());
            }

            if (request.Method == HttpMethod.Get &&
                request.RequestUri?.AbsolutePath.Contains("/jurisdiction/_design/sortable/_view/by_user_id", StringComparison.OrdinalIgnoreCase) == true)
            {
                return CreateJsonResponse(CreateJurisdictionRoleViewJson("testuser", "abstractor", "/jurisdiction-a"));
            }

            if (request.Method == HttpMethod.Put)
            {
                return CreateJsonResponse(@"{ ""ok"": true, ""id"": ""session-1"", ""rev"": ""2-test"" }");
            }

            Assert.Fail($"Unexpected request: {request.Method} {request.RequestUri}");
            return CreateJsonResponse("{}");
        }));

        var result = await AuthenticateWithHandlerAsync(
            CreateRequestTenantRuntime(
                CreateRootRuntimeSettings(isMultiTenantMode: true, configuredTenants: [HostPrefix]),
                new List<OverridableConfiguration> { tenantConfiguration },
                configSets),
            httpClient);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(
            observedRequestPaths,
            Has.Some.Contains("/jurisdiction/_design/sortable/_view/by_user_id"),
            "Expected role lookup to flow through the injected shared CouchDB client.");
    }

    [Test]
    public void AccountController_LoginSourceUsesSharedTimeoutResolver()
    {
        var controllerSource = File.ReadAllText(FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "AccountController.cs"));

        Assert.That(controllerSource, Does.Contain("SessionTimeoutHelper.GetSessionIdleTimeoutMinutes"));
        Assert.That(controllerSource, Does.Not.Contain("GetInteger(\"session_idle_timeout_minutes\", host_prefix)"));
    }

    [Test]
    public void AccountControllerOidc_SourceUsesSharedTimeoutResolver()
    {
        var oidcSource = File.ReadAllText(FindRepoRelativePath("source-code", "mmria", "mmria-server", "Controllers", "AccountController.OIDC.cs"));

        Assert.That(oidcSource, Does.Contain("SessionTimeoutHelper.GetSessionIdleTimeoutMinutes"));
        Assert.That(oidcSource, Does.Not.Contain("config_session_idle_timeout_minutes.Value"));
    }

    private static OverridableConfiguration CreateFallbackConfiguration(int? sharedTimeoutMinutes, bool isMultiTenantMode = false)
    {
        var configuration = new OverridableConfiguration
        {
            _id = "shared_config"
        };

        configuration.boolean_keys["shared"] = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        configuration.string_keys["shared"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        configuration.integer_keys["shared"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        configuration.SetBoolean("shared", "is_multi_tenant_mode", isMultiTenantMode);

        if (sharedTimeoutMinutes.HasValue)
        {
            configuration.SetInteger("shared", "session_idle_timeout_minutes", sharedTimeoutMinutes.Value);
        }

        return configuration;
    }

    private static OverridableConfiguration CreateTenantConfiguration(int? tenantTimeoutMinutes, string configId = "tenant4_config")
    {
        var configuration = new OverridableConfiguration
        {
            _id = configId
        };

        configuration.boolean_keys["shared"] = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        configuration.string_keys["shared"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        configuration.integer_keys["shared"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        configuration.integer_keys[HostPrefix] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (tenantTimeoutMinutes.HasValue)
        {
            configuration.SetInteger(HostPrefix, "session_idle_timeout_minutes", tenantTimeoutMinutes.Value);
        }

        return configuration;
    }

    private static List<ConfigurationSet> CreateConfigSetList()
    {
        return new List<ConfigurationSet>
        {
            new()
            {
                _id = HostPrefix,
                detail_list = new Dictionary<string, DBConfigurationDetail>(StringComparer.OrdinalIgnoreCase)
                {
                    [HostPrefix] = new DBConfigurationDetail
                    {
                        url = LocalCouchDbUrl,
                        prefix = string.Empty,
                        user_name = "tester",
                        user_value = "secret"
                    }
                }
            }
        };
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(HostName);
        context.Request.Path = "/api/case";
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        context.RequestServices = new ServiceCollection().BuildServiceProvider();
        return context;
    }

    private static async Task<AuthenticateResult> AuthenticateWithHandlerAsync(
        RequestTenantRuntime tenantRuntime,
        HttpClient httpClient)
    {
        var context = CreateHttpContext();
        context.Request.Headers["Cookie"] = "sid=session-1";

        var handler = new CustomAuthHandler(
            tenantRuntime,
            new CouchDbHttpClient(new FixedHttpClientFactory(httpClient)),
            new StaticOptionsMonitor<CustomAuthOptions>(new CustomAuthOptions { AuthKey = new StringValues("test-key") }),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            new TestSystemClock());

        await handler.InitializeAsync(
            new AuthenticationScheme(CustomAuthOptions.DefaultScheme, CustomAuthOptions.DefaultScheme, typeof(CustomAuthHandler)),
            context);

        return await handler.AuthenticateAsync();
    }

    private static RootRuntimeSettings CreateRootRuntimeSettings(bool isMultiTenantMode, params string[] configuredTenants)
    {
        return new RootRuntimeSettings
        {
            IsMultiTenantMode = isMultiTenantMode,
            ConfiguredTenants = configuredTenants ?? Array.Empty<string>(),
            SharedConfigId = "shared",
            SingleTenantName = HostPrefix
        };
    }

    private static RequestTenantRuntime CreateRequestTenantRuntime(
        RootRuntimeSettings rootRuntimeSettings,
        List<OverridableConfiguration> configurations,
        List<ConfigurationSet> configurationSets)
    {
        var tenantCatalog = new TenantCatalog(rootRuntimeSettings, configurations, configurationSets);

        return new RequestTenantRuntime(
            HostPrefix,
            tenantCatalog.TryResolveConfiguration(HostPrefix),
            tenantCatalog.TryResolveConfigurationSet(HostPrefix),
            tenantCatalog.TryResolveDbConfig(HostPrefix),
            tenantCatalog.IsTenantAvailable(HostPrefix));
    }

    private static string CreateActiveSessionJson()
    {
        return $$"""
        {
          "_id": "session-1",
          "_rev": "1-test",
          "date_expired": "{{DateTime.Now.AddMinutes(5):O}}",
          "user_id": "testuser",
          "role_list": []
        }
        """;
    }

    private static string CreateJurisdictionRoleViewJson(string userName, string roleName, string jurisdictionId)
    {
        return $$"""
        {
          "rows": [
            {
              "key": "{{userName}}",
              "value": {
                "jurisdiction_id": "{{jurisdictionId}}",
                "user_id": "{{userName}}",
                "role_name": "{{roleName}}",
                "is_active": true,
                "effective_start_date": "2020-01-01T00:00:00Z",
                "effective_end_date": "2099-01-01T00:00:00Z"
              }
            }
          ]
        }
        """;
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
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

    private sealed class StaticOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    {
        private readonly TOptions _options;

        public StaticOptionsMonitor(TOptions options)
        {
            _options = options;
        }

        public TOptions CurrentValue => _options;

        public TOptions Get(string? name)
        {
            return _options;
        }

        public IDisposable OnChange(Action<TOptions, string?> listener)
        {
            return EmptyDisposable.Instance;
        }
    }

    private sealed class TestSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
