#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace mmria.server.utils;

public sealed class c_document_sync_all
{
    private static readonly SemaphoreSlim s_startup_rebuild_gate = new(1, 1);
    private const string StartupRebuildCheckpointDocumentId = "startup-rebuild-status";

    private sealed class case_batch_document
    {
        public string id { get; init; }
        public string document_json { get; init; }
    }

    private sealed class startup_rebuild_checkpoint
    {
        public string _id { get; set; } = StartupRebuildCheckpointDocumentId;
        public string _rev { get; set; }
        public string status { get; set; }
        public string metadata_version { get; set; }
        public string last_processed_id { get; set; }
        public int completed_batch_count { get; set; }
        public int processed_case_count { get; set; }
        public int skipped_case_count { get; set; }
        public int document_error_count { get; set; }
        public int de_id_bulk_error_count { get; set; }
        public int report_bulk_error_count { get; set; }
        public int total_de_id_doc_count { get; set; }
        public int total_report_doc_count { get; set; }
        public string started_utc { get; set; }
        public string last_updated_utc { get; set; }
        public string completed_utc { get; set; }
        public string last_error { get; set; }
    }

/*
{
"index": {
"partial_filter_selector": {
    "_id": {
        "$regex": "^opioid"

    }
},
"fields": ["_id"]
},
"ddoc" : "opioid-report-index",
"type" : "json"
}
*/
    public sealed class Report_Opioid_Index_Attribute_Partial_Filter_Selector
    {
        public Report_Opioid_Index_Attribute_Partial_Filter_Selector(){}
        public Dictionary<string,string> _id
        { get;set;} = new Dictionary<string, string>(){
        {"$regex", "^opioid"}};

    }

    public sealed class Report_PowerBI_Index_Attribute_Partial_Filter_Selector
    {
        public Report_PowerBI_Index_Attribute_Partial_Filter_Selector(){}
        public Dictionary<string,string> _id
        { get;set;} = new Dictionary<string, string>(){
        {"$regex", "^powerbi"}};

    }
    public sealed class Report_Opioid_Index_Attribute_Struct
    {
        public Report_Opioid_Index_Attribute_Struct(){}

        public  Report_Opioid_Index_Attribute_Partial_Filter_Selector
            partial_filter_selector { get; set;} = new Report_Opioid_Index_Attribute_Partial_Filter_Selector();
            public List<string> fields { get; set;} = new List<string>(){"_id"}; 
    }

    public sealed class Report_PowerBI_Index_Attribute_Struct
    {
        public Report_PowerBI_Index_Attribute_Struct(){}

        public  Report_PowerBI_Index_Attribute_Partial_Filter_Selector
            partial_filter_selector { get; set;} = new Report_PowerBI_Index_Attribute_Partial_Filter_Selector();
            public List<string> fields { get; set;} = new List<string>(){"_id"}; 
    }  
    public sealed class Report_Opioid_Index_Struct
    {
        public Report_Opioid_Index_Struct(){}
        public Report_Opioid_Index_Attribute_Struct index {get;set;} = new Report_Opioid_Index_Attribute_Struct();

        public string ddoc { get; set; } = "opioid-report-index";
        public string type {get; set;} = "json";
    }

    public sealed class Report_PowerBI_Index_Struct
    {
        public Report_PowerBI_Index_Struct(){}
        public Report_PowerBI_Index_Attribute_Struct index {get;set;} = new Report_PowerBI_Index_Attribute_Struct();

        public string ddoc { get; set; } = "powerbi-report-index";
        public string type {get; set;} = "json";
    }

    string couchdb_url;
    string user_name;
    string user_value;

    string metadata_version;

    mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly string _host_prefix;

