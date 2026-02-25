namespace mmria_case_generator.Models
{
    /// <summary>
    /// Strategy for controlling how case data is generated
    /// </summary>
    public class GenerationStrategy
    {
        public string Name { get; set; } = "complete";
        public int GridRowsMin { get; set; } = 0;
        public int GridRowsMax { get; set; } = 3;
        public int MultiformInstancesMin { get; set; } = 1;
        public int MultiformInstancesMax { get; set; } = 2;
        public double CompletenessPercentage { get; set; } = 0.85;
        public bool RequiredFieldsOnly { get; set; } = false;
        public bool GenerateEdgeCases { get; set; } = false;

        public static GenerationStrategy FromName(string name)
        {
            return name.ToLower() switch
            {
                "complete" => new GenerationStrategy
                {
                    Name = "complete",
                    GridRowsMin = 0,
                    GridRowsMax = 3,
                    MultiformInstancesMin = 1,
                    MultiformInstancesMax = 2,
                    CompletenessPercentage = 0.85,
                    RequiredFieldsOnly = false,
                    GenerateEdgeCases = false
                },
                "minimal" => new GenerationStrategy
                {
                    Name = "minimal",
                    GridRowsMin = 0,
                    GridRowsMax = 0,
                    MultiformInstancesMin = 1,
                    MultiformInstancesMax = 1,
                    CompletenessPercentage = 0.0,
                    RequiredFieldsOnly = true,
                    GenerateEdgeCases = false
                },
                "edge" => new GenerationStrategy
                {
                    Name = "edge",
                    GridRowsMin = 3,
                    GridRowsMax = 5,
                    MultiformInstancesMin = 2,
                    MultiformInstancesMax = 3,
                    CompletenessPercentage = 1.0,
                    RequiredFieldsOnly = false,
                    GenerateEdgeCases = true
                },
                "sparse" => new GenerationStrategy
                {
                    Name = "sparse",
                    GridRowsMin = 0,
                    GridRowsMax = 2,
                    MultiformInstancesMin = 1,
                    MultiformInstancesMax = 1,
                    CompletenessPercentage = 0.4,
                    RequiredFieldsOnly = false,
                    GenerateEdgeCases = false
                },
                _ => FromName("complete")
            };
        }
    }
}
