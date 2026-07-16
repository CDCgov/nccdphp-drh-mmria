#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Diagnostics;
using mmria.common.SharedLibraries.DeIdentified;
using mmria.common.SharedLibraries.Report;
using Newtonsoft.Json.Linq;


namespace mmria.server.utils;

public sealed class c_sync_document
{

    string document_json;
    string document_id;
    string method;

    string metadata_version;

    mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly IDeIdentifiedRepository _deIdentifiedRepository;
    private readonly IReportRepository _reportRepository;
    private readonly bool _isShowSyncDocumentStatus;
    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly string _host_prefix;
    private readonly c_document_sync_rebuild_context _rebuild_context;
    private readonly bool _skip_revision_lookup;

    public c_sync_document 
    (
        string p_document_id, 
        string p_document_json, 
        string p_method,
        string p_metadata_version,
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        IDeIdentifiedRepository deIdentifiedRepository = null,
        IReportRepository reportRepository = null,
        mmria.common.couchdb.OverridableConfiguration configuration = null,
        string host_prefix = null,
        c_document_sync_rebuild_context rebuild_context = null,
        bool skip_revision_lookup = false
    )
    {
        this.document_json = p_document_json;
        this.document_id = p_document_id;
        metadata_version = p_metadata_version;
        db_config = _db_config;
        _couchDbHttpClient = couchDbHttpClient;
        _deIdentifiedRepository = deIdentifiedRepository;
        _reportRepository = reportRepository;
        _configuration = configuration;
        _host_prefix = host_prefix;
        _rebuild_context = rebuild_context;
        _skip_revision_lookup = skip_revision_lookup || rebuild_context != null;
        
        // Default to true if configuration is not provided or key doesn't exist
        _isShowSyncDocumentStatus = configuration?.GetBoolean("is_show_sync_document_status", host_prefix ?? "shared") ?? true;

        switch (p_method.ToUpperInvariant ())
        {
            case "DELETE":
                this.method = "DELETE";
                break;
            case "PUT":
            default:
                this.method = "PUT";
                break;
        }
        
    }



    private string set_revision(string p_document, string p_revision_id)
    {

        string result = null;


        var request_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(p_document);
        IDictionary<string, object> expando_object = request_result as IDictionary<string, object>;

        if(expando_object != null)
        {
            expando_object ["_rev"] = p_revision_id;
        }

        result =  Newtonsoft.Json.JsonConvert.SerializeObject(expando_object);

        return result;
    }


    private async System.Threading.Tasks.Task<string> get_revision(string p_document_url)
    {
        if(_skip_revision_lookup)
        {
            return null;
        }

        string result = null;

        string temp_document_json = null;

        try
        {
            
            temp_document_json = await _couchDbHttpClient.ExecuteAsync("GET", p_document_url, null, db_config.user_name, db_config.user_value);
            var request_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(temp_document_json);
            IDictionary<string, object> updater = request_result as IDictionary<string, object>;
            if(updater != null && updater.ContainsKey("_rev"))
            {
                result = updater ["_rev"].ToString ();
            }
        }
        catch(Exception ex) 
        {
            if (!(ex.Message.IndexOf ("(404) Object Not Found") > -1)) 
            {
                //System.Console.WriteLine ("c_sync_document.get_revision");
                //System.Console.WriteLine (ex);
            }
        }

        return result;
    }

    private async System.Threading.Tasks.Task<string> get_case_template_json_async()
    {
        if(!string.IsNullOrWhiteSpace(_rebuild_context?.case_template_json))
        {
            return _rebuild_context.case_template_json;
        }

        return await c_case_template_resolver.ReadBestAvailableCaseTemplateAsync(metadata_version, System.Console.WriteLine);
    }

    private async System.Threading.Tasks.Task<string> build_de_identified_json_async(System.Dynamic.ExpandoObject source_object)
    {
        string de_identified_json = await new mmria.server.utils.c_de_identifier(document_json, metadata_version, db_config, _couchDbHttpClient, source_object, _rebuild_context).executeAsync();

        if(string.IsNullOrEmpty(de_identified_json))
        {
            try
            {
                de_identified_json = await get_case_template_json_async();
                var case_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(de_identified_json);
                var byName = (IDictionary<string, object>)case_expando_object;
                var created_by = byName["created_by"] as string;
                if(string.IsNullOrWhiteSpace(created_by))
                {
                    byName["created_by"] = "system2";
                }

                if(byName.ContainsKey("last_updated_by"))
                {
                    byName["last_updated_by"] = "system2";
                }
                else
                {
                    byName.Add("last_updated_by", "system2");
                }

                byName["_id"] = this.document_id;
                de_identified_json = Newtonsoft.Json.JsonConvert.SerializeObject(case_expando_object);
            }
            catch (Exception)
            {
            }
        }

        return de_identified_json;
    }

