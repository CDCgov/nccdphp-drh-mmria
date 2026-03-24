#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace mmria.server.utils;

public sealed class c_document_sync_all_legacy
{
    public class legacy_progress
    {
        public int batch_number { get; set; }
        public string last_processed_id { get; set; }
        public int completed_batch_count { get; set; }
        public int processed_case_count { get; set; }
        public int skipped_case_count { get; set; }
        public int document_error_count { get; set; }
        public int de_id_bulk_error_count { get; set; }
        public int report_bulk_error_count { get; set; }
        public int total_de_id_doc_count { get; set; }
        public int total_report_doc_count { get; set; }
        public long fetch_elapsed_ms { get; set; }
        public long build_elapsed_ms { get; set; }
        public long write_elapsed_ms { get; set; }
    }

    public sealed class legacy_result : legacy_progress
    {
        public bool rebuild_completed_successfully { get; set; }
        public string last_error { get; set; }
    }

    public sealed class Report_Opioid_Index_Attribute_Partial_Filter_Selector
    {
        public Report_Opioid_Index_Attribute_Partial_Filter_Selector(){}
        public Dictionary<string, string> _id { get; set; } = new() { { "$regex", "^opioid" } };
    }

    public sealed class Report_PowerBI_Index_Attribute_Partial_Filter_Selector
    {
        public Report_PowerBI_Index_Attribute_Partial_Filter_Selector(){}
        public Dictionary<string, string> _id { get; set; } = new() { { "$regex", "^powerbi" } };
    }

    public sealed class Report_Opioid_Index_Attribute_Struct
    {
        public Report_Opioid_Index_Attribute_Partial_Filter_Selector partial_filter_selector { get; set; } = new();
        public List<string> fields { get; set; } = new() { "_id" };
    }

    public sealed class Report_PowerBI_Index_Attribute_Struct
    {
        public Report_PowerBI_Index_Attribute_Partial_Filter_Selector partial_filter_selector { get; set; } = new();
        public List<string> fields { get; set; } = new() { "_id" };
    }

    public sealed class Report_Opioid_Index_Struct
    {
        public Report_Opioid_Index_Attribute_Struct index { get; set; } = new();
        public string ddoc { get; set; } = "opioid-report-index";
        public string type { get; set; } = "json";
    }

    public sealed class Report_PowerBI_Index_Struct
    {
        public Report_PowerBI_Index_Attribute_Struct index { get; set; } = new();
        public string ddoc { get; set; } = "powerbi-report-index";
        public string type { get; set; } = "json";
    }

    private readonly string couchdb_url;
    private readonly string user_name;
    private readonly string user_value;
    private readonly string metadata_version;
    private readonly mmria.common.couchdb.DBConfigurationDetail db_config;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly string _host_prefix;
    private readonly int _page_size;
    private readonly int _batch_delay_ms;
    private readonly int _write_retry_count;
    private readonly int _write_retry_delay_ms;
    private readonly Func<legacy_progress, Task> _progress_callback;

    public c_document_sync_all_legacy
    (
        string p_couchdb_url,
        string p_user_name,
        string p_value,
        string p_metadata_version,
        mmria.common.couchdb.DBConfigurationDetail p_db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.couchdb.OverridableConfiguration configuration = null,
        string host_prefix = null,
        int page_size = 100,
        int batch_delay_ms = 0,
        int write_retry_count = 0,
        int write_retry_delay_ms = 0,
        Func<legacy_progress, Task> progress_callback = null
    )
    {
        couchdb_url = p_couchdb_url;
        user_name = p_user_name;
        user_value = p_value;
        metadata_version = p_metadata_version;
        db_config = p_db_config;
        _couchDbHttpClient = couchDbHttpClient;
        _configuration = configuration;
        _host_prefix = host_prefix;
        _page_size = Math.Max(1, page_size);
        _batch_delay_ms = Math.Max(0, batch_delay_ms);
        _write_retry_count = Math.Max(0, write_retry_count);
        _write_retry_delay_ms = Math.Max(0, write_retry_delay_ms);
        _progress_callback = progress_callback;
    }

    private string get_database_scripts_directory()
    {
        return c_case_template_resolver.GetDatabaseScriptsDirectory();
    }

