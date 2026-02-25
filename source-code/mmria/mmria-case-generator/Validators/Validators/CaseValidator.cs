using mmria_case_generator.Models;
using mmria_case_generator.Utilities;
using mmria.common.metadata;
using System.Text;

namespace mmria_case_generator.Validators
{
    /// <summary>
    /// Orchestrates validation of generated cases
    /// </summary>
    public class CaseValidator
    {
        private readonly MetadataConstraintValidator _constraintValidator;
        private readonly JurisdictionRuleEngine? _jurisdictionValidator;
        private readonly Dictionary<string, node> _metadataNodes;

        public CaseValidator(Dictionary<string, node> metadataNodes, JurisdictionRuleEngine? jurisdictionValidator = null)
        {
            _constraintValidator = new MetadataConstraintValidator();
            _jurisdictionValidator = jurisdictionValidator;
            _metadataNodes = metadataNodes;
        }

        /// <summary>
        /// Validate a generated case against all rules
        /// </summary>
        public ValidationReport ValidateCase(Dictionary<string, object?> caseData, int caseNumber)
        {
            var report = new ValidationReport
            {
                CaseNumber = caseNumber,
                CaseId = caseData.ContainsKey("_id") ? caseData["_id"]?.ToString() : null
            };

            // 1. Validate against metadata constraints
            var metadataResult = _constraintValidator.ValidateCase(caseData, _metadataNodes);
            report.MetadataErrors.AddRange(metadataResult.Errors);
            report.MetadataWarnings.AddRange(metadataResult.Warnings);

            // 2. Validate against jurisdiction rules (if provided)
            if (_jurisdictionValidator != null)
            {
                ValidateJurisdictionRules(caseData, report);
            }

            // 3. Overall status
            report.IsValid = report.MetadataErrors.Count == 0 && report.JurisdictionErrors.Count == 0;

            return report;
        }

        /// <summary>
        /// Validate jurisdiction-specific rules
        /// </summary>
        private void ValidateJurisdictionRules(Dictionary<string, object?> caseData, ValidationReport report)
        {
            if (_jurisdictionValidator == null) return;

            foreach (var kvp in caseData)
            {
                var (isValid, errorMessage) = _jurisdictionValidator.ValidateValue(kvp.Key, kvp.Value);
                if (!isValid && errorMessage != null)
                {
                    report.JurisdictionErrors.Add(errorMessage);
                }
            }
        }

        /// <summary>
        /// Validate multiple cases and generate summary
        /// </summary>
        public BatchValidationSummary ValidateBatch(List<Dictionary<string, object?>> cases)
        {
            var summary = new BatchValidationSummary
            {
                TotalCases = cases.Count
            };

            for (int i = 0; i < cases.Count; i++)
            {
                var report = ValidateCase(cases[i], i + 1);
                summary.Reports.Add(report);

                if (report.IsValid)
                    summary.ValidCases++;
                else
                    summary.InvalidCases++;

                summary.TotalErrors += report.MetadataErrors.Count + report.JurisdictionErrors.Count;
                summary.TotalWarnings += report.MetadataWarnings.Count + report.JurisdictionWarnings.Count;
            }

            return summary;
        }
    }

    /// <summary>
    /// Validation report for a single case
    /// </summary>
    public class ValidationReport
    {
        public int CaseNumber { get; set; }
        public string? CaseId { get; set; }
        public bool IsValid { get; set; }
        public List<string> MetadataErrors { get; set; } = new();
        public List<string> MetadataWarnings { get; set; } = new();
        public List<string> JurisdictionErrors { get; set; } = new();
        public List<string> JurisdictionWarnings { get; set; } = new();

        public int TotalErrors => MetadataErrors.Count + JurisdictionErrors.Count;
        public int TotalWarnings => MetadataWarnings.Count + JurisdictionWarnings.Count;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Case #{CaseNumber} ({CaseId}): {(IsValid ? "✅ VALID" : "❌ INVALID")}");

            if (MetadataErrors.Count > 0)
            {
                sb.AppendLine($"  Metadata Errors ({MetadataErrors.Count}):");
                foreach (var error in MetadataErrors.Take(5))
                    sb.AppendLine($"    - {error}");
                if (MetadataErrors.Count > 5)
                    sb.AppendLine($"    ... and {MetadataErrors.Count - 5} more");
            }

            if (JurisdictionErrors.Count > 0)
            {
                sb.AppendLine($"  Jurisdiction Errors ({JurisdictionErrors.Count}):");
                foreach (var error in JurisdictionErrors.Take(5))
                    sb.AppendLine($"    - {error}");
                if (JurisdictionErrors.Count > 5)
                    sb.AppendLine($"    ... and {JurisdictionErrors.Count - 5} more");
            }

            if (MetadataWarnings.Count > 0)
            {
                sb.AppendLine($"  Metadata Warnings ({MetadataWarnings.Count}):");
                foreach (var warning in MetadataWarnings.Take(3))
                    sb.AppendLine($"    - {warning}");
                if (MetadataWarnings.Count > 3)
                    sb.AppendLine($"    ... and {MetadataWarnings.Count - 3} more");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Summary of batch validation results
    /// </summary>
    public class BatchValidationSummary
    {
        public int TotalCases { get; set; }
        public int ValidCases { get; set; }
        public int InvalidCases { get; set; }
        public int TotalErrors { get; set; }
        public int TotalWarnings { get; set; }
        public List<ValidationReport> Reports { get; set; } = new();

        public double ValidationRate => TotalCases > 0 ? (double)ValidCases / TotalCases * 100 : 0;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Batch Validation Summary ===");
            sb.AppendLine($"Total Cases: {TotalCases}");
            sb.AppendLine($"Valid Cases: {ValidCases} ({ValidationRate:F1}%)");
            sb.AppendLine($"Invalid Cases: {InvalidCases}");
            sb.AppendLine($"Total Errors: {TotalErrors}");
            sb.AppendLine($"Total Warnings: {TotalWarnings}");
            sb.AppendLine();

            // Show first few invalid cases
            var invalidReports = Reports.Where(r => !r.IsValid).Take(3).ToList();
            if (invalidReports.Count > 0)
            {
                sb.AppendLine("Sample Invalid Cases:");
                foreach (var report in invalidReports)
                {
                    sb.AppendLine(report.ToString());
                }
            }

            return sb.ToString();
        }
    }
}
