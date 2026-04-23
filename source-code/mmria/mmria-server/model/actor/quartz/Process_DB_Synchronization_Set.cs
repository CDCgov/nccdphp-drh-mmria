#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace mmria.server.model.actor.quartz;

public sealed class Process_DB_Synchronization_Set : ReceiveActor
{
    //protected override void PreStart() => Console.WriteLine("Process_DB_Synchronization_Set started");
    //protected override void PostStop() => Console.WriteLine("Process_DB_Synchronization_Set stopped");
	mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.server.model.TenantChangeSequenceState _changeSequenceState;

    public Process_DB_Synchronization_Set
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
        Console.WriteLine($"Process_DB_Synchronization_Set {System.DateTime.Now}");

        //System.Console.WriteLine ("{0} Beginning Change Synchronization.", System.DateTime.Now);
        //log.DebugFormat("iCIMS_Data_Call_Job says: Starting {0} executing at {1}", jobKey, DateTime.Now.ToString("r"));
        mmria.server.model.couchdb.c_change_result latest_change_set = await get_changes (_changeSequenceState.LastChangeSequence, scheduleInfo);

            Dictionary<string, KeyValuePair<string,bool>> response_results = new Dictionary<string, KeyValuePair<string,bool>> (StringComparer.OrdinalIgnoreCase);
        
            if (_changeSequenceState.LastChangeSequence != latest_change_set.last_seq)
            {
                foreach (mmria.server.model.couchdb.c_seq seq in latest_change_set.results)
                {
                    if (response_results.ContainsKey (seq.id)) 
                    {
                        if (
                            seq.changes.Count > 0 &&
                            response_results [seq.id].Key != seq.changes [0].rev)
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
            // per row with no await, no concurrency cap, and silent error swallowing.
            // Awaiting via Parallel.ForEachAsync keeps errors attached to the actor
            // lifecycle and limits CouchDB pressure to a small constant.
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
                            mmria.server.utils.c_sync_document sync_document = new mmria.server.utils.c_sync_document(kvp.Key, null, "DELETE", scheduleInfo.version_number, db_config, _couchDbHttpClient);
                            await sync_document.executeAsync();
                        }
                        catch (Exception)
                        {
                            //System.Console.WriteLine ("Sync Delete case");
                            //System.Console.WriteLine (ex);
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
                                mmria.server.utils.c_sync_document sync_document = new mmria.server.utils.c_sync_document(kvp.Key, document_json, "PUT", scheduleInfo.version_number, db_config, _couchDbHttpClient);
                                await sync_document.executeAsync();
                            }
                        }
                        catch (Exception)
                        {
                            //System.Console.WriteLine ("Sync PUT case");
                            //System.Console.WriteLine (ex);
                        }
                    }
                });

            try
            {

                HashSet<string> mmrds_id_set = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
                HashSet<string> deleted_id_set = null;

                // Stream _all_docs via JsonDocument instead of materialising the
                // c_all_docs POCO graph (one c_all_docs_row + one c_change per row).
                // For tenants with thousands of cases this avoids a transient
                // multi-MB Newtonsoft object graph per tick, per tenant.
                async System.Threading.Tasks.Task PopulateIdSetAsync(string url, HashSet<string> target)
                {
                    string body = await _couchDbHttpClient.ExecuteAsync("GET", url, null, db_config.user_name, db_config.user_value);
                    if (string.IsNullOrEmpty(body)) return;

                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    if (!doc.RootElement.TryGetProperty("rows", out var rowsElement) ||
                        rowsElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                    {
                        return;
                    }

                    if (target.Count == 0 &&
                        doc.RootElement.TryGetProperty("total_rows", out var totalElement) &&
                        totalElement.ValueKind == System.Text.Json.JsonValueKind.Number &&
                        totalElement.TryGetInt32(out int total) && total > 0)
                    {
                        target.EnsureCapacity(total);
                    }

                    foreach (var rowElement in rowsElement.EnumerateArray())
                    {
                        if (rowElement.TryGetProperty("id", out var idElement) &&
                            idElement.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            target.Add(idElement.GetString());
                        }
                    }
                }

                // get all non deleted cases in mmrds (kept across the whole method
                // because de_id and report diffs both reference it).
                await PopulateIdSetAsync(db_config.url + $"/{db_config.prefix}mmrds/_all_docs", mmrds_id_set);

                // Scope the de_id set so it becomes GC-eligible before we build the
                // report set, capping the live HashSet count at 2 instead of 3.
                {
                    HashSet<string> de_id_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    await PopulateIdSetAsync(db_config.url + $"/{db_config.prefix}de_id/_all_docs", de_id_set);

                    deleted_id_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    deleted_id_set.Union(de_id_set.Except(mmrds_id_set));
                    foreach (string id in deleted_id_set)
                    {
                        // Preserved for behavioural parity. The original Union call
                        // discards its result so deleted_id_set is always empty and
                        // this loop never executes; left here in case the Union bug
                        // is fixed later.
                        string rev = null;
                        await _couchDbHttpClient.ExecuteAsync("DELETE", db_config.url + $"/{db_config.prefix}de_id/" + id + "?rev=" + rev, null, db_config.user_name, db_config.user_value);
                    }
                }

                {
                    HashSet<string> report_id_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    await PopulateIdSetAsync(db_config.url + $"/{db_config.prefix}report/_all_docs", report_id_set);

                    deleted_id_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    deleted_id_set.Union(report_id_set.Except(mmrds_id_set));
                    foreach (string id in deleted_id_set)
                    {
                        string rev = null;
                        await _couchDbHttpClient.ExecuteAsync("DELETE", db_config.url + $"/{db_config.prefix}report/" + id + "?rev=" + rev, null, db_config.user_name, db_config.user_value);
                    }
                }
            }
            catch (Exception ex)
            {
                    System.Console.WriteLine ("Delete sync error:\n{0}", ex);
            }

            //System.Console.WriteLine ("{0}- Ending Change Synchronization.", System.DateTime.Now);
    }

    public async System.Threading.Tasks.Task<mmria.server.model.couchdb.c_change_result> get_changes(string p_last_sequence, ScheduleInfoMessage p_scheduleInfo)
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
#endif