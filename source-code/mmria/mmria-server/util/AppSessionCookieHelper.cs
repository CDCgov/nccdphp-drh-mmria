using System;
using Microsoft.AspNetCore.Http;

namespace mmria.server.util;

public static class AppSessionCookieHelper
{
    private const string SessionCookieName = "sid";
    private const string SessionExpiryCookieName = "expires_at";
    private const string CookiePath = "/";

    public static void AppendAppSessionCookies(
        HttpResponse response,
        string sessionId,
        DateTime expiresAt,
        bool isSecure,
        string sessionExpiryCookieValue = null)
    {
        AppendSessionIdCookie(response, sessionId, expiresAt, isSecure);

        if (!string.IsNullOrWhiteSpace(sessionExpiryCookieValue))
        {
            AppendSessionExpiryCookie(response, sessionExpiryCookieValue, expiresAt, isSecure);
        }
    }

    public static void AppendSessionIdCookie(HttpResponse response, string sessionId, DateTime expiresAt, bool isSecure)
    {
        response.Cookies.Append(
            SessionCookieName,
            NormalizeCookieValue(sessionId, nameof(sessionId)),
            CreateCookieOptions(expiresAt, isSecure));
    }

    public static void AppendSessionExpiryCookie(HttpResponse response, string expiresAtValue, DateTime expiresAt, bool isSecure)
    {
        response.Cookies.Append(
            SessionExpiryCookieName,
            NormalizeCookieValue(expiresAtValue, nameof(expiresAtValue)),
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
        var normalizedExpiresAt = NormalizeCookieExpiration(expiresAt);
        var maxAge = normalizedExpiresAt <= DateTimeOffset.UtcNow
            ? TimeSpan.Zero
            : normalizedExpiresAt - DateTimeOffset.UtcNow;

        return new CookieOptions
        {
            HttpOnly = true,
            Expires = normalizedExpiresAt,
            IsEssential = true,
            MaxAge = maxAge,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Secure = isSecure
        };
    }

    private static string NormalizeCookieValue(string value, string paramName)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var trimmedValue = value.Trim();
        if (trimmedValue.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
        {
            throw new ArgumentException("Cookie value contains invalid control characters.", paramName);
        }

        return trimmedValue;
    }

    private static DateTimeOffset NormalizeCookieExpiration(DateTime expiresAt)
    {
        return expiresAt.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(expiresAt),
            DateTimeKind.Local => new DateTimeOffset(expiresAt.ToUniversalTime()),
            _ => new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc))
        };
    }
}
