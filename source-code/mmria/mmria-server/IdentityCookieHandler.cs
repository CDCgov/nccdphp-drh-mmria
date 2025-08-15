using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;

namespace mmria.server;
public class IdentityCookieHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityCookieHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext != null)
        {            
            var authenticationCookie = httpContext.Request.Cookies[".AspNetCore.Identity.Application"];
            if (!string.IsNullOrEmpty(authenticationCookie))
            {                
                request.Headers.Add("Cookie", new CookieHeaderValue(".AspNetCore.Identity.Application", authenticationCookie).ToString());
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}