using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Akka.Actor;
using mmria.common.ije;

namespace mmria.services.populate_cdc_instance;

public sealed class PopulateCDCInstanceSupervisor : ReceiveActor
{
    public record PopulateFinished(DateTime Date_Completed);

    string transfer_result = "Ready to transfer";
    int transfer_status_number = 0;
    DateTime? date_submitted = DateTime.Now;
    DateTime? date_completed;
    int duration_in_hours = 0;
    int duration_in_minutes = 0;

    string error_message = "";
      
    IConfiguration configuration;
    ILogger logger;

    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
    public PopulateCDCInstanceSupervisor()
    {
        
        //Context.ActorOf<PopulateCDCInstance>("child");

        var data = GetPopulate();

        if(data.transfer_status_number == 1)
        {
            SetTransferStatus();
        }
        else
        {

            transfer_result = data.transfer_result;
            transfer_status_number = data.transfer_status_number.Value;
            date_submitted = data.date_submitted;
            date_completed = data.date_completed;
            duration_in_hours = data.duration_in_hours.Value;
            duration_in_minutes = data.duration_in_minutes.Value;
            error_message = data.error_message;
        }

        


        Receive<DateTime>(message =>
        {
            mmria.common.metadata.Populate_CDC_Instance_Record result;
            if
            (
                transfer_status_number == 0 || 
                transfer_status_number == 1
            )
            {
                result = new mmria.common.metadata.Populate_CDC_Instance_Record()
                {
                    transfer_result = transfer_result,
                    transfer_status_number = transfer_status_number,
                    date_submitted = date_submitted,
                    date_completed = date_completed,
                    duration_in_hours = duration_in_hours,
                    duration_in_minutes = duration_in_minutes,
                    error_message = error_message
                };
            }
            else
            {
                var time_diff = DateTime.Now - date_submitted;
                var running_duration_in_hours = (int) time_diff.Value.TotalHours;
                var running_duration_in_minutes = (int) time_diff.Value.TotalMinutes % 60;
                result = new mmria.common.metadata.Populate_CDC_Instance_Record()
                {
                    transfer_result = transfer_result,
                    transfer_status_number = transfer_status_number,
                    date_submitted = date_submitted,
                    date_completed = date_completed,
                    duration_in_hours = running_duration_in_hours,
                    duration_in_minutes = running_duration_in_minutes,
                    error_message = error_message
                };
            }
            Sender.Tell(result);
        });

        Receive<mmria.common.metadata.Populate_CDC_Instance>(message =>
        {

            //var processor = Context.ActorSelection("akka://mmria-actor-system/user/populate-cdc-instance-supervisor/child*");

            var processor = Context.ActorOf<PopulateCDCInstance>();
            
            processor.Tell(message);


            transfer_status_number = 1;
            date_submitted = DateTime.Now;
            date_completed = null;
            duration_in_hours = 0;
            duration_in_minutes = 0;
            transfer_result = $"Transfer in progress (Submitted {GetDateString(date_submitted)} at {GetTimeString(date_submitted)}). Please check again later for completion status.";
            error_message = "";

            SetTransferStatus();

            Sender.Tell
            (
                new mmria.common.metadata.Populate_CDC_Instance_Record()
                {
                    transfer_result = transfer_result,
                    transfer_status_number = transfer_status_number,
                    date_submitted = date_submitted,
                    date_completed = date_completed,
                    duration_in_hours = duration_in_hours,
                    duration_in_minutes = duration_in_minutes,
                    error_message = error_message
                }
            );

        });


        Receive<PopulateFinished>(message =>
        {
            transfer_status_number = 0;
            date_completed = DateTime.Now;
            var time_diff = date_completed - date_submitted;
            duration_in_hours = (int) time_diff.Value.TotalHours;
            duration_in_minutes = (int) time_diff.Value.TotalMinutes % 60;
            transfer_result = $"Transfer complete. Time to transfer: {duration_in_hours} hrs {duration_in_minutes} min | Submitted {GetDateString(date_submitted)} at {GetTimeString(date_submitted)} | Completed {GetDateString(date_completed)} at {GetTimeString(date_completed)}";
            error_message = "";

            SetTransferStatus();
        });

        
        Receive<PopulateCDCInstance.Status>(message =>
        {
            date_completed = DateTime.Now;
            var time_diff = date_completed - date_submitted;
            duration_in_hours = (int) time_diff.Value.TotalHours;
            duration_in_minutes = (int) time_diff.Value.TotalMinutes % 60;
            
            if(message.Name == "Error")
            {
                transfer_status_number = 2;
                transfer_result =  @$"Transfer could not be completed ( Time to transfer: {duration_in_hours} hrs {duration_in_minutes} min | Submitted {GetDateString(date_submitted)} at {GetTimeString(date_submitted)}| Failed {GetDateString(date_completed)} at {GetTimeString(date_completed)}).

        Please contact your system administrator for assistance.Transfer complete.";

                error_message = message.Description;
            }
            else
            {
                transfer_status_number = 0;
                transfer_result = $"Transfer complete. Time to transfer: {duration_in_hours} hrs {duration_in_minutes} min | Submitted {GetDateString(date_submitted)} at {GetTimeString(date_submitted)} | Completed {GetDateString(date_completed)} at {GetTimeString(date_completed)}";
                error_message = "";
            }

            SetTransferStatus();
            
            
        });

    }

