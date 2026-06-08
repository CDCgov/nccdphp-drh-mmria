using System;
using System.IO;
using mmria.common.SharedLibraries.Security.FileSystem;

namespace mmria.services.Utilities;

public static class PathSanitizer
{
    /// <summary>
    /// Validates that a value is safe to use as a single path segment (file or folder name).
    /// Rejects null/whitespace, directory traversal sequences, directory separators, and rooted paths.
    /// </summary>
    public static string ValidatePathSegment(string value, string paramName)
    {
        return ContainedFileStore.ValidateContainedName(value, paramName);
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

        return ContainedFileStore.CreateSafeContainedName(sanitized, "document");
    }
}
