namespace mmria_case_generator.Models
{
    /// <summary>
    /// Jurisdiction-specific data generation rules
    /// </summary>
    public class JurisdictionRules
    {
        public string JurisdictionCode { get; set; } = string.Empty;
        public string JurisdictionName { get; set; } = string.Empty;
        public Dictionary<string, FieldRule> FieldRules { get; set; } = new();
        public Dictionary<string, double> ValueWeights { get; set; } = new();
        public List<string> RequiredFields { get; set; } = new();
        public List<string> OptionalFields { get; set; } = new();
    }

    /// <summary>
    /// Rules for a specific field
    /// </summary>
    public class FieldRule
    {
        public string FieldPath { get; set; } = string.Empty;
        public string? DefaultValue { get; set; }
        public List<string>? AllowedValues { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public string? ValidationPattern { get; set; }
        public bool IsRequired { get; set; }
    }

    /// <summary>
    /// Predefined jurisdiction configurations
    /// </summary>
    public static class JurisdictionRulesets
    {
        public static Dictionary<string, JurisdictionRules> GetAll() => new()
        {
            // Massachusetts
            ["MA"] = new JurisdictionRules
            {
                JurisdictionCode = "MA",
                JurisdictionName = "Massachusetts",
                FieldRules = new Dictionary<string, FieldRule>
                {
                    ["home_record/case_opening_date"] = new FieldRule
                    {
                        FieldPath = "home_record/case_opening_date",
                        IsRequired = true
                    },
                    ["home_record/record_id"] = new FieldRule
                    {
                        FieldPath = "home_record/record_id",
                        ValidationPattern = @"^MA-\d{6}$",
                        IsRequired = true
                    }
                },
                ValueWeights = new Dictionary<string, double>
                {
                    // Massachusetts demographic approximations
                    ["race:white"] = 0.76,
                    ["race:black"] = 0.09,
                    ["race:asian"] = 0.07,
                    ["race:hispanic"] = 0.12,
                    ["race:other"] = 0.03
                },
                RequiredFields = new List<string>
                {
                    "home_record/case_opening_date",
                    "home_record/date_of_death",
                    "death_certificate/certificate_date"
                }
            },

            // Texas
            ["TX"] = new JurisdictionRules
            {
                JurisdictionCode = "TX",
                JurisdictionName = "Texas",
                FieldRules = new Dictionary<string, FieldRule>
                {
                    ["home_record/record_id"] = new FieldRule
                    {
                        FieldPath = "home_record/record_id",
                        ValidationPattern = @"^TX-\d{6}$",
                        IsRequired = true
                    }
                },
                ValueWeights = new Dictionary<string, double>
                {
                    // Texas demographic approximations
                    ["race:white"] = 0.42,
                    ["race:black"] = 0.12,
                    ["race:asian"] = 0.05,
                    ["race:hispanic"] = 0.39,
                    ["race:other"] = 0.02
                },
                RequiredFields = new List<string>
                {
                    "home_record/case_opening_date",
                    "home_record/date_of_death"
                }
            },

            // California
            ["CA"] = new JurisdictionRules
            {
                JurisdictionCode = "CA",
                JurisdictionName = "California",
                FieldRules = new Dictionary<string, FieldRule>
                {
                    ["home_record/record_id"] = new FieldRule
                    {
                        FieldPath = "home_record/record_id",
                        ValidationPattern = @"^CA-\d{6}$",
                        IsRequired = true
                    }
                },
                ValueWeights = new Dictionary<string, double>
                {
                    // California demographic approximations
                    ["race:white"] = 0.37,
                    ["race:black"] = 0.06,
                    ["race:asian"] = 0.15,
                    ["race:hispanic"] = 0.39,
                    ["race:other"] = 0.03
                },
                RequiredFields = new List<string>
                {
                    "home_record/case_opening_date",
                    "home_record/date_of_death"
                }
            },

            // New York
            ["NY"] = new JurisdictionRules
            {
                JurisdictionCode = "NY",
                JurisdictionName = "New York",
                FieldRules = new Dictionary<string, FieldRule>
                {
                    ["home_record/record_id"] = new FieldRule
                    {
                        FieldPath = "home_record/record_id",
                        ValidationPattern = @"^NY-\d{6}$",
                        IsRequired = true
                    }
                },
                ValueWeights = new Dictionary<string, double>
                {
                    // New York demographic approximations
                    ["race:white"] = 0.56,
                    ["race:black"] = 0.14,
                    ["race:asian"] = 0.09,
                    ["race:hispanic"] = 0.19,
                    ["race:other"] = 0.02
                },
                RequiredFields = new List<string>
                {
                    "home_record/case_opening_date",
                    "home_record/date_of_death"
                }
            },

            // Default/Test jurisdiction
            ["TEST"] = new JurisdictionRules
            {
                JurisdictionCode = "TEST",
                JurisdictionName = "Test Jurisdiction",
                FieldRules = new Dictionary<string, FieldRule>(),
                ValueWeights = new Dictionary<string, double>
                {
                    // US national averages
                    ["race:white"] = 0.60,
                    ["race:black"] = 0.13,
                    ["race:asian"] = 0.06,
                    ["race:hispanic"] = 0.18,
                    ["race:other"] = 0.03
                },
                RequiredFields = new List<string>()
            }
        };

        public static JurisdictionRules GetForJurisdiction(string jurisdictionCode)
        {
            var all = GetAll();
            return all.ContainsKey(jurisdictionCode) ? all[jurisdictionCode] : all["TEST"];
        }
    }
}
