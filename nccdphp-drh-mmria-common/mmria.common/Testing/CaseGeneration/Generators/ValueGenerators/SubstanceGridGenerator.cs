using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using mmria.common.Testing.CaseGeneration.Models;

namespace mmria.common.Testing.CaseGeneration.Generators.ValueGenerators
{
    /// <summary>
    /// Generates substance grid rows for prenatal and social environmental forms.
    /// Populates grids with real substances from metadata and realistic values.
    /// </summary>
    public class SubstanceGridGenerator : ValueGeneratorBase
    {
        private readonly MetadataManager _metadataManager;
        private List<string> _cachedSubstances;

        public SubstanceGridGenerator(Faker faker, GenerationStrategy strategy, Random random, MetadataManager metadataManager)
            : base(faker, strategy, random)
        {
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
        /// Generate substance grid for prenatal records.
        /// Fields: substance, substance_other, screening, couseling_education, comments
        /// </summary>
        public List<Dictionary<string, object?>> GeneratePrenatalSubstanceGrid()
        {
            var results = new List<Dictionary<string, object?>>();

            if (!_cachedSubstances.Any())
                return results;

            int rowCount = Strategy.CompletenessPercentage switch
            {
                >= 80 => Random.Next(1, 4),      // Complete: 1-3 substances
                >= 50 => Random.Next(0, 2),      // Sparse: 0-1 substance
                _ => 0                             // Minimal: no substances
            };

            var usedSubstances = new HashSet<string>();

            for (int i = 0; i < rowCount; i++)
            {
                string substance = Faker.PickRandom(_cachedSubstances);

                // Avoid duplicates
                if (usedSubstances.Contains(substance))
                    continue;

                usedSubstances.Add(substance);

                var row = new Dictionary<string, object?>
                {
                    ["substance"] = substance,
                    ["substance_other"] = Random.NextDouble() < 0.3 ? Faker.Lorem.Sentence() : "",
                    ["screening"] = Random.Next(2).ToString(),                    // 0 or 1
                    ["couseling_education"] = Random.Next(2).ToString(),          // 0 or 1
                    ["comments"] = Random.NextDouble() < 0.4 ? Faker.Lorem.Sentence() : ""
                };

                results.Add(row);
            }

            return results;
        }

        /// <summary>
        /// Generate substance grid for social/environmental profile.
        /// Fields: substance, substance_other, timing_of_substance_use
        /// </summary>
        public List<Dictionary<string, object?>> GenerateSocialSubstanceGrid()
        {
            var results = new List<Dictionary<string, object?>>();

            if (!_cachedSubstances.Any())
                return results;

            int rowCount = Strategy.CompletenessPercentage switch
            {
                >= 80 => Random.Next(1, 4),      // Complete: 1-3 substances
                >= 50 => Random.Next(0, 2),      // Sparse: 0-1 substance
                _ => 0                             // Minimal: no substances
            };

            var usedSubstances = new HashSet<string>();

            for (int i = 0; i < rowCount; i++)
            {
                string substance = Faker.PickRandom(_cachedSubstances);

                // Avoid duplicates
                if (usedSubstances.Contains(substance))
                    continue;

                usedSubstances.Add(substance);

                var row = new Dictionary<string, object?>
                {
                    ["substance"] = substance,
                    ["substance_other"] = Random.NextDouble() < 0.3 ? Faker.Lorem.Sentence() : "",
                    ["timing_of_substance_use"] = Random.Next(4).ToString()       // 0, 1, 2, or 3
                };

                results.Add(row);
            }

            return results;
        }
    }
}

