using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using Akka.Actor;
using mmria.common.SharedLibraries.MMRIAServices.DAL;
using mmria.common.SharedLibraries.MMRIAServices.Helper;
using mmria.common.SharedLibraries.MMRIAServices.Manager;

namespace RecordsProcessor_Worker.Actors;


/*

const mor_max_length = 5000;
const nat_max_length = 4000;
const fet_max_length = 6000;


function validate_length(p_array, p_max_length)
{
    let result = true;

    for(let i = 0; i < p_array.length; i++)
    {
        let item = p_array[i];
        if(item.l != p_max_length)
        {
            result = false;
            break;
        }
    }

    return result;
}

*/

public sealed class BatchProcessor : ReceiveActor
{
    string _id;
    private int my_count = -1;
    const int mor_max_length = 5000;
    const int nat_max_length = 4000;
    const int fet_max_length = 6000;

    HashSet<string> g_cdc_identifier_set = new();

    IConfiguration configuration;
    ILogger logger;
    mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    MMRIAServicesManager _mmriaServicesManager;

    mmria.common.couchdb.DBConfigurationDetail item_db_info;

    private IActorRef batchItemRouter;
    private int pending_items = 0;
    
    // Chunk-based processing fields
    private int _chunkSize = 10; // Default chunk size
    private List<KeyValuePair<string, (string, mmria.common.ije.BatchItem)>> _remainingItems = new();
    private int _currentChunkPending = 0;
    private mmria.common.ije.NewIJESet_Message _currentMessage;
    private string[] _nat_list;
    private string[] _fet_list;
    private string _reportingState;
    private DateTime _importDate;

    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");

    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(
            maxNrOfRetries: 3,
            withinTimeRange: TimeSpan.FromMinutes(1),
            localOnlyDecider: ex =>
            {
                Console.WriteLine($"BatchItemProcessor error: {ex.GetType().Name} - {ex.Message}");
                return Directive.Restart;
            });
    }

    private Dictionary<string, (string, mmria.common.ije.BatchItem)> batch_item_set = new (StringComparer.OrdinalIgnoreCase);

    private mmria.common.ije.Batch batch;
    public BatchProcessor(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
        _mmriaServicesManager = new MMRIAServicesManager(new MMRIAServicesDAL(_couchDbHttpClient), _couchDbHttpClient);
        // Create router pool with 5 workers for bounded parallelism
        batchItemRouter = Context.ActorOf(
            Props.Create<RecordsProcessor_Worker.Actors.BatchItemProcessor>(_couchDbHttpClient)
                .WithRouter(new Akka.Routing.RoundRobinPool(5)),
            "batch-item-router"
        );

        ReceiveAsync<mmria.common.ije.NewIJESet_Message>(async message =>
        {
            try
            {
                Console.WriteLine("BatchProcessor: Received NewIJESet_Message");
                await Process_Message(message);
                Console.WriteLine("BatchProcessor: Completed Processing NewIJESet_Message");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BatchProcessor: Error processing NewIJESet_Message: {ex}");
            }
        });

        ReceiveAsync<mmria.common.ije.BatchItem>(async message =>
        {

            await Process_Message(message);
        });

        ReceiveAsync<mmria.common.ije.BatchItemComplete>(async message =>
        {
            pending_items--;
            _currentChunkPending--;
            
            Console.WriteLine($"BatchItem completed. Total pending: {pending_items}, Chunk pending: {_currentChunkPending}, Remaining: {_remainingItems.Count}");
            
            // When current chunk completes, dispatch next chunk
            if (_currentChunkPending == 0 && _remainingItems.Count > 0)
            {
                DispatchNextChunk();
            }
            
            // Finalize when all items complete
            if (pending_items == 0 && batch != null)
            {
                await Finalize_Batch();
            }
        });
        
        ReceiveAsync<mmria.common.ije.BatchRemoveDataMessage>(async message =>
        {
            await Process_Message(message);
        });
    }
    public BatchProcessor(string p_id):base()
    {
        _id = p_id;
        //IConfiguration p_configuration
        //configuration = p_configuration;
        //logger = p_logger;


        

        
    }
    private async System.Threading.Tasks.Task Process_Message(mmria.common.ije.NewIJESet_Message message)
    {
        mmria.common.couchdb.ConfigurationSet db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;
        var initialization = MMRIAServicesHelper.InitializeBatchImport(
            message,
            db_config_set,
            mor_max_length,
            nat_max_length,
            fet_max_length,
            mmria.services.vitalsimport.Program.vitals_import_additional_tenants);
        var mor_set = initialization.MorSet;
        var status_builder = initialization.StatusBuilder;
        var is_valid_file_name = initialization.IsValidFileName;
        var ReportingState = initialization.ReportingState;
        var ImportDate = initialization.ImportDate;
        item_db_info = initialization.ItemDbInfo;

        

        string[] nat_list = initialization.NatSet;
        string[] fet_list = initialization.FetSet;

        if(nat_list == null)
        {
            nat_list = new string[0];
        }

        if(fet_list == null)
        {
            fet_list = new string[0];
        }
        
        var duplicate_count = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var duplicate_is_found = false;

        if(status_builder.Length == 0)
        {
            var duplicate_check = await _mmriaServicesManager.CheckForVitalImportBatchDuplicates(
                mor_set,
                mor_max_length,
                ImportDate,
                message.mor_file_name,
                ReportingState,
                item_db_info,
                batch_item_set,
                g_cdc_identifier_set);
            duplicate_count = duplicate_check.duplicate_count;
            duplicate_is_found = duplicate_check.duplicate_is_found;
        }
        

        if(duplicate_is_found)
        {
            status_builder.AppendLine("Invalid batch duplicates were found:");
            foreach(var kvp in duplicate_count)
            {
                if(kvp.Value > 1)
                {
                    status_builder.AppendLine($"duplicate MOR CDC identifier occurrence count: {kvp.Value}");
                }
            }
        }


        foreach(var item in MMRIAServicesHelper.validate_AssociatedNAT(nat_list, g_cdc_identifier_set))
            status_builder.AppendLine(item);

        foreach(var item in MMRIAServicesHelper.validate_AssociatedFET(fet_list, g_cdc_identifier_set))
            status_builder.AppendLine(item);

        if(status_builder.Length == 0)
        {
            // Store message and items for chunked processing
            _currentMessage = message;
            _remainingItems = batch_item_set.ToList();
            _nat_list = nat_list;
            _fet_list = fet_list;
            _reportingState = ReportingState;
            _importDate = ImportDate;
            pending_items = batch_item_set.Count;

            batch = new mmria.common.ije.Batch()
            {
                id = message.batch_id,
                date_created  = DateTime.UtcNow,
                created_by = "vital-import",
                date_last_updated   = DateTime.UtcNow,
                last_updated_by = "vital-import", 
                Status = mmria.common.ije.Batch.StatusEnum.Validating,
                reporting_state = ReportingState,
                ImportDate = ImportDate,
                mor_file_name = message.mor_file_name,
                nat_file_name = message.nat_file_name,
                fet_file_name = message.fet_file_name,
                StatusInfo = status_builder.ToString(),
                record_result = MMRIAServicesHelper.ConvertBatchItemDictionaryToList(batch_item_set)

            };

            var BatchStatusMessage = new mmria.common.ije.BatchStatusMessage()
            {
                id = batch.id,
                status = batch.Status
            };
            Context.ActorSelection("akka://mmria-actor-system/user/batch-supervisor").Tell(BatchStatusMessage);
            
            // Save batch immediately so it's visible for tracking
            var save_batch_result = await _mmriaServicesManager.save_batch(
                batch,
                batch,
                mmria.services.vitalsimport.Program.couchdb_url,
                mmria.services.vitalsimport.Program.timer_user_name,
                mmria.services.vitalsimport.Program.timer_value
            );
            if(save_batch_result.result)
            {
                batch = save_batch_result.updated_batch;
            }

            Console.WriteLine($"Starting chunked batch processing with {pending_items} items (chunk size: {_chunkSize})");

            if (pending_items == 0)
            {
                await Finalize_Batch();
            }
            else
            {
                DispatchNextChunk();
            }
        }
        else
        {
            
            batch = new mmria.common.ije.Batch()
            {
                id = message.batch_id,
                date_created  = DateTime.UtcNow,
                created_by = "vital-import",
                date_last_updated   = DateTime.UtcNow,
                last_updated_by = "vital-import", 
                Status = mmria.common.ije.Batch.StatusEnum.BatchRejected,
                reporting_state = ReportingState,
                ImportDate = ImportDate,
                mor_file_name = message.mor_file_name,
                nat_file_name = message.nat_file_name,
                fet_file_name = message.fet_file_name,
                StatusInfo = status_builder.ToString(),
                record_result = MMRIAServicesHelper.ConvertBatchItemDictionaryToList(batch_item_set)

            };

            var BatchStatusMessage = new mmria.common.ije.BatchStatusMessage()
            {
                id = batch.id,
                status = batch.Status
            };
            Context.ActorSelection("akka://mmria-actor-system/user/batch-supervisor").Tell(BatchStatusMessage);

            var save_batch_result = await _mmriaServicesManager.save_batch(
                batch,
                batch,
                mmria.services.vitalsimport.Program.couchdb_url,
                mmria.services.vitalsimport.Program.timer_user_name,
                mmria.services.vitalsimport.Program.timer_value
            );
            if(save_batch_result.result)
            {
                batch = save_batch_result.updated_batch;
            }

            Context.Stop(this.Self);

        }

        

        
        
    }

    private void DispatchNextChunk()
    {
        var itemsToDispatch = _remainingItems.Take(_chunkSize).ToList();
        
        if (itemsToDispatch.Count == 0)
            return;

        _currentChunkPending = itemsToDispatch.Count;
        _remainingItems = _remainingItems.Skip(_chunkSize).ToList();

        Console.WriteLine($"Dispatching chunk: {itemsToDispatch.Count} items, {_remainingItems.Count} remaining");

        foreach (var kvp in itemsToDispatch)
        {
            var batch_tuple = kvp.Value;
            try
            {
                var StartBatchItemMessage = new mmria.common.ije.StartBatchItemMessage()
                {
                    case_folder = _currentMessage.case_folder,
                    cdc_unique_id = batch_tuple.Item2.CDCUniqueID,
                    record_id = batch_tuple.Item2.mmria_record_id,
                    ImportDate = _importDate,
                    ImportFileName = _currentMessage.mor_file_name,
                    host_state = _reportingState,
                    mor = batch_tuple.Item1,
                    nat = MMRIAServicesHelper.GetAssociatedNat(_nat_list, batch_tuple.Item2.CDCUniqueID?.Trim()),
                    fet = MMRIAServicesHelper.GetAssociatedFet(_fet_list, batch_tuple.Item2.CDCUniqueID?.Trim()),
                    BatchProcessorPath = Self.Path.ToStringWithAddress()
                };

                batchItemRouter.Tell(StartBatchItemMessage);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error queueing batch item: {ex.Message}");
                pending_items--;
                _currentChunkPending--;
            }
        }
    }

    private async System.Threading.Tasks.Task Finalize_Batch()
    {
        Console.WriteLine($"Finalizing batch {batch.id}");
        
        // Convert batch_item_set to final record results
        var final_record_result = MMRIAServicesHelper.ConvertBatchItemDictionaryToList(batch_item_set);
        
        // Update batch with final status and results
        var finalBatch = new mmria.common.ije.Batch()
        {
            id = batch.id,
            _rev = batch._rev,
            date_created = batch.date_created,
            created_by = batch.created_by,
            date_last_updated = DateTime.UtcNow,
            last_updated_by = batch.last_updated_by,
            Status = mmria.common.ije.Batch.StatusEnum.Finished,
            reporting_state = batch.reporting_state,
            ImportDate = batch.ImportDate,
            mor_file_name = batch.mor_file_name,
            nat_file_name = batch.nat_file_name,
            fet_file_name = batch.fet_file_name,
            StatusInfo = $"Completed {final_record_result.Count} items",
            record_result = final_record_result
        };
        
        batch = finalBatch;
        
        var save_batch_result = await _mmriaServicesManager.save_batch(
            batch,
            batch,
            mmria.services.vitalsimport.Program.couchdb_url,
            mmria.services.vitalsimport.Program.timer_user_name,
            mmria.services.vitalsimport.Program.timer_value
        );

        if(save_batch_result.result)
        {
            batch = save_batch_result.updated_batch;
            Console.WriteLine($"Batch {batch.id} saved successfully with {batch.record_result.Count} results");
            
            var BatchStatusMessage = new mmria.common.ije.BatchStatusMessage()
            {
                id = batch.id,
                status = batch.Status
            };
            Context.ActorSelection("akka://mmria-actor-system/user/batch-supervisor").Tell(BatchStatusMessage);
        }
        else
        {
            Console.WriteLine($"Failed to save batch {batch.id}");
        }
        
        Context.Stop(this.Self);
    }

    private async System.Threading.Tasks.Task Process_Message(mmria.common.ije.BatchItem message)
    {
        var new_item = (batch_item_set[message.CDCUniqueID].Item1, message);
        batch_item_set[message.CDCUniqueID] = new_item;

        var current_status = batch.Status;
        int finished_count = 0;

        foreach(var item in batch_item_set)
        {
            if
            (
                item.Value.Item2.Status == mmria.common.ije.BatchItem.StatusEnum.NewCaseAdded ||
                item.Value.Item2.Status == mmria.common.ije.BatchItem.StatusEnum.ExistingCaseSkipped ||
                item.Value.Item2.Status == mmria.common.ije.BatchItem.StatusEnum.ImportFailed 
            )
            {
                finished_count += 1;
            }
        }          

        if(finished_count == batch_item_set.Count)
        {
            current_status = mmria.common.ije.Batch.StatusEnum.Finished;
        }

        var new_batch = new mmria.common.ije.Batch()
        {
            id = batch.id,
            _rev = batch._rev, 
            date_created  = batch.date_created,
            created_by = batch.created_by,
            date_last_updated  = DateTime.UtcNow,
            last_updated_by = batch.last_updated_by, 
            Status = current_status,
            reporting_state = batch.reporting_state,
            ImportDate = batch.ImportDate,
            mor_file_name = batch.mor_file_name,
            nat_file_name = batch.nat_file_name,
            fet_file_name = batch.fet_file_name,
            StatusInfo = batch.StatusInfo,
            record_result = MMRIAServicesHelper.ConvertBatchItemDictionaryToList(batch_item_set)

        };

        batch = new_batch;


        var BatchStatusMessage = new mmria.common.ije.BatchStatusMessage()
        {
            id = batch.id,
            status = batch.Status
        };
        Context.ActorSelection("akka://mmria-actor-system/user/batch-supervisor").Tell(BatchStatusMessage);

        if
        (
            current_status == mmria.common.ije.Batch.StatusEnum.Finished ||
            current_status == mmria.common.ije.Batch.StatusEnum.BatchRejected
        )
        {
            var save_batch_result = await _mmriaServicesManager.save_batch(
                batch,
                batch,
                mmria.services.vitalsimport.Program.couchdb_url,
                mmria.services.vitalsimport.Program.timer_user_name,
                mmria.services.vitalsimport.Program.timer_value
            );
            if(save_batch_result.result)
            {
                batch = save_batch_result.updated_batch;
            }
            Context.Stop(this.Self);
        }

        
    }
    private async System.Threading.Tasks.Task Process_Message(mmria.common.ije.BatchRemoveDataMessage message)
    {
        var config_timer_user_name = mmria.services.vitalsimport.Program.timer_user_name;
        var config_timer_value = mmria.services.vitalsimport.Program.timer_value;

        var config_couchdb_url = mmria.services.vitalsimport.Program.couchdb_url;
        var db_prefix = "";

        var  batch = await _mmriaServicesManager.Get_batch(
            mmria.services.vitalsimport.Program.couchdb_url,
            mmria.services.vitalsimport.Program.timer_user_name,
            mmria.services.vitalsimport.Program.timer_value,
            message.id
        );

        mmria.common.couchdb.ConfigurationSet db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;
        item_db_info = db_config_set.detail_list[batch.reporting_state];
        
        if(batch.Status != mmria.common.ije.Batch.StatusEnum.BatchRejected)
        {
            foreach(var item in batch.record_result)
            {
                // remove from db

                try
                {
                    string request_string = $"{item_db_info.url}/{item_db_info.prefix}mmrds/_all_docs?include_docs=true";

                    var case_id = item.mmria_id;

                    var case_expando = await _mmriaServicesManager.GetCaseById(item_db_info, case_id);
                    var rev_dynamic = ((IDictionary<string,object>)case_expando)["_rev"];
                    string rev = null;
                    if(rev_dynamic != null)
                    {
                        rev = rev_dynamic.ToString();
                    }

                    if (!string.IsNullOrWhiteSpace (case_id) && !string.IsNullOrWhiteSpace(rev)) 
                    {
                        request_string = $"{item_db_info.url}/{item_db_info.prefix}mmrds/{case_id}?rev={rev}";
                        string responseFromServer = await _couchDbHttpClient.ExecuteAsync("DELETE", request_string, null, item_db_info.user_name, item_db_info.user_value);

                        // to do synchronize
                    } 

                }
                catch(Exception ex)
                {
                    Console.WriteLine (ex);
                } 

            }
        }

        await _mmriaServicesManager.delete_batch_document(
            mmria.services.vitalsimport.Program.couchdb_url,
            mmria.services.vitalsimport.Program.timer_user_name,
            mmria.services.vitalsimport.Program.timer_value,
            message.id
        );

    }
}

   

