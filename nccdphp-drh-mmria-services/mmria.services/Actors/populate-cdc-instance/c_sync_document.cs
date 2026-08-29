using System;
using System.Collections.Generic;
using mmria.common.SharedLibraries.DeIdentified;
using mmria.common.SharedLibraries.Report;
using Newtonsoft.Json.Linq;


namespace mmria.server.utils;

public sealed class c_sync_document
{

    private string document_json;
    private string document_id;
    private string method;

    common.couchdb.DBConfigurationDetail connection;

    string metadata_release_version_name;
    mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly IDeIdentifiedRepository _deIdentifiedRepository;
    private readonly IReportRepository _reportRepository;
    private readonly bool _isShowSyncDocumentStatus;
    private readonly c_document_sync_rebuild_context _rebuild_context;
    private readonly bool _skip_revision_lookup;

    public c_sync_document
    (
        string p_document_id,
        string p_document_json,
        common.couchdb.DBConfigurationDetail p_connection,
        string p_metadata_release_version_name,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        IDeIdentifiedRepository deIdentifiedRepository = null,
        IReportRepository reportRepository = null,
        string p_method = "PUT",
        mmria.common.couchdb.OverridableConfiguration configuration = null,
        string host_prefix = null,
        c_document_sync_rebuild_context rebuild_context = null,
        bool skip_revision_lookup = false
    )
    {
        this.document_json = p_document_json;
        this.document_id = p_document_id;
        connection = p_connection;
        metadata_release_version_name = p_metadata_release_version_name;
        _couchDbHttpClient = couchDbHttpClient;
        _deIdentifiedRepository = deIdentifiedRepository;
        _reportRepository = reportRepository;
        _isShowSyncDocumentStatus = configuration?.GetBoolean("is_show_sync_document_status", host_prefix ?? "shared") ?? true;
        _rebuild_context = rebuild_context;
        _skip_revision_lookup = skip_revision_lookup || rebuild_context != null;

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
            
            temp_document_json = await _couchDbHttpClient.ExecuteAsync("GET", p_document_url, null, connection.user_name, connection.user_value);
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
        if(_rebuild_context != null)
        {
            return _rebuild_context.case_template_json;
        }

        var case_template_path = mmria.common.SharedLibraries.MMRIAServices.Helper.MMRIAServicesHelper.ResolveDatabaseScriptPath($"case-version-{metadata_release_version_name}.json");

        using (var sr = new System.IO.StreamReader(case_template_path))
        {
            return await sr.ReadToEndAsync();
        }
    }

