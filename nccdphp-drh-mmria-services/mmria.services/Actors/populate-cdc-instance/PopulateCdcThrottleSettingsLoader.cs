using System;
using Microsoft.Extensions.Configuration;
using mmria.common.SharedLibraries.MMRIAServices.Model;

namespace mmria.services.populate_cdc_instance;

public static class PopulateCdcThrottleSettingsLoader
{
    public static PopulateCdcThrottleSettings Load(IConfiguration configuration)
    {
        var defaults = PopulateCdcThrottleSettings.CreateDefaults();

        return new PopulateCdcThrottleSettings
        {
            Copy = new PopulateCdcPhaseThrottleSettings
            {
                PageSize = GetConfiguredInteger(configuration, "populate_cdc_copy_page_size", defaults.Copy.PageSize, 1),
                MaxParallelism = GetConfiguredInteger(configuration, "populate_cdc_copy_max_parallelism", defaults.Copy.MaxParallelism, 1),
                BulkDocChunkSize = GetConfiguredInteger(configuration, "populate_cdc_copy_bulk_doc_chunk_size", defaults.Copy.BulkDocChunkSize, 0),
                BatchDelayMs = GetConfiguredInteger(configuration, "populate_cdc_copy_batch_delay_ms", defaults.Copy.BatchDelayMs, 0),
                BulkWriteRetryCount = GetConfiguredInteger(configuration, "populate_cdc_copy_bulk_write_retry_count", defaults.Copy.BulkWriteRetryCount, 0),
                BulkWriteRetryDelayMs = GetConfiguredInteger(configuration, "populate_cdc_copy_bulk_write_retry_delay_ms", defaults.Copy.BulkWriteRetryDelayMs, 0)
            },
            Rebuild = new PopulateCdcPhaseThrottleSettings
            {
                PageSize = GetConfiguredInteger(configuration, "populate_cdc_rebuild_page_size", defaults.Rebuild.PageSize, 1),
                MaxParallelism = GetConfiguredInteger(configuration, "populate_cdc_rebuild_max_parallelism", defaults.Rebuild.MaxParallelism, 1),
                BulkDocChunkSize = GetConfiguredInteger(configuration, "populate_cdc_rebuild_bulk_doc_chunk_size", defaults.Rebuild.BulkDocChunkSize, 0),
                BatchDelayMs = GetConfiguredInteger(configuration, "populate_cdc_rebuild_batch_delay_ms", defaults.Rebuild.BatchDelayMs, 0),
                BulkWriteRetryCount = GetConfiguredInteger(configuration, "populate_cdc_rebuild_bulk_write_retry_count", defaults.Rebuild.BulkWriteRetryCount, 0),
                BulkWriteRetryDelayMs = GetConfiguredInteger(configuration, "populate_cdc_rebuild_bulk_write_retry_delay_ms", defaults.Rebuild.BulkWriteRetryDelayMs, 0)
            }
        };
    }

    private static int GetConfiguredInteger(
        IConfiguration configuration,
        string key,
        int defaultValue,
        int minimumValue,
        int? maximumValue = null)
    {
        string raw_value =
            configuration?[$"mmria_settings:{key}"] ??
            configuration?[key];

        if(!int.TryParse(raw_value, out int configured_value))
        {
            configured_value = defaultValue;
        }

        configured_value = Math.Max(minimumValue, configured_value);

        if(maximumValue.HasValue)
        {
            configured_value = Math.Min(maximumValue.Value, configured_value);
        }

        return configured_value;
    }
}
