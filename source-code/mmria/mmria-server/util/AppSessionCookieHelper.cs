using System;
using Microsoft.AspNetCore.Http;

namespace mmria.server.util;

public static class AppSessionCookieHelper
{
    public const string SessionCookieName = "__Host-mmria-sid";
    public const string LegacySessionCookieName = "sid";
    public const string SessionExpiryCookieName = "__Host-mmria-expires_at";
    public const string LegacySessionExpiryCookieName = "expires_at";
    public const string SessionScopeCookieName = "mmria_session_scope";
    public const string StandardSessionScopeValue = "standard";
    public const string OfflineModeSessionScopeValue = "offline_mode";
    private const string CookiePath = "/";

    public static string GetSessionIdCookie(HttpRequest request)
    {
        if (request == null)
        {
            return null;
        }

        var sessionId = request.Cookies[SessionCookieName];
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            return NormalizeCookieValue(sessionId, nameof(request));
        }

        var legacySessionId = request.Cookies[LegacySessionCookieName];
        return string.IsNullOrWhiteSpace(legacySessionId)
            ? null
            : NormalizeCookieValue(legacySessionId, nameof(request));
    }

    public static bool HasSessionIdCookie(HttpRequest request) =>
        !string.IsNullOrWhiteSpace(GetSessionIdCookie(request));

    public static void AppendAppSessionCookies(
        HttpResponse response,
        string sessionId,
        DateTime expiresAt,
        bool isSecure,
        string sessionExpiryCookieValue = null,
        string sessionScope = null)
    {
        AppendSessionIdCookie(response, sessionId, expiresAt, isSecure);

        if (!string.IsNullOrWhiteSpace(sessionExpiryCookieValue))
        {
            AppendSessionExpiryCookie(response, sessionExpiryCookieValue, expiresAt, isSecure);
        }

        if (!string.IsNullOrWhiteSpace(sessionScope))
        {
            AppendSessionScopeCookie(response, sessionScope, expiresAt, isSecure);
        }
    }

    public static void AppendSessionIdCookie(HttpResponse response, string sessionId, DateTime expiresAt, bool isSecure)
    {
        response.Cookies.Append(
            SessionCookieName,
            NormalizeCookieValue(sessionId, nameof(sessionId)),
            CreateLiveCookieOptions());
    }

    public static void AppendSessionExpiryCookie(HttpResponse response, string expiresAtValue, DateTime expiresAt, bool isSecure)
    {
        response.Cookies.Append(
            SessionExpiryCookieName,
            NormalizeCookieValue(expiresAtValue, nameof(expiresAtValue)),
            CreateLiveCookieOptions());
    }

    public static void AppendSessionScopeCookie(HttpResponse response, string sessionScope, DateTime expiresAt, bool isSecure)
    {
        ClearReadableSessionScopeCookie(response);
    }

    public static void ClearSessionCookies(HttpResponse response, bool isSecure)
    {
        response.Cookies.Append(
            SessionCookieName,
            string.Empty,
            CreateExpiredCookieOptions());
        response.Cookies.Append(
            SessionExpiryCookieName,
            string.Empty,
            CreateExpiredCookieOptions());
        response.Cookies.Append(
            LegacySessionCookieName,
            string.Empty,
            CreateExpiredCookieOptions());
        response.Cookies.Append(
            LegacySessionExpiryCookieName,
            string.Empty,
            CreateExpiredCookieOptions());

        ClearReadableSessionScopeCookie(response);
    }

    private static void ClearReadableSessionScopeCookie(HttpResponse response)
    {
        response.Cookies.Append(
            SessionScopeCookieName,
            string.Empty,
            CreateExpiredCookieOptions(httpOnly: false));
    }

    private static CookieOptions CreateLiveCookieOptions(bool httpOnly = true)
    {
        return new CookieOptions
        {
            HttpOnly = httpOnly,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Secure = true
        };
    }

    private static CookieOptions CreateExpiredCookieOptions(bool httpOnly = true)
    {
        return new CookieOptions
        {
            HttpOnly = httpOnly,
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            IsEssential = true,
            MaxAge = TimeSpan.Zero,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Secure = true
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
}
