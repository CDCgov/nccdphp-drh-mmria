#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using mmria.server.model.actor;

namespace mmria.server.model.actor.quartz;
   
public sealed class Process_Central_Pull_list : ReceiveActor
{
    private static int run_count = 0;
    private const int SkipCount = 0;
    //protected override void PreStart() => Console.WriteLine("Rebuild_Export_Queue started");
    //protected override void PostStop() => Console.WriteLine("Rebuild_Export_Queue stopped");
	mmria.common.couchdb.DBConfigurationDetail db_config = null;
    mmria.common.couchdb.ConfigurationSet config_db;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly string _host_prefix;

    public Process_Central_Pull_list
    (
        mmria.common.couchdb.ConfigurationSet _configuration_set,
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.couchdb.OverridableConfiguration configuration = null,
        string host_prefix = null
    )
    {
        config_db = _configuration_set;
        db_config = _db_config;
        _couchDbHttpClient = couchDbHttpClient;
        _configuration = configuration;
        _host_prefix = host_prefix;
        
        ReceiveAsync<ScheduleInfoMessage>(async scheduleInfo => await Process_Schedule(scheduleInfo));
    }
    private async System.Threading.Tasks.Task Process_Schedule(ScheduleInfoMessage scheduleInfo)
    {
        //Console.WriteLine($"Process_Central_Pull_list Process_Schedule {System.DateTime.Now}");

        if(run_count < SkipCount)
        {
            run_count ++;
            Context.Stop(this.Self);
            return;
        }
        else if(run_count == SkipCount)
        {
            run_count ++;
        }
        else
        {
            var midnight_timespan = new TimeSpan(0, 0, 0);
            var difference = DateTime.Now - midnight_timespan;
            if(difference.Hour != 0 && difference.Minute != 0)
            {
                Context.Stop(this.Self);
                return;
            }
        }
    
        if (!string.IsNullOrWhiteSpace(scheduleInfo.cdc_instance_pull_list))
        {
        
            try
            {
                var db_url = $"{scheduleInfo.couch_db_url}/{scheduleInfo.db_prefix}mmrds";
                await _couchDbHttpClient.ExecuteAsync("DELETE", db_url, null, scheduleInfo.user_name, scheduleInfo.user_value);

                        string current_directory = AppContext.BaseDirectory;

                        System.Console.WriteLine("mmrds_curl\n{0}", await _couchDbHttpClient.ExecuteAsync("PUT", scheduleInfo.couch_db_url + $"/{scheduleInfo.db_prefix}mmrds", null, scheduleInfo.user_name, scheduleInfo.user_value));

                        await _couchDbHttpClient.ExecuteAsync("PUT", scheduleInfo.couch_db_url + $"/{scheduleInfo.db_prefix}mmrds/_security", "{\"admins\":{\"names\":[],\"roles\":[\"form_designer\"]},\"members\":{\"names\":[],\"roles\":[\"abstractor\",\"data_analyst\",\"timer\"]}}", scheduleInfo.user_name, scheduleInfo.user_value);
                        System.Console.WriteLine("mmrds/_security completed successfully");

                        try 
                        {
                            using (var  sr = new System.IO.StreamReader(System.IO.Path.Combine (current_directory, "database-scripts/case_design_sortable.json")))
                            {

                                string case_design_sortable = await sr.ReadToEndAsync();
                                await _couchDbHttpClient.ExecuteAsync("PUT", scheduleInfo.couch_db_url + $"/{scheduleInfo.db_prefix}mmrds/_design/sortable", case_design_sortable, scheduleInfo.user_name, scheduleInfo.user_value);
                            }

                            using (var  sr = new System.IO.StreamReader(System.IO.Path.Combine (current_directory, "database-scripts/case_store_design_auth.json")))
                            {
                                string case_store_design_auth = await sr.ReadToEndAsync();
                                await _couchDbHttpClient.ExecuteAsync("PUT", scheduleInfo.couch_db_url + $"/{scheduleInfo.db_prefix}mmrds/_design/auth", case_store_design_auth, scheduleInfo.user_name, scheduleInfo.user_value);
                            }
                                                            
                        }
                        catch (Exception ex) 
                        {
                            System.Console.WriteLine($"unable to configure mmrds database:\n{ex}");
                        }


                    try
                    {

                        await _couchDbHttpClient.ExecuteAsync("DELETE", scheduleInfo.couch_db_url + $"/{scheduleInfo.db_prefix}de_id", null, scheduleInfo.user_name, scheduleInfo.user_value);
                    }
                    catch (Exception)
                    {
                    
                    }
                    

                    try
                    {
                        await _couchDbHttpClient.ExecuteAsync("DELETE", scheduleInfo.couch_db_url + $"/{scheduleInfo.db_prefix}report", null, scheduleInfo.user_name, scheduleInfo.user_value);
                    }
                    catch (Exception)
                    {
                    
                    }


                    try
                    {
                        await _couchDbHttpClient.ExecuteAsync("PUT", scheduleInfo.couch_db_url + $"/{scheduleInfo.db_prefix}de_id", null, scheduleInfo.user_name, scheduleInfo.user_value);
                    }
                    catch (Exception)
                    {
                    
                    }

                    try 
                    {
                        
                        if(!System.IO.Directory.Exists(System.IO.Path.Combine(current_directory, "database-scripts")))
                        {
                            current_directory = System.IO.Directory.GetCurrentDirectory();
                        }

                        using (var  sr = new System.IO.StreamReader(System.IO.Path.Combine( current_directory,  "database-scripts/case_design_sortable.json")))
                        {
                            string result = await sr.ReadToEndAsync();
                            await _couchDbHttpClient.ExecuteAsync("PUT", scheduleInfo.couch_db_url + $"/{scheduleInfo.db_prefix}de_id/_design/sortable", result, scheduleInfo.user_name, scheduleInfo.user_value);
                        }

        
                    } 
                    catch (Exception) 
                    {

                    }



                    try
                    {
                        await _couchDbHttpClient.ExecuteAsync("PUT", scheduleInfo.couch_db_url + $"/{scheduleInfo.db_prefix}report", null, scheduleInfo.user_name, scheduleInfo.user_value);
                    }
                    catch (Exception)
                    {
                    
                    }


                    try
                    {
                        var Report_Opioid_Index = new mmria.server.utils.c_document_sync_all.Report_Opioid_Index_Struct();
                        string index_json = Newtonsoft.Json.JsonConvert.SerializeObject (Report_Opioid_Index);
                        await _couchDbHttpClient.ExecuteAsync("POST", scheduleInfo.couch_db_url + $"/{scheduleInfo.db_prefix}report/_index", index_json, scheduleInfo.user_name, scheduleInfo.user_value);
                    }
                    catch (Exception)
                    {
                    
                    }

                
                    var config_cdc_instance_pull_list = scheduleInfo.cdc_instance_pull_list;
                    var cdc_instance_pull = config_cdc_instance_pull_list.Split(",");
                                
                    for (var i = 0; i < cdc_instance_pull.Length; i++)
                    {

                        var instance_name = cdc_instance_pull[i];
                        try
                        {
                            if(config_db.detail_list.ContainsKey(instance_name))
                            {
                                var db_info = config_db.detail_list[instance_name];

                                string url = $"{db_info.url}/{db_info.prefix}mmrds/_all_docs?include_docs=true";
                                string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", url, null, db_info.user_name, db_info.user_value);
                                var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<System.Dynamic.ExpandoObject>>(responseFromServer);

                                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;

                                foreach(var case_response_item in case_response.rows)
                                {
                                    var case_item = case_response_item.doc as IDictionary<string,object>;

                                    string _id = "";

                                    if(case_item == null)
                                    {
                                        continue;
                                    }
                                    else if (case_item.ContainsKey ("_id")) 
                                    {
                                        _id = case_item ["_id"].ToString();
                                    }
                                    else
                                    {
                                        continue;
                                    }

                                    if (_id.IndexOf ("_design/") > -1)
                                    {
                                        continue;
                                    }

                                    var  target_url = $"{scheduleInfo.couch_db_url}/{scheduleInfo.db_prefix}mmrds/{_id}";

                                    var document_json = Newtonsoft.Json.JsonConvert.SerializeObject(case_item);
                                    var de_identified_json = await new mmria.server.utils.c_cdc_de_identifier(document_json, instance_name, scheduleInfo, null).executeAsync();
                                    
                                    var de_identified_case = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(de_identified_json);

                                    var de_identified_dictionary = de_identified_case as IDictionary<string,object>;

                                    if(de_identified_dictionary == null)
                                    {
                                        continue;
                                    }
                                    
                                    var revision = await get_revision
                                    (
                                        target_url,
                                        scheduleInfo.user_name,
                                        scheduleInfo.user_value
                                    );
                                    
                                    if(!string.IsNullOrWhiteSpace(revision))
                                    {
                                        de_identified_dictionary["_rev"] = revision;
                                    }                                    
                                    
                                    var save_json = document_json = Newtonsoft.Json.JsonConvert.SerializeObject(de_identified_dictionary);

                                    var put_result_string = await Put_Document(save_json, _id, target_url, scheduleInfo.user_name, scheduleInfo.user_value);

                                    var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(put_result_string);

                                    if(result.ok)
                                    {
                                        var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message
                                        (
                                            _id,
                                            de_identified_json,
                                            "PUT",
                                            scheduleInfo.version_number
                                        );

                                        Context.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, _configuration, _host_prefix)).Tell(Sync_Document_Message);
                                    }

                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine($"Problem pulling instance:{instance_name}");
                            Console.WriteLine(ex);
                        }
                        
                    }

/*
                    PostCommand($"{scheduleInfo.couch_db_url}/{scheduleInfo.db_prefix}mmrds/_compact",scheduleInfo.user_name, scheduleInfo.user_value).GetAwaiter().GetResult();
                    PostCommand($"{scheduleInfo.couch_db_url}/{scheduleInfo.db_prefix}mmrds/_view_cleanup",scheduleInfo.user_name, scheduleInfo.user_value).GetAwaiter().GetResult();
                    PostCommand($"{scheduleInfo.couch_db_url}/{scheduleInfo.db_prefix}de_id/_compact",scheduleInfo.user_name, scheduleInfo.user_value).GetAwaiter().GetResult();
                    PostCommand($"{scheduleInfo.couch_db_url}/{scheduleInfo.db_prefix}de_id/_view_cleanup",scheduleInfo.user_name, scheduleInfo.user_value).GetAwaiter().GetResult();
                    PostCommand($"{scheduleInfo.couch_db_url}/{scheduleInfo.db_prefix}report/_compact",scheduleInfo.user_name, scheduleInfo.user_value).GetAwaiter().GetResult();
                    PostCommand($"{scheduleInfo.couch_db_url}/{scheduleInfo.db_prefix}report/_view_cleanup",scheduleInfo.user_name, scheduleInfo.user_value).GetAwaiter().GetResult();
*/
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Process_Central_Pull_list error: {ex}");
                }
        }
        
        Context.Stop(this.Self);
    }