    private string ensure_document_id(string p_document_json, string p_document_id, bool remove_revision = false)
    {
        if(string.IsNullOrWhiteSpace(p_document_json))
        {
            return p_document_json;
        }

        var document_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(p_document_json);
        var byName = (IDictionary<string, object>)document_expando_object;
        byName["_id"] = p_document_id;

        if(remove_revision)
        {
            byName.Remove("_rev");
        }

        return Newtonsoft.Json.JsonConvert.SerializeObject(document_expando_object);
    }

    private void add_report_document(List<string> report_document_json_list, string report_document_json, string report_document_id)
    {
        if(string.IsNullOrWhiteSpace(report_document_json))
        {
            return;
        }

        report_document_json_list.Add(ensure_document_id(report_document_json, report_document_id, remove_revision: _skip_revision_lookup));
    }

    private void log_sync_stage(string stage, Stopwatch stopwatch)
    {
        if(_isShowSyncDocumentStatus && stopwatch != null)
        {
            System.Console.WriteLine($"c_sync_document {stage} {document_id} {stopwatch.ElapsedMilliseconds}ms");
        }
    }

    private async System.Threading.Tasks.Task<mmria.common.metadata.app> get_metadata_async()
    {
        if(_rebuild_context?.metadata != null)
        {
            return _rebuild_context.metadata;
        }

        string metadata_url = db_config.url + $"/metadata/version_specification-{metadata_version}/metadata";
        string metadata_response = await _couchDbHttpClient.ExecuteAsync("GET", metadata_url, null, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_response);
    }

    private async System.Threading.Tasks.Task<HashSet<string>> get_de_identified_set_async()
    {
        if(_rebuild_context?.de_identified_set?.Count > 0)
        {
            return new HashSet<string>(_rebuild_context.de_identified_set, StringComparer.OrdinalIgnoreCase);
        }

        string de_identified_response = await _couchDbHttpClient.ExecuteAsync("GET", db_config.url + "/metadata/de-identified-list", null, db_config.user_name, db_config.user_value);
        System.Dynamic.ExpandoObject de_identified_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(de_identified_response);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(string path in (IList<object>)(((IDictionary<string, object>)de_identified_expando_object)["paths"]))
        {
            result.Add(path);
        }

        return result;
    }

    private async System.Threading.Tasks.Task<c_document_sync_rebuild_context> get_effective_rebuild_context_async()
    {
        if
        (
            _rebuild_context?.metadata != null &&
            _rebuild_context.de_identified_set?.Count > 0
        )
        {
            return _rebuild_context;
        }

        var metadata_task = get_metadata_async();
        var de_identified_set_task = get_de_identified_set_async();

        await System.Threading.Tasks.Task.WhenAll(metadata_task, de_identified_set_task);

        return new c_document_sync_rebuild_context
        {
            metadata = metadata_task.Result,
            de_identified_set = de_identified_set_task.Result,
            case_template_json = _rebuild_context?.case_template_json
        };
    }

    private static string get_document_id(string p_document_json)
    {
        if(string.IsNullOrWhiteSpace(p_document_json))
        {
            return null;
        }

        var document_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(p_document_json);
        var byName = document_expando_object as IDictionary<string, object>;

        if(byName != null && byName.ContainsKey("_id"))
        {
            return byName["_id"]?.ToString();
        }

        return null;
    }

    private string prepare_document_json_for_persist(string p_document_json, string p_document_id, string p_revision_id)
    {
        if(string.IsNullOrWhiteSpace(p_document_json))
        {
            return p_document_json;
        }

        string result = ensure_document_id(p_document_json, p_document_id, remove_revision: string.IsNullOrWhiteSpace(p_revision_id));

        if(!string.IsNullOrWhiteSpace(p_revision_id))
        {
            result = set_revision(result, p_revision_id);
        }

        return result;
    }

