using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace mmria.server.utils;

public sealed class c_document_sync_all
{
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

    common.couchdb.DBConfigurationDetail connection;

    string metadata_release_version_name;
    private string couchdb_url;
    private string user_name;
    private string user_value;

    private string prefix;
    mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly string _host_prefix;
    private readonly Action<string> _progressCallback;

    public c_document_sync_all (
        common.couchdb.DBConfigurationDetail p_connection, 
        string p_metadata_release_version_name, 
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.couchdb.OverridableConfiguration configuration = null,
        string host_prefix = null,
        Action<string> progressCallback = null
    )
    {
        this.connection = p_connection;
        metadata_release_version_name = p_metadata_release_version_name;
        this.couchdb_url = connection.url;
        this.user_name = connection.user_name;
        this.user_value = connection.user_value;
        this.prefix = connection.prefix;
        _couchDbHttpClient = couchDbHttpClient;
        _configuration = configuration;
        _host_prefix = host_prefix;
        _progressCallback = progressCallback;
    }

    private void ReportProgress(string message)
    {
        _progressCallback?.Invoke(message);
    }

    private async Task<string> read_case_template_json_async()
    {
        try
        {
            var case_template_path = mmria.common.SharedLibraries.MMRIAServices.Helper.MMRIAServicesHelper.ResolveDatabaseScriptPath($"case-version-{metadata_release_version_name}.json");

            using (var sr = new System.IO.StreamReader(case_template_path))
            {
                return await sr.ReadToEndAsync();
            }
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
        string metadata_url = connection.url + $"/metadata/version_specification-{metadata_release_version_name}/metadata";

        var metadata_task = _couchDbHttpClient.ExecuteAsync("GET", metadata_url, null, connection.user_name, connection.user_value);
        var de_identified_list_task = _couchDbHttpClient.ExecuteAsync("GET", connection.url + "/metadata/de-identified-list", null, connection.user_name, connection.user_value);
        var case_template_task = read_case_template_json_async();

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

            string url = this.couchdb_url + $"/{this.prefix}mmrds/_all_docs?{string.Join("&", query_parameters)}";
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

    private async Task<int> get_total_case_document_count_async()
    {
        string total_rows_url = this.couchdb_url + $"/{this.prefix}mmrds/_all_docs?limit=0";
        string total_rows_response = await _couchDbHttpClient.ExecuteAsync("GET", total_rows_url, null, this.user_name, this.user_value);

        if(string.IsNullOrWhiteSpace(total_rows_response))
        {
            return 0;
        }

        var total_rows_payload = JObject.Parse(total_rows_response);
        int total_rows = total_rows_payload.Value<int?>("total_rows") ?? 0;

        string design_start_key = Uri.EscapeDataString("\"_design/\"");
        string design_end_key = Uri.EscapeDataString("\"_design0\"");
        string design_rows_url = this.couchdb_url + $"/{this.prefix}mmrds/_all_docs?include_docs=false&startkey={design_start_key}&endkey={design_end_key}";
        string design_rows_response = await _couchDbHttpClient.ExecuteAsync("GET", design_rows_url, null, this.user_name, this.user_value);

        if(string.IsNullOrWhiteSpace(design_rows_response))
        {
            return total_rows;
        }

        var design_rows_payload = JObject.Parse(design_rows_response);
        var design_rows = design_rows_payload["rows"] as JArray;
        int design_row_count = design_rows?.Count ?? 0;

        return Math.Max(0, total_rows - design_row_count);
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

        string response = await _couchDbHttpClient.ExecuteAsync("POST", this.couchdb_url + $"/{this.prefix}{database_name}/_bulk_docs", payload, this.user_name, this.user_value);

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
        int document_error_count = 0;
        int de_id_bulk_error_count = 0;
        int report_bulk_error_count = 0;
        int total_de_id_doc_count = 0;
        int total_report_doc_count = 0;
        int total_case_document_count = 0;
        c_document_sync_rebuild_context rebuild_context = null;

        System.Console.WriteLine($"[PopulateCDC] CDC rebuild settings: page size {page_size}, max parallelism {max_parallelism}.");
        ReportProgress("Phase 2 of 2: preparing CDC de-identified case database/report database rebuild from the CDC case database.");

        try
        {
            total_case_document_count = await get_total_case_document_count_async();
            rebuild_context = await load_rebuild_context_async();
            System.Console.WriteLine($"[PopulateCDC] CDC rebuild case count: {total_case_document_count}.");

            await _couchDbHttpClient.ExecuteAsync("DELETE", this.couchdb_url + $"/{this.prefix}de_id", null, this.user_name, this.user_value);
        }
        catch (Exception)
        {
        
        }
        

        try
        {
            await _couchDbHttpClient.ExecuteAsync("DELETE", this.couchdb_url + $"/{this.prefix}report", null, this.user_name, this.user_value);
        }
        catch (Exception)
        {
        
        }


        try
        {
            await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{this.prefix}de_id", null, this.user_name, this.user_value);
        }
        catch (Exception)
        {
        
        }

        try 
        {
            
            var case_design_sortable_path = mmria.common.SharedLibraries.MMRIAServices.Helper.MMRIAServicesHelper.ResolveDatabaseScriptPath("case_design_sortable.json");

            using (var  sr = new System.IO.StreamReader(case_design_sortable_path))
            {
                string result = await sr.ReadToEndAsync ();
                await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{this.prefix}de_id/_design/sortable", result, this.user_name, this.user_value);
            }


        } 
        catch (Exception) 
        {

        }



        try
        {
            await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{this.prefix}report", null, this.user_name, this.user_value);
        }
        catch (Exception)
        {
        
        }


        try
        {
            var Report_Opioid_Index = new Report_Opioid_Index_Struct();
            string index_json = Newtonsoft.Json.JsonConvert.SerializeObject (Report_Opioid_Index);
            await _couchDbHttpClient.ExecuteAsync("POST", this.couchdb_url + $"/{this.prefix}report/_index", index_json, this.user_name, this.user_value);
        }
        catch (Exception)
        {
        
        }

        try
        {
            var Report_PowerBI_Index = new Report_PowerBI_Index_Struct();
            
            string index_json = Newtonsoft.Json.JsonConvert.SerializeObject (Report_PowerBI_Index);
            await _couchDbHttpClient.ExecuteAsync("POST", this.couchdb_url + $"/{this.prefix}report/_index", index_json, this.user_name, this.user_value);
        }
        catch (Exception)
        {
        
        }

        try
        {
            var interactive_aggregate_report_path = mmria.common.SharedLibraries.MMRIAServices.Helper.MMRIAServicesHelper.ResolveDatabaseScriptPath("interactive-aggregate-report-view.json");

            using (var  sr = new System.IO.StreamReader(interactive_aggregate_report_path))
            {
                string result = await sr.ReadToEndAsync ();
                await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{this.prefix}report/_design/interactive_aggregate_report", result, this.user_name, this.user_value);
            }

        }
        catch (Exception)
        {
        
        }


        try
        {
            var data_summary_view_path = mmria.common.SharedLibraries.MMRIAServices.Helper.MMRIAServicesHelper.ResolveDatabaseScriptPath("data-summary-view.json");

            using (var  sr = new System.IO.StreamReader(data_summary_view_path))
            {
                string result = await sr.ReadToEndAsync ();
                await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{this.prefix}report/_design/data_summary_view_report", result, this.user_name, this.user_value);
            }

        }
        catch (Exception)
        {
        
        }

        string last_processed_case_id = null;

        for(var page = 0; ; page++)
        {
            try
            {
                var fetch_stopwatch = Stopwatch.StartNew();
                var rows = await get_case_batch_async(last_processed_case_id, page_size);
                fetch_stopwatch.Stop();

                if(rows.Count == 0)
                {
                    System.Console.WriteLine($"[PopulateCDC] No more CDC source cases after batch {page + 1}. Fetch time: {fetch_stopwatch.ElapsedMilliseconds} ms.");
                    break;
                }

                last_processed_case_id = rows[^1].id;

                System.Console.WriteLine($"[PopulateCDC] Starting CDC rebuild batch {page + 1} with {rows.Count} source cases.");

                var de_id_documents = new ConcurrentBag<string>();
                var report_documents = new ConcurrentBag<string>();
                var build_stopwatch = Stopwatch.StartNew();

                await Parallel.ForEachAsync(rows, new ParallelOptions { MaxDegreeOfParallelism = max_parallelism }, async (row, cancellation_token) =>
                {
                    try
                    {
                        var sync_document = new c_sync_document(row.id, row.document_json, connection, metadata_release_version_name, _couchDbHttpClient, rebuild_context: rebuild_context, skip_revision_lookup: true);
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
                    $"[PopulateCDC] CDC rebuild batch {page + 1}: fetched {rows.Count} cases in {fetch_stopwatch.ElapsedMilliseconds} ms, " +
                    $"built {de_id_documents.Count} de_id docs and {report_documents.Count} report docs in {build_stopwatch.ElapsedMilliseconds} ms, " +
                    $"wrote docs in {write_stopwatch.ElapsedMilliseconds} ms.");
                ReportProgress(
                    $"Phase 2 of 2: processed {processed_case_count} of {total_case_document_count} CDC case documents so far. " +
                    $"Generated {total_de_id_doc_count} de-identified case documents and {total_report_doc_count} report documents. " +
                    $"Build errors: {document_error_count}. de-identified case database bulk errors: {de_id_bulk_error_count}. report database bulk errors: {report_bulk_error_count}.");
            }
            catch (Exception ex)
            {
                System.Console.Write($"error running c_docment_sync_all\n{ex}");
                break;
            }
        }

        System.Console.WriteLine(
            $"[PopulateCDC] CDC rebuild processed {processed_case_count} mmrds docs, generated {total_de_id_doc_count} de_id docs and {total_report_doc_count} report docs. " +
            $"Document build errors: {document_error_count}. de_id bulk errors: {de_id_bulk_error_count}. report bulk errors: {report_bulk_error_count}.");
        ReportProgress(
            $"Phase 2 of 2 complete. Processed {processed_case_count} of {total_case_document_count} CDC case documents, generated {total_de_id_doc_count} de-identified case documents and {total_report_doc_count} report documents. " +
            $"Build errors: {document_error_count}. de-identified case database bulk errors: {de_id_bulk_error_count}. report database bulk errors: {report_bulk_error_count}.");

    }
}

