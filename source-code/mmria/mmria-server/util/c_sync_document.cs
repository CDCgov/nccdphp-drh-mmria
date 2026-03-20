#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;


namespace mmria.server.utils;

public sealed class c_sync_document
{

    string document_json;
    string document_id;
    string method;

    string metadata_version;

    mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
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

    public async System.Threading.Tasks.Task<c_document_sync_build_result> build_documents_async()
    {
        if(this.method == "DELETE")
        {
            throw new InvalidOperationException("build_documents_async only supports PUT operations.");
        }

        var result = new c_document_sync_build_result();
        var source_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(document_json);

        result.de_identified_json = await build_de_identified_json_async(source_object);

        string aggregate_json = await new mmria.server.utils.c_convert_to_report_object(document_json, metadata_version, db_config, _couchDbHttpClient, _configuration, _host_prefix, source_object, _rebuild_context?.metadata).executeAsync();
        if(!string.IsNullOrWhiteSpace(aggregate_json))
        {
            result.report_document_json_list.Add(ensure_document_id(aggregate_json, this.document_id, remove_revision: _skip_revision_lookup));
        }

        string opioid_report_json = await new mmria.server.utils.c_convert_to_opioid_report_object(document_json, "overdose", metadata_version, db_config, _couchDbHttpClient, _configuration, _host_prefix, source_object, _rebuild_context?.metadata).executeAsync();
        add_report_document(result.report_document_json_list, opioid_report_json, "opioid-" + this.document_id);

        string powerbi_report_json = await new mmria.server.utils.c_convert_to_opioid_report_object(document_json, "powerbi", metadata_version, db_config, _couchDbHttpClient, _configuration, _host_prefix, source_object, _rebuild_context?.metadata).executeAsync();
        add_report_document(result.report_document_json_list, powerbi_report_json, "powerbi-" + this.document_id);

        string dqr_detail_report_json = await new mmria.server.utils.c_convert_to_dqr_detail(document_json, "dqr-detail", metadata_version, db_config, _couchDbHttpClient, source_object, _rebuild_context?.metadata).executeAsync();
        add_report_document(result.report_document_json_list, dqr_detail_report_json, "dqr-" + this.document_id);

        string freq_detail_report_json = await new mmria.server.utils.c_generate_frequency_summary_report(document_json, "freq-detail", metadata_version, db_config, _couchDbHttpClient, source_object, _rebuild_context?.metadata).executeAsync();
        add_report_document(result.report_document_json_list, freq_detail_report_json, "freq-" + this.document_id);

        return result;
    }