    private bool url_endpoint_exists (string p_target_server, string p_user_name, string p_value, string p_method = "HEAD")
    {
        bool result = false;

        try 
        {
            // Note: CouchDbHttpClient doesn't support HEAD, using GET instead
            _couchDbHttpClient.ExecuteAsync(p_method == "HEAD" ? "GET" : p_method, p_target_server, null, p_user_name, p_value).GetAwaiter().GetResult(); // Note: This method is not async, keeping as-is
            /*
            HTTP/1.1 200 OK
            Cache-Control: must-revalidate
            Content-Type: application/json
            Date: Mon, 12 Aug 2013 01:27:41 GMT
            Server: CouchDB (Erlang/OTP)*/
            result = true;
        } 
        catch (Exception) 
        {
            // do nothing for now
        }


        return result;
    }

    private async System.Threading.Tasks.Task<string> PostCommand (string p_database_url, string p_user_name, string p_user_value)
    {
        string result = null;
        try
        {
            result = await _couchDbHttpClient.ExecuteAsync("POST", p_database_url, null, p_user_name, p_user_value);
        }
        catch (Exception ex)
        {
            result = ex.ToString ();
        }
        return result;
    }

    private async System.Threading.Tasks.Task<string> Put_Document (string p_document_json, string p_id, string p_database_url, string p_user_name, string p_user_value)
    {
        string result = null;
        try
        {
            result = await _couchDbHttpClient.ExecuteAsync("PUT", p_database_url, p_document_json, p_user_name, p_user_value);
        }
        catch (Exception ex)
        {
            result = ex.ToString ();
        }
        return result;
    }

    private async System.Threading.Tasks.Task<string> get_revision
    (
        string p_document_url,
        string p_user_name,
        string p_user_value

    )
    {

        string result = null;

        string temp_document_json = null;

        try
        {
            
            temp_document_json = await _couchDbHttpClient.ExecuteAsync("GET", p_document_url, null, p_user_name, p_user_value);
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

}
#endif