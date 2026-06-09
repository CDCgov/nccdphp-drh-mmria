#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.MMRIARebuild.Model;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.MMRIARebuild.Manager;

public sealed class c_document_sync_all_legacy
{
    private const string RebuildClientName = "CouchDbRebuild";

    public class legacy_progress
    {
        public string document_write_status { get; set; }
        public string index_restore_mode { get; set; }
        public string index_warmup_status { get; set; }
        public List<StartupRebuildIndexSurfaceSummary> index_surfaces { get; set; } = new();
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
    private readonly string _index_restore_mode;
    private readonly int _index_warm_delay_ms;
    private readonly int _index_warm_poll_delay_ms;
    private readonly int _index_warm_timeout_ms;
    private readonly int _index_warm_max_surfaces_per_run;
    private readonly List<StartupRebuildIndexSurfaceSummary> _index_surface_statuses = new();
    private readonly bool _resume_existing_run;
    private readonly string _resume_after_source_id;
    private readonly string _target_generation;
    private readonly string _run_id;
    private readonly DurableTenantRebuildState _resume_state;

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
        string index_restore_mode = null,
        int index_warm_delay_ms = 60000,
        int index_warm_poll_delay_ms = 10000,
        int index_warm_timeout_ms = 15 * 60 * 1000,
        int index_warm_max_surfaces_per_run = 0,
        bool resume_existing_run = false,
        string resume_after_source_id = null,
        string target_generation = null,
        string run_id = null,
        DurableTenantRebuildState resume_state = null
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
        _index_restore_mode = DbRebuildSettings.ResolveStartupRebuildIndexRestoreMode(index_restore_mode, add_indexes_at_beginning);
        _add_indexes_at_beginning = DbRebuildSettings.RestoresIndexesAtBeginning(_index_restore_mode);
        _index_warm_delay_ms = Math.Max(0, index_warm_delay_ms);
        _index_warm_poll_delay_ms = Math.Max(1000, index_warm_poll_delay_ms);
        _index_warm_timeout_ms = Math.Max(1000, index_warm_timeout_ms);
        _index_warm_max_surfaces_per_run = Math.Max(0, index_warm_max_surfaces_per_run);
        _resume_existing_run = resume_existing_run;
        _resume_after_source_id = string.IsNullOrWhiteSpace(resume_after_source_id) ? null : resume_after_source_id.Trim();
        _target_generation = string.IsNullOrWhiteSpace(target_generation) ? run_id : target_generation.Trim();
        _run_id = string.IsNullOrWhiteSpace(run_id) ? _target_generation : run_id.Trim();
        _resume_state = resume_state;

        foreach (var surface in resume_state?.index_surfaces ?? Enumerable.Empty<StartupRebuildIndexSurfaceSummary>())
        {
            _index_surface_statuses.Add(new StartupRebuildIndexSurfaceSummary
            {
                query_surface = surface.query_surface,
                status = surface.status,
                attempt_count = surface.attempt_count,
                elapsed_ms = surface.elapsed_ms,
                started_utc = surface.started_utc,
                last_updated_utc = surface.last_updated_utc,
                completed_utc = surface.completed_utc,
                last_error = surface.last_error
            });
        }
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
        string contentType = "application/json",
        bool throwOnError = false)
    {
        return _couchDbHttpClient.ExecuteAsync(
            method,
            url,
            payload,
            user_name,
            user_value,
            contentType,
            throwOnError: throwOnError,
            clientName: RebuildClientName);
    }

