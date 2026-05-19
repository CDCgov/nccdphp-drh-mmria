using System.Security.Cryptography;
using System.Text;

namespace mmria.common.SharedLibraries.Security.FileSystem;

public static class ContainedFileStore
{
    public readonly struct TrustedDirectoryRoot
    {
        public TrustedDirectoryRoot(string value)
        {
            Value = NormalizeTrustedDirectoryRoot(value, nameof(value));
        }

        public string Value { get; }
    }

    public readonly struct ContainedPathSegment
    {
        public ContainedPathSegment(string value, string paramName)
        {
            Value = ValidateContainedName(value, paramName);
        }

        public string Value { get; }
    }

    public readonly struct ContainedDirectoryPath
    {
        internal ContainedDirectoryPath(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    public readonly struct ContainedFilePath
    {
        internal ContainedFilePath(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

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
            return ValidateContainedName(fallback, nameof(fallbackName));
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

    public static string CreateStableArtifactName(string value, string prefix, string extension, int hashLength = 32)
    {
        var safePrefix = CreateSafeContainedName(prefix, "artifact", 48);
        var normalizedExtension = NormalizeExtension(extension);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
        var hash = Convert.ToHexString(bytes).ToLowerInvariant();
        var length = Math.Clamp(hashLength, 12, hash.Length);

        return ValidateContainedName($"{safePrefix}-{hash[..length]}{normalizedExtension}", nameof(value));
    }

    public static string CreateGeneratedArtifactName(string prefix, string extension)
    {
        var safePrefix = CreateSafeContainedName(prefix, "artifact", 48);
        var normalizedExtension = NormalizeExtension(extension);
        return ValidateContainedName($"{safePrefix}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{normalizedExtension}", nameof(prefix));
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
        var path = ResolveContainedDirectoryPath(
            new TrustedDirectoryRoot(trustedBaseDirectory),
            new ContainedPathSegment(childDirectoryName, nameof(childDirectoryName)));
        return path.Value;
    }

    public static string ResolveContainedFilePath(string trustedBaseDirectory, string fileName)
    {
        var path = ResolveContainedFilePath(
            new TrustedDirectoryRoot(trustedBaseDirectory),
            new ContainedPathSegment(fileName, nameof(fileName)));
        return path.Value;
    }

    public static ContainedDirectoryPath ResolveContainedDirectoryPath(TrustedDirectoryRoot trustedBaseDirectory, ContainedPathSegment childDirectoryName)
    {
        var combinedPath = Path.GetFullPath(Path.Combine(trustedBaseDirectory.Value, childDirectoryName.Value));
        EnsureContainedPath(trustedBaseDirectory.Value, combinedPath, nameof(childDirectoryName));
        return new ContainedDirectoryPath(combinedPath);
    }

    public static ContainedFilePath ResolveContainedFilePath(TrustedDirectoryRoot trustedBaseDirectory, ContainedPathSegment fileName)
    {
        var combinedPath = Path.GetFullPath(Path.Combine(trustedBaseDirectory.Value, fileName.Value));
        EnsureContainedPath(trustedBaseDirectory.Value, combinedPath, nameof(fileName));
        return new ContainedFilePath(combinedPath);
    }

    public static string EnsureContainedDirectoryExists(string trustedBaseDirectory, string childDirectoryName)
    {
        var safePath = ResolveContainedDirectoryPath(
            new TrustedDirectoryRoot(trustedBaseDirectory),
            new ContainedPathSegment(childDirectoryName, nameof(childDirectoryName)));
        EnsureContainedDirectoryExists(safePath);
        return safePath.Value;
    }

    public static void EnsureContainedDirectoryExists(ContainedDirectoryPath safePath)
    {
        ThrowIfExistingPathOrAncestorIsReparsePoint(safePath.Value, nameof(safePath));
        Directory.CreateDirectory(safePath.Value);
    }

    public static FileStream OpenContainedWriteStream(string trustedBaseDirectory, string fileName)
    {
        var safePath = ResolveContainedFilePath(
            new TrustedDirectoryRoot(trustedBaseDirectory),
            new ContainedPathSegment(fileName, nameof(fileName)));
        return OpenContainedWriteStream(safePath);
    }

    public static FileStream OpenContainedWriteStream(ContainedFilePath safePath)
    {
        ThrowIfExistingPathOrAncestorIsReparsePoint(safePath.Value, nameof(safePath));
        return new FileStream(
            safePath.Value,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            8192,
            true);
    }

    public static FileStream OpenContainedAppendStream(string trustedBaseDirectory, string fileName)
    {
        var safePath = ResolveContainedFilePath(
            new TrustedDirectoryRoot(trustedBaseDirectory),
            new ContainedPathSegment(fileName, nameof(fileName)));
        return OpenContainedAppendStream(safePath);
    }

    public static FileStream OpenContainedAppendStream(ContainedFilePath safePath)
    {
        ThrowIfExistingPathOrAncestorIsReparsePoint(safePath.Value, nameof(safePath));
        return new FileStream(
            safePath.Value,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            8192,
            true);
    }

    public static Task<byte[]> ReadContainedFileAsync(string trustedBaseDirectory, string fileName)
    {
        var safePath = ResolveContainedFilePath(
            new TrustedDirectoryRoot(trustedBaseDirectory),
            new ContainedPathSegment(fileName, nameof(fileName)));
        return ReadContainedFileAsync(safePath);
    }

    public static Task<byte[]> ReadContainedFileAsync(ContainedFilePath safePath)
    {
        ThrowIfExistingPathOrAncestorIsReparsePoint(safePath.Value, nameof(safePath));
        return File.ReadAllBytesAsync(safePath.Value);
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

    public static bool TryFindExistingFile(string trustedBaseDirectory, string fileName, out FileInfo fileInfo)
    {
        fileInfo = null;
        var safeFileName = ValidateContainedName(fileName, nameof(fileName));
        var rootDirectory = GetTrustedRootDirectory(trustedBaseDirectory);
        if (rootDirectory == null)
        {
            return false;
        }

        foreach (var candidate in rootDirectory.EnumerateFiles())
        {
            if (!string.Equals(candidate.Name, safeFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (HasExistingPathOrAncestorReparsePoint(candidate.FullName))
            {
                return false;
            }

            fileInfo = candidate;
            return true;
        }

        return false;
    }

    public static bool TryFindExistingDirectory(string trustedBaseDirectory, string directoryName, out DirectoryInfo directoryInfo)
    {
        directoryInfo = null;
        var safeDirectoryName = ValidateContainedName(directoryName, nameof(directoryName));
        var rootDirectory = GetTrustedRootDirectory(trustedBaseDirectory);
        if (rootDirectory == null)
        {
            return false;
        }

        foreach (var candidate in rootDirectory.EnumerateDirectories())
        {
            if (!string.Equals(candidate.Name, safeDirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (HasExistingPathOrAncestorReparsePoint(candidate.FullName))
            {
                return false;
            }

            directoryInfo = candidate;
            return true;
        }

        return false;
    }

    public static async Task<byte[]> ReadExistingFileByNameAsync(string trustedBaseDirectory, string fileName)
    {
        if (!TryFindExistingFile(trustedBaseDirectory, fileName, out var fileInfo))
        {
            throw new FileNotFoundException("The requested file was not found.", fileName);
        }

        return await ReadExistingFileAsync(fileInfo);
    }

    public static Task<byte[]> ReadExistingFileAsync(FileInfo fileInfo)
    {
        if (fileInfo == null)
        {
            throw new ArgumentNullException(nameof(fileInfo));
        }

        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("The requested file was not found.", fileInfo.Name);
        }

        ThrowIfExistingPathOrAncestorIsReparsePoint(fileInfo.FullName, nameof(fileInfo));
        return File.ReadAllBytesAsync(fileInfo.FullName);
    }

    public static bool DeleteExistingFileByName(string trustedBaseDirectory, string fileName)
    {
        if (!TryFindExistingFile(trustedBaseDirectory, fileName, out var fileInfo))
        {
            return false;
        }

        File.Delete(fileInfo.FullName);
        return true;
    }

    public static bool DeleteExistingDirectoryByName(string trustedBaseDirectory, string directoryName, bool recursive)
    {
        if (!TryFindExistingDirectory(trustedBaseDirectory, directoryName, out var directoryInfo))
        {
            return false;
        }

        directoryInfo.Delete(recursive);
        return true;
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

        if (trimmedValue[^1] == '.' || trimmedValue[^1] == ' ')
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

    private static DirectoryInfo GetTrustedRootDirectory(string trustedBaseDirectory)
    {
        var normalizedRoot = NormalizeTrustedDirectoryRoot(trustedBaseDirectory, nameof(trustedBaseDirectory));
        if (HasExistingPathOrAncestorReparsePoint(normalizedRoot) || !Directory.Exists(normalizedRoot))
        {
            return null;
        }

        return new DirectoryInfo(normalizedRoot);
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var normalizedExtension = extension.Trim();
        if (!normalizedExtension.StartsWith(".", StringComparison.Ordinal))
        {
            normalizedExtension = "." + normalizedExtension;
        }

        return ValidateContainedName("file" + normalizedExtension, nameof(extension))["file".Length..];
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
        if (string.IsNullOrWhiteSpace(value))
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
