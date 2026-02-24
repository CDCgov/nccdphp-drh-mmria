#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.getset;

namespace mmria.services.tests;

/// <summary>
/// JobDataHelper provides utilities for creating and managing background jobs and events.
/// Simplifies test setup for background processing scenarios.
/// </summary>
public class JobDataHelper
{
    private readonly CouchDbHttpClient _httpClient;
    private readonly string _databaseUrl;
    private readonly string? _userName;
    private readonly string? _password;

    public JobDataHelper(CouchDbHttpClient httpClient, string databaseUrl, string? userName = null, string? password = null)
    {
        _httpClient = httpClient;
        _databaseUrl = databaseUrl;
        _userName = userName;
        _password = password;
    }

    /// <summary>
    /// Create a new background job record
    /// </summary>
    public Dictionary<string, object> CreateJob(string jobType, Dictionary<string, object>? payload = null)
    {
        var jobId = $"job-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        
        var job = new Dictionary<string, object>
        {
            { "_id", jobId },
            { "job_type", jobType },
            { "status", "pending" },
            { "created_date", DateTime.UtcNow },
            { "created_by", "test_system" },
            { "retry_count", 0 },
            { "max_retries", 3 },
            { "next_execution_date", DateTime.UtcNow },
            { "error_count", 0 },
            { "last_error", null }
        };

        if (payload != null)
        {
            job["payload"] = payload;
        }

        return job;
    }

    /// <summary>
    /// Create a job with specific cron schedule
    /// </summary>
    public Dictionary<string, object> CreateScheduledJob(string jobType, string cronExpression, Dictionary<string, object>? payload = null)
    {
        var job = CreateJob(jobType, payload);
        job["cron_expression"] = cronExpression;
        job["is_recurring"] = true;
        return job;
    }

    /// <summary>
    /// Create an event record
    /// </summary>
    public Dictionary<string, object> CreateEvent(string eventType, Dictionary<string, object>? payload = null)
    {
        var eventId = $"event-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        
        var @event = new Dictionary<string, object>
        {
            { "_id", eventId },
            { "event_type", eventType },
            { "status", "pending" },
            { "created_date", DateTime.UtcNow },
            { "created_by", "test_system" },
            { "retry_count", 0 },
            { "max_retries", 3 },
            { "error_details", null },
            { "sequence_number", GenerateSequenceNumber() }
        };

        if (payload != null)
        {
            @event["payload"] = payload;
        }

        return @event;
    }

    /// <summary>
    /// Create a batch operation record
    /// </summary>
    public Dictionary<string, object> CreateBatchOperation(string batchType, int recordCount, Dictionary<string, object>? metadata = null)
    {
        var batchId = $"batch-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        
        var batch = new Dictionary<string, object>
        {
            { "_id", batchId },
            { "batch_type", batchType },
            { "status", "queued" },
            { "created_date", DateTime.UtcNow },
            { "record_count", recordCount },
            { "processed_count", 0 },
            { "failed_count", 0 },
            { "skipped_count", 0 },
            { "started_date", null },
            { "completed_date", null },
            { "duration_ms", 0 }
        };

        if (metadata != null)
        {
            batch["metadata"] = metadata;
        }

        return batch;
    }

    /// <summary>
    /// Persist job to CouchDB
    /// </summary>
    public async Task<string> SaveJobAsync(Dictionary<string, object> job)
    {
        var jobId = job["_id"].ToString()!;
        var url = $"{_databaseUrl}/{jobId}";

        // TODO: Serialize job to JSON and POST to URL
        // await _httpClient.ExecuteAsync("POST", url, ...)

        await Task.CompletedTask;
        return jobId;
    }

