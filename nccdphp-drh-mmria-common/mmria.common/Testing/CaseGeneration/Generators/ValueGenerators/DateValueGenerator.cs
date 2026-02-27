using System;
using Bogus;
using mmria.common.Testing.CaseGeneration.Models;

namespace mmria.common.Testing.CaseGeneration.Generators.ValueGenerators
{
    public class DateValueGenerator : ValueGeneratorBase
    {
        public DateValueGenerator(Faker faker, GenerationStrategy strategy, Random random)
            : base(faker, strategy, random)
        {
        }

        public DateTime? GenerateDate(string fieldName, bool isRequired)
        {
            if (!ShouldPopulateField(isRequired)) return DateTime.MinValue;

            var lowerName = fieldName.ToLower();
            var now = DateTime.Now;

            // Context-aware date generation
            if (lowerName.Contains("birth_date") || lowerName.Contains("date_of_birth"))
            {
                if (lowerName.Contains("maternal") || lowerName.Contains("mother"))
                    return Faker.Date.Past(45, now.AddYears(-18)); // 18-45 years ago
                return Faker.Date.Past(2); // 0-2 years ago for child
            }

            if (lowerName.Contains("death_date") || lowerName.Contains("date_of_death"))
                return Faker.Date.Past(2);

            if (lowerName.Contains("lmp") || lowerName.Contains("last_menstrual"))
                return Faker.Date.Past(1, now.AddMonths(-9)); // ~9 months ago

            if (lowerName.Contains("prenatal") || lowerName.Contains("visit"))
                return Faker.Date.Past(1, now.AddMonths(-3)); // during pregnancy

            if (lowerName.Contains("admission") || lowerName.Contains("discharge"))
                return Faker.Date.Recent(90);

            // Edge cases
            if (Strategy.GenerateEdgeCases && Random.Next(2) == 0)
            {
                return Random.Next(3) switch
                {
                    0 => new DateTime(1900, 1, 1),
                    1 => new DateTime(2000, 1, 1),
                    _ => now
                };
            }

            return Faker.Date.Past(100);
        }

        public DateTime? GenerateDateTime(string fieldName, bool isRequired)
        {
            var date = GenerateDate(fieldName, isRequired);
            if (!date.HasValue) return null;

            var hour = Random.Next(0, 24);
            var minute = Random.Next(0, 60);
            return date.Value.AddHours(hour).AddMinutes(minute);
        }

        public string? GenerateDateString(string fieldName, bool isRequired)
        {
            var date = GenerateDate(fieldName, isRequired);
            return date == DateTime.MinValue ? "" : date?.ToString("yyyy-MM-dd");
        }

        public string? GenerateDateTimeString(string fieldName, bool isRequired)
        {
            var dateTime = GenerateDateTime(fieldName, isRequired);
            return dateTime == DateTime.MinValue ? "" : dateTime?.ToString("yyyy-MM-ddTHH:mm:ss");
        }

        public string? GenerateTimeString(string fieldName, bool isRequired)
        {
            if (!ShouldPopulateField(isRequired)) return "";
            
            var hour = Random.Next(0, 24);
            var minute = Random.Next(0, 60);
            return $"{hour:D2}:{minute:D2}";
        }
    }
}



