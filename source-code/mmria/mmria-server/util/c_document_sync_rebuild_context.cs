#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace mmria.server.utils;

public sealed class c_document_sync_rebuild_context
{
    public c_document_sync_rebuild_context() { }

    public mmria.common.metadata.app metadata { get; init; }

    public HashSet<string> de_identified_set { get; init; } = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    public string case_template_json { get; init; }
}

public sealed class c_document_sync_build_result
{
    public string de_identified_json { get; set; }

    public List<string> report_document_json_list { get; set; } = new();
}

public static class c_case_template_resolver
{
    public static string GetDatabaseScriptsDirectory()
    {
        string current_directory = AppContext.BaseDirectory;
        if(!Directory.Exists(Path.Combine(current_directory, "database-scripts")))
        {
            current_directory = Directory.GetCurrentDirectory();
        }

        return Path.Combine(current_directory, "database-scripts");
    }

    public static async Task<string> ReadBestAvailableCaseTemplateAsync(string metadata_version, Action<string> log = null)
    {
        string scripts_directory = GetDatabaseScriptsDirectory();
        string exact_file_name = $"case-version-{metadata_version}.json";
        string exact_path = Path.Combine(scripts_directory, exact_file_name);

        if(File.Exists(exact_path))
        {
            return await File.ReadAllTextAsync(exact_path);
        }

        string fallback_path = Directory
            .GetFiles(scripts_directory, "case-version-*.json")
            .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if(!string.IsNullOrWhiteSpace(fallback_path))
        {
            log?.Invoke($"Case template '{exact_file_name}' was not found. Falling back to '{Path.GetFileName(fallback_path)}'.");
            return await File.ReadAllTextAsync(fallback_path);
        }

        log?.Invoke($"Case template '{exact_file_name}' was not found and no fallback case-version template is available.");
        return null;
    }
}
#endif
