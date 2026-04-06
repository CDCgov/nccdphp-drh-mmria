using System;
using Microsoft.AspNetCore.Http;

namespace mmria.server.util;

public static class AppSessionCookieHelper
{
    private const string SessionCookieName = "sid";
    private const string SessionExpiryCookieName = "expires_at";
    private const string CookiePath = "/";

    public static void AppendSessionIdCookie(HttpResponse response, string sessionId, DateTime expiresAt, bool isSecure)
    {
        response.Cookies.Append(
            SessionCookieName,
            sessionId ?? string.Empty,
            CreateCookieOptions(expiresAt, isSecure));
    }

    public static void AppendSessionExpiryCookie(HttpResponse response, string expiresAtValue, DateTime expiresAt, bool isSecure)
    {
        response.Cookies.Append(
            SessionExpiryCookieName,
            expiresAtValue ?? string.Empty,
            CreateCookieOptions(expiresAt, isSecure));
    }

    public static void ClearSessionCookies(HttpResponse response, bool isSecure)
    {
        var expiredAt = DateTime.UtcNow.AddDays(-1);

        response.Cookies.Append(
            SessionCookieName,
            string.Empty,
            CreateCookieOptions(expiredAt, isSecure));
        response.Cookies.Append(
            SessionExpiryCookieName,
            string.Empty,
            CreateCookieOptions(expiredAt, isSecure));
    }

    private static CookieOptions CreateCookieOptions(DateTime expiresAt, bool isSecure)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Expires = expiresAt,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Secure = isSecure
        };
    }
}
