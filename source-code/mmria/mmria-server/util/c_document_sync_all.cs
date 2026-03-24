#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using mmria.server.util;
using Newtonsoft.Json.Linq;

namespace mmria.server.utils;

public sealed class c_document_sync_all
{
    private static readonly SemaphoreSlim s_startup_rebuild_gate = new(1, 1);
    private const string StartupRebuildDatabaseName = "db_rebuild";
    private const string LegacyStartupRebuildCheckpointDocumentId = "startup-rebuild-status";
    private const string StartupRunSummaryDocumentId = "startup-run-summary";
    private const string StartupRebuildSecurityPayload = "{\"admins\":{\"names\":[],\"roles\":[\"form_designer\"]},\"members\":{\"names\":[],\"roles\":[\"abstractor\",\"data_analyst\",\"timer\"]}}";

    private sealed class case_batch_document
    {
        public string id { get; init; }
        public string document_json { get; init; }
    }

    private sealed class startup_rebuild_tenant_summary
    {
        public string host_prefix { get; set; }
        public string couchdb_url { get; set; }
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

    private sealed class startup_run_summary
    {
        public string _id { get; set; } = StartupRunSummaryDocumentId;
        public string _rev { get; set; }
        public string status { get; set; }
        public string metadata_version { get; set; }
        public string summary_host_prefix { get; set; }
        public List<string> configured_tenants { get; set; } = new List<string>();
        public Dictionary<string, startup_rebuild_tenant_summary> tenant_statuses { get; set; } = new Dictionary<string, startup_rebuild_tenant_summary>(StringComparer.OrdinalIgnoreCase);
        public int total_tenant_count { get; set; }
        public int completed_tenant_count { get; set; }
        public int paused_tenant_count { get; set; }
        public int running_tenant_count { get; set; }
        public int pending_tenant_count { get; set; }
        public int total_processed_case_count { get; set; }
        public int total_skipped_case_count { get; set; }
        public int total_document_error_count { get; set; }
        public int total_de_id_bulk_error_count { get; set; }
        public int total_report_bulk_error_count { get; set; }
        public int total_de_id_doc_count { get; set; }
        public int total_report_doc_count { get; set; }
        public string started_utc { get; set; }
        public string last_updated_utc { get; set; }
        public string completed_utc { get; set; }
        public string last_error { get; set; }
    }

    private sealed class batch_processing_result
    {
        public int document_error_count { get; set; }
        public int de_id_doc_count { get; set; }
        public int report_doc_count { get; set; }
        public int de_id_bulk_error_count { get; set; }
        public int report_bulk_error_count { get; set; }
        public long build_elapsed_ms { get; set; }
        public long write_elapsed_ms { get; set; }
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
    private readonly mmria.server.util.TenantRebuildCoordinator.TenantRebuildLease _tenant_rebuild_lease;
    private readonly string _rebuild_source;
    private readonly string _rebuild_mode;

