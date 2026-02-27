namespace mmria_server.tests.Helpers;

/// <summary>
/// Miscellaneous helper utilities for tests
/// </summary>
public static class MiscHelpers
{
    /// <summary>
    /// Build metadata URL by replacing template placeholders
    /// </summary>
    /// <param name="multiTenantMetadataUrlTemplate">Template URL with {replace} and {version} placeholders</param>
    /// <param name="targetTestTenant">Target tenant to replace {replace} placeholder</param>
    /// <param name="metadataVersion">Metadata version to replace {version} placeholder</param>
    /// <returns>Fully formed metadata URL</returns>
    public static string BuildMetadataUrl(string multiTenantMetadataUrlTemplate, string targetTestTenant, string metadataVersion)
    {
        if (string.IsNullOrEmpty(multiTenantMetadataUrlTemplate))
        {
            // Fallback to default if template not provided
            return $"https://{targetTestTenant}-mmria.local:12345/api/version/{metadataVersion}/metadata";
        }

        return multiTenantMetadataUrlTemplate
            .Replace("{replace}", targetTestTenant)
            .Replace("{version}", metadataVersion);
    }
}
