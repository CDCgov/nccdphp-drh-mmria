using System;
using System.IO;

namespace mmria.services.Utilities;

public static class PathSanitizer
{
    /// <summary>
    /// Validates that a value is safe to use as a single path segment (file or folder name).
    /// Rejects null/whitespace, directory traversal sequences, directory separators, and rooted paths.
    /// </summary>
    public static string ValidatePathSegment(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be null or whitespace.", paramName);

        var trimmed = value.Trim();

        if (trimmed.Contains(".."))
            throw new ArgumentException("Value must not contain directory traversal sequences.", paramName);

        if (trimmed.Contains('/') || trimmed.Contains('\\'))
            throw new ArgumentException("Value must not contain directory separators.", paramName);

        if (Path.IsPathRooted(trimmed))
            throw new ArgumentException("Value must not be a rooted path.", paramName);

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            if (trimmed.Contains(c))
                throw new ArgumentException($"Value contains invalid path character '{c}'.", paramName);
        }

        return trimmed;
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