    private async System.Threading.Tasks.Task<string> build_de_identified_json_async(System.Dynamic.ExpandoObject source_object)
    {
        string de_identified_json = await new mmria.server.utils.c_de_identifier(document_json, connection, metadata_release_version_name, _couchDbHttpClient, source_object, _rebuild_context).executeAsync();

        if(string.IsNullOrEmpty(de_identified_json))
        {
            try
            {
                de_identified_json = await get_case_template_json_async();

                if(string.IsNullOrWhiteSpace(de_identified_json))
                {
                    return null;
                }

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

        string aggregate_json = await new mmria.server.utils.c_convert_to_report_object(document_json, connection, metadata_release_version_name, _couchDbHttpClient, source_object, _rebuild_context?.metadata).execute();
        add_report_document(result.report_document_json_list, aggregate_json, this.document_id);

        string opioid_report_json = await new mmria.server.utils.c_convert_to_opioid_report_object(document_json, connection, metadata_release_version_name, _couchDbHttpClient, "overdose", source_object, _rebuild_context?.metadata).execute();
        add_report_document(result.report_document_json_list, opioid_report_json, "opioid-" + this.document_id);

        string powerbi_report_json = await new mmria.server.utils.c_convert_to_opioid_report_object(document_json, connection, metadata_release_version_name, _couchDbHttpClient, "powerbi", source_object, _rebuild_context?.metadata).execute();
        add_report_document(result.report_document_json_list, powerbi_report_json, "powerbi-" + this.document_id);

        string dqr_detail_report_json = await new mmria.server.utils.c_convert_to_dqr_detail(document_json, connection, metadata_release_version_name, _couchDbHttpClient, "dqr-detail", source_object, _rebuild_context?.metadata).execute();
        add_report_document(result.report_document_json_list, dqr_detail_report_json, "dqr-" + this.document_id);

        string freq_detail_report_json = await new mmria.server.utils.c_generate_frequency_summary_report(connection, metadata_release_version_name, document_json, _couchDbHttpClient, "freq-detail", source_object, _rebuild_context?.metadata).execute();
        add_report_document(result.report_document_json_list, freq_detail_report_json, "freq-" + this.document_id);

        return result;
    }

    public async System.Threading.Tasks.Task executeAsync()
    {

        string de_identified_revision = _skip_revision_lookup ? null : await _deIdentifiedRepository.GetRevisionAsync(this.document_id, connection);
        string de_identified_json = null;

        if(this.method == "DELETE")
        {
        }
        else
        {
            de_identified_json = await build_de_identified_json_async(null);

            if(!string.IsNullOrEmpty(de_identified_revision))
            {
                de_identified_json = set_revision (de_identified_json, de_identified_revision);
            }
        }

        try
        {
            if(this.method == "DELETE")
            {
                await _deIdentifiedRepository.DeleteDocumentAsync(this.document_id, de_identified_revision, connection);
            }
            else if(!string.IsNullOrEmpty(de_identified_json))
            {
                await _deIdentifiedRepository.UpsertDocumentAsync(this.document_id, JObject.Parse(de_identified_json), connection);
            }
        }
        catch (Exception)
        {
            //System.Console.WriteLine("c_sync_document de_id");
            //System.Console.WriteLine(ex);
        }
    
        


        try
        {
            string aggregate_json = await new mmria.server.utils.c_convert_to_report_object(document_json, connection, metadata_release_version_name, _couchDbHttpClient).execute();

            string aggregate_revision = _skip_revision_lookup ? null : await _reportRepository.GetRevisionAsync(this.document_id, connection);

            if(!string.IsNullOrEmpty(aggregate_revision))
            {
                aggregate_json = set_revision (aggregate_json, aggregate_revision);
            }

            if(this.method == "DELETE")
            {
                await _reportRepository.DeleteDocumentAsync(this.document_id, aggregate_revision, connection);
            }
            else if(!string.IsNullOrWhiteSpace(aggregate_json))
            {
                await _reportRepository.UpsertDocumentAsync(this.document_id, JObject.Parse(aggregate_json), connection);
            }

        }
        catch (Exception)
        {
            //System.Console.WriteLine("sync aggregate_id");
            //System.Console.WriteLine(ex);
        }



        try
        {
            string opioid_report_json = await new mmria.server.utils.c_convert_to_opioid_report_object(document_json, connection, metadata_release_version_name, _couchDbHttpClient).execute();

            if(!string.IsNullOrWhiteSpace(opioid_report_json))
            {
                var opioid_id = "opioid-" + this.document_id;
                string aggregate_revision = await get_revision (connection.url + $"/report/" + opioid_id);


                var opioid_report_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (opioid_report_json);
                var byName = (IDictionary<string,object>)opioid_report_expando_object;
                byName["_id"] = opioid_id;
                opioid_report_json =  Newtonsoft.Json.JsonConvert.SerializeObject(opioid_report_expando_object);

                System.Text.StringBuilder opioid_aggregate_url = new System.Text.StringBuilder();

                if(!string.IsNullOrEmpty(aggregate_revision))
                {
                    opioid_report_json = set_revision (opioid_report_json, aggregate_revision);
                }


                opioid_aggregate_url.Append(connection.url);
                opioid_aggregate_url.Append($"/report/");
                opioid_aggregate_url.Append(opioid_id);
    
                if(this.method == "DELETE")
                {
                    opioid_aggregate_url.Append("?rev=");
                    opioid_aggregate_url.Append(aggregate_revision);	
                }

                string aggregate_result = await _couchDbHttpClient.ExecuteAsync(this.method, opioid_aggregate_url.ToString(), opioid_report_json, connection.user_name, connection.user_value);
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
            string opioid_report_json = await new mmria.server.utils.c_convert_to_opioid_report_object(document_json, connection, metadata_release_version_name, _couchDbHttpClient, "powerbi").execute();

            if(!string.IsNullOrWhiteSpace(opioid_report_json))
            {
                var opioid_id = "powerbi-" + this.document_id;
                string aggregate_revision = await get_revision (connection.url + $"/report/" + opioid_id);


                var opioid_report_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (opioid_report_json);
                var byName = (IDictionary<string,object>)opioid_report_expando_object;
                byName["_id"] = opioid_id;
                opioid_report_json =  Newtonsoft.Json.JsonConvert.SerializeObject(opioid_report_expando_object);

                System.Text.StringBuilder opioid_aggregate_url = new System.Text.StringBuilder();

                if(!string.IsNullOrEmpty(aggregate_revision))
                {
                    opioid_report_json = set_revision (opioid_report_json, aggregate_revision);
                }


                opioid_aggregate_url.Append(connection.url);
                opioid_aggregate_url.Append($"/report/");
                opioid_aggregate_url.Append(opioid_id);
    
                if(this.method == "DELETE")
                {
                    opioid_aggregate_url.Append("?rev=");
                    opioid_aggregate_url.Append(aggregate_revision);	
                }

                string aggregate_result = await _couchDbHttpClient.ExecuteAsync(this.method, opioid_aggregate_url.ToString(), opioid_report_json, connection.user_name, connection.user_value);
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
            string dqr_detail_report_json = await new mmria.server.utils.c_convert_to_dqr_detail(document_json, connection, metadata_release_version_name, _couchDbHttpClient, "dqr-detail").execute();

            if(!string.IsNullOrWhiteSpace(dqr_detail_report_json))
            {
                var dqr_id = "dqr-" + this.document_id;
                string current_detail_revision = await get_revision (connection.url + $"/report/" + dqr_id);


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


                dqr_detail_url.Append(connection.url);
                dqr_detail_url.Append($"/report/");
                dqr_detail_url.Append(dqr_id);
    
                if(this.method == "DELETE")
                {
                    dqr_detail_url.Append("?rev=");
                    dqr_detail_url.Append(current_detail_revision);	
                }

                string dqr_detail_result = await _couchDbHttpClient.ExecuteAsync(this.method, dqr_detail_url.ToString(), dqr_detail_report_json, connection.user_name, connection.user_value);
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
            string freq_detail_report_json = await new mmria.server.utils.c_generate_frequency_summary_report(connection, metadata_release_version_name, document_json, _couchDbHttpClient, "freq-detail").execute();

            if(!string.IsNullOrWhiteSpace(freq_detail_report_json))
            {
                var freq_id = "freq-" + this.document_id;
                string current_detail_revision = await get_revision (connection.url + $"/report/" + freq_id);


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


                freq_detail_url.Append(connection.url);
                freq_detail_url.Append($"/report/");
                freq_detail_url.Append(freq_id);
    
                if(this.method == "DELETE")
                {
                    freq_detail_url.Append("?rev=");
                    freq_detail_url.Append(current_detail_revision);	
                }

                string freq_detail_result = await _couchDbHttpClient.ExecuteAsync(this.method, freq_detail_url.ToString(), freq_detail_report_json, connection.user_name, connection.user_value);
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


