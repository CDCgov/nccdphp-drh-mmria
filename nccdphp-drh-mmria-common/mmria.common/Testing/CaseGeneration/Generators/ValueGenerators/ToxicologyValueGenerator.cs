using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using mmria.common.Testing.CaseGeneration.Models;
using mmria.common.metadata;

namespace mmria.common.Testing.CaseGeneration.Generators.ValueGenerators
{
    /// <summary>
    /// Generates realistic toxicology results for autopsy reports.
    /// Sources real substance names from metadata lookup.
    /// Strategy-aware to control number and type of substances.
    /// </summary>
    public class ToxicologyValueGenerator : ValueGeneratorBase
    {
        private readonly ToxicologyClassifier _classifier;
        private readonly MetadataManager _metadataManager;
        private List<string> _cachedSubstances;

        public ToxicologyValueGenerator(Faker faker, GenerationStrategy strategy, Random random, ToxicologyClassifier classifier, MetadataManager metadataManager)
            : base(faker, strategy, random)
        {
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _metadataManager = metadataManager ?? throw new ArgumentNullException(nameof(metadataManager));
            _cachedSubstances = LoadSubstancesFromMetadata();
        }

        /// <summary>
        /// Load all available substances from metadata lookup.
        /// </summary>
        private List<string> LoadSubstancesFromMetadata()
        {
            var substances = new List<string>();

            if (_metadataManager.Lookup.TryGetValue("substance", out var substanceNodes))
            {
                foreach (var node in substanceNodes)
                {
                    if (!string.IsNullOrWhiteSpace(node.display))
                    {
                        substances.Add(node.display);
                    }
                }
            }

            return substances;
        }

        /// <summary>
        /// Generate a list of toxicology results (substances found in autopsy).
        /// Pulls real substance names from metadata lookup.
        /// Distribution depends on strategy:
        /// - Complete/Edge: 1-3 substances
        /// - Sparse: 0-1 substance
        /// - Minimal: No toxicology
        /// </summary>
        public List<Dictionary<string, object?>> GenerateToxicologyResults()
        {
            var results = new List<Dictionary<string, object?>>();

            if (!_cachedSubstances.Any())
                return results; // No substances available

            int resultCount = Strategy.CompletenessPercentage switch
            {
                >= 80 => Random.Next(1, 4),      // Complete: 1-3 substances
                >= 50 => Random.Next(0, 2),      // Sparse: 0-1 substance
                _ => 0                             // Minimal: no toxicology
            };

            // For edge cases, add more substances
            if (Strategy.GenerateEdgeCases && Random.NextDouble() < 0.3)
                resultCount = Random.Next(3, 5);

            var usedSubstances = new HashSet<string>();

            for (int i = 0; i < resultCount; i++)
            {
                // Pick random substance from metadata
                string substance = Faker.PickRandom(_cachedSubstances);

                // Avoid duplicates
                if (usedSubstances.Contains(substance))
                    continue;

                usedSubstances.Add(substance);

                var drugClass = _classifier.Classify(substance);
                var (concentration, unit) = GenerateRealisticConcentration(substance, drugClass);

                var result = new Dictionary<string, object?>
                {
                    ["substance"] = substance,
                    ["concentration"] = concentration,
                    ["unit_of_measure"] = unit,
                    ["level"] = Random.Next(0, 5).ToString(),  // 0-4 detection level
                    ["result"] = "Positive",
                    ["drug_class"] = drugClass
                };

                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Generate realistic concentration value and unit based on drug class.
        /// </summary>
        private (string concentration, string unit) GenerateRealisticConcentration(string substance, string drugClass)
        {
            // Different drug classes have different typical ranges and units
            var (minConc, maxConc, units) = drugClass.ToLowerInvariant() switch
            {
                "opioid" => (0.5, 500.0, new[] { "ng/mL", "ng/g" }),
                "benzodiazepine" => (0.01, 50.0, new[] { "ng/mL", "μg/mL" }),
                "cocaine" => (0.1, 200.0, new[] { "ng/mL" }),
                "amphetamine" => (0.1, 1000.0, new[] { "ng/mL" }),
                "alcohol" => (5.0, 400.0, new[] { "mg/100mL", "%" }),
                "cannabinoid" => (1.0, 100.0, new[] { "ng/mL" }),
                "buprenorphine_methadone" => (0.1, 200.0, new[] { "ng/mL" }),
                _ => (0.1, 100.0, new[] { "ng/mL" })  // Default for "other"
            };

            double concentration = minConc + (Random.NextDouble() * (maxConc - minConc));
            string unit = Faker.PickRandom(units);
            
            // Format based on unit and value
            string formatted = unit switch
            {
                "mg/100mL" or "%" => $"{concentration:F1}",
                "μg/mL" => $"{concentration:F3}",
                _ => $"{concentration:F2}"
            };

            return (formatted, unit);
        }

        private string FormatLevel(double level) => $"{level:F2} ng/mL";
    }
}

