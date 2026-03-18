using System;
using System.Collections.Generic;

namespace mmria.common.Testing.CaseGeneration.Models
{
    /// <summary>
    /// Configuration for case data generation
    /// </summary>
    public class GenerationConfig
    {
        public string Jurisdiction { get; set; } = "TESTJURISDICTION";
        public int CaseCount { get; set; } = 10;
        public string MetadataVersion { get; set; } = "25.10.14";
        public string OutputDirectory { get; set; } = "c:\\temp\\sample-cases";
        public string MetadataUrl { get; set; } = "https://test-couchdb.mmria.org/metadata/{version}/mmrds-metadata";
        public GenerationStrategy Strategy { get; set; } = GenerationStrategy.FromName("complete");
        public bool SaveToCouchDb { get; set; } = false;
        public string? CouchDbUrl { get; set; }
        public string? DatabaseName { get; set; } = "mmria";
        public string? CouchDbUsername { get; set; }
        public string? CouchDbPassword { get; set; }
        public bool ValidateBeforeSave { get; set; } = false;
        public int? RandomSeed { get; set; }
        public string CreatedBy { get; set; } = "case-generator";
        public string LastUpdatedBy { get; set; } = "case-generator";
        public string HostState { get; set; } = "";
        public string JurisdictionId { get; set; } = "/";
        public DemographicWeights? DemographicWeights { get; set; } = new();
        public string? ErVisitVitalSignsCountsCsv { get; set; }

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

        public IReadOnlyList<int> GetErVisitVitalSignsCounts()
        {
            if (string.IsNullOrWhiteSpace(ErVisitVitalSignsCountsCsv))
            {
                return Array.Empty<int>();
            }

            var parsed = new List<int>();
            var parts = ErVisitVitalSignsCountsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                if (int.TryParse(part, out var count) && count >= 0)
                {
                    parsed.Add(count);
                }
            }

            return parsed;
        }

        public int? GetErVisitVitalSignsCountForCase(int caseNumber)
        {
            var counts = GetErVisitVitalSignsCounts();
            if (counts.Count == 0 || caseNumber <= 0)
            {
                return null;
            }

            var index = (caseNumber - 1) % counts.Count;
            return counts[index];
        }
    }
}



