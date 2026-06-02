using System;
using System.Collections.Generic;
using System.Net.Http;

namespace mmria.common;

/// <summary>
/// IHttpClientFactory shim for static call sites where DI is not available
/// (legacy authorization helpers, CVS DAL, startup config loading, etc.).
/// </summary>
/// <remarks>
/// Backed by a single process-wide pooled <see cref="SocketsHttpHandler"/> so
/// that connections are reused across calls — no per-call handler allocation,
/// no TIME_WAIT churn, no SNAT exhaustion under multi-tenant load.
/// <para>
/// The returned <see cref="HttpClient"/> instances wrap the shared handler with
/// <c>disposeHandler:false</c>, so callers that dispose the returned client do
/// not tear down the shared connection pool.
/// </para>
/// <para>
/// Note: prior attempts to delegate to the DI-registered IHttpClientFactory
/// caused regressions in features that construct this factory directly. Keep
/// this shim self-contained.
/// </para>
/// </remarks>
public class SimpleHttpClientFactory : IHttpClientFactory
{
    private static readonly HashSet<string> s_noRedirectClientNames = new(StringComparer.Ordinal)
    {
        "BackupAdmin"
    };

    private static readonly SocketsHttpHandler s_sharedHandler = new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        MaxConnectionsPerServer = 64,
        AutomaticDecompression = System.Net.DecompressionMethods.All,
    };

    private static readonly SocketsHttpHandler s_noRedirectHandler = new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        MaxConnectionsPerServer = 64,
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
    };

    public HttpClient CreateClient(string name)
    {
        if (s_noRedirectClientNames.Contains(name))
        {
            return new HttpClient(s_noRedirectHandler, disposeHandler: false);
        }

        return new HttpClient(s_sharedHandler, disposeHandler: false);
    }
}
