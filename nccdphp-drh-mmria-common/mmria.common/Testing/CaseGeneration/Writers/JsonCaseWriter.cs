using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using mmria.common.Testing.CaseGeneration.Models;

namespace mmria.common.Testing.CaseGeneration.Writers
{
    public class JsonCaseWriter
    {
        private readonly GenerationConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonCaseWriter(GenerationConfig config)
        {
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public string WriteCaseToFile(Dictionary<string, object?> caseData, int caseNumber)
        {
            Directory.CreateDirectory(_config.OutputDirectory);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"case_{caseNumber:D3}_{_config.Jurisdiction}_{timestamp}.json";
            var filePath = Path.Combine(_config.OutputDirectory, fileName);

            var json = JsonSerializer.Serialize(caseData, _jsonOptions);
            File.WriteAllText(filePath, json);

            return filePath;
        }

        public List<string> WriteCasesToFiles(List<Dictionary<string, object?>> cases)
        {
            Console.WriteLine($"\nWriting {cases.Count} cases to: {_config.OutputDirectory}");
            Directory.CreateDirectory(_config.OutputDirectory);

            var filePaths = new List<string>();
            for (int i = 0; i < cases.Count; i++)
            {
                var caseData = cases[i];
                var filePath = WriteCaseToFile(caseData, i + 1);
                filePaths.Add(filePath);

                if ((i + 1) % 10 == 0 || (i + 1) == cases.Count)
                {
                    Console.WriteLine($"  Wrote {i + 1}/{cases.Count} files");
                }
            }

            Console.WriteLine($"✓ All cases written successfully");
            return filePaths;
        }

        public string WriteCasesToSingleFile(List<Dictionary<string, object?>> cases, string fileName = "cases.json")
        {
            Directory.CreateDirectory(_config.OutputDirectory);
            var filePath = Path.Combine(_config.OutputDirectory, fileName);

            var json = JsonSerializer.Serialize(cases, _jsonOptions);
            File.WriteAllText(filePath, json);

            Console.WriteLine($"✓ Wrote all cases to: {filePath}");
            return filePath;
        }

        public string GetSummary(List<Dictionary<string, object?>> cases, List<string> filePaths)
        {
            var summary = $@"
========================================
Case Generation Summary
========================================
Jurisdiction: {_config.Jurisdiction}
Metadata Version: {_config.MetadataVersion}
Strategy: {_config.Strategy.Name}
Cases Generated: {cases.Count}
Output Directory: {_config.OutputDirectory}
Files Created: {filePaths.Count}

Strategy Details:
  - Grid Rows: {_config.Strategy.GridRowsMin}-{_config.Strategy.GridRowsMax}
  - Multiform Instances: {_config.Strategy.MultiformInstancesMin}-{_config.Strategy.MultiformInstancesMax}
  - Completeness: {_config.Strategy.CompletenessPercentage:P0}
  - Required Only: {_config.Strategy.RequiredFieldsOnly}
  - Edge Cases: {_config.Strategy.GenerateEdgeCases}

Sample Files:
{string.Join("\n", filePaths.Take(5).Select(f => $"  - {Path.GetFileName(f)}"))}
{(filePaths.Count > 5 ? $"  ... and {filePaths.Count - 5} more" : "")}
========================================
";

            return summary;
        }
    }
}