    private async Task execute_barrier_request_async(
        string method,
        string url,
        string payload = null)
    {
        string response = await execute_rebuild_request_async(
            method,
            url,
            payload,
            throwOnError: true);

        if (string.IsNullOrWhiteSpace(response))
        {
            return;
        }

        var payloadObject = JObject.Parse(response);
        if (!string.IsNullOrWhiteSpace(payloadObject.Value<string>("error")))
        {
            throw new HttpRequestException(
                $"CouchDB barrier query returned error '{payloadObject.Value<string>("error")}', reason '{payloadObject.Value<string>("reason")}'.");
        }
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

    private async Task<List<string>> get_case_id_batch_async(string start_after_id, int take)
    {
        string url = couchdb_url + $"/{db_config.prefix}mmrds/_all_docs?limit={take}";
        if(!string.IsNullOrWhiteSpace(start_after_id))
        {
            string encoded_start_key = Uri.EscapeDataString(
                Newtonsoft.Json.JsonConvert.SerializeObject(start_after_id));
            url += $"&startkey={encoded_start_key}&skip=1";
        }

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
        await write_target_generation_markers_async();

        if(_add_indexes_at_beginning)
        {
            System.Console.WriteLine("Restoring legacy de_id/report designs and indexes before rebuild writes start.");
            await restore_target_designs_async(wait_for_index_completion: false);
        }
    }

    private async Task write_target_generation_markers_async()
    {
        await write_target_generation_marker_async("de_id");
        await write_target_generation_marker_async("report");
    }

    private async Task write_target_generation_marker_async(string database_name)
    {
        if(string.IsNullOrWhiteSpace(_target_generation) || string.IsNullOrWhiteSpace(_run_id))
        {
            return;
        }

        var marker = new JObject
        {
            ["_id"] = "_local/mmria-rebuild-generation",
            ["run_id"] = _run_id,
            ["target_generation"] = _target_generation,
            ["tenant"] = _host_prefix ?? db_config?.prefix,
            ["metadata_version"] = metadata_version,
            ["created_utc"] = DateTime.UtcNow.ToString("o")
        };

        await execute_rebuild_request_async(
            "PUT",
            couchdb_url + $"/{db_config.prefix}{database_name}/_local/mmria-rebuild-generation",
            marker.ToString(Newtonsoft.Json.Formatting.None));
    }

    private async Task verify_target_generation_markers_async()
    {
        await verify_target_generation_marker_async("de_id");
        await verify_target_generation_marker_async("report");
    }

    private async Task verify_target_generation_marker_async(string database_name)
    {
        if(string.IsNullOrWhiteSpace(_target_generation) || string.IsNullOrWhiteSpace(_run_id))
        {
            throw new InvalidOperationException(
                $"Resume for {db_config.prefix}{database_name} requires a durable run_id and target_generation. requires_force_fresh");
        }

        string response = await execute_rebuild_request_async(
            "GET",
            couchdb_url + $"/{db_config.prefix}{database_name}/_local/mmria-rebuild-generation");

        if(string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException(
                $"Resume marker missing for {db_config.prefix}{database_name}. requires_force_fresh");
        }

        var marker = JObject.Parse(response);
        if(string.Equals(marker.Value<string>("error"), "not_found", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(marker.Value<string>("run_id"), _run_id, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(marker.Value<string>("target_generation"), _target_generation, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Resume marker mismatch for {db_config.prefix}{database_name}. requires_force_fresh");
        }
    }

    private async Task<string> finalize_target_databases_async(Func<Task> index_progress_callback = null)
    {
        System.Console.WriteLine("Restoring legacy de_id/report designs and indexes after rebuild writes complete.");
        return await restore_target_designs_async(
            wait_for_index_completion: DbRebuildSettings.WaitsForIndexWarmup(_index_restore_mode),
            index_progress_callback);
    }

    private async Task<string> warm_target_indexes_async(Func<Task> index_progress_callback = null)
    {
        System.Console.WriteLine("Warming legacy de_id/report designs and indexes after rebuild writes complete.");
        int warm_cycle_count = 0;

        while(true)
        {
            warm_cycle_count++;
            string warm_status = await restore_target_designs_async(
                wait_for_index_completion: true,
                index_progress_callback);

            if(string.Equals(warm_status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                return "completed";
            }

            var pending_surfaces = _index_surface_statuses
                .Where(item => !string.Equals(item.status, "completed", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.query_surface)
                .ToList();

            if(pending_surfaces.Count == 0)
            {
                return "completed";
            }

            System.Console.WriteLine(
                $">>> INDEX WARM-UP cycle {warm_cycle_count} completed for {db_config.prefix}. " +
                $"{pending_surfaces.Count} surface(s) still pending: {string.Join(", ", pending_surfaces)}. " +
                "Continuing with the next warm-up cycle. <<<");
            await report_index_progress_async(index_progress_callback);
        }
    }

    private async Task<string> restore_target_designs_async(
        bool wait_for_index_completion,
        Func<Task> index_progress_callback = null)
    {
        int warmed_surface_count = 0;
        bool stagger_warmup = DbRebuildSettings.StaggersIndexWarmup(_index_restore_mode);

        async Task restore_surface_async(
            string query_surface_label,
            Func<Task> restore_action,
            Func<Task> barrier_query_action)
        {
            var existing_status = get_or_create_index_surface_status(query_surface_label);
            if(string.Equals(existing_status.status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                System.Console.WriteLine($">>> SKIPPING completed {db_config.prefix}{query_surface_label} during index warm-up. <<<");
                await report_index_progress_async(index_progress_callback);
                return;
            }

            if(is_index_surface_restored_enough(existing_status.status))
            {
                System.Console.WriteLine(
                    $">>> REUSING restored {db_config.prefix}{query_surface_label} during index warm-up. " +
                    $"Current status: {existing_status.status}. <<<");
            }
            else
            {
                await restore_action();
            }

            if(!wait_for_index_completion)
            {
                mark_index_surface_status(query_surface_label, "pending");
                System.Console.WriteLine(
                    $">>> INDEXING PENDING for {db_config.prefix}{query_surface_label}. " +
                    $"Mode '{_index_restore_mode}' restored the query surface without forcing a barrier query. <<<");
                await report_index_progress_async(index_progress_callback);
                return;
            }

            mark_index_surface_status(query_surface_label, "restored");
            await report_index_progress_async(index_progress_callback);

            if(_index_warm_max_surfaces_per_run > 0 &&
                warmed_surface_count >= _index_warm_max_surfaces_per_run)
            {
                mark_index_surface_status(query_surface_label, "pending");
                System.Console.WriteLine(
                    $">>> INDEXING PENDING for {db_config.prefix}{query_surface_label}. " +
                    $"Configured max warm surfaces per cycle is {_index_warm_max_surfaces_per_run}. <<<");
                await report_index_progress_async(index_progress_callback);
                return;
            }

            if((stagger_warmup || DbRebuildSettings.DelaysIndexWarmup(_index_restore_mode)) && _index_warm_delay_ms > 0)
            {
                System.Console.WriteLine(
                    $">>> DELAYING {_index_warm_delay_ms} ms before warming {db_config.prefix}{query_surface_label}. " +
                    $"Mode: {_index_restore_mode}. <<<");
                await Task.Delay(_index_warm_delay_ms);
            }

            warmed_surface_count++;
            await wait_for_query_surface_restore_async(
                query_surface_label,
                barrier_query_action,
                index_progress_callback);
        }

        await restore_surface_async(
            "de_id/_design/sortable",
            restore_de_id_sortable_design_async,
            ensure_de_id_sortable_ready_async);

        await restore_surface_async(
            "report/_design/powerbi-report-index",
            restore_report_powerbi_index_async,
            ensure_report_powerbi_index_ready_async);

        await restore_surface_async(
            "report/_design/opioid-report-index",
            restore_report_opioid_index_async,
            ensure_report_opioid_index_ready_async);

        await restore_surface_async(
            "report/_design/interactive_aggregate_report",
            restore_interactive_report_view_async,
            ensure_interactive_report_view_ready_async);

        await restore_surface_async(
            "report/_design/data_summary_view_report",
            restore_data_summary_view_async,
            ensure_data_summary_view_ready_async);

        return wait_for_index_completion &&
            _index_surface_statuses.All(item => string.Equals(item.status, "completed", StringComparison.OrdinalIgnoreCase))
            ? "completed"
            : "pending";
    }

    private static bool is_index_surface_restored_enough(string status)
    {
        return string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "restored", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "warming", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase);
    }

    private StartupRebuildIndexSurfaceSummary get_or_create_index_surface_status(string query_surface_label)
    {
        var status = _index_surface_statuses.FirstOrDefault(
            item => string.Equals(item.query_surface, query_surface_label, StringComparison.OrdinalIgnoreCase));

        if(status != null)
        {
            return status;
        }

        status = new StartupRebuildIndexSurfaceSummary
        {
            query_surface = query_surface_label,
            status = "not_started",
            started_utc = DateTime.UtcNow.ToString("o"),
            last_updated_utc = DateTime.UtcNow.ToString("o")
        };
        _index_surface_statuses.Add(status);
        return status;
    }

    private void mark_index_surface_status(
        string query_surface_label,
        string status,
        int attempt_count = -1,
        long elapsed_ms = -1,
        string last_error = null)
    {
        var surface_status = get_or_create_index_surface_status(query_surface_label);
        surface_status.status = status;
        surface_status.last_updated_utc = DateTime.UtcNow.ToString("o");

        if(attempt_count >= 0)
        {
            surface_status.attempt_count = attempt_count;
        }

        if(elapsed_ms >= 0)
        {
            surface_status.elapsed_ms = elapsed_ms;
        }

        surface_status.last_error = last_error;

        if(string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            surface_status.completed_utc = DateTime.UtcNow.ToString("o");
        }
    }

    private List<StartupRebuildIndexSurfaceSummary> clone_index_surface_statuses()
    {
        return _index_surface_statuses
            .Select(item => new StartupRebuildIndexSurfaceSummary
            {
                query_surface = item.query_surface,
                status = item.status,
                attempt_count = item.attempt_count,
                elapsed_ms = item.elapsed_ms,
                started_utc = item.started_utc,
                last_updated_utc = item.last_updated_utc,
                completed_utc = item.completed_utc,
                last_error = item.last_error
            })
            .ToList();
    }

    private async Task report_index_progress_async(Func<Task> index_progress_callback)
    {
        if(index_progress_callback != null)
        {
            await index_progress_callback();
        }
    }

    private async Task reset_database_async(string database_name)
    {
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
            await execute_rebuild_request_async(
                "PUT",
                couchdb_url + $"/{db_config.prefix}de_id/_design/sortable",
                sortable_design);
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
        Func<Task> barrier_query_action,
        Func<Task> index_progress_callback = null)
    {
        Stopwatch wait_stopwatch = Stopwatch.StartNew();
        Exception last_exception = null;
        int attempt_count = 0;

        while(true)
        {
            attempt_count++;

            try
            {
                mark_index_surface_status(
                    query_surface_label,
                    "warming",
                    attempt_count,
                    wait_stopwatch.ElapsedMilliseconds);
                await report_index_progress_async(index_progress_callback);

                await barrier_query_action();
                mark_index_surface_status(
                    query_surface_label,
                    "completed",
                    attempt_count,
                    wait_stopwatch.ElapsedMilliseconds);
                await report_index_progress_async(index_progress_callback);
                System.Console.WriteLine(
                    $">>> COMPLETED {db_config.prefix}{query_surface_label} barrier query at {DateTime.Now:HH:mm:ss.fff} " +
                    $"after attempt {attempt_count} and {wait_stopwatch.ElapsedMilliseconds} ms <<<");
                return;
            }
            catch (Exception ex)
            {
                last_exception = ex;
                mark_index_surface_status(
                    query_surface_label,
                    "warming",
                    attempt_count,
                    wait_stopwatch.ElapsedMilliseconds,
                    ex.Message);
                await report_index_progress_async(index_progress_callback);

                if(attempt_count == 1 || attempt_count % 5 == 0)
                {
                    System.Console.WriteLine(
                        $">>> WAITING FOR {db_config.prefix}{query_surface_label} barrier query to succeed. " +
                        $"Attempt {attempt_count}, elapsed {wait_stopwatch.ElapsedMilliseconds} ms, " +
                        $"next retry in {_index_warm_poll_delay_ms} ms, timeout {_index_warm_timeout_ms} ms. " +
                        $"Last response: {ex.Message} <<<");
                }
            }

            if(wait_stopwatch.ElapsedMilliseconds >= _index_warm_timeout_ms)
            {
                mark_index_surface_status(
                    query_surface_label,
                    "failed",
                    attempt_count,
                    wait_stopwatch.ElapsedMilliseconds,
                    last_exception?.Message);
                await report_index_progress_async(index_progress_callback);
                throw new TimeoutException(
                    $"Timed out waiting for barrier query for {db_config.prefix}{query_surface_label} " +
                    $"after {wait_stopwatch.ElapsedMilliseconds} ms and {attempt_count} attempt(s).",
                    last_exception);
            }

            await Task.Delay(_index_warm_poll_delay_ms);
        }
    }

    private Task ensure_de_id_sortable_ready_async()
    {
        return execute_barrier_request_async(
            "GET",
            couchdb_url + $"/{db_config.prefix}de_id/_design/sortable/_view/by_date_created?limit=1&update=true");
    }

    private Task ensure_report_powerbi_index_ready_async()
    {
        return ensure_report_index_ready_async("^powerbi-", "powerbi-report-index");
    }

    private Task ensure_report_opioid_index_ready_async()
    {
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

        await execute_barrier_request_async(
            "POST",
            couchdb_url + $"/{db_config.prefix}report/_find",
            payload.ToString(Newtonsoft.Json.Formatting.None));
    }

    private Task ensure_interactive_report_view_ready_async()
    {
        return execute_barrier_request_async(
            "GET",
            couchdb_url + $"/{db_config.prefix}report/_design/interactive_aggregate_report/_view/indicator_id?limit=1&update=true");
    }

    private Task ensure_data_summary_view_ready_async()
    {
        return execute_barrier_request_async(
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
        string payload = document.ToString(Newtonsoft.Json.Formatting.None);
        string url = couchdb_url + $"/{db_config.prefix}{database_name}/{Uri.EscapeDataString(document_id)}";
        int conflict_count = 0;

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

                var response_payload = JObject.Parse(response);
                if(string.Equals(response_payload.Value<string>("error"), "conflict", StringComparison.OrdinalIgnoreCase))
                {
                    conflict_count++;
                    if(conflict_count > _write_retry_count + 1)
                    {
                        System.Console.WriteLine($"Legacy {database_name} write conflict retry limit reached for '{document_id}'.");
                        return false;
                    }

                    string current_revision = await try_get_document_revision_async(database_name, document_id);
                    if(string.IsNullOrWhiteSpace(current_revision))
                    {
                        System.Console.WriteLine($"Legacy {database_name} write conflict for '{document_id}' but no current _rev was found.");
                        return false;
                    }

                    document["_rev"] = current_revision;
                    payload = document.ToString(Newtonsoft.Json.Formatting.None);
                    continue;
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

    private async Task<string> try_get_document_revision_async(string database_name, string document_id)
    {
        string response = await execute_rebuild_request_async(
            "GET",
            couchdb_url + $"/{db_config.prefix}{database_name}/{Uri.EscapeDataString(document_id)}");

        if(string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var payload = JObject.Parse(response);
        if(string.Equals(payload.Value<string>("error"), "not_found", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return payload.Value<string>("_rev");
    }

    private async Task report_progress_async(legacy_progress progress)
    {
        if(_progress_callback != null)
        {
            await _progress_callback(progress);
        }
    }

    private async Task report_result_progress_async(legacy_result result)
    {
        if(result == null)
        {
            return;
        }

        result.index_restore_mode = _index_restore_mode;
        result.index_surfaces = clone_index_surface_statuses();
        await report_progress_async(result);
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
        var result = new legacy_result
        {
            document_write_status = "running",
            index_restore_mode = _index_restore_mode,
            index_warmup_status = "not_started",
            index_surfaces = clone_index_surface_statuses(),
            last_processed_id = _resume_existing_run ? _resume_state?.last_completed_source_id ?? _resume_after_source_id : null,
            completed_batch_count = _resume_existing_run ? _resume_state?.completed_batch_count ?? 0 : 0,
            processed_case_count = _resume_existing_run ? _resume_state?.processed_case_count ?? 0 : 0,
            skipped_case_count = _resume_existing_run ? _resume_state?.skipped_case_count ?? 0 : 0,
            document_error_count = _resume_existing_run ? _resume_state?.document_error_count ?? 0 : 0,
            de_id_bulk_error_count = _resume_existing_run ? _resume_state?.de_id_bulk_error_count ?? 0 : 0,
            report_bulk_error_count = _resume_existing_run ? _resume_state?.report_bulk_error_count ?? 0 : 0,
            total_de_id_doc_count = _resume_existing_run ? _resume_state?.total_de_id_doc_count ?? 0 : 0,
            total_report_doc_count = _resume_existing_run ? _resume_state?.total_report_doc_count ?? 0 : 0
        };
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
        System.Console.WriteLine($"Legacy design/index restore mode: {_index_restore_mode}");
        System.Console.WriteLine($"Legacy index warm delay: {_index_warm_delay_ms} ms");
        System.Console.WriteLine($"Legacy index warm poll delay: {_index_warm_poll_delay_ms} ms");
        System.Console.WriteLine($"Legacy index warm timeout: {_index_warm_timeout_ms} ms");
        System.Console.WriteLine($"Legacy index warm max surfaces per cycle: {(_index_warm_max_surfaces_per_run <= 0 ? "all" : _index_warm_max_surfaces_per_run.ToString())}");
        System.Console.WriteLine("==============================================================");
        System.Console.WriteLine();

        try
        {
            if(_resume_existing_run)
            {
                System.Console.WriteLine($"Resuming existing rebuild generation '{_target_generation}' after source id '{result.last_processed_id ?? "<none>"}'.");
                await verify_target_generation_markers_async();
            }
            else
            {
                await prepare_target_databases_async();
            }

            bool has_more_documents = false;
            bool document_writes_already_completed = _resume_existing_run &&
                string.Equals(_resume_state?.document_write_status, "completed", StringComparison.OrdinalIgnoreCase);

            if(!document_writes_already_completed)
            {
                var rebuild_context = await load_rebuild_context_async();
                string start_after_id = result.last_processed_id;

                for(;;)
                {
                    int batch_number = result.completed_batch_count + 1;
                    var fetch_stopwatch = Stopwatch.StartNew();
                    List<string> document_ids = await get_case_id_batch_async(start_after_id, _page_size);
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
                            result.last_processed_id = document_id;
                            start_after_id = document_id;
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
                        document_write_status = result.document_write_status,
                        index_restore_mode = result.index_restore_mode,
                        index_warmup_status = result.index_warmup_status,
                        index_surfaces = clone_index_surface_statuses(),
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
            }
            else
            {
                result.document_write_status = "completed";
                System.Console.WriteLine($"{tenant_log_label} document writes were already completed; resuming design/index work only.");
            }

            result.document_write_status = "completed";
            result.index_surfaces = clone_index_surface_statuses();
            await report_result_progress_async(result);

            if((has_more_documents || result.completed_batch_count == 0) && !_add_indexes_at_beginning)
            {
                System.Console.WriteLine($"{tenant_log_label} write phase finished. Restoring report/de_id indexes and designs.");
            }

            if(!_add_indexes_at_beginning)
            {
                result.index_warmup_status = DbRebuildSettings.WaitsForIndexWarmup(_index_restore_mode)
                    ? "running"
                    : "pending";
                await report_result_progress_async(result);
                result.index_warmup_status = await finalize_target_databases_async(
                    async () =>
                    {
                        result.index_surfaces = clone_index_surface_statuses();
                        await report_result_progress_async(result);
                    });
            }
            else
            {
                result.index_warmup_status = "pending";
                await report_result_progress_async(result);
            }

            if(DbRebuildSettings.DefersIndexWarmup(_index_restore_mode) &&
                !string.Equals(result.index_warmup_status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                result.index_warmup_status = "running";
                await report_result_progress_async(result);
                result.index_warmup_status = await warm_target_indexes_async(
                    async () =>
                    {
                        result.index_surfaces = clone_index_surface_statuses();
                        await report_result_progress_async(result);
                    });
            }

            result.index_surfaces = clone_index_surface_statuses();
            result.rebuild_completed_successfully = true;
        }
        catch (Exception ex)
        {
            result.last_error = ex.ToString();
            if(!string.Equals(result.document_write_status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                result.document_write_status = "failed";
            }

            result.index_surfaces = clone_index_surface_statuses();
            if(result.index_surfaces.Any(item => string.Equals(item.status, "failed", StringComparison.OrdinalIgnoreCase)))
            {
                result.index_warmup_status = "failed";
            }

            System.Console.WriteLine($"error running c_docment_sync_all_legacy\n{ex}");
        }

        return result;
    }
}

#endif


