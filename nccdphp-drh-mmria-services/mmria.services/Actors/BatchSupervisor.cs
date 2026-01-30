using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Akka.Actor;
using mmria.common.ije;

namespace RecordsProcessor_Worker.Actors;

public sealed class BatchSupervisor : ReceiveActor
{

    Dictionary<string, mmria.common.ije.Batch.StatusEnum> batch_id_list;
    IConfiguration configuration;
    ILogger logger;
    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
    public BatchSupervisor()
    {
        //IConfiguration p_configuration
        //configuration = p_configuration;
        //logger = p_logger;
        batch_id_list = new Dictionary<string, mmria.common.ije.Batch.StatusEnum>();

        var alldocs = GetBatchSet();
        foreach(var row in alldocs.rows)
        {
            batch_id_list.Add(row.id, row.doc.Status);
        }

        Receive<mmria.common.ije.NewIJESet_Message>(message =>
        {

                string ping_result = PingCVSServer(mmria.services.vitalsimport.Program.DbConfigSet);
                int ping_count = 1;
                
                while
                (
                    (
                        ping_result == null ||
                        ping_result.ToLower() != "Server is up!".ToLower()
                    ) && 
                    ping_count < 2
                )   
                {

                    Console.WriteLine($"{DateTime.Now.ToString("o")} CVS Server Not running: Waiting 40 seconds to try again: {ping_result}");

					const int Milliseconds_In_Second = 1000;
					var next_date = DateTime.Now.AddMilliseconds(40 * Milliseconds_In_Second);
                    while(DateTime.Now < next_date)
					{
						// do nothing
					}
                    
                    ping_result = PingCVSServer(mmria.services.vitalsimport.Program.DbConfigSet);
                    ping_count +=1;

                    

                }


            batch_id_list.Add(message.batch_id, mmria.common.ije.Batch.StatusEnum.InProcess);
            var batch_processor = Context.ActorOf<RecordsProcessor_Worker.Actors.BatchProcessor>(message.batch_id);
            batch_processor.Tell(message);
            //Console.WriteLine(JsonConvert.SerializeObject(message));
            //Sender.Tell("Message Recieved");
            
        });

        Receive<mmria.common.ije.BatchStatusMessage>(message =>
        {
            batch_id_list[message.id] = message.status;
            
        });



        Receive<mmria.common.ije.BatchRemoveDataMessage>(message =>
        {
            if(batch_id_list.ContainsKey(message.id))
            {
                if
                (
                    batch_id_list[message.id] == mmria.common.ije.Batch.StatusEnum.Finished ||
                    batch_id_list[message.id] == mmria.common.ije.Batch.StatusEnum.BatchRejected
                )
                {
                    var batch_processor = Context.ActorOf<RecordsProcessor_Worker.Actors.BatchProcessor>(message.id);
                    batch_processor.Tell(message);
                }
            }
            
        });


        
    }

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


    public string PingCVSServer
    (
        mmria.common.couchdb.ConfigurationSet ConfigDB
    ) 
    { 
        var response_string = "";
        try
        {
            var base_url = ConfigDB.name_value["cvs_api_url"];

            var sever_status_body = new mmria.common.cvs.server_status_post_body()
            {
                id = ConfigDB.name_value["cvs_api_id"],
                secret = ConfigDB.name_value["cvs_api_key"],

            };

            var body_text =  System.Text.Json.JsonSerializer.Serialize(sever_status_body);
            var server_statu_curl = new mmria.getset.cURL("POST", null, base_url, body_text);

            response_string = server_statu_curl.execute();
            System.Console.WriteLine(response_string);

        }
        catch(System.Net.WebException ex)
        {
            System.Console.WriteLine($"cvsAPIController  POST\n{ex}");
            
            /*return Problem(
                type: "/docs/errors/forbidden",
                title: "CVS API Error",
                detail: ex.Message,
                statusCode: (int) ex.Status,
                instance: HttpContext.Request.Path
            );*/
        }
//"Server is up!"


        return response_string.Trim('"');
    }
}
