using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mmria.server.util;

public static class ContainedPathHelper
{
    public static string CreateSafeContainedName(string value, string fallbackName = "item", int maxLength = 120)
    {
        var fallback = string.IsNullOrWhiteSpace(fallbackName) ? "item" : fallbackName.Trim();
        var trimmedValue = new string((value ?? string.Empty).Where(character => !char.IsControl(character)).ToArray()).Trim();
        if (trimmedValue.Length == 0)
        {
            return fallback;
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(trimmedValue.Length);
        foreach (var character in trimmedValue)
        {
            if (char.IsLetterOrDigit(character) || character is '-' or '_' or '.')
            {
                builder.Append(character);
                continue;
            }

            if (char.IsWhiteSpace(character) ||
                character == Path.DirectorySeparatorChar ||
                character == Path.AltDirectorySeparatorChar ||
                invalidCharacters.Contains(character))
            {
                builder.Append('-');
                continue;
            }

            builder.Append('-');
        }

        var normalizedValue = builder
            .ToString()
            .Trim('-', '.', ' ')
            .Replace("--", "-", StringComparison.Ordinal);

        while (normalizedValue.Contains("--", StringComparison.Ordinal))
        {
            normalizedValue = normalizedValue.Replace("--", "-", StringComparison.Ordinal);
        }

        if (normalizedValue.Length == 0)
        {
            normalizedValue = fallback;
        }

        if (normalizedValue.Length > maxLength)
        {
            normalizedValue = normalizedValue[..maxLength].TrimEnd('-', '.', ' ');
        }

        if (normalizedValue.Length == 0)
        {
            normalizedValue = fallback;
        }

        return ValidateContainedName(normalizedValue, nameof(value));
    }

    public static string CreateSafeDownloadFileName(string value, string fallbackName = "download.bin", int maxLength = 180)
    {
        return CreateSafeContainedName(value, fallbackName, maxLength);
    }

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

    public static string EnsureContainedDirectoryExists(string trustedBaseDirectory, string childDirectoryName)
    {
        var safePath = ResolveContainedDirectoryPath(trustedBaseDirectory, childDirectoryName);
        ThrowIfExistingPathIsReparsePoint(safePath, nameof(childDirectoryName));
        Directory.CreateDirectory(safePath);
        return safePath;
    }

    public static FileStream OpenContainedWriteStream(string trustedBaseDirectory, string fileName)
    {
        var safePath = ResolveContainedFilePath(trustedBaseDirectory, fileName);
        ThrowIfExistingPathIsReparsePoint(safePath, nameof(fileName));
        return new FileStream(
            safePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            8192,
            true);
    }

    public static Task<byte[]> ReadContainedFileAsync(string trustedBaseDirectory, string fileName)
    {
        var safePath = ResolveContainedFilePath(trustedBaseDirectory, fileName);
        ThrowIfExistingPathIsReparsePoint(safePath, nameof(fileName));
        return File.ReadAllBytesAsync(safePath);
    }

    public static bool ContainedFileExists(string trustedBaseDirectory, string fileName)
    {
        var safePath = ResolveContainedFilePath(trustedBaseDirectory, fileName);
        if (IsExistingPathReparsePoint(safePath))
        {
            return false;
        }

        return File.Exists(safePath);
    }

    public static void DeleteContainedFile(string trustedBaseDirectory, string fileName)
    {
        var safePath = ResolveContainedFilePath(trustedBaseDirectory, fileName);
        ThrowIfExistingPathIsReparsePoint(safePath, nameof(fileName));
        if (File.Exists(safePath))
        {
            File.Delete(safePath);
        }
    }

    public static void DeleteContainedDirectoryIfEmpty(string trustedBaseDirectory, string childDirectoryName)
    {
        var safePath = ResolveContainedDirectoryPath(trustedBaseDirectory, childDirectoryName);
        ThrowIfExistingPathIsReparsePoint(safePath, nameof(childDirectoryName));
        if (!Directory.Exists(safePath))
        {
            return;
        }

        if (!Directory.EnumerateFileSystemEntries(safePath).Any())
        {
            Directory.Delete(safePath);
        }
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

    private static void ThrowIfExistingPathIsReparsePoint(string path, string paramName)
    {
        if (IsExistingPathReparsePoint(path))
        {
            throw new ArgumentException("Reparse points are not allowed for contained file operations.", paramName);
        }
    }

    private static bool IsExistingPathReparsePoint(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return false;
        }

        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch
        {
            return true;
        }
    }
}
