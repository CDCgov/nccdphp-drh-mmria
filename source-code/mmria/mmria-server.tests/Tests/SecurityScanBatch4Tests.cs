#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using mmria.common.SharedLibraries.Account.DAL;
using mmria.common.SharedLibraries.Account.Manager;
using mmria.common.SharedLibraries.Account.Model;
using mmria.common.SharedLibraries.Session.DAL;
using mmria.common.SharedLibraries.Session.Manager;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.server.Controllers;
using mmria.server.util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace mmria_server.tests.Tests;

[TestFixture]
public sealed class SecurityScanBatch4Tests
{
    [Test]
    public void DownloadRequest_ToString_RedactsSensitiveFields_AndSupportsWithExpressions()
    {
        var request = new DownloadRequest
        {
            BeginDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc),
            Mailbox = "Mortality",
            seaBucketKMSKey = "kms-secret",
            clientName = "steve-client",
            clientSecretKey = "super-secret",
            base_url = "https://steve.example/api/",
            file_name = "mortality-export",
            download_directory = @"C:\exports"
        };

        var copy = request with { Mailbox = "PRAMS" };
        var text = copy.ToString();

        Assert.That(copy.Mailbox, Is.EqualTo("PRAMS"));
        Assert.That(copy.clientSecretKey, Is.EqualTo("super-secret"));
        Assert.That(text, Does.Contain("DownloadRequest"));
        Assert.That(text, Does.Contain("PRAMS"));
        Assert.That(text, Does.Contain(request.BeginDate.ToString("O", CultureInfo.InvariantCulture)));
        Assert.That(text, Does.Not.Contain("kms-secret"));
        Assert.That(text, Does.Not.Contain("steve-client"));
        Assert.That(text, Does.Not.Contain("super-secret"));
        Assert.That(text, Does.Not.Contain("https://steve.example/api/"));
        Assert.That(text, Does.Not.Contain(@"C:\exports"));
        Assert.That(text, Does.Not.Contain("mortality-export"));
    }

    [Test]
    public async Task Login_Post_WithLocalReturnUrl_RedirectsLocally()
    {
        var controller = CreateAccountController();

        var result = await controller.Login(
            new ApplicationUser { UserName = "alice", Value = "Password123!" },
            "/case");

        Assert.That(result, Is.TypeOf<RedirectResult>());
        Assert.That(((RedirectResult)result).Url, Is.EqualTo("/case"));
    }

    [Test]
    public async Task Login_Post_WithExternalReturnUrl_FallsBackToHome()
    {
        var controller = CreateAccountController();

        var result = await controller.Login(
            new ApplicationUser { UserName = "alice", Value = "Password123!" },
            "https://evil.test/phish");

        Assert.That(result, Is.TypeOf<RedirectToActionResult>());

        var redirect = (RedirectToActionResult)result;
        Assert.That(redirect.ActionName, Is.EqualTo("Index"));
        Assert.That(redirect.ControllerName, Is.EqualTo("Home"));
    }

    [Test]
    public void OfflineLogin_Post_WithLocalReturnUrl_RedirectsLocally()
    {
        var controller = CreateAccountController();

        var result = controller.OfflineLogin(
            new OfflineApplicationUser { OfflineKey = "abc123" },
            "/offline/home");

        Assert.That(result, Is.TypeOf<RedirectResult>());
        Assert.That(((RedirectResult)result).Url, Is.EqualTo("/offline/home"));
    }

    [Test]
    public void OfflineLogin_Post_WithExternalReturnUrl_FallsBackToHome()
    {
        var controller = CreateAccountController();

        var result = controller.OfflineLogin(
            new OfflineApplicationUser { OfflineKey = "abc123" },
            "https://evil.test/phish");

        Assert.That(result, Is.TypeOf<RedirectToActionResult>());

        var redirect = (RedirectToActionResult)result;
        Assert.That(redirect.ActionName, Is.EqualTo("Index"));
        Assert.That(redirect.ControllerName, Is.EqualTo("Home"));
    }

    [Test]
    public async Task LoggerMetadata_RemovesCreatedByFromSessionItems_AndMasksTheLabel()
    {
        var modulesResponse = """
            {
              "rows": [
                {
                  "value": {
                    "context": "OfflineSync",
                    "offline_session_id": "12345678-abcdef",
                    "user_name": "alice"
                  }
                }
              ]
            }
            """;

        var offlineSessionsResponse = """
            {
              "rows": [
                {
                  "value": {
                    "_id": "12345678-abcdef",
                    "created_by": "alice",
                    "date_created": "2026-03-29T12:34:56Z",
                    "date_last_updated": "2026-03-29T13:00:00Z",
                    "offline_state": "1"
                  }
                }
              ]
            }
            """;

        var httpClient = CreateCouchDbHttpClient(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            return path switch
            {
                "/logging/_design/sortable/_view/by-offline-session" => CreateJsonResponse(modulesResponse),
                "/offline_cases/_design/sortable/_view/lightweight-status-only" => CreateJsonResponse(offlineSessionsResponse),
                _ => throw new InvalidOperationException($"Unexpected logger metadata request: {request.Method} {request.RequestUri}")
            };
        });

        var httpContext = CreateHttpContext();
        var controller = new loggerController(
            new HttpContextAccessor { HttpContext = httpContext },
            CreateRequestTenantRuntime(),
            httpClient)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = await controller.GetMetadata();

        Assert.That(result, Is.TypeOf<JsonResult>());

        var payload = JToken.FromObject(((JsonResult)result).Value!);
        var sessionItem = payload["sessionIds"]!.First!;

        Assert.That(sessionItem["value"]?.Value<string>(), Is.EqualTo("12345678-abcdef"));
        Assert.That(sessionItem["hasLogData"]?.Value<bool>(), Is.True);
        Assert.That(sessionItem["name"]?.Value<string>(), Does.StartWith("12345678..."));
        Assert.That(sessionItem["name"]?.Value<string>(), Does.Not.Contain("alice"));
        Assert.That(sessionItem["createdBy"], Is.Null);
    }

    [Test]
    public void ServerLogViewerSource_DoesNotStoreCreatedByMetadata()
    {
        var source = File.ReadAllText(FindRepoRelativePath(
            "source-code",
            "mmria",
            "mmria-server",
            "wwwroot",
            "scripts",
            "logger",
            "server-log-viewer.js"));

        Assert.That(source, Does.Not.Contain("dataset.createdBy"));
    }

    private static AccountController CreateAccountController()
    {
        var httpContext = CreateHttpContext();
        var httpClient = CreateCouchDbHttpClient(request =>
        {
            var absolutePath = request.RequestUri!.AbsolutePath;

            if (absolutePath == "/session/_design/session_event_sortable/_view/by_user_id")
            {
                return CreateJsonResponse("""{ "total_rows": 0, "offset": 0, "rows": [] }""");
            }

            if (absolutePath == "/_users/org.couchdb.user:alice")
            {
                return CreateJsonResponse("""
                    {
                      "_id": "org.couchdb.user:alice",
                      "name": "alice",
                      "roles": [],
                      "type": "user",
                      "app_prefix_list": { "__no_prefix__": true }
                    }
                    """);
            }

            if (absolutePath == "/_session" && request.Method == HttpMethod.Post)
            {
                return CreateJsonResponse("""{ "ok": true, "name": "alice", "roles": [] }""");
            }

            if (absolutePath == "/jurisdiction/_design/sortable/_view/by_user_id")
            {
                return CreateJsonResponse("""{ "total_rows": 0, "offset": 0, "rows": [] }""");
            }

            if (absolutePath.StartsWith("/session/", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Method == HttpMethod.Get)
                {
                    var sessionId = absolutePath.Split('/').Last();
                    return CreateJsonResponse($$"""
                        {
                          "_id": "{{sessionId}}",
                          "_rev": "1-test",
                          "user_id": "alice",
                          "date_created": "2026-03-29T12:00:00Z",
                          "date_last_updated": "2026-03-29T12:00:00Z",
                          "date_expired": "2026-03-29T13:00:00Z",
                          "is_active": true,
                          "session_event_id": "event-1",
                          "role_list": [],
                          "data": {}
                        }
                        """);
                }

                if (request.Method == HttpMethod.Put)
                {
                    var id = absolutePath.Split('/').Last();
                    return CreateJsonResponse($$"""{ "ok": true, "id": "{{id}}", "rev": "1-test" }""");
                }
            }

            throw new InvalidOperationException($"Unexpected account test request: {request.Method} {request.RequestUri}");
        });

        var tenantRuntime = CreateRequestTenantRuntime();
        var accountDal = new AccountDAL(httpClient);
        var accountManager = new AccountManager(accountDal, httpClient);
        var sessionDal = new SessionDAL(httpClient);
        var sessionManager = new SessionManager(sessionDal);

        var controller = new AccountController(
            new HttpContextAccessor { HttpContext = httpContext },
            sessionManager,
            tenantRuntime,
            httpClient,
            accountManager)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider()),
            Url = new TestUrlHelper()
        };

        return controller;
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.7";
        context.RequestServices = new Microsoft.Extensions.DependencyInjection.ServiceCollection().BuildServiceProvider();
        return context;
    }

    private static CouchDbHttpClient CreateCouchDbHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new RecordingHttpMessageHandler(request => Task.FromResult(responder(request)));
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://tenant4.test")
        };

        return new CouchDbHttpClient(new FixedHttpClientFactory(client));
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static RequestTenantRuntime CreateRequestTenantRuntime()
    {
        const string hostPrefix = "tenant4";
        const string url = "http://tenant4.test";

        var configuration = new OverridableConfiguration
        {
            _id = $"{hostPrefix}_shared"
        };
        configuration.SetBoolean("shared", "sams:is_enabled", false);
        configuration.SetInteger("shared", "unsuccessful_login_attempts_number_before_lockout", 5);
        configuration.SetInteger("shared", "unsuccessful_login_attempts_within_number_of_minutes", 3);
        configuration.SetInteger("shared", "unsuccessful_login_attempts_lockout_number_of_minutes", 3);
        configuration.SetString(hostPrefix, "couchdb_url", url);
        configuration.SetString(hostPrefix, "db_prefix", string.Empty);
        configuration.SetString(hostPrefix, "timer_user_name", "tester");
        configuration.SetString(hostPrefix, "timer_value", "secret");
        configuration.SetBoolean(hostPrefix, "is_offline_mode_enabled", false);
        configuration.SetBoolean(hostPrefix, "is_offline_logging_enabled", false);
        configuration.SetInteger(hostPrefix, "offline_logging_max_logs", 10000);

        var configurationSet = new ConfigurationSet
        {
            _id = hostPrefix
        };
        configurationSet.detail_list[hostPrefix] = new DBConfigurationDetail
        {
            url = url,
            prefix = string.Empty,
            user_name = "tester",
            user_value = "secret"
        };

        return new RequestTenantRuntime(
            hostPrefix,
            configuration,
            configurationSet,
            configurationSet.detail_list[hostPrefix],
            isTenantAvailable: true);
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

    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal);
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class TestUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext => new();

        public string? Action(UrlActionContext actionContext) => null;

        public string? Content(string? contentPath) => contentPath;

        public bool IsLocalUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (url[0] == '/')
            {
                return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
            }

            if (url[0] == '~' && url.Length > 1 && url[1] == '/')
            {
                return true;
            }

            return false;
        }

        public string? Link(string? routeName, object? values) => null;

        public string? RouteUrl(UrlRouteContext routeContext) => null;
    }
}
