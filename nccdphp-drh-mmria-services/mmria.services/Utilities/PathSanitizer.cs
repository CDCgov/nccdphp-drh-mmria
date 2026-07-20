using System;
using System.IO;

namespace mmria.services.Utilities;

public static class PathSanitizer
{
    // Characters that are invalid in Windows filenames but not returned by
    // Path.GetInvalidFileNameChars() on Linux.  Enforced on all platforms so
    // exported files are usable by Windows clients receiving downloads.
    private static readonly char[] s_crossPlatformUnsafe = { '<', '>', ':', '"', '|', '?', '*' };

    /// <summary>
    /// Validates that a value is safe to use as a single path segment (file or folder name).
    /// Rejects null/whitespace, directory traversal sequences, directory separators, and rooted paths.
    /// </summary>
    public static string ValidatePathSegment(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be null or whitespace.", paramName);

        var trimmed = value.Trim();

        if (trimmed is "." or "..")
            throw new ArgumentException("Value must not be a relative directory segment.", paramName);

        if (trimmed.Contains('/') || trimmed.Contains('\\'))
            throw new ArgumentException("Value must not contain directory separators.", paramName);

        if (Path.IsPathRooted(trimmed))
            throw new ArgumentException("Value must not be a rooted path.", paramName);

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            if (trimmed.Contains(c))
                throw new ArgumentException($"Value contains invalid path character '{c}'.", paramName);
        }

        foreach (var c in s_crossPlatformUnsafe)
        {
            if (trimmed.Contains(c))
                throw new ArgumentException($"Value contains cross-platform unsafe character '{c}'.", paramName);
        }

        return trimmed;
    }

    /// <summary>
    /// Combines a trusted base directory with a single path segment and returns the
    /// canonical absolute path, after confirming the result is contained within the
    /// base directory.  Use this instead of <see cref="Path.Combine"/> whenever one
    /// of the components originates from user-supplied or database-sourced data.
    /// </summary>
    /// <param name="trustedBaseDirectory">
    /// The server-controlled root directory.  Must be a fully-qualified path.
    /// </param>
    /// <param name="segment">
    /// A single file or folder name (no directory separators or traversal sequences).
    /// Validated by <see cref="ValidatePathSegment"/> before use.
    /// </param>
    /// <param name="segmentParamName">Parameter name reported in exception messages.</param>
    /// <returns>The canonical absolute path of the combined result.</returns>
    public static string ResolveContainedPath(string trustedBaseDirectory, string segment, string segmentParamName)
    {
        if (string.IsNullOrWhiteSpace(trustedBaseDirectory))
            throw new ArgumentException("Base directory must not be null or whitespace.", nameof(trustedBaseDirectory));

        var normalizedRoot = Path.GetFullPath(trustedBaseDirectory);
        if (!Path.IsPathFullyQualified(normalizedRoot))
            throw new ArgumentException("Base directory must be a fully-qualified path.", nameof(trustedBaseDirectory));

        // Append separator so the StartsWith containment check cannot be fooled by a
        // sibling directory whose name shares a common prefix with normalizedRoot.
        if (!Path.EndsInDirectorySeparator(normalizedRoot))
            normalizedRoot += Path.DirectorySeparatorChar;

        var validatedSegment = ValidatePathSegment(segment, segmentParamName);
        var combined = Path.GetFullPath(Path.Combine(normalizedRoot, validatedSegment));

        if (!combined.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Resolved path escaped the configured base directory.",
                segmentParamName);

        return combined;
    }

    /// <summary>
    /// Sanitizes a CouchDB document ID for use as a safe folder/file name segment.
    /// Replaces characters that are valid in CouchDB IDs but unsafe in file paths.
    /// </summary>
    public static string SanitizeDocumentId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Document ID must not be null or whitespace.", nameof(id));

        var sanitized = id
            .Replace(":", "-")
            .Replace(".", "-")
            .Replace("/", "-")
            .Replace("\\", "-");

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(c.ToString(), string.Empty);
        }

        return sanitized;
    }
}
