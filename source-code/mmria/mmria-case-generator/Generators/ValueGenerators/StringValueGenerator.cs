using System;
using Bogus;
using mmria_case_generator.Models;

namespace mmria_case_generator.Generators.ValueGenerators
{
    public class StringValueGenerator : ValueGeneratorBase
    {
        public StringValueGenerator(Faker faker, GenerationStrategy strategy, Random random)
            : base(faker, strategy, random)
        {
        }

        public string? Generate(string fieldName, bool isRequired)
        {
            if (!ShouldPopulateField(isRequired)) return "";

            var lowerName = fieldName.ToLower();
            
            if (lowerName.Contains("first_name") || lowerName.Contains("firstname"))
                return Faker.Name.FirstName();
            if (lowerName.Contains("last_name") || lowerName.Contains("lastname"))
                return Faker.Name.LastName();
            if (lowerName.Contains("middle_name") || lowerName.Contains("middlename"))
                return Faker.Name.FirstName();
            if (lowerName.Contains("email"))
                return Faker.Internet.Email();
            if (lowerName.Contains("phone"))
                return Faker.Phone.PhoneNumber();
            if (lowerName.Contains("address") || lowerName.Contains("street"))
                return Faker.Address.StreetAddress();
            if (lowerName.Contains("city"))
                return Faker.Address.City();
            if (lowerName.Contains("state"))
                return Faker.Address.StateAbbr();
            if (lowerName.Contains("zip"))
                return Faker.Address.ZipCode();
            if (lowerName.Contains("county"))
                return Faker.Address.County();
            if (lowerName.Contains("ssn"))
                return Faker.Random.Replace("###-##-####");
            if (lowerName.Contains("url"))
                return Faker.Internet.Url();
            if (lowerName.Contains("company") || lowerName.Contains("employer"))
                return Faker.Company.CompanyName();

            return Faker.Lorem.Sentence(Random.Next(3, 10));
        }
    }
}