    public c_document_sync_all 
    (
        string p_couchdb_url, 
        string p_user_name, 
        string p_value,
        string p_metadata_version,
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.couchdb.OverridableConfiguration configuration = null,
        string host_prefix = null
    )
    {
        this.couchdb_url = p_couchdb_url;
        this.user_name = p_user_name;
        this.user_value = p_value;

        metadata_version = p_metadata_version;
        db_config = _db_config;
        _couchDbHttpClient = couchDbHttpClient;
        _configuration = configuration;
        _host_prefix = host_prefix;
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

    private async Task<string> try_read_database_script_async(string file_name)
    {
        try
        {
            return await read_database_script_async(file_name);
        }
        catch (System.IO.FileNotFoundException)
        {
            return null;
        }
        catch (System.IO.DirectoryNotFoundException)
        {
            return null;
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

    private string get_startup_rebuild_checkpoint_url()
    {
        return this.couchdb_url + $"/{db_config.prefix}logging/{StartupRebuildCheckpointDocumentId}";
    }

    private async Task<startup_rebuild_checkpoint> try_get_startup_rebuild_checkpoint_async()
    {
        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            get_startup_rebuild_checkpoint_url(),
            null,
            this.user_name,
            this.user_value
        );

        if(string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var payload = JObject.Parse(response);
        if(string.Equals(payload.Value<string>("error"), "not_found", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var checkpoint = payload.ToObject<startup_rebuild_checkpoint>();
        if(checkpoint != null && string.IsNullOrWhiteSpace(checkpoint._id))
        {
            checkpoint._id = StartupRebuildCheckpointDocumentId;
        }

        return checkpoint;
    }

    private bool can_resume_from_checkpoint(startup_rebuild_checkpoint checkpoint)
    {
        return
            checkpoint != null &&
            !string.Equals(checkpoint.status, "completed", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(checkpoint.metadata_version, metadata_version, StringComparison.OrdinalIgnoreCase);
    }

    private async Task save_startup_rebuild_checkpoint_async(startup_rebuild_checkpoint checkpoint)
    {
        if(checkpoint == null)
        {
            return;
        }

        checkpoint._id = StartupRebuildCheckpointDocumentId;
        checkpoint.last_updated_utc = DateTime.UtcNow.ToString("o");

        for(int attempt = 0; attempt < 2; attempt++)
        {
            string payload = Newtonsoft.Json.JsonConvert.SerializeObject(
                checkpoint,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                }
            );
            string response = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                get_startup_rebuild_checkpoint_url(),
                payload,
                this.user_name,
                this.user_value
            );

            if(!string.IsNullOrWhiteSpace(response))
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(response);
                if(result?.ok == true)
                {
                    checkpoint._rev = result.rev;
                    return;
                }

                var response_payload = JObject.Parse(response);
                if(
                    attempt == 0 &&
                    string.Equals(response_payload.Value<string>("error"), "conflict", StringComparison.OrdinalIgnoreCase)
                )
                {
                    var latest_checkpoint = await try_get_startup_rebuild_checkpoint_async();
                    checkpoint._rev = latest_checkpoint?._rev;
                    continue;
                }
            }

            System.Console.WriteLine(
                $"Failed to save startup rebuild checkpoint for '{db_config.url}'. " +
                $"Response: {response ?? "<null>"}"
            );
            break;
        }
    }

    private async Task<bool> url_endpoint_exists_async(string url)
    {
        try
        {
            await _couchDbHttpClient.ExecuteAsync(
                "HEAD",
                url,
                null,
                this.user_name,
                this.user_value,
                throwOnError: true
            );

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task ensure_database_exists_async(string database_name, bool resetExistingDatabase)
    {
        string database_url = this.couchdb_url + $"/{db_config.prefix}{database_name}";

        if(resetExistingDatabase)
        {
            try
            {
                await _couchDbHttpClient.ExecuteAsync("DELETE", database_url, null, this.user_name, this.user_value);
                System.Console.WriteLine($">>> DELETED {db_config.prefix}{database_name} database at {DateTime.Now:HH:mm:ss.fff} <<<");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to DELETE {db_config.prefix}{database_name}: {ex.Message}");
            }
        }

        if(resetExistingDatabase || !await url_endpoint_exists_async(database_url))
        {
            try
            {
                await _couchDbHttpClient.ExecuteAsync("PUT", database_url, null, this.user_name, this.user_value);
                System.Console.WriteLine($">>> CREATED {db_config.prefix}{database_name} database at {DateTime.Now:HH:mm:ss.fff} <<<");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to CREATE {db_config.prefix}{database_name}: {ex.Message}");
            }
        }
    }

    private async Task ensure_de_id_sortable_design_async(bool forceRestore)
    {
        string design_url = this.couchdb_url + $"/{db_config.prefix}de_id/_design/sortable";
        if(!forceRestore && await url_endpoint_exists_async(design_url))
        {
            return;
        }

        try
        {
            string sortable_design = await read_database_script_async("case_design_sortable.json");
            await _couchDbHttpClient.ExecuteAsync("PUT", design_url, sortable_design, this.user_name, this.user_value);
            System.Console.WriteLine($">>> RESTORED {db_config.prefix}de_id/_design/sortable at {DateTime.Now:HH:mm:ss.fff} <<<");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("========== ERROR RESTORING _design/sortable ==========");
            System.Console.WriteLine($"ERROR: Failed to restore de_id/_design/sortable: {ex.Message}");
            System.Console.WriteLine($"Current Directory (BaseDirectory): {AppContext.BaseDirectory}");
            System.Console.WriteLine($"Target URL: {design_url}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            System.Console.WriteLine("======================================================");
            System.Console.WriteLine();
        }
    }

    private async Task ensure_report_views_and_indexes_async(bool forceRestore)
    {
        string report_url = this.couchdb_url + $"/{db_config.prefix}report";
        bool report_created_or_reset = forceRestore || !await url_endpoint_exists_async(report_url);

        await ensure_database_exists_async("report", forceRestore);

        if(forceRestore || report_created_or_reset)
        {
            try
            {
                var report_opioid_index = new Report_Opioid_Index_Struct();
                string index_json = Newtonsoft.Json.JsonConvert.SerializeObject(report_opioid_index);
                await _couchDbHttpClient.ExecuteAsync("POST", report_url + "/_index", index_json, this.user_name, this.user_value);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to CREATE opioid report index: {ex.Message}");
            }

            try
            {
                var report_powerbi_index = new Report_PowerBI_Index_Struct();
                string index_json = Newtonsoft.Json.JsonConvert.SerializeObject(report_powerbi_index);
                await _couchDbHttpClient.ExecuteAsync("POST", report_url + "/_index", index_json, this.user_name, this.user_value);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to CREATE powerbi report index: {ex.Message}");
            }
        }

        string interactive_report_view_url = report_url + "/_design/interactive_aggregate_report";
        if(forceRestore || !await url_endpoint_exists_async(interactive_report_view_url))
        {
            try
            {
                string interactive_report_view = await read_database_script_async("interactive-aggregate-report-view.json");
                await _couchDbHttpClient.ExecuteAsync("PUT", interactive_report_view_url, interactive_report_view, this.user_name, this.user_value);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to RESTORE interactive aggregate report view: {ex.Message}");
            }
        }

        string data_summary_view_url = report_url + "/_design/data_summary_view_report";
        if(forceRestore || !await url_endpoint_exists_async(data_summary_view_url))
        {
            try
            {
                string data_summary_view = await read_database_script_async("data-summary-view.json");
                await _couchDbHttpClient.ExecuteAsync("PUT", data_summary_view_url, data_summary_view, this.user_name, this.user_value);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to RESTORE data summary view: {ex.Message}");
            }
        }
    }

    private async Task ensure_target_databases_async(bool resetExistingDatabases)
    {
        await ensure_database_exists_async("de_id", resetExistingDatabases);
        await ensure_de_id_sortable_design_async(forceRestore: resetExistingDatabases);
        await ensure_report_views_and_indexes_async(forceRestore: resetExistingDatabases);
    }

    private async Task<List<case_batch_document>> get_case_batch_async(string start_after_id, int take)
    {
        var result = new List<case_batch_document>();
        string next_start_key = start_after_id;

        while(result.Count < take)
        {
            int requested_row_count = take - result.Count;
            var query_parameters = new List<string>
            {
                "include_docs=true"
            };

            if(!string.IsNullOrWhiteSpace(next_start_key))
            {
                requested_row_count++;
                string start_key_parameter = Uri.EscapeDataString(Newtonsoft.Json.JsonConvert.SerializeObject(next_start_key));
                query_parameters.Add($"startkey={start_key_parameter}");
            }

            query_parameters.Add($"limit={requested_row_count}");

            string url = this.couchdb_url + $"/{db_config.prefix}mmrds/_all_docs?{string.Join("&", query_parameters)}";
            string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, this.user_name, this.user_value);

            if(string.IsNullOrWhiteSpace(response))
            {
                break;
            }

            var payload = JObject.Parse(response);
            var rows = payload["rows"] as JArray;
            if(rows == null || rows.Count == 0)
            {
                break;
            }

            string last_row_id = null;

            foreach(var row in rows.OfType<JObject>())
            {
                var id = row.Value<string>("id");
                if(string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                last_row_id = id;

                if(!string.IsNullOrWhiteSpace(next_start_key) && string.Equals(id, next_start_key, StringComparison.Ordinal))
                {
                    continue;
                }

                var doc = row["doc"] as JObject;
                if(doc == null || id.IndexOf("_design/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                result.Add(new case_batch_document
                {
                    id = id,
                    document_json = doc.ToString(Newtonsoft.Json.Formatting.None)
                });

                if(result.Count == take)
                {
                    break;
                }
            }

            if(string.IsNullOrWhiteSpace(last_row_id) || rows.Count < requested_row_count)
            {
                break;
            }

            next_start_key = last_row_id;
        }

        return result;
    }

    private async Task hydrate_existing_revisions_async(string database_name, JArray docs)
    {
        if(docs == null || docs.Count == 0)
        {
            return;
        }

        var document_lookup = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
        foreach(var doc in docs.OfType<JObject>())
        {
            string id = doc.Value<string>("_id");
            if(!string.IsNullOrWhiteSpace(id))
            {
                document_lookup[id] = doc;
            }
        }

        if(document_lookup.Count == 0)
        {
            return;
        }

        string lookup_payload = new JObject
        {
            ["keys"] = new JArray(document_lookup.Keys)
        }.ToString(Newtonsoft.Json.Formatting.None);

        string response = await _couchDbHttpClient.ExecuteAsync(
            "POST",
            this.couchdb_url + $"/{db_config.prefix}{database_name}/_all_docs?include_docs=false",
            lookup_payload,
            this.user_name,
            this.user_value
        );

        if(string.IsNullOrWhiteSpace(response))
        {
            return;
        }

        var payload = JObject.Parse(response);
        var rows = payload["rows"] as JArray;
        if(rows == null)
        {
            return;
        }

        foreach(var row in rows.OfType<JObject>())
        {
            string id = row.Value<string>("id");
            string rev = row["value"]?["rev"]?.ToString();

            if(
                !string.IsNullOrWhiteSpace(id) &&
                !string.IsNullOrWhiteSpace(rev) &&
                document_lookup.TryGetValue(id, out var existing_document)
            )
            {
                existing_document["_rev"] = rev;
            }
        }
    }

    private async Task<(int success_count, int error_count)> bulk_write_async(string database_name, List<string> document_json_list)
    {
        if(document_json_list == null || document_json_list.Count == 0)
        {
            return (0, 0);
        }

        var docs = new JArray(document_json_list.Select(JObject.Parse));
        await hydrate_existing_revisions_async(database_name, docs);
        var payload = new JObject
        {
            ["docs"] = docs
        }.ToString(Newtonsoft.Json.Formatting.None);

        string response = await _couchDbHttpClient.ExecuteAsync("POST", this.couchdb_url + $"/{db_config.prefix}{database_name}/_bulk_docs", payload, this.user_name, this.user_value);

        if(string.IsNullOrWhiteSpace(response) || !response.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            System.Console.WriteLine($"bulk_write_async received unexpected {database_name} response: {response}");
            return (0, document_json_list.Count);
        }

        var results = Newtonsoft.Json.JsonConvert.DeserializeObject<List<mmria.common.model.couchdb.document_put_response>>(response) ?? new();

        int error_count = results.Count(item => item == null || item.ok == false);
        return (results.Count, error_count);
    }

    public async Task executeAsync ()
    {
        const int page_size = 25;
        int max_parallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 2));
        int processed_case_count = 0;
        int skipped_case_count = 0;
        int document_error_count = 0;
        int de_id_bulk_error_count = 0;
        int report_bulk_error_count = 0;
        int total_de_id_doc_count = 0;
        int total_report_doc_count = 0;
        int completed_batch_count = 0;
        string start_after_id = null;
        startup_rebuild_checkpoint checkpoint = null;
        bool is_resume = false;
        bool rebuild_completed_successfully = false;

        var slot_wait_stopwatch = Stopwatch.StartNew();

        System.Console.WriteLine();
        System.Console.WriteLine("========== c_document_sync_all.executeAsync() ==========");
        System.Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        System.Console.WriteLine($"Tenant prefix: '{db_config.prefix}'");
        System.Console.WriteLine($"CouchDB URL: {this.couchdb_url}");
        System.Console.WriteLine($"Page size: {page_size}");
        System.Console.WriteLine($"Max parallelism: {max_parallelism}");
        System.Console.WriteLine("=======================================================");
        System.Console.WriteLine();

        try
        {
            checkpoint = await try_get_startup_rebuild_checkpoint_async();
            is_resume = can_resume_from_checkpoint(checkpoint);
        }
        catch (Exception checkpoint_load_ex)
        {
            System.Console.WriteLine($"Failed to load startup rebuild checkpoint: {checkpoint_load_ex}");
        }

        if(is_resume)
        {
            processed_case_count = checkpoint.processed_case_count;
            skipped_case_count = checkpoint.skipped_case_count;
            document_error_count = checkpoint.document_error_count;
            de_id_bulk_error_count = checkpoint.de_id_bulk_error_count;
            report_bulk_error_count = checkpoint.report_bulk_error_count;
            total_de_id_doc_count = checkpoint.total_de_id_doc_count;
            total_report_doc_count = checkpoint.total_report_doc_count;
            completed_batch_count = checkpoint.completed_batch_count;
            start_after_id = checkpoint.last_processed_id;
            checkpoint.status = "running";
            checkpoint.last_error = null;

            System.Console.WriteLine(
                $"Resuming startup rebuild from checkpoint for '{db_config.url}'. " +
                $"Last processed source case id: '{start_after_id ?? "<beginning>"}'. " +
                $"Completed batches: {completed_batch_count}. Processed cases so far: {processed_case_count}."
            );
        }
        else
        {
            checkpoint = new startup_rebuild_checkpoint
            {
                status = "running",
                metadata_version = metadata_version,
                started_utc = DateTime.UtcNow.ToString("o"),
                last_processed_id = null,
                completed_batch_count = 0,
                processed_case_count = 0,
                skipped_case_count = 0,
                document_error_count = 0,
                de_id_bulk_error_count = 0,
                report_bulk_error_count = 0,
                total_de_id_doc_count = 0,
                total_report_doc_count = 0,
                completed_utc = null,
                last_error = null
            };

            System.Console.WriteLine($"Starting a fresh startup rebuild for '{db_config.url}'.");
        }

        System.Console.WriteLine($"Waiting for startup rebuild slot for '{db_config.url}'.");
        await s_startup_rebuild_gate.WaitAsync();
        slot_wait_stopwatch.Stop();
        System.Console.WriteLine($"Acquired startup rebuild slot for '{db_config.url}'.");

        var active_rebuild_stopwatch = Stopwatch.StartNew();

        try
        {
            try
            {
                await save_startup_rebuild_checkpoint_async(checkpoint);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Unable to persist startup rebuild checkpoint before rebuild execution: {ex.Message}");
            }

            await ensure_target_databases_async(resetExistingDatabases: !is_resume);

            c_document_sync_rebuild_context rebuild_context = null;
            var rebuild_context_stopwatch = Stopwatch.StartNew();
            try
            {
                rebuild_context = await load_rebuild_context_async();
                rebuild_context_stopwatch.Stop();
                System.Console.WriteLine($"Loaded startup rebuild context in {rebuild_context_stopwatch.ElapsedMilliseconds} ms.");
            }
            catch (Exception ex)
            {
                rebuild_context_stopwatch.Stop();
                System.Console.WriteLine($"Startup rebuild context load failed after {rebuild_context_stopwatch.ElapsedMilliseconds} ms. Continuing without cached rebuild context.");
                System.Console.WriteLine(ex.ToString());
            }

            for(;;)
            {
                int batch_number = completed_batch_count + 1;
                try
                {
                    var fetch_stopwatch = Stopwatch.StartNew();
                    var case_batch = await get_case_batch_async(start_after_id, page_size);
                    fetch_stopwatch.Stop();

                    var rows = case_batch ?? new List<case_batch_document>();

                    if(rows.Count == 0)
                    {
                        rebuild_completed_successfully = true;
                        System.Console.WriteLine($"No more source cases after batch {batch_number}. Fetch time: {fetch_stopwatch.ElapsedMilliseconds} ms.");
                        break;
                    }

                    System.Console.WriteLine($"Starting batch {batch_number} with {rows.Count} source cases.");

                    var de_id_documents = new ConcurrentBag<string>();
                    var report_documents = new ConcurrentBag<string>();
                    var build_stopwatch = Stopwatch.StartNew();

                    await Parallel.ForEachAsync(rows, new ParallelOptions { MaxDegreeOfParallelism = max_parallelism }, async (row, cancellation_token) =>
                    {
                        try
                        {
                            var sync_document = new c_sync_document(row.id, row.document_json, "PUT", metadata_version, db_config, _couchDbHttpClient, _configuration, _host_prefix, rebuild_context, skip_revision_lookup: true);
                            var build_result = await sync_document.build_documents_async();

                            if(!string.IsNullOrWhiteSpace(build_result.de_identified_json))
                            {
                                de_id_documents.Add(build_result.de_identified_json);
                            }

                            foreach(var report_document_json in build_result.report_document_json_list)
                            {
                                if(!string.IsNullOrWhiteSpace(report_document_json))
                                {
                                    report_documents.Add(report_document_json);
                                }
                            }
                        }
                        catch (Exception document_ex)
                        {
                            System.Threading.Interlocked.Increment(ref document_error_count);
                            System.Console.WriteLine($"error running c_docment_sync_all.document {row?.id}\n{document_ex}");
                        }
                    });

                    build_stopwatch.Stop();

                    var write_stopwatch = Stopwatch.StartNew();
                    var de_id_write_result = await bulk_write_async("de_id", de_id_documents.ToList());
                    var report_write_result = await bulk_write_async("report", report_documents.ToList());
                    write_stopwatch.Stop();

                    processed_case_count += rows.Count;
                    total_de_id_doc_count += de_id_documents.Count;
                    total_report_doc_count += report_documents.Count;
                    de_id_bulk_error_count += de_id_write_result.error_count;
                    report_bulk_error_count += report_write_result.error_count;
                    completed_batch_count++;
                    start_after_id = rows.Last().id;

                    checkpoint.last_processed_id = start_after_id;
                    checkpoint.completed_batch_count = completed_batch_count;
                    checkpoint.processed_case_count = processed_case_count;
                    checkpoint.skipped_case_count = skipped_case_count;
                    checkpoint.document_error_count = document_error_count;
                    checkpoint.de_id_bulk_error_count = de_id_bulk_error_count;
                    checkpoint.report_bulk_error_count = report_bulk_error_count;
                    checkpoint.total_de_id_doc_count = total_de_id_doc_count;
                    checkpoint.total_report_doc_count = total_report_doc_count;
                    checkpoint.completed_utc = null;
                    checkpoint.last_error = null;

                    try
                    {
                        await save_startup_rebuild_checkpoint_async(checkpoint);
                    }
                    catch (Exception checkpoint_save_ex)
                    {
                        System.Console.WriteLine($"Failed to persist startup rebuild checkpoint after batch {batch_number}: {checkpoint_save_ex}");
                    }

                    System.Console.WriteLine(
                        $"Batch {batch_number}: fetched {rows.Count} cases in {fetch_stopwatch.ElapsedMilliseconds} ms, " +
                        $"built {de_id_documents.Count} de_id docs and {report_documents.Count} report docs in {build_stopwatch.ElapsedMilliseconds} ms, " +
                        $"wrote docs in {write_stopwatch.ElapsedMilliseconds} ms.");
                }
                catch (Exception ex)
                {
                    checkpoint.status = "paused";
                    checkpoint.last_error = ex.ToString();
                    checkpoint.last_processed_id = start_after_id;
                    checkpoint.completed_batch_count = completed_batch_count;
                    checkpoint.processed_case_count = processed_case_count;
                    checkpoint.skipped_case_count = skipped_case_count;
                    checkpoint.document_error_count = document_error_count;
                    checkpoint.de_id_bulk_error_count = de_id_bulk_error_count;
                    checkpoint.report_bulk_error_count = report_bulk_error_count;
                    checkpoint.total_de_id_doc_count = total_de_id_doc_count;
                    checkpoint.total_report_doc_count = total_report_doc_count;

                    try
                    {
                        await save_startup_rebuild_checkpoint_async(checkpoint);
                    }
                    catch (Exception checkpoint_save_ex)
                    {
                        System.Console.WriteLine($"Failed to persist paused startup rebuild checkpoint: {checkpoint_save_ex}");
                    }

                    System.Console.Write($"error running c_docment_sync_all\n{ex}");
                    break;
                }
            }

            active_rebuild_stopwatch.Stop();
            checkpoint.status = rebuild_completed_successfully ? "completed" : "paused";
            checkpoint.last_processed_id = start_after_id;
            checkpoint.completed_batch_count = completed_batch_count;
            checkpoint.processed_case_count = processed_case_count;
            checkpoint.skipped_case_count = skipped_case_count;
            checkpoint.document_error_count = document_error_count;
            checkpoint.de_id_bulk_error_count = de_id_bulk_error_count;
            checkpoint.report_bulk_error_count = report_bulk_error_count;
            checkpoint.total_de_id_doc_count = total_de_id_doc_count;
            checkpoint.total_report_doc_count = total_report_doc_count;
            checkpoint.completed_utc = rebuild_completed_successfully ? DateTime.UtcNow.ToString("o") : null;
            if(rebuild_completed_successfully)
            {
                checkpoint.last_error = null;
            }

            try
            {
                await save_startup_rebuild_checkpoint_async(checkpoint);
            }
            catch (Exception checkpoint_save_ex)
            {
                System.Console.WriteLine($"Failed to persist final startup rebuild checkpoint: {checkpoint_save_ex}");
            }

            System.Console.WriteLine();
            System.Console.WriteLine(
                $"Startup rebuild complete in {active_rebuild_stopwatch.Elapsed.TotalSeconds:F1} seconds " +
                $"after waiting {slot_wait_stopwatch.Elapsed.TotalSeconds:F1} seconds for the startup rebuild slot. " +
                $"Processed {processed_case_count} cases, generated {total_de_id_doc_count} de_id docs and {total_report_doc_count} report docs. " +
                $"Document build errors: {document_error_count}. de_id bulk errors: {de_id_bulk_error_count}. report bulk errors: {report_bulk_error_count}. Skipped cases: {skipped_case_count}.");
            System.Console.WriteLine();
        }
        finally
        {
            s_startup_rebuild_gate.Release();
            System.Console.WriteLine($"Released startup rebuild slot for '{db_config.url}'.");
        }
    }
}

#endif
