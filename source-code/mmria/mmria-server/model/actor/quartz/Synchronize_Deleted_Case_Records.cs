using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using mmria.server.model.actor;
using mmria.common.SharedLibraries.Case;
using mmria.common.SharedLibraries.DeIdentified;
using mmria.common.SharedLibraries.Report;

namespace mmria.server.model.actor.quartz;

public sealed class Synchronize_Deleted_Case_Records : ReceiveActor
{
    //protected override void PreStart() => Console.WriteLine("Synchronize_Deleted_Case_Records started");
    //protected override void PostStop() => Console.WriteLine("Synchronize_Deleted_Case_Records stopped");
	mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.server.model.TenantChangeSequenceState _changeSequenceState;
    private readonly ICaseRepository _caseRepository;
    private readonly IDeIdentifiedRepository _deIdentifiedRepository;
    private readonly IReportRepository _reportRepository;

    public Synchronize_Deleted_Case_Records
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
        Console.WriteLine($"Synchronize_Deleted_Case_Records {System.DateTime.Now}");

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
                            mmria.server.utils.c_sync_document sync_document = new mmria.server.utils.c_sync_document(kvp.Key, null, "DELETE", scheduleInfo.version_number, db_config, _couchDbHttpClient, _deIdentifiedRepository, _reportRepository);
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
                        string document_json = null;

                        try
                        {
                            document_json = await _caseRepository.GetCaseDocumentJsonAsync(kvp.Key, db_config);
                            if (!string.IsNullOrEmpty(document_json) && document_json.IndexOf("\"_id\":\"_design/\"") < 0)
                            {
                                #if !IS_PMSS_ENHANCED
                                mmria.server.utils.c_sync_document sync_document = new mmria.server.utils.c_sync_document(kvp.Key, document_json, "PUT", scheduleInfo.version_number, db_config, _couchDbHttpClient, _deIdentifiedRepository, _reportRepository);
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

}
