using System;
using System.Collections.Generic;

namespace mmria.common.Testing.IJEGeneration.Models
{
    public class IJEGenerationConfig
    {
        public string OutputDirectory { get; set; } = @"c:\temp\test-ije-files";
        public int RecordsPerFile { get; set; } = 5;
        public string StateCode { get; set; } = "TENANT1";
        public List<string> JurisdictionSampling { get; set; } = new() { "MI", "AL", "GA", "FL" };
        public List<int> YearOfDeathSampling { get; set; } = new() { 2019, 2020, 2022, 2023 };
        public int? RandomSeed { get; set; }
        public bool WriteFilesToDisk { get; set; } = false;
        public DateTime? Timestamp { get; set; }
    }
}