using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Web;
using Quartz;
using Quartz.Impl;
using Microsoft.Extensions.Configuration;

namespace mmria.server.model;

/*
public sealed class remove_deleted_job : IJob
{
    string couch_db_url = null;
    string user_name = null;
    string user_value = null;
    private IConfiguration Configuration = null;

    public remove_deleted_job(IConfiguration configuration)
    {
            this.couch_db_url = db_config.url;
            this.user_name = db_config.user_name;
            this.user_value = db_config.user_value;
            Configuration = configuration;
    }

    public Task Execute(IJobExecutionContext context)
    {
        //Common.Logging.ILog log = Common.Logging.LogManager.GetCurrentClassLogger();
        //log.Debug("IJob.Execute");

        JobKey jobKey = context.JobDetail.Key;
        //log.DebugFormat("iCIMS_Data_Call_Job says: Starting {0} executing at {1}", jobKey, DateTime.Now.ToString("r"));
        mmria.server.model.couchdb.c_change_result latest_change_set = GetJobInfo(Program.Last_Change_Sequence);

        Dictionary<string, KeyValuePair<string,bool>> response_results = new Dictionary<string, KeyValuePair<string,bool>> (StringComparer.OrdinalIgnoreCase);
        
        if (Program.Last_Change_Sequence != latest_change_set.last_seq)
        {
            foreach (mmria.server.model.couchdb.c_seq seq in latest_change_set.results)
            {
                if (response_results.ContainsKey (seq.id)) 
                {
                    if 
                    (
                        seq.changes.Count > 0 &&
                        response_results [seq.id].Key != seq.changes [0].rev
                    )
                    {
                        if (seq.deleted == null)
                        {
                            response_results [seq.id] = new KeyValuePair<string, bool> (seq.changes [0].rev, false);
                        }
                        else
                        {
                            response_results [seq.id] = new KeyValuePair<string, bool> (seq.changes [0].rev, true);
                        }
                        
                    }
                }
                else 
                {
                    if (seq.deleted == null)
                    {
                        response_results.Add (seq.id, new KeyValuePair<string, bool> (seq.changes [0].rev, false));
                    }
                    else
                    {
                        response_results.Add (seq.id, new KeyValuePair<string, bool> (seq.changes [0].rev, true));
                    }
                }
            }
        }

        
        if (Program.Change_Sequence_Call_Count < int.MaxValue)
        {
            Program.Change_Sequence_Call_Count++;
        }

        if (Program.DateOfLastChange_Sequence_Call.Count > 9)
        {
            Program.DateOfLastChange_Sequence_Call.Clear();
        }

        Program.DateOfLastChange_Sequence_Call.Add(DateTime.Now);

        Program.Last_Change_Sequence = latest_change_set.last_seq;

        foreach (KeyValuePair<string, KeyValuePair<string, bool>> kvp in response_results)
        {
            System.Threading.Tasks.Task.Run
            (
                new Action(() => 
                {
                    if (kvp.Value.Value)
                    {
                        try
                        {
                            mmria.server.utils.c_sync_document sync_document = new mmria.server.utils.c_sync_document (kvp.Key, null, "DELETE");
                            sync_document.executeAsync ();
                            
        
                        }
                        catch (Exception ex)
                        {
                                System.Console.WriteLine ("Sync Delete case");
                                System.Console.WriteLine (ex);
                        }
                    }
                    else
                    {
    
                        string document_url = db_config.url + $"/{db_config.prefix}mmrds/" + kvp.Key;
                        var document_curl = new cURL ("GET", null, document_url, null, db_config.user_name, db_config.user_value);
                        string document_json = null;
    
                        try
                        {
                            document_json = document_curl.execute ();
                            if (!string.IsNullOrEmpty (document_json) && document_json.IndexOf ("\"_id\":\"_design/") < 0)
                            {
                                mmria.server.utils.c_sync_document sync_document = new mmria.server.utils.c_sync_document (kvp.Key, document_json);
                                sync_document.executeAsync ();
                            }
        
                        }
                        catch (Exception ex)
                        {
                                System.Console.WriteLine ("Sync PUT case");
                                System.Console.WriteLine (ex);
                        }
                    }
                })
            );
        }

        return Task.CompletedTask;
    }



    public mmria.server.model.couchdb.c_change_result GetJobInfo(string p_last_sequence)
    {

        mmria.server.model.couchdb.c_change_result result = new mmria.server.model.couchdb.c_change_result();
        string url = null;

        if (string.IsNullOrWhiteSpace(p_last_sequence))
        {
            url = db_config.url + $"/{db_config.prefix}mmrds/_changes";
        }
        else
        {
            url = db_config.url + $"/{db_config.prefix}mmrds/_changes?since=" + p_last_sequence;
        }
        var curl = new cURL ("GET", null, url, null, this.user_name, this.user_value);
        string res = curl.execute();
        
        result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.server.model.couchdb.c_change_result>(res);

        return result;
    }


    private string Get_Change_Set()
    {
        string result = null;


        return result;
    }

    private string Get_Job(string Job_Id)
    {

        string result = null;

        var url = string.Format(System.Configuration.ConfigurationManager.AppSettings["icims_job_detail_url"], Job_Id);
        
        System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
        request.Credentials = new System.Net.NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["icims_user_id"], System.Configuration.ConfigurationManager.AppSettings["icims_password"]);
        request.PreAuthenticate = false;

        try
        {
            using (System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    System.Text.Encoding enc = System.Text.Encoding.GetEncoding(1252);
                    System.IO.StreamReader loResponseStream = new
                        System.IO.StreamReader(response.GetResponseStream(), enc);

                    string Response = loResponseStream.ReadToEnd();

                    loResponseStream.Close();
                    response.Close();
                    System.Console.Write(Response);
                    result = Response;
                    return result;
                }
                else
                {
                    return result;
                }
            }
        }
        catch (System.Net.WebException)
        {
            return result;
        }
        catch
        {
            return result;
        }
    }
}
*/