    public c_document_sync_all 
    (
        string p_couchdb_url, 
        string p_user_name, 
        string p_value,
        string p_metadata_version,
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.couchdb.OverridableConfiguration configuration = null,
        string host_prefix = null,
        mmria.server.util.TenantRebuildCoordinator.TenantRebuildLease tenant_rebuild_lease = null,
        string rebuild_source = "startup",
        string rebuild_mode = null
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
        _tenant_rebuild_lease = tenant_rebuild_lease;
        _rebuild_source = rebuild_source;
        _rebuild_mode = rebuild_mode;
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

    private string get_effective_host_prefix()
    {
        if(!string.IsNullOrWhiteSpace(_host_prefix))
        {
            return _host_prefix.Trim();
        }

        if(!string.IsNullOrWhiteSpace(db_config?.prefix))
        {
            return db_config.prefix.Trim();
        }

        return "shared";
    }

    private int get_rebuild_setting(string key, int default_value, int minimum_value, int? maximum_value = null)
    {
        int configured_value = _configuration?.GetInteger(key, get_effective_host_prefix()) ?? default_value;
        configured_value = Math.Max(minimum_value, configured_value);

        if(maximum_value.HasValue)
        {
            configured_value = Math.Min(maximum_value.Value, configured_value);
        }

        return configured_value;
    }

    private string get_startup_rebuild_mode()
    {
        string configured_mode = _configuration?.GetString("startup_rebuild_mode", get_effective_host_prefix());
        return string.Equals(configured_mode?.Trim(), "compatibility", StringComparison.OrdinalIgnoreCase)
            ? "compatibility"
            : "bulk";
    }

    private static bool is_transient_bulk_write_exception(Exception ex)
    {
        if(ex == null)
        {
            return false;
        }

        if(ex is HttpRequestException || ex is TaskCanceledException || ex is TimeoutException)
        {
            return true;
        }

        return is_transient_bulk_write_exception(ex.InnerException);
    }

    private List<string> get_configured_tenants()
    {
        string current_host_prefix = get_effective_host_prefix();
        string summary_host_prefix = _configuration?.GetString("multi_tenant_re_build_src", current_host_prefix);

        if(string.IsNullOrWhiteSpace(summary_host_prefix))
        {
            return new List<string> { current_host_prefix };
        }

        string configured_tenants = _configuration?.GetString("multi_tenant_jurisdictions", current_host_prefix);
        if(string.IsNullOrWhiteSpace(configured_tenants))
        {
            return new List<string> { current_host_prefix };
        }

        return configured_tenants
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string get_summary_host_prefix()
    {
        string current_host_prefix = get_effective_host_prefix();
        string summary_host_prefix = _configuration?.GetString("multi_tenant_re_build_src", current_host_prefix);

        if(string.IsNullOrWhiteSpace(summary_host_prefix))
        {
            return current_host_prefix;
        }

        return summary_host_prefix.Trim();
    }

    private string get_rebuild_database_url(string base_couchdb_url)
    {
        return base_couchdb_url + $"/{db_config.prefix}{StartupRebuildDatabaseName}";
    }

    private string get_legacy_startup_rebuild_checkpoint_url()
    {
        return get_rebuild_database_url(this.couchdb_url) + $"/{LegacyStartupRebuildCheckpointDocumentId}";
    }

    private string get_startup_run_summary_base_url()
    {
        string current_host_prefix = get_effective_host_prefix();
        string summary_host_prefix = get_summary_host_prefix();

        if(string.Equals(summary_host_prefix, current_host_prefix, StringComparison.OrdinalIgnoreCase))
        {
            return this.couchdb_url;
        }

        string tenant_url_template = _configuration?.GetString("multi_tenant_shared_config_id_template_couchdb_url", current_host_prefix);
        if(string.IsNullOrWhiteSpace(tenant_url_template))
        {
            return null;
        }

        return tenant_url_template.Replace("{replace}", summary_host_prefix, StringComparison.OrdinalIgnoreCase);
    }

    private string get_startup_run_summary_url()
    {
        string summary_base_url = get_startup_run_summary_base_url();
        if(string.IsNullOrWhiteSpace(summary_base_url))
        {
            return null;
        }

        return get_rebuild_database_url(summary_base_url) + $"/{StartupRunSummaryDocumentId}";
    }

    private async Task ensure_rebuild_database_exists_async(string base_couchdb_url)
    {
        if(string.IsNullOrWhiteSpace(base_couchdb_url))
        {
            return;
        }

        string rebuild_database_url = get_rebuild_database_url(base_couchdb_url);
        if(await url_endpoint_exists_async(rebuild_database_url))
        {
            return;
        }

        try
        {
            await _couchDbHttpClient.ExecuteAsync("PUT", rebuild_database_url, null, this.user_name, this.user_value);
        }
        catch (Exception)
        {
            // Another setup path may have created the database first.
        }

        try
        {
            await _couchDbHttpClient.ExecuteAsync("PUT", rebuild_database_url + "/_security", StartupRebuildSecurityPayload, this.user_name, this.user_value);
        }
        catch (Exception security_ex)
        {
            System.Console.WriteLine($"Failed to configure {db_config.prefix}{StartupRebuildDatabaseName}/_security at '{base_couchdb_url}': {security_ex.Message}");
        }
    }

    private async Task delete_legacy_startup_rebuild_checkpoint_async()
    {
        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            get_legacy_startup_rebuild_checkpoint_url(),
            null,
            this.user_name,
            this.user_value
        );

        if(string.IsNullOrWhiteSpace(response))
        {
            return;
        }

        var payload = JObject.Parse(response);
        if(string.Equals(payload.Value<string>("error"), "not_found", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string rev = payload.Value<string>("_rev");
        if(string.IsNullOrWhiteSpace(rev))
        {
            return;
        }

        await _couchDbHttpClient.ExecuteAsync(
            "DELETE",
            get_legacy_startup_rebuild_checkpoint_url() + $"?rev={Uri.EscapeDataString(rev)}",
            null,
            this.user_name,
            this.user_value
        );
    }

    private async Task<startup_run_summary> try_get_startup_run_summary_async()
    {
        string summary_url = get_startup_run_summary_url();
        if(string.IsNullOrWhiteSpace(summary_url))
        {
            return null;
        }

        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            summary_url,
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

        var summary = payload.ToObject<startup_run_summary>();
        if(summary != null && string.IsNullOrWhiteSpace(summary._id))
        {
            summary._id = StartupRunSummaryDocumentId;
        }

        return normalize_startup_run_summary(summary);
    }

    private static startup_run_summary normalize_startup_run_summary(startup_run_summary summary)
    {
        summary ??= new startup_run_summary();
        summary.configured_tenants ??= new List<string>();
        summary.tenant_statuses ??= new Dictionary<string, startup_rebuild_tenant_summary>(StringComparer.OrdinalIgnoreCase);
        if(string.IsNullOrWhiteSpace(summary._id))
        {
            summary._id = StartupRunSummaryDocumentId;
        }

        return summary;
    }

    private startup_run_summary get_cached_startup_run_summary(string summary_host_prefix)
    {
        if(!StartupRunSummaryCache.TryGet(summary_host_prefix, out var cached_summary_payload))
        {
            return null;
        }

        return normalize_startup_run_summary(cached_summary_payload.ToObject<startup_run_summary>());
    }

    private void set_cached_startup_run_summary(string summary_host_prefix, startup_run_summary summary)
    {
        if(string.IsNullOrWhiteSpace(summary_host_prefix) || summary == null)
        {
            return;
        }

        StartupRunSummaryCache.Set(summary_host_prefix, JObject.FromObject(normalize_startup_run_summary(summary)));
    }

    private startup_run_summary create_startup_run_summary(List<string> configured_tenants, string summary_host_prefix)
    {
        var summary = new startup_run_summary
        {
            status = "running",
            metadata_version = metadata_version,
            summary_host_prefix = summary_host_prefix,
            configured_tenants = configured_tenants.ToList(),
            tenant_statuses = new Dictionary<string, startup_rebuild_tenant_summary>(StringComparer.OrdinalIgnoreCase),
            started_utc = DateTime.UtcNow.ToString("o"),
            completed_utc = null,
            last_error = null
        };

        foreach(string tenant in configured_tenants)
        {
            summary.tenant_statuses[tenant] = new startup_rebuild_tenant_summary
            {
                host_prefix = tenant,
                status = "pending"
            };
        }

        return summary;
    }

    private void update_run_summary_totals(startup_run_summary summary, List<string> configured_tenants)
    {
        configured_tenants ??= new List<string>();
        summary.configured_tenants = configured_tenants.ToList();
        summary.total_tenant_count = configured_tenants.Count;
        summary.completed_tenant_count = 0;
        summary.paused_tenant_count = 0;
        summary.running_tenant_count = 0;
        summary.pending_tenant_count = 0;
        summary.total_processed_case_count = 0;
        summary.total_skipped_case_count = 0;
        summary.total_document_error_count = 0;
        summary.total_de_id_bulk_error_count = 0;
        summary.total_report_bulk_error_count = 0;
        summary.total_de_id_doc_count = 0;
        summary.total_report_doc_count = 0;

        foreach(string tenant in configured_tenants)
        {
            if(!summary.tenant_statuses.TryGetValue(tenant, out var tenant_summary) || tenant_summary == null)
            {
                summary.pending_tenant_count++;
                continue;
            }

            summary.total_processed_case_count += tenant_summary.processed_case_count;
            summary.total_skipped_case_count += tenant_summary.skipped_case_count;
            summary.total_document_error_count += tenant_summary.document_error_count;
            summary.total_de_id_bulk_error_count += tenant_summary.de_id_bulk_error_count;
            summary.total_report_bulk_error_count += tenant_summary.report_bulk_error_count;
            summary.total_de_id_doc_count += tenant_summary.total_de_id_doc_count;
            summary.total_report_doc_count += tenant_summary.total_report_doc_count;

            switch(tenant_summary.status?.ToLowerInvariant())
            {
                case "completed":
                    summary.completed_tenant_count++;
                    break;
                case "paused":
                    summary.paused_tenant_count++;
                    break;
                case "running":
                    summary.running_tenant_count++;
                    break;
                default:
                    summary.pending_tenant_count++;
                    break;
            }
        }

        summary.last_updated_utc = DateTime.UtcNow.ToString("o");
        summary.last_error = summary.tenant_statuses.Values
            .Where(item => item != null && !string.IsNullOrWhiteSpace(item.last_error))
            .Select(item => item.last_error)
            .FirstOrDefault();

        if(summary.total_tenant_count > 0 && summary.completed_tenant_count == summary.total_tenant_count)
        {
            summary.status = "completed";
            summary.completed_utc ??= DateTime.UtcNow.ToString("o");
        }
        else if(summary.running_tenant_count > 0)
        {
            summary.status = "running";
            summary.completed_utc = null;
        }
        else if(summary.paused_tenant_count > 0)
        {
            summary.status = "incomplete";
            summary.completed_utc = null;
        }
        else
        {
            summary.status = "running";
            summary.completed_utc = null;
        }
    }

    private async Task save_startup_run_summary_async(startup_run_summary summary)
    {
        if(summary == null)
        {
            return;
        }

        string summary_base_url = get_startup_run_summary_base_url();
        string summary_url = get_startup_run_summary_url();
        if(string.IsNullOrWhiteSpace(summary_base_url) || string.IsNullOrWhiteSpace(summary_url))
        {
            return;
        }

        await ensure_rebuild_database_exists_async(summary_base_url);

        summary._id = StartupRunSummaryDocumentId;
        summary.last_updated_utc = DateTime.UtcNow.ToString("o");

        for(int attempt = 0; attempt < 2; attempt++)
        {
            string payload = Newtonsoft.Json.JsonConvert.SerializeObject(
                summary,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                }
            );

            string response = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                summary_url,
                payload,
                this.user_name,
                this.user_value
            );

            if(!string.IsNullOrWhiteSpace(response))
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(response);
                if(result?.ok == true)
                {
                    summary._rev = result.rev;
                    set_cached_startup_run_summary(summary.summary_host_prefix, summary);
                    return;
                }

                var response_payload = JObject.Parse(response);
                if(
                    attempt == 0 &&
                    string.Equals(response_payload.Value<string>("error"), "conflict", StringComparison.OrdinalIgnoreCase)
                )
                {
                    var latest_summary = await try_get_startup_run_summary_async();
                    summary._rev = latest_summary?._rev;
                    continue;
                }
            }

            System.Console.WriteLine(
                $"Failed to save startup run summary for '{summary_base_url}'. " +
                $"Response: {response ?? "<null>"}"
            );
            break;
        }
    }

