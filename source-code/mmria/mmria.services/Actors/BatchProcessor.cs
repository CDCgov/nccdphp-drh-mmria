using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using Akka.Actor;

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

    mmria.common.couchdb.DBConfigurationDetail item_db_info;

    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");

    private Dictionary<string, (string, mmria.common.ije.BatchItem)> batch_item_set = new (StringComparer.OrdinalIgnoreCase);

    private mmria.common.ije.Batch batch;
    public BatchProcessor()
    {
        Receive<mmria.common.ije.NewIJESet_Message>(message =>
        {

            Process_Message(message);
        });

        Receive<mmria.common.ije.BatchItem>(message =>
        {

            Process_Message(message);
        });

        
        Receive<mmria.common.ije.BatchRemoveDataMessage>(message =>
        {
            Process_Message(message);
        });
    }
    public BatchProcessor(string p_id):base()
    {
        _id = p_id;
        //IConfiguration p_configuration
        //configuration = p_configuration;
        //logger = p_logger;


        

        
    }
    private void Process_Message(mmria.common.ije.NewIJESet_Message message)
    {
        Console.WriteLine($"Processing Message : {message}");

        

        var mor_set = message.mor.Split("\n");

        var status_builder = new System.Text.StringBuilder();

        var is_valid_file_name = false;

        var mor_length_is_valid = validate_length(message?.mor?.Split("\n"), mor_max_length);
        var nat_length_is_valid = validate_length(message?.nat?.Split("\n"), nat_max_length);
        var fet_length_is_valid = validate_length(message?.fet?.Split("\n"), fet_max_length);


        var patt = new System.Text.RegularExpressions.Regex("[0-9]{4}_20[0-9]{2}_[0-2][0-9]_[0-3][0-9]_[A-Z,a-z]{2}.[mM][oO][rR]");

        if (patt.Match(message.mor_file_name).Length == 0) 
        {
            status_builder.AppendLine("mor file name format incorrect. File name must be in Year_Month_Day_StateCode format. (e.g. 2020_2021_01_01_KS.mor");
        }

        if(!mor_length_is_valid) status_builder.AppendLine("mor length is invalid.");
        if(!nat_length_is_valid) status_builder.AppendLine("nat length is invalid.");
        if(!fet_length_is_valid) status_builder.AppendLine("fet length is invalid.");


        var ReportingState = get_state_from_file_name(message.mor_file_name);
        var ImportDate = DateTime.Now;
        
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
            ExistingRecordIds = GetExistingRecordIds();
        }

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
            foreach(var kvp in batch_item_set)
            {
                var batch_tuple = kvp.Value;
                try
                {
                    var StartBatchItemMessage = new mmria.common.ije.StartBatchItemMessage()
                    {
                        case_folder = message.case_folder,
                        cdc_unique_id = batch_tuple.Item2.CDCUniqueID,
                        record_id = batch_tuple.Item2.mmria_record_id,
                        ImportDate = ImportDate,
                        ImportFileName = message.mor_file_name,
                        host_state = ReportingState,
                        mor = batch_tuple.Item1,
                        nat = GetAssociatedNat(nat_list, batch_tuple.Item2.CDCUniqueID?.Trim()),
                        fet = GetAssociatedFet(fet_list, batch_tuple.Item2.CDCUniqueID?.Trim())
                    };

                    var batch_item_processor = Context.ActorOf<RecordsProcessor_Worker.Actors.BatchItemProcessor>(batch_tuple.Item2.CDCUniqueID?.Trim());
                    batch_item_processor.Tell(StartBatchItemMessage);
                }
                catch(Exception ex)
                {

                }
                
            }


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

            if(save_batch(batch))
            {
            }
            Context.Stop(this.Self);
    

        }

        

        
        
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
    private void Process_Message(mmria.common.ije.BatchItem message)
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
            if(save_batch(batch))
            {
            }
            Context.Stop(this.Self);
        }

        
    }


    private bool save_batch(mmria.common.ije.Batch p_batch)
    {
        bool result = false;


        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(batch, settings);

        string put_url = $"{mmria.services.vitalsimport.Program.couchdb_url}/vital_import/{p_batch.id}";
        var document_curl = new mmria.getset.cURL ("PUT", null, put_url, object_string, mmria.services.vitalsimport.Program.timer_user_name, mmria.services.vitalsimport.Program.timer_value);
        try
        {
            var responseFromServer = document_curl.execute();
            var	put_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

            if(put_result.ok)
            {
                result = true;


                var new_batch = new mmria.common.ije.Batch()
                {
                    id = batch.id,
                    date_created  = batch.date_created,
                    created_by = batch.created_by,
                    date_last_updated  = DateTime.UtcNow,
                    last_updated_by = batch.last_updated_by, 
                    Status = p_batch.Status,
                    reporting_state = batch.reporting_state,
                    ImportDate = batch.ImportDate,
                    mor_file_name = batch.mor_file_name,
                    nat_file_name = batch.nat_file_name,
                    fet_file_name = batch.fet_file_name,
                    StatusInfo = batch.StatusInfo,
                    record_result = Convert(batch_item_set)

                };

                batch = new_batch;
            }
            
        }
        catch(Exception ex)
        {
            //Console.Write("auth_session_token: {0}", auth_session_token);
            Console.WriteLine(ex);
        }

        return result;
    }


    private mmria.common.ije.Batch Get_batch(string _id)
    {
        mmria.common.ije.Batch result = null;

        string put_url = $"{mmria.services.vitalsimport.Program.couchdb_url}/vital_import/{_id}";
        var document_curl = new mmria.getset.cURL ("GET", null, put_url, null, mmria.services.vitalsimport.Program.timer_user_name, mmria.services.vitalsimport.Program.timer_value);
        try
        {
            var responseFromServer = document_curl.execute();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.ije.Batch>(responseFromServer);
        }
        catch(Exception ex)
        {
            //Console.Write("auth_session_token: {0}", auth_session_token);
            Console.WriteLine(ex);
        }

        return result;
    }


    private bool Delete_batch(string _id)
    {
        bool result = false;

        var batch = Get_batch(_id);

        string put_url = $"{mmria.services.vitalsimport.Program.couchdb_url}/vital_import/{_id}?rev={batch._rev}";
        var document_curl = new mmria.getset.cURL ("DELETE", null, put_url, null, mmria.services.vitalsimport.Program.timer_user_name, mmria.services.vitalsimport.Program.timer_value);
        try
        {
            var responseFromServer = document_curl.execute();
            var delete_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);

            result = true;
        }
        catch(Exception ex)
        {
            //Console.Write("auth_session_token: {0}", auth_session_token);
            Console.WriteLine(ex);
        }

        return result;
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

                var mom_ssn = item.Substring(mom_ssn_start, 9).Trim();
                if (!g_cdc_identifier_set.Contains(mom_ssn))
                {
                    result.Add($"Missing Id in NAT file Line: {i+1}  id: {mom_ssn}");
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
                var mom_ssn = item.Substring(mom_ssn_start, 9).Trim();
                if (!g_cdc_identifier_set.Contains(mom_ssn))
                {
                    result.Add($"Missing Id in FET file Line: {i+1}  id: {mom_ssn}");
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

    private void Process_Message(mmria.common.ije.BatchRemoveDataMessage message)
    {
        var config_timer_user_name = mmria.services.vitalsimport.Program.timer_user_name;
        var config_timer_value = mmria.services.vitalsimport.Program.timer_value;

        var config_couchdb_url = mmria.services.vitalsimport.Program.couchdb_url;
        var db_prefix = "";

        var  batch = Get_batch(message.id);

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

                    var case_expando = GetCaseById(item_db_info, case_id);
                    var rev_dynamic = ((IDictionary<string,object>)case_expando)["_rev"];
                    string rev = null;
                    if(rev_dynamic != null)
                    {
                        rev = rev_dynamic.ToString();
                    }

                    if (!string.IsNullOrWhiteSpace (case_id) && !string.IsNullOrWhiteSpace(rev)) 
                    {
                        request_string = $"{item_db_info.url}/{item_db_info.prefix}mmrds/{case_id}?rev={rev}";
                        var case_curl = new mmria.getset.cURL("DELETE", null, request_string, null, item_db_info.user_name, item_db_info.user_value);
                        string responseFromServer = case_curl.execute();

                        // to do synchronize
                    } 

                }
                catch(Exception ex)
                {
                    Console.WriteLine (ex);
                } 

            }
        }

        Delete_batch(message.id);

    }


    private System.Dynamic.ExpandoObject GetCaseById(mmria.common.couchdb.DBConfigurationDetail db_info, string case_id) 
    { 
        try
        {
            string request_string = $"{db_info.url}/{db_info.prefix}mmrds/_all_docs?include_docs=true";

            if (!string.IsNullOrWhiteSpace (case_id)) 
            {
                request_string = $"{db_info.url}/{db_info.prefix}mmrds/{case_id}";
                var case_curl = new mmria.getset.cURL("GET", null, request_string, null, db_info.user_name, db_info.user_value);
                string responseFromServer = case_curl.execute();

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (responseFromServer);

                return result;

            } 

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    } 

    public HashSet<string> GetExistingRecordIds()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        try
        {
            string request_string = $"{item_db_info.url}/{item_db_info.prefix}mmrds/_design/sortable/_view/by_date_created?skip=0&take=25000";

            var case_view_curl = new mmria.getset.cURL("GET", null, request_string, null, item_db_info.user_name, item_db_info.user_value);
            string responseFromServer = case_view_curl.execute();

            mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

            foreach (mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
            {
                result.Add(cvi.value.record_id);

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    private int GenerateRandomFourDigits()
    {
        int _min = 1000;
        int _max = 9999;
        Random _rdm = new Random(System.DateTime.Now.Millisecond + my_count);
        my_count ++;
        return _rdm.Next(_min, _max);
        
    }



}

   

