using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;

namespace mmria.services.backup;

public sealed class BackupColdProcessor : ReceiveActor
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public BackupColdProcessor(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient ?? throw new ArgumentNullException(nameof(couchDbHttpClient));
        Become(Waiting);
    }

    void Processing()
    {
        Receive<mmria.services.backup.BackupSupervisor.PerformBackupMessage>(_ =>
        {
            // discard message while processing
        });
    }

    void Waiting()
    {
        ReceiveAsync<mmria.services.backup.BackupSupervisor.PerformBackupMessage>(async message =>
        {
            Become(Processing);
            await Process_Message(message);
        });
    }

    async Task Process_Message(mmria.services.backup.BackupSupervisor.PerformBackupMessage message)
    {
        string runId = "pending";
        int processedSegmentCount = 0;
        int readySegmentCount = 0;
        bool compressionQueued = false;

        try
        {
            mmria.common.couchdb.ConfigurationSet db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;
            string root_folder = db_config_set.name_value["backup_storage_root_folder"];

            var databaseNames = new[]
            {
                "configuration",
                "audit",
                "mmrds",
                "_users",
                "metadata",
                "jurisdiction",
                "session"
            };

            runId = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-ddd");
            string targetFolder = System.IO.Path.Combine(root_folder, runId);
            System.IO.Directory.CreateDirectory(targetFolder);

            var excludeFromBackupSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] excludedTenants = GetExcludedTenants(db_config_set, excludeFromBackupSet);

            LogRunStart(runId, targetFolder, excludedTenants);

            var backup = new Backup(_couchDbHttpClient);
            var databaseSummaries = new List<DatabaseBackupSummary>();
            var segmentSummaries = new List<SegmentBackupSummary>();

            string vitalImportFolder = System.IO.Path.Combine(targetFolder, "vital_import");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(vitalImportFolder, "_design"));

            var vitalImportResult = await backup.Execute(
                new[]
                {
                    "backup",
                    "user_name:" + mmria.services.vitalsimport.Program.timer_user_name,
                    "password:" + mmria.services.vitalsimport.Program.timer_value,
                    $"database_url: {mmria.services.vitalsimport.Program.couchdb_url}/vital_import",
                    $"backup_file_path:{vitalImportFolder}"
                });

            var vitalImportDatabaseSummaries = new List<DatabaseBackupSummary>
            {
                CreateDatabaseSummary("vital_import", "vital_import", vitalImportResult)
            };

            databaseSummaries.AddRange(vitalImportDatabaseSummaries);
            LogDatabaseIssues(runId, vitalImportDatabaseSummaries);

            var vitalImportSegmentSummary = FinalizeSegment(targetFolder, runId, "vital_import", vitalImportDatabaseSummaries);
            segmentSummaries.Add(vitalImportSegmentSummary);
            processedSegmentCount += 1;
            if(vitalImportSegmentSummary.ReadyForCompression)
            {
                readySegmentCount += 1;
            }

            foreach(var kvp in db_config_set.detail_list)
            {
                string prefix = kvp.Key.ToLowerInvariant();
                var data_connection = kvp.Value;

                if(kvp.Key.Equals("vital_import", StringComparison.OrdinalIgnoreCase) ||
                    excludeFromBackupSet.Contains(kvp.Key))
                {
                    continue;
                }

                string prefixFolder = System.IO.Path.Combine(targetFolder, prefix);
                System.IO.Directory.CreateDirectory(prefixFolder);

                var prefixDatabaseSummaries = new List<DatabaseBackupSummary>();

                foreach(string databaseName in databaseNames)
                {
                    string dbFolder = System.IO.Path.Combine(prefixFolder, databaseName);
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dbFolder, "_design"));

                    Backup.BackupResultMessage backupResultMessage;

                    try
                    {
                        backupResultMessage = await backup.Execute(
                            new[]
                            {
                                "backup",
                                "user_name:" + data_connection.user_name,
                                "password:" + data_connection.user_value,
                                $"database_url:{data_connection.url}/{databaseName}",
                                $"backup_file_path:{dbFolder}"
                            });
                    }
                    catch(Exception ex)
                    {
                        backupResultMessage = new Backup.BackupResultMessage()
                        {
                            Status = "Error",
                            Detail = ex.ToString(),
                            SuccessCount = 0,
                            ErrorCount = 1,
                            Doc_ID_Count = 0
                        };
                    }

                    prefixDatabaseSummaries.Add(CreateDatabaseSummary(prefix, databaseName, backupResultMessage));
                }

                databaseSummaries.AddRange(prefixDatabaseSummaries);
                LogDatabaseIssues(runId, prefixDatabaseSummaries);

                var segmentSummary = FinalizeSegment(targetFolder, runId, prefix, prefixDatabaseSummaries);
                segmentSummaries.Add(segmentSummary);
                processedSegmentCount += 1;
                if(segmentSummary.ReadyForCompression)
                {
                    readySegmentCount += 1;
                }
            }

            WriteCountFiles(root_folder, targetFolder, runId, segmentSummaries, databaseSummaries);

            if(readySegmentCount > 0)
            {
                compressionQueued = true;
                LogCompressionQueued(runId, readySegmentCount);

                var file_compressor = Context.ActorSelection("akka://mmria-actor-system/user/backup-supervisor");
                file_compressor.Tell(new mmria.services.backup.BackupSupervisor.PerformBackupMessage()
                {
                    type = "compress",
                    ReturnToSender = false
                });
            }
            else
            {
                LogCompressionSkipped(runId);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"{GetRunPrefix(runId)} Run failed. Detail='{NormalizeDetail(ex.ToString())}'");
        }

        if(message.ReturnToSender)
        {
            this.Sender.Tell(new mmria.services.backup.BackupSupervisor.BackupFinishedMessage()
            {
                type = "cold",
                DateEnded = DateTime.Now
            });
        }

        int failedSegmentCount = Math.Max(0, processedSegmentCount - readySegmentCount);
        Console.WriteLine($"{GetRunPrefix(runId)} Completed cold backup. SegmentsProcessed={processedSegmentCount} ReadyForCompression={readySegmentCount} FailedSegments={failedSegmentCount} CompressionQueued={compressionQueued}");

        Context.Stop(this.Self);
    }

    private static DatabaseBackupSummary CreateDatabaseSummary(
        string segmentName,
        string databaseName,
        Backup.BackupResultMessage backupResultMessage)
    {
        return new DatabaseBackupSummary(
            segmentName,
            databaseName,
            backupResultMessage?.Status ?? "Error",
            backupResultMessage?.Doc_ID_Count ?? 0,
            backupResultMessage?.SuccessCount ?? 0,
            backupResultMessage?.ErrorCount ?? 0,
            NormalizeDetail(backupResultMessage?.Detail));
    }

    private static void LogRunStart(string runId, string targetFolder, IReadOnlyCollection<string> excludedTenants)
    {
        string excludedTenantText = excludedTenants.Count == 0
            ? "<none>"
            : string.Join(", ", excludedTenants);

        Console.WriteLine($"{GetRunPrefix(runId)} Starting cold backup. TargetFolder='{targetFolder}' ExcludedTenants='{excludedTenantText}'");
    }

    private static string[] GetExcludedTenants(
        mmria.common.couchdb.ConfigurationSet configurationSet,
        ISet<string> excludeFromBackupSet)
    {
        if(!configurationSet.name_value.TryGetValue("exclude_from_backup_list", out string excludedTenantList) ||
            string.IsNullOrWhiteSpace(excludedTenantList))
        {
            return Array.Empty<string>();
        }

        var tenants = excludedTenantList
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();

        foreach(string tenant in tenants)
        {
            excludeFromBackupSet.Add(tenant);
        }

        return tenants;
    }

    private static void LogDatabaseIssues(string runId, IEnumerable<DatabaseBackupSummary> databaseSummaries)
    {
        foreach(DatabaseBackupSummary summary in databaseSummaries.Where(ShouldLogDatabaseIssue))
        {
            Console.WriteLine(
                $"{GetRunPrefix(runId)} Segment='{summary.SegmentName}' Db='{summary.DatabaseName}' Status={summary.Status} DocumentCount={summary.DocumentCount} SuccessCount={summary.SuccessCount} ErrorCount={summary.ErrorCount} Detail='{summary.Detail}'");
        }
    }

    private static bool ShouldLogDatabaseIssue(DatabaseBackupSummary summary)
    {
        return !summary.Status.Equals("Success", StringComparison.OrdinalIgnoreCase);
    }

    private static SegmentBackupSummary FinalizeSegment(
        string targetFolder,
        string runId,
        string segmentName,
        IReadOnlyCollection<DatabaseBackupSummary> databaseSummaries)
    {
        string segmentStatus = GetSegmentStatus(databaseSummaries);
        int dbAttemptedCount = databaseSummaries.Count;
        int dbSucceededCount = databaseSummaries.Count(summary => summary.Status.Equals("Success", StringComparison.OrdinalIgnoreCase));
        int dbFailedCount = dbAttemptedCount - dbSucceededCount;
        int totalDocumentCount = databaseSummaries.Sum(summary => summary.DocumentCount);
        bool readyForCompression = segmentStatus.Equals("Success", StringComparison.OrdinalIgnoreCase);

        string segmentSummaryFileLine =
            $"Segment {segmentName} Status: {segmentStatus} DbAttempted: {dbAttemptedCount} DbSucceeded: {dbSucceededCount} DbFailed: {dbFailedCount} DocumentCount: {totalDocumentCount}";

        var markerResult = WriteSegmentMarker(
            targetFolder,
            segmentName,
            readyForCompression,
            new[] { segmentSummaryFileLine }.Concat(databaseSummaries.Select(FormatBackupSummary)));

        var segmentSummary = new SegmentBackupSummary(
            segmentName,
            segmentStatus,
            dbAttemptedCount,
            dbSucceededCount,
            dbFailedCount,
            totalDocumentCount,
            markerResult.ReadyForCompression,
            markerResult.MarkerType,
            markerResult.MarkerPath);

        Console.WriteLine(
            $"{GetRunPrefix(runId)} Segment='{segmentSummary.SegmentName}' Status={segmentSummary.Status} DbAttempted={segmentSummary.DbAttemptedCount} DbSucceeded={segmentSummary.DbSucceededCount} DbFailed={segmentSummary.DbFailedCount} DocumentCount={segmentSummary.TotalDocumentCount} Marker={segmentSummary.MarkerType} Path='{segmentSummary.MarkerPath}'");

        return segmentSummary;
    }

    private static string GetSegmentStatus(IEnumerable<DatabaseBackupSummary> databaseSummaries)
    {
        bool hasError = false;
        bool hasPartial = false;

        foreach(DatabaseBackupSummary summary in databaseSummaries)
        {
            if(summary.Status.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
                summary.Status.Equals("Validation Error", StringComparison.OrdinalIgnoreCase))
            {
                hasError = true;
                break;
            }

            if(summary.Status.Equals("Partial", StringComparison.OrdinalIgnoreCase))
            {
                hasPartial = true;
            }
        }

        if(hasError)
        {
            return "Error";
        }

        if(hasPartial)
        {
            return "Partial";
        }

        return "Success";
    }

    private static string FormatBackupSummary(DatabaseBackupSummary backupSummary)
    {
        return $"{backupSummary.SegmentName} {backupSummary.DatabaseName} BackupStatus: {backupSummary.Status} DocCount: {backupSummary.DocumentCount} SuccessCount: {backupSummary.SuccessCount} ErrorCount: {backupSummary.ErrorCount} Detail: {backupSummary.Detail}";
    }

    private static void WriteCountFiles(
        string rootFolder,
        string targetFolder,
        string runId,
        IReadOnlyCollection<SegmentBackupSummary> segmentSummaries,
        IReadOnlyCollection<DatabaseBackupSummary> databaseSummaries)
    {
        List<string> documentText = new();

        foreach(SegmentBackupSummary summary in segmentSummaries.OrderBy(summary => summary.SegmentName, StringComparer.OrdinalIgnoreCase))
        {
            documentText.Add(
                $"Segment {summary.SegmentName} Status: {summary.Status} DbAttempted: {summary.DbAttemptedCount} DbSucceeded: {summary.DbSucceededCount} DbFailed: {summary.DbFailedCount} DocumentCount: {summary.TotalDocumentCount} Marker: {summary.MarkerType} Path: {summary.MarkerPath}");
        }

        foreach(DatabaseBackupSummary summary in databaseSummaries
            .OrderBy(summary => summary.SegmentName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(summary => summary.DatabaseName, StringComparer.OrdinalIgnoreCase))
        {
            documentText.Add(FormatBackupSummary(summary));
        }

        string fileContents = string.Join(Environment.NewLine, documentText);

        string countFilePath = System.IO.Path.Combine(targetFolder, "db_record_count.txt");
        System.IO.File.WriteAllText(countFilePath, fileContents);

        countFilePath = System.IO.Path.Combine(rootFolder, $"{runId}-db_record_count.txt");
        System.IO.File.WriteAllText(countFilePath, fileContents);
    }

    private static void LogCompressionQueued(string runId, int readySegmentCount)
    {
        Console.WriteLine($"{GetRunPrefix(runId)} Compression queued for {readySegmentCount} ready segment(s).");
    }

    private static void LogCompressionSkipped(string runId)
    {
        Console.WriteLine($"{GetRunPrefix(runId)} Compression skipped because no segments were fully successful.");
    }

    private static string NormalizeDetail(string detail)
    {
        if(string.IsNullOrWhiteSpace(detail))
        {
            return "";
        }

        return detail
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
    }

    private static SegmentMarkerWriteResult WriteSegmentMarker(string targetFolder, string segmentName, bool readyForCompression, IEnumerable<string> summaryLines)
    {
        var ready_file_path = System.IO.Path.Combine(targetFolder, $"{segmentName}-ready-for-compression.txt");
        var error_file_path = System.IO.Path.Combine(targetFolder, $"{segmentName}-backup-error.txt");
        var file_contents = string.Join(Environment.NewLine, summaryLines ?? Enumerable.Empty<string>());

        if(System.IO.File.Exists(ready_file_path))
        {
            System.IO.File.Delete(ready_file_path);
        }

        if(System.IO.File.Exists(error_file_path))
        {
            System.IO.File.Delete(error_file_path);
        }

        if(readyForCompression)
        {
            System.IO.File.WriteAllText(ready_file_path, file_contents);
            return new SegmentMarkerWriteResult(true, "ready", ready_file_path);
        }

        System.IO.File.WriteAllText(error_file_path, file_contents);
        return new SegmentMarkerWriteResult(false, "error", error_file_path);
    }

    private static string GetRunPrefix(string runId)
    {
        return $"[ColdBackup][{runId}]";
    }

    private sealed record DatabaseBackupSummary(
        string SegmentName,
        string DatabaseName,
        string Status,
        int DocumentCount,
        int SuccessCount,
        int ErrorCount,
        string Detail);

    private sealed record SegmentBackupSummary(
        string SegmentName,
        string Status,
        int DbAttemptedCount,
        int DbSucceededCount,
        int DbFailedCount,
        int TotalDocumentCount,
        bool ReadyForCompression,
        string MarkerType,
        string MarkerPath);

    private sealed record SegmentMarkerWriteResult(
        bool ReadyForCompression,
        string MarkerType,
        string MarkerPath);

}
   

