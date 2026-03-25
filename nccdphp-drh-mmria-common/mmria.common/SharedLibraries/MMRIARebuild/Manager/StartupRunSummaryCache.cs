using System;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.MMRIARebuild.Manager;

internal static class StartupRunSummaryCache
{
    private static readonly ConcurrentDictionary<string, JObject> s_summary_by_host_prefix =
        new(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeHostPrefix(string hostPrefix)
    {
        return string.IsNullOrWhiteSpace(hostPrefix) ? "shared" : hostPrefix.Trim();
    }

    public static bool TryGet(string hostPrefix, out JObject summary)
    {
        summary = null;

        if (!s_summary_by_host_prefix.TryGetValue(NormalizeHostPrefix(hostPrefix), out var cachedSummary) ||
            cachedSummary == null)
        {
            return false;
        }

        summary = (JObject)cachedSummary.DeepClone();
        return true;
    }

    public static void Set(string hostPrefix, JObject summary)
    {
        if (summary == null)
        {
            return;
        }

        s_summary_by_host_prefix[NormalizeHostPrefix(hostPrefix)] = (JObject)summary.DeepClone();
    }

    public static void Remove(string hostPrefix)
    {
        s_summary_by_host_prefix.TryRemove(NormalizeHostPrefix(hostPrefix), out _);
    }

    internal static void ClearForTests()
    {
        s_summary_by_host_prefix.Clear();
    }
}


