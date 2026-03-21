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

    private sealed class case_batch_document
    {
        public string id { get; init; }
        public string document_json { get; init; }
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

    private async Task<List<case_batch_document>> get_case_batch_async(int skip, int take)
    {
        string url = this.couchdb_url + $"/{db_config.prefix}mmrds/_all_docs?include_docs=true&skip={skip}&limit={take}";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, this.user_name, this.user_value);
        var result = new List<case_batch_document>();

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
            var id = row.Value<string>("id");
            var doc = row["doc"] as JObject;
            if(string.IsNullOrWhiteSpace(id) || doc == null || id.IndexOf("_design/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            result.Add(new case_batch_document
            {
                id = id,
                document_json = doc.ToString(Newtonsoft.Json.Formatting.None)
            });
        }

        return result;
    }

    private async Task<(int success_count, int error_count)> bulk_write_async(string database_name, List<string> document_json_list)
    {
        if(document_json_list == null || document_json_list.Count == 0)
        {
            return (0, 0);
        }

        var docs = new JArray(document_json_list.Select(JObject.Parse));
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

        System.Console.WriteLine($"Waiting for startup rebuild slot for '{db_config.url}'.");
        await s_startup_rebuild_gate.WaitAsync();
        slot_wait_stopwatch.Stop();
        System.Console.WriteLine($"Acquired startup rebuild slot for '{db_config.url}'.");

        var active_rebuild_stopwatch = Stopwatch.StartNew();

        try
        {
            try
            {
                await _couchDbHttpClient.ExecuteAsync("DELETE", this.couchdb_url + $"/{db_config.prefix}de_id", null, this.user_name, this.user_value);
                System.Console.WriteLine($">>> DELETED {db_config.prefix}de_id database at {DateTime.Now:HH:mm:ss.fff} <<<");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to DELETE {db_config.prefix}de_id: {ex.Message}");
            }

            try
            {
                await _couchDbHttpClient.ExecuteAsync("DELETE", this.couchdb_url + $"/{db_config.prefix}report", null, this.user_name, this.user_value);
                System.Console.WriteLine($">>> DELETED {db_config.prefix}report database at {DateTime.Now:HH:mm:ss.fff} <<<");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to DELETE {db_config.prefix}report: {ex.Message}");
            }

            try
            {
                await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{db_config.prefix}de_id", null, this.user_name, this.user_value);
                System.Console.WriteLine($">>> CREATED {db_config.prefix}de_id database at {DateTime.Now:HH:mm:ss.fff} <<<");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to CREATE {db_config.prefix}de_id: {ex.Message}");
            }

            try
            {
                string sortable_design = await read_database_script_async("case_design_sortable.json");
                await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{db_config.prefix}de_id/_design/sortable", sortable_design, this.user_name, this.user_value);
                System.Console.WriteLine($">>> RESTORED {db_config.prefix}de_id/_design/sortable at {DateTime.Now:HH:mm:ss.fff} <<<");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("========== ERROR RESTORING _design/sortable ==========");
                System.Console.WriteLine($"ERROR: Failed to restore de_id/_design/sortable: {ex.Message}");
                System.Console.WriteLine($"Current Directory (BaseDirectory): {AppContext.BaseDirectory}");
                System.Console.WriteLine($"Target URL: {this.couchdb_url}/{db_config.prefix}de_id/_design/sortable");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                System.Console.WriteLine("======================================================");
                System.Console.WriteLine();
            }

            try
            {
                await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{db_config.prefix}report", null, this.user_name, this.user_value);
                System.Console.WriteLine($">>> CREATED {db_config.prefix}report database at {DateTime.Now:HH:mm:ss.fff} <<<");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to CREATE {db_config.prefix}report: {ex.Message}");
            }

            try
            {
                var report_opioid_index = new Report_Opioid_Index_Struct();
                string index_json = Newtonsoft.Json.JsonConvert.SerializeObject(report_opioid_index);
                await _couchDbHttpClient.ExecuteAsync("POST", this.couchdb_url + $"/{db_config.prefix}report/_index", index_json, this.user_name, this.user_value);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to CREATE opioid report index: {ex.Message}");
            }

            try
            {
                var report_powerbi_index = new Report_PowerBI_Index_Struct();
                string index_json = Newtonsoft.Json.JsonConvert.SerializeObject(report_powerbi_index);
                await _couchDbHttpClient.ExecuteAsync("POST", this.couchdb_url + $"/{db_config.prefix}report/_index", index_json, this.user_name, this.user_value);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to CREATE powerbi report index: {ex.Message}");
            }

            try
            {
                string interactive_report_view = await read_database_script_async("interactive-aggregate-report-view.json");
                await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{db_config.prefix}report/_design/interactive_aggregate_report", interactive_report_view, this.user_name, this.user_value);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to RESTORE interactive aggregate report view: {ex.Message}");
            }

            try
            {
                string data_summary_view = await read_database_script_async("data-summary-view.json");
                await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{db_config.prefix}report/_design/data_summary_view_report", data_summary_view, this.user_name, this.user_value);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to RESTORE data summary view: {ex.Message}");
            }

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

            for(var page = 0; ; page++)
            {
                try
                {
                    var fetch_stopwatch = Stopwatch.StartNew();
                    var case_batch = await get_case_batch_async(page * page_size, page_size);
                    fetch_stopwatch.Stop();

                    var rows = case_batch ?? new List<case_batch_document>();

                    if(rows.Count == 0)
                    {
                        System.Console.WriteLine($"No more source cases after batch {page + 1}. Fetch time: {fetch_stopwatch.ElapsedMilliseconds} ms.");
                        break;
                    }

                    System.Console.WriteLine($"Starting batch {page + 1} with {rows.Count} source cases.");

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

                    System.Console.WriteLine(
                        $"Batch {page + 1}: fetched {rows.Count} cases in {fetch_stopwatch.ElapsedMilliseconds} ms, " +
                        $"built {de_id_documents.Count} de_id docs and {report_documents.Count} report docs in {build_stopwatch.ElapsedMilliseconds} ms, " +
                        $"wrote docs in {write_stopwatch.ElapsedMilliseconds} ms.");
                }
                catch (Exception ex)
                {
                    System.Console.Write($"error running c_docment_sync_all\n{ex}");
                    break;
                }
            }

            active_rebuild_stopwatch.Stop();
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
