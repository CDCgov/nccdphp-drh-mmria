using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using Akka.Actor;
using mmria.common.SharedLibraries.MMRIAServices.DAL;
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
    const int mor_max_length = 5001;
    const int nat_max_length = 4001;
    const int fet_max_length = 6001;

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
        _mmriaServicesManager = new MMRIAServicesManager(new MMRIAServicesDAL(_couchDbHttpClient));
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
        Console.WriteLine($"Process_Message started");
        Console.WriteLine($"Processing Message : {message}");
        Console.WriteLine($"MOR length: {message?.mor?.Length ?? 0}, NAT length: {message?.nat?.Length ?? 0}, FET length: {message?.fet?.Length ?? 0}");

        

        var mor_set = message.mor.Split("\n");
        Console.WriteLine($"MOR lines: {mor_set?.Length ?? 0}");

        var status_builder = new System.Text.StringBuilder();

        var is_valid_file_name = false;
        Console.WriteLine("Validating lengths");

        var mor_length_is_valid = validate_length(message?.mor?.Split("\n"), mor_max_length);
        var nat_length_is_valid = validate_length(message?.nat?.Split("\n"), nat_max_length);
        var fet_length_is_valid = validate_length(message?.fet?.Split("\n"), fet_max_length);

        Console.WriteLine("Checking file names");

        var test_tenants = new string[] {"tenant1","tenant2","tenant3","tenant4","tenant5"}; 
        var qa_tenants = new string[] {"tenant1qa","tenant2qa","tenant3qa","tenant4qa","tenant5qa"}; 

        // Check QA tenants FIRST (more specific)
        if (qa_tenants.Any(t => message.mor_file_name.ToLower().Contains(t)))
        {
            var patt = new System.Text.RegularExpressions.Regex("^[0-9]{4}_20[0-9]{2}_[0-2][0-9]_[0-3][0-9]_(tenant[1-5]qa).[mM][oO][rR]$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!patt.IsMatch(message.mor_file_name)) 
            {
                status_builder.AppendLine("mor file name format incorrect. File name must be in ####_20##_Year_Month_Day_TENANT[1-5]QA format. (e.g. 2026_2026_01_18_TENANT2QA.MOR)");
            }
        }
        else if (test_tenants.Any(t => message.mor_file_name.ToLower().Contains(t)))
        {
            var patt = new System.Text.RegularExpressions.Regex("^[0-9]{4}_20[0-9]{2}_[0-2][0-9]_[0-3][0-9]_(tenant[1-5]).[mM][oO][rR]$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!patt.IsMatch(message.mor_file_name)) 
            {
                status_builder.AppendLine("mor file name format incorrect. File name must be in ####_20##_Year_Month_Day_TENANT[1-5] format. (e.g. 2026_2026_01_18_TENANT2.MOR)");
            }
        }        
        else // Regular state codes (remove the duplicate qa_tenants check on line 159)
        {
            var patt = new System.Text.RegularExpressions.Regex("^[0-9]{4}_20[0-9]{2}_[0-2][0-9]_[0-3][0-9]_[A-Z]{2,9}.[mM][oO][rR]$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!patt.IsMatch(message.mor_file_name)) 
            {
                status_builder.AppendLine("mor file name format incorrect. File name must be in ####_20##_Year_Month_Day_StateCode format. (e.g. 2020_2021_01_01_KS.mor)");
            }
        }

        if(!mor_length_is_valid) status_builder.AppendLine("mor length is invalid.");
        if(!nat_length_is_valid) status_builder.AppendLine("nat length is invalid.");
        if(!fet_length_is_valid) status_builder.AppendLine("fet length is invalid.");


        var ReportingState = get_state_from_file_name(message.mor_file_name);
        var ImportDate = DateTime.Now;
        Console.WriteLine($"ReportingState: {ReportingState}");
        
        mmria.common.couchdb.ConfigurationSet db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;
        if(db_config_set.detail_list.ContainsKey(ReportingState))
        {
            is_valid_file_name = true;
            
            item_db_info = db_config_set.detail_list[ReportingState];
        }
        else
        {
            status_builder.AppendLine($"Invalid reporting state {ReportingState}");
        }

        

        string[] nat_list = message?.nat?.Split("\n");
        string[] fet_list = message?.fet?.Split("\n");

        if(nat_list == null)
        {
            nat_list = new string[0];
        }

        if(fet_list == null)
        {
            fet_list = new string[0];
        }
        
        var duplicate_count = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        var duplicate_is_found = false;




        HashSet<string> ExistingRecordIds = null;
        if(ExistingRecordIds == null)
        {
            Console.WriteLine("Getting existing record IDs");
            ExistingRecordIds = await _mmriaServicesManager.GetExistingRecordIds(item_db_info);
            Console.WriteLine($"Found {ExistingRecordIds?.Count ?? 0} existing records");
        }

        Console.WriteLine("Processing MOR records");
        foreach(var row in mor_set)
        {
            if(row.Length == mor_max_length)
            {
                var batch_item = Convert(row, ImportDate, message.mor_file_name, ReportingState, ExistingRecordIds);

                string record_id;

                if(batch_item_set.ContainsKey(batch_item.CDCUniqueID))
                {
                    duplicate_is_found = true;
                    duplicate_count[batch_item.CDCUniqueID]+= 1;
                    continue;
                }

                g_cdc_identifier_set.Add(batch_item.CDCUniqueID?.Trim());

                batch_item_set.Add(batch_item.CDCUniqueID?.Trim(), (row, batch_item));
                duplicate_count[batch_item.CDCUniqueID] = 1;

    
            }
        }
        

        if(duplicate_is_found)
        {
            status_builder.AppendLine("Invalid batch duplicates were found:");
            foreach(var kvp in duplicate_count)
            {
                if(kvp.Value > 1)
                {
                    status_builder.AppendLine($"duplicate {kvp.Key}: {kvp.Value}");
                }
            }
        }


        foreach(var item in validate_AssociatedNAT(nat_list))
            status_builder.AppendLine(item);

        foreach(var item in validate_AssociatedFET(nat_list))
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
            
            Console.WriteLine($"Starting chunked batch processing with {pending_items} items (chunk size: {_chunkSize})");
            
            // Dispatch first chunk
            DispatchNextChunk();


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
                record_result = Convert(batch_item_set)

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
            
            // Batch finalization will happen when all items complete
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
                record_result = Convert(batch_item_set)

            };

            var BatchStatusMessage = new mmria.common.ije.BatchStatusMessage()
            {
                id = batch.id,
                status = batch.Status
            };
            Context.ActorSelection("akka://mmria-actor-system/user/batch-supervisor").Tell(BatchStatusMessage);
    

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
                    nat = GetAssociatedNat(_nat_list, batch_tuple.Item2.CDCUniqueID?.Trim()),
                    fet = GetAssociatedFet(_fet_list, batch_tuple.Item2.CDCUniqueID?.Trim()),
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
        var final_record_result = Convert(batch_item_set);
        
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

    private List<mmria.common.ije.BatchItem> Convert(Dictionary<string,(string, mmria.common.ije.BatchItem)> p_val)
    {
        List<mmria.common.ije.BatchItem> result = new();

        foreach(var kvp in p_val)
        {
            result.Add(kvp.Value.Item2);
        }

        return result;
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
            record_result = Convert(batch_item_set)

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
    private bool validate_length(IList<string> p_array, int p_max_length)
    {
        var result = true;

        if(p_array != null)
            for(var i = 0; i < p_array.Count; i++)
            {
                var item = p_array[i];
                if(item.Length > 0 && item.Length != p_max_length)
                {
                    result = false;
                    break;
                }
            }

        return result;
    }

    IList<string> validate_AssociatedNAT(IList<string> p_array) 
    {
        var result = new List<string>();

        int mom_ssn_start = 2000-1;

        for (var i = 0; i < p_array.Count; i++) 
        {
            var item = p_array[i];
            if (item.Length > mom_ssn_start + 9) 
            {
                // Don't store SSN in a variable - use inline comparison
                if (!g_cdc_identifier_set.Contains(item.Substring(mom_ssn_start, 9).Trim()))
                {
                    result.Add($"Missing identifier in NAT file at line: {i+1}");
                }
            }
        }

        return result;
    }

    IList<string> validate_AssociatedFET(IList<string> p_array) 
    {
        var result = new List<string>();

        int mom_ssn_start = 4039-1;

        for (var i = 0; i < p_array.Count; i++) 
        {
            var item = p_array[i];
            if (item.Length > mom_ssn_start + 9) 
            {
                // Don't store SSN in a variable - use inline comparison
                if (!g_cdc_identifier_set.Contains(item.Substring(mom_ssn_start, 9).Trim()))
                {
                    result.Add($"Missing identifier in FET file at line: {i+1}");
                }
            }
        }

        return result;
    }

    private mmria.common.ije.BatchItem Convert
    (
            string LineItem, 
            DateTime ImportDate,
            string ImportFileName,
            string ReportingState,
            HashSet<string> ExistingRecordIds
    )
    {
        /*
        CDCUniqueID
            ImportDate
            ImportFileName
            ReportingState
            StateOfDeathRecord
            DateOfDeath
            DateOfBirth
            LastName
            FirstName
            MMRIARecordID
            StatusDetail
            */

        var x = mor_get_header(LineItem);

        string record_id = null;

        do
        {
            record_id = $"{ReportingState.ToUpper()}-{x["DOD_YR"]}-{GenerateRandomFourDigits().ToString()}";
        }
        while (ExistingRecordIds.Contains(record_id));
        ExistingRecordIds.Add(record_id);

        var result = new mmria.common.ije.BatchItem()
        {
            Status = mmria.common.ije.BatchItem.StatusEnum.InProcess,
            CDCUniqueID = x["SSN"]?.Trim(),
            mmria_record_id = record_id,
            ImportDate = ImportDate,
            ImportFileName = ImportFileName,
            ReportingState = ReportingState,
            
            StateOfDeathRecord = x["DSTATE"],
            DateOfDeath = $"{x["DOD_YR"]}-{x["DOD_MO"]}-{x["DOD_DY"]}",
            DateOfBirth = $"{x["DOB_YR"]}-{x["DOB_MO"]}-{x["DOB_DY"]}",
            LastName = x["LNAME"],
            FirstName = x["GNAME"]//,
            //MMRIARecordID = x[""],
            //StatusDetail = x[""]
        };

        return result;
    }

    private string get_state_from_file_name(string p_val)
    {
        var remove_extension = p_val.Split(".");
        var split_on_underscore = remove_extension[0].Split("_");

        return split_on_underscore[split_on_underscore.Length -1];
    }

    private Dictionary<string,string> mor_get_header(string row)
    {
            var result = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            /*
DState 5 2
DOD_YR 1 4, 
DOD_MO 237 2, 
DOD_DY 239 2
DOB_YR 205 4, 
DOB_MO 209 2, 
DOD_DY 239 2
LNAME 78 50
GNAME 27 50
*/
result.Add("DState",row.Substring(5-1, 2));
result.Add("DOD_YR",row.Substring(1-1, 4));
result.Add("DOD_MO",row.Substring(237-1, 2));
result.Add("DOD_DY",row.Substring(239-1, 2));
result.Add("DOB_YR",row.Substring(205-1, 4));
result.Add("DOB_MO",row.Substring(209-1, 2));
result.Add("DOB_DY",row.Substring(211-1, 2));
result.Add("LNAME",row.Substring(78-1, 50));
result.Add("GNAME",row.Substring(27-1, 50));
result.Add("SSN",row.Substring(191-1, 9)?.Trim());

        return result;

        /*
        2 home_record/state of death - DState
3 home_recode/date_of_death - DOD_YR, DOD_MO, DOD_DY
4 death_certificate/date_of_birth - DOB_YR, DOB_MO, DOD_DY
5 home_record/last_name - LNAME  
6 home_record/first_name - GNAME*/
    }

    private List<string> GetAssociatedNat(string[] p_nat_list, string p_cdc_unique_id)
    {
        var result = new List<string>();
        int mom_ssn_start = 2000-1;
        if (p_nat_list != null)
            foreach (var item in p_nat_list)
            {
                if (item.Length > mom_ssn_start + 9)
                {
                    var mom_ssn = item.Substring(mom_ssn_start, 9)?.Trim();
                    if (mom_ssn == p_cdc_unique_id)
                    {
                        result.Add(item);
                    }
                }
            }

        return result;
    }

    private List<string> GetAssociatedFet(string[] p_fet_list, string p_cdc_unique_id)
    {
        var result = new List<string>();
        int mom_ssn_start = 4039-1;
        if(p_fet_list != null)
            foreach(var item in p_fet_list)
            {
                if(item.Length > mom_ssn_start + 9)
                {
                    var mom_ssn = item.Substring(mom_ssn_start, 9)?.Trim();
                    if(mom_ssn == p_cdc_unique_id)
                    {
                        result.Add(item);
                    }
                }
            }

        return result;
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
    private int GenerateRandomFourDigits()
    {
        int _min = 1000;
        int _max = 9999;
        return System.Security.Cryptography.RandomNumberGenerator.GetInt32(_min, _max + 1);
    }
}

   

