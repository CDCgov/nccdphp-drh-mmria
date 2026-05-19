using System.IO;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.Security.FileSystem;

namespace mmria.server.util;

public static class ContainedPathHelper
{
    public static ContainedFileStore.TrustedDirectoryRoot CreateTrustedDirectoryRoot(string baseDirectory) =>
        new(baseDirectory);

    public static ContainedFileStore.ContainedPathSegment CreateContainedPathSegment(string value, string paramName) =>
        new(value, paramName);

    public static string CreateSafeContainedName(string value, string fallbackName = "item", int maxLength = 120) =>
        ContainedFileStore.CreateSafeContainedName(value, fallbackName, maxLength);

    public static string CreateSafeDownloadFileName(string value, string fallbackName = "download.bin", int maxLength = 180) =>
        ContainedFileStore.CreateSafeDownloadFileName(value, fallbackName, maxLength);

    public static string CreateStableArtifactName(string value, string prefix, string extension, int hashLength = 32) =>
        ContainedFileStore.CreateStableArtifactName(value, prefix, extension, hashLength);

    public static string CreateGeneratedArtifactName(string prefix, string extension) =>
        ContainedFileStore.CreateGeneratedArtifactName(prefix, extension);

    public static string NormalizeTrustedDirectoryRoot(string baseDirectory, string paramName = "baseDirectory") =>
        ContainedFileStore.NormalizeTrustedDirectoryRoot(baseDirectory, paramName);

    public static string ResolveContainedDirectoryPath(string trustedBaseDirectory, string childDirectoryName) =>
        ContainedFileStore.ResolveContainedDirectoryPath(trustedBaseDirectory, childDirectoryName);

    public static string ResolveContainedFilePath(string trustedBaseDirectory, string fileName) =>
        ContainedFileStore.ResolveContainedFilePath(trustedBaseDirectory, fileName);

    public static ContainedFileStore.ContainedDirectoryPath ResolveContainedDirectoryPath(
        ContainedFileStore.TrustedDirectoryRoot trustedBaseDirectory,
        ContainedFileStore.ContainedPathSegment childDirectoryName) =>
        ContainedFileStore.ResolveContainedDirectoryPath(trustedBaseDirectory, childDirectoryName);

    public static ContainedFileStore.ContainedFilePath ResolveContainedFilePath(
        ContainedFileStore.TrustedDirectoryRoot trustedBaseDirectory,
        ContainedFileStore.ContainedPathSegment fileName) =>
        ContainedFileStore.ResolveContainedFilePath(trustedBaseDirectory, fileName);

    public static string EnsureContainedDirectoryExists(string trustedBaseDirectory, string childDirectoryName) =>
        ContainedFileStore.EnsureContainedDirectoryExists(trustedBaseDirectory, childDirectoryName);

    public static void EnsureContainedDirectoryExists(ContainedFileStore.ContainedDirectoryPath safePath) =>
        ContainedFileStore.EnsureContainedDirectoryExists(safePath);

    public static FileStream OpenContainedWriteStream(string trustedBaseDirectory, string fileName) =>
        ContainedFileStore.OpenContainedWriteStream(trustedBaseDirectory, fileName);

    public static FileStream OpenContainedWriteStream(ContainedFileStore.ContainedFilePath safePath) =>
        ContainedFileStore.OpenContainedWriteStream(safePath);

    public static FileStream OpenContainedAppendStream(string trustedBaseDirectory, string fileName) =>
        ContainedFileStore.OpenContainedAppendStream(trustedBaseDirectory, fileName);

    public static FileStream OpenContainedAppendStream(ContainedFileStore.ContainedFilePath safePath) =>
        ContainedFileStore.OpenContainedAppendStream(safePath);

    public static Task<byte[]> ReadContainedFileAsync(string trustedBaseDirectory, string fileName) =>
        ContainedFileStore.ReadContainedFileAsync(trustedBaseDirectory, fileName);

    public static Task<byte[]> ReadContainedFileAsync(ContainedFileStore.ContainedFilePath safePath) =>
        ContainedFileStore.ReadContainedFileAsync(safePath);

    public static bool ContainedFileExists(string trustedBaseDirectory, string fileName) =>
        ContainedFileStore.ContainedFileExists(trustedBaseDirectory, fileName);

    public static void DeleteContainedFile(string trustedBaseDirectory, string fileName) =>
        ContainedFileStore.DeleteContainedFile(trustedBaseDirectory, fileName);

    public static void DeleteContainedDirectoryIfEmpty(string trustedBaseDirectory, string childDirectoryName) =>
        ContainedFileStore.DeleteContainedDirectoryIfEmpty(trustedBaseDirectory, childDirectoryName);

    public static bool TryFindExistingFile(string trustedBaseDirectory, string fileName, out FileInfo fileInfo) =>
        ContainedFileStore.TryFindExistingFile(trustedBaseDirectory, fileName, out fileInfo);

    public static bool TryFindExistingDirectory(string trustedBaseDirectory, string directoryName, out DirectoryInfo directoryInfo) =>
        ContainedFileStore.TryFindExistingDirectory(trustedBaseDirectory, directoryName, out directoryInfo);

    public static Task<byte[]> ReadExistingFileByNameAsync(string trustedBaseDirectory, string fileName) =>
        ContainedFileStore.ReadExistingFileByNameAsync(trustedBaseDirectory, fileName);

    public static Task<byte[]> ReadExistingFileAsync(FileInfo fileInfo) =>
        ContainedFileStore.ReadExistingFileAsync(fileInfo);

    public static bool DeleteExistingFileByName(string trustedBaseDirectory, string fileName) =>
        ContainedFileStore.DeleteExistingFileByName(trustedBaseDirectory, fileName);

    public static bool DeleteExistingDirectoryByName(string trustedBaseDirectory, string directoryName, bool recursive) =>
        ContainedFileStore.DeleteExistingDirectoryByName(trustedBaseDirectory, directoryName, recursive);

    public static string ValidateContainedName(string value, string paramName) =>
        ContainedFileStore.ValidateContainedName(value, paramName);
}
