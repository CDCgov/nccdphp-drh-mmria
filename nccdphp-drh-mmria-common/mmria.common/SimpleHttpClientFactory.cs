using System.Net.Http;

namespace mmria.common;

/// <summary>
/// Simple IHttpClientFactory implementation for use in static methods where dependency injection is not available
/// </summary>
public class SimpleHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient();
    }
}
