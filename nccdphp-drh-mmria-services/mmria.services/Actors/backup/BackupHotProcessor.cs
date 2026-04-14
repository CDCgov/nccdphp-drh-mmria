using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Akka.Actor;
using mmria.common.getset;

namespace mmria.services.backup;

public sealed class BackupHotProcessor : ReceiveActor
{
    private static readonly TimeSpan DefaultReplicationPollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan DefaultReplicationPollTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DefaultCrashingFailFastThreshold = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultProgressHeartbeatInterval = TimeSpan.FromSeconds(30);
    private static readonly HashSet<string> TerminalFailureStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "failed"
    };

    private readonly CouchDbHttpClient _couchDbHttpClient;
    private readonly TimeSpan _replicationPollInterval;
    private readonly TimeSpan _replicationPollTimeout;
    private readonly TimeSpan _crashingFailFastThreshold;
    private readonly TimeSpan _progressHeartbeatInterval;

    public BackupHotProcessor(CouchDbHttpClient couchDbHttpClient)
        : this(
            couchDbHttpClient,
            DefaultReplicationPollInterval,
            DefaultReplicationPollTimeout,
            DefaultCrashingFailFastThreshold,
            DefaultProgressHeartbeatInterval)
    {
    }

    public BackupHotProcessor(
        CouchDbHttpClient couchDbHttpClient,
        TimeSpan replicationPollInterval,
        TimeSpan replicationPollTimeout)
        : this(
            couchDbHttpClient,
            replicationPollInterval,
            replicationPollTimeout,
            DefaultCrashingFailFastThreshold,
            DefaultProgressHeartbeatInterval)
    {
    }

    public BackupHotProcessor(
        CouchDbHttpClient couchDbHttpClient,
        TimeSpan replicationPollInterval,
        TimeSpan replicationPollTimeout,
        TimeSpan crashingFailFastThreshold,
        TimeSpan progressHeartbeatInterval)
    {
        _couchDbHttpClient = couchDbHttpClient ?? throw new ArgumentNullException(nameof(couchDbHttpClient));
        _replicationPollInterval = replicationPollInterval <= TimeSpan.Zero
            ? DefaultReplicationPollInterval
            : replicationPollInterval;
        _replicationPollTimeout = replicationPollTimeout <= TimeSpan.Zero
            ? DefaultReplicationPollTimeout
            : replicationPollTimeout;
        _crashingFailFastThreshold = crashingFailFastThreshold <= TimeSpan.Zero
            ? DefaultCrashingFailFastThreshold
            : crashingFailFastThreshold;
        _progressHeartbeatInterval = progressHeartbeatInterval <= TimeSpan.Zero
            ? DefaultProgressHeartbeatInterval
            : progressHeartbeatInterval;

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


    private async Task Process_Message(mmria.services.backup.BackupSupervisor.PerformBackupMessage message)
    {
        string runId = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-ddd");
        int itemsPlanned = 0;
        int itemCountAttempted = 0;
        int successCount = 0;
        int failureCount = 0;
        int skippedCount = 0;

        try
        {
            mmria.common.couchdb.ConfigurationSet dbConfigSet = mmria.services.vitalsimport.Program.DbConfigSet;
            string backupDbUrl = TrimTrailingSlash(dbConfigSet.name_value["backup_db_url"]);
            string backupDbUser = dbConfigSet.name_value["backup_db_user"];
            string backupDbPassword = dbConfigSet.name_value["backup_db_user_value"];

            var workItems = BuildWorkItems(dbConfigSet).ToList();
            itemsPlanned = workItems.Count;
            WriteLog(runId, $"Starting hot backup. BackupDbUrl='{backupDbUrl}' ItemsPlanned={itemsPlanned}");

            foreach(ReplicationWorkItem workItem in workItems)
            {
                itemCountAttempted += 1;
                ReplicationExecutionResult result = await ExecuteReplicationWorkItemAsync(
                    runId,
                    workItem,
                    backupDbUrl,
                    backupDbUser,
                    backupDbPassword);

                if(result.Result.Equals("Success", StringComparison.OrdinalIgnoreCase))
                {
                    successCount += 1;
                }
                else
                {
                    failureCount += 1;
                }

                WriteLog(
                    runId,
                    $"Source='{workItem.SourceDisplayName}' Target='{workItem.TargetDatabaseName}' TargetDb={result.TargetDatabaseResult} ReplicationId='{result.ReplicationDocumentId ?? string.Empty}' Result={result.Result} State='{result.ReplicationState}'");

                if(!result.Result.Equals("Success", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(result.Detail))
                {
                    WriteLog(runId, $"Source='{workItem.SourceDisplayName}' Detail='{NormalizeDetail(result.Detail)}'");
                }

                if(result.ShouldStopRun)
                {
                    skippedCount = Math.Max(0, itemsPlanned - itemCountAttempted);
                    WriteLog(
                        runId,
                        $"Stopping hot backup after Source='{workItem.SourceDisplayName}' because the backup target became unreachable. ItemsSkipped={skippedCount}");
                    break;
                }
            }
        }
        catch(Exception ex)
        {
            failureCount += 1;
            WriteLog(runId, $"Run failed. Detail='{NormalizeDetail(ex.ToString())}'");
        }

        skippedCount = Math.Max(skippedCount, Math.Max(0, itemsPlanned - itemCountAttempted));

        if(message.ReturnToSender)
        {
            this.Sender.Tell(new mmria.services.backup.BackupSupervisor.BackupFinishedMessage()
            {
                type = "hot",
                DateEnded = DateTime.Now
            });
        }

        WriteLog(
            runId,
            $"Completed hot backup. ItemsPlanned={itemsPlanned} ItemsAttempted={itemCountAttempted} Succeeded={successCount} Failed={failureCount} Skipped={skippedCount}");
        Context.Stop(this.Self);
    }

    private IEnumerable<ReplicationWorkItem> BuildWorkItems(mmria.common.couchdb.ConfigurationSet dbConfigSet)
    {
        yield return new ReplicationWorkItem(
            SourceDisplayName: "vital_import",
            SourceUrl: $"{TrimTrailingSlash(mmria.services.vitalsimport.Program.couchdb_url)}/vital_import",
            SourceUserName: mmria.services.vitalsimport.Program.timer_user_name,
            SourcePassword: mmria.services.vitalsimport.Program.timer_value,
            TargetDatabaseName: "vital_import");

        string[] replicationDatabases =
        {
            "audit",
            "mmrds",
            "jurisdiction",
            "session",
            "_users",
            "configuration",
            "offline_cases"
        };

        foreach(var kvp in dbConfigSet.detail_list)
        {
            if(kvp.Key.Equals("vital_import", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string prefix = kvp.Key.ToLowerInvariant();
            var dataConnection = kvp.Value;

            foreach(string sourceDatabaseName in replicationDatabases)
            {
                yield return new ReplicationWorkItem(
                    SourceDisplayName: $"{prefix}/{sourceDatabaseName}",
                    SourceUrl: $"{TrimTrailingSlash(dataConnection.url)}/{sourceDatabaseName}",
                    SourceUserName: dataConnection.user_name,
                    SourcePassword: dataConnection.user_value,
                    TargetDatabaseName: sourceDatabaseName.StartsWith("_", StringComparison.Ordinal)
                        ? $"{prefix}{sourceDatabaseName}"
                        : $"{prefix}_{sourceDatabaseName}");
            }
        }
    }

    private async Task<ReplicationExecutionResult> ExecuteReplicationWorkItemAsync(
        string runId,
        ReplicationWorkItem workItem,
        string backupDbUrl,
        string backupDbUser,
        string backupDbPassword)
    {
        string targetDatabaseResultLabel = "Unavailable";
        string replicationDocumentId = null;
        string outageStage = "target DB creation";

        try
        {
            TargetDatabaseCreateResult targetDatabaseResult = await EnsureTargetDatabaseAsync(
                backupDbUrl,
                workItem.TargetDatabaseName,
                backupDbUser,
                backupDbPassword);

            targetDatabaseResultLabel = targetDatabaseResult.Result;

            if(!targetDatabaseResult.IsSuccess)
            {
                return new ReplicationExecutionResult(
                    TargetDatabaseResult: targetDatabaseResult.Result,
                    ReplicationDocumentId: null,
                    Result: "Error",
                    ReplicationState: "target_db_create_failed",
                    Detail: targetDatabaseResult.Detail,
                    ShouldStopRun: false);
            }

            outageStage = "replication submission";

            ReplicationSubmissionResult submissionResult = await SubmitReplicationAsync(
                workItem,
                backupDbUrl,
                backupDbUser,
                backupDbPassword);

            replicationDocumentId = submissionResult.ReplicationDocumentId;

            if(!submissionResult.IsSuccess)
            {
                return new ReplicationExecutionResult(
                    TargetDatabaseResult: targetDatabaseResult.Result,
                    ReplicationDocumentId: submissionResult.ReplicationDocumentId,
                    Result: "Error",
                    ReplicationState: "submission_failed",
                    Detail: submissionResult.Detail,
                    ShouldStopRun: false);
            }

            WriteLog(
                runId,
                $"Submitted Source='{workItem.SourceDisplayName}' Target='{workItem.TargetDatabaseName}' TargetDb={targetDatabaseResult.Result} ReplicationId='{submissionResult.ReplicationDocumentId}'");

            outageStage = "replication status check";

            ReplicationCompletionResult completionResult = await WaitForReplicationCompletionAsync(
                runId,
                workItem,
                backupDbUrl,
                backupDbUser,
                backupDbPassword,
                submissionResult.ReplicationDocumentId);

            return new ReplicationExecutionResult(
                TargetDatabaseResult: targetDatabaseResult.Result,
                ReplicationDocumentId: submissionResult.ReplicationDocumentId,
                Result: completionResult.IsSuccess ? "Success" : "Error",
                ReplicationState: completionResult.ReplicationState,
                Detail: completionResult.Detail,
                ShouldStopRun: false);
        }
        catch(Exception ex) when (IsBackupTargetTransportFailure(ex))
        {
            return CreateBackupTargetUnreachableResult(
                workItem,
                targetDatabaseResultLabel,
                replicationDocumentId,
                backupDbUrl,
                outageStage,
                ex);
        }
    }

    private async Task<TargetDatabaseCreateResult> EnsureTargetDatabaseAsync(
        string backupDbUrl,
        string targetDatabaseName,
        string backupDbUser,
        string backupDbPassword)
    {
        string targetDatabaseUrl = CombineUrl(backupDbUrl, targetDatabaseName);
        CouchDbHttpResponse response = await _couchDbHttpClient.ExecuteForResponseAsync(
            "PUT",
            targetDatabaseUrl,
            payload: null,
            contentType: "application/json",
            requestOptions: CreateSilentRequestOptions(backupDbUser, backupDbPassword));

        if(response.StatusCode == 201)
        {
            return new TargetDatabaseCreateResult(true, "Created", null);
        }

        if(response.StatusCode == 412)
        {
            return new TargetDatabaseCreateResult(true, "AlreadyExists", null);
        }

        return new TargetDatabaseCreateResult(
            false,
            "CreateFailed",
            $"Target DB create failed for {targetDatabaseName}. HTTP {response.StatusCode}. Response: {PreviewBody(response.Body)}");
    }

    private async Task<ReplicationSubmissionResult> SubmitReplicationAsync(
        ReplicationWorkItem workItem,
        string backupDbUrl,
        string backupDbUser,
        string backupDbPassword)
    {
        string replicationUrl = CombineUrl(backupDbUrl, "_replicator");

        var replicateStruct = new Replicate_Struct();
        replicateStruct.source.url = workItem.SourceUrl;
        replicateStruct.source.headers.Authorization = BuildBasicAuthValue(workItem.SourceUserName, workItem.SourcePassword);
        replicateStruct.target.url = CombineUrl(backupDbUrl, workItem.TargetDatabaseName);
        replicateStruct.target.headers.Authorization = BuildBasicAuthValue(backupDbUser, backupDbPassword);
        replicateStruct.create_target = false;
        replicateStruct.continuous = false;

        var settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
        };
        string payload = Newtonsoft.Json.JsonConvert.SerializeObject(replicateStruct, settings);

        CouchDbHttpResponse response = await _couchDbHttpClient.ExecuteForResponseAsync(
            "POST",
            replicationUrl,
            payload,
            "application/json",
            CreateSilentRequestOptions(backupDbUser, backupDbPassword));

        if(response.StatusCode < 200 || response.StatusCode >= 300)
        {
            return new ReplicationSubmissionResult(
                false,
                null,
                $"Replication submission failed for {workItem.SourceDisplayName}. HTTP {response.StatusCode}. Response: {PreviewBody(response.Body)}");
        }

        string replicationDocumentId = TryGetJsonString(response.Body, "id");
        if(string.IsNullOrWhiteSpace(replicationDocumentId))
        {
            return new ReplicationSubmissionResult(
                false,
                null,
                $"Replication submission response did not include an id for {workItem.SourceDisplayName}. Response: {PreviewBody(response.Body)}");
        }

        return new ReplicationSubmissionResult(true, replicationDocumentId, null);
    }

    private async Task<ReplicationCompletionResult> WaitForReplicationCompletionAsync(
        string runId,
        ReplicationWorkItem workItem,
        string backupDbUrl,
        string backupDbUser,
        string backupDbPassword,
        string replicationDocumentId)
    {
        string replicationDocumentUrl = CombineUrl(backupDbUrl, "_scheduler", "docs", "_replicator", Uri.EscapeDataString(replicationDocumentId));
        DateTime deadline = DateTime.UtcNow.Add(_replicationPollTimeout);
        string lastLoggedState = null;
        string lastLoggedDetail = null;
        DateTime lastProgressLogAt = DateTime.MinValue;
        string crashingReason = null;
        DateTime? crashingSince = null;

        while(DateTime.UtcNow <= deadline)
        {
            CouchDbHttpResponse response = await _couchDbHttpClient.ExecuteForResponseAsync(
                "GET",
                replicationDocumentUrl,
                payload: null,
                contentType: "application/json",
                requestOptions: CreateSilentRequestOptions(backupDbUser, backupDbPassword));

            if(response.StatusCode == 404)
            {
                await Task.Delay(_replicationPollInterval);
                continue;
            }

            if(response.StatusCode < 200 || response.StatusCode >= 300)
            {
                return new ReplicationCompletionResult(
                    false,
                    "status_check_failed",
                    $"Replication status check failed for {replicationDocumentId}. HTTP {response.StatusCode}. Response: {PreviewBody(response.Body)}");
            }

            ReplicationStateStatus stateStatus = ParseReplicationState(response.Body);
            DateTime now = DateTime.UtcNow;

            if(ShouldWriteProgressLog(stateStatus, lastLoggedState, lastLoggedDetail, lastProgressLogAt, now))
            {
                WriteLog(
                    runId,
                    $"Progress Source='{workItem.SourceDisplayName}' ReplicationId='{replicationDocumentId}' State='{stateStatus.ReplicationState}' Detail='{NormalizeDetail(stateStatus.Detail)}'");
                lastLoggedState = stateStatus.ReplicationState;
                lastLoggedDetail = NormalizeDetail(stateStatus.Detail);
                lastProgressLogAt = now;
            }

            if(stateStatus.IsTerminal)
            {
                return new ReplicationCompletionResult(stateStatus.IsSuccess, stateStatus.ReplicationState, stateStatus.Detail);
            }

            if(stateStatus.ReplicationState.Equals("crashing", StringComparison.OrdinalIgnoreCase))
            {
                string normalizedCrashingReason = NormalizeDetail(stateStatus.Detail);
                if(string.Equals(crashingReason, normalizedCrashingReason, StringComparison.Ordinal))
                {
                    if(crashingSince.HasValue && now - crashingSince.Value >= _crashingFailFastThreshold)
                    {
                        return new ReplicationCompletionResult(
                            false,
                            "crashing",
                            $"Replication remained in crashing state for {_crashingFailFastThreshold.TotalSeconds:#0} seconds. {normalizedCrashingReason}");
                    }
                }
                else
                {
                    crashingReason = normalizedCrashingReason;
                    crashingSince = now;
                }
            }
            else
            {
                crashingReason = null;
                crashingSince = null;
            }

            await Task.Delay(_replicationPollInterval);
        }

        return new ReplicationCompletionResult(
            false,
            "timeout",
            $"Replication {replicationDocumentId} did not complete within {_replicationPollTimeout.TotalMinutes:#0} minutes.");
    }

    private static ReplicationStateStatus ParseReplicationState(string responseBody)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(responseBody);
            JsonElement root = document.RootElement;

            string replicationState = TryGetJsonString(root, "state") ?? "pending";
            string detail = ExtractSchedulerDetail(root);

            if(replicationState.Equals("completed", StringComparison.OrdinalIgnoreCase))
            {
                return new ReplicationStateStatus(true, true, "completed", detail);
            }

            if(TerminalFailureStates.Contains(replicationState))
            {
                return new ReplicationStateStatus(true, false, replicationState, detail);
            }

            return new ReplicationStateStatus(false, false, replicationState, detail);
        }
        catch(JsonException ex)
        {
            return new ReplicationStateStatus(
                true,
                false,
                "invalid_status_response",
                $"Invalid replication status response. {NormalizeDetail(ex.Message)} Body: {PreviewBody(responseBody)}");
        }
    }

    private static CouchDbRequestOptions CreateSilentRequestOptions(string userName, string password)
    {
        return new CouchDbRequestOptions
        {
            UserName = userName,
            Password = password,
            SuppressErrorLogging = true
        };
    }

    private bool ShouldWriteProgressLog(
        ReplicationStateStatus stateStatus,
        string lastLoggedState,
        string lastLoggedDetail,
        DateTime lastProgressLogAt,
        DateTime now)
    {
        string normalizedDetail = NormalizeDetail(stateStatus.Detail);
        if(!string.Equals(lastLoggedState, stateStatus.ReplicationState, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if(!string.Equals(lastLoggedDetail, normalizedDetail, StringComparison.Ordinal))
        {
            return true;
        }

        return lastProgressLogAt == DateTime.MinValue ||
            now - lastProgressLogAt >= _progressHeartbeatInterval;
    }

    private static string ExtractSchedulerDetail(JsonElement root)
    {
        if(root.TryGetProperty("info", out JsonElement infoElement))
        {
            if(infoElement.ValueKind == JsonValueKind.String)
            {
                return NormalizeDetail(infoElement.GetString());
            }

            if(infoElement.ValueKind == JsonValueKind.Object)
            {
                string error = TryGetJsonString(infoElement, "error");
                if(!string.IsNullOrWhiteSpace(error))
                {
                    return NormalizeDetail(error);
                }

                return NormalizeDetail(infoElement.ToString());
            }
        }

        string stateReason = TryGetJsonString(root, "reason");
        if(!string.IsNullOrWhiteSpace(stateReason))
        {
            return NormalizeDetail(stateReason);
        }

        return string.Empty;
    }

    private static string CombineUrl(string baseUrl, string pathSegment)
    {
        return $"{TrimTrailingSlash(baseUrl)}/{pathSegment.TrimStart('/')}";
    }

    private static string CombineUrl(string baseUrl, string pathSegment1, string pathSegment2)
    {
        return $"{TrimTrailingSlash(baseUrl)}/{pathSegment1.Trim('/').Trim()}/{pathSegment2.TrimStart('/')}";
    }

    private static string CombineUrl(string baseUrl, string pathSegment1, string pathSegment2, string pathSegment3, string pathSegment4)
    {
        return $"{TrimTrailingSlash(baseUrl)}/{pathSegment1.Trim('/')}/{pathSegment2.Trim('/')}/{pathSegment3.Trim('/')}/{pathSegment4.TrimStart('/')}";
    }

    private static string TrimTrailingSlash(string url)
    {
        return string.IsNullOrWhiteSpace(url)
            ? string.Empty
            : url.Trim().TrimEnd('/');
    }

    private static string TryGetJsonString(string json, string propertyName)
    {
        if(string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return TryGetJsonString(document.RootElement, propertyName);
        }
        catch(JsonException)
        {
            return null;
        }
    }

    private static string TryGetJsonString(JsonElement element, string propertyName)
    {
        if(!element.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null => null,
            _ => value.ToString()
        };
    }

    private static string PreviewBody(string body)
    {
        if(string.IsNullOrWhiteSpace(body))
        {
            return "(empty response)";
        }

        string normalized = NormalizeDetail(body);
        return normalized.Length <= 256
            ? normalized
            : normalized.Substring(0, 256);
    }

    private static string NormalizeDetail(string detail)
    {
        if(string.IsNullOrWhiteSpace(detail))
        {
            return string.Empty;
        }

        return detail.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static bool IsBackupTargetTransportFailure(Exception exception)
    {
        return exception is HttpRequestException || exception is OperationCanceledException;
    }

    private static ReplicationExecutionResult CreateBackupTargetUnreachableResult(
        ReplicationWorkItem workItem,
        string targetDatabaseResult,
        string replicationDocumentId,
        string backupDbUrl,
        string outageStage,
        Exception exception)
    {
        return new ReplicationExecutionResult(
            TargetDatabaseResult: string.IsNullOrWhiteSpace(targetDatabaseResult) ? "Unavailable" : targetDatabaseResult,
            ReplicationDocumentId: replicationDocumentId,
            Result: "Error",
            ReplicationState: "backup_target_unreachable",
            Detail: DescribeBackupTargetOutage(workItem, backupDbUrl, outageStage, exception),
            ShouldStopRun: true);
    }

    private static string DescribeBackupTargetOutage(
        ReplicationWorkItem workItem,
        string backupDbUrl,
        string outageStage,
        Exception exception)
    {
        string issueLabel = exception is OperationCanceledException
            ? "Backup target request timed out or was canceled"
            : "Backup target unreachable";

        return $"{issueLabel} during {outageStage} for {workItem.SourceDisplayName} via {backupDbUrl}. {NormalizeDetail(exception.Message)}";
    }

    private static string GetRunPrefix(string runId)
    {
        return $"[HotBackup][{runId}]";
    }

    private static void WriteLog(string runId, string message)
    {
        Console.WriteLine($"{GetRunPrefix(runId)} {message}");
    }

    private static string BuildBasicAuthValue(string userName, string password)
    {
        byte[] credentialBytes = null;
        char[] encodedChars = null;
        try
        {
            int userByteCount = Encoding.UTF8.GetByteCount(userName);
            int passByteCount = Encoding.UTF8.GetByteCount(password);
            int totalLen = userByteCount + 1 + passByteCount;
            credentialBytes = new byte[totalLen];

            int offset = Encoding.UTF8.GetBytes(userName.AsSpan(), credentialBytes);
            credentialBytes[offset++] = (byte)':';
            Encoding.UTF8.GetBytes(password.AsSpan(), credentialBytes.AsSpan(offset));

            int base64Len = ((totalLen + 2) / 3) * 4;
            encodedChars = new char[base64Len];

            if (!Convert.TryToBase64Chars(credentialBytes.AsSpan(0, totalLen), encodedChars, out int charsWritten))
            {
                throw new InvalidOperationException("Failed to encode credentials.");
            }

            return "Basic " + new string(encodedChars, 0, charsWritten);
        }
        finally
        {
            if (credentialBytes != null)
            {
                CryptographicOperations.ZeroMemory(credentialBytes);
            }
            if (encodedChars != null)
            {
                Array.Clear(encodedChars, 0, encodedChars.Length);
            }
        }
    }

    private sealed record ReplicationWorkItem(
        string SourceDisplayName,
        string SourceUrl,
        string SourceUserName,
        string SourcePassword,
        string TargetDatabaseName);

    private sealed record TargetDatabaseCreateResult(
        bool IsSuccess,
        string Result,
        string Detail);

    private sealed record ReplicationSubmissionResult(
        bool IsSuccess,
        string ReplicationDocumentId,
        string Detail);

    private sealed record ReplicationCompletionResult(
        bool IsSuccess,
        string ReplicationState,
        string Detail);

    private sealed record ReplicationStateStatus(
        bool IsTerminal,
        bool IsSuccess,
        string ReplicationState,
        string Detail);

    private sealed record ReplicationExecutionResult(
        string TargetDatabaseResult,
        string ReplicationDocumentId,
        string Result,
        string ReplicationState,
        string Detail,
        bool ShouldStopRun);
}


     

   

