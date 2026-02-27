using Bogus;
using mmria.common.Testing.CaseGeneration.Models;

namespace mmria.common.Testing.CaseGeneration.Generators.ValueGenerators
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



