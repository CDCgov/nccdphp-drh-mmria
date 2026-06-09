using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;

namespace mmria.services.backup;

public sealed class BackupColdProcessor : ReceiveActor
{
    private static readonly string[] RequiredTenantDatabaseNames =
    {
        "configuration",
        "audit",
        "mmrds",
        "_users",
        "metadata",
        "jurisdiction",
        "session"
    };

    private static readonly string[] OptionalTenantDatabaseNames =
    {
        "offline_cases",
        "backups",
        "logging"
    };

    private static readonly string[] TenantDatabaseNames = RequiredTenantDatabaseNames
        .Concat(OptionalTenantDatabaseNames)
        .ToArray();

    private static readonly HashSet<string> OptionalTenantDatabaseNameSet =
        new(OptionalTenantDatabaseNames, StringComparer.OrdinalIgnoreCase);

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
        string targetFolder = null;
        int processedSegmentCount = 0;
        int readySegmentCount = 0;
        bool compressionQueued = false;

        try
        {
            mmria.common.couchdb.ConfigurationSet db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;
            string root_folder = db_config_set.name_value["backup_storage_root_folder"];

            runId = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-ddd");
            targetFolder = System.IO.Path.Combine(root_folder, runId);
            System.IO.Directory.CreateDirectory(targetFolder);

            var excludeFromBackupSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] excludedTenants = GetExcludedTenants(db_config_set, excludeFromBackupSet);

            LogRunStart(runId, targetFolder, excludedTenants);

            var backup = new Backup(_couchDbHttpClient);
            var databaseSummaries = new List<DatabaseBackupSummary>();
            var segmentSummaries = new List<SegmentBackupSummary>();

