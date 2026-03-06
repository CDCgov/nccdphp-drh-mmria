using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.Testing.IJEGeneration.Generators;
using mmria.common.Testing.IJEGeneration.Models;

namespace mmria.common.Testing.IJEGeneration.Services
{
    /// <summary>
    /// Public API for generating synthetic IJE files for tests and local data setup.
    /// Mirrors the case generator service pattern by returning generated artifacts and
    /// optionally persisting them to disk.
    /// </summary>
    public class IJEGeneratorService
    {
        public async Task<IJEGenerationResult> GenerateFilesAsync(IJEGenerationConfig config)
        {
            var result = new IJEGenerationResult();

            try
            {
                var generator = new TestIJEFileGenerator(config.RandomSeed);
                var files = generator.GenerateAllFiles(
                    config.RecordsPerFile,
                    config.StateCode,
                    config.JurisdictionSampling,
                    config.YearOfDeathSampling,
                    config.Timestamp);

                result.GeneratedFiles = files.ToList();

                if (config.WriteFilesToDisk)
                {
                    if (string.IsNullOrWhiteSpace(config.OutputDirectory))
                    {
                        throw new InvalidOperationException("OutputDirectory is required when WriteFilesToDisk is true.");
                    }

                    Directory.CreateDirectory(config.OutputDirectory);

                    foreach (var file in result.GeneratedFiles)
                    {
                        var outputPath = Path.Combine(config.OutputDirectory, file.FileName);
                        await File.WriteAllLinesAsync(outputPath, file.Records);
                        file.OutputPath = outputPath;
                    }

                    result.OutputDirectory = config.OutputDirectory;
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }
}