#if IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;


namespace mmria.pmss.server.utils;

public sealed class c_sync_document
{

    string document_json;
    string document_id;
    string method;

    string metadata_version;

    mmria.common.couchdb.DBConfigurationDetail db_config = null;

    public c_sync_document 
    (
        string p_document_id, 
        string p_document_json, 
        string p_method,
        string p_metadata_version,
        mmria.common.couchdb.DBConfigurationDetail _db_config
    )
    {
        this.document_json = p_document_json;
        this.document_id = p_document_id;
        metadata_version = p_metadata_version;
        db_config = _db_config;

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

        string result = null;

        var document_curl = new mmria.server.cURL("GET", null, p_document_url, null, db_config.user_name, db_config.user_value);
        string temp_document_json = null;

        try
        {
            
            temp_document_json = await document_curl.executeAsync();
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
            de_identified_json = await new mmria.server.utils.c_de_identifier(document_json, metadata_version, db_config).executeAsync();

            if(string.IsNullOrEmpty(de_identified_json))
            {
                try 
                {
                    
                    string current_directory = AppContext.BaseDirectory;
                    if(!System.IO.Directory.Exists(System.IO.Path.Combine(current_directory, "database-scripts")))
                    {
                        current_directory = System.IO.Directory.GetCurrentDirectory();
                    }

                    using (var  sr = new System.IO.StreamReader(System.IO.Path.Combine( current_directory,  $"database-scripts/case-version-{metadata_version}.json")))
                    {
                        de_identified_json = await sr.ReadToEndAsync ();
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

        var de_identfied_curl = new mmria.server.cURL(this.method, null, de_identfied_url.ToString(), de_identified_json, db_config.user_name, db_config.user_value);
        try
        {
            string de_id_result = await de_identfied_curl.executeAsync();
            System.Console.WriteLine("sync de_id");
            System.Console.WriteLine(de_id_result);

        }
        catch (Exception)
        {
            //System.Console.WriteLine("c_sync_document de_id");
            //System.Console.WriteLine(ex);
        }
    

        try
        {
            string freq_detail_report_json = new mmria.server.utils.c_generate_frequency_summary_report(document_json, "freq-detail", metadata_version, db_config).execute();

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

                var freq_detail_curl = new mmria.server.cURL(this.method, null, freq_detail_url.ToString(), freq_detail_report_json,  db_config.user_name, db_config.user_value);

                string freq_detail_result = await freq_detail_curl.executeAsync();
                System.Console.WriteLine("c_sync_document freq detail");
                System.Console.WriteLine(freq_detail_result);
            }

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("sync freq detail error");
            System.Console.WriteLine(ex);
        }
        
        return;

        try
        {
            string aggregate_json = new mmria.server.utils.c_convert_to_report_object(document_json, metadata_version, db_config).execute();

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

            var aggregate_curl = new mmria.server.cURL(this.method, null, aggregate_url.ToString(), aggregate_json,  db_config.user_name, db_config.user_value);

            string aggregate_result = await aggregate_curl.executeAsync();
            System.Console.WriteLine("c_sync_document aggregate_id");
            System.Console.WriteLine(aggregate_result);

        }
        catch (Exception)
        {
            //System.Console.WriteLine("sync aggregate_id");
            //System.Console.WriteLine(ex);
        }


        
        try
        {
            string opioid_report_json = new mmria.server.utils.c_convert_to_opioid_report_object(document_json, "overdose", metadata_version, db_config).execute();

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

                var aggregate_curl = new mmria.server.cURL(this.method, null, opioid_aggregate_url.ToString(), opioid_report_json,  db_config.user_name, db_config.user_value);

                string aggregate_result = await aggregate_curl.executeAsync();
                System.Console.WriteLine("c_sync_document aggregate_id");
                System.Console.WriteLine(aggregate_result);
            }

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("sync aggregate_id");
            System.Console.WriteLine(ex);
        }

        try
        {
            string opioid_report_json = new mmria.server.utils.c_convert_to_opioid_report_object(document_json, "powerbi", metadata_version, db_config).execute();

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

                var aggregate_curl = new mmria.server.cURL(this.method, null, opioid_aggregate_url.ToString(), opioid_report_json,  db_config.user_name, db_config.user_value);

                string aggregate_result = await aggregate_curl.executeAsync();
                System.Console.WriteLine("c_sync_document aggregate_id");
                System.Console.WriteLine(aggregate_result);
            }

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("sync aggregate_id");
            System.Console.WriteLine(ex);
        }


        try
        {
            string dqr_detail_report_json = new mmria.server.utils.c_convert_to_dqr_detail(document_json, "dqr-detail", metadata_version, db_config).execute();

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

                var dqr_detail_curl = new mmria.server.cURL(this.method, null, dqr_detail_url.ToString(), dqr_detail_report_json,  db_config.user_name, db_config.user_value);

                string dqr_detail_result = await dqr_detail_curl.executeAsync();
                System.Console.WriteLine("c_sync_document dqr detail");
                System.Console.WriteLine(dqr_detail_result);
            }

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("sync dqr detail error");
            System.Console.WriteLine(ex);
        }


        


 

    }
}


#endif