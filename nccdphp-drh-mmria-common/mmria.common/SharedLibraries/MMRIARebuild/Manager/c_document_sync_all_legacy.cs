#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.Case;
using mmria.common.SharedLibraries.DeIdentified;
using mmria.common.SharedLibraries.Report;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.MMRIARebuild.Manager;

public sealed class c_document_sync_all_legacy
{
    private const string RebuildClientName = "CouchDbRebuild";
    private const int BarrierQueryPollDelayMs = 2000;
    private const int BarrierQueryTimeoutMs = 15 * 60 * 1000;

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
    private readonly bool _add_indexes_at_beginning;
    private readonly Func<legacy_progress, Task> _progress_callback;
    private readonly ICaseRepository _caseRepository;
    private readonly IDeIdentifiedRepository _deIdentifiedRepository;
    private readonly IReportRepository _reportRepository;
    private readonly mmria.common.SharedLibraries.MetadataVersion.IMetadataRepository _metadataRepository;

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
        bool add_indexes_at_beginning = true,
        Func<legacy_progress, Task> progress_callback = null,
        ICaseRepository caseRepository = null,
        IDeIdentifiedRepository deIdentifiedRepository = null,
        IReportRepository reportRepository = null,
        mmria.common.SharedLibraries.MetadataVersion.IMetadataRepository metadataRepository = null
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
        _add_indexes_at_beginning = add_indexes_at_beginning;
        _progress_callback = progress_callback;
        _caseRepository = caseRepository;
        _deIdentifiedRepository = deIdentifiedRepository;
        _reportRepository = reportRepository;
        _metadataRepository = metadataRepository;
    }

    private string get_database_scripts_directory()
    {
        return c_case_template_resolver.GetDatabaseScriptsDirectory();
    }

    private async Task<string> read_database_script_async(string file_name)
    {
        return await c_case_template_resolver.ReadDatabaseScriptAsync(file_name, System.Console.WriteLine);
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

    private Task<string> execute_rebuild_request_async(
        string method,
        string url,
        string payload = null,
        string contentType = "application/json")
    {
        return _couchDbHttpClient.ExecuteAsync(
            method,
            url,
            payload,
            user_name,
            user_value,
            contentType,
            clientName: RebuildClientName);
    }

    private async Task<c_document_sync_rebuild_context> load_rebuild_context_async()
    {
        string metadata_url = db_config.url + $"/metadata/version_specification-{metadata_version}/metadata";

        var metadata_task = _couchDbHttpClient.ExecuteAsync("GET", metadata_url, null, db_config.user_name, db_config.user_value);
        var de_identified_list_task = _couchDbHttpClient.ExecuteAsync("GET", db_config.url + "/metadata/de-identified-list", null, db_config.user_name, db_config.user_value);
        var case_template_task = c_case_template_resolver.ReadBestAvailableCaseTemplateAsync(metadata_version, System.Console.WriteLine);

        await Task.WhenAll(metadata_task, de_identified_list_task, case_template_task);

        var metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_task.Result);
        var de_identified_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(de_identified_list_task.Result);
        var de_identified_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(string path in (IList<object>)(((IDictionary<string, object>)de_identified_expando_object)["paths"]))
        {
            de_identified_set.Add(path);
        }

        return new c_document_sync_rebuild_context
        {
            metadata = metadata,
            de_identified_set = de_identified_set,
            case_template_json = case_template_task.Result
        };
    }

    private async Task<List<string>> get_case_id_batch_async(int skip, int take)
    {
        string url = couchdb_url + $"/{db_config.prefix}mmrds/_all_docs?skip={skip}&limit={take}";
        string response = await execute_rebuild_request_async("GET", url);
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
        return await execute_rebuild_request_async(
            "GET",
            couchdb_url + $"/{db_config.prefix}mmrds/{Uri.EscapeDataString(document_id)}");
    }

    private async Task reset_target_databases_async()
    {
        await reset_database_async("de_id");
        await reset_database_async("report");
    }

    private async Task prepare_target_databases_async()
    {
        System.Console.WriteLine("Preparing legacy de_id/report databases before rebuild writes start.");
        await reset_target_databases_async();

        if(_add_indexes_at_beginning)
        {
            System.Console.WriteLine("Restoring legacy de_id/report designs and indexes before rebuild writes start.");
            await restore_target_designs_async(wait_for_index_completion: false);
        }
    }

    private async Task finalize_target_databases_async()
    {
        System.Console.WriteLine("Restoring legacy de_id/report designs and indexes after rebuild writes complete.");
        await restore_target_designs_async(wait_for_index_completion: true);
    }

    private async Task restore_target_designs_async(bool wait_for_index_completion)
    {
        await restore_de_id_sortable_design_async();
        await wait_for_query_surface_restore_async(
            "de_id/_design/sortable",
            wait_for_index_completion,
            ensure_de_id_sortable_ready_async);

        await restore_report_powerbi_index_async();
        await wait_for_query_surface_restore_async(
            "report/_design/powerbi-report-index",
            wait_for_index_completion,
            ensure_report_powerbi_index_ready_async);

        await restore_report_opioid_index_async();
        await wait_for_query_surface_restore_async(
            "report/_design/opioid-report-index",
            wait_for_index_completion,
            ensure_report_opioid_index_ready_async);

        await restore_interactive_report_view_async();
        await wait_for_query_surface_restore_async(
            "report/_design/interactive_aggregate_report",
            wait_for_index_completion,
            ensure_interactive_report_view_ready_async);

        await restore_data_summary_view_async();
        await wait_for_query_surface_restore_async(
            "report/_design/data_summary_view_report",
            wait_for_index_completion,
            ensure_data_summary_view_ready_async);
    }

    private async Task reset_database_async(string database_name)
    {
        if(string.Equals(database_name, "de_id", StringComparison.OrdinalIgnoreCase) && _deIdentifiedRepository != null)
        {
            await _deIdentifiedRepository.DropAndResetAsync(db_config);
            System.Console.WriteLine($">>> DELETED+CREATED {db_config.prefix}de_id via IDeIdentifiedRepository at {DateTime.Now:HH:mm:ss.fff} <<<");
            return;
        }

        if(string.Equals(database_name, "report", StringComparison.OrdinalIgnoreCase) && _reportRepository != null)
        {
            await _reportRepository.DropAndResetWithSystemDocPreservationAsync(db_config);
            System.Console.WriteLine($">>> DELETED+CREATED {db_config.prefix}report via IReportRepository at {DateTime.Now:HH:mm:ss.fff} <<<");
            return;
        }

        string database_url = couchdb_url + $"/{db_config.prefix}{database_name}";

        try
        {
            await execute_rebuild_request_async("DELETE", database_url);
            System.Console.WriteLine($">>> DELETED {db_config.prefix}{database_name} database at {DateTime.Now:HH:mm:ss.fff} <<<");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to DELETE {db_config.prefix}{database_name}: {ex.Message}");
        }

        try
        {
            await execute_rebuild_request_async("PUT", database_url);
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
            string sortable_design = await read_database_script_async("case_design_sortable_de_id.json");
            if(_deIdentifiedRepository != null)
            {
                await _deIdentifiedRepository.EnsureDesignDocumentAsync("sortable", sortable_design, db_config);
            }
            else
            {
                await execute_rebuild_request_async(
                    "PUT",
                    couchdb_url + $"/{db_config.prefix}de_id/_design/sortable",
                    sortable_design);
            }
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
            throw;
        }
    }

    private async Task restore_report_powerbi_index_async()
    {
        await restore_report_query_surface_async(
            "_design/powerbi-report-index",
            async () =>
            {
                var report_powerbi_index = new Report_PowerBI_Index_Struct();
                string index_json = Newtonsoft.Json.JsonConvert.SerializeObject(report_powerbi_index);
                if(_reportRepository != null)
                    await _reportRepository.EnsureIndexAsync(index_json, db_config);
                else
                    await execute_rebuild_request_async("POST", couchdb_url + $"/{db_config.prefix}report/_index", index_json);
            });
    }

    private async Task restore_report_opioid_index_async()
    {
        await restore_report_query_surface_async(
            "_design/opioid-report-index",
            async () =>
            {
                var report_opioid_index = new Report_Opioid_Index_Struct();
                string index_json = Newtonsoft.Json.JsonConvert.SerializeObject(report_opioid_index);
                if(_reportRepository != null)
                    await _reportRepository.EnsureIndexAsync(index_json, db_config);
                else
                    await execute_rebuild_request_async("POST", couchdb_url + $"/{db_config.prefix}report/_index", index_json);
            });
    }

    private async Task restore_interactive_report_view_async()
    {
        await restore_report_query_surface_async(
            "_design/interactive_aggregate_report",
            async () =>
            {
                string interactive_report_view = await read_database_script_async("interactive-aggregate-report-view.json");
                if(_reportRepository != null)
                    await _reportRepository.EnsureDesignDocumentAsync("interactive_aggregate_report", interactive_report_view, db_config);
                else
                    await execute_rebuild_request_async(
                        "PUT",
                        couchdb_url + $"/{db_config.prefix}report/_design/interactive_aggregate_report",
                        interactive_report_view);
            });
    }

    private async Task restore_data_summary_view_async()
    {
        await restore_report_query_surface_async(
            "_design/data_summary_view_report",
            async () =>
            {
                string data_summary_view = await read_database_script_async("data-summary-view.json");
                if(_reportRepository != null)
                    await _reportRepository.EnsureDesignDocumentAsync("data_summary_view_report", data_summary_view, db_config);
                else
                    await execute_rebuild_request_async(
                        "PUT",
                        couchdb_url + $"/{db_config.prefix}report/_design/data_summary_view_report",
                        data_summary_view);
            });
    }

    private async Task restore_report_query_surface_async(string query_surface_label, Func<Task> restore_action)
    {
        try
        {
            await restore_action();
            System.Console.WriteLine($">>> RESTORED {db_config.prefix}report/{query_surface_label} at {DateTime.Now:HH:mm:ss.fff} <<<");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("========== ERROR RESTORING REPORT DESIGNS / INDEXES ==========");
            System.Console.WriteLine($"ERROR: Failed to restore report query surface '{query_surface_label}': {ex.Message}");
            System.Console.WriteLine($"Target URL Prefix: {couchdb_url}/{db_config.prefix}report");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            System.Console.WriteLine("==============================================================");
            System.Console.WriteLine();
            throw;
        }
    }

    private async Task wait_for_query_surface_restore_async(
        string query_surface_label,
        bool wait_for_index_completion,
        Func<Task> barrier_query_action)
    {
        if(!wait_for_index_completion)
        {
            return;
        }

        Stopwatch wait_stopwatch = Stopwatch.StartNew();
        bool logged_wait_message = false;
        Exception last_exception = null;

        while(true)
        {
            try
            {
                await barrier_query_action();
                System.Console.WriteLine(
                    $">>> COMPLETED {db_config.prefix}{query_surface_label} barrier query at {DateTime.Now:HH:mm:ss.fff} " +
                    $"after waiting {wait_stopwatch.ElapsedMilliseconds} ms <<<");
                return;
            }
            catch (Exception ex)
            {
                last_exception = ex;

                if(!logged_wait_message)
                {
                    System.Console.WriteLine(
                        $">>> WAITING FOR {db_config.prefix}{query_surface_label} barrier query to succeed. " +
                        $"Initial response: {ex.Message} <<<");
                    logged_wait_message = true;
                }
            }

            if(wait_stopwatch.ElapsedMilliseconds >= BarrierQueryTimeoutMs)
            {
                throw new TimeoutException(
                    $"Timed out waiting for barrier query for {db_config.prefix}{query_surface_label} " +
                    $"after {wait_stopwatch.ElapsedMilliseconds} ms.",
                    last_exception);
            }

            await Task.Delay(BarrierQueryPollDelayMs);
        }
    }

    private Task ensure_de_id_sortable_ready_async()
    {
        if(_deIdentifiedRepository != null)
            return _deIdentifiedRepository.WaitForIndexReadyAsync(db_config);
        return execute_rebuild_request_async(
            "GET",
            couchdb_url + $"/{db_config.prefix}de_id/_design/sortable/_view/by_date_created?limit=1&update=true");
    }

    private Task ensure_report_powerbi_index_ready_async()
    {
        if(_reportRepository != null)
            return _reportRepository.WaitForIndexReadyAsync(db_config);
        return ensure_report_index_ready_async("^powerbi-", "powerbi-report-index");
    }

    private Task ensure_report_opioid_index_ready_async()
    {
        if(_reportRepository != null)
            return _reportRepository.WaitForIndexReadyAsync(db_config);
        return ensure_report_index_ready_async("^opioid-", "opioid-report-index");
    }

    private async Task ensure_report_index_ready_async(string id_regex, string design_doc_name)
    {
        var payload = new JObject
        {
            ["selector"] = new JObject
            {
                ["_id"] = new JObject
                {
                    ["$regex"] = id_regex
                }
            },
            ["use_index"] = design_doc_name,
            ["limit"] = 1
        };

        await execute_rebuild_request_async(
            "POST",
            couchdb_url + $"/{db_config.prefix}report/_find",
            payload.ToString(Newtonsoft.Json.Formatting.None));
    }

    private Task ensure_interactive_report_view_ready_async()
    {
        if(_reportRepository != null)
            return _reportRepository.WaitForIndexReadyAsync(db_config);
        return execute_rebuild_request_async(
            "GET",
            couchdb_url + $"/{db_config.prefix}report/_design/interactive_aggregate_report/_view/indicator_id?limit=1&update=true");
    }

    private Task ensure_data_summary_view_ready_async()
    {
        if(_reportRepository != null)
            return _reportRepository.WaitForIndexReadyAsync(db_config);
        return execute_rebuild_request_async(
            "GET",
            couchdb_url + $"/{db_config.prefix}report/_design/data_summary_view_report/_view/year_of_death?limit=1&update=true");
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

        // Route through repository when available
        if(string.Equals(database_name, "de_id", StringComparison.OrdinalIgnoreCase) && _deIdentifiedRepository != null)
        {
            var put_result = await _deIdentifiedRepository.UpsertDocumentAsync(document_id, document, db_config);
            return put_result?.ok == true;
        }
        if(string.Equals(database_name, "report", StringComparison.OrdinalIgnoreCase) && _reportRepository != null)
        {
            var put_result = await _reportRepository.UpsertDocumentAsync(document_id, document, db_config);
            return put_result?.ok == true;
        }

        string payload = document.ToString(Newtonsoft.Json.Formatting.None);
        string url = couchdb_url + $"/{db_config.prefix}{database_name}/{Uri.EscapeDataString(document_id)}";

        for(int attempt = 0; ; attempt++)
        {
            try
            {
                string response = await execute_rebuild_request_async("PUT", url, payload);
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

    private string get_tenant_log_label()
    {
        string tenant_name = _host_prefix;

        if(string.IsNullOrWhiteSpace(tenant_name))
        {
            tenant_name = db_config?.prefix;
        }

        tenant_name = tenant_name?.Trim();
        if(string.IsNullOrWhiteSpace(tenant_name))
        {
            return "Tenant";
        }

        tenant_name = tenant_name.TrimEnd('-', '_');
        if(string.IsNullOrWhiteSpace(tenant_name))
        {
            return "Tenant";
        }

        return char.ToUpperInvariant(tenant_name[0]) + tenant_name.Substring(1);
    }

    public async Task<legacy_result> executeAsync()
    {
        var result = new legacy_result();
        string tenant_log_label = get_tenant_log_label();

        System.Console.WriteLine();
        System.Console.WriteLine("========== c_document_sync_all_legacy.executeAsync() ==========");
        System.Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        System.Console.WriteLine($"Tenant: '{tenant_log_label}'");
        System.Console.WriteLine($"CouchDB URL: {couchdb_url}");
        System.Console.WriteLine($"Legacy page size: {_page_size}");
        System.Console.WriteLine($"{tenant_log_label} batch delay: {_batch_delay_ms} ms");
        System.Console.WriteLine($"Legacy write retries: {_write_retry_count}");
        System.Console.WriteLine($"Legacy write retry delay: {_write_retry_delay_ms} ms");
        System.Console.WriteLine($"Legacy design/index restore mode: {(_add_indexes_at_beginning ? "beginning" : "end")}");
        System.Console.WriteLine("==============================================================");
        System.Console.WriteLine();

        try
        {
            await prepare_target_databases_async();
            var rebuild_context = await load_rebuild_context_async();

            bool has_more_documents = false;
            for(int page = 0; ; page++)
            {
                int batch_number = page + 1;
                var fetch_stopwatch = Stopwatch.StartNew();
                List<string> document_ids = await get_case_id_batch_async(page * _page_size, _page_size);
                fetch_stopwatch.Stop();

                if(document_ids.Count == 0)
                {
                    System.Console.WriteLine($"No more source cases after {tenant_log_label} Batch {batch_number}. Fetch time: {fetch_stopwatch.ElapsedMilliseconds} ms.");
                    break;
                }

                has_more_documents = true;
                System.Console.WriteLine($"Starting {tenant_log_label} Batch {batch_number} with {document_ids.Count} source cases.");

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
                            _metadataRepository,
                            deIdentifiedRepository: null,
                            reportRepository: null,
                            configuration: _configuration,
                            host_prefix: _host_prefix,
                            rebuild_context: rebuild_context,
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
                    catch (System.Collections.Generic.KeyNotFoundException document_ex)
                    {
                        result.document_error_count++;
                        System.Console.WriteLine($"[DbRebuildError] [tenant:{get_tenant_log_label()}] [case:{document_id}] KeyNotFoundException: {document_ex.Message}");
                    }
                    catch (Exception document_ex)
                    {
                        result.document_error_count++;
                        System.Console.WriteLine($"[DbRebuildError] [tenant:{get_tenant_log_label()}] [case:{document_id}] error running document rebuild — {document_ex.GetType().Name}: {document_ex.Message}");
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
                    $"{tenant_log_label} Batch {batch_number}: fetched {document_ids.Count} case ids in {fetch_stopwatch.ElapsedMilliseconds} ms, " +
                    $"built {batch_de_id_doc_count} de_id docs and {batch_report_doc_count} report docs in {build_elapsed_ms} ms, " +
                    $"wrote docs in {write_elapsed_ms} ms.");

                if(_batch_delay_ms > 0)
                {
                    await Task.Delay(_batch_delay_ms);
                }
            }

            if((has_more_documents || result.completed_batch_count == 0) && !_add_indexes_at_beginning)
            {
                System.Console.WriteLine($"{tenant_log_label} write phase finished. Restoring report/de_id indexes and designs.");
            }

            if(!_add_indexes_at_beginning)
            {
                await finalize_target_databases_async();
            }
            result.rebuild_completed_successfully = true;
        }
        catch (Exception ex)
        {
            result.last_error = ex.ToString();
            System.Console.WriteLine($"[DbRebuildError] [tenant:{get_tenant_log_label()}] error running c_document_sync_all_legacy — {ex.GetType().Name}: {ex.Message}");
        }

        return result;
    }
}

#endif


