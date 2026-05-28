using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using mmria.server.model.actor;

namespace mmria.server.model.actor.quartz;

public sealed class Synchronize_Deleted_Case_Records : ReceiveActor
{
    //protected override void PreStart() => Console.WriteLine("Synchronize_Deleted_Case_Records started");
    //protected override void PostStop() => Console.WriteLine("Synchronize_Deleted_Case_Records stopped");
	mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.server.model.TenantChangeSequenceState _changeSequenceState;

    public Synchronize_Deleted_Case_Records
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        db_config = _db_config;
        _couchDbHttpClient = couchDbHttpClient;
        _changeSequenceState = Program.GetTenantChangeSequenceState(
            mmria.server.model.TenantChangeSequenceState.KeyFor(db_config));

        ReceiveAsync<ScheduleInfoMessage>(async scheduleInfo => await Process_Schedule(scheduleInfo));
    }
    private async System.Threading.Tasks.Task Process_Schedule(ScheduleInfoMessage scheduleInfo)
    {
        Console.WriteLine($"Synchronize_Deleted_Case_Records {System.DateTime.Now}");

        mmria.server.model.couchdb.c_change_result latest_change_set = await GetJobInfo(_changeSequenceState.LastChangeSequence, scheduleInfo);

            Dictionary<string, KeyValuePair<string,bool>> response_results = new Dictionary<string, KeyValuePair<string,bool>> (StringComparer.OrdinalIgnoreCase);
            
            if (_changeSequenceState.LastChangeSequence != latest_change_set.last_seq)
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

            
            _changeSequenceState.RecordCall();
            _changeSequenceState.LastChangeSequence = latest_change_set.last_seq;

            // Bound the per-change-row fan-out. The previous code did Task.Run
            // per row with no await, no concurrency cap, and detached errors from
            // the actor lifecycle. Awaiting via Parallel.ForEachAsync surfaces
            // failures and limits CouchDB pressure to a small constant.
            var syncParallelOptions = new System.Threading.Tasks.ParallelOptions
            {
                MaxDegreeOfParallelism = 4
            };

            await System.Threading.Tasks.Parallel.ForEachAsync(
                response_results,
                syncParallelOptions,
                async (kvp, ct) =>
                {
                    if (kvp.Value.Value)
                    {
                        try
                        {
                            #if !IS_PMSS_ENHANCED
                            mmria.server.utils.c_sync_document sync_document = new mmria.server.utils.c_sync_document(kvp.Key, null, "DELETE", scheduleInfo.version_number, db_config, _couchDbHttpClient);
                            await sync_document.executeAsync();
                            #endif
                            #if IS_PMSS_ENHANCED
                            mmria.pmss.server.utils.c_sync_document sync_document = new mmria.pmss.server.utils.c_sync_document(kvp.Key, null, "DELETE", scheduleInfo.version_number, db_config);
                            await sync_document.executeAsync();
                            #endif
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine("Sync Delete case");
                            System.Console.WriteLine(ex);
                        }
                    }
                    else
                    {
                        string document_url = db_config.url + $"/{db_config.prefix}mmrds/" + kvp.Key;
                        string document_json = null;

                        try
                        {
                            document_json = await _couchDbHttpClient.ExecuteAsync("GET", document_url, null, db_config.user_name, db_config.user_value);
                            if (!string.IsNullOrEmpty(document_json) && document_json.IndexOf("\"_id\":\"_design/") < 0)
                            {
                                #if !IS_PMSS_ENHANCED
                                mmria.server.utils.c_sync_document sync_document = new mmria.server.utils.c_sync_document(kvp.Key, document_json, "PUT", scheduleInfo.version_number, db_config, _couchDbHttpClient);
                                await sync_document.executeAsync();
                                #endif
                                #if IS_PMSS_ENHANCED
                                mmria.pmss.server.utils.c_sync_document sync_document = new mmria.pmss.server.utils.c_sync_document(kvp.Key, document_json, "PUT", scheduleInfo.version_number, db_config);
                                await sync_document.executeAsync();
                                #endif
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine("Sync PUT case");
                            System.Console.WriteLine(ex);
                        }
                    }
                });
    }

    public async Task<mmria.server.model.couchdb.c_change_result> GetJobInfo(string p_last_sequence, ScheduleInfoMessage p_scheduleInfo)
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
        string res = await _couchDbHttpClient.ExecuteAsync("GET", url, null, p_scheduleInfo.user_name, p_scheduleInfo.user_value);
        
        result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.server.model.couchdb.c_change_result>(res);

        return result;
    }


}
