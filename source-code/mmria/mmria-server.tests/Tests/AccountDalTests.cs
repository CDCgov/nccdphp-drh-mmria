#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mmria.common.getset;
using mmria.common.SharedLibraries.Account.DAL;
using NUnit.Framework;

namespace mmria_server.tests.Tests;

[TestFixture]
public class AccountDalTests
{
    [Test]
    [Category("Account")]
    public async Task AuthenticateWithSessionAsync_PostsFormUrlEncodedBodyToSessionEndpoint()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new RecordingHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = request.Content == null ? null : await request.Content.ReadAsStringAsync();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true,\"name\":\"user5\",\"roles\":[\"reviewer\"]}", Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler);
        var couchDbClient = new CouchDbHttpClient(new FixedHttpClientFactory(client));
        var dal = new AccountDAL(couchDbClient);

        var result = await dal.AuthenticateWithSessionAsync("user5", "pa ss+&", "http://tenant5-couchdb.local:6984/");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ok, Is.True);
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(capturedRequest.RequestUri!.ToString(), Is.EqualTo("http://tenant5-couchdb.local:6984/_session"));
        Assert.That(capturedRequest.Content?.Headers.ContentType?.MediaType, Is.EqualTo("application/x-www-form-urlencoded"));

        Assert.That(capturedBody, Is.Not.Null.And.Not.Empty);
        var fields = ParseFormUrlEncoded(capturedBody!);
        Assert.That(fields.ContainsKey("name"), Is.True);
        Assert.That(fields.ContainsKey("password"), Is.True);
        Assert.That(fields["name"], Is.EqualTo("user5"));
        Assert.That(fields["password"], Is.EqualTo("pa ss+&"));
    }

    [Test]
    [Category("Account")]
    public async Task AuthenticateWithSessionAsync_TrimsUserNameAndBackfillsMissingResponseName()
    {
        string? capturedBody = null;

        var handler = new RecordingHttpMessageHandler(async request =>
        {
            capturedBody = request.Content == null ? null : await request.Content.ReadAsStringAsync();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true,\"roles\":[\"_admin\"]}", Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler);
        var couchDbClient = new CouchDbHttpClient(new FixedHttpClientFactory(client));
        var dal = new AccountDAL(couchDbClient);

        var result = await dal.AuthenticateWithSessionAsync("  user5  ", "password", "http://tenant5-couchdb.local:6984");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ok, Is.True);
        Assert.That(result.name, Is.EqualTo("user5"));

        Assert.That(capturedBody, Is.Not.Null.And.Not.Empty);
        var fields = ParseFormUrlEncoded(capturedBody!);
        Assert.That(fields["name"], Is.EqualTo("user5"));
        Assert.That(fields["password"], Is.EqualTo("password"));
    }

    private static Dictionary<string, string> ParseFormUrlEncoded(string value)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var segments = value.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            var parts = segment.Split('=', 2);
            var key = UrlDecode(parts[0]);
            var decodedValue = parts.Length > 1 ? UrlDecode(parts[1]) : string.Empty;
            result[key] = decodedValue;
        }

        return result;
    }

    private static string UrlDecode(string value)
    {
        return Uri.UnescapeDataString(value.Replace('+', ' '));
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
