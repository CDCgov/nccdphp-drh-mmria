using System;

namespace mmria.cmd.line.tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("MMRIA Command Line Test Utility");
            Console.WriteLine("================================");
            Console.WriteLine();

            var generator = new TestIJEFileGenerator();
            
            // Determine output directory
            string outputDir;
            if (args.Length > 0)
            {
                outputDir = args[0];
            }
            else
            {
                outputDir = @"c:\temp\test-ije-files";
            }

            // Determine number of records
            int recordCount = 1;
            if (args.Length > 1 && int.TryParse(args[1], out int parsedCount))
            {
                recordCount = parsedCount;
            }

            // Determine state code
            string stateCode = "LOCALHOST";
            if (args.Length > 2 && !string.IsNullOrWhiteSpace(args[2]))
            {
                stateCode = args[2].Length >= 2 ? args[2].Substring(0, 2) : args[2];
            }

            Console.WriteLine($"Output Directory: {outputDir}");
            Console.WriteLine($"Records per file: {recordCount}");
            Console.WriteLine($"State Code: {stateCode}");
            Console.WriteLine();
            
            // Generate all test files
            generator.GenerateAllTestFiles(outputDir, recordCount, stateCode);
            
            Console.WriteLine();
            Console.WriteLine("Generation complete!");
            Console.WriteLine();
            Console.WriteLine("Usage: mmria-cmd-line-tests [output-directory] [record-count] [state-code]");
            Console.WriteLine("  output-directory: Path where files will be generated (default: c:\\temp\\test-ije-files)");
            Console.WriteLine("  record-count: Number of records per file (default: 10)");
            Console.WriteLine("  state-code: Two-letter state code (default: MI)");
            Console.WriteLine();
            Console.WriteLine("Example: mmria-cmd-line-tests c:\\temp\\files 5 AL");
            Console.WriteLine("  Generates: 2025_2025_11_26_AL.MOR, 2025_2025_11_26_AL.NAT, 2025_2025_11_26_AL.FET");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
