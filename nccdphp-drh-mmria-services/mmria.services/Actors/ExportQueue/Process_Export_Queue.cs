using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using mmria.common.getset;
using mmria.common.SharedLibraries.ExportQueue;
using mmria.services.Models;
using mmria.services.Utilities;

namespace mmria.services.ExportQueue;

public sealed class Process_Export_Queue : ReceiveActor
{
    //protected override void PreStart() => Console.WriteLine("Process_Export_Queue started");
    //protected override void PostStop() => Console.WriteLine("Process_Export_Queue stopped");

	mmria.common.couchdb.DBConfigurationDetail db_config = null;
    mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly IExportQueueRepository _exportQueueRepository;

    public Process_Export_Queue
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        IExportQueueRepository exportQueueRepository
    )
    {
        db_config = _db_config;
        _couchDbHttpClient = couchDbHttpClient;
        _exportQueueRepository = exportQueueRepository;

        ReceiveAsync<ScheduleInfoMessage>(async scheduleInfoMessage =>
        {
            //Console.WriteLine($"Process_Export_Queue {System.DateTime.Now}");

            //System.Console.WriteLine ("{0} Beginning Export Queue Item Processing", System.DateTime.Now);
            System.Console.WriteLine($"[EXPORT-QUEUE] actor start url='{db_config.url}' prefix='{db_config.prefix}'");
            var __export_queue_sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await Process_Export_Queue_Item (scheduleInfoMessage);
            }
            catch(Exception ex)
            {
                // to nothing for now
                System.Console.WriteLine ("[EXPORT-QUEUE] error url='{0}' prefix='{1}' Process_Export_Queue_Item: {2}", db_config.url, db_config.prefix, ex);

            }

            try
            {
                Process_Export_Queue_Delete (scheduleInfoMessage);
            }
            catch(Exception ex)
            {
                // to nothing for now
                System.Console.WriteLine ("[EXPORT-QUEUE] error url='{0}' prefix='{1}' Process_Export_Queue_Delete: {2}", db_config.url, db_config.prefix, ex);

            }

            System.Console.WriteLine($"[EXPORT-QUEUE] tick complete url='{db_config.url}' prefix='{db_config.prefix}' elapsed_ms={__export_queue_sw.ElapsedMilliseconds}");

            Context.Stop(this.Self);
        });
    }

    private static bool HasStringValue(IDictionary<string, object> document, string key)
    {
        return !string.IsNullOrWhiteSpace(GetOptionalString(document, key));
    }

    private static string GetOptionalString(IDictionary<string, object> document, string key)
    {
        if
        (
            document == null ||
            !document.ContainsKey(key) ||
            document[key] == null
        )
        {
            return null;
        }

        return document[key].ToString();
    }

    private static DateTime? GetOptionalDateTime(IDictionary<string, object> document, string key)
    {
        if
        (
            document == null ||
            !document.ContainsKey(key) ||
            document[key] == null
        )
        {
            return null;
        }

        if (document[key] is DateTime dateTime)
        {
            return dateTime;
        }

        if (DateTime.TryParse(document[key].ToString(), out var parsedDateTime))
        {
            return parsedDateTime;
        }

        return null;
    }

    private static string[] GetOptionalStringArray(IDictionary<string, object> document, string key, bool replaceHyphenWithSlash = false)
    {
        if
        (
            document == null ||
            !document.ContainsKey(key) ||
            document[key] == null
        )
        {
            return null;
        }

        if (document[key] is string[] stringArray)
        {
            return stringArray
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => replaceHyphenWithSlash ? item.Replace("-", "/") : item)
                .ToArray();
        }

        if (document[key] is IList<object> objectList)
        {
            return objectList
                .Where(item => item != null)
                .Select(item =>
                {
                    var value = item.ToString();
                    return replaceHyphenWithSlash ? value.Replace("-", "/") : value;
                })
                .ToArray();
        }

        return null;
    }

    private static export_queue_item CreateQueueItemFromDocument(IDictionary<string, object> document, bool requireExportType)
    {
        var missingRequiredFields = new List<string>();
        var id = GetOptionalString(document, "_id");
        var revision = GetOptionalString(document, "_rev");
        var fileName = GetOptionalString(document, "file_name");
        var exportType = GetOptionalString(document, "export_type");

        if (string.IsNullOrWhiteSpace(id))
        {
            missingRequiredFields.Add("_id");
        }

        if (string.IsNullOrWhiteSpace(revision))
        {
            missingRequiredFields.Add("_rev");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            missingRequiredFields.Add("file_name");
        }

        if
        (
            requireExportType &&
            string.IsNullOrWhiteSpace(exportType)
        )
        {
            missingRequiredFields.Add("export_type");
        }

        if (missingRequiredFields.Count > 0)
        {
            LogMalformedQueueDocument(id, missingRequiredFields);
            return null;
        }

        return new export_queue_item
        {
            _id = id,
            _rev = revision,
            _deleted = document.ContainsKey("_deleted") ? document["_deleted"] as bool? : null,
            date_created = GetOptionalDateTime(document, "date_created"),
            created_by = GetOptionalString(document, "created_by"),
            date_last_updated = GetOptionalDateTime(document, "date_last_updated"),
            last_updated_by = GetOptionalString(document, "last_updated_by"),
            data_type = GetOptionalString(document, "data_type"),
            file_name = fileName,
            export_type = exportType,
            status = GetOptionalString(document, "status"),
            all_or_core = GetOptionalString(document, "all_or_core"),
            grantee_name = GetOptionalString(document, "grantee_name"),
            is_encrypted = GetOptionalString(document, "is_encrypted"),
            zip_key = GetOptionalString(document, "zip_key"),
            de_identified_selection_type = GetOptionalString(document, "de_identified_selection_type"),
            de_identified_field_set = GetOptionalStringArray(document, "de_identified_field_set", replaceHyphenWithSlash: true),
            case_filter_type = GetOptionalString(document, "case_filter_type"),
            case_file_type = GetOptionalString(document, "case_file_type"),
            case_set = GetOptionalStringArray(document, "case_set")
        };
    }

    private static void LogMalformedQueueDocument(string documentId, IEnumerable<string> missingRequiredFields)
    {
        System.Console.WriteLine(
            "check_for_changes_job.Process_Export_Queue: Skipping malformed export_queue document {0}. Missing required field(s): {1}",
            string.IsNullOrWhiteSpace(documentId) ? "(missing _id)" : documentId,
            string.Join(", ", missingRequiredFields));
    }


    public async System.Threading.Tasks.Task Process_Export_Queue_Item (ScheduleInfoMessage scheduleInfoMessage)
    {
        //System.Console.WriteLine ("{0} check_for_changes_job.Process_Export_Queue_Item: started", System.DateTime.Now);

        List<export_queue_item> result = new List<export_queue_item> ();

        IDictionary<string,object> response_result;
        response_result = (IDictionary<string,object>)(await _exportQueueRepository.GetAllQueueDocumentsAsync(db_config));
        IList<object> enumerable_rows = null;
        
        if(response_result != null && response_result.ContainsKey("rows"))
        {
            enumerable_rows = response_result ["rows"] as IList<object>;
        }

        if(enumerable_rows != null)
        foreach (IDictionary<string,object> enumerable_item in enumerable_rows)
        {
            IDictionary<string,object> doc_item =
                enumerable_item.ContainsKey("doc")
                    ? enumerable_item ["doc"] as IDictionary<string,object>
                    : null;
            var status = GetOptionalString(doc_item, "status");
            var dataType = GetOptionalString(doc_item, "data_type");
    
            if 
            (

                doc_item != null &&
                HasStringValue(doc_item, "status") &&
                string.Equals(dataType, "export", StringComparison.OrdinalIgnoreCase) &&
                status.StartsWith("In Queue...", StringComparison.OrdinalIgnoreCase)
            )
            {
                var item = CreateQueueItemFromDocument(doc_item, requireExportType: true);
                if(item != null)
                {
                    result.Add (item);
                }
            }
        }

    
        if (result.Count > 0)
        {
            System.Console.WriteLine($"[EXPORT-QUEUE] processing {result.Count} item(s) url='{db_config.url}' prefix='{db_config.prefix}' first_id='{result[0]._id}'");

            if (result.Count > 1)
            {
                var comparer = Comparer<export_queue_item>.Create
                (
                                    (x, y) => x.date_created.Value.CompareTo (y.date_created.Value) 
                                );

                result.Sort (comparer);
            }

            export_queue_item item_to_process = result [0];


            async System.Threading.Tasks.Task<string> get_revision(string p_id)
            {
                var result = new export_queue_item();
                //var get_curl = new cURL ("GET", null, db_config.url + $"/{db_config.prefix}export_queue
                try
                {
                    Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                    settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    string object_string = Newtonsoft.Json.JsonConvert.SerializeObject (item_to_process, settings);
                    result = await _exportQueueRepository.GetQueueDocumentAsync<export_queue_item>(p_id, db_config);
  
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine (ex);
                }

                return result._rev;

            }
            async System.Threading.Tasks.Task write_error(export_queue_item i, Exception e)
            {
                var message = e.Message;
                if(message.Length > 100)
                    message = message.Substring(0, 100);

                i.status = $"Export error... {message}";
                i.last_updated_by = "mmria-services";
                i.date_last_updated = DateTime.Now;

                var revision = await get_revision(i._id);
                i._rev = revision;
                
                try
                {
                    Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                    settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    string object_string = Newtonsoft.Json.JsonConvert.SerializeObject (item_to_process, settings);
                    await _exportQueueRepository.SaveQueueDocumentAsync(i._id, object_string, db_config);
  
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

                await _exportQueueRepository.SaveQueueDocumentAsync(item_to_process._id, object_string, db_config);

                try
                {
                
                    mmria.services.Utilities.CoreElementExport.core_element_exporter core_element_exporter = new mmria.services.Utilities.CoreElementExport.core_element_exporter(scheduleInfoMessage, _couchDbHttpClient, _exportQueueRepository);
                    await core_element_exporter.Execute(item_to_process);
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

                await _exportQueueRepository.SaveQueueDocumentAsync(item_to_process._id, object_string, db_config);


                try
                {
                    mmria.services.Utilities.Exporter.mmrds_exporter mmrds_exporter = new mmria.services.Utilities.Exporter.mmrds_exporter(scheduleInfoMessage, _couchDbHttpClient, _exportQueueRepository);
                    if(!await mmrds_exporter.Execute(item_to_process))
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

                await _exportQueueRepository.SaveQueueDocumentAsync(item_to_process._id, object_string, db_config);
                args.Add ("is_cdc_de_identified:true");

                try
                {
                    mmria.services.Utilities.Exporter.mmrds_exporter mmrds_exporter = new mmria.services.Utilities.Exporter.mmrds_exporter (scheduleInfoMessage, _couchDbHttpClient, _exportQueueRepository);
                    //mmrds_exporter.Execute (item_to_process);
                    if(!await mmrds_exporter.Execute(item_to_process))
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

                await _exportQueueRepository.SaveQueueDocumentAsync(item_to_process._id, object_string, db_config);
                args.Add ("is_cdc_de_identified:true");

                try
                {
                    mmria.services.Utilities.Exporter.exporter custom_exporter = new mmria.services.Utilities.Exporter.exporter (scheduleInfoMessage, _couchDbHttpClient, _exportQueueRepository);
                    //mmrds_exporter.Execute (item_to_process);
                    if(!await custom_exporter.Execute(item_to_process))
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


    public async System.Threading.Tasks.Task Process_Export_Queue_Delete (ScheduleInfoMessage scheduleInfoMessage)
    {
        //System.Console.WriteLine ("{0} check_for_changes_job.Process_Export_Queue_Delete: started", System.DateTime.Now);

        List<export_queue_item> result = new List<export_queue_item> ();

        IDictionary<string,object> response_result;
        response_result = (IDictionary<string,object>)(await _exportQueueRepository.GetAllQueueDocumentsAsync(db_config));
        IList<object> enumerable_rows = null;
        
        if(response_result != null && response_result.ContainsKey("rows"))
        {
            enumerable_rows = response_result ["rows"] as IList<object>;
        }
        

        if(enumerable_rows != null)
        foreach (IDictionary<string,object> enumerable_item in enumerable_rows)
        {
            IDictionary<string,object> doc_item =
                enumerable_item.ContainsKey("doc")
                    ? enumerable_item ["doc"] as IDictionary<string,object>
                    : null;
            var status = GetOptionalString(doc_item, "status");

            if (
                doc_item != null && 
                HasStringValue(doc_item, "status") &&
                status.StartsWith ("Deleted", StringComparison.OrdinalIgnoreCase))
            {
                var item = CreateQueueItemFromDocument(doc_item, requireExportType: false);
                if(item != null)
                {
                    result.Add (item);
                }
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
                var validated_file_name = PathSanitizer.ValidatePathSegment(item_to_process.file_name, nameof(item_to_process.file_name));
                string item_directory_name = System.IO.Path.GetFileNameWithoutExtension(validated_file_name);
                string export_directory = System.IO.Path.Combine(scheduleInfoMessage.export_directory, item_directory_name);

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

                string file_path = System.IO.Path.Combine(scheduleInfoMessage.export_directory, validated_file_name);
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
                await _exportQueueRepository.SaveQueueDocumentAsync(item_to_process._id, object_string, db_config);
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
