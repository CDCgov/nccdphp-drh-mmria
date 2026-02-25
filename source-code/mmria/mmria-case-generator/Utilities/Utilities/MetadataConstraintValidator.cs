using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using mmria.common.metadata;

namespace mmria_case_generator.Utilities
{
    /// <summary>
    /// Validates generated data against metadata constraints
    /// </summary>
    public class MetadataConstraintValidator
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
        }

        /// <summary>
        /// Validate a generated value against metadata node constraints
        /// </summary>
        public ValidationResult ValidateValue(object? value, node metadataNode, string path)
        {
            var result = new ValidationResult { IsValid = true };

            if (value == null)
            {
                if (metadataNode.is_required == true)
                {
                    result.IsValid = false;
                    result.Errors.Add($"{path}: Required field is null");
                }
                return result;
            }

            switch (metadataNode.type)
            {
                case "string":
                case "textarea":
                    ValidateString(value, metadataNode, path, result);
                    break;
                case "number":
                    ValidateNumber(value, metadataNode, path, result);
                    break;
                case "date":
                case "datetime":
                case "time":
                    ValidateDate(value, metadataNode, path, result);
                    break;
                case "list":
                    ValidateList(value, metadataNode, path, result);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Validate string value
        /// </summary>
        private void ValidateString(object? value, node metadataNode, string path, ValidationResult result)
        {
            var strValue = value?.ToString();
            if (strValue == null) return;

            // Check max length (if property exists and can be parsed)
            if (!string.IsNullOrEmpty(metadataNode.max_length) && 
                int.TryParse(metadataNode.max_length, out var maxLength) && 
                strValue.Length > maxLength)
            {
                result.IsValid = false;
                result.Errors.Add($"{path}: String length {strValue.Length} exceeds max {maxLength}");
            }
        }

        /// <summary>
        /// Validate numeric value
        /// </summary>
        private void ValidateNumber(object? value, node metadataNode, string path, ValidationResult result)
        {
            if (value == null) return;

            double numericValue;
            if (value is double dbl)
            {
                numericValue = dbl;
            }
            else if (value is int i)
            {
                numericValue = i;
            }
            else if (double.TryParse(value.ToString(), out var parsed))
            {
                numericValue = parsed;
            }
            else
            {
                result.IsValid = false;
                result.Errors.Add($"{path}: Cannot parse '{value}' as number");
                return;
            }

            // Basic range validation (node class doesn't have min/max properties)
            // This is a placeholder for future enhancement
        }

        /// <summary>
        /// Validate date value
        /// </summary>
        private void ValidateDate(object? value, node metadataNode, string path, ValidationResult result)
        {
            if (value == null) return;

            DateTime dateValue;
            if (value is DateTime dt)
            {
                dateValue = dt;
            }
            else if (DateTime.TryParse(value.ToString(), out var parsed))
            {
                dateValue = parsed;
            }
            else
            {
                result.IsValid = false;
                result.Errors.Add($"{path}: Cannot parse '{value}' as date");
                return;
            }

            // Check if date is in reasonable range
            if (dateValue.Year < 1900)
            {
                result.Warnings.Add($"{path}: Date {dateValue:yyyy-MM-dd} is before 1900");
            }

            if (dateValue > DateTime.Now)
            {
                result.Warnings.Add($"{path}: Date {dateValue:yyyy-MM-dd} is in the future");
            }
        }

        /// <summary>
        /// Validate list value
        /// </summary>
        private void ValidateList(object? value, node metadataNode, string path, ValidationResult result)
        {
            if (value == null) return;

            // Get valid values from metadata
            var validValues = new HashSet<string>();
            if (metadataNode.values != null)
            {
                foreach (var val in metadataNode.values)
                {
                    if (val.value != null)
                    {
                        validValues.Add(val.value);
                    }
                }
            }

            // Check if value or values are valid
            if (value is List<string> list)
            {
                foreach (var item in list)
                {
                    if (!validValues.Contains(item))
                    {
                        result.Warnings.Add($"{path}: Value '{item}' not in metadata list values");
                    }
                }
            }
            else
            {
                var strValue = value.ToString();
                if (strValue != null && !validValues.Contains(strValue))
                {
                    result.Warnings.Add($"{path}: Value '{strValue}' not in metadata list values");
                }
            }
        }

        /// <summary>
        /// Validate an entire case data dictionary
        /// </summary>
        public ValidationResult ValidateCase(Dictionary<string, object?> caseData, Dictionary<string, node> metadataNodes)
        {
            var result = new ValidationResult { IsValid = true };

            foreach (var kvp in caseData)
            {
                if (metadataNodes.TryGetValue(kvp.Key, out var metadataNode))
                {
                    var fieldResult = ValidateValue(kvp.Value, metadataNode, kvp.Key);
                    if (!fieldResult.IsValid)
                    {
                        result.IsValid = false;
                    }
                    result.Errors.AddRange(fieldResult.Errors);
                    result.Warnings.AddRange(fieldResult.Warnings);
                }
            }

            return result;
        }
    }
}
