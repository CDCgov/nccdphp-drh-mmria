using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using mmria.common.Testing.CaseGeneration.Models;
using mmria.common.metadata;

namespace mmria.common.Testing.CaseGeneration.Generators.ValueGenerators
{
    public class BooleanValueGenerator : ValueGeneratorBase
    {
        private readonly MetadataManager? _metadataManager;

        public BooleanValueGenerator(Faker faker, GenerationStrategy strategy, Random random, MetadataManager? metadataManager = null)
            : base(faker, strategy, random)
        {
            _metadataManager = metadataManager;
        }

        public bool? Generate(string fieldName, bool isRequired)
        {
            if (!ShouldPopulateField(isRequired)) return false;

            var lowerName = fieldName.ToLower();
            
            // Context-aware probabilities
            if (lowerName.Contains("prenatal_care") || lowerName.Contains("received_care"))
                return Random.NextDouble() < 0.8; // 80% yes
            if (lowerName.Contains("married"))
                return Random.NextDouble() < 0.5; // 50% yes
            if (lowerName.Contains("hispanic") || lowerName.Contains("latino"))
                return Random.NextDouble() < 0.18; // 18% yes (US demographic)

            return Strategy.GenerateEdgeCases ? Random.NextDouble() < 0.8 : Random.Next(2) == 0;
        }

        /// <summary>
        /// Generate a yes/no value using metadata lookup codes instead of hardcoded strings.
        /// Returns numeric code from lookup, or null if field should not be populated.
        /// </summary>
        public object? GenerateYesNo(string fieldName, bool isRequired)
        {
            var value = Generate(fieldName, isRequired);
            
            if (value == null)
                return null;

            // If metadata manager is available, use lookup codes
            if (_metadataManager != null)
            {
                var code = value == true 
                    ? GetYesNoCode(true, fieldName)
                    : GetYesNoCode(false, fieldName);
                return code;
            }

            // Fallback to legacy string format if no metadata manager
            return value == true ? "Yes" : "No";
        }

        /// <summary>
        /// Generate a 4-state boolean value: Yes/No/Unknown or null (for blank).
        /// Returns numeric codes from lookup instead of hardcoded strings.
        /// Used for contributing factors and other questions that allow uncertainty.
        /// </summary>
        public object? GenerateFourState(string fieldName, bool isRequired)
        {
            if (!ShouldPopulateField(isRequired)) return null; // Blank

            double roll = Random.NextDouble();

            string selectedState;

            if (Strategy.RequiredFieldsOnly)
            {
                // For required-only mode, strongly prefer Yes/No
                selectedState = roll < 0.5 ? "Yes" : "No";
            }
            else if (Strategy.GenerateEdgeCases)
            {
                // For edge cases, more extreme variation
                if (roll < 0.3) selectedState = "Yes";
                else if (roll < 0.6) selectedState = "No";
                else if (roll < 0.9) selectedState = "Unknown";
                else return null; // Blank
            }
            else
            {
                // Default distribution: 40% Yes, 30% No, 20% Unknown, 10% Blank
                if (roll < 0.4) selectedState = "Yes";
                else if (roll < 0.7) selectedState = "No";
                else if (roll < 0.9) selectedState = "Unknown";
                else return null; // Blank
            }

            // If metadata manager is available, use lookup codes
            if (_metadataManager != null)
            {
                var code = GetYesNoUnknownCode(selectedState, fieldName);
                return code;
            }

            // Fallback to legacy string format if no metadata manager
            return selectedState;
        }

        /// <summary>
        /// Get the numeric code for the given boolean state using metadata lookup.
        /// Public so that DataRelationshipCoordinator can use the same lookup instead of hard-coding strings.
        /// </summary>
        public object GetCode(bool isYes, string fieldName)
        {
            var code = GetYesNoCode(isYes, fieldName);
            return code ?? (isYes ? (object)"1" : "0");
        }

        /// <summary>
        /// Get the numeric code for Yes or No from metadata lookups.
        /// </summary>
        private object? GetYesNoCode(bool isYes, string fieldName)
        {
            // Try to find the lookup from metadata
            var codes = GetYesNoLookupCodes(fieldName);
            
            if (codes != null && codes.ContainsKey(isYes ? "Yes" : "No"))
            {
                return codes[isYes ? "Yes" : "No"];
            }

            // Fallback: return conventional numeric codes (0=No, 1=Yes)
            return isYes ? "1" : "0";
        }

        /// <summary>
        /// Get the numeric code for Yes, No, or Unknown from metadata lookups.
        /// </summary>
        private object? GetYesNoUnknownCode(string state, string fieldName)
        {
            var codes = GetYesNoUnknownLookupCodes(fieldName);
            
            if (codes != null && codes.ContainsKey(state))
            {
                return codes[state];
            }

            // Fallback: return conventional numeric codes
            return state switch
            {
                "Yes" => "1",
                "No" => "0",
                "Unknown" => "2",
                _ => null
            };
        }

        /// <summary>
        /// Extract Yes/No codes from metadata lookup for a field.
        /// Returns dictionary of { "Yes" -> code, "No" -> code } or null if not found.
        /// </summary>
        private Dictionary<string, object>? GetYesNoLookupCodes(string fieldName)
        {
            if (_metadataManager == null)
                return null;

            // Try common yes_no lookup names
            var lookupNames = new[] { "yes_no", "yes_no_not_applicable", "yes_no_unknown" };
            
            foreach (var lookupName in lookupNames)
            {
                if (_metadataManager.Lookup.TryGetValue(lookupName, out var values))
                {
                    var result = new Dictionary<string, object>();
                    
                    foreach (var node in values)
                    {
                        var display = node.display?.ToLower().Trim() ?? "";
                        
                        if (display == "yes")
                            result["Yes"] = TryParseNumeric(node.value);
                        else if (display == "no")
                            result["No"] = TryParseNumeric(node.value);
                    }
                    
                    if (result.ContainsKey("Yes") && result.ContainsKey("No"))
                        return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Extract Yes/No/Unknown codes from metadata lookup for a field.
        /// Returns dictionary of { "Yes" -> code, "No" -> code, "Unknown" -> code } or null if not found.
        /// </summary>
        private Dictionary<string, object>? GetYesNoUnknownLookupCodes(string fieldName)
        {
            if (_metadataManager == null)
                return null;

            // Try common yes_no_unknown lookup names
            var lookupNames = new[] { "yes_no_unknown", "yes_no_not_applicable", "yes_no" };
            
            foreach (var lookupName in lookupNames)
            {
                if (_metadataManager.Lookup.TryGetValue(lookupName, out var values))
                {
                    var result = new Dictionary<string, object>();
                    
                    foreach (var node in values)
                    {
                        var display = node.display?.ToLower().Trim() ?? "";
                        
                        if (display == "yes")
                            result["Yes"] = TryParseNumeric(node.value);
                        else if (display == "no")
                            result["No"] = TryParseNumeric(node.value);
                        else if (display == "unknown")
                            result["Unknown"] = TryParseNumeric(node.value);
                    }
                    
                    if (result.Count >= 2) // At minimum Yes and No
                        return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Parse numeric value from node value string, handling both string and numeric formats.
        /// </summary>
        private object TryParseNumeric(string value)
        {
            if (double.TryParse(value, out var numValue))
                return numValue;
            
            // Return as string if not numeric
            return value;
        }
    }
}



