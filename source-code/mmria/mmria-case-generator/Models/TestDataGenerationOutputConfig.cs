namespace mmria_case_generator.Models
{
    /// <summary>
    /// Configuration for test data output and database settings
    /// Supports multi-tenant scenarios with configurable database selection
    /// </summary>
    public class TestDataGenerationOutputConfig
    {
        /// <summary>
        /// Whether to save generated cases to database
        /// </summary>
        public bool SaveToDatabase { get; set; } = false;

        /// <summary>
        /// Target database selection mode
        /// Options: "configured" (use DatabaseName), "multi-tenant" (select by tenant), "test" (test database)
        /// </summary>
        public string DatabaseSelectionMode { get; set; } = "configured";

        /// <summary>
        /// Default database name (used when DatabaseSelectionMode = "configured")
        /// </summary>
        public string? DatabaseName { get; set; }

        /// <summary>
        /// Test database name prefix (used when DatabaseSelectionMode = "test")
        /// Full name will be: {DatabaseNamePrefix}-{timestamp}
        /// </summary>
        public string TestDatabaseNamePrefix { get; set; } = "mmria-test";

        /// <summary>
        /// CouchDB connection URL
        /// </summary>
        public string? CouchDbUrl { get; set; }

        /// <summary>
        /// CouchDB username (optional for authenticated connections)
        /// </summary>
        public string? CouchDbUsername { get; set; }

        /// <summary>
        /// CouchDB password (optional for authenticated connections)
        /// </summary>
        public string? CouchDbPassword { get; set; }

        /// <summary>
        /// Create database if it doesn't exist
        /// </summary>
        public bool CreateDatabaseIfNotExists { get; set; } = false;

        /// <summary>
        /// Validate cases before saving to database
        /// </summary>
        public bool ValidateBeforeSave { get; set; } = false;

        /// <summary>
        /// Get the effective database name based on selection mode
        /// </summary>
        public string GetEffectiveDatabaseName(string? tenantId = null)
        {
            return DatabaseSelectionMode.ToLower() switch
            {
                "test" => $"{TestDatabaseNamePrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                "multi-tenant" => string.IsNullOrEmpty(tenantId) ? DatabaseName ?? "mmria" : tenantId,
                "configured" or _ => DatabaseName ?? "mmria"
            };
        }
    }
}