    public async System.Threading.Tasks.Task executeAsync()
    {

        string de_identified_revision = await get_revision (db_config.url + $"/{db_config.prefix}de_id/" + this.document_id);
        System.Text.StringBuilder de_identfied_url = new System.Text.StringBuilder();
        string de_identified_json = null;

        de_identfied_url.Append(db_config.url);
        de_identfied_url.Append($"/{db_config.prefix}de_id/");
        de_identfied_url.Append(this.document_id);

        if(this.method == "DELETE")
        {
            de_identfied_url.Append("?rev=");
            de_identfied_url.Append(de_identified_revision);	

        }
        else
        {
            de_identified_json = await new mmria.server.utils.c_de_identifier(document_json, metadata_version, db_config, _couchDbHttpClient).executeAsync();

            if(string.IsNullOrEmpty(de_identified_json))
            {
                try 
                {
                    de_identified_json = await get_case_template_json_async();

                    if(string.IsNullOrWhiteSpace(de_identified_json))
                    {
                        throw new InvalidOperationException($"No case template is available for metadata version '{metadata_version}'.");
                    }

                    var case_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (de_identified_json);


                    var byName = (IDictionary<string,object>)case_expando_object;
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

    
                } 
                catch (Exception) 
                {

                }

            }

            if(!string.IsNullOrEmpty(de_identified_revision))
            {
                de_identified_json = set_revision (de_identified_json, de_identified_revision);
            }
        }

        try
        {
            string de_id_result = await _couchDbHttpClient.ExecuteAsync(this.method, de_identfied_url.ToString(), de_identified_json, db_config.user_name, db_config.user_value);
            if (_isShowSyncDocumentStatus)
            {
                System.Console.WriteLine("sync de_id");
                System.Console.WriteLine(de_id_result);
            }

        }
        catch (Exception)
        {
            //System.Console.WriteLine("c_sync_document de_id");
            //System.Console.WriteLine(ex);
        }
    
        


        try
        {
            string aggregate_json = await new mmria.server.utils.c_convert_to_report_object(document_json, metadata_version, db_config, _couchDbHttpClient, _configuration, _host_prefix).executeAsync();

            string aggregate_revision = await get_revision (db_config.url + $"/{db_config.prefix}report/" + this.document_id);

            System.Text.StringBuilder aggregate_url = new System.Text.StringBuilder();

            if(!string.IsNullOrEmpty(aggregate_revision))
            {
                aggregate_json = set_revision (aggregate_json, aggregate_revision);
            }


            aggregate_url.Append(db_config.url);
            aggregate_url.Append($"/{db_config.prefix}report/");
            aggregate_url.Append(this.document_id);

            if(this.method == "DELETE")
            {
                aggregate_url.Append("?rev=");
                aggregate_url.Append(aggregate_revision);	
            }

            string aggregate_result = await _couchDbHttpClient.ExecuteAsync(this.method, aggregate_url.ToString(), aggregate_json, db_config.user_name, db_config.user_value);
            if (_isShowSyncDocumentStatus)
            {
                System.Console.WriteLine("c_sync_document aggregate_id");
                System.Console.WriteLine(aggregate_result);
            }

        }
        catch (Exception)
        {
            //System.Console.WriteLine("sync aggregate_id");
            //System.Console.WriteLine(ex);
        }



        try
        {
            string opioid_report_json = await new mmria.server.utils.c_convert_to_opioid_report_object(document_json, "overdose", metadata_version, db_config, _couchDbHttpClient, _configuration, _host_prefix).executeAsync();

            if(!string.IsNullOrWhiteSpace(opioid_report_json))
            {
                var opioid_id = "opioid-" + this.document_id;
                string aggregate_revision = await get_revision (db_config.url + $"/{db_config.prefix}report/" + opioid_id);


                var opioid_report_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (opioid_report_json);
                var byName = (IDictionary<string,object>)opioid_report_expando_object;
                byName["_id"] = opioid_id;
                opioid_report_json =  Newtonsoft.Json.JsonConvert.SerializeObject(opioid_report_expando_object);

                System.Text.StringBuilder opioid_aggregate_url = new System.Text.StringBuilder();

                if(!string.IsNullOrEmpty(aggregate_revision))
                {
                    opioid_report_json = set_revision (opioid_report_json, aggregate_revision);
                }


                opioid_aggregate_url.Append(db_config.url);
                opioid_aggregate_url.Append($"/{db_config.prefix}report/");
                opioid_aggregate_url.Append(opioid_id);
    
                if(this.method == "DELETE")
                {
                    opioid_aggregate_url.Append("?rev=");
                    opioid_aggregate_url.Append(aggregate_revision);	
                }

                string aggregate_result = await _couchDbHttpClient.ExecuteAsync(this.method, opioid_aggregate_url.ToString(), opioid_report_json, db_config.user_name, db_config.user_value);

                if (_isShowSyncDocumentStatus)
                {
                    System.Console.WriteLine("c_sync_document aggregate_id");
                    System.Console.WriteLine(aggregate_result);
                }
            }

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("sync aggregate_id");
            System.Console.WriteLine(ex);
        }

        try
        {
            string opioid_report_json = await new mmria.server.utils.c_convert_to_opioid_report_object(document_json, "powerbi", metadata_version, db_config, _couchDbHttpClient, _configuration, _host_prefix).executeAsync();

            if(!string.IsNullOrWhiteSpace(opioid_report_json))
            {
                var opioid_id = "powerbi-" + this.document_id;
                string aggregate_revision = await get_revision (db_config.url + $"/{db_config.prefix}report/" + opioid_id);


                var opioid_report_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (opioid_report_json);
                var byName = (IDictionary<string,object>)opioid_report_expando_object;
                byName["_id"] = opioid_id;
                opioid_report_json =  Newtonsoft.Json.JsonConvert.SerializeObject(opioid_report_expando_object);

                System.Text.StringBuilder opioid_aggregate_url = new System.Text.StringBuilder();

                if(!string.IsNullOrEmpty(aggregate_revision))
                {
                    opioid_report_json = set_revision (opioid_report_json, aggregate_revision);
                }


                opioid_aggregate_url.Append(db_config.url);
                opioid_aggregate_url.Append($"/{db_config.prefix}report/");
                opioid_aggregate_url.Append(opioid_id);
    
                if(this.method == "DELETE")
                {
                    opioid_aggregate_url.Append("?rev=");
                    opioid_aggregate_url.Append(aggregate_revision);	
                }

                string aggregate_result = await _couchDbHttpClient.ExecuteAsync(this.method, opioid_aggregate_url.ToString(), opioid_report_json, db_config.user_name, db_config.user_value);
                if (_isShowSyncDocumentStatus)
                {
                    System.Console.WriteLine("c_sync_document aggregate_id");
                    System.Console.WriteLine(aggregate_result);
                }
            }

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("sync aggregate_id");
            System.Console.WriteLine(ex);
        }


        try
        {
            string dqr_detail_report_json = await new mmria.server.utils.c_convert_to_dqr_detail(document_json, "dqr-detail", metadata_version, db_config, _couchDbHttpClient).executeAsync();

            if(!string.IsNullOrWhiteSpace(dqr_detail_report_json))
            {
                var dqr_id = "dqr-" + this.document_id;
                string current_detail_revision = await get_revision (db_config.url + $"/{db_config.prefix}report/" + dqr_id);


                var dqr_report_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (dqr_detail_report_json);
                var byName = (IDictionary<string,object>)dqr_report_expando_object;
                byName["_id"] = dqr_id;
                dqr_detail_report_json =  Newtonsoft.Json.JsonConvert.SerializeObject(dqr_report_expando_object);
                
                System.Text.StringBuilder dqr_detail_url = new System.Text.StringBuilder();

                if(!string.IsNullOrEmpty(current_detail_revision))
                {
                    dqr_detail_report_json = set_revision (dqr_detail_report_json, current_detail_revision);
                }
                else
                {
                    byName.Remove("_rev");
                    dqr_detail_report_json =  Newtonsoft.Json.JsonConvert.SerializeObject(dqr_report_expando_object);
                }


                dqr_detail_url.Append(db_config.url);
                dqr_detail_url.Append($"/{db_config.prefix}report/");
                dqr_detail_url.Append(dqr_id);
    
                if(this.method == "DELETE")
                {
                    dqr_detail_url.Append("?rev=");
                    dqr_detail_url.Append(current_detail_revision);	
                }

                string dqr_detail_result = await _couchDbHttpClient.ExecuteAsync(this.method, dqr_detail_url.ToString(), dqr_detail_report_json, db_config.user_name, db_config.user_value);
                if (_isShowSyncDocumentStatus)
                {
                    System.Console.WriteLine("c_sync_document dqr detail");
                    System.Console.WriteLine(dqr_detail_result);
                }
            }

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("sync dqr detail error");
            System.Console.WriteLine(ex);
        }


        


        try
        {
            string freq_detail_report_json = await new mmria.server.utils.c_generate_frequency_summary_report(document_json, "freq-detail", metadata_version, db_config, _couchDbHttpClient).executeAsync();

            if(!string.IsNullOrWhiteSpace(freq_detail_report_json))
            {
                var freq_id = "freq-" + this.document_id;
                string current_detail_revision = await get_revision (db_config.url + $"/{db_config.prefix}report/" + freq_id);


                var dqr_report_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (freq_detail_report_json);
                var byName = (IDictionary<string,object>)dqr_report_expando_object;
                byName["_id"] = freq_id;
                freq_detail_report_json =  Newtonsoft.Json.JsonConvert.SerializeObject(dqr_report_expando_object);
                
                System.Text.StringBuilder freq_detail_url = new System.Text.StringBuilder();

                if(!string.IsNullOrEmpty(current_detail_revision))
                {
                    freq_detail_report_json = set_revision (freq_detail_report_json, current_detail_revision);
                }
                else
                {
                    byName.Remove("_rev");
                    freq_detail_report_json =  Newtonsoft.Json.JsonConvert.SerializeObject(dqr_report_expando_object);
                }


                freq_detail_url.Append(db_config.url);
                freq_detail_url.Append($"/{db_config.prefix}report/");
                freq_detail_url.Append(freq_id);
    
                if(this.method == "DELETE")
                {
                    freq_detail_url.Append("?rev=");
                    freq_detail_url.Append(current_detail_revision);	
                }

                string freq_detail_result = await _couchDbHttpClient.ExecuteAsync(this.method, freq_detail_url.ToString(), freq_detail_report_json, db_config.user_name, db_config.user_value);
                if (_isShowSyncDocumentStatus)
                {
                    System.Console.WriteLine("c_sync_document freq detail");
                    System.Console.WriteLine(freq_detail_result);
                }
            }

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("sync freq detail error");
            System.Console.WriteLine(ex);
        }

    }
}


#endif
