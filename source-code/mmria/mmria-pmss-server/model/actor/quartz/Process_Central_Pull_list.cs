using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using mmria.pmss.server.model.actor;

namespace mmria.pmss.server.model.actor.quartz;
   
public sealed class Process_Central_Pull_list : UntypedActor
{
    private static int run_count = 0;
    private const int SkipCount = 0;
    //protected override void PreStart() => Console.WriteLine("Rebuild_Export_Queue started");
    //protected override void PostStop() => Console.WriteLine("Rebuild_Export_Queue stopped");

    protected override void OnReceive(object message)
    {
            

        //Console.WriteLine($"Process_Central_Pull_list OnRecieve {System.DateTime.Now}");

        
        switch (message)
        {
            case ScheduleInfoMessage scheduleInfo:


                if(run_count < SkipCount)
                {
                    run_count ++;
                    break;
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
                        break;
                    }
                }
            
                if (!string.IsNullOrWhiteSpace(Program.config_cdc_instance_pull_list))
                {
                
                    try
                    {
                        var db_url = $"{Program.config_couchdb_url}/{Program.db_prefix}mmrds";
                        var delete_mmrds_curl = new cURL ("DELETE", null, db_url, null, Program.config_timer_user_name, Program.config_timer_value);
                        delete_mmrds_curl.executeAsync ().GetAwaiter().GetResult();

                        string current_directory = AppContext.BaseDirectory;

                        var mmrds_curl = new cURL ("PUT", null, Program.config_couchdb_url + $"/{Program.db_prefix}mmrds", null, Program.config_timer_user_name, Program.config_timer_value);
                        System.Console.WriteLine("mmrds_curl\n{0}", mmrds_curl.executeAsync ().GetAwaiter().GetResult());

                        new cURL ("PUT", null, Program.config_couchdb_url + $"/{Program.db_prefix}mmrds/_security", "{\"admins\":{\"names\":[],\"roles\":[\"form_designer\"]},\"members\":{\"names\":[],\"roles\":[\"abstractor\",\"data_analyst\",\"timer\"]}}", Program.config_timer_user_name, Program.config_timer_value).executeAsync ().GetAwaiter().GetResult();
                        System.Console.WriteLine("mmrds/_security completed successfully");

                        try 
                        {
                            using (var  sr = new System.IO.StreamReader(System.IO.Path.Combine (current_directory, "database-scripts/case_design_sortable.json")))
                            {

                                string case_design_sortable = sr.ReadToEnd ();
                                var case_design_sortable_curl = new cURL ("PUT", null, Program.config_couchdb_url + $"/{Program.db_prefix}mmrds/_design/sortable", case_design_sortable, Program.config_timer_user_name, Program.config_timer_value);
                                case_design_sortable_curl.executeAsync ().GetAwaiter().GetResult();
                            }

                            using (var  sr = new System.IO.StreamReader(System.IO.Path.Combine (current_directory, "database-scripts/case_store_design_auth.json")))
                            {
                                string case_store_design_auth = sr.ReadToEndAsync ().GetAwaiter().GetResult();
                                var case_store_design_auth_curl = new cURL ("PUT", null, Program.config_couchdb_url + $"/{Program.db_prefix}mmrds/_design/auth", case_store_design_auth, Program.config_timer_user_name, Program.config_timer_value);
                                case_store_design_auth_curl.executeAsync ().GetAwaiter().GetResult();    
                            }
                                                            
                        }
                        catch (Exception ex) 
                        {
                            System.Console.WriteLine($"unable to configure mmrds database:\n{ex}");
                        }
                
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }


                    try
                    {

                        var delete_de_id_curl = new cURL ("DELETE", null, Program.config_couchdb_url + $"/{Program.db_prefix}de_id", null, Program.config_timer_user_name, Program.config_timer_value);
                        delete_de_id_curl.execute();
                    }
                    catch (Exception)
                    {
                    
                    }
                    

                    try
                    {
                        var delete_report_curl = new cURL ("DELETE", null, Program.config_couchdb_url + $"/{Program.db_prefix}report", null, Program.config_timer_user_name, Program.config_timer_value);
                        delete_report_curl.execute();
                    }
                    catch (Exception)
                    {
                    
                    }


                    try
                    {
                        var create_de_id_curl = new cURL ("PUT", null, Program.config_couchdb_url + $"/{Program.db_prefix}de_id", null, Program.config_timer_user_name, Program.config_timer_value);
                        create_de_id_curl.execute();
                    }
                    catch (Exception)
                    {
                    
                    }

                    try 
                    {
                        
                        string current_directory = AppContext.BaseDirectory;
                        if(!System.IO.Directory.Exists(System.IO.Path.Combine(current_directory, "database-scripts")))
                        {
                            current_directory = System.IO.Directory.GetCurrentDirectory();
                        }

                        using (var  sr = new System.IO.StreamReader(System.IO.Path.Combine( current_directory,  "database-scripts/case_design_sortable.json")))
                        {
                            string result = sr.ReadToEnd();
                            var create_de_id_curl = new cURL ("PUT", null, Program.config_couchdb_url + $"/{Program.db_prefix}de_id/_design/sortable", result, Program.config_timer_user_name, Program.config_timer_value);
                            create_de_id_curl.execute();					
                        }

        
                    } 
                    catch (Exception) 
                    {

                    }



                    try
                    {
                        var create_report_curl = new cURL ("PUT", null, Program.config_couchdb_url + $"/{Program.db_prefix}report", null, Program.config_timer_user_name, Program.config_timer_value);
                        create_report_curl.execute();	
                    }
                    catch (Exception)
                    {
                    
                    }


                    try
                    {
                        var Report_Opioid_Index = new mmria.pmss.server.utils.c_document_sync_all.Report_Opioid_Index_Struct();
                        string index_json = Newtonsoft.Json.JsonConvert.SerializeObject (Report_Opioid_Index);
                        var create_report_index_curl = new cURL ("POST", null, Program.config_couchdb_url + $"/{Program.db_prefix}report/_index", index_json, Program.config_timer_user_name, Program.config_timer_value);
                        create_report_index_curl.execute();
                    }
                    catch (Exception)
                    {
                    
                    }

                
                    var config_cdc_instance_pull_list = Program.config_cdc_instance_pull_list;
                    var cdc_instance_pull = config_cdc_instance_pull_list.Split(",");
                    var config_db = Program.configuration_set;
                                
                    for (var i = 0; i < cdc_instance_pull.Length; i++)
                    {

                        var instance_name = cdc_instance_pull[i];
                        try
                        {
                            if(config_db.detail_list.ContainsKey(instance_name))
                            {
                                var db_info = config_db.detail_list[instance_name];

                                string url = $"{db_info.url}/{db_info.prefix}mmrds/_all_docs?include_docs=true";
                                var case_curl = new cURL("GET", null, url, null, db_info.user_name, db_info.user_value);
                                string responseFromServer = case_curl.execute();
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

                                    var  target_url = $"{Program.config_couchdb_url}/{Program.db_prefix}mmrds/{_id}";

                                    var document_json = Newtonsoft.Json.JsonConvert.SerializeObject(case_item);
                                    var de_identified_json = new mmria.pmss.server.utils.c_cdc_de_identifier(document_json, instance_name).executeAsync().GetAwaiter().GetResult();
                                    
                                    var de_identified_case = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(de_identified_json);

                                    var de_identified_dictionary = de_identified_case as IDictionary<string,object>;

                                    if(de_identified_dictionary == null)
                                    {
                                        continue;
                                    }
                                    
                                    var revision = get_revision(target_url).GetAwaiter().GetResult();
                                    if(!string.IsNullOrWhiteSpace(revision))
                                    {
                                        de_identified_dictionary["_rev"] = revision;
                                    }                                    
                                    
                                    var save_json = document_json = Newtonsoft.Json.JsonConvert.SerializeObject(de_identified_dictionary);

                                    var put_result_string = Put_Document(save_json, _id, target_url, Program.config_timer_user_name, Program.config_timer_value).GetAwaiter().GetResult();

                                    var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(put_result_string);

                                    if(result.ok)
                                    {
                                        var Sync_Document_Message = new mmria.pmss.server.model.actor.Sync_Document_Message
                                        (
                                            _id,
                                            de_identified_json
                                        );

                                        Context.ActorOf(Props.Create<mmria.pmss.server.model.actor.Synchronize_Case>()).Tell(Sync_Document_Message);
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
                    PostCommand($"{Program.config_couchdb_url}/{Program.db_prefix}mmrds/_compact",Program.config_timer_user_name, Program.config_timer_value).GetAwaiter().GetResult();
                    PostCommand($"{Program.config_couchdb_url}/{Program.db_prefix}mmrds/_view_cleanup",Program.config_timer_user_name, Program.config_timer_value).GetAwaiter().GetResult();
                    PostCommand($"{Program.config_couchdb_url}/{Program.db_prefix}de_id/_compact",Program.config_timer_user_name, Program.config_timer_value).GetAwaiter().GetResult();
                    PostCommand($"{Program.config_couchdb_url}/{Program.db_prefix}de_id/_view_cleanup",Program.config_timer_user_name, Program.config_timer_value).GetAwaiter().GetResult();
                    PostCommand($"{Program.config_couchdb_url}/{Program.db_prefix}report/_compact",Program.config_timer_user_name, Program.config_timer_value).GetAwaiter().GetResult();
                    PostCommand($"{Program.config_couchdb_url}/{Program.db_prefix}report/_view_cleanup",Program.config_timer_user_name, Program.config_timer_value).GetAwaiter().GetResult();
                    */
                }

                break;
        }

        Context.Stop(this.Self);
    }

    private static bool url_endpoint_exists (string p_target_server, string p_user_name, string p_value, string p_method = "HEAD")
    {
        bool result = false;

        var curl = new cURL (p_method, null, p_target_server, null, p_user_name, p_value);
        try 
        {
            curl.execute ();
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
        cURL document_curl = new cURL ("POST", null, p_database_url, null, p_user_name, p_user_value);
        try
        {
            result = await document_curl.executeAsync();
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
        cURL document_curl = new cURL ("PUT", null, p_database_url, p_document_json, p_user_name, p_user_value);
        try
        {
            result = await document_curl.executeAsync();
        }
        catch (Exception ex)
        {
            result = ex.ToString ();
        }
        return result;
    }

    private async System.Threading.Tasks.Task<string> get_revision(string p_document_url)
    {

        string result = null;

        var document_curl = new cURL("GET", null, p_document_url, null, Program.config_timer_user_name, Program.config_timer_value);
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

}