    private async Task<string> read_database_script_async(string file_name)
    {
        using var sr = new System.IO.StreamReader(System.IO.Path.Combine(get_database_scripts_directory(), file_name));
        return await sr.ReadToEndAsync();
    }

    private static bool is_transient_write_exception(Exception ex)
    {
        if(ex == null)
        {
            return false;
        }

        if(ex is HttpRequestException || ex is TaskCanceledException || ex is TimeoutException)
        {
            return true;
        }

        return is_transient_write_exception(ex.InnerException);
    }

    private async Task<List<string>> get_case_id_batch_async(int skip, int take)
    {
        string url = couchdb_url + $"/{db_config.prefix}mmrds/_all_docs?skip={skip}&limit={take}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, user_name, user_value);
        var result = new List<string>();

        if(string.IsNullOrWhiteSpace(response))
        {
            return result;
        }

        var payload = JObject.Parse(response);
        var rows = payload["rows"] as JArray;
        if(rows == null)
        {
            return result;
        }

        foreach(var row in rows.OfType<JObject>())
        {
            string id = row.Value<string>("id");
            if(string.IsNullOrWhiteSpace(id) || id.IndexOf("_design/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            result.Add(id);
        }

        return result;
    }

    private async Task<string> get_case_document_async(string document_id)
    {
        return await _couchDbHttpClient.ExecuteAsync(
            "GET",
            couchdb_url + $"/{db_config.prefix}mmrds/{Uri.EscapeDataString(document_id)}",
            null,
            user_name,
            user_value);
    }

    private async Task ensure_target_databases_async()
    {
        await reset_database_async("de_id");
        await restore_de_id_sortable_design_async();
        await reset_database_async("report");
        await restore_report_indexes_and_views_async();
    }

    private async Task reset_database_async(string database_name)
    {
        string database_url = couchdb_url + $"/{db_config.prefix}{database_name}";

        try
        {
            await _couchDbHttpClient.ExecuteAsync("DELETE", database_url, null, user_name, user_value);
            System.Console.WriteLine($">>> DELETED {db_config.prefix}{database_name} database at {DateTime.Now:HH:mm:ss.fff} <<<");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to DELETE {db_config.prefix}{database_name}: {ex.Message}");
        }

        try
        {
            await _couchDbHttpClient.ExecuteAsync("PUT", database_url, null, user_name, user_value);
            System.Console.WriteLine($">>> CREATED {db_config.prefix}{database_name} database at {DateTime.Now:HH:mm:ss.fff} <<<");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to CREATE {db_config.prefix}{database_name}: {ex.Message}");
        }
    }

    private async Task restore_de_id_sortable_design_async()
    {
        try
        {
            string sortable_design = await read_database_script_async("case_design_sortable.json");
            await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                couchdb_url + $"/{db_config.prefix}de_id/_design/sortable",
                sortable_design,
                user_name,
                user_value);
            System.Console.WriteLine($">>> RESTORED {db_config.prefix}de_id/_design/sortable at {DateTime.Now:HH:mm:ss.fff} <<<");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("========== ERROR RESTORING _design/sortable ==========");
            System.Console.WriteLine($"ERROR: Failed to restore de_id/_design/sortable: {ex.Message}");
            System.Console.WriteLine($"Current Directory (BaseDirectory): {AppContext.BaseDirectory}");
            System.Console.WriteLine($"Target URL: {couchdb_url}/{db_config.prefix}de_id/_design/sortable");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            System.Console.WriteLine("======================================================");
            System.Console.WriteLine();
        }
    }

    private async Task restore_report_indexes_and_views_async()
    {
        try
        {
            var report_opioid_index = new Report_Opioid_Index_Struct();
            string index_json = Newtonsoft.Json.JsonConvert.SerializeObject(report_opioid_index);
            await _couchDbHttpClient.ExecuteAsync("POST", couchdb_url + $"/{db_config.prefix}report/_index", index_json, user_name, user_value);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to CREATE opioid report index: {ex.Message}");
        }

        try
        {
            var report_powerbi_index = new Report_PowerBI_Index_Struct();
            string index_json = Newtonsoft.Json.JsonConvert.SerializeObject(report_powerbi_index);
            await _couchDbHttpClient.ExecuteAsync("POST", couchdb_url + $"/{db_config.prefix}report/_index", index_json, user_name, user_value);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to CREATE powerbi report index: {ex.Message}");
        }

        try
        {
            string interactive_report_view = await read_database_script_async("interactive-aggregate-report-view.json");
            await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                couchdb_url + $"/{db_config.prefix}report/_design/interactive_aggregate_report",
                interactive_report_view,
                user_name,
                user_value);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to RESTORE interactive aggregate report view: {ex.Message}");
        }

        try
        {
            string data_summary_view = await read_database_script_async("data-summary-view.json");
            await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                couchdb_url + $"/{db_config.prefix}report/_design/data_summary_view_report",
                data_summary_view,
                user_name,
                user_value);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to RESTORE data summary view: {ex.Message}");
        }
    }

    private async Task<bool> put_document_async(string database_name, string document_json)
    {
        if(string.IsNullOrWhiteSpace(document_json))
        {
            return false;
        }

        JObject document;
        try
        {
            document = JObject.Parse(document_json);
        }
        catch (Exception parse_ex)
        {
            System.Console.WriteLine($"Unable to parse legacy {database_name} document for direct write: {parse_ex.Message}");
            return false;
        }

        string document_id = document.Value<string>("_id");
        if(string.IsNullOrWhiteSpace(document_id))
        {
            System.Console.WriteLine($"Unable to direct-write legacy {database_name} document because _id is missing.");
            return false;
        }

        document.Remove("_rev");
        string payload = document.ToString(Newtonsoft.Json.Formatting.None);
        string url = couchdb_url + $"/{db_config.prefix}{database_name}/{Uri.EscapeDataString(document_id)}";

        for(int attempt = 0; ; attempt++)
        {
            try
            {
                string response = await _couchDbHttpClient.ExecuteAsync("PUT", url, payload, user_name, user_value);
                if(string.IsNullOrWhiteSpace(response))
                {
                    System.Console.WriteLine($"Legacy {database_name} write returned an empty response for '{document_id}'.");
                    return false;
                }

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(response);
                if(result?.ok == true)
                {
                    return true;
                }

                System.Console.WriteLine($"Legacy {database_name} write received unexpected response: {response}");
                return false;
            }
            catch (Exception ex)
            {
                if(!is_transient_write_exception(ex) || attempt >= _write_retry_count)
                {
                    throw;
                }

                int delay_ms = _write_retry_delay_ms * (attempt + 1);
                System.Console.WriteLine(
                    $"Transient legacy {database_name} write failure for '{db_config.url}' " +
                    $"on document '{document_id}'. Retry {attempt + 1} of {_write_retry_count} in {delay_ms} ms.\n{ex.Message}");

                if(delay_ms > 0)
                {
                    await Task.Delay(delay_ms);
                }
            }
        }
    }

    private async Task report_progress_async(legacy_progress progress)
    {
        if(_progress_callback != null)
        {
            await _progress_callback(progress);
        }
    }

    public async Task<legacy_result> executeAsync()
    {
        var result = new legacy_result();

        System.Console.WriteLine();
        System.Console.WriteLine("========== c_document_sync_all_legacy.executeAsync() ==========");
        System.Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        System.Console.WriteLine($"Tenant prefix: '{db_config.prefix}'");
        System.Console.WriteLine($"CouchDB URL: {couchdb_url}");
        System.Console.WriteLine($"Legacy page size: {_page_size}");
        System.Console.WriteLine($"Legacy batch delay: {_batch_delay_ms} ms");
        System.Console.WriteLine($"Legacy write retries: {_write_retry_count}");
        System.Console.WriteLine($"Legacy write retry delay: {_write_retry_delay_ms} ms");
        System.Console.WriteLine("==============================================================");
        System.Console.WriteLine();

        try
        {
            await ensure_target_databases_async();

            for(int page = 0; ; page++)
            {
                int batch_number = page + 1;
                var fetch_stopwatch = Stopwatch.StartNew();
                List<string> document_ids = await get_case_id_batch_async(page * _page_size, _page_size);
                fetch_stopwatch.Stop();

                if(document_ids.Count == 0)
                {
                    result.rebuild_completed_successfully = true;
                    System.Console.WriteLine($"No more source cases after legacy batch {batch_number}. Fetch time: {fetch_stopwatch.ElapsedMilliseconds} ms.");
                    break;
                }

                System.Console.WriteLine($"Starting legacy batch {batch_number} with {document_ids.Count} source cases.");

                long build_elapsed_ms = 0;
                long write_elapsed_ms = 0;
                int batch_de_id_doc_count = 0;
                int batch_report_doc_count = 0;

                foreach(string document_id in document_ids)
                {
                    result.processed_case_count++;
                    result.last_processed_id = document_id;

                    try
                    {
                        var build_stopwatch = Stopwatch.StartNew();
                        string document_json = await get_case_document_async(document_id);
                        var sync_document = new c_sync_document(
                            document_id,
                            document_json,
                            "PUT",
                            metadata_version,
                            db_config,
                            _couchDbHttpClient,
                            _configuration,
                            _host_prefix,
                            rebuild_context: null,
                            skip_revision_lookup: true);
                        var build_result = await sync_document.build_documents_async();
                        build_stopwatch.Stop();
                        build_elapsed_ms += build_stopwatch.ElapsedMilliseconds;

                        var write_stopwatch = Stopwatch.StartNew();

                        if(!string.IsNullOrWhiteSpace(build_result.de_identified_json))
                        {
                            result.total_de_id_doc_count++;
                            batch_de_id_doc_count++;
                            if(!await put_document_async("de_id", build_result.de_identified_json))
                            {
                                result.de_id_bulk_error_count++;
                            }
                        }

                        foreach(string report_document_json in build_result.report_document_json_list ?? Enumerable.Empty<string>())
                        {
                            if(string.IsNullOrWhiteSpace(report_document_json))
                            {
                                continue;
                            }

                            result.total_report_doc_count++;
                            batch_report_doc_count++;
                            if(!await put_document_async("report", report_document_json))
                            {
                                result.report_bulk_error_count++;
                            }
                        }

                        write_stopwatch.Stop();
                        write_elapsed_ms += write_stopwatch.ElapsedMilliseconds;
                    }
                    catch (Exception document_ex)
                    {
                        result.document_error_count++;
                        System.Console.WriteLine($"error running c_docment_sync_all_legacy.document {document_id}\n{document_ex}");
                    }
                }

                result.completed_batch_count = batch_number;
                result.batch_number = batch_number;
                result.fetch_elapsed_ms = fetch_stopwatch.ElapsedMilliseconds;
                result.build_elapsed_ms = build_elapsed_ms;
                result.write_elapsed_ms = write_elapsed_ms;

                await report_progress_async(new legacy_progress
                {
                    batch_number = batch_number,
                    last_processed_id = result.last_processed_id,
                    completed_batch_count = result.completed_batch_count,
                    processed_case_count = result.processed_case_count,
                    skipped_case_count = result.skipped_case_count,
                    document_error_count = result.document_error_count,
                    de_id_bulk_error_count = result.de_id_bulk_error_count,
                    report_bulk_error_count = result.report_bulk_error_count,
                    total_de_id_doc_count = result.total_de_id_doc_count,
                    total_report_doc_count = result.total_report_doc_count,
                    fetch_elapsed_ms = result.fetch_elapsed_ms,
                    build_elapsed_ms = result.build_elapsed_ms,
                    write_elapsed_ms = result.write_elapsed_ms
                });

                System.Console.WriteLine(
                    $"Legacy batch {batch_number}: fetched {document_ids.Count} case ids in {fetch_stopwatch.ElapsedMilliseconds} ms, " +
                    $"built {batch_de_id_doc_count} de_id docs and {batch_report_doc_count} report docs in {build_elapsed_ms} ms, " +
                    $"wrote docs in {write_elapsed_ms} ms.");

                if(_batch_delay_ms > 0)
                {
                    await Task.Delay(_batch_delay_ms);
                }
            }
        }
        catch (Exception ex)
        {
            result.last_error = ex.ToString();
            System.Console.WriteLine($"error running c_docment_sync_all_legacy\n{ex}");
        }

        return result;
    }
}

#endif