    private async Task sync_startup_run_summary_async(startup_rebuild_tenant_summary tenant_state, bool force_reset, bool persist_to_database)
    {
        if(tenant_state == null)
        {
            return;
        }

        string summary_base_url = get_startup_run_summary_base_url();
        if(string.IsNullOrWhiteSpace(summary_base_url))
        {
            return;
        }

        List<string> configured_tenants = get_configured_tenants()
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();
        string summary_host_prefix = get_summary_host_prefix();
        string current_host_prefix = get_effective_host_prefix();

        await ensure_rebuild_database_exists_async(summary_base_url);

        var summary = force_reset
            ? null
            : get_cached_startup_run_summary(summary_host_prefix);

        summary ??= await try_get_startup_run_summary_async();

        if(
            force_reset ||
            summary == null ||
            !string.Equals(summary.metadata_version, metadata_version, StringComparison.OrdinalIgnoreCase)
        )
        {
            summary = create_startup_run_summary(configured_tenants, summary_host_prefix);
        }

        summary.summary_host_prefix = summary_host_prefix;
        summary.metadata_version = metadata_version;

        var configured_tenant_set = new HashSet<string>(configured_tenants, StringComparer.OrdinalIgnoreCase);
        foreach(string stale_tenant in summary.tenant_statuses.Keys
            .Where(item => !configured_tenant_set.Contains(item))
            .ToList())
        {
            summary.tenant_statuses.Remove(stale_tenant);
        }

        foreach(string tenant in configured_tenants)
        {
            if(!summary.tenant_statuses.ContainsKey(tenant))
            {
                summary.tenant_statuses[tenant] = new startup_rebuild_tenant_summary
                {
                    host_prefix = tenant,
                    status = "pending"
                };
            }
        }

        if(!summary.tenant_statuses.TryGetValue(current_host_prefix, out var tenant_summary) || tenant_summary == null)
        {
            tenant_summary = new startup_rebuild_tenant_summary
            {
                host_prefix = current_host_prefix
            };
            summary.tenant_statuses[current_host_prefix] = tenant_summary;
        }

        tenant_summary.host_prefix = current_host_prefix;
        tenant_summary.couchdb_url = this.couchdb_url;
        tenant_summary.status = tenant_state.status;
        tenant_summary.metadata_version = tenant_state.metadata_version;
        tenant_summary.last_processed_id = tenant_state.last_processed_id;
        tenant_summary.completed_batch_count = tenant_state.completed_batch_count;
        tenant_summary.processed_case_count = tenant_state.processed_case_count;
        tenant_summary.skipped_case_count = tenant_state.skipped_case_count;
        tenant_summary.document_error_count = tenant_state.document_error_count;
        tenant_summary.de_id_bulk_error_count = tenant_state.de_id_bulk_error_count;
        tenant_summary.report_bulk_error_count = tenant_state.report_bulk_error_count;
        tenant_summary.total_de_id_doc_count = tenant_state.total_de_id_doc_count;
        tenant_summary.total_report_doc_count = tenant_state.total_report_doc_count;
        tenant_summary.started_utc = tenant_state.started_utc;
        tenant_summary.last_updated_utc = tenant_state.last_updated_utc;
        tenant_summary.completed_utc = tenant_state.completed_utc;
        tenant_summary.last_error = tenant_state.last_error;

        update_run_summary_totals(summary, configured_tenants);
        set_cached_startup_run_summary(summary_host_prefix, summary);

        if(persist_to_database)
        {
            await save_startup_run_summary_async(summary);
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

    private static void remove_document_revisions(JArray docs)
    {
        if(docs == null)
        {
            return;
        }

        foreach(var doc in docs.OfType<JObject>())
        {
            doc.Remove("_rev");
        }
    }

    private async Task<(int success_count, int error_count)> bulk_write_chunk_async(string database_name, List<string> document_json_list, bool hydrate_existing_revisions)
    {
        if(document_json_list == null || document_json_list.Count == 0)
        {
            return (0, 0);
        }

        var docs = new JArray(document_json_list.Select(JObject.Parse));
        if(hydrate_existing_revisions)
        {
            await hydrate_existing_revisions_async(database_name, docs);
        }
        else
        {
            remove_document_revisions(docs);
        }

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

    private async Task<(int success_count, int error_count)> bulk_write_async(
        string database_name,
        List<string> document_json_list,
        int chunk_size,
        int retry_count,
        int retry_delay_ms,
        bool hydrate_existing_revisions)
    {
        if(document_json_list == null || document_json_list.Count == 0)
        {
            return (0, 0);
        }

        int effective_chunk_size =
            chunk_size <= 0
            ? document_json_list.Count
            : Math.Max(1, chunk_size);

        int success_count = 0;
        int error_count = 0;

        for(int offset = 0; offset < document_json_list.Count; offset += effective_chunk_size)
        {
            var chunk = document_json_list
                .Skip(offset)
                .Take(effective_chunk_size)
                .ToList();

            for(int attempt = 0; ; attempt++)
            {
                try
                {
                    var chunk_result = await bulk_write_chunk_async(database_name, chunk, hydrate_existing_revisions);
                    success_count += chunk_result.success_count;
                    error_count += chunk_result.error_count;
                    break;
                }
                catch (Exception ex)
                {
                    if(!is_transient_bulk_write_exception(ex) || attempt >= retry_count)
                    {
                        throw;
                    }

                    int delay_ms = Math.Max(0, retry_delay_ms) * (attempt + 1);
                    System.Console.WriteLine(
                        $"Transient {database_name} bulk write failure for '{db_config.url}' " +
                        $"on chunk starting at document index {offset}. " +
                        $"Retry {attempt + 1} of {retry_count} in {delay_ms} ms.\n{ex.Message}");

                    if(delay_ms > 0)
                    {
                        await Task.Delay(delay_ms);
                    }
                }
            }
        }

        return (success_count, error_count);
    }

    private async Task<bool> put_document_async(string database_name, string document_json, int retry_count, int retry_delay_ms)
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
            System.Console.WriteLine($"Unable to parse {database_name} document for direct write: {parse_ex.Message}");
            return false;
        }

        string document_id = document.Value<string>("_id");
        if(string.IsNullOrWhiteSpace(document_id))
        {
            System.Console.WriteLine($"Unable to direct-write {database_name} document because _id is missing.");
            return false;
        }

        document.Remove("_rev");

        string url = this.couchdb_url + $"/{db_config.prefix}{database_name}/{Uri.EscapeDataString(document_id)}";
        string payload = document.ToString(Newtonsoft.Json.Formatting.None);

        for(int attempt = 0; ; attempt++)
        {
            try
            {
                string response = await _couchDbHttpClient.ExecuteAsync(
                    "PUT",
                    url,
                    payload,
                    this.user_name,
                    this.user_value);

                if(string.IsNullOrWhiteSpace(response))
                {
                    System.Console.WriteLine($"Direct {database_name} write returned an empty response for '{document_id}'.");
                    return false;
                }

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(response);
                if(result?.ok == true)
                {
                    return true;
                }

                System.Console.WriteLine($"Direct {database_name} write received unexpected response: {response}");
                return false;
            }
            catch (Exception ex)
            {
                if(!is_transient_bulk_write_exception(ex) || attempt >= retry_count)
                {
                    throw;
                }

                int delay_ms = Math.Max(0, retry_delay_ms) * (attempt + 1);
                System.Console.WriteLine(
                    $"Transient {database_name} direct write failure for '{db_config.url}' " +
                    $"on document '{document_id}'. Retry {attempt + 1} of {retry_count} in {delay_ms} ms.\n{ex.Message}");

                if(delay_ms > 0)
                {
                    await Task.Delay(delay_ms);
                }
            }
        }
    }

    private async Task<(int success_count, int error_count)> write_documents_individually_async(
        string database_name,
        IEnumerable<string> document_json_list,
        int retry_count,
        int retry_delay_ms)
    {
        int success_count = 0;
        int error_count = 0;

        foreach(string document_json in document_json_list ?? Enumerable.Empty<string>())
        {
            bool success = await put_document_async(database_name, document_json, retry_count, retry_delay_ms);
            if(success)
            {
                success_count++;
            }
            else
            {
                error_count++;
            }
        }

        return (success_count, error_count);
    }

    private async Task<batch_processing_result> process_batch_bulk_async(
        List<case_batch_document> rows,
        c_document_sync_rebuild_context rebuild_context,
        int max_parallelism,
        int bulk_doc_chunk_size,
        int bulk_write_retry_count,
        int bulk_write_retry_delay_ms,
        bool hydrate_target_revisions)
    {
        var result = new batch_processing_result();
        var de_id_documents = new ConcurrentBag<string>();
        var report_documents = new ConcurrentBag<string>();
        int document_error_count = 0;

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
                Interlocked.Increment(ref document_error_count);
                System.Console.WriteLine($"error running c_docment_sync_all.document {row?.id}\n{document_ex}");
            }
        });
        build_stopwatch.Stop();

