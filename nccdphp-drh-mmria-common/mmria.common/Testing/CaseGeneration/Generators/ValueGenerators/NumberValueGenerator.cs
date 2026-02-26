using System;
using Bogus;
using mmria.common.Testing.CaseGeneration.Models;

namespace mmria.common.Testing.CaseGeneration.Generators.ValueGenerators
{
    public class NumberValueGenerator : ValueGeneratorBase
    {
        public NumberValueGenerator(Faker faker, GenerationStrategy strategy, Random random)
            : base(faker, strategy, random)
        {
        }

        public string? Generate(string fieldName, bool isRequired, double? min = null, double? max = null)
        {
            if (!ShouldPopulateField(isRequired)) return "";

            var lowerName = fieldName.ToLower();

            // Age context-aware
            if (lowerName.Contains("maternal_age") || lowerName.Contains("mother_age"))
            {
                var age = Strategy.GenerateEdgeCases && Random.Next(2) == 0
                    ? Random.Next(12, 55)
                    : Random.Next(18, 45);
                return age.ToString();
            }
            if (lowerName.Contains("age"))
            {
                var age = Strategy.GenerateEdgeCases && Random.Next(2) == 0
                    ? Random.Next(0, 100)
                    : Random.Next(0, 90);
                return age.ToString();
            }

            // Weight context-aware
            if (lowerName.Contains("birth_weight") || lowerName.Contains("birthweight"))
                return Random.Next(2500, 4000).ToString(); // grams
            if (lowerName.Contains("weight"))
                return Random.Next(100, 250).ToString(); // lbs

            // Height/length context-aware
            if (lowerName.Contains("birth_length") || lowerName.Contains("birthlength"))
                return Random.Next(45, 55).ToString(); // cm
            if (lowerName.Contains("height"))
                return Random.Next(60, 72).ToString(); // inches

            // Temperature
            if (lowerName.Contains("temperature"))
            {
                var temp = Strategy.GenerateEdgeCases && Random.Next(2) == 0
                    ? Random.Next(95, 104)
                    : Random.Next(97, 100);
                return temp.ToString();
            }

            // Blood pressure
            if (lowerName.Contains("blood_pressure") || lowerName.Contains("systolic") || lowerName.Contains("diastolic"))
                return Random.Next(80, 180).ToString();

            // Use provided min/max or defaults
            var minValue = min ?? 0;
            var maxValue = max ?? 100;
            var value = Random.NextDouble() * (maxValue - minValue) + minValue;
            return value.ToString("F2");
        }

        public string? GenerateInt(string fieldName = "", bool isRequired = false, int? min = null, int? max = null)
        {
            var stringValue = Generate(fieldName, isRequired, min, max);
            if (string.IsNullOrEmpty(stringValue)) return "";
            if (double.TryParse(stringValue, out var doubleValue))
            {
                return ((int)Math.Round(doubleValue)).ToString();
            }
            return stringValue;
        }
    }
}



