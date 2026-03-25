#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace mmria.common.SharedLibraries.MMRIARebuild.Manager;

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
    private static readonly string[] EmbeddedDatabaseScriptFolderMarkers =
    {
        ".SharedLibraries.MMRIARebuild.database-scripts.",
        ".SharedLibraries.MMRIARebuild.database_scripts."
    };

    private static string find_embedded_database_script_name(string script_file_name)
    {
        string safe_file_name = Path.GetFileName(script_file_name);
        var assembly = typeof(c_case_template_resolver).Assembly;

        return assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name =>
                EmbeddedDatabaseScriptFolderMarkers.Any(marker =>
                    name.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0) &&
                name.EndsWith(safe_file_name, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetDatabaseScriptsDirectory()
    {
        string current_directory = AppContext.BaseDirectory;
        if(!Directory.Exists(Path.Combine(current_directory, "database-scripts")))
        {
            current_directory = Directory.GetCurrentDirectory();
        }

        return Path.Combine(current_directory, "database-scripts");
    }

    public static async Task<string> ReadDatabaseScriptAsync(string script_file_name, Action<string> log = null)
    {
        string safe_file_name = Path.GetFileName(script_file_name);
        string embedded_resource_name = find_embedded_database_script_name(safe_file_name);
        if(!string.IsNullOrWhiteSpace(embedded_resource_name))
        {
            var assembly = typeof(c_case_template_resolver).Assembly;
            using Stream resource_stream = assembly.GetManifestResourceStream(embedded_resource_name);
            if(resource_stream != null)
            {
                using var sr = new StreamReader(resource_stream);
                return await sr.ReadToEndAsync();
            }
        }

        string scripts_directory = GetDatabaseScriptsDirectory();
        string local_script_path = Path.Combine(scripts_directory, safe_file_name);
        if(File.Exists(local_script_path))
        {
            return await File.ReadAllTextAsync(local_script_path);
        }

        log?.Invoke($"Database script '{safe_file_name}' was not found in embedded rebuild assets or local database-scripts.");
        throw new FileNotFoundException($"Unable to find database script '{safe_file_name}'.", local_script_path);
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


