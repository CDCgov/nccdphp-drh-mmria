using System;
using System.IO;

namespace mmria.server.util;

public static class ContainedPathHelper
{
    public static string NormalizeTrustedDirectoryRoot(string baseDirectory, string paramName = "baseDirectory")
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new ArgumentException("Base directory is required.", paramName);
        }

        var rootPath = Path.GetFullPath(baseDirectory);
        if (!Path.IsPathFullyQualified(rootPath))
        {
            throw new ArgumentException("Base directory must be fully qualified.", paramName);
        }

        return Path.EndsInDirectorySeparator(rootPath)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;
    }

    public static string ResolveContainedDirectoryPath(string trustedBaseDirectory, string childDirectoryName)
    {
        var normalizedRoot = NormalizeTrustedDirectoryRoot(trustedBaseDirectory, nameof(trustedBaseDirectory));
        var safeDirectoryName = ValidateContainedName(childDirectoryName, nameof(childDirectoryName));
        var combinedPath = Path.GetFullPath(Path.Combine(normalizedRoot, safeDirectoryName));
        EnsureContainedPath(normalizedRoot, combinedPath, nameof(childDirectoryName));
        return combinedPath;
    }

    public static string ResolveContainedFilePath(string trustedBaseDirectory, string fileName)
    {
        var normalizedRoot = NormalizeTrustedDirectoryRoot(trustedBaseDirectory, nameof(trustedBaseDirectory));
        var safeFileName = ValidateContainedName(fileName, nameof(fileName));
        var combinedPath = Path.GetFullPath(Path.Combine(normalizedRoot, safeFileName));
        EnsureContainedPath(normalizedRoot, combinedPath, nameof(fileName));
        return combinedPath;
    }

    public static FileStream OpenContainedWriteStream(string trustedBaseDirectory, string fileName)
    {
        var safePath = ResolveContainedFilePath(trustedBaseDirectory, fileName);
        return new FileStream(
            safePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            8192,
            true);
    }

    public static string ValidateContainedName(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty path segment is required.", paramName);
        }

        var trimmedValue = value.Trim();
        if (trimmedValue is "." or "..")
        {
            throw new ArgumentException("Relative path operators are not allowed.", paramName);
        }

        if (Path.IsPathRooted(trimmedValue) ||
            trimmedValue.Contains(Path.DirectorySeparatorChar) ||
            trimmedValue.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("Only a single file or directory name is allowed.", paramName);
        }

        if (trimmedValue.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Path segment contains invalid filename characters.", paramName);
        }

        return trimmedValue;
    }

    private static void EnsureContainedPath(string trustedBaseDirectory, string resolvedPath, string paramName)
    {
        if (!resolvedPath.StartsWith(trustedBaseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Resolved path escaped the configured base directory.", paramName);
        }
    }
}
