using mmria_case_generator.Models;

namespace mmria_case_generator.Utilities
{
    /// <summary>
    /// Applies jurisdiction-specific rules to generated case data
    /// </summary>
    public class JurisdictionRuleEngine
    {
        private readonly JurisdictionRules _rules;
        private readonly Random _random;

        public JurisdictionRuleEngine(string jurisdictionCode, Random random)
        {
            _rules = JurisdictionRulesets.GetForJurisdiction(jurisdictionCode);
            _random = random;
        }

        /// <summary>
        /// Apply jurisdiction rules to a field value
        /// </summary>
        public object? ApplyFieldRules(string fieldPath, object? generatedValue, string fieldType)
        {
            // Check if jurisdiction has specific rules for this field
            if (_rules.FieldRules.TryGetValue(fieldPath, out var fieldRule))
            {
                // Use default value if specified
                if (fieldRule.DefaultValue != null)
                {
                    return fieldRule.DefaultValue;
                }

                // Apply allowed values restriction
                if (fieldRule.AllowedValues != null && fieldRule.AllowedValues.Count > 0)
                {
                    var strValue = generatedValue?.ToString();
                    if (strValue != null && !fieldRule.AllowedValues.Contains(strValue))
                    {
                        // Replace with random allowed value
                        return fieldRule.AllowedValues[_random.Next(fieldRule.AllowedValues.Count)];
                    }
                }

                // Apply string length constraints
                if (fieldType == "string" && generatedValue is string strVal)
                {
                    if (fieldRule.MaxLength.HasValue && strVal.Length > fieldRule.MaxLength.Value)
                    {
                        return strVal.Substring(0, fieldRule.MaxLength.Value);
                    }
                    if (fieldRule.MinLength.HasValue && strVal.Length < fieldRule.MinLength.Value)
                    {
                        return strVal.PadRight(fieldRule.MinLength.Value, 'X');
                    }
                }

                // Apply numeric range constraints
                if (fieldType == "number" && generatedValue != null)
                {
                    var numValue = Convert.ToDouble(generatedValue);
                    if (fieldRule.MinValue.HasValue && numValue < fieldRule.MinValue.Value)
                    {
                        return fieldRule.MinValue.Value;
                    }
                    if (fieldRule.MaxValue.HasValue && numValue > fieldRule.MaxValue.Value)
                    {
                        return fieldRule.MaxValue.Value;
                    }
                }
            }

            return generatedValue;
        }

        /// <summary>
        /// Check if a field is required by jurisdiction rules
        /// </summary>
        public bool IsFieldRequired(string fieldPath, bool metadataRequired)
        {
            // Jurisdiction rules can make additional fields required
            if (_rules.RequiredFields.Contains(fieldPath))
            {
                return true;
            }

            // Check field-specific rules
            if (_rules.FieldRules.TryGetValue(fieldPath, out var fieldRule))
            {
                return fieldRule.IsRequired;
            }

            return metadataRequired;
        }

        /// <summary>
        /// Get jurisdiction-specific weight for a value
        /// </summary>
        public double? GetValueWeight(string fieldName, string value)
        {
            var key = $"{fieldName}:{value}".ToLower();
            return _rules.ValueWeights.TryGetValue(key, out var weight) ? weight : null;
        }

        /// <summary>
        /// Get all jurisdiction-specific weights for a field
        /// </summary>
        public Dictionary<string, double>? GetFieldWeights(string fieldName)
        {
            var prefix = $"{fieldName}:".ToLower();
            var weights = _rules.ValueWeights
                .Where(kvp => kvp.Key.StartsWith(prefix))
                .ToDictionary(
                    kvp => kvp.Key.Substring(prefix.Length),
                    kvp => kvp.Value
                );

            return weights.Count > 0 ? weights : null;
        }

        /// <summary>
        /// Validate that generated value meets jurisdiction rules
        /// </summary>
        public (bool isValid, string? errorMessage) ValidateValue(string fieldPath, object? value)
        {
            if (!_rules.FieldRules.TryGetValue(fieldPath, out var fieldRule))
            {
                return (true, null);
            }

            // Check required
            if (fieldRule.IsRequired && value == null)
            {
                return (false, $"{fieldPath} is required by {_rules.JurisdictionCode} jurisdiction");
            }

            // Check allowed values
            if (fieldRule.AllowedValues != null && value != null)
            {
                var strValue = value.ToString();
                if (!fieldRule.AllowedValues.Contains(strValue!))
                {
                    return (false, $"{fieldPath} value '{strValue}' not allowed in {_rules.JurisdictionCode}. Allowed: {string.Join(", ", fieldRule.AllowedValues)}");
                }
            }

            // Check validation pattern
            if (fieldRule.ValidationPattern != null && value != null)
            {
                var strValue = value.ToString();
                if (!System.Text.RegularExpressions.Regex.IsMatch(strValue!, fieldRule.ValidationPattern))
                {
                    return (false, $"{fieldPath} value '{strValue}' does not match required pattern: {fieldRule.ValidationPattern}");
                }
            }

            return (true, null);
        }

        /// <summary>
        /// Get jurisdiction information
        /// </summary>
        public (string code, string name) GetJurisdictionInfo()
        {
            return (_rules.JurisdictionCode, _rules.JurisdictionName);
        }
    }
}