        var write_stopwatch = Stopwatch.StartNew();
        var de_id_write_result = await bulk_write_async(
            "de_id",
            de_id_documents.ToList(),
            bulk_doc_chunk_size,
            bulk_write_retry_count,
            bulk_write_retry_delay_ms,
            hydrate_target_revisions);
        var report_write_result = await bulk_write_async(
            "report",
            report_documents.ToList(),
            bulk_doc_chunk_size,
            bulk_write_retry_count,
            bulk_write_retry_delay_ms,
            hydrate_target_revisions);
        write_stopwatch.Stop();

        result.document_error_count = document_error_count;
        result.de_id_doc_count = de_id_documents.Count;
        result.report_doc_count = report_documents.Count;
        result.de_id_bulk_error_count = de_id_write_result.error_count;
        result.report_bulk_error_count = report_write_result.error_count;
        result.build_elapsed_ms = build_stopwatch.ElapsedMilliseconds;
        result.write_elapsed_ms = write_stopwatch.ElapsedMilliseconds;
        return result;
    }

    private async Task<batch_processing_result> process_batch_compatibility_async(
        List<case_batch_document> rows,
        c_document_sync_rebuild_context rebuild_context,
        int bulk_write_retry_count,
        int bulk_write_retry_delay_ms)
    {
        var result = new batch_processing_result();

        foreach(var row in rows)
        {
            try
            {
                var build_stopwatch = Stopwatch.StartNew();
                var sync_document = new c_sync_document(row.id, row.document_json, "PUT", metadata_version, db_config, _couchDbHttpClient, _configuration, _host_prefix, rebuild_context, skip_revision_lookup: true);
                var build_result = await sync_document.build_documents_async();
                build_stopwatch.Stop();

                result.build_elapsed_ms += build_stopwatch.ElapsedMilliseconds;

                bool has_de_id_document = !string.IsNullOrWhiteSpace(build_result.de_identified_json);
                int report_document_count = build_result.report_document_json_list?.Count ?? 0;

                var write_stopwatch = Stopwatch.StartNew();

                if(has_de_id_document)
                {
                    var de_id_write_result = await write_documents_individually_async(
                        "de_id",
                        new List<string> { build_result.de_identified_json },
                        retry_count: bulk_write_retry_count,
                        retry_delay_ms: bulk_write_retry_delay_ms);
                    result.de_id_bulk_error_count += de_id_write_result.error_count;
                    result.de_id_doc_count++;
                }

                if(report_document_count > 0)
                {
                    var report_write_result = await write_documents_individually_async(
                        "report",
                        build_result.report_document_json_list,
                        retry_count: bulk_write_retry_count,
                        retry_delay_ms: bulk_write_retry_delay_ms);
                    result.report_bulk_error_count += report_write_result.error_count;
                    result.report_doc_count += report_document_count;
                }

                write_stopwatch.Stop();
                result.write_elapsed_ms += write_stopwatch.ElapsedMilliseconds;
            }
            catch (Exception document_ex)
            {
                result.document_error_count++;
                System.Console.WriteLine($"error running c_docment_sync_all.document {row?.id}\n{document_ex}");
            }
        }

        return result;
    }

