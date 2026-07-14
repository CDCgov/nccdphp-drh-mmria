using System;
using System.Globalization;
using System.Threading.Tasks;
using mmria.common.getset;
using mmria.common.metadata;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.SystemOffline.DAL;

namespace mmria.common.SharedLibraries.SystemOffline.Manager;

/// <summary>
/// Manager for system offline feature.
/// Delegates CouchDB/service calls to SystemOfflineDAL.
/// Owns message-substitution logic (previously in mmria.server.util.SystemOfflineMessageFormatter).
/// NO outer try/catch — the controller owns error surfacing.
/// </summary>
public sealed class SystemOfflineManager
{
    private readonly SystemOfflineDAL _dal;

    public SystemOfflineManager(SystemOfflineDAL dal)
    {
        _dal = dal;
    }

    public Task<SystemOfflineConfig> LoadConfigAsync(
        string servicesBaseUrl,
        CouchDbRequestOptions requestOptions)
        => _dal.LoadConfigAsync(servicesBaseUrl, requestOptions);

    public Task<document_put_response> SaveConfigAsync(
        SystemOfflineConfig config,
        string servicesBaseUrl,
        CouchDbRequestOptions requestOptions)
        => _dal.SaveConfigAsync(config, servicesBaseUrl, requestOptions);

    /// <summary>
    /// Substitutes template tokens in a system-offline message string.
    /// Moved from mmria.server.util.SystemOfflineMessageFormatter — logic is unchanged.
    /// Tokens: {{warn_date}}, {{offline_date}}, {{outage_duration}}, {{estimated_restoration}}
    /// </summary>
    public string SubstituteMessage(string message, string warnDateUtc, string offlineDateUtc, int restorationHours = 2)
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
        if (string.IsNullOrWhiteSpace(utcStr)) return null;
        if (!DateTime.TryParse(utcStr, null, DateTimeStyles.RoundtripKind, out var dt)) return null;
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
