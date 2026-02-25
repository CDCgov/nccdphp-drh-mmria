namespace mmria_case_generator.Models
{
    /// <summary>
    /// Master configuration for test case data generation
    /// Coordinates generation strategy, output, and authentication settings
    /// </summary>
    public class GenerationConfig
    {
        #region Case Generation Settings
        public string Jurisdiction { get; set; } = "TESTJURISDICTION";
        public int CaseCount { get; set; } = 10;
        public string MetadataVersion { get; set; } = "25.10.14";
        public string MetadataUrl { get; set; } = "https://test-couchdb.mmria.org/metadata/{version}/mmrds-metadata";
        public GenerationStrategy Strategy { get; set; } = GenerationStrategy.FromName("complete");
        public int? RandomSeed { get; set; }
        #endregion

        #region Case Metadata
        public string CreatedBy { get; set; } = "test-data-generator";
        public string LastUpdatedBy { get; set; } = "test-data-generator";
        public string HostState { get; set; } = "";
        public string JurisdictionId { get; set; } = "/";
        #endregion

        #region Demographics
        public DemographicWeights? DemographicWeights { get; set; } = new();
        #endregion

        #region Output & Database Settings
        /// <summary>
        /// Output configuration (file directory, database options)
        /// </summary>
        public TestDataGenerationOutputConfig OutputConfig { get; set; } = new();

        /// <summary>
        /// File output directory (for JSON export)
        /// </summary>
        public string OutputDirectory { get; set; } = "c:\\temp\\sample-cases";
        #endregion

        #region Authentication & Security
        /// <summary>
        /// Authentication configuration for API access
        /// </summary>
        public TestDataGenerationAuthConfig AuthConfig { get; set; } = new();
        #endregion

        #region Helper Methods
        public string GetResolvedMetadataUrl()
        {
            return MetadataUrl.Replace("{version}", MetadataVersion);
        }

        public string GetHostState()
        {
            return string.IsNullOrEmpty(HostState) ? Jurisdiction : HostState;
        }

        public string GetAddQuarter()
        {
            var now = DateTime.UtcNow;
            var quarter = (now.Month - 1) / 3 + 1;
            return $"Q{quarter}-{now.Year}";
        }

        /// <summary>
        /// Check if this configuration is valid for data generation
        /// </summary>
        public bool IsValid(out List<string> errors)
        {
            errors = new();

            if (string.IsNullOrEmpty(Jurisdiction))
                errors.Add("Jurisdiction cannot be empty");

            if (CaseCount <= 0)
                errors.Add("CaseCount must be greater than 0");

            if (OutputConfig?.SaveToDatabase == true)
            {
                if (string.IsNullOrEmpty(OutputConfig.CouchDbUrl))
                    errors.Add("CouchDbUrl required when SaveToDatabase is true");

                var dbName = OutputConfig.GetEffectiveDatabaseName();
                if (string.IsNullOrEmpty(dbName))
                    errors.Add("Database name cannot be determined");
            }

            return errors.Count == 0;
        }
        #endregion
    }
}
