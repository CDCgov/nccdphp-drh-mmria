using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.common.Testing.IJEGeneration.Models
{
    public class GeneratedIJEFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public List<string> Records { get; set; } = new();
        public string? OutputPath { get; set; }

        public int RecordCount => Records.Count;

        public string Content => string.Join(Environment.NewLine, Records ?? Enumerable.Empty<string>());
    }
}