    private string build_document_url(string p_database_name, string p_document_id, string p_revision_id)
    {
        var request_url = new System.Text.StringBuilder();
        request_url.Append(db_config.url);
        request_url.Append('/');
        request_url.Append(db_config.prefix);
        request_url.Append(p_database_name);
        request_url.Append('/');
        request_url.Append(p_document_id);

        if(this.method == "DELETE")
        {
            request_url.Append("?rev=");
            request_url.Append(p_revision_id);
        }

        return request_url.ToString();
    }

    private async System.Threading.Tasks.Task persist_document_async(string p_database_name, string p_document_id, string p_document_json, string p_error_label)
    {
        string revision;
        if(_skip_revision_lookup)
        {
            revision = null;
        }
        else if(p_database_name == "de_id")
        {
            revision = await _deIdentifiedRepository.GetRevisionAsync(p_document_id, db_config);
        }
        else
        {
            revision = await _reportRepository.GetRevisionAsync(p_document_id, db_config);
        }

        try
        {
            if(this.method == "DELETE")
            {
                if(p_database_name == "de_id")
                    await _deIdentifiedRepository.DeleteDocumentAsync(p_document_id, revision, db_config);
                else
                    await _reportRepository.DeleteDocumentAsync(p_document_id, revision, db_config);
            }
            else
            {
                string request_json = prepare_document_json_for_persist(p_document_json, p_document_id, revision);
                if(!string.IsNullOrWhiteSpace(request_json))
                {
                    var doc = JObject.Parse(request_json);
                    if(p_database_name == "de_id")
                        await _deIdentifiedRepository.UpsertDocumentAsync(p_document_id, doc, db_config);
                    else
                        await _reportRepository.UpsertDocumentAsync(p_document_id, doc, db_config);
                }
            }
        }
        catch(Exception ex)
        {
            if(!string.IsNullOrWhiteSpace(p_error_label))
            {
                System.Console.WriteLine($"sync {p_error_label} error");
                System.Console.WriteLine(ex);
            }
        }
    }

    private async System.Threading.Tasks.Task<c_document_sync_build_result> build_documents_async
    (
        System.Dynamic.ExpandoObject source_object,
        c_document_sync_rebuild_context effective_rebuild_context,
        bool swallow_builder_errors
    )
    {
        var result = new c_document_sync_build_result();

        async System.Threading.Tasks.Task run_builder_async(string error_label, Func<System.Threading.Tasks.Task> builder)
        {
            if(swallow_builder_errors)
            {
                try
                {
                    await builder();
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine($"sync {error_label} error");
                    System.Console.WriteLine(ex);
                }
            }
            else
            {
                await builder();
            }
        }

        await run_builder_async("de_id", async () =>
        {
            result.de_identified_json = await new mmria.server.utils.c_de_identifier(document_json, metadata_version, db_config, _couchDbHttpClient, source_object, effective_rebuild_context).executeAsync();

            if(string.IsNullOrEmpty(result.de_identified_json))
            {
                try
                {
                    string case_template_json = effective_rebuild_context?.case_template_json ?? await get_case_template_json_async();
                    var case_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(case_template_json);
                    var byName = (IDictionary<string, object>)case_expando_object;
                    var created_by = byName["created_by"] as string;
                    if(string.IsNullOrWhiteSpace(created_by))
                    {
                        byName["created_by"] = "system2";
                    }

                    if(byName.ContainsKey("last_updated_by"))
                    {
                        byName["last_updated_by"] = "system2";
                    }
                    else
                    {
                        byName.Add("last_updated_by", "system2");
                    }

                    byName["_id"] = this.document_id;
                    result.de_identified_json = Newtonsoft.Json.JsonConvert.SerializeObject(case_expando_object);
                }
                catch (Exception)
                {
                }
            }
        });

        await run_builder_async("aggregate", async () =>
        {
            string aggregate_json = await new mmria.server.utils.c_convert_to_report_object(document_json, metadata_version, db_config, _couchDbHttpClient, _configuration, _host_prefix, source_object, effective_rebuild_context?.metadata).executeAsync();
            if(!string.IsNullOrWhiteSpace(aggregate_json))
            {
                result.report_document_json_list.Add(ensure_document_id(aggregate_json, this.document_id, remove_revision: _skip_revision_lookup));
            }
        });

        await run_builder_async("aggregate_id", async () =>
        {
            string opioid_report_json = await new mmria.server.utils.c_convert_to_opioid_report_object(document_json, "overdose", metadata_version, db_config, _couchDbHttpClient, _configuration, _host_prefix, source_object, effective_rebuild_context?.metadata).executeAsync();
            add_report_document(result.report_document_json_list, opioid_report_json, "opioid-" + this.document_id);
        });

        await run_builder_async("aggregate_id", async () =>
        {
            string powerbi_report_json = await new mmria.server.utils.c_convert_to_opioid_report_object(document_json, "powerbi", metadata_version, db_config, _couchDbHttpClient, _configuration, _host_prefix, source_object, effective_rebuild_context?.metadata).executeAsync();
            add_report_document(result.report_document_json_list, powerbi_report_json, "powerbi-" + this.document_id);
        });

        await run_builder_async("dqr detail", async () =>
        {
            string dqr_detail_report_json = await new mmria.server.utils.c_convert_to_dqr_detail(document_json, "dqr-detail", metadata_version, db_config, _couchDbHttpClient, source_object, effective_rebuild_context?.metadata).executeAsync();
            add_report_document(result.report_document_json_list, dqr_detail_report_json, "dqr-" + this.document_id);
        });

        await run_builder_async("freq detail", async () =>
        {
            string freq_detail_report_json = await new mmria.server.utils.c_generate_frequency_summary_report(document_json, "freq-detail", metadata_version, db_config, _couchDbHttpClient, source_object, effective_rebuild_context?.metadata).executeAsync();
            add_report_document(result.report_document_json_list, freq_detail_report_json, "freq-" + this.document_id);
        });

        return result;
    }

