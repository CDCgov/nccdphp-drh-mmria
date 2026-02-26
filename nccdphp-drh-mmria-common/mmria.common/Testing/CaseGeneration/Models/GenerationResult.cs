using System;
using System.Collections.Generic;
using mmria.common.Testing.CaseGeneration.Output;
using mmria.common.Testing.CaseGeneration.Validators;

namespace mmria.common.Testing.CaseGeneration.Models
{
    /// <summary>
    /// Result of a case generation operation, including generated cases, validation results, and save outcomes.
    /// </summary>
    public class GenerationResult
    {
        /// <summary>
        /// Indicates whether the generation operation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if operation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Generated case data (list of dictionaries).
        /// </summary>
        public List<Dictionary<string, object?>> GeneratedCases { get; set; } = new();

        /// <summary>
        /// Validation report if validation was performed.
        /// </summary>
        public BatchValidationSummary? ValidationReport { get; set; }

        /// <summary>
        /// CouchDB save results if cases were saved to CouchDB.
        /// </summary>
        public CouchDbBatchResult? CouchDbResult { get; set; }

        /// <summary>
        /// Output directory where JSON files were saved.
        /// </summary>
        public string? OutputDirectory { get; set; }

        /// <summary>
        /// Timestamp when generation completed.
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Summary message suitable for logging.
        /// </summary>
        public string GetSummary()
        {
            if (!Success)
                return $"Generation failed: {ErrorMessage}";

            var summary = $"Generated {GeneratedCases.Count} case(s)";
            
            if (ValidationReport != null)
                summary += $" | Validation: {ValidationReport.ValidationRate:F1}% valid";
            
            if (CouchDbResult != null)
                summary += $" | CouchDB: {CouchDbResult.SuccessCount}/{CouchDbResult.TotalCases} saved";
            
            if (!string.IsNullOrEmpty(OutputDirectory))
                summary += $" | Saved to: {OutputDirectory}";

            return summary;
        }
    }
}

