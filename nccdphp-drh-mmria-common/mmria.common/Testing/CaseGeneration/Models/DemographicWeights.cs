using System;
using System.Collections.Generic;

namespace mmria.common.Testing.CaseGeneration.Models
{
    /// <summary>
    /// Configuration for weighted demographic distributions.
    /// Maps demographic values to their probability weights (0.0-1.0).
    /// </summary>
    public sealed class DemographicWeights
    {
        public Dictionary<string, double> RaceEthnicity { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            {"White", 0.60},
            {"Black", 0.15},
            {"Hispanic", 0.20},
            {"Asian", 0.04},
            {"Other", 0.01}
        };

        public Dictionary<string, double> Education { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            {"High School or Less", 0.40},
            {"Some College", 0.25},
            {"Bachelor's Degree", 0.25},
            {"Advanced Degree", 0.10}
        };

        public Dictionary<string, double> Insurance { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            {"Medicaid", 0.35},
            {"Private", 0.40},
            {"Uninsured", 0.15},
            {"Medicare", 0.08},
            {"Other", 0.02}
        };

        public Dictionary<string, double> AgeRange { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            {"18-25", 0.25},
            {"26-35", 0.50},
            {"36-45", 0.20},
            {"46+", 0.05}
        };

        public Dictionary<string, double> MaritalStatus { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            {"Single", 0.35},
            {"Married", 0.45},
            {"Divorced", 0.15},
            {"Widowed", 0.05}
        };

        public Dictionary<string, double> EmploymentStatus { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            {"Employed", 0.65},
            {"Unemployed", 0.25},
            {"Other", 0.10}
        };

        public Dictionary<string, double> HousingStatus { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            {"Stable", 0.75},
            {"Unstable", 0.15},
            {"Homeless", 0.10}
        };
    }
}

