using System;
using Bogus;
using mmria_case_generator.Models;

namespace mmria_case_generator.Generators.ValueGenerators
{
    public class BooleanValueGenerator : ValueGeneratorBase
    {
        public BooleanValueGenerator(Faker faker, GenerationStrategy strategy, Random random)
            : base(faker, strategy, random)
        {
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

        public string? GenerateYesNo(string fieldName, bool isRequired)
        {
            var value = Generate(fieldName, isRequired);
            return value == true ? "Yes" : "No";
        }

        /// <summary>
        /// Generate a 4-state boolean value: "Yes", "No", "Unknown", or null (for blank).
        /// Used for contributing factors and other questions that allow uncertainty.
        /// </summary>
        public string? GenerateFourState(string fieldName, bool isRequired)
        {
            if (!ShouldPopulateField(isRequired)) return null; // Blank

            double roll = Random.NextDouble();

            if (Strategy.RequiredFieldsOnly)
            {
                // For required-only mode, strongly prefer Yes/No
                return roll < 0.5 ? "Yes" : "No";
            }

            // For edge cases, more extreme variation
            if (Strategy.GenerateEdgeCases)
            {
                if (roll < 0.3) return "Yes";
                if (roll < 0.6) return "No";
                if (roll < 0.9) return "Unknown";
                return null; // Blank
            }

            // Default distribution: 40% Yes, 30% No, 20% Unknown, 10% Blank
            if (roll < 0.4) return "Yes";
            if (roll < 0.7) return "No";
            if (roll < 0.9) return "Unknown";
            return null; // Blank
        }
    }
}


