namespace mmria_case_generator.Models
{
    /// <summary>
    /// Authentication configuration for test data generation API access
    /// Supports API key-based authentication for CI/CD, UI, and tests
    /// </summary>
    public class TestDataGenerationAuthConfig
    {
        /// <summary>
        /// Enable authentication requirement (default: false for backward compatibility)
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// API Key for authorization
        /// Can be set via configuration file, environment variable, or header
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Header name for API key (default: X-API-Key)
        /// </summary>
        public string ApiKeyHeaderName { get; set; } = "X-API-Key";

        /// <summary>
        /// API key validation method
        /// Options: "header" (X-API-Key header), "bearer" (Authorization Bearer token), "query" (?api_key=value)
        /// </summary>
        public string ValidationMethod { get; set; } = "header";

        /// <summary>
        /// Validate the provided key against configured key
        /// </summary>
        public bool ValidateKey(string? providedKey)
        {
            if (!Enabled)
                return true;

            if (string.IsNullOrEmpty(ApiKey))
                return false;

            return ApiKey.Equals(providedKey, StringComparison.Ordinal);
        }
    }
}