    /// <summary>
    /// Retrieve job from CouchDB
    /// </summary>
    public async Task<Dictionary<string, object>?> GetJobAsync(string jobId)
    {
        var url = $"{_databaseUrl}/{jobId}";

        // TODO: GET from URL and deserialize
        // var response = await _httpClient.ExecuteAsync("GET", url, ...)

        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// Update job status
    /// </summary>
    public async Task UpdateJobStatusAsync(string jobId, string newStatus, Dictionary<string, object>? result = null)
    {
        var job = await GetJobAsync(jobId);
        if (job != null)
        {
            job["status"] = newStatus;
            job["last_modified_date"] = DateTime.UtcNow;

            if (newStatus == "completed")
            {
                job["completed_date"] = DateTime.UtcNow;
                if (result != null)
                {
                    job["result"] = result;
                }
            }
            else if (newStatus == "failed")
            {
                job["error_count"] = ((int)job.GetValueOrDefault("error_count", 0)) + 1;
            }

            await SaveJobAsync(job);
        }
    }

    /// <summary>
    /// Mark job for retry
    /// </summary>
    public async Task RetryJobAsync(string jobId, int delaySeconds = 60)
    {
        var job = await GetJobAsync(jobId);
        if (job != null)
        {
            var retryCount = (int)job.GetValueOrDefault("retry_count", 0);
            var maxRetries = (int)job.GetValueOrDefault("max_retries", 3);

            if (retryCount < maxRetries)
            {
                job["retry_count"] = retryCount + 1;
                job["status"] = "pending";
                job["next_execution_date"] = DateTime.UtcNow.AddSeconds(delaySeconds * (retryCount + 1)); // Exponential backoff
                await SaveJobAsync(job);
            }
            else
            {
                job["status"] = "failed";
                job["error_details"] = "Max retries exceeded";
                await SaveJobAsync(job);
            }
        }
    }

    /// <summary>
    /// Save event record to CouchDB
    /// </summary>
    public async Task<string> SaveEventAsync(Dictionary<string, object> @event)
    {
        var eventId = @event["_id"].ToString()!;
        var url = $"{_databaseUrl}/{eventId}";

        // TODO: Serialize event to JSON and POST
        // await _httpClient.ExecuteAsync("POST", url, ...)

        await Task.CompletedTask;
        return eventId;
    }

    /// <summary>
    /// Mark event as processed
    /// </summary>
    public async Task MarkEventProcessedAsync(string eventId)
    {
        var url = $"{_databaseUrl}/{eventId}";

        // TODO: GET event, update status to "processed", PUT back
        // var event = await GetEventAsync(eventId);
        // event["status"] = "processed";
        // event["processed_date"] = DateTime.UtcNow;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Move event to dead letter queue
    /// </summary>
    public async Task MoveEventToDeadLetterQueueAsync(string eventId, string errorReason)
    {
        var url = $"{_databaseUrl}/{eventId}";

        // TODO: Mark event as dead-lettered with error reason
        // event["status"] = "dead_letter";
        // event["dlq_reason"] = errorReason;
        // event["dlq_date"] = DateTime.UtcNow;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Update batch progress
    /// </summary>
    public async Task UpdateBatchProgressAsync(string batchId, int processedCount, int failedCount, int skippedCount)
    {
        var url = $"{_databaseUrl}/{batchId}";

        // TODO: GET batch and update counts
        // batch["processed_count"] = processedCount;
        // batch["failed_count"] = failedCount;
        // batch["skipped_count"] = skippedCount;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Mark batch as completed
    /// </summary>
    public async Task CompleteBatchAsync(string batchId, DateTime startTime)
    {
        var duration = DateTime.UtcNow - startTime;

        // TODO: Update batch with completion info
        // batch["status"] = "completed";
        // batch["completed_date"] = DateTime.UtcNow;
        // batch["duration_ms"] = (long)duration.TotalMilliseconds;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Generate a sequence number for ordering
    /// </summary>
    private long GenerateSequenceNumber()
    {
        return DateTime.UtcNow.Ticks;
    }

    /// <summary>
    /// Query for jobs matching criteria
    /// </summary>
    public JobQueryBuilder CreateJobQuery()
    {
        return new JobQueryBuilder();
    }

    /// <summary>
    /// Query for events matching criteria
    /// </summary>
    public EventQueryBuilder CreateEventQuery()
    {
        return new EventQueryBuilder();
    }
}

/// <summary>
/// JobQueryBuilder provides fluent interface for querying jobs
/// </summary>
public class JobQueryBuilder
{
    private readonly Dictionary<string, object> _selector = new();

    public JobQueryBuilder WithStatus(string status)
    {
        _selector["status"] = status;
        return this;
    }

    public JobQueryBuilder WithJobType(string jobType)
    {
        _selector["job_type"] = jobType;
        return this;
    }

    public JobQueryBuilder WithRetryCount(int count)
    {
        _selector["retry_count"] = count;
        return this;
    }

    public JobQueryBuilder CreatedAfter(DateTime date)
    {
        if (!_selector.ContainsKey("created_date"))
            _selector["created_date"] = new Dictionary<string, object>();

        ((Dictionary<string, object>)_selector["created_date"])["$gte"] = date;
        return this;
    }

    public Dictionary<string, object> Build()
    {
        return new Dictionary<string, object> { { "selector", _selector } };
    }
}

/// <summary>
/// EventQueryBuilder provides fluent interface for querying events
/// </summary>
public class EventQueryBuilder
{
    private readonly Dictionary<string, object> _selector = new();

    public EventQueryBuilder WithStatus(string status)
    {
        _selector["status"] = status;
        return this;
    }

    public EventQueryBuilder WithEventType(string eventType)
    {
        _selector["event_type"] = eventType;
        return this;
    }

    public EventQueryBuilder WithSequenceAfter(long sequenceNumber)
    {
        if (!_selector.ContainsKey("sequence_number"))
            _selector["sequence_number"] = new Dictionary<string, object>();

        ((Dictionary<string, object>)_selector["sequence_number"])["$gte"] = sequenceNumber;
        return this;
    }

    public Dictionary<string, object> Build()
    {
        return new Dictionary<string, object> { { "selector", _selector } };
    }
}