    public async System.Threading.Tasks.Task<c_document_sync_build_result> build_documents_async()
    {
        if(this.method == "DELETE")
        {
            throw new InvalidOperationException("build_documents_async only supports PUT operations.");
        }

        var source_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(document_json);
        var effective_rebuild_context = await get_effective_rebuild_context_async();
        return await build_documents_async(source_object, effective_rebuild_context, swallow_builder_errors: false);
    }

    public async System.Threading.Tasks.Task executeAsync()
    {
        var total_stopwatch = Stopwatch.StartNew();

        if(this.method == "DELETE")
        {
            await persist_document_async("de_id", this.document_id, null, "de_id");
            await persist_document_async("report", this.document_id, null, "aggregate");
            await persist_document_async("report", "opioid-" + this.document_id, null, "aggregate_id");
            await persist_document_async("report", "powerbi-" + this.document_id, null, "aggregate_id");
            await persist_document_async("report", "dqr-" + this.document_id, null, "dqr detail");
            await persist_document_async("report", "freq-" + this.document_id, null, "freq detail");
            log_sync_stage("delete-total", total_stopwatch);
            return;
        }

        var context_stopwatch = Stopwatch.StartNew();
        var source_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(document_json);
        var effective_rebuild_context = await get_effective_rebuild_context_async();
        log_sync_stage("context", context_stopwatch);

        var build_stopwatch = Stopwatch.StartNew();
        var build_result = await build_documents_async(source_object, effective_rebuild_context, swallow_builder_errors: true);
        log_sync_stage("build", build_stopwatch);

        var persist_stopwatch = Stopwatch.StartNew();
        await persist_document_async("de_id", this.document_id, build_result.de_identified_json, "de_id");

        foreach(var report_document_json in build_result.report_document_json_list)
        {
            string report_document_id = get_document_id(report_document_json);
            if(string.IsNullOrWhiteSpace(report_document_id))
            {
                continue;
            }

            string error_label = report_document_id.StartsWith("dqr-", StringComparison.OrdinalIgnoreCase)
                ? "dqr detail"
                : report_document_id.StartsWith("freq-", StringComparison.OrdinalIgnoreCase)
                    ? "freq detail"
                    : "aggregate_id";

            if(report_document_id == this.document_id)
            {
                error_label = "aggregate";
            }

            await persist_document_async("report", report_document_id, report_document_json, error_label);
        }
        log_sync_stage("persist", persist_stopwatch);
        log_sync_stage("total", total_stopwatch);
    }
}


#endif
