using System;
using System.Globalization;

namespace mmria.server.util;

/// <summary>
/// Substitutes template tokens in system-offline message strings with computed date/time values.
/// Tokens use {{...}} syntax. Dates must be stored as UTC ISO strings (with Z suffix).
/// </summary>
/// <remarks>
/// Supported tokens:
///   {{warn_date}}              – Warn date formatted in the server's local time.
///   {{offline_date}}           – Offline date formatted in the server's local time.
///   {{outage_duration}}        – Human-readable span between warn_date and offline_date (e.g. "3 hours").
///   {{estimated_restoration}}  – Offline date + 2 hours, formatted in the server's local time.
/// </remarks>
public static class SystemOfflineMessageFormatter
{
    public static string Substitute(string message, string warnDateUtc, string offlineDateUtc, int restorationHours = 2)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var warnDate    = ParseUtc(warnDateUtc);
        var offlineDate = ParseUtc(offlineDateUtc);

        message = message.Replace("{{warn_date}}",
            warnDate.HasValue ? FormatLocal(warnDate.Value) : "(not set)");

        message = message.Replace("{{offline_date}}",
            offlineDate.HasValue ? FormatLocal(offlineDate.Value) : "(not set)");

        message = message.Replace("{{outage_duration}}",
            FormatSpan(TimeSpan.FromHours(restorationHours)));

        message = message.Replace("{{estimated_restoration}}",
            offlineDate.HasValue ? FormatLocal(offlineDate.Value.AddHours(restorationHours)) : "(not set)");

        return message;
    }

    private static DateTime? ParseUtc(string utcStr)
    {
        if (string.IsNullOrWhiteSpace(utcStr))
            return null;
        if (!DateTime.TryParse(utcStr, null, DateTimeStyles.RoundtripKind, out var dt))
            return null;
        return dt;
    }

    private static string FormatLocal(DateTime utcDt)
        => utcDt.ToLocalTime().ToString("MMMM d, yyyy 'at' h:mm tt");

    private static string FormatSpan(TimeSpan span)
    {
        var totalMinutes = Math.Abs(span.TotalMinutes);
        if (totalMinutes < 60)
        {
            var m = (int)Math.Round(totalMinutes);
            return m == 1 ? "1 minute" : $"{m} minutes";
        }
        var totalHours = Math.Abs(span.TotalHours);
        if (totalHours < 24)
        {
            var h = (int)Math.Round(totalHours);
            return h == 1 ? "1 hour" : $"{h} hours";
        }
        var days = (int)Math.Round(totalHours / 24);
        return days == 1 ? "1 day" : $"{days} days";
    }
}
