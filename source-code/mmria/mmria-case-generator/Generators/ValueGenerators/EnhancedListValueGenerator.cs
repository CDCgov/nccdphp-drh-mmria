using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using mmria_case_generator.Models;

namespace mmria_case_generator.Generators.ValueGenerators
{
    /// <summary>
    /// Enhanced list value generator with weighted selection and common patterns
    /// Uses configurable DemographicWeights for realistic distributions
    /// </summary>
    public class EnhancedListValueGenerator : ValueGeneratorBase
    {
        private readonly DemographicWeights _weights;

        public EnhancedListValueGenerator(Faker faker, GenerationStrategy strategy, Random random, DemographicWeights weights)
            : base(faker, strategy, random)
        {
            _weights = weights ?? throw new ArgumentNullException(nameof(weights));
        }

        /// <summary>
        /// Generate list value with weighted selection if applicable
        /// </summary>
        public object? GenerateWithWeights(
            string fieldName,
            Dictionary<string, string> valueToDisplay,
            bool isRequired,
            bool isMultiSelect)
        {
            if (!ShouldPopulateField(isRequired)) return isMultiSelect ? (object)new List<string>() : "9999";
            if (valueToDisplay.Count == 0) return isMultiSelect ? (object)new List<string>() : "9999";

            var lowerFieldName = fieldName.ToLower();
            
            // Check if we have weights for this field type
            var weights = FindApplicableWeights(lowerFieldName);
            
            if (isMultiSelect)
            {
                return GenerateMultiSelect(valueToDisplay, weights);
            }
            else
            {
                return GenerateSingleSelect(valueToDisplay, weights);
            }
        }

        /// <summary>
        /// Find applicable weights based on field name patterns
        /// </summary>
        private Dictionary<string, double>? FindApplicableWeights(string fieldName)
        {
            var lowerField = fieldName.ToLowerInvariant();

            // Match field to demographic category
            if (lowerField.Contains("race") || lowerField.Contains("ethnicity"))
                return _weights.RaceEthnicity;
            else if (lowerField.Contains("education"))
                return _weights.Education;
            else if (lowerField.Contains("insurance"))
                return _weights.Insurance;
            else if (lowerField.Contains("age"))
                return _weights.AgeRange;
            else if (lowerField.Contains("marital"))
                return _weights.MaritalStatus;
            else if (lowerField.Contains("employment"))
                return _weights.EmploymentStatus;
            else if (lowerField.Contains("housing"))
                return _weights.HousingStatus;

            return null;
        }

        /// <summary>
        /// Generate single selection with weighted probability
        /// </summary>
        private string? GenerateSingleSelect(
            Dictionary<string, string> valueToDisplay,
            Dictionary<string, double>? weights)
        {
            var values = valueToDisplay.Keys.ToList();
            if (values.Count == 0) return null;

            // If we have weights, use weighted selection
            if (weights != null)
            {
                return SelectWeighted(values, weights);
            }

            // Otherwise, uniform random selection
            return values[Random.Next(values.Count)];
        }

        /// <summary>
        /// Generate multi-select with realistic count (1-4 selections)
        /// </summary>
        private List<string>? GenerateMultiSelect(
            Dictionary<string, string> valueToDisplay,
            Dictionary<string, double>? weights)
        {
            var values = valueToDisplay.Keys.ToList();
            if (values.Count == 0) return null;

            // Realistic multi-select: 1-4 items, but usually 1-2
            var count = Random.Next(2) == 0 ? 1 : Random.Next(1, Math.Min(4, values.Count) + 1);

            if (weights != null)
            {
                // Weighted selection without replacement
                var selected = new List<string>();
                var remaining = new List<string>(values);
                
                for (int i = 0; i < count && remaining.Count > 0; i++)
                {
                    var item = SelectWeighted(remaining, weights);
                    if (item != null)
                    {
                        selected.Add(item);
                        remaining.Remove(item);
                    }
                }
                return selected;
            }
            else
            {
                // Uniform random selection
                return values.OrderBy(x => Random.Next()).Take(count).ToList();
            }
        }

        /// <summary>
        /// Select value using weighted probability
        /// </summary>
        private string? SelectWeighted(List<string> values, Dictionary<string, double> weights)
        {
            // Calculate total weight for values that exist in our list
            var applicableWeights = values
                .Select(v => new { Value = v, Weight = GetWeight(v, weights) })
                .ToList();

            var totalWeight = applicableWeights.Sum(x => x.Weight);
            if (totalWeight == 0)
            {
                // Fallback to uniform if no weights match
                return values[Random.Next(values.Count)];
            }

            // Generate random number and select based on cumulative weights
            var randomValue = Random.NextDouble() * totalWeight;
            var cumulative = 0.0;

            foreach (var item in applicableWeights)
            {
                cumulative += item.Weight;
                if (randomValue <= cumulative)
                {
                    return item.Value;
                }
            }

            // Fallback
            return values[Random.Next(values.Count)];
        }

        /// <summary>
        /// Get weight for a value, checking various patterns
        /// </summary>
        private double GetWeight(string value, Dictionary<string, double> weights)
        {
            var lowerValue = value.ToLower().Replace(" ", "_").Replace("-", "_");
            
            // Exact match
            if (weights.ContainsKey(lowerValue))
            {
                return weights[lowerValue];
            }

            // Partial match (e.g., value contains key)
            foreach (var kvp in weights)
            {
                if (lowerValue.Contains(kvp.Key) || kvp.Key.Contains(lowerValue))
                {
                    return kvp.Value;
                }
            }

            // Default weight for unmatched values
            return 1.0;
        }
    }
}