    public async Task executeAsync ()
    {
        mmria.server.util.TenantRebuildCoordinator.TenantRebuildLease tenant_rebuild_lease = _tenant_rebuild_lease;
        if(tenant_rebuild_lease == null)
        {
            if(
                !mmria.server.util.TenantRebuildCoordinator.TryAcquire(
                    get_effective_host_prefix(),
                    _rebuild_source,
                    _rebuild_mode,
                    "queued",
                    out tenant_rebuild_lease,
                    out var existing_reservation
                )
            )
            {
                System.Console.WriteLine(
                    $"Skipping startup rebuild for '{db_config.url}' because tenant '{get_effective_host_prefix()}' " +
                    $"already has a rebuild started by '{existing_reservation?.source ?? "unknown"}' " +
                    $"in status '{existing_reservation?.status ?? "unknown"}'.");
                return;
            }
        }

        int page_size = get_rebuild_setting("startup_rebuild_page_size", 25, 1);
        int max_parallelism = get_rebuild_setting(
            "startup_rebuild_max_parallelism",
            Math.Max(1, Math.Min(Environment.ProcessorCount, 2)),
            1);
        string startup_rebuild_mode = get_startup_rebuild_mode();
        bool use_compatibility_mode = string.Equals(startup_rebuild_mode, "compatibility", StringComparison.OrdinalIgnoreCase);
        int bulk_doc_chunk_size = get_rebuild_setting("startup_rebuild_bulk_doc_chunk_size", 0, 0);
        int batch_delay_ms = get_rebuild_setting("startup_rebuild_batch_delay_ms", 0, 0);
        int bulk_write_retry_count = get_rebuild_setting("startup_rebuild_bulk_write_retry_count", 2, 0);
        int bulk_write_retry_delay_ms = get_rebuild_setting("startup_rebuild_bulk_write_retry_delay_ms", 1000, 0);
        int progress_persist_every_batches = get_rebuild_setting("startup_rebuild_progress_persist_every_batches", 10, 1);
        int processed_case_count = 0;
        int skipped_case_count = 0;
        int document_error_count = 0;
        int de_id_bulk_error_count = 0;
        int report_bulk_error_count = 0;
        int total_de_id_doc_count = 0;
        int total_report_doc_count = 0;
        int completed_batch_count = 0;
        string start_after_id = null;
        bool rebuild_completed_successfully = false;
        var tenant_rebuild_state = new startup_rebuild_tenant_summary
        {
            host_prefix = get_effective_host_prefix(),
            couchdb_url = this.couchdb_url,
            status = "running",
            metadata_version = metadata_version,
            started_utc = DateTime.UtcNow.ToString("o"),
            completed_utc = null,
            last_error = null
        };

        var slot_wait_stopwatch = Stopwatch.StartNew();

        if(use_compatibility_mode)
        {
            max_parallelism = 1;
        }

        System.Console.WriteLine();
        System.Console.WriteLine("========== c_document_sync_all.executeAsync() ==========");
        System.Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        System.Console.WriteLine($"Tenant prefix: '{db_config.prefix}'");
        System.Console.WriteLine($"CouchDB URL: {this.couchdb_url}");
        System.Console.WriteLine($"Startup rebuild mode: {startup_rebuild_mode}");
        System.Console.WriteLine($"Page size: {page_size}");
        System.Console.WriteLine($"Max parallelism: {max_parallelism}");
        System.Console.WriteLine($"Bulk doc chunk size: {(bulk_doc_chunk_size <= 0 ? "disabled" : bulk_doc_chunk_size)}");
        System.Console.WriteLine($"Batch delay: {batch_delay_ms} ms");
        System.Console.WriteLine($"Bulk write retries: {bulk_write_retry_count}");
        System.Console.WriteLine($"Bulk write retry delay: {bulk_write_retry_delay_ms} ms");
        System.Console.WriteLine($"Progress persistence cadence: every {progress_persist_every_batches} batch(es)");
        System.Console.WriteLine("=======================================================");
        System.Console.WriteLine();

        void update_rebuild_state(string status, string last_error, bool is_completed)
        {
            tenant_rebuild_state.status = status;
            tenant_rebuild_state.last_processed_id = start_after_id;
            tenant_rebuild_state.completed_batch_count = completed_batch_count;
            tenant_rebuild_state.processed_case_count = processed_case_count;
            tenant_rebuild_state.skipped_case_count = skipped_case_count;
            tenant_rebuild_state.document_error_count = document_error_count;
            tenant_rebuild_state.de_id_bulk_error_count = de_id_bulk_error_count;
            tenant_rebuild_state.report_bulk_error_count = report_bulk_error_count;
            tenant_rebuild_state.total_de_id_doc_count = total_de_id_doc_count;
            tenant_rebuild_state.total_report_doc_count = total_report_doc_count;
            tenant_rebuild_state.last_updated_utc = DateTime.UtcNow.ToString("o");
            tenant_rebuild_state.completed_utc = is_completed ? DateTime.UtcNow.ToString("o") : null;
            tenant_rebuild_state.last_error = last_error;
        }

        async Task persist_startup_run_summary_async(bool force_reset, string context, bool persist_to_database)
        {
            try
            {
                await sync_startup_run_summary_async(tenant_rebuild_state, force_reset, persist_to_database);
            }
            catch (Exception summary_ex)
            {
                System.Console.WriteLine($"Failed to persist {context} startup run summary: {summary_ex}");
            }
        }

        bool reset_startup_run_summary =
            string.Equals(_rebuild_source, "startup", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(get_effective_host_prefix(), get_configured_tenants().FirstOrDefault(), StringComparison.OrdinalIgnoreCase);

        System.Console.WriteLine($"Starting a fresh startup rebuild for '{db_config.url}'.");

        System.Console.WriteLine($"Waiting for startup rebuild slot for '{db_config.url}'.");
        await s_startup_rebuild_gate.WaitAsync();
        slot_wait_stopwatch.Stop();
        System.Console.WriteLine($"Acquired startup rebuild slot for '{db_config.url}'.");
        tenant_rebuild_lease.UpdateStatus("running");

        var active_rebuild_stopwatch = Stopwatch.StartNew();

        try
        {
            try
            {
                await delete_legacy_startup_rebuild_checkpoint_async();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Unable to delete legacy startup rebuild checkpoint before rebuild execution: {ex.Message}");
            }

            update_rebuild_state("running", null, false);
            await persist_startup_run_summary_async(reset_startup_run_summary, "initial", persist_to_database: true);

            await ensure_target_databases_async(resetExistingDatabases: true);

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

                    batch_processing_result batch_result = use_compatibility_mode
                        ? await process_batch_compatibility_async(
                            rows,
                            rebuild_context,
                            bulk_write_retry_count,
                            bulk_write_retry_delay_ms)
                        : await process_batch_bulk_async(
                            rows,
                            rebuild_context,
                            max_parallelism,
                            bulk_doc_chunk_size,
                            bulk_write_retry_count,
                            bulk_write_retry_delay_ms,
                            hydrate_target_revisions: false);

                    processed_case_count += rows.Count;
                    document_error_count += batch_result.document_error_count;
                    total_de_id_doc_count += batch_result.de_id_doc_count;
                    total_report_doc_count += batch_result.report_doc_count;
                    de_id_bulk_error_count += batch_result.de_id_bulk_error_count;
                    report_bulk_error_count += batch_result.report_bulk_error_count;
                    completed_batch_count++;
                    start_after_id = rows.Last().id;

                    update_rebuild_state("running", null, false);

                    bool should_persist_progress = completed_batch_count % progress_persist_every_batches == 0;
                    await persist_startup_run_summary_async(
                        force_reset: false,
                        context: should_persist_progress ? $"post-batch {batch_number}" : $"cached post-batch {batch_number}",
                        persist_to_database: should_persist_progress);

                    System.Console.WriteLine(
                        $"Batch {batch_number}: fetched {rows.Count} cases in {fetch_stopwatch.ElapsedMilliseconds} ms, " +
                        $"built {batch_result.de_id_doc_count} de_id docs and {batch_result.report_doc_count} report docs in {batch_result.build_elapsed_ms} ms, " +
                        $"wrote docs in {batch_result.write_elapsed_ms} ms.");

                    if(batch_delay_ms > 0)
                    {
                        await Task.Delay(batch_delay_ms);
                    }
                }
                catch (Exception ex)
                {
                    update_rebuild_state("paused", ex.ToString(), false);
                    await persist_startup_run_summary_async(force_reset: false, context: "paused", persist_to_database: true);

                    System.Console.Write($"error running c_docment_sync_all\n{ex}");
                    break;
                }
            }

            active_rebuild_stopwatch.Stop();
            update_rebuild_state(
                rebuild_completed_successfully ? "completed" : "paused",
                rebuild_completed_successfully ? null : tenant_rebuild_state.last_error,
                rebuild_completed_successfully);

            await persist_startup_run_summary_async(force_reset: false, context: "final", persist_to_database: true);

            System.Console.WriteLine();
            System.Console.WriteLine(
                $"Startup rebuild {(rebuild_completed_successfully ? "complete" : "paused")} in {active_rebuild_stopwatch.Elapsed.TotalSeconds:F1} seconds " +
                $"after waiting {slot_wait_stopwatch.Elapsed.TotalSeconds:F1} seconds for the startup rebuild slot. " +
                $"Processed {processed_case_count} cases, generated {total_de_id_doc_count} de_id docs and {total_report_doc_count} report docs. " +
                $"Document build errors: {document_error_count}. de_id bulk errors: {de_id_bulk_error_count}. report bulk errors: {report_bulk_error_count}. Skipped cases: {skipped_case_count}.");
            System.Console.WriteLine();
        }
        finally
        {
            s_startup_rebuild_gate.Release();
            System.Console.WriteLine($"Released startup rebuild slot for '{db_config.url}'.");
            tenant_rebuild_lease.Dispose();
        }
    }
}

#endif