            string vitalImportFolder = System.IO.Path.Combine(targetFolder, "vital_import");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(vitalImportFolder, "_design"));

            WriteSegmentInProgressMarker(targetFolder, "vital_import", "vital_import", "Started", Enumerable.Empty<DatabaseBackupSummary>());
            LogDatabaseStart(runId, "vital_import", "vital_import");

            var vitalImportResult = await backup.Execute(
                new[]
                {
                    "backup",
                    "user_name:" + mmria.services.vitalsimport.Program.timer_user_name,
                    "password:" + mmria.services.vitalsimport.Program.timer_value,
                    $"database_url: {mmria.services.vitalsimport.Program.couchdb_url}/vital_import",
                    $"backup_file_path:{vitalImportFolder}"
                });

            var vitalImportDatabaseSummary = CreateDatabaseSummary("vital_import", "vital_import", vitalImportResult);
            var vitalImportDatabaseSummaries = new List<DatabaseBackupSummary>
            {
                vitalImportDatabaseSummary
            };

            databaseSummaries.AddRange(vitalImportDatabaseSummaries);
            LogDatabaseFinish(runId, vitalImportDatabaseSummary);
            WriteSegmentInProgressMarker(targetFolder, "vital_import", "vital_import", vitalImportDatabaseSummary.Status, vitalImportDatabaseSummaries);
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
                WriteSegmentInProgressMarker(targetFolder, prefix, null, "Started", prefixDatabaseSummaries);

                foreach(string databaseName in TenantDatabaseNames)
                {
                    string dbFolder = System.IO.Path.Combine(prefixFolder, databaseName);

                    Backup.BackupResultMessage backupResultMessage;
                    LogDatabaseStart(runId, prefix, databaseName);
                    WriteSegmentInProgressMarker(targetFolder, prefix, databaseName, "Started", prefixDatabaseSummaries);

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

                    var databaseSummary = CreateDatabaseSummary(prefix, databaseName, backupResultMessage, IsOptionalTenantDatabase(databaseName));
                    prefixDatabaseSummaries.Add(databaseSummary);
                    LogDatabaseFinish(runId, databaseSummary);
                    WriteSegmentInProgressMarker(targetFolder, prefix, databaseName, databaseSummary.Status, prefixDatabaseSummaries);
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
            WriteRunErrorMarker(targetFolder, runId, ex);
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
        Backup.BackupResultMessage backupResultMessage,
        bool isOptionalDatabase = false)
    {
        if(isOptionalDatabase && backupResultMessage?.IsMissingDatabase == true)
        {
            return new DatabaseBackupSummary(
                segmentName,
                databaseName,
                "Skipped",
                0,
                0,
                0,
                CreateSkippedOptionalDatabaseDetail(backupResultMessage));
        }

        return new DatabaseBackupSummary(
            segmentName,
            databaseName,
            backupResultMessage?.Status ?? "Error",
            backupResultMessage?.Doc_ID_Count ?? 0,
            backupResultMessage?.SuccessCount ?? 0,
            backupResultMessage?.ErrorCount ?? 0,
            NormalizeDetail(backupResultMessage?.Detail));
    }

    private static bool IsOptionalTenantDatabase(string databaseName)
    {
        return OptionalTenantDatabaseNameSet.Contains(databaseName);
    }

    private static string CreateSkippedOptionalDatabaseDetail(Backup.BackupResultMessage backupResultMessage)
    {
        string detail = NormalizeDetail(backupResultMessage?.Detail);
        return string.IsNullOrWhiteSpace(detail)
            ? "Optional database is missing; backup skipped."
            : $"Optional database is missing; backup skipped. {detail}";
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

    private static void LogDatabaseStart(string runId, string segmentName, string databaseName)
    {
        Console.WriteLine($"{GetRunPrefix(runId)} Segment='{segmentName}' Db='{databaseName}' Status=Started");
    }

    private static void LogDatabaseFinish(string runId, DatabaseBackupSummary summary)
    {
        Console.WriteLine(
            $"{GetRunPrefix(runId)} Segment='{summary.SegmentName}' Db='{summary.DatabaseName}' Status={summary.Status} DocumentCount={summary.DocumentCount} SuccessCount={summary.SuccessCount} ErrorCount={summary.ErrorCount}");
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
        int dbSkippedCount = databaseSummaries.Count(summary => summary.Status.Equals("Skipped", StringComparison.OrdinalIgnoreCase));
        int dbFailedCount = databaseSummaries.Count(summary =>
            !summary.Status.Equals("Success", StringComparison.OrdinalIgnoreCase) &&
            !summary.Status.Equals("Skipped", StringComparison.OrdinalIgnoreCase));
        int totalDocumentCount = databaseSummaries.Sum(summary => summary.DocumentCount);
        bool readyForCompression = segmentStatus.Equals("Success", StringComparison.OrdinalIgnoreCase);

        string segmentSummaryFileLine =
            $"Segment {segmentName} Status: {segmentStatus} DbAttempted: {dbAttemptedCount} DbSucceeded: {dbSucceededCount} DbSkipped: {dbSkippedCount} DbFailed: {dbFailedCount} DocumentCount: {totalDocumentCount}";

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
            dbSkippedCount,
            dbFailedCount,
            totalDocumentCount,
            markerResult.ReadyForCompression,
            markerResult.MarkerType,
            markerResult.MarkerPath);

        Console.WriteLine(
            $"{GetRunPrefix(runId)} Segment='{segmentSummary.SegmentName}' Status={segmentSummary.Status} DbAttempted={segmentSummary.DbAttemptedCount} DbSucceeded={segmentSummary.DbSucceededCount} DbSkipped={segmentSummary.DbSkippedCount} DbFailed={segmentSummary.DbFailedCount} DocumentCount={segmentSummary.TotalDocumentCount} Marker={segmentSummary.MarkerType} Path='{segmentSummary.MarkerPath}'");

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

    private static void WriteSegmentInProgressMarker(
        string targetFolder,
        string segmentName,
        string currentDatabaseName,
        string currentStatus,
        IEnumerable<DatabaseBackupSummary> completedDatabaseSummaries)
    {
        if(string.IsNullOrWhiteSpace(targetFolder))
        {
            return;
        }

        var currentDatabaseText = string.IsNullOrWhiteSpace(currentDatabaseName)
            ? "<none>"
            : currentDatabaseName;

        var documentText = new List<string>
        {
            $"Segment {segmentName} Status: InProgress CurrentDb: {currentDatabaseText} CurrentStatus: {currentStatus} LastUpdatedUtc: {DateTime.UtcNow:O}"
        };

        documentText.AddRange((completedDatabaseSummaries ?? Enumerable.Empty<DatabaseBackupSummary>()).Select(FormatBackupSummary));

        string inProgressFilePath = GetSegmentInProgressMarkerPath(targetFolder, segmentName);
        System.IO.File.WriteAllText(inProgressFilePath, string.Join(Environment.NewLine, documentText));
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
                $"Segment {summary.SegmentName} Status: {summary.Status} DbAttempted: {summary.DbAttemptedCount} DbSucceeded: {summary.DbSucceededCount} DbSkipped: {summary.DbSkippedCount} DbFailed: {summary.DbFailedCount} DocumentCount: {summary.TotalDocumentCount} Marker: {summary.MarkerType} Path: {summary.MarkerPath}");
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
        var in_progress_file_path = GetSegmentInProgressMarkerPath(targetFolder, segmentName);
        var file_contents = string.Join(Environment.NewLine, summaryLines ?? Enumerable.Empty<string>());

        if(System.IO.File.Exists(in_progress_file_path))
        {
            System.IO.File.Delete(in_progress_file_path);
        }

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

    private static string GetSegmentInProgressMarkerPath(string targetFolder, string segmentName)
    {
        return System.IO.Path.Combine(targetFolder, $"{segmentName}-in-progress.txt");
    }

    private static void WriteRunErrorMarker(string targetFolder, string runId, Exception exception)
    {
        if(string.IsNullOrWhiteSpace(targetFolder))
        {
            return;
        }

        try
        {
            System.IO.Directory.CreateDirectory(targetFolder);

            string fileContents = string.Join(
                Environment.NewLine,
                $"Run {runId} Status: Error LastUpdatedUtc: {DateTime.UtcNow:O}",
                $"Detail: {NormalizeDetail(exception?.ToString())}");

            System.IO.File.WriteAllText(System.IO.Path.Combine(targetFolder, "run-backup-error.txt"), fileContents);
        }
        catch(Exception markerException)
        {
            Console.WriteLine($"{GetRunPrefix(runId)} Failed to write run error marker. Detail='{NormalizeDetail(markerException.ToString())}'");
        }
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
        int DbSkippedCount,
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
   

