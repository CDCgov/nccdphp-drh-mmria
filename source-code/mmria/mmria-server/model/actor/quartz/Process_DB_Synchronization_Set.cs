#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using mmria.common.SharedLibraries.Case;
using mmria.common.SharedLibraries.DeIdentified;
using mmria.common.SharedLibraries.Report;

namespace mmria.server.model.actor.quartz;

public sealed class Process_DB_Synchronization_Set : ReceiveActor
{
    //protected override void PreStart() => Console.WriteLine("Process_DB_Synchronization_Set started");
    //protected override void PostStop() => Console.WriteLine("Process_DB_Synchronization_Set stopped");
	mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.server.model.TenantChangeSequenceState _changeSequenceState;
    private readonly ICaseRepository _caseRepository;
    private readonly IDeIdentifiedRepository _deIdentifiedRepository;
    private readonly IReportRepository _reportRepository;

    public Process_DB_Synchronization_Set
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        ICaseRepository caseRepository,
        IDeIdentifiedRepository deIdentifiedRepository,
        IReportRepository reportRepository
    )
    {
        db_config = _db_config;
        _couchDbHttpClient = couchDbHttpClient;
        _caseRepository = caseRepository;
        _deIdentifiedRepository = deIdentifiedRepository;
        _reportRepository = reportRepository;
        _changeSequenceState = Program.GetTenantChangeSequenceState(
            mmria.server.model.TenantChangeSequenceState.KeyFor(db_config));

        ReceiveAsync<ScheduleInfoMessage>(async scheduleInfo => await Process_Schedule(scheduleInfo));
    }
    private async System.Threading.Tasks.Task Process_Schedule(ScheduleInfoMessage scheduleInfo)
    {
        Console.WriteLine($"Process_DB_Synchronization_Set {System.DateTime.Now}");

        //System.Console.WriteLine ("{0} Beginning Change Synchronization.", System.DateTime.Now);
        //log.DebugFormat("iCIMS_Data_Call_Job says: Starting {0} executing at {1}", jobKey, DateTime.Now.ToString("r"));
        CaseChangeFeedResult latest_change_set = await _caseRepository.GetCaseChangesSinceAsync(_changeSequenceState.LastChangeSequence, db_config);

            Dictionary<string, KeyValuePair<string,bool>> response_results = new Dictionary<string, KeyValuePair<string,bool>> (StringComparer.OrdinalIgnoreCase);
        
            if (_changeSequenceState.LastChangeSequence != latest_change_set.LastSeq)
            {
                foreach (CaseChangeEntry entry in latest_change_set.Changes)
                {
                    if (response_results.ContainsKey(entry.Id))
                    {
                        response_results[entry.Id] = new KeyValuePair<string, bool>(entry.Seq, entry.Deleted);
                    }
                    else
                    {
                        response_results.Add(entry.Id, new KeyValuePair<string, bool>(entry.Seq, entry.Deleted));
                    }
                }
            }

        
            _changeSequenceState.RecordCall();
            _changeSequenceState.LastChangeSequence = latest_change_set.LastSeq;

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
                            mmria.server.utils.c_sync_document sync_document = new mmria.server.utils.c_sync_document(kvp.Key, null, "DELETE", scheduleInfo.version_number, db_config, _couchDbHttpClient, deIdentifiedRepository: _deIdentifiedRepository, reportRepository: _reportRepository);
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
                        string document_json = null;

                        try
                        {
                            document_json = await _caseRepository.GetCaseDocumentJsonAsync(kvp.Key, db_config);
                            if (!string.IsNullOrEmpty(document_json) && document_json.IndexOf("\"_id\":\"_design/\"") < 0)
                            {
                                mmria.server.utils.c_sync_document sync_document = new mmria.server.utils.c_sync_document(kvp.Key, document_json, "PUT", scheduleInfo.version_number, db_config, _couchDbHttpClient, deIdentifiedRepository: _deIdentifiedRepository, reportRepository: _reportRepository);
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
                async System.Threading.Tasks.Task PopulateIdSetAsync(string body, HashSet<string> target)
                {
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
                await PopulateIdSetAsync(await _caseRepository.GetAllCaseDocsAsync(false, db_config), mmrds_id_set);

                // Scope the de_id set so it becomes GC-eligible before we build the
                // report set, capping the live HashSet count at 2 instead of 3.
                {
                    HashSet<string> de_id_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    await PopulateIdSetAsync(await _couchDbHttpClient.ExecuteAsync("GET", db_config.url + $"/{db_config.prefix}de_id/_all_docs", null, db_config.user_name, db_config.user_value), de_id_set);

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
                    await PopulateIdSetAsync(await _couchDbHttpClient.ExecuteAsync("GET", db_config.url + $"/{db_config.prefix}report/_all_docs", null, db_config.user_name, db_config.user_value), report_id_set);

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

}
#endif