using Bogus;
using mmria_case_generator.Models;

namespace mmria_case_generator.Generators.ValueGenerators
{
    public abstract class ValueGeneratorBase
    {
        protected Faker Faker { get; }
        protected GenerationStrategy Strategy { get; }
        protected Random Random { get; }

        protected ValueGeneratorBase(Faker faker, GenerationStrategy strategy, Random random)
        {
            Faker = faker;
            Strategy = strategy;
            Random = random;
        }

        protected bool ShouldPopulateField(bool isRequired)
        {
            if (isRequired) return true;
            if (Strategy.RequiredFieldsOnly) return false;
            return Random.NextDouble() < Strategy.CompletenessPercentage;
        }
    }
}


