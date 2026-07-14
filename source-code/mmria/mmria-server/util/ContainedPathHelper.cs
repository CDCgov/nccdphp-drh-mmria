using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mmria.server.util;

public static class ContainedPathHelper
{
    private static readonly HashSet<string> ReservedWindowsDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON",
        "PRN",
        "AUX",
        "NUL",
        "COM1",
        "COM2",
        "COM3",
        "COM4",
        "COM5",
        "COM6",
        "COM7",
        "COM8",
        "COM9",
        "LPT1",
        "LPT2",
        "LPT3",
        "LPT4",
        "LPT5",
        "LPT6",
        "LPT7",
        "LPT8",
        "LPT9"
    };

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

        if (OperatingSystem.IsWindows() &&
            (rootPath.StartsWith(@"\\?\", StringComparison.Ordinal) || rootPath.StartsWith(@"\\.\", StringComparison.Ordinal)))
        {
            throw new ArgumentException("Base directory must not use the Windows device path namespace.", paramName);
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
        var normalizedRoot = NormalizeTrustedDirectoryRoot(trustedBaseDirectory, nameof(trustedBaseDirectory));
        var safeDirectoryName = ValidateContainedName(childDirectoryName, nameof(childDirectoryName));
        var safePath = ResolveContainedDirectoryPath(normalizedRoot, safeDirectoryName);
        ThrowIfExistingPathOrAncestorIsReparsePoint(safePath, nameof(childDirectoryName));
        var createdDirectory = new DirectoryInfo(normalizedRoot).CreateSubdirectory(safeDirectoryName);
        var createdPath = Path.GetFullPath(createdDirectory.FullName);
        EnsureContainedPath(normalizedRoot, createdPath, nameof(childDirectoryName));
        ThrowIfExistingPathOrAncestorIsReparsePoint(createdPath, nameof(childDirectoryName));
        if (!string.Equals(createdPath, safePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Created directory path did not match the validated path.", nameof(childDirectoryName));
        }

        return createdPath;
    }

    public static FileStream OpenContainedWriteStream(string trustedBaseDirectory, string fileName)
    {
        var safePath = ResolveContainedFilePath(trustedBaseDirectory, fileName);
        ThrowIfExistingPathOrAncestorIsReparsePoint(safePath, nameof(fileName));
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
        ThrowIfExistingPathOrAncestorIsReparsePoint(safePath, nameof(fileName));
        return File.ReadAllBytesAsync(safePath);
    }

    public static bool ContainedFileExists(string trustedBaseDirectory, string fileName)
    {
        var safePath = ResolveContainedFilePath(trustedBaseDirectory, fileName);
        if (HasExistingPathOrAncestorReparsePoint(safePath))
        {
            return false;
        }

        return File.Exists(safePath);
    }

    public static void DeleteContainedFile(string trustedBaseDirectory, string fileName)
    {
        var safePath = ResolveContainedFilePath(trustedBaseDirectory, fileName);
        ThrowIfExistingPathOrAncestorIsReparsePoint(safePath, nameof(fileName));
        if (File.Exists(safePath))
        {
            File.Delete(safePath);
        }
    }

    public static void DeleteContainedDirectoryIfEmpty(string trustedBaseDirectory, string childDirectoryName)
    {
        var safePath = ResolveContainedDirectoryPath(trustedBaseDirectory, childDirectoryName);
        ThrowIfExistingPathOrAncestorIsReparsePoint(safePath, nameof(childDirectoryName));
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

        if (OperatingSystem.IsWindows() && (trimmedValue[^1] == '.' || trimmedValue[^1] == ' '))
        {
            throw new ArgumentException("Path segment must not end with a dot or space.", paramName);
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

        if (IsReservedWindowsDeviceName(trimmedValue))
        {
            throw new ArgumentException("Path segment uses a reserved Windows device name.", paramName);
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

    private static void ThrowIfExistingPathOrAncestorIsReparsePoint(string path, string paramName)
    {
        if (HasExistingPathOrAncestorReparsePoint(path))
        {
            throw new ArgumentException("Reparse points are not allowed for contained file operations.", paramName);
        }
    }

    private static bool HasExistingPathOrAncestorReparsePoint(string path)
    {
        return EnumerateExistingPathChain(path).Any(IsExistingPathReparsePoint);
    }

    private static IEnumerable<string> EnumerateExistingPathChain(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            yield break;
        }

        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrWhiteSpace(root))
        {
            yield break;
        }

        if (File.Exists(root) || Directory.Exists(root))
        {
            yield return root;
        }

        var relativePath = fullPath[root.Length..].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (relativePath.Length == 0)
        {
            yield break;
        }

        var currentPath = root;
        foreach (var segment in relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath = Path.Combine(currentPath, segment);
            if (File.Exists(currentPath) || Directory.Exists(currentPath))
            {
                yield return currentPath;
            }
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

    private static bool IsReservedWindowsDeviceName(string value)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var stem = value;
        int extensionSeparatorIndex = stem.IndexOf('.');
        if (extensionSeparatorIndex >= 0)
        {
            stem = stem[..extensionSeparatorIndex];
        }

        return ReservedWindowsDeviceNames.Contains(stem);
    }
}
