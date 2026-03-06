using System.Collections.Generic;

namespace mmria.common.Testing.IJEGeneration.Models
{
    public class IJEGenerationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OutputDirectory { get; set; }
        public List<GeneratedIJEFile> GeneratedFiles { get; set; } = new();
    }
}