using System;

namespace mmria.common.SharedLibraries.MMRIAServices.Model;

public sealed class PopulateCdcPhaseThrottleSettings
{
    public int PageSize { get; init; }
    public int MaxParallelism { get; init; }
    public int BulkDocChunkSize { get; init; }
    public int BatchDelayMs { get; init; }
    public int BulkWriteRetryCount { get; init; }
    public int BulkWriteRetryDelayMs { get; init; }

    public string ToLogString()
    {
        string chunk_size_label = BulkDocChunkSize <= 0 ? "disabled" : BulkDocChunkSize.ToString();
        return
            $"page size {PageSize}, " +
            $"max parallelism {MaxParallelism}, " +
            $"bulk doc chunk size {chunk_size_label}, " +
            $"batch delay {BatchDelayMs} ms, " +
            $"bulk write retries {BulkWriteRetryCount}, " +
            $"bulk write retry delay {BulkWriteRetryDelayMs} ms";
    }
}

public sealed class PopulateCdcThrottleSettings
{
    public PopulateCdcPhaseThrottleSettings Copy { get; init; } = new();
    public PopulateCdcPhaseThrottleSettings Rebuild { get; init; } = new();

    public static PopulateCdcThrottleSettings CreateDefaults()
    {
        return new PopulateCdcThrottleSettings
        {
            Copy = new PopulateCdcPhaseThrottleSettings
            {
                PageSize = 100,
                MaxParallelism = 1,
                BulkDocChunkSize = 0,
                BatchDelayMs = 0,
                BulkWriteRetryCount = 0,
                BulkWriteRetryDelayMs = 0
            },
            Rebuild = new PopulateCdcPhaseThrottleSettings
            {
                PageSize = 25,
                MaxParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 2)),
                BulkDocChunkSize = 0,
                BatchDelayMs = 0,
                BulkWriteRetryCount = 0,
                BulkWriteRetryDelayMs = 0
            }
        };
    }
}