    public mmria.common.metadata.Populate_CDC_Instance_Record GetStatus()
    {
        return new mmria.common.metadata.Populate_CDC_Instance_Record()
                {
                    transfer_result = transfer_result,
                    transfer_status_number = transfer_status_number,
                    date_submitted = date_submitted,
                    date_completed = date_completed,
                    duration_in_hours = duration_in_hours,
                    duration_in_minutes = duration_in_minutes,
                    error_message = error_message
                };
    }

    string GetDateString(DateTime? value)
    {
        if(value.HasValue)
            return $"{value.Value.Month.ToString().PadLeft(2,'0')}/{value.Value.Day.ToString().PadLeft(2,'0')}/{value.Value.Year.ToString().PadLeft(2,'0')}";
        else 
            return "no date";
    }

    
    string GetTimeString(DateTime? value)
    {
        if(value.HasValue)
            return $"{value.Value.Hour.ToString().PadLeft(2,'0')}:{value.Value.Minute.ToString().PadLeft(2,'0')}:{value.Value.Second.ToString().PadLeft(2,'0')}";
        else 
            return "no date";
    }

    /*
    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy
        (
            maxNrOfRetries: 0,
            withinTimeRange: TimeSpan.FromMinutes(0),
            localOnlyDecider: PopulateCDCFailed
        );
    }

    Directive PopulateCDCFailed(Exception ex)
    {
        transfer_status_number = 2;
        date_completed = DateTime.Now;
        var time_diff = date_completed - date_submitted;
        duration_in_hours = (int) time_diff.Value.TotalHours;
        duration_in_minutes = (int) time_diff.Value.TotalMinutes % 60;
        error_message = ex.Message;

        transfer_result = @$"Transfer could not be completed ( Time to transfer: 2 min | Submitted 09/28/2022 at 10:04:00| Failed 09/28/2022 at 10:06:00).

        Please contact your system administrator for assistance.Transfer complete. Time to transfer: 2 hrs 14 min | Submitted 09/28/2022 at 10:04:00 | Completed 09/28/2022 at 12:18:00";


        return Directive.Restart;
    } */



    private mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch> GetBatchSet()
    {
        var result = new mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch>();

        string url = $"{mmria.services.vitalsimport.Program.couchdb_url}/vital_import/_all_docs?include_docs=true";
        var document_curl = new mmria.getset.cURL ("GET", null, url, null, mmria.services.vitalsimport.Program.timer_user_name, mmria.services.vitalsimport.Program.timer_value);
        try
        {
            var responseFromServer = document_curl.execute();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch>>(responseFromServer);
            
        }
        catch(Exception ex)
        {
            //Console.Write("auth_session_token: {0}", auth_session_token);
            Console.WriteLine(ex);
        }

        return result;
    }


    void SetTransferStatus()
    {
        var data = GetPopulate();

        if(data != null)
        {
            data.transfer_result = transfer_result;
            data.transfer_status_number = transfer_status_number;
            data.date_submitted = date_submitted;
            data.date_completed = date_completed;
            data.duration_in_hours = duration_in_hours;
            data.duration_in_minutes = duration_in_minutes;
            data.error_message = error_message;

            SavePopulate(data);
        }
        else
        {
            Console.WriteLine("Problemd setting Transfer Status");
        }
    }

    public mmria.common.metadata.Populate_CDC_Instance GetPopulate()
    {
        mmria.common.metadata.Populate_CDC_Instance result = new();
        try
        {
            string request_string = $"{mmria.services.vitalsimport.Program.couchdb_url}/metadata/populate-cdc-instance";
            var case_curl = new mmria.getset.cURL("GET", null, request_string, null,mmria.services.vitalsimport.Program.timer_user_name, mmria.services.vitalsimport.Program.timer_value);
            string responseFromServer = case_curl.execute();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Populate_CDC_Instance>(responseFromServer);
    
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    public mmria.common.model.couchdb.document_put_response SavePopulate(mmria.common.metadata.Populate_CDC_Instance data)
    {
        mmria.common.model.couchdb.document_put_response result = null;
        try
        {
            if(data._id == "populate-cdc-instance")
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);

                string request_string = $"{mmria.services.vitalsimport.Program.couchdb_url}/metadata/populate-cdc-instance";
                var case_curl = new mmria.getset.cURL("PUT", null, request_string, json,mmria.services.vitalsimport.Program.timer_user_name, mmria.services.vitalsimport.Program.timer_value);
                string responseFromServer = case_curl.execute();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
    
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }


}
