using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using mmria.getset;

namespace mmria.services.ExportQueue;

public sealed class Process_Export_Queue : UntypedActor
{
    //protected override void PreStart() => Console.WriteLine("Process_Export_Queue started");
    //protected override void PostStop() => Console.WriteLine("Process_Export_Queue stopped");

	mmria.common.couchdb.DBConfigurationDetail db_config = null;

    public Process_Export_Queue
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config
    )
    {
        db_config = _db_config;
    }

    protected override void OnReceive(object message)
    {
        switch(message)
        {
            case mmria.server.model.actor.ScheduleInfoMessage scheduleInfoMessage:
        
            //Console.WriteLine($"Process_Export_Queue {System.DateTime.Now}");

            //System.Console.WriteLine ("{0} Beginning Export Queue Item Processing", System.DateTime.Now);
            try
            {
                Process_Export_Queue_Item (scheduleInfoMessage);
            }
            catch(Exception ex)
            {
                // to nothing for now
                System.Console.WriteLine ("{0} check_for_changes_job.Process_Export_Queue_Item: error\n{1}", System.DateTime.Now, ex);

            }

            try
            {
                Process_Export_Queue_Delete (scheduleInfoMessage);
            }
            catch(Exception ex)
            {
                // to nothing for now
                System.Console.WriteLine ("{0} check_for_changes_job.Process_Export_Queue_Delete: error\n{1}", System.DateTime.Now, ex);

            }
            break;
        }

        Context.Stop(this.Self);
    }


    public void Process_Export_Queue_Item (mmria.server.model.actor.ScheduleInfoMessage scheduleInfoMessage)
    {
        //System.Console.WriteLine ("{0} check_for_changes_job.Process_Export_Queue_Item: started", System.DateTime.Now);

        List<export_queue_item> result = new List<export_queue_item> ();
        
        var get_curl = new cURL ("GET", null, db_config.url + $"/{db_config.prefix}export_queue/_all_docs?include_docs=true", null, scheduleInfoMessage.user_name, scheduleInfoMessage.user_value);

        string responseFromServer = get_curl.execute ();

        IDictionary<string,object> response_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (responseFromServer) as IDictionary<string,object>; 
        IList<object> enumerable_rows = null;
        
        if(response_result != null && response_result.ContainsKey("rows"))
        {
            enumerable_rows = response_result ["rows"] as IList<object>;
        }

        if(enumerable_rows != null)
        foreach (IDictionary<string,object> enumerable_item in enumerable_rows)
        {
            IDictionary<string,object> doc_item = enumerable_item ["doc"] as IDictionary<string,object>;
    
            if 
            (

                doc_item != null &&
                doc_item ["status"] != null &&
                doc_item["data_type"].ToString() == "export" &&
                doc_item ["status"].ToString ().StartsWith("In Queue...", StringComparison.OrdinalIgnoreCase)
            )
            {
                export_queue_item item = new export_queue_item ();

                item._id = doc_item ["_id"].ToString ();
                item._rev = doc_item ["_rev"].ToString ();
                item._deleted = doc_item .ContainsKey("_deleted") ? doc_item["_deleted"] as bool?: null;
                item.date_created = doc_item ["date_created"] as DateTime?;
                item.created_by = doc_item.ContainsKey("created_by") && doc_item ["created_by"] != null ? doc_item ["created_by"].ToString () : null;
                item.date_last_updated = doc_item ["date_last_updated"] as DateTime?;
                item.last_updated_by = doc_item.ContainsKey("last_updated_by") && doc_item ["last_updated_by"] != null ? doc_item ["last_updated_by"].ToString () : null;
                item.data_type = doc_item.ContainsKey("data_type") && doc_item ["data_type"] != null ? doc_item ["data_type"].ToString () : null;
                item.file_name = doc_item ["file_name"] != null ? doc_item ["file_name"].ToString () : null;
                item.export_type = doc_item ["export_type"] != null ? doc_item ["export_type"].ToString () : null;
                item.status = doc_item ["status"] != null ? doc_item ["status"].ToString () : null;

                item.all_or_core = doc_item.ContainsKey("all_or_core") && doc_item ["all_or_core"] != null ? doc_item ["all_or_core"].ToString () : null;
                item.grantee_name = doc_item ["grantee_name"] != null ? doc_item ["grantee_name"].ToString () : null;
                item.is_encrypted = doc_item ["is_encrypted"] != null ? doc_item ["is_encrypted"].ToString () : null;
                item.zip_key = doc_item ["zip_key"] != null ? doc_item ["zip_key"].ToString () : null;
                item.de_identified_selection_type = doc_item ["de_identified_selection_type"] != null ? doc_item ["de_identified_selection_type"].ToString () : null;
                
                item.case_filter_type = doc_item ["case_filter_type"] != null ? doc_item ["case_filter_type"].ToString () : null;

                item.case_file_type = doc_item ["case_file_type"] != null ? doc_item ["case_file_type"].ToString () : null;


                if(doc_item ["de_identified_field_set"] != null)
                {
                    var de_identified_field_set = doc_item ["de_identified_field_set"] as List<object>;
                    if(de_identified_field_set != null)
                    {
                        item.de_identified_field_set = new string[de_identified_field_set.Count];
                        for(var i = 0; i < de_identified_field_set.Count; i++)
                        {
                            item.de_identified_field_set[i] = de_identified_field_set[i].ToString().Replace("-","/");
                        }
                    }
                }
                
                if(doc_item ["case_set"] != null)
                {
                    item.case_set = doc_item ["case_set"] != null ? doc_item ["case_set"] as string[] : null;

                    var case_set = doc_item ["case_set"] as List<object>;
                    if(case_set != null)
                    {
                        item.case_set = new string[case_set.Count];
                        for(var i = 0; i < case_set.Count; i++)
                        {
                            item.case_set[i] = case_set[i].ToString();
                        }
                    }
                }
                


                result.Add (item);
            }
        }

    
        if (result.Count > 0)
        {
            if (result.Count > 1)
            {
                var comparer = Comparer<export_queue_item>.Create
                (
                                    (x, y) => x.date_created.Value.CompareTo (y.date_created.Value) 
                                );

                result.Sort (comparer);
            }

            export_queue_item item_to_process = result [0];


            string get_revision(string p_id)
            {
                var result = new export_queue_item();
                //var get_curl = new cURL ("GET", null, db_config.url + $"/{db_config.prefix}export_queue
                try
                {
                    Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                    settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    string object_string = Newtonsoft.Json.JsonConvert.SerializeObject (item_to_process, settings);
                    var error_curl = new cURL ("GET", null, db_config.url + $"/{db_config.prefix}export_queue/{p_id}", null, scheduleInfoMessage.user_name, scheduleInfoMessage.user_value);

                    var response = error_curl.execute ();
                    result =  Newtonsoft.Json.JsonConvert.DeserializeObject<export_queue_item>(response);
  
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine (ex);
                }

                return result._rev;

            }
            void write_error(export_queue_item i, Exception e)
            {
                var message = e.Message;
                if(message.Length > 100)
                    message = message.Substring(0, 100);

                i.status = $"Export error... {message}";
                i.last_updated_by = "mmria-services";
                i.date_last_updated = DateTime.Now;

                var revision = get_revision(i._id);
                i._rev = revision;
                
                try
                {
                    Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                    settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    string object_string = Newtonsoft.Json.JsonConvert.SerializeObject (item_to_process, settings);
                    var error_curl = new cURL ("PUT", null, db_config.url + $"/{db_config.prefix}export_queue/" + i._id, object_string, scheduleInfoMessage.user_name, scheduleInfoMessage.user_value);

                    //var response = error_curl.execute ();
                    error_curl.execute ();
  
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine (ex);
                }
            }

            item_to_process.date_last_updated = new DateTime?();
            //item_to_process.last_updated_by = g_uid;


            List<string> args = new List<string>();
            args.Add("exporter:exporter");
            args.Add("user_name:" + scheduleInfoMessage.user_name);
            args.Add("password:" + scheduleInfoMessage.user_value);
            args.Add("database_url:" + scheduleInfoMessage.couch_db_url);
            args.Add ("item_file_name:" + item_to_process.file_name);
            args.Add ("item_id:" + item_to_process._id);
            args.Add ("juris_user_name:" + scheduleInfoMessage.jurisdiction_user_name);


            if 
            (
                item_to_process.export_type.StartsWith ("core csv", StringComparison.OrdinalIgnoreCase) ||
                item_to_process.export_type.StartsWith ("core xlsx", StringComparison.OrdinalIgnoreCase)
            )
            {

                item_to_process.status = "Creating Export...";
                item_to_process.last_updated_by = "mmria-services";
                item_to_process.date_last_updated = DateTime.Now;

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string object_string = Newtonsoft.Json.JsonConvert.SerializeObject (item_to_process, settings);
                var set_curl = new cURL ("PUT", null, db_config.url + $"/{db_config.prefix}export_queue/" + item_to_process._id, object_string, scheduleInfoMessage.user_name, scheduleInfoMessage.user_value);

                responseFromServer = set_curl.execute ();

                try
                {
                
                    mmria.services.Utilities.CoreElementExport.core_element_exporter core_element_exporter = new mmria.services.Utilities.CoreElementExport.core_element_exporter(scheduleInfoMessage);
                    core_element_exporter.Execute(item_to_process);
                }
                catch(Exception ex)
                {

                    write_error(item_to_process, ex);
                    System.Console.WriteLine (ex);
                }

            
            }
            else if
            (
                item_to_process.export_type.StartsWith ("all csv", StringComparison.OrdinalIgnoreCase) ||
                item_to_process.export_type.StartsWith ("all xlsx", StringComparison.OrdinalIgnoreCase)
            )
            {
                item_to_process.status = "Creating Export...";
                item_to_process.last_updated_by = "mmria-services";
                item_to_process.date_last_updated = DateTime.Now;

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string object_string = Newtonsoft.Json.JsonConvert.SerializeObject (item_to_process, settings);
                var set_curl = new cURL ("PUT", null, db_config.url + $"/{db_config.prefix}export_queue/" + item_to_process._id, object_string, scheduleInfoMessage.user_name, scheduleInfoMessage.user_value);

                responseFromServer = set_curl.execute ();


                try
                {
                    mmria.services.Utilities.Exporter.mmrds_exporter mmrds_exporter = new mmria.services.Utilities.Exporter.mmrds_exporter(scheduleInfoMessage);
                    if(!mmrds_exporter.Execute(item_to_process))
                    {
                        System.Console.WriteLine ("exporter failed to finish");
                    }
                }
                catch(Exception ex)
                {
                    write_error(item_to_process, ex);
                    System.Console.WriteLine (ex);
                }

            }
            else if (item_to_process.export_type.StartsWith ("cdc csv", StringComparison.OrdinalIgnoreCase)) 
            {


                item_to_process.status = "Creating Export...";
                item_to_process.last_updated_by = "mmria-services";
                item_to_process.date_last_updated = DateTime.Now;

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string object_string = Newtonsoft.Json.JsonConvert.SerializeObject (item_to_process, settings);
                var set_curl = new cURL ("PUT", null, db_config.url + $"/{db_config.prefix}export_queue/" + item_to_process._id, object_string, scheduleInfoMessage.user_name, scheduleInfoMessage.user_value);

                responseFromServer = set_curl.execute ();
                args.Add ("is_cdc_de_identified:true");

                try
                {
                    mmria.services.Utilities.Exporter.mmrds_exporter mmrds_exporter = new mmria.services.Utilities.Exporter.mmrds_exporter (scheduleInfoMessage);
                    //mmrds_exporter.Execute (item_to_process);
                    if(!mmrds_exporter.Execute(item_to_process))
                    {
                        System.Console.WriteLine ("exporter failed to finish");
                    }
                }
                catch(Exception ex)
                {
                    write_error(item_to_process, ex);
                    System.Console.WriteLine (ex);
                }


            }
            else 
            {


                item_to_process.status = "Creating Export...";
                item_to_process.last_updated_by = "mmria-services";
                item_to_process.date_last_updated = DateTime.Now;

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string object_string = Newtonsoft.Json.JsonConvert.SerializeObject (item_to_process, settings);
                var set_curl = new cURL ("PUT", null, db_config.url + $"/{db_config.prefix}export_queue/" + item_to_process._id, object_string, scheduleInfoMessage.user_name, scheduleInfoMessage.user_value);

                responseFromServer = set_curl.execute ();
                args.Add ("is_cdc_de_identified:true");

                try
                {
                    mmria.services.Utilities.Exporter.exporter custom_exporter = new mmria.services.Utilities.Exporter.exporter (scheduleInfoMessage);
                    //mmrds_exporter.Execute (item_to_process);
                    if(!custom_exporter.Execute(item_to_process))
                    {
                        write_error(item_to_process, new Exception("exporter failed to finish"));
                        System.Console.WriteLine ("exporter failed to finish");
                    }
                }
                catch(Exception ex)
                {
                    write_error(item_to_process, ex);
                    System.Console.WriteLine (ex);
                }
            }

        }

    }


    public void Process_Export_Queue_Delete(mmria.server.model.actor.ScheduleInfoMessage scheduleInfoMessage)
    {
        //System.Console.WriteLine ("{0} check_for_changes_job.Process_Export_Queue_Delete: started", System.DateTime.Now);

        List<export_queue_item> result = new List<export_queue_item> ();

        var get_curl = new cURL ("GET", null, db_config.url + $"/{db_config.prefix}export_queue/_all_docs?include_docs=true", null, scheduleInfoMessage.user_name, scheduleInfoMessage.user_value);

        string responseFromServer = get_curl.execute ();

        IDictionary<string,object> response_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (responseFromServer) as IDictionary<string,object>; 
        IList<object> enumerable_rows = null;
        
        if(response_result != null && response_result.ContainsKey("rows"))
        {
            enumerable_rows = response_result ["rows"] as IList<object>;
        }
        

        if(enumerable_rows != null)
        foreach (IDictionary<string,object> enumerable_item in enumerable_rows)
        {
            IDictionary<string,object> doc_item = enumerable_item ["doc"] as IDictionary<string,object>;

            if (
                doc_item != null && 
                doc_item ["status"] != null &&
                doc_item ["status"].ToString ().StartsWith ("Deleted", StringComparison.OrdinalIgnoreCase))
            {
                export_queue_item item = new export_queue_item ();

                item._id = doc_item ["_id"].ToString ();
                item._rev = doc_item ["_rev"].ToString ();
                item._deleted = doc_item.ContainsKey("_deleted") ? doc_item["_deleted"] as bool?: null;
                item.date_created = doc_item ["date_created"] as DateTime?;
                item.created_by = doc_item.ContainsKey("created_by") && doc_item ["created_by"] != null ? doc_item ["created_by"].ToString () : null;
                item.date_last_updated = doc_item ["date_last_updated"] as DateTime?;
                item.last_updated_by = doc_item.ContainsKey("last_updated_by") && doc_item["last_updated_by"] != null ? doc_item ["last_updated_by"].ToString () : null;
                item.data_type = doc_item.ContainsKey("data_type") && doc_item ["data_type"] != null ? doc_item ["data_type"].ToString () : null;
                item.file_name = doc_item ["file_name"] != null ? doc_item ["file_name"].ToString () : null;
                item.export_type = doc_item ["export_type"] != null ? doc_item ["export_type"].ToString () : null;
                item.status = doc_item ["status"] != null ? doc_item ["status"].ToString () : null;

                item.all_or_core =  doc_item.ContainsKey("all_or_core") && doc_item ["all_or_core"] != null ? doc_item ["all_or_core"].ToString () : null;
                item.grantee_name = doc_item.ContainsKey("grantee_name") && doc_item ["grantee_name"] != null ? doc_item ["grantee_name"].ToString () : null;
                item.is_encrypted = doc_item.ContainsKey("is_encrypted") && doc_item ["is_encrypted"] != null ? doc_item ["is_encrypted"].ToString () : null;
                item.zip_key = doc_item.ContainsKey("zip_key") && doc_item ["zip_key"] != null ? doc_item ["zip_key"].ToString () : null;
                item.de_identified_selection_type = doc_item.ContainsKey("de_identified_selection_type") && doc_item ["de_identified_selection_type"] != null ? doc_item ["de_identified_selection_type"].ToString () : null;
                
                item.case_filter_type = doc_item.ContainsKey("case_filter_type") && doc_item ["case_filter_type"] != null ? doc_item ["case_filter_type"].ToString () : null;


                result.Add (item);
            }
        }


        if (result.Count > 0)
        {
            if (result.Count > 1)
            {
                var comparer = Comparer<export_queue_item>.Create
                    (
                        (x, y) => x.date_created.Value.CompareTo (y.date_created.Value) 
                    );

                result.Sort (comparer);
            }

            export_queue_item item_to_process = result [0];

            try
            {
                string item_directory_name = item_to_process.file_name.Substring (0, item_to_process.file_name.LastIndexOf ("."));
                string export_directory = CleanPath.execute(System.IO.Path.Combine (scheduleInfoMessage.export_directory, item_directory_name));

                try
                {
                    if (System.IO.Directory.Exists(export_directory))
                    {
                        System.IO.Directory.Delete(export_directory, true);
                    }
                }
                catch(Exception)
                {
                    // do nothing for now
                    System.Console.WriteLine ("check_for_changes_job.Process_Export_Queue_Delete: Unable to Delete Directory {0}", export_directory);
                }

                string file_path = CleanPath.execute(System.IO.Path.Combine (scheduleInfoMessage.export_directory, item_to_process.file_name));
                try
                {
                    
                    if (System.IO.File.Exists(file_path))
                    {
                        System.IO.File.Delete(file_path);
                    }

                }
                catch(Exception)
                {
                    // do nothing for now
                    System.Console.WriteLine ("Program.Process_Export_Queue_Delete: Unable to Delete File {0}", file_path);
                }

                item_to_process.status = "expunged";
                item_to_process.last_updated_by = "mmria-services";
                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(item_to_process, settings); 
                var set_curl = new cURL ("PUT", null, db_config.url + $"/{db_config.prefix}export_queue/" + item_to_process._id, object_string, scheduleInfoMessage.user_name, scheduleInfoMessage.user_value);

                responseFromServer = get_curl.execute ();
            }
            catch(Exception)
            {
                // do nothing for now
            }

        }

    }

    /*
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                maxNrOfRetries: 0,
                withinTimeRange: TimeSpan.FromMinutes(0),
                localOnlyDecider: OnError
                );
        }

        Directive OnError(Exception ex)
        {
            var result = ex switch
            {
                ArgumentException ae => Directive.Resume,
                NullReferenceException ne => Directive.Restart,
                _ => Directive.Stop
            };
            
            return result;
        }
    */
}
