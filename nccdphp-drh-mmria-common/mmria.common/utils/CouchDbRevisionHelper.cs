using System.Text.RegularExpressions;

namespace mmria.common.utils;

public static class CouchDbRevisionHelper
{
    private static readonly Regex RevisionRegex = new(
        @"^\d+-[A-Za-z0-9]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string NormalizeOptionalRevision(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    public static bool IsValidRevision(string value)
    {
        var normalized = NormalizeOptionalRevision(value);
        return normalized != null && RevisionRegex.IsMatch(normalized);
    }

    public static string NormalizeIncomingRevision(string value)
    {
        var normalized = NormalizeOptionalRevision(value);
        return IsValidRevision(normalized)
            ? normalized
            : null;
    }

    public static string ResolveServerOwnedRevision(string incoming, string existing)
    {
        var normalizedExisting = NormalizeOptionalRevision(existing);
        if (IsValidRevision(normalizedExisting))
        {
            return normalizedExisting;
        }

        var normalizedIncoming = NormalizeOptionalRevision(incoming);
        return IsValidRevision(normalizedIncoming)
            ? normalizedIncoming
            : null;
    }

    public static string DescribeRevisionHandling(string incoming, string existing)
    {
        var normalizedExisting = NormalizeOptionalRevision(existing);
        if (IsValidRevision(normalizedExisting))
        {
            return "resolved_existing";
        }

        var normalizedIncoming = NormalizeOptionalRevision(incoming);
        if (IsValidRevision(normalizedIncoming))
        {
            return "normalized_incoming";
        }

        return "omitted";
    }
}
